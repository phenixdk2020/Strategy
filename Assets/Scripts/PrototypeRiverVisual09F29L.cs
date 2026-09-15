using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29l
// Robust river presentation for the enlarged battlefield.
// The earlier F29G ribbon sampled analytic terrain only 7.5 cm above the stream bed;
// with the coarse 10 m battlefield mesh that could still place parts of the water
// below the rendered ground. F29L raycasts the actual ground mesh and lifts one
// continuous perpendicular-width surface clearly above it, including under the bridge.
[DefaultExecutionOrder(-4200)]
public sealed class PrototypeRiverVisual09F29L : MonoBehaviour
{
    private const float Margin = 8f;
    private const float Step = 1.25f;
    private const float HalfWidth = 3.75f;
    private const float WaterLift = 0.24f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRiverVisual09F29L>() == null)
            new GameObject("PrototypeRiverVisual_v000009f29l").AddComponent<PrototypeRiverVisual09F29L>();
    }

    private void Start()
    {
        GameObject existing = GameObject.Find("ContinuousRiverWater09F29L");
        if (existing != null)
            return;

        DisableOlderRiverRendering();
        BuildContinuousRiver();
    }

    private static void DisableOlderRiverRendering()
    {
        GameObject oldRibbon = GameObject.Find("ContinuousRiverWater09F29G");
        if (oldRibbon != null)
            oldRibbon.SetActive(false);

        MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer != null && renderer.gameObject != null && renderer.gameObject.name == "Stream")
                renderer.enabled = false;
        }
    }

    private static void BuildContinuousRiver()
    {
        GameObject groundObject = GameObject.Find("Battlefield Ground");
        MeshCollider groundCollider = groundObject != null ? groundObject.GetComponent<MeshCollider>() : null;

        float startZ = -PrototypeBootstrap.BattlefieldHalfDepth + Margin;
        float endZ = PrototypeBootstrap.BattlefieldHalfDepth - Margin;
        int rows = Mathf.CeilToInt((endZ - startZ) / Step) + 1;

        Vector3[] vertices = new Vector3[rows * 2];
        Vector2[] uv = new Vector2[rows * 2];
        int[] triangles = new int[(rows - 1) * 6];

        for (int i = 0; i < rows; i++)
        {
            float z = Mathf.Min(endZ, startZ + i * Step);
            float x = PrototypeBootstrap.StreamCenterX(z);

            float beforeZ = Mathf.Max(startZ, z - Step);
            float afterZ = Mathf.Min(endZ, z + Step);
            Vector3 before = new Vector3(PrototypeBootstrap.StreamCenterX(beforeZ), 0f, beforeZ);
            Vector3 after = new Vector3(PrototypeBootstrap.StreamCenterX(afterZ), 0f, afterZ);
            Vector3 tangent = after - before;
            tangent.y = 0f;
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.forward;
            tangent.Normalize();

            Vector3 across = Vector3.Cross(Vector3.up, tangent).normalized;
            if (across.sqrMagnitude < 0.0001f)
                across = Vector3.right;

            float surfaceY = GetRenderedGroundHeight(groundCollider, x, z) + WaterLift;
            Vector3 center = new Vector3(x, surfaceY, z);
            Vector3 left = center - across * HalfWidth;
            Vector3 right = center + across * HalfWidth;
            left.y = surfaceY;
            right.y = surfaceY;

            vertices[i * 2] = left;
            vertices[i * 2 + 1] = right;
            float v = i / (float)Mathf.Max(1, rows - 1);
            uv[i * 2] = new Vector2(0f, v);
            uv[i * 2 + 1] = new Vector2(1f, v);
        }

        int t = 0;
        for (int i = 0; i < rows - 1; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            triangles[t++] = a;
            triangles[t++] = c;
            triangles[t++] = b;
            triangles[t++] = b;
            triangles[t++] = c;
            triangles[t++] = d;
        }

        Mesh mesh = new Mesh
        {
            name = "ContinuousRiverMesh09F29L",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject water = new GameObject("ContinuousRiverWater09F29L");
        MeshFilter filter = water.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = water.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.095f, 0.34f, 0.50f),
            "ContinuousWater09F29L");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Debug.Log(
            "RIVER-VISUAL-09F29L|Continuous=True|Width=" + (HalfWidth * 2f).ToString("0.0") +
            "m|Step=" + Step.ToString("0.00") + "m|Lift=" + WaterLift.ToString("0.00") +
            "m|ActualGroundRaycast=" + (groundCollider != null) +
            "|UnderBridge=True|LegacyStreamRenderers=False|NavigationUnchanged=True");
    }

    private static float GetRenderedGroundHeight(MeshCollider groundCollider, float x, float z)
    {
        if (groundCollider != null)
        {
            Ray ray = new Ray(new Vector3(x, 1000f, z), Vector3.down);
            if (groundCollider.Raycast(ray, out RaycastHit hit, 2000f))
                return hit.point.y;
        }

        return PrototypeBootstrap.SampleGroundHeight(x, z);
    }
}
