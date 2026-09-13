using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10k
/// Adds streamed Terrarium elevation to the active World Imagery tile meshes.
///
/// Design goals:
/// - imagery remains the visible surface/material;
/// - elevation follows the same z/x/y tile identity as the active imagery;
/// - no terrain colliders are created, preserving campaign selection/order raycasts;
/// - pending hidden imagery generations can receive elevation before atomic swap;
/// - gameplay markers are lifted to sampled terrain height after GrandCampaignBootstrap;
/// - world overview stays effectively 2D while regional/country zoom can use a tilted 3D camera.
/// </summary>
[DefaultExecutionOrder(28000)]
public sealed class CampaignImageryTerrainV010K : MonoBehaviour
{
    private sealed class TerrainTileRecord
    {
        public GameObject Tile;
        public MeshFilter Filter;
        public int Z;
        public int X;
        public int Y;
        public int Grid;
        public float[] Meters;
        public double West;
        public double East;
        public double South;
        public double North;
    }

    public static CampaignImageryTerrainV010K Instance { get; private set; }

    private const string ImageryRootName = "BASEMAP_09_9_Imagery";
    private const string ElevationUrl =
        "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png";

    private const int MinimumTerrainZoom = 5;
    private const int MaxConcurrentElevationLoads = 6;
    private const float ScanInterval = 0.18f;
    private const float MarkerRefreshInterval = 0.15f;
    private const float TerrainBaseY = 0.06f;
    private const float TerrainExaggeration = 18f;
    private const float FlatCameraThreshold = 430f;
    private const float CameraHeight = 95f;

    private readonly Dictionary<int, TerrainTileRecord> records =
        new Dictionary<int, TerrainTileRecord>();
    private readonly HashSet<int> requested = new HashSet<int>();

    private GameObject imageryRoot;
    private Camera worldCamera;
    private int inFlight;
    private int successful;
    private int failed;
    private float nextScan;
    private float nextMarkerRefresh;
    private bool terrainEnabled = true;
    private float yawDegrees;

    public bool TerrainEnabled { get { return terrainEnabled; } }
    public float Exaggeration { get { return TerrainExaggeration; } }

    public bool Effective3D
    {
        get
        {
            return terrainEnabled &&
                   worldCamera != null &&
                   worldCamera.orthographicSize < FlatCameraThreshold;
        }
    }

    public string Status
    {
        get
        {
            string mode = terrainEnabled ? (Effective3D ? "3D" : "3D/AUTO-FLAT") : "2D";
            return string.Format(
                "TERRAIN {0} · READY {1} · LOAD {2} · FAIL {3} · x{4:0} height",
                mode,
                CountLiveRecords(),
                inFlight,
                failed,
                TerrainExaggeration);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignImageryTerrainV010K>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ImageryTerrain_v000010k");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignImageryTerrainV010K>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        ResolveDependencies();

        if (Input.GetKeyDown(KeyCode.T))
        {
            terrainEnabled = !terrainEnabled;
            ApplyTerrainStateToAll();
            Debug.Log("CAMPAIGN-10K|Terrain3D=" + terrainEnabled + "|Toggle=T");
        }

        if (terrainEnabled)
        {
            float rotate = 0f;
            if (Input.GetKey(KeyCode.Q)) rotate -= 38f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.E)) rotate += 38f * Time.unscaledDeltaTime;
            if (Mathf.Abs(rotate) > 0.0001f)
                yawDegrees = Mathf.Repeat(yawDegrees + rotate, 360f);
        }

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanInterval;
            PruneDestroyedRecords();
            ScanForImageryTiles();
        }
    }

    private void LateUpdate()
    {
        ResolveDependencies();
        Apply3DCameraAfterWorldStreamer();

        if (Time.unscaledTime >= nextMarkerRefresh)
        {
            nextMarkerRefresh = Time.unscaledTime + MarkerRefreshInterval;
            UpdateGameplayMarkerHeights();
        }
    }

    private void ResolveDependencies()
    {
        if (imageryRoot == null)
            imageryRoot = GameObject.Find(ImageryRootName);
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void ScanForImageryTiles()
    {
        if (imageryRoot == null)
            return;

        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
        for (int i = 0; i < filters.Length && inFlight < MaxConcurrentElevationLoads; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.gameObject == null)
                continue;
            if (!filter.transform.IsChildOf(imageryRoot.transform))
                continue;

            int z;
            int x;
            int y;
            if (!TryParseImageryTile(filter.gameObject.name, out z, out x, out y))
                continue;

            if (z < MinimumTerrainZoom)
                continue;

            int id = filter.gameObject.GetInstanceID();
            if (requested.Contains(id))
                continue;

            requested.Add(id);
            inFlight++;
            StartCoroutine(LoadElevationForTile(filter.gameObject, filter, z, x, y, id));
        }
    }

    private IEnumerator LoadElevationForTile(
        GameObject tile,
        MeshFilter filter,
        int z,
        int x,
        int y,
        int instanceId)
    {
        Texture2D elevation = null;
        yield return LoadElevationTexture(z, x, y, value => elevation = value);

        inFlight = Mathf.Max(0, inFlight - 1);

        if (tile == null || filter == null)
        {
            if (elevation != null)
                Destroy(elevation);
            yield break;
        }

        if (elevation == null)
        {
            failed++;
            Debug.LogWarning(
                "CAMPAIGN-10K|TerrainTile=False|Z=" + z + "|X=" + x + "|Y=" + y +
                "|Fallback=FlatImagery");
            yield break;
        }

        TerrainTileRecord record = BuildTerrainRecord(tile, filter, z, x, y, elevation);
        Destroy(elevation);

        if (record != null)
        {
            records[instanceId] = record;
            successful++;
        }
    }

    private TerrainTileRecord BuildTerrainRecord(
        GameObject tile,
        MeshFilter filter,
        int z,
        int x,
        int y,
        Texture2D elevation)
    {
        if (tile == null || filter == null || elevation == null)
            return null;

        int grid = GridForZoom(z);
        int size = grid + 1;
        float[] meters = new float[size * size];
        Color32[] pixels = elevation.GetPixels32();
        int pw = elevation.width;
        int ph = elevation.height;

        double west = TileXToLon(x, z);
        double east = TileXToLon(x + 1, z);
        double north = TileYToLat(y, z);
        double south = TileYToLat(y + 1, z);

        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)grid;
            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)grid;
                int px = Mathf.Clamp(Mathf.RoundToInt(u * (pw - 1)), 0, pw - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(v * (ph - 1)), 0, ph - 1);
                Color32 c = pixels[py * pw + px];
                float value = c.r * 256f + c.g + c.b / 256f - 32768f;
                meters[gy * size + gx] = Mathf.Clamp(value, 0f, 9000f);
            }
        }

        TerrainTileRecord record = new TerrainTileRecord
        {
            Tile = tile,
            Filter = filter,
            Z = z,
            X = x,
            Y = y,
            Grid = grid,
            Meters = meters,
            West = west,
            East = east,
            South = south,
            North = north
        };

        RebuildTileMesh(record, terrainEnabled);
        return record;
    }

    private void RebuildTileMesh(TerrainTileRecord record, bool withTerrain)
    {
        if (record == null || record.Tile == null || record.Filter == null || record.Meters == null)
            return;

        int grid = record.Grid;
        int size = grid + 1;
        Vector3[] vertices = new Vector3[size * size];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[grid * grid * 6];

        int vertexIndex = 0;
        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)grid;
            double lat = record.South + (record.North - record.South) * v;

            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)grid;
                double lon = record.West + (record.East - record.West) * u;
                float y = TerrainBaseY;
                if (withTerrain)
                    y += MetersToWorld(record.Meters[vertexIndex]);

                vertices[vertexIndex] = CampaignGeoProjection.Project((float)lon, (float)lat, y);
                uv[vertexIndex] = new Vector2(u, v);
                vertexIndex++;
            }
        }

        int ti = 0;
        for (int gy = 0; gy < grid; gy++)
        {
            for (int gx = 0; gx < grid; gx++)
            {
                int i0 = gy * size + gx;
                int i1 = i0 + 1;
                int i2 = i0 + size;
                int i3 = i2 + 1;

                triangles[ti++] = i0;
                triangles[ti++] = i2;
                triangles[ti++] = i1;
                triangles[ti++] = i1;
                triangles[ti++] = i2;
                triangles[ti++] = i3;
            }
        }

        Mesh previous = record.Filter.sharedMesh;
        Mesh mesh = new Mesh
        {
            name = string.Format("ImageryTerrain_09_{0}_{1}_{2}", record.Z, record.X, record.Y),
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        record.Filter.sharedMesh = mesh;

        if (previous != null)
            Destroy(previous);
    }

    private void ApplyTerrainStateToAll()
    {
        foreach (TerrainTileRecord record in records.Values)
        {
            if (record != null && record.Tile != null)
                RebuildTileMesh(record, terrainEnabled);
        }
    }

    private void Apply3DCameraAfterWorldStreamer()
    {
        if (worldCamera == null || !terrainEnabled)
            return;

        float ortho = worldCamera.orthographicSize;
        if (ortho >= FlatCameraThreshold)
            return;

        // CampaignRasterBasemapV010G has already restored the authoritative top-down
        // map center this frame. Read that x/z center, then move the camera backwards
        // and tilt it without changing the streaming center/LOD contract.
        Vector3 center = new Vector3(
            worldCamera.transform.position.x,
            0f,
            worldCamera.transform.position.z);

        float pitch = ortho <= 65f ? 58f : ortho <= 190f ? 68f : 78f;
        float distance = CameraHeight / Mathf.Max(0.15f, Mathf.Tan(pitch * Mathf.Deg2Rad));
        Vector3 back = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.back;

        worldCamera.transform.position = center + Vector3.up * CameraHeight + back * distance;
        worldCamera.transform.LookAt(center, Vector3.up);
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 1200f;
    }

    private void UpdateGameplayMarkerHeights()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject == null)
                continue;

            string name = renderer.gameObject.name;
            float baseOffset;
            float flatY;

            if (name.StartsWith("ZONE_", StringComparison.Ordinal))
            {
                baseOffset = 0.36f;
                flatY = 0.42f;
            }
            else if (name.StartsWith("CITY_", StringComparison.Ordinal))
            {
                baseOffset = 0.42f;
                flatY = 0.48f;
            }
            else if (name.StartsWith("ARMY_", StringComparison.Ordinal))
            {
                baseOffset = 1.41f;
                flatY = 1.47f;
            }
            else
            {
                continue;
            }

            Vector3 p = renderer.transform.position;
            if (!terrainEnabled)
            {
                p.y = flatY;
                renderer.transform.position = p;
                continue;
            }

            Vector2 lonLat = CampaignGeoProjection.Unproject(p);
            float ground;
            if (TrySampleActiveTerrain(lonLat.x, lonLat.y, out ground))
                p.y = ground + baseOffset;
            else
                p.y = flatY;

            renderer.transform.position = p;
        }
    }

    private bool TrySampleActiveTerrain(float lon, float lat, out float worldY)
    {
        TerrainTileRecord best = null;

        foreach (TerrainTileRecord record in records.Values)
        {
            if (record == null || record.Tile == null || !record.Tile.activeInHierarchy || record.Meters == null)
                continue;
            if (lon < record.West || lon > record.East || lat < record.South || lat > record.North)
                continue;

            if (best == null || record.Z > best.Z)
                best = record;
        }

        if (best == null)
        {
            worldY = TerrainBaseY;
            return false;
        }

        float u = Mathf.Clamp01((float)((lon - best.West) / (best.East - best.West)));
        float v = Mathf.Clamp01((float)((lat - best.South) / (best.North - best.South)));
        float meters = BilinearGrid(best, u, v);
        worldY = TerrainBaseY + MetersToWorld(meters);
        return true;
    }

    private static float BilinearGrid(TerrainTileRecord record, float u, float v)
    {
        int grid = record.Grid;
        int size = grid + 1;
        float fx = u * grid;
        float fy = v * grid;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, grid);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, grid);
        int x1 = Mathf.Min(grid, x0 + 1);
        int y1 = Mathf.Min(grid, y0 + 1);
        float tx = fx - x0;
        float ty = fy - y0;

        float h00 = record.Meters[y0 * size + x0];
        float h10 = record.Meters[y0 * size + x1];
        float h01 = record.Meters[y1 * size + x0];
        float h11 = record.Meters[y1 * size + x1];
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), ty);
    }

    private void PruneDestroyedRecords()
    {
        if (records.Count == 0)
            return;

        List<int> remove = null;
        foreach (KeyValuePair<int, TerrainTileRecord> pair in records)
        {
            if (pair.Value != null && pair.Value.Tile != null)
                continue;
            if (remove == null)
                remove = new List<int>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
        {
            records.Remove(remove[i]);
            requested.Remove(remove[i]);
        }
    }

    private int CountLiveRecords()
    {
        int count = 0;
        foreach (TerrainTileRecord record in records.Values)
        {
            if (record != null && record.Tile != null)
                count++;
        }
        return count;
    }

    private IEnumerator LoadElevationTexture(int z, int x, int y, Action<Texture2D> completed)
    {
        string cachePath = ElevationCachePath(z, x, y);
        Texture2D texture = null;

        if (File.Exists(cachePath))
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
                Debug.LogWarning("CAMPAIGN-10K|ElevationCacheRead=False|" + ex.Message);
                if (texture != null)
                    Destroy(texture);
                texture = null;
            }
        }

        if (texture == null)
        {
            string url = string.Format(ElevationUrl, z, x, y);
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
            {
                request.timeout = 20;
                try
                {
                    request.SetRequestHeader("User-Agent", "PROJECT1864-UnityTerrain/0.00.10k");
                }
                catch
                {
                }

                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    texture = DownloadHandlerTexture.GetContent(request);
                    TryWriteElevationCache(cachePath, request.downloadHandler.data);
                }
            }
        }

        completed(texture);
    }

    private static string ElevationCachePath(int z, int x, int y)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "TerrainCache",
            "terrarium",
            z.ToString(),
            x.ToString(),
            y + ".png");
    }

    private static void TryWriteElevationCache(string path, byte[] data)
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
            Debug.LogWarning("CAMPAIGN-10K|ElevationCacheWrite=False|" + ex.Message);
        }
    }

    private static bool TryParseImageryTile(string name, out int z, out int x, out int y)
    {
        z = x = y = 0;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("Basemap_09_", StringComparison.Ordinal))
            return false;

        string[] parts = name.Split('_');
        return parts.Length == 5 &&
               int.TryParse(parts[2], out z) &&
               int.TryParse(parts[3], out x) &&
               int.TryParse(parts[4], out y);
    }

    private static int GridForZoom(int z)
    {
        if (z <= 5) return 6;
        if (z <= 7) return 10;
        if (z <= 9) return 16;
        if (z <= 10) return 20;
        return 24;
    }

    private static float MetersToWorld(float meters)
    {
        const float metersPerLatitudeDegree = 111320f;
        return meters *
               (CampaignGeoProjection.UnitsPerLatitudeDegree / metersPerLatitudeDegree) *
               TerrainExaggeration;
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
