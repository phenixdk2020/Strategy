using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(500)]
public sealed class CampaignLayerManagerV013 : MonoBehaviour
{
    private bool showInfrastructure = true;
    private bool showSettlements = true;
    private bool showLivingWorld = true;
    private bool showOverlays = true;
    private bool showHydrology = true;

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignLayerManagerV013>() != null)
            return;

        GameObject root = new GameObject("CampaignLayerManagerV013");
        root.AddComponent<CampaignLayerManagerV013>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            showInfrastructure = !showInfrastructure;
            SetLayer(CampaignTerrainV013.RootInfrastructure, showInfrastructure);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            showSettlements = !showSettlements;
            SetLayer(CampaignTerrainV013.RootSettlements, showSettlements);
        }
        if (Input.GetKeyDown(KeyCode.F8))
        {
            showLivingWorld = !showLivingWorld;
            SetLayer(CampaignTerrainV013.RootLivingWorld, showLivingWorld);
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            showOverlays = !showOverlays;
            SetLayer(CampaignTerrainV013.RootOverlays, showOverlays);
        }
        if (Input.GetKeyDown(KeyCode.F10))
        {
            showHydrology = !showHydrology;
            SetLayer(CampaignTerrainV013.RootHydrology, showHydrology);
        }
    }

    private static void SetLayer(string name, bool visible)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
            root.SetActive(visible);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            padding = new RectOffset(8, 8, 7, 7)
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
    }

    private void OnGUI()
    {
        EnsureStyles();
        float width = 285f;
        float height = 126f;
        Rect rect = new Rect(Screen.width - width - 12f, 12f, width, height);
        GUI.Box(rect, string.Empty, panelStyle);

        float x = rect.x + 10f;
        float y = rect.y + 8f;
        GUI.Label(new Rect(x, y, width - 20f, 20f), "3D MAP LAYERS", labelStyle);
        y += 21f;
        GUI.Label(new Rect(x, y, width - 20f, 18f), "F6 Infrastructure: " + OnOff(showInfrastructure), labelStyle); y += 17f;
        GUI.Label(new Rect(x, y, width - 20f, 18f), "F7 Settlements: " + OnOff(showSettlements), labelStyle); y += 17f;
        GUI.Label(new Rect(x, y, width - 20f, 18f), "F8 Living world: " + OnOff(showLivingWorld), labelStyle); y += 17f;
        GUI.Label(new Rect(x, y, width - 20f, 18f), "F9 Overlays: " + OnOff(showOverlays), labelStyle); y += 17f;
        GUI.Label(new Rect(x, y, width - 20f, 18f), "F10 Hydrology: " + OnOff(showHydrology), labelStyle);
    }

    private static string OnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
