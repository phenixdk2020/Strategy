using UnityEngine;

// v00.00.09f30, threat behaviour hardened in F30N.
// Bridges the existing F29 mounted-threat Square logic to the new F30 cavalry units.
// F30N also lets AI infantry assess nearby screening/maneuvering cavalry before contact;
// F29's mounted-threat API is team-neutral from F30N, so this bridge only identifies
// the relevant threatened infantry unit and feeds the shared Square state machine.
[DefaultExecutionOrder(35950)]
public sealed class PrototypeCavalrySquareThreat09F30 : MonoBehaviour
{
    private const float MaxThreatDistance = 220f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalrySquareThreat09F30>() == null)
            new GameObject("PrototypeCavalrySquareThreat_v000009f30")
                .AddComponent<PrototypeCavalrySquareThreat09F30>();
    }

    private void Awake()
    {
        Debug.Log(
            "CAVALRY-SQUARE-09F30S|Installed=True|ThreatDistance=220|LOSRequired=True|" +
            "AutoThreshold=62|SymmetricThreatAPI=True|PersistentThreatRefresh=True");
    }

    private void Update()
    {
        PrototypeCavalryUnit09F30[] cavalry =
            UnityEngine.Object.FindObjectsByType<PrototypeCavalryUnit09F30>(
                FindObjectsInactive.Exclude);

        for (int i = 0; i < cavalry.Length; i++)
        {
            PrototypeCavalryUnit09F30 unit = cavalry[i];
            if (unit == null || unit.CurrentStrength <= 0)
                continue;

            Regiment target = FindNearestThreatenedInfantry(unit);
            if (target == null)
                continue;

            float distance =
                PlanarDistance(
                    unit.transform.position,
                    target.transform.position);
            if (distance > MaxThreatDistance)
                continue;

            bool charging =
                unit.Action == PrototypeCavalryAction09F30.Charge &&
                unit.ChargeTarget == target;

            float relativeStrength =
                unit.CurrentStrength /
                (float)Mathf.Max(1, target.CurrentStrength);

            float closingSpeed =
                charging
                    ? 12f
                    : unit.Action == PrototypeCavalryAction09F30.Move
                        ? 7f
                        : 0f;

            // F30N: the F29 mounted-threat API is now team-neutral, so the same
            // square state machine handles both sides and also refreshes LastThreatAt
            // while cavalry remains nearby.
            PrototypeInfantrySquare09F29.ReportMountedThreat(
                target,
                unit.transform.position,
                distance,
                closingSpeed,
                relativeStrength,
                charging);
        }
    }

    private static Regiment FindNearestThreatenedInfantry(
        PrototypeCavalryUnit09F30 cavalry)
    {
        BattleManager battle = BattleManager.Instance;
        if (cavalry == null ||
            battle == null ||
            battle.Regiments == null)
            return null;

        Regiment best = null;
        float bestDistance = MaxThreatDistance;

        // During a committed charge, that charge target has priority.
        Regiment committed = cavalry.ChargeTarget;
        if (committed != null &&
            !committed.IsRouted &&
            committed.CurrentStrength > 0)
        {
            float committedDistance =
                PlanarDistance(
                    cavalry.transform.position,
                    committed.transform.position);
            if (committedDistance <= MaxThreatDistance &&
                committed.CanSee(cavalry))
                return committed;
        }

        for (int i = 0; i < battle.Regiments.Count; i++)
        {
            Regiment candidate = battle.Regiments[i];
            if (candidate == null ||
                candidate.IsRouted ||
                candidate.CurrentStrength <= 0 ||
                candidate.Team != BattleTeam.Prussia ||
                !candidate.CanSee(cavalry))
                continue;

            float distance =
                PlanarDistance(
                    cavalry.transform.position,
                    candidate.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
