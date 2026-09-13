using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f3 river-only navigation constraint, extended through v00.00.09f11.
// Scenery remains pass-through except explicitly approved hard blockers handled by
// PrototypeStaticObstacleRouting09F11. The stream remains a hard tactical barrier:
// any route that would cross water is redirected through the fixed bridge at z=22.
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

    private const float BridgeStagingOffset = 32.0f;
    private const float BridgeEntryOffset = 13.0f;
    private const float PointArrival = 2.0f;
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
            Debug.LogError("RIVER-09F11|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "RIVER-09F11|Installed=True|River=Blocked|Crossing=BridgeOnly|" +
            "BridgeZ=22|Staging=" + BridgeStagingOffset.ToString("0") +
            "m|Entry=" + BridgeEntryOffset.ToString("0") +
            "m|AttackPreSteering=True|BridgeForcesColumn=True|StrictPhaseMachine=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

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

            if (state.HasLastSafe)
            {
                regiment.transform.position = state.LastSafePosition;

                if (Time.unscaledTime - state.LastWaterWarningAt >= 1f)
                {
                    state.LastWaterWarningAt = Time.unscaledTime;
                    Debug.LogWarning(
                        "RIVER-09F11|Unit=" + regiment.RegimentName +
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
            if (!state.BridgeRouteActive)
            {
                state.HasGoal = false;
                SetPhase(regiment, state, "DIRECT");
            }
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

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
            SetPhase(regiment, state, "STAGE_BRIDGE");

            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);

            Debug.Log(
                "RIVER-09F11|Unit=" + regiment.RegimentName +
                "|BridgeRoute=True|StartSide=" + state.StartSide +
                "|ColumnForced=True|Attack=" + continuousAttackGoal);
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        Vector3 nearStaging = bridgeCenter + Vector3.right * (state.StartSide * BridgeStagingOffset);
        Vector3 nearEntry = bridgeCenter + Vector3.right * (state.StartSide * BridgeEntryOffset);
        Vector3 farEntry = bridgeCenter - Vector3.right * (state.StartSide * BridgeEntryOffset);
        Vector3 farStaging = bridgeCenter - Vector3.right * (state.StartSide * BridgeStagingOffset);

        int currentSide = GetBankSide(current);
        bool crossedToOtherBank = currentSide != 0 && currentSide != state.StartSide;

        if (!crossedToOtherBank)
        {
            // v09f11 strict one-way phase machine. The previous implementation could
            // switch CROSS_BRIDGE back to APPROACH_BRIDGE after the pivot moved more
            // than 2 m away from nearEntry, producing the oscillation visible in QA.
            if (state.Phase == "STAGE_BRIDGE")
            {
                if (PlanarDistance(current, nearStaging) > PointArrival)
                {
                    steeringTarget = WithGroundHeight(nearStaging);
                    state.LastSteeringTarget = steeringTarget;
                    return true;
                }

                SetPhase(regiment, state, "APPROACH_BRIDGE");
            }

            if (state.Phase == "APPROACH_BRIDGE")
            {
                if (PlanarDistance(current, nearEntry) > PointArrival)
                {
                    steeringTarget = WithGroundHeight(nearEntry);
                    state.LastSteeringTarget = steeringTarget;
                    return true;
                }

                SetPhase(regiment, state, "CROSS_BRIDGE");
            }

            // Once CROSS_BRIDGE begins it may not return to APPROACH_BRIDGE.
            steeringTarget = WithGroundHeight(farEntry);
            state.LastSteeringTarget = steeringTarget;
            if (state.Phase != "CROSS_BRIDGE")
                SetPhase(regiment, state, "CROSS_BRIDGE");
            return true;
        }

        if (PlanarDistance(current, farStaging) > ExitClearDistance)
        {
            steeringTarget = WithGroundHeight(farStaging);
            state.LastSteeringTarget = steeringTarget;
            SetPhase(regiment, state, "EXIT_BRIDGE");
            return true;
        }

        state.BridgeRouteActive = false;
        steeringTarget = WithGroundHeight(goal);
        state.LastSteeringTarget = steeringTarget;
        SetPhase(regiment, state, "DIRECT");

        Debug.Log(
            "RIVER-09F11|Unit=" + regiment.RegimentName +
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
            "RIVER-09F11|Unit=" + regiment.RegimentName +
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
