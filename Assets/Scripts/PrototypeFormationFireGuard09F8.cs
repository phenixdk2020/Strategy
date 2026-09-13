using System.Collections.Generic;
using UnityEngine;

// v00.00.09f8: a company may not fire while marching in Column or while the
// visual formation is still physically reforming back into Line.
[DefaultExecutionOrder(-35)]
public sealed class PrototypeFormationFireGuard09F8 : MonoBehaviour
{
    private readonly Dictionary<Regiment, RegimentFirePolicy> restorePolicies =
        new Dictionary<Regiment, RegimentFirePolicy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeFormationFireGuard09F8>() != null)
            return;

        GameObject root = new GameObject("PrototypeFormationFireGuard_v000009f8");
        root.AddComponent<PrototypeFormationFireGuard09F8>();
    }

    private void Update()
    {
        restorePolicies.Clear();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted || regiment.FirePolicy == RegimentFirePolicy.HoldFire)
                continue;

            bool ready;
            if (PrototypeFireVisuals09F8.Instance != null)
                ready = PrototypeFireVisuals09F8.Instance.IsFormationFireReady(regiment);
            else
                ready = regiment.Formation == RegimentFormation.Line;

            if (ready)
                continue;

            restorePolicies[regiment] = regiment.FirePolicy;
            regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
        }
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Regiment, RegimentFirePolicy> pair in restorePolicies)
        {
            if (pair.Key != null && !pair.Key.IsRouted)
                pair.Key.SetFirePolicy(pair.Value);
        }
        restorePolicies.Clear();
    }
}
