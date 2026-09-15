using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29l
// Tightens Major/Oberstløjtnant follow behaviour without changing command authority.
// AI ON/OFF decides whether officers make autonomous decisions; it must not decide
// whether a HQ physically follows an already-issued player/higher-command mission.
[DefaultExecutionOrder(820)]
public sealed class PrototypeHqFollow09F29L : MonoBehaviour
{
    private const BindingFlags AnyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const float MajorBehind = 105f;
    private const float MajorRepositionDelta = 55f;
    private const float RegimentalBehind = 210f;
    private const float RegimentalRepositionDelta = 90f;

    private FieldInfo battalionsField;
    private FieldInfo currentMissionField;
    private FieldInfo regHasGoalField;
    private FieldInfo regGoalField;
    private MethodInfo majorSafeRelocationMethod;
    private MethodInfo regSafeRelocationMethod;

    private readonly Dictionary<int, Vector3> authoredMajorGoals = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, string> lastMajorOrderSignatures = new Dictionary<int, string>();
    private readonly HashSet<int> manuallyPinnedMajors = new HashSet<int>();

    private object lastRegimentalMission;
    private Vector3 authoredRegimentalGoal;
    private bool hasAuthoredRegimentalGoal;
    private bool regimentalManuallyPinned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHqFollow09F29L>() == null)
            new GameObject("PrototypeHqFollow_v000009f29l").AddComponent<PrototypeHqFollow09F29L>();
    }

    private void Awake()
    {
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyMember);
        currentMissionField = typeof(PrototypeRegimentalHQ09F28).GetField("currentMission", AnyMember);
        regHasGoalField = typeof(PrototypeRegimentalHQ09F28).GetField("hasHqGoal", AnyMember);
        regGoalField = typeof(PrototypeRegimentalHQ09F28).GetField("hqGoal", AnyMember);
        majorSafeRelocationMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod(
            "IsSafeHqRelocation", AnyMember, null, new[] { typeof(Vector3), typeof(Vector3) }, null);
        regSafeRelocationMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod(
            "IsSafeHqRelocation", AnyMember, null, new[] { typeof(Vector3), typeof(Vector3) }, null);

        Debug.Log(
            "HQ-FOLLOW-09F29L|Installed=True|MajorBehind=" + MajorBehind.ToString("0") +
            "|MajorReposition=" + MajorRepositionDelta.ToString("0") +
            "|RegBehind=" + RegimentalBehind.ToString("0") +
            "|RegReposition=" + RegimentalRepositionDelta.ToString("0") +
            "|FollowIndependentOfAI=True|ManualHQMovePreserved=True");
    }

    private void Update()
    {
        UpdateMajorFollow();
        UpdateRegimentalFollow();
    }

    private void UpdateMajorFollow()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        for (int i = 0; i < battalions.Count; i++)
        {
            object battalion = battalions[i];
            if (battalion == null)
                continue;

            System.Type type = battalion.GetType();
            FieldInfo hqField = type.GetField("HqRoot", AnyMember);
            FieldInfo aiField = type.GetField("AIEnabled", AnyMember);
            FieldInfo hasLastField = type.GetField("HasLastOrder", AnyMember);
            FieldInfo orderField = type.GetField("LastOrder", AnyMember);
            FieldInfo pointField = type.GetField("LastOrderPoint", AnyMember);
            FieldInfo hasGoalField = type.GetField("HasHqGoal", AnyMember);
            FieldInfo goalField = type.GetField("HqGoal", AnyMember);
            if (hqField == null || aiField == null || hasLastField == null || orderField == null ||
                pointField == null || hasGoalField == null || goalField == null)
                continue;

            GameObject hq = hqField.GetValue(battalion) as GameObject;
            bool hasLast = ReadBool(hasLastField, battalion);
            if (hq == null || !hasLast)
                continue;

            MajorOrder09F18 order = (MajorOrder09F18)orderField.GetValue(battalion);
            if (order == MajorOrder09F18.None || order == MajorOrder09F18.HoldPosition)
                continue;

            Vector3 orderPoint = (Vector3)pointField.GetValue(battalion);
            string signature = order + "|" + orderPoint.x.ToString("0.0") + "|" + orderPoint.z.ToString("0.0");
            if (!lastMajorOrderSignatures.TryGetValue(i, out string previousSignature) || previousSignature != signature)
            {
                lastMajorOrderSignatures[i] = signature;
                manuallyPinnedMajors.Remove(i);
            }

            bool aiEnabled = ReadBool(aiField, battalion);
            bool hasGoal = ReadBool(hasGoalField, battalion);
            Vector3 existingGoal = hasGoal ? (Vector3)goalField.GetValue(battalion) : Vector3.zero;

            if (hasGoal && !aiEnabled)
            {
                if (!authoredMajorGoals.TryGetValue(i, out Vector3 authored) || PlanarDistance(existingGoal, authored) > 2.0f)
                    manuallyPinnedMajors.Add(i);
            }

            if (manuallyPinnedMajors.Contains(i))
                continue;

            Vector3 center = hierarchy.GetBattalionCenter(i);
            Vector3 forward = ResolveMissionForward(center, orderPoint, hq.transform.forward, order);
            Vector3 desired = order == MajorOrder09F18.WithdrawHere
                ? Ground(center + forward * MajorBehind)
                : Ground(center - forward * MajorBehind);

            if (!SafeMajorRelocation(hierarchy, hq.transform.position, desired))
                continue;

            if (!hasGoal && PlanarDistance(hq.transform.position, desired) < MajorRepositionDelta)
                continue;

            goalField.SetValue(battalion, desired);
            hasGoalField.SetValue(battalion, true);
            authoredMajorGoals[i] = desired;
        }
    }

    private void UpdateRegimentalFollow()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (regimental == null || hierarchy == null || !regimental.Installed || !hierarchy.Installed ||
            regimental.HqRoot == null || currentMissionField == null || regHasGoalField == null || regGoalField == null)
            return;

        object mission = currentMissionField.GetValue(regimental);
        if (mission == null)
            return;

        if (!ReferenceEquals(mission, lastRegimentalMission))
        {
            lastRegimentalMission = mission;
            regimentalManuallyPinned = false;
        }

        System.Type missionType = mission.GetType();
        FieldInfo orderField = missionType.GetField("Order", AnyMember);
        FieldInfo objectiveField = missionType.GetField("Objective", AnyMember);
        if (orderField == null || objectiveField == null)
            return;

        MajorOrder09F18 order = (MajorOrder09F18)orderField.GetValue(mission);
        if (order == MajorOrder09F18.None || order == MajorOrder09F18.HoldPosition)
            return;

        bool hasGoal = ReadBool(regHasGoalField, regimental);
        Vector3 existingGoal = hasGoal ? (Vector3)regGoalField.GetValue(regimental) : Vector3.zero;
        if (hasGoal && !regimental.AIEnabled)
        {
            if (!hasAuthoredRegimentalGoal || PlanarDistance(existingGoal, authoredRegimentalGoal) > 2.0f)
                regimentalManuallyPinned = true;
        }

        if (regimentalManuallyPinned)
            return;

        GameObject major0 = hierarchy.GetMajorHq(0);
        GameObject major1 = hierarchy.GetMajorHq(1);
        if (major0 == null || major1 == null)
            return;

        Vector3 majorMid = (major0.transform.position + major1.transform.position) * 0.5f;
        Vector3 objective = (Vector3)objectiveField.GetValue(mission);
        Vector3 forward = ResolveMissionForward(majorMid, objective, regimental.HqRoot.transform.forward, order);
        Vector3 desired = order == MajorOrder09F18.WithdrawHere
            ? Ground(majorMid + forward * RegimentalBehind)
            : Ground(majorMid - forward * RegimentalBehind);

        if (!SafeRegimentalRelocation(regimental, regimental.HqRoot.transform.position, desired))
            return;

        if (!hasGoal && PlanarDistance(regimental.HqRoot.transform.position, desired) < RegimentalRepositionDelta)
            return;

        regGoalField.SetValue(regimental, desired);
        regHasGoalField.SetValue(regimental, true);
        authoredRegimentalGoal = desired;
        hasAuthoredRegimentalGoal = true;
    }

    private Vector3 ResolveMissionForward(Vector3 center, Vector3 objective, Vector3 fallback, MajorOrder09F18 order)
    {
        Vector3 forward = Flat(objective - center);

        if (order == MajorOrder09F18.DefendHere && forward.sqrMagnitude < 0.01f)
        {
            Regiment enemy = FindNearestEnemy(center, 1400f);
            if (enemy != null)
                forward = Flat(enemy.transform.position - center);
        }

        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(fallback);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.right;
        return forward.normalized;
    }

    private static Regiment FindNearestEnemy(Vector3 point, float maxDistance)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment best = null;
        float bestDistance = maxDistance;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team != BattleTeam.Prussia || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;
            float d = PlanarDistance(point, candidate.transform.position);
            if (d <= bestDistance)
            {
                best = candidate;
                bestDistance = d;
            }
        }
        return best;
    }

    private bool SafeMajorRelocation(PrototypeRegimentHierarchy09F27 hierarchy, Vector3 from, Vector3 to)
    {
        if (majorSafeRelocationMethod == null)
            return true;
        object result = majorSafeRelocationMethod.Invoke(null, new object[] { from, to });
        return !(result is bool) || (bool)result;
    }

    private bool SafeRegimentalRelocation(PrototypeRegimentalHQ09F28 regimental, Vector3 from, Vector3 to)
    {
        if (regSafeRelocationMethod == null)
            return true;
        object result = regSafeRelocationMethod.Invoke(null, new object[] { from, to });
        return !(result is bool) || (bool)result;
    }

    private static bool ReadBool(FieldInfo field, object instance)
    {
        object value = field != null ? field.GetValue(instance) : null;
        return value is bool && (bool)value;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.14f;
        return point;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude < 0.0001f ? Vector3.zero : value.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
