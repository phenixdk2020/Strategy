using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f9 combat QA layer.
// Keeps officer engagement-distance autonomy, raises TEST hit probability so
// Close/Medium/Long differences are easier to observe, and repairs the 09f7
// black-powder ParticleSystem velocity curves so all XYZ curves use one mode.
[DefaultExecutionOrder(36000)]
public sealed class PrototypeCombatQa09F9 : MonoBehaviour
{
    public const float DanishQaAccuracy = 0.050f;
    public const float PrussianQaAccuracy = 0.052f;

    // Unity 6.6 marks Object.GetInstanceID() obsolete at error level.
    // We only need to remember which particle systems have already been repaired,
    // so keep the actual component references instead of relying on an internal ID.
    private readonly HashSet<ParticleSystem> fixedParticleSystems =
        new HashSet<ParticleSystem>();

    private FieldInfo baseAccuracyField;
    private bool accuracyApplied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCombatQa09F9>() != null)
            return;

        GameObject root = new GameObject("PrototypeCombatQa_v000009f9");
        root.AddComponent<PrototypeCombatQa09F9>();
    }

    private void Awake()
    {
        baseAccuracyField = typeof(Regiment).GetField(
            "baseAccuracy",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "COMBAT-QA-09F9|Installed=True|OfficerRangeAutonomy=True|" +
            "DanishAccuracy=" + DanishQaAccuracy.ToString("0.000") +
            "|PrussianAccuracy=" + PrussianQaAccuracy.ToString("0.000") +
            "|ParticleVelocityRepair=True");
    }

    private void Update()
    {
        ApplyAccuracyOnce();
        RepairParticleVelocityCurves();
    }

    private void ApplyAccuracyOnce()
    {
        if (accuracyApplied || baseAccuracyField == null)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count == 0)
            return;

        int changed = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            float accuracy = regiment.Team == BattleTeam.Denmark
                ? DanishQaAccuracy
                : PrussianQaAccuracy;

            baseAccuracyField.SetValue(regiment, accuracy);
            changed++;
        }

        if (changed == 0)
            return;

        accuracyApplied = true;
        Debug.Log(
            "COMBAT-QA-09F9|AccuracyApplied=True|Units=" + changed +
            "|DistanceCurveStillActive=True|MoraleQAOff=True|CohesionStillActive=True");
    }

    private void RepairParticleVelocityCurves()
    {
        ParticleSystem[] systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>();
        if (systems == null || systems.Length == 0)
            return;

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null || fixedParticleSystems.Contains(ps))
                continue;

            if (!ps.name.Contains("BlackPowderSmoke09F7"))
                continue;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;

            // Unity requires x/y/z curves in Velocity over Lifetime to use the same mode.
            // Use TwoConstants for all three axes. X/Z stay at zero; Y keeps the
            // gentle randomized upward drift used for black-powder smoke.
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.10f, 0.45f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            fixedParticleSystems.Add(ps);
            Debug.Log(
                "PARTICLE-FIX-09F9|Object=" + ps.name +
                "|VelocityMode=TwoConstantsXYZ|Fixed=True");
        }
    }
}
