using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29g
// Continuous river-water overlay. The original river is built from short rotated cubes;
// on the enlarged battlefield small seams/terrain clipping became visible. This ribbon
// sits at the same water level, remains continuous from map edge to map edge and passes
// underneath the bridge. It is visual only; bridge/river navigation rules are unchanged.
[DefaultExecutionOrder(-4300)]
public sealed class PrototypeRiverVisual09F29G : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeRiverVisual09F29G>() == null)
            new GameObject("PrototypeRiverVisual_v000009f29g")
                .AddComponent<PrototypeRiverVisual09F29G>();
    }

    private void Start()
    {
        if (GameObject.Find("ContinuousRiverWater09F29G") != null)
            return;

        const float margin = 8f;
        const float step = 2.5f;
        const float halfWidth = 3.15f;
        const float waterOffset = 0.075f;

        float startZ = -PrototypeBootstrap.BattlefieldHalfDepth + margin;
        float endZ = PrototypeBootstrap.BattlefieldHalfDepth - margin;
        int rows = Mathf.CeilToInt((endZ - startZ) / step) + 1;

        Vector3[] vertices = new Vector3[rows * 2];
        Vector2[] uv = new Vector2[rows * 2];
        int[] triangles = new int[(rows - 1) * 6];

        for (int i = 0; i < rows; i++)
        {
            float z = Mathf.Min(endZ, startZ + i * step);
            float x = PrototypeBootstrap.StreamCenterX(z);
            float y = PrototypeBootstrap.SampleGroundHeight(x, z) + waterOffset;

            vertices[i * 2] = new Vector3(x - halfWidth, y, z);
            vertices[i * 2 + 1] = new Vector3(x + halfWidth, y, z);
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
            name = "ContinuousRiverMesh09F29G",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject water = new GameObject("ContinuousRiverWater09F29G");
        MeshFilter filter = water.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = water.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.105f, 0.31f, 0.46f),
            "ContinuousWater09F29G");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Debug.Log("RIVER-VISUAL-09F29G|Continuous=True|Width=6.3m|Step=2.5m|UnderBridge=True|NavigationUnchanged=True");
    }
}
