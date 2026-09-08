using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public sealed class CampaignTerrainV013 : MonoBehaviour
{
    public const string RootTerrain = "L1_Terrain";
    public const string RootHydrology = "L2_Hydrology";
    public const string RootLandCover = "L3_LandCover";
    public const string RootInfrastructure = "L4_Infrastructure";
    public const string RootSettlements = "L5_Settlements";
    public const string RootStrategicEntities = "L6_StrategicEntities";
    public const string RootLivingWorld = "L7_LivingWorld";
    public const string RootOverlays = "L8_Overlays";
    public const string RootAtmosphere = "L9_Atmosphere";
    public const string RootUiDebug = "L10_UI_Debug";

    private static readonly Dictionary<string, GameObject> roots = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private static readonly List<Vector2[]> landPolygons = new List<Vector2[]>();
    private static bool polygonsReady;

    private Material terrainMaterial;
    private Material buildingMaterial;
    private Material roofMaterial;
    private Material railMaterial;
    private Material roadMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignTerrainV013>() != null)
            return;

        GameObject root = new GameObject("CampaignTerrainV013");
        root.AddComponent<CampaignTerrainV013>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        EnsureLandPolygons();
        EnsureRoots();
        BuildMaterials();
        BuildTerrainMesh();
        UpgradeSea();
        BuildSettlementMiniatures();
        UpgradeExistingMapLines();

        Debug.Log(string.Format(
            "CAMPAIGN-V013|3DWorldBuilt=True|TerrainGrid={0}x{1}|Settlements={2}|Map={3:0}x{4:0}|UnityDistanceAuthoritative=False",
            112,
            136,
            CampaignSession.Nodes.Count,
            CampaignGeoProjection.MapWidth,
            CampaignGeoProjection.MapDepth));
    }

    private void LateUpdate()
    {
        SnapExistingNodesAndFormations();
    }

    public static GameObject GetOrCreateLayerRoot(string name)
    {
        if (roots.TryGetValue(name, out GameObject existing) && existing != null)
            return existing;

        GameObject found = GameObject.Find(name);
        if (found == null)
            found = new GameObject(name);
        roots[name] = found;
        return found;
    }

    public static float SampleRenderHeight(float x, float z)
    {
        EnsureLandPolygons();
        Vector2 p = new Vector2(x, z);
        int region = FindLandPolygonIndex(p);
        if (region < 0)
            return -3.2f;

        float nx = Mathf.InverseLerp(-CampaignGeoProjection.MapWidth * 0.5f, CampaignGeoProjection.MapWidth * 0.5f, x);
        float nz = Mathf.InverseLerp(-CampaignGeoProjection.MapDepth * 0.5f, CampaignGeoProjection.MapDepth * 0.5f, z);
        float broad = Mathf.PerlinNoise(nx * 4.1f + 7.2f, nz * 4.6f + 3.7f);
        float fine = Mathf.PerlinNoise(nx * 12.0f + 19.0f, nz * 11.0f + 5.0f);

        float height;
        switch (region)
        {
            case 4: // Norway
                height = 3.0f + broad * 18.0f + fine * 7.0f + Mathf.Clamp01(nz - 0.42f) * 9.0f;
                break;
            case 3: // Sweden
                height = 2.0f + broad * 8.0f + fine * 3.0f + Mathf.Clamp01(nz - 0.55f) * 4.0f;
                break;
            case 5: // Finland
                height = 1.8f + broad * 5.5f + fine * 2.2f;
                break;
            case 0: // Germany
                height = 1.4f + broad * 5.0f + fine * 1.8f + Mathf.Clamp01(0.28f - nz) * 4.0f;
                break;
            default: // Denmark islands/Jutland
                height = 0.9f + broad * 1.8f + fine * 0.8f;
                break;
        }

        return height;
    }

    public static float SampleSurfaceY(float x, float z)
    {
        float terrain = SampleRenderHeight(x, z);
        return terrain < -0.5f ? -0.45f : terrain;
    }

    private static void EnsureLandPolygons()
    {
        if (polygonsReady)
            return;

        landPolygons.Clear();

        // Germany
        landPolygons.Add(P(new double[,] {
            {47.3,7.5},{47.5,10.0},{47.3,12.3},{48.0,13.8},{49.5,13.5},{50.3,12.5},{51.0,14.8},{52.5,14.7},{53.6,14.3},{54.4,13.0},{54.8,11.0},{54.8,9.4},{54.4,8.6},{53.6,8.0},{53.3,7.0},{52.2,6.8},{51.0,6.0},{50.0,6.0},{49.0,7.2}
        }));
        // Denmark Jutland
        landPolygons.Add(P(new double[,] {
            {54.6,8.0},{54.7,9.7},{55.4,10.1},{56.4,10.6},{57.6,10.6},{57.8,9.5},{57.2,8.2},{55.5,8.0}
        }));
        // Denmark Funen/Zealand combined coarse island set represented as two polygons.
        landPolygons.Add(P(new double[,] {
            {55.05,9.75},{55.12,10.65},{55.65,10.75},{55.72,9.85}
        }));
        landPolygons.Add(P(new double[,] {
            {55.20,10.95},{55.15,12.20},{55.55,12.75},{56.10,12.25},{56.05,11.20}
        }));
        // Sweden
        landPolygons.Add(P(new double[,] {
            {55.3,12.5},{56.0,14.0},{56.0,16.0},{58.0,18.0},{61.0,18.0},{64.0,20.0},{67.0,24.0},{69.0,21.0},{68.0,18.0},{66.0,15.0},{63.0,12.0},{60.0,12.0},{58.0,11.0}
        }));
        // Norway
        landPolygons.Add(P(new double[,] {
            {58.0,5.0},{59.0,5.0},{60.0,5.0},{62.0,5.0},{64.0,6.0},{66.0,12.0},{68.0,14.0},{70.0,20.0},{71.0,25.0},{69.0,29.0},{66.0,24.0},{64.0,14.0},{61.0,12.0},{59.0,12.0},{58.0,8.0}
        }));
        // Finland
        landPolygons.Add(P(new double[,] {
            {59.7,20.5},{60.0,23.0},{60.0,27.0},{62.0,30.0},{65.0,29.0},{68.0,27.0},{69.5,28.0},{69.8,23.0},{67.0,21.0},{64.0,21.0},{62.0,20.0}
        }));

        polygonsReady = true;
    }

    private static Vector2[] P(double[,] latLon)
    {
        int count = latLon.GetLength(0);
        Vector2[] points = new Vector2[count];
        for (int i = 0; i < count; i++)
            points[i] = CampaignGeoProjection.Project(latLon[i, 0], latLon[i, 1]);
        return points;
    }

    private static int FindLandPolygonIndex(Vector2 point)
    {
        for (int i = 0; i < landPolygons.Count; i++)
        {
            if (PointInPolygon(point, landPolygons[i]))
                return NormalizeRegionIndex(i);
        }
        return -1;
    }

    private static int NormalizeRegionIndex(int polygonIndex)
    {
        // 0 Germany, 1-3 Denmark, 4 Sweden, 5 Norway, 6 Finland.
        if (polygonIndex >= 1 && polygonIndex <= 3)
            return 1;
        if (polygonIndex == 4)
            return 3;
        if (polygonIndex == 5)
            return 4;
        if (polygonIndex == 6)
            return 5;
        return polygonIndex;
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersects = ((pi.y > p.y) != (pj.y > p.y)) &&
                              (p.x < (pj.x - pi.x) * (p.y - pi.y) / Mathf.Max(0.00001f, pj.y - pi.y) + pi.x);
            if (intersects)
                inside = !inside;
            j = i;
        }
        return inside;
    }

    private void EnsureRoots()
    {
        GetOrCreateLayerRoot(RootTerrain);
        GetOrCreateLayerRoot(RootHydrology);
        GetOrCreateLayerRoot(RootLandCover);
        GetOrCreateLayerRoot(RootInfrastructure);
        GetOrCreateLayerRoot(RootSettlements);
        GetOrCreateLayerRoot(RootStrategicEntities);
        GetOrCreateLayerRoot(RootLivingWorld);
        GetOrCreateLayerRoot(RootOverlays);
        GetOrCreateLayerRoot(RootAtmosphere);
        GetOrCreateLayerRoot(RootUiDebug);
    }

    private void BuildMaterials()
    {
        terrainMaterial = CreateMaterial(new Color(0.30f, 0.39f, 0.24f), "V013_Terrain");
        buildingMaterial = CreateMaterial(new Color(0.56f, 0.50f, 0.39f), "V013_Building");
        roofMaterial = CreateMaterial(new Color(0.28f, 0.20f, 0.16f), "V013_Roof");
        roadMaterial = CreateMaterial(new Color(0.46f, 0.38f, 0.27f), "V013_Road");
        railMaterial = CreateMaterial(new Color(0.20f, 0.20f, 0.19f), "V013_Rail");
    }

    private void BuildTerrainMesh()
    {
        const int xSegments = 112;
        const int zSegments = 136;
        int xCount = xSegments + 1;
        int zCount = zSegments + 1;

        Vector3[] vertices = new Vector3[xCount * zCount];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        int v = 0;
        for (int z = 0; z < zCount; z++)
        {
            float tz = z / (float)zSegments;
            float wz = Mathf.Lerp(-CampaignGeoProjection.MapDepth * 0.5f, CampaignGeoProjection.MapDepth * 0.5f, tz);
            for (int x = 0; x < xCount; x++)
            {
                float tx = x / (float)xSegments;
                float wx = Mathf.Lerp(-CampaignGeoProjection.MapWidth * 0.5f, CampaignGeoProjection.MapWidth * 0.5f, tx);
                vertices[v] = new Vector3(wx, SampleRenderHeight(wx, wz), wz);
                uv[v] = new Vector2(tx, tz);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * xCount + x;
                int b = a + 1;
                int c = a + xCount;
                int d = c + 1;
                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = b;
                triangles[t++] = b;
                triangles[t++] = c;
                triangles[t++] = d;
            }
        }

        Mesh mesh = new Mesh { name = "CampaignTerrainMesh_v013" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject terrain = new GameObject("CampaignTerrainSurface_v013");
        terrain.transform.SetParent(GetOrCreateLayerRoot(RootTerrain).transform, false);
        MeshFilter filter = terrain.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = terrain.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = terrainMaterial;
    }

    private void UpgradeSea()
    {
        GameObject sea = GameObject.Find("Campaign Sea Base");
        if (sea == null)
            return;

        sea.transform.SetParent(GetOrCreateLayerRoot(RootHydrology).transform, true);
        sea.transform.position = new Vector3(0f, -2.0f, 0f);
        sea.transform.localScale = new Vector3(
            CampaignGeoProjection.MapWidth + 100f,
            3.0f,
            CampaignGeoProjection.MapDepth + 100f);
    }

    private void BuildSettlementMiniatures()
    {
        Transform parent = GetOrCreateLayerRoot(RootSettlements).transform;
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            float baseY = SampleSurfaceY(node.MapPosition.x, node.MapPosition.y);
            GameObject root = new GameObject("Settlement3D_" + node.Id);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(node.MapPosition.x, baseY, node.MapPosition.y);

            int count = node.Terrain == CampaignTerrainType.Urban ? 5 : 3;
            if (node.Terrain == CampaignTerrainType.Fortified)
                count = 4;

            int hash = StableHash(node.Id);
            for (int i = 0; i < count; i++)
            {
                float angle = ((hash + i * 71) % 360) * Mathf.Deg2Rad;
                float radius = 4.8f + (i % 2) * 2.4f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                CreateHouse(root.transform, offset, 1.6f + (i % 3) * 0.45f);
            }

            if (node.HasDepot)
                CreateBlock(root.transform, "Depot", new Vector3(-5.5f, 1.0f, 5.2f), new Vector3(4.6f, 2.0f, 2.7f), buildingMaterial);
            if (node.HasRail)
                CreateBlock(root.transform, "Station", new Vector3(5.6f, 0.7f, -4.8f), new Vector3(5.3f, 1.4f, 1.8f), buildingMaterial);
            if (node.HasPort)
                CreateBlock(root.transform, "PortWarehouse", new Vector3(-6.0f, 0.8f, -5.2f), new Vector3(4.0f, 1.6f, 2.2f), buildingMaterial);
            if (node.Terrain == CampaignTerrainType.Fortified)
            {
                GameObject fort = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fort.name = "Fortification";
                fort.transform.SetParent(root.transform, false);
                fort.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                fort.transform.localScale = new Vector3(5.5f, 0.45f, 5.5f);
                fort.GetComponent<Renderer>().sharedMaterial = roadMaterial;
            }
        }
    }

    private void CreateHouse(Transform parent, Vector3 localPosition, float scale)
    {
        GameObject body = CreateBlock(parent, "House", localPosition + Vector3.up * (0.8f * scale), new Vector3(1.7f, 1.6f, 1.4f) * scale, buildingMaterial);
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(body.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(0.9f, 0.9f, 1.15f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
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
        return block;
    }

    private void UpgradeExistingMapLines()
    {
        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>();
        Transform infrastructure = GetOrCreateLayerRoot(RootInfrastructure).transform;
        Transform overlays = GetOrCreateLayerRoot(RootOverlays).transform;

        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            if (line.name.StartsWith("StrategicLink_", StringComparison.Ordinal) && line.positionCount >= 2)
            {
                Vector3 start = line.GetPosition(0);
                Vector3 end = line.GetPosition(line.positionCount - 1);
                const int samples = 20;
                line.positionCount = samples;
                for (int i = 0; i < samples; i++)
                {
                    float u = i / (float)(samples - 1);
                    Vector3 p = Vector3.Lerp(start, end, u);
                    p.y = SampleSurfaceY(p.x, p.z) + 0.72f;
                    line.SetPosition(i, p);
                }

                line.widthMultiplier = 0.75f;
                line.transform.SetParent(infrastructure, true);
                bool likelyRail = IsLikelyRailLinkFromName(line.name);
                line.sharedMaterial = likelyRail ? railMaterial : roadMaterial;
            }
            else if (line.name.StartsWith("Outline_", StringComparison.Ordinal))
            {
                for (int i = 0; i < line.positionCount; i++)
                {
                    Vector3 p = line.GetPosition(i);
                    p.y = SampleSurfaceY(p.x, p.z) + 1.1f;
                    line.SetPosition(i, p);
                }
                line.transform.SetParent(overlays, true);
            }
        }
    }

    private static bool IsLikelyRailLinkFromName(string lineName)
    {
        int separator = lineName.IndexOf('_');
        if (separator < 0)
            return false;
        string key = lineName.Substring(separator + 1);
        string[] parts = key.Split('|');
        if (parts.Length != 2)
            return false;
        CampaignNodeState a = CampaignSession.GetNode(parts[0]);
        CampaignNodeState b = CampaignSession.GetNode(parts[1]);
        return a != null && b != null && a.HasRail && b.HasRail;
    }

    private void SnapExistingNodesAndFormations()
    {
        CampaignNodeView[] nodes = UnityEngine.Object.FindObjectsByType<CampaignNodeView>();
        foreach (CampaignNodeView view in nodes)
        {
            if (view == null)
                continue;
            Vector3 p = view.transform.position;
            p.y = SampleSurfaceY(p.x, p.z) + 1.25f;
            view.transform.position = p;
        }

        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>();
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;
            Vector3 p = view.transform.position;
            p.y = SampleSurfaceY(p.x, p.z) + 4.0f;
            view.transform.position = p;
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

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}
