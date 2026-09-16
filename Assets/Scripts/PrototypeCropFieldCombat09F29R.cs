using System.Reflection;
using UnityEngine;

// v00.00.09f29r
// Applies crop-field concealment after the F14 tuning manager has written the normal
// per-frame baseAccuracy, but before Regiment.Update resolves a volley. This keeps the
// terrain modifier additive and avoids creating a second combat resolver.
[DefaultExecutionOrder(-8000)]
public sealed class PrototypeCropFieldCombat09F29R : MonoBehaviour
{
    private FieldInfo baseAccuracyField;
    private FieldInfo forcedTargetField;
    private FieldInfo nextFireTimeField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCropFieldCombat09F29R>() == null)
            new GameObject("PrototypeCropFieldCombat_v000009f29r")
                .AddComponent<PrototypeCropFieldCombat09F29R>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        baseAccuracyField = typeof(Regiment).GetField("baseAccuracy", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);

        if (baseAccuracyField == null || nextFireTimeField == null)
        {
            Debug.LogError("CROP-COMBAT-09F29R|Installed=False|Reason=CombatReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "CROP-COMBAT-09F29R|Installed=True|ClosePenalty~2%|MediumPenalty~5%|" +
            "LongPenalty~10%|AcquisitionRangeReduction=True|BallisticCover=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || baseAccuracyField == null)
            return;

        foreach (Regiment shooter in battle.Regiments)
        {
            if (shooter == null || shooter.IsRouted || shooter.FirePolicy == RegimentFirePolicy.HoldFire)
                continue;

            Regiment target = ResolveReferenceTarget(shooter, battle);
            if (target == null)
                continue;

            float distance = PlanarDistance(shooter.transform.position, target.transform.position);
            float visibility = PrototypeCropFieldTerrain09F29R.GetVisibilityRangeMultiplier(target);

            // A concealed target is not literally invisible, but a formation will normally
            // wait until it has a clearer target picture before opening fire. Only delay a
            // volley that is otherwise ready; never alter reload cadence already in progress.
            float triggerRange = shooter.GetFireTriggerRange();
            if (visibility < 0.999f && triggerRange > 0f && distance > triggerRange * visibility)
            {
                object nextRaw = nextFireTimeField.GetValue(shooter);
                if (nextRaw is float nextFire && Time.time >= nextFire)
                    nextFireTimeField.SetValue(shooter, Time.time + 0.35f);
                continue;
            }

            float multiplier = GetCombinedTargetMultiplier(shooter, target, distance);
            if (multiplier >= 0.999f)
                continue;

            object raw = baseAccuracyField.GetValue(shooter);
            if (raw is float accuracy)
                baseAccuracyField.SetValue(shooter, accuracy * multiplier);
        }
    }

    public static float GetCombinedTargetMultiplier(Regiment shooter, Regiment target, float distance)
    {
        float hit = PrototypeCropFieldTerrain09F29R.GetTargetAccuracyMultiplier(shooter, target, distance);
        if (hit >= 0.999f || shooter == null || target == null)
            return hit;

        // Long-range observation through high crop is additionally less certain.
        // Keep this small: it represents target acquisition/readability, not hard cover.
        if (distance > shooter.EffectiveRange)
        {
            float visibility = PrototypeCropFieldTerrain09F29R.GetVisibilityRangeMultiplier(target);
            float acquisition = Mathf.Lerp(1f, visibility, 0.25f);
            hit *= acquisition;
        }

        return Mathf.Clamp(hit, 0.70f, 1f);
    }

    private Regiment ResolveReferenceTarget(Regiment shooter, BattleManager battle)
    {
        if (shooter == null)
            return null;

        if (forcedTargetField != null)
        {
            Regiment forced = forcedTargetField.GetValue(shooter) as Regiment;
            if (forced != null && !forced.IsRouted && forced.Team != shooter.Team)
                return forced;
        }

        Regiment nearest = null;
        float best = shooter.GetFireTriggerRange();
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == shooter || candidate.Team == shooter.Team || candidate.IsRouted)
                continue;

            float distance = PlanarDistance(shooter.transform.position, candidate.transform.position);
            if (distance > best || !shooter.IsTargetInFireArc(candidate))
                continue;

            best = distance;
            nearest = candidate;
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