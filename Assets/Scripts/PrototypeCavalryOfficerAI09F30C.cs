using System.Collections.Generic;
using UnityEngine;

// v00.00.09f30c
// First autonomous cavalry-officer AI. The cavalry officer seeks a flank/rear approach
// before committing a mounted charge. Direct player battlefield/HUD orders always win.
[DefaultExecutionOrder(41800)]
public sealed class PrototypeCavalryOfficerAI09F30C : MonoBehaviour
{
    private sealed class State
    {
        public PrototypeCavalryUnit09F30 Unit;
        public bool Enabled = false;
        public Regiment Target;
        public Vector3 ManeuverPoint;
        public Vector3 TargetAnchor;
        public string Phase = "SEEK";
        public float NextThink;
        public float ManeuverStarted;
        public int PreferredSide = 1;
        public bool HasHigherMission;
        public MajorOrder09F18 HigherMissionOrder = MajorOrder09F18.None;
        public Vector3 HigherMissionGoal;
        public Vector3 HigherMissionFacing;
    }

    public static PrototypeCavalryOfficerAI09F30C Instance { get; private set; }

    private const float ThinkInterval = 1.15f;
    private const float SearchRange = 1200f;
    private const float RearDepth = 145f;
    private const float RearLateral = 70f;
    private const float FlankOffset = 145f;
    private const float FlankRearBias = 50f;
    private const float ChargeCommitRange = 155f;
    private const float ArriveTolerance = 24f;
    private const float ReplanTargetMove = 42f;
    private const float MaxManeuverSeconds = 26f;

    // F30N: cavalry screens outside the infantry body until a real tactical
    // opportunity exists. Movement paths route around hostile infantry bubbles.
    private const float EnemyAvoidRadius = 105f;
    private const float EnemyDetourRadius = 138f;
    private const float StandOffMinDistance = 115f;
    private const float StandOffDistance = 145f;
    private const float OpportunityCohesionThreshold = 62f;
    private const float OpportunityMoraleThreshold = 58f;
    private const float BottomHudHeight = 90f;

    private readonly Dictionary<PrototypeCavalryUnit09F30, State> states =
        new Dictionary<PrototypeCavalryUnit09F30, State>();

    private PrototypeCavalryManager09F30 cavalry;
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryOfficerAI09F30C>() == null)
            new GameObject("PrototypeCavalryOfficerAI_v000009f30c")
                .AddComponent<PrototypeCavalryOfficerAI09F30C>();
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
        if (!installed)
        {
            TryInstall();
            return;
        }

        DetectDirectPlayerOverride();

        foreach (KeyValuePair<PrototypeCavalryUnit09F30, State> pair in states)
        {
            State state = pair.Value;
            if (state == null || state.Unit == null)
                continue;

            if (state.Unit.CurrentStrength <= 0)
            {
                state.Phase = "KAMPUDE";
                continue;
            }

            if (state.HasHigherMission)
            {
                ExecuteHigherMission(state);
                continue;
            }

            if (!state.Enabled)
                continue;
            if (!ParentAllowsDelegatedAI(state.Unit))
            {
                state.Phase = "WAIT PARENT AI";
                if (state.Unit.Action == PrototypeCavalryAction09F30.Move ||
                    state.Unit.Action == PrototypeCavalryAction09F30.Charge)
                    state.Unit.OrderHold();
                continue;
            }
            if (Time.time < state.NextThink)
                continue;
            state.NextThink = Time.time + ThinkInterval;
            Think(state);
        }
    }

    private void TryInstall()
    {
        cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed || cavalry.Gardehusar == null || cavalry.Dragon == null)
            return;

        Register(cavalry.Gardehusar, 1);
        Register(cavalry.Dragon, -1);
        installed = true;
        Debug.Log("CAV-AI-09F30I|Installed=True|DefaultAI=OFF|Plan=SEEK_FLANK_OR_REAR_THEN_CHARGE|ManualOverride=True");
    }

    private void Register(PrototypeCavalryUnit09F30 unit, int preferredSide)
    {
        if (unit == null || states.ContainsKey(unit))
            return;
        states[unit] = new State
        {
            Unit = unit,
            Enabled = false,
            Phase = "MANUEL",
            PreferredSide = preferredSide,
            NextThink = Time.time + Random.Range(0.4f, 1.2f)
        };
    }

    public bool IsAIEnabled(PrototypeCavalryUnit09F30 unit)
    {
        State state;
        return unit != null && states.TryGetValue(unit, out state) && state.Enabled;
    }

    public void SetAIEnabled(PrototypeCavalryUnit09F30 unit, bool enabled, string reason = "PLAYER")
    {
        if (unit == null)
            return;
        State state;
        if (!states.TryGetValue(unit, out state))
        {
            Register(unit, unit.Kind == PrototypeCavalryKind09F30.Gardehusar ? 1 : -1);
            state = states[unit];
        }

        state.Enabled = enabled;
        if (enabled)
            unit.ClearManualFormationOverride();
        state.Target = null;
        state.Phase = enabled ? "SEEK" : "MANUEL";
        state.NextThink = Time.time + 0.25f;
        state.ManeuverStarted = 0f;

        Debug.Log("CAV-AI-09F30C|Unit=" + unit.UnitName + "|AI=" + (enabled ? "ON" : "OFF") + "|Reason=" + reason);
    }

    public void ToggleAI(PrototypeCavalryUnit09F30 unit)
    {
        SetAIEnabled(unit, !IsAIEnabled(unit), "HUD_TOGGLE");
    }

    public void SetHigherMission(
        PrototypeCavalryUnit09F30 unit,
        MajorOrder09F18 order,
        Vector3 goal,
        Vector3 facing,
        bool autonomous)
    {
        if (unit == null)
            return;

        State state;
        if (!states.TryGetValue(unit, out state))
        {
            Register(unit, unit.Kind == PrototypeCavalryKind09F30.Gardehusar ? 1 : -1);
            state = states[unit];
        }

        state.HasHigherMission = true;
        state.HigherMissionOrder = order;
        state.HigherMissionGoal = goal;
        state.HigherMissionFacing = facing.sqrMagnitude > 0.01f ? facing.normalized : unit.transform.forward;
        state.Target = null;
        state.ManeuverStarted = 0f;
        state.Enabled = autonomous || state.Enabled;
        state.Phase = "HQ " + HigherMissionLabel(order);
        state.NextThink = Time.time + 0.15f;

        if (order == MajorOrder09F18.HoldPosition)
        {
            unit.OrderHold();
        }
        else
        {
            Vector3 initialGoal = order == MajorOrder09F18.AttackHere
                ? AvoidEnemyBubbleOnRoute(unit.transform.position, goal, state.PreferredSide)
                : goal;
            unit.OrderMove(initialGoal, state.HigherMissionFacing, true);
        }

        Debug.Log("CAV-HQ-09F30M|Unit=" + unit.UnitName +
                  "|Order=" + order +
                  "|Autonomous=" + autonomous +
                  "|Goal=" + goal.x.ToString("0") + "," + goal.z.ToString("0"));
    }

    public void ClearHigherMission(PrototypeCavalryUnit09F30 unit, string reason = "CLEAR")
    {
        State state;
        if (unit == null || !states.TryGetValue(unit, out state))
            return;

        state.HasHigherMission = false;
        state.HigherMissionOrder = MajorOrder09F18.None;
        state.Phase = state.Enabled ? "SEEK" : "MANUEL";
        Debug.Log("CAV-HQ-09F30M|Unit=" + unit.UnitName + "|HigherMission=False|Reason=" + reason);
    }

    public bool HasHigherMission(PrototypeCavalryUnit09F30 unit, MajorOrder09F18 order)
    {
        State state;
        return unit != null && states.TryGetValue(unit, out state) &&
               state.HasHigherMission && state.HigherMissionOrder == order;
    }

    public string GetPhase(PrototypeCavalryUnit09F30 unit)
    {
        State state;
        if (unit == null || !states.TryGetValue(unit, out state))
            return "—";
        return state.Enabled ? state.Phase : "MANUEL";
    }

    public string GetTargetName(PrototypeCavalryUnit09F30 unit)
    {
        State state;
        if (unit == null || !states.TryGetValue(unit, out state) || state.Target == null)
            return "—";
        return state.Target.RegimentName;
    }

    private void DetectDirectPlayerOverride()
    {
        if (cavalry == null || cavalry.SelectedUnit == null)
            return;

        PrototypeCavalryUnit09F30 selected = cavalry.SelectedUnit;

        // Direct player world order always breaks an inherited higher mission.
        // This must also work when cavalry Officer AI is already OFF, because
        // higher-HQ missions execute independently of the autonomous AI toggle.
        if (Input.GetMouseButtonDown(1))
        {
            ClearHigherMission(selected, "PLAYER_RIGHT_CLICK");
            if (IsAIEnabled(selected))
                SetAIEnabled(selected, false, "PLAYER_RIGHT_CLICK");
        }
    }

    private bool ParentAllowsDelegatedAI(PrototypeCavalryUnit09F30 unit)
    {
        PrototypeCommandAttachment09F30B attachment = unit != null
            ? unit.GetComponent<PrototypeCommandAttachment09F30B>()
            : null;
        if (attachment == null)
            return true;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null)
            return true;

        if (attachment.CurrentCommandParent == PrototypeCavalryCommandControl09F30C.MajorAId)
            return hierarchy.GetBattalionAIEnabled(0);
        if (attachment.CurrentCommandParent == PrototypeCavalryCommandControl09F30C.MajorBId)
            return hierarchy.GetBattalionAIEnabled(1);

        if (attachment.CurrentCommandParent == PrototypeHigherCommandHQ09F30B.RegimentId)
        {
            PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
            return regiment == null || regiment.AIEnabled;
        }

        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (higher != null)
        {
            if (attachment.CurrentCommandParent == PrototypeHigherCommandHQ09F30B.BrigadeId)
                return higher.BrigadeAIEnabled;
            if (attachment.CurrentCommandParent == PrototypeHigherCommandHQ09F30B.DivisionId)
                return higher.DivisionAIEnabled;
        }

        return true;
    }

    private void ExecuteHigherMission(State state)
    {
        PrototypeCavalryUnit09F30 unit = state.Unit;
        if (unit == null)
            return;

        if (state.HigherMissionOrder == MajorOrder09F18.HoldPosition)
        {
            if (unit.Action != PrototypeCavalryAction09F30.Hold)
                unit.OrderHold();
            state.Phase = "HQ HOLD";
            return;
        }

        Vector3 plannedGoal = state.HigherMissionOrder == MajorOrder09F18.AttackHere
            ? AvoidEnemyBubbleOnRoute(
                unit.transform.position,
                state.HigherMissionGoal,
                state.PreferredSide)
            : state.HigherMissionGoal;

        float distance = PlanarDistance(unit.transform.position, plannedGoal);
        if (distance > 14f)
        {
            if (!unit.HasDestination ||
                PlanarDistance(unit.FinalDestination, plannedGoal) > 5f)
                unit.OrderMove(plannedGoal, state.HigherMissionFacing, true);

            bool detouring = PlanarDistance(plannedGoal, state.HigherMissionGoal) > 8f;
            state.Phase = detouring
                ? "HQ ANGRIB / OMGÅR FJENDE"
                : "HQ " + HigherMissionLabel(state.HigherMissionOrder) +
                  " " + PlanarDistance(unit.transform.position, state.HigherMissionGoal).ToString("0") + "m";
            return;
        }

        // A detour waypoint is only an intermediate safety point. Re-evaluate the
        // route on the next frame instead of treating it as mission arrival.
        if (PlanarDistance(plannedGoal, state.HigherMissionGoal) > 8f)
        {
            state.Phase = "HQ ANGRIB / DETOUR KLAR";
            return;
        }

        unit.SetFormation(PrototypeCavalryFormation09F30.Line);
        unit.OrderHold();

        if (state.HigherMissionOrder == MajorOrder09F18.AttackHere && state.Enabled)
        {
            state.HasHigherMission = false;
            state.HigherMissionOrder = MajorOrder09F18.None;
            state.Phase = "SCREEN / SØGER MULIGHED";
            state.NextThink = Time.time + 0.2f;
            return;
        }

        state.Phase = "HQ " + HigherMissionLabel(state.HigherMissionOrder) + " / HOLD";
    }

    private static string HigherMissionLabel(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB";
            case MajorOrder09F18.DefendHere: return "FORSVAR";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AssembleHere: return "SAML";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            default: return order.ToString().ToUpperInvariant();
        }
    }

    private void Think(State state)
    {
        PrototypeCavalryUnit09F30 unit = state.Unit;
        if (unit == null)
            return;

        if (unit.Mode != PrototypeCavalryMode09F30.Mounted)
        {
            state.Phase = "AFSIDDET / HOLD";
            return;
        }

        if (unit.Action == PrototypeCavalryAction09F30.Falter)
        {
            state.Phase = "FALTER / RECOVER";
            return;
        }

        if (unit.Action == PrototypeCavalryAction09F30.Charge)
        {
            state.Phase = "CHARGE";
            return;
        }

        if (!ValidTarget(state.Target))
        {
            state.Target = AcquireTarget(unit, state.PreferredSide);
            if (state.Target == null)
            {
                state.Phase = "HOLD / INTET MÅL";
                return;
            }
            PlanManeuver(state, true);
            return;
        }

        Regiment target = state.Target;
        float targetMoved = PlanarDistance(target.transform.position, state.TargetAnchor);
        float distanceToPoint = PlanarDistance(unit.transform.position, state.ManeuverPoint);
        float distanceToTarget = PlanarDistance(unit.transform.position, target.transform.position);
        string aspect = GetAspect(unit.transform.position, target);

        if (PrototypeInfantrySquare09F29.IsSquareReady(target))
        {
            state.Phase = "PRESSER SQUARE / HOLD";
            if (unit.Action != PrototypeCavalryAction09F30.Hold)
                unit.OrderHold();
            return;
        }

        bool infantryEngaged = IsTargetEngagedByFriendlyInfantry(target);
        bool targetWeakened =
            target.Cohesion <= OpportunityCohesionThreshold ||
            target.Morale <= OpportunityMoraleThreshold;
        bool chargeOpportunity = infantryEngaged || targetWeakened;

        // Do not loiter inside the hostile infantry body while waiting for an opening.
        // Pull back to a screen distance and continue observing.
        if (!chargeOpportunity && distanceToTarget < StandOffMinDistance)
        {
            PlanStandOffEvasion(state);
            return;
        }

        bool reachedManeuverPoint =
            distanceToPoint <= ArriveTolerance ||
            unit.Action == PrototypeCavalryAction09F30.Hold;
        bool goodAspect = aspect == "FLANK" || aspect == "REAR";
        bool timedOut =
            state.ManeuverStarted > 0f &&
            Time.time - state.ManeuverStarted >= MaxManeuverSeconds;

        if (targetMoved > ReplanTargetMove)
        {
            PlanManeuver(state, false);
            return;
        }

        // F30N: geometry alone is no longer enough to trigger a charge. Cavalry
        // waits until friendly infantry has fixed the target in local fire contact,
        // or the target has become materially disorganised.
        if (chargeOpportunity &&
            ((reachedManeuverPoint && goodAspect && distanceToTarget <= ChargeCommitRange) ||
             (timedOut && goodAspect && distanceToTarget <= ChargeCommitRange)))
        {
            unit.SetFormation(PrototypeCavalryFormation09F30.Line);
            unit.OrderCharge(target);
            state.Phase = "CHARGE " + aspect +
                          (infantryEngaged ? " / INF KONTAKT" : " / SVÆKKET MÅL");
            Debug.Log("CAV-AI-09F30N|Unit=" + unit.UnitName +
                      "|Decision=CHARGE|Aspect=" + aspect +
                      "|Target=" + target.RegimentName +
                      "|Distance=" + distanceToTarget.ToString("0") +
                      "|InfantryEngaged=" + infantryEngaged +
                      "|TargetMorale=" + target.Morale.ToString("0") +
                      "|TargetCohesion=" + target.Cohesion.ToString("0"));
            return;
        }

        if (reachedManeuverPoint && !chargeOpportunity)
        {
            unit.SetFormation(PrototypeCavalryFormation09F30.Line);
            if (unit.Action != PrototypeCavalryAction09F30.Hold)
                unit.OrderHold();

            state.Phase = "SCREEN / VENTER PÅ INF-KONTAKT";
            return;
        }

        if (reachedManeuverPoint || timedOut)
        {
            PlanManeuver(state, false);
            return;
        }

        state.Phase = chargeOpportunity
            ? "MANØVRER / MULIGHED ÅBEN"
            : "SCREEN / " + PlannedAspectLabel(state.ManeuverPoint, target);
    }

    private void PlanManeuver(State state, bool newTarget)
    {
        Regiment target = state.Target;
        PrototypeCavalryUnit09F30 unit = state.Unit;
        if (!ValidTarget(target) || unit == null)
            return;

        Vector3 forward = Flat(target.transform.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.right;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float sideDot = Vector3.Dot(
            Flat(unit.transform.position - target.transform.position),
            right);
        int naturalSide = Mathf.Abs(sideDot) > 0.05f
            ? (sideDot >= 0f ? 1 : -1)
            : state.PreferredSide;

        Vector3 rear =
            target.transform.position -
            forward * RearDepth +
            right * (naturalSide * RearLateral);
        Vector3 flank =
            target.transform.position +
            right * (naturalSide * FlankOffset) -
            forward * FlankRearBias;

        rear = SafeMountedPoint(rear, target.transform.position);
        flank = SafeMountedPoint(flank, target.transform.position);

        float rearCost = PlanarDistance(unit.transform.position, rear) * 0.88f;
        float flankCost = PlanarDistance(unit.transform.position, flank);
        Vector3 desired = rearCost <= flankCost * 1.18f ? rear : flank;

        Vector3 routed = AvoidEnemyBubbleOnRoute(
            unit.transform.position,
            desired,
            naturalSide);

        state.ManeuverPoint = routed;
        state.TargetAnchor = target.transform.position;
        state.PreferredSide = naturalSide;
        state.ManeuverStarted =
            newTarget || state.ManeuverStarted <= 0f
                ? Time.time
                : state.ManeuverStarted;

        bool detour = PlanarDistance(routed, desired) > 8f;
        state.Phase = detour
            ? "MANØVRER / OMGÅR FJENDE"
            : "SCREEN / " + PlannedAspectLabel(desired, target);

        unit.SetFormation(PrototypeCavalryFormation09F30.Column);
        unit.OrderMove(routed);

        Debug.Log("CAV-AI-09F30N|Unit=" + unit.UnitName +
                  "|Decision=" + (detour ? "DETOUR" : "SCREEN") +
                  "|Goal=" + routed.x.ToString("0") + "," + routed.z.ToString("0") +
                  "|Desired=" + desired.x.ToString("0") + "," + desired.z.ToString("0") +
                  "|Aim=" + PlannedAspectLabel(desired, target) +
                  "|Target=" + target.RegimentName);
    }

    private void PlanStandOffEvasion(State state)
    {
        if (state == null || state.Unit == null || !ValidTarget(state.Target))
            return;

        PrototypeCavalryUnit09F30 unit = state.Unit;
        Regiment target = state.Target;

        Vector3 away = Flat(unit.transform.position - target.transform.position);
        if (away.sqrMagnitude < 0.01f)
            away = -Flat(target.transform.forward);
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.left;
        away.Normalize();

        Vector3 tangent = Vector3.Cross(Vector3.up, away).normalized * state.PreferredSide;
        Vector3 desired =
            target.transform.position +
            away * StandOffDistance +
            tangent * 28f;
        desired = SafeMountedPoint(desired, target.transform.position);
        desired = AvoidEnemyBubbleOnRoute(
            unit.transform.position,
            desired,
            state.PreferredSide);

        state.ManeuverPoint = desired;
        state.TargetAnchor = target.transform.position;
        state.ManeuverStarted = Time.time;
        state.Phase = "SCREEN / BRYDER AFSTAND";

        unit.SetFormation(PrototypeCavalryFormation09F30.Column);
        unit.OrderMove(desired);

        Debug.Log("CAV-AI-09F30N|Unit=" + unit.UnitName +
                  "|Decision=STAND_OFF|Target=" + target.RegimentName +
                  "|Distance=" +
                  PlanarDistance(unit.transform.position, target.transform.position).ToString("0") +
                  "|Goal=" + desired.x.ToString("0") + "," + desired.z.ToString("0"));
    }

    private static bool IsTargetEngagedByFriendlyInfantry(Regiment target)
    {
        if (!ValidTarget(target) || BattleManager.Instance == null ||
            BattleManager.Instance.Regiments == null)
            return false;

        foreach (Regiment friendly in BattleManager.Instance.Regiments)
        {
            if (friendly == null ||
                friendly.Team != BattleTeam.Denmark ||
                friendly.IsRouted ||
                friendly.CurrentStrength <= 0)
                continue;

            if (PrototypeAttackContact09F29G.GetLocalContactTarget(friendly) == target)
                return true;
        }

        return false;
    }

    private static Vector3 AvoidEnemyBubbleOnRoute(
        Vector3 start,
        Vector3 desired,
        int preferredSide)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return desired;

        Regiment obstacle = null;
        float firstT = 2f;

        foreach (Regiment enemy in battle.Regiments)
        {
            if (!ValidTarget(enemy))
                continue;

            float t;
            float clearance = DistancePointToSegmentXZ(
                enemy.transform.position,
                start,
                desired,
                out t);

            if (t <= 0.06f || t >= 0.94f || clearance >= EnemyAvoidRadius)
                continue;

            if (t < firstT)
            {
                firstT = t;
                obstacle = enemy;
            }
        }

        if (obstacle == null)
            return desired;

        Vector3 travel = Flat(desired - start);
        if (travel.sqrMagnitude < 0.01f)
            return desired;
        travel.Normalize();

        Vector3 perpendicular = Vector3.Cross(Vector3.up, travel).normalized;
        Vector3 candidateA = SafeMountedPoint(
            obstacle.transform.position +
            perpendicular * EnemyDetourRadius -
            travel * 18f,
            obstacle.transform.position);
        Vector3 candidateB = SafeMountedPoint(
            obstacle.transform.position -
            perpendicular * EnemyDetourRadius -
            travel * 18f,
            obstacle.transform.position);

        float costA = DetourCost(start, candidateA, desired);
        float costB = DetourCost(start, candidateB, desired);

        if (Mathf.Abs(costA - costB) < 12f)
            return preferredSide >= 0 ? candidateA : candidateB;

        return costA <= costB ? candidateA : candidateB;
    }

    private static float DetourCost(
        Vector3 start,
        Vector3 candidate,
        Vector3 desired)
    {
        float cost =
            PlanarDistance(start, candidate) +
            PlanarDistance(candidate, desired);

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return cost;

        foreach (Regiment enemy in battle.Regiments)
        {
            if (!ValidTarget(enemy))
                continue;

            float d = PlanarDistance(candidate, enemy.transform.position);
            if (d < EnemyAvoidRadius)
                cost += (EnemyAvoidRadius - d) * 20f + 500f;
        }

        return cost;
    }

    private static float DistancePointToSegmentXZ(
        Vector3 point,
        Vector3 a,
        Vector3 b,
        out float t)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 p0 = new Vector2(a.x, a.z);
        Vector2 p1 = new Vector2(b.x, b.z);
        Vector2 segment = p1 - p0;

        float lengthSq = segment.sqrMagnitude;
        if (lengthSq <= 0.0001f)
        {
            t = 0f;
            return Vector2.Distance(p, p0);
        }

        t = Mathf.Clamp01(Vector2.Dot(p - p0, segment) / lengthSq);
        Vector2 closest = p0 + segment * t;
        return Vector2.Distance(p, closest);
    }

    private static Regiment AcquireTarget(PrototypeCavalryUnit09F30 unit, int preferredSide)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || unit == null)
            return null;

        Regiment best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < battle.Regiments.Count; i++)
        {
            Regiment enemy = battle.Regiments[i];
            if (!ValidTarget(enemy))
                continue;

            float d = PlanarDistance(unit.transform.position, enemy.transform.position);
            if (d > SearchRange)
                continue;

            float score = d;
            if (PrototypeInfantrySquare09F29.IsSquareReady(enemy))
                score += 500f;
            if (PrototypeAttackContact09F29G.IsLocalContact(enemy))
                score -= 90f;

            Vector3 right = Vector3.Cross(Vector3.up, Flat(enemy.transform.forward)).normalized;
            if (right.sqrMagnitude > 0.01f)
            {
                float side = Vector3.Dot(Flat(unit.transform.position - enemy.transform.position), right);
                if ((side >= 0f ? 1 : -1) == preferredSide)
                    score -= 20f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }

    private static bool ValidTarget(Regiment target)
    {
        return target != null && target.Team == BattleTeam.Prussia && !target.IsRouted && target.CurrentStrength > 0;
    }

    private static string GetAspect(Vector3 cavalryPosition, Regiment target)
    {
        Vector3 fromTarget = Flat(cavalryPosition - target.transform.position);
        Vector3 facing = Flat(target.transform.forward);
        if (fromTarget.sqrMagnitude < 0.01f || facing.sqrMagnitude < 0.01f)
            return "FRONT";
        float dot = Vector3.Dot(facing.normalized, fromTarget.normalized);
        if (dot <= -0.55f) return "REAR";
        if (dot >= 0.55f) return "FRONT";
        return "FLANK";
    }

    private static string PlannedAspectLabel(Vector3 point, Regiment target)
    {
        return GetAspect(point, target);
    }

    private static Vector3 SafeMountedPoint(Vector3 point, Vector3 targetPoint)
    {
        float xLimit = Mathf.Max(40f, PrototypeBootstrap.BattlefieldHalfWidth - 30f);
        float zLimit = Mathf.Max(40f, PrototypeBootstrap.BattlefieldHalfDepth - 30f);
        point.x = Mathf.Clamp(point.x, -xLimit, xLimit);
        point.z = Mathf.Clamp(point.z, -zLimit, zLimit);

        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        float delta = point.x - riverX;
        if (Mathf.Abs(delta) < 11f)
        {
            float targetDelta = targetPoint.x - PrototypeBootstrap.StreamCenterX(targetPoint.z);
            float sign = Mathf.Abs(targetDelta) > 1f ? Mathf.Sign(targetDelta) : (delta >= 0f ? 1f : -1f);
            point.x = riverX + sign * 14f;
        }

        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
