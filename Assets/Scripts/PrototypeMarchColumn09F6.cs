using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f6 unified movement-formation policy, tightened by v00.00.09f10.
// Long movement may use Column only outside enemy Long/MaximumRange. Once an enemy
// is inside Long range the company must remain/deploy in Line, except when a river
// crossing is actively owned by the bridge router, which temporarily requires Column.
[DefaultExecutionOrder(1700)]
public sealed class PrototypeMarchColumn09F6 : MonoBehaviour
{
    private readonly HashSet<Regiment> autoColumn = new HashSet<Regiment>();

    private FieldInfo hasDestinationField;
    private FieldInfo destinationField;

    private const float EnterColumnDestinationDistance = 28f;

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
            Debug.LogError("MARCH-09F10|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "MARCH-09F10|Installed=True|Policy=ColumnOnlyOutsideEnemyLongRange|" +
            "DestinationEnter=" + EnterColumnDestinationDistance.ToString("0") +
            "m|EnemyDeploy=DynamicMaximumRange|BridgeException=True");
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
        // Bridge routing owns formation while a legal river crossing is active.
        // This is the only exception to the "enemy inside Long => stay in Line" rule.
        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(regiment))
        {
            autoColumn.Add(regiment);
            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);
            return;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            if (autoColumn.Contains(regiment))
            {
                EnsureLine(regiment, "FINAL_STOP", float.PositiveInfinity);
                autoColumn.Remove(regiment);
            }
            return;
        }

        Vector3 destination = (Vector3)destinationField.GetValue(regiment);
        float destinationDistance = PlanarDistance(regiment.transform.position, destination);
        float enemyDistance = FindNearestEnemyDistance(regiment, battle);
        float combatDeploymentDistance = Mathf.Max(1f, regiment.MaximumRange);

        // v09f10 hard combat-formation rule:
        // if any valid enemy is already inside Long/MaximumRange, never enter Column.
        // This also covers switching fire policy LONG -> MEDIUM before an ATTACK order.
        if (enemyDistance <= combatDeploymentDistance)
        {
            if (regiment.Formation != RegimentFormation.Line || autoColumn.Contains(regiment))
                EnsureLine(regiment, "ENEMY_INSIDE_LONG", enemyDistance);

            autoColumn.Remove(regiment);
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
            "MARCH-09F10|Unit=" + regiment.RegimentName +
            "|Column=True|Destination=" + destinationDistance.ToString("0.0") +
            "|Enemy=" + FormatDistance(enemyDistance) +
            "|LongRange=" + combatDeploymentDistance.ToString("0.0") +
            "|Reason=LONG_MOVE_OUTSIDE_COMBAT_RANGE");
    }

    private static void EnsureLine(Regiment regiment, string reason, float enemyDistance)
    {
        if (regiment.Formation != RegimentFormation.Line)
            regiment.SetFormation(RegimentFormation.Line);

        Debug.Log(
            "MARCH-09F10|Unit=" + regiment.RegimentName +
            "|Column=False|Enemy=" + FormatDistance(enemyDistance) +
            "|LongRange=" + regiment.MaximumRange.ToString("0.0") +
            "|Reason=" + reason);
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
