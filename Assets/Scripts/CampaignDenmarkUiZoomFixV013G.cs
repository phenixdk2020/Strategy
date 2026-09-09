using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13g — Denmark-only label ownership + close zoom correction.
//
// Presentation-only pass. The strategic simulation still contains the wider 50-node
// network, but the current QA view deliberately renders ONLY CampaignMapRegion.Denmark.
// v13f remains responsible for building the corrected Denmark landmesh; after Start()
// this component disables v13f's GUI/LateUpdate and becomes the sole label owner.
[DefaultExecutionOrder(3600)]
public sealed class CampaignDenmarkUiZoomFixV013G : MonoBehaviour
{
    public const string BuildTag = "v00.00.13g";

    private static readonly HashSet<string> OverviewLabels = new HashSet<string>(StringComparer.Ordinal)
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
        "HORSENS",
        "VEJLE",
        "KOLDING",
        "HADERSLEV",
        "KORSOR",
        "ROSKILDE"
    };

    private CampaignMapController mapController;
    private FieldInfo nodeLabelStyleField;
    private MethodInfo ensureStylesMethod;
    private GUIStyle hiddenNodeLabelStyle;
    private GUIStyle overviewLabelStyle;
    private GUIStyle regionalLabelStyle;
    private GUIStyle localLabelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkUiZoomFixV013G>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkUiZoomFixV013G");
        root.AddComponent<CampaignDenmarkUiZoomFixV013G>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();

        // v13f has the lower execution order and has already built the corrected landmesh.
        // Disable only its behaviour now so its landmesh GameObjects stay alive while its
        // old GUI (including Flensburg/Schleswig context) cannot draw a second label set.
        CampaignDenmarkLandmeshFixV013F v13f = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkLandmeshFixV013F>();
        if (v13f != null)
            v13f.enabled = false;

        mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController != null)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            nodeLabelStyleField = typeof(CampaignMapController).GetField("nodeLabelStyle", flags);
            ensureStylesMethod = typeof(CampaignMapController).GetMethod("EnsureStyles", flags);

            // CampaignMapController initializes nodeLabelStyle lazily from OnGUI.
            // Initialize once here, then replace it with a transparent 1x1 style.
            if (ensureStylesMethod != null)
                ensureStylesMethod.Invoke(mapController, null);
        }

        BuildHiddenLabelStyle();
        SuppressLegacyNodeLabels();
        HideNonDenmarkPresentation();
        GroundDenmarkFormations();
        GroundConstructionProjects();
        TuneCloseZoom();

        Debug.Log("CAMPAIGN-V013G|VisibleRegion=DenmarkOnly|LegacyAllEuropeLabels=False|V13FGui=False|ForeignSettlements=False|ForeignLinks=False|CloseZoom=True|LegacyTerrainFloor=False|MinHeight=8|TerrainClearance=5.5|SimulationChanged=False");
    }

    private void LateUpdate()
    {
        SuppressLegacyNodeLabels();
        HideNonDenmarkPresentation();
        GroundDenmarkFormations();
        GroundConstructionProjects();
        MaintainCameraSettings();
    }

    private void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        EnsureVisibleLabelStyles();
        List<Rect> occupied = new List<Rect>();
        float cameraHeight = cam.transform.position.y;

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node) || !ShouldShowLabel(node.Id, cameraHeight))
                continue;

            float y = CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 1.0f;
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(node.MapPosition.x, y, node.MapPosition.y));
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 48f || guiY > Screen.height - 6f)
                continue;

            string flags = string.Empty;
            if (node.HasPort) flags += " ⚓";
            if (node.HasRail) flags += " R";
            if (node.Terrain == CampaignTerrainType.Fortified) flags += " F";

            bool overview = OverviewLabels.Contains(node.Id);
            bool regional = RegionalLabels.Contains(node.Id);
            GUIStyle style = overview ? overviewLabelStyle : regional ? regionalLabelStyle : localLabelStyle;
            float width = overview ? 92f : regional ? 82f : 76f;

            Rect baseRect = new Rect(screen.x - width * 0.5f, guiY - 9f, width, 18f);
            Rect rect = FindNonOverlappingRect(baseRect, occupied);
            if (rect.yMin < 46f || rect.yMax > Screen.height - 4f)
                continue;

            occupied.Add(rect);
            GUI.Box(rect, node.Name + flags, style);
        }
    }

    private void BuildHiddenLabelStyle()
    {
        hiddenNodeLabelStyle = new GUIStyle
        {
            fontSize = 1,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            fixedWidth = 1f,
            fixedHeight = 1f
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        hiddenNodeLabelStyle.normal.textColor = clear;
        hiddenNodeLabelStyle.hover.textColor = clear;
        hiddenNodeLabelStyle.active.textColor = clear;
        hiddenNodeLabelStyle.focused.textColor = clear;
    }

    private void EnsureVisibleLabelStyles()
    {
        if (overviewLabelStyle != null)
            return;

        overviewLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        overviewLabelStyle.normal.textColor = new Color(0.97f, 0.97f, 0.93f);

        regionalLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        regionalLabelStyle.normal.textColor = new Color(0.91f, 0.93f, 0.89f);

        localLabelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 8,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(3, 3, 1, 1)
        };
        localLabelStyle.normal.textColor = new Color(0.84f, 0.87f, 0.82f);
    }

    private void SuppressLegacyNodeLabels()
    {
        if (mapController == null)
            mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController == null)
            return;

        if (nodeLabelStyleField == null)
        {
            nodeLabelStyleField = typeof(CampaignMapController).GetField(
                "nodeLabelStyle",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (hiddenNodeLabelStyle == null)
            BuildHiddenLabelStyle();

        if (nodeLabelStyleField != null)
            nodeLabelStyleField.SetValue(mapController, hiddenNodeLabelStyle);
    }

    private static bool ShouldShowLabel(string nodeId, float cameraHeight)
    {
        if (cameraHeight > 105f)
            return OverviewLabels.Contains(nodeId);

        if (cameraHeight > 55f)
            return OverviewLabels.Contains(nodeId) || RegionalLabels.Contains(nodeId);

        return true; // Close zoom: all Danish nodes only.
    }

    private static bool IsDenmarkNode(CampaignNodeState node)
    {
        return node != null && node.Region == CampaignMapRegion.Denmark;
    }

    private static void HideNonDenmarkPresentation()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || IsDenmarkNode(node))
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("V013E_Settlement_" + node.Id), false);
        }

        // v13e created its focus links before v13g existed and could therefore include
        // the then-retained southern German context. Keep only links whose endpoints
        // are both actual CampaignMapRegion.Denmark nodes.
        GameObject cleanRoot = GameObject.Find("V013E_DENMARK_CLEAN_RENDER");
        if (cleanRoot == null)
            return;

        LineRenderer[] lines = cleanRoot.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.gameObject.name.StartsWith("V013E_Link_", StringComparison.Ordinal))
                continue;

            string edge = line.gameObject.name.Substring("V013E_Link_".Length);
            string[] ids = edge.Split('|');
            if (ids.Length != 2)
                continue;

            CampaignNodeState a = CampaignSession.GetNode(ids[0]);
            CampaignNodeState b = CampaignSession.GetNode(ids[1]);
            line.enabled = IsDenmarkNode(a) && IsDenmarkNode(b);
        }
    }

    private static void GroundDenmarkFormations()
    {
        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>();
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            if (formation == null)
                continue;

            CampaignNodeState current = CampaignSession.GetNode(formation.CurrentNodeId);
            bool visible = IsDenmarkNode(current);
            SetRenderersVisible(view.gameObject, visible);
            if (!visible)
                continue;

            Vector3 p = view.transform.position;
            p.y = HeightFromWorld(p.x, p.z) + 1.75f;
            view.transform.position = p;
        }
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
        if (root == null || !IsDenmarkNode(node))
            return;

        root.transform.position = new Vector3(
            node.MapPosition.x + offset.x,
            CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.04f,
            node.MapPosition.y + offset.y);
        root.transform.localScale = Vector3.one * scale;
        SetRenderersVisible(root, true);
    }

    private static float HeightFromWorld(float x, float z)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        double longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        return CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude);
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
            new Vector2(0f, -40f),
            new Vector2(0f, 40f)
        };

        foreach (Vector2 offset in offsets)
        {
            Rect candidate = new Rect(baseRect.x + offset.x, baseRect.y + offset.y, baseRect.width, baseRect.height);
            if (!OverlapsAny(candidate, occupied))
                return candidate;
        }

        return new Rect(baseRect.x, baseRect.y - 60f, baseRect.width, baseRect.height);
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

    private static void TuneCloseZoom()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.nearClipPlane = 0.15f;
        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 5.5f;
        controller.MinHeight = 8f;
        controller.MaxHeight = 340f;
        controller.PanSpeed = 54f;
        controller.ZoomSpeed = 118f;
        controller.MinPitch = 28f;
        controller.MaxPitch = 75f;
    }

    private static void MaintainCameraSettings()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 5.5f;
        controller.MinHeight = 8f;
        controller.MaxHeight = 340f;
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
}
