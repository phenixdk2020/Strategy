using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13l — Premium Cartographic Visual Pass.
// Presentation-only layer on top of v13j/v13k.
// Adds close-range z12/z13 cartography, smoother 3D drape, premium lighting,
// smaller period landmarks and stricter legacy-visual suppression.
// Strategic simulation, route distance, ETA, campaign time and tactical code are untouched.
[DefaultExecutionOrder(4520)]
public sealed class CampaignPremiumCartographicVisualV013L : MonoBehaviour
{
    public const string BuildTag = "v00.00.13l";
    public const string RootName = "V013L_PREMIUM_CARTOGRAPHIC_VISUAL";

    private const float WaterY = 0.12f;
    private const float SurfaceOffset = 0.028f;
    private const int OverlayGrid = 32;
    private const int CacheDays = 14;
    private const float RefreshInterval = 0.28f;
    private const string TileTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    private const string Attribution = "© OpenStreetMap contributors · ODbL";
    private const string UserAgent = "PROJECT1864-Campaign/0.13l (+https://github.com/phenixdk2020/Strategy)";

    private Camera cam;
    private MeshCollider terrainCollider;
    private Transform root;
    private Transform tileRoot;
    private Transform landmarkRoot;
    private Transform ambienceRoot;

    private readonly Dictionary<string, TileVisual> activeTiles = new Dictionary<string, TileVisual>();
    private readonly Queue<TileRequest> queue = new Queue<TileRequest>();
    private readonly HashSet<string> queuedKeys = new HashSet<string>();
    private readonly HashSet<string> requiredKeys = new HashSet<string>();

    private Coroutine queueWorker;
    private int currentZoom = -1;
    private int currentCenterX = int.MinValue;
    private int currentCenterY = int.MinValue;
    private float nextRefresh;
    private float nextRetune;
    private bool initialized;
    private bool closeTilesAllowed;
    private string status = "venter på 3D-kort";

    private GameObject aalborgBarracks;
    private GameObject aarhusFarm;
    private GUIStyle statusStyle;
    private GUIStyle landmarkStyle;
    private GUIStyle attributionStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignPremiumCartographicVisualV013L>() != null)
            return;

        new GameObject("CampaignPremiumCartographicVisualV013L").AddComponent<CampaignPremiumCartographicVisualV013L>();
    }

    private void Start()
    {
        cam = Camera.main;
        ConfigureCamera();
        ConfigureAtmosphere();
        StartCoroutine(InitializeWhenMapReady());
    }

    private IEnumerator InitializeWhenMapReady()
    {
        while (true)
        {
            GameObject terrain = GameObject.Find("V013J_SmoothTerrain");
            GameObject liveRoot = GameObject.Find(CampaignLiveCartographicDrapeV013K.RootName);
            if (terrain != null && liveRoot != null)
            {
                terrainCollider = terrain.GetComponent<MeshCollider>();
                if (terrainCollider != null)
                    break;
            }

            status = "venter på v13j/v13k";
            yield return new WaitForSecondsRealtime(0.20f);
        }

        CreateRoots();
        SuppressLegacyPresentation();
        RetuneBaseCartography();
        BuildPremiumLandmarks();

        string historicalRaster = Path.Combine(Application.persistentDataPath, "PROJECT1864", "HistoricalMap", "denmark_1864.png");
        string customProvider = Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapProvider", "provider.json");
        closeTilesAllowed = !File.Exists(historicalRaster) && !File.Exists(customProvider);

        if (closeTilesAllowed)
        {
            queueWorker = StartCoroutine(ProcessTileQueue());
            status = "premium live cartography aktiv";
        }
        else if (File.Exists(historicalRaster))
        {
            status = "historisk raster aktivt · premium live tiles deaktiveret";
        }
        else
        {
            status = "custom map provider aktiv · premium live tiles deaktiveret";
        }

        initialized = true;
        Debug.Log("CAMPAIGN-V013L|PremiumCartography=True|CloseTiles=z12-z13|OverlayGrid=32|Lighting=True|SmallLandmarks=True|MapOnly=True|SimulationChanged=False");
    }

    private void Update()
    {
        if (!initialized)
            return;

        SuppressLegacyPresentation();
        UpdateLandmarkLod();

        if (Time.unscaledTime >= nextRetune)
        {
            nextRetune = Time.unscaledTime + 0.75f;
            RetuneBaseCartography();
        }

        if (closeTilesAllowed && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            RefreshCloseTiles();
        }
    }

    private void CreateRoots()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Destroy(old);

        root = new GameObject(RootName).transform;
        tileRoot = CreateChild(root, "L1_HIGH_DETAIL_MAP");
        landmarkRoot = CreateChild(root, "L6_PREMIUM_LANDMARKS");
        ambienceRoot = CreateChild(root, "L9_AMBIENCE");
    }

    private void ConfigureCamera()
    {
        if (cam == null)
            return;

        cam.nearClipPlane = 0.003f;
        cam.fieldOfView = 44f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.MinHeight = 0.14f;
            controller.MaxHeight = 190f;
            controller.TerrainClearance = 0.09f;
            controller.ZoomSpeed = 92f;
            controller.PanSpeed = 31f;
            controller.MinPitch = 18f;
            controller.MaxPitch = 82f;
        }
    }

    private void ConfigureAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.63f, 0.67f, 0.61f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.69f, 0.75f, 0.77f);
        RenderSettings.fogStartDistance = 72f;
        RenderSettings.fogEndDistance = 210f;

        if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
            cam.backgroundColor = new Color(0.57f, 0.68f, 0.74f);

        Light primary = null;
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                primary = light;
                break;
            }
        }

        if (primary == null)
        {
            GameObject lightObject = new GameObject("V013L_PremiumSun");
            primary = lightObject.AddComponent<Light>();
            primary.type = LightType.Directional;
        }

        primary.color = new Color(1.0f, 0.92f, 0.78f);
        primary.intensity = 1.05f;
        primary.shadows = LightShadows.Soft;
        primary.shadowStrength = 0.64f;
        primary.transform.rotation = Quaternion.Euler(48f, -34f, 0f);

        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 85f);
    }

    private void RetuneBaseCartography()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name;
            if (n.StartsWith("V013K_MapTile_", StringComparison.Ordinal))
            {
                Material material = renderer.sharedMaterial;
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.98f, 0.99f, 0.96f));
                if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.98f, 0.99f, 0.96f));
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.015f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;

                Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.GetTexture("_MainTex");
                Texture2D tex2D = texture as Texture2D;
                if (tex2D != null)
                {
                    tex2D.filterMode = FilterMode.Trilinear;
                    tex2D.anisoLevel = 8;
                }
            }
            else if (n == "V013J_SmoothTerrain")
            {
                Material material = renderer.sharedMaterial;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.70f, 0.73f, 0.66f));
                    if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.70f, 0.73f, 0.66f));
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.01f);
                }
            }
        }
    }

    private void RefreshCloseTiles()
    {
        if (cam == null || terrainCollider == null)
            return;

        int zoom = SelectCloseZoom(cam.transform.position.y);
        if (zoom < 0)
        {
            ClearCloseTiles();
            currentZoom = -1;
            currentCenterX = int.MinValue;
            currentCenterY = int.MinValue;
            status = "premium overview · base map";
            return;
        }

        Vector2 lonLat = ResolveCameraTargetLonLat();
        int centerX = LonToTileX(lonLat.x, zoom);
        int centerY = LatToTileY(lonLat.y, zoom);

        if (zoom == currentZoom && centerX == currentCenterX && centerY == currentCenterY)
            return;

        currentZoom = zoom;
        currentCenterX = centerX;
        currentCenterY = centerY;
        requiredKeys.Clear();

        const int radius = 1;
        int span = 1 << zoom;
        int maxTile = span - 1;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y > maxTile)
                continue;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int wrappedX = x;
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

        status = string.Format("premium detail · z{0} · {1}/9 tiles", zoom, activeTiles.Count);
    }

    private int SelectCloseZoom(float height)
    {
        if (height > 5.6f) return -1;
        if (height > 1.55f) return 12;
        return 13;
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
                status = string.Format("premium detail · z{0} · {1}/9 tiles", currentZoom, activeTiles.Count);
            }
            else if (texture != null)
            {
                Destroy(texture);
            }

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
            if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-CacheDays))
                return null;

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                Destroy(texture);
                return null;
            }
            PrepareTexture(texture);
            return texture;
        }
        catch (Exception ex)
        {
            Debug.Log("CAMPAIGN-V013L|TileCacheRead=False|" + ex.Message);
            return null;
        }
    }

    private IEnumerator DownloadTile(TileRequest request, Action<Texture2D> callback)
    {
        string url = TileTemplate
            .Replace("{z}", request.Zoom.ToString())
            .Replace("{x}", request.X.ToString())
            .Replace("{y}", request.Y.ToString());

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url, false))
        {
            webRequest.timeout = 12;
            try { webRequest.SetRequestHeader("User-Agent", UserAgent); } catch { }
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("CAMPAIGN-V013L|TileDownload=False|" + request.Zoom + "/" + request.X + "/" + request.Y + "|" + webRequest.error);
                callback(null);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            PrepareTexture(texture);

            try
            {
                string path = GetTileCachePath(request);
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                byte[] bytes = webRequest.downloadHandler.data;
                if (bytes != null && bytes.Length > 0) File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Debug.Log("CAMPAIGN-V013L|TileCacheWrite=False|" + ex.Message);
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

    private void BuildTileVisual(TileRequest request, Texture2D texture)
    {
        string key = TileKey(request.Zoom, request.X, request.Y);
        GameObject go = new GameObject("V013L_HighDetailTile_" + key.Replace('/', '_'));
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
                double lon = TileXToLon(request.X + u, request.Zoom);
                int index = gy * size + gx;

                Vector3 world = CampaignGeoProjection.Project3D(lat, lon, WaterY);
                world.y = SampleSurfaceY(world.x, world.z) + SurfaceOffset;
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

        Mesh mesh = new Mesh { name = go.name + "_Mesh", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = CreateMapMaterial(texture, go.name + "_Material");
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        activeTiles[key] = new TileVisual(go, texture, material);
    }

    private static Material CreateMapMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material material = new Material(shader) { name = name, color = new Color(0.99f, 1.0f, 0.97f) };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.99f, 1.0f, 0.97f));
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.018f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private void ClearCloseTiles()
    {
        List<string> keys = new List<string>(activeTiles.Keys);
        foreach (string key in keys)
            RemoveTile(key);
        requiredKeys.Clear();
        queue.Clear();
        queuedKeys.Clear();
    }

    private void RemoveTile(string key)
    {
        TileVisual visual;
        if (!activeTiles.TryGetValue(key, out visual)) return;
        activeTiles.Remove(key);
        if (visual.Root != null) Destroy(visual.Root);
        if (visual.Material != null) Destroy(visual.Material);
        if (visual.Texture != null) Destroy(visual.Texture);
    }

    private void SuppressLegacyPresentation()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || (root != null && renderer.transform.IsChildOf(root)))
                continue;

            string n = renderer.gameObject.name;
            if (n.StartsWith("GIS_Road_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_Ferry_", StringComparison.Ordinal) ||
                n.StartsWith("GIS_City_", StringComparison.Ordinal) ||
                n.StartsWith("V013J_City_", StringComparison.Ordinal) ||
                n.StartsWith("V013K_Aalborg_", StringComparison.Ordinal) ||
                n.StartsWith("V013K_Aarhus_", StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }

        CampaignHistoricalMapLabelsV013J labels = UnityEngine.Object.FindAnyObjectByType<CampaignHistoricalMapLabelsV013J>();
        if (labels != null) labels.enabled = false;
    }

    private void BuildPremiumLandmarks()
    {
        HideV13KLandmarks();
        aalborgBarracks = BuildAalborgBarracks();
        aarhusFarm = BuildAarhusFarm();
        UpdateLandmarkLod();
    }

    private void HideV13KLandmarks()
    {
        GameObject liveRoot = GameObject.Find(CampaignLiveCartographicDrapeV013K.RootName);
        if (liveRoot == null) return;
        Transform old = liveRoot.transform.Find("L6_MAP_LANDMARKS");
        if (old != null) old.gameObject.SetActive(false);
    }

    private GameObject BuildAalborgBarracks()
    {
        Vector3 centre = FindLandAnchor(57.0415, 9.9105);
        GameObject site = new GameObject("V013L_Aalborg_Kaserne");
        site.transform.SetParent(landmarkRoot, false);
        site.transform.position = centre;
        site.transform.rotation = Quaternion.Euler(0f, 17f, 0f);

        Material wall = CreateLitColor(new Color(0.72f, 0.58f, 0.43f), "V013L_BarracksWall");
        Material roof = CreateLitColor(new Color(0.33f, 0.16f, 0.12f), "V013L_BarracksRoof");
        Material yard = CreateLitColor(new Color(0.58f, 0.55f, 0.43f), "V013L_ParadeGround");
        Material fence = CreateLitColor(new Color(0.29f, 0.25f, 0.20f), "V013L_Fence");

        CreateBox(site.transform, "ParadeGround", new Vector3(0f, 0.003f, 0f), new Vector3(0.105f, 0.006f, 0.070f), yard);
        CreatePitchedBuilding(site.transform, "Kaserne_A", new Vector3(-0.036f, 0f, -0.020f), new Vector3(0.052f, 0.020f, 0.017f), wall, roof);
        CreatePitchedBuilding(site.transform, "Kaserne_B", new Vector3(0.036f, 0f, -0.020f), new Vector3(0.052f, 0.020f, 0.017f), wall, roof);
        CreatePitchedBuilding(site.transform, "Depot", new Vector3(-0.030f, 0f, 0.026f), new Vector3(0.036f, 0.015f, 0.018f), wall, roof);
        CreatePitchedBuilding(site.transform, "Vagtbygning", new Vector3(0.036f, 0f, 0.028f), new Vector3(0.026f, 0.014f, 0.016f), wall, roof);
        BuildFence(site.transform, 0.064f, 0.047f, fence);
        BuildFlag(site.transform);
        BuildTree(site.transform, new Vector3(-0.072f, 0f, 0.036f), 0.013f);
        BuildTree(site.transform, new Vector3(0.071f, 0f, 0.037f), 0.012f);
        return site;
    }

    private GameObject BuildAarhusFarm()
    {
        Vector3 centre = FindLandAnchor(56.1455, 10.1450);
        GameObject site = new GameObject("V013L_Aarhus_Gaard");
        site.transform.SetParent(landmarkRoot, false);
        site.transform.position = centre;
        site.transform.rotation = Quaternion.Euler(0f, -11f, 0f);

        Material wall = CreateLitColor(new Color(0.75f, 0.66f, 0.49f), "V013L_FarmWall");
        Material roof = CreateLitColor(new Color(0.38f, 0.20f, 0.13f), "V013L_FarmRoof");
        Material field = CreateLitColor(new Color(0.60f, 0.59f, 0.38f), "V013L_Field");
        Material fence = CreateLitColor(new Color(0.34f, 0.29f, 0.21f), "V013L_FarmFence");

        CreateBox(site.transform, "Mark", new Vector3(0f, 0.003f, 0f), new Vector3(0.115f, 0.005f, 0.080f), field);
        CreatePitchedBuilding(site.transform, "Stuehus", new Vector3(-0.026f, 0f, -0.010f), new Vector3(0.043f, 0.017f, 0.022f), wall, roof);
        CreatePitchedBuilding(site.transform, "Lade", new Vector3(0.028f, 0f, 0.018f), new Vector3(0.053f, 0.021f, 0.027f), wall, roof);
        BuildFence(site.transform, 0.067f, 0.050f, fence);
        BuildTree(site.transform, new Vector3(-0.062f, 0f, 0.030f), 0.014f);
        BuildTree(site.transform, new Vector3(0.060f, 0f, -0.030f), 0.013f);
        return site;
    }

    private static void BuildFence(Transform parent, float halfX, float halfZ, Material material)
    {
        for (int i = -3; i <= 3; i++)
        {
            float t = i / 3f;
            CreateBox(parent, "FenceN", new Vector3(t * halfX, 0.008f, halfZ), new Vector3(0.003f, 0.016f, 0.003f), material);
            CreateBox(parent, "FenceS", new Vector3(t * halfX, 0.008f, -halfZ), new Vector3(0.003f, 0.016f, 0.003f), material);
        }
        for (int i = -2; i <= 2; i++)
        {
            float t = i / 2f;
            CreateBox(parent, "FenceE", new Vector3(halfX, 0.008f, t * halfZ), new Vector3(0.003f, 0.016f, 0.003f), material);
            CreateBox(parent, "FenceW", new Vector3(-halfX, 0.008f, t * halfZ), new Vector3(0.003f, 0.016f, 0.003f), material);
        }
    }

    private static void BuildFlag(Transform parent)
    {
        Material poleMaterial = CreateLitColor(new Color(0.28f, 0.25f, 0.21f), "V013L_FlagPole");
        Material red = CreateLitColor(new Color(0.62f, 0.08f, 0.08f), "V013L_DannebrogRed");
        Material white = CreateLitColor(new Color(0.94f, 0.92f, 0.84f), "V013L_DannebrogWhite");

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Flagstang";
        pole.transform.SetParent(parent, false);
        pole.transform.localPosition = new Vector3(0.052f, 0.034f, 0.020f);
        pole.transform.localScale = new Vector3(0.0015f, 0.034f, 0.0015f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        RemoveCollider(pole);

        CreateBox(parent, "Dannebrog", new Vector3(0.061f, 0.061f, 0.020f), new Vector3(0.018f, 0.010f, 0.0015f), red);
        CreateBox(parent, "Dannebrog_H", new Vector3(0.061f, 0.061f, 0.018f), new Vector3(0.018f, 0.0022f, 0.0018f), white);
        CreateBox(parent, "Dannebrog_V", new Vector3(0.057f, 0.061f, 0.018f), new Vector3(0.0022f, 0.010f, 0.0018f), white);
    }

    private static void BuildTree(Transform parent, Vector3 localPosition, float size)
    {
        Material trunk = CreateLitColor(new Color(0.30f, 0.22f, 0.15f), "V013L_TreeTrunk");
        Material crown = CreateLitColor(new Color(0.26f, 0.40f, 0.23f), "V013L_TreeCrown");

        GameObject trunkObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkObject.name = "Trae_Stamme";
        trunkObject.transform.SetParent(parent, false);
        trunkObject.transform.localPosition = localPosition + new Vector3(0f, size * 0.45f, 0f);
        trunkObject.transform.localScale = new Vector3(size * 0.12f, size * 0.45f, size * 0.12f);
        trunkObject.GetComponent<Renderer>().sharedMaterial = trunk;
        RemoveCollider(trunkObject);

        GameObject crownObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownObject.name = "Trae_Krone";
        crownObject.transform.SetParent(parent, false);
        crownObject.transform.localPosition = localPosition + new Vector3(0f, size * 1.15f, 0f);
        crownObject.transform.localScale = new Vector3(size * 0.72f, size, size * 0.72f);
        crownObject.GetComponent<Renderer>().sharedMaterial = crown;
        RemoveCollider(crownObject);
    }

    private Vector3 FindLandAnchor(double lat, double lon)
    {
        Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY);
        float y = SampleSurfaceY(p.x, p.z);
        if (y > WaterY + 0.025f)
        {
            p.y = y + 0.010f;
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
                float testY = SampleSurfaceY(test.x, test.z);
                if (testY > WaterY + 0.025f)
                {
                    test.y = testY + 0.010f;
                    return test;
                }
            }
        }

        p.y = WaterY + 0.04f;
        return p;
    }

    private float SampleSurfaceY(float x, float z)
    {
        if (terrainCollider != null)
        {
            Ray ray = new Ray(new Vector3(x, 35f, z), Vector3.down);
            RaycastHit hit;
            if (terrainCollider.Raycast(ray, out hit, 70f))
                return hit.point.y;
        }
        return WaterY + 0.010f;
    }

    private void UpdateLandmarkLod()
    {
        if (cam == null) return;
        bool visible = cam.transform.position.y < 3.1f;
        if (aalborgBarracks != null && aalborgBarracks.activeSelf != visible) aalborgBarracks.SetActive(visible);
        if (aarhusFarm != null && aarhusFarm.activeSelf != visible) aarhusFarm.SetActive(visible);
    }

    private Vector2 ResolveCameraTargetLonLat()
    {
        Vector3 world;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (terrainCollider != null && terrainCollider.Raycast(ray, out hit, 2000f))
            world = hit.point;
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, WaterY, 0f));
            float enter;
            world = plane.Raycast(ray, out enter) ? ray.GetPoint(enter) : cam.transform.position;
        }

        double lon = CampaignGeoProjection.MinLongitude + ((world.x / CampaignGeoProjection.MapWidth) + 0.5) *
                     (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double lat = CampaignGeoProjection.MinLatitude + ((world.z / CampaignGeoProjection.MapDepth) + 0.5) *
                     (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        return new Vector2((float)lon, (float)lat);
    }

    private string GetTileCachePath(TileRequest request)
    {
        return Path.Combine(Application.persistentDataPath, "PROJECT1864", "MapCache", "premium",
            request.Zoom.ToString(), request.X.ToString(), request.Y + ".png");
    }

    private static int LonToTileX(double lon, int zoom)
    {
        return (int)Math.Floor((lon + 180.0) / 360.0 * Math.Pow(2.0, zoom));
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

    private static void CreatePitchedBuilding(Transform parent, string name, Vector3 localPosition, Vector3 size, Material wall, Material roof)
    {
        CreateBox(parent, name, localPosition + new Vector3(0f, size.y * 0.5f, 0f), size, wall);

        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = name + "_RoofL";
        left.transform.SetParent(parent, false);
        left.transform.localPosition = localPosition + new Vector3(0f, size.y + 0.004f, -size.z * 0.18f);
        left.transform.localRotation = Quaternion.Euler(size.x >= size.z ? 28f : 0f, 0f, size.x < size.z ? 28f : 0f);
        left.transform.localScale = new Vector3(size.x * 1.03f, 0.004f, size.z * 0.62f);
        left.GetComponent<Renderer>().sharedMaterial = roof;
        RemoveCollider(left);

        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = name + "_RoofR";
        right.transform.SetParent(parent, false);
        right.transform.localPosition = localPosition + new Vector3(0f, size.y + 0.004f, size.z * 0.18f);
        right.transform.localRotation = Quaternion.Euler(size.x >= size.z ? -28f : 0f, 0f, size.x < size.z ? -28f : 0f);
        right.transform.localScale = new Vector3(size.x * 1.03f, 0.004f, size.z * 0.62f);
        right.GetComponent<Renderer>().sharedMaterial = roof;
        RemoveCollider(right);
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
        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.LowerLeft };
        statusStyle.normal.textColor = new Color(0.95f, 0.95f, 0.91f);
        landmarkStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        landmarkStyle.normal.textColor = new Color(0.98f, 0.94f, 0.78f);
        attributionStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.LowerRight };
        attributionStyle.normal.textColor = new Color(0.96f, 0.96f, 0.96f);
    }

    private void OnGUI()
    {
        if (!initialized) return;
        EnsureStyles();
        GUI.Label(new Rect(12f, Screen.height - 42f, 720f, 18f), "v13l · " + status, statusStyle);

        if (closeTilesAllowed && currentZoom >= 12)
            GUI.Label(new Rect(Screen.width - 340f, Screen.height - 42f, 328f, 18f), Attribution, attributionStyle);

        if (cam != null && cam.transform.position.y < 3.1f)
        {
            DrawLandmarkLabel(aalborgBarracks, "Aalborg Kaserne");
            DrawLandmarkLabel(aarhusFarm, "Aarhus Gård");
        }
    }

    private void DrawLandmarkLabel(GameObject landmark, string text)
    {
        if (landmark == null || !landmark.activeInHierarchy || cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(landmark.transform.position + Vector3.up * 0.075f);
        if (screen.z <= 0f) return;
        float y = Screen.height - screen.y;
        GUI.Label(new Rect(screen.x - 65f, y - 8f, 130f, 18f), text, landmarkStyle);
    }

    private readonly struct TileRequest
    {
        public readonly int Zoom;
        public readonly int X;
        public readonly int Y;
        public TileRequest(int zoom, int x, int y) { Zoom = zoom; X = x; Y = y; }
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
