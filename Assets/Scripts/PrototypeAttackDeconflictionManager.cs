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
                regiment.EffectiveRange * 1.15f,
                regiment.GetFireTriggerRange() + 12f);

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

            // Unity 6.6 marks Object.GetInstanceID() obsolete. RegimentName is stable
            // and deterministic for the QA OOB, so it is a safer tie-breaker here.
            return string.CompareOrdinal(a.RegimentName, b.RegimentName);
        });

        Regiment primary = attackers[0];
        EngagementState primaryState = states[primary];
        primaryState.HasSlot = false;
        primaryState.SlotIndex = 0;

        Vector3 baseRadial = primary.transform.position - target.transform.position;
        baseRadial.y = 0f;

        if (baseRadial.sqrMagnitude < 0.01f)
        {
            baseRadial = -target.transform.forward;
            baseRadial.y = 0f;
        }

        if (baseRadial.sqrMagnitude < 0.01f)
            baseRadial = Vector3.left;

        baseRadial.Normalize();

        for (int i = 1; i < attackers.Count; i++)
        {
            Regiment attacker = attackers[i];
            EngagementState state = states[attacker];

            bool conflict = state.HasSlot || HasFrontageConflict(attacker, attackers, i, target);
            if (!conflict)
                continue;

            if (!state.HasSlot || state.SlotIndex != i)
            {
                state.HasSlot = true;
                state.SlotIndex = i;

                Debug.Log(string.Format(
                    "AI-SPACING|Unit={0}|Target={1}|Role=Secondary|Slot={2}|Reason=Friendly attack frontage conflict",
                    attacker.RegimentName,
                    target.RegimentName,
                    i));
            }

            float slotAngle = GetSlotAngleDegrees(i);
            Vector3 radial = Quaternion.Euler(0f, slotAngle, 0f) * baseRadial;
            float radius = GetAttackSlotRadius(attacker);

            Vector3 slot = target.transform.position + radial.normalized * radius;
            slot.x = Mathf.Clamp(slot.x, -170f, 170f);
            slot.z = Mathf.Clamp(slot.z, -110f, 110f);
            slot.y = PrototypeBootstrap.SampleGroundHeight(slot.x, slot.z) + 0.10f;

            float distanceToSlot = PlanarDistance(attacker.transform.position, slot);

            attacker.SetFormation(RegimentFormation.Line);

            if (distanceToSlot > SlotArrivalDistance)
            {
                // This manager executes after OfficerAIController and PlayerCommander.
                // It therefore resolves only the final local attack position for an
                // AI-controlled attack and does not interfere with explicit waypoint missions.
                attacker.OrderMove(slot);
            }
            else
            {
                attacker.OrderAttack(target);
            }
        }
    }

    private bool HasFrontageConflict(
        Regiment attacker,
        List<Regiment> orderedAttackers,
        int attackerIndex,
        Regiment target)
    {
        Vector3 attackerRadial = attacker.transform.position - target.transform.position;
        attackerRadial.y = 0f;

        for (int i = 0; i < attackerIndex; i++)
        {
            Regiment earlier = orderedAttackers[i];
            if (earlier == null)
                continue;

            float friendlyDistance =
                PlanarDistance(attacker.transform.position, earlier.transform.position);

            if (friendlyDistance < MinimumFriendlySeparation)
                return true;

            Vector3 earlierRadial = earlier.transform.position - target.transform.position;
            earlierRadial.y = 0f;

            if (attackerRadial.sqrMagnitude > 0.01f && earlierRadial.sqrMagnitude > 0.01f)
            {
                float angularSeparation = Vector3.Angle(
                    attackerRadial.normalized,
                    earlierRadial.normalized);

                if (angularSeparation < 24f)
                    return true;
            }
        }

        return false;
    }

    private static float GetSlotAngleDegrees(int slotIndex)
    {
        int pair = (slotIndex + 1) / 2;
        float angle = 34f * pair;
        return slotIndex % 2 == 1 ? angle : -angle;
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
