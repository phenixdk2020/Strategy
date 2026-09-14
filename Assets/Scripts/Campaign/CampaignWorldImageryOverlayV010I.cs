using UnityEngine;

/// <summary>
/// PROJECT 1864 current Campaign3 status overlay for the imagery-only campaign map.
/// Shows the compact build badge at all times while technical imagery/terrain panels
/// are optional through CampaignHudStateV010N2.
/// </summary>
[DefaultExecutionOrder(30000)]
public sealed class CampaignWorldImageryOverlayV010I : MonoBehaviour
{
    private const string ProviderRootName = "BASEMAP_09_9_Imagery";

    private GameObject providerRoot;
    private CampaignRasterBasemapV010G worldRaster;
    private CampaignImageryTerrainV010K terrain;
    private GUIStyle badgeStyle;
    private GUIStyle worldStyle;
    private GUIStyle mapInfoStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignWorldImageryOverlayV010I>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_WorldImageryOverlay_CURRENT");
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
        else if (worldRaster == null)
        {
            worldRaster = providerRoot.GetComponent<CampaignRasterBasemapV010G>();
        }

        if (terrain == null)
            terrain = CampaignImageryTerrainV010K.Instance;
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

        mapInfoStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 9,
            wordWrap = true
        };
        mapInfoStyle.normal.textColor = Color.white;
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

        // The compact build identity remains visible even in minimal-HUD mode.
        DrawOpaque(new Rect(0f, 0f, 228f, 42f), new Color(0.035f, 0.075f, 0.095f, 0.99f));
        GUI.Box(
            new Rect(8f, 8f, 212f, 31f),
            "PROJECT 1864 | " + CampaignBuildInfo.CurrentVersion,
            badgeStyle);

        if (!CampaignHudStateV010N2.DebugVisible)
            return;

        string status = worldRaster != null ? worldRaster.Status : "INITIALISING";
        string detail = worldRaster != null ? worldRaster.WorldDetailStatus : "Waiting for World Imagery provider";
        string terrainStatus = terrain != null ? terrain.Status : "TERRAIN INITIALISING";

        float panelY = CampaignHudStateV010N2.SelectionVisible ? 280f : 48f;
        Rect panel = new Rect(8f, panelY, Mathf.Min(455f, Screen.width - 16f), 98f);
        DrawOpaque(panel, new Color(0.035f, 0.070f, 0.085f, 0.94f));
        GUI.Box(
            panel,
            "WORLD IMAGERY + 3D TERRAIN\n" +
            status + "\n" +
            detail + "\n" +
            terrainStatus + "\n" +
            "Home=Danmark · PageUp=Europa · End=Verden · WASD/pile=pan · hjul=zoom · MMB=træk · T=2D/3D · Q/E=drej",
            worldStyle);

        // Keep this separate from the campaign data box instead of drawing both
        // directly on top of each other as previous builds did.
        Rect legacyMapInfo = new Rect(8f, Screen.height - 154f, 420f, 68f);
        DrawOpaque(legacyMapInfo, new Color(0.035f, 0.070f, 0.085f, 0.98f));
        GUI.Box(
            legacyMapInfo,
            "BASEMAP: World Imagery | TERRAIN: Terrarium elevation pilot | WGS84 gameplay\n" +
            "Modern imagery/elevation = visuel reference, ikke historisk 1851-sandhed\n" +
            "1851 byer, regioner, infrastruktur og hære er separate gameplay-lag",
            mapInfoStyle);
    }
}
