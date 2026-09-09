using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13m — Unified Denmark 3D Map.
//
// This replaces the stacked v13j + v13k + v13l presentation with ONE visible map renderer:
// - one continuous Denmark overview mesh (land relief + flat water)
// - one stitched z8 cartographic atlas for the complete Denmark campaign envelope
// - optional viewport-only z10-z13 detail tiles above the base atlas
// - old v13j/v13k/v13l renderers and OnGUI status layers are disabled once v13m is ready
// - small Aalborg barracks and Aarhus farm landmarks are rebuilt on the unified map
//
// Simulation, campaign time, authoritative lat/lon, distance/ETA, logistics and tactical code are untouched.
[DefaultExecutionOrder(4650)]
public sealed class CampaignUnifiedDenmark3DMapV013M : MonoBehaviour
{
    public const string BuildTag = "v00.00.13m";
    public const string RootName = "V013M_UNIFIED_DENMARK_3D_MAP";

    private const float WaterY = 0.12f;
    private const float BaseSurfaceOffset = 0.018f;
    private const float DetailSurfaceOffset = 0.032f;

    // Match the validated v13j Denmark mesh envelope so foreign flat land is not introduced.
    private const double MinLat = 54.48;
    private const double MaxLat = 57.82;
    private const double MinLon = 7.70;
    private const double MaxLon = 13.05;

    private const int BaseZoom = 8;
    private const int BaseGridX = 520;
    private const int BaseGridZ = 340;
    private const int DetailGrid = 24;
    private const int TileSize = 256;
    private const int CacheDays = 14;
    private const float DetailRefreshInterval = 0.30f;

    private const string TileTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    private const string Attribution = "© OpenStreetMap contributors · ODbL";
    private const string UserAgent = "PROJECT1864-Campaign/0.13m (+https://github.com/phenixdk2020/Strategy)";

    private Camera cam;
    private MeshCollider landCollider;
    private Renderer oldTerrainRenderer;

    private Transform root;
    private Transform baseRoot;
    private Transform detailRoot;
    private Transform landmarkRoot;

    private Material baseMaterial;
    private Texture2D baseAtlas;
    private GameObject baseSurface;

    private readonly Dictionary<string, TileVisual> detailTiles = new Dictionary<string, TileVisual>();
    private readonly Queue<TileRequest> detailQueue = new Queue<TileRequest>();
    private readonly HashSet<string> queuedDetailKeys = new HashSet<string>();
    private readonly HashSet<string> requiredDetailKeys = new HashSet<string>();

    private Coroutine detailWorker;
    private int currentDetailZoom = -1;
    private int currentDetailX = int.MinValue;
    private int currentDetailY = int.MinValue;
    private float nextDetailRefresh;

    private GameObject aalborgBarracks;
    private GameObject aarhusFarm;

    private bool initialized;
    private bool baseReady;
    private string status = "venter på 3D-terræn";
    private GUIStyle statusStyle;
    private GUIStyle attributionStyle;
    private GUIStyle landmarkStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignUnifiedDenmark3DMapV013M>() != null)
            return;

        new GameObject("CampaignUnifiedDenmark3DMapV013M").AddComponent<CampaignUnifiedDenmark3DMapV013M>();
    }

    private void Start()
    {
        cam = Camera.main;
        ConfigureCamera();
        ConfigureAtmosphere();
        StartCoroutine(InitializeUnifiedMap());
    }

    private IEnumerator InitializeUnifiedMap()
    {
        while (true)
        {
            GameObject terrain = GameObject.Find("V013J_SmoothTerrain");
            if (terrain != null)
            {
                landCollider = terrain.GetComponent<MeshCollider>();
                oldTerrainRenderer = terrain.GetComponent<Renderer>();
                if (landCollider != null)
                    break;
            }

            status = "venter på v13j DEM-terræn";
            yield return new WaitForSecondsRealtime(0.20f);
        }

        CreateRoots();
        BuildBaseSurfaceGeometry();
        BuildLandmarks();

        status = "bygger samlet Danmark-kort";
        yield return BuildBaseAtlas();

        if (baseMaterial != null && baseAtlas != null)
        {
            if (baseMaterial.HasProperty("_BaseMap")) baseMaterial.SetTexture("_BaseMap", baseAtlas);
            if (baseMaterial.HasProperty("_MainTex")) baseMaterial.SetTexture("_MainTex", baseAtlas);
        }

        DeactivateOlderVisualStack();
        baseReady = true;
        initialized = true;
        status = "samlet 3D Danmark-kort klar";

        detailWorker = StartCoroutine(ProcessDetailQueue());
        RefreshDetailTiles(true);

        Debug.Log("CAMPAIGN-V013M|UnifiedMap=True|SingleBaseSurface=True|BaseAtlas=z8|Detail=z10-z13|OldVisualStack=False|MapOnly=True|SimulationChanged=False");
    }

    private void Update()
    {
        if (!initialized)
            return;

        KeepOlderVisualStackDisabled();
        UpdateLandmarkLod();

        if (Time.unscaledTime >= nextDetailRefresh)
        {
            nextDetailRefresh = Time.unscaledTime + DetailRefreshInterval;
            RefreshDetailTiles(false);
        }
    }

    private void CreateRoots()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Destroy(old);

        root = new GameObject(RootName).transform;
        baseRoot = CreateChild(root, "L1_UNIFIED_BASE_MAP");
        detailRoot = CreateChild(root, "L2_VIEWPORT_DETAIL");
        landmarkRoot = CreateChild(root, "L6_LANDMARKS");
    }

    private void ConfigureCamera()
    {
        if (cam == null)
            return;

        cam.nearClipPlane = 0.003f;
        cam.fieldOfView = 45f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.MinHeight = 0.12f;
            controller.MaxHeight = 190f;
            controller.TerrainClearance = 0.08f;
            controller.ZoomSpeed = 95f;
            controller.PanSpeed = 32f;
            controller.MinPitch = 18f;
            controller.MaxPitch = 82f;
        }
    }

    private void ConfigureAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.67f, 0.69f, 0.64f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.68f, 0.75f, 0.79f);
        RenderSettings.fogStartDistance = 85f;
        RenderSettings.fogEndDistance = 235f;

        if (cam != null)
            cam.backgroundColor = new Color(0.52f, 0.66f, 0.75f);

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        Light sun = null;
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }

        if (sun == null)
        {
            GameObject go = new GameObject("V013M_MapSun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(1.0f, 0.94f, 0.84f);
        sun.intensity = 0.94f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.50f;
        sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
    }

    private void BuildBaseSurfaceGeometry()
    {
        int width = BaseGridX + 1;
        int height = BaseGridZ + 1;
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[vertices.Length];
        List<int> triangles = new List<int>(BaseGridX * BaseGridZ * 6);

        int xMin = LonToTileX(MinLon, BaseZoom);
        int xMax = LonToTileX(MaxLon, BaseZoom);
        int yMin = LatToTileY(MaxLat, BaseZoom);
        int yMax = LatToTileY(MinLat, BaseZoom);
        int tileColumns = xMax - xMin + 1;
        int tileRows = yMax - yMin + 1;

        for (int z = 0; z < height; z++)
        {
            double vz = z / (double)BaseGridZ;
            double lat = MinLat + (MaxLat - MinLat) * vz;

            for (int x = 0; x < width; x++)
            {
                double ux = x / (double)BaseGridX;
                double lon = MinLon + (MaxLon - MinLon) * ux;
                int index = z * width + x;

                Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY);
                bool isLand;
                float surfaceY = SampleSurfaceY(p.x, p.z, out isLand);
                p.y = (isLand ? surfaceY : WaterY) + BaseSurfaceOffset;
                vertices[index] = p;

                double globalTileX = LonToTileXDouble(lon, BaseZoom);
                double globalTileY = LatToTileYDouble(lat, BaseZoom);
                float mapU = Mathf.Clamp01((float)((globalTileX - xMin) / tileColumns));
                float mapV = Mathf.Clamp01((float)(1.0 - (globalTileY - yMin) / tileRows));
                uv[index] = new Vector2(mapU, mapV);
            }
        }

        for (int z = 0; z < BaseGridZ; z++)
        {
            for (int x = 0; x < BaseGridX; x++)
            {
                int i0 = z * width + x;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;
                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh mesh = new Mesh
        {
            name = "V013M_UnifiedDenmarkMapMesh",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            uv = uv,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        baseSurface = new GameObject("V013M_UnifiedDenmarkSurface");
        baseSurface.transform.SetParent(baseRoot, false);
        baseSurface.AddComponent<MeshFilter>().sharedMesh = mesh;

        baseMaterial = CreateMapMaterial(null, "V013M_UnifiedMapMaterial");
        MeshRenderer renderer = baseSurface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = baseMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private IEnumerator BuildBaseAtlas()
    {
        int xMin = LonToTileX(MinLon, BaseZoom);
        int xMax = LonToTileX(MaxLon, BaseZoom);
        int yMin = LatToTileY(MaxLat, BaseZoom);
        int yMax = LatToTileY(MinLat, BaseZoom);

        int columns = xMax - xMin + 1;
        int rows = yMax - yMin + 1;
        int total = columns * rows;
        int completed = 0;

        baseAtlas = new Texture2D(columns * TileSize, rows * TileSize, TextureFormat.RGBA32, true, false)
        {
            name = "V013M_Denmark_BaseAtlas_Z8",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 12
        };

        Color32[] fallback = CreateFallbackTilePixels();

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                TileRequest request = new TileRequest(BaseZoom, x, y);
                Texture2D tile = TryLoadCachedTile(request);
                if (tile == null)
                    yield return DownloadTile(request, value => tile = value);

                Color32[] pixels = fallback;
                if (tile != null)
                {
                    try
                    {
                        if (tile.width == TileSize && tile.height == TileSize)
                            pixels = tile.GetPixels32();
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("CAMPAIGN-V013M|BaseTilePixels=False|" + ex.Message);
                    }
                }

                int atlasX = (x - xMin) * TileSize;
                int atlasY = (rows - 1 - (y - yMin)) * TileSize;
                baseAtlas.SetPixels32(atlasX, atlasY, TileSize, TileSize, pixels);

                if (tile != null)
                    Destroy(tile);

                completed++;
                status = "bygger samlet Danmark-kort " + completed + "/" + total;
                if ((completed & 3) == 0)
                    yield return null;
            }
        }

        baseAtlas.Apply(true, false);
    }

    private static Color32[] CreateFallbackTilePixels()
    {
        Color32[] pixels = new Color32[TileSize * TileSize];
        Color32 c = new Color32(178, 201, 202, 255);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        return pixels;
    }

    private void RefreshDetailTiles(bool force)
    {
        if (!baseReady || cam == null || landCollider == null)
            return;

        int zoom = SelectDetailZoom(cam.transform.position.y);
        if (zoom < 0)
        {
            ClearDetailTiles();
            currentDetailZoom = -1;
            currentDetailX = int.MinValue;
            currentDetailY = int.MinValue;
            status = "samlet Danmark overview";
            return;
        }

        Vector2 lonLat = ResolveCameraTargetLonLat();
        int centerX = LonToTileX(lonLat.x, zoom);
        int centerY = LatToTileY(lonLat.y, zoom);

        if (!force && zoom == currentDetailZoom && centerX == currentDetailX && centerY == currentDetailY)
            return;

        currentDetailZoom = zoom;
        currentDetailX = centerX;
        currentDetailY = centerY;
        requiredDetailKeys.Clear();

        // 5x5 detail coverage; the full-country atlas remains underneath, so there are never holes.
        const int radius = 2;
        int span = 1 << zoom;
        int maxTile = span - 1;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y > maxTile) continue;
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int wrappedX = x;
                while (wrappedX < 0) wrappedX += span;
                while (wrappedX >= span) wrappedX -= span;

                string key = TileKey(zoom, wrappedX, y);
                requiredDetailKeys.Add(key);
                if (!detailTiles.ContainsKey(key) && !queuedDetailKeys.Contains(key))
                {
                    detailQueue.Enqueue(new TileRequest(zoom, wrappedX, y));
                    queuedDetailKeys.Add(key);
                }
            }
        }

        List<string> remove = new List<string>();
        foreach (KeyValuePair<string, TileVisual> pair in detailTiles)
            if (!requiredDetailKeys.Contains(pair.Key)) remove.Add(pair.Key);
        foreach (string key in remove) RemoveDetailTile(key);

        status = "3D detail z" + zoom + " · " + detailTiles.Count + "/25";
    }

    private static int SelectDetailZoom(float height)
    {
        if (height > 13f) return -1;
        if (height > 5f) return 10;
        if (height > 2f) return 11;
        if (height > 0.70f) return 12;
        return 13;
    }

    private IEnumerator ProcessDetailQueue()
    {
        while (true)
        {
            if (detailQueue.Count == 0)
            {
                yield return null;
                continue;
            }

            TileRequest request = detailQueue.Dequeue();
            string key = TileKey(request.Zoom, request.X, request.Y);
            queuedDetailKeys.Remove(key);

            if (!requiredDetailKeys.Contains(key) || detailTiles.ContainsKey(key))
                continue;

            Texture2D texture = TryLoadCachedTile(request);
            if (texture == null)
                yield return DownloadTile(request, value => texture = value);

            if (texture != null && requiredDetailKeys.Contains(key))
                BuildDetailTile(request, texture);
            else if (texture != null)
                Destroy(texture);

            status = "3D detail z" + currentDetailZoom + " · " + detailTiles.Count + "/25";
            yield return new WaitForSecondsRealtime(0.045f);
        }
    }

    private void BuildDetailTile(TileRequest request, Texture2D texture)
    {
        string key = TileKey(request.Zoom, request.X, request.Y);
        GameObject go = new GameObject("V013M_DetailTile_" + key.Replace('/', '_'));
        go.transform.SetParent(detailRoot, false);

        int size = DetailGrid + 1;
        Vector3[] vertices = new Vector3[size * size];
        Vector2[] uv = new Vector2[vertices.Length];
        List<int> triangles = new List<int>(DetailGrid * DetailGrid * 6);

        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)DetailGrid;
            double tileY = request.Y + (1.0 - v);
            double lat = TileYToLat(tileY, request.Zoom);

            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)DetailGrid;
                double lon = TileXToLon(request.X + u, request.Zoom);
                int index = gy * size + gx;

                Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY);
                bool isLand;
                float y = SampleSurfaceY(p.x, p.z, out isLand);
                p.y = (isLand ? y : WaterY) + DetailSurfaceOffset;
                vertices[index] = p;
                uv[index] = new Vector2(u, v);
            }
        }

        for (int gy = 0; gy < DetailGrid; gy++)
        {
            for (int gx = 0; gx < DetailGrid; gx++)
            {
                int i0 = gy * size + gx;
                int i1 = i0 + 1;
                int i2 = i0 + size;
                int i3 = i2 + 1;
                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh mesh = new Mesh
        {
            name = go.name + "_Mesh",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            uv = uv,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = CreateMapMaterial(texture, go.name + "_Material");
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        detailTiles[key] = new TileVisual(go, texture, material);
    }

    private Texture2D TryLoadCachedTile(TileRequest request)
    {
        string[] candidates =
        {
            GetUnifiedCachePath(request),
            Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapCache", "interactive", request.Zoom.ToString(), request.X.ToString(), request.Y + ".png"),
            Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapCache", "premium", request.Zoom.ToString(), request.X.ToString(), request.Y + ".png")
        };

        foreach (string path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-CacheDays)) continue;
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                if (texture.LoadImage(File.ReadAllBytes(path), false))
                {
                    PrepareTexture(texture);
                    return texture;
                }
                Destroy(texture);
            }
            catch (Exception ex)
            {
                Debug.Log("CAMPAIGN-V013M|TileCacheRead=False|" + ex.Message);
            }
        }
        return null;
    }

    private IEnumerator DownloadTile(TileRequest request, Action<Texture2D> callback)
    {
        string url = TileTemplate
            .Replace("{z}", request.Zoom.ToString())
            .Replace("{x}", request.X.ToString())
            .Replace("{y}", request.Y.ToString());

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url, false))
        {
            webRequest.timeout = 15;
            try { webRequest.SetRequestHeader("User-Agent", UserAgent); } catch { }
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("CAMPAIGN-V013M|TileDownload=False|" + request.Zoom + "/" + request.X + "/" + request.Y + "|" + webRequest.error);
                callback(null);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            PrepareTexture(texture);

            try
            {
                string path = GetUnifiedCachePath(request);
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                byte[] bytes = webRequest.downloadHandler.data;
                if (bytes != null && bytes.Length > 0) File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Debug.Log("CAMPAIGN-V013M|TileCacheWrite=False|" + ex.Message);
            }

            callback(texture);
        }
    }

    private static void PrepareTexture(Texture2D texture)
    {
        if (texture == null) return;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 12;
    }

    private static Material CreateMapMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material material = new Material(shader)
        {
            name = name,
            color = new Color(1f, 1f, 1f, 1f)
        };
        if (texture != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.012f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private void DeactivateOlderVisualStack()
    {
        CampaignLiveCartographicDrapeV013K v13k = UnityEngine.Object.FindAnyObjectByType<CampaignLiveCartographicDrapeV013K>();
        if (v13k != null) v13k.enabled = false;

        CampaignPremiumCartographicVisualV013L v13l = UnityEngine.Object.FindAnyObjectByType<CampaignPremiumCartographicVisualV013L>();
        if (v13l != null) v13l.enabled = false;

        CampaignHistorical3DMapV013J v13j = UnityEngine.Object.FindAnyObjectByType<CampaignHistorical3DMapV013J>();
        if (v13j != null) v13j.enabled = false;

        SetRootRenderersVisible(CampaignLiveCartographicDrapeV013K.RootName, false);
        SetRootRenderersVisible(CampaignPremiumCartographicVisualV013L.RootName, false);
        SetRootRenderersVisible(CampaignHistorical3DMapV013J.RootName, false);

        // Keep the v13j collider alive after the renderer is hidden.
        if (oldTerrainRenderer != null) oldTerrainRenderer.enabled = false;

        HideNamedPrototypeRenderers();
    }

    private void KeepOlderVisualStackDisabled()
    {
        CampaignLiveCartographicDrapeV013K v13k = UnityEngine.Object.FindAnyObjectByType<CampaignLiveCartographicDrapeV013K>();
        if (v13k != null && v13k.enabled) v13k.enabled = false;
        CampaignPremiumCartographicVisualV013L v13l = UnityEngine.Object.FindAnyObjectByType<CampaignPremiumCartographicVisualV013L>();
        if (v13l != null && v13l.enabled) v13l.enabled = false;
        CampaignHistorical3DMapV013J v13j = UnityEngine.Object.FindAnyObjectByType<CampaignHistorical3DMapV013J>();
        if (v13j != null && v13j.enabled) v13j.enabled = false;

        SetRootRenderersVisible(CampaignLiveCartographicDrapeV013K.RootName, false);
        SetRootRenderersVisible(CampaignPremiumCartographicVisualV013L.RootName, false);
        SetRootRenderersVisible(CampaignHistorical3DMapV013J.RootName, false);
        HideNamedPrototypeRenderers();
    }

    private static void SetRootRenderersVisible(string rootName, bool visible)
    {
        GameObject oldRoot = GameObject.Find(rootName);
        if (oldRoot == null) return;
        Renderer[] renderers = oldRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = visible;
    }

    private void HideNamedPrototypeRenderers()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || (root != null && renderer.transform.IsChildOf(root))) continue;
            string n = renderer.gameObject.name;
            if (n.StartsWith("GIS_Road_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_Ferry_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_City_", StringComparison.Ordinal) ||
                n.StartsWith("V013J_City_", StringComparison.Ordinal) ||
                n.StartsWith("V013K_Aalborg_", StringComparison.Ordinal) ||
                n.StartsWith("V013K_Aarhus_", StringComparison.Ordinal) ||
                n.StartsWith("V013L_Aalborg_", StringComparison.Ordinal) ||
                n.StartsWith("V013L_Aarhus_", StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private float SampleSurfaceY(float x, float z, out bool isLand)
    {
        isLand = false;
        if (landCollider != null)
        {
            Ray ray = new Ray(new Vector3(x, 40f, z), Vector3.down);
            RaycastHit hit;
            if (landCollider.Raycast(ray, out hit, 90f))
            {
                isLand = true;
                return hit.point.y;
            }
        }
        return WaterY;
    }

    private Vector2 ResolveCameraTargetLonLat()
    {
        Vector3 world;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (landCollider != null && landCollider.Raycast(ray, out hit, 2000f))
            world = hit.point;
        else
        {
            Plane waterPlane = new Plane(Vector3.up, new Vector3(0f, WaterY, 0f));
            float enter;
            world = waterPlane.Raycast(ray, out enter) ? ray.GetPoint(enter) : cam.transform.position;
        }

        double lon = CampaignGeoProjection.MinLongitude +
                     ((world.x / CampaignGeoProjection.MapWidth) + 0.5) *
                     (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double lat = CampaignGeoProjection.MinLatitude +
                     ((world.z / CampaignGeoProjection.MapDepth) + 0.5) *
                     (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        lon = Math.Max(MinLon, Math.Min(MaxLon, lon));
        lat = Math.Max(MinLat, Math.Min(MaxLat, lat));
        return new Vector2((float)lon, (float)lat);
    }

    private string GetUnifiedCachePath(TileRequest request)
    {
        return Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapCache", "unified",
            request.Zoom.ToString(), request.X.ToString(), request.Y + ".png");
    }

    private void ClearDetailTiles()
    {
        List<string> keys = new List<string>(detailTiles.Keys);
        foreach (string key in keys) RemoveDetailTile(key);
        detailQueue.Clear();
        queuedDetailKeys.Clear();
        requiredDetailKeys.Clear();
    }

    private void RemoveDetailTile(string key)
    {
        TileVisual visual;
        if (!detailTiles.TryGetValue(key, out visual)) return;
        detailTiles.Remove(key);
        if (visual.Root != null) Destroy(visual.Root);
        if (visual.Material != null) Destroy(visual.Material);
        if (visual.Texture != null) Destroy(visual.Texture);
    }

    private void BuildLandmarks()
    {
        aalborgBarracks = BuildAalborgBarracks();
        aarhusFarm = BuildAarhusFarm();
        UpdateLandmarkLod();
    }

    private GameObject BuildAalborgBarracks()
    {
        Vector3 centre = FindLandAnchor(57.0415, 9.9105);
        GameObject site = new GameObject("V013M_Aalborg_Kaserne");
        site.transform.SetParent(landmarkRoot, false);
        site.transform.position = centre;
        site.transform.rotation = Quaternion.Euler(0f, 18f, 0f);

        Material wall = CreateLitColor(new Color(0.72f, 0.59f, 0.45f), "V013M_BarracksWall");
        Material roof = CreateLitColor(new Color(0.34f, 0.17f, 0.12f), "V013M_BarracksRoof");
        Material yard = CreateLitColor(new Color(0.60f, 0.57f, 0.46f), "V013M_ParadeGround");

        CreateBox(site.transform, "ParadeGround", new Vector3(0f, 0.002f, 0f), new Vector3(0.075f, 0.004f, 0.052f), yard);
        CreatePitchedBuilding(site.transform, "Kaserne_A", new Vector3(-0.025f, 0f, -0.014f), new Vector3(0.038f, 0.015f, 0.014f), wall, roof);
        CreatePitchedBuilding(site.transform, "Kaserne_B", new Vector3(0.025f, 0f, -0.014f), new Vector3(0.038f, 0.015f, 0.014f), wall, roof);
        CreatePitchedBuilding(site.transform, "Depot", new Vector3(0f, 0f, 0.018f), new Vector3(0.030f, 0.012f, 0.015f), wall, roof);
        BuildFlag(site.transform);
        return site;
    }

    private GameObject BuildAarhusFarm()
    {
        Vector3 centre = FindLandAnchor(56.1455, 10.1450);
        GameObject site = new GameObject("V013M_Aarhus_Gaard");
        site.transform.SetParent(landmarkRoot, false);
        site.transform.position = centre;
        site.transform.rotation = Quaternion.Euler(0f, -12f, 0f);

        Material wall = CreateLitColor(new Color(0.76f, 0.67f, 0.50f), "V013M_FarmWall");
        Material roof = CreateLitColor(new Color(0.39f, 0.21f, 0.14f), "V013M_FarmRoof");
        Material field = CreateLitColor(new Color(0.62f, 0.60f, 0.40f), "V013M_FarmField");

        CreateBox(site.transform, "Mark", new Vector3(0f, 0.002f, 0f), new Vector3(0.082f, 0.004f, 0.058f), field);
        CreatePitchedBuilding(site.transform, "Stuehus", new Vector3(-0.018f, 0f, -0.006f), new Vector3(0.034f, 0.014f, 0.017f), wall, roof);
        CreatePitchedBuilding(site.transform, "Lade", new Vector3(0.021f, 0f, 0.012f), new Vector3(0.040f, 0.017f, 0.021f), wall, roof);
        return site;
    }

    private Vector3 FindLandAnchor(double lat, double lon)
    {
        Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY);
        bool isLand;
        float y = SampleSurfaceY(p.x, p.z, out isLand);
        if (isLand)
        {
            p.y = y + 0.035f;
            return p;
        }

        double[] steps = { 0.004, 0.008, 0.014, 0.022, 0.032 };
        foreach (double step in steps)
        {
            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI * 2.0 / 16.0;
                Vector3 test = CampaignGeoProjection.Project3D(
                    lat + Math.Sin(angle) * step,
                    lon + Math.Cos(angle) * step,
                    WaterY);
                float testY = SampleSurfaceY(test.x, test.z, out isLand);
                if (isLand)
                {
                    test.y = testY + 0.035f;
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
        bool visible = cam.transform.position.y < 2.8f;
        if (aalborgBarracks != null && aalborgBarracks.activeSelf != visible) aalborgBarracks.SetActive(visible);
        if (aarhusFarm != null && aarhusFarm.activeSelf != visible) aarhusFarm.SetActive(visible);
    }

    private static void CreatePitchedBuilding(Transform parent, string name, Vector3 localPosition, Vector3 size, Material wall, Material roof)
    {
        CreateBox(parent, name, localPosition + new Vector3(0f, size.y * 0.5f, 0f), size, wall);

        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = name + "_RoofL";
        left.transform.SetParent(parent, false);
        left.transform.localPosition = localPosition + new Vector3(0f, size.y + 0.003f, -size.z * 0.16f);
        left.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
        left.transform.localScale = new Vector3(size.x * 1.03f, 0.003f, size.z * 0.62f);
        left.GetComponent<Renderer>().sharedMaterial = roof;
        RemoveCollider(left);

        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = name + "_RoofR";
        right.transform.SetParent(parent, false);
        right.transform.localPosition = localPosition + new Vector3(0f, size.y + 0.003f, size.z * 0.16f);
        right.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
        right.transform.localScale = new Vector3(size.x * 1.03f, 0.003f, size.z * 0.62f);
        right.GetComponent<Renderer>().sharedMaterial = roof;
        RemoveCollider(right);
    }

    private static void BuildFlag(Transform parent)
    {
        Material pole = CreateLitColor(new Color(0.28f, 0.25f, 0.21f), "V013M_FlagPole");
        Material red = CreateLitColor(new Color(0.66f, 0.08f, 0.08f), "V013M_DannebrogRed");
        Material white = CreateLitColor(new Color(0.95f, 0.94f, 0.88f), "V013M_DannebrogWhite");

        GameObject poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poleObj.name = "Flagstang";
        poleObj.transform.SetParent(parent, false);
        poleObj.transform.localPosition = new Vector3(0.032f, 0.027f, 0.012f);
        poleObj.transform.localScale = new Vector3(0.0012f, 0.027f, 0.0012f);
        poleObj.GetComponent<Renderer>().sharedMaterial = pole;
        RemoveCollider(poleObj);

        CreateBox(parent, "Dannebrog", new Vector3(0.040f, 0.050f, 0.012f), new Vector3(0.016f, 0.009f, 0.0012f), red);
        CreateBox(parent, "Dannebrog_H", new Vector3(0.040f, 0.050f, 0.0105f), new Vector3(0.016f, 0.0018f, 0.0014f), white);
        CreateBox(parent, "Dannebrog_V", new Vector3(0.0365f, 0.050f, 0.0105f), new Vector3(0.0018f, 0.009f, 0.0014f), white);
    }

    private static Material CreateLitColor(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.035f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
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

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private void EnsureStyles()
    {
        if (statusStyle != null) return;
        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.LowerLeft };
        statusStyle.normal.textColor = new Color(0.96f, 0.96f, 0.93f);
        attributionStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.LowerRight };
        attributionStyle.normal.textColor = new Color(0.96f, 0.96f, 0.96f);
        landmarkStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        landmarkStyle.normal.textColor = new Color(0.98f, 0.94f, 0.78f);
    }

    private void OnGUI()
    {
        if (!baseReady) return;
        EnsureStyles();
        GUI.Label(new Rect(12f, Screen.height - 24f, 760f, 18f), "v13m · " + status, statusStyle);
        GUI.Label(new Rect(Screen.width - 340f, Screen.height - 24f, 328f, 18f), Attribution, attributionStyle);

        if (cam != null && cam.transform.position.y < 2.8f)
        {
            DrawLandmarkLabel(aalborgBarracks, "Aalborg Kaserne");
            DrawLandmarkLabel(aarhusFarm, "Aarhus Gård");
        }
    }

    private void DrawLandmarkLabel(GameObject landmark, string text)
    {
        if (landmark == null || !landmark.activeInHierarchy || cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(landmark.transform.position + Vector3.up * 0.060f);
        if (screen.z <= 0f) return;
        float y = Screen.height - screen.y;
        GUI.Label(new Rect(screen.x - 65f, y - 8f, 130f, 18f), text, landmarkStyle);
    }

    private static int LonToTileX(double lon, int zoom)
    {
        return (int)Math.Floor(LonToTileXDouble(lon, zoom));
    }

    private static double LonToTileXDouble(double lon, int zoom)
    {
        return (lon + 180.0) / 360.0 * Math.Pow(2.0, zoom);
    }

    private static int LatToTileY(double lat, int zoom)
    {
        return (int)Math.Floor(LatToTileYDouble(lat, zoom));
    }

    private static double LatToTileYDouble(double lat, int zoom)
    {
        double rad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, zoom);
        return (1.0 - Math.Asinh(Math.Tan(rad)) / Math.PI) * 0.5 * n;
    }

    private static double TileXToLon(double x, int zoom)
    {
        return x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
    }

    private static double TileYToLat(double y, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        return Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * y / n))) * 180.0 / Math.PI;
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
