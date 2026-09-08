using UnityEngine;

// v00.00.09f TEST navigation authority.
// Movement V3 remains the single authoritative tactical steering layer.
// V4 and 09A stay disabled. 09B is allowed to run only as the approved
// decorative-tree pass-through policy and does not own Regiment steering.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeNavigationV3Authority : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3Authority>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationV3Authority_v000009f");
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
        bool treePassThroughEnabled = EnableLayer<PrototypeNavigation09BTreePassThrough>();

        if (announced)
            return;

        announced = true;
        Debug.Log(string.Format(
            "NAV-AUTH|Build=v00.00.09f|Authority=V3|V4Disabled={0}|09ADisabled={1}|09BTreePassThroughEnabled={2}",
            v4Disabled,
            hotfix09aDisabled,
            treePassThroughEnabled));
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

    private static bool EnableLayer<T>() where T : Behaviour
    {
        T layer = Object.FindAnyObjectByType<T>();
        if (layer == null)
            return false;

        if (!layer.enabled)
            layer.enabled = true;

        return true;
    }
}
