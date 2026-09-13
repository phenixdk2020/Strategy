using UnityEngine;

// v00.00.09f2 TEST navigation authority.
// Isolation baseline: scenery, buildings, fences, trees and river are deliberately
// transparent to movement so formation/march behaviour can be tested independently
// from obstacle routing. All legacy/V3/V4 navigation writers are disabled.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeNavigationV3Authority : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3Authority>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationAuthority_v000009f2");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<PrototypeNavigationV3Authority>();
    }

    private void Update()
    {
        bool v1Disabled = DisableLayer<PrototypeBattlefieldNavigationManager>();
        bool recoveryDisabled = DisableLayer<PrototypeNavigationRecoveryManager>();
        bool v3Disabled = DisableLayer<PrototypeBattlefieldNavigationV3>();
        bool v4Disabled = DisableLayer<PrototypeBattlefieldNavigationV4>();
        bool hotfix09aDisabled = DisableLayer<PrototypeNavigation09AHotfix>();
        bool softPassDisabled = DisableLayer<PrototypeNavigation09BTreePassThrough>();

        if (announced)
            return;

        announced = true;
        Debug.Log(string.Format(
            "NAV-AUTH|Build=v00.00.09f2|Authority=DirectTransparent|" +
            "V1Disabled={0}|RecoveryDisabled={1}|V3Disabled={2}|V4Disabled={3}|" +
            "09ADisabled={4}|09BDisabled={5}|SceneryBlocked=False|RiverBlocked=False",
            v1Disabled,
            recoveryDisabled,
            v3Disabled,
            v4Disabled,
            hotfix09aDisabled,
            softPassDisabled));
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
