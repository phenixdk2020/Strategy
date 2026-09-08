using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1300)]
public sealed class PrototypeAttackFrontagePlannerV2 : MonoBehaviour
{
    private sealed class EngagementState
    {
        public Regiment Target;
        public float EnteredAt;
        public bool HasSlot;
        public int SlotIndex;
        public Vector3 SlotPosition;
        public Vector3 TargetPositionAtAssignment;
        public float LastDistanceToSlot = float.PositiveInfinity;
        public float LastProgressAt;
        public int RecoveryCount;
    }

    private readonly Dictionary<Regiment, EngagementState> states =
        new Dictionary<Regiment, EngagementState>();

    private bool legacyPlannerDisabled;
    private float nextThinkTime;

    private const float ThinkInterval = 0.25f;
    private const float SlotArrivalDistance = 4.0f;
    private const float MinimumFriendlySeparation = 22f;
    private const float LateralSlotSpacing = 24f;

    // v00.00.09h2 stability: once a secondary frontage slot is allocated it is
    // sticky. We only reconsider it when the target has moved materially, the slot
    // became blocked/conflicted, or the regiment has made no useful progress for a
    // sustained interval. This prevents the old 0.25 s slot-flip/chase loop.
    private const float ProgressEpsilon = 0.65f;
    private const float StuckReplanSeconds = 3.5f;
    private const float TargetMovementReplanDistance = 14f;
    private const float TargetSwitchAdvantage = 1.18f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackFrontagePlannerV2>() != null)
            return;

        GameObject root = new GameObject("PrototypeAttackFrontagePlannerV2_v009h2");
        root.AddComponent<PrototypeAttackFrontagePlannerV2>();
    }

    private void Update()
    {
        DisableLegacyPlanner();

        if (Time.time < nextThinkTime)
            return;

        nextThinkTime = Time.time + ThinkInterval;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Dictionary<Regiment, List<Regiment>> groups =
            new Dictionary<Regiment, List<Regiment>>();

        foreach (Regiment attacker in battle.Regiments)
        {
            if (!IsAttackAI(attacker))
                continue;

            if (!states.TryGetValue(attacker, out EngagementState state))
            {
                state = new EngagementState();
                states[attacker] = state;
            }

            Regiment nearest = FindNearestEnemy(attacker, battle);
            Regiment target = ChooseStableTarget(attacker, state, nearest);
            if (target == null)
                continue;

            if (state.Target != target)
                ResetForTarget(state, target);

            float threshold = Mathf.Max(
                attacker.EffectiveRange * 1.30f,
                attacker.GetFireTriggerRange() + 18f);

            if (PlanarDistance(attacker.transform.position, target.transform.position) > threshold)
                continue;

            if (!groups.TryGetValue(target, out List<Regiment> list))
            {
                list = new List<Regiment>();
                groups[target] = list;
            }

            list.Add(attacker);
        }

        foreach (KeyValuePair<Regiment, List<Regiment>> pair in groups)
            AllocateFrontage(pair.Key, pair.Value);
    }

    private void DisableLegacyPlanner()
    {
        if (legacyPlannerDisabled)
            return;

        PrototypeAttackDeconflictionManager legacy =
            Object.FindAnyObjectByType<PrototypeAttackDeconflictionManager>();

        if (legacy != null)
        {
            legacy.enabled = false;
            legacyPlannerDisabled = true;
            Debug.Log("AI-SPACING-V3|LegacyPlannerDisabled=True|Build=v00.00.09h2");
        }
    }

    private void AllocateFrontage(Regiment target, List<Regiment> attackers)
    {
        if (target == null || attackers == null || attackers.Count < 2)
            return;

        attackers.Sort((a, b) =>
        {
            EngagementState sa = states[a];
            EngagementState sb = states[b];

            int timeCompare = sa.EnteredAt.CompareTo(sb.EnteredAt);
            if (timeCompare != 0)
                return timeCompare;

            return string.CompareOrdinal(a.RegimentName, b.RegimentName);
        });

        Regiment primary = attackers[0];

        Vector3 radial = primary.transform.position - target.transform.position;
        radial.y = 0f;
        if (radial.sqrMagnitude < 0.01f)
        {
            radial = -target.transform.forward;
            radial.y = 0f;
        }
        if (radial.sqrMagnitude < 0.01f)
            radial = Vector3.left;
        radial.Normalize();

        Vector3 frontage = Vector3.Cross(Vector3.up, radial).normalized;
        if (frontage.sqrMagnitude < 0.01f)
            frontage = Vector3.forward;

        float primaryRadius = Mathf.Clamp(
            PlanarDistance(primary.transform.position, target.transform.position),
            GetAttackRadius(primary) * 0.88f,
            GetAttackRadius(primary) * 1.12f);

        HashSet<int> reservedSlots = new HashSet<int> { 0 };
        List<Vector3> reservedPositions = new List<Vector3>
        {
            primary.transform.position
        };

        for (int i = 1; i < attackers.Count; i++)
        {
            Regiment attacker = attackers[i];
            EngagementState state = states[attacker];

            string replanReason = GetReplanReason(attacker, target, state, reservedSlots);
            if (!string.IsNullOrEmpty(replanReason))
            {
                HashSet<int> choiceReserved = new HashSet<int>(reservedSlots);

                // A genuine stall should try a different frontage slot instead of
                // immediately selecting the exact same destination again.
                if (state.HasSlot && replanReason == "NoProgress")
                    choiceReserved.Add(state.SlotIndex);

                int slotIndex;
                Vector3 slot;
                ChooseBestSlot(
                    attacker,
                    target,
                    radial,
                    frontage,
                    primaryRadius,
                    choiceReserved,
                    reservedPositions,
                    out slotIndex,
                    out slot);

                AssignSlot(state, target, slotIndex, slot);

                if (replanReason == "NoProgress")
                    state.RecoveryCount++;

                Debug.Log(string.Format(
                    "AI-SPACING-V3|Unit={0}|Target={1}|Slot={2}|Pos=({3:0.0},{4:0.0})|Reason={5}|RecoveryCount={6}",
                    attacker.RegimentName,
                    target.RegimentName,
                    state.SlotIndex,
                    state.SlotPosition.x,
                    state.SlotPosition.z,
                    replanReason,
                    state.RecoveryCount));
            }

            if (!state.HasSlot)
                continue;

            reservedSlots.Add(state.SlotIndex);
            reservedPositions.Add(state.SlotPosition);

            attacker.SetFormation(RegimentFormation.Line);

            float distanceToSlot =
                PlanarDistance(attacker.transform.position, state.SlotPosition);
            TrackProgress(state, distanceToSlot);

            if (distanceToSlot > SlotArrivalDistance)
            {
                attacker.OrderMove(state.SlotPosition);
            }
            else
            {
                attacker.OrderHold();
                FaceTarget(attacker, target);

                state.LastDistanceToSlot = distanceToSlot;
                state.LastProgressAt = Time.time;
            }
        }
    }

    private static Regiment ChooseStableTarget(
        Regiment attacker,
        EngagementState state,
        Regiment nearest)
    {
        if (attacker == null)
            return null;

        Regiment current = state != null ? state.Target : null;
        if (!IsValidEnemy(attacker, current))
            return nearest;

        if (nearest == null || nearest == current)
            return current;

        float currentDistance =
            PlanarDistance(attacker.transform.position, current.transform.position);
        float nearestDistance =
            PlanarDistance(attacker.transform.position, nearest.transform.position);

        // Do not churn between two nearly-equidistant enemies. A new nearest target
        // must be materially closer before frontage ownership changes.
        return currentDistance <= nearestDistance * TargetSwitchAdvantage
            ? current
            : nearest;
    }

    private static void ResetForTarget(EngagementState state, Regiment target)
    {
        state.Target = target;
        state.EnteredAt = Time.time;
        state.HasSlot = false;
        state.SlotIndex = 0;
        state.SlotPosition = Vector3.zero;
        state.TargetPositionAtAssignment = target != null
            ? target.transform.position
            : Vector3.zero;
        state.LastDistanceToSlot = float.PositiveInfinity;
        state.LastProgressAt = Time.time;
        state.RecoveryCount = 0;
    }

    private static string GetReplanReason(
        Regiment attacker,
        Regiment target,
        EngagementState state,
        HashSet<int> alreadyReserved)
    {
        if (!state.HasSlot)
            return "InitialSlot";

        if (alreadyReserved.Contains(state.SlotIndex))
            return "SlotConflict";

        if (PrototypeBattlefieldNavigationManager.IsBlockedDestination(
                state.SlotPosition,
                RegimentFormation.Line))
        {
            return "SlotBlocked";
        }

        if (target != null &&
            PlanarDistance(target.transform.position, state.TargetPositionAtAssignment) >=
            TargetMovementReplanDistance)
        {
            return "TargetMoved";
        }

        float distance = PlanarDistance(attacker.transform.position, state.SlotPosition);
        if (distance <= SlotArrivalDistance + 1f)
            return null;

        if (state.LastProgressAt > 0f &&
            Time.time - state.LastProgressAt >= StuckReplanSeconds)
        {
            return "NoProgress";
        }

        return null;
    }

    private static void AssignSlot(
        EngagementState state,
        Regiment target,
        int slotIndex,
        Vector3 slot)
    {
        state.HasSlot = true;
        state.SlotIndex = slotIndex;
        state.SlotPosition = slot;
        state.TargetPositionAtAssignment = target != null
            ? target.transform.position
            : Vector3.zero;
        state.LastDistanceToSlot = float.PositiveInfinity;
        state.LastProgressAt = Time.time;
    }

    private static void TrackProgress(EngagementState state, float distanceToSlot)
    {
        if (float.IsInfinity(state.LastDistanceToSlot) ||
            distanceToSlot <= state.LastDistanceToSlot - ProgressEpsilon)
        {
            state.LastDistanceToSlot = distanceToSlot;
            state.LastProgressAt = Time.time;
            return;
        }

        // Do not let tiny frame-to-frame noise increase the stored best distance.
        // The elapsed LastProgressAt time is what triggers the controlled replan.
    }

    private static void ChooseBestSlot(
        Regiment attacker,
        Regiment target,
        Vector3 radial,
        Vector3 frontage,
        float radius,
        HashSet<int> reservedSlots,
        List<Vector3> reservedPositions,
        out int bestSlotIndex,
        out Vector3 bestSlot)
    {
        bestSlotIndex = 1;
        bestSlot = attacker.transform.position;
        float bestCost = float.PositiveInfinity;
        bool found = false;

        for (int magnitude = 1; magnitude <= 5; magnitude++)
        {
            int[] candidates = { magnitude, -magnitude };

            foreach (int slotIndex in candidates)
            {
                if (reservedSlots.Contains(slotIndex))
                    continue;

                Vector3 requested =
                    target.transform.position +
                    radial * radius +
                    frontage * (slotIndex * LateralSlotSpacing);

                requested.x = Mathf.Clamp(requested.x, -170f, 170f);
                requested.z = Mathf.Clamp(requested.z, -110f, 110f);
                requested.y = 0f;

                Vector3 candidate = requested;
                Vector3 safe;
                bool valid = PrototypeBattlefieldNavigationManager.TryFindNearestValidDestination(
                    requested,
                    RegimentFormation.Line,
                    out safe);

                float safeAdjustment = 0f;
                if (valid)
                {
                    safeAdjustment = PlanarDistance(requested, safe);
                    candidate = safe;
                }

                float travelCost = PlanarDistance(attacker.transform.position, candidate);
                float approachPenalty = PrototypeNavigationRecoveryManager.EstimatePathPenalty(
                    attacker.transform.position,
                    candidate,
                    RegimentFormation.Line);

                float targetLinePenalty = PrototypeNavigationRecoveryManager.EstimatePathPenalty(
                    candidate,
                    target.transform.position,
                    RegimentFormation.Line);

                float friendlyPenalty = 0f;
                foreach (Vector3 reserved in reservedPositions)
                {
                    float d = PlanarDistance(candidate, reserved);
                    if (d < MinimumFriendlySeparation)
                        friendlyPenalty += 1000f + (MinimumFriendlySeparation - d) * 30f;
                }

                float blockedPenalty =
                    PrototypeBattlefieldNavigationManager.IsBlockedDestination(
                        candidate,
                        RegimentFormation.Line)
                        ? 1500f
                        : 0f;

                float cost =
                    travelCost +
                    approachPenalty * 1.10f +
                    targetLinePenalty * 1.65f +
                    safeAdjustment * 8f +
                    friendlyPenalty +
                    blockedPenalty;

                if (cost >= bestCost)
                    continue;

                found = true;
                bestCost = cost;
                bestSlotIndex = slotIndex;
                bestSlot = candidate;
            }
        }

        // Fallback should be rare, but never leave a secondary attacker with its
        // current position as an accidental permanent frontage slot simply because
        // all scored slots were reserved.
        if (!found)
        {
            bestSlotIndex = 1;
            bestSlot = target.transform.position + radial * radius + frontage * LateralSlotSpacing;
            bestSlot.x = Mathf.Clamp(bestSlot.x, -170f, 170f);
            bestSlot.z = Mathf.Clamp(bestSlot.z, -110f, 110f);
        }

        bestSlot.y = PrototypeBootstrap.SampleGroundHeight(bestSlot.x, bestSlot.z) + 0.10f;
    }

    private static float GetAttackRadius(Regiment attacker)
    {
        float trigger = attacker.GetFireTriggerRange();
        if (trigger <= 0.1f)
            trigger = attacker.EffectiveRange * 0.80f;

        return Mathf.Clamp(
            trigger * 0.90f,
            attacker.CloseRange * 0.82f,
            attacker.MaximumRange * 0.90f);
    }

    private static bool IsAttackAI(Regiment regiment)
    {
        if (regiment == null || regiment.IsRouted)
            return false;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
            return false;

        return controller.Mission == OfficerAIMission.AttackNearest ||
               controller.Mission == OfficerAIMission.AttackTarget;
    }

    private static bool IsValidEnemy(Regiment attacker, Regiment candidate)
    {
        return attacker != null &&
               candidate != null &&
               candidate != attacker &&
               candidate.Team != attacker.Team &&
               !candidate.IsRouted &&
               candidate.CurrentStrength > 0;
    }

    private static Regiment FindNearestEnemy(Regiment attacker, BattleManager battle)
    {
        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (!IsValidEnemy(attacker, candidate))
                continue;

            float distance = PlanarDistance(attacker.transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static void FaceTarget(Regiment regiment, Regiment target)
    {
        if (regiment == null || target == null)
            return;

        Vector3 direction = target.transform.position - regiment.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        regiment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
