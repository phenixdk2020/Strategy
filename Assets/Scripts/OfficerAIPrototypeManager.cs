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
    private GUIStyle titleStyle;
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

        // I is deliberately used instead of A because A belongs to WASD camera movement.
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

            // The 18th demonstrates that an attacker may manoeuvre/flank before
            // committing, instead of every enemy regiment simply running straight in.
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
        // Keep the tactical command bar deliberately shallow so the battlefield
        // remains the dominant part of the screen. Two control rows + one status row.
        const float height = 96f;
        float width = Mathf.Max(480f, Screen.width - 12f);
        return new Rect(6f, Screen.height - height - 6f, width, height);
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
        panelStyle.fontSize = 11;
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 13;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
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

        GUI.Box(
            new Rect(Screen.width * 0.5f - 255f, 48f, 510f, 28f),
            "v00.00.09 TACTICAL COMMAND TEST   Difficulty: " + CurrentDifficulty + "   [F6/F7/F8]",
            titleStyle);

        int selectedCount = GetSelectedDanishCount();
        if (selectedCount == 0)
            return;

        Regiment firstRegiment = GetFirstSelectedRegiment();
        OfficerAIController firstController = GetFirstSelectedController();
        if (firstRegiment == null || firstController == null || firstController.Officer == null)
            return;

        Rect panel = GetControlPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);

        float x = panel.x + 6f;
        float y = panel.y + 5f;
        float w = panel.width - 12f;
        float gap = 5f;

        // Row 1: selected formation + AI/doctrine. The information block is kept
        // deliberately short; detailed unit data remains on hover/selection near the unit.
        float infoWidth = Mathf.Clamp(w * 0.34f, 210f, 360f);
        string unitLine = selectedCount > 1
            ? selectedCount + " valgte | " + firstController.Officer.OfficerName + " | Task " + firstController.CurrentTask
            : firstRegiment.RegimentName + " | " + firstController.Officer.OfficerName + " | Task " + firstController.CurrentTask;
        GUI.Label(new Rect(x, y, infoWidth, 24f), unitLine, sectionStyle);

        float controlX = x + infoWidth + gap;
        float controlWidth = Mathf.Max(260f, w - infoWidth - gap);
        float topButtonWidth = (controlWidth - gap * 3f) / 4f;

        if (GUI.Button(
            new Rect(controlX, y, topButtonWidth, 24f),
            firstController.AIEnabled ? "AI ON [I]" : "AI OFF [I]"))
        {
            SetSelectedAIEnabled(!firstController.AIEnabled);
        }

        controlX += topButtonWidth + gap;
        if (GUI.Button(
            new Rect(controlX, y, topButtonWidth, 24f),
            firstController.Doctrine == OfficerAIDoctrine.Defensive ? "[DEFENSIV]" : "DEFENSIV"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Defensive);
        }

        controlX += topButtonWidth + gap;
        if (GUI.Button(
            new Rect(controlX, y, topButtonWidth, 24f),
            firstController.Doctrine == OfficerAIDoctrine.Balanced ? "[BALANCERET]" : "BALANCERET"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Balanced);
        }

        controlX += topButtonWidth + gap;
        if (GUI.Button(
            new Rect(controlX, y, topButtonWidth, 24f),
            firstController.Doctrine == OfficerAIDoctrine.Offensive ? "[OFFENSIV]" : "OFFENSIV"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Offensive);
        }

        // Row 2: aggression intent on the left and fire discipline on the right.
        y += 29f;
        float aggressionWidth = Mathf.Clamp(w * 0.46f, 300f, 560f);
        float aggressionLabelWidth = 105f;
        GUI.Label(
            new Rect(x, y, aggressionLabelWidth, 24f),
            "Aggression " + firstController.OrderAggressiveness.ToString("0"),
            sectionStyle);

        float newAggression = GUI.HorizontalSlider(
            new Rect(x + aggressionLabelWidth, y + 7f, aggressionWidth - aggressionLabelWidth, 18f),
            firstController.OrderAggressiveness,
            0f,
            100f);

        if (Mathf.Abs(newAggression - firstController.OrderAggressiveness) >= 0.5f)
            SetSelectedOrderAggressiveness(newAggression);

        float fireX = x + aggressionWidth + gap * 2f;
        float fireAvailable = Mathf.Max(280f, w - aggressionWidth - gap * 2f);
        float fireLabelWidth = 42f;
        float fireButtonWidth = (fireAvailable - fireLabelWidth - gap * 3f) / 4f;
        GUI.Label(new Rect(fireX, y, fireLabelWidth, 24f), "Ild:", sectionStyle);
        fireX += fireLabelWidth;

        if (GUI.Button(
            new Rect(fireX, y, fireButtonWidth, 24f),
            firstRegiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.HoldFire);
        }

        fireX += fireButtonWidth + gap;
        if (GUI.Button(
            new Rect(fireX, y, fireButtonWidth, 24f),
            firstRegiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[CLOSE]" : "CLOSE"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.CloseRange);
        }

        fireX += fireButtonWidth + gap;
        if (GUI.Button(
            new Rect(fireX, y, fireButtonWidth, 24f),
            firstRegiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MEDIUM]" : "MEDIUM"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.MediumRange);
        }

        fireX += fireButtonWidth + gap;
        if (GUI.Button(
            new Rect(fireX, y, fireButtonWidth, 24f),
            firstRegiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LONG]" : "LONG"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.LongRange);
        }

        // Row 3 is information only and uses very little vertical space.
        y += 28f;
        GUI.Label(
            new Rect(x, y, w, 18f),
            string.Format(
                "{0} | {1} | Range C {2:0} / M {3:0} / L {4:0} | Arc {5:0}° | Officer stats er primære; doctrine/ordre er bias",
                firstController.Officer.CompactSummary,
                firstController.AIEnabled ? "AI ON" : "AI OFF",
                firstRegiment.CloseRange,
                firstRegiment.EffectiveRange,
                firstRegiment.MaximumRange,
                firstRegiment.FireArcHalfAngle * 2f),
            labelStyle);
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
