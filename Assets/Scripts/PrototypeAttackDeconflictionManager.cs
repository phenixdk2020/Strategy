using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(600)]
public sealed class PrototypeAttackDeconflictionManager : MonoBehaviour
{
    private sealed class EngagementState
    {
        public Regiment Target;
        public bool EnteredEngagement;
        public float EnteredAt;
        public bool HasSlot;
        public int SlotIndex;
    }

    private readonly Dictionary<Regiment, EngagementState> states =
        new Dictionary<Regiment, EngagementState>();

    private float nextThinkTime;

    private const float ThinkInterval = 0.20f;
    private const float MinimumFriendlySeparation = 24f;
    private const float SlotArrivalDistance = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackDeconflictionManager>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeAttackDeconflictionManager_v009");
        managerObject.AddComponent<PrototypeAttackDeconflictionManager>();
    }

    private void Update()
    {
        if (Time.time < nextThinkTime)
            return;

        nextThinkTime = Time.time + ThinkInterval;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Dictionary<Regiment, List<Regiment>> groups =
            new Dictionary<Regiment, List<Regiment>>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsAttackAI(regiment))
                continue;

            Regiment target = FindNearestEnemy(regiment, battle);
            if (target == null)
                continue;

            if (!states.TryGetValue(regiment, out EngagementState state))
            {
                state = new EngagementState();
                states[regiment] = state;
            }

            if (state.Target != target)
            {
                state.Target = target;
                state.EnteredEngagement = false;
                state.EnteredAt = 0f;
                state.HasSlot = false;
                state.SlotIndex = 0;
            }

            float distance = PlanarDistance(regiment.transform.position, target.transform.position);
            float engagementThreshold = Mathf.Max(
                regiment.EffectiveRange * 1.30f,
                regiment.GetFireTriggerRange() + 18f);

            if (!state.EnteredEngagement && distance <= engagementThreshold)
            {
                state.EnteredEngagement = true;
                state.EnteredAt = Time.time;
            }

            if (!state.EnteredEngagement)
                continue;

            if (!groups.TryGetValue(target, out List<Regiment> attackers))
            {
                attackers = new List<Regiment>();
                groups[target] = attackers;
            }

            attackers.Add(regiment);
        }

        foreach (KeyValuePair<Regiment, List<Regiment>> pair in groups)
            DeconflictGroup(pair.Key, pair.Value);
    }

    private void DeconflictGroup(Regiment target, List<Regiment> attackers)
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
        EngagementState primaryState = states[primary];
        primaryState.HasSlot = false;
        primaryState.SlotIndex = 0;

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
        Vector3 frontageDirection = Vector3.Cross(Vector3.up, radial).normalized;

        float primaryRadius = Mathf.Clamp(
            PlanarDistance(primary.transform.position, target.transform.position),
            GetAttackSlotRadius(primary) * 0.88f,
            GetAttackSlotRadius(primary) * 1.12f);

        HashSet<int> reservedSlots = new HashSet<int>();
        reservedSlots.Add(0);

        for (int i = 1; i < attackers.Count; i++)
        {
            Regiment attacker = attackers[i];
            EngagementState state = states[attacker];

            int slotIndex = ChooseBestSlot(
                attacker,
                target,
                radial,
                frontageDirection,
                primaryRadius,
                reservedSlots);

            reservedSlots.Add(slotIndex);

            if (!state.HasSlot || state.SlotIndex != slotIndex)
            {
                state.HasSlot = true;
                state.SlotIndex = slotIndex;

                Debug.Log(string.Format(
                    "AI-SPACING|Unit={0}|Target={1}|Role=Secondary|Slot={2}|Reason=Shared target frontage allocation",
                    attacker.RegimentName,
                    target.RegimentName,
                    slotIndex));
            }

            Vector3 slot = BuildSlotPosition(
                target,
                radial,
                frontageDirection,
                primaryRadius,
                slotIndex);

            Vector3 safeSlot;
            if (PrototypeBattlefieldNavigationManager.TryFindNearestValidDestination(
                slot,
                RegimentFormation.Line,
                out safeSlot))
            {
                slot = safeSlot;
            }

            slot.y = PrototypeBootstrap.SampleGroundHeight(slot.x, slot.z) + 0.10f;

            float distanceToSlot = PlanarDistance(attacker.transform.position, slot);
            attacker.SetFormation(RegimentFormation.Line);

            if (distanceToSlot > SlotArrivalDistance)
            {
                attacker.OrderMove(slot);
            }
            else
            {
                // Hold the allocated frontage instead of issuing OrderAttack(target),
                // because OrderAttack would pull every secondary regiment back toward
                // the target centre and recreate the overlap we are trying to avoid.
                attacker.OrderHold();
                FaceTarget(attacker, target);
            }
        }
    }

    private static int ChooseBestSlot(
        Regiment attacker,
        Regiment target,
        Vector3 radial,
        Vector3 frontageDirection,
        float radius,
        HashSet<int> reservedSlots)
    {
        int bestSlot = 1;
        float bestCost = float.PositiveInfinity;

        for (int magnitude = 1; magnitude <= 4; magnitude++)
        {
            int[] candidates = { magnitude, -magnitude };

            foreach (int candidate in candidates)
            {
                if (reservedSlots.Contains(candidate))
                    continue;

                Vector3 slot = BuildSlotPosition(
                    target,
                    radial,
                    frontageDirection,
                    radius,
                    candidate);

                float travelCost = PlanarDistance(attacker.transform.position, slot);

                // A blocked destination is still possible because navigation can route
                // around obstacles, but prefer a directly valid frontage slot when one exists.
                if (PrototypeBattlefieldNavigationManager.IsBlockedDestination(
                    slot,
                    RegimentFormation.Line))
                {
                    travelCost += 60f;
                }

                if (travelCost < bestCost)
                {
                    bestCost = travelCost;
                    bestSlot = candidate;
                }
            }
        }

        return bestSlot;
    }

    private static Vector3 BuildSlotPosition(
        Regiment target,
        Vector3 radial,
        Vector3 frontageDirection,
        float radius,
        int slotIndex)
    {
        float lateralOffset = slotIndex * MinimumFriendlySeparation;
        Vector3 slot = target.transform.position + radial * radius + frontageDirection * lateralOffset;
        slot.x = Mathf.Clamp(slot.x, -170f, 170f);
        slot.z = Mathf.Clamp(slot.z, -110f, 110f);
        return slot;
    }

    private static void FaceTarget(Regiment attacker, Regiment target)
    {
        if (attacker == null || target == null)
            return;

        Vector3 facing = target.transform.position - attacker.transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            return;

        attacker.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
    }

    private static float GetAttackSlotRadius(Regiment attacker)
    {
        float triggerRange = attacker.GetFireTriggerRange();

        if (triggerRange <= 0.1f)
            triggerRange = attacker.EffectiveRange * 0.80f;

        return Mathf.Clamp(
            triggerRange * 0.90f,
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

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
