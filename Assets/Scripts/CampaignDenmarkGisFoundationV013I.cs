using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13i — Denmark GIS Terrain Foundation.
//
// First clean GIS-backed map foundation. Elevation is sampled from the public
// Mapzen/Terrain Tiles Terrarium dataset hosted as an AWS public dataset. The
// renderer is deliberately separated from strategic simulation: lat/lon and
// geodesic route state remain authoritative; Unity Y is presentation only.
//
// When online terrain tiles are unavailable, a deterministic low-relief fallback
// is used so the campaign remains usable offline. Downloaded tiles are cached in
// Application.persistentDataPath/PROJECT1864/TerrainCache.
[DefaultExecutionOrder(4200)]
public sealed class CampaignDenmarkGisFoundationV013I : MonoBehaviour
{
    public const string BuildTag = "v00.00.13i";
    public const string RootName = "V013I_DENMARK_GIS_FOUNDATION";

    private const int TerrainZoom = 8;
    private const int GridResolution = 40;
    private const float ElevationToUnity = 0.014f;
    private const float WaterY = 0.12f;

    private const double MinLat = 54.48;
    private const double MaxLat = 57.82;
    private const double MinLon = 7.70;
    private const double MaxLon = 13.05;

    private readonly HashSet<string> overviewLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "AALBORG", "AARHUS", "FREDERICIA", "ODENSE", "CPH"
    };

    private readonly HashSet<string> regionalLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING", "VIBORG", "HORSENS", "VEJLE", "KOLDING", "HADERSLEV",
        "DYBBOEL", "SONDERBORG", "NYBORG", "KORSOR", "ROSKILDE"
    };

    private readonly List<WaterFeature> hydrology = new List<WaterFeature>();
    private Vector2[][] denmarkRings;

    private Transform gisRoot;
    private Transform terrainRoot;
    private Transform waterRoot;
    private Transform infrastructureRoot;
    private Transform settlementRoot;
    private Transform detailRoot;

    private Material[] terrainMaterials;
    private Material seaMaterial;
    private Material inlandWaterMaterial;
    private Material coastMaterial;
    private Material roadMaterial;
    private Material ferryMaterial;
    private Material cityWallMaterial;
    private Material cityRoofMaterial;
    private Material harborMaterial;
    private Material barracksMaterial;
    private Material fieldMaterial;
    private Material treeTrunkMaterial;
    private Material treeCrownMaterial;

    private int totalTiles;
    private int loadedTiles;
    private int downloadedTiles;
    private int cachedTiles;
    private int fallbackTiles;
    private bool terrainReady;
    private bool finishingPassDone;
    private string terrainStatus = "initialiserer GIS-terrain";

    private GUIStyle overviewStyle;
    private GUIStyle regionalStyle;
    private GUIStyle localStyle;
    private GUIStyle statusStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkGisFoundationV013I>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkGisFoundationV013I");
        root.AddComponent<CampaignDenmarkGisFoundationV013I>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        denmarkRings = ResolveDenmarkRings();
        BuildHydrologyData();

        DisableLegacyPresentationSystems();
        HideLegacyPresentation();
        BuildMaterials();
        CreateRootHierarchy();
        CreateSeaBase();
        BuildHydrologySurfaces();
        TuneLighting();
        TuneCamera();

        StartCoroutine(BuildTerrainFromTerrariumTiles());

        Debug.Log("CAMPAIGN-V013I|GISFoundation=True|DEM=MapzenTerrariumAWS|Zoom=8|Hydrology=DenmarkPilot|MapOnly=True|SimulationChanged=False");
    }

    private void Update()
    {
        HideLegacyPresentation();

        if (seaMaterial != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.11f);
            seaMaterial.color = Color.Lerp(
                new Color(0.070f, 0.180f, 0.270f),
                new Color(0.085f, 0.215f, 0.315f),
                pulse * 0.28f);
        }

        if (terrainReady && !finishingPassDone)
        {
            finishingPassDone = true;
            BuildCoastlines();
            BuildHistoricalRoadAndFerryPilot();
            BuildDenmarkSettlements();
            BuildAalborgHarborAndBarracks();
            BuildLandscapeDressing();
            terrainStatus = string.Format(
                "GIS terrain klar · tiles {0}/{1} · cache {2} · download {3} · fallback {4}",
                loadedTiles, totalTiles, cachedTiles, downloadedTiles, fallbackTiles);
        }
    }

    private void LateUpdate()
    {
        HideLegacyPresentation();
    }

    private void BuildMaterials()
    {
        terrainMaterials = new[]
        {
            CreateLitMaterial(new Color(0.30f, 0.41f, 0.27f), "V013I_Terrain_Grass", 0.05f),
            CreateLitMaterial(new Color(0.35f, 0.43f, 0.28f), "V013I_Terrain_Meadow", 0.04f),
            CreateLitMaterial(new Color(0.37f, 0.39f, 0.25f), "V013I_Terrain_Heath", 0.03f)
        };

        seaMaterial = CreateLitMaterial(new Color(0.075f, 0.19f, 0.29f), "V013I_Sea", 0.72f);
        inlandWaterMaterial = CreateLitMaterial(new Color(0.085f, 0.245f, 0.34f), "V013I_InlandWater", 0.78f);
        coastMaterial = CreateUnlitMaterial(new Color(0.62f, 0.66f, 0.48f), "V013I_Coast");
        roadMaterial = CreateUnlitMaterial(new Color(0.45f, 0.34f, 0.23f), "V013I_Road");
        ferryMaterial = CreateUnlitMaterial(new Color(0.42f, 0.66f, 0.72f), "V013I_Ferry");
        cityWallMaterial = CreateLitMaterial(new Color(0.56f, 0.52f, 0.43f), "V013I_CityWall", 0.08f);
        cityRoofMaterial = CreateLitMaterial(new Color(0.28f, 0.15f, 0.12f), "V013I_CityRoof", 0.05f);
        harborMaterial = CreateLitMaterial(new Color(0.25f, 0.22f, 0.18f), "V013I_Harbor", 0.04f);
        barracksMaterial = CreateLitMaterial(new Color(0.48f, 0.31f, 0.22f), "V013I_Barracks", 0.05f);
        fieldMaterial = CreateLitMaterial(new Color(0.48f, 0.47f, 0.27f), "V013I_Field", 0.03f);
        treeTrunkMaterial = CreateLitMaterial(new Color(0.20f, 0.13f, 0.08f), "V013I_TreeTrunk", 0.02f);
        treeCrownMaterial = CreateLitMaterial(new Color(0.16f, 0.29f, 0.15f), "V013I_TreeCrown", 0.02f);
    }

    private void CreateRootHierarchy()
    {
        GameObject root = new GameObject(RootName);
        gisRoot = root.transform;

        terrainRoot = CreateChild(gisRoot, "L1_GIS_TERRAIN");
        waterRoot = CreateChild(gisRoot, "L2_HYDROLOGY");
        infrastructureRoot = CreateChild(gisRoot, "L4_HISTORICAL_INFRASTRUCTURE_PILOT");
        settlementRoot = CreateChild(gisRoot, "L5_SETTLEMENTS");
        detailRoot = CreateChild(gisRoot, "L7_DIORAMA_DETAILS");
    }

    private void CreateSeaBase()
    {
        Vector3 southWest = CampaignGeoProjection.Project3D(MinLat, MinLon, WaterY - 0.10f);
        Vector3 northEast = CampaignGeoProjection.Project3D(MaxLat, MaxLon, WaterY - 0.10f);
        Vector3 centre = (southWest + northEast) * 0.5f;

        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "V013I_SeaBase";
        sea.transform.SetParent(waterRoot, false);
        sea.transform.position = new Vector3(centre.x, WaterY - 0.28f, centre.z);
        sea.transform.localScale = new Vector3(
            Mathf.Abs(northEast.x - southWest.x) + 36f,
            0.45f,
            Mathf.Abs(northEast.z - southWest.z) + 36f);
        sea.GetComponent<Renderer>().sharedMaterial = seaMaterial;
        RemoveCollider(sea);
    }

    private IEnumerator BuildTerrainFromTerrariumTiles()
    {
        int xMin = LonToTileX(MinLon, TerrainZoom);
        int xMax = LonToTileX(MaxLon, TerrainZoom);
        int yMin = LatToTileY(MaxLat, TerrainZoom);
        int yMax = LatToTileY(MinLat, TerrainZoom);
        totalTiles = (xMax - xMin + 1) * (yMax - yMin + 1);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Texture2D texture = null;
                bool fromCache = false;
                string cachePath = GetTerrainCachePath(TerrainZoom, x, y);

                if (File.Exists(cachePath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(cachePath);
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                        texture.LoadImage(bytes, false);
                        fromCache = true;
                        cachedTiles++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("CAMPAIGN-V013I|TerrainCacheRead=False|" + ex.Message);
                        if (texture != null) Destroy(texture);
                        texture = null;
                    }
                }

                if (texture == null)
                {
                    string url = string.Format(
                        "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png",
                        TerrainZoom, x, y);

                    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, false))
                    {
                        request.timeout = 12;
                        yield return request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            texture = DownloadHandlerTexture.GetContent(request);
                            downloadedTiles++;

                            try
                            {
                                string directory = Path.GetDirectoryName(cachePath);
                                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                                byte[] bytes = request.downloadHandler.data;
                                if (bytes != null && bytes.Length > 0) File.WriteAllBytes(cachePath, bytes);
                            }
                            catch (Exception ex)
                            {
                                Debug.Log("CAMPAIGN-V013I|TerrainCacheWrite=False|" + ex.Message);
                            }
                        }
                    }
                }

                if (texture == null)
                    fallbackTiles++;

                BuildTerrainTileMesh(x, y, texture, fromCache);
                loadedTiles++;
                terrainStatus = string.Format("bygger GIS terrain {0}/{1}", loadedTiles, totalTiles);

                if (texture != null)
                    Destroy(texture);

                yield return null;
            }
        }

        terrainReady = true;
    }

    private void BuildTerrainTileMesh(int tileX, int tileY, Texture2D elevationTexture, bool fromCache)
    {
        int size = GridResolution + 1;
        Vector3[] vertices = new Vector3[size * size];
        bool[] land = new bool[vertices.Length];
        List<int> triangles = new List<int>(GridResolution * GridResolution * 6);

        double west = TileXToLon(tileX, TerrainZoom);
        double east = TileXToLon(tileX + 1, TerrainZoom);
        double north = TileYToLat(tileY, TerrainZoom);
        double south = TileYToLat(tileY + 1, TerrainZoom);

        Color32[] pixels = elevationTexture != null ? elevationTexture.GetPixels32() : null;
        int pixelWidth = elevationTexture != null ? elevationTexture.width : 0;
        int pixelHeight = elevationTexture != null ? elevationTexture.height : 0;

        for (int gy = 0; gy < size; gy++)
        {
            float v = gy / (float)GridResolution;
            double lat = Lerp(north, south, v);

            for (int gx = 0; gx < size; gx++)
            {
                float u = gx / (float)GridResolution;
                double lon = Lerp(west, east, u);
                int index = gy * size + gx;

                bool isLand = IsDenmarkLand(lat, lon) && !IsHydrologyWater(lat, lon);
                land[index] = isLand;

                float elevationMeters = 0f;
                if (isLand)
                {
                    if (pixels != null && pixelWidth > 0 && pixelHeight > 0)
                    {
                        int px = Mathf.Clamp(Mathf.RoundToInt(u * (pixelWidth - 1)), 0, pixelWidth - 1);
                        int py = Mathf.Clamp(Mathf.RoundToInt((1f - v) * (pixelHeight - 1)), 0, pixelHeight - 1);
                        Color32 c = pixels[py * pixelWidth + px];
                        elevationMeters = c.r * 256f + c.g + c.b / 256f - 32768f;
                    }
                    else
                    {
                        elevationMeters = FallbackElevationMeters(lat, lon);
                    }
                }

                elevationMeters = Mathf.Clamp(elevationMeters, 0f, 240f);
                float renderY = WaterY + 0.08f + elevationMeters * ElevationToUnity;
                vertices[index] = CampaignGeoProjection.Project3D(lat, lon, renderY);
            }
        }

        for (int gy = 0; gy < GridResolution; gy++)
        {
            for (int gx = 0; gx < GridResolution; gx++)
            {
                int i0 = gy * size + gx;
                int i1 = i0 + 1;
                int i2 = i0 + size;
                int i3 = i2 + 1;

                if (!(land[i0] && land[i1] && land[i2] && land[i3]))
                    continue;

                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        if (triangles.Count == 0)
            return;

        GameObject tile = new GameObject(string.Format("GIS_Terrain_{0}_{1}_{2}", TerrainZoom, tileX, tileY));
        tile.transform.SetParent(terrainRoot, false);

        Mesh mesh = new Mesh
        {
            name = tile.name + "_Mesh",
            vertices = vertices,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (AverageNormalY(mesh.normals) < 0f)
        {
            int[] reversed = mesh.triangles;
            for (int i = 0; i < reversed.Length; i += 3)
            {
                int tmp = reversed[i + 1];
                reversed[i + 1] = reversed[i + 2];
                reversed[i + 2] = tmp;
            }
            mesh.triangles = reversed;
            mesh.RecalculateNormals();
        }

        MeshFilter filter = tile.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = tile.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = terrainMaterials[Math.Abs(tileX + tileY) % terrainMaterials.Length];

        MeshCollider collider = tile.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    private void BuildHydrologyData()
    {
        hydrology.Add(new WaterFeature("Limfjorden", 5.6f, new[]
        {
            LL(8.22,56.71), LL(8.45,56.70), LL(8.72,56.69), LL(8.95,56.72),
            LL(9.16,56.80), LL(9.34,56.91), LL(9.55,56.99), LL(9.75,57.04),
            LL(9.94,57.05), LL(10.12,57.05), LL(10.31,57.06), LL(10.52,57.07), LL(10.72,57.10)
        }));
        hydrology.Add(new WaterFeature("Mariager Fjord", 1.9f, new[]
        {
            LL(9.94,56.65), LL(10.08,56.69), LL(10.28,56.70), LL(10.48,56.70), LL(10.62,56.72)
        }));
        hydrology.Add(new WaterFeature("Randers Fjord", 1.5f, new[]
        {
            LL(10.03,56.46), LL(10.12,56.50), LL(10.22,56.56), LL(10.32,56.62)
        }));
        hydrology.Add(new WaterFeature("Horsens Fjord", 1.8f, new[]
        {
            LL(9.84,55.86), LL(10.02,55.85), LL(10.22,55.87)
        }));
        hydrology.Add(new WaterFeature("Vejle Fjord", 1.7f, new[]
        {
            LL(9.54,55.71), LL(9.72,55.69), LL(9.91,55.70), LL(10.08,55.71)
        }));
        hydrology.Add(new WaterFeature("Kolding Fjord", 1.2f, new[]
        {
            LL(9.47,55.49), LL(9.61,55.50), LL(9.75,55.51)
        }));
        hydrology.Add(new WaterFeature("Odense Fjord", 2.7f, new[]
        {
            LL(10.38,55.43), LL(10.47,55.49), LL(10.53,55.55), LL(10.57,55.61)
        }));
        hydrology.Add(new WaterFeature("Roskilde Fjord", 3.4f, new[]
        {
            LL(12.08,55.64), LL(12.05,55.74), LL(12.00,55.85), LL(11.94,55.96)
        }));
        hydrology.Add(new WaterFeature("Isefjord", 4.6f, new[]
        {
            LL(11.79,55.72), LL(11.75,55.83), LL(11.72,55.96), LL(11.73,56.08)
        }));
        hydrology.Add(new WaterFeature("Ringkøbing Fjord", 4.8f, new[]
        {
            LL(8.13,55.82), LL(8.18,55.93), LL(8.22,56.04), LL(8.24,56.12)
        }));
        hydrology.Add(new WaterFeature("Nissum Fjord", 3.4f, new[]
        {
            LL(8.15,56.29), LL(8.19,56.37), LL(8.22,56.46)
        }));
    }

    private void BuildHydrologySurfaces()
    {
        foreach (WaterFeature feature in hydrology)
            CreateWaterRibbon(feature);
    }

    private void CreateWaterRibbon(WaterFeature feature)
    {
        if (feature.Points == null || feature.Points.Length < 2)
            return;

        Vector3[] centres = new Vector3[feature.Points.Length];
        for (int i = 0; i < feature.Points.Length; i++)
            centres[i] = CampaignGeoProjection.Project3D(feature.Points[i].y, feature.Points[i].x, WaterY + 0.025f);

        Vector3[] vertices = new Vector3[feature.Points.Length * 2];
        int[] triangles = new int[(feature.Points.Length - 1) * 6];

        double avgLat = 0.0;
        double avgLon = 0.0;
        for (int i = 0; i < feature.Points.Length; i++)
        {
            avgLon += feature.Points[i].x;
            avgLat += feature.Points[i].y;
        }
        avgLon /= feature.Points.Length;
        avgLat /= feature.Points.Length;
        float worldUnitsPerKm = WorldUnitsPerKm(avgLat, avgLon);
        float halfWidth = Mathf.Max(0.12f, feature.WidthKm * worldUnitsPerKm * 0.5f);

        for (int i = 0; i < centres.Length; i++)
        {
            Vector3 before = centres[Mathf.Max(0, i - 1)];
            Vector3 after = centres[Mathf.Min(centres.Length - 1, i + 1)];
            Vector3 tangent = after - before;
            tangent.y = 0f;
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
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
            triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
            triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
        }

        GameObject root = new GameObject("GIS_Water_" + feature.Name.Replace(" ", "_"));
        root.transform.SetParent(waterRoot, false);
        Mesh mesh = new Mesh { name = root.name + "_Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshFilter filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = inlandWaterMaterial;
    }

    private void BuildCoastlines()
    {
        if (denmarkRings == null)
            return;

        for (int r = 0; r < denmarkRings.Length; r++)
        {
            Vector2[] ring = denmarkRings[r];
            if (ring == null || ring.Length < 3)
                continue;

            GameObject root = new GameObject("GIS_Coast_" + r);
            root.transform.SetParent(detailRoot, false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.widthMultiplier = 0.07f;
            line.positionCount = ring.Length;
            line.sharedMaterial = coastMaterial;
            line.numCornerVertices = 2;

            for (int i = 0; i < ring.Length; i++)
            {
                Vector3 world = CampaignGeoProjection.Project3D(ring[i].y, ring[i].x, WaterY + 0.16f);
                world.y = SampleGroundHeight(world.x, world.z, WaterY + 0.20f) + 0.035f;
                line.SetPosition(i, world);
            }
        }
    }

    private void BuildHistoricalRoadAndFerryPilot()
    {
        // Explicit Denmark-only visual network. We intentionally do not draw generic
        // graph edges, which previously produced a straight Roskilde-Fredericia line
        // across open water. Crossings are rendered as explicit ferry legs.
        BuildRoad("HJORRING", "AALBORG");
        BuildRoad("AALBORG", "VIBORG");
        BuildRoadVia("AALBORG", "AARHUS", new[] { LL(9.95,56.64), LL(10.03,56.46), LL(10.16,56.26) });
        BuildRoad("VIBORG", "AARHUS");
        BuildRoad("AARHUS", "HORSENS");
        BuildRoad("HORSENS", "VEJLE");
        BuildRoad("VEJLE", "FREDERICIA");
        BuildRoad("FREDERICIA", "KOLDING");
        BuildRoad("KOLDING", "HADERSLEV");
        BuildRoad("HADERSLEV", "DYBBOEL");
        BuildRoad("ODENSE", "NYBORG");
        BuildRoad("KORSOR", "ROSKILDE");
        BuildRoad("ROSKILDE", "CPH");

        CampaignNodeState fredericia = CampaignSession.GetNode("FREDERICIA");
        CampaignNodeState odense = CampaignSession.GetNode("ODENSE");
        if (fredericia != null && odense != null)
        {
            Vector2 middelfart = LL(9.73,55.51);
            CreateRouteLine("GIS_Ferry_Fredericia_Middelfart", new[]
            {
                NodeLonLat(fredericia), middelfart
            }, ferryMaterial, 0.12f, true);
            CreateRouteLine("GIS_Road_Middelfart_Odense", new[]
            {
                middelfart, NodeLonLat(odense)
            }, roadMaterial, 0.15f, false);
        }

        CampaignNodeState nyborg = CampaignSession.GetNode("NYBORG");
        CampaignNodeState korsor = CampaignSession.GetNode("KORSOR");
        if (nyborg != null && korsor != null)
        {
            CreateRouteLine("GIS_Ferry_Nyborg_Korsor", new[]
            {
                NodeLonLat(nyborg), NodeLonLat(korsor)
            }, ferryMaterial, 0.12f, true);
        }
    }

    private void BuildRoad(string aId, string bId)
    {
        CampaignNodeState a = CampaignSession.GetNode(aId);
        CampaignNodeState b = CampaignSession.GetNode(bId);
        if (!IsDenmarkNode(a) || !IsDenmarkNode(b)) return;
        CreateRouteLine("GIS_Road_" + aId + "_" + bId, new[] { NodeLonLat(a), NodeLonLat(b) }, roadMaterial, 0.15f, false);
    }

    private void BuildRoadVia(string aId, string bId, Vector2[] via)
    {
        CampaignNodeState a = CampaignSession.GetNode(aId);
        CampaignNodeState b = CampaignSession.GetNode(bId);
        if (!IsDenmarkNode(a) || !IsDenmarkNode(b)) return;

        List<Vector2> points = new List<Vector2> { NodeLonLat(a) };
        if (via != null) points.AddRange(via);
        points.Add(NodeLonLat(b));
        CreateRouteLine("GIS_Road_" + aId + "_" + bId, points.ToArray(), roadMaterial, 0.15f, false);
    }

    private void CreateRouteLine(string name, Vector2[] lonLat, Material material, float width, bool overWater)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(infrastructureRoot, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.positionCount = lonLat.Length;
        line.numCornerVertices = 3;

        for (int i = 0; i < lonLat.Length; i++)
        {
            Vector3 p = CampaignGeoProjection.Project3D(lonLat[i].y, lonLat[i].x, WaterY + 0.25f);
            if (!overWater)
                p.y = SampleGroundHeight(p.x, p.z, WaterY + 0.22f) + 0.08f;
            else
                p.y = WaterY + 0.10f;
            line.SetPosition(i, p);
        }
    }

    private void BuildDenmarkSettlements()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node))
                continue;

            if (node.Id == "AALBORG")
                continue;

            Vector3 centre = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, WaterY + 0.3f);
            centre.y = SampleGroundHeight(centre.x, centre.z, WaterY + 0.25f) + 0.06f;

            bool major = overviewLabels.Contains(node.Id);
            int houseCount = major ? 8 : regionalLabels.Contains(node.Id) ? 5 : 3;
            CreateTownDiorama("GIS_City_" + node.Id, centre, houseCount, major ? 0.52f : 0.40f, node.HasPort);
        }
    }

    private void BuildAalborgHarborAndBarracks()
    {
        CampaignNodeState aalborg = CampaignSession.GetNode("AALBORG");
        if (aalborg == null)
            return;

        Vector3 city = CampaignGeoProjection.Project3D(aalborg.Latitude, aalborg.Longitude, WaterY + 0.3f);
        city.y = SampleGroundHeight(city.x, city.z, WaterY + 0.25f) + 0.06f;
        CreateTownDiorama("GIS_City_AALBORG", city, 12, 0.58f, true);

        // Aalborg is placed deliberately as a Limfjord harbour city. South-bank quays,
        // warehouses and a small ferry are shown. No modern Limfjord road bridge is
        // fabricated for the 1864 presentation.
        Vector3 harbour = CampaignGeoProjection.Project3D(57.055, 9.920, WaterY + 0.14f);
        harbour.y = WaterY + 0.09f;
        CreateBox(detailRoot, "Aalborg_Quay_West", harbour + new Vector3(-1.3f, 0f, -0.30f), new Vector3(2.4f, 0.10f, 0.26f), harborMaterial);
        CreateBox(detailRoot, "Aalborg_Quay_East", harbour + new Vector3(1.3f, 0f, -0.30f), new Vector3(2.4f, 0.10f, 0.26f), harborMaterial);

        for (int i = 0; i < 4; i++)
        {
            Vector3 p = city + new Vector3(-1.6f + i * 1.05f, 0.10f, 1.25f);
            CreateHouse(detailRoot, "Aalborg_Warehouse_" + i, p, 0.62f, 0.42f, 0.90f);
        }

        Vector3 norresundby = CampaignGeoProjection.Project3D(57.0585, 9.9225, WaterY + 0.2f);
        norresundby.y = SampleGroundHeight(norresundby.x, norresundby.z, WaterY + 0.24f) + 0.05f;
        CreateTownDiorama("GIS_Decorative_NORRESUNDBY", norresundby + new Vector3(0f, 0f, 1.6f), 4, 0.35f, true);

        CreateBoat("Aalborg_Limfjord_Ferry", harbour + new Vector3(0.3f, 0.08f, 0.10f), 0.50f);
        CreateBoat("Aalborg_Harbor_Boat", harbour + new Vector3(-1.4f, 0.08f, 0.35f), 0.38f);

        // Highly visible construction/barracks compound south of the harbour. This is
        // visual dressing only; authoritative staged construction remains in CampaignConstructionV013.
        Vector3 barracks = city + new Vector3(-2.2f, 0.02f, -2.2f);
        CreateBarracksConstructionCompound(barracks);
    }

    private void BuildLandscapeDressing()
    {
        System.Random random = new System.Random(186413);
        for (int i = 0; i < 95; i++)
        {
            double lat = MinLat + random.NextDouble() * (MaxLat - MinLat);
            double lon = MinLon + random.NextDouble() * (MaxLon - MinLon);
            if (!IsDenmarkLand(lat, lon) || IsHydrologyWater(lat, lon))
                continue;
            if (NearStrategicNode(lat, lon, 0.11))
                continue;

            Vector3 p = CampaignGeoProjection.Project3D(lat, lon, WaterY + 0.3f);
            p.y = SampleGroundHeight(p.x, p.z, WaterY + 0.25f) + 0.025f;

            if (i % 3 == 0)
                CreateFieldPatch(p, (float)(0.8 + random.NextDouble() * 1.6), (float)(0.5 + random.NextDouble() * 1.1), (float)(random.NextDouble() * 170.0));
            else
                CreateTreeCluster(p, 2 + random.Next(0, 4), random);
        }
    }

    private void CreateTownDiorama(string name, Vector3 centre, int houseCount, float scale, bool port)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(settlementRoot, false);
        root.transform.position = centre;

        System.Random random = new System.Random(name.GetHashCode());
        for (int i = 0; i < houseCount; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.35f + 0.20f * Mathf.Sqrt(i + 1);
            Vector3 local = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            float sx = scale * (0.75f + (float)random.NextDouble() * 0.42f);
            float sz = scale * (0.72f + (float)random.NextDouble() * 0.45f);
            CreateHouse(root.transform, "House_" + i, local, sx, scale * 0.58f, sz);
        }

        // Church/tower gives the town a readable strategic silhouette.
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = "ChurchTower";
        tower.transform.SetParent(root.transform, false);
        tower.transform.localPosition = new Vector3(0f, scale * 0.75f, 0f);
        tower.transform.localScale = new Vector3(scale * 0.34f, scale * 1.45f, scale * 0.34f);
        tower.GetComponent<Renderer>().sharedMaterial = cityWallMaterial;
        RemoveCollider(tower);

        if (port)
        {
            CreateBox(root.transform, "PortPier", new Vector3(scale * 1.2f, 0.03f, scale * 0.65f), new Vector3(scale * 1.6f, 0.08f, scale * 0.15f), harborMaterial);
        }
    }

    private void CreateHouse(Transform parent, string name, Vector3 localPosition, float sx, float sy, float sz)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = name;
        body.transform.SetParent(parent, false);
        body.transform.localPosition = localPosition + new Vector3(0f, sy * 0.5f, 0f);
        body.transform.localScale = new Vector3(sx, sy, sz);
        body.GetComponent<Renderer>().sharedMaterial = cityWallMaterial;
        RemoveCollider(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = name + "_Roof";
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = localPosition + new Vector3(0f, sy + 0.08f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        roof.transform.localScale = new Vector3(sx * 0.78f, 0.13f, sz * 0.78f);
        roof.GetComponent<Renderer>().sharedMaterial = cityRoofMaterial;
        RemoveCollider(roof);
    }

    private void CreateBarracksConstructionCompound(Vector3 centre)
    {
        GameObject root = new GameObject("GIS_Aalborg_Barracks_Construction");
        root.transform.SetParent(detailRoot, false);
        root.transform.position = centre;

        CreateBox(root.transform, "ParadeGround", new Vector3(0f, 0.02f, 0f), new Vector3(3.6f, 0.04f, 2.2f), fieldMaterial);
        CreateBox(root.transform, "BarracksFoundation_A", new Vector3(-0.95f, 0.10f, -0.45f), new Vector3(1.55f, 0.20f, 0.55f), barracksMaterial);
        CreateBox(root.transform, "BarracksFoundation_B", new Vector3(0.95f, 0.10f, -0.45f), new Vector3(1.55f, 0.20f, 0.55f), barracksMaterial);
        CreateBox(root.transform, "Storehouse", new Vector3(0f, 0.22f, 0.72f), new Vector3(0.80f, 0.44f, 0.55f), barracksMaterial);

        for (int i = 0; i < 6; i++)
        {
            CreateBox(root.transform, "Timber_" + i,
                new Vector3(-1.35f + i * 0.22f, 0.08f, 0.95f),
                new Vector3(0.38f, 0.07f, 0.09f), harborMaterial);
        }

        // Four corner posts make the site unmistakable at close zoom.
        Vector3[] posts =
        {
            new Vector3(-1.85f,0.35f,-1.15f), new Vector3(1.85f,0.35f,-1.15f),
            new Vector3(-1.85f,0.35f,1.15f), new Vector3(1.85f,0.35f,1.15f)
        };
        for (int i = 0; i < posts.Length; i++)
            CreateBox(root.transform, "FencePost_" + i, posts[i], new Vector3(0.08f, 0.70f, 0.08f), harborMaterial);
    }

    private void CreateBoat(string name, Vector3 position, float scale)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(detailRoot, false);
        root.transform.position = position;

        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.name = "Hull";
        hull.transform.SetParent(root.transform, false);
        hull.transform.localScale = new Vector3(scale * 1.5f, scale * 0.18f, scale * 0.48f);
        hull.GetComponent<Renderer>().sharedMaterial = harborMaterial;
        RemoveCollider(hull);

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(root.transform, false);
        cabin.transform.localPosition = new Vector3(0f, scale * 0.24f, 0f);
        cabin.transform.localScale = new Vector3(scale * 0.45f, scale * 0.30f, scale * 0.32f);
        cabin.GetComponent<Renderer>().sharedMaterial = cityWallMaterial;
        RemoveCollider(cabin);
    }

    private void CreateFieldPatch(Vector3 position, float sx, float sz, float rotation)
    {
        GameObject field = GameObject.CreatePrimitive(PrimitiveType.Cube);
        field.name = "GIS_Field";
        field.transform.SetParent(detailRoot, false);
        field.transform.position = position + new Vector3(0f, 0.015f, 0f);
        field.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        field.transform.localScale = new Vector3(sx, 0.025f, sz);
        field.GetComponent<Renderer>().sharedMaterial = fieldMaterial;
        RemoveCollider(field);
    }

    private void CreateTreeCluster(Vector3 centre, int count, System.Random random)
    {
        GameObject root = new GameObject("GIS_TreeCluster");
        root.transform.SetParent(detailRoot, false);
        root.transform.position = centre;

        for (int i = 0; i < count; i++)
        {
            float x = ((float)random.NextDouble() - 0.5f) * 1.5f;
            float z = ((float)random.NextDouble() - 0.5f) * 1.5f;
            float scale = 0.32f + (float)random.NextDouble() * 0.22f;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(x, scale * 0.55f, z);
            trunk.transform.localScale = new Vector3(scale * 0.16f, scale * 0.55f, scale * 0.16f);
            trunk.GetComponent<Renderer>().sharedMaterial = treeTrunkMaterial;
            RemoveCollider(trunk);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(x, scale * 1.35f, z);
            crown.transform.localScale = new Vector3(scale * 0.85f, scale, scale * 0.85f);
            crown.GetComponent<Renderer>().sharedMaterial = treeCrownMaterial;
            RemoveCollider(crown);
        }
    }

    private void TuneLighting()
    {
        RenderSettings.ambientLight = new Color(0.48f, 0.49f, 0.43f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.48f, 0.57f, 0.61f);
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 360f;

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        bool found = false;
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;
            found = true;
            light.intensity = 1.0f;
            light.color = new Color(1.0f, 0.94f, 0.82f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.48f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (!found)
        {
            GameObject go = new GameObject("V013I_Sun");
            Light sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.color = new Color(1.0f, 0.94f, 0.82f);
            sun.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private void TuneCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 centre = CampaignGeoProjection.Project3D(56.20, 10.10, 0f);
        Vector3 home = new Vector3(centre.x, 92f, centre.z - 42f);
        Quaternion rotation = Quaternion.Euler(57f, 0f, 0f);
        cam.transform.position = home;
        cam.transform.rotation = rotation;
        cam.fieldOfView = 43f;
        cam.nearClipPlane = 0.10f;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.TerrainClearance = 3.2f;
            controller.MinHeight = 4.5f;
            controller.MaxHeight = 260f;
            controller.PanSpeed = 42f;
            controller.ZoomSpeed = 110f;
            controller.MinPitch = 28f;
            controller.MaxPitch = 76f;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo homePositionField = typeof(CampaignMapCameraController).GetField("homePosition", flags);
            FieldInfo homeRotationField = typeof(CampaignMapCameraController).GetField("homeRotation", flags);
            if (homePositionField != null) homePositionField.SetValue(controller, home);
            if (homeRotationField != null) homeRotationField.SetValue(controller, rotation);
        }
    }

    private void DisableLegacyPresentationSystems()
    {
        DisableComponent<CampaignDenmarkPremiumVisualV013H>();
        DisableComponent<CampaignDenmarkUiZoomFixV013G>();
        DisableComponent<CampaignDenmarkLandmeshFixV013F>();
        DisableComponent<CampaignDenmarkCleanRenderV013E>();
        DisableComponent<CampaignDenmarkMapRebuildV013D>();
        DisableComponent<CampaignDenmarkCleanupV013C>();
        DisableComponent<CampaignTerrainV013>();
        DisableComponent<CampaignVisualPolishV013A>();
        DisableComponent<CampaignVisualPolishV013B>();
        DisableComponent<CampaignAtmosphereV013>();
        DisableComponent<CampaignConstructionWorkerPolishV013B>();
    }

    private static void DisableComponent<T>() where T : Behaviour
    {
        T component = UnityEngine.Object.FindAnyObjectByType<T>();
        if (component != null)
            component.enabled = false;
    }

    private void HideLegacyPresentation()
    {
        HideObjectTree("V013H_DENMARK_PREMIUM_VISUAL");
        HideObjectTree("V013E_DENMARK_CLEAN_RENDER");
        HideObjectTree("CampaignTerrainSurface_v013");
        HideObjectTree("GEO_Denmark_NaturalEarth50m");
        HideObjectTree("GEO_Denmark_V013D_BROAD_DIRECT");
        HideObjectTree("Campaign Sea Base");

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || IsUnderGisRoot(renderer.transform))
                continue;

            string name = renderer.gameObject.name;
            if (name.StartsWith("Outline_", StringComparison.Ordinal) ||
                name.StartsWith("StrategicLink_", StringComparison.Ordinal) ||
                name.StartsWith("V013E_Link_", StringComparison.Ordinal) ||
                name.StartsWith("CampaignNode_", StringComparison.Ordinal) ||
                name.StartsWith("CampaignControl_", StringComparison.Ordinal) ||
                name.StartsWith("Settlement3D_", StringComparison.Ordinal) ||
                name.StartsWith("V013E_Settlement_", StringComparison.Ordinal) ||
                name.StartsWith("V013H_City_", StringComparison.Ordinal) ||
                name.StartsWith("DNK_LandPart_", StringComparison.Ordinal) ||
                name.StartsWith("DNK_Coast_", StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private bool IsUnderGisRoot(Transform transform)
    {
        if (gisRoot == null || transform == null)
            return false;
        return transform == gisRoot || transform.IsChildOf(gisRoot);
    }

    private static void HideObjectTree(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null)
            return;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = false;
    }

    private Vector2[][] ResolveDenmarkRings()
    {
        try
        {
            FieldInfo field = typeof(CampaignDenmarkGeography).GetField("DenmarkRings", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2[][] rings = field != null ? field.GetValue(null) as Vector2[][] : null;
            if (rings != null && rings.Length > 0)
                return rings;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CAMPAIGN-V013I|DenmarkRingsReflection=False|" + ex.Message);
        }

        return null;
    }

    private bool IsDenmarkLand(double lat, double lon)
    {
        if (denmarkRings == null)
            return lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;

        Vector2 point = new Vector2((float)lon, (float)lat);
        foreach (Vector2[] ring in denmarkRings)
        {
            if (ring != null && ring.Length >= 3 && PointInPolygon(point, ring))
                return true;
        }
        return false;
    }

    private bool IsHydrologyWater(double lat, double lon)
    {
        foreach (WaterFeature feature in hydrology)
        {
            if (DistanceToPolylineKm(lat, lon, feature.Points) <= feature.WidthKm * 0.5f)
                return true;
        }
        return false;
    }

    private static float DistanceToPolylineKm(double lat, double lon, Vector2[] line)
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

            Vector2 p = new Vector2((float)((lon) * 111.32 * cos), (float)(lat * 111.32));
            Vector2 a = new Vector2((float)(lon0 * 111.32 * cos), (float)(lat0 * 111.32));
            Vector2 b = new Vector2((float)(lon1 * 111.32 * cos), (float)(lat1 * 111.32));
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            float d = Vector2.Distance(p, a + ab * t);
            if (d < best) best = d;
        }
        return best;
    }

    private float SampleGroundHeight(float x, float z, float fallback)
    {
        Ray ray = new Ray(new Vector3(x, 100f, z), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 220f);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (!hit.collider.gameObject.name.StartsWith("GIS_Terrain_", StringComparison.Ordinal))
                continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        return float.IsNegativeInfinity(best) ? fallback : best;
    }

    private float WorldUnitsPerKm(double lat, double lon)
    {
        Vector3 a = CampaignGeoProjection.Project3D(lat, lon, 0f);
        Vector3 b = CampaignGeoProjection.Project3D(lat, lon + 0.01, 0f);
        float km = CampaignGeoProjection.ApproximateDistanceKm(lat, lon, lat, lon + 0.01);
        return km > 0.0001f ? Vector3.Distance(a, b) / km : 0.35f;
    }

    private bool NearStrategicNode(double lat, double lon, double thresholdDegrees)
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node)) continue;
            double dLat = node.Latitude - lat;
            double dLon = node.Longitude - lon;
            if (Math.Sqrt(dLat * dLat + dLon * dLon) < thresholdDegrees)
                return true;
        }
        return false;
    }

    private static bool IsDenmarkNode(CampaignNodeState node)
    {
        return node != null && node.Region == CampaignMapRegion.Denmark;
    }

    private static Vector2 NodeLonLat(CampaignNodeState node)
    {
        return new Vector2((float)node.Longitude, (float)node.Latitude);
    }

    private static Vector2 LL(double lon, double lat)
    {
        return new Vector2((float)lon, (float)lat);
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                              (point.x < (pj.x - pi.x) * (point.y - pi.y) / ((pj.y - pi.y) == 0f ? 0.000001f : (pj.y - pi.y)) + pi.x);
            if (intersects) inside = !inside;
        }
        return inside;
    }

    private static float FallbackElevationMeters(double lat, double lon)
    {
        float nx = (float)((lon - MinLon) * 0.72);
        float nz = (float)((lat - MinLat) * 0.72);
        float broad = Mathf.PerlinNoise(nx, nz);
        float fine = Mathf.PerlinNoise(nx * 2.2f + 13.1f, nz * 2.2f + 7.4f);
        return Mathf.Clamp((broad * 0.70f + fine * 0.30f) * 58f - 8f, 0f, 95f);
    }

    private static string GetTerrainCachePath(int zoom, int x, int y)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "PROJECT1864", "TerrainCache", "terrarium",
            zoom.ToString(), x.ToString(), y + ".png");
    }

    private static int LonToTileX(double lon, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((lon + 180.0) / 360.0 * n);
    }

    private static int LatToTileY(double lat, int zoom)
    {
        double latRad = lat * Math.PI / 180.0;
        double n = Math.Pow(2.0, zoom);
        return (int)Math.Floor((1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) * 0.5 * n);
    }

    private static double TileXToLon(int x, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        return x / n * 360.0 - 180.0;
    }

    private static double TileYToLat(int y, int zoom)
    {
        double n = Math.Pow(2.0, zoom);
        double mercator = Math.PI * (1.0 - 2.0 * y / n);
        return Math.Atan(Math.Sinh(mercator)) * 180.0 / Math.PI;
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    private static float AverageNormalY(Vector3[] normals)
    {
        if (normals == null || normals.Length == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < normals.Length; i++) sum += normals[i].y;
        return sum / normals.Length;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(box);
    }

    private static Material CreateLitMaterial(Color color, string name, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static Material CreateUnlitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private void EnsureLabelStyles()
    {
        if (overviewStyle != null)
            return;

        overviewStyle = CreateLabelStyle(12, FontStyle.Bold, new Color(0.95f, 0.93f, 0.84f));
        regionalStyle = CreateLabelStyle(10, FontStyle.Bold, new Color(0.90f, 0.90f, 0.82f));
        localStyle = CreateLabelStyle(9, FontStyle.Normal, new Color(0.80f, 0.82f, 0.75f));
        statusStyle = CreateLabelStyle(9, FontStyle.Normal, new Color(0.78f, 0.83f, 0.82f));
        statusStyle.alignment = TextAnchor.LowerLeft;
    }

    private static GUIStyle CreateLabelStyle(int size, FontStyle fontStyle, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = fontStyle,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Overflow
        };
        style.normal.textColor = color;
        return style;
    }

    private void OnGUI()
    {
        EnsureLabelStyles();
        Camera cam = Camera.main;
        if (cam == null)
            return;

        List<Rect> occupied = new List<Rect>();
        float height = cam.transform.position.y;

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (!IsDenmarkNode(node))
                continue;
            if (!ShouldShowLabel(node.Id, height))
                continue;

            Vector3 world = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, 0f);
            world.y = SampleGroundHeight(world.x, world.z, WaterY + 0.25f) + 1.15f;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 55f || guiY > Screen.height - 10f)
                continue;

            GUIStyle style = overviewLabels.Contains(node.Id) ? overviewStyle : regionalLabels.Contains(node.Id) ? regionalStyle : localStyle;
            float width = overviewLabels.Contains(node.Id) ? 92f : 78f;
            Rect rect = FindLabelRect(new Rect(screen.x - width * 0.5f, guiY - 8f, width, 18f), occupied);
            if (rect.y < 52f || rect.yMax > Screen.height - 5f)
                continue;

            occupied.Add(rect);
            GUIStyle shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.72f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), node.Name, shadow);
            GUI.Label(rect, node.Name, style);
        }

        GUI.Label(
            new Rect(12f, Screen.height - 42f, 680f, 18f),
            terrainStatus,
            statusStyle);
        GUI.Label(
            new Rect(12f, Screen.height - 24f, 900f, 18f),
            "DEM: Mapzen Terrain Tiles / AWS public dataset · Coast: Natural Earth 1:50m · Hydrology: v13i Denmark pilot vectors",
            statusStyle);
    }

    private bool ShouldShowLabel(string id, float cameraHeight)
    {
        if (cameraHeight > 72f)
            return overviewLabels.Contains(id);
        if (cameraHeight > 30f)
            return overviewLabels.Contains(id) || regionalLabels.Contains(id);
        return true;
    }

    private static Rect FindLabelRect(Rect initial, List<Rect> occupied)
    {
        Vector2[] offsets =
        {
            Vector2.zero, new Vector2(0f,-18f), new Vector2(0f,18f),
            new Vector2(-45f,0f), new Vector2(45f,0f),
            new Vector2(-35f,-18f), new Vector2(35f,-18f)
        };

        foreach (Vector2 offset in offsets)
        {
            Rect candidate = new Rect(initial.position + offset, initial.size);
            bool overlap = false;
            foreach (Rect existing in occupied)
            {
                if (candidate.Overlaps(existing)) { overlap = true; break; }
            }
            if (!overlap) return candidate;
        }
        return initial;
    }

    [Serializable]
    private sealed class WaterFeature
    {
        public readonly string Name;
        public readonly float WidthKm;
        public readonly Vector2[] Points;

        public WaterFeature(string name, float widthKm, Vector2[] points)
        {
            Name = name;
            WidthKm = widthKm;
            Points = points;
        }
    }
}
