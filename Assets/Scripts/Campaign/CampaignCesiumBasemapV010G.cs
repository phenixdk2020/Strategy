using System;
using System.IO;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// v00.00.10g Cesium-backed basemap provider.
/// Supports either Cesium ion World Terrain or MapTiler quantized-mesh terrain.
/// It is activated only when selected by the True 11 Basemap Lab.
/// </summary>
public sealed class CampaignCesiumBasemapV010G : MonoBehaviour
{
    public enum CesiumMode
    {
        IonWorldTerrain,
        MapTilerTerrain
    }

    private const double HomeLongitude = 10.05;
    private const double HomeLatitude = 56.15;
    private const double HomeHeight = 430000.0;
    private const double MinHeight = 100.0;
    private const double MaxHeight = 700000.0;

    private CesiumMode mode;
    private int providerId;
    private bool configured;
    private bool loadStarted;
    private bool loaded;
    private string status = "NOT CONFIGURED";

    private Camera localCamera;
    private Camera cesiumCamera;
    private CesiumGlobeAnchor cameraAnchor;
    private CesiumGeoreference georeference;
    private Cesium3DTileset tileset;
    private Vector3 lastMouse;
    private bool dragging;

    public string Status { get { return status; } }

    public void Configure(CesiumMode cesiumMode, int id)
    {
        mode = cesiumMode;
        providerId = id;
        configured = true;
        status = "READY TO LOAD";
    }

    public void EnsureLoaded(Camera campaignLocalCamera)
    {
        localCamera = campaignLocalCamera;

        if (!configured)
        {
            status = "NOT CONFIGURED";
            return;
        }

        if (!loadStarted)
        {
            loadStarted = true;
            BuildWorld();
        }

        if (loaded)
            ActivateCamera();
    }

    private void BuildWorld()
    {
        string key = mode == CesiumMode.IonWorldTerrain
            ? ResolveIonToken()
            : ResolveMapTilerKey();

        if (string.IsNullOrWhiteSpace(key))
        {
            status = mode == CesiumMode.IonWorldTerrain
                ? "CESIUM TOKEN REQUIRED"
                : "MAPTILER KEY REQUIRED";

            Debug.LogWarning(
                "CAMPAIGN-10G|Provider=" + providerId +
                "|Cesium=False|Reason=KeyMissing|Mode=" + mode);
            return;
        }

        GameObject world = new GameObject(
            mode == CesiumMode.IonWorldTerrain
                ? "CESIUM_ION_WORLD"
                : "CESIUM_MAPTILER_WORLD");
        world.transform.SetParent(transform, false);

        georeference = world.AddComponent<CesiumGeoreference>();
        georeference.SetOriginLongitudeLatitudeHeight(
            HomeLongitude,
            HomeLatitude,
            0.0);

        GameObject terrainObject = new GameObject("CESIUM_TERRAIN");
        terrainObject.transform.SetParent(world.transform, false);

        tileset = terrainObject.AddComponent<Cesium3DTileset>();
        terrainObject.AddComponent<CesiumCameraManager>();
        tileset.createPhysicsMeshes = false;
        tileset.maximumScreenSpaceError = 4.0f;
        tileset.preloadAncestors = true;
        tileset.preloadSiblings = true;
        tileset.forbidHoles = true;
        tileset.maximumSimultaneousTileLoads = 24;
        tileset.maximumCachedBytes = 1024L * 1024L * 1024L;
        tileset.showCreditsOnScreen = true;

        if (mode == CesiumMode.IonWorldTerrain)
        {
            CesiumIonServer server = CesiumIonServer.defaultServer;
            if (server != null)
                tileset.ionServer = server;

            // Set token before asset ID so no empty-token request is created.
            tileset.ionAccessToken = key;
            tileset.tilesetSource = CesiumDataSource.FromCesiumIon;
            tileset.ionAssetID = 1;

            CesiumIonRasterOverlay imagery =
                terrainObject.AddComponent<CesiumIonRasterOverlay>();
            if (server != null)
                imagery.ionServer = server;
            imagery.ionAccessToken = key;
            imagery.ionAssetID = 2;
            imagery.showCreditsOnScreen = true;

            status = "STREAMING CESIUM WORLD TERRAIN";
        }
        else
        {
            tileset.tilesetSource = CesiumDataSource.FromUrl;
            tileset.url =
                "https://api.maptiler.com/tiles/terrain-quantized-mesh-v2/tiles.json?key=" +
                Uri.EscapeDataString(key);

            CesiumUrlTemplateRasterOverlay overlay =
                terrainObject.AddComponent<CesiumUrlTemplateRasterOverlay>();
            overlay.projection = CesiumUrlTemplateRasterOverlayProjection.WebMercator;
            overlay.templateUrl =
                "https://api.maptiler.com/maps/streets-v4/256/{z}/{x}/{reverseY}.png?key=" +
                Uri.EscapeDataString(key);
            overlay.showCreditsOnScreen = true;

            status = "STREAMING MAPTILER 3D TERRAIN";
        }

        CreateCesiumCamera(world.transform);
        loaded = true;
        ActivateCamera();

        Debug.Log(
            "CAMPAIGN-10G|Provider=" + providerId +
            "|Cesium=True|Mode=" + mode +
            "|PhysicsMeshes=False");
    }

    private void CreateCesiumCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject(
            "Basemap Cesium Camera " + providerId.ToString("00"));
        cameraObject.transform.SetParent(parent, false);

        cesiumCamera = cameraObject.AddComponent<Camera>();
        cesiumCamera.tag = "Untagged";
        cesiumCamera.nearClipPlane = 0.25f;
        cesiumCamera.farClipPlane = 2000000f;
        cesiumCamera.fieldOfView = 45f;
        cesiumCamera.backgroundColor = new Color(0.16f, 0.22f, 0.29f);

        cameraAnchor = cameraObject.AddComponent<CesiumGlobeAnchor>();
        cameraAnchor.adjustOrientationForGlobeWhenMoving = true;
        cameraAnchor.detectTransformChanges = true;

        cameraObject.AddComponent<CesiumOriginShift>();
        ResetHome();

        cameraObject.SetActive(false);
    }

    private void ActivateCamera()
    {
        if (!loaded || cesiumCamera == null)
            return;

        if (localCamera != null)
        {
            localCamera.tag = "Untagged";
            localCamera.gameObject.SetActive(false);
        }

        cesiumCamera.gameObject.SetActive(true);
        cesiumCamera.tag = "MainCamera";
    }

    public void DeactivateCamera()
    {
        dragging = false;

        if (cesiumCamera != null)
        {
            cesiumCamera.tag = "Untagged";
            cesiumCamera.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (loaded)
            ActivateCamera();
    }

    private void OnDisable()
    {
        DeactivateCamera();
    }

    private void Update()
    {
        if (!loaded ||
            cesiumCamera == null ||
            cameraAnchor == null ||
            !cesiumCamera.gameObject.activeInHierarchy)
        {
            return;
        }

        double3 llh = cameraAnchor.longitudeLatitudeHeight;
        double height = Math.Max(MinHeight, llh.z);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            height *= Math.Pow(0.78, scroll);
            height = Math.Max(MinHeight, Math.Min(MaxHeight, height));
            llh.z = height;
            cameraAnchor.longitudeLatitudeHeight = llh;
        }

        double panMetresPerSecond = Math.Max(40.0, height * 0.60);
        double east = 0.0;
        double north = 0.0;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            east -= panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            east += panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            north += panMetresPerSecond * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            north -= panMetresPerSecond * Time.unscaledDeltaTime;

        if (Input.GetMouseButtonDown(2))
        {
            dragging = true;
            lastMouse = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
            dragging = false;

        if (dragging && Input.GetMouseButton(2))
        {
            Vector3 now = Input.mousePosition;
            Vector3 delta = now - lastMouse;
            lastMouse = now;

            double metresPerPixel =
                Math.Max(
                    0.08,
                    2.0 * height *
                    Math.Tan(cesiumCamera.fieldOfView * Mathf.Deg2Rad * 0.5) /
                    Math.Max(200, Screen.height));

            east -= delta.x * metresPerPixel;
            north -= delta.y * metresPerPixel;
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            ResetHome();
            return;
        }

        if (Math.Abs(east) > 0.001 || Math.Abs(north) > 0.001)
        {
            double latRad = llh.y * Math.PI / 180.0;
            const double metresPerDegreeLat = 111320.0;
            double metresPerDegreeLon =
                Math.Max(15000.0, metresPerDegreeLat * Math.Cos(latRad));

            llh.x += east / metresPerDegreeLon;
            llh.y += north / metresPerDegreeLat;
            llh.x = Math.Max(7.0, Math.Min(15.7, llh.x));
            llh.y = Math.Max(54.2, Math.Min(58.3, llh.y));
            cameraAnchor.longitudeLatitudeHeight = llh;
        }
    }

    private void ResetHome()
    {
        if (cameraAnchor == null)
            return;

        cameraAnchor.longitudeLatitudeHeight =
            new double3(HomeLongitude, HomeLatitude, HomeHeight);

        if (cesiumCamera != null)
            cesiumCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static string ResolveIonToken()
    {
        string env = Environment.GetEnvironmentVariable("CESIUM_ION_TOKEN");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        string local = Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "Cesium",
            "ion-token.txt");

        try
        {
            if (File.Exists(local))
            {
                string value = File.ReadAllText(local).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-10G|CesiumTokenRead=False|" + ex.Message);
        }

        CesiumIonServer server = CesiumIonServer.defaultServer;
        if (server != null && !string.IsNullOrWhiteSpace(server.defaultIonAccessToken))
            return server.defaultIonAccessToken.Trim();

        return string.Empty;
    }

    private static string ResolveMapTilerKey()
    {
        string env = Environment.GetEnvironmentVariable("MAPTILER_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        string local = Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "Keys",
            "maptiler.txt");

        try
        {
            if (File.Exists(local))
            {
                string value = File.ReadAllText(local).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-10G|MapTilerKeyRead=False|" + ex.Message);
        }

        return string.Empty;
    }
}
