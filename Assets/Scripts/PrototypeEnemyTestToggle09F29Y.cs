using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29y
// TEST-only enemy AI toggle replacement.
// Enemies start with Officer AI OFF. The compact panel is collapsed by default and each
// button is a true ON/OFF toggle instead of a one-way Start action.
[DefaultExecutionOrder(127000)]
public sealed class PrototypeEnemyTestToggle09F29Y : MonoBehaviour
{
    private const float PanelWidth = 188f;
    private const float PanelXMargin = 8f;
    private const float PanelY = 64f;
    private const float HeaderHeight = 25f;
    private const float RowHeight = 25f;
    private const float Gap = 3f;

    private bool open;
    private bool startupOffApplied;
    private PrototypeEnemyTestPanel09F29S legacyPanel;
    private FieldInfo legacyOpenField;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeStyle;
    private GUIStyle tinyStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeEnemyTestToggle09F29Y>() == null)
            new GameObject("PrototypeEnemyTestToggle_v000009f29y")
                .AddComponent<PrototypeEnemyTestToggle09F29Y>();
    }

    private void Awake()
    {
        open = false;
        legacyOpenField = typeof(PrototypeEnemyTestPanel09F29S)
            .GetField("open", BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "ENEMY-TEST-09F29Y|Installed=True|DefaultAI=OFF|Panel=Collapsed|" +
            "Buttons=TrueToggleONOFF|LegacyF29SPanel=Suppress");
    }

    private void Update()
    {
        EnsureLegacyPanelSuppressed();
        SyncLegacyPointerRect();

        if (!startupOffApplied)
            TryApplyStartupOff();
    }

    private void EnsureLegacyPanelSuppressed()
    {
        if (legacyPanel == null)
            legacyPanel = UnityEngine.Object.FindAnyObjectByType<PrototypeEnemyTestPanel09F29S>();

        if (legacyPanel != null && legacyPanel.enabled)
            legacyPanel.enabled = false;
    }

    private void SyncLegacyPointerRect()
    {
        // Several older input layers already ask the F29S panel whether the pointer is
        // above TEST UI. Keep its private open state synchronized even while its OnGUI is
        // disabled, so expanded rows still block battlefield clicks underneath.
        if (legacyPanel != null && legacyOpenField != null)
            legacyOpenField.SetValue(legacyPanel, open);
    }

    private void TryApplyStartupOff()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int enemies = 0;
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.Team != BattleTeam.Prussia)
                continue;

            enemies++;
            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || controller.Officer == null)
                return; // OfficerAIPrototypeManager has not finished configuring the scene.
        }

        if (enemies == 0)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.Team != BattleTeam.Prussia)
                continue;

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetAIEnabled(false);
            unit.OrderHold();
        }

        startupOffApplied = true;
        Debug.Log("ENEMY-TEST-09F29Y|StartupDefaultApplied=True|EnemyAI=OFF|Enemies=" + enemies);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -127000;

        Rect panel = GetPanelRect();
        Color oldColor = GUI.color;
        GUI.color = new Color(0.03f, 0.04f, 0.032f, 0.97f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = oldColor;

        Rect header = new Rect(panel.x, panel.y, panel.width, HeaderHeight);
        GUI.Label(new Rect(header.x + 7f, header.y + 1f, header.width - 42f, header.height - 2f),
            "TEST FJENDE", headerStyle);

        if (GUI.Button(new Rect(header.xMax - 30f, header.y + 2f, 24f, 21f),
            open ? "−" : "+", tinyStyle))
        {
            open = !open;
            SyncLegacyPointerRect();
            ConsumePointer();
        }

        if (!open)
            return;

        float y = header.yMax + Gap;
        DrawEnemyToggle(new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), 1);
        y += RowHeight + Gap;
        DrawEnemyToggle(new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), 2);
    }

    private Rect GetPanelRect()
    {
        float height = HeaderHeight;
        if (open)
            height += Gap + RowHeight * 2f + Gap * 2f;

        return new Rect(
            Mathf.Max(8f, Screen.width - PanelWidth - PanelXMargin),
            PanelY,
            PanelWidth,
            height);
    }

    private void DrawEnemyToggle(Rect rect, int number)
    {
        Regiment enemy = ResolveEnemy(number);
        OfficerAIController controller = enemy != null ? enemy.GetComponent<OfficerAIController>() : null;
        bool active = controller != null && controller.AIEnabled;
        string label = active
            ? "Stop Fjende " + number + "  [AI ON]"
            : "Start Fjende " + number + "  [AI OFF]";

        if (!GUI.Button(rect, label, active ? activeStyle : buttonStyle))
            return;

        ToggleEnemy(number, enemy, controller, active);
        ConsumePointer();
    }

    private static void ToggleEnemy(
        int number,
        Regiment enemy,
        OfficerAIController controller,
        bool active)
    {
        if (enemy == null || enemy.IsRouted || enemy.CurrentStrength <= 0)
        {
            Debug.LogWarning("ENEMY-TEST-09F29Y|Enemy=" + number + "|Toggle=False|Reason=EnemyUnavailable");
            return;
        }

        if (controller == null || controller.Officer == null)
        {
            Debug.LogWarning("ENEMY-TEST-09F29Y|Enemy=" + number + "|Toggle=False|Reason=OfficerAINotReady");
            return;
        }

        if (active)
        {
            controller.SetAIEnabled(false);
            enemy.OrderHold();
            Debug.Log("ENEMY-TEST-09F29Y|Enemy=" + number + "|AI=OFF|Unit=" + enemy.RegimentName);
            return;
        }

        controller.SetDoctrine(OfficerAIDoctrine.Offensive);
        controller.SetAIEnabled(true);
        Debug.Log("ENEMY-TEST-09F29Y|Enemy=" + number + "|AI=ON|Unit=" + enemy.RegimentName);
    }

    private static Regiment ResolveEnemy(int number)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        string preferred = number == 1 ? "8th Regiment" : "18th Regiment";
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit != null && unit.Team == BattleTeam.Prussia && unit.RegimentName == preferred)
                return unit;
        }

        List<Regiment> enemies = new List<Regiment>();
        foreach (Regiment unit in battle.Regiments)
            if (unit != null && unit.Team == BattleTeam.Prussia)
                enemies.Add(unit);

        enemies.Sort((a, b) => string.CompareOrdinal(a.RegimentName, b.RegimentName));
        int index = number - 1;
        return index >= 0 && index < enemies.Count ? enemies[index] : null;
    }

    private void EnsureStyles()
    {
        if (headerStyle != null)
            return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = new Color(0.95f, 0.72f, 0.20f, 1f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        buttonStyle.normal.textColor = new Color(0.90f, 0.91f, 0.84f, 1f);

        activeStyle = new GUIStyle(buttonStyle);
        activeStyle.normal.textColor = new Color(0.65f, 0.95f, 0.62f, 1f);

        tinyStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private static void ConsumePointer()
    {
        Event current = Event.current;
        if (current != null &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp))
            current.Use();
    }
}
