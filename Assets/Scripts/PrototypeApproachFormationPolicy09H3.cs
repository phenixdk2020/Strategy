using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30a: pre-contact deployment hardening.
// Moving AI companies may march in Column while safely outside hostile long range, but
// must START the physical Column -> Line reform far enough outside hostile MaximumRange
// that the company is battle-ready before it enters the enemy firing envelope.
// This protection also applies while a Major owns physical movement: higher command keeps
// movement authority, while this policy performs the safety-critical formation change only.
[DefaultExecutionOrder(1350)]
public sealed class PrototypeApproachFormationPolicy09H3 : MonoBehaviour
{
    private readonly Dictionary<Regiment, bool> marchColumnActive =
        new Dictionary<Regiment, bool>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float MinimumWaypointColumnDistance = 16f;

    // F29J used +10 m. Field QA showed that this only began the visual reform shortly
    // before contact. F30A gives roughly 10 seconds of normal-march distance so the
    // physical three-rank Line can settle BEFORE hostile Long/MaximumRange is crossed.
    private const float EnemyLongRangeDeploymentBuffer = 35f;
    private const float ColumnReentryHysteresis = 24f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeApproachFormationPolicy09H3>() != null)
            return;

        GameObject root = new GameObject("PrototypeApproachFormationPolicy_v000009f30a");
        root.AddComponent<PrototypeApproachFormationPolicy09H3>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        destinationField = typeof(Regiment).GetField("destination", flags);

        if (hasDestinationField == null || destinationField == null)
        {
            Debug.LogWarning("FORMATION-09F30A|Installed=False|Reason=RegimentMovementFieldsNotFound");
            enabled = false;
            return;
        }

        Debug.Log(
            "FORMATION-09F30A|Installed=True|Policy=DeployAndSettleBeforeEnemyLongRange|" +
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

        float hostileLongRange = nearestEnemy != null
            ? nearestEnemy.MaximumRange
            : regiment.MaximumRange;
        float deployDistance = hostileLongRange + EnemyLongRangeDeploymentBuffer;

        // Major/F27 still owns the destination. We only force the formation to begin
        // deploying early; we never replace the mission goal or route here.
        if (!ai.enabled)
        {
            if (hasDestination && nearestEnemy != null && enemyDistance <= deployDistance)
            {
                DeployLine(regiment);
                SetPolicyState(regiment, false, enemyDistance, "HIGHER_COMMAND_EARLY_PRECONTACT_DEPLOY");
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
            SetPolicyState(regiment, false, enemyDistance, "DEPLOY_BEFORE_HOSTILE_LONG_RANGE");
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
            "FORMATION-09F30A|Unit={0}|MarchColumn={1}|Formation={2}|Distance={3:0.0}|Reason={4}",
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
