using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f14 B-271 lethality calibration.
// Regiment.FireVolley still contains the legacy 16-hit QA ceiling. This temporary
// calibration layer only supplements a volley when that ceiling was actually reached,
// so CLOSE tests can measure the requested ~20-30 hit region without disturbing
// uncapped MEDIUM/LONG salves. Remove this layer when the kernel hit resolver is replaced.
[DefaultExecutionOrder(37100)]
public sealed class PrototypeLethalityCalibration09F14 : MonoBehaviour
{
    private sealed class State
    {
        public float LastNextFireTime;
    }

    private readonly Dictionary<Regiment, State> states = new Dictionary<Regiment, State>();

    private FieldInfo nextFireTimeField;
    private FieldInfo forcedTargetField;

    private const int LegacyKernelHitCap = 16;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeLethalityCalibration09F14>() != null)
            return;

        GameObject root = new GameObject("PrototypeLethalityCalibration_v000009f14");
        root.AddComponent<PrototypeLethalityCalibration09F14>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);

        Debug.Log(
            "LETHALITY-09F14|Installed=True|LegacyKernelCap=16|" +
            "SupplementOnlyWhenCapReached=True|PrimaryLog=VOLLEY-CAL-09F14");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || nextFireTimeField == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment shooter in battle.Regiments)
        {
            if (shooter == null)
                continue;

            active.Add(shooter);
            float next = ReadNextFireTime(shooter);

            if (!states.TryGetValue(shooter, out State state))
            {
                states[shooter] = new State { LastNextFireTime = next };
                continue;
            }

            if (next > state.LastNextFireTime + 0.05f && next > Time.time)
                ResolveAndLog(shooter, battle);

            state.LastNextFireTime = next;
        }

        Cleanup(active);
    }

    private void ResolveAndLog(Regiment shooter, BattleManager battle)
    {
        Regiment target = FindVolleyTarget(shooter, battle);
        if (target == null)
        {
            Debug.Log(
                "VOLLEY-CAL-09F14|Shooter=" + shooter.RegimentName +
                "|Target=UNKNOWN|DistanceM=NA|Band=UNKNOWN|KernelHits=UNKNOWN|ExtraHits=0|TotalHits=UNKNOWN");
            return;
        }

        float distance = PlanarDistance(shooter.transform.position, target.transform.position);
        string band = PrototypeCombatTuningManager.GetRangeBandLabel(shooter, distance);
        int firingMen = PrototypeCombatTuningManager.GetConfiguredFiringMen(shooter);
        float terrainMultiplier = PrototypeCropFieldCombat09F29R.GetCombinedTargetMultiplier(shooter, target, distance);
        float expected = PrototypeCombatTuningManager.GetExpectedHitsPreview(shooter, distance) * terrainMultiplier;
        int kernelHits = Mathf.Max(0, target.LastVolleyHits);
        int targetStrengthBeforeKernel = target.CurrentStrength + kernelHits;
        int physicalMaximum = Mathf.Max(0, Mathf.Min(firingMen, targetStrengthBeforeKernel));

        int extraHits = 0;
        int desiredTotal = kernelHits;

        // Only supplement when the legacy kernel visibly hit its hard ceiling.
        if (kernelHits >= LegacyKernelHitCap && physicalMaximum > kernelHits)
        {
            float spread = Random.Range(0.72f, 1.28f);
            desiredTotal = Mathf.Clamp(
                Mathf.RoundToInt(expected * spread),
                kernelHits,
                physicalMaximum);

            extraHits = Mathf.Max(0, desiredTotal - kernelHits);
            if (extraHits > 0)
            {
                // Shock was already applied by the original volley. Supplemental hits
                // add casualty/cohesion effect only; do not apply the volley shock twice.
                target.ReceiveVolley(extraHits, 0f, shooter);
            }
        }

        PrototypeCropFieldTerrain09F29R.NotifyVolley(shooter);

        int totalHits = kernelHits + extraHits;

        Debug.Log(
            "VOLLEY-CAL-09F14|Shooter=" + shooter.RegimentName +
            "|Target=" + target.RegimentName +
            "|DistanceM=" + distance.ToString("0.0") +
            "|Band=" + band +
            "|FiringMen=" + firingMen +
            "|BaseHit=" + PrototypeCombatTuningManager.BaseHitChancePercent.ToString("0.00") + "%" +
            "|RangeFactor=x" + PrototypeCombatTuningManager.GetConfiguredRangeMultiplier(shooter, distance).ToString("0.00") +
            "|Quality=x" + PrototypeCombatTuningManager.GetConfiguredQualityMultiplier(shooter).ToString("0.00") +
            "|Terrain=x" + terrainMultiplier.ToString("0.00") +
            "|Expected=" + expected.ToString("0.00") +
            "|KernelHits=" + kernelHits +
            "|ExtraHits=" + extraHits +
            "|TotalHits=" + totalHits +
            "|TargetRemaining=" + target.CurrentStrength);
    }

    private Regiment FindVolleyTarget(Regiment shooter, BattleManager battle)
    {
        if (forcedTargetField != null)
        {
            Regiment forced = forcedTargetField.GetValue(shooter) as Regiment;
            if (forced != null && forced.Team != shooter.Team)
                return forced;
        }

        Regiment best = null;
        float bestDistance = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == shooter || candidate.Team == shooter.Team)
                continue;

            float distance = PlanarDistance(shooter.transform.position, candidate.transform.position);
            if (distance > shooter.MaximumRange + 5f || distance >= bestDistance)
                continue;

            Vector3 toTarget = candidate.transform.position - shooter.transform.position;
            toTarget.y = 0f;
            Vector3 forward = shooter.transform.forward;
            forward.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f && forward.sqrMagnitude > 0.01f)
            {
                if (Vector3.Angle(forward.normalized, toTarget.normalized) > 50f)
                    continue;
            }

            best = candidate;
            bestDistance = distance;
        }

        return best;
    }

    private float ReadNextFireTime(Regiment regiment)
    {
        object value = nextFireTimeField.GetValue(regiment);
        return value is float f ? f : 0f;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, State> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }
}