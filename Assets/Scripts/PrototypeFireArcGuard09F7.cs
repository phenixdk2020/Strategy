using System.Collections.Generic;
using UnityEngine;

// v00.00.09f7 live-fire guard for the current 1v1 prototype.
// Regiment's legacy fire arc is wider than the new visual cone; this guard temporarily
// holds fire when the nearest in-range enemy is outside the new +/-35 degree sector.
[DefaultExecutionOrder(-50)]
public sealed class PrototypeFireArcGuard09F7 : MonoBehaviour
{
    private readonly Dictionary<Regiment, RegimentFirePolicy> restorePolicies =
        new Dictionary<Regiment, RegimentFirePolicy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeFireArcGuard09F7>() != null)
            return;

        GameObject root = new GameObject("PrototypeFireArcGuard_v000009f7");
        root.AddComponent<PrototypeFireArcGuard09F7>();
    }

    private void Update()
    {
        restorePolicies.Clear();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted || regiment.FirePolicy == RegimentFirePolicy.HoldFire)
                continue;

            Regiment nearest = FindNearestEnemyInTriggerRange(regiment, battle);
            if (nearest == null)
                continue;

            float angle = PlanarAngle(regiment.transform.forward, nearest.transform.position - regiment.transform.position);
            if (angle <= PrototypeRangeTuning09F7.FireArcHalfAngleDegrees)
                continue;

            restorePolicies[regiment] = regiment.FirePolicy;
            regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
        }
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Regiment, RegimentFirePolicy> pair in restorePolicies)
        {
            if (pair.Key != null && !pair.Key.IsRouted)
                pair.Key.SetFirePolicy(pair.Value);
        }
        restorePolicies.Clear();
    }

    private static Regiment FindNearestEnemyInTriggerRange(Regiment regiment, BattleManager battle)
    {
        Regiment nearest = null;
        float best = regiment.GetFireTriggerRange();

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
            if (distance <= best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static float PlanarAngle(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        if (a.sqrMagnitude < 0.001f || b.sqrMagnitude < 0.001f)
            return 0f;
        return Vector3.Angle(a.normalized, b.normalized);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
