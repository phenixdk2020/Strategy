using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29b
// Higher-command attack correction discovered during F29 field QA.
// 1) In a front+reserve regimental attack, the battalion nearest the objective is the
//    assault/front battalion. It must not be sent backwards merely because a total
//    travel-distance permutation is mathematically cheaper.
// 2) The second Prussian QA company must not inherit the old 18th Regiment flank
//    waypoint from the pre-regimental scenario.
[DefaultExecutionOrder(850)]
public sealed class PrototypeAttackAIHotfix09F29B : MonoBehaviour
{
    private FieldInfo currentMissionField;
    private object lastProcessedMission;
    private bool legacyEnemyWaypointCleared;
    private bool installLogged;

    private const float RegimentKnownEnemyRange = 1200f;
    private const float ReserveDepth = 285f;
    private const float FlankOffset = 285f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackAIHotfix09F29B>() != null)
            return;

        new GameObject("PrototypeAttackAIHotfix_v000009f29b")
            .AddComponent<PrototypeAttackAIHotfix09F29B>();
    }

    private void Awake()
    {
        currentMissionField = typeof(PrototypeRegimentalHQ09F28).GetField(
            "currentMission",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (currentMissionField == null)
        {
            Debug.LogError("ATTACK-AI-09F29B|Installed=False|Reason=RegimentalMissionReflectionMissing");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!enabled)
            return;

        NeutralizeLegacyEnemyWaypoint();
        CorrectNewRegimentalAttackRoleAssignment();

        if (!installLogged)
        {
            PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
            PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
            if (hierarchy != null && hierarchy.Installed && regimental != null && regimental.Installed)
            {
                installLogged = true;
                Debug.Log(
                    "ATTACK-AI-09F29B|Installed=True|NearestBattalionKeepsFront=True|" +
                    "ReserveCannotWinByTravelPermutation=True|Legacy18thWaypoint=False");
            }
        }
    }

    private void NeutralizeLegacyEnemyWaypoint()
    {
        if (legacyEnemyWaypointCleared)
            return;

        Regiment enemy = FindRegiment("18th Regiment");
        if (enemy == null)
            return;

        OfficerAIController controller = enemy.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
            return;

        // In the old scenario the 18th used a one-off flank waypoint. It is now a
        // normal second hostile company, so start it as an actual attack formation.
        if (controller.Mission == OfficerAIMission.MoveToPoint)
        {
            Regiment target = FindNearestEnemy(enemy.transform.position, enemy.Team, 5000f);
            if (target != null)
            {
                controller.SetAttackMission(target);
                Debug.Log(
                    "ATTACK-AI-09F29B|Unit=18th Regiment|LegacyWaypointCleared=True|" +
                    "NewMission=AttackTarget|Target=" + target.RegimentName);
            }
        }

        legacyEnemyWaypointCleared = true;
    }

    private void CorrectNewRegimentalAttackRoleAssignment()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;

        if (regimental == null || !regimental.Installed || regimental.HqRoot == null ||
            hierarchy == null || !hierarchy.Installed || hierarchy.BattalionCount < 2)
        {
            return;
        }

        object mission = currentMissionField.GetValue(regimental);
        if (mission == null || ReferenceEquals(mission, lastProcessedMission))
            return;

        lastProcessedMission = mission;

        System.Type missionType = mission.GetType();
        FieldInfo orderField = missionType.GetField(
            "Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo objectiveField = missionType.GetField(
            "Objective", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (orderField == null || objectiveField == null)
        {
            Debug.LogError("ATTACK-AI-09F29B|RoleCorrection=False|Reason=MissionFieldsMissing");
            return;
        }

        object orderValue = orderField.GetValue(mission);
        object objectiveValue = objectiveField.GetValue(mission);
        if (!(orderValue is MajorOrder09F18) || !(objectiveValue is Vector3))
            return;

        MajorOrder09F18 order = (MajorOrder09F18)orderValue;
        if (order != MajorOrder09F18.AttackHere)
            return;

        Vector3 objective = (Vector3)objectiveValue;
        List<Regiment> knownEnemies = KnownEnemies(objective, RegimentKnownEnemyRange);

        bool useReserve =
            regimental.Doctrine == OfficerAIDoctrine.Defensive ||
            (regimental.Doctrine == OfficerAIDoctrine.Balanced && knownEnemies.Count <= 4);

        bool useFlank =
            regimental.Doctrine == OfficerAIDoctrine.Offensive &&
            knownEnemies.Count <= 3;

        if (!useReserve && !useFlank)
            return;

        float distance0 = PlanarDistance(hierarchy.GetBattalionCenter(0), objective);
        float distance1 = PlanarDistance(hierarchy.GetBattalionCenter(1), objective);

        int frontIndex = distance0 <= distance1 ? 0 : 1;
        int supportIndex = frontIndex == 0 ? 1 : 0;

        Vector3 forward = Flat(objective - regimental.HqRoot.transform.position);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        Vector3 frontGoal = Ground(objective);
        Vector3 supportGoal;
        string disposition;

        if (useFlank)
        {
            float sign = supportIndex == 0 ? -1f : 1f;
            supportGoal = Ground(objective + lateral * (sign * FlankOffset) - forward * 45f);
            disposition = "NEAREST FRONT + FARTHER FLANK";
        }
        else
        {
            supportGoal = Ground(objective - forward * ReserveDepth);
            disposition = "NEAREST FRONT + FARTHER RESERVE";
        }

        hierarchy.IssueBattalionOrderFromRegiment(
            frontIndex,
            MajorOrder09F18.AttackHere,
            frontGoal,
            regimental.Doctrine);

        hierarchy.IssueBattalionOrderFromRegiment(
            supportIndex,
            MajorOrder09F18.AttackHere,
            supportGoal,
            regimental.Doctrine);

        Debug.Log(
            "REG-ROLE-09F29B|Corrected=True|Disposition=" + disposition +
            "|FrontBattalion=" + (frontIndex + 1) +
            "|SupportBattalion=" + (supportIndex + 1) +
            "|FrontDistance=" + Mathf.Min(distance0, distance1).ToString("0.0") +
            "|SupportDistance=" + Mathf.Max(distance0, distance1).ToString("0.0") +
            "|KnownEnemies=" + knownEnemies.Count +
            "|NearestBattalionRetreatPrevented=True");
    }

    private static List<Regiment> KnownEnemies(Vector3 point, float range)
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team == BattleTeam.Denmark ||
                regiment.IsRouted || regiment.CurrentStrength <= 0)
            {
                continue;
            }

            if (PlanarDistance(point, regiment.transform.position) <= range)
                result.Add(regiment);
        }

        return result;
    }

    private static Regiment FindNearestEnemy(Vector3 point, BattleTeam ownTeam, float range)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = range;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team == ownTeam ||
                regiment.IsRouted || regiment.CurrentStrength <= 0)
            {
                continue;
            }

            float distance = PlanarDistance(point, regiment.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = regiment;
            }
        }

        return nearest;
    }

    private static Regiment FindRegiment(string name)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.RegimentName == name)
                return regiment;
        }

        return null;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
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
