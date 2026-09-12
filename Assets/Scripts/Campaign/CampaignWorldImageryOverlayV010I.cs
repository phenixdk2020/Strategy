using UnityEngine;

/// <summary>
/// PROJECT 1864 v00.00.10i visible QA/status overlay for provider 9 world imagery.
/// It deliberately does not own gameplay or provider selection; it only exposes
/// the world-streaming controls/status and masks the older v10h build badge.
/// </summary>
[DefaultExecutionOrder(30000)]
public sealed class CampaignWorldImageryOverlayV010I : MonoBehaviour
{
    private const string ProviderRootName = "BASEMAP_09_9_Imagery";

    private GameObject providerRoot;
    private CampaignRasterBasemapV010G worldRaster;
    private GUIStyle badgeStyle;
    private GUIStyle worldStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignWorldImageryOverlayV010I>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_WorldImageryOverlay_v000010i");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignWorldImageryOverlayV010I>();
    }

    private void Update()
    {
        if (providerRoot == null)
        {
            providerRoot = GameObject.Find(ProviderRootName);
            if (providerRoot != null)
                worldRaster = providerRoot.GetComponent<CampaignRasterBasemapV010G>();
        }
    }

    private void EnsureStyles()
    {
        if (badgeStyle != null)
            return;

        badgeStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        badgeStyle.normal.textColor = Color.white;

        worldStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            wordWrap = true
        };
        worldStyle.normal.textColor = Color.white;
    }

    private static void DrawOpaque(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void OnGUI()
    {
        GUI.depth = -2000;
        EnsureStyles();

        // Cover the v10h badge drawn by CampaignMapStyleSwitcher.
        DrawOpaque(new Rect(0f, 0f, 228f, 42f), new Color(0.035f, 0.075f, 0.095f, 0.99f));
        GUI.Box(
            new Rect(8f, 8f, 212f, 31f),
            "PROJECT 1864 | v00.00.10i",
            badgeStyle);

        if (providerRoot == null || !providerRoot.activeInHierarchy || worldRaster == null)
            return;

        string detail = worldRaster.WorldDetailStatus;
        string status = worldRaster.Status;

        Rect panel = new Rect(8f, 194f, Mathf.Min(410f, Screen.width - 16f), 72f);
        DrawOpaque(panel, new Color(0.035f, 0.070f, 0.085f, 0.94f));
        GUI.Box(
            panel,
            "9 IMAGERY · WORLD STREAMING\n" +
            status + "\n" +
            detail + "\n" +
            "Home=Danmark · PageUp=Europa · End=Verden · WASD/pile=pan · hjul=zoom · MMB=træk",
            worldStyle);
    }
}
