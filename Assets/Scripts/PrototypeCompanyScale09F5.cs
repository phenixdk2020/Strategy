using System.Reflection;
using UnityEngine;

// v00.00.09f5 company-scale baseline, extended by v00.00.09f17.
// Internal regiment identifiers remain for compatibility, while the active test units
// represent ~190-man companies on a 1 Unity unit = 1 metre battlefield scale.
[DefaultExecutionOrder(-11500)]
public sealed class PrototypeCompanyScale09F5 : MonoBehaviour
{
    private FieldInfo initialStrengthField;
    private FieldInfo currentStrengthField;
    private FieldInfo effectiveRangeField;
    private FieldInfo maximumRangeField;
    private MethodInfo refreshVisualStrengthMethod;
    private bool applied;

    private const int CompanyStrength = 190;
    private const float EffectiveRangeMetres = 200f;
    private const float LongRangeMetres = 400f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyScale09F5>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyScale_v000009f17");
        root.AddComponent<PrototypeCompanyScale09F5>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        initialStrengthField = typeof(Regiment).GetField("<InitialStrength>k__BackingField", flags);
        currentStrengthField = typeof(Regiment).GetField("<CurrentStrength>k__BackingField", flags);
        effectiveRangeField = typeof(Regiment).GetField("<EffectiveRange>k__BackingField", flags);
        maximumRangeField = typeof(Regiment).GetField("<MaximumRange>k__BackingField", flags);
        refreshVisualStrengthMethod = typeof(Regiment).GetMethod("RefreshVisualStrength", flags);

        if (initialStrengthField == null || currentStrengthField == null ||
            effectiveRangeField == null || maximumRangeField == null)
        {
            Debug.LogError("COMPANY-09F17|Installed=False|Reason=RegimentBackingFieldsMissing");
            enabled = false;
        }
    }

    private void Update()
    {
        if (applied)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 5)
            return;

        int changed = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            bool activeCompany =
                regiment.RegimentName == "1. Regiment" ||
                regiment.RegimentName == "5. Regiment" ||
                regiment.RegimentName == "2. Regiment" ||
                regiment.RegimentName == "3. Regiment" ||
                regiment.RegimentName == "8th Regiment";

            if (!activeCompany)
                continue;

            initialStrengthField.SetValue(regiment, CompanyStrength);
            currentStrengthField.SetValue(regiment, CompanyStrength);
            effectiveRangeField.SetValue(regiment, EffectiveRangeMetres);
            maximumRangeField.SetValue(regiment, LongRangeMetres);
            refreshVisualStrengthMethod?.Invoke(regiment, null);
            changed++;
        }

        if (changed < 5)
            return;

        applied = true;
        Debug.Log(
            "COMPANY-09F17|Applied=True|Companies=5|Denmark=4|Prussia=1|Strength=190|Scale=1u=1m|" +
            "Close=" + (EffectiveRangeMetres * 0.5f).ToString("0") +
            "m|Effective=" + EffectiveRangeMetres.ToString("0") +
            "m|Long=" + LongRangeMetres.ToString("0") + "m");
    }
}
