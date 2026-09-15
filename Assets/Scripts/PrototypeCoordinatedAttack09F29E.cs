using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29e
// Coordinated multi-company attack execution.
// F29B correctly stopped the old frontage planner from fighting multiple movement owners,
// but that also left several companies with the same AttackTarget free to converge on the
// same centre line. F29E keeps OfficerAIController as the physical movement owner and gives
// a group of companies distinct approach/fire positions around their designated target.
// The manager never writes through a disabled controller (Major/higher-command authority)
// and suspends itself during the F26 under-fire reaction.
[DefaultExecutionOrder(1650)]
public sealed class PrototypeCoordinatedAttack09F29E : MonoBehaviour
{
    private sealed class AttackState
    {
        public Regiment Unit;
        public Regiment Target;
        public Vector3 Slot;
        public Vector3 TargetPositionAtPlan;
        public bool HoldingAtSlot;
        public bool Support;
        public int SlotIndex;
    }

    public static PrototypeCoordinatedAttack09F29E Instance { get; private set; }

    private readonly Dictionary<Regiment, AttackState> states =
        new Dictionary<Regiment, AttackState>();

    private FieldInfo missionTargetField;
    private FieldInfo missionPointField;
    private float nextPlan;

    private const float PlanInterval = 0.45f;
    private const float CoordinationRange = 320f;
    private const float SlotArrival = 2.25f;
    private const float TargetReplanDistance = 12f;
    private const float SlotReissueDistance = 6f;
    private const float SupportDepth = 78f;
    private const float SupportSpacing = 58f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCoordinatedAttack09F29E>() == null)
            new GameObject("PrototypeCoordinatedAttack_v000009f29e")
                .AddComponent<PrototypeCoordinatedAttack09F29E>();
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
        missionTargetField = typeof(OfficerAIController).GetField("missionTarget", flags);
        missionPointField = typeof(OfficerAIController).GetField("missionPoint", flags);

        if (missionTargetField == null || missionPointField == null)
        {
            Debug.LogError("COORD-ATTACK-09F29E|Installed=False|Reason=OfficerAIReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "COORD-ATTACK-09F29E|Installed=True|SingleMovementOwner=OfficerAIController|" +
            "ExplicitTargetOnly=True|MaxFrontCompanies=4|UnderFireAuthorityRespected=True|" +
            "HigherCommandDisabledControllerRespected=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        MaintainExistingStates();

        if (Time.time < nextPlan)
            return;
        nextPlan = Time.time + PlanInterval;

        PlanCoordinatedGroups();
    }

    private void MaintainExistingStates()
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        List<KeyValuePair<Regiment, AttackState>> snapshot =
            new List<KeyValuePair<Regiment, AttackState>>(states);

        foreach (KeyValuePair<Regiment, AttackState> pair in snapshot)
        {
            Regiment unit = pair.Key;
            AttackState state = pair.Value;
            if (!IsValidUnit(unit) || state == null || !IsValidEnemy(unit, state.Target))
            {
                Add(ref remove, unit);
                continue;
            }

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled)
            {
                Add(ref remove, unit);
                continue;
            }

            // F26 temporarily disables the controller while returning fire. Its local
            // reaction has authority and our stored coordination slot simply waits.
            if (PrototypeUnderFireReaction09F26.IsReacting(unit))
                continue;

            // A disabled controller outside F26 means a Major/regimental mission has
            // reclaimed physical movement. Drop our local coordination state immediately.
            if (!controller.enabled)
            {
                Debug.Log("COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                          "|Released=True|Reason=HIGHER_COMMAND_MOVEMENT_AUTHORITY");
                Add(ref remove, unit);
                continue;
            }

            // Detect a new explicit player route while we own a coordination state.
            if (controller.Mission == OfficerAIMission.MoveToPoint)
            {
                Vector3 missionPoint = ReadMissionPoint(controller);
                if (PlanarDistance(missionPoint, state.Slot) > 4f)
                {
                    Debug.Log("COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                              "|Released=True|Reason=NEW_EXTERNAL_MOVE_MISSION");
                    Add(ref remove, unit);
                    continue;
                }
            }
            else if (controller.Mission == OfficerAIMission.AttackTarget)
            {
                Regiment explicitTarget = ReadMissionTarget(controller);
                if (explicitTarget != null && explicitTarget != state.Target)
                {
                    Add(ref remove, unit);
                    continue;
                }
            }
            else if (controller.Mission != OfficerAIMission.Hold)
            {
                // Doctrine changes / AttackNearest / defensive missions are external
                // authority changes. Do not try to resurrect the old grouped attack.
                Add(ref remove, unit);
                continue;
            }

            float distanceToSlot = PlanarDistance(unit.transform.position, state.Slot);
            if (distanceToSlot > SlotArrival)
            {
                state.HoldingAtSlot = false;
                if (controller.Mission != OfficerAIMission.MoveToPoint ||
                    PlanarDistance(ReadMissionPoint(controller), state.Slot) > 1.5f)
                {
                    controller.SetMoveMission(state.Slot);
                    unit.OrderMove(state.Slot);
                }
            }
            else
            {
                FaceTarget(unit, state.Target);
                if (!state.HoldingAtSlot || controller.Mission != OfficerAIMission.Hold)
                {
                    unit.OrderHold();
                    controller.SetHoldMission();
                    state.HoldingAtSlot = true;
                    Debug.Log("COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                              "|AtSlot=True|Role=" + (state.Support ? "SUPPORT" : "ASSAULT") +
                              "|Target=" + state.Target.RegimentName);
                }
            }
        }

        if (remove != null)
            for (int i = 0; i < remove.Count; i++)
                states.Remove(remove[i]);
    }

    private void PlanCoordinatedGroups()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Dictionary<Regiment, List<Regiment>> groups =
            new Dictionary<Regiment, List<Regiment>>();

        // Existing states remain part of their engagement even while their local
        // OfficerAI mission is HOLD at an assigned firing slot.
        foreach (KeyValuePair<Regiment, AttackState> pair in states)
        {
            Regiment unit = pair.Key;
            AttackState state = pair.Value;
            if (!IsValidUnit(unit) || state == null || !IsValidEnemy(unit, state.Target))
                continue;
            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled)
                continue;
            AddToGroup(groups, state.Target, unit);
        }

        // Fresh explicitly-designated attacks join coordination. AttackNearest remains
        // under the F29B sticky-target planner until it becomes an explicit AttackTarget.
        foreach (Regiment unit in battle.Regiments)
        {
            if (!IsValidUnit(unit) || states.ContainsKey(unit))
                continue;

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled || !controller.enabled ||
                controller.Mission != OfficerAIMission.AttackTarget ||
                PrototypeUnderFireReaction09F26.IsReacting(unit))
            {
                continue;
            }

            Regiment target = ReadMissionTarget(controller);
            if (!IsValidEnemy(unit, target))
                continue;
            if (PlanarDistance(unit.transform.position, target.transform.position) > CoordinationRange)
                continue;

            AddToGroup(groups, target, unit);
        }

        foreach (KeyValuePair<Regiment, List<Regiment>> pair in groups)
        {
            Regiment target = pair.Key;
            List<Regiment> units = pair.Value;
            RemoveDuplicates(units);

            if (units.Count < 2)
            {
                ReleaseSingleCompanyGroup(target, units);
                continue;
            }

            PlanGroup(target, units);
        }
    }

    private void PlanGroup(Regiment target, List<Regiment> units)
    {
        if (target == null || units == null || units.Count < 2)
            return;

        Vector3 average = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (!IsValidUnit(units[i]))
                continue;
            average += units[i].transform.position;
            validCount++;
        }
        if (validCount == 0)
            return;
        average /= validCount;

        Vector3 radial = Flat(average - target.transform.position);
        if (radial.sqrMagnitude < 0.01f)
            radial = Flat(-target.transform.forward);
        Vector3 lateral = Vector3.Cross(Vector3.up, radial).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        // Preserve current left/right ordering to reduce crossing while deploying.
        units.Sort((a, b) =>
        {
            float pa = Vector3.Dot(a.transform.position - target.transform.position, lateral);
            float pb = Vector3.Dot(b.transform.position - target.transform.position, lateral);
            return pa.CompareTo(pb);
        });

        int frontCount = Mathf.Min(4, units.Count);
        float halfArc = frontCount == 2 ? 30f : (frontCount == 3 ? 60f : 75f);
        float commonRadius = GetCommonAttackRadius(units, frontCount);

        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (!IsValidUnit(unit))
                continue;

            bool support = i >= frontCount;
            Vector3 requested;

            if (!support)
            {
                float angle = frontCount <= 1
                    ? 0f
                    : Mathf.Lerp(-halfArc, halfArc, i / (float)(frontCount - 1));
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * radial;
                requested = target.transform.position + Flat(direction) * commonRadius;
            }
            else
            {
                int supportIndex = i - frontCount;
                float center = (units.Count - frontCount - 1) * 0.5f;
                requested = target.transform.position + radial * (commonRadius + SupportDepth) +
                            lateral * ((supportIndex - center) * SupportSpacing);
            }

            requested = ClampToBattlefield(requested);
            Vector3 slot = requested;
            if (PrototypeBattlefieldNavigationManager.TryFindNearestValidDestination(
                    requested,
                    RegimentFormation.Line,
                    out Vector3 safe))
            {
                slot = safe;
            }
            slot.y = PrototypeBootstrap.SampleGroundHeight(slot.x, slot.z) + 0.10f;

            bool needsIssue = true;
            if (states.TryGetValue(unit, out AttackState existing) && existing != null &&
                existing.Target == target)
            {
                bool targetMoved = PlanarDistance(existing.TargetPositionAtPlan, target.transform.position) >= TargetReplanDistance;
                bool slotChanged = PlanarDistance(existing.Slot, slot) >= SlotReissueDistance;
                needsIssue = targetMoved || slotChanged;
            }

            AttackState state = existing;
            if (state == null)
                state = new AttackState();
            state.Unit = unit;
            state.Target = target;
            state.Slot = slot;
            state.TargetPositionAtPlan = target.transform.position;
            state.Support = support;
            state.SlotIndex = i;
            states[unit] = state;

            if (!needsIssue && state.HoldingAtSlot)
            {
                FaceTarget(unit, target);
                continue;
            }

            if (PrototypeUnderFireReaction09F26.IsReacting(unit))
                continue;

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled || !controller.enabled)
                continue;

            float distanceToSlot = PlanarDistance(unit.transform.position, slot);
            if (distanceToSlot <= SlotArrival)
            {
                unit.OrderHold();
                FaceTarget(unit, target);
                controller.SetHoldMission();
                state.HoldingAtSlot = true;
            }
            else
            {
                controller.SetMoveMission(slot);
                unit.OrderMove(slot);
                state.HoldingAtSlot = false;
            }

            Debug.Log(
                "COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                "|Target=" + target.RegimentName +
                "|Role=" + (support ? "SUPPORT" : "ASSAULT") +
                "|Slot=" + i +
                "|Pos=" + slot.x.ToString("0.0") + "," + slot.z.ToString("0.0") +
                "|MovementOwner=OfficerAIController|Replan=" + needsIssue);
        }
    }

    private void ReleaseSingleCompanyGroup(Regiment target, List<Regiment> units)
    {
        if (units == null || units.Count != 1)
            return;

        Regiment unit = units[0];
        if (!states.TryGetValue(unit, out AttackState state) || state == null)
            return;

        OfficerAIController controller = unit.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled && controller.enabled &&
            IsValidEnemy(unit, target) && !PrototypeUnderFireReaction09F26.IsReacting(unit))
        {
            controller.SetAttackMission(target);
        }

        states.Remove(unit);
        Debug.Log("COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                  "|Released=True|Reason=SINGLE_ATTACKER_REMAINS");
    }

    private float GetCommonAttackRadius(List<Regiment> units, int count)
    {
        float total = 0f;
        int used = 0;
        for (int i = 0; i < count && i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null)
                continue;
            float trigger = unit.GetFireTriggerRange();
            if (trigger <= 0.1f)
                trigger = unit.EffectiveRange;
            float radius = Mathf.Clamp(
                trigger * 0.88f,
                Mathf.Max(24f, unit.CloseRange * 1.15f),
                Mathf.Max(30f, unit.MaximumRange * 0.92f));
            total += radius;
            used++;
        }
        return used > 0 ? total / used : 42f;
    }

    private Regiment ReadMissionTarget(OfficerAIController controller)
    {
        return controller != null && missionTargetField != null
            ? missionTargetField.GetValue(controller) as Regiment
            : null;
    }

    private Vector3 ReadMissionPoint(OfficerAIController controller)
    {
        if (controller == null || missionPointField == null)
            return Vector3.zero;
        object value = missionPointField.GetValue(controller);
        return value is Vector3 ? (Vector3)value : Vector3.zero;
    }

    private static void AddToGroup(
        Dictionary<Regiment, List<Regiment>> groups,
        Regiment target,
        Regiment unit)
    {
        if (target == null || unit == null)
            return;
        if (!groups.TryGetValue(target, out List<Regiment> list))
        {
            list = new List<Regiment>();
            groups[target] = list;
        }
        list.Add(unit);
    }

    private static void RemoveDuplicates(List<Regiment> units)
    {
        if (units == null || units.Count < 2)
            return;
        HashSet<Regiment> seen = new HashSet<Regiment>();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            Regiment unit = units[i];
            if (unit == null || !seen.Add(unit))
                units.RemoveAt(i);
        }
    }

    private static bool IsValidUnit(Regiment unit)
    {
        return unit != null && !unit.IsRouted && unit.CurrentStrength > 0;
    }

    private static bool IsValidEnemy(Regiment unit, Regiment target)
    {
        return IsValidUnit(unit) && target != null && target != unit &&
               target.Team != unit.Team && !target.IsRouted && target.CurrentStrength > 0;
    }

    private static void FaceTarget(Regiment unit, Regiment target)
    {
        if (unit == null || target == null)
            return;
        Vector3 direction = target.transform.position - unit.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;
        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, desired, 5f * Time.deltaTime);
    }

    private static Vector3 ClampToBattlefield(Vector3 point)
    {
        float xLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfWidth - 30f);
        float zLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfDepth - 30f);
        point.x = Mathf.Clamp(point.x, -xLimit, xLimit);
        point.z = Mathf.Clamp(point.z, -zLimit, zLimit);
        point.y = 0f;
        return point;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void Add(ref List<Regiment> list, Regiment unit)
    {
        if (list == null)
            list = new List<Regiment>();
        list.Add(unit);
    }
}
