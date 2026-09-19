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
    }

    public static PrototypeCavalryOfficerAI09F30C Instance { get; private set; }

    private const float ThinkInterval = 1.15f;
    private const float SearchRange = 1200f;
    private const float RearDepth = 115f;
    private const float RearLateral = 42f;
    private const float FlankOffset = 105f;
    private const float FlankRearBias = 38f;
    private const float ChargeCommitRange = 250f;
    private const float ArriveTolerance = 24f;
    private const float ReplanTargetMove = 42f;
    private const float MaxManeuverSeconds = 26f;
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
            if (state == null || state.Unit == null || !state.Enabled)
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
        if (!IsAIEnabled(selected))
            return;

        // F30I: only direct world orders auto-take manual authority here.
        // HUD buttons explicitly decide whether their command disables AI.
        if (Input.GetMouseButtonDown(1))
            SetAIEnabled(selected, false, "PLAYER_RIGHT_CLICK");
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
        return true;
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

        bool reachedManeuverPoint = distanceToPoint <= ArriveTolerance || unit.Action == PrototypeCavalryAction09F30.Hold;
        bool goodAspect = aspect == "FLANK" || aspect == "REAR";
        bool timedOut = state.ManeuverStarted > 0f && Time.time - state.ManeuverStarted >= MaxManeuverSeconds;

        if ((reachedManeuverPoint && goodAspect && distanceToTarget <= ChargeCommitRange) ||
            (timedOut && goodAspect && distanceToTarget <= ChargeCommitRange * 1.2f))
        {
            unit.SetFormation(PrototypeCavalryFormation09F30.Line);
            unit.OrderCharge(target);
            state.Phase = "CHARGE " + aspect;
            Debug.Log("CAV-AI-09F30C|Unit=" + unit.UnitName + "|Decision=CHARGE|Aspect=" + aspect +
                      "|Target=" + target.RegimentName + "|Distance=" + distanceToTarget.ToString("0"));
            return;
        }

        if (targetMoved > ReplanTargetMove || reachedManeuverPoint || timedOut)
        {
            PlanManeuver(state, false);
            return;
        }

        state.Phase = "MANØVRER " + PlannedAspectLabel(state.ManeuverPoint, target);
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

        float sideDot = Vector3.Dot(Flat(unit.transform.position - target.transform.position), right);
        int naturalSide = Mathf.Abs(sideDot) > 0.05f ? (sideDot >= 0f ? 1 : -1) : state.PreferredSide;

        Vector3 rear = target.transform.position - forward * RearDepth + right * (naturalSide * RearLateral);
        Vector3 flank = target.transform.position + right * (naturalSide * FlankOffset) - forward * FlankRearBias;
        rear = SafeMountedPoint(rear, target.transform.position);
        flank = SafeMountedPoint(flank, target.transform.position);

        float rearCost = PlanarDistance(unit.transform.position, rear) * 0.88f;
        float flankCost = PlanarDistance(unit.transform.position, flank);
        state.ManeuverPoint = rearCost <= flankCost * 1.18f ? rear : flank;
        state.TargetAnchor = target.transform.position;
        state.PreferredSide = naturalSide;
        state.ManeuverStarted = newTarget || state.ManeuverStarted <= 0f ? Time.time : state.ManeuverStarted;
        state.Phase = "MANØVRER " + PlannedAspectLabel(state.ManeuverPoint, target);

        unit.SetFormation(PrototypeCavalryFormation09F30.Column);
        unit.OrderMove(state.ManeuverPoint);

        Debug.Log("CAV-AI-09F30C|Unit=" + unit.UnitName + "|Decision=MANEUVER|Goal=" +
                  state.ManeuverPoint.x.ToString("0") + "," + state.ManeuverPoint.z.ToString("0") +
                  "|Aim=" + PlannedAspectLabel(state.ManeuverPoint, target) + "|Target=" + target.RegimentName);
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
