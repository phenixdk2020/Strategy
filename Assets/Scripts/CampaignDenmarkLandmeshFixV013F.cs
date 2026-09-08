using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13f — targeted correction on top of the clean v13e render foundation.
// Fixes the Denmark mesh winding/back-face problem, validates upward normals and takes
// ownership of Denmark label LOD while preserving v13e sea/infrastructure/settlements.
[DefaultExecutionOrder(3400)]
public sealed class CampaignDenmarkLandmeshFixV013F : MonoBehaviour
{
    public const string BuildTag = "v00.00.13f";
    public const string RootName = "V013F_DENMARK_LANDMESH_FIX";

    private static readonly HashSet<string> SouthernContextNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "FLENSBURG",
        "SCHLESWIG"
    };

    private static readonly HashSet<string> StrategicLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "AALBORG",
        "AARHUS",
        "FREDERICIA",
        "ODENSE",
        "CPH"
    };

    private static readonly HashSet<string> RegionalLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING",
        "VIBORG",
        "VEJLE",
        "KOLDING",
        "KORSOR",
        "ROSKILDE"
    };

    private static readonly HashSet<string> LocalLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "HADERSLEV",
        "DYBBOEL",
        "SONDERBORG",
        "FLENSBURG",
        "SCHLESWIG"
    };

    private readonly List<Material> landMaterials = new List<Material>();
    private Material coastMaterial;
    private GUIStyle strategicLabelStyle;
    private GUIStyle regionalLabelStyle;
    private GUIStyle southernLabelStyle;
    private bool built;
    private float averageNormalY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkLandmeshFixV013F>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkLandmeshFixV013F");
        root.AddComponent<CampaignDenmarkLandmeshFixV013F>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();

        // v13e has already created the clean sea, infrastructure and settlement set.
        // Stop its LateUpdate/OnGUI so this pass can own the corrected landmesh and labels.
        CampaignDenmarkCleanRenderV013E v13e = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkCleanRenderV013E>();
        if (v13e != null)
            v13e.enabled = false;

        HideRenderersUnder("V013E_Denmark_DirectBroad");
        MaintainLegacySuppression();
        BuildMaterials();
        built = BuildCorrectedDenmark();
        TuneExistingInfrastructure();
        TuneCamera();
        GroundConstructionProjects();

        if (built && averageNormalY <= 0.05f)
            Debug.LogError(string.Format("CAMPAIGN-V013F|Landmesh=True|NormalValidation=False|AverageNormalY={0:F4}", averageNormalY));
        else
            Debug.Log(string.Format("CAMPAIGN-V013F|Landmesh={0}|Winding=Upward|AverageNormalY={1:F4}|LabelLOD=3Tier|SouthernContext=DeEmphasized|SimulationChanged=False", built, averageNormalY));
    }

    private void LateUpdate()
    {
        MaintainLegacySuppression();
        HideRenderersUnder("V013E_Denmark_DirectBroad");
        GroundConstructionProjects();
        GroundFormations();
    }

    private void BuildMaterials()
    {
        landMaterials.Add(CreateMaterial(new Color(0.315f, 0.435f, 0.245f), "V013F_Land_Grass"));
        landMaterials.Add(CreateMaterial(new Color(0.345f, 0.455f, 0.255f), "V013F_Land_Field"));
        landMaterials.Add(CreateMaterial(new Color(0.295f, 0.410f, 0.230f), "V013F_Land_Heath"));
        coastMaterial = CreateMaterial(new Color(0.80f, 0.77f, 0.61f), "V013F_Coast");

        foreach (Material material in landMaterials)
            SetSmoothness(material, 0.04f);
        SetSmoothness(coastMaterial, 0.03f);
    }

    private bool BuildCorrectedDenmark()
    {
        Vector2[][] rings = GetDenmarkRings();
        if (rings == null || rings.Length == 0)
        {
            Debug.LogError("CAMPAIGN-V013F|DenmarkRings=False|Reason=ReflectionFailed");
            return false;
        }

        GameObject old = GameObject.Find(RootName);
        if (old != null)
            UnityEngine.Object.Destroy(old);

        GameObject root = new GameObject(RootName);
        float normalSum = 0f;
        int normalSamples = 0;
        int builtParts = 0;

        for (int r = 0; r < rings.Length; r++)
        {
            Vector2[] ring = rings[r];
            if (ring == null || ring.Length < 3)
                continue;

            Vector3[] vertices = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                double longitude = ring[i].x;
                double latitude = ring[i].y;
                vertices[i] = CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude) + 0.015f);
            }

            int[] triangles = Triangulate(ring);
            if (triangles.Length < 3)
            {
                Debug.LogWarning("CAMPAIGN-V013F|Triangulation=False|Part=" + (r + 1));
                continue;
            }

            EnsureUpwardWinding(vertices, triangles);

            GameObject part = new GameObject("V013F_LandPart_" + (r + 1));
            part.transform.SetParent(root.transform, false);

            Mesh mesh = new Mesh
            {
                name = "V013F_DenmarkMesh_" + (r + 1),
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = landMaterials[r % landMaterials.Count];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            foreach (Vector3 normal in mesh.normals)
            {
                normalSum += normal.y;
                normalSamples++;
            }

            CreateCoastline(root.transform, ring, r + 1);
            builtParts++;
        }

        averageNormalY = normalSamples > 0 ? normalSum / normalSamples : -1f;
        Debug.Log(string.Format("CAMPAIGN-V013F|Parts={0}|NormalSamples={1}|AverageNormalY={2:F4}", builtParts, normalSamples, averageNormalY));
        return builtParts > 0 && averageNormalY > 0.05f;
    }

    private void CreateCoastline(Transform parent, Vector2[] ring, int part)
    {
        GameObject coast = new GameObject("V013F_Coast_" + part);
        coast.transform.SetParent(parent, false);

        LineRenderer line = coast.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = 0.065f;
        line.positionCount = ring.Length;
        line.sharedMaterial = coastMaterial;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        for (int i = 0; i < ring.Length; i++)
        {
            double longitude = ring[i].x;
            double latitude = ring[i].y;
            line.SetPosition(i, CampaignGeoProjection.Project3D(
                latitude,
                longitude,
                CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude) + 0.075f));
        }
    }

    private static void EnsureUpwardWinding(Vector3[] vertices, int[] triangles)
    {
        float signedY = 0f;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            signedY += Vector3.Cross(b - a, c - a).y;
        }

        if (signedY >= 0f)
            return;

        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int temp = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = temp;
        }
    }

    private static void TuneExistingInfrastructure()
    {
        GameObject root = GameObject.Find("V013E_DENMARK_CLEAN_RENDER");
        if (root == null)
            return;

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            if (line == null || line.sharedMaterial == null)
                continue;

            string materialName = line.sharedMaterial.name ?? string.Empty;
            if (materialName.IndexOf("Rail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                line.widthMultiplier = 0.22f;
                line.startColor = new Color(0.11f, 0.11f, 0.11f, 1f);
                line.endColor = line.startColor;
            }
            else if (materialName.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                line.widthMultiplier = 0.17f;
                line.startColor = new Color(0.22f, 0.52f, 0.72f, 0.95f);
                line.endColor = line.startColor;
            }
            else if (materialName.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                line.widthMultiplier = 0.16f;
                line.startColor = new Color(0.53f, 0.40f, 0.25f, 1f);
                line.endColor = line.startColor;
            }
        }
    }

    private static void TuneCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 centre = CampaignGeoProjection.Project3D(56.05, 10.25, 0f);
        Vector3 homePosition = new Vector3(centre.x, 142f, centre.z - 57f);
        Quaternion homeRotation = Quaternion.Euler(59f, 0f, 0f);

        cam.transform.position = homePosition;
        cam.transform.rotation = homeRotation;
        cam.fieldOfView = 43f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.MinHeight = 38f;
        controller.MaxHeight = 340f;
        controller.PanSpeed = 68f;
        controller.ZoomSpeed = 76f;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo homePositionField = typeof(CampaignMapCameraController).GetField("homePosition", flags);
        FieldInfo homeRotationField = typeof(CampaignMapCameraController).GetField("homeRotation", flags);
        if (homePositionField != null) homePositionField.SetValue(controller, homePosition);
        if (homeRotationField != null) homeRotationField.SetValue(controller, homeRotation);
    }

    private void OnGUI()
    {
        Camera cam = Camera.main;
        if (!built || cam == null)
            return;

        EnsureLabelStyles();

        float cameraHeight = cam.transform.position.y;
        List<Rect> occupied = new List<Rect>();

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || !IsFocusNode(node) || !ShouldShowLabel(node.Id, cameraHeight))
                continue;

            float y = CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 1.0f;
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(node.MapPosition.x, y, node.MapPosition.y));
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 48f || guiY > Screen.height - 8f)
                continue;

            string flags = string.Empty;
            if (node.HasPort) flags += " ⚓";
            if (node.HasRail) flags += " R";
            if (node.Terrain == CampaignTerrainType.Fortified) flags += " F";

            bool southern = SouthernContextNodes.Contains(node.Id);
            GUIStyle style = southern ? southernLabelStyle : cameraHeight > 115f ? strategicLabelStyle : regionalLabelStyle;
            float width = southern ? 76f : 88f;
            Rect baseRect = new Rect(screen.x - width * 0.5f, guiY - 9f, width, 18f);
            Rect rect = FindNonOverlappingRect(baseRect, occupied);

            if (rect.yMin < 46f || rect.yMax > Screen.height - 5f)
                continue;

            occupied.Add(rect);
            GUI.Box(rect, node.Name + flags, style);
        }
    }

    private void EnsureLabelStyles()
    {
        if (strategicLabelStyle != null)
            return;

        strategicLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        strategicLabelStyle.normal.textColor = new Color(0.96f, 0.96f, 0.92f);

        regionalLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        regionalLabelStyle.normal.textColor = new Color(0.90f, 0.92f, 0.88f);

        southernLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 8,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(3, 3, 1, 1)
        };
        southernLabelStyle.normal.textColor = new Color(0.72f, 0.74f, 0.70f);
    }

    private static bool ShouldShowLabel(string nodeId, float cameraHeight)
    {
        if (cameraHeight > 125f)
            return StrategicLabels.Contains(nodeId);

        if (cameraHeight > 82f)
            return StrategicLabels.Contains(nodeId) || RegionalLabels.Contains(nodeId);

        return StrategicLabels.Contains(nodeId) || RegionalLabels.Contains(nodeId) || LocalLabels.Contains(nodeId);
    }

    private static Rect FindNonOverlappingRect(Rect baseRect, List<Rect> occupied)
    {
        Vector2[] offsets =
        {
            Vector2.zero,
            new Vector2(0f, -20f),
            new Vector2(0f, 20f),
            new Vector2(-48f, 0f),
            new Vector2(48f, 0f),
            new Vector2(-42f, -18f),
            new Vector2(42f, -18f),
            new Vector2(-42f, 18f),
            new Vector2(42f, 18f),
            new Vector2(0f, -40f)
        };

        foreach (Vector2 offset in offsets)
        {
            Rect candidate = new Rect(baseRect.x + offset.x, baseRect.y + offset.y, baseRect.width, baseRect.height);
            if (!OverlapsAny(candidate, occupied))
                return candidate;
        }

        return new Rect(baseRect.x, baseRect.y - 58f, baseRect.width, baseRect.height);
    }

    private static void GroundConstructionProjects()
    {
        GroundConstruction("ConstructionProject_QA-BARRACKS-AALBORG", "AALBORG", new Vector2(1.30f, 0.95f), 0.30f);
        GroundConstruction("ConstructionProject_QA-FARM-AARHUS", "AARHUS", new Vector2(-1.20f, 0.90f), 0.31f);
    }

    private static void GroundConstruction(string objectName, string nodeId, Vector2 offset, float scale)
    {
        GameObject root = GameObject.Find(objectName);
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (root == null || node == null)
            return;

        root.transform.position = new Vector3(
            node.MapPosition.x + offset.x,
            CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.04f,
            node.MapPosition.y + offset.y);
        root.transform.localScale = Vector3.one * scale;
        SetRenderersVisible(root, true);
    }

    private static void GroundFormations()
    {
        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            if (formation == null)
                continue;

            CampaignNodeState current = CampaignSession.GetNode(formation.CurrentNodeId);
            bool visible = current != null && IsFocusNode(current);
            SetRenderersVisible(view.gameObject, visible);
            if (!visible)
                continue;

            Vector3 p = view.transform.position;
            p.y = HeightFromWorld(p.x, p.z) + 1.75f;
            view.transform.position = p;
        }
    }

    private static void MaintainLegacySuppression()
    {
        HideRenderersUnder(CampaignTerrainV013.RootTerrain);
        HideRenderersUnder(CampaignTerrainV013.RootHydrology);
        HideRenderersUnder(CampaignTerrainV013.RootLandCover);
        HideRenderersUnder(CampaignTerrainV013.RootInfrastructure);
        HideRenderersUnder(CampaignTerrainV013.RootSettlements);
        HideRenderersUnder(CampaignTerrainV013.RootOverlays);
        HideRenderersUnder("GEO_Denmark_NaturalEarth50m");
        HideRenderersUnder("GEO_Denmark_NaturalEarth50m_V013A");
        HideRenderersUnder("GEO_Denmark_NaturalEarth50m_V013C_BROAD");
        HideRenderersUnder("GEO_Denmark_V013D_BROAD_DIRECT");
        HideRenderersUnder("V013B_DenmarkLandCover");
        HideRenderersUnder("V013B_Vegetation");
        HideRenderersUnder("Campaign Sea Base");
        HideRenderersUnder("CampaignTerrainSurface_v013");
    }

    private static Vector2[][] GetDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField("DenmarkRings", BindingFlags.Static | BindingFlags.NonPublic);
        return field != null ? field.GetValue(null) as Vector2[][] : null;
    }

    private static float HeightFromWorld(float x, float z)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        double longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        return CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude);
    }

    private static bool IsFocusNode(CampaignNodeState node)
    {
        if (node == null)
            return false;
        if (node.Region == CampaignMapRegion.Denmark)
            return true;
        return node.Controller == CampaignNation.Denmark && SouthernContextNodes.Contains(node.Id);
    }

    private static void HideRenderersUnder(string objectName)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
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

    private static bool OverlapsAny(Rect candidate, List<Rect> occupied)
    {
        foreach (Rect occupiedRect in occupied)
        {
            Rect expanded = occupiedRect;
            expanded.xMin -= 3f;
            expanded.xMax += 3f;
            expanded.yMin -= 3f;
            expanded.yMax += 3f;
            if (candidate.Overlaps(expanded))
                return true;
        }
        return false;
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

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
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", value);
    }

    private static int[] Triangulate(Vector2[] input)
    {
        int n = input != null ? input.Length : 0;
        if (n < 3)
            return new int[0];

        List<int> indices = new List<int>(n);
        if (SignedArea(input) > 0f)
        {
            for (int i = 0; i < n; i++) indices.Add(i);
        }
        else
        {
            for (int i = n - 1; i >= 0; i--) indices.Add(i);
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