using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f25 melee core, hardened by v00.00.09f29n.
// Normal melee may start on collider contact, but an explicit CHARGE is allowed to drive
// deeper into the hostile footprint before both formations are stopped. This removes the
// visible no-man's-land that previously appeared between charging companies.
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
    private const float ChargeDeepContactDistance = 2.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMeleeCombatManager>() != null)
            return;

        GameObject root = new GameObject("PrototypeMeleeCombatManager_v009f25");
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

        Debug.Log("MELEE-DIAG|Installed=True|Version=09f29n|Type=InfantryBayonetContact|Pulse=1.25s|ChargeDeepContact=" +
                  ChargeDeepContactDistance.ToString("0.0") + "m|ChargeMomentum=True|Sector=True");
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

                float distance = PlanarDistance(a.transform.position, b.transform.position);
                bool explicitCharge = IsCharging(a) || IsCharging(b);

                // Collider overlap begins while the visual lines are still several metres
                // apart. During an explicit charge do not let that early overlap stop the
                // attacker; allow it to penetrate into the enemy footprint first.
                if (explicitCharge && distance > ChargeDeepContactDistance)
                    continue;

                if (!AreInContact(a, b))
                    continue;

                // Contact becomes bayonet/melee combat rather than two formations
                // continuing to walk through one another or firing volleys point blank.
                a.OrderHold();
                b.OrderHold();
                FaceOpponent(a, b);
                FaceOpponent(b, a);

                // Regiment.Update runs earlier in the frame. Keeping the next volley in
                // the future every melee frame suppresses musket fire for both sides while
                // bayonet contact is active without destroying their chosen fire policy.
                nextFireTimeField.SetValue(a, Time.time + 1.0f);
                nextFireTimeField.SetValue(b, Time.time + 1.0f);

                string key = BuildPairKey(a, b);
                if (!nextPulseByPair.TryGetValue(key, out float nextPulse))
                {
                    nextPulseByPair[key] = Time.time + 0.20f;
                    Debug.Log(string.Format(
                        "MELEE-DIAG|Contact=True|A={0}|B={1}|Distance={2:0.0}|ExplicitCharge={3}|ChargeA={4}|ChargeB={5}",
                        a.RegimentName,
                        b.RegimentName,
                        distance,
                        explicitCharge,
                        HasChargeMomentum(a),
                        HasChargeMomentum(b)));
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
        bool chargeA = HasChargeMomentum(a);
        bool chargeB = HasChargeMomentum(b);
        string sectorA = ContactSector(a, b);
        string sectorB = ContactSector(b, a);

        ApplyMeleeLosses(a, lossA);
        ApplyMeleeLosses(b, lossB);

        Debug.Log(string.Format(
            "MELEE-LOG|A={0}|B={1}|Distance={2:0.0}|LossA={3}|LossB={4}|StrengthA={5}->{6}|StrengthB={7}->{8}|MoraleA={9:0}|MoraleB={10:0}|CohA={11:0}|CohB={12:0}|ChargeA={13}|ChargeB={14}|AttackerSectorOnA={15}|AttackerSectorOnB={16}",
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
            b.Cohesion,
            chargeA,
            chargeB,
            sectorA,
            sectorB));
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

        string sector = ContactSector(defender, attacker);
        float sectorMultiplier = sector == "REAR" ? 1.35f : sector == "FLANK" ? 1.18f : 1.00f;

        bool chargeMomentum = HasChargeMomentum(attacker);
        float chargeMultiplier = chargeMomentum ? 1.22f : 1.00f;

        // A steady Line meeting a frontal bayonet charge blunts much of the initial
        // momentum. Flank/rear contact retains the full charge advantage.
        if (chargeMomentum && sector == "FRONT" &&
            defender.Formation == RegimentFormation.Line &&
            defender.Cohesion >= 70f && defender.Morale >= 60f)
        {
            chargeMultiplier *= 0.86f;
        }

        expected *= sectorMultiplier * chargeMultiplier;
        expected *= Random.Range(0.72f, 1.28f);

        return Mathf.Clamp(Mathf.RoundToInt(expected), 1, 12);
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
        // Only HARD tactical blockers invalidate melee contact. Trees/fence posts are
        // not treated as walls for a company-scale bayonet contact.
        if (HardObstacleBetween(a.transform.position, b.transform.position))
            return false;

        BoxCollider boxA = a.GetComponent<BoxCollider>();
        BoxCollider boxB = b.GetComponent<BoxCollider>();

        if (boxA != null && boxB != null && boxA.bounds.Intersects(boxB.bounds))
            return true;

        return PlanarDistance(a.transform.position, b.transform.position) <= ContactDistance;
    }

    private static bool HardObstacleBetween(Vector3 a, Vector3 b)
    {
        float distance = PlanarDistance(a, b);
        int samples = Mathf.Clamp(Mathf.CeilToInt(distance / 1.5f), 2, 12);

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(a, b, t);

            float riverX = PrototypeBootstrap.StreamCenterX(p.z);
            bool bridge = Mathf.Abs(p.z - 22f) <= 8f;
            if (Mathf.Abs(p.x - riverX) <= 4.7f && !bridge)
                return true;

            Vector3 probe = new Vector3(
                p.x,
                PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 1.0f,
                p.z);
            Collider[] hits = Physics.OverlapSphere(probe, 0.8f);
            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;
                string name = hit.gameObject.name;
                string root = hit.transform.root != null ? hit.transform.root.name : string.Empty;
                if (name.Contains("Farmhouse") || name.Contains("Barn") ||
                    root.Contains("Farmhouse") || root.Contains("Barn"))
                    return true;
            }
        }

        return false;
    }

    private static bool IsCharging(Regiment regiment)
    {
        return PrototypeInfantryCharge09F25.Instance != null &&
               PrototypeInfantryCharge09F25.Instance.IsCharging(regiment);
    }

    private static bool HasChargeMomentum(Regiment regiment)
    {
        return PrototypeInfantryCharge09F25.Instance != null &&
               PrototypeInfantryCharge09F25.Instance.HasChargeMomentum(regiment);
    }

    private static string ContactSector(Regiment defender, Regiment attacker)
    {
        if (defender == null || attacker == null)
            return "FRONT";

        Vector3 toAttacker = attacker.transform.position - defender.transform.position;
        toAttacker.y = 0f;
        if (toAttacker.sqrMagnitude < 0.01f)
            return "FRONT";
        toAttacker.Normalize();

        Vector3 forward = defender.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        float dot = Vector3.Dot(forward, toAttacker);
        if (dot >= 0.50f)
            return "FRONT";
        if (dot <= -0.50f)
            return "REAR";
        return "FLANK";
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
