using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// v00.00.13a presentation pass.
// Simulation state, strategic distance and movement rules are deliberately untouched.
[DefaultExecutionOrder(900)]
public sealed class CampaignVisualPolishV013A : MonoBehaviour
{
    private const string DenmarkRootName = "GEO_Denmark_NaturalEarth50m_V013A";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignVisualPolishV013A>() != null)
            return;

        GameObject root = new GameObject("CampaignVisualPolishV013A");
        root.AddComponent<CampaignVisualPolishV013A>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        PolishWorldMaterials();
        PolishLighting();
        BuildDetailedDenmarkSurface();
        PolishStrategicLines();

        Debug.Log("CAMPAIGN-V013A|VisualPolish=True|DenmarkNaturalEarth=True|LegacyOverlayConflict=False|DefaultOverlays=False");
    }

    private static void PolishWorldMaterials()
    {
        GameObject terrain = GameObject.Find("CampaignTerrainSurface_v013");
        if (terrain != null)
        {
            Renderer renderer = terrain.GetComponent<Renderer>();
            TintRenderer(renderer, new Color(0.255f, 0.335f, 0.205f));
        }

        GameObject sea = GameObject.Find("Campaign Sea Base");
        if (sea != null)
        {
            Renderer renderer = sea.GetComponent<Renderer>();
            TintRenderer(renderer, new Color(0.115f, 0.235f, 0.315f));
        }

        RenderSettings.ambientLight = new Color(0.50f, 0.53f, 0.49f);
    }

    private static void PolishLighting()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>();
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.62f;
            light.shadowBias = 0.06f;
            light.shadowNormalBias = 0.35f;
        }

        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 260f);
    }

    private static void BuildDetailedDenmarkSurface()
    {
        if (GameObject.Find(DenmarkRootName) != null)
            return;

        Material land = CreateMaterial(
            new Color(0.355f, 0.455f, 0.255f),
            "V013A_DenmarkLand");
        Material coast = CreateMaterial(
            new Color(0.79f, 0.76f, 0.61f),
            "V013A_DenmarkCoast");

        GameObject root = CampaignDenmarkGeography.Create(land, coast);
        if (root == null)
            return;

        root.name = DenmarkRootName;
        root.transform.SetParent(
            CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootTerrain).transform,
            true);

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.mesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[i]);
                world.y = CampaignTerrainV013.SampleSurfaceY(world.x, world.z) + 0.26f;
                vertices[i] = filter.transform.InverseTransformPoint(world);
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        LineRenderer[] coasts = root.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in coasts)
        {
            if (line == null)
                continue;

            line.widthMultiplier = 0.22f;
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                p.y = CampaignTerrainV013.SampleSurfaceY(p.x, p.z) + 0.50f;
                line.SetPosition(i, p);
            }
        }
    }

    private static void PolishStrategicLines()
    {
        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>();
        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            if (line.name.StartsWith("StrategicLink_", StringComparison.Ordinal))
            {
                line.widthMultiplier = 0.46f;
            }
            else if (line.name.StartsWith("Outline_", StringComparison.Ordinal))
            {
                // Political/geographic outlines live on L8 and are OFF by default in v13a.
                // If enabled they remain thin enough not to dominate roads/terrain.
                line.widthMultiplier = 0.24f;
            }
            else if (line.name == "CampaignRouteGhost")
            {
                line.widthMultiplier = 0.85f;
            }
        }
    }

    private static void TintRenderer(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;
        if (material != null)
            material.color = color;
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.12f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.12f);

        return material;
    }
}
