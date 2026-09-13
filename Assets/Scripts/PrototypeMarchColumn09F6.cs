using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f6 unified movement-formation policy.
// Applies to both player-routed and AI-routed units: long movement -> Column,
// then deploys back to Line near the destination or enemy contact.
[DefaultExecutionOrder(1700)]
public sealed class PrototypeMarchColumn09F6 : MonoBehaviour
{
    private readonly HashSet<Regiment> autoColumn = new HashSet<Regiment>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float EnterColumnDestinationDistance = 28f;
    private const float DeployLineDestinationDistance = 16f;
    private const float DeployLineEnemyDistance = 70f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMarchColumn09F6>() != null)
            return;

        GameObject root = new GameObject("PrototypeMarchColumn_v000009f6");
        root.AddComponent<PrototypeMarchColumn09F6>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        destinationField = typeof(Regiment).GetField("destination", flags);

        if (hasDestinationField == null || destinationField == null)
        {
            Debug.LogError("MARCH-09F6|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "MARCH-09F6|Installed=True|Policy=LongMoveColumnThenLine|" +
            "DestinationEnter=" + EnterColumnDestinationDistance.ToString("0") +
            "m|DestinationDeploy=" + DeployLineDestinationDistance.ToString("0") +
            "m|EnemyDeploy=" + DeployLineEnemyDistance.ToString("0") + "m");
    }

    private void Update()
    {
        PrototypeApproachFormationPolicy09H3 oldPolicy =
            UnityEngine.Object.FindAnyObjectByType<PrototypeApproachFormationPolicy09H3>();
        if (oldPolicy != null && oldPolicy.enabled)
            oldPolicy.enabled = false;

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
        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            if (autoColumn.Contains(regiment))
            {
                regiment.SetFormation(RegimentFormation.Line);
                autoColumn.Remove(regiment);
                Debug.Log("MARCH-09F6|Unit=" + regiment.RegimentName + "|Column=False|Reason=STOPPED");
            }
            return;
        }

        Vector3 destination = (Vector3)destinationField.GetValue(regiment);
        float destinationDistance = PlanarDistance(regiment.transform.position, destination);
        float enemyDistance = FindNearestEnemyDistance(regiment, battle);

        bool mustDeploy =
            destinationDistance <= DeployLineDestinationDistance ||
            enemyDistance <= DeployLineEnemyDistance;

        if (mustDeploy)
        {
            if (autoColumn.Contains(regiment))
            {
                if (regiment.Formation != RegimentFormation.Line)
                    regiment.SetFormation(RegimentFormation.Line);
                autoColumn.Remove(regiment);
                Debug.Log(
                    "MARCH-09F6|Unit=" + regiment.RegimentName +
                    "|Column=False|Destination=" + destinationDistance.ToString("0.0") +
                    "|Enemy=" + FormatDistance(enemyDistance) +
                    "|Reason=DEPLOY_LINE");
            }
            return;
        }

        if (destinationDistance < EnterColumnDestinationDistance)
            return;

        if (!autoColumn.Contains(regiment))
        {
            autoColumn.Add(regiment);
            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);

            Debug.Log(
                "MARCH-09F6|Unit=" + regiment.RegimentName +
                "|Column=True|Destination=" + destinationDistance.ToString("0.0") +
                "|Enemy=" + FormatDistance(enemyDistance) +
                "|Reason=LONG_MOVE");
            return;
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);
    }

    private static float FindNearestEnemyDistance(Regiment regiment, BattleManager battle)
    {
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
                best = distance;
        }

        return best;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static string FormatDistance(float distance)
    {
        return float.IsInfinity(distance) ? "INF" : distance.ToString("0.0");
    }
}
