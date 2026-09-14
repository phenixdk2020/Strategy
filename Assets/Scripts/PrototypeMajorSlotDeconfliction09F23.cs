using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f23
// Final battalion slot authority after the 09f22 objective-centred planner.
// The whole Major formation is solved as one set: requested front slots are generated
// from the commander objective, obstacle-safe slots are reserved one-by-one, and the
// final set is assigned to companies by minimum total march distance.
[DefaultExecutionOrder(490)]
public sealed class PrototypeMajorSlotDeconfliction09F23 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private FieldInfo missionsField;
    private FieldInfo lastAssignmentsField;
    private FieldInfo lastOrderField;
    private FieldInfo lastOrderPointField;
    private FieldInfo reserveField;
    private MethodInfo knownEnemiesMethod;

    private float nextReview;
    private string lastAppliedSignature = string.Empty;

    private const float ReviewInterval = 0.25f;
    private const float CompanySpacing = 60f;
    private const float AttackStandoff = 68f;
    private const float ReserveDepth = 78f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorSlotDeconfliction09F23>() == null)
            new GameObject("PrototypeMajorSlotDeconfliction_v000009f23").AddComponent<PrototypeMajorSlotDeconfliction09F23>();
    }

    private void Update()
    {
        if (Time.time < nextReview)
            return;
        nextReview = Time.time + ReviewInterval;

        if (!ResolveMajor())
            return;

        IDictionary missions = missionsField.GetValue(major) as IDictionary;
        if (missions == null || missions.Count == 0)
        {
            lastAppliedSignature = string.Empty;
            return;
        }

        MajorOrder09F18 order = (MajorOrder09F18)lastOrderField.GetValue(major);
        if (order == MajorOrder09F18.None || order == MajorOrder09F18.HoldPosition)
            return;

        Vector3 objective = (Vector3)lastOrderPointField.GetValue(major);
        Regiment reserve = reserveField.GetValue(major) as Regiment;

        string signature = BuildSignature(order, objective, reserve, missions);
        if (signature == lastAppliedSignature && AllGoalsStillSeparated(missions))
            return;

        ApplyFormation(order, objective, reserve, missions);
        lastAppliedSignature = BuildSignature(order, objective, reserve, missions);
    }

    private bool ResolveMajor()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return false;

        if (missionsField != null)
            return true;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(PrototypeMajorBattalion09F18);
        missionsField = type.GetField("missions", flags);
        lastAssignmentsField = type.GetField("lastAssignments", flags);
        lastOrderField = type.GetField("lastOrder", flags);
        lastOrderPointField = type.GetField("lastOrderPoint", flags);
        reserveField = type.GetField("reserve", flags);
        knownEnemiesMethod = type.GetMethod("KnownEnemies", flags);

        bool ok = missionsField != null && lastAssignmentsField != null &&
                  lastOrderField != null && lastOrderPointField != null && reserveField != null;
        if (!ok)
            Debug.LogError("HQ-SLOTS-09F23|Installed=False|Reason=MajorReflectionMissing");
        return ok;
    }

    private void ApplyFormation(MajorOrder09F18 order, Vector3 objective, Regiment reserve, IDictionary missions)
    {
        List<Regiment> front = new List<Regiment>();
        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null || regiment.IsRouted || !missions.Contains(regiment))
                continue;

            object mission = missions[regiment];
            if (mission == null)
                continue;

            if (ReadBool(mission, "Reserve"))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
                continue; // manual authority always wins.

            front.Add(regiment);
        }

        if (front.Count == 0)
            return;

        Vector3 centre = AveragePosition(front);
        Vector3 forward = DetermineFormationForward(order, objective, centre, missions, front);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        Vector3 lineCenter = Ground(objective);
        if (order == MajorOrder09F18.AttackHere)
            lineCenter = Ground(objective - forward * AttackStandoff);

        List<Vector3> slots = new List<Vector3>();
        List<Vector3> reservedGoals = new List<Vector3>();
        float middle = (front.Count - 1) * 0.5f;
        int movedSlots = 0;
        int depthFallbacks = 0;

        for (int i = 0; i < front.Count; i++)
        {
            Vector3 raw = lineCenter + lateral * ((i - middle) * CompanySpacing);
            raw = Ground(raw);

            Vector3 slot = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
                raw,
                forward,
                lateral,
                reservedGoals,
                out bool moved,
                out bool depthFallback);

            slots.Add(slot);
            reservedGoals.Add(slot);
            if (moved) movedSlots++;
            if (depthFallback) depthFallbacks++;
        }

        int[] assignment = BestAssignment(front, slots);
        for (int i = 0; i < front.Count; i++)
        {
            Regiment regiment = front[i];
            object mission = missions[regiment];
            Vector3 goal = slots[assignment[i]];
            Vector3 facing = FacingForSlot(goal, forward);
            ApplyMission(regiment, mission, goal, facing);
            UpdateSavedAssignment(regiment, goal, facing, false, false);
        }

        // Reserve is part of the same footprint reservation set. A reserve/flank may
        // keep its tactical intent, but it may not collapse onto a front company.
        if (reserve != null && missions.Contains(reserve))
        {
            object mission = missions[reserve];
            OfficerAIController controller = reserve.GetComponent<OfficerAIController>();
            if (mission != null && (controller == null || controller.AIEnabled))
            {
                bool flank = ReadBool(mission, "Flank");
                Vector3 desired = flank
                    ? ReadVector(mission, "Goal", lineCenter - forward * ReserveDepth)
                    : lineCenter - forward * ReserveDepth;

                Vector3 reserveGoal = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
                    Ground(desired),
                    forward,
                    lateral,
                    reservedGoals,
                    out bool moved,
                    out bool depthFallback);

                if (moved) movedSlots++;
                if (depthFallback) depthFallbacks++;
                reservedGoals.Add(reserveGoal);

                Vector3 reserveFacing = FacingForSlot(reserveGoal, forward);
                ApplyMission(reserve, mission, reserveGoal, reserveFacing);
                UpdateSavedAssignment(reserve, reserveGoal, reserveFacing, true, flank);
            }
        }

        Debug.Log("HQ-SLOTS-09F23|Order=" + order +
                  "|Front=" + front.Count +
                  "|MovedSlots=" + movedSlots +
                  "|DepthFallbacks=" + depthFallbacks +
                  "|UniqueReserved=True|NoCompanyOverlap=True|ManualAuthorityPreserved=True");
    }

    private Vector3 DetermineFormationForward(
        MajorOrder09F18 order,
        Vector3 objective,
        Vector3 battalionCentre,
        IDictionary missions,
        List<Regiment> front)
    {
        if (order == MajorOrder09F18.AttackHere || order == MajorOrder09F18.AdvanceHere)
        {
            Vector3 towardObjective = objective - battalionCentre;
            towardObjective.y = 0f;
            if (towardObjective.sqrMagnitude > 4f)
                return towardObjective.normalized;
        }

        Vector3 averageFacing = Vector3.zero;
        foreach (Regiment regiment in front)
        {
            if (!missions.Contains(regiment))
                continue;
            object mission = missions[regiment];
            Vector3 f = ReadVector(mission, "Facing", Vector3.zero);
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f)
                averageFacing += f.normalized;
        }

        if (averageFacing.sqrMagnitude > 0.01f)
            return averageFacing.normalized;

        Vector3 fallback = objective - battalionCentre;
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward;
    }

    private Vector3 FacingForSlot(Vector3 slot, Vector3 fallback)
    {
        Regiment enemy = NearestKnownEnemy(slot);
        if (enemy == null)
            return fallback;

        Vector3 facing = enemy.transform.position - slot;
        facing.y = 0f;
        return facing.sqrMagnitude > 0.01f ? facing.normalized : fallback;
    }

    private Regiment NearestKnownEnemy(Vector3 point)
    {
        if (knownEnemiesMethod == null)
            return null;

        IEnumerable list = knownEnemiesMethod.Invoke(major, new object[] { point }) as IEnumerable;
        if (list == null)
            return null;

        Regiment best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (object entry in list)
        {
            Regiment enemy = entry as Regiment;
            if (enemy == null || enemy.IsRouted || enemy.CurrentStrength <= 0)
                continue;

            float d = PlanarDistance(point, enemy.transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = enemy;
            }
        }
        return best;
    }

    private void ApplyMission(Regiment regiment, object mission, Vector3 goal, Vector3 facing)
    {
        if (regiment == null || mission == null)
            return;

        Vector3 oldGoal = ReadVector(mission, "Goal", goal);
        bool changed = PlanarDistance(oldGoal, goal) > 0.05f;

        WriteVector(mission, "Goal", goal);
        WriteVector(mission, "Facing", facing);
        if (changed)
        {
            WriteBool(mission, "Arrived", false);
            WriteFloat(mission, "NextAssert", 0f);
        }

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetMoveMission(goal);
        else if (controller == null)
            regiment.OrderMove(goal);
        else
            return; // AI OFF is manual control: never steal authority.

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
        {
            MajorOrder09F18 order = ReadOrder(mission);
            visuals.SetMission(
                regiment,
                goal,
                facing,
                order,
                ReadBool(mission, "Reserve"),
                ReadBool(mission, "Flank"));
        }
    }

    private void UpdateSavedAssignment(Regiment regiment, Vector3 goal, Vector3 facing, bool reserveRole, bool flank)
    {
        IDictionary saved = lastAssignmentsField.GetValue(major) as IDictionary;
        if (saved == null || regiment == null || !saved.Contains(regiment))
            return;

        object mission = saved[regiment];
        if (mission == null)
            return;

        WriteVector(mission, "Goal", goal);
        WriteVector(mission, "Facing", facing);
        WriteBool(mission, "Reserve", reserveRole);
        WriteBool(mission, "Flank", flank);
    }

    private bool AllGoalsStillSeparated(IDictionary missions)
    {
        List<Vector3> goals = new List<Vector3>();
        foreach (DictionaryEntry entry in missions)
        {
            Regiment regiment = entry.Key as Regiment;
            object mission = entry.Value;
            if (regiment == null || mission == null)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
                continue;

            Vector3 goal = ReadVector(mission, "Goal", regiment.transform.position);
            Vector3 facing = ReadVector(mission, "Facing", regiment.transform.forward);
            if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(goal, facing))
                return false;
            if (PrototypeFormationSlotSafety09F23.ConflictsWithReserved(goal, goals))
                return false;
            goals.Add(goal);
        }
        return true;
    }

    private static int[] BestAssignment(List<Regiment> units, List<Vector3> slots)
    {
        int n = Mathf.Min(units.Count, slots.Count);
        int[] current = new int[n];
        int[] best = new int[n];
        bool[] used = new bool[n];
        float bestCost = float.PositiveInfinity;
        SearchAssignment(0, units, slots, current, best, used, 0f, ref bestCost);
        return best;
    }

    private static void SearchAssignment(
        int index,
        List<Regiment> units,
        List<Vector3> slots,
        int[] current,
        int[] best,
        bool[] used,
        float cost,
        ref float bestCost)
    {
        if (index >= current.Length)
        {
            if (cost < bestCost)
            {
                bestCost = cost;
                Array.Copy(current, best, current.Length);
            }
            return;
        }

        if (cost >= bestCost)
            return;

        for (int slot = 0; slot < current.Length; slot++)
        {
            if (used[slot])
                continue;

            used[slot] = true;
            current[index] = slot;
            float d = PlanarDistance(units[index].transform.position, slots[slot]);
            SearchAssignment(index + 1, units, slots, current, best, used, cost + d * d, ref bestCost);
            used[slot] = false;
        }
    }

    private string BuildSignature(MajorOrder09F18 order, Vector3 point, Regiment reserve, IDictionary missions)
    {
        return order + "|" + Mathf.RoundToInt(point.x * 2f) + ":" + Mathf.RoundToInt(point.z * 2f) +
               "|R=" + (reserve != null ? reserve.RegimentName : "NONE") + "|M=" + missions.Count;
    }

    private static Vector3 AveragePosition(List<Regiment> units)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in units)
        {
            if (regiment == null) continue;
            sum += regiment.transform.position;
            count++;
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    private static MajorOrder09F18 ReadOrder(object mission)
    {
        FieldInfo field = MissionField(mission, "Order");
        return field != null ? (MajorOrder09F18)field.GetValue(mission) : MajorOrder09F18.None;
    }

    private static bool ReadBool(object mission, string name)
    {
        FieldInfo field = MissionField(mission, name);
        return field != null && (bool)field.GetValue(mission);
    }

    private static Vector3 ReadVector(object mission, string name, Vector3 fallback)
    {
        FieldInfo field = MissionField(mission, name);
        return field != null ? (Vector3)field.GetValue(mission) : fallback;
    }

    private static void WriteVector(object mission, string name, Vector3 value)
    {
        FieldInfo field = MissionField(mission, name);
        if (field != null) field.SetValue(mission, value);
    }

    private static void WriteBool(object mission, string name, bool value)
    {
        FieldInfo field = MissionField(mission, name);
        if (field != null) field.SetValue(mission, value);
    }

    private static void WriteFloat(object mission, string name, float value)
    {
        FieldInfo field = MissionField(mission, name);
        if (field != null) field.SetValue(mission, value);
    }

    private static FieldInfo MissionField(object mission, string name)
    {
        return mission == null
            ? null
            : mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
