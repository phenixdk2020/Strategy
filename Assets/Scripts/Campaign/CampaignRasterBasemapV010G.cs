using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10h
/// Resilient XYZ/Web-Mercator raster basemap loader.
///
/// v10h fixes the v10g failure mode where deactivating a provider stopped its
/// coroutine while loadStarted stayed true, leaving providers permanently stuck
/// at states such as LOADING 18/30. Tiles are now loaded concurrently, successful
/// tiles survive provider switches, failed tiles can be retried, and the raster
/// root is revealed atomically after the current overview set has completed.
/// </summary>
public sealed class CampaignRasterBasemapV010G : MonoBehaviour
{
    private const double MinLat = 54.45;
    private const double MaxLat = 57.85;
    private const double MinLon = 7.55;
    private const double MaxLon = 15.35;
    private const double PriorityLat = 57.0488; // Aalborg / Limfjord QA first
    private const double PriorityLon = 9.9217;
    private const int MinimumCacheDays = 7;
    private const int MaxConcurrentLoads = 6;

    private sealed class TileJob
    {
        public int X;
        public int Y;
        public float Priority;
    }

    private int providerId;
    private int zoom;
    private string providerName;
    private string urlTemplate;
    private string keyEnvironment;
    private string keyFile;
    private string attribution;

    private Transform tileRoot;
    private bool configured;
    private bool loading;
    private bool loadComplete;
    private int expectedTiles;
    private int completedTiles;
    private int failedTiles;
    private int inFlightTiles;
    private int loadGeneration;

    private readonly HashSet<string> successfulTiles = new HashSet<string>(StringComparer.Ordinal);

    public string Status
    {
        get
        {
            if (!configured) return "NOT CONFIGURED";
            if (loadComplete)
            {
                return failedTiles > 0
                    ? string.Format("READY · {0} FAILED", failedTiles)
                    : "READY";
            }

            if (loading)
                return string.Format("LOADING {0}/{1} · {2} ACTIVE", completedTiles, expectedTiles, inFlightTiles);

            if (successfulTiles.Count > 0)
                return string.Format("PAUSED {0}/{1} · RESUME ON SELECT", successfulTiles.Count, expectedTiles);

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

        GameObject root = new GameObject("XYZ_TILES_ATOMIC");
        tileRoot = root.transform;
        tileRoot.SetParent(transform, false);
        root.SetActive(false);
    }

    public void EnsureLoaded()
    {
        if (!configured || loading)
            return;

        if (loadComplete)
        {
            if (tileRoot != null)
                tileRoot.gameObject.SetActive(true);
            return;
        }

        int generation = ++loadGeneration;
        loading = true;
        inFlightTiles = 0;
        failedTiles = 0;

        if (tileRoot != null)
            tileRoot.gameObject.SetActive(false);

        StartCoroutine(LoadTiles(generation));
    }

    private void OnDisable()
    {
        if (!loading)
            return;

        // Unity stops coroutines on inactive GameObjects. Explicitly invalidate the
        // old session so selecting this provider again can resume from successful
        // cached/materialized tiles instead of remaining frozen forever.
        loadGeneration++;
        loading = false;
        inFlightTiles = 0;
        completedTiles = successfulTiles.Count;

        Debug.Log(
            "CAMPAIGN-10H|Provider=" + providerId +
            "|RasterPaused=True|Successful=" + successfulTiles.Count +
            "|Expected=" + expectedTiles +
            "|ResumeOnSelect=True");
    }

    private IEnumerator LoadTiles(int generation)
    {
        string key = ResolveKey();
        if (urlTemplate.Contains("{key}") && string.IsNullOrWhiteSpace(key))
        {
            expectedTiles = 1;
            completedTiles = 1;
            failedTiles = 1;
            loading = false;
            loadComplete = true;
            Debug.LogWarning(
                "CAMPAIGN-10H|Provider=" + providerId +
                "|Loaded=False|Reason=APIKeyMissing|Name=" + providerName);
            yield break;
        }

        int xMin = LonToTileX(MinLon, zoom);
        int xMax = LonToTileX(MaxLon, zoom);
        int yMin = LatToTileY(MaxLat, zoom);
        int yMax = LatToTileY(MinLat, zoom);

        expectedTiles = (xMax - xMin + 1) * (yMax - yMin + 1);
        completedTiles = successfulTiles.Count;

        int priorityX = LonToTileX(PriorityLon, zoom);
        int priorityY = LatToTileY(PriorityLat, zoom);

        List<TileJob> jobs = new List<TileJob>(expectedTiles);
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                string id = TileId(x, y);
                if (successfulTiles.Contains(id))
                    continue;

                float dx = x - priorityX;
                float dy = y - priorityY;
                jobs.Add(new TileJob
                {
                    X = x,
                    Y = y,
                    Priority = dx * dx + dy * dy
                });
            }
        }

        jobs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        Queue<TileJob> queue = new Queue<TileJob>(jobs);

        Debug.Log(
            "CAMPAIGN-10H|Provider=" + providerId +
            "|RasterLoad=True|Expected=" + expectedTiles +
            "|AlreadyReady=" + successfulTiles.Count +
            "|Concurrent=" + MaxConcurrentLoads +
            "|Priority=AalborgLimfjord");

        while ((queue.Count > 0 || inFlightTiles > 0) && generation == loadGeneration)
        {
            while (queue.Count > 0 && inFlightTiles < MaxConcurrentLoads && generation == loadGeneration)
            {
                TileJob job = queue.Dequeue();
                inFlightTiles++;
                StartCoroutine(LoadTileTracked(job.X, job.Y, key, generation));
            }

            yield return null;
        }

        if (generation != loadGeneration)
            yield break;

        loading = false;
        loadComplete = true;
        completedTiles = expectedTiles;

        if (tileRoot != null)
            tileRoot.gameObject.SetActive(true);

        Debug.Log(
            "CAMPAIGN-10H|Provider=" + providerId +
            "|RasterReady=True|Tiles=" + expectedTiles +
            "|Successful=" + successfulTiles.Count +
            "|Failed=" + failedTiles +
            "|Zoom=" + zoom +
            "|AtomicReveal=True|Attribution=" + attribution);
    }

    private IEnumerator LoadTileTracked(int x, int y, string key, int generation)
    {
        bool success = false;
        yield return LoadTile(x, y, key, value => success = value);

        if (generation != loadGeneration)
            yield break;

        if (success)
            successfulTiles.Add(TileId(x, y));
        else
            failedTiles++;

        completedTiles++;
        inFlightTiles = Mathf.Max(0, inFlightTiles - 1);
    }

    private IEnumerator LoadTile(int x, int y, string key, Action<bool> completed)
    {
        string cachePath = CachePath(x, y);
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
                Debug.LogWarning("CAMPAIGN-10H|RasterCacheRead=False|" + ex.Message);
                if (texture != null)
                    Destroy(texture);
                texture = null;
            }
        }

        if (texture == null)
        {
            string url = urlTemplate
                .Replace("{z}", zoom.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{key}", UnityWebRequest.EscapeURL(key ?? string.Empty));

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
            {
                request.timeout = 20;
                try
                {
                    request.SetRequestHeader("User-Agent", "PROJECT1864-UnityMapLab/0.00.10h");
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
                        "CAMPAIGN-10H|Provider=" + providerId +
                        "|Tile=False|Z=" + zoom +
                        "|X=" + x +
                        "|Y=" + y +
                        "|HTTP=" + request.responseCode +
                        "|Error=" + request.error);
                }
            }
        }

        if (texture != null)
        {
            CreateTileObject(x, y, texture);
            completed(true);
        }
        else
        {
            // Keep provider geometry rectangular even if a source tile fails. This
            // is a neutral missing-tile patch, not a fallback to another map source.
            CreateMissingTileObject(x, y);
            completed(false);
        }
    }

    private void CreateTileObject(int x, int y, Texture2D texture)
    {
        GameObject go = CreateTileGeometry(x, y, "Basemap");
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateTextureMaterial(texture, go.name + "_MAT");
    }

    private void CreateMissingTileObject(int x, int y)
    {
        GameObject go = CreateTileGeometry(x, y, "Missing");
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateSolidMaterial(new Color(0.08f, 0.13f, 0.17f), go.name + "_MISSING");
    }

    private GameObject CreateTileGeometry(int x, int y, string kind)
    {
        double west = TileXToLon(x, zoom);
        double east = TileXToLon(x + 1, zoom);
        double north = TileYToLat(y, zoom);
        double south = TileYToLat(y + 1, zoom);

        Vector3 nw = CampaignGeoProjection.Project((float)west, (float)north, 0.06f);
        Vector3 ne = CampaignGeoProjection.Project((float)east, (float)north, 0.06f);
        Vector3 sw = CampaignGeoProjection.Project((float)west, (float)south, 0.06f);
        Vector3 se = CampaignGeoProjection.Project((float)east, (float)south, 0.06f);

        string name = string.Format("{0}_{1:00}_{2}_{3}_{4}", kind, providerId, zoom, x, y);
        Mesh mesh = new Mesh
        {
            name = name + "_Mesh",
            vertices = new[] { sw, se, nw, ne },
            uv = new[]
            {
                new Vector2(0f,0f),
                new Vector2(1f,0f),
                new Vector2(0f,1f),
                new Vector2(1f,1f)
            },
            triangles = new[] { 0, 2, 1, 1, 2, 3 }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        go.transform.SetParent(tileRoot, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        return go;
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
            string path = Path.Combine(Application.persistentDataPath, "PROJECT1864", "Keys", keyFile);
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
                Debug.LogWarning("CAMPAIGN-10H|KeyRead=False|" + ex.Message);
            }
        }

        return string.Empty;
    }

    private string CachePath(int x, int y)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "BasemapCache",
            "provider-" + providerId.ToString("00"),
            zoom.ToString(),
            x.ToString(),
            y + ".img");
    }

    private static bool IsFreshCache(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromDays(MinimumCacheDays);
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
            Debug.LogWarning("CAMPAIGN-10H|RasterCacheWrite=False|" + ex.Message);
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

    private static string TileId(int x, int y)
    {
        return x + ":" + y;
    }

    private static int LonToTileX(double lon, int z)
    {
        double n = Math.Pow(2.0, z);
        return Mathf.Clamp((int)Math.Floor((lon + 180.0) / 360.0 * n), 0, (int)n - 1);
    }

    private static int LatToTileY(double lat, int z)
    {
        lat = Math.Max(-85.05112878, Math.Min(85.05112878, lat));
        double rad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, z);
        return Mathf.Clamp(
            (int)Math.Floor((1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) * 0.5 * n),
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
