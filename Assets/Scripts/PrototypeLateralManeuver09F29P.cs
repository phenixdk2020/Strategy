using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29p
// Short formation-preserving lateral correction.
// A company that overlaps or blocks a friendly formation may step left/right without
// rotating the whole formation 90 degrees. Parent orders are only suspended for the
// few seconds needed to clear frontage, then resume normally.
[DefaultExecutionOrder(-80)]
public sealed class PrototypeLateralManeuver09F29P : MonoBehaviour
{
    private sealed class SideStepState
    {
        public Regiment Unit;
        public Vector3 Target;
        public Quaternion Facing;
        public float StartedAt;
    }

    private const float ThinkInterval = 0.30f;
    private const float FriendlyTrigger = 13.0f;
    private const float DesiredClearance = 19.0f;
    private const float SideStepDistance = 11.0f;
    private const float SideStepSpeed = 1.45f;
    private const float ArrivalDistance = 0.45f;
    private const float MaxDuration = 12.0f;
    private const float Cooldown = 4.0f;

    private readonly Dictionary<Regiment, SideStepState> active = new Dictionary<Regiment, SideStepState>();
    private readonly Dictionary<Regiment, float> nextEligible = new Dictionary<Regiment, float>();

    private FieldInfo hasDestinationField;
    private FieldInfo forcedTargetField;
    private float nextThink;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeLateralManeuver09F29P>() == null)
            new GameObject("PrototypeLateralManeuver_v000009f29p")
                .AddComponent<PrototypeLateralManeuver09F29P>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);

        if (hasDestinationField == null || forcedTargetField == null)
        {
            Debug.LogError("SIDESTEP-09F29P|Installed=False|Reason=RegimentReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "SIDESTEP-09F29P|Installed=True|PreserveFacing=True|RotateToTravel=False|" +
            "Distance=" + SideStepDistance.ToString("0.0") + "m|Speed=" + SideStepSpeed.ToString("0.00") +
            "mps|FriendlyTrigger=" + FriendlyTrigger.ToString("0.0") + "m");
    }

    private void Update()
    {
        SuppressParentMovementWhileStepping();

        if (Time.time < nextThink)
            return;
        nextThink = Time.time + ThinkInterval;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (!Eligible(unit) || active.ContainsKey(unit))
                continue;

            if (nextEligible.TryGetValue(unit, out float readyAt) && Time.time < readyAt)
                continue;

            Regiment blocker = FindOverlappingFriendly(unit, battle.Regiments);
            if (blocker == null)
                continue;

            Vector3 target;
            if (!TryChooseSideStep(unit, blocker, battle.Regiments, out target))
                continue;

            active[unit] = new SideStepState
            {
                Unit = unit,
                Target = target,
                Facing = unit.transform.rotation,
                StartedAt = Time.time
            };

            hasDestinationField.SetValue(unit, false);
            Debug.Log(
                "SIDESTEP-09F29P|Start=True|Unit=" + unit.RegimentName +
                "|Blocker=" + blocker.RegimentName +
                "|Target=" + target.x.ToString("0.0") + "," + target.z.ToString("0.0") +
                "|FacingPreserved=True");
        }
    }

    private void LateUpdate()
    {
        if (active.Count == 0)
            return;

        List<Regiment> finish = null;
        foreach (KeyValuePair<Regiment, SideStepState> pair in
                 new List<KeyValuePair<Regiment, SideStepState>>(active))
        {
            Regiment unit = pair.Key;
            SideStepState state = pair.Value;
            if (unit == null || state == null || !Eligible(unit) ||
                Time.time - state.StartedAt > MaxDuration)
            {
                AddFinish(ref finish, unit);
                continue;
            }

            // Last writer for the frame: higher command may still own the mission, but
            // it must not rotate/march the company while a short lateral correction runs.
            hasDestinationField.SetValue(unit, false);

            Vector3 here = unit.transform.position;
            Vector3 target = state.Target;
            target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
            Vector3 planar = target - here;
            planar.y = 0f;

            if (planar.magnitude <= ArrivalDistance)
            {
                AddFinish(ref finish, unit);
                continue;
            }

            Vector3 step = planar.normalized * SideStepSpeed * Time.unscaledDeltaTime;
            if (step.magnitude > planar.magnitude)
                step = planar;

            Vector3 next = here + step;
            next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
            unit.transform.position = next;
            unit.transform.rotation = state.Facing;
        }

        if (finish == null)
            return;

        for (int i = 0; i < finish.Count; i++)
            Finish(finish[i]);
    }

    private void SuppressParentMovementWhileStepping()
    {
        if (active.Count == 0 || hasDestinationField == null)
            return;

        foreach (KeyValuePair<Regiment, SideStepState> pair in active)
        {
            Regiment unit = pair.Key;
            if (unit != null)
                hasDestinationField.SetValue(unit, false);
        }
    }

    private bool Eligible(Regiment unit)
    {
        if (unit == null || unit.IsRouted || unit.CurrentStrength <= 0)
            return false;
        if (PrototypeInfantrySquare09F29.IsInSquare(unit))
            return false;
        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(unit))
            return false;
        if (PrototypeAttackContact09F29G.IsLocalContact(unit))
            return false;
        if (PrototypeUnderFireReaction09F26.IsReacting(unit))
            return false;

        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && (charge.IsCharging(unit) || charge.IsChargeMeleeParticipant(unit)))
            return false;

        object forced = forcedTargetField != null ? forcedTargetField.GetValue(unit) : null;
        if (forced is Regiment)
            return false;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment enemy in battle.Regiments)
            {
                if (enemy == null || enemy.Team == unit.Team || enemy.IsRouted || enemy.CurrentStrength <= 0)
                    continue;
                if (PlanarDistance(unit.transform.position, enemy.transform.position) < 10f)
                    return false;
            }
        }

        return true;
    }

    private static Regiment FindOverlappingFriendly(Regiment unit, IReadOnlyList<Regiment> units)
    {
        Regiment nearest = null;
        float best = FriendlyTrigger;

        for (int i = 0; i < units.Count; i++)
        {
            Regiment other = units[i];
            if (other == null || other == unit || other.Team != unit.Team || other.IsRouted || other.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(unit.transform.position, other.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = other;
            }
        }

        return nearest;
    }

    private static bool TryChooseSideStep(
        Regiment unit,
        Regiment blocker,
        IReadOnlyList<Regiment> units,
        out Vector3 target)
    {
        target = unit.transform.position;

        Vector3 right = unit.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        right.Normalize();

        Vector3 leftTarget = Clamp(unit.transform.position - right * SideStepDistance);
        Vector3 rightTarget = Clamp(unit.transform.position + right * SideStepDistance);

        float leftScore = ClearanceScore(unit, blocker, leftTarget, units);
        float rightScore = ClearanceScore(unit, blocker, rightTarget, units);

        if (leftScore < DesiredClearance && rightScore < DesiredClearance)
            return false;

        target = rightScore > leftScore ? rightTarget : leftTarget;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        return true;
    }

    private static float ClearanceScore(
        Regiment unit,
        Regiment blocker,
        Vector3 candidate,
        IReadOnlyList<Regiment> units)
    {
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment other = units[i];
            if (other == null || other == unit || other.Team != unit.Team || other.IsRouted)
                continue;
            nearest = Mathf.Min(nearest, PlanarDistance(candidate, other.transform.position));
        }

        float blockerDistance = blocker != null
            ? PlanarDistance(candidate, blocker.transform.position)
            : nearest;
        float riverDistance = Mathf.Abs(candidate.x - PrototypeBootstrap.StreamCenterX(candidate.z));
        if (riverDistance < 8f)
            return 0f;

        return Mathf.Min(nearest, blockerDistance);
    }

    private void Finish(Regiment unit)
    {
        if (unit == null)
            return;

        active.Remove(unit);
        nextEligible[unit] = Time.time + Cooldown;
        hasDestinationField.SetValue(unit, false);

        Debug.Log(
            "SIDESTEP-09F29P|End=True|Unit=" + unit.RegimentName +
            "|FacingPreserved=True|ParentMissionMayResume=True");
    }

    private static Vector3 Clamp(Vector3 point)
    {
        const float margin = 20f;
        point.x = Mathf.Clamp(point.x,
            -PrototypeBootstrap.BattlefieldHalfWidth + margin,
            PrototypeBootstrap.BattlefieldHalfWidth - margin);
        point.z = Mathf.Clamp(point.z,
            -PrototypeBootstrap.BattlefieldHalfDepth + margin,
            PrototypeBootstrap.BattlefieldHalfDepth - margin);
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void AddFinish(ref List<Regiment> list, Regiment unit)
    {
        if (list == null)
            list = new List<Regiment>();
        list.Add(unit);
    }
}
