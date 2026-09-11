using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Independent generated basemap builders for v00.00.10g.
/// The four generated candidates own their own geometry; they do not recolor the
/// v10e Natural Earth renderers.
/// </summary>
public sealed class CampaignGeneratedBasemapV010G : MonoBehaviour
{
    public enum GeneratedKind
    {
        DemHydrology,
        Procedural,
        Diorama,
        Hoi4
    }

    private sealed class WaterFeature
    {
        public string Name;
        public float WidthKm;
        public Vector2[] Points;

        public WaterFeature(string name, float widthKm, params Vector2[] points)
        {
            Name = name;
            WidthKm = widthKm;
            Points = points;
        }
    }

    private sealed class ZoneSeed
    {
        public string Id;
        public float Lon;
        public float Lat;
        public Color Color;

        public ZoneSeed(string id, float lon, float lat, Color color)
        {
            Id = id;
            Lon = lon;
            Lat = lat;
            Color = color;
        }
    }

    private const double MinLat = 54.42;
    private const double MaxLat = 57.88;
    private const double MinLon = 7.50;
    private const double MaxLon = 15.40;
    private const int DemZoom = 7;
    private const int DemGrid = 28;

    private readonly List<WaterFeature> water = new List<WaterFeature>();
    private Vector2[][] denmarkRings;
    private GeneratedKind kind;
    private int providerId;
    private bool configured;
    private bool loadStarted;
    private bool loaded;
    private int expectedTiles;
    private int completedTiles;
    private int failedTiles;

    public string Status
    {
        get
        {
            if (!configured) return "NOT CONFIGURED";
            if (loaded) return failedTiles == 0 ? "READY" : "READY / DEM FALLBACK";
            if (!loadStarted) return "READY TO BUILD";
            if (kind == GeneratedKind.DemHydrology && expectedTiles > 0)
                return string.Format("BUILDING DEM {0}/{1}", completedTiles, expectedTiles);
            return "BUILDING";
        }
    }

    public void Configure(GeneratedKind generatedKind, int id)
    {
        kind = generatedKind;
        providerId = id;
        configured = true;
        denmarkRings = ResolveDenmarkRings();
        BuildWaterFeatures();
    }

    public void EnsureLoaded()
    {
        if (!configured || loadStarted || loaded)
            return;

        loadStarted = true;

        switch (kind)
        {
            case GeneratedKind.DemHydrology:
                StartCoroutine(BuildDemHydrology());
                break;
            case GeneratedKind.Procedural:
                BuildProcedural();
                loaded = true;
                break;
            case GeneratedKind.Diorama:
                BuildDiorama();
                loaded = true;
                break;
            case GeneratedKind.Hoi4:
                BuildHoi4();
                loaded = true;
                break;
        }

        if (loaded)
            Debug.Log("CAMPAIGN-10G|Provider=" + providerId + "|Generated=True|Kind=" + kind);
    }

    private IEnumerator BuildDemHydrology()
    {
        Transform seaRoot = Child("DEM_SEA");
        Transform terrainRoot = Child("DEM_TERRAIN");
        CreateSeaPlane(seaRoot, new Color(0.08f, 0.24f, 0.36f));

        int xMin = LonToTileX(MinLon, DemZoom);
        int xMax = LonToTileX(MaxLon, DemZoom);
        int yMin = LatToTileY(MaxLat, DemZoom);
        int yMax = LatToTileY(MinLat, DemZoom);
        expectedTiles = (xMax - xMin + 1) * (yMax - yMin + 1);

        Material terrainMaterial = CreateLitMaterial(
            new Color(0.30f, 0.43f, 0.24f),
            "V10G_DEM_TERRAIN");

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Texture2D elevation = null;
                string url = string.Format(
                    "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png",
                    DemZoom,
                    x,
                    y);

                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
                {
                    request.timeout = 18;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                        elevation = DownloadHandlerTexture.GetContent(request);
                    else
                        failedTiles++;
                }

                BuildDemTile(terrainRoot, x, y, elevation, terrainMaterial);

                if (elevation != null)
                    Destroy(elevation);

                completedTiles++;
                yield return null;
            }
        }

        BuildWaterRibbons(Child("DEM_HYDROLOGY"), 0.075f);
        loaded = true;

        Debug.Log(
            "CAMPAIGN-10G|Provider=01|DEM=True|Source=MapzenTerrariumAWS|" +
            "HydrologyCut=True|Limfjord=True|Tiles=" + completedTiles +
            "|Failed=" + failedTiles);
    }

    private void BuildDemTile(
        Transform parent,
        int tileX,
        int tileY,
        Texture2D elevation,
        Material material)
    {
        int size = DemGrid + 1;
        Vector3[] vertices = new Vector3[size * size];
        bool[] isLand = new bool[vertices.Length];
        List<int> triangles = new List<int>(DemGrid * DemGrid * 6);

        Color32[] pixels = elevation != null ? elevation.GetPixels32() : null;
        int pw = elevation != null ? elevation.width : 0;
        int ph = elevation != null ? elevation.height : 0;

        double west = TileXToLon(tileX, DemZoom);
        double east = TileXToLon(tileX + 1, DemZoom);
        double north = TileYToLat(tileY, DemZoom);
        double south = TileYToLat(tileY + 1, DemZoom);

        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)DemGrid;
            double lat = north + (south - north) * v;

            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)DemGrid;
                double lon = west + (east - west) * u;
                int index = gy * size + gx;

                bool land = IsLand(lat, lon);
                isLand[index] = land;

                float meters = 0f;
                if (land)
                {
                    if (pixels != null && pw > 0 && ph > 0)
                    {
                        int px = Mathf.Clamp(Mathf.RoundToInt(u * (pw - 1)), 0, pw - 1);
                        int py = Mathf.Clamp(Mathf.RoundToInt((1f - v) * (ph - 1)), 0, ph - 1);
                        Color32 c = pixels[py * pw + px];
                        meters = c.r * 256f + c.g + c.b / 256f - 32768f;
                    }
                    else
                    {
                        meters = FallbackElevation(lat, lon);
                    }
                }

                meters = Mathf.Clamp(meters, 0f, 240f);
                float y = 0.08f + meters * 0.012f;
                vertices[index] = CampaignGeoProjection.Project((float)lon, (float)lat, y);
            }
        }

        for (int gy = 0; gy < DemGrid; gy++)
        {
            for (int gx = 0; gx < DemGrid; gx++)
            {
                int i0 = gy * size + gx;
                int i1 = i0 + 1;
                int i2 = i0 + size;
                int i3 = i2 + 1;

                if (!(isLand[i0] && isLand[i1] && isLand[i2] && isLand[i3]))
                    continue;

                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        if (triangles.Count == 0)
            return;

        GameObject go = new GameObject(
            string.Format("DEM_{0}_{1}_{2}", DemZoom, tileX, tileY));
        go.transform.SetParent(parent, false);

        Mesh mesh = new Mesh
        {
            name = go.name + "_MESH",
            vertices = vertices,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private void BuildProcedural()
    {
        Transform seaRoot = Child("PROC_SEA");
        Transform landRoot = Child("PROC_LAND");
        Transform hydroRoot = Child("PROC_HYDROLOGY");
        Transform dressingRoot = Child("PROC_DRESSING");

        CreateSeaPlane(seaRoot, new Color(0.08f, 0.25f, 0.33f));

        Material grass = CreateLitMaterial(new Color(0.29f, 0.43f, 0.20f), "PROC_GRASS");
        Material heath = CreateLitMaterial(new Color(0.39f, 0.38f, 0.20f), "PROC_HEATH");
        Material field = CreateLitMaterial(new Color(0.50f, 0.47f, 0.24f), "PROC_FIELD");

        BuildSampledLandMesh(
            landRoot,
            0.085,
            0.065,
            0.55f,
            new[] { grass, heath, field },
            true);

        BuildWaterRibbons(hydroRoot, 0.09f);
        BuildProceduralDressing(dressingRoot, 1864107, false);
    }

    private void BuildDiorama()
    {
        Transform seaRoot = Child("DIORAMA_SEA");
        Transform landRoot = Child("DIORAMA_LAND");
        Transform hydroRoot = Child("DIORAMA_HYDROLOGY");
        Transform dressingRoot = Child("DIORAMA_DRESSING");

        CreateSeaPlane(seaRoot, new Color(0.14f, 0.36f, 0.47f));

        Material grass = CreateLitMaterial(new Color(0.37f, 0.50f, 0.23f), "DIORAMA_GRASS");
        Material earth = CreateLitMaterial(new Color(0.46f, 0.39f, 0.23f), "DIORAMA_EARTH");

        BuildSampledLandMesh(
            landRoot,
            0.10,
            0.075,
            1.35f,
            new[] { grass, earth },
            true);

        BuildWaterRibbons(hydroRoot, 0.12f);
        BuildProceduralDressing(dressingRoot, 1864110, true);
        BuildDioramaCities(dressingRoot);
    }

    private void BuildHoi4()
    {
        Transform seaRoot = Child("HOI4_SEA");
        Transform provinceRoot = Child("HOI4_PROVINCES");
        Transform hydroRoot = Child("HOI4_HYDROLOGY");
        Transform borderRoot = Child("HOI4_BORDERS");

        CreateSeaPlane(seaRoot, new Color(0.07f, 0.18f, 0.28f));

        ZoneSeed[] seeds = GetZoneSeeds();
        List<Vector3>[] verts = new List<Vector3>[seeds.Length];
        List<int>[] tris = new List<int>[seeds.Length];

        for (int i = 0; i < seeds.Length; i++)
        {
            verts[i] = new List<Vector3>();
            tris[i] = new List<int>();
        }

        const double stepLon = 0.10;
        const double stepLat = 0.075;

        for (double lat = MinLat; lat < MaxLat; lat += stepLat)
        {
            for (double lon = MinLon; lon < MaxLon; lon += stepLon)
            {
                double cLat = lat + stepLat * 0.5;
                double cLon = lon + stepLon * 0.5;
                if (!IsLand(cLat, cLon))
                    continue;

                int zone = NearestSeed(seeds, cLon, cLat);
                int baseIndex = verts[zone].Count;

                verts[zone].Add(CampaignGeoProjection.Project((float)lon, (float)lat, 0.08f));
                verts[zone].Add(CampaignGeoProjection.Project((float)(lon + stepLon), (float)lat, 0.08f));
                verts[zone].Add(CampaignGeoProjection.Project((float)lon, (float)(lat + stepLat), 0.08f));
                verts[zone].Add(CampaignGeoProjection.Project((float)(lon + stepLon), (float)(lat + stepLat), 0.08f));

                tris[zone].Add(baseIndex);
                tris[zone].Add(baseIndex + 2);
                tris[zone].Add(baseIndex + 1);
                tris[zone].Add(baseIndex + 1);
                tris[zone].Add(baseIndex + 2);
                tris[zone].Add(baseIndex + 3);
            }
        }

        for (int i = 0; i < seeds.Length; i++)
        {
            if (verts[i].Count == 0)
                continue;

            GameObject go = new GameObject("PROVINCE_" + seeds[i].Id);
            go.transform.SetParent(provinceRoot, false);

            Mesh mesh = new Mesh
            {
                name = go.name + "_MESH",
                vertices = verts[i].ToArray(),
                triangles = tris[i].ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateUnlitMaterial(seeds[i].Color, go.name + "_MAT");
        }

        BuildWaterRibbons(hydroRoot, 0.11f);
        BuildHoi4Borders(borderRoot, seeds);
    }

    private void BuildSampledLandMesh(
        Transform parent,
        double stepLon,
        double stepLat,
        float reliefScale,
        Material[] palette,
        bool splitPalette)
    {
        List<Vector3>[] vertices = new List<Vector3>[palette.Length];
        List<int>[] triangles = new List<int>[palette.Length];

        for (int i = 0; i < palette.Length; i++)
        {
            vertices[i] = new List<Vector3>();
            triangles[i] = new List<int>();
        }

        for (double lat = MinLat; lat < MaxLat; lat += stepLat)
        {
            for (double lon = MinLon; lon < MaxLon; lon += stepLon)
            {
                double cLat = lat + stepLat * 0.5;
                double cLon = lon + stepLon * 0.5;
                if (!IsLand(cLat, cLon))
                    continue;

                float n = Mathf.PerlinNoise(
                    (float)((cLon - MinLon) * 0.83 + 7.1),
                    (float)((cLat - MinLat) * 1.17 + 3.9));

                int paletteIndex = splitPalette
                    ? Mathf.Clamp(Mathf.FloorToInt(n * palette.Length), 0, palette.Length - 1)
                    : 0;

                float h00 = SampleGeneratedHeight(lon, lat, reliefScale);
                float h10 = SampleGeneratedHeight(lon + stepLon, lat, reliefScale);
                float h01 = SampleGeneratedHeight(lon, lat + stepLat, reliefScale);
                float h11 = SampleGeneratedHeight(lon + stepLon, lat + stepLat, reliefScale);

                int b = vertices[paletteIndex].Count;
                vertices[paletteIndex].Add(CampaignGeoProjection.Project((float)lon, (float)lat, 0.08f + h00));
                vertices[paletteIndex].Add(CampaignGeoProjection.Project((float)(lon + stepLon), (float)lat, 0.08f + h10));
                vertices[paletteIndex].Add(CampaignGeoProjection.Project((float)lon, (float)(lat + stepLat), 0.08f + h01));
                vertices[paletteIndex].Add(CampaignGeoProjection.Project((float)(lon + stepLon), (float)(lat + stepLat), 0.08f + h11));

                triangles[paletteIndex].Add(b);
                triangles[paletteIndex].Add(b + 2);
                triangles[paletteIndex].Add(b + 1);
                triangles[paletteIndex].Add(b + 1);
                triangles[paletteIndex].Add(b + 2);
                triangles[paletteIndex].Add(b + 3);
            }
        }

        for (int i = 0; i < palette.Length; i++)
        {
            if (vertices[i].Count == 0)
                continue;

            GameObject go = new GameObject("GeneratedLand_" + i);
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh
            {
                name = go.name + "_MESH",
                vertices = vertices[i].ToArray(),
                triangles = triangles[i].ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = palette[i];
        }
    }

    private void BuildProceduralDressing(Transform parent, int seed, bool diorama)
    {
        System.Random random = new System.Random(seed);
        Material tree = CreateLitMaterial(
            diorama ? new Color(0.14f, 0.29f, 0.12f) : new Color(0.12f, 0.25f, 0.10f),
            diorama ? "DIORAMA_TREE" : "PROC_TREE");

        int count = diorama ? 120 : 80;
        for (int i = 0; i < count; i++)
        {
            double lon = MinLon + random.NextDouble() * (13.15 - MinLon);
            double lat = MinLat + random.NextDouble() * (MaxLat - MinLat);
            if (!IsLand(lat, lon))
                continue;

            Vector3 p = CampaignGeoProjection.Project(
                (float)lon,
                (float)lat,
                0.28f + SampleGeneratedHeight(lon, lat, diorama ? 1.35f : 0.55f));

            GameObject treeGo = GameObject.CreatePrimitive(
                diorama ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
            treeGo.name = diorama ? "DioramaTree" : "ProceduralTree";
            treeGo.transform.SetParent(parent, false);
            treeGo.transform.position = p;
            float s = diorama ? 0.22f : 0.14f;
            treeGo.transform.localScale = new Vector3(s, diorama ? 0.34f : 0.28f, s);
            treeGo.GetComponent<Renderer>().sharedMaterial = tree;
            RemoveCollider(treeGo);
        }
    }

    private void BuildDioramaCities(Transform parent)
    {
        Material wall = CreateLitMaterial(new Color(0.72f, 0.63f, 0.49f), "DIORAMA_WALL");
        Material roof = CreateLitMaterial(new Color(0.42f, 0.18f, 0.13f), "DIORAMA_ROOF");

        Vector3[] cities =
        {
            CampaignGeoProjection.Project(9.9217f,57.0488f,0.52f),
            CampaignGeoProjection.Project(10.2039f,56.1629f,0.52f),
            CampaignGeoProjection.Project(10.4024f,55.4038f,0.52f),
            CampaignGeoProjection.Project(12.5683f,55.6761f,0.52f),
            CampaignGeoProjection.Project(9.7526f,55.5657f,0.52f)
        };

        for (int c = 0; c < cities.Length; c++)
        {
            GameObject root = new GameObject("DioramaCity_" + c);
            root.transform.SetParent(parent, false);
            root.transform.position = cities[c];

            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Vector3 local = new Vector3(Mathf.Cos(a) * 0.35f, 0.12f, Mathf.Sin(a) * 0.35f);

                GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
                house.name = "House";
                house.transform.SetParent(root.transform, false);
                house.transform.localPosition = local;
                house.transform.localScale = new Vector3(0.24f, 0.22f, 0.20f);
                house.GetComponent<Renderer>().sharedMaterial = wall;
                RemoveCollider(house);

                GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "Roof";
                top.transform.SetParent(root.transform, false);
                top.transform.localPosition = local + new Vector3(0f, 0.16f, 0f);
                top.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                top.transform.localScale = new Vector3(0.20f, 0.07f, 0.20f);
                top.GetComponent<Renderer>().sharedMaterial = roof;
                RemoveCollider(top);
            }
        }
    }

    private void BuildWaterFeatures()
    {
        water.Clear();

        water.Add(new WaterFeature(
            "Limfjorden",
            5.6f,
            LL(8.22,56.71), LL(8.45,56.70), LL(8.72,56.69), LL(8.95,56.72),
            LL(9.16,56.80), LL(9.34,56.91), LL(9.55,56.99), LL(9.75,57.04),
            LL(9.94,57.05), LL(10.12,57.05), LL(10.31,57.06), LL(10.52,57.07),
            LL(10.72,57.10)));

        water.Add(new WaterFeature(
            "MariagerFjord",
            1.9f,
            LL(9.94,56.65), LL(10.08,56.69), LL(10.28,56.70), LL(10.48,56.70), LL(10.62,56.72)));

        water.Add(new WaterFeature(
            "RandersFjord",
            1.5f,
            LL(10.03,56.46), LL(10.12,56.50), LL(10.22,56.56), LL(10.32,56.62)));

        water.Add(new WaterFeature(
            "VejleFjord",
            1.7f,
            LL(9.54,55.71), LL(9.72,55.69), LL(9.91,55.70), LL(10.08,55.71)));

        water.Add(new WaterFeature(
            "OdenseFjord",
            2.7f,
            LL(10.38,55.43), LL(10.47,55.49), LL(10.53,55.55), LL(10.57,55.61)));

        water.Add(new WaterFeature(
            "RoskildeFjord",
            3.4f,
            LL(12.08,55.64), LL(12.05,55.74), LL(12.00,55.85), LL(11.94,55.96)));

        water.Add(new WaterFeature(
            "Isefjord",
            4.6f,
            LL(11.79,55.72), LL(11.75,55.83), LL(11.72,55.96), LL(11.73,56.08)));

        water.Add(new WaterFeature(
            "RingkobingFjord",
            4.8f,
            LL(8.13,55.82), LL(8.18,55.93), LL(8.22,56.04), LL(8.24,56.12)));

        water.Add(new WaterFeature(
            "NissumFjord",
            3.4f,
            LL(8.15,56.29), LL(8.19,56.37), LL(8.22,56.46)));
    }

    private void BuildWaterRibbons(Transform parent, float y)
    {
        Material waterMaterial = CreateLitMaterial(
            new Color(0.08f, 0.28f, 0.39f),
            "V10G_INLAND_WATER");

        for (int w = 0; w < water.Count; w++)
        {
            WaterFeature feature = water[w];
            if (feature.Points == null || feature.Points.Length < 2)
                continue;

            Vector3[] centres = new Vector3[feature.Points.Length];
            for (int i = 0; i < feature.Points.Length; i++)
                centres[i] = CampaignGeoProjection.Project(feature.Points[i].x, feature.Points[i].y, y);

            float worldUnitsPerKm = ApproxWorldUnitsPerKm(feature.Points[feature.Points.Length / 2]);
            float halfWidth = Mathf.Max(0.06f, feature.WidthKm * worldUnitsPerKm * 0.5f);

            Vector3[] vertices = new Vector3[centres.Length * 2];
            int[] triangles = new int[(centres.Length - 1) * 6];

            for (int i = 0; i < centres.Length; i++)
            {
                Vector3 before = centres[Mathf.Max(0, i - 1)];
                Vector3 after = centres[Mathf.Min(centres.Length - 1, i + 1)];
                Vector3 tangent = after - before;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector3.right;
                tangent.Normalize();

                Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
                vertices[i * 2] = centres[i] - side * halfWidth;
                vertices[i * 2 + 1] = centres[i] + side * halfWidth;
            }

            int t = 0;
            for (int i = 0; i < centres.Length - 1; i++)
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

            GameObject go = new GameObject("Water_" + feature.Name);
            go.transform.SetParent(parent, false);
            Mesh mesh = new Mesh
            {
                name = go.name + "_MESH",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = waterMaterial;
        }
    }

    private bool IsLand(double lat, double lon)
    {
        if (denmarkRings == null || denmarkRings.Length == 0)
            return lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= 13.2;

        Vector2 p = new Vector2((float)lon, (float)lat);
        bool land = false;

        for (int i = 0; i < denmarkRings.Length; i++)
        {
            Vector2[] ring = denmarkRings[i];
            if (ring != null && ring.Length >= 3 && PointInPolygon(p, ring))
            {
                land = true;
                break;
            }
        }

        if (!land)
            return false;

        for (int i = 0; i < water.Count; i++)
        {
            WaterFeature feature = water[i];
            if (DistanceToPolylineKm(lat, lon, feature.Points) <= feature.WidthKm * 0.5f)
                return false;
        }

        return true;
    }

    private static Vector2[][] ResolveDenmarkRings()
    {
        try
        {
            FieldInfo field = typeof(CampaignDenmarkGeography).GetField(
                "DenmarkRings",
                BindingFlags.Static | BindingFlags.NonPublic);

            return field != null ? field.GetValue(null) as Vector2[][] : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-10G|DenmarkRings=False|" + ex.Message);
            return null;
        }
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersects =
                ((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x <
                 (pj.x - pi.x) * (point.y - pi.y) /
                 (Mathf.Abs(pj.y - pi.y) < 0.000001f ? 0.000001f : (pj.y - pi.y)) +
                 pi.x);

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static float DistanceToPolylineKm(
        double lat,
        double lon,
        Vector2[] line)
    {
        if (line == null || line.Length < 2)
            return float.MaxValue;

        float best = float.MaxValue;

        for (int i = 0; i < line.Length - 1; i++)
        {
            double lat0 = line[i].y;
            double lon0 = line[i].x;
            double lat1 = line[i + 1].y;
            double lon1 = line[i + 1].x;
            double refLat = (lat + lat0 + lat1) / 3.0;
            double cos = Math.Cos(refLat * Math.PI / 180.0);

            Vector2 p = new Vector2(
                (float)(lon * 111.32 * cos),
                (float)(lat * 111.32));

            Vector2 a = new Vector2(
                (float)(lon0 * 111.32 * cos),
                (float)(lat0 * 111.32));

            Vector2 b = new Vector2(
                (float)(lon1 * 111.32 * cos),
                (float)(lat1 * 111.32));

            Vector2 ab = b - a;
            float along = ab.sqrMagnitude > 0.000001f
                ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude)
                : 0f;

            best = Mathf.Min(best, Vector2.Distance(p, a + ab * along));
        }

        return best;
    }

    private static float SampleGeneratedHeight(double lon, double lat, float scale)
    {
        float broad = Mathf.PerlinNoise(
            (float)((lon - MinLon) * 0.52 + 4.3),
            (float)((lat - MinLat) * 0.74 + 8.8));

        float fine = Mathf.PerlinNoise(
            (float)((lon - MinLon) * 1.91 + 19.1),
            (float)((lat - MinLat) * 2.10 + 6.4));

        return Mathf.Max(0f, broad * 0.72f + fine * 0.28f - 0.28f) * scale;
    }

    private static float FallbackElevation(double lat, double lon)
    {
        float n = Mathf.PerlinNoise(
            (float)((lon - MinLon) * 0.75 + 3.1),
            (float)((lat - MinLat) * 0.82 + 8.7));

        return Mathf.Clamp(n * 80f - 14f, 0f, 110f);
    }

    private static float ApproxWorldUnitsPerKm(Vector2 lonLat)
    {
        Vector3 a = CampaignGeoProjection.Project(lonLat.x, lonLat.y, 0f);
        Vector3 b = CampaignGeoProjection.Project(lonLat.x + 0.01f, lonLat.y, 0f);

        double latRad = lonLat.y * Math.PI / 180.0;
        double km = 111.32 * Math.Cos(latRad) * 0.01;
        return km > 0.0001 ? Vector3.Distance(a, b) / (float)km : 0.35f;
    }

    private static Vector2 LL(double lon, double lat)
    {
        return new Vector2((float)lon, (float)lat);
    }

    private void CreateSeaPlane(Transform parent, Color color)
    {
        Vector3 sw = CampaignGeoProjection.Project((float)MinLon, (float)MinLat, -0.02f);
        Vector3 ne = CampaignGeoProjection.Project((float)MaxLon, (float)MaxLat, -0.02f);
        Vector3 centre = (sw + ne) * 0.5f;

        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "GeneratedSea";
        sea.transform.SetParent(parent, false);
        sea.transform.position = new Vector3(centre.x, -0.14f, centre.z);
        sea.transform.localScale = new Vector3(
            Mathf.Abs(ne.x - sw.x) + 5f,
            0.20f,
            Mathf.Abs(ne.z - sw.z) + 5f);
        sea.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(color, "GeneratedSeaMat");
        RemoveCollider(sea);
    }

    private Transform Child(string name)
    {
        Transform existing = transform.Find(name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private static Material CreateLitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.04f);
        return material;
    }

    private static Material CreateUnlitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private static int LonToTileX(double lon, int z)
    {
        double n = Math.Pow(2.0, z);
        return (int)Math.Floor((lon + 180.0) / 360.0 * n);
    }

    private static int LatToTileY(double lat, int z)
    {
        double rad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, z);
        return (int)Math.Floor(
            (1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) *
            0.5 * n);
    }

    private static double TileXToLon(int x, int z)
    {
        double n = Math.Pow(2.0, z);
        return x / n * 360.0 - 180.0;
    }

    private static double TileYToLat(int y, int z)
    {
        double n = Math.Pow(2.0, z);
        double mercator = Math.PI * (1.0 - 2.0 * y / n);
        return Math.Atan(Math.Sinh(mercator)) * 180.0 / Math.PI;
    }

    private static ZoneSeed[] GetZoneSeeds()
    {
        return new[]
        {
            new ZoneSeed("DK-VEN", 9.88f,57.36f,new Color(0.47f,0.58f,0.32f)),
            new ZoneSeed("DK-NJ", 9.45f,56.86f,new Color(0.43f,0.53f,0.29f)),
            new ZoneSeed("DK-MJ", 9.25f,56.28f,new Color(0.52f,0.55f,0.31f)),
            new ZoneSeed("DK-VJ", 8.70f,55.82f,new Color(0.48f,0.47f,0.27f)),
            new ZoneSeed("DK-OJ",10.03f,56.10f,new Color(0.55f,0.59f,0.33f)),
            new ZoneSeed("DK-SJ", 9.25f,55.25f,new Color(0.50f,0.44f,0.27f)),
            new ZoneSeed("DK-FYN",10.30f,55.34f,new Color(0.58f,0.54f,0.32f)),
            new ZoneSeed("DK-NSJ",12.13f,55.94f,new Color(0.54f,0.60f,0.35f)),
            new ZoneSeed("DK-KBH",12.5683f,55.6761f,new Color(0.69f,0.58f,0.30f)),
            new ZoneSeed("DK-SSJ",11.82f,55.28f,new Color(0.56f,0.50f,0.31f)),
            new ZoneSeed("DK-LF",11.62f,54.78f,new Color(0.49f,0.56f,0.32f)),
            new ZoneSeed("DK-BOR",14.91f,55.12f,new Color(0.57f,0.46f,0.29f))
        };
    }

    private static int NearestSeed(ZoneSeed[] seeds, double lon, double lat)
    {
        int best = 0;
        double bestD = double.MaxValue;
        double cos = Math.Cos(lat * Math.PI / 180.0);

        for (int i = 0; i < seeds.Length; i++)
        {
            double dx = (seeds[i].Lon - lon) * cos;
            double dy = seeds[i].Lat - lat;
            double d = dx * dx + dy * dy;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return best;
    }

    private void BuildHoi4Borders(Transform parent, ZoneSeed[] seeds)
    {
        Material material = CreateUnlitMaterial(
            new Color(0.88f, 0.78f, 0.50f),
            "HOI4_BORDER");

        int[,] links =
        {
            {0,1},{1,2},{2,3},{2,4},{3,5},{4,5},{5,6},
            {6,7},{6,9},{7,8},{7,9},{9,10}
        };

        for (int i = 0; i < links.GetLength(0); i++)
        {
            ZoneSeed a = seeds[links[i,0]];
            ZoneSeed b = seeds[links[i,1]];

            GameObject go = new GameObject("HOI4_Link_" + a.Id + "_" + b.Id);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.08f;
            line.sharedMaterial = material;
            line.SetPosition(0, CampaignGeoProjection.Project(a.Lon, a.Lat, 0.13f));
            line.SetPosition(1, CampaignGeoProjection.Project(b.Lon, b.Lat, 0.13f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
