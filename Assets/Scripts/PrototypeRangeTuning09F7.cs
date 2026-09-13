using System.Reflection;
using UnityEngine;

// v00.00.09f7 tactical range tuning for the current 1u = 1m company-scale test.
// Close remains derived by Regiment as EffectiveRange * 0.5.
[DefaultExecutionOrder(-11400)]
public sealed class PrototypeRangeTuning09F7 : MonoBehaviour
{
    public const float EffectiveRangeMetres = 80f;
    public const float MaximumRangeMetres = 115f;
    public const float FireArcHalfAngleDegrees = 35f;

    private FieldInfo effectiveRangeField;
    private FieldInfo maximumRangeField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRangeTuning09F7>() != null)
            return;

        GameObject root = new GameObject("PrototypeRangeTuning_v000009f7");
        root.AddComponent<PrototypeRangeTuning09F7>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        effectiveRangeField = typeof(Regiment).GetField("<EffectiveRange>k__BackingField", flags);
        maximumRangeField = typeof(Regiment).GetField("<MaximumRange>k__BackingField", flags);

        if (effectiveRangeField == null || maximumRangeField == null)
        {
            Debug.LogError("RANGE-09F7|Installed=False|Reason=RegimentRangeBackingFieldsMissing");
            enabled = false;
        }
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            effectiveRangeField.SetValue(regiment, EffectiveRangeMetres);
            maximumRangeField.SetValue(regiment, MaximumRangeMetres);
        }
    }

    private void Start()
    {
        Debug.Log(
            "RANGE-09F7|Installed=True|Close=" + (EffectiveRangeMetres * 0.5f).ToString("0") +
            "m|Medium=" + EffectiveRangeMetres.ToString("0") +
            "m|Long=" + MaximumRangeMetres.ToString("0") +
            "m|ConeTotal=" + (FireArcHalfAngleDegrees * 2f).ToString("0") + "deg");
    }
}
