using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29b attack-authority hotfix.
// This layer no longer writes physical movement every 0.25 s. The old behaviour could
// fight the Major mission owner, OfficerAIController and under-fire reaction at once.
// F29B keeps frontage allocation as a diagnostic/advisory layer, respects explicit
// AttackTarget missions, and converts AttackNearest to a sticky explicit target only
// after contact has entered the engagement envelope.
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
        public Vector3 LastTargetPosition;
    }

    private readonly Dictionary<Regiment, EngagementState> states =
        new Dictionary<Regiment, EngagementState>();

    private bool legacyPlannerDisabled;
    private float nextThinkTime;
    private FieldInfo missionTargetField;

    private const float ThinkInterval = 0.35f;
    private const float MinimumFriendlySeparation = 55f;
    private const float LateralSlotSpacing = 60f;
    private const float StickyContactFloor = 140f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackFrontagePlannerV2>() != null)
            return;

        GameObject root = new GameObject("PrototypeAttackFrontagePlannerV2_v000009f29b");
        root.AddComponent<PrototypeAttackFrontagePlannerV2>();
    }

    private void Awake()
    {
        missionTargetField = typeof(OfficerAIController).GetField(
            "missionTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (missionTargetField == null)
        {
            Debug.LogError("AI-SPACING-09F29B|Installed=False|Reason=MissionTargetReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "AI-SPACING-09F29B|Installed=True|MovementWrites=False|ExplicitTargetRespected=True|" +
            "MajorAuthorityRespected=True|UnderFireAuthorityRespected=True|StickyContactTarget=True");
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
        HashSet<Regiment> activeAttackers = new HashSet<Regiment>();

        foreach (Regiment attacker in battle.Regiments)
        {
            OfficerAIController controller;
            if (!IsPlannerEligible(attacker, out controller))
                continue;

            Regiment target = ResolveTarget(attacker, controller, battle);
            if (!IsValidEnemy(attacker, target))
                continue;

            activeAttackers.Add(attacker);

            EngagementState state = GetOrCreate(attacker);
            if (state.Target != target)
            {
                state.Target = target;
                state.EnteredAt = Time.time;
                state.HasSlot = false;
                state.SlotIndex = 0;
                state.SlotPosition = Vector3.zero;
                state.LastTargetPosition = target.transform.position;
            }

            float threshold = GetEngagementThreshold(attacker);
            float distance = PlanarDistance(attacker.transform.position, target.transform.position);

            // AttackNearest is allowed to assess targets at long range. Once it enters
            // the actual engagement envelope, lock the chosen enemy as AttackTarget.
            // It remains locked until that target routs/dies, preventing nearest-target
            // oscillation during the approach/firefight.
            if (controller.Mission == OfficerAIMission.AttackNearest &&
                distance <= Mathf.Max(threshold, StickyContactFloor))
            {
                controller.SetAttackMission(target);
                Debug.Log(
                    "AI-TARGET-09F29B|Unit=" + attacker.RegimentName +
                    "|Target=" + target.RegimentName +
                    "|Locked=True|Reason=ContactEntered|Distance=" + distance.ToString("0.0"));
            }

            if (distance > threshold)
                continue;

            if (!groups.TryGetValue(target, out List<Regiment> attackers))
            {
                attackers = new List<Regiment>();
                groups[target] = attackers;
            }

            attackers.Add(attacker);
        }

        CleanupStates(activeAttackers);

        foreach (KeyValuePair<Regiment, List<Regiment>> pair in groups)
            AllocateAdvisoryFrontage(pair.Key, pair.Value);
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
            Debug.Log("AI-SPACING-09F29B|LegacyPlannerDisabled=True");
        }

        legacyPlannerDisabled = true;
    }

    private bool IsPlannerEligible(Regiment attacker, out OfficerAIController controller)
    {
        controller = null;

        if (attacker == null || attacker.IsRouted || attacker.CurrentStrength <= 0)
            return false;

        controller = attacker.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
            return false;

        // CRITICAL F29B authority rule: enabled=false means another system deliberately
        // owns physical movement (e.g. Major mission placement). Never write or re-plan it.
        if (!controller.enabled)
            return false;

        if (PrototypeUnderFireReaction09F26.IsReacting(attacker))
            return false;

        return controller.Mission == OfficerAIMission.AttackNearest ||
               controller.Mission == OfficerAIMission.AttackTarget;
    }

    private Regiment ResolveTarget(
        Regiment attacker,
        OfficerAIController controller,
        BattleManager battle)
    {
        if (controller.Mission == OfficerAIMission.AttackTarget)
        {
            Regiment explicitTarget = missionTargetField.GetValue(controller) as Regiment;
            return IsValidEnemy(attacker, explicitTarget) ? explicitTarget : null;
        }

        Regiment nearest = FindNearestEnemy(attacker, battle);
        if (!states.TryGetValue(attacker, out EngagementState state) ||
            !IsValidEnemy(attacker, state.Target))
        {
            return nearest;
        }

        if (!IsValidEnemy(attacker, nearest) || nearest == state.Target)
            return state.Target;

        float currentDistance = PlanarDistance(attacker.transform.position, state.Target.transform.position);
        float candidateDistance = PlanarDistance(attacker.transform.position, nearest.transform.position);

        // Hysteresis before contact: only swap if the new candidate is both materially
        // closer in metres and clearly closer proportionally. Small geometry changes
        // must not make a formation pivot between enemies every think cycle.
        bool materiallyBetter =
            candidateDistance + 45f < currentDistance &&
            candidateDistance < currentDistance * 0.82f;

        return materiallyBetter ? nearest : state.Target;
    }

    private EngagementState GetOrCreate(Regiment attacker)
    {
        if (!states.TryGetValue(attacker, out EngagementState state) || state == null)
        {
            state = new EngagementState();
            states[attacker] = state;
        }
        return state;
    }

    private void AllocateAdvisoryFrontage(Regiment target, List<Regiment> attackers)
    {
        if (target == null || attackers == null || attackers.Count < 2)
            return;

        // The nearest company to the target is the primary. This prevents a farther
        // company from becoming the frontage anchor and making the nearer formation
        // manoeuvre backwards merely because it entered the group first.
        attackers.Sort((a, b) =>
        {
            float da = PlanarDistance(a.transform.position, target.transform.position);
            float db = PlanarDistance(b.transform.position, target.transform.position);
            int distanceCompare = da.CompareTo(db);
            if (distanceCompare != 0)
                return distanceCompare;

            return string.CompareOrdinal(a.RegimentName, b.RegimentName);
        });

        Regiment primary = attackers[0];
        EngagementState primaryState = GetOrCreate(primary);
        primaryState.HasSlot = true;
        primaryState.SlotIndex = 0;
        primaryState.SlotPosition = primary.transform.position;
        primaryState.LastTargetPosition = target.transform.position;

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
            EngagementState state = GetOrCreate(attacker);

            bool targetMoved = PlanarDistance(state.LastTargetPosition, target.transform.position) > 14f;
            if (!state.HasSlot || state.Target != target || targetMoved)
            {
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

                state.Target = target;
                state.HasSlot = true;
                state.SlotIndex = slotIndex;
                state.SlotPosition = slot;
                state.LastTargetPosition = target.transform.position;

                Debug.Log(
                    "AI-SPACING-09F29B|Unit=" + attacker.RegimentName +
                    "|Target=" + target.RegimentName +
                    "|Slot=" + slotIndex +
                    "|Pos=" + slot.x.ToString("0.0") + "," + slot.z.ToString("0.0") +
                    "|MovementWrite=False|Authority=OfficerAI");
            }

            reservedSlots.Add(state.SlotIndex);
            reservedPositions.Add(state.SlotPosition);
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

                float xLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfWidth - 30f);
                float zLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfDepth - 30f);
                requested.x = Mathf.Clamp(requested.x, -xLimit, xLimit);
                requested.z = Mathf.Clamp(requested.z, -zLimit, zLimit);
                requested.y = 0f;

                Vector3 candidate = requested;
                if (PrototypeBattlefieldNavigationManager.TryFindNearestValidDestination(
                    requested,
                    RegimentFormation.Line,
                    out Vector3 safe))
                {
                    candidate = safe;
                }

                float travelCost = PlanarDistance(attacker.transform.position, candidate);
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

                float cost = travelCost + friendlyPenalty + blockedPenalty;
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

    private static float GetEngagementThreshold(Regiment attacker)
    {
        return Mathf.Max(
            attacker.EffectiveRange * 1.30f,
            attacker.GetFireTriggerRange() + 18f);
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

    private static bool IsValidEnemy(Regiment attacker, Regiment candidate)
    {
        return attacker != null &&
               candidate != null &&
               candidate != attacker &&
               candidate.Team != attacker.Team &&
               !candidate.IsRouted &&
               candidate.CurrentStrength > 0;
    }

    private void CleanupStates(HashSet<Regiment> activeAttackers)
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, EngagementState> pair in states)
        {
            Regiment unit = pair.Key;
            if (unit != null && !unit.IsRouted && unit.CurrentStrength > 0 &&
                (activeAttackers.Contains(unit) || pair.Value.Target != null))
            {
                continue;
            }

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(unit);
        }

        if (remove == null)
            return;

        foreach (Regiment unit in remove)
            states.Remove(unit);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
