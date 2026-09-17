using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30a
// Higher-command tactical hardening.
// - captures explicit player facing before F29G commits the order;
// - DEFEND HERE keeps the clicked objective authoritative and builds frontage perpendicular
//   to explicit facing, or toward the nearest relevant enemy when no arrow was dragged;
// - ATTACK HERE allocates stronger companies (men + experience) to assault/front roles and
//   uses the weakest suitable company as a pure reserve when a reserve role exists;
// - this layer only corrects a newly committed mission plan. F27 remains the recurring mover.

[DefaultExecutionOrder(850)]
public sealed class PrototypeOfficerIntentCapture09F30A : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DragFacingThreshold = 5f;

    private FieldInfo levelField;
    private FieldInfo battalionIndexField;
    private FieldInfo orderField;
    private FieldInfo anchorPlacedField;
    private FieldInfo anchorField;
    private FieldInfo dragPointField;

    public static int LastCommitFrame { get; private set; } = -1;
    public static bool LastWasRegimental { get; private set; }
    public static int LastBattalionIndex { get; private set; } = -1;
    public static MajorOrder09F18 LastOrder { get; private set; } = MajorOrder09F18.None;
    public static Vector3 LastObjective { get; private set; }
    public static bool LastHadExplicitFacing { get; private set; }
    public static Vector3 LastFacing { get; private set; } = Vector3.forward;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeOfficerIntentCapture09F30A>() == null)
            new GameObject("PrototypeOfficerIntentCapture_v000009f30a")
                .AddComponent<PrototypeOfficerIntentCapture09F30A>();
    }

    private void Awake()
    {
        Type type = typeof(PrototypeOfficerFacingOrder09F29G);
        levelField = type.GetField("level", PrivateInstance);
        battalionIndexField = type.GetField("battalionIndex", PrivateInstance);
        orderField = type.GetField("order", PrivateInstance);
        anchorPlacedField = type.GetField("anchorPlaced", PrivateInstance);
        anchorField = type.GetField("anchor", PrivateInstance);
        dragPointField = type.GetField("dragPoint", PrivateInstance);
    }

    private void Update()
    {
        PrototypeOfficerFacingOrder09F29G input = PrototypeOfficerFacingOrder09F29G.Instance;
        if (input == null || !input.HasPendingOrder || !Input.GetMouseButtonUp(0))
            return;

        if (!ReadBool(anchorPlacedField, input) || orderField == null || anchorField == null || dragPointField == null)
            return;

        object rawOrder = orderField.GetValue(input);
        if (!(rawOrder is MajorOrder09F18))
            return;

        MajorOrder09F18 order = (MajorOrder09F18)rawOrder;
        if (order == MajorOrder09F18.None)
            return;

        Vector3 objective = (Vector3)anchorField.GetValue(input);
        Vector3 drag = (Vector3)dragPointField.GetValue(input);
        Vector3 facing = drag - objective;
        facing.y = 0f;
        bool explicitFacing = facing.magnitude >= DragFacingThreshold;
        if (explicitFacing)
            facing.Normalize();

        object rawLevel = levelField != null ? levelField.GetValue(input) : null;
        string levelName = rawLevel != null ? rawLevel.ToString() : string.Empty;

        LastCommitFrame = Time.frameCount;
        LastWasRegimental = string.Equals(levelName, "Regiment", StringComparison.Ordinal);
        LastBattalionIndex = ReadInt(battalionIndexField, input, -1);
        LastOrder = order;
        LastObjective = objective;
        LastHadExplicitFacing = explicitFacing;
        LastFacing = explicitFacing ? facing : Vector3.forward;

        Debug.Log("OFFICER-INTENT-09F30A|Level=" + levelName +
                  "|Order=" + order +
                  "|Objective=" + objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
                  "|Facing=" + (explicitFacing ? "EXPLICIT" : "AUTO") +
                  (explicitFacing ? "|Dir=" + facing.x.ToString("0.00") + "," + facing.z.ToString("0.00") : string.Empty));
    }

    private static bool ReadBool(FieldInfo field, object target)
    {
        if (field == null || target == null)
            return false;
        object value = field.GetValue(target);
        return value is bool && (bool)value;
    }

    private static int ReadInt(FieldInfo field, object target, int fallback)
    {
        if (field == null || target == null)
            return fallback;
        object value = field.GetValue(target);
        return value is int ? (int)value : fallback;
    }
}

[DefaultExecutionOrder(980)]
public sealed class PrototypeOfficerTacticalDoctrine09F30A : MonoBehaviour
{
    private sealed class MissionRole
    {
        public Vector3 Goal;
        public Vector3 Facing;
        public bool Reserve;
        public bool Flank;
        public float Importance;
    }

    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float CompanySpacing = 60f;
    private const float ReserveDepth = 86f;
    private const float RegimentBattalionLateral = 170f;
    private const float AutoFacingEnemyRange = 2200f;

    private FieldInfo battalionsField;
    private MethodInfo setMissionVisualMethod;
    private readonly Dictionary<object, object> lastAttackSignature = new Dictionary<object, object>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeOfficerTacticalDoctrine09F30A>() == null)
            new GameObject("PrototypeOfficerTacticalDoctrine_v000009f30a")
                .AddComponent<PrototypeOfficerTacticalDoctrine09F30A>();
    }

    private void Awake()
    {
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);
        setMissionVisualMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SetMissionVisual", AnyInstance);

        Debug.Log("OFFICER-TACTICS-09F30A|Installed=True|DefendObjectiveAuthoritative=True|" +
                  "AutoFacing=NearestThreatThenCurrentFront|AttackAllocation=MenPlusExperience");
    }

    private void Update()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        // F29G commits at execution order 900. This runs after it and corrects only the
        // newly-created plan, before the next recurring F27 movement update.
        if (PrototypeOfficerIntentCapture09F30A.LastCommitFrame == Time.frameCount &&
            PrototypeOfficerIntentCapture09F30A.LastOrder == MajorOrder09F18.DefendHere)
        {
            Vector3 objective = PrototypeOfficerIntentCapture09F30A.LastObjective;
            Vector3 facing = PrototypeOfficerIntentCapture09F30A.LastHadExplicitFacing
                ? Flat(PrototypeOfficerIntentCapture09F30A.LastFacing)
                : ResolveAutoFacing(objective, hierarchy,
                    PrototypeOfficerIntentCapture09F30A.LastWasRegimental
                        ? -1
                        : PrototypeOfficerIntentCapture09F30A.LastBattalionIndex);

            if (PrototypeOfficerIntentCapture09F30A.LastWasRegimental)
                ReframeRegimentalDefend(hierarchy, battalions, objective, facing);
            else
                ReframeBattalionDefend(hierarchy, battalions,
                    PrototypeOfficerIntentCapture09F30A.LastBattalionIndex, objective, facing);
        }

        int count = Mathf.Min(hierarchy.BattalionCount, battalions.Count);
        for (int i = 0; i < count; i++)
            ProcessAttackAllocation(hierarchy, battalions, i);
    }

    private void ReframeRegimentalDefend(
        PrototypeRegimentHierarchy09F27 hierarchy,
        IList battalions,
        Vector3 objective,
        Vector3 facing)
    {
        if (hierarchy.BattalionCount < 2 || battalions.Count < 2)
            return;

        Vector3 lateral = Vector3.Cross(Vector3.up, facing).normalized;
        if (lateral.sqrMagnitude < 0.001f)
            lateral = Vector3.right;

        Vector3 slotA = Ground(objective - lateral * RegimentBattalionLateral);
        Vector3 slotB = Ground(objective + lateral * RegimentBattalionLateral);

        Vector3 center0 = hierarchy.GetBattalionCenter(0);
        Vector3 center1 = hierarchy.GetBattalionCenter(1);
        float normal = PlanarDistance(center0, slotA) + PlanarDistance(center1, slotB);
        float swapped = PlanarDistance(center0, slotB) + PlanarDistance(center1, slotA);

        Vector3 goal0 = normal <= swapped ? slotA : slotB;
        Vector3 goal1 = normal <= swapped ? slotB : slotA;

        ReframeBattalionDefend(hierarchy, battalions, 0, goal0, facing);
        ReframeBattalionDefend(hierarchy, battalions, 1, goal1, facing);

        Debug.Log("DEFEND-FACING-09F30A|Level=REGIMENT|Objective=" +
                  objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
                  "|Facing=" + facing.x.ToString("0.00") + "," + facing.z.ToString("0.00") +
                  "|BattalionsReframed=True");
    }

    private void ReframeBattalionDefend(
        PrototypeRegimentHierarchy09F27 hierarchy,
        IList battalions,
        int battalionIndex,
        Vector3 objective,
        Vector3 facing)
    {
        if (battalionIndex < 0 || battalionIndex >= battalions.Count)
            return;

        object battalion = battalions[battalionIndex];
        if (battalion == null)
            return;

        Type battalionType = battalion.GetType();
        FieldInfo missionsField = battalionType.GetField("Missions", AnyInstance);
        IDictionary missions = missionsField != null ? missionsField.GetValue(battalion) as IDictionary : null;
        if (missions == null || missions.Count == 0)
            return;

        FieldInfo lastOrderPointField = battalionType.GetField("LastOrderPoint", AnyInstance);
        FieldInfo lastDecisionField = battalionType.GetField("LastDecisionText", AnyInstance);
        if (lastOrderPointField != null)
            lastOrderPointField.SetValue(battalion, objective);

        Vector3 lateral = Vector3.Cross(Vector3.up, facing).normalized;
        if (lateral.sqrMagnitude < 0.001f)
            lateral = Vector3.right;

        List<DictionaryEntry> front = new List<DictionaryEntry>();
        List<DictionaryEntry> reserve = new List<DictionaryEntry>();

        foreach (DictionaryEntry entry in missions)
        {
            Regiment unit = entry.Key as Regiment;
            object mission = entry.Value;
            if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;

            Type mt = mission.GetType();
            bool isReserve = ReadBool(mt.GetField("Reserve", AnyInstance), mission);
            bool isFlank = ReadBool(mt.GetField("Flank", AnyInstance), mission);
            if (isReserve || isFlank)
                reserve.Add(entry);
            else
                front.Add(entry);
        }

        front.Sort((a, b) =>
        {
            Regiment ua = a.Key as Regiment;
            Regiment ub = b.Key as Regiment;
            float pa = ua != null ? Vector3.Dot(ua.transform.position, lateral) : 0f;
            float pb = ub != null ? Vector3.Dot(ub.transform.position, lateral) : 0f;
            return pa.CompareTo(pb);
        });

        List<Vector3> reservedGoals = new List<Vector3>();
        float middle = (front.Count - 1) * 0.5f;

        for (int i = 0; i < front.Count; i++)
        {
            Vector3 desired = objective + lateral * ((i - middle) * CompanySpacing);
            Vector3 legal = ResolveLegalSlot(desired, facing, lateral, reservedGoals);
            reservedGoals.Add(legal);
            ApplyMission(hierarchy, battalionIndex, front[i], legal, facing, false, false);
        }

        for (int i = 0; i < reserve.Count; i++)
        {
            Vector3 desired = objective - facing * ReserveDepth + lateral * ((i - (reserve.Count - 1) * 0.5f) * CompanySpacing);
            Vector3 legal = ResolveLegalSlot(desired, facing, lateral, reservedGoals);
            reservedGoals.Add(legal);
            ApplyMission(hierarchy, battalionIndex, reserve[i], legal, facing, true, false);
        }

        if (lastDecisionField != null)
            lastDecisionField.SetValue(battalion,
                "F30A: forsvarsfront følger ordrepunkt + facing; reserve bag front");

        Debug.Log("DEFEND-FACING-09F30A|Level=BATTALION|Battalion=" + (battalionIndex + 1) +
                  "|Objective=" + objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
                  "|Facing=" + facing.x.ToString("0.00") + "," + facing.z.ToString("0.00") +
                  "|Front=" + front.Count + "|Reserve=" + reserve.Count);
    }

    private void ApplyMission(
        PrototypeRegimentHierarchy09F27 hierarchy,
        int battalionIndex,
        DictionaryEntry entry,
        Vector3 goal,
        Vector3 facing,
        bool reserve,
        bool flank)
    {
        Regiment unit = entry.Key as Regiment;
        object mission = entry.Value;
        if (unit == null || mission == null)
            return;

        Type mt = mission.GetType();
        SetField(mt.GetField("Goal", AnyInstance), mission, Ground(goal));
        SetField(mt.GetField("Facing", AnyInstance), mission, Flat(facing));
        SetField(mt.GetField("Reserve", AnyInstance), mission, reserve);
        SetField(mt.GetField("Flank", AnyInstance), mission, flank);
        SetField(mt.GetField("Arrived", AnyInstance), mission, false);
        SetField(mt.GetField("NextAssert", AnyInstance), mission, 0f);

        // Correct the stale one-frame destination created by F27 at commit time.
        unit.OrderMove(Ground(goal));

        if (setMissionVisualMethod != null)
            setMissionVisualMethod.Invoke(hierarchy, new object[] { unit, mission, battalionIndex });
    }

    private void ProcessAttackAllocation(
        PrototypeRegimentHierarchy09F27 hierarchy,
        IList battalions,
        int battalionIndex)
    {
        object battalion = battalions[battalionIndex];
        if (battalion == null)
            return;

        Type bt = battalion.GetType();
        FieldInfo lastOrderField = bt.GetField("LastOrder", AnyInstance);
        FieldInfo missionsField = bt.GetField("Missions", AnyInstance);
        FieldInfo lastOrderPointField = bt.GetField("LastOrderPoint", AnyInstance);
        FieldInfo lastDecisionField = bt.GetField("LastDecisionText", AnyInstance);

        if (lastOrderField == null || missionsField == null)
            return;

        object orderValue = lastOrderField.GetValue(battalion);
        if (!(orderValue is MajorOrder09F18) || (MajorOrder09F18)orderValue != MajorOrder09F18.AttackHere)
        {
            lastAttackSignature.Remove(battalion);
            return;
        }

        IDictionary missions = missionsField.GetValue(battalion) as IDictionary;
        if (missions == null || missions.Count < 2)
            return;

        object signature = null;
        foreach (DictionaryEntry entry in missions)
        {
            if (entry.Value != null)
            {
                signature = entry.Value;
                break;
            }
        }
        if (signature == null)
            return;

        object previous;
        if (lastAttackSignature.TryGetValue(battalion, out previous) && object.ReferenceEquals(previous, signature))
            return;
        lastAttackSignature[battalion] = signature;

        Vector3 objective = lastOrderPointField != null && lastOrderPointField.GetValue(battalion) is Vector3
            ? (Vector3)lastOrderPointField.GetValue(battalion)
            : hierarchy.GetBattalionCenter(battalionIndex);

        List<Regiment> units = new List<Regiment>();
        List<MissionRole> roles = new List<MissionRole>();
        Dictionary<Regiment, object> unitMission = new Dictionary<Regiment, object>();

        foreach (DictionaryEntry entry in missions)
        {
            Regiment unit = entry.Key as Regiment;
            object mission = entry.Value;
            if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;

            Type mt = mission.GetType();
            FieldInfo goalField = mt.GetField("Goal", AnyInstance);
            FieldInfo facingField = mt.GetField("Facing", AnyInstance);
            if (goalField == null || facingField == null)
                continue;

            Vector3 goal = (Vector3)goalField.GetValue(mission);
            bool reserve = ReadBool(mt.GetField("Reserve", AnyInstance), mission);
            bool flank = ReadBool(mt.GetField("Flank", AnyInstance), mission);

            float importance;
            if (reserve && !flank)
                importance = -1000f;
            else if (flank)
                importance = 500f;
            else
                importance = 1000f - PlanarDistance(goal, objective) * 0.05f;

            units.Add(unit);
            unitMission[unit] = mission;
            roles.Add(new MissionRole
            {
                Goal = goal,
                Facing = (Vector3)facingField.GetValue(mission),
                Reserve = reserve,
                Flank = flank,
                Importance = importance
            });
        }

        if (units.Count < 2 || roles.Count != units.Count)
            return;

        units.Sort((a, b) => AssaultPower(b).CompareTo(AssaultPower(a)));
        roles.Sort((a, b) => b.Importance.CompareTo(a.Importance));

        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            MissionRole role = roles[i];
            object mission = unitMission[unit];
            Type mt = mission.GetType();

            SetField(mt.GetField("Goal", AnyInstance), mission, role.Goal);
            SetField(mt.GetField("Facing", AnyInstance), mission, Flat(role.Facing));
            SetField(mt.GetField("Reserve", AnyInstance), mission, role.Reserve);
            SetField(mt.GetField("Flank", AnyInstance), mission, role.Flank);
            SetField(mt.GetField("Arrived", AnyInstance), mission, false);
            SetField(mt.GetField("NextAssert", AnyInstance), mission, 0f);

            unit.OrderMove(role.Goal);
            if (setMissionVisualMethod != null)
                setMissionVisualMethod.Invoke(hierarchy, new object[] { unit, mission, battalionIndex });

            Debug.Log("ASSAULT-ALLOC-09F30A|Battalion=" + (battalionIndex + 1) +
                      "|Rank=" + (i + 1) + "|Unit=" + unit.RegimentName +
                      "|Men=" + unit.CurrentStrength + "|Exp=" + unit.Experience.ToString("0") +
                      "|Power=" + AssaultPower(unit).ToString("0.0") +
                      "|Role=" + (role.Flank ? "FLANK" : role.Reserve ? "RESERVE" : "ASSAULT"));
        }

        if (lastDecisionField != null)
            lastDecisionField.SetValue(battalion,
                "F30A assault: stærkeste kompagnier i front/flanke; svageste egnet reserve");
    }

    private Vector3 ResolveAutoFacing(
        Vector3 objective,
        PrototypeRegimentHierarchy09F27 hierarchy,
        int battalionIndex)
    {
        Regiment threat = FindNearestEnemy(objective, AutoFacingEnemyRange);
        if (threat != null)
            return Flat(threat.transform.position - objective);

        Vector3 average = Vector3.zero;
        int count = 0;

        if (battalionIndex >= 0 && battalionIndex < hierarchy.BattalionCount)
        {
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
            AddCurrentFacing(companies, ref average, ref count);
        }
        else
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
                AddCurrentFacing(hierarchy.GetCompanies(i), ref average, ref count);
        }

        return count > 0 && average.sqrMagnitude > 0.001f ? Flat(average) : Vector3.forward;
    }

    private static void AddCurrentFacing(IReadOnlyList<Regiment> units, ref Vector3 total, ref int count)
    {
        if (units == null)
            return;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;
            Vector3 f = unit.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.001f)
                continue;
            total += f.normalized;
            count++;
        }
    }

    private static Regiment FindNearestEnemy(Vector3 point, float maxRange)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = maxRange;
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.Team == BattleTeam.Denmark || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;
            float d = PlanarDistance(point, unit.transform.position);
            if (d < best)
            {
                best = d;
                nearest = unit;
            }
        }
        return nearest;
    }

    private static Vector3 ResolveLegalSlot(
        Vector3 desired,
        Vector3 forward,
        Vector3 lateral,
        List<Vector3> reserved)
    {
        Vector3 legal = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
            Ground(desired), Flat(forward), lateral, reserved, out bool moved, out bool depthFallback);
        return Ground(legal);
    }

    private static float AssaultPower(Regiment unit)
    {
        if (unit == null || unit.CurrentStrength <= 0)
            return 0f;
        float experienceFactor = 0.60f + 0.40f * Mathf.Clamp01(unit.Experience / 100f);
        return unit.CurrentStrength * experienceFactor;
    }

    private static bool ReadBool(FieldInfo field, object target)
    {
        if (field == null || target == null)
            return false;
        object value = field.GetValue(target);
        return value is bool && (bool)value;
    }

    private static void SetField(FieldInfo field, object target, object value)
    {
        if (field != null && target != null)
            field.SetValue(target, value);
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
