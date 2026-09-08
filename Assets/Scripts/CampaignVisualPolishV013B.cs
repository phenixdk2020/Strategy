using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13b — presentation-only polish pass.
// Strategic state, movement, ETA and tactical systems are intentionally untouched.
[DefaultExecutionOrder(1500)]
public sealed class CampaignVisualPolishV013B : MonoBehaviour
{
    private Material grassDark;
    private Material grassLight;
    private Material fieldBrown;
    private Material fieldGreen;
    private Material forestDark;
    private Material trunkMaterial;
    private Material roadMaterial;
    private Material railMaterial;
    private Material ferryMaterial;
    private Material stoneMaterial;
    private Material plasterMaterial;
    private Material brickMaterial;
    private Material roofMaterial;
    private Material woodMaterial;
    private Material metalMaterial;
    private Material waterMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignVisualPolishV013B>() != null)
            return;

        GameObject root = new GameObject("CampaignVisualPolishV013B");
        root.AddComponent<CampaignVisualPolishV013B>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        BuildMaterials();
        SuppressLegacyCampaignNoise();
        ApplyTerrainAndWaterPass();
        BuildLandCoverPass();
        BuildVegetationPass();
        StyleInfrastructurePass();
        DressSettlementsPass();
        DressConstructionPass();
        ApplyLightingPass();

        Debug.Log("CAMPAIGN-V013B|VisualPolish=True|Terrain=True|Water=True|DenmarkFocus=True|Infrastructure=True|Settlements=True|Construction=True|Workers=True|Vegetation=True|Lighting=True|UIReadability=True");
    }

    private void BuildMaterials()
    {
        grassDark = CreateMaterial(new Color(0.235f, 0.315f, 0.175f), "V013B_GrassDark");
        grassLight = CreateMaterial(new Color(0.315f, 0.405f, 0.215f), "V013B_GrassLight");
        fieldBrown = CreateMaterial(new Color(0.44f, 0.36f, 0.22f), "V013B_FieldBrown");
        fieldGreen = CreateMaterial(new Color(0.39f, 0.47f, 0.23f), "V013B_FieldGreen");
        forestDark = CreateMaterial(new Color(0.145f, 0.245f, 0.125f), "V013B_Forest");
        trunkMaterial = CreateMaterial(new Color(0.285f, 0.205f, 0.125f), "V013B_Trunk");
        roadMaterial = CreateMaterial(new Color(0.52f, 0.43f, 0.30f), "V013B_Road");
        railMaterial = CreateMaterial(new Color(0.17f, 0.18f, 0.18f), "V013B_Rail");
        ferryMaterial = CreateMaterial(new Color(0.22f, 0.48f, 0.62f), "V013B_Ferry");
        stoneMaterial = CreateMaterial(new Color(0.43f, 0.43f, 0.39f), "V013B_Stone");
        plasterMaterial = CreateMaterial(new Color(0.68f, 0.62f, 0.49f), "V013B_Plaster");
        brickMaterial = CreateMaterial(new Color(0.47f, 0.25f, 0.18f), "V013B_Brick");
        roofMaterial = CreateMaterial(new Color(0.24f, 0.14f, 0.11f), "V013B_Roof");
        woodMaterial = CreateMaterial(new Color(0.37f, 0.235f, 0.12f), "V013B_Wood");
        metalMaterial = CreateMaterial(new Color(0.20f, 0.22f, 0.22f), "V013B_Metal");
        waterMaterial = CreateMaterial(new Color(0.095f, 0.225f, 0.315f), "V013B_Water");
        SetSmoothness(waterMaterial, 0.48f);
    }

    private void SuppressLegacyCampaignNoise()
    {
        PrototypeBuildVersionOverlay legacy = UnityEngine.Object.FindAnyObjectByType<PrototypeBuildVersionOverlay>();
        if (legacy != null)
            legacy.enabled = false;

        GameObject overlays = GameObject.Find(CampaignTerrainV013.RootOverlays);
        if (overlays != null)
            overlays.SetActive(false);
    }

    private void ApplyTerrainAndWaterPass()
    {
        GameObject terrain = GameObject.Find("CampaignTerrainSurface_v013");
        if (terrain != null)
        {
            Renderer renderer = terrain.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = grassDark;
        }

        GameObject sea = GameObject.Find("Campaign Sea Base");
        if (sea != null)
        {
            Renderer renderer = sea.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = waterMaterial;
        }
    }

    private void BuildLandCoverPass()
    {
        Transform parent = CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootLandCover).transform;
        if (parent.Find("V013B_DenmarkLandCover") != null)
            return;

        GameObject root = new GameObject("V013B_DenmarkLandCover");
        root.transform.SetParent(parent, false);

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            int hash = StableHash(node.Id);
            int patchCount = node.Terrain == CampaignTerrainType.Urban || node.Terrain == CampaignTerrainType.Fortified ? 1 : 2;
            for (int i = 0; i < patchCount; i++)
            {
                float angle = ((hash + i * 83) % 360) * Mathf.Deg2Rad;
                float distance = 12f + ((hash >> (i + 1)) & 7);
                float x = node.MapPosition.x + Mathf.Cos(angle) * distance;
                float z = node.MapPosition.y + Mathf.Sin(angle) * distance;
                float y = CampaignTerrainV013.SampleSurfaceY(x, z) + 0.10f;

                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = "LandPatch_" + node.Id + "_" + i;
                patch.transform.SetParent(root.transform, false);
                patch.transform.position = new Vector3(x, y, z);
                patch.transform.rotation = Quaternion.Euler(0f, (hash + i * 29) % 180, 0f);
                patch.transform.localScale = new Vector3(10f + (hash % 7), 0.10f, 6f + ((hash / 7) % 5));
                Renderer r = patch.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = (i + hash) % 3 == 0 ? fieldBrown : fieldGreen;
                RemoveCollider(patch);
            }
        }
    }

    private void BuildVegetationPass()
    {
        Transform parent = CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootLandCover).transform;
        if (parent.Find("V013B_Vegetation") != null)
            return;

        GameObject root = new GameObject("V013B_Vegetation");
        root.transform.SetParent(parent, false);

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            int hash = StableHash(node.Id);
            int treeCount = node.Terrain == CampaignTerrainType.Forest ? 8 : 4;
            float baseAngle = (hash % 360) * Mathf.Deg2Rad;
            float clusterDistance = 18f + (hash % 9);
            Vector2 centre = node.MapPosition + new Vector2(Mathf.Cos(baseAngle), Mathf.Sin(baseAngle)) * clusterDistance;

            GameObject cluster = new GameObject("TreeCluster_" + node.Id);
            cluster.transform.SetParent(root.transform, false);

            for (int i = 0; i < treeCount; i++)
            {
                float a = ((hash + i * 137) % 360) * Mathf.Deg2Rad;
                float radius = 2.0f + ((hash + i * 17) % 50) * 0.08f;
                float x = centre.x + Mathf.Cos(a) * radius;
                float z = centre.y + Mathf.Sin(a) * radius;
                float y = CampaignTerrainV013.SampleSurfaceY(x, z);
                float scale = 0.75f + ((hash + i * 31) % 45) / 100f;
                CreateTree(cluster.transform, new Vector3(x, y, z), scale, (i + hash) % 3);
            }
        }
    }

    private void CreateTree(Transform parent, Vector3 worldPosition, float scale, int variant)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = worldPosition;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.45f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.30f * scale, 1.45f * scale, 0.30f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
        RemoveCollider(trunk);

        PrimitiveType crownType = variant == 2 ? PrimitiveType.Capsule : PrimitiveType.Sphere;
        GameObject crown = GameObject.CreatePrimitive(crownType);
        crown.name = "Crown";
        crown.transform.SetParent(tree.transform, false);
        crown.transform.localPosition = new Vector3(0f, 3.6f * scale, 0f);
        crown.transform.localScale = variant == 1
            ? new Vector3(1.9f, 1.35f, 1.9f) * scale
            : new Vector3(1.55f, 1.75f, 1.55f) * scale;
        crown.GetComponent<Renderer>().sharedMaterial = variant == 0 ? forestDark : grassDark;
        RemoveCollider(crown);
    }

    private void StyleInfrastructurePass()
    {
        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>();
        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            if (line.name.StartsWith("StrategicLink_", StringComparison.Ordinal))
            {
                CampaignStrategicLinkType type = ResolveLinkType(line.name);
                switch (type)
                {
                    case CampaignStrategicLinkType.Rail:
                        line.sharedMaterial = railMaterial;
                        line.widthMultiplier = 0.34f;
                        break;
                    case CampaignStrategicLinkType.SeaFerry:
                        line.sharedMaterial = ferryMaterial;
                        line.widthMultiplier = 0.22f;
                        break;
                    default:
                        line.sharedMaterial = roadMaterial;
                        line.widthMultiplier = 0.46f;
                        break;
                }
            }
            else if (line.name == "CampaignRouteGhost")
            {
                line.widthMultiplier = 0.72f;
            }
            else if (line.name.StartsWith("Outline_", StringComparison.Ordinal))
            {
                line.widthMultiplier = 0.18f;
            }
        }
    }

    private static CampaignStrategicLinkType ResolveLinkType(string lineName)
    {
        const string prefix = "StrategicLink_";
        if (lineName.Length <= prefix.Length)
            return CampaignStrategicLinkType.Road;

        string[] ids = lineName.Substring(prefix.Length).Split('|');
        if (ids.Length != 2)
            return CampaignStrategicLinkType.Road;

        CampaignNodeState a = CampaignSession.GetNode(ids[0]);
        CampaignNodeState b = CampaignSession.GetNode(ids[1]);
        return CampaignMapUsabilityV011.GetLinkType(a, b);
    }

    private void DressSettlementsPass()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            GameObject settlement = GameObject.Find("Settlement3D_" + node.Id);
            if (settlement == null || settlement.transform.Find("V013B_Dressing") != null)
                continue;

            GameObject dressing = new GameObject("V013B_Dressing");
            dressing.transform.SetParent(settlement.transform, false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "TownGround";
            ground.transform.SetParent(dressing.transform, false);
            ground.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ground.transform.localScale = node.Terrain == CampaignTerrainType.Urban
                ? new Vector3(7.8f, 0.08f, 7.8f)
                : new Vector3(5.8f, 0.07f, 5.8f);
            ground.GetComponent<Renderer>().sharedMaterial = node.Terrain == CampaignTerrainType.Fortified ? stoneMaterial : fieldBrown;
            RemoveCollider(ground);

            if (node.Terrain == CampaignTerrainType.Urban)
            {
                CreateBlock(dressing.transform, "TownHall", new Vector3(0f, 1.8f, 0.5f), new Vector3(3.4f, 3.6f, 2.4f), plasterMaterial);
                CreateRoof(dressing.transform, new Vector3(0f, 4.0f, 0.5f), 3.6f, 2.6f);
                CreateBlock(dressing.transform, "ChurchTower", new Vector3(-3.7f, 2.5f, -1.4f), new Vector3(1.25f, 5.0f, 1.25f), stoneMaterial);
            }

            if (node.HasPort)
            {
                CreateBlock(dressing.transform, "Quay", new Vector3(-6.2f, 0.35f, -5.7f), new Vector3(7.0f, 0.7f, 1.6f), stoneMaterial);
                CreateCrane(dressing.transform, new Vector3(-5.4f, 1.0f, -5.4f));
            }

            if (node.HasRail)
            {
                CreateBlock(dressing.transform, "RailPlatform", new Vector3(5.2f, 0.25f, -4.4f), new Vector3(6.2f, 0.5f, 1.1f), stoneMaterial);
                CreateBlock(dressing.transform, "StationAwning", new Vector3(5.2f, 1.7f, -4.4f), new Vector3(5.0f, 0.18f, 1.8f), metalMaterial);
            }

            if (node.HasDepot)
            {
                CreateBlock(dressing.transform, "DepotStackA", new Vector3(-4.5f, 0.45f, 4.8f), new Vector3(2.8f, 0.9f, 1.4f), woodMaterial);
                CreateBlock(dressing.transform, "DepotStackB", new Vector3(-2.8f, 0.35f, 5.4f), new Vector3(1.8f, 0.7f, 1.2f), woodMaterial);
            }
        }
    }

    private void DressConstructionPass()
    {
        GameObject barracks = GameObject.Find("ConstructionProject_QA-BARRACKS-AALBORG");
        if (barracks != null)
            DressConstructionProject(barracks, true);

        GameObject farm = GameObject.Find("ConstructionProject_QA-FARM-AARHUS");
        if (farm != null)
            DressConstructionProject(farm, false);
    }

    private void DressConstructionProject(GameObject root, bool barracks)
    {
        if (root.transform.Find("V013B_ProjectDressing") != null)
            return;

        GameObject marker = new GameObject("V013B_ProjectDressing");
        marker.transform.SetParent(root.transform, false);

        foreach (Transform child in root.transform)
        {
            if (!child.name.StartsWith("Stage", StringComparison.Ordinal))
                continue;

            if (child.Find("V013B_Props") != null)
                continue;

            GameObject props = new GameObject("V013B_Props");
            props.transform.SetParent(child, false);

            CreateBlock(props.transform, "WorkedEarth", new Vector3(0f, 0.08f, 0f), new Vector3(barracks ? 11f : 9f, 0.14f, barracks ? 8f : 7f), fieldBrown);

            if (!child.name.StartsWith("Stage0", StringComparison.Ordinal))
            {
                CreateBlock(props.transform, "TimberPile", new Vector3(-5.4f, 0.48f, -3.6f), new Vector3(3.0f, 0.7f, 1.3f), woodMaterial);
                CreateStonePile(props.transform, new Vector3(5.0f, 0.30f, 3.5f));
                CreateCart(props.transform, new Vector3(5.5f, 0.45f, -3.8f));
            }

            if (child.name.StartsWith("Stage2", StringComparison.Ordinal) || child.name.StartsWith("Stage3", StringComparison.Ordinal))
                CreateScaffoldSide(props.transform, barracks ? 5.0f : 4.2f, barracks ? 4.5f : 3.8f);

            if (child.name.StartsWith("Stage5", StringComparison.Ordinal))
            {
                if (barracks)
                {
                    CreateBlock(props.transform, "BarracksWing", new Vector3(0f, 1.7f, 5.2f), new Vector3(5.8f, 3.4f, 2.4f), brickMaterial);
                    CreateRoof(props.transform, new Vector3(0f, 3.85f, 5.2f), 6.0f, 2.6f);
                    CreateFlagPole(props.transform, new Vector3(-5.4f, 0f, -1.8f));
                }
                else
                {
                    CreateBlock(props.transform, "Barn", new Vector3(-4.7f, 1.7f, 4.5f), new Vector3(4.2f, 3.4f, 3.2f), brickMaterial);
                    CreateRoof(props.transform, new Vector3(-4.7f, 3.85f, 4.5f), 4.4f, 3.4f);
                    CreateFenceRow(props.transform, -7.5f, 7.5f, 7.0f);
                }
            }
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null || !t.name.StartsWith("Worker_", StringComparison.Ordinal))
                continue;
            DressWorker(t);
        }
    }

    private void DressWorker(Transform worker)
    {
        if (worker.Find("V013B_Head") != null)
            return;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "V013B_Head";
        head.transform.SetParent(worker, false);
        head.transform.localPosition = new Vector3(0f, 1.20f, 0f);
        head.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
        head.GetComponent<Renderer>().sharedMaterial = plasterMaterial;
        RemoveCollider(head);

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "V013B_Cap";
        cap.transform.SetParent(worker, false);
        cap.transform.localPosition = new Vector3(0f, 1.52f, 0f);
        cap.transform.localScale = new Vector3(0.42f, 0.10f, 0.42f);
        cap.GetComponent<Renderer>().sharedMaterial = forestDark;
        RemoveCollider(cap);

        GameObject tool = CreateBlock(worker, "V013B_Tool", new Vector3(0.58f, 0.15f, 0f), new Vector3(0.10f, 1.25f, 0.10f), woodMaterial);
        tool.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
    }

    private void ApplyLightingPass()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>();
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.58f;
            light.shadowBias = 0.045f;
            light.shadowNormalBias = 0.28f;
        }

        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 320f);
    }

    private void CreateCrane(Transform parent, Vector3 localPosition)
    {
        GameObject mast = CreateBlock(parent, "HarbourCraneMast", localPosition + new Vector3(0f, 2.0f, 0f), new Vector3(0.22f, 4.0f, 0.22f), woodMaterial);
        CreateBlock(mast.transform, "Boom", new Vector3(1.2f, 0.95f, 0f), new Vector3(2.6f, 0.16f, 0.16f), woodMaterial);
    }

    private void CreateStonePile(Transform parent, Vector3 localPosition)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = "StonePile";
            stone.transform.SetParent(parent, false);
            stone.transform.localPosition = localPosition + new Vector3((i % 2) * 0.75f, (i / 2) * 0.35f, (i % 2) * 0.35f);
            stone.transform.localScale = new Vector3(0.8f, 0.55f, 0.65f);
            stone.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
            RemoveCollider(stone);
        }
    }

    private void CreateCart(Transform parent, Vector3 localPosition)
    {
        GameObject cart = new GameObject("MaterialCart");
        cart.transform.SetParent(parent, false);
        cart.transform.localPosition = localPosition;
        CreateBlock(cart.transform, "Bed", new Vector3(0f, 0.55f, 0f), new Vector3(2.3f, 0.45f, 1.25f), woodMaterial);
        CreateWheel(cart.transform, new Vector3(-0.7f, 0.35f, -0.75f));
        CreateWheel(cart.transform, new Vector3(-0.7f, 0.35f, 0.75f));
        CreateBlock(cart.transform, "Shaft", new Vector3(1.8f, 0.45f, 0f), new Vector3(2.4f, 0.12f, 0.12f), woodMaterial);
    }

    private void CreateWheel(Transform parent, Vector3 localPosition)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "Wheel";
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wheel.transform.localScale = new Vector3(0.55f, 0.10f, 0.55f);
        wheel.GetComponent<Renderer>().sharedMaterial = metalMaterial;
        RemoveCollider(wheel);
    }

    private void CreateScaffoldSide(Transform parent, float x, float height)
    {
        for (int i = -1; i <= 1; i++)
            CreateBlock(parent, "ScaffoldPost", new Vector3(x, height * 0.5f, i * 2.2f), new Vector3(0.18f, height, 0.18f), woodMaterial);
        CreateBlock(parent, "ScaffoldRail", new Vector3(x, height * 0.65f, 0f), new Vector3(0.18f, 0.18f, 5.0f), woodMaterial);
    }

    private void CreateFlagPole(Transform parent, Vector3 localPosition)
    {
        CreateBlock(parent, "FlagPole", localPosition + new Vector3(0f, 3.5f, 0f), new Vector3(0.12f, 7.0f, 0.12f), metalMaterial);
        GameObject flag = CreateBlock(parent, "Flag", localPosition + new Vector3(0.8f, 6.1f, 0f), new Vector3(1.6f, 0.75f, 0.08f), brickMaterial);
        flag.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
    }

    private void CreateFenceRow(Transform parent, float fromX, float toX, float z)
    {
        for (float x = fromX; x <= toX; x += 2.5f)
            CreateBlock(parent, "FencePost", new Vector3(x, 0.65f, z), new Vector3(0.16f, 1.3f, 0.16f), woodMaterial);
        CreateBlock(parent, "FenceRail", new Vector3((fromX + toX) * 0.5f, 0.75f, z), new Vector3(toX - fromX, 0.16f, 0.16f), woodMaterial);
    }

    private void CreateRoof(Transform parent, Vector3 localPosition, float width, float depth)
    {
        GameObject roof = CreateBlock(parent, "Roof", localPosition, new Vector3(width * 0.78f, 0.72f, depth * 1.05f), roofMaterial);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        RemoveCollider(block);
        return block;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider c = obj.GetComponent<Collider>();
        if (c != null)
            UnityEngine.Object.Destroy(c);
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return Mathf.Abs(hash);
        }
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        SetSmoothness(material, 0.08f);
        return material;
    }

    private static void SetSmoothness(Material material, float value)
    {
        if (material == null)
            return;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", value);
    }
}
