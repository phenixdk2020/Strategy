using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30c
// Dynamic OOB: cavalry/support rows physically move under the HQ that currently owns
// CurrentCommandParent. This makes attachment visible without opening a separate panel.
[DefaultExecutionOrder(39440)]
public sealed class PrototypeHigherCommandOob09F30C : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float PanelX = 8f;
    private const float PanelY = 39f;
    private const float PanelWidth = 326f;
    private const float HeaderHeight = 27f;
    private const float RowHeight = 22f;
    private const float DoubleClickSeconds = 0.34f;

    private static PrototypeHigherCommandOob09F30C instance;
    private readonly bool[] battalionOpen = { true, true };
    private bool open = true;
    private bool oldDisabled;
    private Camera cam;
    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

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
    private GUIStyle tinyButtonStyle;
    private Texture2D panelTexture;
    private Texture2D rowTexture;
    private Texture2D altTexture;
    private Texture2D selectedTexture;
    private Texture2D hoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30C>() == null)
            new GameObject("PrototypeHigherCommandOOB_v000009f30c")
                .AddComponent<PrototypeHigherCommandOob09F30C>();
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

        DisableOldPanel();

        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && IsPointerOverPanel(Input.mousePosition))
            Input.ResetInputAxes();
    }

    private void DisableOldPanel()
    {
        if (oldDisabled)
            return;
        PrototypeHigherCommandOob09F30B old = UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30B>();
        if (old != null)
            old.enabled = false;
        oldDisabled = true;
        Debug.Log("OOB-09F30C|DynamicAttachmentRows=True|Parents=DIVISION,BRIGADE,REGIMENT,MAJOR_A,MAJOR_B");
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
        float rows = 3f + 2f + 2f; // Division, Brigade, Regiment + two Majors + two cavalry rows.
        if (battalionOpen[0]) rows += 4f;
        if (battalionOpen[1]) rows += 4f;
        float h = HeaderHeight + (open ? rows * RowHeight + 6f : 0f);
        return new Rect(PanelX, PanelY, PanelWidth, h);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -125450;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 48f, HeaderHeight - 4f),
            "ORDER OF BATTLE — DYNAMISK KOMMANDO   [O]", headerStyle);
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

        float y = panel.y + HeaderHeight + 2f;
        y = DrawHigherRow(y, panel.x + 5f, panel.width - 10f, "XX", "1. DIVISION • DIVISIONSCHEF",
            AggregateStrength(hierarchy).ToString(), higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Division,
            "DIVISION", delegate(bool dbl) { higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Division, dbl); });
        y = DrawAttachedForParent(y, panel.x + 23f, panel.width - 28f, cavalry,
            PrototypeCavalryCommandControl09F30C.DivisionId, higher);

        y = DrawHigherRow(y, panel.x + 16f, panel.width - 21f, "X", "1. BRIGADE • BRIGADECHEF",
            AggregateStrength(hierarchy).ToString(), higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Brigade,
            "BRIGADE", delegate(bool dbl) { higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Brigade, dbl); });
        y = DrawAttachedForParent(y, panel.x + 34f, panel.width - 39f, cavalry,
            PrototypeCavalryCommandControl09F30C.BrigadeId, higher);

        if (DrawRow(new Rect(panel.x + 27f, y, panel.width - 32f, RowHeight), "III",
            "1. REGIMENT • OBERSTLØJTNANT", AggregateStrength(hierarchy).ToString(), regiment.Selected, 2))
        {
            higher.ClearSelectionOnly();
            bool dbl = RegisterClick("REGIMENT");
            if (dbl) helper.SelectRegimentalAndFocus(true);
            else if (selectRegimentalOnlyMethod != null) selectRegimentalOnlyMethod.Invoke(helper, null);
            ConsumePointer();
        }
        y += RowHeight;
        y = DrawAttachedForParent(y, panel.x + 45f, panel.width - 50f, cavalry,
            PrototypeCavalryCommandControl09F30C.RegimentId, higher);

        int selectedBattalion = GetSelectedBattalion(hierarchy);
        for (int b = 0; b < Mathf.Min(2, hierarchy.BattalionCount); b++)
        {
            Rect majorRect = new Rect(panel.x + 38f, y, panel.width - 43f, RowHeight);
            Rect exp = new Rect(majorRect.x + 1f, majorRect.y + 1f, 21f, majorRect.height - 2f);
            if (GUI.Button(exp, battalionOpen[b] ? "▼" : "▶", tinyButtonStyle))
            {
                battalionOpen[b] = !battalionOpen[b];
                ConsumePointer();
            }

            Rect entity = new Rect(majorRect.x + 23f, majorRect.y, majorRect.width - 23f, majorRect.height);
            string majorName = (b == 0 ? "MAJOR A" : "MAJOR B") + " • " + (b + 1) + ". BATALJON";
            if (DrawRow(entity, "II", majorName, AggregateBattalionStrength(hierarchy, b).ToString(),
                selectedBattalion == b, 3 + b))
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
                        string right = unit != null ? unit.CurrentStrength + " " + GetCompanyStatus(unit) : "—";
                        if (DrawRow(new Rect(panel.x + 67f, y, panel.width - 72f, RowHeight), "I", name, right,
                            unit != null && unit.IsSelected, 5 + c + b * 4))
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

            string parent = b == 0
                ? PrototypeCavalryCommandControl09F30C.MajorAId
                : PrototypeCavalryCommandControl09F30C.MajorBId;
            y = DrawAttachedForParent(y, panel.x + 67f, panel.width - 72f, cavalry, parent, higher);
        }
    }

    private float DrawHigherRow(float y, float x, float width, string echelon, string name, string right,
        bool selected, string key, System.Action<bool> action)
    {
        if (DrawRow(new Rect(x, y, width, RowHeight), echelon, name, right, selected, key.GetHashCode()))
        {
            bool dbl = RegisterClick(key);
            action(dbl);
            ConsumePointer();
        }
        return y + RowHeight;
    }

    private float DrawAttachedForParent(float y, float x, float width, PrototypeCavalryManager09F30 cavalry,
        string parent, PrototypeHigherCommandHQ09F30B higher)
    {
        y = DrawCavalryIfParent(y, x, width, cavalry, cavalry.Gardehusar, parent, higher, "GARDE");
        y = DrawCavalryIfParent(y, x, width, cavalry, cavalry.Dragon, parent, higher, "DRAGON");
        return y;
    }

    private float DrawCavalryIfParent(float y, float x, float width, PrototypeCavalryManager09F30 cavalry,
        PrototypeCavalryUnit09F30 unit, string parent, PrototypeHigherCommandHQ09F30B higher, string key)
    {
        if (unit == null || PrototypeCavalryCommandControl09F30C.GetParent(unit) != parent)
            return y;

        PrototypeCavalryOfficerAI09F30C ai = PrototypeCavalryOfficerAI09F30C.Instance;
        string aiState = ai != null && ai.IsAIEnabled(unit) ? "AI" : "MAN";
        string right = unit.CurrentStrength + " " + ShortCavalryStatus(unit) + " " + aiState + " [ATT]";
        if (DrawRow(new Rect(x, y, width, RowHeight), "I", "↳ " + unit.UnitName, right, unit.IsSelected,
            key == "GARDE" ? 41 : 42))
        {
            higher.ClearSelectionOnly();
            bool dbl = RegisterClick("CAV_" + key);
            cavalry.SelectUnit(unit);
            if (dbl) higher.FocusBehindExternal(unit.transform);
            ConsumePointer();
        }
        return y + RowHeight;
    }

    private bool DrawRow(Rect rect, string echelon, string name, string right, bool selected, int stripe)
    {
        GUI.DrawTexture(rect, selected ? selectedTexture : (((stripe & 1) == 0) ? rowTexture : altTexture));
        bool clicked = GUI.Button(rect, GUIContent.none, rowStyle);
        GUI.Label(new Rect(rect.x + 4f, rect.y, 31f, rect.height), echelon, echelonStyle);
        GUI.Label(new Rect(rect.x + 36f, rect.y, rect.width - 139f, rect.height), name, nameStyle);
        GUI.Label(new Rect(rect.xMax - 99f, rect.y, 94f, rect.height), right, rightStyle);
        return clicked;
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
        if (unit.Action == PrototypeCavalryAction09F30.Charge) return "CHG";
        if (unit.Action == PrototypeCavalryAction09F30.Move) return "→";
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
        nameStyle.normal.textColor = new Color(0.94f, 0.94f, 0.87f, 1f);
        rightStyle = new GUIStyle(GUI.skin.label);
        rightStyle.fontSize = 8;
        rightStyle.alignment = TextAnchor.MiddleRight;
        rightStyle.normal.textColor = new Color(0.67f, 0.70f, 0.62f, 1f);
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
