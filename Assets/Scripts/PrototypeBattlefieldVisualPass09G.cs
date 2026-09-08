using UnityEngine;

// v00.00.09g TEST visual-only battlefield expansion.
// Adds a much larger scenic world around the existing tactical core without writing
// Regiment destinations, formations, combat state or Officer AI state. The central
// 360x240 tactical mesh remains the authoritative clickable/movement test area while
// the 720x480 outer world provides visual scale for the graphics pass.
[DefaultExecutionOrder(12000)]
public sealed class PrototypeBattlefieldVisualPass09G : MonoBehaviour
{
    private bool installed;

    private Material outerGrass;
    private Material fieldGreen;
    private Material fieldDry;
    private Material fieldDark;
    private Material roadMaterial;
    private Material hedgeMaterial;
    private Material trunkMaterial;
    private Material crownMaterialA;
    private Material crownMaterialB;
    private Material wallMaterial;
    private Material roofMaterial;
    private Material hayMaterial;
    private Material stoneMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattlefieldVisualPass09G>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattlefieldVisualPass_v000009g");
        root.AddComponent<PrototypeBattlefieldVisualPass09G>();
    }

    private void Update()
    {
        if (installed)
            return;

        if (GameObject.Find("Battlefield Ground") == null || Camera.main == null)
            return;

        Install();
        installed = true;
    }

    private void Install()
    {
        CreateMaterials();
        CreateOuterTerrain();
        CreateFieldMosaic();
        CreateSecondaryRoads();
        CreateHedgerows();
        CreateScenicWoodland();
        CreateHamlets();
        CreateRuralDetails();
        TuneAtmosphere();

        Debug.Log(
            "VIS-09G|Installed=True|ScenicMap=720x480|CentralTacticalCore=360x240|" +
            "Fields=18|WoodlandTrees=156|Hamlets=5|SecondaryRoads=3|MovementWrites=False");
    }

    private void CreateMaterials()
    {
        outerGrass = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.29f, 0.39f, 0.18f), "09G_OuterGrass");
        fieldGreen = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.38f, 0.46f, 0.20f), "09G_FieldGreen");
        fieldDry = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.57f, 0.50f, 0.27f), "09G_FieldDry");
        fieldDark = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.27f, 0.34f, 0.14f), "09G_FieldDark");
        roadMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.50f, 0.39f, 0.25f), "09G_Road");
        hedgeMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.19f, 0.30f, 0.12f), "09G_Hedge");
        trunkMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.22f, 0.14f, 0.08f), "09G_TreeTrunk");
        crownMaterialA = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.15f, 0.29f, 0.11f), "09G_TreeCrownA");
        crownMaterialB = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.21f, 0.35f, 0.14f), "09G_TreeCrownB");
        wallMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.73f, 0.69f, 0.59f), "09G_HouseWall");
        roofMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.25f, 0.17f, 0.12f), "09G_HouseRoof");
        hayMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.64f, 0.52f, 0.23f), "09G_Hay");
        stoneMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.39f, 0.40f, 0.37f), "09G_Stone");
    }

    private void CreateOuterTerrain()
    {
        const int xSegments = 180;
        const int zSegments = 120;
        const float width = 720f;
        const float depth = 480f;

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
                // The original tactical mesh sits 0.10 m above this scenic mesh in
                // the centre, preventing z-fighting while remaining visually seamless.
                vertices[v] = new Vector3(
                    px,
                    PrototypeBootstrap.SampleGroundHeight(px, pz) - 0.10f,
                    pz);
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
            name = "PrototypeScenicBattlefieldMesh_v009g_720x480",
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject ground = new GameObject("09G Scenic Outer Battlefield 720x480");
        MeshFilter filter = ground.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = ground.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = outerGrass;

        // Deliberately no collider: 09g is a graphics/map-scale pass. The existing
        // central tactical mesh remains the movement/click surface until navigation
        // stabilization is accepted separately.
    }

    private void CreateFieldMosaic()
    {
        CreateField(-270f, -165f, 76f, 42f, 7f, fieldDry);
        CreateField(-185f, -180f, 60f, 38f, -8f, fieldGreen);
        CreateField(-92f, -165f, 58f, 34f, 4f, fieldDark);
        CreateField(48f, -170f, 72f, 40f, -6f, fieldDry);
        CreateField(155f, -175f, 66f, 36f, 5f, fieldGreen);
        CreateField(270f, -160f, 74f, 42f, -9f, fieldDark);

        CreateField(-278f, -72f, 70f, 34f, -5f, fieldGreen);
        CreateField(-200f, -66f, 54f, 28f, 8f, fieldDry);
        CreateField(205f, -70f, 66f, 34f, 6f, fieldDry);
        CreateField(286f, -66f, 62f, 30f, -7f, fieldGreen);

        CreateField(-286f, 78f, 70f, 38f, 4f, fieldDark);
        CreateField(-205f, 82f, 58f, 34f, -6f, fieldDry);
        CreateField(212f, 76f, 62f, 32f, -5f, fieldGreen);
        CreateField(292f, 82f, 58f, 36f, 7f, fieldDry);

        CreateField(-270f, 175f, 72f, 38f, -8f, fieldDry);
        CreateField(-165f, 170f, 66f, 36f, 6f, fieldGreen);
        CreateField(120f, 174f, 72f, 40f, -5f, fieldDark);
        CreateField(255f, 168f, 82f, 42f, 8f, fieldDry);
    }

    private void CreateField(
        float x,
        float z,
        float width,
        float depth,
        float yaw,
        Material material)
    {
        GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        patch.name = "09G Scenic Field";
        patch.transform.position = new Vector3(
            x,
            PrototypeBootstrap.SampleGroundHeight(x, z) + 0.015f,
            z);
        patch.transform.rotation = GroundRotation(x, z, yaw);
        patch.transform.localScale = new Vector3(width, 0.035f, depth);
        patch.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(patch.GetComponent<Collider>());
    }

    private void CreateSecondaryRoads()
    {
        CreateRoadSpline(
            "09G North Road",
            new Vector3(-340f, 0f, 146f),
            new Vector3(340f, 0f, 118f),
            66,
            3.8f,
            10f);

        CreateRoadSpline(
            "09G South Road",
            new Vector3(-335f, 0f, -146f),
            new Vector3(332f, 0f, -128f),
            64,
            3.5f,
            -14f);

        CreateRoadSpline(
            "09G Diagonal Track",
            new Vector3(-255f, 0f, 215f),
            new Vector3(280f, 0f, -215f),
            58,
            2.8f,
            17f);
    }

    private void CreateRoadSpline(
        string name,
        Vector3 start,
        Vector3 end,
        int segments,
        float width,
        float curveOffset)
    {
        Vector3 previous = start;
        previous.y = PrototypeBootstrap.SampleGroundHeight(previous.x, previous.z) + 0.035f;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(start, end, t);
            float wave = Mathf.Sin(t * Mathf.PI * 2f) * curveOffset;
            Vector3 across = Vector3.Cross((end - start).normalized, Vector3.up);
            p += across * wave;
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.035f;
            CreateSegment(name, previous, p, width, 0.035f, roadMaterial);
            previous = p;
        }
    }

    private void CreateHedgerows()
    {
        CreateHedge(-318f, -104f, -205f, -104f);
        CreateHedge(-285f, 32f, -198f, 32f);
        CreateHedge(-168f, 132f, -78f, 132f);
        CreateHedge(182f, -104f, 302f, -104f);
        CreateHedge(205f, 42f, 322f, 42f);
        CreateHedge(78f, 148f, 178f, 148f);

        CreateHedge(-238f, -210f, -238f, -145f);
        CreateHedge(-128f, -218f, -128f, -148f);
        CreateHedge(100f, -220f, 100f, -152f);
        CreateHedge(236f, -216f, 236f, -150f);
        CreateHedge(-248f, 148f, -248f, 214f);
        CreateHedge(-98f, 152f, -98f, 220f);
        CreateHedge(158f, 150f, 158f, 216f);
        CreateHedge(290f, 144f, 290f, 210f);
    }

    private void CreateHedge(float x1, float z1, float x2, float z2)
    {
        Vector3 a = new Vector3(x1, PrototypeBootstrap.SampleGroundHeight(x1, z1) + 0.65f, z1);
        Vector3 b = new Vector3(x2, PrototypeBootstrap.SampleGroundHeight(x2, z2) + 0.65f, z2);
        CreateSegment("09G Scenic Hedge", a, b, 1.3f, 1.25f, hedgeMaterial);
    }

    private void CreateScenicWoodland()
    {
        Random.State oldState = Random.state;
        Random.InitState(18640907);

        CreateTreeCluster(new Vector2(-286f, -14f), 34, 42f, 25f);
        CreateTreeCluster(new Vector2(-238f, 190f), 27, 48f, 27f);
        CreateTreeCluster(new Vector2(-42f, 192f), 25, 52f, 28f);
        CreateTreeCluster(new Vector2(238f, 186f), 28, 50f, 29f);
        CreateTreeCluster(new Vector2(284f, -8f), 24, 42f, 26f);
        CreateTreeCluster(new Vector2(232f, -190f), 18, 42f, 24f);

        Random.state = oldState;
    }

    private void CreateTreeCluster(Vector2 center, int count, float radiusX, float radiusZ)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radial = Mathf.Sqrt(Random.value);
            float x = center.x + Mathf.Cos(angle) * radiusX * radial;
            float z = center.y + Mathf.Sin(angle) * radiusZ * radial;
            float scale = Random.Range(0.75f, 1.45f);
            CreateScenicTree(x, z, scale, (i & 1) == 0 ? crownMaterialA : crownMaterialB);
        }
    }

    private void CreateScenicTree(float x, float z, float scale, Material crownMaterial)
    {
        GameObject root = new GameObject("09G ScenicTree");
        root.transform.position = new Vector3(x, PrototypeBootstrap.SampleGroundHeight(x, z), z);

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "09G ScenicTree Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.45f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.24f * scale, 1.45f * scale, 0.24f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "09G ScenicTree Crown";
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 3.85f * scale, 0f);
        crown.transform.localScale = new Vector3(2.05f * scale, 2.55f * scale, 2.05f * scale);
        crown.GetComponent<Renderer>().sharedMaterial = crownMaterial;
        Destroy(crown.GetComponent<Collider>());
    }

    private void CreateHamlets()
    {
        CreateHamlet(-298f, -198f, 5, 14f);
        CreateHamlet(-265f, 112f, 6, -8f);
        CreateHamlet(286f, -182f, 5, -11f);
        CreateHamlet(274f, 118f, 6, 8f);
        CreateHamlet(38f, 210f, 5, -4f);
    }

    private void CreateHamlet(float x, float z, int houses, float yaw)
    {
        for (int i = 0; i < houses; i++)
        {
            float row = i / 3;
            float col = i % 3;
            float px = x + (col - 1f) * 9f;
            float pz = z + row * 9f;
            CreateScenicHouse(px, pz, yaw + (i % 2 == 0 ? 3f : -4f));
        }
    }

    private void CreateScenicHouse(float x, float z, float yaw)
    {
        float y = PrototypeBootstrap.SampleGroundHeight(x, z);
        GameObject root = new GameObject("09G Scenic House");
        root.transform.position = new Vector3(x, y, z);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "Walls";
        walls.transform.SetParent(root.transform, false);
        walls.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        walls.transform.localScale = new Vector3(6.2f, 2.5f, 4.5f);
        walls.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        Destroy(walls.GetComponent<Collider>());

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(root.transform, false);
        roof.transform.localPosition = new Vector3(0f, 2.75f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        roof.transform.localScale = new Vector3(6.8f, 0.55f, 5.0f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
        Destroy(roof.GetComponent<Collider>());

        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.name = "Chimney";
        chimney.transform.SetParent(root.transform, false);
        chimney.transform.localPosition = new Vector3(1.6f, 3.35f, 0.6f);
        chimney.transform.localScale = new Vector3(0.55f, 1.25f, 0.55f);
        chimney.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
        Destroy(chimney.GetComponent<Collider>());
    }

    private void CreateRuralDetails()
    {
        Vector2[] hayLocations =
        {
            new Vector2(-218f, -160f), new Vector2(-192f, -172f),
            new Vector2(-142f, 178f), new Vector2(-118f, 165f),
            new Vector2(126f, -184f), new Vector2(152f, -168f),
            new Vector2(205f, 168f), new Vector2(230f, 182f)
        };

        foreach (Vector2 p in hayLocations)
        {
            GameObject hay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hay.name = "09G Haystack";
            hay.transform.position = new Vector3(
                p.x,
                PrototypeBootstrap.SampleGroundHeight(p.x, p.y) + 0.9f,
                p.y);
            hay.transform.localScale = new Vector3(1.4f, 0.9f, 1.4f);
            hay.GetComponent<Renderer>().sharedMaterial = hayMaterial;
            Destroy(hay.GetComponent<Collider>());
        }

        Vector2[] stones =
        {
            new Vector2(-330f, 18f), new Vector2(-312f, 52f),
            new Vector2(-72f, -208f), new Vector2(-34f, -218f),
            new Vector2(66f, 214f), new Vector2(102f, 205f),
            new Vector2(318f, 28f), new Vector2(328f, 66f)
        };

        for (int i = 0; i < stones.Length; i++)
        {
            Vector2 p = stones[i];
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "09G Field Stone";
            rock.transform.position = new Vector3(
                p.x,
                PrototypeBootstrap.SampleGroundHeight(p.x, p.y) + 0.38f,
                p.y);
            rock.transform.localScale = new Vector3(1.2f, 0.75f, 1.0f) * (1f + i * 0.025f);
            rock.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
            Destroy(rock.GetComponent<Collider>());
        }
    }

    private void TuneAtmosphere()
    {
        RenderSettings.ambientLight = new Color(0.51f, 0.54f, 0.49f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.69f, 0.69f);
        RenderSettings.fogDensity = 0.00135f;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 1500f;
        }
    }

    private Quaternion GroundRotation(float x, float z, float yaw)
    {
        const float sample = 1.5f;
        float left = PrototypeBootstrap.SampleGroundHeight(x - sample, z);
        float right = PrototypeBootstrap.SampleGroundHeight(x + sample, z);
        float back = PrototypeBootstrap.SampleGroundHeight(x, z - sample);
        float front = PrototypeBootstrap.SampleGroundHeight(x, z + sample);

        Vector3 normal = new Vector3(
            -(right - left) / (sample * 2f),
            1f,
            -(front - back) / (sample * 2f)).normalized;

        return Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, yaw, 0f);
    }

    private void CreateSegment(
        string name,
        Vector3 a,
        Vector3 b,
        float width,
        float height,
        Material material)
    {
        Vector3 center = (a + b) * 0.5f;
        Vector3 delta = b - a;
        if (delta.sqrMagnitude < 0.001f)
            return;

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.position = center;
        segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        segment.transform.localScale = new Vector3(width, height, delta.magnitude);
        segment.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(segment.GetComponent<Collider>());
    }
}
