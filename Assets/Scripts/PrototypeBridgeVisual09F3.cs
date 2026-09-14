using UnityEngine;

// v00.00.09f3 fixed bridge visual, aligned to the 09f15 river curve.
[DefaultExecutionOrder(-5000)]
public sealed class PrototypeBridgeVisual09F3 : MonoBehaviour
{
    private const float BridgeZ = 22.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBridgeVisual09F3>() != null)
            return;

        GameObject root = new GameObject("PrototypeBridgeVisual_v000009f3");
        root.AddComponent<PrototypeBridgeVisual09F3>();
    }

    private void Start()
    {
        if (GameObject.Find("PrototypeBridge_v009f3") != null ||
            GameObject.Find("PrototypeBridge_v009") != null)
        {
            return;
        }

        float x = PrototypeBootstrap.StreamCenterX(BridgeZ);
        float y = PrototypeBootstrap.SampleGroundHeight(x, BridgeZ) + 0.24f;

        GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "PrototypeBridge_v009f3";
        bridge.transform.position = new Vector3(x, y, BridgeZ);
        bridge.transform.localScale = new Vector3(17.5f, 0.30f, 6.4f);
        bridge.GetComponent<Renderer>().sharedMaterial =
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.34f, 0.235f, 0.125f),
                "BridgeDeck09F15");

        Collider collider = bridge.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        CreateRail(bridge.transform, -2.82f);
        CreateRail(bridge.transform, 2.82f);

        Debug.Log(
            "BRIDGE-09F15|Created=True|Z=22|NavigationCollider=False|" +
            "RiverWidth=5.5m|RiverRuleOwner=PrototypeRiverBridgeOnly09F3");
    }

    private static void CreateRail(Transform bridge, float localZ)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "BridgeRail";
        rail.transform.SetParent(bridge, false);
        rail.transform.localPosition = new Vector3(0f, 1.8f, localZ / 6.4f);
        rail.transform.localScale = new Vector3(1f, 8f, 0.05f);
        rail.GetComponent<Renderer>().sharedMaterial =
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.22f, 0.135f, 0.065f),
                "BridgeRail09F15");

        Collider collider = rail.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }
}
