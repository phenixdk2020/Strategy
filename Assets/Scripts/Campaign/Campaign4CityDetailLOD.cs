using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 close-zoom city presentation layer.
///
/// The strategic city markers remain cheap at country scale. When the camera is
/// close enough to a city, the marker is replaced by a deterministic stylised
/// 3D settlement made from runtime geometry. This is a performance-friendly LOD
/// foundation that can later swap the generated blocks for authored 1851 prefabs.
/// </summary>
[DefaultExecutionOrder(32100)]
public sealed class Campaign4CityDetailLOD : MonoBehaviour
{
    private sealed class CityLod
    {
        public int Rank;
        public Transform Root;
        public GameObject DetailRoot;
        public Renderer[] MarkerRenderers;
        public bool DetailVisible;
    }

    private readonly List<CityLod> cities = new List<CityLod>();

    private Material roadMaterial;
    private Material plasterMaterial;
    private Material brickMaterial;
    private Material timberMaterial;
    private Material roofMaterial;
    private Material churchMaterial;

    private Camera mapCamera;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4CityDetailLOD>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_City_Detail_LOD");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4CityDetailLOD>();
    }

    private void Start()
    {
        BuildMaterials();
        TryBuild();
    }

    private void Update()
    {
        if (!built)
        {
            TryBuild();
            return;
        }

        if (mapCamera == null)
            mapCamera = Camera.main;
        if (mapCamera == null)
            return;

        UpdateLodVisibility();
    }

    private void BuildMaterials()
    {
        roadMaterial = CreateMaterial(new Color(0.26f, 0.23f, 0.18f), "C4_Road");
        plasterMaterial = CreateMaterial(new Color(0.74f, 0.68f, 0.54f), "C4_Plaster");
        brickMaterial = CreateMaterial(new Color(0.48f, 0.25f, 0.18f), "C4_Brick");
        timberMaterial = CreateMaterial(new Color(0.29f, 0.21f, 0.14f), "C4_Timber");
        roofMaterial = CreateMaterial(new Color(0.38f, 0.12f, 0.08f), "C4_RoofTile");
        churchMaterial = CreateMaterial(new Color(0.66f, 0.63f, 0.54f), "C4_ChurchStone");
    }

    private void TryBuild()
    {
        GameObject cityLayer = GameObject.Find("CAMPAIGN4_Cities_1850_Top40");
        if (cityLayer == null || cityLayer.transform.childCount < 40)
            return;

        cities.Clear();

        for (int i = 0; i < cityLayer.transform.childCount; i++)
        {
            Transform city = cityLayer.transform.GetChild(i);
            if (city == null || !city.name.StartsWith("CITY1850_", StringComparison.Ordinal))
                continue;

            int rank = ParseRank(city.name);
            if (rank <= 0)
                continue;

            Renderer[] markerRenderers = city.GetComponentsInChildren<Renderer>(true);
            GameObject detail = BuildSettlement(city, rank);
            detail.SetActive(false);

            cities.Add(new CityLod
            {
                Rank = rank,
                Root = city,
                DetailRoot = detail,
                MarkerRenderers = markerRenderers,
                DetailVisible = false
            });
        }

        built = cities.Count == 40;
        if (built)
        {
            Debug.Log(
                "CAMPAIGN4-CITYLOD|Installed=True|Cities=40|" +
                "Mode=MarkerToGenerated3DSettlement|AuthorPrefabsLater=True");
        }
    }

    private GameObject BuildSettlement(Transform city, int rank)
    {
        GameObject detail = new GameObject("Detail3D");
        detail.transform.SetParent(city, false);
        detail.transform.localPosition = Vector3.zero;
        detail.transform.localRotation = Quaternion.identity;

        float radius = rank == 1 ? 0.56f : rank <= 5 ? 0.42f : rank <= 15 ? 0.33f : 0.26f;
        int buildingCount = rank == 1 ? 52 : rank <= 5 ? 34 : rank <= 15 ? 20 : 12;

        BuildRoad(detail.transform, new Vector3(0f, 0.025f, 0f), new Vector3(radius * 2.25f, 0.018f, 0.045f), 0f);
        BuildRoad(detail.transform, new Vector3(0f, 0.027f, 0f), new Vector3(radius * 1.85f, 0.018f, 0.040f), 90f);

        System.Random random = new System.Random(StableHash(city.name));
        for (int i = 0; i < buildingCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            float distance = radius * (0.18f + 0.82f * Mathf.Sqrt((float)random.NextDouble()));
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            // Keep the main street crossing readable.
            if (Mathf.Abs(x) < 0.055f) x += x < 0f ? -0.065f : 0.065f;
            if (Mathf.Abs(z) < 0.050f) z += z < 0f ? -0.060f : 0.060f;

            float width = Mathf.Lerp(0.045f, 0.095f, (float)random.NextDouble());
            float depth = Mathf.Lerp(0.040f, 0.085f, (float)random.NextDouble());
            float height = Mathf.Lerp(0.055f, 0.14f, (float)random.NextDouble());
            float rotation = (float)random.NextDouble() * 180f;

            Material wall = SelectWallMaterial(random.Next(0, 3));
            BuildHouse(detail.transform, new Vector3(x, 0.04f, z), width, depth, height, rotation, wall);
        }

        BuildChurch(detail.transform, rank, radius);

        return detail;
    }

    private void BuildRoad(Transform parent, Vector3 localPosition, Vector3 scale, float rotationY)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(parent, false);
        road.transform.localPosition = localPosition;
        road.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
        road.transform.localScale = scale;
        road.GetComponent<Renderer>().sharedMaterial = roadMaterial;
        RemoveCollider(road);
    }

    private void BuildHouse(
        Transform parent,
        Vector3 localPosition,
        float width,
        float depth,
        float height,
        float rotationY,
        Material wallMaterial)
    {
        GameObject root = new GameObject("House");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        body.transform.localScale = new Vector3(width, height, depth);
        body.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        RemoveCollider(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(root.transform, false);
        roof.transform.localPosition = new Vector3(0f, height + 0.016f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(width * 0.72f, width * 0.72f, depth * 1.08f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
        RemoveCollider(roof);
    }

    private void BuildChurch(Transform parent, int rank, float radius)
    {
        float scale = rank == 1 ? 1.35f : rank <= 10 ? 1.0f : 0.82f;

        GameObject nave = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nave.name = "Church_Nave";
        nave.transform.SetParent(parent, false);
        nave.transform.localPosition = new Vector3(radius * 0.08f, 0.075f * scale, -radius * 0.04f);
        nave.transform.localScale = new Vector3(0.13f * scale, 0.15f * scale, 0.25f * scale);
        nave.GetComponent<Renderer>().sharedMaterial = churchMaterial;
        RemoveCollider(nave);

        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = "Church_Tower";
        tower.transform.SetParent(parent, false);
        tower.transform.localPosition = new Vector3(radius * 0.08f, 0.15f * scale, -radius * 0.16f);
        tower.transform.localScale = new Vector3(0.085f * scale, 0.30f * scale, 0.085f * scale);
        tower.GetComponent<Renderer>().sharedMaterial = churchMaterial;
        RemoveCollider(tower);

        GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spire.name = "Church_Spire";
        spire.transform.SetParent(parent, false);
        spire.transform.localPosition = new Vector3(radius * 0.08f, 0.33f * scale, -radius * 0.16f);
        spire.transform.localScale = new Vector3(0.045f * scale, 0.11f * scale, 0.045f * scale);
        spire.GetComponent<Renderer>().sharedMaterial = roofMaterial;
        RemoveCollider(spire);
    }

    private void UpdateLodVisibility()
    {
        Vector3 cameraPosition = mapCamera.transform.position;

        for (int i = 0; i < cities.Count; i++)
        {
            CityLod city = cities[i];
            if (city.Root == null || city.DetailRoot == null)
                continue;

            float threshold = city.Rank == 1 ? 31f : city.Rank <= 10 ? 23f : 17f;
            float distance = Vector3.Distance(cameraPosition, city.Root.position);
            bool showDetail = distance <= threshold;

            if (showDetail == city.DetailVisible)
                continue;

            city.DetailVisible = showDetail;
            city.DetailRoot.SetActive(showDetail);

            for (int r = 0; r < city.MarkerRenderers.Length; r++)
            {
                Renderer renderer = city.MarkerRenderers[r];
                if (renderer != null)
                    renderer.enabled = !showDetail;
            }
        }
    }

    private Material SelectWallMaterial(int index)
    {
        if (index == 0) return plasterMaterial;
        if (index == 1) return brickMaterial;
        return timberMaterial;
    }

    private static int ParseRank(string objectName)
    {
        const string prefix = "CITY1850_";
        if (objectName.Length < prefix.Length + 2)
            return -1;

        string value = objectName.Substring(prefix.Length, 2);
        return int.TryParse(value, out int rank) ? rank : -1;
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return hash;
        }
    }

    private static void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}
