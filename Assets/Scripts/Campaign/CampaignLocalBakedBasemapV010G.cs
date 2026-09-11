using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// v00.00.10g local QGIS/Blender baked basemap adapter.
/// Expected files:
/// StreamingAssets/PROJECT1864/Basemaps/QGIS/qgis-denmark.png
/// StreamingAssets/PROJECT1864/Basemaps/QGIS/qgis-denmark.bounds
/// Bounds format: minLon,minLat,maxLon,maxLat
/// </summary>
public sealed class CampaignLocalBakedBasemapV010G : MonoBehaviour
{
    private int providerId;
    private bool configured;
    private bool attempted;
    private string status = "NOT CONFIGURED";

    public string Status { get { return status; } }

    public void Configure(int id)
    {
        providerId = id;
        configured = true;
        status = "READY / LOCAL ASSET CHECK";
    }

    public void EnsureLoaded()
    {
        if (!configured || attempted)
            return;

        attempted = true;

        string folder = Path.Combine(
            Application.streamingAssetsPath,
            "PROJECT1864",
            "Basemaps",
            "QGIS");

        string imagePath = Path.Combine(folder, "qgis-denmark.png");
        string boundsPath = Path.Combine(folder, "qgis-denmark.bounds");

        if (!File.Exists(imagePath) || !File.Exists(boundsPath))
        {
            status = "QGIS BAKED ASSET MISSING";
            Debug.LogWarning(
                "CAMPAIGN-10G|Provider=" + providerId +
                "|LocalBaked=False|Expected=" + imagePath);
            return;
        }

        try
        {
            double minLon;
            double minLat;
            double maxLon;
            double maxLat;
            if (!TryReadBounds(
                File.ReadAllText(boundsPath),
                out minLon,
                out minLat,
                out maxLon,
                out maxLat))
            {
                status = "QGIS BOUNDS INVALID";
                return;
            }

            byte[] bytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                Destroy(texture);
                status = "QGIS IMAGE INVALID";
                return;
            }

            CreateQuad(texture, minLon, minLat, maxLon, maxLat);
            status = "READY";

            Debug.Log(
                "CAMPAIGN-10G|Provider=" + providerId +
                "|LocalBaked=True|Path=" + imagePath);
        }
        catch (Exception ex)
        {
            status = "QGIS LOAD FAILED";
            Debug.LogError(
                "CAMPAIGN-10G|Provider=" + providerId +
                "|LocalBaked=False|Error=" + ex.Message);
        }
    }

    private void CreateQuad(
        Texture2D texture,
        double minLon,
        double minLat,
        double maxLon,
        double maxLat)
    {
        Vector3 sw = CampaignGeoProjection.Project((float)minLon, (float)minLat, 0.065f);
        Vector3 se = CampaignGeoProjection.Project((float)maxLon, (float)minLat, 0.065f);
        Vector3 nw = CampaignGeoProjection.Project((float)minLon, (float)maxLat, 0.065f);
        Vector3 ne = CampaignGeoProjection.Project((float)maxLon, (float)maxLat, 0.065f);

        Mesh mesh = new Mesh
        {
            name = "QGIS_BAKED_MESH",
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

        GameObject go = new GameObject("QGIS_BAKED_DENMARK");
        go.transform.SetParent(transform, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "QGIS_BAKED_MAT",
            mainTexture = texture
        };

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        renderer.sharedMaterial = material;
    }

    private static bool TryReadBounds(
        string text,
        out double minLon,
        out double minLat,
        out double maxLon,
        out double maxLat)
    {
        minLon = minLat = maxLon = maxLat = 0.0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] parts = text.Trim()
            .Replace(";", ",")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
            return false;

        return
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minLon) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out minLat) &&
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out maxLon) &&
            double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out maxLat) &&
            minLon < maxLon &&
            minLat < maxLat;
    }
}
