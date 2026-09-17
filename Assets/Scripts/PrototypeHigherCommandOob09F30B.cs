using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30b
// Unified OOB for XX Division -> X Brigade -> III Regiment -> II Battalions -> I units.
// Replaces the separate legacy infantry and cavalry OOB panels while reusing their proven
// selection/focus helpers for lower-echelon entities.
[DefaultExecutionOrder(39420)]
public sealed class PrototypeHigherCommandOob09F30B : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float PanelX = 8f;
    private const float PanelY = 39f;
    private const float PanelWidth = 318f;
    private const float HeaderHeight = 27f;
    private const float RowHeight = 22f;
    private const float SectionHeight = 18f;
    private const float DoubleClickSeconds = 0.34f;

    private static PrototypeHigherCommandOob09F30B instance;

    private bool open = true;
    private readonly bool[] battalionOpen = { true, true };
    private Camera cam;
    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;
    private bool legacyDisabled;

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
    private GUIStyle sectionStyle;
    private GUIStyle tinyButtonStyle;
    private Texture2D panelTexture;
    private Texture2D rowTexture;
    private Texture2D altTexture;
    private Texture2D selectedTexture;
    private Texture2D hoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30B>() == null)
            new GameObject("PrototypeHigherCommandOOB_v000009f30b")
                .AddComponent<PrototypeHigherCommandOob09F30B>();
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

        DisableLegacyPanelsOnceReady();
    }

    private void DisableLegacyPanelsOnceReady()
    {
        if (legacyDisabled)
            return;

        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        PrototypeOobNavigator09F29Q oldOob = PrototypeOobNavigator09F29Q.Instance;
        PrototypeCavalryOob09F30A oldCavalry = UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryOob09F30A>();
        if (higher == null || !higher.Installed || oldOob == null)
            return;

        oldOob.enabled = false;
        if (oldCavalry != null)
            oldCavalry.enabled = false;
        legacyDisabled = true;
        Debug.Log("OOB-09F30B|Unified=True|LegacyInfantryPanel=False|LegacyCavalryPanel=False|Hierarchy=XX-X-III-II-I");
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
        float height = HeaderHeight;
        if (open)
        {
            height += RowHeight * 3f; // division, brigade, regiment
            for (int b = 0; b < 2; b++)
            {
                height += RowHeight;
                if (battalionOpen[b])
                    height += RowHeight * 4f;
            }
            height += SectionHeight + RowHeight * 2f + 8f;
        }
        return new Rect(PanelX, PanelY, PanelWidth, height);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -125300;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 48f, HeaderHeight - 4f),
            "ORDER OF BATTLE — HØJERE KOMMANDO   [O]", headerStyle);
        if (GUI.Button(new Rect(panel.xMax - 33f, panel.y + 3f, 26f, 21f), open ? "−" : "+", tinyButtonStyle))
        {
            open = !open;
            ConsumePointer();
        }
        if (!open)
            return;

        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeOobNavigator09F29Q oldOob = PrototypeOobNavigator09F29Q.Instance;

        if (higher == null || !higher.Installed || hierarchy == null || !hierarchy.Installed ||
            regimental == null || !regimental.Installed || cavalry == null || !cavalry.Installed || oldOob == null)
        {
            GUI.Label(new Rect(panel.x + 8f, panel.y + HeaderHeight + 5f, panel.width - 16f, 24f),
                "HQ/OOB initialiseres …", nameStyle);
            return;
        }

        float y = panel.y + HeaderHeight + 2f;
        if (DrawRow(new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight),
                "XX", "1. DIVISION • DIVISIONSCHEF", AggregateStrength(hierarchy).ToString(),
                higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Division, 0))
        {
            bool dbl = RegisterClick("DIVISION");
            higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Division, dbl);
            ConsumePointer();
        }
        y += RowHeight;

        if (DrawRow(new Rect(panel.x + 16f, y, panel.width - 21f, RowHeight),
                "X", "1. BRIGADE • BRIGADECHEF", AggregateStrength(hierarchy).ToString(),
                higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Brigade, 1))
        {
            bool dbl = RegisterClick("BRIGADE");
            higher.SelectLevel(PrototypeHigherCommandLevel09F30B.Brigade, dbl);
            ConsumePointer();
        }
        y += RowHeight;

        if (DrawRow(new Rect(panel.x + 27f, y, panel.width - 32f, RowHeight),
                "III", "1. REGIMENT • OBERSTLØJTNANT", AggregateStrength(hierarchy).ToString(),
                regimental.Selected, 2))
        {
            higher.ClearSelectionOnly();
            bool dbl = RegisterClick("REGIMENT");
            if (dbl)
                oldOob.SelectRegimentalAndFocus(true);
            else if (selectRegimentalOnlyMethod != null)
                selectRegimentalOnlyMethod.Invoke(oldOob, null);
            ConsumePointer();
        }
        y += RowHeight;

        int selectedBattalion = GetSelectedBattalion(hierarchy);
        for (int b = 0; b < Mathf.Min(2, hierarchy.BattalionCount); b++)
        {
            Rect majorRect = new Rect(panel.x + 38f, y, panel.width - 43f, RowHeight);
            Rect expand = new Rect(majorRect.x + 1f, majorRect.y + 1f, 21f, majorRect.height - 2f);
            if (GUI.Button(expand, battalionOpen[b] ? "▼" : "▶", tinyButtonStyle))
            {
                battalionOpen[b] = !battalionOpen[b];
                ConsumePointer();
            }

            Rect entityRect = new Rect(majorRect.x + 23f, majorRect.y, majorRect.width - 23f, majorRect.height);
            string majorName = (b == 0 ? "MAJOR A" : "MAJOR B") + " • " + (b + 1) + ". BATALJON";
            if (DrawRow(entityRect, "II", majorName, AggregateBattalionStrength(hierarchy, b).ToString(),
                    selectedBattalion == b, b + 3))
            {
                higher.ClearSelectionOnly();
                bool dbl = RegisterClick("MAJOR" + b);
                if (dbl)
                    oldOob.SelectMajorAndFocus(b, true);
                else if (selectMajorOnlyMethod != null)
                    selectMajorOnlyMethod.Invoke(oldOob, new object[] { b });
                ConsumePointer();
            }
            y += RowHeight;

            if (!battalionOpen[b])
                continue;

            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null)
                continue;

            for (int c = 0; c < companies.Count && c < 4; c++)
            {
                Regiment unit = companies[c];
                string name = unit != null ? PrototypeUnitNames09F29C.Get(unit) : "KOMPAGNI";
                string right = unit != null ? unit.CurrentStrength + " " + GetCompanyStatus(unit) : "—";
                if (DrawRow(new Rect(panel.x + 67f, y, panel.width - 72f, RowHeight),
                        "I", name, right, unit != null && unit.IsSelected, c + b * 4))
                {
                    if (unit != null)
                    {
                        higher.ClearSelectionOnly();
                        bool dbl = RegisterClick("COMP" + b + "_" + c);
                        if (dbl)
                            oldOob.SelectCompanyAndFocus(unit, true);
                        else if (selectCompanyOnlyMethod != null)
                            selectCompanyOnlyMethod.Invoke(oldOob, new object[] { unit });
                        ConsumePointer();
                    }
                }
                y += RowHeight;
            }
        }

        GUI.Label(new Rect(panel.x + 41f, y, panel.width - 48f, SectionHeight),
            "ATTACHED / SUPPORT — BRIGADE", sectionStyle);
        y += SectionHeight;

        DrawCavalryRow(higher, cavalry, cavalry.Gardehusar,
            new Rect(panel.x + 48f, y, panel.width - 53f, RowHeight), "GARDE");
        y += RowHeight;
        DrawCavalryRow(higher, cavalry, cavalry.Dragon,
            new Rect(panel.x + 48f, y, panel.width - 53f, RowHeight), "DRAGON");
    }

    private void DrawCavalryRow(PrototypeHigherCommandHQ09F30B higher, PrototypeCavalryManager09F30 cavalry,
        PrototypeCavalryUnit09F30 unit, Rect rect, string key)
    {
        if (unit == null)
            return;

        string parent = higher.GetCavalryCommandParent(unit) == PrototypeHigherCommandHQ09F30B.RegimentId ? "REG" : "BRIG";
        string right = unit.CurrentStrength + " " + ShortCavalryStatus(unit) + " " + parent;
        if (DrawRow(rect, "I", unit.UnitName, right, unit.IsSelected, key == "GARDE" ? 10 : 11))
        {
            higher.ClearSelectionOnly();
            bool dbl = RegisterClick("CAV_" + key);
            cavalry.SelectUnit(unit);
            if (dbl)
                higher.FocusBehindExternal(unit.transform);
            ConsumePointer();
        }
    }

    private bool DrawRow(Rect rect, string echelon, string name, string right, bool selected, int stripe)
    {
        Color old = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, selected ? selectedTexture : (((stripe & 1) == 0) ? rowTexture : altTexture));
        GUI.color = old;

        bool clicked = GUI.Button(rect, GUIContent.none, rowStyle);
        GUI.Label(new Rect(rect.x + 4f, rect.y, 31f, rect.height), echelon, echelonStyle);
        GUI.Label(new Rect(rect.x + 36f, rect.y, rect.width - 128f, rect.height), name, nameStyle);
        GUI.Label(new Rect(rect.xMax - 89f, rect.y, 84f, rect.height), right, rightStyle);
        return clicked;
    }

    private int GetSelectedBattalion(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static int AggregateStrength(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null)
            return 0;
        int total = 0;
        for (int b = 0; b < hierarchy.BattalionCount; b++)
            total += AggregateBattalionStrength(hierarchy, b);
        return total;
    }

    private static int AggregateBattalionStrength(PrototypeRegimentHierarchy09F27 hierarchy, int battalion)
    {
        IReadOnlyList<Regiment> companies = hierarchy != null ? hierarchy.GetCompanies(battalion) : null;
        if (companies == null)
            return 0;
        int total = 0;
        for (int i = 0; i < companies.Count; i++)
            if (companies[i] != null)
                total += companies[i].CurrentStrength;
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
            object value = regimentHasDestinationField.GetValue(unit);
            if (value is bool && (bool)value) return "→";
        }
        return "HOLD";
    }

    private static string ShortCavalryStatus(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null) return "—";
        if (unit.Action == PrototypeCavalryAction09F30.Charge) return "CHARGE";
        if (unit.Action == PrototypeCavalryAction09F30.Move) return "→";
        if (unit.Action == PrototypeCavalryAction09F30.Falter) return "FALTER";
        return unit.Mode == PrototypeCavalryMode09F30.Mounted ? "HOLD" : "AFSID";
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
        if (panelStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.030f, 0.040f, 0.032f, 0.97f));
        rowTexture = MakeTexture(new Color(0.075f, 0.090f, 0.065f, 0.96f));
        altTexture = MakeTexture(new Color(0.055f, 0.070f, 0.052f, 0.96f));
        selectedTexture = MakeTexture(new Color(0.44f, 0.34f, 0.075f, 0.98f));
        hoverTexture = MakeTexture(new Color(0.22f, 0.28f, 0.14f, 0.38f));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.padding = new RectOffset(0, 0, 0, 0);

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
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
        echelonStyle.normal.textColor = new Color(0.38f, 0.70f, 1.00f, 1f);

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 9;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.normal.textColor = new Color(0.94f, 0.94f, 0.87f, 1f);

        rightStyle = new GUIStyle(GUI.skin.label);
        rightStyle.fontSize = 8;
        rightStyle.alignment = TextAnchor.MiddleRight;
        rightStyle.normal.textColor = new Color(0.67f, 0.70f, 0.62f, 1f);

        sectionStyle = new GUIStyle(nameStyle);
        sectionStyle.fontSize = 8;
        sectionStyle.normal.textColor = new Color(0.82f, 0.70f, 0.32f, 1f);

        tinyButtonStyle = new GUIStyle(GUI.skin.button);
        tinyButtonStyle.fontSize = 9;
        tinyButtonStyle.fontStyle = FontStyle.Bold;
        tinyButtonStyle.padding = new RectOffset(1, 1, 1, 1);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}
