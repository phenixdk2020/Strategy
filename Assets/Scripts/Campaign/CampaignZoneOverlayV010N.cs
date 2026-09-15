using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign3 zone overlay.
///
/// v00.00.10n6e replaces the sparse city/zone-centre-only Voronoi scaffold with
/// a much denser historical seed field derived from DigDag-origin parish data
/// (Danish parishes as of 1 January 1820, grouped to historical Amt/Herred).
///
/// The external source is christianvedels/A_perfect_storm Data/Geo.csv. The
/// repository documents its parish geography as originating from DigDag. n6e
/// groups parish centroids by County + Hundred and uses one averaged seed per
/// Herred, then adds canonical CITY-REG-01 and zone-centre guard sites. This keeps
/// the current land-clipped/no-overlap architecture while greatly reducing the
/// long county "arms" produced by the previous sparse seed set.
///
/// IMPORTANT: the historical seed source is 1820, not exact 1851 polygon data.
/// Therefore HistoricallyExactGeometry remains false. Exact 1851 production
/// geometry is still targeted at dated DigDag Amt/Region polygons.
/// </summary>
[DefaultExecutionOrder(21000)]
public sealed class CampaignZoneOverlayV010N : MonoBehaviour
{
    public const string GeometryMode = "DIGDAG_1820_HERRED_SEEDS_LAND_CLIPPED_APPROX";
    public const string FallbackGeometryMode = "FALLBACK_CANONICAL_CITY_ZONECENTRE_MULTI_SITE";
    public const string IntendedHistoricalSource = "DigDag - Amt og Region";

    private const string HistoricalSeedUrl =
        "https://raw.githubusercontent.com/christianvedels/A_perfect_storm/main/Data/Geo.csv";
    private const string HistoricalCacheFile = "PROJECT1864_DigDag1820_Geo.csv";
    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";
    private const string OverlayRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const float OverlayY = 0.74f;
    private const float BoundaryWidth = 0.040f;
    private const float MinPolygonArea = 0.000010f;
    private const float Epsilon = 0.000001f;
    private const float DuplicateSiteDistance = 0.0012f;
    private const int MinimumHistoricalHerredSeeds = 40;

    private sealed class InfluenceSite
    {
        public string ZoneId;
        public string Label;
        public Vector2 Point;
    }

    private sealed class SeedAccumulator
    {
        public string ZoneId;
        public string Label;
        public double LongitudeSum;
        public double LatitudeSum;
        public int Count;
    }

    private sealed class GeneratedPart
    {
        public string ZoneId;
        public readonly List<Vector2> Polygon = new List<Vector2>();
    }

    private readonly List<GameObject> overlayObjects = new List<GameObject>();
    private readonly List<InfluenceSite> loadedHistoricalSites = new List<InfluenceSite>();

    private GameObject overlayRoot;
    private Material boundaryMaterial;
    private bool built;
    private bool visible = true;
    private bool seedLoadComplete;
    private bool historicalSeedLoadSucceeded;
    private string historicalSeedSource = "not-loaded";
    private GUIStyle badgeStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneOverlayV010N>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZoneOverlay_CURRENT");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneOverlayV010N>();
    }

    private void Awake()
    {
        boundaryMaterial = CreateBoundaryMaterial();
        StartCoroutine(LoadHistoricalSeeds());
    }

    private IEnumerator LoadHistoricalSeeds()
    {
        seedLoadComplete = false;
        historicalSeedLoadSucceeded = false;
        loadedHistoricalSites.Clear();

        string cachePath = Path.Combine(Application.persistentDataPath, HistoricalCacheFile);
        string text = null;

        try
        {
            if (File.Exists(cachePath))
            {
                text = File.ReadAllText(cachePath);
                if (TryParseHistoricalSeeds(text, loadedHistoricalSites))
                {
                    historicalSeedLoadSucceeded = true;
                    historicalSeedSource = "PersistentCache";
                }
                else
                {
                    loadedHistoricalSites.Clear();
                    text = null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(CampaignBuildInfo.LogTag + "|HistoricalSeedCacheRead=False|Error=" + ex.Message);
            loadedHistoricalSites.Clear();
            text = null;
        }

        if (!historicalSeedLoadSucceeded)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(HistoricalSeedUrl))
            {
                request.timeout = 12;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success &&
                    !string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    text = request.downloadHandler.text;
                    loadedHistoricalSites.Clear();
                    if (TryParseHistoricalSeeds(text, loadedHistoricalSites))
                    {
                        historicalSeedLoadSucceeded = true;
                        historicalSeedSource = "DigDagDerivedGeo1820Download";

                        try
                        {
                            File.WriteAllText(cachePath, text);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                CampaignBuildInfo.LogTag + "|HistoricalSeedCacheWrite=False|Error=" + ex.Message);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(
                        CampaignBuildInfo.LogTag +
                        "|HistoricalSeedDownload=False|Url=" + HistoricalSeedUrl +
                        "|Error=" + request.error);
                }
            }
        }

        seedLoadComplete = true;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|HistoricalCountySeeds=" + historicalSeedLoadSucceeded +
            "|HistoricalSeedSource=" + historicalSeedSource +
            "|SeedCount=" + loadedHistoricalSites.Count +
            "|SourceDate=1820-01-01" +
            "|HistoricallyExact1851=False");
    }

    private static bool TryParseHistoricalSeeds(string text, List<InfluenceSite> output)
    {
        if (string.IsNullOrWhiteSpace(text) || output == null)
            return false;

        Dictionary<string, SeedAccumulator> groups = new Dictionary<string, SeedAccumulator>();
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(';');
            if (fields.Length < 6)
                continue;

            string county = fields[0].Trim();
            string hundred = fields[1].Trim();
            if (!TryMapCountyToZone(county, out string zoneId))
                continue;

            string lonText = fields[4].Trim().Replace(',', '.');
            string latText = fields[5].Trim().Replace(',', '.');
            if (!double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) ||
                !double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                continue;

            string key = zoneId + "|" + hundred;
            if (!groups.TryGetValue(key, out SeedAccumulator acc))
            {
                acc = new SeedAccumulator
                {
                    ZoneId = zoneId,
                    Label = county + "/" + hundred
                };
                groups[key] = acc;
            }

            acc.LongitudeSum += lon;
            acc.LatitudeSum += lat;
            acc.Count++;
        }

        foreach (KeyValuePair<string, SeedAccumulator> pair in groups)
        {
            SeedAccumulator acc = pair.Value;
            if (acc.Count <= 0)
                continue;

            AddSiteIfDistinct(
                output,
                acc.ZoneId,
                "HERRED_1820_" + acc.Label,
                new Vector2(
                    (float)(acc.LongitudeSum / acc.Count),
                    (float)(acc.LatitudeSum / acc.Count)));
        }

        int herredSeedCount = output.Count;
        if (herredSeedCount < MinimumHistoricalHerredSeeds)
        {
            output.Clear();
            return false;
        }

        // CITY-REG-01 guard sites keep every canonical market town on the correct
        // side of the approximation. Because the historical Herred field is dense,
        // these city cells stay local instead of producing the long sparse arms
        // seen in n5/n6d.
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            AddSiteIfDistinct(
                output,
                city.ZoneId,
                "CITY_GUARD_" + city.Id,
                new Vector2(city.Longitude, city.Latitude));
        }

        // Zone-centre guard sites preserve all 20 canonical zone identities and
        // give the separate København Stad zone a stable seed even though the
        // historical source is parish/county oriented.
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            AddSiteIfDistinct(
                output,
                zone.Id,
                "ZONE_GUARD_" + zone.Id,
                new Vector2(zone.Longitude, zone.Latitude));
        }

        return true;
    }

    private static bool TryMapCountyToZone(string county, out string zoneId)
    {
        zoneId = null;
        if (string.IsNullOrWhiteSpace(county))
            return false;

        switch (county.Trim().ToLowerInvariant())
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

    private void Update()
    {
        if (!built && seedLoadComplete && GrandCampaignBootstrap.Instance != null)
            TryBuildOverlay();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            visible = !visible;
            if (overlayRoot != null)
                overlayRoot.SetActive(visible);

            Debug.Log(
                CampaignBuildInfo.LogTag + "|ZoneOverlay=" + visible +
                "|Toggle=Z|Geometry=" +
                (historicalSeedLoadSucceeded ? GeometryMode : FallbackGeometryMode));
        }
    }

    private void TryBuildOverlay()
    {
        List<List<Vector2>> landRings = ReadLandRingsFromCampaignGeography();
        if (landRings.Count == 0)
            return;

        List<InfluenceSite> sites = historicalSeedLoadSucceeded
            ? new List<InfluenceSite>(loadedHistoricalSites)
            : BuildFallbackInfluenceSites();

        if (sites.Count == 0)
            return;

        built = true;

        GameObject previousRoot = GameObject.Find(OverlayRootName);
        if (previousRoot != null)
            Destroy(previousRoot);

        overlayRoot = new GameObject(OverlayRootName);
        DontDestroyOnLoad(overlayRoot);

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        Dictionary<string, CampaignDenmark1851Registry.ZoneDef> zoneById =
            new Dictionary<string, CampaignDenmark1851Registry.ZoneDef>();
        Dictionary<string, int> zonePartCounters = new Dictionary<string, int>();

        for (int i = 0; i < zones.Length; i++)
        {
            zoneById[zones[i].Id] = zones[i];
            zonePartCounters[zones[i].Id] = 0;
        }

        HashSet<string> zonesWithGeometry = new HashSet<string>();
        List<GeneratedPart> generatedParts = new List<GeneratedPart>();
        int polygonParts = 0;
        int segments = 0;
        int tinyPartsSkipped = 0;

        for (int s = 0; s < sites.Count; s++)
        {
            InfluenceSite site = sites[s];
            if (!zoneById.TryGetValue(site.ZoneId, out CampaignDenmark1851Registry.ZoneDef zone))
                continue;

            for (int r = 0; r < landRings.Count; r++)
            {
                List<Vector2> polygon = new List<Vector2>(landRings[r]);

                for (int o = 0; o < sites.Count; o++)
                {
                    if (o == s)
                        continue;

                    polygon = ClipToTargetHalfPlane(polygon, site.Point, sites[o].Point);
                    if (polygon.Count < 3)
                        break;
                }

                polygon = RemoveNearDuplicatePoints(polygon);
                if (polygon.Count < 3)
                    continue;

                float area = Mathf.Abs(SignedArea(polygon));
                if (area < MinPolygonArea)
                {
                    tinyPartsSkipped++;
                    continue;
                }

                int zonePart = zonePartCounters[zone.Id] + 1;
                zonePartCounters[zone.Id] = zonePart;
                CreateBoundary(zone, polygon, r + 1, zonePart, site.Label);

                GeneratedPart part = new GeneratedPart { ZoneId = zone.Id };
                part.Polygon.AddRange(polygon);
                generatedParts.Add(part);

                zonesWithGeometry.Add(zone.Id);
                polygonParts++;
                segments += polygon.Count;
            }
        }

        ValidateCanonicalAnchors(
            generatedParts,
            out int citiesCorrect,
            out int cityFailures,
            out int centresCorrect,
            out int centreFailures);

        string activeMode = historicalSeedLoadSucceeded ? GeometryMode : FallbackGeometryMode;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Installed=True|Overlay=1851Zones" +
            "|Zones=" + zonesWithGeometry.Count +
            "|InfluenceSites=" + sites.Count +
            "|PolygonParts=" + polygonParts +
            "|BoundarySegments=" + segments +
            "|TinyPartsSkipped=" + tinyPartsSkipped +
            "|LandClipped=True" +
            "|AreaOverlap=FalseByVoronoiConstruction" +
            "|CityAnchorValidation=" + citiesCorrect + "/" + CampaignDenmark1851Registry.Cities.Length +
            "|CityAnchorFailures=" + cityFailures +
            "|ZoneCentreValidation=" + centresCorrect + "/" + zones.Length +
            "|ZoneCentreFailures=" + centreFailures +
            "|HistoricalSeedSource=" + historicalSeedSource +
            "|Geometry=" + activeMode +
            "|HistoricallyExact1851=False" +
            "|HistoricalSourceTarget=DigDag_Amt_Region|Toggle=Z");
    }

    private static List<InfluenceSite> BuildFallbackInfluenceSites()
    {
        List<InfluenceSite> sites = new List<InfluenceSite>();

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            AddSiteIfDistinct(
                sites,
                zone.Id,
                "ZONECENTER_" + zone.Id,
                new Vector2(zone.Longitude, zone.Latitude));
        }

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            AddSiteIfDistinct(
                sites,
                city.ZoneId,
                "CITY_" + city.Id,
                new Vector2(city.Longitude, city.Latitude));
        }

        return sites;
    }

    private static void AddSiteIfDistinct(
        List<InfluenceSite> sites,
        string zoneId,
        string label,
        Vector2 point)
    {
        float sqrTolerance = DuplicateSiteDistance * DuplicateSiteDistance;
        for (int i = 0; i < sites.Count; i++)
        {
            InfluenceSite existing = sites[i];
            if (existing.ZoneId == zoneId && (existing.Point - point).sqrMagnitude <= sqrTolerance)
                return;
        }

        sites.Add(new InfluenceSite
        {
            ZoneId = zoneId,
            Label = label,
            Point = point
        });
    }

    private static void ValidateCanonicalAnchors(
        List<GeneratedPart> parts,
        out int citiesCorrect,
        out int cityFailures,
        out int centresCorrect,
        out int centreFailures)
    {
        citiesCorrect = 0;
        cityFailures = 0;
        centresCorrect = 0;
        centreFailures = 0;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            Vector2 point = new Vector2(city.Longitude, city.Latitude);
            if (PointIsInsideZone(parts, city.ZoneId, point))
                citiesCorrect++;
            else
                cityFailures++;
        }

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            Vector2 point = new Vector2(zone.Longitude, zone.Latitude);
            if (PointIsInsideZone(parts, zone.Id, point))
                centresCorrect++;
            else
                centreFailures++;
        }
    }

    private static bool PointIsInsideZone(List<GeneratedPart> parts, string zoneId, Vector2 point)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            GeneratedPart part = parts[i];
            if (part.ZoneId == zoneId && PointInPolygon(point, part.Polygon))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return false;

        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool yCross = (pi.y > point.y) != (pj.y > point.y);
            if (yCross)
            {
                float denominator = pj.y - pi.y;
                if (Mathf.Abs(denominator) > Epsilon)
                {
                    float xCross = (pj.x - pi.x) * (point.y - pi.y) / denominator + pi.x;
                    if (point.x < xCross)
                        inside = !inside;
                }
            }
            j = i;
        }
        return inside;
    }

    private static List<List<Vector2>> ReadLandRingsFromCampaignGeography()
    {
        List<List<Vector2>> rings = new List<List<Vector2>>();
        GameObject root = GameObject.Find(GeographyRootName);
        if (root == null)
            return rings;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (!child.name.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Vector3[] vertices = filter.sharedMesh.vertices;
            if (vertices == null || vertices.Length < 3)
                continue;

            List<Vector2> ring = new List<Vector2>(vertices.Length);
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 world = child.TransformPoint(vertices[v]);
                ring.Add(CampaignGeoProjection.Unproject(world));
            }

            ring = RemoveNearDuplicatePoints(ring);
            if (ring.Count >= 3)
                rings.Add(ring);
        }

        return rings;
    }

    private static List<Vector2> ClipToTargetHalfPlane(
        List<Vector2> input,
        Vector2 target,
        Vector2 other)
    {
        List<Vector2> output = new List<Vector2>();
        if (input == null || input.Count == 0)
            return output;

        Vector2 normal = other - target;
        if (normal.sqrMagnitude <= Epsilon * Epsilon)
            return new List<Vector2>(input);

        float threshold = (other.sqrMagnitude - target.sqrMagnitude) * 0.5f;
        Vector2 previous = input[input.Count - 1];
        float previousValue = Vector2.Dot(previous, normal) - threshold;
        bool previousInside = previousValue <= Epsilon;

        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            float currentValue = Vector2.Dot(current, normal) - threshold;
            bool currentInside = currentValue <= Epsilon;

            if (currentInside != previousInside)
            {
                float denominator = previousValue - currentValue;
                float t = Mathf.Abs(denominator) < Epsilon
                    ? 0.5f
                    : previousValue / denominator;
                output.Add(Vector2.Lerp(previous, current, Mathf.Clamp01(t)));
            }

            if (currentInside)
                output.Add(current);

            previous = current;
            previousValue = currentValue;
            previousInside = currentInside;
        }

        return output;
    }

    private static List<Vector2> RemoveNearDuplicatePoints(List<Vector2> input)
    {
        List<Vector2> output = new List<Vector2>();
        if (input == null || input.Count == 0)
            return output;

        float sqrTolerance = Epsilon * Epsilon;
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 point = input[i];
            if (output.Count == 0 || (output[output.Count - 1] - point).sqrMagnitude > sqrTolerance)
                output.Add(point);
        }

        if (output.Count > 2 && (output[0] - output[output.Count - 1]).sqrMagnitude <= sqrTolerance)
            output.RemoveAt(output.Count - 1);

        return output;
    }

    private static float SignedArea(List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return 0f;

        float area = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    private void CreateBoundary(
        CampaignDenmark1851Registry.ZoneDef zone,
        List<Vector2> polygon,
        int landPart,
        int zonePart,
        string siteLabel)
    {
        GameObject go = new GameObject(
            "ZONE_BORDER_" + zone.Id + "_LAND_" + landPart + "_PART_" + zonePart);
        go.transform.SetParent(overlayRoot.transform, false);
        overlayObjects.Add(go);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = polygon.Count;
        line.widthMultiplier = BoundaryWidth;
        line.sharedMaterial = boundaryMaterial;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p = polygon[i];
            line.SetPosition(i, CampaignGeoProjection.Project(p.x, p.y, OverlayY));
        }

        go.AddComponent<CampaignZoneOverlayMetadataV010N>().Initialize(
            zone.Id,
            zone.Name,
            (historicalSeedLoadSucceeded ? "HISTORICAL_SEED:" : "FALLBACK_SEED:") + siteLabel,
            false,
            true,
            true);
    }

    private static Material CreateBoundaryMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_HISTORICAL_SEED_APPROX",
            color = new Color(0.95f, 0.78f, 0.24f, 1.00f)
        };
    }

    private void EnsureGuiStyle()
    {
        if (badgeStyle != null)
            return;

        badgeStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        badgeStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!CampaignHudStateV010N2.DebugVisible)
            return;

        EnsureGuiStyle();
        string state = visible ? "ON" : "OFF";
        string mode = historicalSeedLoadSucceeded ? "DigDag-derived 1820 Herred seeds" : "fallback seeds";
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 390f), Screen.height - 52f, 382f, 44f);
        GUI.Box(
            rect,
            CampaignBuildInfo.CurrentVersion + " | ZONES " + state + " | Z toggle\n" +
            mode + " · land-clipped · 1851 exact polygons still target",
            badgeStyle);
    }
}

public sealed class CampaignZoneOverlayMetadataV010N : MonoBehaviour
{
    public string ZoneId { get; private set; }
    public string ZoneName { get; private set; }
    public string GroupName { get; private set; }
    public bool HistoricallyExactGeometry { get; private set; }
    public bool LandClipped { get; private set; }
    public bool AreaOverlapFreeByConstruction { get; private set; }

    public void Initialize(
        string zoneId,
        string zoneName,
        string groupName,
        bool historicallyExactGeometry,
        bool landClipped,
        bool areaOverlapFreeByConstruction)
    {
        ZoneId = zoneId;
        ZoneName = zoneName;
        GroupName = groupName;
        HistoricallyExactGeometry = historicallyExactGeometry;
        LandClipped = landClipped;
        AreaOverlapFreeByConstruction = areaOverlapFreeByConstruction;
    }
}
