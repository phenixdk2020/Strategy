using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6f - dense historical Amt approximation.
///
/// The n6e Herred-centre Voronoi field was still too sparse and could create long
/// artificial county arms. n6f uses the individual historical parish centroids
/// from the DigDag-derived 1820 dataset as the ownership field instead.
///
/// The SAME ownership resolver drives both the visible boundary mesh and map
/// clicks. This removes the previous possibility that the yellow line and the
/// information panel could disagree about which Amt the pointer was inside.
///
/// Historical guardrail: parish centroids approximate county boundaries; they are
/// not the exact dated 1851 Amt polygons. Exact DigDag Amt/Region polygons remain
/// the production target.
/// </summary>
[DefaultExecutionOrder(21800)]
public sealed class CampaignHistoricalAmtOverlayV010N6F : MonoBehaviour
{
    private sealed class ParishSite
    {
        public string ZoneId;
        public Vector2 Geo;
    }

    public const string GeometryMode = "DIGDAG_1820_PARISH_DENSE_FIELD_GRID_CONTOUR";
    private const string HistoricalSeedUrl =
        "https://raw.githubusercontent.com/christianvedels/A_perfect_storm/main/Data/Geo.csv";
    private const string HistoricalCacheFile = "PROJECT1864_DigDag1820_Geo.csv";
    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";
    private const string NewBoundaryRootName = "ZONE_LINES_10N6F_PARISH_GRID";
    private const string LegacyOverlayRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const string LegacyPolishRootName = "ZONE_LINES_10N5_SHARED_COLLINEAR";

    private const float RenderY = 0.765f;
    private const float BoundaryWidth = 0.020f;
    private const float GridStepDegrees = 0.025f;
    private const float ParishDedupDegrees = 0.018f;
    private const float CopenhagenCityRadiusDegrees = 0.105f;
    private const int MinimumParishSites = 500;

    public static CampaignHistoricalAmtOverlayV010N6F Instance { get; private set; }
    public static bool IsReady => Instance != null && Instance.ready;

    private readonly List<ParishSite> parishSites = new List<ParishSite>();
    private readonly List<List<Vector2>> landRings = new List<List<Vector2>>();
    private readonly Dictionary<string, int> zoneIndex = new Dictionary<string, int>();

    private bool ready;
    private bool visible = true;
    private string dataSource = "not-loaded";
    private GameObject boundaryRoot;
    private Material boundaryMaterial;
    private Vector2 copenhagenGeo;
    private int copenhagenZoneIndex = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignHistoricalAmtOverlayV010N6F>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_HISTORICAL_AMT_10N6F");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignHistoricalAmtOverlayV010N6F>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        boundaryMaterial = CreateBoundaryMaterial();
        BuildZoneIndex();
        StartCoroutine(InitializeHistoricalAmt());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && ready)
        {
            visible = !visible;
            if (boundaryRoot != null)
                boundaryRoot.SetActive(visible);

            Debug.Log(CampaignBuildInfo.LogTag + "|HistoricalAmtOverlay=" + visible + "|Toggle=Z");
        }
    }

    private IEnumerator InitializeHistoricalAmt()
    {
        while (GrandCampaignBootstrap.Instance == null || GameObject.Find(GeographyRootName) == null)
            yield return null;

        ReadLandRings();
        if (landRings.Count == 0)
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|HistoricalAmtOverlay=False|Reason=NoLandGeometry");
            yield break;
        }

        string text = null;
        string cachePath = Path.Combine(Application.persistentDataPath, HistoricalCacheFile);

        try
        {
            if (File.Exists(cachePath))
            {
                text = File.ReadAllText(cachePath);
                if (ParseParishSites(text))
                    dataSource = "PersistentCache";
                else
                    text = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(CampaignBuildInfo.LogTag + "|ParishCacheRead=False|Error=" + ex.Message);
            text = null;
        }

        if (parishSites.Count < MinimumParishSites)
        {
            parishSites.Clear();
            using (UnityWebRequest request = UnityWebRequest.Get(HistoricalSeedUrl))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success ||
                    string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    Debug.LogError(
                        CampaignBuildInfo.LogTag + "|HistoricalAmtOverlay=False|Reason=ParishDownloadFailed|Error=" + request.error);
                    yield break;
                }

                text = request.downloadHandler.text;
                if (!ParseParishSites(text))
                {
                    Debug.LogError(CampaignBuildInfo.LogTag + "|HistoricalAmtOverlay=False|Reason=ParishParseFailed");
                    yield break;
                }

                dataSource = "DigDagDerived1820Download";
                try { File.WriteAllText(cachePath, text); }
                catch (Exception ex)
                {
                    Debug.LogWarning(CampaignBuildInfo.LogTag + "|ParishCacheWrite=False|Error=" + ex.Message);
                }
            }
        }

        yield return BuildBoundaryMeshCoroutine();
        SuppressLegacyZoneLines();
        ready = boundaryRoot != null;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|HistoricalAmtOverlay=" + ready +
            "|Geometry=" + GeometryMode +
            "|ParishSites=" + parishSites.Count +
            "|LandParts=" + landRings.Count +
            "|DataSource=" + dataSource +
            "|ClickResolver=SameParishOwnershipField" +
            "|HistoricallyExact1851=False");
    }

    private void BuildZoneIndex()
    {
        zoneIndex.Clear();
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
            zoneIndex[zones[i].Id] = i;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i].Id == "COPENHAGEN" || cities[i].Name == "København")
            {
                copenhagenGeo = new Vector2(cities[i].Longitude, cities[i].Latitude);
                break;
            }
        }

        if (zoneIndex.TryGetValue("DK-Z01-KBH", out int kbh))
            copenhagenZoneIndex = kbh;
    }

    private bool ParseParishSites(string text)
    {
        parishSites.Clear();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        float dedupSqr = ParishDedupDegrees * ParishDedupDegrees;
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] f = lines[i].Split(';');
            if (f.Length < 6)
                continue;

            if (!TryMapCountyToZone(f[0].Trim(), out string zoneId))
                continue;

            string lonText = f[4].Trim().Replace(',', '.');
            string latText = f[5].Trim().Replace(',', '.');
            if (!float.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out float lon) ||
                !float.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out float lat))
                continue;

            Vector2 p = new Vector2(lon, lat);
            bool duplicate = false;
            for (int s = parishSites.Count - 1; s >= 0; s--)
            {
                ParishSite existing = parishSites[s];
                if (existing.ZoneId != zoneId)
                    continue;
                if ((existing.Geo - p).sqrMagnitude <= dedupSqr)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                parishSites.Add(new ParishSite { ZoneId = zoneId, Geo = p });
        }

        return parishSites.Count >= MinimumParishSites;
    }

    public static bool TryResolveWorld(Vector3 worldPoint, out string zoneId)
    {
        zoneId = null;
        if (!IsReady)
            return false;

        Vector2 geo = CampaignGeoProjection.Unproject(worldPoint);
        return Instance.TryResolveGeo(geo, out zoneId);
    }

    public bool TryResolveGeo(Vector2 geo, out string zoneId)
    {
        zoneId = null;
        if (!ready || !IsLand(geo))
            return false;

        int owner = ResolveOwnerIndex(geo);
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        if (owner < 0 || owner >= zones.Length)
            return false;

        zoneId = zones[owner].Id;
        return true;
    }

    private int ResolveOwnerIndex(Vector2 geo)
    {
        if (copenhagenZoneIndex >= 0)
        {
            float cosKbh = Mathf.Cos(geo.y * Mathf.Deg2Rad);
            float dx = (geo.x - copenhagenGeo.x) * cosKbh;
            float dy = geo.y - copenhagenGeo.y;
            if (dx * dx + dy * dy <= CopenhagenCityRadiusDegrees * CopenhagenCityRadiusDegrees)
                return copenhagenZoneIndex;
        }

        float cosLat = Mathf.Cos(geo.y * Mathf.Deg2Rad);
        float best = float.MaxValue;
        string bestZone = null;

        for (int i = 0; i < parishSites.Count; i++)
        {
            ParishSite site = parishSites[i];
            float dx = (site.Geo.x - geo.x) * cosLat;
            float dy = site.Geo.y - geo.y;
            float d = dx * dx + dy * dy;
            if (d < best)
            {
                best = d;
                bestZone = site.ZoneId;
            }
        }

        return bestZone != null && zoneIndex.TryGetValue(bestZone, out int index) ? index : -1;
    }

    private IEnumerator BuildBoundaryMeshCoroutine()
    {
        float minLon = float.MaxValue, maxLon = float.MinValue;
        float minLat = float.MaxValue, maxLat = float.MinValue;

        for (int r = 0; r < landRings.Count; r++)
        {
            List<Vector2> ring = landRings[r];
            for (int i = 0; i < ring.Count; i++)
            {
                minLon = Mathf.Min(minLon, ring[i].x);
                maxLon = Mathf.Max(maxLon, ring[i].x);
                minLat = Mathf.Min(minLat, ring[i].y);
                maxLat = Mathf.Max(maxLat, ring[i].y);
            }
        }

        int nx = Mathf.CeilToInt((maxLon - minLon) / GridStepDegrees) + 1;
        int ny = Mathf.CeilToInt((maxLat - minLat) / GridStepDegrees) + 1;
        int[,] owners = new int[nx, ny];

        for (int y = 0; y < ny; y++)
        {
            float lat = minLat + y * GridStepDegrees;
            for (int x = 0; x < nx; x++)
            {
                float lon = minLon + x * GridStepDegrees;
                Vector2 geo = new Vector2(lon, lat);
                owners[x, y] = IsLand(geo) ? ResolveOwnerIndex(geo) : -1;
            }

            if ((y & 7) == 0)
                yield return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int segmentCount = 0;

        for (int y = 0; y < ny; y++)
        {
            float lat = minLat + y * GridStepDegrees;
            for (int x = 0; x < nx; x++)
            {
                int a = owners[x, y];
                if (a < 0)
                    continue;

                float lon = minLon + x * GridStepDegrees;

                if (x + 1 < nx)
                {
                    int b = owners[x + 1, y];
                    if (b >= 0 && b != a)
                    {
                        float edgeLon = lon + GridStepDegrees * 0.5f;
                        AddBoundaryQuad(
                            new Vector2(edgeLon, lat - GridStepDegrees * 0.5f),
                            new Vector2(edgeLon, lat + GridStepDegrees * 0.5f),
                            vertices, triangles);
                        segmentCount++;
                    }
                }

                if (y + 1 < ny)
                {
                    int b = owners[x, y + 1];
                    if (b >= 0 && b != a)
                    {
                        float edgeLat = lat + GridStepDegrees * 0.5f;
                        AddBoundaryQuad(
                            new Vector2(lon - GridStepDegrees * 0.5f, edgeLat),
                            new Vector2(lon + GridStepDegrees * 0.5f, edgeLat),
                            vertices, triangles);
                        segmentCount++;
                    }
                }
            }

            if ((y & 15) == 0)
                yield return null;
        }

        if (boundaryRoot != null)
            Destroy(boundaryRoot);

        boundaryRoot = new GameObject(NewBoundaryRootName);
        DontDestroyOnLoad(boundaryRoot);

        Mesh mesh = new Mesh { name = "AMT_BOUNDARIES_10N6F" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        MeshFilter filter = boundaryRoot.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = boundaryRoot.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = boundaryMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|AmtBoundaryMesh=True|Grid=" + nx + "x" + ny +
            "|Segments=" + segmentCount +
            "|Vertices=" + vertices.Count +
            "|GridStep=" + GridStepDegrees.ToString("0.000"));
    }

    private static void AddBoundaryQuad(
        Vector2 geoA,
        Vector2 geoB,
        List<Vector3> vertices,
        List<int> triangles)
    {
        Vector3 a = CampaignGeoProjection.Project(geoA.x, geoA.y, RenderY);
        Vector3 b = CampaignGeoProjection.Project(geoB.x, geoB.y, RenderY);
        Vector3 d = b - a;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0000001f)
            return;

        d.Normalize();
        Vector3 p = new Vector3(-d.z, 0f, d.x) * (BoundaryWidth * 0.5f);
        int start = vertices.Count;
        vertices.Add(a - p);
        vertices.Add(a + p);
        vertices.Add(b + p);
        vertices.Add(b - p);
        triangles.Add(start + 0);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private void ReadLandRings()
    {
        landRings.Clear();
        GameObject root = GameObject.Find(GeographyRootName);
        if (root == null)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (!child.name.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Vector3[] verts = filter.sharedMesh.vertices;
            if (verts == null || verts.Length < 3)
                continue;

            List<Vector2> ring = new List<Vector2>(verts.Length);
            for (int v = 0; v < verts.Length; v++)
                ring.Add(CampaignGeoProjection.Unproject(child.TransformPoint(verts[v])));

            if (ring.Count >= 3)
                landRings.Add(ring);
        }
    }

    private bool IsLand(Vector2 geo)
    {
        for (int i = 0; i < landRings.Count; i++)
            if (PointInPolygon(geo, landRings[i]))
                return true;
        return false;
    }

    private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            if ((pi.y > point.y) != (pj.y > point.y))
            {
                float den = pj.y - pi.y;
                if (Mathf.Abs(den) > 0.0000001f)
                {
                    float x = (pj.x - pi.x) * (point.y - pi.y) / den + pi.x;
                    if (point.x < x)
                        inside = !inside;
                }
            }
            j = i;
        }
        return inside;
    }

    private void SuppressLegacyZoneLines()
    {
        GameObject legacy = GameObject.Find(LegacyOverlayRootName);
        if (legacy != null)
        {
            LineRenderer[] lines = legacy.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
                lines[i].enabled = false;
        }

        GameObject polish = GameObject.Find(LegacyPolishRootName);
        if (polish != null)
            Destroy(polish);

        CampaignZoneLinePolishV010N3 oldPolish = Object.FindAnyObjectByType<CampaignZoneLinePolishV010N3>();
        if (oldPolish != null)
            oldPolish.enabled = false;
    }

    private static bool TryMapCountyToZone(string county, out string zoneId)
    {
        zoneId = null;
        switch ((county ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "aalborg": zoneId = "DK-Z16-AAL"; return true;
            case "aarhus": zoneId = "DK-Z13-AAR"; return true;
            case "bornholm": zoneId = "DK-Z08-BOR"; return true;
            case "frederiksborg": zoneId = "DK-Z03-FRB"; return true;
            case "holbaek": zoneId = "DK-Z04-HOL"; return true;
            case "hjoerring": zoneId = "DK-Z17-HJO"; return true;
            case "koebenhavn": zoneId = "DK-Z02-KBH-AMT"; return true;
            case "maribo": zoneId = "DK-Z07-MAR"; return true;
            case "odense": zoneId = "DK-Z09-ODE"; return true;
            case "praestoe": zoneId = "DK-Z06-PRA"; return true;
            case "randers": zoneId = "DK-Z14-RAN"; return true;
            case "ribe": zoneId = "DK-Z20-RIB"; return true;
            case "ringkoebing": zoneId = "DK-Z19-RIN"; return true;
            case "skanderborg": zoneId = "DK-Z12-SKA"; return true;
            case "soroe": zoneId = "DK-Z05-SOR"; return true;
            case "svendborg": zoneId = "DK-Z10-SVE"; return true;
            case "thisted": zoneId = "DK-Z18-THI"; return true;
            case "vejle": zoneId = "DK-Z11-VEJ"; return true;
            case "viborg": zoneId = "DK-Z15-VIB"; return true;
            default: return false;
        }
    }

    private static Material CreateBoundaryMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "AMT_BOUNDARY_10N6F",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }
}
