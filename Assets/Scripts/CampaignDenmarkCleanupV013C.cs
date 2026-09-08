using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13c — Denmark terrain cleanup and grounding pass.
// Presentation-only: strategic state, route distance, ETA and tactical systems are untouched.
[DefaultExecutionOrder(2600)]
public sealed class CampaignDenmarkCleanupV013C : MonoBehaviour
{
    private const string DetailedDenmarkRootA = "GEO_Denmark_NaturalEarth50m_V013A";
    private const string DetailedDenmarkRootLegacy = "GEO_Denmark_NaturalEarth50m";
    private const float CleanSeaY = -0.72f;
    private const int TerrainXCount = 113; // v13 terrain: 112 segments + 1
    private const int TerrainZCount = 137; // v13 terrain: 136 segments + 1

    private readonly List<Vector2[]> denmarkRings = new List<Vector2[]>();
    private Mesh terrainMesh;
    private Transform terrainTransform;
    private Vector3[] cleanVertices;
    private bool[] denmarkMask;
    private Material levelGroundMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkCleanupV013C>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkCleanupV013C");
        root.AddComponent<CampaignDenmarkCleanupV013C>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        levelGroundMaterial = CreateMaterial(new Color(0.30f, 0.35f, 0.20f), "V013C_LevelGround");

        CaptureDetailedDenmarkRings();
        RebuildDenmarkTerrainSurface();
        RedrapeDetailedDenmark();
        RedrapeDenmarkInfrastructure();
        GroundDenmarkStaticObjects();
        GroundDenmarkLandCover();
        EnsureDenmarkSettlementPads();
        EnsureConstructionPads();

        Debug.Log(string.Format(
            "CAMPAIGN-V013C|DenmarkCleanup=True|Rings={0}|TerrainMesh={1}|Grounding=True|CoarseDenmarkCarved=True|GentleRelief=True|SimulationChanged=False",
            denmarkRings.Count,
            terrainMesh != null));
    }

    private void LateUpdate()
    {
        // Older v13 helpers still use the original procedural height function.
        // Re-ground Denmark objects after those helpers have run, without changing state.
        GroundDenmarkDynamicObjects();
        GroundConstructionRoots();
    }

    private void CaptureDetailedDenmarkRings()
    {
        denmarkRings.Clear();

        GameObject root = GameObject.Find(DetailedDenmarkRootA);
        if (root == null)
            root = GameObject.Find(DetailedDenmarkRootLegacy);

        if (root == null)
        {
            Debug.LogWarning("CAMPAIGN-V013C|DetailedDenmarkRoot=False|Fallback=LegacyEnvelopeOnly");
            return;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Vector3[] vertices = filter.sharedMesh.vertices;
            if (vertices == null || vertices.Length < 3)
                continue;

            Vector2[] ring = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[i]);
                ring[i] = new Vector2(world.x, world.z);
            }
            denmarkRings.Add(ring);
        }
    }

    private void RebuildDenmarkTerrainSurface()
    {
        GameObject terrainObject = GameObject.Find("CampaignTerrainSurface_v013");
        if (terrainObject == null)
        {
            Debug.LogWarning("CAMPAIGN-V013C|TerrainSurface=False");
            return;
        }

        MeshFilter filter = terrainObject.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            Debug.LogWarning("CAMPAIGN-V013C|TerrainMesh=False");
            return;
        }

        terrainTransform = terrainObject.transform;
        terrainMesh = filter.mesh;
        Vector3[] source = terrainMesh.vertices;
        cleanVertices = (Vector3[])source.Clone();
        denmarkMask = new bool[cleanVertices.Length];

        if (cleanVertices.Length != TerrainXCount * TerrainZCount)
        {
            Debug.LogWarning(string.Format(
                "CAMPAIGN-V013C|UnexpectedTerrainGrid=True|Vertices={0}|Expected={1}",
                cleanVertices.Length,
                TerrainXCount * TerrainZCount));
        }

        for (int i = 0; i < cleanVertices.Length; i++)
        {
            Vector3 local = cleanVertices[i];
            Vector3 world = terrainTransform.TransformPoint(local);
            double latitude;
            double longitude;
            BroadUnproject(world.x, world.z, out latitude, out longitude);

            bool inDenmark = IsInsideDetailedDenmark(new Vector2(world.x, world.z));
            denmarkMask[i] = inDenmark;

            float desiredWorldY = world.y;

            if (inDenmark)
            {
                desiredWorldY = GentleDenmarkHeight(world.x, world.z, latitude, longitude);
            }
            else if (IsLegacyDenmarkEnvelope(latitude, longitude))
            {
                // The old v13 terrain used coarse Jutland/Funen/Zealand polygons.
                // Carve their overshoot back to water using the detailed Denmark surface.
                desiredWorldY = CleanSeaY;
            }
            else if (world.y < -1.0f)
            {
                // Keep the hidden seabed just below the visible water plane instead of
                // deep rectangular trenches that read as vertical map walls.
                desiredWorldY = CleanSeaY;
            }

            Vector3 desiredWorld = new Vector3(world.x, desiredWorldY, world.z);
            cleanVertices[i] = terrainTransform.InverseTransformPoint(desiredWorld);
        }

        SmoothDenmarkVertices(cleanVertices, denmarkMask, 3, 0.46f);

        terrainMesh.vertices = cleanVertices;
        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateBounds();
    }

    private void SmoothDenmarkVertices(Vector3[] vertices, bool[] mask, int passes, float strength)
    {
        if (vertices == null || mask == null || vertices.Length != mask.Length)
            return;
        if (vertices.Length != TerrainXCount * TerrainZCount)
            return;

        Vector3[] buffer = new Vector3[vertices.Length];

        for (int pass = 0; pass < passes; pass++)
        {
            Array.Copy(vertices, buffer, vertices.Length);

            for (int z = 1; z < TerrainZCount - 1; z++)
            {
                for (int x = 1; x < TerrainXCount - 1; x++)
                {
                    int index = z * TerrainXCount + x;
                    if (!mask[index])
                        continue;

                    float sum = vertices[index].y;
                    int count = 1;
                    int left = index - 1;
                    int right = index + 1;
                    int down = index - TerrainXCount;
                    int up = index + TerrainXCount;

                    if (mask[left]) { sum += vertices[left].y; count++; }
                    if (mask[right]) { sum += vertices[right].y; count++; }
                    if (mask[down]) { sum += vertices[down].y; count++; }
                    if (mask[up]) { sum += vertices[up].y; count++; }

                    float average = sum / count;
                    buffer[index].y = Mathf.Lerp(vertices[index].y, average, strength);
                }
            }

            Array.Copy(buffer, vertices, vertices.Length);
        }
    }

    private float GentleDenmarkHeight(float x, float z, double latitude, double longitude)
    {
        float nx = (float)((longitude - 7.5) / 8.0);
        float nz = (float)((latitude - 54.5) / 3.5);
        float broad = Mathf.PerlinNoise(nx * 2.1f + 3.7f, nz * 2.4f + 8.3f);
        float fine = Mathf.PerlinNoise(nx * 5.8f + 11.1f, nz * 6.3f + 2.4f);

        // Gentle central-Jutland rise. This is visual proxy relief, not DEM elevation.
        float dx = (float)(longitude - 9.35);
        float dz = (float)(latitude - 56.15);
        float centralJutland = Mathf.Exp(-(dx * dx / 0.95f + dz * dz / 0.75f)) * 0.20f;

        return 0.22f + broad * 0.34f + fine * 0.10f + centralJutland;
    }

    private static bool IsLegacyDenmarkEnvelope(double latitude, double longitude)
    {
        bool jutland = latitude >= 54.90 && latitude <= 57.85 && longitude >= 7.80 && longitude <= 10.95;
        bool funen = latitude >= 54.92 && latitude <= 55.85 && longitude >= 9.70 && longitude <= 10.98;
        bool zealandAndSouthIslands = latitude >= 54.55 && latitude <= 56.25 && longitude >= 10.90 && longitude <= 12.48;
        return jutland || funen || zealandAndSouthIslands;
    }

    private bool IsInsideDetailedDenmark(Vector2 point)
    {
        if (denmarkRings.Count == 0)
            return false;

        for (int i = 0; i < denmarkRings.Count; i++)
        {
            if (PointInPolygon(point, denmarkRings[i]))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        if (polygon == null || polygon.Length < 3)
            return false;

        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            float denominator = b.y - a.y;

            if (Mathf.Abs(denominator) < 0.00001f)
                denominator = denominator >= 0f ? 0.00001f : -0.00001f;

            bool intersects = ((a.y > point.y) != (b.y > point.y)) &&
                              (point.x < (b.x - a.x) * (point.y - a.y) / denominator + a.x);
            if (intersects)
                inside = !inside;

            j = i;
        }

        return inside;
    }

    private void RedrapeDetailedDenmark()
    {
        GameObject root = GameObject.Find(DetailedDenmarkRootA);
        if (root == null)
            root = GameObject.Find(DetailedDenmarkRootLegacy);
        if (root == null || terrainMesh == null)
            return;

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.mesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[i]);
                world.y = SampleCleanHeight(world.x, world.z) + 0.06f;
                vertices[i] = filter.transform.InverseTransformPoint(world);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        LineRenderer[] coastlines = root.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer line in coastlines)
        {
            if (line == null)
                continue;

            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                p.y = SampleCleanHeight(p.x, p.z) + 0.12f;
                line.SetPosition(i, p);
            }
            line.widthMultiplier = 0.16f;
        }
    }

    private void RedrapeDenmarkInfrastructure()
    {
        if (terrainMesh == null)
            return;

        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.name.StartsWith("StrategicLink_", StringComparison.Ordinal))
                continue;

            bool touchesDenmark = LinkTouchesDenmark(line.name);
            if (!touchesDenmark)
                continue;

            CampaignStrategicLinkType type = ResolveLinkType(line.name);
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                if (type == CampaignStrategicLinkType.SeaFerry && !IsInsideDetailedDenmark(new Vector2(p.x, p.z)))
                    p.y = -0.24f;
                else
                    p.y = SampleCleanHeight(p.x, p.z) + 0.18f;
                line.SetPosition(i, p);
            }
        }
    }

    private bool LinkTouchesDenmark(string lineName)
    {
        const string prefix = "StrategicLink_";
        if (lineName.Length <= prefix.Length)
            return false;

        string[] ids = lineName.Substring(prefix.Length).Split('|');
        if (ids.Length != 2)
            return false;

        CampaignNodeState a = CampaignSession.GetNode(ids[0]);
        CampaignNodeState b = CampaignSession.GetNode(ids[1]);
        return (a != null && a.Region == CampaignMapRegion.Denmark) ||
               (b != null && b.Region == CampaignMapRegion.Denmark);
    }

    private static CampaignStrategicLinkType ResolveLinkType(string lineName)
    {
        const string prefix = "StrategicLink_";
        if (lineName.Length <= prefix.Length)
            return CampaignStrategicLinkType.Road;

        string[] ids = lineName.Substring(prefix.Length).Split('|');
        if (ids.Length != 2)
            return CampaignStrategicLinkType.Road;

        return CampaignMapUsabilityV011.GetLinkType(
            CampaignSession.GetNode(ids[0]),
            CampaignSession.GetNode(ids[1]));
    }

    private void GroundDenmarkStaticObjects()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            GameObject settlement = GameObject.Find("Settlement3D_" + node.Id);
            if (settlement != null)
            {
                Vector3 p = settlement.transform.position;
                p.y = SampleCleanHeight(p.x, p.z);
                settlement.transform.position = p;
            }
        }

        GroundDenmarkDynamicObjects();
        GroundConstructionRoots();
    }

    private void GroundDenmarkDynamicObjects()
    {
        if (terrainMesh == null)
            return;

        CampaignNodeView[] nodes = UnityEngine.Object.FindObjectsByType<CampaignNodeView>(FindObjectsSortMode.None);
        foreach (CampaignNodeView view in nodes)
        {
            if (view == null)
                continue;

            CampaignNodeState node = CampaignSession.GetNode(view.NodeId);
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            Vector3 p = view.transform.position;
            p.y = SampleCleanHeight(p.x, p.z) + 1.25f;
            view.transform.position = p;
        }

        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            Vector3 p = view.transform.position;
            if (!IsNearDenmarkWorld(p.x, p.z))
                continue;

            p.y = SampleCleanHeight(p.x, p.z) + 4.0f;
            view.transform.position = p;
        }
    }

    private void GroundConstructionRoots()
    {
        GroundNamedObject("ConstructionProject_QA-BARRACKS-AALBORG");
        GroundNamedObject("ConstructionProject_QA-FARM-AARHUS");
    }

    private void GroundNamedObject(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null || terrainMesh == null)
            return;

        Vector3 p = root.transform.position;
        p.y = SampleCleanHeight(p.x, p.z);
        root.transform.position = p;
    }

    private void GroundDenmarkLandCover()
    {
        if (terrainMesh == null)
            return;

        GameObject patches = GameObject.Find("V013B_DenmarkLandCover");
        if (patches != null)
        {
            foreach (Transform child in patches.transform)
            {
                Vector3 p = child.position;
                p.y = SampleCleanHeight(p.x, p.z) + 0.06f;
                child.position = p;
            }
        }

        GameObject vegetation = GameObject.Find("V013B_Vegetation");
        if (vegetation != null)
        {
            Transform[] transforms = vegetation.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t == null || !string.Equals(t.name, "Tree", StringComparison.Ordinal))
                    continue;

                Vector3 p = t.position;
                p.y = SampleCleanHeight(p.x, p.z);
                t.position = p;
            }
        }
    }

    private void EnsureDenmarkSettlementPads()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            GameObject settlement = GameObject.Find("Settlement3D_" + node.Id);
            if (settlement == null || settlement.transform.Find("V013C_LevelGround") != null)
                continue;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "V013C_LevelGround";
            pad.transform.SetParent(settlement.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            float radius = node.Terrain == CampaignTerrainType.Urban ? 8.2f : 6.2f;
            if (node.Terrain == CampaignTerrainType.Fortified)
                radius = 7.0f;
            pad.transform.localScale = new Vector3(radius, 0.05f, radius);
            Renderer renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = levelGroundMaterial;
            RemoveCollider(pad);
        }
    }

    private void EnsureConstructionPads()
    {
        EnsureConstructionPad("ConstructionProject_QA-BARRACKS-AALBORG", new Vector3(10.5f, 0.06f, 8.0f));
        EnsureConstructionPad("ConstructionProject_QA-FARM-AARHUS", new Vector3(9.0f, 0.06f, 8.0f));
    }

    private void EnsureConstructionPad(string objectName, Vector3 scale)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null || root.transform.Find("V013C_LevelGround") != null)
            return;

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "V013C_LevelGround";
        pad.transform.SetParent(root.transform, false);
        pad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        pad.transform.localScale = scale;
        Renderer renderer = pad.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = levelGroundMaterial;
        RemoveCollider(pad);
    }

    private float SampleCleanHeight(float worldX, float worldZ)
    {
        if (cleanVertices == null || cleanVertices.Length != TerrainXCount * TerrainZCount || terrainTransform == null)
            return CampaignTerrainV013.SampleSurfaceY(worldX, worldZ);

        Vector3 local = terrainTransform.InverseTransformPoint(new Vector3(worldX, 0f, worldZ));
        float tx = Mathf.InverseLerp(-CampaignGeoProjection.MapWidth * 0.5f, CampaignGeoProjection.MapWidth * 0.5f, local.x);
        float tz = Mathf.InverseLerp(-CampaignGeoProjection.MapDepth * 0.5f, CampaignGeoProjection.MapDepth * 0.5f, local.z);

        float fx = Mathf.Clamp(tx, 0f, 1f) * (TerrainXCount - 1);
        float fz = Mathf.Clamp(tz, 0f, 1f) * (TerrainZCount - 1);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, TerrainXCount - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, TerrainZCount - 1);
        int x1 = Mathf.Min(x0 + 1, TerrainXCount - 1);
        int z1 = Mathf.Min(z0 + 1, TerrainZCount - 1);

        float ux = fx - x0;
        float uz = fz - z0;
        float y00 = cleanVertices[z0 * TerrainXCount + x0].y;
        float y10 = cleanVertices[z0 * TerrainXCount + x1].y;
        float y01 = cleanVertices[z1 * TerrainXCount + x0].y;
        float y11 = cleanVertices[z1 * TerrainXCount + x1].y;
        float y0 = Mathf.Lerp(y00, y10, ux);
        float y1 = Mathf.Lerp(y01, y11, ux);
        float localY = Mathf.Lerp(y0, y1, uz);

        return terrainTransform.TransformPoint(new Vector3(local.x, localY, local.z)).y;
    }

    private static void BroadUnproject(float x, float z, out double latitude, out double longitude)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
    }

    private static bool IsNearDenmarkWorld(float x, float z)
    {
        double latitude;
        double longitude;
        BroadUnproject(x, z, out latitude, out longitude);
        return latitude >= 54.2 && latitude <= 58.2 && longitude >= 7.2 && longitude <= 15.8;
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

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.08f);
        return material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        if (obj == null)
            return;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }
}
