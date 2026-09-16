using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29y
// Presentation-only terrain polish.
// - Replaces the continuous river visual with two segments and a real dry gap under the bridge.
// - Replaces sparse F29R crop-row visuals with a terrain-conforming golden field surface and
//   much denser combined-mesh crop rows. F29R concealment/movement gameplay remains unchanged.
[DefaultExecutionOrder(-4100)]
public sealed class PrototypeTerrainVisualPolish09F29Y : MonoBehaviour
{
    private const float RiverMargin = 8f;
    private const float RiverStep = 1.25f;
    private const float RiverHalfWidth = 3.75f;
    private const float RiverWaterLift = 0.24f;
    private const float BridgeZ = 22.0f;
    private const float BridgeDryHalfZ = 4.8f;

    private bool riverBuilt;
    private bool cropsBuilt;
    private Material riverMaterial;
    private Material fieldBaseMaterial;
    private Material cropMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTerrainVisualPolish09F29Y>() == null)
            new GameObject("PrototypeTerrainVisualPolish_v000009f29y")
                .AddComponent<PrototypeTerrainVisualPolish09F29Y>();
    }

    private void Awake()
    {
        riverMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.095f, 0.34f, 0.50f), "RiverWater09F29Y");
        fieldBaseMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.72f, 0.57f, 0.14f), "FieldBase09F29Y");
        cropMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.84f, 0.70f, 0.20f), "CropRows09F29Y");
    }

    private void Start()
    {
        TryBuildDryBridgeRiver();
    }

    private void Update()
    {
        if (!riverBuilt)
            TryBuildDryBridgeRiver();
        if (!cropsBuilt)
            TryBuildDenseCrops();
    }

    private void TryBuildDryBridgeRiver()
    {
        if (riverBuilt)
            return;

        GameObject existing = GameObject.Find("ContinuousRiverWater09F29Y");
        if (existing != null)
        {
            riverBuilt = true;
            return;
        }

        GameObject legacy = GameObject.Find("ContinuousRiverWater09F29L");
        if (legacy == null)
            return; // let F29L create first so visual ownership is explicit.

        legacy.SetActive(false);

        GameObject groundObject = GameObject.Find("Battlefield Ground");
        MeshCollider groundCollider = groundObject != null ? groundObject.GetComponent<MeshCollider>() : null;

        float startZ = -PrototypeBootstrap.BattlefieldHalfDepth + RiverMargin;
        float endZ = PrototypeBootstrap.BattlefieldHalfDepth - RiverMargin;
        float gapStart = BridgeZ - BridgeDryHalfZ;
        float gapEnd = BridgeZ + BridgeDryHalfZ;

        GameObject root = new GameObject("ContinuousRiverWater09F29Y");
        BuildRiverSegment(root.transform, "RiverSouth09F29Y", startZ, gapStart, groundCollider);
        BuildRiverSegment(root.transform, "RiverNorth09F29Y", gapEnd, endZ, groundCollider);

        riverBuilt = true;
        Debug.Log(
            "TERRAIN-09F29Y|River=True|DryBridgeGap=True|BridgeZ=" + BridgeZ.ToString("0.0") +
            "|Gap=" + (BridgeDryHalfZ * 2f).ToString("0.0") + "m|NavigationUnchanged=True");
    }

    private void BuildRiverSegment(
        Transform parent,
        string name,
        float startZ,
        float endZ,
        MeshCollider groundCollider)
    {
        if (endZ <= startZ + 0.1f)
            return;

        int rows = Mathf.CeilToInt((endZ - startZ) / RiverStep) + 1;
        Vector3[] vertices = new Vector3[rows * 2];
        Vector2[] uv = new Vector2[rows * 2];
        int[] triangles = new int[(rows - 1) * 6];

        for (int i = 0; i < rows; i++)
        {
            float z = Mathf.Min(endZ, startZ + i * RiverStep);
            float x = PrototypeBootstrap.StreamCenterX(z);
            float beforeZ = Mathf.Max(startZ, z - RiverStep);
            float afterZ = Mathf.Min(endZ, z + RiverStep);
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

            float y = GetRenderedGroundHeight(groundCollider, x, z) + RiverWaterLift;
            Vector3 center = new Vector3(x, y, z);
            vertices[i * 2] = center - across * RiverHalfWidth;
            vertices[i * 2 + 1] = center + across * RiverHalfWidth;
            vertices[i * 2].y = y;
            vertices[i * 2 + 1].y = y;
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
            triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
            triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
        }

        Mesh mesh = new Mesh
        {
            name = name + "Mesh",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = riverMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void TryBuildDenseCrops()
    {
        if (cropsBuilt)
            return;

        if (GameObject.Find("CropFields09F29Y") != null)
        {
            cropsBuilt = true;
            return;
        }

        GameObject oldRoot = GameObject.Find("CropFields09F29R");
        PrototypeCropFieldTerrain09F29R terrain = PrototypeCropFieldTerrain09F29R.Instance;
        if (oldRoot == null || terrain == null)
            return;

        oldRoot.SetActive(false);
        GameObject root = new GameObject("CropFields09F29Y");

        IReadOnlyList<PrototypeCropFieldTerrain09F29R.FieldDescriptor> fields =
            PrototypeCropFieldTerrain09F29R.Fields;
        for (int i = 0; i < fields.Count; i++)
            BuildField(root.transform, fields[i], i);

        cropsBuilt = true;
        Debug.Log(
            "TERRAIN-09F29Y|CropVisual=True|Fields=" + fields.Count +
            "|GoldenGround=True|DenseRows=True|GameplayOwner=F29R|ConcealmentUnchanged=True");
    }

    private void BuildField(
        Transform parent,
        PrototypeCropFieldTerrain09F29R.FieldDescriptor field,
        int index)
    {
        float centerY = PrototypeBootstrap.SampleGroundHeight(field.Center.x, field.Center.y);
        GameObject fieldRoot = new GameObject("CropField09F29Y_" + (index + 1));
        fieldRoot.transform.SetParent(parent, false);
        fieldRoot.transform.position = new Vector3(field.Center.x, centerY, field.Center.y);
        fieldRoot.transform.rotation = Quaternion.Euler(0f, field.Yaw, 0f);

        GameObject baseObject = new GameObject("CropFieldBase09F29Y");
        baseObject.transform.SetParent(fieldRoot.transform, false);
        baseObject.AddComponent<MeshFilter>().sharedMesh = BuildConformingBase(fieldRoot.transform, field, centerY);
        MeshRenderer baseRenderer = baseObject.AddComponent<MeshRenderer>();
        baseRenderer.sharedMaterial = fieldBaseMaterial;
        baseRenderer.shadowCastingMode = ShadowCastingMode.Off;
        baseRenderer.receiveShadows = true;

        GameObject cropObject = new GameObject("DenseCropRows09F29Y");
        cropObject.transform.SetParent(fieldRoot.transform, false);
        cropObject.AddComponent<MeshFilter>().sharedMesh = BuildCropRows(fieldRoot.transform, field, centerY);
        MeshRenderer cropRenderer = cropObject.AddComponent<MeshRenderer>();
        cropRenderer.sharedMaterial = cropMaterial;
        cropRenderer.shadowCastingMode = ShadowCastingMode.On;
        cropRenderer.receiveShadows = true;
    }

    private static Mesh BuildConformingBase(
        Transform fieldRoot,
        PrototypeCropFieldTerrain09F29R.FieldDescriptor field,
        float centerY)
    {
        const float cell = 8f;
        int nx = Mathf.Max(2, Mathf.CeilToInt(field.Width / cell));
        int nz = Mathf.Max(2, Mathf.CeilToInt(field.Depth / cell));
        int vx = nx + 1;
        int vz = nz + 1;
        Vector3[] vertices = new Vector3[vx * vz];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[nx * nz * 6];

        for (int z = 0; z < vz; z++)
        {
            float tz = z / (float)nz;
            float localZ = Mathf.Lerp(-field.Depth * 0.5f, field.Depth * 0.5f, tz);
            for (int x = 0; x < vx; x++)
            {
                float tx = x / (float)nx;
                float localX = Mathf.Lerp(-field.Width * 0.5f, field.Width * 0.5f, tx);
                Vector3 world = fieldRoot.TransformPoint(new Vector3(localX, 0f, localZ));
                float ground = PrototypeBootstrap.SampleGroundHeight(world.x, world.z);
                int idx = z * vx + x;
                vertices[idx] = new Vector3(localX, ground - centerY + 0.055f, localZ);
                uv[idx] = new Vector2(tx, tz);
            }
        }

        int t = 0;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                int a = z * vx + x;
                int b = a + 1;
                int c = a + vx;
                int d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "CropFieldBaseMesh09F29Y",
            indexFormat = IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildCropRows(
        Transform fieldRoot,
        PrototypeCropFieldTerrain09F29R.FieldDescriptor field,
        float centerY)
    {
        float rowSpacing = Mathf.Lerp(2.7f, 1.8f, field.Density);
        int rows = Mathf.Max(12, Mathf.FloorToInt(field.Depth * 0.94f / rowSpacing));
        int segments = Mathf.Max(4, Mathf.CeilToInt(field.Width / 8f));
        float segmentCell = field.Width * 0.94f / segments;
        float segmentLength = segmentCell * 0.92f;
        float halfRowWidth = 0.16f;
        float cropHeight = Mathf.Lerp(0.48f, 0.66f, field.Density);

        List<Vector3> vertices = new List<Vector3>(rows * segments * 8);
        List<int> triangles = new List<int>(rows * segments * 36);

        for (int row = 0; row < rows; row++)
        {
            float tz = rows <= 1 ? 0.5f : row / (float)(rows - 1);
            float localZ = Mathf.Lerp(-field.Depth * 0.47f, field.Depth * 0.47f, tz);
            float stagger = (row & 1) == 0 ? 0f : segmentCell * 0.18f;

            for (int segment = 0; segment < segments; segment++)
            {
                float localX = -field.Width * 0.47f + (segment + 0.5f) * segmentCell + stagger;
                if (localX > field.Width * 0.47f)
                    localX -= field.Width * 0.94f;

                Vector3 world = fieldRoot.TransformPoint(new Vector3(localX, 0f, localZ));
                float ground = PrototypeBootstrap.SampleGroundHeight(world.x, world.z);
                float bottom = ground - centerY + 0.06f;
                float top = bottom + cropHeight;
                AddBox(
                    vertices,
                    triangles,
                    localX - segmentLength * 0.5f,
                    localX + segmentLength * 0.5f,
                    bottom,
                    top,
                    localZ - halfRowWidth,
                    localZ + halfRowWidth);
            }
        }

        Mesh mesh = new Mesh
        {
            name = "DenseCropRowsMesh09F29Y",
            indexFormat = IndexFormat.UInt32
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddBox(
        List<Vector3> v,
        List<int> t,
        float x0,
        float x1,
        float y0,
        float y1,
        float z0,
        float z1)
    {
        int b = v.Count;
        v.Add(new Vector3(x0, y0, z0));
        v.Add(new Vector3(x1, y0, z0));
        v.Add(new Vector3(x1, y0, z1));
        v.Add(new Vector3(x0, y0, z1));
        v.Add(new Vector3(x0, y1, z0));
        v.Add(new Vector3(x1, y1, z0));
        v.Add(new Vector3(x1, y1, z1));
        v.Add(new Vector3(x0, y1, z1));

        int[] q =
        {
            0,2,1, 0,3,2,
            4,5,6, 4,6,7,
            0,1,5, 0,5,4,
            1,2,6, 1,6,5,
            2,3,7, 2,7,6,
            3,0,4, 3,4,7
        };
        for (int i = 0; i < q.Length; i++)
            t.Add(b + q[i]);
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
