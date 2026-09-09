using System.Collections.Generic;
using UnityEngine;

// v00.00.09l4 - prevents the old Regiment pivot/HQ from acting as the firing unit
// while companies are the visible tactical formations. Company-level fire resolution
// is a later gate; until then parent regiment fire is suppressed rather than visually
// pretending the mounted HQ is firing the whole regiment volley.
[DefaultExecutionOrder(11200)]
public sealed class PrototypeCompanyCombatAuthority09L4 : MonoBehaviour
{
    private readonly Dictionary<Regiment, RegimentFirePolicy> savedPolicies =
        new Dictionary<Regiment, RegimentFirePolicy>();
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyCombatAuthority09L4>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyCombatAuthority_v000009l4");
        root.AddComponent<PrototypeCompanyCombatAuthority09L4>();
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        HashSet<Regiment> parents = new HashSet<Regiment>();

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            Regiment regiment = company != null ? company.ParentRegiment : null;
            if (regiment == null || !regiment.gameObject.activeInHierarchy || !parents.Add(regiment))
                continue;

            if (!savedPolicies.ContainsKey(regiment))
                savedPolicies[regiment] = regiment.FirePolicy;

            if (regiment.FirePolicy != RegimentFirePolicy.HoldFire)
                regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);

            if (regiment.ShowRange)
            {
                regiment.ShowRange = false;
                regiment.RefreshRangeVisibility();
            }
        }

        if (!announced && parents.Count > 0)
        {
            announced = true;
            Debug.Log(
                "COMBAT-AUTH-09L4|Installed=True|ParentRegimentFire=False|" +
                "ParentRangeCone=False|CompanyCombatResolver=Deferred|Reason=HQMustNotFireVolley");
        }
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Regiment, RegimentFirePolicy> pair in savedPolicies)
        {
            if (pair.Key == null)
                continue;
            pair.Key.SetFirePolicy(pair.Value);
            pair.Key.ShowRange = true;
            pair.Key.RefreshRangeVisibility();
        }
    }
}
