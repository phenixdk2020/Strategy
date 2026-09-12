using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 v00.00.10l strategic visual pass.
///
/// Purpose:
/// - Move the Denmark overview away from a raw technical DEM prototype and toward
///   the approved painterly/diorama campaign-map art direction.
/// - Preserve gameplay geography and city data while improving presentation.
/// - Add an illustrative road scaffold that is NOT copied 1:1 from the visual
///   reference and is NOT claimed as exact historical road cartography.
///
/// The reference image is an art-direction target only. Terrain, roads, city
/// positions and gameplay data remain generated from project data/contracts.
/// </summary>
[DefaultExecutionOrder(32400)]
public sealed class Campaign4StrategicVisualPassV010L : MonoBehaviour
{
    private sealed class RoadLink
    {
        public readonly string A;
        public readonly string B;
        public readonly float Curvature;

        public RoadLink(string a, string b, float curvature)
        {
            A = a;
            B = b;
            Curvature = curvature;
        }
    }

    private static readonly Dictionary<string, Vector2> RoadAnchors =
        new Dictionary<string, Vector2>(StringComparer.Ordinal)
        {
            { "Skagen",              new Vector2(10.5839f, 57.7209f) },
            { "Hjørring",            new Vector2( 9.9823f, 57.4642f) },
            { "Aalborg",             new Vector2( 9.9217f, 57.0488f) },
            { "Viborg",              new Vector2( 9.4020f, 56.4532f) },
            { "Randers",             new Vector2(10.0364f, 56.4607f) },
            { "Aarhus",              new Vector2(10.2039f, 56.1629f) },
            { "Horsens",             new Vector2( 9.8503f, 55.8607f) },
            { "Vejle",               new Vector2( 9.5357f, 55.7113f) },
            { "Kolding",             new Vector2( 9.4722f, 55.4904f) },
            { "Fredericia",          new Vector2( 9.7526f, 55.5657f) },
            { "Ribe",                new Vector2( 8.7622f, 55.3305f) },
            { "Varde",               new Vector2( 8.4807f, 55.6211f) },
            { "Middelfart",           new Vector2( 9.7305f, 55.5059f) },
            { "Odense",              new Vector2(10.3883f, 55.3959f) },
            { "Assens",              new Vector2( 9.9008f, 55.2702f) },
            { "Svendborg",           new Vector2(10.6073f, 55.0598f) },
            { "Nyborg",              new Vector2(10.7896f, 55.3127f) },
            { "Kalundborg",          new Vector2(11.0886f, 55.6795f) },
            { "Holbæk",              new Vector2(11.7167f, 55.7167f) },
            { "Slagelse",            new Vector2(11.3546f, 55.4028f) },
            { "Korsør",              new Vector2(11.1386f, 55.3299f) },
            { "Roskilde",            new Vector2(12.0803f, 55.6415f) },
            { "Køge",                new Vector2(12.1821f, 55.4580f) },
            { "Næstved",             new Vector2(11.7609f, 55.2299f) },
            { "Vordingborg",         new Vector2(11.9106f, 55.0080f) },
            { "Hillerød",            new Vector2(12.3083f, 55.9279f) },
            { "Helsingør",           new Vector2(12.5926f, 56.0361f) },
            { "København",           new Vector2(12.5683f, 55.6761f) },
            { "Nakskov",             new Vector2(11.1454f, 54.8304f) },
            { "Maribo",              new Vector2(11.5002f, 54.7744f) },
            { "Nykøbing Falster",    new Vector2(11.8743f, 54.7656f) }
        };

    // Illustrative strategic connectivity only. These links are deliberately
    // project-authored and not traced from the generated reference artwork.
    private static readonly RoadLink[] RoadLinks =
    {
        new RoadLink("Skagen", "Hjørring",  0.08f),
        new RoadLink("Hjørring", "Aalborg", -0.06f),
        new RoadLink("Aalborg", "Randers",   0.05f),
        new RoadLink("Randers", "Aarhus",   -0.05f),
        new RoadLink("Viborg", "Randers",    0.08f),
        new RoadLink("Viborg", "Horsens",   -0.10f),
        new RoadLink("Aarhus", "Horsens",    0.05f),
        new RoadLink("Horsens", "Vejle",    -0.04f),
        new RoadLink("Vejle", "Kolding",     0.05f),
        new RoadLink("Kolding", "Fredericia",-0.04f),
        new RoadLink("Varde", "Ribe",        0.06f),
        new RoadLink("Varde", "Kolding",    -0.09f),

        new RoadLink("Middelfart", "Odense",  0.06f),
        new RoadLink("Odense", "Nyborg",     -0.04f),
        new RoadLink("Assens", "Odense",      0.05f),
        new RoadLink("Odense", "Svendborg",  -0.08f),

        new RoadLink("Kalundborg", "Holbæk",  0.08f),
        new RoadLink("Holbæk", "Roskilde",   -0.06f),
        new RoadLink("Roskilde", "København", 0.04f),
        new RoadLink("Roskilde", "Slagelse", -0.05f),
        new RoadLink("Slagelse", "Korsør",    0.03f),
        new RoadLink("Roskilde", "Køge",      0.06f),
        new RoadLink("Køge", "Næstved",     -0.05f),
        new RoadLink("Næstved", "Vordingborg",0.04f),
        new RoadLink("Hillerød", "Helsingør",-0.05f),
        new RoadLink("Hillerød", "København", 0.07f),

        new RoadLink("Nakskov", "Maribo",     0.05f),
        new RoadLink("Maribo", "Nykøbing Falster", -0.05f)
    };

    private readonly HashSet<Mesh> preparedMeshes = new HashSet<Mesh>();
    private readonly List<Vector3> terrainSamples = new List<Vector3>(4096);

    private Material terrainMaterial;
    private Material seaMaterial;
    private Material inlandWaterMaterial;
    private Material roadMaterial;
    private Material forestMaterial;
    private Material cityMarkerMaterial;
    private Texture2D landTexture;

    private Transform providerRoot;
    private Transform roadRoot;
    private Transform forestRoot;
    private bool environmentStyled;
    private bool roadsBuilt;
    private bool forestsBuilt;
    private bool cityMarkersStyled;
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4StrategicVisualPassV010L>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_v10l_Strategic_Visual_Pass");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4StrategicVisualPassV010L>();
    }

    private void Start()
    {
        BuildMaterials();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 0.50f;

        if (providerRoot == null)
        {
            GameObject provider = GameObject.Find("BASEMAP_01_1_DEM");
            if (provider != null)
                providerRoot = provider.transform;
        }

        if (providerRoot == null || !providerRoot.gameObject.activeInHierarchy)
            return;

        if (!environmentStyled)
            StyleEnvironment();

        PrepareNewDemMeshes();

        if (!cityMarkersStyled)
            StyleCityMarkers();

        // Wait until a useful amount of streamed terrain has arrived before
        // creating dressing so height sampling covers most of Denmark.
        if (preparedMeshes.Count >= 8 && terrainSamples.Count >= 800)
        {
            if (!roadsBuilt)
                BuildRoadNetwork();

            if (!forestsBuilt)
                BuildForestMasses();
        }
    }

    private void BuildMaterials()
    {
        landTexture = BuildLandcoverTexture(512, 512);

        terrainMaterial = CreateLitMaterial(new Color(0.62f, 0.66f, 0.47f), "C4L_Terrain");
        ApplyTexture(terrainMaterial, landTexture);
        SetSmoothness(terrainMaterial, 0.14f);

        seaMaterial = CreateLitMaterial(new Color(0.035f, 0.13f, 0.20f), "C4L_Sea");
        SetSmoothness(seaMaterial, 0.62f);

        inlandWaterMaterial = CreateLitMaterial(new Color(0.045f, 0.18f, 0.25f), "C4L_InlandWater");
        SetSmoothness(inlandWaterMaterial, 0.55f);

        roadMaterial = CreateLitMaterial(new Color(0.42f, 0.34f, 0.22f), "C4L_StrategicRoad");
        SetSmoothness(roadMaterial, 0.08f);

        forestMaterial = CreateLitMaterial(new Color(0.10f, 0.23f, 0.10f), "C4L_ForestMass");
        SetSmoothness(forestMaterial, 0.06f);

        cityMarkerMaterial = CreateLitMaterial(new Color(0.65f, 0.13f, 0.055f), "C4L_CityMarkerRust");
        SetSmoothness(cityMarkerMaterial, 0.32f);
    }

    private void StyleEnvironment()
    {
        RenderSettings.ambientLight = new Color(0.41f, 0.43f, 0.37f);

        Camera camera = Camera.main;
        if (camera != null)
            camera.backgroundColor = new Color(0.025f, 0.095f, 0.14f);

        GameObject sunObject = GameObject.Find("Campaign Sun");
        if (sunObject != null)
        {
            Light sun = sunObject.GetComponent<Light>();
            if (sun != null)
            {
                sun.color = new Color(1.00f, 0.90f, 0.72f);
                sun.intensity = 1.12f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.52f;
            }

            sunObject.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
        }

        Renderer[] renderers = providerRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name;
            string parentName = renderer.transform.parent != null ? renderer.transform.parent.name : string.Empty;

            if (n.StartsWith("DEM_", StringComparison.Ordinal))
                renderer.sharedMaterial = terrainMaterial;
            else if (parentName == "DEM_SEA" || n.IndexOf("Sea", StringComparison.OrdinalIgnoreCase) >= 0)
                renderer.sharedMaterial = seaMaterial;
            else if (parentName == "DEM_HYDROLOGY" || n.StartsWith("Water_", StringComparison.Ordinal))
                renderer.sharedMaterial = inlandWaterMaterial;
        }

        environmentStyled = true;
        Debug.Log("CAMPAIGN4-VISUAL|Version=v00.00.10l|EnvironmentStyled=True|ReferenceCopied=False");
    }

    private void PrepareNewDemMeshes()
    {
        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);

        Vector3 min = CampaignGeoProjection.Project(7.50f, 54.42f, 0f);
        Vector3 max = CampaignGeoProjection.Project(15.40f, 57.88f, 0f);
        float minX = Mathf.Min(min.x, max.x);
        float maxX = Mathf.Max(min.x, max.x);
        float minZ = Mathf.Min(min.z, max.z);
        float maxZ = Mathf.Max(min.z, max.z);

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            GameObject go = filter.gameObject;
            if (go == null || !go.name.StartsWith("DEM_", StringComparison.Ordinal))
                continue;

            Mesh mesh = filter.sharedMesh;
            if (!preparedMeshes.Add(mesh))
                continue;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = new Vector2[vertices.Length];

            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[v]);
                uv[v] = new Vector2(
                    Mathf.InverseLerp(minX, maxX, world.x),
                    Mathf.InverseLerp(minZ, maxZ, world.z));

                if ((v % 6) == 0)
                    terrainSamples.Add(world);
            }

            mesh.uv = uv;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = terrainMaterial;
        }
    }

    private void StyleCityMarkers()
    {
        GameObject root = GameObject.Find("CAMPAIGN4_Cities_1850_Top40");
        if (root == null || root.transform.childCount < 40)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform city = root.transform.GetChild(i);
            if (city == null)
                continue;

            for (int c = 0; c < city.childCount; c++)
            {
                Transform child = city.GetChild(c);
                if (child == null || (child.name != "Marker" && child.name != "Centre"))
                    continue;

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = cityMarkerMaterial;
            }
        }

        cityMarkersStyled = true;
        Debug.Log("CAMPAIGN4-VISUAL|Version=v00.00.10l|CityMarkers=RustRed|Count=40");
    }

    private void BuildRoadNetwork()
    {
        GameObject root = new GameObject("CAMPAIGN4_v10l_StrategicRoads_NONCANON");
        root.transform.SetParent(transform, false);
        roadRoot = root.transform;

        int built = 0;
        for (int i = 0; i < RoadLinks.Length; i++)
        {
            RoadLink link = RoadLinks[i];
            if (!RoadAnchors.TryGetValue(link.A, out Vector2 a) ||
                !RoadAnchors.TryGetValue(link.B, out Vector2 b))
                continue;

            BuildRoad(link, a, b, i);
            built++;
        }

        roadsBuilt = true;
        Debug.Log(
            "CAMPAIGN4-ROADS|Version=v00.00.10l|Links=" + built +
            "|ReferenceTrace=False|HistoricalExact=False|Purpose=StrategicVisualScaffold");
    }

    private void BuildRoad(RoadLink link, Vector2 a, Vector2 b, int index)
    {
        GameObject go = new GameObject("Road_" + index.ToString("00") + "_" + link.A + "_" + link.B);
        go.transform.SetParent(roadRoot, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = roadMaterial;
        line.widthMultiplier = 0.075f;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;

        const int points = 18;
        line.positionCount = points;

        Vector2 direction = b - a;
        Vector2 perpendicular = direction.sqrMagnitude > 0.000001f
            ? new Vector2(-direction.y, direction.x).normalized
            : Vector2.zero;

        float lengthDegrees = direction.magnitude;
        for (int p = 0; p < points; p++)
        {
            float t = p / (float)(points - 1);
            Vector2 ll = Vector2.Lerp(a, b, t);
            float bend = Mathf.Sin(t * Mathf.PI) * link.Curvature * Mathf.Min(1f, lengthDegrees);
            ll += perpendicular * bend;

            Vector3 world = CampaignGeoProjection.Project(ll.x, ll.y, 0f);
            world.y = SampleTerrainHeight(world.x, world.z) + 0.055f;
            line.SetPosition(p, world);
        }
    }

    private void BuildForestMasses()
    {
        GameObject root = new GameObject("CAMPAIGN4_v10l_ForestMasses");
        root.transform.SetParent(transform, false);
        forestRoot = root.transform;

        System.Random random = new System.Random(18511012);
        int created = 0;
        int attempts = 0;

        while (created < 110 && attempts < 1200 && terrainSamples.Count > 0)
        {
            attempts++;
            Vector3 sample = terrainSamples[random.Next(0, terrainSamples.Count)];

            float n = Mathf.PerlinNoise(sample.x * 0.105f + 13.4f, sample.z * 0.105f + 7.9f);
            if (n < 0.59f)
                continue;

            GameObject mass = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mass.name = "ForestMass_" + created.ToString("000");
            mass.transform.SetParent(forestRoot, false);
            mass.transform.position = sample + new Vector3(0f, 0.16f, 0f);

            float sx = Mathf.Lerp(0.25f, 0.60f, (float)random.NextDouble());
            float sz = Mathf.Lerp(0.22f, 0.55f, (float)random.NextDouble());
            float sy = Mathf.Lerp(0.10f, 0.22f, (float)random.NextDouble());
            mass.transform.localScale = new Vector3(sx, sy, sz);
            mass.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f);

            Renderer renderer = mass.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = forestMaterial;

            RemoveCollider(mass);
            created++;
        }

        forestsBuilt = true;
        Debug.Log(
            "CAMPAIGN4-LANDCOVER|Version=v00.00.10l|ForestMasses=" + created +
            "|LandTexture=True|Procedural=True");
    }

    private float SampleTerrainHeight(float x, float z)
    {
        if (terrainSamples.Count == 0)
            return 0.22f;

        float bestDistance = float.MaxValue;
        float bestY = 0.22f;

        for (int i = 0; i < terrainSamples.Count; i++)
        {
            Vector3 s = terrainSamples[i];
            float dx = s.x - x;
            float dz = s.z - z;
            float d = dx * dx + dz * dz;
            if (d >= bestDistance)
                continue;

            bestDistance = d;
            bestY = s.y;
        }

        return bestY;
    }

    private static Texture2D BuildLandcoverTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
        {
            name = "C4L_Procedural_Landcover",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color lowGrass = new Color(0.34f, 0.43f, 0.20f);
        Color fieldGreen = new Color(0.43f, 0.48f, 0.23f);
        Color fieldGold = new Color(0.49f, 0.43f, 0.22f);
        Color heath = new Color(0.34f, 0.32f, 0.18f);
        Color forest = new Color(0.13f, 0.27f, 0.11f);

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);

                float broad = Mathf.PerlinNoise(u * 5.8f + 2.4f, v * 5.3f + 7.1f);
                float fields = Mathf.PerlinNoise(u * 17.0f + 4.2f, v * 15.0f + 1.3f);
                float fine = Mathf.PerlinNoise(u * 42.0f + 8.7f, v * 39.0f + 3.5f);

                Color c;
                if (broad > 0.68f)
                    c = Color.Lerp(forest, lowGrass, Mathf.InverseLerp(0.68f, 0.82f, broad) * 0.30f);
                else if (fields > 0.62f)
                    c = Color.Lerp(fieldGold, fieldGreen, fine);
                else if (broad < 0.31f)
                    c = Color.Lerp(heath, lowGrass, fine * 0.35f);
                else
                    c = Color.Lerp(lowGrass, fieldGreen, fields * 0.55f);

                float micro = Mathf.Lerp(0.91f, 1.07f, fine);
                c *= micro;
                c.a = 1f;
                pixels[y * width + x] = c;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private static Material CreateLitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    private static void ApplyTexture(Material material, Texture2D texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        material.mainTexture = texture;
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

    private static void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
    }
}
