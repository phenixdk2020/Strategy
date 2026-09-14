using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 HUD visibility controller.
/// Keeps the essential campaign bar visible while allowing large information/debug
/// panels to be hidden. Preferences persist through PlayerPrefs.
///
/// v10n4 changes the default so the large legacy selection/info panel starts hidden.
/// The compact contextual zone card remains the normal map-click information view.
///
/// F1 = toggle all non-essential HUD panels
/// F2 = toggle legacy selection/info panel
/// F3 = toggle technical/debug panels
/// F4 = reset HUD preferences to the current clean default
/// </summary>
[DefaultExecutionOrder(-25000)]
public sealed class CampaignHudStateV010N2 : MonoBehaviour
{
    private const string KeyHudVisible = "PROJECT1864.Campaign3.HudVisible";
    private const string KeySelection = "PROJECT1864.Campaign3.SelectionPanel";
    private const string KeyDebug = "PROJECT1864.Campaign3.DebugPanels";
    private const string KeyV10N4DefaultsApplied = "PROJECT1864.Campaign3.V10N4DefaultsApplied";

    private static bool loaded;
    private static bool hudVisible = true;
    private static bool selectionPanel;
    private static bool debugPanels;

    public static bool HudVisible
    {
        get { EnsureLoaded(); return hudVisible; }
    }

    public static bool SelectionVisible
    {
        get { EnsureLoaded(); return hudVisible && selectionPanel; }
    }

    public static bool DebugVisible
    {
        get { EnsureLoaded(); return hudVisible && debugPanels; }
    }

    public static bool SelectionEnabled
    {
        get { EnsureLoaded(); return selectionPanel; }
    }

    public static bool DebugEnabled
    {
        get { EnsureLoaded(); return debugPanels; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        EnsureLoaded();
        if (Object.FindAnyObjectByType<CampaignHudStateV010N2>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_HUD_STATE_CURRENT");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignHudStateV010N2>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            hudVisible = !hudVisible;
            Save();
            LogState("F1");
        }

        if (Input.GetKeyDown(KeyCode.F2))
            ToggleSelectionPanel();

        if (Input.GetKeyDown(KeyCode.F3))
            ToggleDebugPanels();

        if (Input.GetKeyDown(KeyCode.F4))
            ResetPreferences();
    }

    public static void ToggleSelectionPanel()
    {
        EnsureLoaded();

        if (!hudVisible)
        {
            hudVisible = true;
            selectionPanel = true;
        }
        else
        {
            selectionPanel = !selectionPanel;
        }

        Save();
        LogState("F2/INFO");
    }

    public static void ToggleDebugPanels()
    {
        EnsureLoaded();

        if (!hudVisible)
        {
            hudVisible = true;
            debugPanels = true;
        }
        else
        {
            debugPanels = !debugPanels;
        }

        Save();
        LogState("F3/DEBUG");
    }

    public static void ToggleHud()
    {
        EnsureLoaded();
        hudVisible = !hudVisible;
        Save();
        LogState("HUD");
    }

    public static void ResetPreferences()
    {
        loaded = true;
        hudVisible = true;
        selectionPanel = false;
        debugPanels = false;
        PlayerPrefs.SetInt(KeyV10N4DefaultsApplied, 1);
        Save();
        LogState("F4/RESET");
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        hudVisible = PlayerPrefs.GetInt(KeyHudVisible, 1) != 0;
        selectionPanel = PlayerPrefs.GetInt(KeySelection, 0) != 0;
        debugPanels = PlayerPrefs.GetInt(KeyDebug, 0) != 0;

        // One-time migration for users who already had the n2/n3 INFO panel stored
        // as visible. The first n4 run deliberately starts with both large legacy
        // INFO and technical debug panels closed.
        if (PlayerPrefs.GetInt(KeyV10N4DefaultsApplied, 0) == 0)
        {
            hudVisible = true;
            selectionPanel = false;
            debugPanels = false;
            PlayerPrefs.SetInt(KeyV10N4DefaultsApplied, 1);
            Save();
            LogState("V10N4_DEFAULT_MIGRATION");
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(KeyHudVisible, hudVisible ? 1 : 0);
        PlayerPrefs.SetInt(KeySelection, selectionPanel ? 1 : 0);
        PlayerPrefs.SetInt(KeyDebug, debugPanels ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void LogState(string source)
    {
        Debug.Log(
            CampaignBuildInfo.LogTag + "|HUD=True|Source=" + source +
            "|Visible=" + hudVisible +
            "|Selection=" + selectionPanel +
            "|Debug=" + debugPanels);
    }
}
