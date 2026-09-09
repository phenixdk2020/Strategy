using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13h — Denmark Premium Visual Pass.
// Presentation-only layer on top of the corrected v13f landmesh + v13e clean world.
// No campaign simulation, ETA, movement, logistics, tactical AI or battle navigation changes.
[DefaultExecutionOrder(3800)]
public sealed class CampaignDenmarkPremiumVisualV013H : MonoBehaviour
{
    public const string BuildTag = "v00.00.13h";
    public const string RootName = "V013H_DENMARK_PREMIUM_VISUAL";

    private static readonly HashSet<string> OverviewLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "AALBORG", "AARHUS", "FREDERICIA", "ODENSE", "CPH"
    };

    private static readonly HashSet<string> RegionalLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING", "VIBORG", "HORSENS", "VEJLE", "KOLDING", "HADERSLEV", "KORSOR", "ROSKILDE"
    };

    private Transform premiumRoot;
    private Transform terrainDetailRoot;
    private Transform vegetationRoot;
    private Transform settlementRoot;
    private Transform constructionRoot;

    private readonly List<Material> terrainMaterials = new List<Material>();
    private readonly List<Material> fieldMaterials = new List<Material>();
    private Material hedgeMaterial;
    private Material trunkMaterial;
    private Material foliageMaterialA;
    private Material foliageMaterialB;
    private Material wallMaterial;
    private Material roofMaterial;
    private Material churchMaterial;
    private Material stationMaterial;
    private Material roadMaterial;
    private Material railMaterial;
    private Material ferryMaterial;
    private Material waterMaterial;
    private Material timberMaterial;
    private Material fenceMaterial;

    private CampaignMapController mapController;
    private FieldInfo nodeLabelStyleField;
    private MethodInfo ensureStylesMethod;
    private GUIStyle hiddenNodeLabelStyle;
    private GUIStyle overviewLabelStyle;
    private GUIStyle regionalLabelStyle;
    private GUIStyle localLabelStyle;

    private Vector2[][] denmarkRings;
    private Color baseWaterColor = new Color(0.075f, 0.19f, 0.29f);
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkPremiumVisualV013H>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkPremiumVisualV013H");
        root.AddComponent<CampaignDenmarkPremiumVisualV013H>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        denmarkRings = GetDenmarkRings();

        // v13g has already hidden foreign presentation and configured close zoom.
        // Keep those scene changes, but stop its GUI/LateUpdate so v13h becomes the sole
        // presentation owner for labels and premium visual maintenance.
        CampaignDenmarkUiZoomFixV013G v13g = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkUiZoomFixV013G>();
        if (v13g != null)
            v13g.enabled = false;

        SetupLegacyLabelSuppression();
        BuildMaterials();
        CreateRootHierarchy();
        TuneExistingLandmesh();
        TuneWater();
        BuildFieldPattern();
        BuildVegetation();
        BuildPremiumSettlements();
        TuneInfrastructure();
        BuildConstructionDressing();
        TuneLightingAndAtmosphere();
        TuneCamera();
        HideNonDenmarkPresentation();
        SuppressLegacyNodeLabels();
        GroundConstructionProjects();
        GroundDenmarkFormations();

        built = true;
        Debug.Log("CAMPAIGN-V013H|PremiumVisual=True|VisibleRegion=DenmarkOnly|TerrainMaterials=Premium|Water=Premium|Fields=True|Vegetation=True|Settlements=PremiumMiniatures|Infrastructure=Premium|ConstructionDressing=True|Labels=Elegant|Atmosphere=True|SimulationChanged=False");
    }

    private void Update()
    {
        if (waterMaterial == null)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.22f);
        Color c = Color.Lerp(baseWaterColor, new Color(0.095f, 0.23f, 0.34f), pulse * 0.32f);
        waterMaterial.color = c;
    }

    private void LateUpdate()
    {
        HideNonDenmarkPresentation();
        SuppressLegacyNodeLabels();
        GroundConstructionProjects();
        GroundDenmarkFormations();
        MaintainCameraSettings();
    }

    private void SetupLegacyLabelSuppression()
    {
        mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController == null)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        nodeLabelStyleField = typeof(CampaignMapController).GetField("nodeLabelStyle", flags);
        ensureStylesMethod = typeof(CampaignMapController).GetMethod("EnsureStyles", flags);
        if (ensureStylesMethod != null)
            ensureStylesMethod.Invoke(mapController, null);

        hiddenNodeLabelStyle = new GUIStyle
        {
            fontSize = 1,
            fixedWidth = 1f,
            fixedHeight = 1f,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        hiddenNodeLabelStyle.normal.textColor = clear;
        hiddenNodeLabelStyle.hover.textColor = clear;
        hiddenNodeLabelStyle.active.textColor = clear;
        hiddenNodeLabelStyle.focused.textColor = clear;
    }

    private void BuildMaterials()
    {
        terrainMaterials.Add(CreateLitMaterial(new Color(0.285f, 0.385f, 0.225f), "V013H_Terrain_Grass"));
        terrainMaterials.Add(CreateLitMaterial(new Color(0.335f, 0.415f, 0.235f), "V013H_Terrain_Meadow"));
        terrainMaterials.Add(CreateLitMaterial(new Color(0.255f, 0.355f, 0.205f), "V013H_Terrain_Heath"));

        fieldMaterials.Add(CreateLitMaterial(new Color(0.49f, 0.48f, 0.25f), "V013H_Field_Rye"));
        fieldMaterials.Add(CreateLitMaterial(new Color(0.43f, 0.46f, 0.24f), "V013H_Field_Grass"));
        fieldMaterials.Add(CreateLitMaterial(new Color(0.56f, 0.50f, 0.29f), "V013H_Field_Earth"));
        fieldMaterials.Add(CreateLitMaterial(new Color(0.38f, 0.44f, 0.22f), "V013H_Field_Pasture"));

        hedgeMaterial = CreateLitMaterial(new Color(0.18f, 0.30f, 0.15f), "V013H_Hedge");
        trunkMaterial = CreateLitMaterial(new Color(0.24f, 0.16f, 0.10f), "V013H_TreeTrunk");
        foliageMaterialA = CreateLitMaterial(new Color(0.16f, 0.29f, 0.14f), "V013H_FoliageA");
        foliageMaterialB = CreateLitMaterial(new Color(0.20f, 0.34f, 0.17f), "V013H_FoliageB");
        wallMaterial = CreateLitMaterial(new Color(0.66f, 0.59f, 0.46f), "V013H_TownWall");
        roofMaterial = CreateLitMaterial(new Color(0.33f, 0.16f, 0.12f), "V013H_TownRoof");
        churchMaterial = CreateLitMaterial(new Color(0.58f, 0.55f, 0.47f), "V013H_Church");
        stationMaterial = CreateLitMaterial(new Color(0.42f, 0.37f, 0.30f), "V013H_Station");
        roadMaterial = CreateLitMaterial(new Color(0.56f, 0.45f, 0.31f), "V013H_Road");
        railMaterial = CreateLitMaterial(new Color(0.12f, 0.12f, 0.11f), "V013H_Rail");
        ferryMaterial = CreateLitMaterial(new Color(0.22f, 0.53f, 0.70f), "V013H_Ferry");
        timberMaterial = CreateLitMaterial(new Color(0.43f, 0.29f, 0.16f), "V013H_Timber");
        fenceMaterial = CreateLitMaterial(new Color(0.34f, 0.25f, 0.16f), "V013H_Fence");

        waterMaterial = CreateLitMaterial(baseWaterColor, "V013H_Water");
        SetSmoothness(waterMaterial, 0.78f);
        if (waterMaterial.HasProperty("_Metallic"))
            waterMaterial.SetFloat("_Metallic", 0.08f);

        foreach (Material material in terrainMaterials)
            SetSmoothness(material, 0.04f);
        foreach (Material material in fieldMaterials)
            SetSmoothness(material, 0.02f);
    }

    private void CreateRootHierarchy()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            UnityEngine.Object.Destroy(old);

        GameObject root = new GameObject(RootName);
        premiumRoot = root.transform;
        terrainDetailRoot = CreateChildRoot(premiumRoot, "01_TERRAIN_DETAILS");
        vegetationRoot = CreateChildRoot(premiumRoot, "02_VEGETATION");
        settlementRoot = CreateChildRoot(premiumRoot, "03_SETTLEMENTS");
        constructionRoot = CreateChildRoot(premiumRoot, "04_CONSTRUCTION_DRESSING");
    }

    private void TuneExistingLandmesh()
    {
        GameObject root = GameObject.Find(CampaignDenmarkLandmeshFixV013F.RootName);
        if (root == null)
            return;

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        int index = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.name.StartsWith("V013F_LandPart_", StringComparison.Ordinal))
                continue;

            renderer.sharedMaterial = terrainMaterials[index % terrainMaterials.Count];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            index++;
        }

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.gameObject.name.StartsWith("V013F_Coast_", StringComparison.Ordinal))
                continue;

            line.widthMultiplier = 0.038f;
            line.startColor = new Color(0.80f, 0.76f, 0.58f, 0.75f);
            line.endColor = line.startColor;
        }
    }

    private void TuneWater()
    {
        GameObject sea = GameObject.Find("V013E_CleanSea");
        if (sea == null)
            return;

        Renderer renderer = sea.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = waterMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void BuildFieldPattern()
    {
        if (denmarkRings == null || denmarkRings.Length == 0)
            return;

        System.Random rng = new System.Random(18641308);
        int created = 0;
        int attempts = 0;

        while (created < 62 && attempts++ < 1500)
        {
            double latitude = 54.72 + rng.NextDouble() * 2.70;
            double longitude = 8.15 + rng.NextDouble() * 4.65;
            if (!IsSafelyInsideDenmark(latitude, longitude, 0.035))
                continue;
            if (NearMajorCity(latitude, longitude, 0.16))
                continue;

            Vector3 pos = CampaignGeoProjection.Project3D(
                latitude,
                longitude,
                CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude) + 0.036f);

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "PremiumField_" + created;
            patch.transform.SetParent(terrainDetailRoot, false);
            patch.transform.position = pos;
            patch.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 180.0), 0f);
            patch.transform.localScale = new Vector3(
                1.15f + (float)rng.NextDouble() * 2.4f,
                0.022f,
                0.85f + (float)rng.NextDouble() * 1.75f);

            Renderer renderer = patch.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = fieldMaterials[created % fieldMaterials.Count];
            RemoveCollider(patch);

            if (created % 3 == 0)
                CreateHedgeStrip(pos, patch.transform.eulerAngles.y, 1.0f + (float)rng.NextDouble() * 1.6f);

            created++;
        }
    }

    private void CreateHedgeStrip(Vector3 centre, float angle, float length)
    {
        GameObject hedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hedge.name = "Hedgerow";
        hedge.transform.SetParent(terrainDetailRoot, false);
        hedge.transform.position = centre + Vector3.up * 0.08f;
        hedge.transform.rotation = Quaternion.Euler(0f, angle + 90f, 0f);
        hedge.transform.localScale = new Vector3(length, 0.14f, 0.10f);
        Renderer renderer = hedge.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = hedgeMaterial;
        RemoveCollider(hedge);
    }

    private void BuildVegetation()
    {
        if (denmarkRings == null || denmarkRings.Length == 0)
            return;

        System.Random rng = new System.Random(18641309);
        int clusters = 0;
        int attempts = 0;

        while (clusters < 26 && attempts++ < 900)
        {
            double latitude = 54.74 + rng.NextDouble() * 2.68;
            double longitude = 8.18 + rng.NextDouble() * 4.58;
            if (!IsSafelyInsideDenmark(latitude, longitude, 0.028))
                continue;
            if (NearMajorCity(latitude, longitude, 0.13))
                continue;

            Vector3 basePos = CampaignGeoProjection.Project3D(
                latitude,
                longitude,
                CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude));

            int treeCount = 4 + rng.Next(0, 5);
            for (int t = 0; t < treeCount; t++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float radius = 0.25f + (float)rng.NextDouble() * 1.35f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                CreateTree(basePos + offset, 0.72f + (float)rng.NextDouble() * 0.42f, (t & 1) == 0);
            }

            clusters++;
        }
    }

    private void CreateTree(Vector3 position, float scale, bool alternate)
    {
        GameObject root = new GameObject("PremiumTree");
        root.transform.SetParent(vegetationRoot, false);
        root.transform.position = position;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.22f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.07f * scale, 0.22f * scale, 0.07f * scale);
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        if (trunkRenderer != null)
            trunkRenderer.sharedMaterial = trunkMaterial;
        RemoveCollider(trunk);

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 0.62f * scale, 0f);
        crown.transform.localScale = new Vector3(0.52f, 0.65f, 0.48f) * scale;
        Renderer crownRenderer = crown.GetComponent<Renderer>();
        if (crownRenderer != null)
            crownRenderer.sharedMaterial = alternate ? foliageMaterialA : foliageMaterialB;
        RemoveCollider(crown);
    }

    private void BuildPremiumSettlements()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node))
                continue;

            SetRenderersVisible(GameObject.Find("V013E_Settlement_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);

            int houses = GetHouseCount(node.Id);
            float scale = GetCityScale(node.Id);
            CreateCityMiniature(node, houses, scale);
        }
    }

    private void CreateCityMiniature(CampaignNodeState node, int houseCount, float scale)
    {
        GameObject city = new GameObject("V013H_City_" + node.Id);
        city.transform.SetParent(settlementRoot, false);
        city.transform.position = new Vector3(
            node.MapPosition.x,
            CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.045f,
            node.MapPosition.y);

        int hash = StableHash(node.Id);
        for (int i = 0; i < houseCount; i++)
        {
            float angle = ((hash + i * 61) % 360) * Mathf.Deg2Rad;
            float ring = i == 0 ? 0f : 0.62f + 0.24f * (i % 4);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * ring, 0f, Mathf.Sin(angle) * ring);
            CreateHouse(city.transform, offset, scale * (0.78f + 0.08f * (i % 3)));
        }

        CreateChurch(city.transform, new Vector3(0.25f, 0f, -0.20f), scale);

        if (node.HasRail)
            CreateStation(city.transform, new Vector3(1.25f * scale, 0f, 0.30f * scale), scale);
        if (node.HasPort)
            CreatePort(city.transform, new Vector3(-1.15f * scale, 0f, -0.65f * scale), scale);
    }

    private void CreateHouse(Transform parent, Vector3 offset, float scale)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "House";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = offset + new Vector3(0f, 0.18f * scale, 0f);
        body.transform.localScale = new Vector3(0.38f, 0.36f, 0.31f) * scale;
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyRenderer.sharedMaterial = wallMaterial;
        RemoveCollider(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = offset + new Vector3(0f, 0.43f * scale, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(0.31f, 0.31f, 0.40f) * scale;
        Renderer roofRenderer = roof.GetComponent<Renderer>();
        if (roofRenderer != null)
            roofRenderer.sharedMaterial = roofMaterial;
        RemoveCollider(roof);
    }

    private void CreateChurch(Transform parent, Vector3 offset, float scale)
    {
        GameObject nave = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nave.name = "ChurchNave";
        nave.transform.SetParent(parent, false);
        nave.transform.localPosition = offset + new Vector3(0f, 0.22f * scale, 0f);
        nave.transform.localScale = new Vector3(0.36f, 0.44f, 0.64f) * scale;
        Renderer nr = nave.GetComponent<Renderer>();
        if (nr != null) nr.sharedMaterial = churchMaterial;
        RemoveCollider(nave);

        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = "ChurchTower";
        tower.transform.SetParent(parent, false);
        tower.transform.localPosition = offset + new Vector3(0f, 0.50f * scale, -0.36f * scale);
        tower.transform.localScale = new Vector3(0.22f, 0.70f, 0.22f) * scale;
        Renderer tr = tower.GetComponent<Renderer>();
        if (tr != null) tr.sharedMaterial = churchMaterial;
        RemoveCollider(tower);

        GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spire.name = "ChurchSpire";
        spire.transform.SetParent(parent, false);
        spire.transform.localPosition = offset + new Vector3(0f, 0.96f * scale, -0.36f * scale);
        spire.transform.localScale = new Vector3(0.12f, 0.24f, 0.12f) * scale;
        Renderer sr = spire.GetComponent<Renderer>();
        if (sr != null) sr.sharedMaterial = roofMaterial;
        RemoveCollider(spire);
    }

    private void CreateStation(Transform parent, Vector3 offset, float scale)
    {
        GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
        station.name = "Station";
        station.transform.SetParent(parent, false);
        station.transform.localPosition = offset + new Vector3(0f, 0.14f * scale, 0f);
        station.transform.localScale = new Vector3(0.70f, 0.28f, 0.24f) * scale;
        Renderer renderer = station.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = stationMaterial;
        RemoveCollider(station);

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = offset + new Vector3(0f, 0.035f, -0.28f * scale);
        platform.transform.localScale = new Vector3(0.95f, 0.05f, 0.12f) * scale;
        Renderer pr = platform.GetComponent<Renderer>();
        if (pr != null) pr.sharedMaterial = railMaterial;
        RemoveCollider(platform);
    }

    private void CreatePort(Transform parent, Vector3 offset, float scale)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Pier";
            pier.transform.SetParent(parent, false);
            pier.transform.localPosition = offset + new Vector3(i * 0.25f * scale, 0.035f, 0f);
            pier.transform.localScale = new Vector3(0.12f, 0.05f, 0.72f) * scale;
            Renderer renderer = pier.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = timberMaterial;
            RemoveCollider(pier);
        }
    }

    private void TuneInfrastructure()
    {
        GameObject cleanRoot = GameObject.Find("V013E_DENMARK_CLEAN_RENDER");
        if (cleanRoot == null)
            return;

        LineRenderer[] lines = cleanRoot.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.gameObject.name.StartsWith("V013E_Link_", StringComparison.Ordinal))
                continue;

            if (!LinkIsDenmarkOnly(line.gameObject.name))
            {
                line.enabled = false;
                continue;
            }

            string materialName = line.sharedMaterial != null ? line.sharedMaterial.name : string.Empty;
            if (materialName.IndexOf("Rail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                line.sharedMaterial = railMaterial;
                line.widthMultiplier = 0.13f;
            }
            else if (materialName.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                line.sharedMaterial = ferryMaterial;
                line.widthMultiplier = 0.10f;
            }
            else
            {
                line.sharedMaterial = roadMaterial;
                line.widthMultiplier = 0.19f;
            }
        }
    }

    private void BuildConstructionDressing()
    {
        CampaignNodeState aalborg = CampaignSession.GetNode("AALBORG");
        CampaignNodeState aarhus = CampaignSession.GetNode("AARHUS");

        if (aalborg != null)
        {
            Vector3 p = new Vector3(
                aalborg.MapPosition.x + 1.30f,
                CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(aalborg.Latitude, aalborg.Longitude) + 0.035f,
                aalborg.MapPosition.y + 0.95f);
            CreateConstructionSiteDressing("AalborgBarracksDressing", p, true);
        }

        if (aarhus != null)
        {
            Vector3 p = new Vector3(
                aarhus.MapPosition.x - 1.20f,
                CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(aarhus.Latitude, aarhus.Longitude) + 0.035f,
                aarhus.MapPosition.y + 0.90f);
            CreateConstructionSiteDressing("AarhusFarmDressing", p, false);
        }
    }

    private void CreateConstructionSiteDressing(string name, Vector3 position, bool barracks)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(constructionRoot, false);
        root.transform.position = position;

        float halfX = barracks ? 1.25f : 1.05f;
        float halfZ = barracks ? 0.85f : 1.00f;
        CreateFence(root.transform, new Vector3(0f, 0.08f, halfZ), new Vector3(halfX * 2f, 0.12f, 0.07f));
        CreateFence(root.transform, new Vector3(0f, 0.08f, -halfZ), new Vector3(halfX * 2f, 0.12f, 0.07f));
        CreateFence(root.transform, new Vector3(halfX, 0.08f, 0f), new Vector3(0.07f, 0.12f, halfZ * 2f));
        CreateFence(root.transform, new Vector3(-halfX, 0.08f, 0f), new Vector3(0.07f, 0.12f, halfZ * 2f));

        for (int i = 0; i < 4; i++)
        {
            GameObject timber = GameObject.CreatePrimitive(PrimitiveType.Cube);
            timber.name = "TimberStack";
            timber.transform.SetParent(root.transform, false);
            timber.transform.localPosition = new Vector3(-0.55f + i * 0.18f, 0.10f + i * 0.02f, -0.42f);
            timber.transform.localScale = new Vector3(0.42f, 0.07f, 0.09f);
            Renderer renderer = timber.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = timberMaterial;
            RemoveCollider(timber);
        }

        if (!barracks)
        {
            GameObject field = GameObject.CreatePrimitive(PrimitiveType.Cube);
            field.name = "FarmField";
            field.transform.SetParent(root.transform, false);
            field.transform.localPosition = new Vector3(1.45f, 0.02f, 0.10f);
            field.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            field.transform.localScale = new Vector3(1.2f, 0.025f, 0.72f);
            Renderer renderer = field.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = fieldMaterials[0];
            RemoveCollider(field);
        }
    }

    private void CreateFence(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fence.name = "Fence";
        fence.transform.SetParent(parent, false);
        fence.transform.localPosition = localPosition;
        fence.transform.localScale = localScale;
        Renderer renderer = fence.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = fenceMaterial;
        RemoveCollider(fence);
    }

    private static void TuneLightingAndAtmosphere()
    {
        RenderSettings.ambientLight = new Color(0.43f, 0.46f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.69f, 0.72f);
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 430f;

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>();
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.intensity = 1.08f;
            light.color = new Color(1.0f, 0.94f, 0.82f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.52f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }
    }

    private static void TuneCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 centre = CampaignGeoProjection.Project3D(55.95, 10.35, 0f);
        Vector3 homePosition = new Vector3(centre.x, 118f, centre.z - 48f);
        Quaternion homeRotation = Quaternion.Euler(58f, 0f, 0f);

        cam.transform.position = homePosition;
        cam.transform.rotation = homeRotation;
        cam.fieldOfView = 41f;
        cam.nearClipPlane = 0.12f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 4.8f;
        controller.MinHeight = 7f;
        controller.MaxHeight = 330f;
        controller.PanSpeed = 50f;
        controller.ZoomSpeed = 120f;
        controller.MinPitch = 27f;
        controller.MaxPitch = 75f;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo homePositionField = typeof(CampaignMapCameraController).GetField("homePosition", flags);
        FieldInfo homeRotationField = typeof(CampaignMapCameraController).GetField("homeRotation", flags);
        if (homePositionField != null) homePositionField.SetValue(controller, homePosition);
        if (homeRotationField != null) homeRotationField.SetValue(controller, homeRotation);
    }

    private static void MaintainCameraSettings()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;

        controller.UseLegacyTerrainFloor = false;
        controller.TerrainClearance = 4.8f;
        controller.MinHeight = 7f;
        controller.MaxHeight = 330f;
    }

    private void OnGUI()
    {
        if (!built)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        EnsureVisibleLabelStyles();
        List<Rect> occupied = new List<Rect>();
        float cameraHeight = cam.transform.position.y;

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node) || !ShouldShowLabel(node.Id, cameraHeight))
                continue;

            float y = CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 1.35f;
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(node.MapPosition.x, y, node.MapPosition.y));
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 44f || guiY > Screen.height - 6f)
                continue;

            bool overview = OverviewLabels.Contains(node.Id);
            bool regional = RegionalLabels.Contains(node.Id);
            GUIStyle style = overview ? overviewLabelStyle : regional ? regionalLabelStyle : localLabelStyle;
            float width = overview ? 94f : regional ? 82f : 76f;
            Rect rect = FindNonOverlappingRect(new Rect(screen.x - width * 0.5f, guiY - 8f, width, 18f), occupied);
            if (rect.yMin < 44f || rect.yMax > Screen.height - 4f)
                continue;

            occupied.Add(rect);
            string text = node.Name;
            if (node.HasPort) text += "  ⚓";
            if (node.HasRail) text += "  ▪";

            GUIStyle shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.78f);
            Rect shadowRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
            GUI.Label(shadowRect, text, shadow);
            GUI.Label(rect, text, style);
        }
    }

    private void EnsureVisibleLabelStyles()
    {
        if (overviewLabelStyle != null)
            return;

        overviewLabelStyle = CreateLabelStyle(11, FontStyle.Bold, new Color(0.96f, 0.93f, 0.83f));
        regionalLabelStyle = CreateLabelStyle(9, FontStyle.Normal, new Color(0.91f, 0.91f, 0.84f));
        localLabelStyle = CreateLabelStyle(8, FontStyle.Normal, new Color(0.82f, 0.84f, 0.78f));
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Overflow
        };
        style.normal.textColor = color;
        return style;
    }

    private void SuppressLegacyNodeLabels()
    {
        if (mapController == null)
            mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController == null || nodeLabelStyleField == null || hiddenNodeLabelStyle == null)
            return;

        nodeLabelStyleField.SetValue(mapController, hiddenNodeLabelStyle);
    }

    private static bool ShouldShowLabel(string nodeId, float cameraHeight)
    {
        if (cameraHeight > 100f)
            return OverviewLabels.Contains(nodeId);
        if (cameraHeight > 48f)
            return OverviewLabels.Contains(nodeId) || RegionalLabels.Contains(nodeId);
        return true;
    }

    private static void HideNonDenmarkPresentation()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || IsDenmarkNode(node))
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("V013E_Settlement_" + node.Id), false);
        }
    }

    private static void GroundDenmarkFormations()
    {
        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>();
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            if (formation == null)
                continue;

            CampaignNodeState node = CampaignSession.GetNode(formation.CurrentNodeId);
            bool visible = IsDenmarkNode(node);
            SetRenderersVisible(view.gameObject, visible);
            if (!visible)
                continue;

            Vector3 p = view.transform.position;
            p.y = HeightFromWorld(p.x, p.z) + 1.65f;
            view.transform.position = p;
        }
    }

    private static void GroundConstructionProjects()
    {
        GroundConstruction("ConstructionProject_QA-BARRACKS-AALBORG", "AALBORG", new Vector2(1.30f, 0.95f), 0.30f);
        GroundConstruction("ConstructionProject_QA-FARM-AARHUS", "AARHUS", new Vector2(-1.20f, 0.90f), 0.31f);
    }

    private static void GroundConstruction(string objectName, string nodeId, Vector2 offset, float scale)
    {
        GameObject root = GameObject.Find(objectName);
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (root == null || !IsDenmarkNode(node))
            return;

        root.transform.position = new Vector3(
            node.MapPosition.x + offset.x,
            CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.04f,
            node.MapPosition.y + offset.y);
        root.transform.localScale = Vector3.one * scale;
        SetRenderersVisible(root, true);
    }

    private bool NearMajorCity(double latitude, double longitude, double threshold)
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node))
                continue;

            double dLat = node.Latitude - latitude;
            double dLon = node.Longitude - longitude;
            if (Math.Sqrt(dLat * dLat + dLon * dLon) < threshold)
                return true;
        }
        return false;
    }

    private bool IsSafelyInsideDenmark(double latitude, double longitude, double margin)
    {
        if (!PointInAnyRing(latitude, longitude))
            return false;

        return PointInAnyRing(latitude + margin, longitude) &&
               PointInAnyRing(latitude - margin, longitude) &&
               PointInAnyRing(latitude, longitude + margin) &&
               PointInAnyRing(latitude, longitude - margin);
    }

    private bool PointInAnyRing(double latitude, double longitude)
    {
        if (denmarkRings == null)
            return false;

        Vector2 p = new Vector2((float)longitude, (float)latitude);
        foreach (Vector2[] ring in denmarkRings)
        {
            if (ring != null && ring.Length >= 3 && PointInPolygon(p, ring))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                              (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x);
            if (intersects)
                inside = !inside;
        }
        return inside;
    }

    private static bool LinkIsDenmarkOnly(string objectName)
    {
        const string prefix = "V013E_Link_";
        if (!objectName.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        string[] ids = objectName.Substring(prefix.Length).Split('|');
        if (ids.Length != 2)
            return false;

        return IsDenmarkNode(CampaignSession.GetNode(ids[0])) && IsDenmarkNode(CampaignSession.GetNode(ids[1]));
    }

    private static int GetHouseCount(string nodeId)
    {
        switch (nodeId)
        {
            case "CPH": return 11;
            case "AARHUS":
            case "AALBORG": return 8;
            case "ODENSE": return 7;
            case "FREDERICIA": return 6;
            case "KOLDING":
            case "HADERSLEV":
            case "ROSKILDE": return 5;
            default: return 4;
        }
    }

    private static float GetCityScale(string nodeId)
    {
        switch (nodeId)
        {
            case "CPH": return 1.18f;
            case "AARHUS":
            case "AALBORG": return 1.05f;
            case "ODENSE": return 1.00f;
            default: return 0.88f;
        }
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

    private static Vector2[][] GetDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField("DenmarkRings", BindingFlags.Static | BindingFlags.NonPublic);
        return field != null ? field.GetValue(null) as Vector2[][] : null;
    }

    private static bool IsDenmarkNode(CampaignNodeState node)
    {
        return node != null && node.Region == CampaignMapRegion.Denmark;
    }

    private static float HeightFromWorld(float x, float z)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        double longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        return CampaignDenmarkCleanRenderV013E.SampleDenmarkHeight(latitude, longitude);
    }

    private static Rect FindNonOverlappingRect(Rect baseRect, List<Rect> occupied)
    {
        Vector2[] offsets =
        {
            Vector2.zero,
            new Vector2(0f, -18f),
            new Vector2(0f, 18f),
            new Vector2(-42f, 0f),
            new Vector2(42f, 0f),
            new Vector2(-38f, -17f),
            new Vector2(38f, -17f),
            new Vector2(-38f, 17f),
            new Vector2(38f, 17f)
        };

        foreach (Vector2 offset in offsets)
        {
            Rect candidate = new Rect(baseRect.x + offset.x, baseRect.y + offset.y, baseRect.width, baseRect.height);
            if (!OverlapsAny(candidate, occupied))
                return candidate;
        }

        return new Rect(baseRect.x, baseRect.y - 52f, baseRect.width, baseRect.height);
    }

    private static bool OverlapsAny(Rect candidate, List<Rect> occupied)
    {
        foreach (Rect occupiedRect in occupied)
        {
            Rect expanded = occupiedRect;
            expanded.xMin -= 2f;
            expanded.xMax += 2f;
            expanded.yMin -= 2f;
            expanded.yMax += 2f;
            if (candidate.Overlaps(expanded))
                return true;
        }
        return false;
    }

    private static Material CreateLitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }

    private static void SetSmoothness(Material material, float value)
    {
        if (material == null)
            return;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", value);
    }

    private static Transform CreateChildRoot(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void SetRenderersVisible(GameObject root, bool visible)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj != null ? obj.GetComponent<Collider>() : null;
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }
}
