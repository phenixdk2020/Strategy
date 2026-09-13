using UnityEngine;

// v00.00.09f3 TEST navigation authority.
// Scenery stays fully pass-through. All legacy obstacle/pathfinding writers are
// disabled. PrototypeRiverBridgeOnly09F3 is the only tactical route constraint and
// exists solely to enforce bridge-only river crossing.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeNavigationV3Authority : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3Authority>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationAuthority_v000009f3");
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
        bool manualRecoveryDisabled = DisableLayer<PrototypeManualRouteRecovery09H4>();

        PrototypeRiverBridgeOnly09F3 riverOnly =
            Object.FindAnyObjectByType<PrototypeRiverBridgeOnly09F3>();
        bool riverOnlyActive = riverOnly != null && riverOnly.enabled;

        if (announced)
            return;

        announced = true;
        Debug.Log(string.Format(
            "NAV-AUTH|Build=v00.00.09f3|Authority=RiverBridgeOnly|" +
            "V1Disabled={0}|RecoveryDisabled={1}|V3Disabled={2}|V4Disabled={3}|" +
            "09ADisabled={4}|09BDisabled={5}|ManualRecoveryDisabled={6}|" +
            "SceneryBlocked=False|RiverBlocked=True|BridgeOnly=True|RiverLayerActive={7}",
            v1Disabled,
            recoveryDisabled,
            v3Disabled,
            v4Disabled,
            hotfix09aDisabled,
            softPassDisabled,
            manualRecoveryDisabled,
            riverOnlyActive));
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
