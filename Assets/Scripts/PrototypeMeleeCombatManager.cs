using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(1500)]
public sealed class PrototypeMeleeCombatManager : MonoBehaviour
{
    private readonly Dictionary<string, float> nextPulseByPair =
        new Dictionary<string, float>();

    private FieldInfo currentStrengthField;
    private FieldInfo moraleField;
    private FieldInfo cohesionField;
    private FieldInfo underFireTimerField;
    private FieldInfo nextFireTimeField;
    private MethodInfo refreshVisualStrengthMethod;
    private MethodInfo routeMethod;
    private bool ready;

    private const float PulseInterval = 1.25f;
    private const float ContactDistance = 7.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMeleeCombatManager>() != null)
            return;

        GameObject root = new GameObject("PrototypeMeleeCombatManager_v009");
        root.AddComponent<PrototypeMeleeCombatManager>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        currentStrengthField = typeof(Regiment).GetField("<CurrentStrength>k__BackingField", flags);
        moraleField = typeof(Regiment).GetField("<Morale>k__BackingField", flags);
        cohesionField = typeof(Regiment).GetField("<Cohesion>k__BackingField", flags);
        underFireTimerField = typeof(Regiment).GetField("underFireTimer", flags);
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);
        refreshVisualStrengthMethod = typeof(Regiment).GetMethod("RefreshVisualStrength", flags);
        routeMethod = typeof(Regiment).GetMethod("Route", flags);

        ready =
            currentStrengthField != null &&
            moraleField != null &&
            cohesionField != null &&
            underFireTimerField != null &&
            nextFireTimeField != null &&
            refreshVisualStrengthMethod != null &&
            routeMethod != null;

        if (!ready)
        {
            Debug.LogError("MELEE-DIAG|Init=Failed|Reason=Required Regiment fields/methods not found");
            enabled = false;
            return;
        }

        Debug.Log("MELEE-DIAG|Installed=True|Type=InfantryBayonetContact|Pulse=1.25s");
    }

    private void Update()
    {
        if (!ready || BattleManager.Instance == null || BattleManager.Instance.Regiments == null)
            return;

        IReadOnlyList<Regiment> regiments = BattleManager.Instance.Regiments;

        for (int i = 0; i < regiments.Count; i++)
        {
            Regiment a = regiments[i];
            if (!CanMelee(a))
                continue;

            for (int j = i + 1; j < regiments.Count; j++)
            {
                Regiment b = regiments[j];
                if (!CanMelee(b) || a.Team == b.Team)
                    continue;

                if (!AreInContact(a, b))
                    continue;

                // Contact becomes bayonet/melee combat rather than two formations
                // continuing to walk through one another or firing volleys point blank.
                a.OrderHold();
                b.OrderHold();
                FaceOpponent(a, b);
                FaceOpponent(b, a);

                nextFireTimeField.SetValue(a, Time.time + 0.40f);
                nextFireTimeField.SetValue(b, Time.time + 0.40f);

                string key = BuildPairKey(a, b);
                if (!nextPulseByPair.TryGetValue(key, out float nextPulse))
                {
                    nextPulseByPair[key] = Time.time + 0.20f;
                    Debug.Log(string.Format(
                        "MELEE-DIAG|Contact=True|A={0}|B={1}|Distance={2:0.0}",
                        a.RegimentName,
                        b.RegimentName,
                        PlanarDistance(a.transform.position, b.transform.position)));
                    continue;
                }

                if (Time.time < nextPulse)
                    continue;

                ResolveMeleePulse(a, b);
                nextPulseByPair[key] = Time.time + PulseInterval;
            }
        }
    }

    private void ResolveMeleePulse(Regiment a, Regiment b)
    {
        if (!CanMelee(a) || !CanMelee(b))
            return;

        int lossA = CalculateLosses(defender: a, attacker: b);
        int lossB = CalculateLosses(defender: b, attacker: a);

        int beforeA = a.CurrentStrength;
        int beforeB = b.CurrentStrength;

        ApplyMeleeLosses(a, lossA);
        ApplyMeleeLosses(b, lossB);

        Debug.Log(string.Format(
            "MELEE-LOG|A={0}|B={1}|Distance={2:0.0}|LossA={3}|LossB={4}|StrengthA={5}->{6}|StrengthB={7}->{8}|MoraleA={9:0}|MoraleB={10:0}|CohA={11:0}|CohB={12:0}",
            a.RegimentName,
            b.RegimentName,
            PlanarDistance(a.transform.position, b.transform.position),
            lossA,
            lossB,
            beforeA,
            a.CurrentStrength,
            beforeB,
            b.CurrentStrength,
            a.Morale,
            b.Morale,
            a.Cohesion,
            b.Cohesion));
    }

    private static int CalculateLosses(Regiment defender, Regiment attacker)
    {
        float strengthMass = Mathf.Clamp(attacker.CurrentStrength / 600f, 0.25f, 1.20f);
        float attackQuality = Mathf.Clamp01(
            (attacker.Morale * 0.35f +
             attacker.Cohesion * 0.35f +
             attacker.Experience * 0.30f) / 100f);

        float defenderStability = Mathf.Lerp(0.72f, 1.12f, defender.Cohesion / 100f);
        float expected = 2.2f + 4.2f * strengthMass * attackQuality / defenderStability;
        expected *= Random.Range(0.72f, 1.28f);

        return Mathf.Clamp(Mathf.RoundToInt(expected), 1, 10);
    }

    private void ApplyMeleeLosses(Regiment regiment, int requestedLosses)
    {
        if (regiment == null || regiment.IsRouted)
            return;

        int current = regiment.CurrentStrength;
        int losses = Mathf.Clamp(requestedLosses, 0, current);
        int remaining = current - losses;

        float newMorale = Mathf.Max(
            0f,
            regiment.Morale - 1.8f - losses * 0.28f);

        float newCohesion = Mathf.Max(
            0f,
            regiment.Cohesion - 1.4f - losses * 0.22f);

        currentStrengthField.SetValue(regiment, remaining);
        moraleField.SetValue(regiment, newMorale);
        cohesionField.SetValue(regiment, newCohesion);
        underFireTimerField.SetValue(regiment, 7f);
        refreshVisualStrengthMethod.Invoke(regiment, null);

        if (remaining <= 0 ||
            newMorale <= 17f ||
            remaining <= regiment.InitialStrength * 0.24f)
        {
            routeMethod.Invoke(regiment, null);
        }
    }

    private static bool CanMelee(Regiment regiment)
    {
        return regiment != null &&
               !regiment.IsRouted &&
               regiment.CurrentStrength > 0;
    }

    private static bool AreInContact(Regiment a, Regiment b)
    {
        BoxCollider boxA = a.GetComponent<BoxCollider>();
        BoxCollider boxB = b.GetComponent<BoxCollider>();

        if (boxA != null && boxB != null && boxA.bounds.Intersects(boxB.bounds))
            return true;

        return PlanarDistance(a.transform.position, b.transform.position) <= ContactDistance;
    }

    private static void FaceOpponent(Regiment regiment, Regiment opponent)
    {
        Vector3 direction = opponent.transform.position - regiment.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        regiment.transform.rotation = Quaternion.Slerp(
            regiment.transform.rotation,
            desired,
            9f * Time.deltaTime);
    }

    private static string BuildPairKey(Regiment a, Regiment b)
    {
        string first = a.RegimentName;
        string second = b.RegimentName;

        if (string.CompareOrdinal(first, second) <= 0)
            return first + "||" + second;

        return second + "||" + first;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
