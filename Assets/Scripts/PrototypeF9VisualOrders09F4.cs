using System.Reflection;
using UnityEngine;

// v00.00.09f4 companion section for the existing KAMP F9 tuning panel.
// It is only visible while PrototypeCombatTuningManager's F9 panel is open and
// extends that panel with living/fallen display ratios and explicit tactical orders.
[DefaultExecutionOrder(-8800)]
public sealed class PrototypeF9VisualOrders09F4 : MonoBehaviour
{
    private FieldInfo showPanelField;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private bool commanderSuppressed;
    private bool boxSelectionSuppressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeF9VisualOrders09F4>() != null)
            return;

        GameObject root = new GameObject("PrototypeF9VisualOrders_v000009f4");
        root.AddComponent<PrototypeF9VisualOrders09F4>();
    }

    private void Awake()
    {
        showPanelField = typeof(PrototypeCombatTuningManager).GetField(
            "showPanel",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log("UI-09F4|F9Extension=True|VisualScaleControls=True|OrderButtons=True");
    }

    private void Update()
    {
        if (!IsCombatPanelVisible())
        {
            RestorePointerHandlers();
            return;
        }

        Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (GetExtensionRect().Contains(guiPoint))
            SuppressPointerHandlers();
        else
            RestorePointerHandlers();
    }

    private void OnDisable()
    {
        RestorePointerHandlers();
    }

    private bool IsCombatPanelVisible()
    {
        PrototypeCombatTuningManager manager = PrototypeCombatTuningManager.Instance;
        if (manager == null || showPanelField == null)
            return false;

        object value = showPanelField.GetValue(manager);
        return value is bool && (bool)value;
    }

    private static Rect GetMainCombatRect()
    {
        float width = Mathf.Min(460f, Mathf.Max(350f, Screen.width - 80f));
        return new Rect((Screen.width - width) * 0.5f, 102f, width, 346f);
    }

    private static Rect GetExtensionRect()
    {
        Rect main = GetMainCombatRect();
        const float extensionHeight = 176f;

        if (main.yMax + extensionHeight + 8f <= Screen.height)
            return new Rect(main.x, main.yMax + 4f, main.width, extensionHeight);

        float rightX = main.xMax + 4f;
        if (rightX + main.width <= Screen.width - 4f)
            return new Rect(rightX, main.y, main.width, extensionHeight);

        return new Rect(main.x, Mathf.Max(4f, main.y - extensionHeight - 4f), main.width, extensionHeight);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 10;
        panelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 12;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;

        valueStyle = new GUIStyle(labelStyle);
        valueStyle.alignment = TextAnchor.MiddleRight;
        valueStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        if (!IsCombatPanelVisible())
            return;

        EnsureStyles();
        Rect panel = GetExtensionRect();
        GUI.Box(panel, string.Empty, panelStyle);

        float x = panel.x + 10f;
        float y = panel.y + 6f;
        float width = panel.width - 20f;

        GUI.Label(new Rect(x, y, width, 20f), "VISNING / TAKTISKE ORDRER  —  v00.00.09f4", titleStyle);
        y += 25f;

        Regiment selected = GetFirstSelectedDanish();
        string liveInfo = selected == null
            ? "ingen enhed valgt"
            : selected.CurrentStrength + " mænd → " + PrototypeBattleVisuals09F4.GetVisibleLivingCount(selected) + " modeller";
        string fallenInfo = selected == null
            ? "ingen enhed valgt"
            : Mathf.Max(0, selected.InitialStrength - selected.CurrentStrength) + " tab → " + PrototypeBattleVisuals09F4.GetVisibleFallenCount(selected) + " faldne";

        GUI.Label(new Rect(x, y, 150f, 22f), "Vis soldater", labelStyle);
        if (GUI.Button(new Rect(x + 150f, y, 86f, 22f), "1 : " + PrototypeBattleVisuals09F4.SoldierDisplayRatio))
            PrototypeBattleVisuals09F4.CycleSoldierDisplayRatio();
        GUI.Label(new Rect(x + 242f, y, width - 242f, 22f), liveInfo, valueStyle);
        y += 25f;

        GUI.Label(new Rect(x, y, 150f, 22f), "Vis faldne", labelStyle);
        if (GUI.Button(new Rect(x + 150f, y, 86f, 22f), "1 : " + PrototypeBattleVisuals09F4.CasualtyDisplayRatio))
            PrototypeBattleVisuals09F4.CycleCasualtyDisplayRatio();
        GUI.Label(new Rect(x + 242f, y, width - 242f, 22f), fallenInfo, valueStyle);
        y += 28f;

        float buttonGap = 5f;
        float buttonWidth = (width - buttonGap * 2f) / 3f;

        if (GUI.Button(new Rect(x, y, buttonWidth, 28f), "FORSVAR HER"))
            BeginOrder(PrototypeTacticalOrderMode09F4.DefendHere);

        if (GUI.Button(new Rect(x + buttonWidth + buttonGap, y, buttonWidth, 28f), "ANGRIB"))
            BeginOrder(PrototypeTacticalOrderMode09F4.Attack);

        if (GUI.Button(new Rect(x + (buttonWidth + buttonGap) * 2f, y, buttonWidth, 28f), "EROBR HER"))
            BeginOrder(PrototypeTacticalOrderMode09F4.CaptureHere);

        y += 33f;

        if (GUI.Button(new Rect(x, y, 148f, 24f), "VISNING = 1 : 1"))
        {
            PrototypeBattleVisuals09F4.SetSoldierDisplayRatio(1);
            PrototypeBattleVisuals09F4.SetCasualtyDisplayRatio(1);
        }

        string help = "Skala ændrer kun grafik — styrke/tab i simulationen er altid faktiske mænd.";
        GUI.Label(new Rect(x + 156f, y, width - 156f, 28f), help, labelStyle);
    }

    private static Regiment GetFirstSelectedDanish()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null &&
                regiment.Team == BattleTeam.Denmark &&
                regiment.IsSelected &&
                !regiment.IsRouted)
            {
                return regiment;
            }
        }

        return null;
    }

    private static void BeginOrder(PrototypeTacticalOrderMode09F4 mode)
    {
        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders == null)
            return;

        orders.BeginOrder(mode);

        // The command was chosen inside F9; close the tuning panel so the user can
        // immediately click the terrain/target while the pending-order banner stays visible.
        if (PrototypeCombatTuningManager.Instance != null)
            PrototypeCombatTuningManager.Instance.TogglePanel();
    }

    private void SuppressPointerHandlers()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && commander.enabled)
        {
            commander.enabled = false;
            commanderSuppressed = true;
        }

        PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
        if (box != null && box.enabled)
        {
            box.enabled = false;
            boxSelectionSuppressed = true;
        }
    }

    private void RestorePointerHandlers()
    {
        if (commanderSuppressed)
        {
            PlayerCommander commander = PlayerCommander.Instance;
            if (commander != null)
                commander.enabled = true;
            commanderSuppressed = false;
        }

        if (boxSelectionSuppressed)
        {
            PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
            if (box != null)
                box.enabled = true;
            boxSelectionSuppressed = false;
        }
    }
}
