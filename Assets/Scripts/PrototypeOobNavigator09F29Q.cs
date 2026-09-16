using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29q + v00.00.09f29s OOB interaction polish
// Collapsible Order of Battle navigator.
// OOB rows are selection/navigation shortcuts only: they do not issue tactical orders
// and therefore do not bypass the existing Kaptajn -> Major -> Oberstløjtnant authority.
// F29S UX rule: single-click changes command selection without moving the camera;
// double-click deliberately navigates behind the selected company/HQ.
[DefaultExecutionOrder(39400)]
public sealed class PrototypeOobNavigator09F29Q : MonoBehaviour
{
    public static PrototypeOobNavigator09F29Q Instance { get; private set; }

    private const float PanelX = 8f;
    private const float PanelY = 39f;
    private const float PanelWidth = 292f;
    private const float HeaderHeight = 27f;
    private const float RegimentRowHeight = 25f;
    private const float MajorRowHeight = 23f;
    private const float CompanyRowHeight = 21f;
    private const float DoubleClickSeconds = 0.34f;

    private static readonly Color PanelColor = new Color(0.030f, 0.040f, 0.032f, 0.96f);
    private static readonly Color RowColor = new Color(0.075f, 0.090f, 0.065f, 0.95f);
    private static readonly Color RowAlternate = new Color(0.055f, 0.070f, 0.052f, 0.95f);
    private static readonly Color SelectedColor = new Color(0.44f, 0.34f, 0.075f, 0.98f);
    private static readonly Color FriendlyColor = new Color(0.38f, 0.70f, 1.00f, 1f);
    private static readonly Color HqColor = new Color(0.95f, 0.82f, 0.35f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.94f, 0.87f, 1f);
    private static readonly Color MutedColor = new Color(0.67f, 0.70f, 0.62f, 1f);

    private Camera cam;
    private bool open = true;
    private readonly bool[] battalionOpen = { true, true };

    private FieldInfo playerSelectedField;
    private FieldInfo selectedBattalionField;
    private FieldInfo regimentHasDestinationField;
    private MethodInfo selectMajorMethod;
    private MethodInfo setRegimentalSelectedMethod;

    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle rowButtonStyle;
    private GUIStyle echelonStyle;
    private GUIStyle nameStyle;
    private GUIStyle rightStyle;
    private GUIStyle tinyButtonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeOobNavigator09F29Q>() == null)
            new GameObject("PrototypeOobNavigator_v000009f29q")
                .AddComponent<PrototypeOobNavigator09F29Q>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        playerSelectedField = typeof(PlayerCommander).GetField("selected", flags);
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", flags);
        regimentHasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        selectMajorMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SelectMajor", flags);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod("SetSelected", flags);

        Debug.Log(
            "OOB-09F29Q|Installed=True|Toggle=O|Hierarchy=III-II-I|" +
            "SingleClick=SelectOnly|DoubleClick=BehindSelected|Status=True|AuthorityBypass=False");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (Input.GetKeyDown(KeyCode.O))
            open = !open;
    }

    public static bool IsPointerOverPanel(Vector3 mousePosition)
    {
        PrototypeOobNavigator09F29Q instance = Instance;
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
            height += RegimentRowHeight + 4f;
            for (int b = 0; b < 2; b++)
            {
                height += MajorRowHeight;
                if (battalionOpen[b])
                    height += CompanyRowHeight * 4f;
            }
            height += 8f;
        }
        return new Rect(PanelX, PanelY, PanelWidth, height);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -125000;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 46f, HeaderHeight - 4f),
            "ORDER OF BATTLE   [O]", headerStyle);

        if (GUI.Button(new Rect(panel.xMax - 33f, panel.y + 3f, 26f, 21f), open ? "−" : "+", tinyButtonStyle))
        {
            open = !open;
            ConsumePointer();
        }

        if (!open)
            return;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (hierarchy == null || !hierarchy.Installed || regimental == null || !regimental.Installed)
        {
            GUI.Label(new Rect(panel.x + 8f, panel.y + HeaderHeight + 4f, panel.width - 16f, 24f),
                "OOB initialiseres …", nameStyle);
            return;
        }

        float y = panel.y + HeaderHeight + 2f;
        int regimentStrength = AggregateStrength(hierarchy, -1);
        Rect regRect = new Rect(panel.x + 5f, y, panel.width - 10f, RegimentRowHeight);
        if (DrawEntityRow(regRect, "III", "1. REGIMENT  •  OBERSTLØJTNANT",
                regimentStrength.ToString(), regimental.Selected, 0, HqColor))
        {
            bool dbl = RegisterClick("REGHQ");
            if (dbl)
                SelectRegimentalAndFocus(true);
            else
                SelectRegimentalOnly();
        }
        y += RegimentRowHeight + 2f;

        int selectedBattalion = GetSelectedBattalion(hierarchy);
        for (int b = 0; b < Mathf.Min(2, hierarchy.BattalionCount); b++)
        {
            Rect majorRect = new Rect(panel.x + 5f, y, panel.width - 10f, MajorRowHeight);
            Rect expander = new Rect(majorRect.x + 2f, majorRect.y + 2f, 21f, majorRect.height - 4f);
            if (GUI.Button(expander, battalionOpen[b] ? "▼" : "▶", tinyButtonStyle))
            {
                battalionOpen[b] = !battalionOpen[b];
                ConsumePointer();
            }

            Rect majorEntityRect = new Rect(majorRect.x + 25f, majorRect.y,
                majorRect.width - 25f, majorRect.height);
            string majorName = (b == 0 ? "MAJOR A" : "MAJOR B") + "  •  " + (b + 1) + ". BATALJON";
            if (DrawEntityRow(majorEntityRect, "II", majorName,
                    AggregateStrength(hierarchy, b).ToString(), selectedBattalion == b, b + 1, HqColor))
            {
                bool dbl = RegisterClick("MAJOR" + b);
                if (dbl)
                    SelectMajorAndFocus(b, true);
                else
                    SelectMajorOnly(b);
            }
            y += MajorRowHeight;

            if (!battalionOpen[b])
                continue;

            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null)
                continue;

            for (int c = 0; c < companies.Count; c++)
            {
                Regiment unit = companies[c];
                Rect companyRect = new Rect(panel.x + 30f, y, panel.width - 35f, CompanyRowHeight);
                string name = unit != null ? PrototypeUnitNames09F29C.Get(unit) : "KOMPAGNI";
                string right = unit != null
                    ? unit.CurrentStrength.ToString() + "  " + GetStatus(unit)
                    : "—";

                if (DrawEntityRow(companyRect, "I", name, right,
                        unit != null && unit.IsSelected, c, FriendlyColor))
                {
                    if (unit != null)
                    {
                        bool dbl = RegisterClick("COMP" + b + "_" + c);
                        if (dbl)
                            SelectCompanyAndFocus(unit, true);
                        else
                            SelectCompanyOnly(unit);
                    }
                }
                y += CompanyRowHeight;
            }
        }
    }

    private bool DrawEntityRow(
        Rect rect,
        string echelon,
        string name,
        string right,
        bool selected,
        int stripe,
        Color echelonColor)
    {
        Color old = GUI.color;
        GUI.color = selected ? SelectedColor : ((stripe & 1) == 0 ? RowColor : RowAlternate);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;

        bool clicked = GUI.Button(rect, GUIContent.none, rowButtonStyle);

        GUIStyle localEchelon = echelonStyle;
        Color previousEchelon = localEchelon.normal.textColor;
        localEchelon.normal.textColor = selected ? new Color(1f, 0.87f, 0.28f, 1f) : echelonColor;
        GUI.Label(new Rect(rect.x + 4f, rect.y, 27f, rect.height), echelon, localEchelon);
        localEchelon.normal.textColor = previousEchelon;

        GUI.Label(new Rect(rect.x + 32f, rect.y, rect.width - 105f, rect.height), name, nameStyle);
        GUI.Label(new Rect(rect.xMax - 78f, rect.y, 73f, rect.height), right, rightStyle);
        return clicked;
    }

    private bool RegisterClick(string key)
    {
        float now = Time.unscaledTime;
        bool doubleClick = key == lastClickKey && now - lastClickAt <= DoubleClickSeconds;
        lastClickKey = key;
        lastClickAt = now;
        return doubleClick;
    }

    private void SelectCompanyOnly(Regiment unit)
    {
        if (!SelectCompanyCore(unit))
            return;
        ConsumePointer();
        Debug.Log("OOB-09F29S|Select=Company|Unit=" + unit.RegimentName + "|CameraMoved=False");
    }

    public void SelectCompanyAndFocus(Regiment unit, bool behind)
    {
        if (!SelectCompanyCore(unit))
            return;

        FocusTransform(unit.transform, behind);
        ConsumePointer();
        Debug.Log("OOB-09F29Q|Select=Company|Unit=" + unit.RegimentName + "|Behind=" + behind);
    }

    private bool SelectCompanyCore(Regiment unit)
    {
        if (unit == null || unit.Team != BattleTeam.Denmark)
            return false;

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();

        ClearCompanySelection();
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
            {
                selected.Add(unit);
                unit.SetSelected(true);
            }
        }
        return true;
    }

    private void SelectMajorOnly(int battalionIndex)
    {
        GameObject hq;
        if (!SelectMajorCore(battalionIndex, out hq))
            return;
        ConsumePointer();
        Debug.Log("OOB-09F29S|Select=Major|Battalion=" + (battalionIndex + 1) + "|CameraMoved=False");
    }

    public void SelectMajorAndFocus(int battalionIndex, bool behind)
    {
        GameObject hq;
        if (!SelectMajorCore(battalionIndex, out hq))
            return;

        if (hq != null)
            FocusTransform(hq.transform, behind);

        ConsumePointer();
        Debug.Log("OOB-09F29Q|Select=Major|Battalion=" + (battalionIndex + 1) + "|Behind=" + behind);
    }

    private bool SelectMajorCore(int battalionIndex, out GameObject hq)
    {
        hq = null;
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || selectMajorMethod == null)
            return false;

        selectMajorMethod.Invoke(hierarchy, new object[] { battalionIndex });
        hq = hierarchy.GetMajorHq(battalionIndex);
        return true;
    }

    private void SelectRegimentalOnly()
    {
        Transform target;
        if (!SelectRegimentalCore(out target))
            return;
        ConsumePointer();
        Debug.Log("OOB-09F29S|Select=RegimentalHQ|CameraMoved=False");
    }

    public void SelectRegimentalAndFocus(bool behind)
    {
        Transform target;
        if (!SelectRegimentalCore(out target))
            return;

        if (target != null)
            FocusTransform(target, behind);

        ConsumePointer();
        Debug.Log("OOB-09F29Q|Select=RegimentalHQ|Behind=" + behind);
    }

    private bool SelectRegimentalCore(out Transform target)
    {
        target = null;
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed || setRegimentalSelectedMethod == null)
            return false;

        setRegimentalSelectedMethod.Invoke(regimental, new object[] { true });
        if (regimental.HqRoot != null)
            target = regimental.HqRoot.transform;
        return true;
    }

    private void ClearCompanySelection()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment regiment in battle.Regiments)
                if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                    regiment.SetSelected(false);
        }

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
                selected.Clear();
        }
    }

    private void FocusTransform(Transform target, bool behind)
    {
        if (target == null)
            return;
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        Vector3 forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        if (behind)
        {
            Vector3 p = target.position - forward * 48f;
            p.y = PrototypeBootstrap.SampleGroundHeight(target.position.x, target.position.z) + 34f;
            cam.transform.position = ClampCamera(p);
            cam.transform.rotation = Quaternion.Euler(33f, target.eulerAngles.y, 0f);
            return;
        }

        Vector3 cameraForward = cam.transform.forward;
        cameraForward.y = 0f;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.forward;
        cameraForward.Normalize();

        float height = Mathf.Clamp(cam.transform.position.y, 18f, 600f);
        Vector3 position = target.position - cameraForward * Mathf.Clamp(height * 0.72f, 22f, 260f);
        position.y = height;
        cam.transform.position = ClampCamera(position);
    }

    private static Vector3 ClampCamera(Vector3 p)
    {
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 10f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 10f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, 9.5f, 600f);
        return p;
    }

    private int GetSelectedBattalion(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static int AggregateStrength(PrototypeRegimentHierarchy09F27 hierarchy, int battalion)
    {
        if (hierarchy == null || !hierarchy.Installed)
            return 0;

        int total = 0;
        int start = battalion < 0 ? 0 : battalion;
        int end = battalion < 0 ? hierarchy.BattalionCount : battalion + 1;
        for (int b = start; b < end; b++)
        {
            IReadOnlyList<Regiment> units = hierarchy.GetCompanies(b);
            if (units == null)
                continue;
            for (int i = 0; i < units.Count; i++)
                if (units[i] != null)
                    total += units[i].CurrentStrength;
        }
        return total;
    }

    private string GetStatus(Regiment unit)
    {
        if (unit == null)
            return "—";
        if (unit.IsRouted)
            return "ROUT";

        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && charge.IsChargeMeleeParticipant(unit))
            return "MELEE";
        if (charge != null && charge.IsCharging(unit))
            return "CHARGE";
        if (PrototypeInfantrySquare09F29.IsInSquare(unit))
            return "SQUARE";
        if (PrototypeAttackContact09F29G.IsLocalContact(unit))
            return "KAMP";
        if (PrototypeUnderFireReaction09F26.IsReacting(unit))
            return "ILD";

        if (regimentHasDestinationField != null)
        {
            object value = regimentHasDestinationField.GetValue(unit);
            if (value is bool && (bool)value)
                return "→";
        }
        return "HOLD";
    }

    private static void ConsumePointer()
    {
        // Button action runs on release after the OOB has performed its selection.
        // Reset prevents the same release from leaking into a later same-frame input layer.
        Input.ResetInputAxes();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(PanelColor);
        panelStyle.padding = new RectOffset(0, 0, 0, 0);

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.normal.textColor = HqColor;

        rowButtonStyle = new GUIStyle(GUI.skin.button);
        rowButtonStyle.normal.background = null;
        rowButtonStyle.hover.background = MakeTexture(new Color(0.22f, 0.28f, 0.14f, 0.38f));
        rowButtonStyle.active.background = MakeTexture(new Color(0.35f, 0.32f, 0.10f, 0.50f));
        rowButtonStyle.border = new RectOffset(0, 0, 0, 0);

        echelonStyle = new GUIStyle(GUI.skin.label);
        echelonStyle.fontSize = 9;
        echelonStyle.fontStyle = FontStyle.Bold;
        echelonStyle.alignment = TextAnchor.MiddleCenter;
        echelonStyle.normal.textColor = FriendlyColor;

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 9;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.normal.textColor = TextColor;

        rightStyle = new GUIStyle(GUI.skin.label);
        rightStyle.fontSize = 8;
        rightStyle.alignment = TextAnchor.MiddleRight;
        rightStyle.normal.textColor = MutedColor;

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
