using System.Reflection;
using UnityEngine;

// v00.00.09f2 bottom command bar and enemy start gate.
// Replaces the old raised 09f IMGUI bar after OfficerAIController installation.
[DefaultExecutionOrder(-9500)]
public sealed class PrototypeBottomCommandBar09F2 : MonoBehaviour
{
    private const float PanelHeight = 64f;
    private bool officerManagerSuppressed;
    private bool enemyGateApplied;
    private bool enemyStarted;
    private bool commanderSuppressed;
    private bool boxSelectionSuppressed;
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private GUIStyle boldStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBottomCommandBar09F2>() != null)
            return;

        GameObject root = new GameObject("PrototypeBottomCommandBar_v000009f2");
        root.AddComponent<PrototypeBottomCommandBar09F2>();
    }

    private void Update()
    {
        SuppressLegacyOfficerManagerWhenReady();
        ApplyInitialEnemyHold();
        HandleHotkeys();
        RestorePointerComponents();
    }

    private void OnDisable()
    {
        RestorePointerComponents();
    }

    private void SuppressLegacyOfficerManagerWhenReady()
    {
        if (officerManagerSuppressed)
            return;

        OfficerAIPrototypeManager manager = OfficerAIPrototypeManager.Instance;
        if (manager == null || !ControllersInstalled())
            return;

        FieldInfo installedField = typeof(OfficerAIPrototypeManager).GetField(
            "installed",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (installedField != null)
            installedField.SetValue(manager, false);

        manager.enabled = false;
        officerManagerSuppressed = true;
        Debug.Log("UI-09F2|LegacyOfficerBarDisabled=True|BottomDock=True|BottomGap=0");
    }

    private bool ControllersInstalled()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return false;

        int active = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;
            active++;
            if (regiment.GetComponent<OfficerAIController>() == null)
                return false;
        }

        return active > 0;
    }

    private void ApplyInitialEnemyHold()
    {
        if (enemyGateApplied || !ControllersInstalled())
            return;

        SetEnemyActive(false);
        enemyGateApplied = true;
        Debug.Log("ENEMY-GATE-09F2|InitialState=HOLD|AttackStarted=False|StartKey=F5");
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            SetEnemyActive(!enemyStarted);

        if (Input.GetKeyDown(KeyCode.I))
            ToggleSelectedDanishAI();
    }

    private void SetEnemyActive(bool active)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Prussia)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null)
                continue;

            if (active)
            {
                controller.SetDoctrine(OfficerAIDoctrine.Offensive);
                controller.SetAIEnabled(true);
            }
            else
            {
                controller.SetAIEnabled(false);
                regiment.OrderHold();
            }
        }

        enemyStarted = active;
        Debug.Log("ENEMY-GATE-09F2|AttackStarted=" + active + "|State=" + (active ? "ATTACK" : "HOLD"));
    }

    private void ToggleSelectedDanishAI()
    {
        OfficerAIController first = GetFirstSelectedController();
        if (first == null)
            return;

        bool newValue = !first.AIEnabled;
        ForEachSelectedDanish((regiment, controller) => controller.SetAIEnabled(newValue));
    }

    private void UpdatePointerIsolation()
    {
        Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        bool overPanel = GetPanelRect().Contains(guiPoint);

        PlayerCommander commander = PlayerCommander.Instance;
        if (overPanel)
        {
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
        else
        {
            RestorePointerComponents();
        }
    }

    private void RestorePointerComponents()
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

    private static Rect GetPanelRect()
    {
        return new Rect(0f, Screen.height - PanelHeight, Screen.width, PanelHeight);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 9;
        panelStyle.alignment = TextAnchor.UpperLeft;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 9;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;

        boldStyle = new GUIStyle(labelStyle);
        boldStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        // F30I: retired legacy bottom bar. F5 enemy gate/hotkeys remain active.
        return;
    }

    private void DrawFireButton(ref float x, float y, string label, RegimentFirePolicy policy, Regiment reference)
    {
        string text = reference.FirePolicy == policy ? "[" + label + "]" : label;
        if (GUI.Button(new Rect(x, y, 62f, 27f), text))
            ForEachSelectedDanish((r, c) => r.SetFirePolicy(policy));
        x += 65f;
    }

    private void ForEachSelectedDanish(System.Action<Regiment, OfficerAIController> action)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                action(regiment, controller);
        }
    }

    private int GetSelectedDanishCount()
    {
        int count = 0;
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return count;

        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                count++;

        return count;
    }

    private Regiment GetFirstSelectedRegiment()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                return regiment;

        return null;
    }

    private OfficerAIController GetFirstSelectedController()
    {
        Regiment regiment = GetFirstSelectedRegiment();
        return regiment != null ? regiment.GetComponent<OfficerAIController>() : null;
    }
}
