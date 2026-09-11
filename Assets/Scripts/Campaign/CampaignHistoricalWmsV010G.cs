using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// v00.00.10g Datafordeler historical high-table-sheet WMS provider.
/// The service requires an API key. No modern-map fallback is used.
/// </summary>
public sealed class CampaignHistoricalWmsV010G : MonoBehaviour
{
    private const double MinLat = 54.45;
    private const double MaxLat = 57.85;
    private const double MinLon = 7.55;
    private const double MaxLon = 15.35;

    private int providerId;
    private bool configured;
    private bool loadStarted;
    private int completed;
    private int failed;
    private const int TileColumns = 3;
    private const int TileRows = 2;

    public string Status
    {
        get
        {
            if (!configured) return "NOT CONFIGURED";
            if (!loadStarted) return "READY / KEY CHECK";
            int total = TileColumns * TileRows;
            if (completed < total)
                return string.Format("LOADING HISTORICAL {0}/{1}", completed, total);
            if (failed == total)
                return "DATAFORDELER KEY/DATA REQUIRED";
            return failed > 0 ? "READY / PARTIAL COVERAGE" : "READY";
        }
    }

    public void Configure(int id)
    {
        providerId = id;
        configured = true;
    }

    public void EnsureLoaded()
    {
        if (!configured || loadStarted)
            return;

        loadStarted = true;
        StartCoroutine(LoadHistorical());
    }

    private IEnumerator LoadHistorical()
    {
        string key = ResolveDatafordelerKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            completed = TileColumns * TileRows;
            failed = completed;
            Debug.LogWarning(
                "CAMPAIGN-10G|Provider=05|Historical=False|" +
                "Reason=DATAFORDELER_API_KEY_Missing");
            yield break;
        }

        for (int row = 0; row < TileRows; row++)
        {
            double south = MinLat + (MaxLat - MinLat) * row / TileRows;
            double north = MinLat + (MaxLat - MinLat) * (row + 1) / TileRows;

            for (int col = 0; col < TileColumns; col++)
            {
                double west = MinLon + (MaxLon - MinLon) * col / TileColumns;
                double east = MinLon + (MaxLon - MinLon) * (col + 1) / TileColumns;

                yield return LoadWmsTile(key, west, south, east, north, col, row);
                completed++;
                yield return null;
            }
        }

        Debug.Log(
            "CAMPAIGN-10G|Provider=05|Historical=True|" +
            "Service=HoejeMaalebordsbladeWMS|Tiles=" + completed +
            "|Failed=" + failed);
    }

    private IEnumerator LoadWmsTile(
        string key,
        double west,
        double south,
        double east,
        double north,
        int col,
        int row)
    {
        Vector2d a = Wgs84ToUtm32(south, west);
        Vector2d b = Wgs84ToUtm32(south, east);
        Vector2d c = Wgs84ToUtm32(north, west);
        Vector2d d = Wgs84ToUtm32(north, east);

        double minE = Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X));
        double maxE = Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X));
        double minN = Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
        double maxN = Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));

        string bbox = string.Join(
            ",",
            F(minE),
            F(minN),
            F(maxE),
            F(maxN));

        string url =
            "https://wms.datafordeler.dk/HoejeMaalebordsblade/" +
            "topo20_hoeje_maalebordsblade/1.0.0/wms" +
            "?apikey=" + UnityWebRequest.EscapeURL(key) +
            "&SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap" +
            "&BBOX=" + bbox +
            "&CRS=EPSG:25832" +
            "&WIDTH=1024&HEIGHT=1024" +
            "&STYLES=&FORMAT=image/jpeg" +
            "&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96" +
            "&Layers=dtk_hoeje_maalebordsblade";

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
        {
            request.timeout = 25;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                failed++;
                Debug.LogWarning(
                    "CAMPAIGN-10G|Provider=05|HistoricalTile=False|" +
                    "HTTP=" + request.responseCode +
                    "|Error=" + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                failed++;
                yield break;
            }

            CreateQuad(
                texture,
                west,
                south,
                east,
                north,
                "Historical_" + col + "_" + row);
        }
    }

    private void CreateQuad(
        Texture2D texture,
        double west,
        double south,
        double east,
        double north,
        string name)
    {
        Vector3 sw = CampaignGeoProjection.Project((float)west, (float)south, 0.065f);
        Vector3 se = CampaignGeoProjection.Project((float)east, (float)south, 0.065f);
        Vector3 nw = CampaignGeoProjection.Project((float)west, (float)north, 0.065f);
        Vector3 ne = CampaignGeoProjection.Project((float)east, (float)north, 0.065f);

        Mesh mesh = new Mesh
        {
            name = name + "_MESH",
            vertices = new[] { sw, se, nw, ne },
            uv = new[]
            {
                new Vector2(0f,0f),
                new Vector2(1f,0f),
                new Vector2(0f,1f),
                new Vector2(1f,1f)
            },
            triangles = new[] { 0,2,1, 1,2,3 }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = TextureMaterial(texture, name + "_MAT");
    }

    private static string ResolveDatafordelerKey()
    {
        string env = Environment.GetEnvironmentVariable("DATAFORDELER_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        string path = Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864",
            "Keys",
            "datafordeler.txt");

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
            Debug.LogWarning("CAMPAIGN-10G|DatafordelerKeyRead=False|" + ex.Message);
        }

        return string.Empty;
    }

    private static Material TextureMaterial(Texture texture, string name)
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

    private struct Vector2d
    {
        public double X;
        public double Y;

        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    // WGS84 -> ETRS89/UTM32 approximation. At Denmark scale, WGS84 and ETRS89
    // are sufficiently close for the WMS comparison layer.
    private static Vector2d Wgs84ToUtm32(double latitude, double longitude)
    {
        const double a = 6378137.0;
        const double f = 1.0 / 298.257223563;
        const double k0 = 0.9996;
        const double lon0 = 9.0 * Math.PI / 180.0;

        double e2 = f * (2.0 - f);
        double ep2 = e2 / (1.0 - e2);
        double lat = latitude * Math.PI / 180.0;
        double lon = longitude * Math.PI / 180.0;

        double sin = Math.Sin(lat);
        double cos = Math.Cos(lat);
        double tan = Math.Tan(lat);

        double n = a / Math.Sqrt(1.0 - e2 * sin * sin);
        double t = tan * tan;
        double c = ep2 * cos * cos;
        double aa = cos * (lon - lon0);

        double m =
            a *
            ((1.0 - e2 / 4.0 - 3.0 * e2 * e2 / 64.0 - 5.0 * e2 * e2 * e2 / 256.0) * lat
             - (3.0 * e2 / 8.0 + 3.0 * e2 * e2 / 32.0 + 45.0 * e2 * e2 * e2 / 1024.0) * Math.Sin(2.0 * lat)
             + (15.0 * e2 * e2 / 256.0 + 45.0 * e2 * e2 * e2 / 1024.0) * Math.Sin(4.0 * lat)
             - (35.0 * e2 * e2 * e2 / 3072.0) * Math.Sin(6.0 * lat));

        double easting =
            k0 * n *
            (aa +
             (1.0 - t + c) * Math.Pow(aa, 3.0) / 6.0 +
             (5.0 - 18.0 * t + t * t + 72.0 * c - 58.0 * ep2) * Math.Pow(aa, 5.0) / 120.0)
            + 500000.0;

        double northing =
            k0 *
            (m +
             n * tan *
             (aa * aa / 2.0 +
              (5.0 - t + 9.0 * c + 4.0 * c * c) * Math.Pow(aa, 4.0) / 24.0 +
              (61.0 - 58.0 * t + t * t + 600.0 * c - 330.0 * ep2) * Math.Pow(aa, 6.0) / 720.0));

        return new Vector2d(easting, northing);
    }

    private static string F(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
