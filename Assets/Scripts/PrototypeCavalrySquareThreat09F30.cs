using System.Reflection;
using UnityEngine;

// v00.00.09f30, threat behaviour hardened in F30N.
// Bridges the existing F29 mounted-threat Square logic to the new F30 cavalry units.
// F30N also lets AI infantry assess nearby screening/maneuvering cavalry before contact;
// F29's public API was originally Denmark-only because no real enemy cavalry existed yet.
// This layer keeps that API for Danish infantry and mirrors the same score for Prussian
// TEST infantry so Danish cavalry can exercise automatic Square response too.
[DefaultExecutionOrder(35950)]
public sealed class PrototypeCavalrySquareThreat09F30 : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float MaxThreatDistance = 220f;
    private const float AutoSquareThreshold = 62f;

    private MethodInfo enterSquareMethod;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalrySquareThreat09F30>() == null)
            new GameObject("PrototypeCavalrySquareThreat_v000009f30")
                .AddComponent<PrototypeCavalrySquareThreat09F30>();
    }

    private void Awake()
    {
        enterSquareMethod = typeof(PrototypeInfantrySquare09F29)
            .GetMethod("EnterSquare", PrivateInstance);

        Debug.Log(
            "CAVALRY-SQUARE-09F30|Installed=True|ThreatDistance=220|" +
            "AutoThreshold=62|SymmetricTestBridge=True");
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

            if (target.Team == BattleTeam.Denmark)
            {
                PrototypeInfantrySquare09F29.ReportMountedThreat(
                    target,
                    unit.transform.position,
                    distance,
                    closingSpeed,
                    relativeStrength,
                    charging);
                continue;
            }

            OfficerAIController controller =
                target.GetComponent<OfficerAIController>();
            if (controller == null ||
                !controller.AIEnabled ||
                PrototypeInfantrySquare09F29.IsInSquare(target))
                continue;

            float proximity =
                1f - Mathf.Clamp01(distance / MaxThreatDistance);

            float score =
                (charging ? 38f : 10f) +
                proximity * 34f +
                Mathf.Clamp(closingSpeed, 0f, 12f) * 2.1f +
                Mathf.Clamp(relativeStrength, 0.25f, 2f) * 9f;

            if (score < AutoSquareThreshold ||
                enterSquareMethod == null ||
                PrototypeInfantrySquare09F29.Instance == null)
                continue;

            enterSquareMethod.Invoke(
                PrototypeInfantrySquare09F29.Instance,
                new object[]
                {
                    target,
                    true,
                    unit.transform.position
                });

            Debug.Log(
                "CAVALRY-SQUARE-09F30N|Target=" +
                target.RegimentName +
                "|Team=Prussia|Score=" +
                score.ToString("0") +
                "|Distance=" +
                distance.ToString("0") +
                "|Charging=" +
                charging +
                "|Action=FORM_SQUARE");
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
            if (committedDistance <= MaxThreatDistance)
                return committed;
        }

        for (int i = 0; i < battle.Regiments.Count; i++)
        {
            Regiment candidate = battle.Regiments[i];
            if (candidate == null ||
                candidate.IsRouted ||
                candidate.CurrentStrength <= 0 ||
                candidate.Team != BattleTeam.Prussia)
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
