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
        const float height = 174f;
        float width = Mathf.Max(480f, Screen.width - 20f);
        return new Rect(10f, Screen.height - height - 10f, width, height);
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

        sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontSize = 11;
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = Color.white;
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
        float y = panel.y + 7f;
        float w = panel.width - 20f;

        string title = selectedCount > 1
            ? "ORDRER - " + selectedCount + " valgte danske regimenter"
            : "ORDRER - " + firstRegiment.RegimentName;

        GUI.Label(new Rect(x, y, w, 20f), title, sectionStyle);
        y += 20f;

        GUI.Label(
            new Rect(x, y, w, 20f),
            string.Format(
                "{0} | AI {1} | Task {2} | {3}",
                firstController.Officer.OfficerName,
                firstController.AIEnabled ? "ON" : "OFF",
                firstController.CurrentTask,
                firstController.Officer.CompactSummary),
            labelStyle);
        y += 24f;

        float aiWidth = 104f;
        float doctrineWidth = Mathf.Clamp((w - aiWidth - 36f) / 3f, 90f, 160f);
        float rowX = x;

        if (GUI.Button(
            new Rect(rowX, y, aiWidth, 27f),
            firstController.AIEnabled ? "AI: ON [I]" : "AI: OFF [I]"))
        {
            SetSelectedAIEnabled(!firstController.AIEnabled);
        }

        rowX += aiWidth + 12f;

        if (GUI.Button(
            new Rect(rowX, y, doctrineWidth, 27f),
            firstController.Doctrine == OfficerAIDoctrine.Defensive ? "[DEFENSIV]" : "DEFENSIV"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Defensive);
        }

        rowX += doctrineWidth + 8f;

        if (GUI.Button(
            new Rect(rowX, y, doctrineWidth, 27f),
            firstController.Doctrine == OfficerAIDoctrine.Balanced ? "[BALANCERET]" : "BALANCERET"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Balanced);
        }

        rowX += doctrineWidth + 8f;

        if (GUI.Button(
            new Rect(rowX, y, doctrineWidth, 27f),
            firstController.Doctrine == OfficerAIDoctrine.Offensive ? "[OFFENSIV]" : "OFFENSIV"))
        {
            SetSelectedDoctrine(OfficerAIDoctrine.Offensive);
        }

        y += 33f;

        GUI.Label(
            new Rect(x, y, 260f, 20f),
            "Ordre-intent: forsigtig 0  <---->  100 aggressiv",
            labelStyle);

        float valueBoxWidth = 46f;
        GUI.Box(
            new Rect(panel.xMax - valueBoxWidth - 12f, y - 2f, valueBoxWidth, 22f),
            firstController.OrderAggressiveness.ToString("0"));

        y += 20f;

        float newAggression = GUI.HorizontalSlider(
            new Rect(x + 6f, y, w - 18f, 18f),
            firstController.OrderAggressiveness,
            0f,
            100f);

        if (Mathf.Abs(newAggression - firstController.OrderAggressiveness) >= 0.5f)
            SetSelectedOrderAggressiveness(newAggression);

        y += 25f;

        GUI.Label(new Rect(x, y, 180f, 20f), "Åbn ild:", sectionStyle);

        float fireStart = x + 72f;
        float fireGap = 8f;
        float fireWidth = Mathf.Clamp((w - 72f - fireGap * 3f) / 4f, 76f, 130f);

        if (GUI.Button(
            new Rect(fireStart, y - 3f, fireWidth, 27f),
            firstRegiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.HoldFire);
        }

        fireStart += fireWidth + fireGap;

        if (GUI.Button(
            new Rect(fireStart, y - 3f, fireWidth, 27f),
            firstRegiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[CLOSE]" : "CLOSE"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.CloseRange);
        }

        fireStart += fireWidth + fireGap;

        if (GUI.Button(
            new Rect(fireStart, y - 3f, fireWidth, 27f),
            firstRegiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MEDIUM]" : "MEDIUM"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.MediumRange);
        }

        fireStart += fireWidth + fireGap;

        if (GUI.Button(
            new Rect(fireStart, y - 3f, fireWidth, 27f),
            firstRegiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LONG]" : "LONG"))
        {
            SetSelectedFirePolicy(RegimentFirePolicy.LongRange);
        }

        y += 29f;

        GUI.Label(
            new Rect(x, y, w, 20f),
            string.Format(
                "Range: Close <= {0:0} | Medium <= {1:0} | Long <= {2:0} | Fire arc {3:0}° | Officer stats er primære; doctrine/ordre er bias.",
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
