using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30d
// OOB layout hardening: fixed columns, scroll view, dynamic attachment placement and
// direct drag/drop of cavalry support between Division/Brigade/Regiment/Major A/Major B.
[DefaultExecutionOrder(-4500)]
public sealed class PrototypeHigherCommandOob09F30D : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float PanelX = 8f;
    private const float PanelY = 39f;
    private const float PanelWidth = 535f;
    private const float HeaderHeight = 27f;
    private const float ColumnHeaderHeight = 16f;
    private const float RowHeight = 22f;
    private const float DoubleClickSeconds = 0.34f;
    private const float DragThreshold = 5f;

    private static PrototypeHigherCommandOob09F30D instance;
    private readonly bool[] battalionOpen = { true, true };
    private bool open = true;
    private bool legacyDisabled;
    private Vector2 scroll;
    private Camera cam;
    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

    private PrototypeCavalryUnit09F30 dragCandidate;
    private bool dragActive;
    private Vector2 dragStart;

    private FieldInfo selectedBattalionField;
    private FieldInfo regimentHasDestinationField;
    private MethodInfo selectCompanyOnlyMethod;
    private MethodInfo selectMajorOnlyMethod;
    private MethodInfo selectRegimentalOnlyMethod;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle rowStyle;
    private GUIStyle echelonStyle;
    private GUIStyle nameStyle;
    private GUIStyle rightStyle;
    private GUIStyle columnHeaderStyle;
    private GUIStyle tinyButtonStyle;
    private Texture2D panelTexture;
    private Texture2D rowTexture;
    private Texture2D altTexture;
    private Texture2D selectedTexture;
    private Texture2D hoverTexture;
    private Texture2D dropTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30D>() == null)
            new GameObject("PrototypeHigherCommandOOB_v000009f30d")
                .AddComponent<PrototypeHigherCommandOob09F30D>();
    }

    private void Awake()
    {
        instance = this;
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", BindingFlags.Instance | BindingFlags.NonPublic);
        regimentHasDestinationField = typeof(Regiment)
            .GetField("hasDestination", BindingFlags.Instance | BindingFlags.NonPublic);
        selectCompanyOnlyMethod = typeof(PrototypeOobNavigator09F29Q).GetMethod("SelectCompanyOnly", AnyInstance);
        selectMajorOnlyMethod = typeof(PrototypeOobNavigator09F29Q).GetMethod("SelectMajorOnly", AnyInstance);
        selectRegimentalOnlyMethod = typeof(PrototypeOobNavigator09F29Q).GetMethod("SelectRegimentalOnly", AnyInstance);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (Input.GetKeyDown(KeyCode.O))
            open = !open;

        DisableLegacyPanels();

        // Consume battlefield mouse input before command/movement layers see it.
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButton(0)) &&
            IsPointerOverPanel(Input.mousePosition))
            Input.ResetInputAxes();
    }

    private void DisableLegacyPanels()
    {
        if (legacyDisabled)
            return;

        PrototypeHigherCommandOob09F30C oldC = UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30C>();
        PrototypeHigherCommandOob09F30B oldB = UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30B>();
        PrototypeOobNavigator09F29Q oldInfantry = PrototypeOobNavigator09F29Q.Instance;
        PrototypeOobStatus09F29V oldStatus = UnityEngine.Object.FindAnyObjectByType<PrototypeOobStatus09F29V>();
        PrototypeCavalryOob09F30A oldCavalry = UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryOob09F30A>();

        if (oldC != null) oldC.enabled = false;
        if (oldB != null) oldB.enabled = false;
        if (oldInfantry != null) oldInfantry.enabled = false;
        if (oldStatus != null) oldStatus.enabled = false;
        if (oldCavalry != null) oldCavalry.enabled = false;

        legacyDisabled = true;
        Debug.Log("OOB-09F30I|Installed=True|LegacyPanels=False|LegacyStatusOverlay=False|FixedColumns=True|ScrollView=True|DragDropAttachment=True");
    }

    public static bool IsPointerOverPanel(Vector3 mousePosition)
    {
        if (instance == null)
            return false;
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return instance.GetPanelRect().Contains(gui);
    }

    private Rect GetPanelRect()
    {
        if (!open)
            return new Rect(PanelX, PanelY, PanelWidth, HeaderHeight);

        float contentHeight = ComputeContentHeight();
        float maxBody = Mathf.Max(180f, Mathf.Min(520f, Screen.height - PanelY - HeaderHeight - 12f));
        float visibleBody = Mathf.Min(contentHeight, maxBody);
        return new Rect(PanelX, PanelY, PanelWidth, HeaderHeight + visibleBody + 6f);
    }

    private float ComputeContentHeight()
    {
        int rows = 3 + 2 + 2; // Division, Brigade, Regiment + Majors + 2 cavalry rows.
        if (battalionOpen[0]) rows += 4;
        if (battalionOpen[1]) rows += 4;
        return ColumnHeaderHeight + rows * RowHeight + 5f;
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -125600;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 48f, HeaderHeight - 4f),
            "ORDER OF BATTLE   [O]", headerStyle);
        if (GUI.Button(new Rect(panel.xMax - 33f, panel.y + 3f, 26f, 21f), open ? "−" : "+", tinyButtonStyle))
        {
            open = !open;
            ConsumePointer();
        }
        if (!open)
            return;

        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeOobNavigator09F29Q helper = PrototypeOobNavigator09F29Q.Instance;
        if (higher == null || !higher.Installed || hierarchy == null || !hierarchy.Installed ||
            regiment == null || !regiment.Installed || cavalry == null || !cavalry.Installed || helper == null)
            return;

        Rect viewport = new Rect(panel.x + 4f, panel.y + HeaderHeight, panel.width - 8f, panel.height - HeaderHeight - 4f);
        float contentHeight = ComputeContentHeight();
        Rect content = new Rect(0f, 0f, viewport.width - 18f, contentHeight);
        scroll = GUI.BeginScrollView(viewport, scroll, content, false, contentHeight > viewport.height);

        DrawColumnHeaders(content.width);
        float y = ColumnHeaderHeight;

        y = DrawHigherRow(y, content.width, 0f, "XX", "1. DIVISION", AggregateStrength(hierarchy).ToString(), "",
            higher.DivisionAIEnabled ? "ON" : "OFF", "",
            higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Division, "DIVISION", "Divisionschef",
            PrototypeCavalryCommandControl09F30C.DivisionId,
            delegate(bool dbl) { higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Division, dbl); });
        y = DrawAttachedForParent(y, content.width, 18f, cavalry, PrototypeCavalryCommandControl09F30C.DivisionId, higher);

        y = DrawHigherRow(y, content.width, 14f, "X", "1. BRIGADE", AggregateStrength(hierarchy).ToString(), "",
            higher.BrigadeAIEnabled ? "ON" : "OFF", "",
            higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Brigade, "BRIGADE", "Brigadechef",
            PrototypeCavalryCommandControl09F30C.BrigadeId,
            delegate(bool dbl) { higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Brigade, dbl); });
        y = DrawAttachedForParent(y, content.width, 32f, cavalry, PrototypeCavalryCommandControl09F30C.BrigadeId, higher);

        Rect regimentRect = RowRect(y, content.width);
        bool regimentClick = DrawRow(
            regimentRect,
            28f,
            "III",
            "1. REGIMENT",
            AggregateStrength(hierarchy).ToString(),
            "",
            regiment.AIEnabled ? "ON" : "OFF",
            "",
            regiment.Selected,
            2,
            "Regimentschef: Oberstløjtnant");
        if (HandleDropTarget(regimentRect, PrototypeCavalryCommandControl09F30C.RegimentId))
            regimentClick = false;
        if (regimentClick)
        {
            higher.ClearSelectionOnly();
            bool dbl = RegisterClick("REGIMENT");
            if (dbl) helper.SelectRegimentalAndFocus(true);
            else if (selectRegimentalOnlyMethod != null) selectRegimentalOnlyMethod.Invoke(helper, null);
            ConsumePointer();
        }
        y += RowHeight;
        y = DrawAttachedForParent(y, content.width, 46f, cavalry, PrototypeCavalryCommandControl09F30C.RegimentId, higher);

        int selectedBattalion = GetSelectedBattalion(hierarchy);
        for (int b = 0; b < Mathf.Min(2, hierarchy.BattalionCount); b++)
        {
            Rect row = RowRect(y, content.width);
            Rect exp = new Rect(row.x + 40f, row.y + 1f, 21f, row.height - 2f);
            if (GUI.Button(exp, battalionOpen[b] ? "▼" : "▶", tinyButtonStyle))
            {
                battalionOpen[b] = !battalionOpen[b];
                ConsumePointer();
            }

            string battalionName = (b + 1) + ". BATALJON";
            string ai = hierarchy.GetBattalionAIEnabled(b) ? "ON" : "OFF";
            Rect entity = row;
            bool clicked = DrawRow(entity, 54f, "II", battalionName,
                AggregateBattalionStrength(hierarchy, b).ToString(), "", ai, "",
                selectedBattalion == b, 3 + b, "Bataljonschef: " + (b == 0 ? "Major A" : "Major B"));

            string majorParent = b == 0
                ? PrototypeCavalryCommandControl09F30C.MajorAId
                : PrototypeCavalryCommandControl09F30C.MajorBId;
            if (HandleDropTarget(entity, majorParent))
                clicked = false;

            if (clicked)
            {
                higher.ClearSelectionOnly();
                bool dbl = RegisterClick("MAJOR" + b);
                if (dbl) helper.SelectMajorAndFocus(b, true);
                else if (selectMajorOnlyMethod != null) selectMajorOnlyMethod.Invoke(helper, new object[] { b });
                ConsumePointer();
            }
            y += RowHeight;

            if (battalionOpen[b])
            {
                IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
                if (companies != null)
                {
                    for (int c = 0; c < companies.Count && c < 4; c++)
                    {
                        Regiment unit = companies[c];
                        string name = unit != null ? PrototypeUnitNames09F29C.Get(unit) : "KOMPAGNI";
                        string men = unit != null ? unit.CurrentStrength.ToString() : "—";
                        string status = unit != null ? GetCompanyStatus(unit) : "—";
                        OfficerAIController companyAi =
                            unit != null ? unit.GetComponent<OfficerAIController>() : null;
                        string companyAiState =
                            companyAi != null
                                ? (companyAi.AIEnabled ? "ON" : "OFF")
                                : "—";

                        Rect companyRect = RowRect(y, content.width);
                        if (DrawRow(companyRect, 76f, "I", name, men, status, companyAiState, "",
                            unit != null && unit.IsSelected, 5 + c + b * 4, name))
                        {
                            if (unit != null)
                            {
                                higher.ClearSelectionOnly();
                                bool dbl = RegisterClick("COMP" + b + "_" + c);
                                if (dbl) helper.SelectCompanyAndFocus(unit, true);
                                else if (selectCompanyOnlyMethod != null) selectCompanyOnlyMethod.Invoke(helper, new object[] { unit });
                                ConsumePointer();
                            }
                        }
                        y += RowHeight;
                    }
                }
            }

            y = DrawAttachedForParent(y, content.width, 76f, cavalry, majorParent, higher);
        }

        // Release a drag that ended somewhere invalid.
        Event e = Event.current;
        if (e != null && e.type == EventType.MouseUp && e.button == 0 && dragCandidate != null)
        {
            dragCandidate = null;
            dragActive = false;
        }

        GUI.EndScrollView();

        if (dragCandidate != null && dragActive)
        {
            Rect hint = new Rect(Event.current.mousePosition.x + 14f, Event.current.mousePosition.y + 12f, 180f, 22f);
            GUI.Box(hint, "Flyt: " + dragCandidate.UnitName, tinyButtonStyle);
        }
    }

    private void DrawColumnHeaders(float width)
    {
        Rect rect = new Rect(4f, 0f, width - 8f, ColumnHeaderHeight);
        float menX = rect.xMax - 162f;
        GUI.Label(new Rect(menX, rect.y, 44f, rect.height), "MÆND", columnHeaderStyle);
        GUI.Label(new Rect(menX + 45f, rect.y, 51f, rect.height), "STATUS", columnHeaderStyle);
        GUI.Label(new Rect(menX + 97f, rect.y, 31f, rect.height), "AI", columnHeaderStyle);
        GUI.Label(new Rect(menX + 129f, rect.y, 33f, rect.height), "TILK", columnHeaderStyle);
    }

    private float DrawHigherRow(float y, float width, float indent, string echelon, string name, string men,
        string status, string ai, string attach, bool selected, string key, string tooltip,
        string dropParent, System.Action<bool> action)
    {
        Rect rect = RowRect(y, width);
        bool clicked = DrawRow(rect, indent, echelon, name, men, status, ai, attach, selected, key.GetHashCode(), tooltip);
        if (HandleDropTarget(rect, dropParent))
            clicked = false;
        if (clicked)
        {
            bool dbl = RegisterClick(key);
            action(dbl);
            ConsumePointer();
        }
        return y + RowHeight;
    }

    private float DrawAttachedForParent(float y, float width, float indent, PrototypeCavalryManager09F30 cavalry,
        string parent, PrototypeHigherCommandHQ09F30B higher)
    {
        y = DrawCavalryIfParent(y, width, indent, cavalry, cavalry.Gardehusar, parent, higher, "GARDE");
        y = DrawCavalryIfParent(y, width, indent, cavalry, cavalry.Dragon, parent, higher, "DRAGON");
        return y;
    }

    private float DrawCavalryIfParent(float y, float width, float indent, PrototypeCavalryManager09F30 cavalry,
        PrototypeCavalryUnit09F30 unit, string parent, PrototypeHigherCommandHQ09F30B higher, string key)
    {
        if (unit == null || PrototypeCavalryCommandControl09F30C.GetParent(unit) != parent)
            return y;

        PrototypeCavalryOfficerAI09F30C aiController = PrototypeCavalryOfficerAI09F30C.Instance;
        string ai =
            aiController != null && aiController.IsAIEnabled(unit)
                ? "ON"
                : "OFF";
        Rect rect = RowRect(y, width);
        DrawRowVisual(rect, indent, "I", "↳ " + unit.UnitName, unit.CurrentStrength.ToString(),
            ShortCavalryStatus(unit), ai, "ATT", unit.IsSelected, key == "GARDE" ? 41 : 42,
            "Organic: 1. Division / Kavaleri | Aktuel kommando: " + parent +
            " | Klik = vælg | Dobbeltklik = fokus | Træk = ændr attachment");

        // IMPORTANT: cavalry rows do not use GUI.Button here. GUI.Button consumes the
        // MouseDown/MouseUp events required by the explicit click-vs-drag state machine.
        HandleCavalryDrag(rect, unit, cavalry, higher, key);
        return y + RowHeight;
    }

    private void HandleCavalryDrag(Rect rect, PrototypeCavalryUnit09F30 unit, PrototypeCavalryManager09F30 cavalry,
        PrototypeHigherCommandHQ09F30B higher, string key)
    {
        Event e = Event.current;
        if (e == null)
            return;

        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            dragCandidate = unit;
            dragActive = false;
            dragStart = e.mousePosition;
            e.Use();
            return;
        }

        if (dragCandidate == unit && e.type == EventType.MouseDrag && e.button == 0)
        {
            if (!dragActive && Vector2.Distance(dragStart, e.mousePosition) >= DragThreshold)
                dragActive = true;
            if (dragActive)
                e.Use();
            return;
        }

        if (dragCandidate == unit && e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition))
        {
            if (!dragActive)
            {
                higher.ClearSelectionOnly();
                bool dbl = RegisterClick("CAV_" + key);
                cavalry.SelectUnit(unit);
                if (dbl) higher.FocusBehindExternal(unit.transform);
            }
            dragCandidate = null;
            dragActive = false;
            e.Use();
        }
    }

    private bool HandleDropTarget(Rect rect, string parent)
    {
        if (dragCandidate == null || !dragActive)
            return false;

        Event e = Event.current;
        if (e == null)
            return false;

        bool hover = rect.Contains(e.mousePosition);
        if (hover)
            GUI.DrawTexture(rect, dropTexture);

        if (hover && e.type == EventType.MouseUp && e.button == 0)
        {
            PrototypeCavalryCommandControl09F30C.SetParent(dragCandidate, parent);
            Debug.Log("OOB-DND-09F30D|Unit=" + dragCandidate.UnitName + "|DropParent=" + parent + "|Success=True");
            dragCandidate = null;
            dragActive = false;
            e.Use();
            return true;
        }
        return false;
    }

    private bool DrawRow(Rect rect, float indent, string echelon, string name, string men, string status,
        string ai, string attach, bool selected, int stripe, string tooltip)
    {
        // Button must be processed before the visual row. Drawing an empty IMGUI button
        // after the labels covered the Division/Brigade/Regiment/Battalion/Company text,
        // leaving only cavalry rows visible because cavalry uses DrawRowVisual directly.
        bool clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), rowStyle);
        DrawRowVisual(rect, indent, echelon, name, men, status, ai, attach, selected, stripe, tooltip);
        return clicked;
    }

    private void DrawRowVisual(Rect rect, float indent, string echelon, string name, string men, string status,
        string ai, string attach, bool selected, int stripe, string tooltip)
    {
        GUI.DrawTexture(rect, selected ? selectedTexture : (((stripe & 1) == 0) ? rowTexture : altTexture));

        float menX = rect.xMax - 162f;
        GUI.Label(new Rect(rect.x + 4f + indent, rect.y, 31f, rect.height), new GUIContent(echelon, tooltip), echelonStyle);
        float nameX = rect.x + 36f + indent;
        float nameW = Mathf.Max(40f, menX - nameX - 4f);
        GUI.Label(new Rect(nameX, rect.y, nameW, rect.height), new GUIContent(name, tooltip), nameStyle);
        GUI.Label(new Rect(menX, rect.y, 44f, rect.height), men, rightStyle);
        GUI.Label(new Rect(menX + 45f, rect.y, 51f, rect.height), status, rightStyle);
        GUI.Label(new Rect(menX + 97f, rect.y, 31f, rect.height), ai, rightStyle);
        GUI.Label(new Rect(menX + 129f, rect.y, 33f, rect.height), attach, rightStyle);
    }

    private static Rect RowRect(float y, float width)
    {
        return new Rect(4f, y, width - 8f, RowHeight);
    }

    private int GetSelectedBattalion(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null) return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static int AggregateStrength(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        int total = 0;
        for (int b = 0; hierarchy != null && b < hierarchy.BattalionCount; b++)
            total += AggregateBattalionStrength(hierarchy, b);
        return total;
    }

    private static int AggregateBattalionStrength(PrototypeRegimentHierarchy09F27 hierarchy, int battalion)
    {
        IReadOnlyList<Regiment> list = hierarchy != null ? hierarchy.GetCompanies(battalion) : null;
        int total = 0;
        if (list != null)
            for (int i = 0; i < list.Count; i++) if (list[i] != null) total += list[i].CurrentStrength;
        return total;
    }

    private string GetCompanyStatus(Regiment unit)
    {
        if (unit == null) return "—";
        if (unit.IsRouted) return "ROUT";
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && charge.IsChargeMeleeParticipant(unit)) return "MELEE";
        if (charge != null && charge.IsCharging(unit)) return "CHARGE";
        if (PrototypeInfantrySquare09F29.IsInSquare(unit)) return "SQUARE";
        if (PrototypeAttackContact09F29G.IsLocalContact(unit)) return "KAMP";
        if (PrototypeUnderFireReaction09F26.IsReacting(unit)) return "ILD";
        if (regimentHasDestinationField != null)
        {
            object v = regimentHasDestinationField.GetValue(unit);
            if (v is bool && (bool)v) return "→";
        }
        return "HOLD";
    }

    private static string ShortCavalryStatus(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null) return "—";
        if (unit.Action == PrototypeCavalryAction09F30.Charge) return "CHG";
        if (unit.Action == PrototypeCavalryAction09F30.Move) return "MOVE";
        if (unit.Action == PrototypeCavalryAction09F30.Falter) return "FLT";
        return unit.Mode == PrototypeCavalryMode09F30.Mounted ? "HOLD" : "AFS";
    }

    private bool RegisterClick(string key)
    {
        float now = Time.unscaledTime;
        bool dbl = key == lastClickKey && now - lastClickAt <= DoubleClickSeconds;
        lastClickKey = key;
        lastClickAt = now;
        return dbl;
    }

    private static void ConsumePointer()
    {
        Input.ResetInputAxes();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelTexture = MakeTexture(new Color(0.030f, 0.040f, 0.032f, 0.97f));
        rowTexture = MakeTexture(new Color(0.075f, 0.090f, 0.065f, 0.96f));
        altTexture = MakeTexture(new Color(0.055f, 0.070f, 0.052f, 0.96f));
        selectedTexture = MakeTexture(new Color(0.44f, 0.34f, 0.075f, 0.98f));
        hoverTexture = MakeTexture(new Color(0.22f, 0.28f, 0.14f, 0.38f));
        dropTexture = MakeTexture(new Color(0.22f, 0.48f, 0.76f, 0.32f));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.95f, 0.82f, 0.35f, 1f);

        rowStyle = new GUIStyle(GUI.skin.button);
        rowStyle.normal.background = null;
        rowStyle.hover.background = hoverTexture;
        rowStyle.active.background = hoverTexture;
        rowStyle.border = new RectOffset(0, 0, 0, 0);

        echelonStyle = new GUIStyle(GUI.skin.label);
        echelonStyle.fontSize = 9;
        echelonStyle.fontStyle = FontStyle.Bold;
        echelonStyle.alignment = TextAnchor.MiddleCenter;
        echelonStyle.normal.textColor = new Color(0.38f, 0.70f, 1f, 1f);

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 9;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.normal.textColor = new Color(0.94f, 0.94f, 0.87f, 1f);

        rightStyle = new GUIStyle(GUI.skin.label);
        rightStyle.fontSize = 8;
        rightStyle.alignment = TextAnchor.MiddleRight;
        rightStyle.normal.textColor = new Color(0.72f, 0.75f, 0.67f, 1f);

        columnHeaderStyle = new GUIStyle(rightStyle);
        columnHeaderStyle.fontSize = 7;
        columnHeaderStyle.fontStyle = FontStyle.Bold;
        columnHeaderStyle.normal.textColor = new Color(0.55f, 0.61f, 0.52f, 1f);

        tinyButtonStyle = new GUIStyle(GUI.skin.button);
        tinyButtonStyle.fontSize = 9;
        tinyButtonStyle.fontStyle = FontStyle.Bold;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.hideFlags = HideFlags.HideAndDontSave;
        t.SetPixel(0, 0, color);
        t.Apply(false, true);
        return t;
    }
}
