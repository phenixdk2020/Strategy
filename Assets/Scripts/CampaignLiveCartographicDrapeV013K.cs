using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13k — Live Cartographic 3D Drape.
//
// Purpose:
// - Use a real map as a texture layer draped over the smooth v13j 3D terrain.
// - Request only tiles that are currently visible around the camera target (no country prefetch).
// - Cache viewed tiles for at least 14 days.
// - Hide prototype road lines/city blocks so map cartography becomes the visual ground truth.
// - Keep campaign simulation, lat/lon, route distance, ETA and combat isolation untouched.
//
// Default DEV provider is OpenStreetMap Standard. It is an interactive development source,
// not a production/offline map distribution pipeline. A local provider.json can override it.
[DefaultExecutionOrder(4450)]
public sealed class CampaignLiveCartographicDrapeV013K : MonoBehaviour
{
    public const string BuildTag = "v00.00.13k";
    public const string RootName = "V013K_LIVE_CARTOGRAPHIC_DRAPE";

    private const float WaterY = 0.12f;
    private const float SurfaceOffset = 0.018f;
    private const int OverlayGrid = 16;
    private const int CacheDays = 14;
    private const float RefreshInterval = 0.35f;

    private const double MapMinLat = 54.20;
    private const double MapMaxLat = 58.10;
    private const double MapMinLon = 7.20;
    private const double MapMaxLon = 13.60;

    private const string DefaultTileTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    private const string DefaultAttribution = "© OpenStreetMap contributors · ODbL";
    private const string DefaultUserAgent = "PROJECT1864-Campaign/0.13k (+https://github.com/phenixdk2020/Strategy)";

    private Camera cam;
    private MeshCollider terrainCollider;
    private Transform tileRoot;
    private Transform landmarkRoot;

    private readonly Dictionary<string, TileVisual> activeTiles = new Dictionary<string, TileVisual>();
    private readonly Queue<TileRequest> queue = new Queue<TileRequest>();
    private readonly HashSet<string> queuedKeys = new HashSet<string>();
    private readonly HashSet<string> requiredKeys = new HashSet<string>();

    private ProviderConfig provider;
    private Coroutine queueWorker;
    private float nextRefresh;
    private int currentZoom = -1;
    private int currentCenterX = int.MinValue;
    private int currentCenterY = int.MinValue;
    private bool initialized;
    private bool localHistoricalRaster;
    private string status = "venter på v13j 3D terrain";

    private GameObject aalborgBarracks;
    private GameObject aarhusFarm;
    private GUIStyle statusStyle;
    private GUIStyle attributionStyle;
    private GUIStyle landmarkStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignLiveCartographicDrapeV013K>() != null)
            return;

        new GameObject("CampaignLiveCartographicDrapeV013K").AddComponent<CampaignLiveCartographicDrapeV013K>();
    }

    private void Start()
    {
        cam = Camera.main;
        provider = LoadProviderConfig();
        ConfigureCloseCamera();
        StartCoroutine(InitializeWhenTerrainReady());
    }

    private IEnumerator InitializeWhenTerrainReady()
    {
        while (true)
        {
            GameObject terrain = GameObject.Find("V013J_SmoothTerrain");
            if (terrain != null)
            {
                terrainCollider = terrain.GetComponent<MeshCollider>();
                if (terrainCollider != null)
                    break;
            }

            status = "venter på v13j 3D terrain";
            yield return new WaitForSecondsRealtime(0.20f);
        }

        CreateRoots();
        HidePrototypePresentation();
        BuildLandmarks();

        string historicalRasterPath = Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864", "HistoricalMap", "denmark_1864.png");
        localHistoricalRaster = File.Exists(historicalRasterPath);

        initialized = true;
        if (localHistoricalRaster)
        {
            status = "lokalt historisk raster er aktivt · live tiles springes over";
        }
        else
        {
            status = "live 3D landkort aktivt · henter kun synlige kortfelter";
            queueWorker = StartCoroutine(ProcessTileQueue());
            RefreshVisibleTiles(true);
        }

        Debug.Log("CAMPAIGN-V013K|LiveCartographicDrape=True|ViewportTilesOnly=True|CacheDays=14|PrototypeRoadLines=False|SmallLandmarks=True|MapOnly=True|SimulationChanged=False");
    }

    private void Update()
    {
        if (!initialized)
            return;

        HidePrototypePresentation();
        UpdateLandmarkLod();

        if (!localHistoricalRaster && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            RefreshVisibleTiles(false);
        }
    }

    private void CreateRoots()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Destroy(old);

        Transform root = new GameObject(RootName).transform;
        tileRoot = CreateChild(root, "L1_LIVE_MAP_TILES");
        landmarkRoot = CreateChild(root, "L6_MAP_LANDMARKS");
    }

    private void ConfigureCloseCamera()
    {
        if (cam == null)
            return;

        cam.nearClipPlane = 0.005f;
        cam.fieldOfView = 48f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.MinHeight = 0.22f;
            controller.MaxHeight = 190f;
            controller.TerrainClearance = 0.14f;
            controller.ZoomSpeed = 105f;
            controller.PanSpeed = 38f;
            controller.MinPitch = 20f;
            controller.MaxPitch = 80f;
        }
    }

    private void RefreshVisibleTiles(bool force)
    {
        if (cam == null || terrainCollider == null)
            return;

        Vector2 lonLat = ResolveCameraTargetLonLat();
        int zoom = SelectZoom(cam.transform.position.y);
        int centerX = LonToTileX(lonLat.x, zoom);
        int centerY = LatToTileY(lonLat.y, zoom);

        if (!force && zoom == currentZoom && centerX == currentCenterX && centerY == currentCenterY)
            return;

        currentZoom = zoom;
        currentCenterX = centerX;
        currentCenterY = centerY;
        requiredKeys.Clear();

        // 3x3 active viewport. No background/country prefetch.
        const int radius = 1;
        int maxTile = (1 << zoom) - 1;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y > maxTile) continue;
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int wrappedX = x;
                int span = 1 << zoom;
                while (wrappedX < 0) wrappedX += span;
                while (wrappedX >= span) wrappedX -= span;

                string key = TileKey(zoom, wrappedX, y);
                requiredKeys.Add(key);
                if (!activeTiles.ContainsKey(key) && !queuedKeys.Contains(key))
                {
                    queue.Enqueue(new TileRequest(zoom, wrappedX, y));
                    queuedKeys.Add(key);
                }
            }
        }

        List<string> remove = new List<string>();
        foreach (KeyValuePair<string, TileVisual> pair in activeTiles)
        {
            if (!requiredKeys.Contains(pair.Key))
                remove.Add(pair.Key);
        }
        foreach (string key in remove)
            RemoveTile(key);

        status = string.Format("live landkort · z{0} · synlige tiles {1}/9", zoom, activeTiles.Count);
    }

    private IEnumerator ProcessTileQueue()
    {
        while (true)
        {
            if (queue.Count == 0)
            {
                yield return null;
                continue;
            }

            TileRequest request = queue.Dequeue();
            string key = TileKey(request.Zoom, request.X, request.Y);
            queuedKeys.Remove(key);

            if (!requiredKeys.Contains(key) || activeTiles.ContainsKey(key))
                continue;

            Texture2D texture = TryLoadCachedTile(request);
            if (texture == null)
                yield return DownloadTile(request, value => texture = value);

            if (texture != null && requiredKeys.Contains(key))
            {
                BuildTileVisual(request, texture);
                status = string.Format("live landkort · z{0} · synlige tiles {1}/9", currentZoom, activeTiles.Count);
            }
            else if (texture != null)
            {
                Destroy(texture);
            }

            // Sequential requests keep the development client modest and predictable.
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    private Texture2D TryLoadCachedTile(TileRequest request)
    {
        string path = GetTileCachePath(request);
        if (!File.Exists(path))
            return null;

        try
        {
            DateTime ageLimit = DateTime.UtcNow.AddDays(-CacheDays);
            if (File.GetLastWriteTimeUtc(path) < ageLimit)
                return null;

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                Destroy(texture);
                return null;
            }
            PrepareTileTexture(texture);
            return texture;
        }
        catch (Exception ex)
        {
            Debug.Log("CAMPAIGN-V013K|TileCacheRead=False|" + ex.Message);
            return null;
        }
    }

    private IEnumerator DownloadTile(TileRequest request, Action<Texture2D> callback)
    {
        string url = provider.UrlTemplate
            .Replace("{z}", request.Zoom.ToString())
            .Replace("{x}", request.X.ToString())
            .Replace("{y}", request.Y.ToString());

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url, false))
        {
            webRequest.timeout = 12;
            try { webRequest.SetRequestHeader("User-Agent", provider.UserAgent); } catch { }
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("CAMPAIGN-V013K|TileDownload=False|" + request.Zoom + "/" + request.X + "/" + request.Y + "|" + webRequest.error);
                callback(null);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            PrepareTileTexture(texture);

            try
            {
                string path = GetTileCachePath(request);
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                byte[] bytes = webRequest.downloadHandler.data;
                if (bytes != null && bytes.Length > 0)
                    File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Debug.Log("CAMPAIGN-V013K|TileCacheWrite=False|" + ex.Message);
            }

            callback(texture);
        }
    }

    private static void PrepareTileTexture(Texture2D texture)
    {
        if (texture == null) return;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 4;
    }

    private void BuildTileVisual(TileRequest request, Texture2D texture)
    {
        string key = TileKey(request.Zoom, request.X, request.Y);
        GameObject go = new GameObject("V013K_MapTile_" + key.Replace('/', '_'));
        go.transform.SetParent(tileRoot, false);

        int size = OverlayGrid + 1;
        Vector3[] vertices = new Vector3[size * size];
        Vector2[] uv = new Vector2[vertices.Length];
        List<int> triangles = new List<int>(OverlayGrid * OverlayGrid * 6);

        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)OverlayGrid;
            double tileY = request.Y + (1.0 - v);
            double lat = TileYToLat(tileY, request.Zoom);

            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)OverlayGrid;
                double tileX = request.X + u;
                double lon = TileXToLon(tileX, request.Zoom);
                int index = gy * size + gx;

                Vector3 world = CampaignGeoProjection.Project3D(lat, lon, WaterY);
                world.y = SampleMapSurfaceY(world.x, world.z) + SurfaceOffset;
                vertices[index] = world;
                uv[index] = new Vector2(u, v);
            }
        }

        for (int gy = 0; gy < OverlayGrid; gy++)
        {
            for (int gx = 0; gx < OverlayGrid; gx++)
            {
                int i0 = gy * size + gx;
                int i1 = i0 + 1;
                int i2 = i0 + size;
                int i3 = i2 + 1;
                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh mesh = new Mesh { name = go.name + "_Mesh", vertices = vertices, uv = uv, triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = CreateTileMaterial(texture, go.name + "_Material");
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        activeTiles[key] = new TileVisual(go, texture, material);
    }

    private Material CreateTileMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material material = new Material(shader) { name = name, color = Color.white };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.03f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private void RemoveTile(string key)
    {
        TileVisual visual;
        if (!activeTiles.TryGetValue(key, out visual))
            return;

        activeTiles.Remove(key);
        if (visual.Root != null) Destroy(visual.Root);
        if (visual.Material != null) Destroy(visual.Material);
        if (visual.Texture != null) Destroy(visual.Texture);
    }

    private Vector2 ResolveCameraTargetLonLat()
    {
        Vector3 world;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (terrainCollider != null && terrainCollider.Raycast(ray, out hit, 2000f))
        {
            world = hit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, WaterY, 0f));
            float enter;
            world = plane.Raycast(ray, out enter) ? ray.GetPoint(enter) : cam.transform.position;
        }

        double lon = CampaignGeoProjection.MinLongitude +
                     ((world.x / CampaignGeoProjection.MapWidth) + 0.5) *
                     (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double lat = CampaignGeoProjection.MinLatitude +
                     ((world.z / CampaignGeoProjection.MapDepth) + 0.5) *
                     (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);

        lon = Math.Max(MapMinLon, Math.Min(MapMaxLon, lon));
        lat = Math.Max(MapMinLat, Math.Min(MapMaxLat, lat));
        return new Vector2((float)lon, (float)lat);
    }

    private float SampleMapSurfaceY(float x, float z)
    {
        if (terrainCollider != null)
        {
            Ray ray = new Ray(new Vector3(x, 40f, z), Vector3.down);
            RaycastHit hit;
            if (terrainCollider.Raycast(ray, out hit, 80f))
                return hit.point.y;
        }
        return WaterY + 0.012f;
    }

    private void HidePrototypePresentation()
    {
        CampaignHistoricalMapLabelsV013J labels = UnityEngine.Object.FindAnyObjectByType<CampaignHistoricalMapLabelsV013J>();
        if (labels != null) labels.enabled = false;

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || (tileRoot != null && renderer.transform.IsChildOf(tileRoot)) ||
                (landmarkRoot != null && renderer.transform.IsChildOf(landmarkRoot)))
                continue;

            string n = renderer.gameObject.name;
            if (n.StartsWith("GIS_Road_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_Ferry_", StringComparison.Ordinal) ||
                n.StartsWith("V013J_City_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_City_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_Aalborg_Barracks_", StringComparison.Ordinal) ||
                n.StartsWith("V013J_Aalborg_", StringComparison.Ordinal) ||
                n.StartsWith("V013J_Aarhus_", StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private void BuildLandmarks()
    {
        aalborgBarracks = BuildAalborgBarracks();
        aarhusFarm = BuildAarhusFarm();
        UpdateLandmarkLod();
    }

    private GameObject BuildAalborgBarracks()
    {
        Vector3 centre = FindLandAnchor(57.0445, 9.9050);
        GameObject root = new GameObject("V013K_Aalborg_Barracks_Landmark");
        root.transform.SetParent(landmarkRoot, false);
        root.transform.position = centre;

        Material wall = CreateLitColor(new Color(0.67f, 0.53f, 0.38f), "V013K_BarracksWall");
        Material roof = CreateLitColor(new Color(0.34f, 0.16f, 0.12f), "V013K_BarracksRoof");
        Material yard = CreateLitColor(new Color(0.52f, 0.49f, 0.35f), "V013K_ParadeGround");

        CreateBox(root.transform, "ParadeGround", new Vector3(0f, 0.002f, 0f), new Vector3(0.16f, 0.004f, 0.10f), yard);
        CreateBuilding(root.transform, "Barracks_A", new Vector3(-0.045f, 0f, -0.023f), new Vector3(0.065f, 0.018f, 0.020f), wall, roof);
        CreateBuilding(root.transform, "Barracks_B", new Vector3(0.045f, 0f, -0.023f), new Vector3(0.065f, 0.018f, 0.020f), wall, roof);
        CreateBuilding(root.transform, "Storehouse", new Vector3(0f, 0f, 0.032f), new Vector3(0.045f, 0.015f, 0.024f), wall, roof);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "FlagPole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0.075f, 0.035f, 0.030f);
        pole.transform.localScale = new Vector3(0.0025f, 0.035f, 0.0025f);
        pole.GetComponent<Renderer>().sharedMaterial = CreateLitColor(new Color(0.35f, 0.31f, 0.24f), "V013K_FlagPole");
        RemoveCollider(pole);

        return root;
    }

    private GameObject BuildAarhusFarm()
    {
        Vector3 centre = FindLandAnchor(56.1455, 10.1450);
        GameObject root = new GameObject("V013K_Aarhus_Farm_Landmark");
        root.transform.SetParent(landmarkRoot, false);
        root.transform.position = centre;

        Material wall = CreateLitColor(new Color(0.72f, 0.63f, 0.45f), "V013K_FarmWall");
        Material roof = CreateLitColor(new Color(0.39f, 0.20f, 0.13f), "V013K_FarmRoof");
        Material field = CreateLitColor(new Color(0.55f, 0.56f, 0.32f), "V013K_FarmField");

        CreateBox(root.transform, "Field", new Vector3(0f, 0.002f, 0f), new Vector3(0.15f, 0.004f, 0.11f), field);
        CreateBuilding(root.transform, "Farmhouse", new Vector3(-0.025f, 0f, 0f), new Vector3(0.050f, 0.016f, 0.025f), wall, roof);
        CreateBuilding(root.transform, "Barn", new Vector3(0.038f, 0f, 0.018f), new Vector3(0.060f, 0.020f, 0.030f), wall, roof);
        return root;
    }

    private Vector3 FindLandAnchor(double lat, double lon)
    {
        Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY);
        float y = SampleMapSurfaceY(p.x, p.z);
        if (y > WaterY + 0.04f)
        {
            p.y = y + 0.01f;
            return p;
        }

        // Radial search keeps harbour-city landmarks on the nearest land surface.
        double[] steps = { 0.006, 0.012, 0.020, 0.030 };
        foreach (double step in steps)
        {
            for (int i = 0; i < 12; i++)
            {
                double angle = i * Math.PI * 2.0 / 12.0;
                double testLat = lat + Math.Sin(angle) * step;
                double testLon = lon + Math.Cos(angle) * step;
                Vector3 test = CampaignGeoProjection.Project3D(testLat, testLon, WaterY);
                float testY = SampleMapSurfaceY(test.x, test.z);
                if (testY > WaterY + 0.04f)
                {
                    test.y = testY + 0.01f;
                    return test;
                }
            }
        }

        p.y = WaterY + 0.05f;
        return p;
    }

    private void UpdateLandmarkLod()
    {
        if (cam == null) return;
        bool visible = cam.transform.position.y < 9.0f;
        if (aalborgBarracks != null && aalborgBarracks.activeSelf != visible) aalborgBarracks.SetActive(visible);
        if (aarhusFarm != null && aarhusFarm.activeSelf != visible) aarhusFarm.SetActive(visible);
    }

    private static void CreateBuilding(Transform parent, string name, Vector3 localPosition, Vector3 size, Material wall, Material roof)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = name;
        body.transform.SetParent(parent, false);
        body.transform.localPosition = localPosition + new Vector3(0f, size.y * 0.5f, 0f);
        body.transform.localScale = size;
        body.GetComponent<Renderer>().sharedMaterial = wall;
        RemoveCollider(body);

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = name + "_Roof";
        top.transform.SetParent(parent, false);
        top.transform.localPosition = localPosition + new Vector3(0f, size.y + 0.004f, 0f);
        top.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        top.transform.localScale = new Vector3(size.x * 0.78f, 0.006f, size.z * 0.78f);
        top.GetComponent<Renderer>().sharedMaterial = roof;
        RemoveCollider(top);
    }

    private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(go);
    }

    private static Material CreateLitColor(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.04f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private int SelectZoom(float height)
    {
        if (height > 80f) return 7;
        if (height > 38f) return 8;
        if (height > 15f) return 9;
        if (height > 5f) return 10;
        return 11;
    }

    private ProviderConfig LoadProviderConfig()
    {
        ProviderConfig config = new ProviderConfig
        {
            UrlTemplate = DefaultTileTemplate,
            Attribution = DefaultAttribution,
            UserAgent = DefaultUserAgent
        };

        string path = Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapProvider", "provider.json");
        if (!File.Exists(path))
            return config;

        try
        {
            ProviderConfig loaded = JsonUtility.FromJson<ProviderConfig>(File.ReadAllText(path));
            if (loaded != null)
            {
                if (!string.IsNullOrWhiteSpace(loaded.UrlTemplate)) config.UrlTemplate = loaded.UrlTemplate;
                if (!string.IsNullOrWhiteSpace(loaded.Attribution)) config.Attribution = loaded.Attribution;
                if (!string.IsNullOrWhiteSpace(loaded.UserAgent)) config.UserAgent = loaded.UserAgent;
            }
        }
        catch (Exception ex)
        {
            Debug.Log("CAMPAIGN-V013K|ProviderConfig=False|" + ex.Message);
        }
        return config;
    }

    private string GetTileCachePath(TileRequest request)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864", "MapCache", "interactive",
            request.Zoom.ToString(), request.X.ToString(), request.Y + ".png");
    }

    private void EnsureStyles()
    {
        if (statusStyle != null) return;

        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.LowerLeft };
        statusStyle.normal.textColor = new Color(0.92f, 0.92f, 0.88f);

        attributionStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.LowerRight };
        attributionStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

        landmarkStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        landmarkStyle.normal.textColor = new Color(0.98f, 0.93f, 0.76f);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.Label(new Rect(12f, Screen.height - 24f, 720f, 18f), "v13k · " + status, statusStyle);

        if (!localHistoricalRaster)
        {
            Rect attributionRect = new Rect(Screen.width - 340f, Screen.height - 24f, 328f, 18f);
            GUI.Label(attributionRect, provider != null ? provider.Attribution : DefaultAttribution, attributionStyle);
        }

        if (cam != null && cam.transform.position.y < 9f)
        {
            DrawLandmarkLabel(aalborgBarracks, "Aalborg Kaserne · QA");
            DrawLandmarkLabel(aarhusFarm, "Aarhus Farm · QA");
        }
    }

    private void DrawLandmarkLabel(GameObject landmark, string text)
    {
        if (landmark == null || !landmark.activeInHierarchy || cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(landmark.transform.position + Vector3.up * 0.08f);
        if (screen.z <= 0f) return;
        float y = Screen.height - screen.y;
        GUI.Label(new Rect(screen.x - 70f, y - 10f, 140f, 20f), text, landmarkStyle);
    }

    private static int LonToTileX(double lon, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((lon + 180.0) / 360.0 * n);
    }

    private static int LatToTileY(double lat, int zoom)
    {
        double rad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((1.0 - Math.Asinh(Math.Tan(rad)) / Math.PI) * 0.5 * n);
    }

    private static double TileXToLon(double x, int zoom)
    {
        return x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
    }

    private static double TileYToLat(double y, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        double mercator = Math.PI * (1.0 - 2.0 * y / n);
        return Math.Atan(Math.Sinh(mercator)) * 180.0 / Math.PI;
    }

    private static string TileKey(int zoom, int x, int y)
    {
        return zoom + "/" + x + "/" + y;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    [Serializable]
    private sealed class ProviderConfig
    {
        public string UrlTemplate;
        public string Attribution;
        public string UserAgent;
    }

    private readonly struct TileRequest
    {
        public readonly int Zoom;
        public readonly int X;
        public readonly int Y;

        public TileRequest(int zoom, int x, int y)
        {
            Zoom = zoom;
            X = x;
            Y = y;
        }
    }

    private sealed class TileVisual
    {
        public readonly GameObject Root;
        public readonly Texture2D Texture;
        public readonly Material Material;

        public TileVisual(GameObject root, Texture2D texture, Material material)
        {
            Root = root;
            Texture = texture;
            Material = material;
        }
    }
}
