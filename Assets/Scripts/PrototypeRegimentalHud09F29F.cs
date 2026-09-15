using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29f
// Dedicated Oberstløjtnant HUD replacement.
// F29E embedded PrototypeRegimentalHud09F29E inside another script file and also
// built IMGUI styles from Awake. In runtime QA the replacement HUD was not reliably
// visible and the legacy F28 panel remained on screen. F29F moves the replacement
// to its own matching script/class file, creates IMGUI styles only from OnGUI, does
// not require the hierarchy to be fully installed before drawing the core panel,
// and disables the old F29E overlay if it happens to exist.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeRegimentalHud09F29F : MonoBehaviour
{
    private const float HudHeight = 90f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private FieldInfo pendingOrderField;
    private FieldInfo lastOrderTextField;
    private FieldInfo lastDecisionTextField;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle valueStyle;
    private GUIStyle greenButtonStyle;
    private GUIStyle redButtonStyle;

    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D greenTexture;
    private Texture2D greenHoverTexture;
    private Texture2D redTexture;
    private Texture2D redHoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalHud09F29F>() == null)
            new GameObject("PrototypeRegimentalHud_v000009f29f")
                .AddComponent<PrototypeRegimentalHud09F29F>();
    }

    private void Awake()
    {
        pendingOrderField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("pendingTargetOrder", PrivateInstance);
        lastOrderTextField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("lastOrderText", PrivateInstance);
        lastDecisionTextField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("lastDecisionText", PrivateInstance);

        // F29F is the authoritative replacement HUD. If the experimental F29E
        // overlay did instantiate, turn only that overlay off; F28 command logic stays alive.
        PrototypeRegimentalHud09F29E oldHud =
            UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalHud09F29E>();
        if (oldHud != null)
            oldHud.enabled = false;

        Debug.Log("HUD-09F29F|Installed=True|DedicatedScript=True|LegacyF29EOverlayDisabled=" +
                  (oldHud != null) + "|DrawWithoutHierarchyGate=True");
    }

    private void OnGUI()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed || !regimental.Selected)
            return;

        BuildStyles();
        GUI.depth = -100000;

        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            "1. REGIMENT | OBERSTLØJTNANT | REGIMENTSKOMMANDO | F29F", headerStyle);

        float width = panel.width;
        float infoWidth = Mathf.Clamp(width * 0.24f, 280f, 350f);
        float battalionWidth = Mathf.Clamp(width * 0.31f, 370f, 500f);
        float commandX = infoWidth + 8f;
        float battalionX = width - battalionWidth - 6f;
        float commandWidth = Mathf.Max(400f, battalionX - commandX - 7f);
        float y = panel.y + 21f;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        List<Regiment> all = hierarchy != null && hierarchy.Installed
            ? GetAllCompanies(hierarchy)
            : new List<Regiment>();
        AggregateStats stats = CalculateStats(all);

        DrawSectionLabel(new Rect(7f, y, infoWidth - 12f, 10f), "ENHEDSINFO / AI");
        if (stats.Count > 0)
        {
            GUI.Label(new Rect(9f, y + 10f, infoWidth - 14f, 13f),
                "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses +
                " | Moral " + stats.Morale.ToString("0"), valueStyle);
            GUI.Label(new Rect(9f, y + 23f, infoWidth - 14f, 13f),
                "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") + " r/m",
                valueStyle);
        }
        else
        {
            GUI.Label(new Rect(9f, y + 10f, infoWidth - 14f, 26f),
                "Regiments HQ aktivt | venter på bataljonsstatus", valueStyle);
        }

        bool aiOn = regimental.AIEnabled;
        OfficerAIDoctrine doctrine = regimental.Doctrine;
        float stateY = y + 39f;
        const float stateGap = 3f;
        float stateW = (infoWidth - 15f - stateGap * 3f) / 4f;

        if (GUI.Button(new Rect(8f, stateY, stateW, 19f), aiOn ? "AI ON" : "AI OFF", StateStyle(aiOn)))
            regimental.ToggleAI();
        if (GUI.Button(new Rect(8f + (stateW + stateGap), stateY, stateW, 19f), "DEF",
                StateStyle(doctrine == OfficerAIDoctrine.Defensive)))
            regimental.SetDoctrine(OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(8f + (stateW + stateGap) * 2f, stateY, stateW, 19f), "BAL",
                StateStyle(doctrine == OfficerAIDoctrine.Balanced)))
            regimental.SetDoctrine(OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(8f + (stateW + stateGap) * 3f, stateY, stateW, 19f), "OFF",
                StateStyle(doctrine == OfficerAIDoctrine.Offensive)))
            regimental.SetDoctrine(OfficerAIDoctrine.Offensive);

        DrawSectionLabel(new Rect(commandX, y, commandWidth, 10f), "REGIMENTSORDRER");
        MajorOrder09F18 pending = GetPendingOrder(regimental);
        const float gap = 4f;
        float orderW = (commandWidth - gap * 2f) / 3f;
        float row1 = y + 11f;
        float row2 = y + 36f;

        DrawOrderButton(regimental, new Rect(commandX, row1, orderW, 21f),
            "ANGRIB HER", MajorOrder09F18.AttackHere, pending);
        DrawOrderButton(regimental, new Rect(commandX + orderW + gap, row1, orderW, 21f),
            "FORSVAR HER", MajorOrder09F18.DefendHere, pending);
        DrawOrderButton(regimental, new Rect(commandX + (orderW + gap) * 2f, row1, orderW, 21f),
            "RYK FREM", MajorOrder09F18.AdvanceHere, pending);
        DrawOrderButton(regimental, new Rect(commandX, row2, orderW, 21f),
            "TILBAGETRÆK", MajorOrder09F18.WithdrawHere, pending);
        DrawOrderButton(regimental, new Rect(commandX + orderW + gap, row2, orderW, 21f),
            "SAML", MajorOrder09F18.AssembleHere, pending);

        if (GUI.Button(new Rect(commandX + (orderW + gap) * 2f, row2, orderW, 21f),
                "STOP / HOLD", redButtonStyle))
        {
            SetPendingOrder(regimental, MajorOrder09F18.None);
            Vector3 point = regimental.HqRoot != null
                ? regimental.HqRoot.transform.position
                : Vector3.zero;
            regimental.IssueRegimentalOrder(MajorOrder09F18.HoldPosition, point, false);
        }

        DrawSectionLabel(new Rect(battalionX, y, battalionWidth - 4f, 10f),
            "BATALJONER UNDER OBERSTLØJTNANT — MÆND / TAB / MORAL / AMMO");

        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount && i < 2; i++)
            {
                IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(i);
                AggregateStats battalion = CalculateStats(companies);
                bool battalionAi = hierarchy.GetBattalionAIEnabled(i);
                OfficerAIDoctrine battalionDoctrine = hierarchy.GetBattalionDoctrine(i);
                string row = (i + 1) + ". BATALJON | MAJOR " + (i == 0 ? "A" : "B") +
                             "  " + battalion.Current + "/" + battalion.Initial +
                             "  T" + battalion.Losses +
                             "  M" + battalion.Morale.ToString("0") +
                             "  A" + battalion.Ammo.ToString("0") +
                             "  " + (battalionAi ? "AI ON" : "AI OFF") +
                             "  " + DoctrineShort(battalionDoctrine);
                GUI.Label(new Rect(battalionX + 2f, y + 12f + i * 20f, battalionWidth - 7f, 16f),
                    row, valueStyle);
            }
        }
        else
        {
            GUI.Label(new Rect(battalionX + 2f, y + 12f, battalionWidth - 7f, 16f),
                "Bataljonsstatus ikke klar endnu", valueStyle);
        }

        string orderText = ReadString(lastOrderTextField, regimental);
        string decisionText = ReadString(lastDecisionTextField, regimental);
        if (!string.IsNullOrEmpty(orderText))
        {
            GUI.Label(new Rect(battalionX + 2f, y + 51f, battalionWidth - 7f, 12f),
                orderText + (string.IsNullOrEmpty(decisionText) ? string.Empty : " | " + decisionText),
                valueStyle);
        }

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel))
        {
            current.Use();
        }
    }

    private void DrawOrderButton(
        PrototypeRegimentalHQ09F28 regimental,
        Rect rect,
        string label,
        MajorOrder09F18 order,
        MajorOrder09F18 pending)
    {
        if (GUI.Button(rect, label, StateStyle(pending == order)))
        {
            SetPendingOrder(regimental, order);
            Debug.Log("HUD-09F29F|RegimentalOrderPending=True|Order=" + order);
        }
    }

    private MajorOrder09F18 GetPendingOrder(PrototypeRegimentalHQ09F28 regimental)
    {
        if (pendingOrderField == null || regimental == null)
            return MajorOrder09F18.None;
        object value = pendingOrderField.GetValue(regimental);
        return value is MajorOrder09F18 ? (MajorOrder09F18)value : MajorOrder09F18.None;
    }

    private void SetPendingOrder(PrototypeRegimentalHQ09F28 regimental, MajorOrder09F18 order)
    {
        if (pendingOrderField != null && regimental != null)
            pendingOrderField.SetValue(regimental, order);
    }

    private static string ReadString(FieldInfo field, object instance)
    {
        if (field == null || instance == null)
            return string.Empty;
        return field.GetValue(instance) as string ?? string.Empty;
    }

    private static string DoctrineShort(OfficerAIDoctrine doctrine)
    {
        switch (doctrine)
        {
            case OfficerAIDoctrine.Defensive: return "DEF";
            case OfficerAIDoctrine.Offensive: return "OFF";
            default: return "BAL";
        }
    }

    private static List<Regiment> GetAllCompanies(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        List<Regiment> result = new List<Regiment>();
        if (hierarchy == null)
            return result;

        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null)
                continue;
            for (int i = 0; i < companies.Count; i++)
                if (companies[i] != null)
                    result.Add(companies[i]);
        }
        return result;
    }

    private struct AggregateStats
    {
        public int Count;
        public int Initial;
        public int Current;
        public int Losses;
        public float Morale;
        public float Cohesion;
        public float Ammo;
    }

    private static AggregateStats CalculateStats(IReadOnlyList<Regiment> units)
    {
        AggregateStats result = new AggregateStats();
        if (units == null)
            return result;

        float morale = 0f;
        float cohesion = 0f;
        float ammo = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null)
                continue;
            result.Count++;
            result.Initial += unit.InitialStrength;
            result.Current += unit.CurrentStrength;
            morale += unit.Morale;
            cohesion += unit.Cohesion;
            ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(unit);
        }

        result.Losses = Mathf.Max(0, result.Initial - result.Current);
        if (result.Count > 0)
        {
            result.Morale = morale / result.Count;
            result.Cohesion = cohesion / result.Count;
            result.Ammo = ammo / result.Count;
        }
        return result;
    }

    private static AggregateStats CalculateStats(List<Regiment> units)
    {
        return CalculateStats((IReadOnlyList<Regiment>)units);
    }

    private GUIStyle StateStyle(bool active)
    {
        return active ? greenButtonStyle : redButtonStyle;
    }

    private void DrawSectionLabel(Rect rect, string text)
    {
        GUI.Label(rect, text, sectionStyle);
    }

    private void BuildStyles()
    {
        if (panelStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.055f, 0.065f, 0.055f, 0.995f), "HUD29F_PANEL");
        headerTexture = MakeTexture(new Color(0.13f, 0.16f, 0.11f, 1f), "HUD29F_HEADER");
        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "HUD29F_GREEN");
        greenHoverTexture = MakeTexture(new Color(0.22f, 0.56f, 0.25f, 1f), "HUD29F_GREEN_HOVER");
        redTexture = MakeTexture(new Color(0.43f, 0.14f, 0.12f, 1f), "HUD29F_RED");
        redHoverTexture = MakeTexture(new Color(0.57f, 0.19f, 0.16f, 1f), "HUD29F_RED_HOVER");

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.padding = new RectOffset(4, 4, 3, 3);

        headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = headerTexture;
        headerStyle.normal.textColor = new Color(0.96f, 0.94f, 0.84f);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.padding = new RectOffset(7, 5, 1, 1);

        valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.normal.textColor = new Color(0.94f, 0.93f, 0.85f);
        valueStyle.fontSize = 8;
        valueStyle.alignment = TextAnchor.MiddleLeft;
        valueStyle.clipping = TextClipping.Clip;

        sectionStyle = new GUIStyle(valueStyle);
        sectionStyle.normal.textColor = new Color(0.77f, 0.67f, 0.35f);
        sectionStyle.fontStyle = FontStyle.Bold;

        greenButtonStyle = MakeButtonStyle(greenTexture, greenHoverTexture);
        redButtonStyle = MakeButtonStyle(redTexture, redHoverTexture);
    }

    private static GUIStyle MakeButtonStyle(Texture2D normal, Texture2D hover)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = hover;
        style.focused.background = hover;
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.fontSize = 8;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(2, 2, 1, 1);
        return style;
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
