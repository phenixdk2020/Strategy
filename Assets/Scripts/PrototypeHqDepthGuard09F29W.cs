using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29w
// Keeps AI-controlled Danish HQs behind their actual subordinate front relative to the
// nearest active enemy. This layer never moves a Transform directly; it only corrects
// the goal consumed by the existing F27/F28 HQ movers, preserving one physical owner.
[DefaultExecutionOrder(900)]
public sealed class PrototypeHqDepthGuard09F29W : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const float ReviewInterval = 0.50f;
    private const float MajorMinBehind = 120f;
    private const float MajorPreferredBehind = 175f;
    private const float MajorMaxLateral = 240f;
    private const float RegimentMinBehindMajors = 220f;
    private const float RegimentPreferredBehindMajors = 285f;
    private const float RegimentMaxLateral = 320f;

    private FieldInfo battalionsField;
    private FieldInfo regimentalGoalField;
    private FieldInfo regimentalHasGoalField;
    private float nextReview;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHqDepthGuard09F29W>() == null)
            new GameObject("PrototypeHqDepthGuard_v000009f29w").AddComponent<PrototypeHqDepthGuard09F29W>();
    }

    private void Awake()
    {
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);
        regimentalGoalField = typeof(PrototypeRegimentalHQ09F28).GetField("hqGoal", AnyInstance);
        regimentalHasGoalField = typeof(PrototypeRegimentalHQ09F28).GetField("hasHqGoal", AnyInstance);

        Debug.Log(
            "HQ-DEPTH-09F29W|Installed=True|PhysicalOwner=F27/F28|" +
            "MajorMinBehind=" + MajorMinBehind.ToString("0") +
            "|MajorPreferred=" + MajorPreferredBehind.ToString("0") +
            "|RegimentMinBehindMajors=" + RegimentMinBehindMajors.ToString("0") +
            "|RegimentPreferred=" + RegimentPreferredBehindMajors.ToString("0") +
            "|ManualHQAuthorityPreserved=True");
    }

    private void Update()
    {
        if (Time.time < nextReview)
            return;
        nextReview = Time.time + ReviewInterval;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed)
            return;

        GuardMajors(hierarchy);
        GuardRegimentalHq(hierarchy);
    }

    private void GuardMajors(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        int count = Mathf.Min(hierarchy.BattalionCount, battalions.Count);
        for (int i = 0; i < count; i++)
        {
            if (!hierarchy.GetBattalionAIEnabled(i))
                continue; // manual Major movement/placement remains player-owned.

            GameObject hq = hierarchy.GetMajorHq(i);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(i);
            if (hq == null || companies == null)
                continue;

            Vector3 center = ActiveCenter(companies, hq.transform.position);
            Regiment enemy = FindNearestEnemy(center);
            if (enemy == null)
                continue;

            Vector3 forward = Flat(enemy.transform.position - center);
            object battalion = battalions[i];
            if (battalion == null)
                continue;

            System.Type type = battalion.GetType();
            FieldInfo goalField = type.GetField("HqGoal", AnyInstance);
            FieldInfo hasGoalField = type.GetField("HasHqGoal", AnyInstance);
            if (goalField == null || hasGoalField == null)
                continue;

            bool hasGoal = ReadBool(hasGoalField, battalion);
            Vector3 existingGoal = hasGoal ? ReadVector(goalField, battalion, hq.transform.position) : hq.transform.position;

            if (IsAdequatelyBehind(center, hq.transform.position, forward, MajorMinBehind, MajorMaxLateral))
                continue;
            if (hasGoal && IsAdequatelyBehind(center, existingGoal, forward, MajorMinBehind, MajorMaxLateral))
                continue; // F27 is already moving the HQ to a valid rear position.

            if (!TrySafeRearGoal(hq.transform.position, center, forward, MajorPreferredBehind, out Vector3 desired))
                continue;

            goalField.SetValue(battalion, desired);
            hasGoalField.SetValue(battalion, true);

            Debug.Log(
                "HQ-DEPTH-09F29W|Level=MAJOR|Battalion=" + (i + 1) +
                "|Corrected=True|Enemy=" + enemy.RegimentName +
                "|CurrentDepth=" + RearDepth(center, hq.transform.position, forward).ToString("0") +
                "|GoalDepth=" + RearDepth(center, desired, forward).ToString("0") +
                "|Goal=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0"));
        }
    }

    private void GuardRegimentalHq(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed || regimental.HqRoot == null)
            return;
        if (!regimental.AIEnabled)
            return; // right-click/manual HQ placement stays authoritative.
        if (regimentalGoalField == null || regimentalHasGoalField == null)
            return;

        GameObject majorA = hierarchy.GetMajorHq(0);
        GameObject majorB = hierarchy.GetMajorHq(1);
        if (majorA == null || majorB == null)
            return;

        Vector3 majorMid = (majorA.transform.position + majorB.transform.position) * 0.5f;
        Regiment enemy = FindNearestEnemy(majorMid);
        if (enemy == null)
            return;

        Vector3 forward = Flat(enemy.transform.position - majorMid);
        Vector3 current = regimental.HqRoot.transform.position;
        bool hasGoal = ReadBool(regimentalHasGoalField, regimental);
        Vector3 existingGoal = hasGoal
            ? ReadVector(regimentalGoalField, regimental, current)
            : current;

        if (IsAdequatelyBehind(majorMid, current, forward, RegimentMinBehindMajors, RegimentMaxLateral))
            return;
        if (hasGoal && IsAdequatelyBehind(majorMid, existingGoal, forward, RegimentMinBehindMajors, RegimentMaxLateral))
            return;

        if (!TrySafeRearGoal(current, majorMid, forward, RegimentPreferredBehindMajors, out Vector3 desired))
            return;

        regimentalGoalField.SetValue(regimental, desired);
        regimentalHasGoalField.SetValue(regimental, true);

        Debug.Log(
            "HQ-DEPTH-09F29W|Level=REGIMENT|Corrected=True|Enemy=" + enemy.RegimentName +
            "|CurrentDepth=" + RearDepth(majorMid, current, forward).ToString("0") +
            "|GoalDepth=" + RearDepth(majorMid, desired, forward).ToString("0") +
            "|Goal=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0"));
    }

    private static Vector3 ActiveCenter(IReadOnlyList<Regiment> units, Vector3 fallback)
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;
            total += unit.transform.position;
            count++;
        }
        return count > 0 ? total / count : fallback;
    }

    private static Regiment FindNearestEnemy(Vector3 point)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment best = null;
        float bestDistance = float.MaxValue;
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.Team != BattleTeam.Prussia || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(point, unit.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = unit;
            }
        }
        return best;
    }

    private static bool IsAdequatelyBehind(
        Vector3 anchor,
        Vector3 hq,
        Vector3 forward,
        float minimumBehind,
        float maxLateral)
    {
        float depth = RearDepth(anchor, hq, forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 offset = hq - anchor;
        offset.y = 0f;
        float lateral = Mathf.Abs(Vector3.Dot(offset, right));
        return depth >= minimumBehind && lateral <= maxLateral;
    }

    private static float RearDepth(Vector3 anchor, Vector3 hq, Vector3 forward)
    {
        Vector3 fromHqToAnchor = anchor - hq;
        fromHqToAnchor.y = 0f;
        return Vector3.Dot(fromHqToAnchor, forward);
    }

    private static bool TrySafeRearGoal(
        Vector3 current,
        Vector3 anchor,
        Vector3 forward,
        float preferredBehind,
        out Vector3 goal)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float[] depthExtra = { 0f, 70f, 140f };
        float[] lateral = { 0f, 85f, -85f, 170f, -170f };

        for (int d = 0; d < depthExtra.Length; d++)
        {
            for (int l = 0; l < lateral.Length; l++)
            {
                Vector3 candidate = anchor - forward * (preferredBehind + depthExtra[d]) + right * lateral[l];
                candidate = Ground(candidate);
                if (IsSafeHqRelocation(current, candidate))
                {
                    goal = candidate;
                    return true;
                }
            }
        }

        goal = current;
        return false;
    }

    private static bool IsSafeHqRelocation(Vector3 current, Vector3 goal)
    {
        Vector3 travel = goal - current;
        travel.y = 0f;
        if (travel.sqrMagnitude < 0.01f)
            return true;
        if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(goal, travel))
            return false;

        int currentSide = BankSide(current);
        int goalSide = BankSide(goal);
        return !(currentSide != 0 && goalSide != 0 && currentSide != goalSide);
    }

    private static int BankSide(Vector3 point)
    {
        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        float delta = point.x - riverX;
        if (Mathf.Abs(delta) < 4.0f)
            return 0;
        return delta < 0f ? -1 : 1;
    }

    private static bool ReadBool(FieldInfo field, object target)
    {
        object value = field.GetValue(target);
        return value is bool && (bool)value;
    }

    private static Vector3 ReadVector(FieldInfo field, object target, Vector3 fallback)
    {
        object value = field.GetValue(target);
        return value is Vector3 ? (Vector3)value : fallback;
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
