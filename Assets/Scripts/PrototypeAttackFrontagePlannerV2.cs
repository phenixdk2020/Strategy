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
    }

    private readonly Dictionary<Regiment, EngagementState> states =
        new Dictionary<Regiment, EngagementState>();

    private bool legacyPlannerDisabled;
    private float nextThinkTime;

    private const float ThinkInterval = 0.25f;
    private const float SlotArrivalDistance = 4.0f;
    private const float MinimumFriendlySeparation = 22f;
    private const float LateralSlotSpacing = 24f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackFrontagePlannerV2>() != null)
            return;

        GameObject root = new GameObject("PrototypeAttackFrontagePlannerV2_v009");
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

            Regiment target = FindNearestEnemy(attacker, battle);
            if (target == null)
                continue;

            if (!states.TryGetValue(attacker, out EngagementState state))
            {
                state = new EngagementState();
                states[attacker] = state;
            }

            if (state.Target != target)
            {
                state.Target = target;
                state.EnteredAt = Time.time;
                state.HasSlot = false;
                state.SlotIndex = 0;
                state.SlotPosition = Vector3.zero;
            }

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
            Debug.Log("AI-SPACING-V2|LegacyPlannerDisabled=True");
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

        HashSet<int> reservedSlots = new HashSet<int>();
        reservedSlots.Add(0);

        List<Vector3> reservedPositions = new List<Vector3>();
        reservedPositions.Add(primary.transform.position);

        for (int i = 1; i < attackers.Count; i++)
        {
            Regiment attacker = attackers[i];
            EngagementState state = states[attacker];

            int slotIndex;
            Vector3 slot;
            ChooseBestSlot(
                attacker,
                target,
                radial,
                frontage,
                primaryRadius,
                reservedSlots,
                reservedPositions,
                out slotIndex,
                out slot);

            reservedSlots.Add(slotIndex);
            reservedPositions.Add(slot);

            if (!state.HasSlot ||
                state.SlotIndex != slotIndex ||
                PlanarDistance(state.SlotPosition, slot) > 3f)
            {
                state.HasSlot = true;
                state.SlotIndex = slotIndex;
                state.SlotPosition = slot;

                Debug.Log(string.Format(
                    "AI-SPACING-V2|Unit={0}|Target={1}|Slot={2}|Pos=({3:0.0},{4:0.0})|Reason=ObstacleAwareSharedFrontage",
                    attacker.RegimentName,
                    target.RegimentName,
                    slotIndex,
                    slot.x,
                    slot.z));
            }

            attacker.SetFormation(RegimentFormation.Line);

            if (PlanarDistance(attacker.transform.position, slot) > SlotArrivalDistance)
            {
                attacker.OrderMove(slot);
            }
            else
            {
                attacker.OrderHold();
                FaceTarget(attacker, target);
            }
        }
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

                // A house/tree between the final slot and the target is especially bad:
                // the regiment would arrive but have poor frontage/line of fire.
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

                bestCost = cost;
                bestSlotIndex = slotIndex;
                bestSlot = candidate;
            }
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

    private static Regiment FindNearestEnemy(Regiment attacker, BattleManager battle)
    {
        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null ||
                candidate == attacker ||
                candidate.Team == attacker.Team ||
                candidate.IsRouted)
            {
                continue;
            }

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
