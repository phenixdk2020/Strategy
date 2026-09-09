using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13g — Denmark label ownership + close zoom correction.
//
// v13f owns the Denmark labels, but CampaignMapController still drew its legacy
// all-Europe node labels. This pass suppresses that legacy label layer without
// touching campaign state. It also removes the hidden v13 terrain floor from the
// camera clamp so the clean Denmark map can be inspected at useful close zoom.
[DefaultExecutionOrder(3600)]
public sealed class CampaignDenmarkUiZoomFixV013G : MonoBehaviour
{
    public const string BuildTag = "v00.00.13g";

    private static readonly HashSet<string> FocusNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING",
        "AALBORG",
        "VIBORG",
        "AARHUS",
        "HORSENS",
        "VEJLE",
        "FREDERICIA",
        "KOLDING",
        "HADERSLEV",
        "DYBBOEL",
        "SONDERBORG",
        "ODENSE",
        "KORSOR",
        "ROSKILDE",
        "CPH",
        "FLENSBURG",
        "SCHLESWIG"
    };

    private CampaignMapController mapController;
    private FieldInfo nodeLabelStyleField;
    private MethodInfo ensureStylesMethod;
    private GUIStyle hiddenNodeLabelStyle;

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

        mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController != null)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            nodeLabelStyleField = typeof(CampaignMapController).GetField("nodeLabelStyle", flags);
            ensureStylesMethod = typeof(CampaignMapController).GetMethod("EnsureStyles", flags);

            // EnsureStyles normally runs from OnGUI and would otherwise overwrite our
            // hidden style on the first frame. Initialize it once, then take ownership.
            if (ensureStylesMethod != null)
                ensureStylesMethod.Invoke(mapController, null);
        }

        BuildHiddenLabelStyle();
        SuppressLegacyNodeLabels();
        HideNonFocusLegacyMarkers();
        TuneCloseZoom();

        Debug.Log("CAMPAIGN-V013G|LegacyAllEuropeLabels=False|DenmarkLabels=v13f|CloseZoom=True|LegacyTerrainFloor=False|MinHeight=10|TerrainClearance=6.5|SimulationChanged=False");
    }

    private void LateUpdate()
    {
        SuppressLegacyNodeLabels();
        HideNonFocusLegacyMarkers();
        MaintainCameraSettings();
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

    private static void HideNonFocusLegacyMarkers()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || FocusNodes.Contains(node.Id))
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
        }
    }

    private static void TuneCloseZoom()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.nearClipPlane = 0.20f;
        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 6.5f;
        controller.MinHeight = 10f;
        controller.MaxHeight = 340f;
        controller.PanSpeed = 58f;
        controller.ZoomSpeed = 112f;
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

        // Other older polish components may attempt to restore their camera limits.
        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 6.5f;
        controller.MinHeight = 10f;
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
