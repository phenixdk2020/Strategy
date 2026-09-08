using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09h3 AI approach-formation policy.
// Formation-only layer: moving AI regiments march in Column while they are still
// outside the deployment zone. OfficerAI/frontage may deploy to Line near contact.
// This component never writes destination, combat, morale or selection state.
[DefaultExecutionOrder(1350)]
public sealed class PrototypeApproachFormationPolicy09H3 : MonoBehaviour
{
    private readonly Dictionary<Regiment, bool> marchColumnActive =
        new Dictionary<Regiment, bool>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float MinimumWaypointColumnDistance = 16f;
    private const float DeploymentBuffer = 12f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeApproachFormationPolicy09H3>() != null)
            return;

        GameObject root = new GameObject("PrototypeApproachFormationPolicy_v000009h3");
        root.AddComponent<PrototypeApproachFormationPolicy09H3>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        destinationField = typeof(Regiment).GetField("destination", flags);

        if (hasDestinationField == null || destinationField == null)
        {
            Debug.LogWarning("FORMATION-09H3|Installed=False|Reason=RegimentMovementFieldsNotFound");
            enabled = false;
            return;
        }

        Debug.Log(
            "FORMATION-09H3|Installed=True|Policy=MarchColumnThenDeployLine|" +
            "FormationWritesOnly=True|MovementWrites=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            ApplyPolicy(regiment, battle);
        }
    }

    private void ApplyPolicy(Regiment regiment, BattleManager battle)
    {
        OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
        if (ai == null || !ai.AIEnabled)
        {
            SetPolicyState(regiment, false, 0f, "AI_OFF");
            return;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            SetPolicyState(regiment, false, 0f, "NO_DESTINATION");
            return;
        }

        Vector3 destination = (Vector3)destinationField.GetValue(regiment);
        float destinationDistance = PlanarDistance(regiment.transform.position, destination);
        if (destinationDistance <= 3.5f)
        {
            SetPolicyState(regiment, false, destinationDistance, "DESTINATION_NEAR");
            return;
        }

        Regiment nearestEnemy = FindNearestEnemy(regiment, battle);
        float enemyDistance = nearestEnemy != null
            ? PlanarDistance(regiment.transform.position, nearestEnemy.transform.position)
            : float.PositiveInfinity;

        // Attack missions remain in marching column until roughly one effective
        // range plus a small deployment buffer from contact. The existing OfficerAI
        // and frontage planner are then free to deploy Line for the final approach.
        float deployDistance = regiment.EffectiveRange + DeploymentBuffer;

        bool attackMission =
            ai.Mission == OfficerAIMission.AttackNearest ||
            ai.Mission == OfficerAIMission.AttackTarget;

        bool moveMission = ai.Mission == OfficerAIMission.MoveToPoint;

        bool shouldMarchColumn =
            (attackMission && enemyDistance > deployDistance) ||
            (moveMission && destinationDistance > MinimumWaypointColumnDistance &&
             enemyDistance > regiment.EffectiveRange);

        if (!shouldMarchColumn)
        {
            SetPolicyState(regiment, false, enemyDistance, "DEPLOYMENT_ZONE");
            return;
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        SetPolicyState(regiment, true, enemyDistance, attackMission ? "ATTACK_APPROACH" : "MOVE_APPROACH");
    }

    private void SetPolicyState(
        Regiment regiment,
        bool active,
        float distance,
        string reason)
    {
        bool previous;
        if (marchColumnActive.TryGetValue(regiment, out previous) && previous == active)
            return;

        marchColumnActive[regiment] = active;

        Debug.Log(string.Format(
            "FORMATION-09H3|Unit={0}|MarchColumn={1}|Formation={2}|Distance={3:0.0}|Reason={4}",
            regiment.RegimentName,
            active,
            regiment.Formation,
            distance,
            reason));
    }

    private static Regiment FindNearestEnemy(Regiment regiment, BattleManager battle)
    {
        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null ||
                candidate == regiment ||
                candidate.Team == regiment.Team ||
                candidate.IsRouted ||
                candidate.CurrentStrength <= 0)
            {
                continue;
            }

            float distance = PlanarDistance(regiment.transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
