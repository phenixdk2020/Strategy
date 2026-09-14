using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6 - Proposal 3 / Isometric Miniature Town.
///
/// Converts the existing round CITY_* runtime markers into procedural miniature
/// 3D settlements without changing CityId, ZoneId, click semantics or labels.
/// The old primitive renderer is hidden; a stable BoxCollider remains for input.
///
/// C = 3 houses
/// B = 6 buildings + church
/// A = 12 buildings + church + civic centre
///
/// This is a runtime visual foundation. Later versions may replace individual
/// procedural buildings with authored historical assets while keeping the same
/// city-marker/data contract.
/// </summary>
[DefaultExecutionOrder(23500)]
public sealed class CampaignCityIconV010N6 : MonoBehaviour
{
    private const string IconRootName = "CITY_ICON_ISOMETRIC_10N6";
    private const float GroundLift = 0.04f;

    private static Material wallLight;
    private static Material wallWarm;
    private static Material wallStone;
    private static Material roofRed;
    private static Material roofBrown;
    private static Material roofDark;
    private static Material churchRoof;
    private static Material groundMaterial;
    private static Material treeMaterial;

    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignCityIconV010N6>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_CITY_ICONS_10N6");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignCityIconV010N6>();
    }

    private void Update()
    {
        if (installed || GrandCampaignBootstrap.Instance == null)
            return;

        GrandCampaignCityMarker[] markers = Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsSortMode.None);
        if (markers == null || markers.Length == 0)
            return;

        int converted = 0;
        int tierA = 0;
        int tierB = 0;
        int tierC = 0;

        EnsureMaterials();

        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignCityMarker marker = markers[i];
            if (marker == null || string.IsNullOrEmpty(marker.CityId))
                continue;

            CampaignDenmark1851Registry.CityDef city = FindCity(marker.CityId);
            if (city == null)
                continue;

            if (InstallOnMarker(marker.gameObject, city))
            {
                converted++;
                if (city.Tier == CampaignDenmark1851Registry.CityTier.A) tierA++;
                else if (city.Tier == CampaignDenmark1851Registry.CityTier.B) tierB++;
                else tierC++;
            }
        }

        // CITY-REG-01 currently contains 68 cities. Wait for the complete runtime
        // population before declaring the conversion installed.
        if (converted >= CampaignDenmark1851Registry.Cities.Length || CountInstalledIcons() >= CampaignDenmark1851Registry.Cities.Length)
        {
            installed = true;
            Debug.Log(
                CampaignBuildInfo.LogTag +
                "|IsometricCityIcons=True" +
                "|Style=Proposal3_IsometricMiniatureTown" +
                "|Cities=" + CampaignDenmark1851Registry.Cities.Length +
                "|TierA=" + CountTier(CampaignDenmark1851Registry.CityTier.A) +
                "|TierB=" + CountTier(CampaignDenmark1851Registry.CityTier.B) +
                "|TierC=" + CountTier(CampaignDenmark1851Registry.CityTier.C) +
                "|RoundCityRenderer=False" +
                "|StableClickCollider=True");
        }
    }

    private static bool InstallOnMarker(GameObject markerObject, CampaignDenmark1851Registry.CityDef city)
    {
        if (markerObject == null || city == null)
            return false;

        Transform existing = markerObject.transform.Find(IconRootName);
        if (existing != null)
            return false;

        // The old cylinder carried both visuals and the click collider. Keep the
        // marker object/data identity but remove the round visual and replace the
        // collider with a simple invisible box matching the miniature town.
        Renderer rootRenderer = markerObject.GetComponent<Renderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;

        Collider[] colliders = markerObject.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        markerObject.transform.localScale = Vector3.one;

        float iconScale = GetIconScale(city);
        BoxCollider clickBox = markerObject.AddComponent<BoxCollider>();
        clickBox.center = new Vector3(0f, 0.42f, 0f);
        clickBox.size = new Vector3(iconScale * 1.55f, 1.6f, iconScale * 1.40f);

        GameObject root = new GameObject(IconRootName);
        root.transform.SetParent(markerObject.transform, false);
        root.transform.localPosition = new Vector3(0f, GroundLift, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, 35f + StableHash01(city.Id) * 20f, 0f);
        root.transform.localScale = Vector3.one * iconScale;

        CreateSquareGround(root.transform, city.Tier);

        switch (city.Tier)
        {
            case CampaignDenmark1851Registry.CityTier.A:
                BuildTierA(root.transform, city);
                break;
            case CampaignDenmark1851Registry.CityTier.B:
                BuildTierB(root.transform, city);
                break;
            default:
                BuildTierC(root.transform, city);
                break;
        }

        return true;
    }

    private static void BuildTierC(Transform parent, CampaignDenmark1851Registry.CityDef city)
    {
        Vector3[] positions =
        {
            new Vector3(-0.24f, 0f, -0.08f),
            new Vector3( 0.18f, 0f,  0.12f),
            new Vector3( 0.02f, 0f, -0.30f)
        };

        for (int i = 0; i < positions.Length; i++)
            CreateHouse(parent, positions[i], i, city.Id, 0.18f, 0.22f);

        CreateTree(parent, new Vector3(0.33f, 0f, -0.18f), 0.13f);
    }

    private static void BuildTierB(Transform parent, CampaignDenmark1851Registry.CityDef city)
    {
        Vector3[] positions =
        {
            new Vector3(-0.36f, 0f, -0.22f),
            new Vector3(-0.34f, 0f,  0.18f),
            new Vector3( 0.02f, 0f, -0.36f),
            new Vector3( 0.31f, 0f, -0.12f),
            new Vector3( 0.34f, 0f,  0.27f),
            new Vector3(-0.03f, 0f,  0.34f)
        };

        for (int i = 0; i < positions.Length; i++)
            CreateHouse(parent, positions[i], i, city.Id, 0.17f, 0.21f);

        CreateChurch(parent, new Vector3(0f, 0f, 0f), 0.22f, 0.34f);
        CreateTree(parent, new Vector3(0.48f, 0f, 0.02f), 0.12f);
        CreateTree(parent, new Vector3(-0.48f, 0f, 0.02f), 0.11f);
    }

    private static void BuildTierA(Transform parent, CampaignDenmark1851Registry.CityDef city)
    {
        Vector3[] positions =
        {
            new Vector3(-0.52f, 0f, -0.34f),
            new Vector3(-0.23f, 0f, -0.43f),
            new Vector3( 0.12f, 0f, -0.47f),
            new Vector3( 0.47f, 0f, -0.31f),
            new Vector3(-0.55f, 0f,  0.04f),
            new Vector3( 0.52f, 0f,  0.06f),
            new Vector3(-0.49f, 0f,  0.39f),
            new Vector3(-0.16f, 0f,  0.49f),
            new Vector3( 0.19f, 0f,  0.47f),
            new Vector3( 0.50f, 0f,  0.37f),
            new Vector3(-0.21f, 0f, -0.05f),
            new Vector3( 0.24f, 0f,  0.02f)
        };

        for (int i = 0; i < positions.Length; i++)
            CreateHouse(parent, positions[i], i, city.Id, 0.16f, 0.20f);

        CreateChurch(parent, new Vector3(-0.02f, 0f, 0.15f), 0.24f, 0.42f);
        CreateCivicBuilding(parent, new Vector3(0.05f, 0f, -0.20f));

        CreateTree(parent, new Vector3(-0.66f, 0f, -0.05f), 0.13f);
        CreateTree(parent, new Vector3( 0.67f, 0f,  0.18f), 0.13f);
        CreateTree(parent, new Vector3(-0.05f, 0f,  0.65f), 0.11f);
    }

    private static void CreateHouse(
        Transform parent,
        Vector3 localPosition,
        int index,
        string cityId,
        float width,
        float depth)
    {
        float variation = StableHash01(cityId + "_" + index);
        float bodyHeight = Mathf.Lerp(0.16f, 0.25f, variation);
        float buildingWidth = width * Mathf.Lerp(0.88f, 1.14f, StableHash01(cityId + "W" + index));
        float buildingDepth = depth * Mathf.Lerp(0.88f, 1.12f, StableHash01(cityId + "D" + index));
        float yaw = Mathf.Round(StableHash01(cityId + "R" + index) * 3f) * 90f;

        GameObject house = new GameObject("House_" + (index + 1).ToString("D2"));
        house.transform.SetParent(parent, false);
        house.transform.localPosition = localPosition;
        house.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject body = CreatePrimitiveNoCollider(PrimitiveType.Cube, "Body", house.transform);
        body.transform.localScale = new Vector3(buildingWidth, bodyHeight, buildingDepth);
        body.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = variation < 0.34f ? wallWarm : (variation < 0.68f ? wallLight : wallStone);

        GameObject roof = CreateGabledRoof("Roof", house.transform, buildingWidth * 1.10f, buildingDepth * 1.10f, Mathf.Max(0.08f, bodyHeight * 0.46f));
        roof.transform.localPosition = new Vector3(0f, bodyHeight + 0.01f, 0f);
        roof.GetComponent<Renderer>().sharedMaterial = variation < 0.55f ? roofRed : roofBrown;
    }

    private static void CreateChurch(Transform parent, Vector3 localPosition, float width, float towerHeight)
    {
        GameObject church = new GameObject("Church");
        church.transform.SetParent(parent, false);
        church.transform.localPosition = localPosition;

        GameObject nave = CreatePrimitiveNoCollider(PrimitiveType.Cube, "Nave", church.transform);
        nave.transform.localScale = new Vector3(width, 0.25f, width * 1.45f);
        nave.transform.localPosition = new Vector3(0f, 0.125f, 0f);
        nave.GetComponent<Renderer>().sharedMaterial = wallLight;

        GameObject naveRoof = CreateGabledRoof("NaveRoof", church.transform, width * 1.12f, width * 1.58f, 0.12f);
        naveRoof.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        naveRoof.GetComponent<Renderer>().sharedMaterial = churchRoof;

        GameObject tower = CreatePrimitiveNoCollider(PrimitiveType.Cube, "Tower", church.transform);
        tower.transform.localScale = new Vector3(width * 0.58f, towerHeight, width * 0.58f);
        tower.transform.localPosition = new Vector3(0f, towerHeight * 0.5f, -width * 0.52f);
        tower.GetComponent<Renderer>().sharedMaterial = wallStone;

        GameObject spire = CreatePrimitiveNoCollider(PrimitiveType.Cylinder, "Spire", church.transform);
        spire.transform.localScale = new Vector3(width * 0.43f, 0.13f, width * 0.43f);
        spire.transform.localPosition = new Vector3(0f, towerHeight + 0.12f, -width * 0.52f);
        spire.GetComponent<Renderer>().sharedMaterial = roofDark;
    }

    private static void CreateCivicBuilding(Transform parent, Vector3 localPosition)
    {
        GameObject civic = new GameObject("CivicCentre");
        civic.transform.SetParent(parent, false);
        civic.transform.localPosition = localPosition;
        civic.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject body = CreatePrimitiveNoCollider(PrimitiveType.Cube, "Body", civic.transform);
        body.transform.localScale = new Vector3(0.28f, 0.24f, 0.38f);
        body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = wallStone;

        GameObject roof = CreateGabledRoof("Roof", civic.transform, 0.31f, 0.42f, 0.12f);
        roof.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        roof.GetComponent<Renderer>().sharedMaterial = roofDark;
    }

    private static void CreateSquareGround(Transform parent, CampaignDenmark1851Registry.CityTier tier)
    {
        GameObject ground = CreatePrimitiveNoCollider(PrimitiveType.Cube, "TownGround", parent);
        float size = tier == CampaignDenmark1851Registry.CityTier.A ? 1.50f :
                     tier == CampaignDenmark1851Registry.CityTier.B ? 1.18f : 0.82f;
        ground.transform.localScale = new Vector3(size, 0.025f, size * 0.82f);
        ground.transform.localPosition = new Vector3(0f, -0.018f, 0f);
        ground.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
    }

    private static void CreateTree(Transform parent, Vector3 localPosition, float scale)
    {
        GameObject tree = CreatePrimitiveNoCollider(PrimitiveType.Sphere, "Tree", parent);
        tree.transform.localPosition = new Vector3(localPosition.x, scale * 0.62f, localPosition.z);
        tree.transform.localScale = new Vector3(scale, scale * 1.25f, scale);
        tree.GetComponent<Renderer>().sharedMaterial = treeMaterial;
    }

    private static GameObject CreateGabledRoof(string name, Transform parent, float width, float depth, float height)
    {
        GameObject roof = new GameObject(name);
        roof.transform.SetParent(parent, false);

        float x = width * 0.5f;
        float z = depth * 0.5f;

        Vector3[] vertices =
        {
            new Vector3(-x, 0f, -z),
            new Vector3( x, 0f, -z),
            new Vector3(-x, 0f,  z),
            new Vector3( x, 0f,  z),
            new Vector3(0f, height, -z),
            new Vector3(0f, height,  z)
        };

        int[] triangles =
        {
            0, 4, 1,
            2, 3, 5,
            0, 2, 5, 0, 5, 4,
            1, 4, 5, 1, 5, 3,
            0, 1, 3, 0, 3, 2
        };

        Mesh mesh = new Mesh
        {
            name = "GabledRoofMesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = roof.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        roof.AddComponent<MeshRenderer>();
        return roof;
    }

    private static GameObject CreatePrimitiveNoCollider(PrimitiveType type, string name, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        return go;
    }

    private static float GetIconScale(CampaignDenmark1851Registry.CityDef city)
    {
        float baseScale = city.Tier == CampaignDenmark1851Registry.CityTier.A ? 1.04f :
                          city.Tier == CampaignDenmark1851Registry.CityTier.B ? 0.78f : 0.53f;

        float tierPopulationMax = city.Tier == CampaignDenmark1851Registry.CityTier.A ? 0.15f :
                                  city.Tier == CampaignDenmark1851Registry.CityTier.B ? 0.12f : 0.10f;
        float populationFactor = Mathf.Clamp01((Mathf.Log10(Mathf.Max(300, city.Population1850)) - 2.5f) / 3.0f);
        return baseScale * (1f + tierPopulationMax * populationFactor);
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i].Id == cityId)
                return cities[i];
        return null;
    }

    private static int CountInstalledIcons()
    {
        GrandCampaignCityMarker[] markers = Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < markers.Length; i++)
            if (markers[i] != null && markers[i].transform.Find(IconRootName) != null)
                count++;
        return count;
    }

    private static int CountTier(CampaignDenmark1851Registry.CityTier tier)
    {
        int count = 0;
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i].Tier == tier)
                count++;
        return count;
    }

    private static float StableHash01(string value)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static void EnsureMaterials()
    {
        if (wallLight != null)
            return;

        wallLight = CreateMaterial(new Color(0.78f, 0.73f, 0.61f), "CITY10N6_WallLight");
        wallWarm = CreateMaterial(new Color(0.66f, 0.54f, 0.40f), "CITY10N6_WallWarm");
        wallStone = CreateMaterial(new Color(0.55f, 0.56f, 0.52f), "CITY10N6_Stone");
        roofRed = CreateMaterial(new Color(0.48f, 0.19f, 0.12f), "CITY10N6_RoofRed");
        roofBrown = CreateMaterial(new Color(0.31f, 0.20f, 0.13f), "CITY10N6_RoofBrown");
        roofDark = CreateMaterial(new Color(0.17f, 0.18f, 0.17f), "CITY10N6_RoofDark");
        churchRoof = CreateMaterial(new Color(0.20f, 0.24f, 0.23f), "CITY10N6_ChurchRoof");
        groundMaterial = CreateMaterial(new Color(0.31f, 0.35f, 0.24f), "CITY10N6_TownGround");
        treeMaterial = CreateMaterial(new Color(0.20f, 0.31f, 0.17f), "CITY10N6_Tree");
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }
}
