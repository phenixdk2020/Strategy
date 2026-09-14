using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f17 authoritative four-company battalion HQ prototype.
// It takes over from the 09f15/09f16 two-company Major after the physical HQ has spawned.
[DefaultExecutionOrder(380)]
public sealed class PrototypeMajorBattalion09F17 : MonoBehaviour
{
    private enum BattalionOrder
    {
        None,
        AttackHere,
        DefendHere,
        WithdrawHere,
        AdvanceHere,
        HoldPosition,
        AssembleHere
    }

    private sealed class Mission
    {
        public BattalionOrder Order;
        public Vector3 Goal;
        public Vector3 Facing;
        public bool Reserve;
        public bool FlankReserve;
        public bool AttackCommitted;
    }

    public static PrototypeMajorBattalion09F17 Instance { get; private set; }

    private readonly List<Regiment> companies = new List<Regiment>();
    private readonly Dictionary<Regiment, Mission> missions = new Dictionary<Regiment, Mission>();
    private readonly Dictionary<Regiment, LineRenderer> links = new Dictionary<Regiment, LineRenderer>();

    private PrototypeMajorHQ09F15 legacy;
    private GameObject hqRoot;
    private Camera cam;
    private bool installed;
    private bool selected;
    private bool aiEnabled;
    private float nextAiDecisionAt;
    private BattalionOrder pendingOrder;
    private string lastOrder = "Ingen bataljonsordre";
    private Regiment currentReserve;

    private LineRenderer selectionRing;
    private LineRenderer targetRing;
    private Material linkMaterial;
    private Material selectionMaterial;
    private Material targetMaterial;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;
    private GUIStyle hoverStyle;

    private const float CompanySpacing = 60f;
    private const float ReserveDepth = 72f;
    private const float FlankOffset = 105f;
    private const float ArrivalDistance = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorBattalion09F17>() != null)
            return;

        GameObject root = new GameObject("PrototypeMajorBattalion_v000009f17");
        root.AddComponent<PrototypeMajorBattalion09F17>();
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
        if (cam == null)
            cam = Camera.main;

        if (!installed)
        {
            TryTakeOwnership();
            return;
        }

        RefreshCompanies();
        UpdateLinks();
        UpdateMissions();
        UpdateTargetRing();

        if (aiEnabled && Time.time >= nextAiDecisionAt)
        {
            nextAiDecisionAt = Time.time + 5f;
            RunAIDecision();
        }

        HandleSelectionAndTargetInput();
    }

    private void TryTakeOwnership()
    {
        legacy = PrototypeMajorHQ09F15.Instance;
        if (legacy == null)
            return;

        FieldInfo hqRootField = typeof(PrototypeMajorHQ09F15).GetField(
            "hqRoot", BindingFlags.Instance | BindingFlags.NonPublic);
        hqRoot = hqRootField != null ? hqRootField.GetValue(legacy) as GameObject : null;
        if (hqRoot == null)
            return;

        // The 09f15 component created the physical Major + horses. 09f17 owns behavior/UI.
        legacy.enabled = false;

        PrototypeMajorHQ09F16 oldAi = Object.FindAnyObjectByType<PrototypeMajorHQ09F16>();
        if (oldAi != null)
            oldAi.enabled = false;

        PrototypeMajorSelectionBridge09F16 oldSelection = Object.FindAnyObjectByType<PrototypeMajorSelectionBridge09F16>();
        if (oldSelection != null)
            oldSelection.enabled = false;

        LineRenderer[] oldLines = Object.FindObjectsByType<LineRenderer>();
        foreach (LineRenderer line in oldLines)
        {
            if (line != null && line.gameObject.name.StartsWith("MajorCommandLink_"))
                Destroy(line.gameObject);
        }

        CreateMaterials();
        CreateSelectionRing();
        CreateTargetRing();
        RefreshCompanies();
        RebuildLinks();

        installed = true;
        Debug.Log("HQ-09F17|Installed=True|Rank=Major|Companies=4|AI=OFF|ReserveAI=True|FlankReserve=True");
    }

    private void CreateMaterials()
    {
        linkMaterial = CreateUnlit(new Color(0.82f, 0.69f, 0.29f, 0.88f), "HQ17Link");
        selectionMaterial = CreateUnlit(new Color(1f, 0.80f, 0.18f, 0.95f), "HQ17Selection");
        targetMaterial = CreateUnlit(new Color(0.92f, 0.72f, 0.18f, 0.95f), "HQ17Target");
    }

    private static Material CreateUnlit(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private void CreateSelectionRing()
    {
        GameObject go = new GameObject("MajorSelectionRing09F17");
        go.transform.SetParent(transform, false);
        selectionRing = go.AddComponent<LineRenderer>();
        selectionRing.useWorldSpace = true;
        selectionRing.loop = true;
        selectionRing.widthMultiplier = 0.24f;
        selectionRing.positionCount = 48;
        selectionRing.sharedMaterial = selectionMaterial;
        selectionRing.enabled = false;
    }

    private void CreateTargetRing()
    {
        GameObject go = new GameObject("MajorTargetRing09F17");
        go.transform.SetParent(transform, false);
        targetRing = go.AddComponent<LineRenderer>();
        targetRing.useWorldSpace = true;
        targetRing.loop = true;
        targetRing.widthMultiplier = 0.22f;
        targetRing.positionCount = 48;
        targetRing.sharedMaterial = targetMaterial;
        targetRing.enabled = false;
    }

    private void RefreshCompanies()
    {
        Regiment[] desired =
        {
            Find("1. Regiment"),
            Find("5. Regiment"),
            Find("2. Regiment"),
            Find("3. Regiment")
        };

        bool changed = companies.Count != 4;
        if (!changed)
        {
            for (int i = 0; i < 4; i++)
                if (companies[i] != desired[i]) changed = true;
        }
        if (!changed)
            return;

        companies.Clear();
        foreach (Regiment regiment in desired)
            if (regiment != null) companies.Add(regiment);

        RebuildLinks();
    }

    private Regiment Find(string name)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return null;
        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.RegimentName == name)
                return regiment;
        return null;
    }

    private void RebuildLinks()
    {
        foreach (LineRenderer line in links.Values)
            if (line != null) Destroy(line.gameObject);
        links.Clear();

        foreach (Regiment regiment in companies)
        {
            if (regiment == null) continue;
            GameObject go = new GameObject("Major17Link_" + regiment.RegimentName);
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 20;
            line.widthMultiplier = 0.16f;
            line.numCapVertices = 2;
            line.sharedMaterial = linkMaterial;
            line.enabled = selected;
            links[regiment] = line;
        }
    }

    private void UpdateLinks()
    {
        if (hqRoot == null) return;
        foreach (KeyValuePair<Regiment, LineRenderer> pair in links)
        {
            Regiment regiment = pair.Key;
            LineRenderer line = pair.Value;
            if (line == null) continue;
            line.enabled = selected && regiment != null;
            if (!line.enabled) continue;

            Vector3 a = hqRoot.transform.position;
            Vector3 b = regiment.transform.position;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
                line.SetPosition(i, p);
            }
        }
    }

    private void HandleSelectionAndTargetInput()
    {
        if (cam == null || !Input.GetMouseButtonDown(0))
            return;

        if (IsPointerOverPanel(Input.mousePosition))
            return;

        if (selected && pendingOrder != BattalionOrder.None)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
            {
                bool useReserve = aiEnabled && (pendingOrder == BattalionOrder.AttackHere || pendingOrder == BattalionOrder.DefendHere);
                bool allowFlank = useReserve && EvaluateFlankOpportunity(point);
                IssueOrder(pendingOrder, point, useReserve, allowFlank);
                pendingOrder = BattalionOrder.None;
                targetRing.enabled = false;
            }
            return;
        }

        bool hit = RayHitsHQ(Input.mousePosition);
        selected = hit;
        if (selectionRing != null)
            selectionRing.enabled = selected;
        if (!selected)
        {
            pendingOrder = BattalionOrder.None;
            if (targetRing != null) targetRing.enabled = false;
        }
    }

    private bool RayHitsHQ(Vector3 mousePosition)
    {
        if (cam == null || hqRoot == null) return false;
        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider != null ? hit.collider.transform : null;
            if (t != null && (t == hqRoot.transform || t.IsChildOf(hqRoot.transform)))
                return true;
        }
        return false;
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = default;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null) continue;
            if (hit.collider.gameObject.name != "Battlefield Ground") continue;
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }
        return false;
    }

    private void IssueOrder(BattalionOrder order, Vector3 point, bool useReserve, bool allowFlank)
    {
        if (companies.Count == 0) return;
        missions.Clear();

        Regiment threat = FindNearestEnemy(point, 1200f);
        Vector3 forward = threat != null ? threat.transform.position - point : point - hqRoot.transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = hqRoot.transform.forward;
        forward.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;

        List<Regiment> front = new List<Regiment>(companies);
        currentReserve = useReserve ? ChooseReserve() : null;
        if (currentReserve != null)
            front.Remove(currentReserve);

        float centerIndex = (front.Count - 1) * 0.5f;
        for (int i = 0; i < front.Count; i++)
        {
            Regiment regiment = front[i];
            Vector3 goal = point + lateral * ((i - centerIndex) * CompanySpacing);
            PrepareCompany(regiment);
            missions[regiment] = new Mission
            {
                Order = order,
                Goal = Ground(goal),
                Facing = forward,
                Reserve = false,
                FlankReserve = false
            };
            ApplyMoveForOrder(regiment, order, Ground(goal));
        }

        if (currentReserve != null)
        {
            Vector3 reserveGoal = point - forward * ReserveDepth;
            bool flank = allowFlank && (order == BattalionOrder.AttackHere || order == BattalionOrder.DefendHere);
            if (flank)
            {
                Vector3 left = point - forward * 18f - lateral * FlankOffset;
                Vector3 right = point - forward * 18f + lateral * FlankOffset;
                reserveGoal = PlanarDistance(currentReserve.transform.position, left) <= PlanarDistance(currentReserve.transform.position, right)
                    ? left
                    : right;
            }

            PrepareCompany(currentReserve);
            missions[currentReserve] = new Mission
            {
                Order = order,
                Goal = Ground(reserveGoal),
                Facing = forward,
                Reserve = true,
                FlankReserve = flank
            };
            currentReserve.SetFormation(RegimentFormation.Line);
            currentReserve.OrderMove(Ground(reserveGoal));
        }

        lastOrder = Label(order) +
                    (currentReserve != null ? " | reserve: " + DisplayCompany(currentReserve) : "") +
                    (allowFlank && currentReserve != null ? " | flankeforsøg" : "");

        Debug.Log("HQ-ORDER-09F17|Order=" + order +
                  "|Companies=" + companies.Count +
                  "|Front=" + front.Count +
                  "|Reserve=" + (currentReserve != null ? currentReserve.RegimentName : "NONE") +
                  "|Flank=" + (allowFlank && currentReserve != null));
    }

    private void PrepareCompany(Regiment regiment)
    {
        if (regiment == null) return;
        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetAIEnabled(false);
    }

    private static void ApplyMoveForOrder(Regiment regiment, BattalionOrder order, Vector3 goal)
    {
        if (regiment == null) return;
        if (order == BattalionOrder.AttackHere)
            regiment.SetFirePolicy(RegimentFirePolicy.LongRange);
        if (order == BattalionOrder.WithdrawHere)
            regiment.SetFormation(RegimentFormation.Line);
        regiment.OrderMove(goal);
    }

    private void UpdateMissions()
    {
        if (missions.Count == 0) return;
        List<Regiment> completed = null;

        foreach (KeyValuePair<Regiment, Mission> pair in missions)
        {
            Regiment regiment = pair.Key;
            Mission mission = pair.Value;
            if (regiment == null || regiment.IsRouted)
            {
                AddCompleted(ref completed, regiment);
                continue;
            }

            if (mission.AttackCommitted)
                continue;

            if (mission.Order == BattalionOrder.AttackHere && !mission.Reserve)
            {
                Regiment enemy = FindNearestEnemy(regiment.transform.position, 115f);
                if (enemy != null)
                {
                    regiment.OrderAttack(enemy);
                    mission.AttackCommitted = true;
                    continue;
                }
            }

            if (mission.Reserve && mission.FlankReserve)
            {
                Regiment enemy = FindNearestEnemy(regiment.transform.position,
                    mission.Order == BattalionOrder.AttackHere ? 130f : 90f);
                float ratio = regiment.InitialStrength > 0 ? regiment.CurrentStrength / (float)regiment.InitialStrength : 0f;
                if (enemy != null && ratio >= 0.60f && regiment.Cohesion >= 55f)
                {
                    regiment.OrderAttack(enemy);
                    mission.AttackCommitted = true;
                    Debug.Log("HQ-RESERVE-09F17|Unit=" + regiment.RegimentName + "|Committed=True|Flank=True|Order=" + mission.Order);
                    continue;
                }
            }

            float distance = PlanarDistance(regiment.transform.position, mission.Goal);
            if (distance > ArrivalDistance)
            {
                regiment.OrderMove(mission.Goal);
                continue;
            }

            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            if (mission.Facing.sqrMagnitude > 0.01f)
                regiment.transform.rotation = Quaternion.LookRotation(mission.Facing.normalized, Vector3.up);

            // A reserve remains active in the mission table so AI can commit it later.
            if (!mission.Reserve)
                AddCompleted(ref completed, regiment);
        }

        if (completed == null) return;
        foreach (Regiment regiment in completed)
            if (regiment != null) missions.Remove(regiment);
    }

    private void RunAIDecision()
    {
        if (companies.Count < 4)
            return;

        Vector3 center = CompanyCenter();
        Regiment enemy = FindNearestEnemy(center, 1200f);
        if (enemy == null)
        {
            HoldAll();
            return;
        }

        float distance = PlanarDistance(center, enemy.transform.position);
        float strength = AverageStrengthRatio();
        float cohesion = AverageCohesion();
        Vector3 toward = FlatDirection(center, enemy.transform.position);

        if (strength < 0.55f || cohesion < 45f)
        {
            IssueOrder(BattalionOrder.DefendHere, center - toward * 45f, true, false);
            Debug.Log("HQ-AI-09F17|Decision=DEFEND|Reason=WEAK|Reserve=True");
            return;
        }

        bool flank = strength >= 0.72f && cohesion >= 62f && distance <= 240f;
        if (distance <= 145f)
        {
            IssueOrder(BattalionOrder.AttackHere, enemy.transform.position, true, flank);
            Debug.Log("HQ-AI-09F17|Decision=ATTACK|Reserve=True|Flank=" + flank);
            return;
        }

        if (distance <= 260f)
        {
            IssueOrder(BattalionOrder.DefendHere, center + toward * 28f, true, flank);
            Debug.Log("HQ-AI-09F17|Decision=DEFEND_FORWARD|Reserve=True|Flank=" + flank);
            return;
        }

        Vector3 advance = center + toward * Mathf.Min(85f, Mathf.Max(30f, distance - 150f));
        IssueOrder(BattalionOrder.AdvanceHere, advance, false, false);
        Debug.Log("HQ-AI-09F17|Decision=ADVANCE|Distance=" + distance.ToString("0"));
    }

    private bool EvaluateFlankOpportunity(Vector3 objective)
    {
        Regiment enemy = FindNearestEnemy(objective, 300f);
        return enemy != null && AverageStrengthRatio() >= 0.72f && AverageCohesion() >= 62f;
    }

    private Regiment ChooseReserve()
    {
        Regiment best = null;
        float bestScore = float.NegativeInfinity;
        foreach (Regiment regiment in companies)
        {
            if (regiment == null || regiment.IsRouted) continue;
            float ratio = regiment.InitialStrength > 0 ? regiment.CurrentStrength / (float)regiment.InitialStrength : 0f;
            float score = ratio * 70f + regiment.Cohesion * 0.30f;
            if (score > bestScore)
            {
                bestScore = score;
                best = regiment;
            }
        }
        return best;
    }

    private void HoldAll()
    {
        pendingOrder = BattalionOrder.None;
        missions.Clear();
        currentReserve = null;
        foreach (Regiment regiment in companies)
        {
            if (regiment == null || regiment.IsRouted) continue;
            PrepareCompany(regiment);
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
        }
        lastOrder = "HOLD POSITION";
    }

    private Regiment FindNearestEnemy(Vector3 point, float maxDistance)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return null;
        Regiment nearest = null;
        float best = maxDistance;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team != BattleTeam.Prussia || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;
            float d = PlanarDistance(point, candidate.transform.position);
            if (d >= best) continue;
            best = d;
            nearest = candidate;
        }
        return nearest;
    }

    private Vector3 CompanyCenter()
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in companies)
        {
            if (regiment == null) continue;
            total += regiment.transform.position;
            count++;
        }
        return count > 0 ? total / count : hqRoot.transform.position;
    }

    private float AverageStrengthRatio()
    {
        float total = 0f;
        int count = 0;
        foreach (Regiment regiment in companies)
        {
            if (regiment == null || regiment.InitialStrength <= 0) continue;
            total += regiment.CurrentStrength / (float)regiment.InitialStrength;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AverageCohesion()
    {
        float total = 0f;
        int count = 0;
        foreach (Regiment regiment in companies)
        {
            if (regiment == null) continue;
            total += regiment.Cohesion;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.y = 0f;
        return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void AddCompleted(ref List<Regiment> list, Regiment regiment)
    {
        if (list == null) list = new List<Regiment>();
        list.Add(regiment);
    }

    private void UpdateTargetRing()
    {
        if (targetRing == null || !selected || pendingOrder == BattalionOrder.None || cam == null)
        {
            if (targetRing != null) targetRing.enabled = false;
            UpdateSelectionRing();
            return;
        }

        if (TryGetGround(Input.mousePosition, out Vector3 point))
        {
            Color color = pendingOrder == BattalionOrder.AttackHere ? PrototypeUiTheme09F15.Attack :
                          pendingOrder == BattalionOrder.DefendHere ? PrototypeUiTheme09F15.Defend :
                          pendingOrder == BattalionOrder.WithdrawHere ? PrototypeUiTheme09F15.Withdraw :
                          PrototypeUiTheme09F15.Move;
            targetMaterial.color = color;
            DrawCircle(targetRing, point, pendingOrder == BattalionOrder.AssembleHere ? 12f : 22f, 0.42f);
            targetRing.enabled = true;
        }
        UpdateSelectionRing();
    }

    private void UpdateSelectionRing()
    {
        if (selectionRing == null || hqRoot == null) return;
        selectionRing.enabled = selected;
        if (selected) DrawCircle(selectionRing, hqRoot.transform.position, 6.2f, 0.30f);
    }

    private static void DrawCircle(LineRenderer line, Vector3 center, float radius, float y)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + y;
            line.SetPosition(i, p);
        }
    }

    private Rect PanelRect()
    {
        float width = Mathf.Min(980f, Mathf.Max(740f, Screen.width - 70f));
        const float height = 120f;
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 16f, width, height);
    }

    private bool IsPointerOverPanel(Vector3 mouse)
    {
        if (!selected) return false;
        Vector2 p = new Vector2(mouse.x, Screen.height - mouse.y);
        return PanelRect().Contains(p);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
        hoverStyle = PrototypeUiTheme09F15.Panel(9);
        hoverStyle.alignment = TextAnchor.UpperLeft;
    }

    private void OnGUI()
    {
        if (!installed) return;
        EnsureStyles();

        if (!selected && cam != null && RayHitsHQ(Input.mousePosition))
        {
            Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Rect hover = new Rect(Mathf.Clamp(p.x + 15f, 8f, Screen.width - 250f), Mathf.Clamp(p.y - 20f, 38f, Screen.height - 78f), 242f, 70f);
            GUI.Box(hover,
                "MAJOR | BATALJONS HQ\n" +
                "HQ AI: " + (aiEnabled ? "ON" : "OFF") + " | 3 heste | 4 kompagnier\n" +
                "Aktuel: " + lastOrder,
                hoverStyle);
        }

        if (!selected) return;
        GUI.depth = -860;
        Rect panel = PanelRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.Box(new Rect(panel.x + 7f, panel.y + 6f, panel.width - 14f, 22f), "BATALJONS HQ | MAJOR | 4 KOMPAGNIER", headerStyle);

        if (GUI.Button(new Rect(panel.xMax - 102f, panel.y + 7f, 88f, 20f), aiEnabled ? "HQ AI: ON" : "HQ AI: OFF", aiEnabled ? accentStyle : buttonStyle))
        {
            aiEnabled = !aiEnabled;
            nextAiDecisionAt = Time.time + 0.5f;
            Debug.Log("HQ-AI-09F17|Enabled=" + aiEnabled);
        }

        float infoX = panel.x + 10f;
        float infoY = panel.y + 33f;
        const float infoWidth = 328f;
        for (int i = 0; i < companies.Count; i++)
        {
            Regiment regiment = companies[i];
            if (regiment == null) continue;
            string role = "FRONT";
            if (missions.TryGetValue(regiment, out Mission mission))
            {
                if (mission.Reserve) role = mission.FlankReserve ? "FLANKE-RESERVE" : "RESERVE";
                else role = Label(mission.Order);
            }
            int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
            GUI.Label(new Rect(infoX, infoY, infoWidth, 15f),
                DisplayCompany(regiment) + "  " + regiment.CurrentStrength + "/" + regiment.InitialStrength +
                "  tab " + losses + " | " + role, labelStyle);
            infoY += 15f;
        }
        GUI.Label(new Rect(infoX, panel.yMax - 17f, infoWidth, 14f), "Aktuel: " + lastOrder, mutedStyle);

        float commandX = panel.x + 346f;
        float commandY = panel.y + 34f;
        float commandWidth = panel.xMax - commandX - 10f;
        const float gap = 5f;
        float buttonWidth = (commandWidth - gap * 2f) / 3f;
        const float buttonHeight = 31f;

        if (GUI.Button(new Rect(commandX, commandY, buttonWidth, buttonHeight), "ANGRIB HER", buttonStyle)) pendingOrder = BattalionOrder.AttackHere;
        if (GUI.Button(new Rect(commandX + buttonWidth + gap, commandY, buttonWidth, buttonHeight), "FORSVAR HER", buttonStyle)) pendingOrder = BattalionOrder.DefendHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, commandY, buttonWidth, buttonHeight), "TILBAGETRÆK HERTIL", buttonStyle)) pendingOrder = BattalionOrder.WithdrawHere;

        commandY += buttonHeight + gap;
        if (GUI.Button(new Rect(commandX, commandY, buttonWidth, buttonHeight), "RYK FREM HERTIL", buttonStyle)) pendingOrder = BattalionOrder.AdvanceHere;
        if (GUI.Button(new Rect(commandX + buttonWidth + gap, commandY, buttonWidth, buttonHeight), "HOLD POSITION", buttonStyle)) HoldAll();
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, commandY, buttonWidth, buttonHeight), "SAML HER", buttonStyle)) pendingOrder = BattalionOrder.AssembleHere;
    }

    private static string DisplayCompany(Regiment regiment)
    {
        if (regiment == null) return "-";
        if (regiment.RegimentName == "1. Regiment") return "1. Kompagni";
        if (regiment.RegimentName == "5. Regiment") return "2. Kompagni";
        if (regiment.RegimentName == "2. Regiment") return "3. Kompagni";
        if (regiment.RegimentName == "3. Regiment") return "4. Kompagni";
        return regiment.RegimentName;
    }

    private static string Label(BattalionOrder order)
    {
        switch (order)
        {
            case BattalionOrder.AttackHere: return "ANGRIB";
            case BattalionOrder.DefendHere: return "FORSVAR";
            case BattalionOrder.WithdrawHere: return "TILBAGETRÆK";
            case BattalionOrder.AdvanceHere: return "RYK FREM";
            case BattalionOrder.HoldPosition: return "HOLD";
            case BattalionOrder.AssembleHere: return "SAML";
            default: return "-";
        }
    }
}
