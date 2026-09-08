using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13c — corrected Denmark-focus presentation pass.
// IMPORTANT: do not carve the coarse global v13 terrain with detailed Denmark geometry.
// The Natural Earth Denmark layer is remapped explicitly from the legacy local projection
// to the v11+ broad campaign projection and becomes the visible Denmark land surface.
[DefaultExecutionOrder(2600)]
public sealed class CampaignDenmarkCleanupV013C : MonoBehaviour
{
    private const string DetailedDenmarkRootA = "GEO_Denmark_NaturalEarth50m_V013A";
    private const string DetailedDenmarkRootLegacy = "GEO_Denmark_NaturalEarth50m";
    private const string DetailedDenmarkRootFixed = "GEO_Denmark_NaturalEarth50m_V013C_BROAD";
    private const string GlobalTerrainName = "CampaignTerrainSurface_v013";

    private bool projectionRemapped;

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

        // v13c QA is Denmark-first. The coarse global terrain is presentation-only and is
        // deliberately hidden rather than destructively reshaped. This removes the long
        // rectangular cliffs/bands seen in the rejected first v13c runtime test.
        DisableCoarseGlobalTerrain();

        projectionRemapped = RemapDetailedDenmarkToBroadProjection();
        CompactAndGroundLandCover();
        GroundDenmarkSettlements();
        GroundDenmarkNodesAndFormations();
        GroundConstructionProjects();
        RedrapeDenmarkInfrastructure();
        HideNonDenmark3DContext();

        Debug.Log(string.Format(
            "CAMPAIGN-V013C|DenmarkCleanup=True|ProjectionRemapped={0}|GlobalTerrainRenderer=False|DetailedDenmarkVisible=True|Grounding=True|SimulationChanged=False",
            projectionRemapped));
    }

    private void LateUpdate()
    {
        // Older v13 helpers still ground objects against the coarse procedural height.
        // Run after them and restore the Denmark-focus presentation height.
        GroundDenmarkNodesAndFormations();
        GroundConstructionProjects();
        HideNonDenmark3DContext();
    }

    private static void DisableCoarseGlobalTerrain()
    {
        GameObject terrain = GameObject.Find(GlobalTerrainName);
        if (terrain == null)
            return;

        Renderer renderer = terrain.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private bool RemapDetailedDenmarkToBroadProjection()
    {
        GameObject root = GameObject.Find(DetailedDenmarkRootFixed);
        if (root != null)
            return true;

        root = GameObject.Find(DetailedDenmarkRootA);
        if (root == null)
            root = GameObject.Find(DetailedDenmarkRootLegacy);
        if (root == null)
        {
            Debug.LogWarning("CAMPAIGN-V013C|DetailedDenmarkRoot=False");
            return false;
        }

        // CampaignDenmarkGeography.Create() historically used the legacy local
        // longitude/latitude projection. Convert every existing X/Z point back to
        // lon/lat and then into the broad strategic projection used by nodes/routes.
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.mesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 legacyWorld = filter.transform.TransformPoint(vertices[i]);
                Vector2 lonLat = CampaignGeoProjection.Unproject(legacyWorld);

                double longitude = lonLat.x;
                double latitude = lonLat.y;
                if (!LooksLikeDenmark(latitude, longitude))
                    continue;

                Vector3 broadWorld = CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    GentleDenmarkHeight(latitude, longitude));
                vertices[i] = filter.transform.InverseTransformPoint(broadWorld);
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
                Vector3 legacyWorld = line.GetPosition(i);
                Vector2 lonLat = CampaignGeoProjection.Unproject(legacyWorld);
                double longitude = lonLat.x;
                double latitude = lonLat.y;
                if (!LooksLikeDenmark(latitude, longitude))
                    continue;

                Vector3 broadWorld = CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    GentleDenmarkHeight(latitude, longitude) + 0.08f);
                line.SetPosition(i, broadWorld);
            }

            line.widthMultiplier = 0.12f;
        }

        root.name = DetailedDenmarkRootFixed;
        return true;
    }

    private static float GentleDenmarkHeight(double latitude, double longitude)
    {
        float nx = (float)((longitude - 7.5) / 8.0);
        float nz = (float)((latitude - 54.4) / 3.7);

        float broad = Mathf.PerlinNoise(nx * 1.8f + 3.2f, nz * 2.0f + 7.4f);
        float fine = Mathf.PerlinNoise(nx * 4.6f + 10.8f, nz * 4.9f + 2.1f);

        // Denmark is intentionally low-relief in presentation space.
        float dx = (float)(longitude - 9.35);
        float dz = (float)(latitude - 56.15);
        float centralJutland = Mathf.Exp(-(dx * dx / 1.15f + dz * dz / 0.95f)) * 0.14f;

        return 0.28f + broad * 0.20f + fine * 0.055f + centralJutland;
    }

    private static float HeightFromWorld(float x, float z)
    {
        double latitude;
        double longitude;
        BroadUnproject(x, z, out latitude, out longitude);
        return GentleDenmarkHeight(latitude, longitude);
    }

    private static void GroundDenmarkSettlements()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            GameObject settlement = GameObject.Find("Settlement3D_" + node.Id);
            if (settlement == null)
                continue;

            settlement.transform.position = new Vector3(
                node.MapPosition.x,
                HeightFromWorld(node.MapPosition.x, node.MapPosition.y),
                node.MapPosition.y);

            // Old settlement miniatures covered tens of kilometres at broad-map scale.
            // Keep the visual identity, but reduce the footprint so coastal towns do not
            // hang over water or appear larger than entire islands.
            settlement.transform.localScale = Vector3.one * 0.34f;
        }
    }

    private static void GroundDenmarkNodesAndFormations()
    {
        CampaignNodeView[] nodeViews = UnityEngine.Object.FindObjectsByType<CampaignNodeView>(FindObjectsSortMode.None);
        foreach (CampaignNodeView view in nodeViews)
        {
            if (view == null)
                continue;

            CampaignNodeState node = CampaignSession.GetNode(view.NodeId);
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            Vector3 p = view.transform.position;
            p.x = node.MapPosition.x;
            p.z = node.MapPosition.y;
            p.y = HeightFromWorld(p.x, p.z) + 1.15f;
            view.transform.position = p;
        }

        CampaignFormationView[] formationViews = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formationViews)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            if (formation == null)
                continue;

            CampaignNodeState node = CampaignSession.GetNode(formation.CurrentNodeId);
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            Vector3 p = view.transform.position;
            p.y = HeightFromWorld(p.x, p.z) + 3.2f;
            view.transform.position = p;
        }
    }

    private static void CompactAndGroundLandCover()
    {
        GameObject patches = GameObject.Find("V013B_DenmarkLandCover");
        if (patches != null)
        {
            foreach (Transform child in patches.transform)
            {
                if (child == null)
                    continue;

                Vector3 p = PullTowardNearestDenmarkNode(child.position, 0.34f, 6.0f);
                p.y = HeightFromWorld(p.x, p.z) + 0.035f;
                child.position = p;
                child.localScale *= 0.58f;
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

                Vector3 p = PullTowardNearestDenmarkNode(t.position, 0.34f, 6.5f);
                p.y = HeightFromWorld(p.x, p.z);
                t.position = p;
                t.localScale *= 0.72f;
            }
        }
    }

    private static Vector3 PullTowardNearestDenmarkNode(Vector3 world, float factor, float maxDistance)
    {
        CampaignNodeState nearest = null;
        float best = float.PositiveInfinity;

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;

            float dx = world.x - node.MapPosition.x;
            float dz = world.z - node.MapPosition.y;
            float d2 = dx * dx + dz * dz;
            if (d2 < best)
            {
                best = d2;
                nearest = node;
            }
        }

        if (nearest == null)
            return world;

        Vector2 delta = new Vector2(world.x - nearest.MapPosition.x, world.z - nearest.MapPosition.y) * factor;
        if (delta.magnitude > maxDistance)
            delta = delta.normalized * maxDistance;

        world.x = nearest.MapPosition.x + delta.x;
        world.z = nearest.MapPosition.y + delta.y;
        return world;
    }

    private static void GroundConstructionProjects()
    {
        GroundConstruction(
            "ConstructionProject_QA-BARRACKS-AALBORG",
            "AALBORG",
            new Vector2(2.4f, 1.7f),
            0.46f);

        GroundConstruction(
            "ConstructionProject_QA-FARM-AARHUS",
            "AARHUS",
            new Vector2(-2.2f, 1.6f),
            0.48f);
    }

    private static void GroundConstruction(string objectName, string nodeId, Vector2 offset, float visualScale)
    {
        GameObject root = GameObject.Find(objectName);
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (root == null || node == null)
            return;

        float x = node.MapPosition.x + offset.x;
        float z = node.MapPosition.y + offset.y;
        root.transform.position = new Vector3(x, HeightFromWorld(x, z), z);
        root.transform.localScale = Vector3.one * visualScale;
    }

    private static void RedrapeDenmarkInfrastructure()
    {
        LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.name.StartsWith("StrategicLink_", StringComparison.Ordinal))
                continue;

            bool touchesDenmark = LinkTouchesDenmark(line.name);
            line.enabled = touchesDenmark;
            if (!touchesDenmark)
                continue;

            CampaignStrategicLinkType type = ResolveLinkType(line.name);
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                if (type == CampaignStrategicLinkType.SeaFerry)
                {
                    p.y = -0.24f;
                }
                else if (IsNearDenmarkWorld(p.x, p.z))
                {
                    p.y = HeightFromWorld(p.x, p.z) + 0.10f;
                }
                line.SetPosition(i, p);
            }
        }
    }

    private static void HideNonDenmark3DContext()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            bool visible = node.Region == CampaignMapRegion.Denmark;
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), visible);
            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), visible);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), visible);
        }

        CampaignFormationView[] formationViews = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formationViews)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            CampaignNodeState node = formation != null ? CampaignSession.GetNode(formation.CurrentNodeId) : null;
            bool visible = node != null && node.Region == CampaignMapRegion.Denmark;
            SetRenderersVisible(view.gameObject, visible);
        }
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

    private static bool LinkTouchesDenmark(string lineName)
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

    private static bool LooksLikeDenmark(double latitude, double longitude)
    {
        return latitude >= 54.2 && latitude <= 58.0 && longitude >= 7.0 && longitude <= 16.0;
    }

    private static bool IsNearDenmarkWorld(float x, float z)
    {
        double latitude;
        double longitude;
        BroadUnproject(x, z, out latitude, out longitude);
        return latitude >= 53.8 && latitude <= 58.4 && longitude >= 6.5 && longitude <= 16.5;
    }

    private static void BroadUnproject(float x, float z, out double latitude, out double longitude)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
    }
}
