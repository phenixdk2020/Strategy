using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29e
// Coordinated multi-company attack execution for explicit AttackTarget missions.
// Keeps OfficerAIController as the local physical movement owner, yields to Major/higher
// authority when controller.enabled=false, and yields to F26 under-fire reaction.
// Companies attacking the same target receive distinct legal firing/deployment slots
// instead of independently converging on the target centre.
[DefaultExecutionOrder(1650)]
public sealed class PrototypeCoordinatedAttack09F29E : MonoBehaviour
{
    private sealed class AttackState
    {
        public Regiment Unit;
        public Regiment Target;
        public Vector3 Slot;
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
            "COORD-ATTACK-09F29E|Installed=True|MovementOwner=OfficerAIController|" +
            "ExplicitTargetOnly=True|MaxFrontCompanies=4|DynamicBattlefieldSlots=True|" +
            "FormationSafety=F23|UnderFireAuthorityRespected=True|HigherCommandAuthorityRespected=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        MaintainStates();

        if (Time.time < nextPlan)
            return;

        nextPlan = Time.time + PlanInterval;
        PlanGroups();
    }

    private void MaintainStates()
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

            if (PrototypeUnderFireReaction09F26.IsReacting(unit))
                continue;

            // F27 deliberately disables the local controller while a Major owns physical
            // placement. Never compete with that higher authority.
            if (!controller.enabled)
            {
                Debug.Log("COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                          "|Released=True|Reason=HIGHER_COMMAND_MOVEMENT_AUTHORITY");
                Add(ref remove, unit);
                continue;
            }

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
                Add(ref remove, unit);
                continue;
            }

            float distance = PlanarDistance(unit.transform.position, state.Slot);
            if (distance > SlotArrival)
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

    private void PlanGroups()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Dictionary<Regiment, List<Regiment>> groups =
            new Dictionary<Regiment, List<Regiment>>();

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
            List<Regiment> units = pair.Value;
            RemoveDuplicates(units);

            if (units.Count < 2)
            {
                ReleaseSingleCompanyGroup(pair.Key, units);
                continue;
            }

            PlanGroup(pair.Key, units);
        }
    }

    private void PlanGroup(Regiment target, List<Regiment> units)
    {
        if (target == null || units == null || units.Count < 2)
            return;

        Vector3 average = Vector3.zero;
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (!IsValidUnit(units[i]))
                continue;
            average += units[i].transform.position;
            count++;
        }
        if (count == 0)
            return;
        average /= count;

        Vector3 radial = Flat(average - target.transform.position);
        if (radial.sqrMagnitude < 0.01f)
            radial = Flat(-target.transform.forward);
        Vector3 lateral = Vector3.Cross(Vector3.up, radial).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        // Keep existing left/right order as much as possible so the attack unfolds
        // around the target instead of companies crossing through one another.
        units.Sort((a, b) =>
        {
            float pa = Vector3.Dot(a.transform.position - target.transform.position, lateral);
            float pb = Vector3.Dot(b.transform.position - target.transform.position, lateral);
            return pa.CompareTo(pb);
        });

        int frontCount = Mathf.Min(4, units.Count);
        float halfArc = frontCount == 2 ? 30f : (frontCount == 3 ? 60f : 75f);
        float radius = GetCommonAttackRadius(units, frontCount);
        List<Vector3> reserved = new List<Vector3>();

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
                requested = target.transform.position + Flat(direction) * radius;
            }
            else
            {
                int supportIndex = i - frontCount;
                float supportCenter = (units.Count - frontCount - 1) * 0.5f;
                requested = target.transform.position + radial * (radius + SupportDepth) +
                            lateral * ((supportIndex - supportCenter) * SupportSpacing);
            }

            requested = ClampToBattlefield(requested);
            Vector3 facing = Flat(target.transform.position - requested);
            Vector3 slotLine = Vector3.Cross(Vector3.up, facing).normalized;
            if (slotLine.sqrMagnitude < 0.01f)
                slotLine = lateral;

            Vector3 legal = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
                requested,
                facing,
                slotLine,
                reserved,
                out bool movedForSafety,
                out bool depthFallback);

            bool needsIssue = true;
            AttackState state = null;
            if (states.TryGetValue(unit, out AttackState existing) && existing != null &&
                existing.Target == target)
            {
                state = existing;
                if (PlanarDistance(existing.Slot, legal) < SlotReissueDistance)
                {
                    legal = existing.Slot;
                    needsIssue = false;
                }
            }

            if (state == null)
                state = new AttackState();

            state.Unit = unit;
            state.Target = target;
            state.Slot = legal;
            state.Support = support;
            state.SlotIndex = i;
            states[unit] = state;
            reserved.Add(legal);

            if (PrototypeUnderFireReaction09F26.IsReacting(unit))
                continue;

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled || !controller.enabled)
                continue;

            float distance = PlanarDistance(unit.transform.position, legal);
            if (distance <= SlotArrival)
            {
                unit.OrderHold();
                FaceTarget(unit, target);
                controller.SetHoldMission();
                state.HoldingAtSlot = true;
            }
            else if (needsIssue || controller.Mission != OfficerAIMission.MoveToPoint ||
                     PlanarDistance(ReadMissionPoint(controller), legal) > 1.5f)
            {
                controller.SetMoveMission(legal);
                unit.OrderMove(legal);
                state.HoldingAtSlot = false;
            }

            if (needsIssue)
            {
                Debug.Log(
                    "COORD-ATTACK-09F29E|Unit=" + unit.RegimentName +
                    "|Target=" + target.RegimentName +
                    "|Role=" + (support ? "SUPPORT" : "ASSAULT") +
                    "|Slot=" + i +
                    "|Pos=" + legal.x.ToString("0.0") + "," + legal.z.ToString("0.0") +
                    "|SafetyMoved=" + movedForSafety +
                    "|DepthFallback=" + depthFallback +
                    "|MovementOwner=OfficerAIController");
            }
        }
    }

    private void ReleaseSingleCompanyGroup(Regiment target, List<Regiment> units)
    {
        if (units == null || units.Count != 1)
            return;

        Regiment unit = units[0];
        if (!states.ContainsKey(unit))
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

    private static float GetCommonAttackRadius(List<Regiment> units, int count)
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

            total += Mathf.Clamp(
                trigger * 0.88f,
                Mathf.Max(24f, unit.CloseRange * 1.15f),
                Mathf.Max(30f, unit.MaximumRange * 0.92f));
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
        unit.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
