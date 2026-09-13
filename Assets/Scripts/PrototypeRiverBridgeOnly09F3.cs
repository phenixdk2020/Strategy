using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f3 river-only navigation constraint, extended by v00.00.09f9.
// Scenery remains pass-through. The stream is the only tactical movement barrier:
// any route that would cross water is redirected through the fixed bridge at z=22.
// v09f9 also exposes pre-movement steering for continuously tracked ATTACK targets,
// so Regiment.Update cannot overwrite the bridge route with a direct water crossing.
[DefaultExecutionOrder(5000)]
public sealed class PrototypeRiverBridgeOnly09F3 : MonoBehaviour
{
    private sealed class RiverState
    {
        public bool HasGoal;
        public Vector3 FinalGoal;
        public Vector3 LastSteeringTarget;
        public int StartSide;
        public string Phase = "DIRECT";
        public bool HasLastSafe;
        public Vector3 LastSafePosition;
        public bool BridgeRouteActive;
        public float LastWaterWarningAt = -100f;
    }

    public static PrototypeRiverBridgeOnly09F3 Instance { get; private set; }

    private readonly Dictionary<Regiment, RiverState> states =
        new Dictionary<Regiment, RiverState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;

    private const float RiverHalfWidth = 2.20f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 9.0f;
    private const float BridgeHalfWidthZ = 4.0f;

    // v09f9: the old 11 m approach was too short for a 190-man company to visibly
    // reform from three-rank Line into a six-wide marching Column before the bridge.
    private const float BridgeApproachOffset = 24.0f;
    private const float ApproachArrival = 2.0f;
    private const float ExitClearDistance = 2.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeRiverBridgeOnly09F3>() != null)
            return;

        GameObject root = new GameObject("PrototypeRiverBridgeOnly_v000009f3");
        root.AddComponent<PrototypeRiverBridgeOnly09F3>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("RIVER-09F9|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "RIVER-09F9|Installed=True|River=Blocked|Crossing=BridgeOnly|" +
            "BridgeZ=22|ApproachOffset=" + BridgeApproachOffset.ToString("0") +
            "m|AttackPreSteering=True|BridgeForcesColumn=True|SceneryObstacleWrites=False");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Called by Regiment before UpdateMovement when a tracked attack target is active.
    /// This is the critical v09f9 fix: the bridge steering target is resolved before the
    /// regiment takes its movement step instead of after it has already stepped toward water.
    /// </summary>
    public static bool TryGetAttackSteering(
        Regiment regiment,
        Vector3 requestedGoal,
        out Vector3 steeringTarget)
    {
        steeringTarget = requestedGoal;
        if (Instance == null || !Instance.enabled || regiment == null || regiment.IsRouted)
            return false;

        RiverState state = Instance.GetOrCreateState(regiment);
        requestedGoal.y = 0f;

        return Instance.ResolveBridgeSteering(
            regiment,
            state,
            requestedGoal,
            true,
            out steeringTarget);
    }

    public static bool IsBridgeRouteActive(Regiment regiment)
    {
        if (Instance == null || regiment == null)
            return false;

        return Instance.states.TryGetValue(regiment, out RiverState state) &&
               state != null &&
               state.BridgeRouteActive;
    }

    private RiverState GetOrCreateState(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out RiverState state) || state == null)
        {
            state = new RiverState();
            states[regiment] = state;
        }
        return state;
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            active.Add(regiment);
            RiverState state = GetOrCreateState(regiment);
            ApplyRiverConstraint(regiment, state);
        }

        Cleanup(active);
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            if (!states.TryGetValue(regiment, out RiverState state))
                continue;

            Vector3 current = regiment.transform.position;
            if (!IsInOpenWater(current))
            {
                state.LastSafePosition = current;
                state.HasLastSafe = true;
                continue;
            }

            // Safety net. Under normal v09f9 attack routing this should no longer fire.
            if (state.HasLastSafe)
            {
                regiment.transform.position = state.LastSafePosition;

                // Avoid flooding the Console if another future movement writer bypasses
                // the bridge authority. One warning per unit per second is enough for QA.
                if (Time.unscaledTime - state.LastWaterWarningAt >= 1f)
                {
                    state.LastWaterWarningAt = Time.unscaledTime;
                    Debug.LogWarning(
                        "RIVER-09F9|Unit=" + regiment.RegimentName +
                        "|OpenWaterPrevented=True|RestoredLastSafe=True|UnexpectedWriter=True");
                }
            }
        }
    }

    private void ApplyRiverConstraint(Regiment regiment, RiverState state)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        if (!IsInOpenWater(current))
        {
            state.LastSafePosition = regiment.transform.position;
            state.HasLastSafe = true;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            // Keep an active attack bridge route alive if Regiment briefly reaches an
            // intermediate steering point. The attack pre-steering call will continue it.
            if (!state.BridgeRouteActive)
            {
                state.HasGoal = false;
                SetPhase(regiment, state, "DIRECT");
            }
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

        // A raw destination equal to our own previous steering point is not a new goal.
        bool rawIsOurSteering =
            state.HasGoal && NearlySame(rawDestination, state.LastSteeringTarget, 1.25f);

        Vector3 requestedGoal = rawIsOurSteering
            ? state.FinalGoal
            : rawDestination;

        if (ResolveBridgeSteering(
                regiment,
                state,
                requestedGoal,
                false,
                out Vector3 steering))
        {
            WriteSteering(regiment, state, steering);
            return;
        }

        WriteSteering(regiment, state, state.HasGoal ? state.FinalGoal : requestedGoal);
        SetPhase(regiment, state, "DIRECT");
    }

    private bool ResolveBridgeSteering(
        Regiment regiment,
        RiverState state,
        Vector3 requestedGoal,
        bool continuousAttackGoal,
        out Vector3 steeringTarget)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;
        requestedGoal.y = 0f;

        UpdateFinalGoal(state, current, requestedGoal, continuousAttackGoal);
        Vector3 goal = state.FinalGoal;

        Vector3 bridgeCenter = new Vector3(
            StreamCenterX(BridgeZ),
            0f,
            BridgeZ);

        // If no route is currently active, determine whether this final goal requires
        // crossing open water. Same-bank routes that cut through a stream meander are
        // also redirected through the fixed bridge.
        if (!state.BridgeRouteActive)
        {
            bool crossingRequired =
                SegmentTouchesOpenWater(current, goal) ||
                IsInOpenWater(goal);

            if (!crossingRequired)
            {
                steeringTarget = goal;
                return false;
            }

            state.StartSide = GetBankSide(current);
            if (state.StartSide == 0 && state.HasLastSafe)
                state.StartSide = GetBankSide(state.LastSafePosition);
            if (state.StartSide == 0)
                state.StartSide = current.x < StreamCenterX(current.z) ? -1 : 1;

            state.BridgeRouteActive = true;

            // The bridge is narrow enough that the 190-man company must approach
            // and cross in marching column. Do this as soon as the route is identified,
            // leaving the longer 24 m approach for visible physical reformation.
            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);

            Debug.Log(
                "RIVER-09F9|Unit=" + regiment.RegimentName +
                "|BridgeRoute=True|StartSide=" + state.StartSide +
                "|ColumnForced=True|Attack=" + continuousAttackGoal);
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        Vector3 entry = bridgeCenter + Vector3.right * (state.StartSide * BridgeApproachOffset);
        Vector3 exit = bridgeCenter - Vector3.right * (state.StartSide * BridgeApproachOffset);

        int currentSide = GetBankSide(current);
        bool crossedToOtherBank = currentSide != 0 && currentSide != state.StartSide;

        if (!crossedToOtherBank)
        {
            if (!IsBridgeZone(current) && PlanarDistance(current, entry) > ApproachArrival)
            {
                steeringTarget = WithGroundHeight(entry);
                state.LastSteeringTarget = steeringTarget;
                SetPhase(regiment, state, "APPROACH_BRIDGE");
                return true;
            }

            steeringTarget = WithGroundHeight(exit);
            state.LastSteeringTarget = steeringTarget;
            SetPhase(regiment, state, "CROSS_BRIDGE");
            return true;
        }

        // Do not immediately fan back out on the far bridge edge. Keep the company
        // in column until its centre has reached the far staging point.
        if (PlanarDistance(current, exit) > ExitClearDistance)
        {
            steeringTarget = WithGroundHeight(exit);
            state.LastSteeringTarget = steeringTarget;
            SetPhase(regiment, state, "EXIT_BRIDGE");
            return true;
        }

        state.BridgeRouteActive = false;
        steeringTarget = WithGroundHeight(goal);
        state.LastSteeringTarget = steeringTarget;
        SetPhase(regiment, state, "DIRECT");

        Debug.Log(
            "RIVER-09F9|Unit=" + regiment.RegimentName +
            "|BridgeRoute=False|CrossingComplete=True|FinalGoalResumed=True");

        return false;
    }

    private void UpdateFinalGoal(
        RiverState state,
        Vector3 current,
        Vector3 requestedGoal,
        bool continuousAttackGoal)
    {
        Vector3 sanitized = SanitizeGoal(requestedGoal, current);

        if (!state.HasGoal)
        {
            state.FinalGoal = sanitized;
            state.HasGoal = true;
            return;
        }

        // Tracked ATTACK targets may move every frame. Update their final goal without
        // resetting StartSide/bridge phase while the crossing is already in progress.
        if (continuousAttackGoal)
        {
            state.FinalGoal = sanitized;
            return;
        }

        bool isOwnSteering = NearlySame(sanitized, state.LastSteeringTarget, 1.25f);
        if (!isOwnSteering && !NearlySame(sanitized, state.FinalGoal, 1.25f))
        {
            state.FinalGoal = sanitized;

            if (!state.BridgeRouteActive)
            {
                state.StartSide = GetBankSide(current);
                SetPhaseSilently(state, "DIRECT");
            }
        }
    }

    private void WriteSteering(Regiment regiment, RiverState state, Vector3 target)
    {
        target = WithGroundHeight(target);
        destinationField.SetValue(regiment, target);
        hasDestinationField.SetValue(regiment, true);
        state.LastSteeringTarget = target;
    }

    private static Vector3 WithGroundHeight(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static Vector3 SanitizeGoal(Vector3 requested, Vector3 current)
    {
        if (!IsInOpenWater(requested))
            return requested;

        int side = GetBankSide(current);
        if (side == 0)
            side = requested.x < StreamCenterX(requested.z) ? -1 : 1;

        float riverX = StreamCenterX(requested.z);
        requested.x = riverX + side * (RiverHalfWidth + 1.5f);
        requested.y = 0f;
        return requested;
    }

    private static bool SegmentTouchesOpenWater(Vector3 a, Vector3 b)
    {
        const int samples = 64;
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(a, b, t);
            if (IsInOpenWater(p))
                return true;
        }

        return false;
    }

    private static bool IsInOpenWater(Vector3 point)
    {
        if (IsBridgeZone(point))
            return false;

        float riverX = StreamCenterX(point.z);
        return Mathf.Abs(point.x - riverX) < RiverHalfWidth;
    }

    private static bool IsBridgeZone(Vector3 point)
    {
        float riverX = StreamCenterX(BridgeZ);
        return Mathf.Abs(point.z - BridgeZ) <= BridgeHalfWidthZ &&
               Mathf.Abs(point.x - riverX) <= BridgeHalfLengthX;
    }

    private static int GetBankSide(Vector3 point)
    {
        float delta = point.x - StreamCenterX(point.z);
        if (delta < -RiverHalfWidth)
            return -1;
        if (delta > RiverHalfWidth)
            return 1;
        return 0;
    }

    private static float StreamCenterX(float z)
    {
        return Mathf.Sin(z * 0.065f) * 4.8f;
    }

    private static bool NearlySame(Vector3 a, Vector3 b, float tolerance)
    {
        return PlanarDistance(a, b) <= tolerance;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void SetPhase(Regiment regiment, RiverState state, string phase)
    {
        if (state.Phase == phase)
            return;

        state.Phase = phase;
        Debug.Log(
            "RIVER-09F9|Unit=" + regiment.RegimentName +
            "|Phase=" + phase +
            "|BridgeOnly=True|Column=" + (regiment.Formation == RegimentFormation.Column));
    }

    private static void SetPhaseSilently(RiverState state, string phase)
    {
        state.Phase = phase;
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, RiverState> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }
}
