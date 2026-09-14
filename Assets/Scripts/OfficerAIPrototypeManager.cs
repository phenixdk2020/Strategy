using System.Reflection;
using UnityEngine;

public enum OfficerAIDifficulty
{
    Easy,
    Normal,
    Hard
}

[DefaultExecutionOrder(-10000)]
public sealed class OfficerAIPrototypeManager : MonoBehaviour
{
    public static OfficerAIPrototypeManager Instance { get; private set; }
    public static OfficerAIDifficulty CurrentDifficulty { get; private set; } = OfficerAIDifficulty.Normal;

    private bool installed;
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<OfficerAIPrototypeManager>() != null)
            return;

        GameObject managerObject = new GameObject("OfficerAIPrototypeManager_v00.00.09f16");
        managerObject.AddComponent<OfficerAIPrototypeManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!installed)
            TryInstallOfficerAI();
        if (!installed)
            return;

        if (Input.GetKeyDown(KeyCode.F6)) SetDifficulty(OfficerAIDifficulty.Easy);
        if (Input.GetKeyDown(KeyCode.F7)) SetDifficulty(OfficerAIDifficulty.Normal);
        if (Input.GetKeyDown(KeyCode.F8)) SetDifficulty(OfficerAIDifficulty.Hard);
        if (Input.GetKeyDown(KeyCode.I)) ToggleSelectedAI();
    }

    private void TryInstallOfficerAI()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count == 0)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            DisableLegacyRegimentAI(regiment);
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null)
                controller = regiment.gameObject.AddComponent<OfficerAIController>();

            bool enemyControlled = regiment.Team == BattleTeam.Prussia;
            Vector3? initialWaypoint = null;
            if (regiment.RegimentName == "18th Regiment")
                initialWaypoint = new Vector3(18f, 0f, 72f);

            controller.Configure(enemyControlled, initialWaypoint);
        }

        installed = true;
        Debug.Log("AI-DIAG|System=v00.00.09f16|SharedOfficerCore=Installed|CompactBottomUI=True|Difficulty=Normal");
    }

    private static void DisableLegacyRegimentAI(Regiment regiment)
    {
        FieldInfo isAIBackingField = typeof(Regiment).GetField(
            "<IsAI>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (isAIBackingField != null)
        {
            isAIBackingField.SetValue(regiment, false);
            return;
        }

        FieldInfo legacyThinkTimer = typeof(Regiment).GetField(
            "aiThinkTimer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (legacyThinkTimer != null)
        {
            legacyThinkTimer.SetValue(regiment, float.PositiveInfinity);
            Debug.LogWarning("AI-DIAG|LegacyAI=DisabledViaTimerFallback|Unit=" + regiment.RegimentName);
            return;
        }

        Debug.LogError("AI-DIAG|LegacyAI=DisableFailed|Unit=" + regiment.RegimentName);
    }

    public static float GetReactionMultiplier(BattleTeam team)
    {
        if (team == BattleTeam.Denmark)
            return 1f;
        switch (CurrentDifficulty)
        {
            case OfficerAIDifficulty.Easy: return 1.28f;
            case OfficerAIDifficulty.Hard: return 0.84f;
            default: return 1f;
        }
    }

    public static float GetDecisionNoise(BattleTeam team)
    {
        if (team == BattleTeam.Denmark)
            return 0.10f;
        switch (CurrentDifficulty)
        {
            case OfficerAIDifficulty.Easy: return 0.18f;
            case OfficerAIDifficulty.Hard: return 0.055f;
            default: return 0.10f;
        }
    }

    private static void SetDifficulty(OfficerAIDifficulty value)
    {
        CurrentDifficulty = value;
        Debug.Log("AI-DIAG|Difficulty=" + value + "|NoCombatBonuses=True");
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (!installed || GetSelectedDanishCount() == 0)
            return false;
        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return GetControlPanelRect().Contains(guiPoint);
    }

    private static Rect GetControlPanelRect()
    {
        float width = Mathf.Min(940f, Mathf.Max(700f, Screen.width - 90f));
        const float height = 104f;
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 18f, width, height);
    }

    private void ToggleSelectedAI()
    {
        OfficerAIController first = GetFirstSelectedController();
        if (first != null)
            SetSelectedAIEnabled(!first.AIEnabled);
    }

    private void SetSelectedAIEnabled(bool enabledValue)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment)) continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null) controller.SetAIEnabled(enabledValue);
        }
    }

    private void SetSelectedDoctrine(OfficerAIDoctrine doctrine)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment)) continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null) controller.SetDoctrine(doctrine);
        }
    }

    private void SetSelectedOrderAggressiveness(float value)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment)) continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null) controller.SetOrderAggressiveness(value);
        }
    }

    private void SetSelectedFirePolicy(RegimentFirePolicy policy)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (IsSelectedDanish(regiment)) regiment.SetFirePolicy(policy);
        }
    }

    private static bool IsSelectedDanish(Regiment regiment)
    {
        return regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected;
    }

    private OfficerAIController GetFirstSelectedController()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return null;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment)) continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null) return controller;
        }
        return null;
    }

    private int GetSelectedDanishCount()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return 0;
        int count = 0;
        foreach (Regiment regiment in battle.Regiments)
            if (IsSelectedDanish(regiment)) count++;
        return count;
    }

    private Regiment GetFirstSelectedRegiment()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null) return null;
        foreach (Regiment regiment in battle.Regiments)
            if (IsSelectedDanish(regiment)) return regiment;
        return null;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        labelStyle = PrototypeUiTheme09F15.Label(9);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
    }

    private static string DisplayUnitName(Regiment regiment)
    {
        if (regiment == null) return "KOMPAGNI";
        if (regiment.RegimentName == "1. Regiment") return "1. KOMPAGNI";
        if (regiment.RegimentName == "5. Regiment") return "2. KOMPAGNI";
        return regiment.RegimentName.ToUpperInvariant();
    }

    private void OnGUI()
    {
        if (!installed) return;
        EnsureStyles();

        int selectedCount = GetSelectedDanishCount();
        if (selectedCount == 0) return;

        Regiment firstRegiment = GetFirstSelectedRegiment();
        OfficerAIController firstController = GetFirstSelectedController();
        if (firstRegiment == null || firstController == null || firstController.Officer == null) return;

        GUI.depth = -840;
        Rect panel = GetControlPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);
        string title = selectedCount > 1
            ? selectedCount + " KOMPAGNIER VALGT"
            : DisplayUnitName(firstRegiment) + " | KAPTAJN";
        GUI.Box(new Rect(panel.x + 7f, panel.y + 6f, panel.width - 14f, 22f), title, headerStyle);

        string aiText = firstController.AIEnabled ? "AI: ON" : "AI: OFF";
        if (GUI.Button(new Rect(panel.xMax - 96f, panel.y + 7f, 82f, 20f), aiText,
            firstController.AIEnabled ? accentStyle : buttonStyle))
            SetSelectedAIEnabled(!firstController.AIEnabled);

        float leftX = panel.x + 12f;
        float y = panel.y + 35f;
        const float leftWidth = 284f;
        int losses = Mathf.Max(0, firstRegiment.InitialStrength - firstRegiment.CurrentStrength);
        GUI.Label(new Rect(leftX, y, leftWidth, 16f),
            "Styrke " + firstRegiment.CurrentStrength + "/" + firstRegiment.InitialStrength +
            "  |  tab " + losses + "  |  " + firstRegiment.Formation, labelStyle);
        y += 17f;
        GUI.Label(new Rect(leftX, y, leftWidth, 16f),
            "Moral " + firstRegiment.Morale.ToString("0") +
            "  |  Cohesion " + firstRegiment.Cohesion.ToString("0") +
            "  |  Agg " + firstController.OrderAggressiveness.ToString("0"), labelStyle);
        y += 17f;
        GUI.Label(new Rect(leftX, y, leftWidth, 16f),
            "I = AI  |  Z/X = facing  |  F/C = Line/Column  |  T = range", mutedStyle);

        float commandX = panel.x + 304f;
        float commandWidth = panel.xMax - commandX - 10f;
        const float gap = 5f;
        const float buttonHeight = 29f;
        float rowY = panel.y + 35f;
        float topWidth = (commandWidth - gap * 5f) / 6f;

        if (GUI.Button(new Rect(commandX, rowY, topWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF", buttonStyle))
            SetSelectedDoctrine(OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(commandX + (topWidth + gap), rowY, topWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL", buttonStyle))
            SetSelectedDoctrine(OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(commandX + (topWidth + gap) * 2f, rowY, topWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF", buttonStyle))
            SetSelectedDoctrine(OfficerAIDoctrine.Offensive);
        if (GUI.Button(new Rect(commandX + (topWidth + gap) * 3f, rowY, topWidth, buttonHeight), "<15", buttonStyle) && PlayerCommander.Instance != null)
            PlayerCommander.Instance.RotateSelectedFacing(-15f);
        if (GUI.Button(new Rect(commandX + (topWidth + gap) * 4f, rowY, topWidth, buttonHeight), "15>", buttonStyle) && PlayerCommander.Instance != null)
            PlayerCommander.Instance.RotateSelectedFacing(15f);
        if (GUI.Button(new Rect(commandX + (topWidth + gap) * 5f, rowY, topWidth, buttonHeight), "KAMP F9", buttonStyle) && PrototypeCombatTuningManager.Instance != null)
            PrototypeCombatTuningManager.Instance.TogglePanel();

        rowY += buttonHeight + gap;
        float fireWidth = (commandWidth - gap * 3f) / 4f;
        if (GUI.Button(new Rect(commandX, rowY, fireWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD", buttonStyle))
            SetSelectedFirePolicy(RegimentFirePolicy.HoldFire);
        if (GUI.Button(new Rect(commandX + fireWidth + gap, rowY, fireWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[CLOSE]" : "CLOSE", buttonStyle))
            SetSelectedFirePolicy(RegimentFirePolicy.CloseRange);
        if (GUI.Button(new Rect(commandX + (fireWidth + gap) * 2f, rowY, fireWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MED]" : "MED", buttonStyle))
            SetSelectedFirePolicy(RegimentFirePolicy.MediumRange);
        if (GUI.Button(new Rect(commandX + (fireWidth + gap) * 3f, rowY, fireWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LONG]" : "LONG", buttonStyle))
            SetSelectedFirePolicy(RegimentFirePolicy.LongRange);

        // Keep the aggression control compact and aligned with the shared bottom-panel design.
        float sliderWidth = Mathf.Min(180f, leftWidth - 74f);
        float newAggression = GUI.HorizontalSlider(
            new Rect(leftX + 72f, panel.yMax - 13f, sliderWidth, 12f),
            firstController.OrderAggressiveness, 0f, 100f);
        if (Mathf.Abs(newAggression - firstController.OrderAggressiveness) >= 0.5f)
            SetSelectedOrderAggressiveness(newAggression);
    }
}

public static class BattleManagerOfficerAIExtensions
{
    public static float GetAIReactionMultiplier(this BattleManager manager, BattleTeam team)
    {
        return OfficerAIPrototypeManager.GetReactionMultiplier(team);
    }

    public static float GetAIDecisionNoise(this BattleManager manager, BattleTeam team)
    {
        return OfficerAIPrototypeManager.GetDecisionNoise(team);
    }
}
