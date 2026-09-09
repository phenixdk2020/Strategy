using System.Collections.Generic;
using UnityEngine;

// v00.00.09l2 - keeps lightweight company tactical centres on local terrain while
// whole-regiment V3 movement or individual company movement changes X/Z position.
[DefaultExecutionOrder(650)]
public sealed class PrototypeCompanyGrounding09L2 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyGrounding09L2>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyGrounding_v000009l2");
        root.AddComponent<PrototypeCompanyGrounding09L2>();
    }

    private void LateUpdate()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null)
                continue;

            Vector3 world = company.transform.position;
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.10f;
            company.transform.position = world;
        }
    }
}
