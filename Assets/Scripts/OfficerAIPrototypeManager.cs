using System;
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
    private Camera cam;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<OfficerAIPrototypeManager>() != null)
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

        if (Input.GetKeyDown(KeyCode.A))
            ToggleSelectedAI();

        if (Input.GetKeyDown(KeyCode.H))
            SetSelectedHoldMission();

        if (Input.GetMouseButtonDown(1))
            CaptureSelectedAIMissionFromRightClick();
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
                initialWaypoint = new Vector3(20f, 0f, 32f);

            controller.Configure(enemyControlled, initialWaypoint);
        }

        installed = true;
        Debug.Log("AI-DIAG|System=v00.00.09|SharedOfficerCore=Installed|Difficulty=Normal|PlayerTeam=Denmark");
    }

    private static void DisableLegacyRegimentAI(Regiment regiment)
    {
        // v00.00.08 embedded a very small enemy-only AI directly in Regiment.
        // v00.00.09 moves AI to OfficerAIController. Until Regiment is refactored in
        // the next cleanup pass, disable the old private flag safely at runtime.
        FieldInfo isAIBackingField = typeof(Regiment).GetField(
            "<IsAI>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (isAIBackingField != null)
        {
            isAIBackingField.SetValue(regiment, false);
            return;
        }

        // Defensive fallback: prevent the legacy think loop from firing if a future
        // compiler changes the auto-property backing-field name.
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
        // Difficulty affects only the computer opponent in this Denmark-player P0A.
        // Delegated Danish officers always use their actual profile at reference speed.
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

    private void ToggleSelectedAI()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.ToggleAI();
        }
    }

    private void SetSelectedHoldMission()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetHoldMission();
        }
    }

    private void CaptureSelectedAIMissionFromRightClick()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        bool hasDelegatedSelection = false;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
            {
                hasDelegatedSelection = true;
                break;
            }
        }

        if (!hasDelegatedSelection)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Regiment targetRegiment = null;
        foreach (RaycastHit hit in hits)
        {
            Regiment possibleTarget = hit.collider.GetComponentInParent<Regiment>();
            if (possibleTarget != null && possibleTarget.Team == BattleTeam.Prussia)
            {
                targetRegiment = possibleTarget;
                break;
            }
        }

        if (targetRegiment != null)
        {
            foreach (Regiment regiment in battle.Regiments)
            {
                if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                    continue;

                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null && controller.AIEnabled)
                    controller.SetAttackMission(targetRegiment);
            }
            return;
        }

        Vector3? groundPoint = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;

            groundPoint = hit.point;
            break;
        }

        if (!groundPoint.HasValue)
            return;

        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();

        int delegatedIndex = 0;
        int delegatedCount = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                delegatedCount++;
        }

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled)
                continue;

            float offset = (delegatedIndex - (delegatedCount - 1) * 0.5f) * 8f;
            controller.SetMoveMission(groundPoint.Value + right * offset);
            delegatedIndex++;
        }
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
    }

    private void OnGUI()
    {
        if (!installed)
            return;

        EnsureStyles();

        GUI.Box(
            new Rect(Screen.width * 0.5f - 230f, 48f, 460f, 28f),
            "v00.00.09 OFFICER AI TEST   Difficulty: " + CurrentDifficulty + "   [F6/F7/F8]",
            titleStyle);

        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        int row = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !regiment.IsSelected || regiment.Team != BattleTeam.Denmark)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null || controller.Officer == null)
                continue;

            string text = string.Format(
                "{0} | AI {1} | {2}\n{3}\nTask: {4} | Reason: {5}",
                regiment.RegimentName,
                controller.AIEnabled ? "ON" : "OFF",
                controller.Officer.OfficerName,
                controller.Officer.CompactSummary,
                controller.CurrentTask,
                controller.ReasonCode);

            GUI.Box(new Rect(410f, Screen.height - 116f - row * 82f, 520f, 76f), text, panelStyle);
            row++;
        }

        GUI.Box(
            new Rect(Screen.width - 330f, Screen.height - 116f, 320f, 106f),
            "OFFICER AI\nA = AI UNIT ON/OFF for selected Danish units\nRight-click while AI ON = mission move/attack\nH = Hold mission | F6 Easy | F7 Normal | F8 Hard\nEnemy + delegated units use same officer decision core",
            panelStyle);
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
