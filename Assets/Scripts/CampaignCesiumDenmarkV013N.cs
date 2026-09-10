using System;
using System.IO;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13n — Cesium Denmark 3D Foundation.
// Replaces the visible hand-built GIS/cartographic render stack with Cesium World Terrain
// plus Bing Maps Aerial (no labels) streamed through Cesium ion.
// Campaign simulation remains MAP-ONLY and is not changed by this presentation layer.
[DefaultExecutionOrder(-9000)]
public sealed class CampaignCesiumDenmarkV013N : MonoBehaviour
{
    public const string BuildTag = "v00.00.13n";
    public const string RootName = "V013N_CESIUM_DENMARK_3D";

    private const double HomeLongitude = 10.05;
    private const double HomeLatitude = 56.15;
    private const double HomeHeightMeters = 430000.0;
    private const double MinHeightMeters = 80.0;
    private const double MaxHeightMeters = 700000.0;

    private const long CesiumWorldTerrainAssetId = 1;
    private const long BingMapsAerialAssetId = 2;

    private CesiumGeoreference georeference;
    private Cesium3DTileset terrainTileset;
    private CesiumIonRasterOverlay aerialOverlay;
    private CesiumGlobeAnchor cameraAnchor;
    private Camera cam;

    private bool ready;
    private string status = "starter Cesium Danmark";
    private string tokenSource = "Cesium ion default/login";
    private GUIStyle statusStyle;
    private GUIStyle helpStyle;

    private Vector3 lastMousePosition;
    private bool dragging;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignCesiumDenmarkV013N>() != null)
            return;

        new GameObject("CampaignCesiumDenmarkV013N").AddComponent<CampaignCesiumDenmarkV013N>();
    }

    private void Awake()
    {
        DisableLegacyPresentationComponents();
        HideLegacyPresentationRenderers();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        DisableLegacyPresentationComponents();
        HideLegacyPresentationRenderers();
        BuildCesiumWorld();
    }

    private void Update()
    {
        DisableLegacyPresentationComponents();
        HideLegacyPresentationRenderers();

        if (!ready || cam == null || cameraAnchor == null)
            return;

        UpdateCameraInput();
    }

    private void LateUpdate()
    {
        HideLegacyPresentationRenderers();
    }

    private void BuildCesiumWorld()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            Destroy(existing);

        GameObject root = new GameObject(RootName);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        georeference = root.AddComponent<CesiumGeoreference>();
        georeference.SetOriginLongitudeLatitudeHeight(HomeLongitude, HomeLatitude, 0.0);

        GameObject terrainObject = new GameObject("V013N_CesiumWorldTerrain_BingAerial");
        terrainObject.transform.SetParent(root.transform, false);
        terrainObject.transform.localPosition = Vector3.zero;
        terrainObject.transform.localRotation = Quaternion.identity;
        terrainObject.transform.localScale = Vector3.one;

        terrainTileset = terrainObject.AddComponent<Cesium3DTileset>();
        terrainObject.AddComponent<CesiumCameraManager>();
        terrainTileset.tilesetSource = CesiumDataSource.FromCesiumIon;
        terrainTileset.ionAssetID = CesiumWorldTerrainAssetId;
        terrainTileset.maximumScreenSpaceError = 4.0f;
        terrainTileset.preloadAncestors = true;
        terrainTileset.preloadSiblings = true;
        terrainTileset.forbidHoles = true;
        terrainTileset.maximumSimultaneousTileLoads = 24;
        terrainTileset.maximumCachedBytes = 1024L * 1024L * 1024L;
        terrainTileset.showCreditsOnScreen = true;

        aerialOverlay = terrainObject.AddComponent<CesiumIonRasterOverlay>();
        aerialOverlay.ionAssetID = BingMapsAerialAssetId;

        string localToken = ResolveLocalIonToken();
        if (!string.IsNullOrWhiteSpace(localToken))
        {
            terrainTileset.ionAccessToken = localToken;
            aerialOverlay.ionAccessToken = localToken;
            tokenSource = "lokal ion-token";
        }

        ConfigureCamera(root.transform);
        ConfigureSceneLighting();

        ready = true;
        status = "Cesium World Terrain + Bing Aerial · Danmark";
        Debug.Log("CAMPAIGN-V013N|Cesium=True|TerrainAsset=1|ImageryAsset=2|LabelsInImagery=False|OldMapStack=False|MapOnly=True|SimulationChanged=False|TokenSource=" + tokenSource);
    }

    private void ConfigureCamera(Transform cesiumRoot)
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        CampaignMapCameraController oldController = cam.GetComponent<CampaignMapCameraController>();
        if (oldController != null)
            oldController.enabled = false;

        cam.transform.SetParent(cesiumRoot, false);
        cam.nearClipPlane = 0.25f;
        cam.farClipPlane = 2000000f;
        cam.fieldOfView = 45f;
        cam.backgroundColor = new Color(0.17f, 0.23f, 0.29f);

        cameraAnchor = cam.GetComponent<CesiumGlobeAnchor>();
        if (cameraAnchor == null)
            cameraAnchor = cam.gameObject.AddComponent<CesiumGlobeAnchor>();

        cameraAnchor.adjustOrientationForGlobeWhenMoving = true;
        cameraAnchor.detectTransformChanges = true;

        if (cam.GetComponent<CesiumOriginShift>() == null)
            cam.gameObject.AddComponent<CesiumOriginShift>();

        ResetCameraHome();
    }

    private void ResetCameraHome()
    {
        if (cameraAnchor == null || cam == null)
            return;

        cameraAnchor.longitudeLatitudeHeight = new double3(HomeLongitude, HomeLatitude, HomeHeightMeters);
        cam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void UpdateCameraInput()
    {
        double3 llh = cameraAnchor.longitudeLatitudeHeight;
        double height = Math.Max(MinHeightMeters, llh.z);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            double factor = Math.Pow(0.78, scroll);
            height = Math.Max(MinHeightMeters, Math.Min(MaxHeightMeters, height * factor));
            llh.z = height;
            cameraAnchor.longitudeLatitudeHeight = llh;
        }

        double metresPerPixel = Math.Max(0.08, 2.0 * height * Math.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5) / Math.Max(200, Screen.height));
        double panMetresPerSecond = Math.Max(40.0, height * 0.60);
        double eastMetres = 0.0;
        double northMetres = 0.0;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) eastMetres -= panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) eastMetres += panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) northMetres += panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) northMetres -= panMetresPerSecond * Time.unscaledDeltaTime;

        if (Input.GetMouseButtonDown(2))
        {
            dragging = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2)) dragging = false;

        if (dragging && Input.GetMouseButton(2))
        {
            Vector3 now = Input.mousePosition;
            Vector3 delta = now - lastMousePosition;
            lastMousePosition = now;
            eastMetres -= delta.x * metresPerPixel;
            northMetres -= delta.y * metresPerPixel;
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            ResetCameraHome();
            return;
        }

        if (Math.Abs(eastMetres) > 0.001 || Math.Abs(northMetres) > 0.001)
        {
            double latRad = llh.y * Math.PI / 180.0;
            double metresPerDegreeLat = 111320.0;
            double metresPerDegreeLon = Math.Max(15000.0, metresPerDegreeLat * Math.Cos(latRad));
            llh.x += eastMetres / metresPerDegreeLon;
            llh.y += northMetres / metresPerDegreeLat;

            // Denmark-first development envelope. Keeps the camera from wandering away during map QA.
            llh.x = Math.Max(7.0, Math.Min(13.7, llh.x));
            llh.y = Math.Max(54.2, Math.Min(58.3, llh.y));
            cameraAnchor.longitudeLatitudeHeight = llh;
        }
    }

    private static string ResolveLocalIonToken()
    {
        string env = Environment.GetEnvironmentVariable("CESIUM_ION_TOKEN");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        try
        {
            string path = Path.Combine(Application.persistentDataPath, "PROJECT1864", "Cesium", "ion-token.txt");
            if (File.Exists(path))
            {
                string value = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch (Exception ex)
        {
            Debug.Log("CAMPAIGN-V013N|TokenRead=False|" + ex.Message);
        }

        // Empty is intentional: Cesium may use the project/default ion token configured by the editor login.
        return string.Empty;
    }

    private static void DisableLegacyPresentationComponents()
    {
        MonoBehaviour[] components = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (MonoBehaviour component in components)
        {
            if (component == null || component is CampaignCesiumDenmarkV013N)
                continue;

            string n = component.GetType().Name;
            if (n == "CampaignUnifiedMapStartupGateV013M1" ||
                n == "CampaignUnifiedDenmark3DMapV013M" ||
                n == "CampaignPremiumCartographicVisualV013L" ||
                n == "CampaignLiveCartographicDrapeV013K" ||
                n == "CampaignHistorical3DMapV013J" ||
                n == "CampaignHistoricalMapLabelsV013J" ||
                n == "CampaignDenmarkGisFoundationV013I" ||
                n == "CampaignDenmarkPremiumVisualV013H" ||
                n == "CampaignDenmarkUiZoomFixV013G" ||
                n == "CampaignDenmarkLandmeshFixV013F" ||
                n == "CampaignDenmarkCleanRenderV013E" ||
                n == "CampaignDenmarkMapRebuildV013D" ||
                n == "CampaignDenmarkV013DLegacyGate" ||
                n == "CampaignDenmarkCleanupV013C" ||
                n == "CampaignLayerManagerV013")
            {
                component.enabled = false;
            }
        }
    }

    private static void HideLegacyPresentationRenderers()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Transform t = renderer.transform;
            if (t != null && t.root != null && t.root.name == RootName)
                continue;

            string n = renderer.gameObject.name;
            if (IsLegacyVisualName(n))
                renderer.enabled = false;
        }
    }

    private static bool IsLegacyVisualName(string n)
    {
        if (string.IsNullOrEmpty(n)) return false;

        return n.StartsWith("V013A_", StringComparison.Ordinal) ||
               n.StartsWith("V013B_", StringComparison.Ordinal) ||
               n.StartsWith("V013C_", StringComparison.Ordinal) ||
               n.StartsWith("V013D_", StringComparison.Ordinal) ||
               n.StartsWith("V013E_", StringComparison.Ordinal) ||
               n.StartsWith("V013F_", StringComparison.Ordinal) ||
               n.StartsWith("V013G_", StringComparison.Ordinal) ||
               n.StartsWith("V013H_", StringComparison.Ordinal) ||
               n.StartsWith("V013I_", StringComparison.Ordinal) ||
               n.StartsWith("V013J_", StringComparison.Ordinal) ||
               n.StartsWith("V013K_", StringComparison.Ordinal) ||
               n.StartsWith("V013L_", StringComparison.Ordinal) ||
               n.StartsWith("V013M_", StringComparison.Ordinal) ||
               n.StartsWith("GIS_", StringComparison.Ordinal) ||
               n.StartsWith("Outline_", StringComparison.Ordinal) ||
               n.StartsWith("StrategicLink_", StringComparison.Ordinal) ||
               n.StartsWith("CampaignNode_", StringComparison.Ordinal) ||
               n.StartsWith("CampaignControl_", StringComparison.Ordinal) ||
               n.StartsWith("Settlement3D_", StringComparison.Ordinal);
    }

    private void ConfigureSceneLighting()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.63f, 0.66f, 0.69f);

        Light sun = null;
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
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
            GameObject go = new GameObject("V013N_DenmarkSun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(1.0f, 0.96f, 0.90f);
        sun.intensity = 1.0f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    private void EnsureStyles()
    {
        if (statusStyle != null) return;

        statusStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        statusStyle.normal.textColor = new Color(0.94f, 0.94f, 0.90f);

        helpStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 10
        };
        helpStyle.normal.textColor = new Color(0.84f, 0.86f, 0.88f);
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Box(new Rect(12f, 112f, 455f, 27f),
            "13n · " + status + " · " + tokenSource,
            statusStyle);

        GUI.Label(new Rect(16f, 140f, 620f, 21f),
            "Zoom: musehjul · Pan: midterste mus / WASD · Home: Danmark",
            helpStyle);
    }
}
