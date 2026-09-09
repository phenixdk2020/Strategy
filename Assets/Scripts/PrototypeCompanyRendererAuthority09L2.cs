using UnityEngine;

// v00.00.09l2 - preserves the retained 09k mounted Regimental HQ objects, then disables
// the old regiment-wide foot renderer so 09l2 company-anchored soldiers are not duplicated.
[DefaultExecutionOrder(10840)]
public sealed class PrototypeCompanyRendererAuthority09L2 : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyRendererAuthority09L2>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyRendererAuthority_v000009l2");
        root.AddComponent<PrototypeCompanyRendererAuthority09L2>();
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int pilotRegiments = 0;
        int hqReady = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !IsPilot(regiment.RegimentName))
                continue;

            pilotRegiments++;
            if (regiment.transform.Find("RegimentalHQ09K") != null)
                hqReady++;
        }

        if (pilotRegiments != 4 || hqReady != 4)
            return;

        PrototypeFullScaleRenderer09K oldRenderer =
            Object.FindAnyObjectByType<PrototypeFullScaleRenderer09K>();
        if (oldRenderer != null && oldRenderer.enabled)
            oldRenderer.enabled = false;

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "RENDER-AUTH-09L2|CompanyRenderer=True|OldRegimentFootRendererDisabled=True|" +
                "MountedRegimentalHQRetained=True|DuplicateInfantry=False");
        }
    }

    private static bool IsPilot(string name)
    {
        return name == "1. Regiment" ||
               name == "5. Regiment" ||
               name == "8th Regiment" ||
               name == "18th Regiment";
    }
}
