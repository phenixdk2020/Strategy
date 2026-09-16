using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29y
// Defensive stability guard.
// - DefendHere company slots remain on the intended river bank.
// - A bridge-centred defend order inherits the bank occupied by the battalion front.
// - Reserve/flank companies never contribute to the Major HQ reference centre.
// - Major HQ rear-axis comes from the committed mission Facing, not orderPoint-currentCenter,
//   so the axis cannot flip when companies pass the objective.
// This component does not move transforms. F27 remains the physical HQ/company mover.
[DefaultExecutionOrder(-500)]
public sealed class PrototypeDefensiveStability09F29Y : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float BankDeadZone = 7.0f;
    private const float MajorRearDepth = 155f;

    private FieldInfo battalionsField;
    private FieldInfo regimentHasDestinationField;
    private readonly Dictionary<Regiment, int> lastCorrectedBank = new Dictionary<Regiment, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeDefensiveStability09F29Y>() == null)
            new GameObject("PrototypeDefensiveStability_v000009f29y")
                .AddComponent<PrototypeDefensiveStability09F29Y>();
    }

    private void Awake()
    {
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);
        regimentHasDestinationField = typeof(Regiment).GetField("hasDestination", BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "DEF-STABILITY-09F29Y|Installed=True|DefendBankLock=True|" +
            "ReserveExcludedFromHqAnchor=True|StableMissionFacing=True|PhysicalMover=F27");
    }

    private void Update()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        int count = Mathf.Min(hierarchy.BattalionCount, battalions.Count);
        for (int i = 0; i < count; i++)
            GuardBattalion(i, battalions[i]);
    }

    private void GuardBattalion(int battalionIndex, object battalion)
    {
        if (battalion == null)
            return;

        Type type = battalion.GetType();
        FieldInfo lastOrderField = type.GetField("LastOrder", AnyInstance);
        FieldInfo lastOrderPointField = type.GetField("LastOrderPoint", AnyInstance);
        FieldInfo missionsField = type.GetField("Missions", AnyInstance);
        FieldInfo aiEnabledField = type.GetField("AIEnabled", AnyInstance);
        FieldInfo hqRootField = type.GetField("HqRoot", AnyInstance);
        FieldInfo hqGoalField = type.GetField("HqGoal", AnyInstance);
        FieldInfo hasHqGoalField = type.GetField("HasHqGoal", AnyInstance);

        if (lastOrderField == null || missionsField == null)
            return;

        object rawOrder = lastOrderField.GetValue(battalion);
        if (!(rawOrder is MajorOrder09F18) || (MajorOrder09F18)rawOrder != MajorOrder09F18.DefendHere)
            return;

        IDictionary missions = missionsField.GetValue(battalion) as IDictionary;
        if (missions == null || missions.Count == 0)
            return;

        Vector3 orderPoint = lastOrderPointField != null && lastOrderPointField.GetValue(battalion) is Vector3
            ? (Vector3)lastOrderPointField.GetValue(battalion)
            : Vector3.zero;

        GameObject hqRoot = hqRootField != null ? hqRootField.GetValue(battalion) as GameObject : null;
        int defendBank = ResolveDefendBank(missions, orderPoint, hqRoot);
        if (defendBank == 0)
            return;

        EnforceMissionBank(battalionIndex, missions, defendBank);

        bool aiEnabled = aiEnabledField != null && aiEnabledField.GetValue(battalion) is bool &&
                         (bool)aiEnabledField.GetValue(battalion);
        if (!aiEnabled || hqRoot == null || hqGoalField == null || hasHqGoalField == null)
            return;

        if (!TryGetFrontReference(missions, hqRoot.transform.position, out Vector3 frontCenter, out Vector3 facing))
            return;

        if (!TryRearHqGoal(frontCenter, facing, defendBank, out Vector3 desired))
            return;

        Vector3 current = hqRoot.transform.position;
        Vector3 target = PlanarDistance(current, desired) <= 4f ? current : desired;
        object oldGoalValue = hqGoalField.GetValue(battalion);
        Vector3 oldGoal = oldGoalValue is Vector3 ? (Vector3)oldGoalValue : current;
        bool oldHasGoal = hasHqGoalField.GetValue(battalion) is bool && (bool)hasHqGoalField.GetValue(battalion);

        hqGoalField.SetValue(battalion, Ground(target));
        hasHqGoalField.SetValue(battalion, true);

        if (!oldHasGoal || PlanarDistance(oldGoal, target) > 20f)
        {
            Debug.Log(
                "DEF-STABILITY-09F29Y|Level=MAJOR|Battalion=" + (battalionIndex + 1) +
                "|HqGoalStabilized=True|ReserveIgnored=True|Bank=" + defendBank +
                "|Goal=" + target.x.ToString("0.0") + "," + target.z.ToString("0.0"));
        }
    }

    private void EnforceMissionBank(int battalionIndex, IDictionary missions, int defendBank)
    {
        List<Vector3> reserved = new List<Vector3>();

        // Front companies first, reserve/flank second. This preserves the combat frontage
        // and lets the reserve adapt around already accepted front slots.
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (DictionaryEntry entry in missions)
            {
                Regiment unit = entry.Key as Regiment;
                object mission = entry.Value;
                if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                    continue;

                Type missionType = mission.GetType();
                FieldInfo orderField = missionType.GetField("Order", AnyInstance);
                FieldInfo goalField = missionType.GetField("Goal", AnyInstance);
                FieldInfo facingField = missionType.GetField("Facing", AnyInstance);
                FieldInfo reserveField = missionType.GetField("Reserve", AnyInstance);
                FieldInfo flankField = missionType.GetField("Flank", AnyInstance);
                FieldInfo arrivedField = missionType.GetField("Arrived", AnyInstance);
                FieldInfo nextAssertField = missionType.GetField("NextAssert", AnyInstance);

                if (orderField == null || goalField == null || facingField == null)
                    continue;
                object orderValue = orderField.GetValue(mission);
                if (!(orderValue is MajorOrder09F18) || (MajorOrder09F18)orderValue != MajorOrder09F18.DefendHere)
                    continue;

                bool reserve = ReadBool(reserveField, mission);
                bool flank = ReadBool(flankField, mission);
                bool secondary = reserve || flank;
                if ((pass == 0 && secondary) || (pass == 1 && !secondary))
                    continue;

                Vector3 goal = (Vector3)goalField.GetValue(mission);
                Vector3 facing = Flat((Vector3)facingField.GetValue(mission));
                int side = BankSide(goal);
                bool legal = side == defendBank && PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(goal, facing);

                if (legal && !PrototypeFormationSlotSafety09F23.ConflictsWithReserved(goal, reserved))
                {
                    reserved.Add(goal);
                    continue;
                }

                if (!TrySameBankGoal(goal, facing, defendBank, reserved, out Vector3 corrected))
                    continue;

                goalField.SetValue(mission, corrected);
                if (arrivedField != null)
                    arrivedField.SetValue(mission, false);
                if (nextAssertField != null)
                    nextAssertField.SetValue(mission, 0f);

                // Cancel only the stale destination. F27 will issue the corrected movement
                // later in the same frame and remains the destination/movement authority.
                if (regimentHasDestinationField != null)
                    regimentHasDestinationField.SetValue(unit, false);

                reserved.Add(corrected);

                if (!lastCorrectedBank.TryGetValue(unit, out int previous) || previous != defendBank)
                {
                    lastCorrectedBank[unit] = defendBank;
                    Debug.Log(
                        "DEF-STABILITY-09F29Y|Unit=" + unit.RegimentName +
                        "|Battalion=" + (battalionIndex + 1) +
                        "|DefendBankCorrected=True|Reserve=" + reserve +
                        "|From=" + goal.x.ToString("0.0") + "," + goal.z.ToString("0.0") +
                        "|To=" + corrected.x.ToString("0.0") + "," + corrected.z.ToString("0.0"));
                }
            }
        }
    }

    private static int ResolveDefendBank(IDictionary missions, Vector3 orderPoint, GameObject hqRoot)
    {
        int pointSide = BankSide(orderPoint);
        if (pointSide != 0)
            return pointSide;

        int frontScore = 0;
        int allScore = 0;
        foreach (DictionaryEntry entry in missions)
        {
            Regiment unit = entry.Key as Regiment;
            object mission = entry.Value;
            if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;

            int side = BankSide(unit.transform.position);
            allScore += side;

            Type missionType = mission.GetType();
            bool reserve = ReadBool(missionType.GetField("Reserve", AnyInstance), mission);
            bool flank = ReadBool(missionType.GetField("Flank", AnyInstance), mission);
            if (!reserve && !flank)
                frontScore += side;
        }

        if (frontScore != 0)
            return frontScore < 0 ? -1 : 1;
        if (allScore != 0)
            return allScore < 0 ? -1 : 1;
        return hqRoot != null ? BankSide(hqRoot.transform.position) : 0;
    }

    private static bool TryGetFrontReference(
        IDictionary missions,
        Vector3 fallback,
        out Vector3 center,
        out Vector3 facing)
    {
        Vector3 total = Vector3.zero;
        Vector3 facingTotal = Vector3.zero;
        int count = 0;

        foreach (DictionaryEntry entry in missions)
        {
            Regiment unit = entry.Key as Regiment;
            object mission = entry.Value;
            if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;

            Type missionType = mission.GetType();
            bool reserve = ReadBool(missionType.GetField("Reserve", AnyInstance), mission);
            bool flank = ReadBool(missionType.GetField("Flank", AnyInstance), mission);
            if (reserve || flank)
                continue;

            FieldInfo facingField = missionType.GetField("Facing", AnyInstance);
            total += unit.transform.position;
            if (facingField != null && facingField.GetValue(mission) is Vector3)
                facingTotal += Flat((Vector3)facingField.GetValue(mission));
            count++;
        }

        if (count == 0)
        {
            center = fallback;
            facing = Vector3.forward;
            return false;
        }

        center = total / count;
        center.y = PrototypeBootstrap.SampleGroundHeight(center.x, center.z) + 0.10f;
        facing = facingTotal.sqrMagnitude > 0.001f ? Flat(facingTotal) : Vector3.forward;
        return true;
    }

    private static bool TryRearHqGoal(Vector3 frontCenter, Vector3 facing, int bank, out Vector3 goal)
    {
        Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
        float[] depth = { MajorRearDepth, 190f, 225f, 260f };
        float[] lateral = { 0f, 65f, -65f, 130f, -130f };

        for (int d = 0; d < depth.Length; d++)
        {
            for (int l = 0; l < lateral.Length; l++)
            {
                Vector3 candidate = Ground(frontCenter - facing * depth[d] + right * lateral[l]);
                if (BankSide(candidate) != bank)
                    continue;
                if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(candidate, facing))
                    continue;
                goal = candidate;
                return true;
            }
        }

        goal = frontCenter;
        return false;
    }

    private static bool TrySameBankGoal(
        Vector3 desired,
        Vector3 facing,
        int bank,
        IList<Vector3> reserved,
        out Vector3 result)
    {
        float baseOffset = Mathf.Max(12f, Mathf.Abs(desired.x - PrototypeBootstrap.StreamCenterX(desired.z)));
        float[] zShift = { 0f, 12f, -12f, 24f, -24f, 36f, -36f, 54f, -54f };

        for (float extra = 0f; extra <= 120f; extra += 10f)
        {
            for (int i = 0; i < zShift.Length; i++)
            {
                float z = desired.z + zShift[i];
                float riverX = PrototypeBootstrap.StreamCenterX(z);
                Vector3 candidate = Ground(new Vector3(
                    riverX + bank * (baseOffset + extra),
                    0f,
                    z));

                if (BankSide(candidate) != bank)
                    continue;
                if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(candidate, facing))
                    continue;
                if (PrototypeFormationSlotSafety09F23.ConflictsWithReserved(candidate, reserved))
                    continue;

                result = candidate;
                return true;
            }
        }

        result = desired;
        return false;
    }

    private static int BankSide(Vector3 point)
    {
        float delta = point.x - PrototypeBootstrap.StreamCenterX(point.z);
        if (Mathf.Abs(delta) <= BankDeadZone)
            return 0;
        return delta < 0f ? -1 : 1;
    }

    private static bool ReadBool(FieldInfo field, object target)
    {
        if (field == null || target == null)
            return false;
        object value = field.GetValue(target);
        return value is bool && (bool)value;
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
