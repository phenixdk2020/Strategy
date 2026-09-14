using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f22
// Re-plans Major-owned company endpoints around the actual player objective.
// Front slots are unique and assigned by minimum total march distance, so companies
// do not criss-cross simply because of internal list order. Attack slots are centred
// on the clicked objective, not on an arbitrary nearby enemy centre.
[DefaultExecutionOrder(470)]
public sealed class PrototypeMajorFormationPlanner09F22 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private FieldInfo missionsField;
    private FieldInfo lastAssignmentsField;
    private FieldInfo lastOrderField;
    private FieldInfo lastOrderPointField;
    private FieldInfo reserveField;
    private MethodInfo safeCompanyMethod;
    private MethodInfo knownEnemiesMethod;

    private string lastSignature = string.Empty;
    private float nextReview;

    private const float ReviewInterval = 0.20f;
    private const float CompanySpacing = 60f;
    private const float AttackStandoff = 68f;
    private const float ReserveDepth = 78f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMajorFormationPlanner09F22>() == null)
            new GameObject("PrototypeMajorFormationPlanner_v000009f22").AddComponent<PrototypeMajorFormationPlanner09F22>();
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
            lastSignature = string.Empty;
            return;
        }

        MajorOrder09F18 order = (MajorOrder09F18)lastOrderField.GetValue(major);
        if (order == MajorOrder09F18.None || order == MajorOrder09F18.HoldPosition)
            return;

        Vector3 objective = (Vector3)lastOrderPointField.GetValue(major);
        Regiment reserve = reserveField.GetValue(major) as Regiment;
        string signature = BuildSignature(order, objective, reserve, missions);
        if (signature == lastSignature)
            return;

        Replan(order, objective, reserve, missions);
        lastSignature = BuildSignature(order, objective, reserve, missions);
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
        safeCompanyMethod = type.GetMethod("SafeCompany", flags);
        knownEnemiesMethod = type.GetMethod("KnownEnemies", flags);

        bool ok = missionsField != null && lastOrderField != null &&
                  lastOrderPointField != null && reserveField != null &&
                  safeCompanyMethod != null;
        if (!ok)
            Debug.LogError("HQ-FORMATION-09F22|Installed=False|Reason=MajorReflectionMissing");
        return ok;
    }

    private void Replan(MajorOrder09F18 order, Vector3 objective, Regiment reserve, IDictionary missions)
    {
        List<Regiment> front = new List<Regiment>();
        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null || regiment.IsRouted || !missions.Contains(regiment))
                continue;
            if (regiment == reserve)
                continue;

            object mission = missions[regiment];
            if (mission == null)
                continue;
            bool reserveRole = ReadBool(mission, "Reserve");
            if (!reserveRole)
                front.Add(regiment);
        }

        if (front.Count == 0)
            return;

        Vector3 battalionCenter = AveragePosition(front);
        Vector3 forward = GetObjectiveForward(order, objective, battalionCenter, missions, front);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        Vector3 lineCenter = Ground(objective);
        if (order == MajorOrder09F18.AttackHere)
            lineCenter = Ground(objective - forward * AttackStandoff);

        List<Vector3> slots = new List<Vector3>();
        float middle = (front.Count - 1) * 0.5f;
        for (int i = 0; i < front.Count; i++)
        {
            Vector3 raw = lineCenter + lateral * ((i - middle) * CompanySpacing);
            slots.Add(SafeCompany(raw, forward));
        }

        int[] assignment = BestAssignment(front, slots);
        for (int i = 0; i < front.Count; i++)
        {
            Regiment regiment = front[i];
            Vector3 goal = slots[assignment[i]];
            Vector3 facing = FacingForSlot(goal, forward);
            object mission = missions[regiment];
            ApplyMission(regiment, mission, goal, facing, false, false);
            UpdateSavedAssignment(regiment, goal, facing, false, false);
        }

        if (reserve != null && missions.Contains(reserve))
        {
            object mission = missions[reserve];
            bool flank = ReadBool(mission, "Flank");
            if (!flank)
            {
                Vector3 reserveGoal = SafeCompany(lineCenter - forward * ReserveDepth, forward);
                Vector3 reserveFacing = FacingForSlot(reserveGoal, forward);
                ApplyMission(reserve, mission, reserveGoal, reserveFacing, true, false);
                UpdateSavedAssignment(reserve, reserveGoal, reserveFacing, true, false);
            }
            else
            {
                // Keep the 09f19 tactical flank destination, but make the company face
                // the nearest relevant enemy rather than preserving a parallel front facing.
                Vector3 currentGoal = ReadVector(mission, "Goal", reserve.transform.position);
                Vector3 flankFacing = FacingForSlot(currentGoal, forward);
                WriteVector(mission, "Facing", flankFacing);
                UpdateSavedAssignment(reserve, currentGoal, flankFacing, true, true);
            }
        }

        Debug.Log("HQ-FORMATION-09F22|Order=" + order +
                  "|Objective=" + objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
                  "|Front=" + front.Count +
                  "|NearestUniqueSlots=True|ObjectiveCentred=True|IndividualFacing=True");
    }

    private Vector3 GetObjectiveForward(
        MajorOrder09F18 order,
        Vector3 objective,
        Vector3 battalionCenter,
        IDictionary missions,
        List<Regiment> front)
    {
        if (order == MajorOrder09F18.AttackHere || order == MajorOrder09F18.AdvanceHere)
        {
            Vector3 towardObjective = objective - battalionCenter;
            towardObjective.y = 0f;
            if (towardObjective.sqrMagnitude > 4f)
                return towardObjective.normalized;
        }

        Regiment threat = NearestKnownEnemy(objective);
        if (threat != null)
        {
            Vector3 towardEnemy = threat.transform.position - objective;
            towardEnemy.y = 0f;
            if (towardEnemy.sqrMagnitude > 0.01f)
                return towardEnemy.normalized;
        }

        foreach (Regiment r in front)
        {
            if (!missions.Contains(r))
                continue;
            object mission = missions[r];
            Vector3 existing = ReadVector(mission, "Facing", Vector3.zero);
            existing.y = 0f;
            if (existing.sqrMagnitude > 0.01f)
                return existing.normalized;
        }

        Vector3 fallback = objective - (major.HqRoot != null ? major.HqRoot.transform.position : battalionCenter);
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
        if (knownEnemiesMethod != null)
        {
            object value = knownEnemiesMethod.Invoke(major, new object[] { point });
            IEnumerable list = value as IEnumerable;
            if (list != null)
            {
                Regiment best = null;
                float bestDistance = float.PositiveInfinity;
                foreach (object entry in list)
                {
                    Regiment enemy = entry as Regiment;
                    if (enemy == null || enemy.IsRouted || enemy.CurrentStrength <= 0)
                        continue;
                    float distance = PlanarDistance(point, enemy.transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = enemy;
                    }
                }
                if (best != null)
                    return best;
            }
        }
        return null;
    }

    private void ApplyMission(
        Regiment regiment,
        object mission,
        Vector3 goal,
        Vector3 facing,
        bool reserveRole,
        bool flank)
    {
        if (mission == null || regiment == null)
            return;

        WriteVector(mission, "Goal", goal);
        WriteVector(mission, "Facing", facing);
        WriteBool(mission, "Reserve", reserveRole);
        WriteBool(mission, "Flank", flank);
        WriteBool(mission, "Arrived", false);
        WriteFloat(mission, "NextAssert", 0f);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetMoveMission(goal);
        else
            regiment.OrderMove(goal);

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
        {
            MajorOrder09F18 missionOrder = ReadOrder(mission);
            visuals.SetMission(regiment, goal, facing, missionOrder, reserveRole, flank);
        }
    }

    private void UpdateSavedAssignment(Regiment regiment, Vector3 goal, Vector3 facing, bool reserveRole, bool flank)
    {
        if (lastAssignmentsField == null || regiment == null)
            return;
        IDictionary saved = lastAssignmentsField.GetValue(major) as IDictionary;
        if (saved == null || !saved.Contains(regiment))
            return;
        object mission = saved[regiment];
        if (mission == null)
            return;
        WriteVector(mission, "Goal", goal);
        WriteVector(mission, "Facing", facing);
        WriteBool(mission, "Reserve", reserveRole);
        WriteBool(mission, "Flank", flank);
        WriteBool(mission, "Arrived", false);
        WriteFloat(mission, "NextAssert", 0f);
    }

    private Vector3 SafeCompany(Vector3 point, Vector3 facing)
    {
        object value = safeCompanyMethod.Invoke(major, new object[] { point, facing });
        return value is Vector3 ? (Vector3)value : Ground(point);
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
            float distance = PlanarDistance(units[index].transform.position, slots[slot]);
            SearchAssignment(index + 1, units, slots, current, best, used,
                cost + distance * distance, ref bestCost);
            used[slot] = false;
        }
    }

    private static Vector3 AveragePosition(List<Regiment> units)
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in units)
        {
            if (regiment == null)
                continue;
            total += regiment.transform.position;
            count++;
        }
        return count > 0 ? total / count : Vector3.zero;
    }

    private string BuildSignature(MajorOrder09F18 order, Vector3 point, Regiment reserve, IDictionary missions)
    {
        string reserveName = reserve != null ? reserve.RegimentName : "NONE";
        return order + "|" + Mathf.RoundToInt(point.x * 2f) + ":" + Mathf.RoundToInt(point.z * 2f) +
               "|R=" + reserveName + "|M=" + missions.Count;
    }

    private static MajorOrder09F18 ReadOrder(object mission)
    {
        if (mission == null)
            return MajorOrder09F18.None;
        FieldInfo field = mission.GetType().GetField("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null ? (MajorOrder09F18)field.GetValue(mission) : MajorOrder09F18.None;
    }

    private static bool ReadBool(object mission, string name)
    {
        FieldInfo field = mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && (bool)field.GetValue(mission);
    }

    private static Vector3 ReadVector(object mission, string name, Vector3 fallback)
    {
        FieldInfo field = mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null ? (Vector3)field.GetValue(mission) : fallback;
    }

    private static void WriteVector(object mission, string name, Vector3 value)
    {
        FieldInfo field = mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(mission, value);
    }

    private static void WriteBool(object mission, string name, bool value)
    {
        FieldInfo field = mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(mission, value);
    }

    private static void WriteFloat(object mission, string name, float value)
    {
        FieldInfo field = mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(mission, value);
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
