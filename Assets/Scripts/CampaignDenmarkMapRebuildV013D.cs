using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13d — Denmark map rebuild.
//
// Purpose:
// - build Denmark ONCE in the same broad lat/lon projection used by campaign nodes,
// - stop remapping/deforming the old v13a Denmark mesh,
// - keep the coarse global v13 terrain hidden during Denmark QA,
// - remove v13b land-cover artifacts while the geographic foundation is validated,
// - show a clean Denmark-first presentation without changing strategic simulation state.
[DefaultExecutionOrder(2400)]
public sealed class CampaignDenmarkMapRebuildV013D : MonoBehaviour
{
    public const string BuildTag = "v00.00.13d";
    public const string DenmarkRootName = "GEO_Denmark_V013D_BROAD_DIRECT";

    private const string LegacyRootA = "GEO_Denmark_NaturalEarth50m_V013A";
    private const string LegacyRootC = "GEO_Denmark_NaturalEarth50m_V013C_BROAD";
    private const string LegacyRoot = "GEO_Denmark_NaturalEarth50m";
    private const string GlobalTerrainName = "CampaignTerrainSurface_v013";

    private static readonly HashSet<string> SouthernContextNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "FLENSBURG",
        "SCHLESWIG"
    };

    private Material landMaterial;
    private Material coastMaterial;
    private GUIStyle labelStyle;
    private GUIStyle hiddenNodeLabelStyle;
    private FieldInfo controllerNodeLabelStyleField;
    private CampaignMapController mapController;
    private bool built;

    public static bool DenmarkFocusActive
    {
        get
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!DenmarkFocusActive)
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkMapRebuildV013D>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkMapRebuildV013D");
        root.AddComponent<CampaignDenmarkMapRebuildV013D>();
    }

    private void Awake()
    {
        // v13d supersedes the v13c repair component. Disable it BEFORE its Start()
        // so the old remap path cannot touch the Denmark geometry again.
        CampaignDenmarkCleanupV013C oldCleanup = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkCleanupV013C>();
        if (oldCleanup != null)
            oldCleanup.enabled = false;

        DisableHelperByTypeName("CampaignMapUsabilityV011");
        DisableHelperByTypeName("CampaignMapSearchHoverV011");
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();

        mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        controllerNodeLabelStyleField = typeof(CampaignMapController).GetField(
            "nodeLabelStyle",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BuildMaterials();
        HideOldPresentationArtifacts();
        built = BuildDirectBroadDenmarkSurface();
        GroundAndScaleDenmarkContext();
        GroundConstructionProjects();
        RedrapeDenmarkInfrastructure();
        HideNonDenmarkRenderers();
        TuneSeaAndLighting();

        Debug.Log(string.Format(
            "CAMPAIGN-V013D|DenmarkMapRebuild=True|DirectBroadProjection={0}|LegacyRemap=False|GlobalTerrain=False|LegacyLandCover=False|DenmarkLabelsOnly=True|SimulationChanged=False",
            built));
    }

    private void LateUpdate()
    {
        // CampaignTerrainV013 still exists as a compatibility layer and runs its own
        // LateUpdate. v13d deliberately wins the presentation grounding afterwards.
        GroundAndScaleDenmarkContext();
        GroundConstructionProjects();
        RedrapeDenmarkInfrastructure();
        HideNonDenmarkRenderers();
        SuppressLegacyNodeLabels();
    }

    private void BuildMaterials()
    {
        landMaterial = CreateMaterial(new Color(0.30f, 0.405f, 0.235f), "V013D_DenmarkLand");
        coastMaterial = CreateMaterial(new Color(0.74f, 0.72f, 0.57f), "V013D_DenmarkCoast");

        SetSmoothness(landMaterial, 0.06f);
        SetSmoothness(coastMaterial, 0.04f);
    }

    private static void HideOldPresentationArtifacts()
    {
        HideAndDestroy(LegacyRootA);
        HideAndDestroy(LegacyRootC);
        HideAndDestroy(LegacyRoot);

        GameObject terrain = GameObject.Find(GlobalTerrainName);
        if (terrain != null)
        {
            Renderer renderer = terrain.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        // These v13b objects were generated before the projection problem was isolated.
        // Hide them for the clean geography QA pass instead of trying to pull/stretch them.
        SetRootActive("V013B_DenmarkLandCover", false);
        SetRootActive("V013B_Vegetation", false);

        GameObject overlays = GameObject.Find(CampaignTerrainV013.RootOverlays);
        if (overlays != null)
            overlays.SetActive(false);
    }

    private bool BuildDirectBroadDenmarkSurface()
    {
        GameObject existing = GameObject.Find(DenmarkRootName);
        if (existing != null)
            return true;

        Vector2[][] rings = GetDenmarkRings();
        if (rings == null || rings.Length == 0)
        {
            Debug.LogError("CAMPAIGN-V013D|DenmarkRings=False|Reason=ReflectionFailed");
            return false;
        }

        GameObject root = new GameObject(DenmarkRootName);
        root.transform.SetParent(
            CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootTerrain).transform,
            false);

        for (int r = 0; r < rings.Length; r++)
        {
            Vector2[] ring = rings[r];
            if (ring == null || ring.Length < 3)
                continue;

            GameObject island = new GameObject("DNK_V013D_LandPart_" + (r + 1));
            island.transform.SetParent(root.transform, false);

            Vector3[] vertices = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                double longitude = ring[i].x;
                double latitude = ring[i].y;
                vertices[i] = CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    SampleDenmarkHeight(latitude, longitude));
            }

            int[] triangles = Triangulate(ring);
            Mesh mesh = new Mesh
            {
                name = "DNK_V013D_BroadDirect_Part_" + (r + 1),
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = island.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = island.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = landMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            GameObject coast = new GameObject("DNK_V013D_Coast_" + (r + 1));
            coast.transform.SetParent(root.transform, false);
            LineRenderer line = coast.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.widthMultiplier = 0.09f;
            line.positionCount = ring.Length;
            line.sharedMaterial = coastMaterial;

            for (int i = 0; i < ring.Length; i++)
            {
                double longitude = ring[i].x;
                double latitude = ring[i].y;
                line.SetPosition(i, CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    SampleDenmarkHeight(latitude, longitude) + 0.07f));
            }
        }

        return true;
    }

    private static Vector2[][] GetDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField(
            "DenmarkRings",
            BindingFlags.Static | BindingFlags.NonPublic);

        return field != null ? field.GetValue(null) as Vector2[][] : null;
    }

    public static float SampleDenmarkHeight(double latitude, double longitude)
    {
        // Very low visual relief while geography is being validated.
        // The final DEM/elevation layer is intentionally not invented here.
        float x = (float)((longitude - 8.0) / 7.5);
        float z = (float)((latitude - 54.4) / 3.5);
        float broad = Mathf.PerlinNoise(x * 1.65f + 4.8f, z * 1.55f + 8.1f) - 0.5f;

        float dx = (float)(longitude - 9.35);
        float dz = (float)(latitude - 56.15);
        float centralJutland = Mathf.Exp(-(dx * dx / 1.25f + dz * dz / 1.10f)) * 0.09f;

        return 0.34f + broad * 0.10f + centralJutland;
    }

    private static void GroundAndScaleDenmarkContext()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            bool visible = IsDenmarkFocusNode(node);

            GameObject marker = GameObject.Find("CampaignNode_" + node.Id);
            GameObject settlement = GameObject.Find("Settlement3D_" + node.Id);
            GameObject control = GameObject.Find("CampaignControl_" + node.Id);

            if (!visible)
            {
                SetRenderersVisible(marker, false);
                SetRenderersVisible(settlement, false);
                SetRenderersVisible(control, false);
                continue;
            }

            float y = SampleDenmarkHeight(node.Latitude, node.Longitude);

            if (marker != null)
            {
                marker.transform.position = new Vector3(node.MapPosition.x, y + 0.72f, node.MapPosition.y);
                marker.transform.localScale = node.Terrain == CampaignTerrainType.Fortified
                    ? new Vector3(1.75f, 0.28f, 1.75f)
                    : new Vector3(1.35f, 0.24f, 1.35f);
                SetRenderersVisible(marker, true);
            }

            if (settlement != null)
            {
                settlement.transform.position = new Vector3(node.MapPosition.x, y + 0.02f, node.MapPosition.y);
                float scale = node.Terrain == CampaignTerrainType.Urban ? 0.21f : 0.18f;
                if (node.Terrain == CampaignTerrainType.Fortified)
                    scale = 0.22f;
                settlement.transform.localScale = Vector3.one * scale;
                SetRenderersVisible(settlement, true);
            }

            if (control != null)
            {
                control.transform.position = new Vector3(node.MapPosition.x, y + 0.50f, node.MapPosition.y);
                control.transform.localScale = new Vector3(0.72f, 0.10f, 0.72f);
                SetRenderersVisible(control, true);
            }
        }

        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            CampaignNodeState node = formation != null ? CampaignSession.GetNode(formation.CurrentNodeId) : null;
            bool visible = node != null && IsDenmarkFocusNode(node);
            SetRenderersVisible(view.gameObject, visible);

            if (!visible)
                continue;

            float y = SampleDenmarkHeight(node.Latitude, node.Longitude);
            Vector3 p = view.transform.position;
            p.y = y + 2.1f;
            view.transform.position = p;
            view.transform.localScale = new Vector3(2.8f, 1.15f, 1.55f);
        }
    }

    private static void GroundConstructionProjects()
    {
        GroundConstruction("ConstructionProject_QA-BARRACKS-AALBORG", "AALBORG", new Vector2(1.55f, 1.10f), 0.33f);
        GroundConstruction("ConstructionProject_QA-FARM-AARHUS", "AARHUS", new Vector2(-1.45f, 1.05f), 0.34f);
    }

    private static void GroundConstruction(string objectName, string nodeId, Vector2 offset, float visualScale)
    {
        GameObject root = GameObject.Find(objectName);
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (root == null || node == null)
            return;

        float x = node.MapPosition.x + offset.x;
        float z = node.MapPosition.y + offset.y;
        root.transform.position = new Vector3(
            x,
            SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.03f,
            z);
        root.transform.localScale = Vector3.one * visualScale;
        SetRenderersVisible(root, true);
    }

    private static void RedrapeDenmarkInfrastructure()
    {
        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.name.StartsWith("StrategicLink_", StringComparison.Ordinal))
                continue;

            string[] ids = ParseLinkIds(line.name);
            CampaignNodeState a = ids != null ? CampaignSession.GetNode(ids[0]) : null;
            CampaignNodeState b = ids != null ? CampaignSession.GetNode(ids[1]) : null;

            bool visible = a != null && b != null &&
                           (IsDenmarkFocusNode(a) || IsDenmarkFocusNode(b));
            line.enabled = visible;
            if (!visible)
                continue;

            CampaignStrategicLinkType type = CampaignMapUsabilityV011.GetLinkType(a, b);
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = line.positionCount <= 1 ? 0f : i / (float)(line.positionCount - 1);
                double latitude = a.Latitude + (b.Latitude - a.Latitude) * t;
                double longitude = a.Longitude + (b.Longitude - a.Longitude) * t;
                Vector3 p = CampaignGeoProjection.Project3D(latitude, longitude, 0f);

                if (type == CampaignStrategicLinkType.SeaFerry)
                    p.y = -0.40f;
                else if (latitude >= 54.2 && latitude <= 58.0 && longitude >= 7.0 && longitude <= 16.0)
                    p.y = SampleDenmarkHeight(latitude, longitude) + 0.10f;
                else
                    p.y = 0.12f;

                line.SetPosition(i, p);
            }

            line.widthMultiplier = type == CampaignStrategicLinkType.Rail ? 0.22f : 0.18f;
        }
    }

    private static string[] ParseLinkIds(string lineName)
    {
        const string prefix = "StrategicLink_";
        if (string.IsNullOrEmpty(lineName) || !lineName.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        string[] ids = lineName.Substring(prefix.Length).Split('|');
        return ids.Length == 2 ? ids : null;
    }

    private static void HideNonDenmarkRenderers()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || IsDenmarkFocusNode(node))
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
        }
    }

    private static bool IsDenmarkFocusNode(CampaignNodeState node)
    {
        if (node == null)
            return false;

        if (node.Region == CampaignMapRegion.Denmark)
            return true;

        return node.Controller == CampaignNation.Denmark && SouthernContextNodes.Contains(node.Id);
    }

    private static void TuneSeaAndLighting()
    {
        GameObject sea = GameObject.Find("Campaign Sea Base");
        if (sea != null)
        {
            Renderer renderer = sea.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material seaMat = CreateMaterial(new Color(0.105f, 0.225f, 0.305f), "V013D_Sea");
                SetSmoothness(seaMat, 0.34f);
                renderer.sharedMaterial = seaMat;
            }
        }

        RenderSettings.ambientLight = new Color(0.47f, 0.50f, 0.46f);

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.intensity = 0.96f;
            light.color = new Color(1.0f, 0.955f, 0.86f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.48f;
        }
    }

    private static void DisableHelperByTypeName(string typeName)
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;
            if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
                behaviour.enabled = false;
        }
    }

    private void SuppressLegacyNodeLabels()
    {
        if (mapController == null || controllerNodeLabelStyleField == null)
            return;

        if (hiddenNodeLabelStyle == null)
        {
            hiddenNodeLabelStyle = new GUIStyle
            {
                fontSize = 1,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            hiddenNodeLabelStyle.normal.textColor = Color.clear;
            hiddenNodeLabelStyle.hover.textColor = Color.clear;
            hiddenNodeLabelStyle.active.textColor = Color.clear;
            hiddenNodeLabelStyle.focused.textColor = Color.clear;
        }

        controllerNodeLabelStyleField.SetValue(mapController, hiddenNodeLabelStyle);
    }

    private void OnGUI()
    {
        SuppressLegacyNodeLabels();

        if (!built || Camera.main == null)
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2)
            };
            labelStyle.normal.textColor = new Color(0.94f, 0.94f, 0.89f);
        }

        Camera cam = Camera.main;
        List<Rect> occupied = new List<Rect>();

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkFocusNode(node))
                continue;

            float y = SampleDenmarkHeight(node.Latitude, node.Longitude) + 1.2f;
            Vector3 world = new Vector3(node.MapPosition.x, y, node.MapPosition.y);
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 52f || guiY > Screen.height - 8f)
                continue;

            string flags = string.Empty;
            if (node.HasPort) flags += " ⚓";
            if (node.HasRail) flags += " R";
            if (node.Terrain == CampaignTerrainType.Fortified) flags += " F";

            Rect rect = new Rect(screen.x - 43f, guiY - 10f, 86f, 18f);
            for (int pass = 0; pass < 6 && OverlapsAny(rect, occupied); pass++)
                rect.y -= 18f;

            occupied.Add(rect);
            GUI.Box(rect, node.Name + flags, labelStyle);
        }
    }

    private static bool OverlapsAny(Rect candidate, List<Rect> occupied)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            Rect expanded = occupied[i];
            expanded.xMin -= 2f;
            expanded.xMax += 2f;
            expanded.yMin -= 2f;
            expanded.yMax += 2f;
            if (candidate.Overlaps(expanded))
                return true;
        }
        return false;
    }

    private static void SetRootActive(string name, bool active)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
            root.SetActive(active);
    }

    private static void HideAndDestroy(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null)
            return;

        root.SetActive(false);
        UnityEngine.Object.Destroy(root);
    }

    private static void SetRenderersVisible(GameObject root, bool visible)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }

    private static void SetSmoothness(Material material, float value)
    {
        if (material == null)
            return;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", value);
    }

    private static int[] Triangulate(Vector2[] input)
    {
        int n = input != null ? input.Length : 0;
        if (n < 3)
            return new int[0];

        List<int> indices = new List<int>(n);
        if (SignedArea(input) > 0f)
        {
            for (int i = 0; i < n; i++)
                indices.Add(i);
        }
        else
        {
            for (int i = n - 1; i >= 0; i--)
                indices.Add(i);
        }

        List<int> triangles = new List<int>((n - 2) * 3);
        int guard = 0;

        while (indices.Count > 2 && guard++ < n * n * 2)
        {
            bool clipped = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 a = input[prev];
                Vector2 b = input[curr];
                Vector2 c = input[next];

                if (Cross(b - a, c - b) <= 0.0000001f)
                    continue;

                bool contains = false;
                for (int k = 0; k < indices.Count; k++)
                {
                    int candidate = indices[k];
                    if (candidate == prev || candidate == curr || candidate == next)
                        continue;

                    if (PointInTriangle(input[candidate], a, b, c))
                    {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                    continue;

                triangles.Add(prev);
                triangles.Add(curr);
                triangles.Add(next);
                indices.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
                break;
        }

        return triangles.ToArray();
    }

    private static float SignedArea(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            area += points[j].x * points[i].y - points[i].x * points[j].y;
        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);

        bool hasNegative = c1 < -0.000001f || c2 < -0.000001f || c3 < -0.000001f;
        bool hasPositive = c1 > 0.000001f || c2 > 0.000001f || c3 > 0.000001f;
        return !(hasNegative && hasPositive);
    }
}
