using UnityEngine;

// v00.00.09f11 TEST navigation authority.
// Legacy obstacle/pathfinding stacks remain disabled because they previously caused
// stuck units. Active route constraints are now deliberately narrow and explicit:
// 1) PrototypeRiverBridgeOnly09F3 for bridge-only river crossing.
// 2) PrototypeStaticObstacleRouting09F11 for approved hard buildings only.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeNavigationV3Authority : MonoBehaviour
{
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3Authority>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationAuthority_v000009f11");
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

        PrototypeRiverBridgeOnly09F3 river =
            Object.FindAnyObjectByType<PrototypeRiverBridgeOnly09F3>();
        PrototypeStaticObstacleRouting09F11 buildings =
            Object.FindAnyObjectByType<PrototypeStaticObstacleRouting09F11>();

        bool riverActive = river != null && river.enabled;
        bool buildingRoutingActive = buildings != null && buildings.enabled;

        if (announced)
            return;

        announced = true;
        Debug.Log(string.Format(
            "NAV-AUTH|Build=v00.00.09f11|Authority=River+ApprovedBuildings|" +
            "V1Disabled={0}|RecoveryDisabled={1}|V3Disabled={2}|V4Disabled={3}|" +
            "09ADisabled={4}|09BDisabled={5}|ManualRecoveryDisabled={6}|" +
            "TreesBlocked=False|FencesBlocked=False|HousesBlocked=True|" +
            "RiverBlocked=True|BridgeOnly=True|RiverLayerActive={7}|BuildingRouterActive={8}",
            v1Disabled,
            recoveryDisabled,
            v3Disabled,
            v4Disabled,
            hotfix09aDisabled,
            softPassDisabled,
            manualRecoveryDisabled,
            riverActive,
            buildingRoutingActive));
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
