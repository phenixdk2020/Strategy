using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13j — Historical 3D Map Foundation.
// Rebuilds the v13i GIS pilot into one smooth high-resolution mainland Denmark mesh,
// applies a single seamless cartographic texture, shrinks settlement miniatures,
// replaces oversized construction dressing and enables close map inspection.
// Strategic lat/lon, route distance, ETA and campaign simulation are untouched.
[DefaultExecutionOrder(4320)]
public sealed class CampaignHistorical3DMapV013J : MonoBehaviour
{
    public const string BuildTag = "v00.00.13j";
    public const string RootName = "V013J_HISTORICAL_3D_MAP";

    private const int TerrainZoom = 8;
    private const int GridX = 520;
    private const int GridZ = 340;
    private const float WaterY = 0.12f;
    private const float ElevationToUnity = 0.0062f;

    private const double MinLat = 54.48;
    private const double MaxLat = 57.82;
    private const double MinLon = 7.70;
    private const double MaxLon = 13.05;

    private readonly List<HydroLine> hydrology = new List<HydroLine>();
    private readonly Dictionary<long, TerrainTile> terrainTiles = new Dictionary<long, TerrainTile>();
    private readonly List<CityVisual> cityVisuals = new List<CityVisual>();

    private CampaignDenmarkGisFoundationV013I v13i;
    private Transform root;
    private Transform terrainRoot;
    private Transform cityRoot;
    private Transform constructionRoot;

    private Vector2[][] denmarkRings;
    private RingBounds[] ringBounds;

    private Material terrainMaterial;
    private Material cityWallMaterial;
    private Material cityRoofMaterial;
    private Material harborMaterial;
    private Material barracksMaterial;
    private Material farmMaterial;
    private Material fieldMaterial;

    private bool buildStarted;
    private bool ready;
    private string status = "venter på GIS-cache";
    private string mapTextureSource = "procedural cartographic fallback";

    private GUIStyle statusStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignHistorical3DMapV013J>() != null)
            return;

        GameObject go = new GameObject("CampaignHistorical3DMapV013J");
        go.AddComponent<CampaignHistorical3DMapV013J>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        BuildHydrologyData();
        ResolveDenmarkRings();
        CreateMaterials();
        ConfigureCamera();
    }

    private void Update()
    {
        if (!buildStarted)
        {
            v13i = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkGisFoundationV013I>();
            if (v13i != null && IsV13IReady(v13i))
            {
                buildStarted = true;
                StartCoroutine(BuildHistoricalMap());
            }
        }

        if (ready)
        {
            HideLegacyOversizedVisuals();
            UpdateCityLod();
        }
    }

    private IEnumerator BuildHistoricalMap()
    {
        status = "indlæser DEM-cache";
        LoadTerrainTileCache();
        yield return null;

        status = "bygger glat 3D-kort";
        CreateRootHierarchy();
        yield return BuildSmoothTerrain();

        status = "bygger små byer og anlæg";
        HideLegacyOversizedVisuals();
        BuildSmallSettlements();
        BuildAalborgHarbourAndBarracks();
        BuildAarhusFarm();
        RetuneExistingInfrastructure();
        ConfigureCamera();

        if (v13i != null)
            v13i.enabled = false;

        ready = true;
        status = "Historical 3D map klar · " + mapTextureSource;
        Debug.Log("CAMPAIGN-V013J|Historical3DMap=True|SmoothMesh=True|Grid=520x340|SingleCartographicTexture=True|SmallSettlements=True|CloseZoom=True|MapOnly=True|SimulationChanged=False");
    }

    private bool IsV13IReady(CampaignDenmarkGisFoundationV013I component)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo terrainReady = typeof(CampaignDenmarkGisFoundationV013I).GetField("terrainReady", flags);
        FieldInfo finishingPassDone = typeof(CampaignDenmarkGisFoundationV013I).GetField("finishingPassDone", flags);
        return terrainReady != null && finishingPassDone != null &&
               (bool)terrainReady.GetValue(component) && (bool)finishingPassDone.GetValue(component);
    }

    private void ResolveDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField("DenmarkRings", BindingFlags.Static | BindingFlags.NonPublic);
        denmarkRings = field != null ? field.GetValue(null) as Vector2[][] : null;
        if (denmarkRings == null)
        {
            ringBounds = new RingBounds[0];
            return;
        }

        ringBounds = new RingBounds[denmarkRings.Length];
        for (int r = 0; r < denmarkRings.Length; r++)
        {
            Vector2[] ring = denmarkRings[r];
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            foreach (Vector2 p in ring)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            ringBounds[r] = new RingBounds(minX, maxX, minY, maxY);
        }
    }

    private void CreateRootHierarchy()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Destroy(old);

        root = new GameObject(RootName).transform;
        terrainRoot = CreateChild(root, "L1_SMOOTH_TERRAIN");
        cityRoot = CreateChild(root, "L5_SMALL_SETTLEMENTS");
        constructionRoot = CreateChild(root, "L6_SMALL_CONSTRUCTION");
    }

    private IEnumerator BuildSmoothTerrain()
    {
        int width = GridX + 1;
        int height = GridZ + 1;
        int count = width * height;

        Vector3[] vertices = new Vector3[count];
        Vector2[] uv = new Vector2[count];
        bool[] land = new bool[count];
        float[] elevation = new float[count];

        for (int z = 0; z < height; z++)
        {
            float vz = z / (float)GridZ;
            double lat = MinLat + (MaxLat - MinLat) * vz;

            for (int x = 0; x < width; x++)
            {
                float ux = x / (float)GridX;
                double lon = MinLon + (MaxLon - MinLon) * ux;
                int index = z * width + x;

                bool isLand = IsDenmarkLand(lat, lon) && !IsHydrologyWater(lat, lon);
                land[index] = isLand;
                elevation[index] = isLand ? Mathf.Clamp(SampleElevationMeters(lat, lon), 0f, 220f) : 0f;
                uv[index] = new Vector2(ux, vz);
            }

            if ((z & 15) == 0)
            {
                status = string.Format("bygger glat 3D-kort {0}%", Mathf.RoundToInt(vz * 100f));
                yield return null;
            }
        }

        SmoothElevation(elevation, land, width, height, 2, 0.32f);

        for (int z = 0; z < height; z++)
        {
            float vz = z / (float)GridZ;
            double lat = MinLat + (MaxLat - MinLat) * vz;
            for (int x = 0; x < width; x++)
            {
                float ux = x / (float)GridX;
                double lon = MinLon + (MaxLon - MinLon) * ux;
                int index = z * width + x;
                float y = WaterY + 0.05f + elevation[index] * ElevationToUnity;
                vertices[index] = CampaignGeoProjection.Project3D(lat, lon, y);
            }
        }

        List<int> triangles = new List<int>(GridX * GridZ * 6);
        for (int z = 0; z < GridZ; z++)
        {
            for (int x = 0; x < GridX; x++)
            {
                int i0 = z * width + x;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;
                if (!(land[i0] && land[i1] && land[i2] && land[i3]))
                    continue;

                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh mesh = new Mesh
        {
            name = "V013J_Denmark_Mainland_Smooth_Mesh",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            uv = uv,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject terrain = new GameObject("V013J_SmoothTerrain");
        terrain.transform.SetParent(terrainRoot, false);
        MeshFilter filter = terrain.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = terrain.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = terrainMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        MeshCollider collider = terrain.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        HideOldTerrainTiles();
    }

    private void SmoothElevation(float[] values, bool[] land, int width, int height, int passes, float strength)
    {
        float[] buffer = new float[values.Length];
        for (int pass = 0; pass < passes; pass++)
        {
            Array.Copy(values, buffer, values.Length);
            for (int z = 1; z < height - 1; z++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int i = z * width + x;
                    if (!land[i])
                        continue;

                    float sum = values[i];
                    int n = 1;
                    int[] neighbours = { i - 1, i + 1, i - width, i + width };
                    foreach (int j in neighbours)
                    {
                        if (land[j])
                        {
                            sum += values[j];
                            n++;
                        }
                    }
                    buffer[i] = Mathf.Lerp(values[i], sum / n, strength);
                }
            }
            Array.Copy(buffer, values, values.Length);
        }
    }

    private void LoadTerrainTileCache()
    {
        terrainTiles.Clear();
        int xMin = LonToTileX(MinLon, TerrainZoom);
        int xMax = LonToTileX(MaxLon, TerrainZoom);
        int yMin = LatToTileY(MaxLat, TerrainZoom);
        int yMax = LatToTileY(MinLat, TerrainZoom);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                string path = GetTerrainCachePath(TerrainZoom, x, y);
                if (!File.Exists(path))
                    continue;

                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                    texture.LoadImage(bytes, false);
                    terrainTiles[TileKey(x, y)] = new TerrainTile(texture.width, texture.height, texture.GetPixels32());
                    Destroy(texture);
                }
                catch (Exception ex)
                {
                    Debug.Log("CAMPAIGN-V013J|TerrainCacheRead=False|" + ex.Message);
                }
            }
        }
    }

    private float SampleElevationMeters(double lat, double lon)
    {
        double n = Math.Pow(2.0, TerrainZoom);
        double tx = (lon + 180.0) / 360.0 * n;
        double latRad = lat * Math.PI / 180.0;
        double ty = (1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) * 0.5 * n;
        int x = (int)Math.Floor(tx);
        int y = (int)Math.Floor(ty);

        TerrainTile tile;
        if (!terrainTiles.TryGetValue(TileKey(x, y), out tile))
            return FallbackElevationMeters(lat, lon);

        float u = (float)(tx - x);
        float v = (float)(ty - y);
        float px = Mathf.Clamp(u * (tile.Width - 1), 0f, tile.Width - 1);
        float py = Mathf.Clamp(v * (tile.Height - 1), 0f, tile.Height - 1);

        int x0 = Mathf.FloorToInt(px);
        int y0 = Mathf.FloorToInt(py);
        int x1 = Mathf.Min(x0 + 1, tile.Width - 1);
        int y1 = Mathf.Min(y0 + 1, tile.Height - 1);
        float fx = px - x0;
        float fy = py - y0;

        float a = DecodeTerrarium(tile.Pixels[y0 * tile.Width + x0]);
        float b = DecodeTerrarium(tile.Pixels[y0 * tile.Width + x1]);
        float c = DecodeTerrarium(tile.Pixels[y1 * tile.Width + x0]);
        float d = DecodeTerrarium(tile.Pixels[y1 * tile.Width + x1]);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float DecodeTerrarium(Color32 c)
    {
        return c.r * 256f + c.g + c.b / 256f - 32768f;
    }

    private float FallbackElevationMeters(double lat, double lon)
    {
        float nx = (float)((lon - MinLon) * 0.72);
        float nz = (float)((lat - MinLat) * 0.72);
        float broad = Mathf.PerlinNoise(nx, nz);
        float fine = Mathf.PerlinNoise(nx * 2.1f + 13.1f, nz * 2.1f + 7.4f);
        return Mathf.Clamp((broad * 0.74f + fine * 0.26f) * 48f - 7f, 0f, 90f);
    }

    private void CreateMaterials()
    {
        Texture2D mapTexture = TryLoadHistoricalTexture();
        if (mapTexture == null)
            mapTexture = CreateProceduralCartographicTexture(1024, 768);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        terrainMaterial = new Material(shader) { name = "V013J_CartographicTerrain" };
        terrainMaterial.color = Color.white;
        if (terrainMaterial.HasProperty("_BaseMap")) terrainMaterial.SetTexture("_BaseMap", mapTexture);
        if (terrainMaterial.HasProperty("_MainTex")) terrainMaterial.SetTexture("_MainTex", mapTexture);
        if (terrainMaterial.HasProperty("_Smoothness")) terrainMaterial.SetFloat("_Smoothness", 0.02f);
        if (terrainMaterial.HasProperty("_Metallic")) terrainMaterial.SetFloat("_Metallic", 0f);

        cityWallMaterial = CreateLitMaterial(new Color(0.72f, 0.67f, 0.56f), "V013J_CityWall");
        cityRoofMaterial = CreateLitMaterial(new Color(0.42f, 0.22f, 0.16f), "V013J_CityRoof");
        harborMaterial = CreateLitMaterial(new Color(0.48f, 0.39f, 0.27f), "V013J_Harbor");
        barracksMaterial = CreateLitMaterial(new Color(0.66f, 0.43f, 0.30f), "V013J_Barracks");
        farmMaterial = CreateLitMaterial(new Color(0.62f, 0.52f, 0.34f), "V013J_Farm");
        fieldMaterial = CreateLitMaterial(new Color(0.61f, 0.58f, 0.37f), "V013J_Field");
    }

    private Texture2D TryLoadHistoricalTexture()
    {
        string path = Path.Combine(Application.persistentDataPath, "PROJECT1864", "HistoricalMap", "denmark_1864.png");
        if (!File.Exists(path))
            return null;

        try
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            texture.LoadImage(File.ReadAllBytes(path), false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            mapTextureSource = "lokal historical raster: denmark_1864.png";
            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-V013J|HistoricalTexture=False|" + ex.Message);
            return null;
        }
    }

    private Texture2D CreateProceduralCartographicTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            name = "V013J_ProceduralHistoricalCartography",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[width * height];
        Color low = new Color(0.62f, 0.67f, 0.47f);
        Color mid = new Color(0.55f, 0.62f, 0.42f);
        Color high = new Color(0.72f, 0.68f, 0.49f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float broad = Mathf.PerlinNoise(u * 8.0f + 11.3f, v * 8.0f + 4.7f);
                float fine = Mathf.PerlinNoise(u * 34.0f + 2.4f, v * 34.0f + 19.1f);
                Color baseColor = Color.Lerp(low, mid, broad);
                baseColor = Color.Lerp(baseColor, high, Mathf.Clamp01((fine - 0.58f) * 1.5f));
                float paper = 0.96f + (fine - 0.5f) * 0.05f;
                baseColor *= paper;
                pixels[y * width + x] = baseColor;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private void BuildSmallSettlements()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;
            if (node.Id == "AALBORG")
                continue;

            Vector3 p = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, 0f);
            p.y = SampleGroundHeight(p.x, p.z) + 0.025f;
            bool major = node.Id == "AARHUS" || node.Id == "ODENSE" || node.Id == "CPH" || node.Id == "FREDERICIA";
            GameObject visual = CreateMiniTown("V013J_City_" + node.Id, p, major ? 4 : 2, major ? 0.12f : 0.085f, node.HasPort);
            cityVisuals.Add(new CityVisual(node.Id, visual, major));
        }
    }

    private void BuildAalborgHarbourAndBarracks()
    {
        CampaignNodeState node = CampaignSession.GetNode("AALBORG");
        if (node == null)
            return;

        Vector3 p = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, 0f);
        p.y = SampleGroundHeight(p.x, p.z) + 0.025f;
        GameObject city = CreateMiniTown("V013J_City_AALBORG", p, 5, 0.13f, true);
        cityVisuals.Add(new CityVisual(node.Id, city, true));

        Transform harbour = new GameObject("V013J_Aalborg_Harbour").transform;
        harbour.SetParent(constructionRoot, false);
        Vector3 hp = CampaignGeoProjection.Project3D(57.055, 9.920, WaterY + 0.04f);
        harbour.position = hp;
        CreateBox(harbour, "QuayA", new Vector3(-0.18f, 0.02f, 0f), new Vector3(0.32f, 0.025f, 0.045f), harborMaterial);
        CreateBox(harbour, "QuayB", new Vector3(0.18f, 0.02f, 0f), new Vector3(0.32f, 0.025f, 0.045f), harborMaterial);
        CreateBox(harbour, "Warehouse", new Vector3(-0.12f, 0.07f, -0.10f), new Vector3(0.12f, 0.10f, 0.08f), cityWallMaterial);

        Vector3 bp = p + new Vector3(-0.34f, 0f, -0.28f);
        bp.y = SampleGroundHeight(bp.x, bp.z) + 0.02f;
        Transform barracks = new GameObject("V013J_Aalborg_Barracks").transform;
        barracks.SetParent(constructionRoot, false);
        barracks.position = bp;
        CreateBox(barracks, "ParadeGround", Vector3.zero, new Vector3(0.34f, 0.012f, 0.22f), fieldMaterial);
        CreateBox(barracks, "BlockA", new Vector3(-0.09f, 0.045f, -0.04f), new Vector3(0.14f, 0.08f, 0.05f), barracksMaterial);
        CreateBox(barracks, "BlockB", new Vector3(0.09f, 0.045f, -0.04f), new Vector3(0.14f, 0.08f, 0.05f), barracksMaterial);
        CreateBox(barracks, "Store", new Vector3(0f, 0.04f, 0.07f), new Vector3(0.09f, 0.07f, 0.06f), barracksMaterial);
    }

    private void BuildAarhusFarm()
    {
        CampaignNodeState node = CampaignSession.GetNode("AARHUS");
        if (node == null)
            return;

        Vector3 p = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, 0f) + new Vector3(-0.28f, 0f, 0.23f);
        p.y = SampleGroundHeight(p.x, p.z) + 0.02f;
        Transform farm = new GameObject("V013J_Aarhus_Farm").transform;
        farm.SetParent(constructionRoot, false);
        farm.position = p;
        CreateBox(farm, "FieldA", new Vector3(-0.08f, 0f, 0f), new Vector3(0.18f, 0.01f, 0.16f), fieldMaterial);
        CreateBox(farm, "FieldB", new Vector3(0.12f, 0f, 0.02f), new Vector3(0.14f, 0.01f, 0.13f), fieldMaterial);
        CreateBox(farm, "FarmHouse", new Vector3(-0.02f, 0.045f, -0.10f), new Vector3(0.10f, 0.08f, 0.07f), farmMaterial);
        CreateBox(farm, "Barn", new Vector3(0.10f, 0.04f, -0.09f), new Vector3(0.12f, 0.07f, 0.06f), farmMaterial);
    }

    private GameObject CreateMiniTown(string name, Vector3 centre, int houses, float scale, bool port)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(cityRoot, false);
        go.transform.position = centre;

        for (int i = 0; i < houses; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.08f + Mathf.Sqrt(i + 1) * 0.035f;
            Vector3 local = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            CreateHouse(go.transform, "House_" + i, local, scale);
        }

        CreateBox(go.transform, "Church", new Vector3(0f, scale * 0.55f, 0f), new Vector3(scale * 0.32f, scale * 1.1f, scale * 0.32f), cityWallMaterial);
        if (port)
            CreateBox(go.transform, "Pier", new Vector3(scale * 1.1f, 0.01f, scale * 0.55f), new Vector3(scale * 1.4f, 0.015f, scale * 0.16f), harborMaterial);
        return go;
    }

    private void CreateHouse(Transform parent, string name, Vector3 local, float scale)
    {
        CreateBox(parent, name, local + new Vector3(0f, scale * 0.20f, 0f), new Vector3(scale * 0.72f, scale * 0.40f, scale * 0.58f), cityWallMaterial);
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = name + "_Roof";
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = local + new Vector3(0f, scale * 0.43f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        roof.transform.localScale = new Vector3(scale * 0.58f, scale * 0.12f, scale * 0.46f);
        roof.GetComponent<Renderer>().sharedMaterial = cityRoofMaterial;
        RemoveCollider(roof);
    }

    private void HideLegacyOversizedVisuals()
    {
        HideObjectTree("V013I_DENMARK_GIS_FOUNDATION/L5_SETTLEMENTS");
        HideObjectTree("V013I_DENMARK_GIS_FOUNDATION/L7_DIORAMA_DETAILS");
        HideObjectTree("ConstructionProject_QA-BARRACKS-AALBORG");
        HideObjectTree("ConstructionProject_QA-FARM-AARHUS");
        HideObjectTree("GIS_Aalborg_Barracks_Construction");

        GameObject v13iRoot = GameObject.Find("V013I_DENMARK_GIS_FOUNDATION");
        if (v13iRoot != null)
        {
            LineRenderer[] lines = v13iRoot.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer line in lines)
            {
                if (line != null && line.name.StartsWith("GIS_Coast_", StringComparison.Ordinal))
                    line.enabled = false;
            }
        }
    }

    private void HideOldTerrainTiles()
    {
        GameObject v13iRoot = GameObject.Find("V013I_DENMARK_GIS_FOUNDATION");
        if (v13iRoot == null)
            return;

        MeshRenderer[] renderers = v13iRoot.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.name.StartsWith("GIS_Terrain_", StringComparison.Ordinal))
                renderer.enabled = false;
        }

        MeshCollider[] colliders = v13iRoot.GetComponentsInChildren<MeshCollider>(true);
        foreach (MeshCollider collider in colliders)
        {
            if (collider != null && collider.gameObject.name.StartsWith("GIS_Terrain_", StringComparison.Ordinal))
                collider.enabled = false;
        }
    }

    private void RetuneExistingInfrastructure()
    {
        GameObject v13iRoot = GameObject.Find("V013I_DENMARK_GIS_FOUNDATION");
        if (v13iRoot == null)
            return;

        LineRenderer[] lines = v13iRoot.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;
            if (line.name.StartsWith("GIS_Road_", StringComparison.Ordinal))
                line.widthMultiplier = 0.055f;
            else if (line.name.StartsWith("GIS_Ferry_", StringComparison.Ordinal))
                line.widthMultiplier = 0.045f;
        }
    }

    private void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.nearClipPlane = 0.02f;
        cam.fieldOfView = 48f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.TerrainClearance = 0.70f;
            controller.MinHeight = 0.70f;
            controller.MaxHeight = 230f;
            controller.PanSpeed = 38f;
            controller.ZoomSpeed = 125f;
            controller.MinPitch = 22f;
            controller.MaxPitch = 78f;
        }
    }

    private void UpdateCityLod()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float h = cam.transform.position.y;
        foreach (CityVisual city in cityVisuals)
        {
            if (city.Root == null)
                continue;
            bool visible = h < 52f || city.Major;
            SetRenderersVisible(city.Root, visible);
        }
    }

    private float SampleGroundHeight(float x, float z)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out hit, 220f) && hit.collider != null)
            return hit.point.y;
        return WaterY + 0.08f;
    }

    private bool IsDenmarkLand(double lat, double lon)
    {
        if (denmarkRings == null || denmarkRings.Length == 0)
            return lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;

        Vector2 p = new Vector2((float)lon, (float)lat);
        for (int i = 0; i < denmarkRings.Length; i++)
        {
            RingBounds b = ringBounds[i];
            if (p.x < b.MinX || p.x > b.MaxX || p.y < b.MinY || p.y > b.MaxY)
                continue;
            if (PointInPolygon(p, denmarkRings[i]))
                return true;
        }
        return false;
    }

    private bool IsHydrologyWater(double lat, double lon)
    {
        foreach (HydroLine feature in hydrology)
        {
            if (DistanceToPolylineKm(lat, lon, feature.Points) <= feature.WidthKm * 0.5f)
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                              (point.x < (pj.x - pi.x) * (point.y - pi.y) / ((pj.y - pi.y) == 0f ? 0.000001f : (pj.y - pi.y)) + pi.x);
            if (intersects) inside = !inside;
        }
        return inside;
    }

    private static float DistanceToPolylineKm(double lat, double lon, Vector2[] line)
    {
        if (line == null || line.Length < 2)
            return float.MaxValue;

        float best = float.MaxValue;
        for (int i = 0; i < line.Length - 1; i++)
        {
            double lat0 = line[i].y;
            double lon0 = line[i].x;
            double lat1 = line[i + 1].y;
            double lon1 = line[i + 1].x;
            double refLat = (lat + lat0 + lat1) / 3.0;
            double cos = Math.Cos(refLat * Math.PI / 180.0);
            Vector2 p = new Vector2((float)(lon * 111.32 * cos), (float)(lat * 111.32));
            Vector2 a = new Vector2((float)(lon0 * 111.32 * cos), (float)(lat0 * 111.32));
            Vector2 b = new Vector2((float)(lon1 * 111.32 * cos), (float)(lat1 * 111.32));
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            float d = Vector2.Distance(p, a + ab * t);
            if (d < best) best = d;
        }
        return best;
    }

    private void BuildHydrologyData()
    {
        hydrology.Add(new HydroLine(5.6f, new[] { LL(8.22,56.71),LL(8.45,56.70),LL(8.72,56.69),LL(8.95,56.72),LL(9.16,56.80),LL(9.34,56.91),LL(9.55,56.99),LL(9.75,57.04),LL(9.94,57.05),LL(10.12,57.05),LL(10.31,57.06),LL(10.52,57.07),LL(10.72,57.10) }));
        hydrology.Add(new HydroLine(1.9f, new[] { LL(9.94,56.65),LL(10.08,56.69),LL(10.28,56.70),LL(10.48,56.70),LL(10.62,56.72) }));
        hydrology.Add(new HydroLine(1.5f, new[] { LL(10.03,56.46),LL(10.12,56.50),LL(10.22,56.56),LL(10.32,56.62) }));
        hydrology.Add(new HydroLine(1.8f, new[] { LL(9.84,55.86),LL(10.02,55.85),LL(10.22,55.87) }));
        hydrology.Add(new HydroLine(1.7f, new[] { LL(9.54,55.71),LL(9.72,55.69),LL(9.91,55.70),LL(10.08,55.71) }));
        hydrology.Add(new HydroLine(1.2f, new[] { LL(9.47,55.49),LL(9.61,55.50),LL(9.75,55.51) }));
        hydrology.Add(new HydroLine(2.7f, new[] { LL(10.38,55.43),LL(10.47,55.49),LL(10.53,55.55),LL(10.57,55.61) }));
        hydrology.Add(new HydroLine(3.4f, new[] { LL(12.08,55.64),LL(12.05,55.74),LL(12.00,55.85),LL(11.94,55.96) }));
        hydrology.Add(new HydroLine(4.6f, new[] { LL(11.79,55.72),LL(11.75,55.83),LL(11.72,55.96),LL(11.73,56.08) }));
        hydrology.Add(new HydroLine(4.8f, new[] { LL(8.13,55.82),LL(8.18,55.93),LL(8.22,56.04),LL(8.24,56.12) }));
        hydrology.Add(new HydroLine(3.4f, new[] { LL(8.15,56.29),LL(8.19,56.37),LL(8.22,56.46) }));
    }

    private static Vector2 LL(double lon, double lat) { return new Vector2((float)lon, (float)lat); }

    private static long TileKey(int x, int y) { return ((long)x << 32) ^ (uint)y; }

    private static string GetTerrainCachePath(int zoom, int x, int y)
    {
        return Path.Combine(Application.persistentDataPath, "PROJECT1864", "TerrainCache", "terrarium", zoom.ToString(), x.ToString(), y + ".png");
    }

    private static int LonToTileX(double lon, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((lon + 180.0) / 360.0 * n);
    }

    private static int LatToTileY(double lat, int zoom)
    {
        double latRad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) * 0.5 * n);
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static Material CreateLitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.02f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(box);
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static void HideObjectTree(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
            return;
        SetRenderersVisible(go, false);
    }

    private static void SetRenderersVisible(GameObject go, bool visible)
    {
        if (go == null)
            return;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = visible;
    }

    private void OnGUI()
    {
        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.LowerLeft
            };
            statusStyle.normal.textColor = new Color(0.82f, 0.84f, 0.77f);
        }
        GUI.Label(new Rect(12f, Screen.height - 24f, 900f, 18f), "v13j · " + status, statusStyle);
    }

    [Serializable]
    private sealed class TerrainTile
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;
        public TerrainTile(int width, int height, Color32[] pixels) { Width = width; Height = height; Pixels = pixels; }
    }

    private sealed class HydroLine
    {
        public readonly float WidthKm;
        public readonly Vector2[] Points;
        public HydroLine(float widthKm, Vector2[] points) { WidthKm = widthKm; Points = points; }
    }

    private struct RingBounds
    {
        public readonly float MinX, MaxX, MinY, MaxY;
        public RingBounds(float minX, float maxX, float minY, float maxY) { MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; }
    }

    private sealed class CityVisual
    {
        public readonly string Id;
        public readonly GameObject Root;
        public readonly bool Major;
        public CityVisual(string id, GameObject root, bool major) { Id = id; Root = root; Major = major; }
    }
}
