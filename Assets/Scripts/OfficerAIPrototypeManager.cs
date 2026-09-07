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
        const float width = 382f;
        const float height = 300f;
        return new Rect(Screen.width - width - 10f, Screen.height - height - 10f, width, height);
    }

    private void ToggleSelectedAI()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

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
        panelStyle.fontSize = 12;
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.wordWrap = true;
        panelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 13;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 11;
        labelStyle.normal.textColor = Color.white;
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

        float x = panel.x + 10f;
        float y = panel.y + 8f;
        float w = panel.width - 20f;

        GUI.Label(
            new Rect(x, y, w, 20f),
            selectedCount > 1
                ? "ORDRER - " + selectedCount + " valgte danske regimenter"
                : "ORDRER - " + firstRegiment.RegimentName,
            labelStyle);
        y += 22f;

        string officerLine = string.Format(
            "{0} | AI {1} | Task {2}",
            firstController.Officer.OfficerName,
            firstController.AIEnabled ? "ON" : "OFF",
            firstController.CurrentTask);
        GUI.Label(new Rect(x, y, w, 20f), officerLine, labelStyle);
        y += 24f;

        if (GUI.Button(new Rect(x, y, 110f, 26f), firstController.AIEnabled ? "AI: ON [I]" : "AI: OFF [I]"))
            SetSelectedAIEnabled(!firstController.AIEnabled);

        GUI.Label(
            new Rect(x + 120f, y + 3f, w - 120f, 22f),
            "Officer: " + firstController.Officer.CompactSummary,
            labelStyle);
        y += 34f;

        GUI.Label(new Rect(x, y, w, 20f), "Officer AI doctrine:", labelStyle);
        y += 20f;

        if (GUI.Button(new Rect(x, y, 112f, 25f), firstController.Doctrine == OfficerAIDoctrine.Defensive ? "[DEFENSIV]" : "DEFENSIV"))
            SetSelectedDoctrine(OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(x + 120f, y, 112f, 25f), firstController.Doctrine == OfficerAIDoctrine.Balanced ? "[BALANCERET]" : "BALANCERET"))
            SetSelectedDoctrine(OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(x + 240f, y, 112f, 25f), firstController.Doctrine == OfficerAIDoctrine.Offensive ? "[OFFENSIV]" : "OFFENSIV"))
            SetSelectedDoctrine(OfficerAIDoctrine.Offensive);
        y += 34f;

        GUI.Label(
            new Rect(x, y, w, 20f),
            "Ordre-intent: 0 forsigtig  <---->  100 aggressiv   [" + firstController.OrderAggressiveness.ToString("0") + "]",
            labelStyle);
        y += 20f;

        float newAggression = GUI.HorizontalSlider(
            new Rect(x + 6f, y, w - 12f, 18f),
            firstController.OrderAggressiveness,
            0f,
            100f);

        if (Mathf.Abs(newAggression - firstController.OrderAggressiveness) >= 0.5f)
            SetSelectedOrderAggressiveness(newAggression);
        y += 28f;

        GUI.Label(
            new Rect(x, y, w, 20f),
            "Åbn ild når fjenden er inden for:",
            labelStyle);
        y += 20f;

        if (GUI.Button(new Rect(x, y, 80f, 25f), firstRegiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD"))
            SetSelectedFirePolicy(RegimentFirePolicy.HoldFire);
        if (GUI.Button(new Rect(x + 88f, y, 80f, 25f), firstRegiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[CLOSE]" : "CLOSE"))
            SetSelectedFirePolicy(RegimentFirePolicy.CloseRange);
        if (GUI.Button(new Rect(x + 176f, y, 80f, 25f), firstRegiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MEDIUM]" : "MEDIUM"))
            SetSelectedFirePolicy(RegimentFirePolicy.MediumRange);
        if (GUI.Button(new Rect(x + 264f, y, 88f, 25f), firstRegiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LONG]" : "LONG"))
            SetSelectedFirePolicy(RegimentFirePolicy.LongRange);
        y += 31f;

        GUI.Label(
            new Rect(x, y, w, 40f),
            string.Format(
                "Range: Close <= {0:0} | Medium <= {1:0} | Long <= {2:0}   Fire arc: {3:0}° total\nOfficer stats remain dominant; doctrine/order only bias decisions.",
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
