using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum MajorOrder09F18 { None, AttackHere, DefendHere, WithdrawHere, AdvanceHere, HoldPosition, AssembleHere }

// v00.00.09f18: authoritative four-company battalion command/AI layer.
[DefaultExecutionOrder(390)]
public sealed class PrototypeMajorBattalion09F18 : MonoBehaviour
{
    private sealed class Mission
    {
        public MajorOrder09F18 Order;
        public Vector3 Goal;
        public Vector3 Facing;
        public bool Reserve;
        public bool Flank;
        public bool Arrived;
        public float NextAssert;
    }

    public static PrototypeMajorBattalion09F18 Instance { get; private set; }
    public bool Installed { get; private set; }
    public bool Selected { get; private set; }
    public bool AIEnabled { get; private set; }
    public OfficerAIDoctrine Doctrine { get; private set; } = OfficerAIDoctrine.Balanced;
    public GameObject HqRoot { get; private set; }
    public IReadOnlyList<Regiment> Companies => companies;
    public string LastOrderText { get; private set; } = "Ingen bataljonsordre";
    public string LastDecisionText { get; private set; } = "Reserve: ikke vurderet";

    private readonly List<Regiment> companies = new List<Regiment>();
    private readonly Dictionary<Regiment, Mission> missions = new Dictionary<Regiment, Mission>();
    private readonly Dictionary<Regiment, Mission> lastAssignments = new Dictionary<Regiment, Mission>();
    private readonly HashSet<Regiment> manualDetached = new HashSet<Regiment>();

    private PrototypeMajorBattalion09F17 previous;
    private MethodInfo clearRoute;
    private FieldInfo playerSelected;
    private PrototypeBottomCommandBar09F2 bottomBar;
    private MajorOrder09F18 lastOrder = MajorOrder09F18.None;
    private Vector3 lastOrderPoint;
    private bool hasLastOrder;
    private Regiment reserve;
    private float nextMajorThink;
    private float nextReserveThink;
    private bool hasHqGoal;
    private Vector3 hqGoal;

    private const float Spacing = 60f;
    private const float AttackStandoff = 68f;
    private const float ReserveDepth = 78f;
    private const float FlankOffset = 112f;
    private const float KnownRange = 700f;
    private const float Arrival = 5f;
    private const float HqSpeed = 6.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorBattalion09F18>() == null)
            new GameObject("PrototypeMajorBattalion_v000009f18").AddComponent<PrototypeMajorBattalion09F18>();
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
        if (bottomBar != null)
            bottomBar.enabled = true;
    }

    private void Update()
    {
        if (!Installed)
        {
            TryInstall();
            return;
        }

        RefreshCompanies();
        SyncManualAiState();
        UpdateHqMove();
        UpdateMissions();

        if (AIEnabled && Time.time >= nextMajorThink)
        {
            nextMajorThink = Time.time + 6f;
            ThinkMajor();
        }

        if (Time.time >= nextReserveThink)
        {
            nextReserveThink = Time.time + 2.5f;
            ReviewReserve();
        }
    }

    private void TryInstall()
    {
        previous = PrototypeMajorBattalion09F17.Instance;
        if (previous == null)
            return;

        FieldInfo rootField = typeof(PrototypeMajorBattalion09F17).GetField(
            "hqRoot", BindingFlags.Instance | BindingFlags.NonPublic);
        HqRoot = rootField != null ? rootField.GetValue(previous) as GameObject : null;
        if (HqRoot == null)
            return;

        previous.enabled = false;
        clearRoute = typeof(PlayerCommander).GetMethod("ClearRoute", BindingFlags.Instance | BindingFlags.NonPublic);
        playerSelected = typeof(PlayerCommander).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic);
        bottomBar = Object.FindAnyObjectByType<PrototypeBottomCommandBar09F2>();
        RefreshCompanies();
        Installed = true;

        Debug.Log("HQ-09F18|Installed=True|UnifiedAI=True|Companies=" + companies.Count + "|MajorAI=OFF");
    }

    private void RefreshCompanies()
    {
        string[] names = { "1. Regiment", "5. Regiment", "2. Regiment", "3. Regiment" };
        List<Regiment> wanted = new List<Regiment>();
        foreach (string name in names)
        {
            Regiment r = FindOwn(name);
            if (r != null)
                wanted.Add(r);
        }

        bool same = wanted.Count == companies.Count;
        if (same)
            for (int i = 0; i < wanted.Count; i++)
                if (wanted[i] != companies[i])
                    same = false;

        if (same)
            return;

        companies.Clear();
        companies.AddRange(wanted);
    }

    private Regiment FindOwn(string name)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment r in battle.Regiments)
            if (r != null && r.RegimentName == name)
                return r;
        return null;
    }

    public void SetSelected(bool value)
    {
        Selected = value;
        if (bottomBar == null)
            bottomBar = Object.FindAnyObjectByType<PrototypeBottomCommandBar09F2>();
        if (bottomBar != null)
            bottomBar.enabled = !value;

        if (value)
            ClearCompanySelection();
    }

    private void ClearCompanySelection()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelected != null)
        {
            List<Regiment> selected = playerSelected.GetValue(commander) as List<Regiment>;
            if (selected != null)
            {
                foreach (Regiment r in selected)
                    if (r != null)
                        r.SetSelected(false);
                selected.Clear();
                return;
            }
        }

        foreach (Regiment r in companies)
            if (r != null)
                r.SetSelected(false);
    }

    public void ToggleAI()
    {
        AIEnabled = !AIEnabled;
        nextMajorThink = Time.time + 0.3f;
        Debug.Log("AI-AUTHORITY-09F18|Unit=MAJOR|AI=" + (AIEnabled ? "ON" : "OFF"));
    }

    public void SetDoctrine(OfficerAIDoctrine value)
    {
        Doctrine = value;
    }

    public void MoveMajor(Vector3 point)
    {
        AIEnabled = false;
        hqGoal = SafeHq(point);
        hasHqGoal = true;
        Debug.Log("AI-AUTHORITY-09F18|Unit=MAJOR|Authority=PLAYER|AI=OFF|MoveHQ=True");
    }

    public void ReleaseCompanyToManual(Regiment regiment)
    {
        if (regiment == null || regiment.Team != BattleTeam.Denmark)
            return;

        missions.Remove(regiment);
        manualDetached.Add(regiment);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetAIEnabled(false);

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.ClearMission(regiment);

        Debug.Log("AI-AUTHORITY-09F18|Unit=" + regiment.RegimentName + "|Authority=PLAYER|AI=OFF");
    }

    private void SyncManualAiState()
    {
        List<Regiment> detach = null;
        foreach (KeyValuePair<Regiment, Mission> pair in missions)
        {
            Regiment r = pair.Key;
            OfficerAIController controller = r != null ? r.GetComponent<OfficerAIController>() : null;
            if (controller != null && !controller.AIEnabled)
            {
                if (detach == null)
                    detach = new List<Regiment>();
                detach.Add(r);
            }
        }

        if (detach != null)
            foreach (Regiment r in detach)
                ReleaseCompanyToManual(r);

        List<Regiment> rejoin = null;
        foreach (Regiment r in manualDetached)
        {
            OfficerAIController controller = r != null ? r.GetComponent<OfficerAIController>() : null;
            if (controller != null && controller.AIEnabled)
            {
                if (rejoin == null)
                    rejoin = new List<Regiment>();
                rejoin.Add(r);
            }
        }

        if (rejoin == null)
            return;

        foreach (Regiment r in rejoin)
        {
            manualDetached.Remove(r);
            if (lastAssignments.TryGetValue(r, out Mission saved))
                Assign(r, Clone(saved), false);
        }
    }

    public void IssueOrder(MajorOrder09F18 order, Vector3 point, bool autonomous = false)
    {
        if (!Installed || companies.Count == 0)
            return;

        if (order == MajorOrder09F18.HoldPosition)
        {
            HoldAll();
            return;
        }

        List<Regiment> active = new List<Regiment>();
        foreach (Regiment r in companies)
            if (r != null && !r.IsRouted && r.CurrentStrength > 0)
                active.Add(r);
        if (active.Count == 0)
            return;

        List<Regiment> known = KnownEnemies(point);
        Regiment threat = Nearest(point, known);
        Vector3 forward = threat != null
            ? Flat(threat.transform.position - point)
            : Flat(point - HqRoot.transform.position);
        if (order == MajorOrder09F18.WithdrawHere)
            forward = threat != null ? Flat(threat.transform.position - point) : Flat(HqRoot.transform.forward);

        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = Ground(point);
        if (order == MajorOrder09F18.AttackHere && threat != null)
            center = Ground(threat.transform.position - forward * AttackStandoff);

        bool useReserve = ShouldUseReserve(order, active.Count, known.Count);
        reserve = useReserve ? BestReserve(active) : null;
        bool flank = false;
        int flankSign = 0;
        if (reserve != null)
            flank = TryFlank(center, forward, lateral, known, out flankSign);

        manualDetached.Clear();
        missions.Clear();
        lastAssignments.Clear();

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.ClearAllMissions();

        List<Regiment> front = new List<Regiment>(active);
        if (reserve != null)
            front.Remove(reserve);

        float middle = (front.Count - 1) * 0.5f;
        for (int i = 0; i < front.Count; i++)
        {
            Vector3 goal = SafeCompany(center + lateral * ((i - middle) * Spacing), forward);
            Mission mission = NewMission(order, goal, forward, false, false);
            lastAssignments[front[i]] = Clone(mission);
            Assign(front[i], mission, true);
        }

        if (reserve != null)
        {
            Vector3 reserveGoal = flank
                ? center - forward * 22f + lateral * (flankSign * FlankOffset)
                : center - forward * ReserveDepth;
            Mission mission = NewMission(order, SafeCompany(reserveGoal, forward), forward, true, flank);
            lastAssignments[reserve] = Clone(mission);
            Assign(reserve, mission, true);
        }

        lastOrder = order;
        lastOrderPoint = point;
        hasLastOrder = true;
        LastOrderText = Label(order) + " | kendte fjender: " + known.Count;
        LastDecisionText = reserve == null
            ? "Reserve: ingen - hele styrken bruges"
            : "Reserve: " + Name(reserve) + (flank ? " | flanke" : " | bag front");

        Debug.Log("HQ-ORDER-09F18|Order=" + order +
                  "|Autonomous=" + autonomous +
                  "|KnownEnemies=" + known.Count +
                  "|Front=" + front.Count +
                  "|Reserve=" + (reserve != null ? reserve.RegimentName : "NONE") +
                  "|Flank=" + flank +
                  "|CompanyAI=ON|OldManualRoutesCleared=True");
    }

    private Mission NewMission(MajorOrder09F18 order, Vector3 goal, Vector3 facing, bool reserveRole, bool flank)
    {
        return new Mission
        {
            Order = order,
            Goal = goal,
            Facing = facing,
            Reserve = reserveRole,
            Flank = flank
        };
    }

    private Mission Clone(Mission mission)
    {
        return NewMission(mission.Order, mission.Goal, mission.Facing, mission.Reserve, mission.Flank);
    }

    private void Assign(Regiment regiment, Mission mission, bool clearOldPlayerRoute)
    {
        if (clearOldPlayerRoute)
            ClearPlayerRoute(regiment);

        manualDetached.Remove(regiment);
        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null)
        {
            if (!controller.AIEnabled)
                controller.SetAIEnabled(true);

            if (mission.Order == MajorOrder09F18.AttackHere)
            {
                controller.SetDoctrine(OfficerAIDoctrine.Offensive);
                regiment.SetFirePolicy(RegimentFirePolicy.LongRange);
            }
            else if (mission.Order == MajorOrder09F18.DefendHere)
            {
                controller.SetDoctrine(OfficerAIDoctrine.Defensive);
                regiment.SetFirePolicy(RegimentFirePolicy.MediumRange);
            }
            else
            {
                controller.SetDoctrine(OfficerAIDoctrine.Balanced);
            }

            controller.SetMoveMission(mission.Goal);
        }
        else
        {
            regiment.OrderMove(mission.Goal);
        }

        missions[regiment] = mission;
        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.SetMission(regiment, mission.Goal, mission.Facing, mission.Order, mission.Reserve, mission.Flank);
    }

    private void ClearPlayerRoute(Regiment regiment)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && clearRoute != null)
            clearRoute.Invoke(commander, new object[] { regiment });
    }

    private void UpdateMissions()
    {
        foreach (KeyValuePair<Regiment, Mission> pair in new List<KeyValuePair<Regiment, Mission>>(missions))
        {
            Regiment regiment = pair.Key;
            Mission mission = pair.Value;
            if (regiment == null || regiment.IsRouted)
            {
                missions.Remove(regiment);
                continue;
            }

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
                continue;
            if (mission.Arrived)
                continue;

            Regiment enemy = Nearest(regiment.transform.position, KnownEnemies(regiment.transform.position));
            bool canStopToFight =
                (mission.Order == MajorOrder09F18.AttackHere || mission.Order == MajorOrder09F18.DefendHere) &&
                enemy != null &&
                Dist(regiment.transform.position, enemy.transform.position) <= regiment.GetFireTriggerRange() * 0.96f;

            if (Dist(regiment.transform.position, mission.Goal) <= Arrival || canStopToFight)
            {
                mission.Arrived = true;
                regiment.SetFormation(RegimentFormation.Line);
                regiment.transform.rotation = Quaternion.LookRotation(Flat(mission.Facing));
                if (controller != null)
                    controller.SetHoldMission();
                else
                    regiment.OrderHold();

                PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
                if (visuals != null)
                    visuals.MarkArrived(regiment);
                continue;
            }

            if (Time.time >= mission.NextAssert)
            {
                mission.NextAssert = Time.time + 1.25f;
                if (controller != null)
                    controller.SetMoveMission(mission.Goal);
                else
                    regiment.OrderMove(mission.Goal);
            }
        }
    }

    private bool ShouldUseReserve(MajorOrder09F18 order, int activeCount, int knownEnemyCount)
    {
        if (activeCount < 4 || knownEnemyCount <= 1)
            return false;
        if (order != MajorOrder09F18.AttackHere && order != MajorOrder09F18.DefendHere)
            return false;
        return AverageStrength() >= 0.62f && AverageCohesion() >= 50f;
    }

    private bool TryFlank(Vector3 center, Vector3 forward, Vector3 lateral, List<Regiment> known, out int sign)
    {
        sign = 0;
        if (known.Count == 0 || AverageStrength() < 0.72f || AverageCohesion() < 58f)
            return false;

        int left = 0;
        int right = 0;
        foreach (Regiment enemy in known)
        {
            float side = Vector3.Dot(enemy.transform.position - center, lateral);
            if (side < -20f) left++;
            else if (side > 20f) right++;
        }

        if (left == right)
            return false;

        sign = left < right ? -1 : 1;
        return GoalSafe(center - forward * 22f + lateral * sign * FlankOffset, forward);
    }

    private Regiment BestReserve(List<Regiment> list)
    {
        Regiment best = null;
        float bestScore = -1f;
        foreach (Regiment r in list)
        {
            float score = (r.CurrentStrength / (float)Mathf.Max(1, r.InitialStrength)) * 70f + r.Cohesion * 0.3f;
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }
        return best;
    }

    private void ReviewReserve()
    {
        if (reserve == null || !missions.TryGetValue(reserve, out Mission reserveMission))
            return;
        if (!reserveMission.Reserve || !reserveMission.Arrived)
            return;

        Regiment weakest = null;
        float weakestRatio = 1f;
        foreach (Regiment r in companies)
        {
            if (r == null || r == reserve || r.InitialStrength <= 0)
                continue;
            float ratio = r.CurrentStrength / (float)r.InitialStrength;
            if (ratio < weakestRatio)
            {
                weakestRatio = ratio;
                weakest = r;
            }
        }

        if (weakest == null)
            return;

        Regiment enemy = Nearest(weakest.transform.position, KnownEnemies(weakest.transform.position));
        if (enemy == null)
            return;
        if (weakestRatio > 0.66f && Dist(weakest.transform.position, enemy.transform.position) > 95f)
            return;

        Vector3 forward = Flat(enemy.transform.position - weakest.transform.position);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        reserveMission.Goal = SafeCompany(weakest.transform.position - forward * 18f + lateral * 54f, forward);
        reserveMission.Facing = forward;
        reserveMission.Reserve = false;
        reserveMission.Flank = true;
        reserveMission.Arrived = false;
        reserveMission.NextAssert = 0f;

        OfficerAIController controller = reserve.GetComponent<OfficerAIController>();
        if (controller != null)
            controller.SetMoveMission(reserveMission.Goal);

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.SetMission(reserve, reserveMission.Goal, reserveMission.Facing, reserveMission.Order, false, true);

        LastDecisionText = "Reserve indsat mod truet fløj: " + Name(reserve);
    }

    private void HoldAll()
    {
        manualDetached.Clear();
        missions.Clear();
        reserve = null;

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.ClearAllMissions();

        foreach (Regiment r in companies)
        {
            if (r == null || r.IsRouted)
                continue;
            ClearPlayerRoute(r);
            OfficerAIController controller = r.GetComponent<OfficerAIController>();
            if (controller != null)
            {
                if (!controller.AIEnabled)
                    controller.SetAIEnabled(true);
                controller.SetHoldMission();
            }
            else
            {
                r.OrderHold();
            }
        }

        lastOrder = MajorOrder09F18.HoldPosition;
        hasLastOrder = true;
        lastOrderPoint = Center();
        LastOrderText = "HOLD POSITION";
        LastDecisionText = "Reserve: ingen";
    }

    private void ThinkMajor()
    {
        List<Regiment> known = KnownEnemies(Center());
        Regiment enemy = Nearest(Center(), known);
        if (enemy == null)
            return;

        float distance = Dist(Center(), enemy.transform.position);
        MajorOrder09F18 order;
        Vector3 goal;
        Vector3 forward = Flat(enemy.transform.position - Center());

        if (AverageStrength() < 0.52f || AverageCohesion() < 42f)
        {
            order = MajorOrder09F18.DefendHere;
            goal = Center() - forward * 55f;
        }
        else if (distance <= 170f || Doctrine == OfficerAIDoctrine.Offensive)
        {
            order = MajorOrder09F18.AttackHere;
            goal = enemy.transform.position;
        }
        else if (distance <= 300f)
        {
            order = MajorOrder09F18.DefendHere;
            goal = Center() + forward * 30f;
        }
        else
        {
            order = MajorOrder09F18.AdvanceHere;
            goal = Center() + forward * Mathf.Min(95f, distance - 180f);
        }

        if (hasLastOrder && lastOrder == order && Dist(lastOrderPoint, goal) < 32f)
            return;

        IssueOrder(order, goal, true);
    }

    private List<Regiment> KnownEnemies(Vector3 reference)
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment enemy in battle.Regiments)
        {
            if (enemy == null || enemy.Team != BattleTeam.Prussia || enemy.IsRouted || enemy.CurrentStrength <= 0)
                continue;

            bool known = HqRoot != null && Dist(HqRoot.transform.position, enemy.transform.position) <= KnownRange;
            if (!known)
            {
                foreach (Regiment own in companies)
                {
                    if (own != null && Dist(own.transform.position, enemy.transform.position) <= KnownRange)
                    {
                        known = true;
                        break;
                    }
                }
            }

            if (!known && Dist(reference, enemy.transform.position) <= 360f)
                known = true;
            if (known)
                result.Add(enemy);
        }
        return result;
    }

    private Regiment Nearest(Vector3 point, List<Regiment> list)
    {
        Regiment best = null;
        float bestDistance = float.MaxValue;
        foreach (Regiment r in list)
        {
            float distance = Dist(point, r.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = r;
            }
        }
        return best;
    }

    private Vector3 Center()
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment r in companies)
        {
            if (r == null || r.IsRouted)
                continue;
            total += r.transform.position;
            count++;
        }
        return count > 0 ? total / count : HqRoot.transform.position;
    }

    private float AverageStrength()
    {
        float total = 0f;
        int count = 0;
        foreach (Regiment r in companies)
        {
            if (r == null || r.InitialStrength <= 0)
                continue;
            total += r.CurrentStrength / (float)r.InitialStrength;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AverageCohesion()
    {
        float total = 0f;
        int count = 0;
        foreach (Regiment r in companies)
        {
            if (r == null)
                continue;
            total += r.Cohesion;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private Vector3 SafeCompany(Vector3 point, Vector3 facing)
    {
        Vector3 forward = Flat(facing);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 back = -forward;
        Vector3[] candidates =
        {
            point,
            point + lateral * 30f,
            point - lateral * 30f,
            point + lateral * 60f,
            point - lateral * 60f,
            point + back * 30f,
            point + back * 55f,
            point + back * 30f + lateral * 30f,
            point + back * 30f - lateral * 30f
        };

        foreach (Vector3 candidate in candidates)
            if (GoalSafe(candidate, forward))
                return Ground(candidate);
        return Ground(point);
    }

    private bool GoalSafe(Vector3 point, Vector3 facing)
    {
        if (Mathf.Abs(point.x) > PrototypeBootstrap.BattlefieldHalfWidth - 30f ||
            Mathf.Abs(point.z) > PrototypeBootstrap.BattlefieldHalfDepth - 30f)
            return false;

        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        if (Mathf.Abs(point.x - riverX) <= 5f && Mathf.Abs(point.z - 22f) > 7f)
            return false;

        Collider[] hits = Physics.OverlapBox(
            new Vector3(point.x, PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 1f, point.z),
            new Vector3(25f, 2.5f, 5f),
            Quaternion.LookRotation(Flat(facing)));

        foreach (Collider hit in hits)
        {
            string name = hit != null ? hit.gameObject.name : string.Empty;
            string rootName = hit != null && hit.transform.root != null ? hit.transform.root.name : string.Empty;
            if (name.Contains("Farmhouse") || name.Contains("Barn") ||
                rootName.Contains("Farmhouse") || rootName.Contains("Barn"))
                return false;
        }
        return true;
    }

    private Vector3 SafeHq(Vector3 point)
    {
        point = Ground(point);
        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        if (Mathf.Abs(point.x - riverX) <= 4f && Mathf.Abs(point.z - 22f) > 7f)
            point.x += point.x >= riverX ? 10f : -10f;
        return Ground(point);
    }

    private void UpdateHqMove()
    {
        if (HqRoot == null || !hasHqGoal)
            return;

        Vector3 delta = hqGoal - HqRoot.transform.position;
        delta.y = 0f;
        if (delta.magnitude <= 1.4f)
        {
            hasHqGoal = false;
            return;
        }

        Vector3 step = delta.normalized * HqSpeed * Time.deltaTime;
        if (step.magnitude > delta.magnitude)
            step = delta;

        Vector3 next = HqRoot.transform.position + step;
        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.1f;
        HqRoot.transform.position = next;
        HqRoot.transform.rotation = Quaternion.Slerp(
            HqRoot.transform.rotation,
            Quaternion.LookRotation(delta.normalized),
            4f * Time.deltaTime);
    }

    public string Role(Regiment regiment)
    {
        if (regiment == null)
            return "-";
        if (manualDetached.Contains(regiment))
            return "MANUEL / AI OFF";
        if (missions.TryGetValue(regiment, out Mission mission))
            return mission.Reserve ? (mission.Flank ? "FLANKE" : "RESERVE") : Label(mission.Order);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        return controller != null && controller.AIEnabled ? "AI ON" : "AI OFF";
    }

    public static string Name(Regiment regiment)
    {
        if (regiment == null) return "-";
        if (regiment.RegimentName == "1. Regiment") return "1. Kmp";
        if (regiment.RegimentName == "5. Regiment") return "2. Kmp";
        if (regiment.RegimentName == "2. Regiment") return "3. Kmp";
        if (regiment.RegimentName == "3. Regiment") return "4. Kmp";
        return regiment.RegimentName;
    }

    public static string Label(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB";
            case MajorOrder09F18.DefendHere: return "FORSVAR";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            case MajorOrder09F18.AssembleHere: return "SAML";
            default: return "-";
        }
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.1f;
        return point;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude < 0.01f ? Vector3.forward : value.normalized;
    }

    private static float Dist(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
