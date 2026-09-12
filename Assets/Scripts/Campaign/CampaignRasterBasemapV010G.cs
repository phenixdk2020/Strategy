using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10i.
/// Generic XYZ basemap loader with a special world-streaming mode for provider 9.
/// Providers 3/8 keep the v10h fixed Denmark overview behaviour.
/// Provider 9 streams Esri World Imagery around the camera, changes LOD with zoom,
/// retains the old visible generation until the next tile set is ready, and restores
/// the local Denmark camera when another basemap is selected.
/// </summary>
[DefaultExecutionOrder(25000)]
public sealed class CampaignRasterBasemapV010G : MonoBehaviour
{
    private const double DenmarkMinLat = 54.45;
    private const double DenmarkMaxLat = 57.85;
    private const double DenmarkMinLon = 7.55;
    private const double DenmarkMaxLon = 15.35;
    private const double DenmarkPriorityLat = 57.0488;
    private const double DenmarkPriorityLon = 9.9217;

    private const double DenmarkHomeLat = 56.10;
    private const double DenmarkHomeLon = 10.20;
    private const float DenmarkHomeOrtho = 43f;

    private const int MinimumCacheDays = 7;
    private const int FixedConcurrentLoads = 6;
    private const int WorldConcurrentLoads = 8;
    private const int MaxWorldTiles = 96;
    private const float WorldRefreshDelay = 0.22f;
    private const float WorldMinOrtho = 2.4f;
    private const float WorldMaxOrtho = 1850f;

    private sealed class TileJob
    {
        public int X;
        public int Y;
        public int Z;
        public float Priority;
    }

    private struct TileRange
    {
        public int XMin;
        public int XMax;
        public int YMin;
        public int YMax;
        public int Count;
    }

    private int providerId;
    private int zoom;
    private string providerName;
    private string urlTemplate;
    private string keyEnvironment;
    private string keyFile;
    private string attribution;
    private bool configured;
    private bool worldMode;

    // v10h fixed Denmark overview state for providers 3/8.
    private Transform fixedTileRoot;
    private bool fixedLoading;
    private bool fixedComplete;
    private int fixedExpected;
    private int fixedCompleted;
    private int fixedFailed;
    private int fixedInFlight;
    private int fixedGeneration;
    private readonly HashSet<string> fixedSuccessful = new HashSet<string>(StringComparer.Ordinal);

    // v10i provider-9 world streaming state.
    private Camera worldCamera;
    private bool worldControlActive;
    private bool cameraSnapshotValid;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool savedCameraOrthographic;
    private float savedCameraOrthoSize;
    private float savedCameraFieldOfView;

    private double worldCenterLon = DenmarkHomeLon;
    private double worldCenterLat = DenmarkHomeLat;
    private float worldOrtho = DenmarkHomeOrtho;
    private bool worldDirty;
    private float worldDirtySince;
    private bool worldLoading;
    private int worldGeneration;
    private int worldExpected;
    private int worldCompleted;
    private int worldFailed;
    private int worldInFlight;
    private int worldActiveZoom = -1;
    private int worldPendingZoom = -1;
    private Transform worldActiveRoot;
    private Transform worldPendingRoot;
    private bool middleDragging;
    private Vector3 lastMousePosition;

    public bool IsWorldStreamingMode { get { return worldMode; } }

    public string WorldDetailStatus
    {
        get
        {
            if (!worldMode)
                return string.Empty;

            return string.Format(
                "Center {0:0.00}°N {1:0.00}°E | Ortho {2:0.0} | z{3}",
                worldCenterLat,
                worldCenterLon,
                worldOrtho,
                worldLoading ? worldPendingZoom : worldActiveZoom);
        }
    }

    public string Status
    {
        get
        {
            if (!configured)
                return "NOT CONFIGURED";

            if (worldMode)
            {
                if (worldLoading)
                    return string.Format(
                        "WORLD z{0} LOADING {1}/{2} · {3} ACTIVE",
                        worldPendingZoom,
                        worldCompleted,
                        worldExpected,
                        worldInFlight);

                if (worldActiveRoot != null && worldActiveZoom >= 0)
                    return string.Format("WORLD z{0} READY · {1} TILES", worldActiveZoom, worldExpected);

                return "WORLD READY";
            }

            if (fixedComplete)
                return fixedFailed > 0
                    ? string.Format("READY · {0} FAILED", fixedFailed)
                    : "READY";

            if (fixedLoading)
                return string.Format(
                    "LOADING {0}/{1} · {2} ACTIVE",
                    fixedCompleted,
                    fixedExpected,
                    fixedInFlight);

            if (fixedSuccessful.Count > 0)
                return string.Format(
                    "PAUSED {0}/{1} · RESUME ON SELECT",
                    fixedSuccessful.Count,
                    fixedExpected);

            return "READY TO LOAD";
        }
    }

    public void Configure(
        int id,
        string name,
        string template,
        int tileZoom,
        string environmentKey,
        string localKeyFile,
        string providerAttribution)
    {
        providerId = id;
        providerName = name ?? ("Provider " + id);
        urlTemplate = template ?? string.Empty;
        zoom = Mathf.Clamp(tileZoom, 1, 18);
        keyEnvironment = environmentKey ?? string.Empty;
        keyFile = localKeyFile ?? string.Empty;
        attribution = providerAttribution ?? string.Empty;
        configured = !string.IsNullOrWhiteSpace(urlTemplate);
        worldMode = providerId == 9;

        if (!worldMode)
        {
            GameObject root = new GameObject("XYZ_TILES_ATOMIC");
            fixedTileRoot = root.transform;
            fixedTileRoot.SetParent(transform, false);
            root.SetActive(false);
        }
    }

    public void EnsureLoaded()
    {
        if (!configured)
            return;

        if (worldMode)
        {
            ActivateWorldStreaming();
            return;
        }

        EnsureFixedLoaded();
    }

    private void Update()
    {
        if (!worldMode || !worldControlActive || !gameObject.activeInHierarchy)
            return;

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
                return;
        }

        bool changed = HandleWorldCameraInput();
        ApplyWorldCamera();

        if (changed)
            MarkWorldDirty(true);

        if (worldDirty && !worldLoading &&
            Time.unscaledTime - worldDirtySince >= WorldRefreshDelay)
        {
            BeginWorldRefresh();
        }
    }

    private void OnDisable()
    {
        if (worldMode)
        {
            DeactivateWorldStreaming();
            return;
        }

        if (!fixedLoading)
            return;

        fixedGeneration++;
        fixedLoading = false;
        fixedInFlight = 0;
        fixedCompleted = fixedSuccessful.Count;

        Debug.Log(
            "CAMPAIGN-10I|Provider=" + providerId +
            "|RasterPaused=True|Successful=" + fixedSuccessful.Count +
            "|Expected=" + fixedExpected +
            "|ResumeOnSelect=True");
    }

    // ---------------------------------------------------------------------
    // Provider 9: world imagery streaming + camera/LOD
    // ---------------------------------------------------------------------

    private void ActivateWorldStreaming()
    {
        worldCamera = Camera.main;
        if (worldCamera == null)
        {
            Debug.LogWarning("CAMPAIGN-10I|WorldImagery=False|Reason=MainCameraMissing");
            return;
        }

        if (!worldControlActive)
        {
            savedCameraPosition = worldCamera.transform.position;
            savedCameraRotation = worldCamera.transform.rotation;
            savedCameraOrthographic = worldCamera.orthographic;
            savedCameraOrthoSize = worldCamera.orthographicSize;
            savedCameraFieldOfView = worldCamera.fieldOfView;
            cameraSnapshotValid = true;
        }

        worldControlActive = true;
        ApplyWorldCamera();
        MarkWorldDirty(false);

        Debug.Log(
            "CAMPAIGN-10I|Provider=09|WorldStreaming=True|Start=Denmark|" +
            "LOD=Dynamic|AtomicSwap=True|MaxTiles=" + MaxWorldTiles);
    }

    private void DeactivateWorldStreaming()
    {
        worldGeneration++;
        worldLoading = false;
        worldInFlight = 0;
        worldDirty = false;
        middleDragging = false;

        if (worldPendingRoot != null)
        {
            DestroyTileRoot(worldPendingRoot);
            worldPendingRoot = null;
        }

        if (cameraSnapshotValid && worldCamera != null)
        {
            worldCamera.orthographic = savedCameraOrthographic;
            worldCamera.orthographicSize = savedCameraOrthoSize;
            worldCamera.fieldOfView = savedCameraFieldOfView;
            worldCamera.transform.position = savedCameraPosition;
            worldCamera.transform.rotation = savedCameraRotation;
        }

        cameraSnapshotValid = false;
        worldControlActive = false;
    }

    private bool HandleWorldCameraInput()
    {
        bool changed = false;
        float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        float aspect = Mathf.Max(0.5f, Screen.width / (float)Mathf.Max(1, Screen.height));
        double halfLat = worldOrtho / CampaignGeoProjection.UnitsPerLatitudeDegree;
        double halfLon = worldOrtho * aspect / LongitudeUnitsPerDegree();

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.001f)
        {
            worldOrtho = Mathf.Clamp(
                worldOrtho * Mathf.Pow(0.80f, wheel),
                WorldMinOrtho,
                WorldMaxOrtho);
            changed = true;
        }

        double lonStep = Math.Max(0.02, halfLon * 1.15 * dt);
        double latStep = Math.Max(0.02, halfLat * 1.15 * dt);

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            worldCenterLon -= lonStep;
            changed = true;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            worldCenterLon += lonStep;
            changed = true;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            worldCenterLat += latStep;
            changed = true;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            worldCenterLat -= latStep;
            changed = true;
        }

        if (Input.GetMouseButtonDown(2))
        {
            middleDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
            middleDragging = false;

        if (middleDragging && Input.GetMouseButton(2))
        {
            Vector3 now = Input.mousePosition;
            Vector3 delta = now - lastMousePosition;
            lastMousePosition = now;

            worldCenterLon -= delta.x / Mathf.Max(1f, Screen.width) * halfLon * 2.0;
            worldCenterLat -= delta.y / Mathf.Max(1f, Screen.height) * halfLat * 2.0;
            changed = delta.sqrMagnitude > 0.001f || changed;
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            worldCenterLon = DenmarkHomeLon;
            worldCenterLat = DenmarkHomeLat;
            worldOrtho = DenmarkHomeOrtho;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            worldCenterLon = 14.0;
            worldCenterLat = 52.0;
            worldOrtho = 255f;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            worldCenterLon = 0.0;
            worldCenterLat = 0.0;
            worldOrtho = 1800f;
            changed = true;
        }

        worldCenterLon = Math.Max(-179.5, Math.Min(179.5, worldCenterLon));
        worldCenterLat = Math.Max(-80.0, Math.Min(80.0, worldCenterLat));
        return changed;
    }

    private void ApplyWorldCamera()
    {
        if (worldCamera == null)
            return;

        Vector3 center = CampaignGeoProjection.Project(
            (float)worldCenterLon,
            (float)worldCenterLat,
            0f);

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = worldOrtho;
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 500f;
        worldCamera.transform.position = new Vector3(center.x, 95f, center.z);
        worldCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void MarkWorldDirty(bool cancelPending)
    {
        worldDirty = true;
        worldDirtySince = Time.unscaledTime;

        if (cancelPending && worldLoading)
            CancelPendingWorldGeneration();
    }

    private void CancelPendingWorldGeneration()
    {
        worldGeneration++;
        worldLoading = false;
        worldInFlight = 0;

        if (worldPendingRoot != null)
        {
            DestroyTileRoot(worldPendingRoot);
            worldPendingRoot = null;
        }
    }

    private void BeginWorldRefresh()
    {
        if (!worldControlActive || worldCamera == null)
            return;

        int targetZoom = ChooseWorldZoom(worldOrtho);
        TileRange range = ComputeVisibleRange(targetZoom);

        while (targetZoom > 2 && range.Count > MaxWorldTiles)
        {
            targetZoom--;
            range = ComputeVisibleRange(targetZoom);
        }

        List<TileJob> jobs = BuildWorldJobs(range, targetZoom);
        if (jobs.Count == 0)
            return;

        int generation = ++worldGeneration;
        worldLoading = true;
        worldDirty = false;
        worldPendingZoom = targetZoom;
        worldExpected = jobs.Count;
        worldCompleted = 0;
        worldFailed = 0;
        worldInFlight = 0;

        GameObject pending = new GameObject(
            string.Format("WORLD_IMAGERY_PENDING_z{0}_g{1}", targetZoom, generation));
        worldPendingRoot = pending.transform;
        worldPendingRoot.SetParent(transform, false);
        pending.SetActive(false);

        StartCoroutine(LoadWorldGeneration(generation, jobs));

        Debug.Log(
            "CAMPAIGN-10I|Provider=09|WorldRefresh=True|Zoom=" + targetZoom +
            "|Tiles=" + jobs.Count +
            "|CenterLat=" + worldCenterLat.ToString("0.000") +
            "|CenterLon=" + worldCenterLon.ToString("0.000"));
    }

    private IEnumerator LoadWorldGeneration(int generation, List<TileJob> jobs)
    {
        string key = ResolveKey();
        Queue<TileJob> queue = new Queue<TileJob>(jobs);

        while ((queue.Count > 0 || worldInFlight > 0) && generation == worldGeneration)
        {
            while (queue.Count > 0 &&
                   worldInFlight < WorldConcurrentLoads &&
                   generation == worldGeneration)
            {
                TileJob job = queue.Dequeue();
                worldInFlight++;
                StartCoroutine(LoadWorldTileTracked(job, key, generation));
            }

            yield return null;
        }

        if (generation != worldGeneration || worldPendingRoot == null)
            yield break;

        worldPendingRoot.gameObject.SetActive(true);

        if (worldActiveRoot != null)
            DestroyTileRoot(worldActiveRoot);

        worldActiveRoot = worldPendingRoot;
        worldPendingRoot = null;
        worldActiveZoom = worldPendingZoom;
        worldLoading = false;
        worldInFlight = 0;

        Debug.Log(
            "CAMPAIGN-10I|Provider=09|WorldReady=True|Zoom=" + worldActiveZoom +
            "|Tiles=" + worldExpected +
            "|Failed=" + worldFailed +
            "|AtomicSwap=True|Attribution=" + attribution);
    }

    private IEnumerator LoadWorldTileTracked(TileJob job, string key, int generation)
    {
        Texture2D texture = null;
        yield return FetchTexture(job.Z, job.X, job.Y, key, value => texture = value);

        if (generation != worldGeneration || worldPendingRoot == null)
        {
            if (texture != null)
                Destroy(texture);
            yield break;
        }

        if (texture != null)
            CreateTileObject(worldPendingRoot, job.Z, job.X, job.Y, texture, false);
        else
        {
            worldFailed++;
            CreateTileObject(worldPendingRoot, job.Z, job.X, job.Y, null, true);
        }

        worldCompleted++;
        worldInFlight = Mathf.Max(0, worldInFlight - 1);
    }

    private List<TileJob> BuildWorldJobs(TileRange range, int z)
    {
        int centerX = LonToTileX(worldCenterLon, z);
        int centerY = LatToTileY(worldCenterLat, z);
        List<TileJob> jobs = new List<TileJob>(range.Count);

        for (int y = range.YMin; y <= range.YMax; y++)
        {
            for (int x = range.XMin; x <= range.XMax; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                jobs.Add(new TileJob
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Priority = dx * dx + dy * dy
                });
            }
        }

        jobs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return jobs;
    }

    private TileRange ComputeVisibleRange(int z)
    {
        float aspect = Mathf.Max(0.5f, Screen.width / (float)Mathf.Max(1, Screen.height));
        double halfLat = worldOrtho / CampaignGeoProjection.UnitsPerLatitudeDegree * 1.08;
        double halfLon = worldOrtho * aspect / LongitudeUnitsPerDegree() * 1.08;

        double minLat = Math.Max(-85.05112878, worldCenterLat - halfLat);
        double maxLat = Math.Min(85.05112878, worldCenterLat + halfLat);
        double minLon = Math.Max(-180.0, worldCenterLon - halfLon);
        double maxLon = Math.Min(180.0, worldCenterLon + halfLon);

        int n = 1 << z;
        int xMin = Mathf.Clamp(LonToTileX(minLon, z) - 1, 0, n - 1);
        int xMax = Mathf.Clamp(LonToTileX(maxLon, z) + 1, 0, n - 1);
        int yMin = Mathf.Clamp(LatToTileY(maxLat, z) - 1, 0, n - 1);
        int yMax = Mathf.Clamp(LatToTileY(minLat, z) + 1, 0, n - 1);

        return new TileRange
        {
            XMin = xMin,
            XMax = xMax,
            YMin = yMin,
            YMax = yMax,
            Count = Mathf.Max(0, xMax - xMin + 1) * Mathf.Max(0, yMax - yMin + 1)
        };
    }

    private static int ChooseWorldZoom(float ortho)
    {
        if (ortho > 1200f) return 2;
        if (ortho > 700f) return 3;
        if (ortho > 360f) return 4;
        if (ortho > 190f) return 5;
        if (ortho > 100f) return 6;
        if (ortho > 55f) return 7;
        if (ortho > 27f) return 8;
        if (ortho > 13.5f) return 9;
        if (ortho > 6.8f) return 10;
        if (ortho > 3.4f) return 11;
        return 12;
    }

    // ---------------------------------------------------------------------
    // Fixed Denmark raster providers (3 / 8)
    // ---------------------------------------------------------------------

    private void EnsureFixedLoaded()
    {
        if (fixedLoading)
            return;

        if (fixedComplete)
        {
            if (fixedTileRoot != null)
                fixedTileRoot.gameObject.SetActive(true);
            return;
        }

        int generation = ++fixedGeneration;
        fixedLoading = true;
        fixedInFlight = 0;
        fixedFailed = 0;

        if (fixedTileRoot != null)
            fixedTileRoot.gameObject.SetActive(false);

        StartCoroutine(LoadFixedGeneration(generation));
    }

    private IEnumerator LoadFixedGeneration(int generation)
    {
        string key = ResolveKey();
        if (urlTemplate.Contains("{key}") && string.IsNullOrWhiteSpace(key))
        {
            fixedExpected = 1;
            fixedCompleted = 1;
            fixedFailed = 1;
            fixedLoading = false;
            fixedComplete = true;
            yield break;
        }

        int xMin = LonToTileX(DenmarkMinLon, zoom);
        int xMax = LonToTileX(DenmarkMaxLon, zoom);
        int yMin = LatToTileY(DenmarkMaxLat, zoom);
        int yMax = LatToTileY(DenmarkMinLat, zoom);
        fixedExpected = (xMax - xMin + 1) * (yMax - yMin + 1);
        fixedCompleted = fixedSuccessful.Count;

        int priorityX = LonToTileX(DenmarkPriorityLon, zoom);
        int priorityY = LatToTileY(DenmarkPriorityLat, zoom);
        List<TileJob> jobs = new List<TileJob>(fixedExpected);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                string id = TileId(zoom, x, y);
                if (fixedSuccessful.Contains(id))
                    continue;

                float dx = x - priorityX;
                float dy = y - priorityY;
                jobs.Add(new TileJob
                {
                    X = x,
                    Y = y,
                    Z = zoom,
                    Priority = dx * dx + dy * dy
                });
            }
        }

        jobs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        Queue<TileJob> queue = new Queue<TileJob>(jobs);

        while ((queue.Count > 0 || fixedInFlight > 0) && generation == fixedGeneration)
        {
            while (queue.Count > 0 &&
                   fixedInFlight < FixedConcurrentLoads &&
                   generation == fixedGeneration)
            {
                TileJob job = queue.Dequeue();
                fixedInFlight++;
                StartCoroutine(LoadFixedTileTracked(job, key, generation));
            }
            yield return null;
        }

        if (generation != fixedGeneration)
            yield break;

        fixedLoading = false;
        fixedComplete = true;
        fixedCompleted = fixedExpected;
        if (fixedTileRoot != null)
            fixedTileRoot.gameObject.SetActive(true);
    }

    private IEnumerator LoadFixedTileTracked(TileJob job, string key, int generation)
    {
        Texture2D texture = null;
        yield return FetchTexture(job.Z, job.X, job.Y, key, value => texture = value);

        if (generation != fixedGeneration)
        {
            if (texture != null)
                Destroy(texture);
            yield break;
        }

        if (texture != null)
        {
            fixedSuccessful.Add(TileId(job.Z, job.X, job.Y));
            CreateTileObject(fixedTileRoot, job.Z, job.X, job.Y, texture, false);
        }
        else
        {
            fixedFailed++;
            CreateTileObject(fixedTileRoot, job.Z, job.X, job.Y, null, true);
        }

        fixedCompleted++;
        fixedInFlight = Mathf.Max(0, fixedInFlight - 1);
    }

    // ---------------------------------------------------------------------
    // Shared tile/cache/material helpers
    // ---------------------------------------------------------------------

    private IEnumerator FetchTexture(
        int z,
        int x,
        int y,
        string key,
        Action<Texture2D> completed)
    {
        string cachePath = CachePath(z, x, y);
        Texture2D texture = null;

        if (IsFreshCache(cachePath))
        {
            try
            {
                byte[] data = File.ReadAllBytes(cachePath);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(data, false))
                {
                    Destroy(texture);
                    texture = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CAMPAIGN-10I|RasterCacheRead=False|" + ex.Message);
                if (texture != null)
                    Destroy(texture);
                texture = null;
            }
        }

        if (texture == null)
        {
            string url = urlTemplate
                .Replace("{z}", z.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{key}", UnityWebRequest.EscapeURL(key ?? string.Empty));

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
            {
                request.timeout = 20;
                try
                {
                    request.SetRequestHeader("User-Agent", "PROJECT1864-UnityMapLab/0.00.10i");
                }
                catch
                {
                    // Some Unity platforms do not allow overriding User-Agent.
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    texture = DownloadHandlerTexture.GetContent(request);
                    TryWriteCache(cachePath, request.downloadHandler.data);
                }
                else
                {
                    Debug.LogWarning(
                        "CAMPAIGN-10I|Provider=" + providerId +
                        "|Tile=False|Z=" + z +
                        "|X=" + x +
                        "|Y=" + y +
                        "|HTTP=" + request.responseCode +
                        "|Error=" + request.error);
                }
            }
        }

        completed(texture);
    }

    private void CreateTileObject(
        Transform parent,
        int z,
        int x,
        int y,
        Texture2D texture,
        bool missing)
    {
        if (parent == null)
        {
            if (texture != null)
                Destroy(texture);
            return;
        }

        string name = string.Format(
            "{0}_{1:00}_{2}_{3}_{4}",
            missing ? "Missing" : "Basemap",
            providerId,
            z,
            x,
            y);

        Transform existing = parent.Find(name);
        if (existing != null)
            DestroyTileObject(existing.gameObject);

        double west = TileXToLon(x, z);
        double east = TileXToLon(x + 1, z);
        double north = TileYToLat(y, z);
        double south = TileYToLat(y + 1, z);

        Vector3 nw = CampaignGeoProjection.Project((float)west, (float)north, 0.06f);
        Vector3 ne = CampaignGeoProjection.Project((float)east, (float)north, 0.06f);
        Vector3 sw = CampaignGeoProjection.Project((float)west, (float)south, 0.06f);
        Vector3 se = CampaignGeoProjection.Project((float)east, (float)south, 0.06f);

        Mesh mesh = new Mesh
        {
            name = name + "_Mesh",
            vertices = new[] { sw, se, nw, ne },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            },
            triangles = new[] { 0, 2, 1, 1, 2, 3 }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        if (missing)
            renderer.sharedMaterial = CreateSolidMaterial(
                new Color(0.08f, 0.13f, 0.17f),
                name + "_MISSING");
        else
            renderer.sharedMaterial = CreateTextureMaterial(texture, name + "_MAT");
    }

    private static void DestroyTileObject(GameObject go)
    {
        if (go == null)
            return;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            Material material = renderer.sharedMaterial;
            Texture texture = material.mainTexture;
            if (texture != null)
                Destroy(texture);
            Destroy(material);
        }

        MeshFilter filter = go.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            Destroy(filter.sharedMesh);

        Destroy(go);
    }

    private static void DestroyTileRoot(Transform root)
    {
        if (root == null)
            return;

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
            if (material == null)
                continue;

            Texture texture = material.mainTexture;
            if (texture != null)
                Destroy(texture);
            Destroy(material);
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] != null && filters[i].sharedMesh != null)
                Destroy(filters[i].sharedMesh);
        }

        Destroy(root.gameObject);
    }

    private string ResolveKey()
    {
        if (!string.IsNullOrWhiteSpace(keyEnvironment))
        {
            string env = Environment.GetEnvironmentVariable(keyEnvironment);
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();
        }

        if (!string.IsNullOrWhiteSpace(keyFile))
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                "PROJECT1864",
                "Keys",
                keyFile);

            try
            {
                if (File.Exists(path))
                {
                    string value = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CAMPAIGN-10I|KeyRead=False|" + ex.Message);
            }
        }

        return string.Empty;
    }

    private string CachePath(int z, int x, int y)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "BasemapCache",
            "provider-" + providerId.ToString("00"),
            z.ToString(),
            x.ToString(),
            y + ".img");
    }

    private static bool IsFreshCache(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(path) <
                   TimeSpan.FromDays(MinimumCacheDays);
        }
        catch
        {
            return false;
        }
    }

    private static void TryWriteCache(string path, byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, data);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-10I|RasterCacheWrite=False|" + ex.Message);
        }
    }

    private static Material CreateTextureMaterial(Texture texture, string name)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        return material;
    }

    private static Material CreateSolidMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static string TileId(int z, int x, int y)
    {
        return z + ":" + x + ":" + y;
    }

    private static float LongitudeUnitsPerDegree()
    {
        Vector3 a = CampaignGeoProjection.Project(
            CampaignGeoProjection.OriginLongitude,
            CampaignGeoProjection.OriginLatitude,
            0f);
        Vector3 b = CampaignGeoProjection.Project(
            CampaignGeoProjection.OriginLongitude + 1f,
            CampaignGeoProjection.OriginLatitude,
            0f);
        return Mathf.Max(0.001f, Mathf.Abs(b.x - a.x));
    }

    private static int LonToTileX(double lon, int z)
    {
        double n = Math.Pow(2.0, z);
        return Mathf.Clamp(
            (int)Math.Floor((lon + 180.0) / 360.0 * n),
            0,
            (int)n - 1);
    }

    private static int LatToTileY(double lat, int z)
    {
        lat = Math.Max(-85.05112878, Math.Min(85.05112878, lat));
        double rad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, z);
        return Mathf.Clamp(
            (int)Math.Floor(
                (1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) *
                0.5 * n),
            0,
            (int)n - 1);
    }

    private static double TileXToLon(int x, int z)
    {
        double n = Math.Pow(2.0, z);
        return x / n * 360.0 - 180.0;
    }

    private static double TileYToLat(int y, int z)
    {
        double n = Math.Pow(2.0, z);
        double mercator = Math.PI * (1.0 - 2.0 * y / n);
        return Math.Atan(Math.Sinh(mercator)) * 180.0 / Math.PI;
    }
}
