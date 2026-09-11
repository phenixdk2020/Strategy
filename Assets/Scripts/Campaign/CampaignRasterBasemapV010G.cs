using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// v00.00.10g generic XYZ/Web-Mercator raster basemap.
/// Loads only the Denmark overview tiles needed by the active provider and
/// keeps a persistent cache. No colliders are created so campaign selection
/// remains owned by gameplay markers.
/// </summary>
public sealed class CampaignRasterBasemapV010G : MonoBehaviour
{
    private const double MinLat = 54.45;
    private const double MaxLat = 57.85;
    private const double MinLon = 7.55;
    private const double MaxLon = 15.35;
    private const int MinimumCacheDays = 7;

    private int providerId;
    private int zoom;
    private string providerName;
    private string urlTemplate;
    private string keyEnvironment;
    private string keyFile;
    private string attribution;

    private Transform tileRoot;
    private bool configured;
    private bool loadStarted;
    private int expectedTiles;
    private int completedTiles;
    private int failedTiles;

    public string Status
    {
        get
        {
            if (!configured) return "NOT CONFIGURED";
            if (!loadStarted) return "READY TO LOAD";
            if (completedTiles < expectedTiles)
                return string.Format("LOADING {0}/{1}", completedTiles, expectedTiles);
            if (failedTiles > 0)
                return string.Format("READY WITH {0} FAILED", failedTiles);
            return "READY";
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

        tileRoot = new GameObject("XYZ_TILES").transform;
        tileRoot.SetParent(transform, false);
    }

    public void EnsureLoaded()
    {
        if (!configured || loadStarted)
            return;

        loadStarted = true;
        StartCoroutine(LoadTiles());
    }

    private IEnumerator LoadTiles()
    {
        string key = ResolveKey();
        if (urlTemplate.Contains("{key}") && string.IsNullOrWhiteSpace(key))
        {
            expectedTiles = 1;
            completedTiles = 1;
            failedTiles = 1;
            Debug.LogWarning(
                "CAMPAIGN-10G|Provider=" + providerId +
                "|Loaded=False|Reason=APIKeyMissing|Name=" + providerName);
            yield break;
        }

        int xMin = LonToTileX(MinLon, zoom);
        int xMax = LonToTileX(MaxLon, zoom);
        int yMin = LatToTileY(MaxLat, zoom);
        int yMax = LatToTileY(MinLat, zoom);

        expectedTiles = (xMax - xMin + 1) * (yMax - yMin + 1);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                yield return LoadTile(x, y, key);
                completedTiles++;

                // Avoid bursts against public map services.
                if ((completedTiles & 3) == 0)
                    yield return null;
            }
        }

        Debug.Log(
            "CAMPAIGN-10G|Provider=" + providerId +
            "|Raster=True|Tiles=" + completedTiles +
            "|Failed=" + failedTiles +
            "|Zoom=" + zoom +
            "|Attribution=" + attribution);
    }

    private IEnumerator LoadTile(int x, int y, string key)
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
                Debug.LogWarning("CAMPAIGN-10G|RasterCacheRead=False|" + ex.Message);
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
                    request.SetRequestHeader(
                        "User-Agent",
                        "PROJECT1864-UnityMapLab/0.00.10g");
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
                    failedTiles++;
                    Debug.LogWarning(
                        "CAMPAIGN-10G|Provider=" + providerId +
                        "|Tile=False|Z=" + zoom +
                        "|X=" + x +
                        "|Y=" + y +
                        "|HTTP=" + request.responseCode +
                        "|Error=" + request.error);
                }
            }
        }

        if (texture != null)
            CreateTileObject(x, y, texture);
    }

    private void CreateTileObject(int x, int y, Texture2D texture)
    {
        double west = TileXToLon(x, zoom);
        double east = TileXToLon(x + 1, zoom);
        double north = TileYToLat(y, zoom);
        double south = TileYToLat(y + 1, zoom);

        Vector3 nw = CampaignGeoProjection.Project((float)west, (float)north, 0.06f);
        Vector3 ne = CampaignGeoProjection.Project((float)east, (float)north, 0.06f);
        Vector3 sw = CampaignGeoProjection.Project((float)west, (float)south, 0.06f);
        Vector3 se = CampaignGeoProjection.Project((float)east, (float)south, 0.06f);

        Mesh mesh = new Mesh
        {
            name = string.Format("Basemap_{0:00}_{1}_{2}_{3}", providerId, zoom, x, y),
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

        GameObject go = new GameObject(mesh.name);
        go.transform.SetParent(tileRoot, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        Material material = CreateTextureMaterial(texture, mesh.name + "_MAT");
        renderer.sharedMaterial = material;
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
                Debug.LogWarning("CAMPAIGN-10G|KeyRead=False|" + ex.Message);
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
            DateTime modified = File.GetLastWriteTimeUtc(path);
            return DateTime.UtcNow - modified < TimeSpan.FromDays(MinimumCacheDays);
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
            Debug.LogWarning("CAMPAIGN-10G|RasterCacheWrite=False|" + ex.Message);
        }
    }

    private static Material CreateTextureMaterial(Texture texture, string name)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        return material;
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
