using UnityEngine;

// v00.00.09f3 fixed bridge visual.
// The old navigation managers used to create the bridge as a side effect. They are
// disabled in 09f3, so the bridge visual is now owned independently from navigation.
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

        float x = StreamCenterX(BridgeZ);
        float y = PrototypeBootstrap.SampleGroundHeight(x, BridgeZ) + 0.24f;

        GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "PrototypeBridge_v009f3";
        bridge.transform.position = new Vector3(x, y, BridgeZ);
        bridge.transform.localScale = new Vector3(16f, 0.28f, 6.2f);
        bridge.GetComponent<Renderer>().sharedMaterial =
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.38f, 0.28f, 0.16f),
                "BridgeDeck09F3");

        Collider collider = bridge.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        CreateRail(bridge.transform, -2.75f);
        CreateRail(bridge.transform, 2.75f);

        Debug.Log(
            "BRIDGE-09F3|Created=True|Z=22|NavigationCollider=False|" +
            "RiverRuleOwner=PrototypeRiverBridgeOnly09F3");
    }

    private static void CreateRail(Transform bridge, float localZ)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "BridgeRail";
        rail.transform.SetParent(bridge, false);
        rail.transform.localPosition = new Vector3(0f, 1.8f, localZ / 6.2f);
        rail.transform.localScale = new Vector3(1f, 8f, 0.05f);
        rail.GetComponent<Renderer>().sharedMaterial =
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.25f, 0.16f, 0.08f),
                "BridgeRail09F3");

        Collider collider = rail.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private static float StreamCenterX(float z)
    {
        return Mathf.Sin(z * 0.065f) * 4.8f;
    }
}
