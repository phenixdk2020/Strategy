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
    private GUIStyle labelStyle;
    private GUIStyle sectionStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<OfficerAIPrototypeManager>() != null)
            return;

        GameObject managerObject = new GameObject("OfficerAIPrototypeManager_v00.00.09");
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

        if (Input.GetKeyDown(KeyCode.F6))
            SetDifficulty(OfficerAIDifficulty.Easy);
        if (Input.GetKeyDown(KeyCode.F7))
            SetDifficulty(OfficerAIDifficulty.Normal);
        if (Input.GetKeyDown(KeyCode.F8))
            SetDifficulty(OfficerAIDifficulty.Hard);

        if (Input.GetKeyDown(KeyCode.I))
            ToggleSelectedAI();
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
        Debug.Log("AI-DIAG|System=v00.00.09|SharedOfficerCore=Installed|Scenario=DenmarkDefends-PrussiaAttacks|Difficulty=Normal");
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
            case OfficerAIDifficulty.Easy:
                return 1.28f;
            case OfficerAIDifficulty.Hard:
                return 0.84f;
            default:
                return 1f;
        }
    }

    public static float GetDecisionNoise(BattleTeam team)
    {
        if (team == BattleTeam.Denmark)
            return 0.10f;

        switch (CurrentDifficulty)
        {
            case OfficerAIDifficulty.Easy:
                return 0.18f;
            case OfficerAIDifficulty.Hard:
                return 0.055f;
            default:
                return 0.10f;
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

    private Rect GetControlPanelRect()
    {
        // Safe margins deliberately keep the bar inside the visible Game view even
        // when the Unity Game tab is zoomed slightly above 1.0x.
        const float height = 58f;
        float width = Mathf.Max(360f, Screen.width - 100f);
        return new Rect(50f, Screen.height - height - 42f, width, height);
    }

    private void ToggleSelectedAI()
    {
        OfficerAIController first = GetFirstSelectedController();
        if (first == null)
            return;

        SetSelectedAIEnabled(!first.AIEnabled);
    }

    private void SetSelectedAIEnabled(bool enabledValue)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetAIEnabled(enabledValue);
        }
    }

    private void SetSelectedDoctrine(OfficerAIDoctrine doctrine)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetDoctrine(doctrine);
        }
    }

    private void SetSelectedOrderAggressiveness(float value)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetOrderAggressiveness(value);
        }
    }

    private void SetSelectedFirePolicy(RegimentFirePolicy policy)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment))
                continue;

            regiment.SetFirePolicy(policy);
        }
    }

    private static bool IsSelectedDanish(Regiment regiment)
    {
        return regiment != null &&
               regiment.Team == BattleTeam.Denmark &&
               regiment.IsSelected;
    }

    private OfficerAIController GetFirstSelectedController()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsSelectedDanish(regiment))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                return controller;
        }

        return null;
    }

    private int GetSelectedDanishCount()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return 0;

        int count = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (IsSelectedDanish(regiment))
                count++;
        }

        return count;
    }

    private Regiment GetFirstSelectedRegiment()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (IsSelectedDanish(regiment))
                return regiment;
        }

        return null;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 8;
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 8;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;

        sectionStyle = new GUIStyle(labelStyle);
        sectionStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        if (!installed)
            return;

        EnsureStyles();

        int selectedCount = GetSelectedDanishCount();
        if (selectedCount == 0)
            return;

        Regiment firstRegiment = GetFirstSelectedRegiment();
        OfficerAIController firstController = GetFirstSelectedController();
        if (firstRegiment == null || firstController == null || firstController.Officer == null)
            return;

        Rect panel = GetControlPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);

        float x = panel.x + 4f;
        float y = panel.y + 4f;
        float w = panel.width - 8f;
        const float gap = 3f;
        const float buttonHeight = 23f;

        // Row 1: compact unit summary and all action buttons.
        float infoWidth = Mathf.Clamp(w * 0.18f, 120f, 240f);
        string unitLine = selectedCount > 1
            ? selectedCount + " valgte | " + firstController.Officer.OfficerName
            : firstRegiment.RegimentName + " | " + firstController.Officer.OfficerName;
        GUI.Label(new Rect(x, y, infoWidth, buttonHeight), unitLine, sectionStyle);

        float controlX = x + infoWidth + gap;
        float controlWidth = Mathf.Max(224f, w - infoWidth - gap);
        float topButtonWidth = Mathf.Max(32f, (controlWidth - gap * 6f) / 7f);

        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), firstController.AIEnabled ? "AI ON" : "AI OFF"))
            SetSelectedAIEnabled(!firstController.AIEnabled);

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF"))
            SetSelectedDoctrine(OfficerAIDoctrine.Defensive);

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL"))
            SetSelectedDoctrine(OfficerAIDoctrine.Balanced);

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), firstController.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF"))
            SetSelectedDoctrine(OfficerAIDoctrine.Offensive);

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), "<15 [Z]"))
        {
            if (PlayerCommander.Instance != null)
                PlayerCommander.Instance.RotateSelectedFacing(-15f);
        }

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), "15> [X]"))
        {
            if (PlayerCommander.Instance != null)
                PlayerCommander.Instance.RotateSelectedFacing(15f);
        }

        controlX += topButtonWidth + gap;
        if (GUI.Button(new Rect(controlX, y, topButtonWidth, buttonHeight), "KAMP F9"))
        {
            if (PrototypeCombatTuningManager.Instance != null)
                PrototypeCombatTuningManager.Instance.TogglePanel();
        }

        // Row 2: aggression plus fire policy.
        y += buttonHeight + 4f;
        float aggressionWidth = Mathf.Clamp(w * 0.36f, 210f, 470f);
        float aggressionLabelWidth = 58f;
        GUI.Label(new Rect(x, y, aggressionLabelWidth, buttonHeight), "Agg " + firstController.OrderAggressiveness.ToString("0"), sectionStyle);

        float newAggression = GUI.HorizontalSlider(
            new Rect(x + aggressionLabelWidth, y + 7f, Mathf.Max(80f, aggressionWidth - aggressionLabelWidth), 16f),
            firstController.OrderAggressiveness,
            0f,
            100f);

        if (Mathf.Abs(newAggression - firstController.OrderAggressiveness) >= 0.5f)
            SetSelectedOrderAggressiveness(newAggression);

        float fireX = x + aggressionWidth + gap * 2f;
        float fireAvailable = Mathf.Max(160f, w - aggressionWidth - gap * 2f);
        float fireButtonWidth = Mathf.Max(34f, (fireAvailable - gap * 3f) / 4f);

        if (GUI.Button(new Rect(fireX, y, fireButtonWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD"))
            SetSelectedFirePolicy(RegimentFirePolicy.HoldFire);

        fireX += fireButtonWidth + gap;
        if (GUI.Button(new Rect(fireX, y, fireButtonWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[CLOSE]" : "CLOSE"))
            SetSelectedFirePolicy(RegimentFirePolicy.CloseRange);

        fireX += fireButtonWidth + gap;
        if (GUI.Button(new Rect(fireX, y, fireButtonWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MED]" : "MED"))
            SetSelectedFirePolicy(RegimentFirePolicy.MediumRange);

        fireX += fireButtonWidth + gap;
        if (GUI.Button(new Rect(fireX, y, fireButtonWidth, buttonHeight), firstRegiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LONG]" : "LONG"))
            SetSelectedFirePolicy(RegimentFirePolicy.LongRange);
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
