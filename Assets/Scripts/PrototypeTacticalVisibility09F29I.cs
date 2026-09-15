using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29i
// Tactical readability layer for the battle map.
// Complements the F29D semantic/NATO zoom without replacing it:
// - close-zoom HQ beacons and fixed-size labels,
// - persistent order/objective labels for Regiment and Majors,
// - selected-company facing arrow,
// - combat/under-fire alerts,
// - TAB toggles the extended tactical overlay.
[DefaultExecutionOrder(37200)]
public sealed class PrototypeTacticalVisibility09F29I : MonoBehaviour
{
    private const float SemanticHqStartHeight = 72f;
    private const float BottomHudGuard = 96f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly Color Friendly = new Color(0.22f, 0.58f, 0.92f, 1f);
    private static readonly Color Selected = new Color(1.00f, 0.80f, 0.18f, 1f);
    private static readonly Color Combat = new Color(0.96f, 0.31f, 0.15f, 1f);
    private static readonly Color Objective = new Color(0.94f, 0.74f, 0.20f, 1f);
    private static readonly Color Panel = new Color(0.035f, 0.045f, 0.040f, 0.94f);

    private Camera cam;
    private bool tacticalOverlay = true;

    private FieldInfo selectedBattalionField;
    private FieldInfo battalionsField;
    private FieldInfo currentRegimentalMissionField;

    private Texture2D whiteTexture;
    private Texture2D panelTexture;
    private GUIStyle hqStyle;
    private GUIStyle labelStyle;
    private GUIStyle orderStyle;
    private GUIStyle combatStyle;
    private GUIStyle toggleStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalVisibility09F29I>() == null)
            new GameObject("PrototypeTacticalVisibility_v000009f29i")
                .AddComponent<PrototypeTacticalVisibility09F29I>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", PrivateInstance);
        battalionsField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("battalions", PrivateInstance);
        currentRegimentalMissionField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("currentMission", PrivateInstance);

        BuildStyles();
        Debug.Log(
            "TACTICAL-VIS-09F29I|Installed=True|Overlay=ON|Toggle=TAB|" +
            "CloseHQBeacons=True|OrderLabels=True|ObjectiveLabels=True|" +
            "SelectedFacing=True|CombatAlerts=True");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            tacticalOverlay = !tacticalOverlay;
            Debug.Log("TACTICAL-VIS-09F29I|Overlay=" + (tacticalOverlay ? "ON" : "OFF"));
        }
    }

    private void OnGUI()
    {
        if (cam == null)
            return;

        BuildStyles();
        GUI.depth = -4650;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        float cameraHeight = cam.transform.position.y;
        int selectedMajor = GetSelectedBattalion(hierarchy);

        DrawOverlayToggle();

        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null)
                    continue;

                bool selected = selectedMajor == i;
                string majorName = "MAJOR " + (i == 0 ? "A" : "B");

                // F29D already owns the full NATO HQ counter from 72 m and out.
                // F29I fills the close-zoom readability gap instead of drawing a duplicate.
                if (cameraHeight < SemanticHqStartHeight)
                    DrawCloseHq(hq.transform.position, "II", majorName, selected, false);

                if (TryGetBattalionOrder(hierarchy, i, out string order, out Vector3 point))
                {
                    if (tacticalOverlay || selected)
                    {
                        DrawOrderTag(hq.transform.position, majorName + " • " + order, selected);
                        DrawObjectiveTag(point, majorName + " • " + order, selected);
                    }
                }
            }
        }

        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
        {
            bool selected = regimental.Selected;
            if (cameraHeight < SemanticHqStartHeight)
                DrawCloseHq(regimental.HqRoot.transform.position, "III", "OBERSTLØJTNANT", selected, true);

            if (TryGetRegimentalOrder(regimental, out string order, out Vector3 point))
            {
                if (tacticalOverlay || selected)
                {
                    DrawOrderTag(regimental.HqRoot.transform.position, "1. REGIMENT • " + order, selected);
                    DrawObjectiveTag(point, "REGIMENT • " + order, selected);
                }
            }
        }

        DrawSelectedFacing();

        if (tacticalOverlay)
            DrawCombatAlerts();
    }

    private void DrawOverlayToggle()
    {
        const float width = 188f;
        Rect rect = new Rect(Screen.width - width - 8f, 35f, width, 22f);
        GUI.Box(rect, "TACTICAL OVERLAY " + (tacticalOverlay ? "ON" : "OFF") + "  [TAB]", toggleStyle);
    }

    private void DrawCloseHq(Vector3 world, string echelon, string label, bool selected, bool regimental)
    {
        if (!TryProject(world + Vector3.up * (regimental ? 4.6f : 3.8f), out Vector2 anchor))
            return;

        Color color = selected ? Selected : Friendly;
        float width = regimental ? 118f : 94f;
        float height = regimental ? 36f : 32f;
        Rect frame = new Rect(anchor.x - width * 0.5f, anchor.y - height - 28f, width, height);
        frame.y = Mathf.Min(frame.y, Screen.height - BottomHudGuard - frame.height - 2f);
        if (!IsUseful(frame))
            return;

        GUI.Box(frame, GUIContent.none, hqStyle);
        DrawBorder(frame, color, selected ? 3f : 2f);
        GUI.Label(new Rect(frame.x, frame.y - 14f, frame.width, 13f), echelon, labelStyle);
        GUI.Label(new Rect(frame.x + 2f, frame.y + 1f, frame.width - 4f, 16f), "HQ", labelStyle);
        GUI.Label(new Rect(frame.x - 30f, frame.yMax + 1f, frame.width + 60f, 14f), label, labelStyle);

        Vector3 groundScreen3 = cam.WorldToScreenPoint(world + Vector3.up * 0.15f);
        if (groundScreen3.z > 0f)
        {
            Vector2 ground = new Vector2(groundScreen3.x, Screen.height - groundScreen3.y);
            Vector2 start = new Vector2(frame.center.x, frame.yMax);
            DrawLine(start, ground, color, 2f);
            DrawSolid(new Rect(ground.x - 3f, ground.y - 3f, 6f, 6f), color);
        }
    }

    private void DrawOrderTag(Vector3 world, string text, bool selected)
    {
        if (!TryProject(world + Vector3.up * 1.0f, out Vector2 anchor))
            return;

        float width = Mathf.Clamp(MeasureText(text, orderStyle) + 18f, 105f, 230f);
        Rect rect = new Rect(anchor.x - width * 0.5f, anchor.y + 18f, width, 18f);
        rect.y = Mathf.Min(rect.y, Screen.height - BottomHudGuard - rect.height - 2f);
        if (!IsUseful(rect))
            return;

        GUI.Box(rect, text, orderStyle);
        DrawBorder(rect, selected ? Selected : Friendly, selected ? 2f : 1f);
    }

    private void DrawObjectiveTag(Vector3 world, string text, bool selected)
    {
        if (!TryProject(world + Vector3.up * 0.8f, out Vector2 anchor))
            return;

        float width = Mathf.Clamp(MeasureText(text, orderStyle) + 18f, 110f, 240f);
        Rect rect = new Rect(anchor.x - width * 0.5f, anchor.y - 28f, width, 18f);
        if (!IsUseful(rect))
            return;

        Color color = selected ? Selected : Objective;
        GUI.Box(rect, text, orderStyle);
        DrawBorder(rect, color, selected ? 2f : 1f);
    }

    private void DrawSelectedFacing()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !regiment.IsSelected || regiment.CurrentStrength <= 0)
                continue;

            Vector3 startWorld = regiment.transform.position + Vector3.up * 0.35f;
            Vector3 endWorld = regiment.transform.position + Flat(regiment.transform.forward) * 34f + Vector3.up * 0.35f;
            if (!TryProject(startWorld, out Vector2 start) || !TryProject(endWorld, out Vector2 end))
                continue;

            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 1f)
                continue;

            DrawArrow(start, end, Selected, 2.5f);
        }
    }

    private void DrawCombatAlerts()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.CurrentStrength <= 0 || regiment.IsRouted)
                continue;

            bool underFire = PrototypeUnderFireReaction09F26.IsReacting(regiment) || regiment.HasHitFeedback;
            bool engaged = underFire || HasFireContact(regiment, battle.Regiments);
            if (!engaged)
                continue;

            if (!TryProject(regiment.transform.position + Vector3.up * 3.5f, out Vector2 anchor))
                continue;

            string text = underFire ? "UNDER ILD" : "KAMP";
            float width = underFire ? 72f : 50f;
            Rect rect = new Rect(anchor.x - width * 0.5f, anchor.y - 45f, width, 17f);
            if (!IsUseful(rect))
                continue;

            GUI.Box(rect, text, combatStyle);
            DrawBorder(rect, Combat, 2f);
        }
    }

    private static bool HasFireContact(Regiment unit, IReadOnlyList<Regiment> units)
    {
        if (unit == null || units == null)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            Regiment enemy = units[i];
            if (enemy == null || enemy.Team == unit.Team || enemy.IsRouted || enemy.CurrentStrength <= 0)
                continue;

            if (unit.CanFireAt(enemy) || enemy.CanFireAt(unit))
                return true;
        }
        return false;
    }

    private int GetSelectedBattalion(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private bool TryGetBattalionOrder(
        PrototypeRegimentHierarchy09F27 hierarchy,
        int battalionIndex,
        out string order,
        out Vector3 point)
    {
        order = string.Empty;
        point = Vector3.zero;
        if (hierarchy == null || battalionsField == null)
            return false;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null || battalionIndex < 0 || battalionIndex >= battalions.Count)
            return false;

        object battalion = battalions[battalionIndex];
        if (battalion == null)
            return false;

        var type = battalion.GetType();
        FieldInfo hasField = type.GetField("HasLastOrder", PrivateInstance | BindingFlags.Public);
        FieldInfo orderField = type.GetField("LastOrder", PrivateInstance | BindingFlags.Public);
        FieldInfo pointField = type.GetField("LastOrderPoint", PrivateInstance | BindingFlags.Public);
        if (hasField == null || orderField == null || pointField == null)
            return false;

        object has = hasField.GetValue(battalion);
        if (!(has is bool) || !(bool)has)
            return false;

        object orderValue = orderField.GetValue(battalion);
        object pointValue = pointField.GetValue(battalion);
        if (!(pointValue is Vector3))
            return false;

        order = OrderLabel(orderValue != null ? orderValue.ToString() : string.Empty);
        point = (Vector3)pointValue;
        return !string.IsNullOrEmpty(order);
    }

    private bool TryGetRegimentalOrder(
        PrototypeRegimentalHQ09F28 regimental,
        out string order,
        out Vector3 point)
    {
        order = string.Empty;
        point = Vector3.zero;
        if (regimental == null || currentRegimentalMissionField == null)
            return false;

        object mission = currentRegimentalMissionField.GetValue(regimental);
        if (mission == null)
            return false;

        var type = mission.GetType();
        FieldInfo orderField = type.GetField("Order", PrivateInstance | BindingFlags.Public);
        FieldInfo objectiveField = type.GetField("Objective", PrivateInstance | BindingFlags.Public);
        if (orderField == null || objectiveField == null)
            return false;

        object orderValue = orderField.GetValue(mission);
        object pointValue = objectiveField.GetValue(mission);
        if (!(pointValue is Vector3))
            return false;

        order = OrderLabel(orderValue != null ? orderValue.ToString() : string.Empty);
        point = (Vector3)pointValue;
        return !string.IsNullOrEmpty(order);
    }

    private static string OrderLabel(string value)
    {
        switch (value)
        {
            case "AttackHere": return "ANGRIB";
            case "DefendHere": return "FORSVAR";
            case "AdvanceHere": return "RYK FREM";
            case "WithdrawHere": return "TILBAGETRÆK";
            case "AssembleHere": return "SAML";
            case "HoldPosition": return "HOLD";
            default: return string.Empty;
        }
    }

    private bool TryProject(Vector3 world, out Vector2 gui)
    {
        gui = Vector2.zero;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0f)
            return false;

        gui = new Vector2(screen.x, Screen.height - screen.y);
        return gui.x >= -180f && gui.x <= Screen.width + 180f &&
               gui.y >= -100f && gui.y <= Screen.height - BottomHudGuard + 100f;
    }

    private static bool IsUseful(Rect rect)
    {
        return rect.xMax >= -20f && rect.xMin <= Screen.width + 20f &&
               rect.yMax >= -20f && rect.yMin <= Screen.height - BottomHudGuard + 20f;
    }

    private void DrawArrow(Vector2 start, Vector2 end, Color color, float width)
    {
        DrawLine(start, end, color, width);
        Vector2 direction = end - start;
        if (direction.sqrMagnitude < 0.1f)
            return;
        direction.Normalize();
        Vector2 side = new Vector2(-direction.y, direction.x);
        DrawLine(end, end - direction * 8f + side * 5f, color, width);
        DrawLine(end, end - direction * 8f - side * 5f, color, width);
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        DrawSolid(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawSolid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawSolid(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawSolid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.1f)
            return;

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.color = color;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width),
            whiteTexture != null ? whiteTexture : Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private void DrawSolid(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture != null ? whiteTexture : Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static float MeasureText(string text, GUIStyle style)
    {
        if (style == null || string.IsNullOrEmpty(text))
            return 100f;
        return style.CalcSize(new GUIContent(text)).x;
    }

    private void BuildStyles()
    {
        if (hqStyle != null)
            return;

        whiteTexture = MakeTexture(Color.white, "TACTVIS_WHITE");
        panelTexture = MakeTexture(Panel, "TACTVIS_PANEL");

        hqStyle = new GUIStyle(GUI.skin.box);
        hqStyle.normal.background = panelTexture;
        hqStyle.padding = new RectOffset(0, 0, 0, 0);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 9;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;

        orderStyle = new GUIStyle(GUI.skin.box);
        orderStyle.normal.background = panelTexture;
        orderStyle.normal.textColor = new Color(0.95f, 0.93f, 0.82f, 1f);
        orderStyle.alignment = TextAnchor.MiddleCenter;
        orderStyle.fontSize = 8;
        orderStyle.fontStyle = FontStyle.Bold;
        orderStyle.padding = new RectOffset(3, 3, 1, 1);

        combatStyle = new GUIStyle(orderStyle);
        combatStyle.normal.textColor = new Color(1f, 0.86f, 0.75f, 1f);

        toggleStyle = new GUIStyle(orderStyle);
        toggleStyle.fontSize = 9;
        toggleStyle.normal.textColor = new Color(0.92f, 0.90f, 0.80f, 1f);
    }

    private static Texture2D MakeTexture(Color color, string name)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}