using System.Reflection;
using UnityEngine;

// v00.00.09f8: shorter tactical range test.
// 1 Unity world unit = 1 metre. Close remains EffectiveRange * 0.5 in Regiment.
[DefaultExecutionOrder(-11350)]
public sealed class PrototypeRangeTuning09F8 : MonoBehaviour
{
    public const float EffectiveRangeMetres = 70f;
    public const float MaximumRangeMetres = 100f;
    public const float FireArcHalfAngleDegrees = 35f;

    private FieldInfo effectiveRangeField;
    private FieldInfo maximumRangeField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRangeTuning09F8>() != null)
            return;

        GameObject root = new GameObject("PrototypeRangeTuning_v000009f8");
        root.AddComponent<PrototypeRangeTuning09F8>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        effectiveRangeField = typeof(Regiment).GetField("<EffectiveRange>k__BackingField", flags);
        maximumRangeField = typeof(Regiment).GetField("<MaximumRange>k__BackingField", flags);

        if (effectiveRangeField == null || maximumRangeField == null)
        {
            Debug.LogError("RANGE-09F8|Installed=False|Reason=RegimentRangeBackingFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "RANGE-09F8|Installed=True|Close=35m|Medium=70m|Long=100m|" +
            "ConeTotal=70deg|Scale=1u=1m");
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
}
