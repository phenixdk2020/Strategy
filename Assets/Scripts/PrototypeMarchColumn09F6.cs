using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f6 unified movement-formation policy.
// Applies to both player-routed and AI-routed units: long movement -> Column,
// then deploys back to Line at the final stop or when approaching enemy contact.
// Temporary river/bridge steering destinations do NOT force a premature Line deployment.
[DefaultExecutionOrder(1700)]
public sealed class PrototypeMarchColumn09F6 : MonoBehaviour
{
    private readonly HashSet<Regiment> autoColumn = new HashSet<Regiment>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float EnterColumnDestinationDistance = 28f;
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
            "m|EnemyDeploy=" + DeployLineEnemyDistance.ToString("0") +
            "m|BridgeSteeringPreservesColumn=True");
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
                if (regiment.Formation != RegimentFormation.Line)
                    regiment.SetFormation(RegimentFormation.Line);

                autoColumn.Remove(regiment);
                Debug.Log("MARCH-09F6|Unit=" + regiment.RegimentName + "|Column=False|Reason=FINAL_STOP");
            }
            return;
        }

        Vector3 destination = (Vector3)destinationField.GetValue(regiment);
        float destinationDistance = PlanarDistance(regiment.transform.position, destination);
        float enemyDistance = FindNearestEnemyDistance(regiment, battle);

        // Enemy proximity is the tactical deployment trigger. We intentionally do not
        // use proximity to the current steering destination because the river system
        // temporarily substitutes bridge entry/exit points for the real final goal.
        if (enemyDistance <= DeployLineEnemyDistance)
        {
            if (autoColumn.Contains(regiment))
            {
                if (regiment.Formation != RegimentFormation.Line)
                    regiment.SetFormation(RegimentFormation.Line);

                autoColumn.Remove(regiment);
                Debug.Log(
                    "MARCH-09F6|Unit=" + regiment.RegimentName +
                    "|Column=False|Enemy=" + enemyDistance.ToString("0.0") +
                    "|Reason=ENEMY_CONTACT_ZONE");
            }
            return;
        }

        if (autoColumn.Contains(regiment))
        {
            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);
            return;
        }

        if (destinationDistance < EnterColumnDestinationDistance)
            return;

        autoColumn.Add(regiment);
        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        Debug.Log(
            "MARCH-09F6|Unit=" + regiment.RegimentName +
            "|Column=True|Destination=" + destinationDistance.ToString("0.0") +
            "|Enemy=" + FormatDistance(enemyDistance) +
            "|Reason=LONG_MOVE");
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
