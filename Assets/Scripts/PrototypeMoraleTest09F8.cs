using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f8 QA mode: hold morale at 100 so hit-count testing is not distorted
// by accumulated morale loss. Cohesion and distance accuracy remain active.
[DefaultExecutionOrder(-10000)]
public sealed class PrototypeMoraleTest09F8 : MonoBehaviour
{
    private FieldInfo moraleField;
    private readonly Dictionary<Regiment, int> previousStrength =
        new Dictionary<Regiment, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMoraleTest09F8>() != null)
            return;

        GameObject root = new GameObject("PrototypeMoraleTest_v000009f8");
        root.AddComponent<PrototypeMoraleTest09F8>();
    }

    private void Awake()
    {
        moraleField = typeof(Regiment).GetField(
            "<Morale>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (moraleField == null)
        {
            Debug.LogError("MORALE-09F8|Installed=False|Reason=MoraleBackingFieldMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "MORALE-09F8|Installed=True|Mode=QA_BYPASS|MoraleLocked=100|" +
            "DistanceAccuracy=True|CohesionAccuracy=True");
    }

    private void Update()
    {
        LockMorale();
        LogNewCasualties();
    }

    private void LateUpdate()
    {
        // Reset any morale shock applied during this frame so it cannot accumulate.
        LockMorale();
    }

    private void LockMorale()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || moraleField == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null)
                moraleField.SetValue(regiment, 100f);
        }
    }

    private void LogNewCasualties()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment target in battle.Regiments)
        {
            if (target == null)
                continue;

            if (!previousStrength.TryGetValue(target, out int before))
            {
                previousStrength[target] = target.CurrentStrength;
                continue;
            }

            if (target.CurrentStrength < before)
            {
                int losses = before - target.CurrentStrength;
                Regiment attacker = FindNearestEnemy(target, battle);
                float distance = attacker != null
                    ? PlanarDistance(attacker.transform.position, target.transform.position)
                    : -1f;

                Debug.Log(
                    "HITTEST-09F8|Target=" + target.RegimentName +
                    "|Attacker=" + (attacker != null ? attacker.RegimentName : "UNKNOWN") +
                    "|Distance=" + (distance >= 0f ? distance.ToString("0.0") : "NA") +
                    "m|Band=" + GetBand(distance) +
                    "|Hits=" + losses +
                    "|Remaining=" + target.CurrentStrength +
                    "|Morale=OFF|Cohesion=" + target.Cohesion.ToString("0.0"));
            }

            previousStrength[target] = target.CurrentStrength;
        }
    }

    private static string GetBand(float distance)
    {
        if (distance < 0f)
            return "UNKNOWN";
        if (distance <= PrototypeRangeTuning09F8.EffectiveRangeMetres * 0.5f)
            return "SHORT";
        if (distance <= PrototypeRangeTuning09F8.EffectiveRangeMetres)
            return "MEDIUM";
        if (distance <= PrototypeRangeTuning09F8.MaximumRangeMetres)
            return "LONG";
        return "OUT";
    }

    private static Regiment FindNearestEnemy(Regiment target, BattleManager battle)
    {
        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == target || candidate.Team == target.Team || candidate.IsRouted)
                continue;

            float distance = PlanarDistance(candidate.transform.position, target.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
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
