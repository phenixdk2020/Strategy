using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29j: pre-contact deployment hardening.
// Moving AI companies may march in Column while safely outside hostile long range, but
// must deploy to Line before crossing an enemy's maximum firing range. This protection
// also applies while a Major owns physical movement: higher command keeps movement
// authority, while this policy is allowed to perform the safety-critical Column -> Line
// formation change only. It never writes a movement destination.
[DefaultExecutionOrder(1350)]
public sealed class PrototypeApproachFormationPolicy09H3 : MonoBehaviour
{
    private readonly Dictionary<Regiment, bool> marchColumnActive =
        new Dictionary<Regiment, bool>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float MinimumWaypointColumnDistance = 16f;
    private const float EnemyLongRangeDeploymentBuffer = 10f;
    private const float ColumnReentryHysteresis = 14f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeApproachFormationPolicy09H3>() != null)
            return;

        GameObject root = new GameObject("PrototypeApproachFormationPolicy_v000009f29j");
        root.AddComponent<PrototypeApproachFormationPolicy09H3>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        destinationField = typeof(Regiment).GetField("destination", flags);

        if (hasDestinationField == null || destinationField == null)
        {
            Debug.LogWarning("FORMATION-09F29J-AI|Installed=False|Reason=RegimentMovementFieldsNotFound");
            enabled = false;
            return;
        }

        Debug.Log(
            "FORMATION-09F29J-AI|Installed=True|Policy=MarchColumnDeployBeforeEnemyLongRange|" +
            "EnemyLongBuffer=" + EnemyLongRangeDeploymentBuffer.ToString("0") +
            "|FormationWritesOnly=True|MovementWrites=False|HigherCommandPreContactDeploy=True");
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
        // Square is a stronger tactical formation state and owns its own physical rules.
        // Never collapse an active square to Line/Column from the approach policy.
        if (PrototypeInfantrySquare09F29.IsInSquare(regiment))
        {
            SetPolicyState(regiment, false, 0f, "SQUARE_OWNS_FORMATION");
            return;
        }

        OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
        if (ai == null || !ai.AIEnabled)
        {
            SetPolicyState(regiment, false, 0f, "AI_OFF");
            return;
        }

        if (PrototypeUnderFireReaction09F26.IsReacting(regiment))
        {
            SetPolicyState(regiment, false, 0f, "UNDER_FIRE_REACTION_OWNS_FORMATION");
            return;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        Vector3 destination = hasDestination
            ? (Vector3)destinationField.GetValue(regiment)
            : regiment.transform.position;
        float destinationDistance = hasDestination
            ? PlanarDistance(regiment.transform.position, destination)
            : 0f;

        Regiment nearestEnemy = FindNearestEnemy(regiment, battle);
        float enemyDistance = nearestEnemy != null
            ? PlanarDistance(regiment.transform.position, nearestEnemy.transform.position)
            : float.PositiveInfinity;

        // Deploy against the ENEMY weapon envelope, not our own. A formation therefore
        // finishes changing out of march column before it crosses hostile LONG range.
        float hostileLongRange = nearestEnemy != null
            ? nearestEnemy.MaximumRange
            : regiment.MaximumRange;
        float deployDistance = hostileLongRange + EnemyLongRangeDeploymentBuffer;

        // F27 deliberately disables OfficerAIController while the Major owns physical
        // movement to a battalion/company slot. Movement authority remains with F27,
        // but formation safety must still be able to deploy the company before enemy fire.
        if (!ai.enabled)
        {
            if (hasDestination && nearestEnemy != null && enemyDistance <= deployDistance)
            {
                DeployLine(regiment);
                SetPolicyState(regiment, false, enemyDistance, "HIGHER_COMMAND_PRECONTACT_DEPLOY");
            }
            else
            {
                SetPolicyState(regiment, false, enemyDistance, "HIGHER_COMMAND_OWNS_MOVEMENT");
            }
            return;
        }

        if (!hasDestination)
        {
            DeployLine(regiment);
            SetPolicyState(regiment, false, enemyDistance, "NO_DESTINATION");
            return;
        }

        if (destinationDistance <= 3.5f)
        {
            DeployLine(regiment);
            SetPolicyState(regiment, false, destinationDistance, "DESTINATION_NEAR");
            return;
        }

        bool attackMission =
            ai.Mission == OfficerAIMission.AttackNearest ||
            ai.Mission == OfficerAIMission.AttackTarget;

        bool moveMission = ai.Mission == OfficerAIMission.MoveToPoint;

        // Hysteresis prevents Line <-> Column oscillation if the enemy or formation
        // hovers around the deployment threshold. Once deployed, a company needs a
        // clearly larger safety gap before it may reform march column again.
        bool wasMarchColumn = false;
        marchColumnActive.TryGetValue(regiment, out wasMarchColumn);
        float columnThreshold = deployDistance + (wasMarchColumn ? 0f : ColumnReentryHysteresis);

        bool shouldMarchColumn =
            (attackMission && enemyDistance > columnThreshold) ||
            (moveMission && destinationDistance > MinimumWaypointColumnDistance &&
             enemyDistance > columnThreshold);

        if (!shouldMarchColumn)
        {
            DeployLine(regiment);
            SetPolicyState(regiment, false, enemyDistance, "BEFORE_ENEMY_LONG_RANGE");
            return;
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        SetPolicyState(regiment, true, enemyDistance, attackMission ? "ATTACK_APPROACH" : "MOVE_APPROACH");
    }

    private static void DeployLine(Regiment regiment)
    {
        if (regiment != null &&
            !PrototypeInfantrySquare09F29.IsInSquare(regiment) &&
            regiment.Formation != RegimentFormation.Line)
        {
            regiment.SetFormation(RegimentFormation.Line);
        }
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
            "FORMATION-09F29J-AI|Unit={0}|MarchColumn={1}|Formation={2}|Distance={3:0.0}|Reason={4}",
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
