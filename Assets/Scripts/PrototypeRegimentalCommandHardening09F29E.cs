using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29e
// Regimental command readability / formation hardening.
// - Gives the Oberstløjtnant the same compact red/green HUD language as F29C.
// - Keeps the selected regimental objective visible after the click.
// - Selecting the Oberstløjtnant shows all company destinations and the complete
//   Oberstløjtnant -> Major -> company command chain.
// - Repairs the overly-wide F28 defensive battalion split so a DEFEND objective is
//   actually covered through the centre rather than leaving an open corridor.
// - Repairs front/reserve and front/flank role assignment using tactical progress.
// - Tightens visual/physical slot arrival so a company cannot be marked ARRIVED while
//   its centre is still several metres outside the displayed destination footprint.

[DefaultExecutionOrder(-31000)]
public sealed class PrototypeRegimentalHud09F29E : MonoBehaviour
{
    private const float HudHeight = 90f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private FieldInfo pendingOrderField;
    private FieldInfo lastOrderTextField;
    private FieldInfo lastDecisionTextField;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle sectionStyle;
    private GUIStyle greenButtonStyle;
    private GUIStyle redButtonStyle;
    private GUIStyle smallValueStyle;

    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D greenTexture;
    private Texture2D greenHoverTexture;
    private Texture2D redTexture;
    private Texture2D redHoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalHud09F29E>() == null)
            new GameObject("PrototypeRegimentalHud_v000009f29e")
                .AddComponent<PrototypeRegimentalHud09F29E>();
    }

    private void Awake()
    {
        pendingOrderField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("pendingTargetOrder", PrivateInstance);
        lastOrderTextField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("lastOrderText", PrivateInstance);
        lastDecisionTextField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("lastDecisionText", PrivateInstance);
        BuildStyles();
    }

    private void OnGUI()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (regimental == null || !regimental.Installed || !regimental.Selected ||
            hierarchy == null || !hierarchy.Installed)
        {
            return;
        }

        BuildStyles();
        GUI.depth = -6500;

        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            "1. REGIMENT | OBERSTLØJTNANT | REGIMENTSKOMMANDO", headerStyle);

        float width = panel.width;
        float infoWidth = Mathf.Clamp(width * 0.24f, 285f, 355f);
        float battalionWidth = Mathf.Clamp(width * 0.31f, 390f, 500f);
        float commandX = infoWidth + 8f;
        float battalionX = width - battalionWidth - 6f;
        float commandWidth = Mathf.Max(420f, battalionX - commandX - 7f);
        float y = panel.y + 21f;

        List<Regiment> all = GetAllCompanies(hierarchy);
        AggregateStats stats = CalculateStats(all);
        DrawSectionLabel(new Rect(7f, y, infoWidth - 12f, 10f), "ENHEDSINFO / AI");
        GUI.Label(new Rect(9f, y + 10f, infoWidth - 14f, 13f),
            "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses +
            " | Moral " + stats.Morale.ToString("0"), smallValueStyle);
        GUI.Label(new Rect(9f, y + 23f, infoWidth - 14f, 13f),
            "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") + " r/m",
            smallValueStyle);

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
                row, smallValueStyle);
        }

        string orderText = ReadString(lastOrderTextField, regimental);
        string decisionText = ReadString(lastDecisionTextField, regimental);
        if (!string.IsNullOrEmpty(orderText))
            GUI.Label(new Rect(battalionX + 2f, y + 51f, battalionWidth - 7f, 12f),
                orderText + (string.IsNullOrEmpty(decisionText) ? string.Empty : " | " + decisionText),
                smallValueStyle);

        // Consume the already-reserved 90 px command area after our controls have run.
        // The legacy F28 HUD remains a logic dependency but must not double-fire below us.
        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition))
        {
            if (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
                current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel)
            {
                current.Use();
            }
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
            Debug.Log("HUD-09F29E|RegimentalOrderPending=True|Order=" + order);
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
        object value = field.GetValue(instance);
        return value as string ?? string.Empty;
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

        int count = 0;
        float morale = 0f;
        float cohesion = 0f;
        float ammo = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null)
                continue;
            result.Initial += unit.InitialStrength;
            result.Current += unit.CurrentStrength;
            morale += unit.Morale;
            cohesion += unit.Cohesion;
            ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(unit);
            count++;
        }

        result.Losses = Mathf.Max(0, result.Initial - result.Current);
        if (count > 0)
        {
            result.Morale = morale / count;
            result.Cohesion = cohesion / count;
            result.Ammo = ammo / count;
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

        panelTexture = MakeTexture(new Color(0.055f, 0.065f, 0.055f, 0.995f), "HUD29E_PANEL");
        headerTexture = MakeTexture(new Color(0.13f, 0.16f, 0.11f, 1f), "HUD29E_HEADER");
        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "HUD29E_GREEN");
        greenHoverTexture = MakeTexture(new Color(0.22f, 0.56f, 0.25f, 1f), "HUD29E_GREEN_HOVER");
        redTexture = MakeTexture(new Color(0.43f, 0.14f, 0.12f, 1f), "HUD29E_RED");
        redHoverTexture = MakeTexture(new Color(0.57f, 0.19f, 0.16f, 1f), "HUD29E_RED_HOVER");

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

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = new Color(0.94f, 0.93f, 0.85f);
        labelStyle.fontSize = 9;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        sectionStyle = new GUIStyle(labelStyle);
        sectionStyle.normal.textColor = new Color(0.77f, 0.67f, 0.35f);
        sectionStyle.fontSize = 8;
        sectionStyle.fontStyle = FontStyle.Bold;

        smallValueStyle = new GUIStyle(labelStyle);
        smallValueStyle.fontSize = 8;
        smallValueStyle.clipping = TextClipping.Clip;

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

[DefaultExecutionOrder(33500)]
public sealed class PrototypeRegimentalCommandHardening09F29E : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const float DefendBattalionHalfSeparation = 84f;
    private const float BattalionReserveDepth = 285f;
    private const float BattalionFlankOffset = 285f;
    private const float ExactSlotArrival = 0.85f;
    private const float OriginalArrivalWindow = 5.0f;
    private const float LinkHeight = 0.88f;
    private const int LinkSamples = 28;
    private const int ObjectiveRingSamples = 96;

    private PrototypeRegimentHierarchy09F27 hierarchy;
    private PrototypeRegimentalHQ09F28 regimental;
    private Camera cam;

    private FieldInfo currentMissionField;
    private FieldInfo hierarchyVisualsField;
    private FieldInfo hierarchyLinksField;

    private object lastProcessedMission;
    private readonly HashSet<Regiment> preciseArrivalOwned = new HashSet<Regiment>();

    private LineRenderer objectiveRing;
    private LineRenderer objectiveCrossA;
    private LineRenderer objectiveCrossB;
    private Material objectiveMaterial;
    private GUIStyle objectiveLabelStyle;
    private MajorOrder09F18 visibleObjectiveOrder = MajorOrder09F18.None;
    private Vector3 visibleObjectivePoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalCommandHardening09F29E>() == null)
            new GameObject("PrototypeRegimentalCommandHardening_v000009f29e")
                .AddComponent<PrototypeRegimentalCommandHardening09F29E>();
    }

    private void Awake()
    {
        currentMissionField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("currentMission", PrivateInstance);
        hierarchyVisualsField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("visuals", PrivateInstance);
        hierarchyLinksField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("commandLinks", PrivateInstance);

        EnsureObjectiveVisuals();
        Debug.Log(
            "REG-HARDEN-09F29E|Installed=True|RegimentalHUD=True|PersistentObjective=True|" +
            "RegimentalShowsCompanyGoals=True|RegimentalShowsMajorCompanyLinks=True|" +
            "DefendContinuousCentre=True|PreciseSlotArrival=" + ExactSlotArrival.ToString("0.00") +
            "|AttackRoleCorrection=True");
    }

    private void Update()
    {
        Resolve();
        if (hierarchy == null || !hierarchy.Installed || regimental == null || !regimental.Installed)
            return;

        ProcessNewRegimentalMission();
        EnforcePreciseSlotArrival();
    }

    private void LateUpdate()
    {
        Resolve();
        if (hierarchy == null || !hierarchy.Installed || regimental == null || !regimental.Installed)
            return;

        RefreshRegimentalObjective();
        if (!regimental.Selected)
            return;

        ShowCompleteCommandChain();
        ShowAllCompanyMissionVisuals();
    }

    private void OnGUI()
    {
        if (cam == null || visibleObjectiveOrder == MajorOrder09F18.None ||
            regimental == null || !regimental.Selected)
        {
            return;
        }

        Vector3 screen = cam.WorldToScreenPoint(visibleObjectivePoint + Vector3.up * 1.0f);
        if (screen.z <= 0f)
            return;

        if (objectiveLabelStyle == null)
        {
            objectiveLabelStyle = new GUIStyle(GUI.skin.box);
            objectiveLabelStyle.fontSize = 9;
            objectiveLabelStyle.fontStyle = FontStyle.Bold;
            objectiveLabelStyle.alignment = TextAnchor.MiddleCenter;
            objectiveLabelStyle.normal.textColor = Color.white;
        }

        Vector2 gui = new Vector2(screen.x, Screen.height - screen.y);
        Rect rect = new Rect(gui.x - 90f, gui.y - 18f, 180f, 18f);
        if (rect.yMax < Screen.height - 94f)
        {
            GUI.depth = -4400;
            GUI.Box(rect, Label(visibleObjectiveOrder) + " | REGIMENT", objectiveLabelStyle);
        }
    }

    private void Resolve()
    {
        if (hierarchy == null)
            hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (regimental == null)
            regimental = PrototypeRegimentalHQ09F28.Instance;
        if (cam == null)
            cam = Camera.main;
    }

    private void ProcessNewRegimentalMission()
    {
        object mission = currentMissionField != null ? currentMissionField.GetValue(regimental) : null;
        if (ReferenceEquals(mission, lastProcessedMission))
            return;

        lastProcessedMission = mission;
        if (!TryReadMission(mission, out MajorOrder09F18 order, out Vector3 objective))
            return;

        if (order == MajorOrder09F18.DefendHere)
        {
            ApplyCompactDefense(objective);
            return;
        }

        if (order == MajorOrder09F18.AttackHere)
            ApplyAttackRoleCorrection(objective);
    }

    private void ApplyCompactDefense(Vector3 objective)
    {
        Vector3 forward = DirectionTowardNearestEnemy(objective, 1200f);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(objective - regimental.HqRoot.transform.position);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        Vector3 slotA = Ground(objective - lateral * DefendBattalionHalfSeparation);
        Vector3 slotB = Ground(objective + lateral * DefendBattalionHalfSeparation);
        AssignTwoBattalionGoals(MajorOrder09F18.DefendHere, slotA, slotB, false);

        Debug.Log(
            "REG-DEFEND-09F29E|Objective=" + objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
            "|HalfSeparation=" + DefendBattalionHalfSeparation.ToString("0") +
            "|CentreCovered=True|Reason=ContinuousSixCompanyFrontWithBattalionReserves");
    }

    private void ApplyAttackRoleCorrection(Vector3 objective)
    {
        int knownEnemies = CountEnemies(objective, 1200f);
        bool useReserve = regimental.Doctrine == OfficerAIDoctrine.Defensive ||
                          (regimental.Doctrine == OfficerAIDoctrine.Balanced && knownEnemies <= 4);
        bool useFlank = regimental.Doctrine == OfficerAIDoctrine.Offensive && knownEnemies <= 3;

        if (!useReserve && !useFlank)
            return;

        Vector3 forward = Flat(objective - regimental.HqRoot.transform.position);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        float d0 = PlanarDistance(hierarchy.GetBattalionCenter(0), objective);
        float d1 = PlanarDistance(hierarchy.GetBattalionCenter(1), objective);
        int frontIndex = d0 <= d1 ? 0 : 1;
        int secondIndex = 1 - frontIndex;

        Vector3 frontGoal;
        Vector3 secondGoal;
        string role;

        if (useFlank)
        {
            frontGoal = Ground(objective - lateral * 110f);
            secondGoal = Ground(objective + lateral * BattalionFlankOffset - forward * 45f);
            role = "FRONT+FLANK";
        }
        else
        {
            frontGoal = Ground(objective);
            secondGoal = Ground(objective - forward * BattalionReserveDepth);
            role = "FRONT+RESERVE";
        }

        hierarchy.IssueBattalionOrderFromRegiment(frontIndex, MajorOrder09F18.AttackHere, frontGoal, regimental.Doctrine);
        hierarchy.IssueBattalionOrderFromRegiment(secondIndex, MajorOrder09F18.AttackHere, secondGoal, regimental.Doctrine);

        Debug.Log(
            "REG-ATTACK-09F29E|Role=" + role +
            "|FrontBattalion=" + (frontIndex + 1) +
            "|SecondBattalion=" + (secondIndex + 1) +
            "|FrontRule=NearestBattalionToObjective|KnownEnemies=" + knownEnemies);
    }

    private void AssignTwoBattalionGoals(
        MajorOrder09F18 order,
        Vector3 slotA,
        Vector3 slotB,
        bool preserveRole)
    {
        GameObject major0 = hierarchy.GetMajorHq(0);
        GameObject major1 = hierarchy.GetMajorHq(1);
        if (major0 == null || major1 == null)
            return;

        float normalCost = PlanarDistance(major0.transform.position, slotA) +
                           PlanarDistance(major1.transform.position, slotB);
        float swappedCost = PlanarDistance(major0.transform.position, slotB) +
                            PlanarDistance(major1.transform.position, slotA);

        Vector3 goal0 = normalCost <= swappedCost ? slotA : slotB;
        Vector3 goal1 = normalCost <= swappedCost ? slotB : slotA;
        hierarchy.IssueBattalionOrderFromRegiment(0, order, goal0, regimental.Doctrine);
        hierarchy.IssueBattalionOrderFromRegiment(1, order, goal1, regimental.Doctrine);
    }

    private void EnforcePreciseSlotArrival()
    {
        IDictionary visuals = hierarchyVisualsField != null
            ? hierarchyVisualsField.GetValue(hierarchy) as IDictionary
            : null;
        if (visuals == null)
            return;

        List<Regiment> completed = null;
        HashSet<Regiment> present = new HashSet<Regiment>();

        foreach (DictionaryEntry entry in visuals)
        {
            Regiment unit = entry.Key as Regiment;
            object visual = entry.Value;
            if (unit == null || visual == null)
                continue;
            present.Add(unit);

            if (!TryReadVisual(visual, out Vector3 goal, out Vector3 facing, out MajorOrder09F18 order,
                    out bool arrived, out LineRenderer path, out LineRenderer footprint,
                    out FieldInfo arrivedField))
            {
                continue;
            }

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled || unit.IsRouted)
            {
                if (preciseArrivalOwned.Contains(unit))
                    Add(ref completed, unit);
                continue;
            }

            float distance = PlanarDistance(unit.transform.position, goal);
            bool reacting = PrototypeUnderFireReaction09F26.IsReacting(unit);

            if (preciseArrivalOwned.Contains(unit))
            {
                if (reacting)
                    continue;

                if (distance <= ExactSlotArrival)
                {
                    unit.OrderHold();
                    Face(unit, facing);
                    controller.enabled = true;
                    arrivedField.SetValue(visual, true);
                    Add(ref completed, unit);
                    Debug.Log("HQ-ARRIVAL-09F29E|Unit=" + unit.RegimentName +
                              "|PreciseArrival=True|Distance=" + distance.ToString("0.00") +
                              "|Goal=" + goal.x.ToString("0.0") + "," + goal.z.ToString("0.0"));
                }
                else
                {
                    controller.enabled = false;
                    unit.OrderMove(goal);
                    arrivedField.SetValue(visual, false);
                }
                continue;
            }

            // F27 historically marked arrival at 4.5 m. The displayed footprint is only
            // 6.4 m deep, so the formation centre could visibly stop before entering it.
            // Only correct that narrow legacy window; do not override genuine tactical
            // pauses farther from the assigned slot.
            if (arrived && distance > ExactSlotArrival && distance <= OriginalArrivalWindow && !reacting)
            {
                preciseArrivalOwned.Add(unit);
                controller.enabled = false;
                unit.OrderMove(goal);
                arrivedField.SetValue(visual, false);
                Debug.Log("HQ-ARRIVAL-09F29E|Unit=" + unit.RegimentName +
                          "|CorrectionStarted=True|LegacyDistance=" + distance.ToString("0.00"));
            }
        }

        if (completed != null)
            for (int i = 0; i < completed.Count; i++)
                preciseArrivalOwned.Remove(completed[i]);

        if (preciseArrivalOwned.Count > 0)
        {
            List<Regiment> stale = null;
            foreach (Regiment unit in preciseArrivalOwned)
            {
                if (unit != null && present.Contains(unit))
                    continue;
                Add(ref stale, unit);
            }
            if (stale != null)
                for (int i = 0; i < stale.Count; i++)
                    preciseArrivalOwned.Remove(stale[i]);
        }
    }

    private void ShowCompleteCommandChain()
    {
        IDictionary linksByBattalion = hierarchyLinksField != null
            ? hierarchyLinksField.GetValue(hierarchy) as IDictionary
            : null;
        if (linksByBattalion == null)
            return;

        for (int battalionIndex = 0; battalionIndex < hierarchy.BattalionCount; battalionIndex++)
        {
            GameObject major = hierarchy.GetMajorHq(battalionIndex);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
            if (major == null || companies == null || !linksByBattalion.Contains(battalionIndex))
                continue;

            List<LineRenderer> lines = linksByBattalion[battalionIndex] as List<LineRenderer>;
            if (lines == null)
                continue;

            for (int i = 0; i < lines.Count && i < companies.Count; i++)
            {
                LineRenderer line = lines[i];
                Regiment company = companies[i];
                if (line == null || company == null)
                    continue;
                line.enabled = true;
                SetTerrainFollowingConnection(line, major.transform.position, company.transform.position, LinkHeight);
            }
        }
    }

    private void ShowAllCompanyMissionVisuals()
    {
        IDictionary visuals = hierarchyVisualsField != null
            ? hierarchyVisualsField.GetValue(hierarchy) as IDictionary
            : null;
        if (visuals == null)
            return;

        foreach (DictionaryEntry entry in visuals)
        {
            Regiment unit = entry.Key as Regiment;
            object visual = entry.Value;
            if (unit == null || visual == null)
                continue;

            if (!TryReadVisual(visual, out Vector3 goal, out Vector3 facing, out MajorOrder09F18 order,
                    out bool arrived, out LineRenderer path, out LineRenderer footprint,
                    out FieldInfo arrivedField))
            {
                continue;
            }

            if (footprint != null)
            {
                footprint.enabled = true;
                DrawFootprint(footprint, goal, facing, 0.58f);
            }

            if (path != null)
            {
                path.enabled = !arrived;
                if (!arrived)
                    SetTerrainFollowingConnection(path, unit.transform.position, goal, 0.60f);
            }
        }
    }

    private void RefreshRegimentalObjective()
    {
        EnsureObjectiveVisuals();
        object mission = currentMissionField != null ? currentMissionField.GetValue(regimental) : null;
        if (!regimental.Selected || !TryReadMission(mission, out MajorOrder09F18 order, out Vector3 objective) ||
            order == MajorOrder09F18.HoldPosition || order == MajorOrder09F18.None)
        {
            SetObjectiveVisible(false);
            visibleObjectiveOrder = MajorOrder09F18.None;
            return;
        }

        float radius;
        switch (order)
        {
            case MajorOrder09F18.DefendHere: radius = 72f; break;
            case MajorOrder09F18.AttackHere: radius = 58f; break;
            case MajorOrder09F18.AssembleHere: radius = 40f; break;
            default: radius = 50f; break;
        }

        Color color = ColorFor(order);
        objectiveMaterial.color = color;
        DrawTerrainCircle(objectiveRing, objective, radius, 0.95f);
        DrawTerrainCross(objectiveCrossA, objectiveCrossB, objective, Mathf.Max(15f, radius * 0.35f), 0.98f);
        SetObjectiveVisible(true);
        visibleObjectiveOrder = order;
        visibleObjectivePoint = objective;
    }

    private bool TryReadMission(object mission, out MajorOrder09F18 order, out Vector3 objective)
    {
        order = MajorOrder09F18.None;
        objective = Vector3.zero;
        if (mission == null)
            return false;

        Type type = mission.GetType();
        FieldInfo orderField = type.GetField("Order", AnyInstance);
        FieldInfo objectiveField = type.GetField("Objective", AnyInstance);
        if (orderField == null || objectiveField == null)
            return false;

        object o = orderField.GetValue(mission);
        object p = objectiveField.GetValue(mission);
        if (!(o is MajorOrder09F18) || !(p is Vector3))
            return false;

        order = (MajorOrder09F18)o;
        objective = (Vector3)p;
        return true;
    }

    private bool TryReadVisual(
        object visual,
        out Vector3 goal,
        out Vector3 facing,
        out MajorOrder09F18 order,
        out bool arrived,
        out LineRenderer path,
        out LineRenderer footprint,
        out FieldInfo arrivedField)
    {
        goal = Vector3.zero;
        facing = Vector3.forward;
        order = MajorOrder09F18.None;
        arrived = false;
        path = null;
        footprint = null;
        arrivedField = null;
        if (visual == null)
            return false;

        Type type = visual.GetType();
        FieldInfo goalField = type.GetField("Goal", AnyInstance);
        FieldInfo facingField = type.GetField("Facing", AnyInstance);
        FieldInfo orderField = type.GetField("Order", AnyInstance);
        arrivedField = type.GetField("Arrived", AnyInstance);
        FieldInfo pathField = type.GetField("Path", AnyInstance);
        FieldInfo footprintField = type.GetField("Footprint", AnyInstance);
        if (goalField == null || facingField == null || orderField == null || arrivedField == null)
            return false;

        object g = goalField.GetValue(visual);
        object f = facingField.GetValue(visual);
        object o = orderField.GetValue(visual);
        object a = arrivedField.GetValue(visual);
        if (!(g is Vector3) || !(f is Vector3) || !(o is MajorOrder09F18) || !(a is bool))
            return false;

        goal = (Vector3)g;
        facing = (Vector3)f;
        order = (MajorOrder09F18)o;
        arrived = (bool)a;
        if (pathField != null)
            path = pathField.GetValue(visual) as LineRenderer;
        if (footprintField != null)
            footprint = footprintField.GetValue(visual) as LineRenderer;
        return true;
    }

    private void EnsureObjectiveVisuals()
    {
        if (objectiveRing != null)
            return;

        objectiveMaterial = CreateUnlit(new Color(0.35f, 0.72f, 0.90f, 0.98f), "REG29E_Objective");
        objectiveRing = CreateLine("RegimentalObjectiveRing09F29E", true, 0.48f, objectiveMaterial);
        objectiveRing.positionCount = ObjectiveRingSamples;
        objectiveCrossA = CreateLine("RegimentalObjectiveCrossA09F29E", false, 0.42f, objectiveMaterial);
        objectiveCrossA.positionCount = 2;
        objectiveCrossB = CreateLine("RegimentalObjectiveCrossB09F29E", false, 0.42f, objectiveMaterial);
        objectiveCrossB.positionCount = 2;
        SetObjectiveVisible(false);
    }

    private LineRenderer CreateLine(string name, bool loop, float width, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private void SetObjectiveVisible(bool visible)
    {
        if (objectiveRing != null) objectiveRing.enabled = visible;
        if (objectiveCrossA != null) objectiveCrossA.enabled = visible;
        if (objectiveCrossB != null) objectiveCrossB.enabled = visible;
    }

    private static void DrawTerrainCircle(LineRenderer line, Vector3 center, float radius, float yOffset)
    {
        if (line == null)
            return;
        line.positionCount = ObjectiveRingSamples;
        for (int i = 0; i < ObjectiveRingSamples; i++)
        {
            float angle = i / (float)ObjectiveRingSamples * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static void DrawTerrainCross(
        LineRenderer a,
        LineRenderer b,
        Vector3 center,
        float half,
        float yOffset)
    {
        if (a == null || b == null)
            return;

        Vector3 a0 = center + new Vector3(-half, 0f, 0f);
        Vector3 a1 = center + new Vector3(half, 0f, 0f);
        Vector3 b0 = center + new Vector3(0f, 0f, -half);
        Vector3 b1 = center + new Vector3(0f, 0f, half);
        a0.y = PrototypeBootstrap.SampleGroundHeight(a0.x, a0.z) + yOffset;
        a1.y = PrototypeBootstrap.SampleGroundHeight(a1.x, a1.z) + yOffset;
        b0.y = PrototypeBootstrap.SampleGroundHeight(b0.x, b0.z) + yOffset;
        b1.y = PrototypeBootstrap.SampleGroundHeight(b1.x, b1.z) + yOffset;
        a.SetPosition(0, a0);
        a.SetPosition(1, a1);
        b.SetPosition(0, b0);
        b.SetPosition(1, b1);
    }

    private static void SetTerrainFollowingConnection(LineRenderer line, Vector3 a, Vector3 b, float yOffset)
    {
        if (line == null)
            return;
        line.positionCount = LinkSamples + 1;
        for (int i = 0; i <= LinkSamples; i++)
        {
            float t = i / (float)LinkSamples;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static void DrawFootprint(LineRenderer line, Vector3 center, Vector3 facing, float yOffset)
    {
        facing = Flat(facing);
        Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
        Vector3[] points =
        {
            center - right * 24f - facing * 3.2f,
            center + right * 24f - facing * 3.2f,
            center + right * 24f + facing * 3.2f,
            center - right * 24f + facing * 3.2f,
            center - right * 24f - facing * 3.2f
        };
        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 p = points[i];
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private Vector3 DirectionTowardNearestEnemy(Vector3 point, float range)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return Vector3.zero;

        Regiment nearest = null;
        float best = range;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == BattleTeam.Denmark ||
                candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;
            float distance = PlanarDistance(point, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }
        return nearest != null ? Flat(nearest.transform.position - point) : Vector3.zero;
    }

    private int CountEnemies(Vector3 point, float range)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return 0;
        int count = 0;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == BattleTeam.Denmark ||
                candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;
            if (PlanarDistance(point, candidate.transform.position) <= range)
                count++;
        }
        return count;
    }

    private static void Face(Regiment unit, Vector3 facing)
    {
        if (unit == null)
            return;
        facing = Flat(facing);
        if (facing.sqrMagnitude < 0.01f)
            return;
        unit.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
    }

    private static void Add(ref List<Regiment> list, Regiment unit)
    {
        if (list == null)
            list = new List<Regiment>();
        list.Add(unit);
    }

    private static Color ColorFor(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder09F18.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder09F18.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    private static string Label(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB HER";
            case MajorOrder09F18.DefendHere: return "FORSVAR HER";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.AssembleHere: return "SAML";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            default: return "INGEN";
        }
    }

    private static Material CreateUnlit(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
