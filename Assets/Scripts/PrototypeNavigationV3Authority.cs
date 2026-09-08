using UnityEngine;

// v00.00.09e TEST navigation authority.
// Movement V3 is restored as the single authoritative tactical steering layer.
// V4 and the 09A/09B hotfix experiments remain in the repository for comparison,
// but are disabled before their first Update so they cannot compete for Regiment
// destination/formation state.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeNavigationV3Authority : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3Authority>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationV3Authority_v000009e");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<PrototypeNavigationV3Authority>();
    }

    private void Update()
    {
        PrototypeBattlefieldNavigationV3 v3 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>();

        if (v3 == null)
            return;

        if (!v3.enabled)
            v3.enabled = true;

        bool v4Disabled = DisableLayer<PrototypeBattlefieldNavigationV4>();
        bool hotfix09aDisabled = DisableLayer<PrototypeNavigation09AHotfix>();
        bool hotfix09bDisabled = DisableLayer<PrototypeNavigation09BTreePassThrough>();

        if (announced)
            return;

        announced = true;
        Debug.Log(string.Format(
            "NAV-AUTH|Build=v00.00.09e|Authority=V3|V4Disabled={0}|09ADisabled={1}|09BDisabled={2}",
            v4Disabled,
            hotfix09aDisabled,
            hotfix09bDisabled));
    }

    private static bool DisableLayer<T>() where T : Behaviour
    {
        T layer = Object.FindAnyObjectByType<T>();
        if (layer == null)
            return false;

        if (layer.enabled)
            layer.enabled = false;

        return true;
    }
}
