using UnityEngine;

public sealed class PrototypeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Object.FindFirstObjectByType<BattleManager>() != null)
            return;

        GameObject bootstrapObject = new GameObject("PROJECT1864_PrototypeBootstrap");
        bootstrapObject.AddComponent<PrototypeBootstrap>();
    }

    private void Awake()
    {
        BuildBattlefield();
    }

    private void BuildBattlefield()
    {
        Application.targetFrameRate = 120;
        RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.53f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.70f, 0.72f);
        RenderSettings.fogDensity = 0.0035f;

        CreateLighting();
        CreateCamera();
        CreateGround();
        CreateStream();
        CreateRoad();
        CreateFarmstead();
        CreateVegetation();
        CreateFences();

        GameObject systems = new GameObject("BattleSystems");
        systems.AddComponent<BattleManager>();
        systems.AddComponent<PlayerCommander>();

        CreateRegiment("1. Regiment", BattleTeam.Denmark, 620, false, new Vector3(-47f, 0f, -19f), Quaternion.Euler(0f, 88f, 0f));
        CreateRegiment("5. Regiment", BattleTeam.Denmark, 585, false, new Vector3(-52f, 0f, 20f), Quaternion.Euler(0f, 75f, 0f));
        CreateRegiment("8th Regiment", BattleTeam.Prussia, 610, true, new Vector3(52f, 0f, -15f), Quaternion.Euler(0f, -92f, 0f));
        CreateRegiment("18th Regiment", BattleTeam.Prussia, 560, true, new Vector3(58f, 0f, 28f), Quaternion.Euler(0f, -105f, 0f), new Vector3(20f, 0f, 32f));
    }

    private void CreateLighting()
    {
        GameObject lightObject = new GameObject("Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.94f, 0.83f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 48f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 600f;
        cameraObject.transform.position = new Vector3(-2f, 48f, -62f);
        cameraObject.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<RTSCameraController>();
    }

    private void CreateGround()
    {
        const int xSegments = 72;
        const int zSegments = 48;
        const float width = 180f;
        const float depth = 120f;

        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        int v = 0;
        for (int z = 0; z <= zSegments; z++)
        {
            float pz = -depth * 0.5f + depth * z / zSegments;
            for (int x = 0; x <= xSegments; x++)
            {
                float px = -width * 0.5f + width * x / xSegments;
                vertices[v] = new Vector3(px, SampleGroundHeight(px, pz), pz);
                uvs[v] = new Vector2(x / (float)xSegments, z / (float)zSegments);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i = z * (xSegments + 1) + x;
                triangles[t++] = i;
                triangles[t++] = i + xSegments + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + xSegments + 1;
                triangles[t++] = i + xSegments + 2;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "PrototypeBattlefieldMesh",
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject ground = new GameObject("Battlefield Ground");
        MeshFilter mf = ground.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = ground.AddComponent<MeshRenderer>();
        mr.sharedMaterial = CreateSharedMaterial(new Color(0.32f, 0.42f, 0.20f), "Grass");
        MeshCollider mc = ground.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    public static float SampleGroundHeight(float x, float z)
    {
        float ridge = 4.6f * Mathf.Exp(-((x + 34f) * (x + 34f) / 900f + (z + 17f) * (z + 17f) / 330f));
        float northHill = 2.8f * Mathf.Exp(-((x - 28f) * (x - 28f) / 1100f + (z - 25f) * (z - 25f) / 520f));
        float rolls = 0.8f * Mathf.Sin(x * 0.055f) * Mathf.Cos(z * 0.075f);
        float streamDip = -1.1f * Mathf.Exp(-(x * x) / 120f);
        return ridge + northHill + rolls + streamDip;
    }

    private void CreateStream()
    {
        Material water = CreateSharedMaterial(new Color(0.18f, 0.40f, 0.52f), "Water");
        Vector3 previous = Vector3.zero;
        for (int i = 0; i < 31; i++)
        {
            float z = -58f + i * 3.9f;
            float x = Mathf.Sin(z * 0.10f) * 3.4f;
            Vector3 p = new Vector3(x, SampleGroundHeight(x, z) + 0.07f, z);
            if (i > 0)
                CreateSegment("Stream", previous, p, 2.4f, 0.06f, water);
            previous = p;
        }
    }

    private void CreateRoad()
    {
        Material road = CreateSharedMaterial(new Color(0.55f, 0.43f, 0.28f), "Road");
        Vector3 previous = Vector3.zero;
        for (int i = 0; i < 31; i++)
        {
            float x = -86f + i * 5.7f;
            float z = 13f + Mathf.Sin(x * 0.045f) * 3.2f;
            Vector3 p = new Vector3(x, SampleGroundHeight(x, z) + 0.10f, z);
            if (i > 0)
                CreateSegment("Road", previous, p, 4.6f, 0.05f, road);
            previous = p;
        }
    }

    private void CreateSegment(string name, Vector3 a, Vector3 b, float width, float height, Material material)
    {
        Vector3 center = (a + b) * 0.5f;
        Vector3 delta = b - a;
        float length = delta.magnitude;
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.position = center;
        segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        segment.transform.localScale = new Vector3(width, height, length);
        segment.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(segment.GetComponent<Collider>());
    }

    private void CreateFarmstead()
    {
        Material wall = CreateSharedMaterial(new Color(0.78f, 0.74f, 0.64f), "FarmWall");
        Material roof = CreateSharedMaterial(new Color(0.28f, 0.20f, 0.14f), "FarmRoof");
        Vector3 basePos = new Vector3(-31f, SampleGroundHeight(-31f, -27f), -27f);

        GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
        house.name = "Farmhouse";
        house.transform.position = basePos + Vector3.up * 1.5f;
        house.transform.localScale = new Vector3(8f, 3f, 5f);
        house.GetComponent<Renderer>().sharedMaterial = wall;

        GameObject roofBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofBlock.name = "FarmRoof";
        roofBlock.transform.position = basePos + Vector3.up * 3.25f;
        roofBlock.transform.localScale = new Vector3(8.6f, 0.65f, 5.7f);
        roofBlock.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
        roofBlock.GetComponent<Renderer>().sharedMaterial = roof;
        Destroy(roofBlock.GetComponent<Collider>());

        GameObject barn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barn.name = "Barn";
        barn.transform.position = new Vector3(-22f, SampleGroundHeight(-22f, -29f) + 1.1f, -29f);
        barn.transform.localScale = new Vector3(5f, 2.2f, 4f);
        barn.GetComponent<Renderer>().sharedMaterial = wall;
    }

    private void CreateVegetation()
    {
        Random.InitState(1864);
        for (int i = 0; i < 42; i++)
        {
            float x = Random.Range(-84f, 84f);
            float z = Random.Range(-55f, 55f);
            if (Mathf.Abs(x) < 8f || (x < -18f && x > -42f && z < -18f && z > -38f))
                continue;
            CreateTree(new Vector3(x, SampleGroundHeight(x, z), z), Random.Range(0.8f, 1.35f));
        }
    }

    private void CreateTree(Vector3 position, float scale)
    {
        Material trunkMat = CreateSharedMaterial(new Color(0.24f, 0.16f, 0.10f), "TreeTrunk");
        Material crownMat = CreateSharedMaterial(new Color(0.17f, 0.31f, 0.13f), "TreeCrown");

        GameObject root = new GameObject("Tree");
        root.transform.position = position;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.5f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.28f * scale, 1.5f * scale, 0.28f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 4.1f * scale, 0f);
        crown.transform.localScale = new Vector3(2.3f * scale, 3.0f * scale, 2.3f * scale);
        crown.GetComponent<Renderer>().sharedMaterial = crownMat;
        Destroy(crown.GetComponent<Collider>());
    }

    private void CreateFences()
    {
        Material wood = CreateSharedMaterial(new Color(0.30f, 0.22f, 0.13f), "Fence");
        CreateFenceLine(new Vector3(-63f, 0f, -7f), new Vector3(-18f, 0f, -7f), wood);
        CreateFenceLine(new Vector3(18f, 0f, 7f), new Vector3(72f, 0f, 7f), wood);
        CreateFenceLine(new Vector3(-43f, 0f, -34f), new Vector3(-17f, 0f, -34f), wood);
    }

    private void CreateFenceLine(Vector3 start, Vector3 end, Material material)
    {
        Vector3 delta = end - start;
        int posts = Mathf.Max(2, Mathf.CeilToInt(delta.magnitude / 4f));
        for (int i = 0; i < posts; i++)
        {
            float t = i / (float)(posts - 1);
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y = SampleGroundHeight(p.x, p.z);
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "FencePost";
            post.transform.position = p + Vector3.up * 0.65f;
            post.transform.localScale = new Vector3(0.14f, 1.3f, 0.14f);
            post.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(post.GetComponent<Collider>());
        }
    }

    private void CreateRegiment(string unitName, BattleTeam team, int strength, bool isAI, Vector3 p, Quaternion rotation, Vector3? aiWaypoint = null)
    {
        p.y = SampleGroundHeight(p.x, p.z) + 0.10f;
        GameObject unit = new GameObject(unitName);
        unit.transform.rotation = rotation;
        Regiment regiment = unit.AddComponent<Regiment>();
        regiment.Initialize(unitName, team, strength, isAI, p, aiWaypoint);
    }

    public static Material CreateSharedMaterial(Color color, string materialName)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        return material;
    }
}
