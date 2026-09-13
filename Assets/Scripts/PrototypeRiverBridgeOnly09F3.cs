using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f3 river-only navigation constraint.
// Scenery remains pass-through. The stream is the only tactical movement barrier:
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
    }

    private readonly Dictionary<Regiment, RiverState> states =
        new Dictionary<Regiment, RiverState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;

    private const float RiverHalfWidth = 2.20f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 9.0f;
    private const float BridgeHalfWidthZ = 4.0f;
    private const float BridgeApproachOffset = 11.0f;
    private const float ApproachArrival = 2.0f;

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
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("RIVER-09F3|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "RIVER-09F3|Installed=True|River=Blocked|Crossing=BridgeOnly|" +
            "BridgeZ=22|SceneryObstacleWrites=False");
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

            if (!states.TryGetValue(regiment, out RiverState state))
            {
                state = new RiverState();
                states[regiment] = state;
            }

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

            // Hard safety net: no representative regiment centre may occupy open
            // water. If another system writes a bad destination in the same frame,
            // restore the last legal position and keep the bridge route active.
            if (state.HasLastSafe)
            {
                regiment.transform.position = state.LastSafePosition;
                Debug.LogWarning(
                    "RIVER-09F3|Unit=" + regiment.RegimentName +
                    "|OpenWaterPrevented=True|RestoredLastSafe=True");
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
            state.HasGoal = false;
            SetPhase(regiment, state, "DIRECT");
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

        bool externalGoal =
            !state.HasGoal ||
            (!NearlySame(rawDestination, state.FinalGoal, 1.25f) &&
             !NearlySame(rawDestination, state.LastSteeringTarget, 1.25f));

        if (externalGoal)
        {
            state.FinalGoal = SanitizeGoal(rawDestination, current);
            state.StartSide = GetBankSide(current);
            if (state.StartSide == 0)
                state.StartSide = GetBankSide(state.LastSafePosition);
            if (state.StartSide == 0)
                state.StartSide = current.x < StreamCenterX(current.z) ? -1 : 1;
            state.HasGoal = true;
            SetPhase(regiment, state, "DIRECT");
        }

        Vector3 goal = state.FinalGoal;
        bool crossingRequired = SegmentTouchesOpenWater(current, goal);

        if (!crossingRequired && !IsInOpenWater(goal))
        {
            WriteSteering(regiment, state, goal);
            SetPhase(regiment, state, "DIRECT");
            return;
        }

        Vector3 bridgeCenter = new Vector3(
            StreamCenterX(BridgeZ),
            0f,
            BridgeZ);

        Vector3 entry = bridgeCenter + Vector3.right * (state.StartSide * BridgeApproachOffset);
        Vector3 exit = bridgeCenter - Vector3.right * (state.StartSide * BridgeApproachOffset);

        int currentSide = GetBankSide(current);
        bool crossedToOtherBank = currentSide != 0 && currentSide != state.StartSide;

        if (!crossedToOtherBank)
        {
            if (!IsBridgeZone(current) && PlanarDistance(current, entry) > ApproachArrival)
            {
                entry.y = PrototypeBootstrap.SampleGroundHeight(entry.x, entry.z) + 0.10f;
                WriteSteering(regiment, state, entry);
                SetPhase(regiment, state, "APPROACH_BRIDGE");
                return;
            }

            exit.y = PrototypeBootstrap.SampleGroundHeight(exit.x, exit.z) + 0.10f;
            WriteSteering(regiment, state, exit);
            SetPhase(regiment, state, "CROSS_BRIDGE");
            return;
        }

        WriteSteering(regiment, state, goal);
        SetPhase(regiment, state, "EXIT_BRIDGE");
    }

    private void WriteSteering(Regiment regiment, RiverState state, Vector3 target)
    {
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        destinationField.SetValue(regiment, target);
        hasDestinationField.SetValue(regiment, true);
        state.LastSteeringTarget = target;
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
        const int samples = 48;
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
            "RIVER-09F3|Unit=" + regiment.RegimentName +
            "|Phase=" + phase +
            "|BridgeOnly=True");
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
