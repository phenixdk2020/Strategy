using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30a
// Formation-preserving lateral correction.
// F29P handled only close physical overlap. F30A also detects friendly formations that
// obstruct another company's actual firing lane to a live target. The tactically less
// useful formation shifts left/right while preserving Line and facing.
[DefaultExecutionOrder(-80)]
public sealed class PrototypeLateralManeuver09F29P : MonoBehaviour
{
    private sealed class SideStepState
    {
        public Regiment Unit;
        public Vector3 Target;
        public Quaternion Facing;
        public float StartedAt;
        public float HoldUntil;
        public bool Arrived;
        public string Reason;
    }

    private const float ThinkInterval = 0.30f;
    private const float FriendlyTrigger = 13.0f;
    private const float DesiredClearance = 10.5f;
    private const float OverlapSideStepDistance = 12.0f;

    // Company Line frontage is about 48 m. 55 m keeps the two complete formations from
    // sitting directly one behind another while still allowing neighbouring frontage.
    private const float FireLaneConflictHalfWidth = 55.0f;
    private const float FireLaneDesiredClearance = 60.0f;
    private const float MinFireLaneStep = 18.0f;
    private const float MaxFireLaneStep = 46.0f;

    private const float SideStepSpeed = 1.75f;
    private const float ArrivalDistance = 0.45f;
    private const float MaxDuration = 32.0f;
    private const float FireLaneSettleSeconds = 4.0f;
    private const float Cooldown = 4.0f;

    private readonly Dictionary<Regiment, SideStepState> active =
        new Dictionary<Regiment, SideStepState>();
    private readonly Dictionary<Regiment, float> nextEligible =
        new Dictionary<Regiment, float>();

    private FieldInfo hasDestinationField;
    private FieldInfo forcedTargetField;
    private FieldInfo missionTargetField;
    private float nextThink;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeLateralManeuver09F29P>() == null)
            new GameObject("PrototypeLateralManeuver_v000009f30a")
                .AddComponent<PrototypeLateralManeuver09F29P>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);
        missionTargetField = typeof(OfficerAIController).GetField("missionTarget", flags);

        if (hasDestinationField == null || forcedTargetField == null)
        {
            Debug.LogError("SIDESTEP-09F30A|Installed=False|Reason=RegimentReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "SIDESTEP-09F30A|Installed=True|PreserveFacing=True|RotateToTravel=False|" +
            "Overlap=True|FriendlyFireLane=True|FireLaneClearance=" +
            FireLaneDesiredClearance.ToString("0") + "m|Speed=" + SideStepSpeed.ToString("0.00") + "mps");
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

            Regiment closeFriendly = FindOverlappingFriendly(unit, battle.Regiments);
            Regiment laneShooter = null;
            Regiment laneBlocker = null;
            Regiment laneTarget = null;
            string reason = null;
            Vector3 target;

            if (closeFriendly != null)
            {
                // Existing F29P deterministic overlap rule.
                if (string.CompareOrdinal(unit.RegimentName ?? string.Empty,
                                          closeFriendly.RegimentName ?? string.Empty) < 0)
                    continue;

                if (!TryChooseOverlapSideStep(unit, closeFriendly, battle.Regiments, out target))
                    continue;

                reason = "OVERLAP";
            }
            else
            {
                if (!TryFindFireLaneConflict(
                        unit,
                        battle.Regiments,
                        out laneShooter,
                        out laneBlocker,
                        out laneTarget))
                    continue;

                if (!TryChooseFireLaneSideStep(
                        unit,
                        laneShooter,
                        laneBlocker,
                        laneTarget,
                        battle.Regiments,
                        out target))
                    continue;

                reason = "FIRE_LANE";
            }

            active[unit] = new SideStepState
            {
                Unit = unit,
                Target = target,
                Facing = unit.transform.rotation,
                StartedAt = Time.time,
                HoldUntil = 0f,
                Arrived = false,
                Reason = reason
            };

            hasDestinationField.SetValue(unit, false);

            Debug.Log(
                "SIDESTEP-09F30A|Start=True|Unit=" + unit.RegimentName +
                "|Reason=" + reason +
                (laneShooter != null ? "|Shooter=" + laneShooter.RegimentName : string.Empty) +
                (laneBlocker != null ? "|Blocker=" + laneBlocker.RegimentName : string.Empty) +
                (laneTarget != null ? "|Enemy=" + laneTarget.RegimentName : string.Empty) +
                "|Target=" + target.x.ToString("0.0") + "," + target.z.ToString("0.0") +
                "|FacingPreserved=True|Formation=" + unit.Formation);
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

            // Last writer for the frame: higher command may retain mission ownership,
            // but must not pull the company back into the blocked lane while Side Step owns
            // this short local manoeuvre.
            hasDestinationField.SetValue(unit, false);
            unit.transform.rotation = state.Facing;

            if (state.Arrived)
            {
                if (Time.time >= state.HoldUntil)
                    AddFinish(ref finish, unit);
                continue;
            }

            Vector3 here = unit.transform.position;
            Vector3 target = state.Target;
            target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
            Vector3 planar = target - here;
            planar.y = 0f;

            if (planar.magnitude <= ArrivalDistance)
            {
                state.Arrived = true;
                state.HoldUntil = Time.time + (state.Reason == "FIRE_LANE" ? FireLaneSettleSeconds : 0.35f);
                continue;
            }

            Vector3 step = planar.normalized * SideStepSpeed * Time.deltaTime;
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

        // Explicit MANUAL attack remains above autonomous local manoeuvre. AI-owned
        // AttackTarget is allowed to sidestep because obtaining a clear firing lane is
        // part of executing that higher mission, not replacing it.
        object forced = forcedTargetField != null ? forcedTargetField.GetValue(unit) : null;
        OfficerAIController controller = unit.GetComponent<OfficerAIController>();
        if (forced is Regiment && (controller == null || !controller.AIEnabled))
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
            if (other == null || other == unit || other.Team != unit.Team ||
                other.IsRouted || other.CurrentStrength <= 0)
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

    private bool TryFindFireLaneConflict(
        Regiment requestedMover,
        IReadOnlyList<Regiment> units,
        out Regiment shooterResult,
        out Regiment blockerResult,
        out Regiment targetResult)
    {
        shooterResult = null;
        blockerResult = null;
        targetResult = null;

        if (!IsAutoTacticalUnit(requestedMover))
            return false;

        for (int s = 0; s < units.Count; s++)
        {
            Regiment shooter = units[s];
            if (!IsPotentialShooter(shooter, requestedMover.Team))
                continue;

            Regiment target = ResolveLiveFireTarget(shooter, units);
            if (target == null)
                continue;

            Vector3 fireForward = target.transform.position - shooter.transform.position;
            fireForward.y = 0f;
            float targetDistance = fireForward.magnitude;
            if (targetDistance < 8f)
                continue;
            fireForward /= targetDistance;

            float triggerRange = Mathf.Max(4f, shooter.GetFireTriggerRange());
            if (targetDistance > Mathf.Min(shooter.MaximumRange, triggerRange) + 3f)
                continue;

            float facingAngle = Vector3.Angle(Flat(shooter.transform.forward), fireForward);
            if (facingAngle > 36f)
                continue;

            Vector3 laneRight = Vector3.Cross(Vector3.up, fireForward).normalized;

            for (int b = 0; b < units.Count; b++)
            {
                Regiment blocker = units[b];
                if (blocker == null || blocker == shooter || blocker.Team != shooter.Team ||
                    blocker.IsRouted || blocker.CurrentStrength <= 0)
                    continue;

                Vector3 relative = blocker.transform.position - shooter.transform.position;
                relative.y = 0f;
                float along = Vector3.Dot(relative, fireForward);
                if (along <= 5f || along >= targetDistance - 4f)
                    continue;

                float lateral = Mathf.Abs(Vector3.Dot(relative, laneRight));
                if (lateral >= FireLaneConflictHalfWidth)
                    continue;

                Regiment mover = ChooseFireLaneMover(shooter, blocker, target);
                if (mover == null || mover != requestedMover)
                    continue;

                shooterResult = shooter;
                blockerResult = blocker;
                targetResult = target;
                return true;
            }
        }

        return false;
    }

    private Regiment ChooseFireLaneMover(Regiment shooter, Regiment blocker, Regiment target)
    {
        bool shooterAuto = IsAutoTacticalUnit(shooter) && Eligible(shooter);
        bool blockerAuto = IsAutoTacticalUnit(blocker) && Eligible(blocker);

        if (!shooterAuto && !blockerAuto)
            return null;
        if (!blockerAuto)
            return shooterAuto ? shooter : null;
        if (!shooterAuto)
            return blocker;

        // If the front formation is itself productively engaging the SAME target, keep
        // that established frontage and move the rear shooter laterally to obtain its own lane.
        bool blockerUseful = IsProductivelyEngaging(blocker, target);
        float shooterDistance = PlanarDistance(shooter.transform.position, target.transform.position);
        float blockerDistance = PlanarDistance(blocker.transform.position, target.transform.position);

        if (blockerUseful && blockerDistance + 3f < shooterDistance)
            return shooter;

        // Otherwise the obstruction is the tactically less useful formation and it moves.
        return blocker;
    }

    private bool IsProductivelyEngaging(Regiment unit, Regiment target)
    {
        if (unit == null || target == null || unit.Formation != RegimentFormation.Line)
            return false;

        Regiment ownTarget = ResolveLiveFireTarget(unit, BattleManager.Instance != null
            ? BattleManager.Instance.Regiments
            : null);
        if (ownTarget != target)
            return false;

        Vector3 toTarget = target.transform.position - unit.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance <= 0.1f || distance > unit.GetFireTriggerRange() + 3f)
            return false;

        return Vector3.Angle(Flat(unit.transform.forward), toTarget.normalized) <= 36f;
    }

    private Regiment ResolveLiveFireTarget(Regiment shooter, IReadOnlyList<Regiment> units)
    {
        if (shooter == null)
            return null;

        Regiment forced = forcedTargetField != null
            ? forcedTargetField.GetValue(shooter) as Regiment
            : null;
        if (IsValidEnemy(shooter, forced))
            return forced;

        OfficerAIController ai = shooter.GetComponent<OfficerAIController>();
        if (ai != null && missionTargetField != null)
        {
            Regiment missionTarget = missionTargetField.GetValue(ai) as Regiment;
            if (IsValidEnemy(shooter, missionTarget))
                return missionTarget;
        }

        if (units == null)
            return null;

        Regiment nearest = null;
        float best = Mathf.Max(4f, shooter.GetFireTriggerRange()) + 3f;
        Vector3 forward = Flat(shooter.transform.forward);

        for (int i = 0; i < units.Count; i++)
        {
            Regiment enemy = units[i];
            if (!IsValidEnemy(shooter, enemy))
                continue;

            Vector3 delta = enemy.transform.position - shooter.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= 0.1f || distance >= best)
                continue;
            if (Vector3.Angle(forward, delta.normalized) > 36f)
                continue;

            best = distance;
            nearest = enemy;
        }

        return nearest;
    }

    private static bool IsPotentialShooter(Regiment unit, BattleTeam team)
    {
        if (unit == null || unit.Team != team || unit.IsRouted || unit.CurrentStrength <= 0)
            return false;
        if (unit.Formation != RegimentFormation.Line)
            return false;
        if (PrototypeInfantrySquare09F29.IsInSquare(unit))
            return false;
        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(unit))
            return false;
        return true;
    }

    private static bool IsAutoTacticalUnit(Regiment unit)
    {
        if (unit == null)
            return false;
        OfficerAIController ai = unit.GetComponent<OfficerAIController>();
        return ai != null && ai.AIEnabled;
    }

    private static bool IsValidEnemy(Regiment own, Regiment candidate)
    {
        return own != null && candidate != null && candidate != own &&
               candidate.Team != own.Team && !candidate.IsRouted && candidate.CurrentStrength > 0;
    }

    private static bool TryChooseOverlapSideStep(
        Regiment unit,
        Regiment blocker,
        IReadOnlyList<Regiment> units,
        out Vector3 target)
    {
        target = unit.transform.position;

        Vector3 right = Flat(unit.transform.right);
        Vector3 leftTarget = Clamp(unit.transform.position - right * OverlapSideStepDistance);
        Vector3 rightTarget = Clamp(unit.transform.position + right * OverlapSideStepDistance);

        float leftScore = ClearanceScore(unit, blocker, leftTarget, units);
        float rightScore = ClearanceScore(unit, blocker, rightTarget, units);

        if (leftScore < DesiredClearance && rightScore < DesiredClearance)
            return false;

        target = rightScore > leftScore ? rightTarget : leftTarget;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        return true;
    }

    private static bool TryChooseFireLaneSideStep(
        Regiment unit,
        Regiment shooter,
        Regiment blocker,
        Regiment enemy,
        IReadOnlyList<Regiment> units,
        out Vector3 target)
    {
        target = unit.transform.position;
        if (shooter == null || blocker == null || enemy == null)
            return false;

        Vector3 fireForward = Flat(enemy.transform.position - shooter.transform.position);
        Vector3 right = Vector3.Cross(Vector3.up, fireForward).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Flat(unit.transform.right);

        Vector3 relative = unit.transform.position - shooter.transform.position;
        relative.y = 0f;
        float currentLateral = Vector3.Dot(relative, right);
        float requiredShift = Mathf.Clamp(
            FireLaneDesiredClearance - Mathf.Abs(currentLateral) + 3f,
            MinFireLaneStep,
            MaxFireLaneStep);

        Vector3 leftTarget = Clamp(unit.transform.position - right * requiredShift);
        Vector3 rightTarget = Clamp(unit.transform.position + right * requiredShift);

        float leftScore = FireLaneCandidateScore(unit, shooter, enemy, leftTarget, units);
        float rightScore = FireLaneCandidateScore(unit, shooter, enemy, rightTarget, units);

        if (leftScore < FireLaneDesiredClearance * 0.70f &&
            rightScore < FireLaneDesiredClearance * 0.70f)
            return false;

        target = rightScore > leftScore ? rightTarget : leftTarget;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        return true;
    }

    private static float FireLaneCandidateScore(
        Regiment unit,
        Regiment shooter,
        Regiment enemy,
        Vector3 candidate,
        IReadOnlyList<Regiment> units)
    {
        if (PrototypeBattlefieldNavigationManager.IsBlockedDestination(candidate, RegimentFormation.Line))
            return 0f;

        float riverDistance = Mathf.Abs(candidate.x - PrototypeBootstrap.StreamCenterX(candidate.z));
        if (riverDistance < 8f)
            return 0f;

        float friendlyClearance = float.PositiveInfinity;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment other = units[i];
            if (other == null || other == unit || other.Team != unit.Team ||
                other.IsRouted || other.CurrentStrength <= 0)
                continue;
            friendlyClearance = Mathf.Min(
                friendlyClearance,
                PlanarDistance(candidate, other.transform.position));
        }

        Vector3 forward = Flat(enemy.transform.position - shooter.transform.position);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 relative = candidate - shooter.transform.position;
        relative.y = 0f;
        float laneClearance = Mathf.Abs(Vector3.Dot(relative, right));

        return Mathf.Min(friendlyClearance, laneClearance);
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

        string reason = active.TryGetValue(unit, out SideStepState state) && state != null
            ? state.Reason
            : "UNKNOWN";

        active.Remove(unit);
        nextEligible[unit] = Time.time + Cooldown;
        hasDestinationField.SetValue(unit, false);

        Debug.Log(
            "SIDESTEP-09F30A|End=True|Unit=" + unit.RegimentName +
            "|Reason=" + reason +
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

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
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
