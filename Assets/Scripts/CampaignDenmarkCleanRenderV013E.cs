using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13e — clean Denmark render foundation.
//
// This pass intentionally stops stacking visual repair layers on top of the old v13
// procedural world. The strategic simulation remains untouched. Only presentation is
// rebuilt for a Denmark-first QA slice:
//   CLEAN SEA -> DIRECT LAT/LON DENMARK -> CLEAN LINKS -> CLEAN SETTLEMENTS -> TOKENS/UI.
[DefaultExecutionOrder(3200)]
public sealed class CampaignDenmarkCleanRenderV013E : MonoBehaviour
{
    public const string BuildTag = "v00.00.13e";
    public const string CleanRootName = "V013E_DENMARK_CLEAN_RENDER";

    private static readonly HashSet<string> SouthernContextNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "FLENSBURG",
        "SCHLESWIG"
    };

    private static readonly HashSet<string> PrimaryLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "AALBORG",
        "AARHUS",
        "FREDERICIA",
        "ODENSE",
        "CPH",
        "FLENSBURG"
    };

    private static readonly HashSet<string> SecondaryLabels = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING",
        "VIBORG",
        "VEJLE",
        "KOLDING",
        "HADERSLEV",
        "DYBBOEL",
        "SONDERBORG",
        "KORSOR",
        "ROSKILDE",
        "SCHLESWIG"
    };

    private Material seaMaterial;
    private Material landMaterial;
    private Material coastMaterial;
    private Material roadMaterial;
    private Material railMaterial;
    private Material ferryMaterial;
    private Material townWallMaterial;
    private Material townRoofMaterial;
    private Material fortMaterial;

    private Transform cleanRoot;
    private Transform geographyRoot;
    private Transform infrastructureRoot;
    private Transform settlementRoot;

    private CampaignMapController mapController;
    private FieldInfo nodeLabelStyleField;
    private GUIStyle hiddenNodeLabelStyle;
    private GUIStyle cleanLabelStyle;
    private bool built;

    public static bool DenmarkFocusActive
    {
        get
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!DenmarkFocusActive)
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkCleanRenderV013E>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkCleanRenderV013E");
        root.AddComponent<CampaignDenmarkCleanRenderV013E>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();

        // Older passes may already have created their presentation objects during Start().
        // Disable their behaviours and then hide every old render layer in one deterministic
        // cleanup step. We do not delete campaign state, colliders or simulation objects.
        DisableLegacyPresentationBehaviours();
        HideLegacyRendererTrees();
        HideLegacyRootLevelMarkers();

        mapController = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (mapController != null)
        {
            nodeLabelStyleField = typeof(CampaignMapController).GetField(
                "nodeLabelStyle",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        BuildMaterials();
        CreateRootHierarchy();
        BuildCleanSea();
        built = BuildDirectDenmark();
        BuildCleanInfrastructure();
        BuildCleanSettlements();
        GroundInteractiveMarkersAndFormations();
        GroundConstructionProjects();
        TuneCameraForDenmark();
        TuneLighting();
        SuppressLegacyNodeLabels();

        Debug.Log(string.Format(
            "CAMPAIGN-V013E|CleanRender=True|DirectDenmark={0}|OldL1=False|OldL2=False|OldL3=False|OldL4=False|OldL5=False|OldL8=False|CleanSea=True|CleanSettlements=True|LabelLOD=True|SimulationChanged=False",
            built));
    }

    private void LateUpdate()
    {
        // Keep the old render hierarchy suppressed even if another helper tries to toggle it.
        HideLegacyRendererTrees();
        HideLegacyRootLevelMarkers();
        GroundInteractiveMarkersAndFormations();
        GroundConstructionProjects();
        SuppressLegacyNodeLabels();
    }

    private static void DisableLegacyPresentationBehaviours()
    {
        string[] names =
        {
            "CampaignVisualPolishV013A",
            "CampaignVisualPolishV013B",
            "CampaignDenmarkCleanupV013C",
            "CampaignDenmarkV013DLegacyGate",
            "CampaignDenmarkMapRebuildV013D",
            "CampaignMapUsabilityV011",
            "CampaignMapSearchHoverV011",
            "CampaignLayerManagerV013"
        };

        HashSet<string> disabledNames = new HashSet<string>(names, StringComparer.Ordinal);
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour is CampaignDenmarkCleanRenderV013E)
                continue;

            if (disabledNames.Contains(behaviour.GetType().Name))
                behaviour.enabled = false;
        }
    }

    private static void HideLegacyRendererTrees()
    {
        HideRenderersUnder(CampaignTerrainV013.RootTerrain);
        HideRenderersUnder(CampaignTerrainV013.RootHydrology);
        HideRenderersUnder(CampaignTerrainV013.RootLandCover);
        HideRenderersUnder(CampaignTerrainV013.RootInfrastructure);
        HideRenderersUnder(CampaignTerrainV013.RootSettlements);
        HideRenderersUnder(CampaignTerrainV013.RootOverlays);

        HideRenderersUnder("GEO_Denmark_NaturalEarth50m");
        HideRenderersUnder("GEO_Denmark_NaturalEarth50m_V013A");
        HideRenderersUnder("GEO_Denmark_NaturalEarth50m_V013C_BROAD");
        HideRenderersUnder("GEO_Denmark_V013D_BROAD_DIRECT");
        HideRenderersUnder("V013B_DenmarkLandCover");
        HideRenderersUnder("V013B_Vegetation");
        HideRenderersUnder("Campaign Sea Base");
        HideRenderersUnder("CampaignTerrainSurface_v013");
    }

    private static void HideLegacyRootLevelMarkers()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
        }
    }

    private void BuildMaterials()
    {
        seaMaterial = CreateMaterial(new Color(0.085f, 0.19f, 0.275f), "V013E_Sea");
        landMaterial = CreateMaterial(new Color(0.315f, 0.425f, 0.245f), "V013E_DenmarkLand");
        coastMaterial = CreateMaterial(new Color(0.78f, 0.75f, 0.58f), "V013E_Coast");
        roadMaterial = CreateMaterial(new Color(0.48f, 0.39f, 0.27f), "V013E_Road");
        railMaterial = CreateMaterial(new Color(0.17f, 0.18f, 0.18f), "V013E_Rail");
        ferryMaterial = CreateMaterial(new Color(0.20f, 0.45f, 0.60f), "V013E_Ferry");
        townWallMaterial = CreateMaterial(new Color(0.60f, 0.53f, 0.41f), "V013E_TownWall");
        townRoofMaterial = CreateMaterial(new Color(0.29f, 0.17f, 0.13f), "V013E_TownRoof");
        fortMaterial = CreateMaterial(new Color(0.39f, 0.37f, 0.31f), "V013E_Fort");

        SetSmoothness(seaMaterial, 0.40f);
        SetSmoothness(landMaterial, 0.05f);
        SetSmoothness(coastMaterial, 0.03f);
    }

    private void CreateRootHierarchy()
    {
        GameObject existing = GameObject.Find(CleanRootName);
        if (existing != null)
            UnityEngine.Object.Destroy(existing);

        GameObject root = new GameObject(CleanRootName);
        cleanRoot = root.transform;

        geographyRoot = CreateChildRoot(cleanRoot, "01_GEOGRAPHY");
        infrastructureRoot = CreateChildRoot(cleanRoot, "02_INFRASTRUCTURE");
        settlementRoot = CreateChildRoot(cleanRoot, "03_SETTLEMENTS");
    }

    private void BuildCleanSea()
    {
        Vector3 centre = CampaignGeoProjection.Project3D(56.0, 10.25, -0.55f);

        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "V013E_CleanSea";
        sea.transform.SetParent(geographyRoot, false);
        sea.transform.position = centre;
        sea.transform.localScale = new Vector3(500f, 0.12f, 420f);

        Renderer renderer = sea.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = seaMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        RemoveCollider(sea);
    }

    private bool BuildDirectDenmark()
    {
        Vector2[][] rings = GetDenmarkRings();
        if (rings == null || rings.Length == 0)
        {
            Debug.LogError("CAMPAIGN-V013E|DenmarkRings=False|Reason=ReflectionFailed");
            return false;
        }

        GameObject landRootObject = new GameObject("V013E_Denmark_DirectBroad");
        landRootObject.transform.SetParent(geographyRoot, false);

        for (int r = 0; r < rings.Length; r++)
        {
            Vector2[] ring = rings[r];
            if (ring == null || ring.Length < 3)
                continue;

            GameObject island = new GameObject("V013E_LandPart_" + (r + 1));
            island.transform.SetParent(landRootObject.transform, false);

            Vector3[] vertices = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                double longitude = ring[i].x;
                double latitude = ring[i].y;
                vertices[i] = CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    SampleDenmarkHeight(latitude, longitude));
            }

            int[] triangles = Triangulate(ring);
            if (triangles.Length < 3)
            {
                Debug.LogWarning("CAMPAIGN-V013E|Triangulation=False|Part=" + (r + 1));
                continue;
            }

            Mesh mesh = new Mesh
            {
                name = "V013E_DenmarkMesh_" + (r + 1),
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = island.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer meshRenderer = island.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = landMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            GameObject coast = new GameObject("V013E_Coast_" + (r + 1));
            coast.transform.SetParent(landRootObject.transform, false);

            LineRenderer line = coast.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.widthMultiplier = 0.075f;
            line.positionCount = ring.Length;
            line.sharedMaterial = coastMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            for (int i = 0; i < ring.Length; i++)
            {
                double longitude = ring[i].x;
                double latitude = ring[i].y;
                line.SetPosition(i, CampaignGeoProjection.Project3D(
                    latitude,
                    longitude,
                    SampleDenmarkHeight(latitude, longitude) + 0.045f));
            }
        }

        return true;
    }

    private void BuildCleanInfrastructure()
    {
        HashSet<string> created = new HashSet<string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState a = pair.Value;
            if (a == null || !IsFocusNode(a))
                continue;

            foreach (string linkedId in a.Links)
            {
                CampaignNodeState b = CampaignSession.GetNode(linkedId);
                if (b == null || !IsFocusNode(b))
                    continue;

                string key = MakeEdgeKey(a.Id, b.Id);
                if (!created.Add(key))
                    continue;

                CampaignStrategicLinkType type = CampaignMapUsabilityV011.GetLinkType(a, b);
                Material material = type == CampaignStrategicLinkType.Rail
                    ? railMaterial
                    : type == CampaignStrategicLinkType.SeaFerry
                        ? ferryMaterial
                        : roadMaterial;

                GameObject lineObject = new GameObject("V013E_Link_" + key);
                lineObject.transform.SetParent(infrastructureRoot, false);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 16;
                line.sharedMaterial = material;
                line.widthMultiplier = type == CampaignStrategicLinkType.Rail ? 0.18f : 0.14f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    double latitude = a.Latitude + (b.Latitude - a.Latitude) * t;
                    double longitude = a.Longitude + (b.Longitude - a.Longitude) * t;
                    float y = type == CampaignStrategicLinkType.SeaFerry
                        ? -0.47f
                        : SampleDenmarkHeight(latitude, longitude) + 0.075f;

                    line.SetPosition(i, CampaignGeoProjection.Project3D(latitude, longitude, y));
                }
            }
        }
    }

    private void BuildCleanSettlements()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || !IsFocusNode(node))
                continue;

            float y = SampleDenmarkHeight(node.Latitude, node.Longitude);
            GameObject root = new GameObject("V013E_Settlement_" + node.Id);
            root.transform.SetParent(settlementRoot, false);
            root.transform.position = new Vector3(node.MapPosition.x, y + 0.025f, node.MapPosition.y);

            int houseCount = node.Terrain == CampaignTerrainType.Urban ? 3 : 2;
            if (node.Terrain == CampaignTerrainType.Fortified)
                houseCount = 2;

            int hash = StableHash(node.Id);
            for (int i = 0; i < houseCount; i++)
            {
                float angle = ((hash + i * 97) % 360) * Mathf.Deg2Rad;
                float radius = i == 0 ? 0.0f : 0.78f + 0.18f * i;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                CreateHouse(root.transform, offset, 0.72f + (i % 2) * 0.12f);
            }

            if (node.Terrain == CampaignTerrainType.Fortified)
            {
                GameObject fort = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fort.name = "Fort";
                fort.transform.SetParent(root.transform, false);
                fort.transform.localPosition = new Vector3(0f, 0.10f, 0f);
                fort.transform.localScale = new Vector3(1.15f, 0.10f, 1.15f);
                Renderer renderer = fort.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = fortMaterial;
                RemoveCollider(fort);
            }
        }
    }

    private void CreateHouse(Transform parent, Vector3 localPosition, float scale)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "House";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = localPosition + Vector3.up * 0.23f * scale;
        body.transform.localScale = new Vector3(0.58f, 0.46f, 0.48f) * scale;
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyRenderer.sharedMaterial = townWallMaterial;
        RemoveCollider(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(body.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.57f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.localScale = new Vector3(0.72f, 0.72f, 1.05f);
        Renderer roofRenderer = roof.GetComponent<Renderer>();
        if (roofRenderer != null)
            roofRenderer.sharedMaterial = townRoofMaterial;
        RemoveCollider(roof);
    }

    private static void GroundInteractiveMarkersAndFormations()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            GameObject marker = GameObject.Find("CampaignNode_" + node.Id);
            if (marker != null && IsFocusNode(node))
            {
                marker.transform.position = new Vector3(
                    node.MapPosition.x,
                    SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.55f,
                    node.MapPosition.y);
            }
        }

        CampaignFormationView[] formations = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in formations)
        {
            if (view == null)
                continue;

            CampaignFormationState formation = CampaignSession.GetFormation(view.FormationId);
            if (formation == null)
                continue;

            CampaignNodeState current = CampaignSession.GetNode(formation.CurrentNodeId);
            bool visible = current != null && IsFocusNode(current);
            SetRenderersVisible(view.gameObject, visible);
            if (!visible)
                continue;

            Vector3 p = view.transform.position;
            p.y = HeightFromWorld(p.x, p.z) + 1.75f;
            view.transform.position = p;
            view.transform.localScale = new Vector3(2.25f, 0.90f, 1.25f);
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
        if (root == null || node == null)
            return;

        root.transform.position = new Vector3(
            node.MapPosition.x + offset.x,
            SampleDenmarkHeight(node.Latitude, node.Longitude) + 0.03f,
            node.MapPosition.y + offset.y);
        root.transform.localScale = Vector3.one * scale;
        SetRenderersVisible(root, true);
    }

    private static void TuneCameraForDenmark()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        Vector3 centre = CampaignGeoProjection.Project3D(56.05, 10.25, 0f);
        Vector3 homePosition = new Vector3(centre.x, 150f, centre.z - 62f);
        Quaternion homeRotation = Quaternion.Euler(59f, 0f, 0f);

        cam.transform.position = homePosition;
        cam.transform.rotation = homeRotation;
        cam.fieldOfView = 44f;

        if (controller == null)
            return;

        controller.MinHeight = 42f;
        controller.MaxHeight = 360f;
        controller.PanSpeed = 72f;
        controller.ZoomSpeed = 80f;

        FieldInfo homePositionField = typeof(CampaignMapCameraController).GetField(
            "homePosition",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo homeRotationField = typeof(CampaignMapCameraController).GetField(
            "homeRotation",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (homePositionField != null)
            homePositionField.SetValue(controller, homePosition);
        if (homeRotationField != null)
            homeRotationField.SetValue(controller, homeRotation);
    }

    private static void TuneLighting()
    {
        RenderSettings.ambientLight = new Color(0.48f, 0.51f, 0.46f);

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.intensity = 0.94f;
            light.color = new Color(1.0f, 0.95f, 0.86f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.44f;
        }
    }

    private void SuppressLegacyNodeLabels()
    {
        if (mapController == null || nodeLabelStyleField == null)
            return;

        if (hiddenNodeLabelStyle == null)
        {
            hiddenNodeLabelStyle = new GUIStyle
            {
                fontSize = 1,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            hiddenNodeLabelStyle.normal.textColor = Color.clear;
            hiddenNodeLabelStyle.hover.textColor = Color.clear;
            hiddenNodeLabelStyle.active.textColor = Color.clear;
            hiddenNodeLabelStyle.focused.textColor = Color.clear;
        }

        nodeLabelStyleField.SetValue(mapController, hiddenNodeLabelStyle);
    }

    private void OnGUI()
    {
        SuppressLegacyNodeLabels();

        Camera cam = Camera.main;
        if (!built || cam == null)
            return;

        if (cleanLabelStyle == null)
        {
            cleanLabelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2)
            };
            cleanLabelStyle.normal.textColor = new Color(0.94f, 0.94f, 0.90f);
        }

        float cameraHeight = cam.transform.position.y;
        List<Rect> occupied = new List<Rect>();

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || !IsFocusNode(node) || !ShouldShowLabel(node.Id, cameraHeight))
                continue;

            float y = SampleDenmarkHeight(node.Latitude, node.Longitude) + 1.0f;
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(node.MapPosition.x, y, node.MapPosition.y));
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 52f || guiY > Screen.height - 8f)
                continue;

            string flags = string.Empty;
            if (node.HasPort) flags += " ⚓";
            if (node.HasRail) flags += " R";
            if (node.Terrain == CampaignTerrainType.Fortified) flags += " F";

            Rect rect = new Rect(screen.x - 42f, guiY - 9f, 84f, 18f);
            int pass = 0;
            while (pass++ < 8 && OverlapsAny(rect, occupied))
                rect.y -= 18f;

            occupied.Add(rect);
            GUI.Box(rect, node.Name + flags, cleanLabelStyle);
        }
    }

    private static bool ShouldShowLabel(string nodeId, float cameraHeight)
    {
        if (cameraHeight > 140f)
            return PrimaryLabels.Contains(nodeId);

        if (cameraHeight > 92f)
            return PrimaryLabels.Contains(nodeId) || SecondaryLabels.Contains(nodeId);

        return true;
    }

    public static float SampleDenmarkHeight(double latitude, double longitude)
    {
        float x = (float)((longitude - 8.0) / 7.5);
        float z = (float)((latitude - 54.4) / 3.5);
        float broad = Mathf.PerlinNoise(x * 1.35f + 4.2f, z * 1.30f + 7.6f) - 0.5f;

        float dx = (float)(longitude - 9.35);
        float dz = (float)(latitude - 56.15);
        float centralJutland = Mathf.Exp(-(dx * dx / 1.55f + dz * dz / 1.35f)) * 0.055f;

        return 0.28f + broad * 0.055f + centralJutland;
    }

    private static float HeightFromWorld(float x, float z)
    {
        double lon01 = x / CampaignGeoProjection.MapWidth + 0.5;
        double lat01 = z / CampaignGeoProjection.MapDepth + 0.5;
        double longitude = CampaignGeoProjection.MinLongitude + lon01 * (CampaignGeoProjection.MaxLongitude - CampaignGeoProjection.MinLongitude);
        double latitude = CampaignGeoProjection.MinLatitude + lat01 * (CampaignGeoProjection.MaxLatitude - CampaignGeoProjection.MinLatitude);
        return SampleDenmarkHeight(latitude, longitude);
    }

    private static Vector2[][] GetDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField(
            "DenmarkRings",
            BindingFlags.Static | BindingFlags.NonPublic);
        return field != null ? field.GetValue(null) as Vector2[][] : null;
    }

    private static bool IsFocusNode(CampaignNodeState node)
    {
        if (node == null)
            return false;
        if (node.Region == CampaignMapRegion.Denmark)
            return true;
        return node.Controller == CampaignNation.Denmark && SouthernContextNodes.Contains(node.Id);
    }

    private static string MakeEdgeKey(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
    }

    private static Transform CreateChildRoot(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void HideRenderersUnder(string objectName)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
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

    private static bool OverlapsAny(Rect candidate, List<Rect> occupied)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            Rect expanded = occupied[i];
            expanded.xMin -= 2f;
            expanded.xMax += 2f;
            expanded.yMin -= 2f;
            expanded.yMax += 2f;
            if (candidate.Overlaps(expanded))
                return true;
        }
        return false;
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
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", value);
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj != null ? obj.GetComponent<Collider>() : null;
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }

    private static int[] Triangulate(Vector2[] input)
    {
        int n = input != null ? input.Length : 0;
        if (n < 3)
            return new int[0];

        List<int> indices = new List<int>(n);
        if (SignedArea(input) > 0f)
        {
            for (int i = 0; i < n; i++)
                indices.Add(i);
        }
        else
        {
            for (int i = n - 1; i >= 0; i--)
                indices.Add(i);
        }

        List<int> triangles = new List<int>((n - 2) * 3);
        int guard = 0;

        while (indices.Count > 2 && guard++ < n * n * 2)
        {
            bool clipped = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 a = input[prev];
                Vector2 b = input[curr];
                Vector2 c = input[next];

                if (Cross(b - a, c - b) <= 0.0000001f)
                    continue;

                bool contains = false;
                for (int k = 0; k < indices.Count; k++)
                {
                    int candidate = indices[k];
                    if (candidate == prev || candidate == curr || candidate == next)
                        continue;

                    if (PointInTriangle(input[candidate], a, b, c))
                    {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                    continue;

                triangles.Add(prev);
                triangles.Add(curr);
                triangles.Add(next);
                indices.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
                break;
        }

        return triangles.ToArray();
    }

    private static float SignedArea(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            area += points[j].x * points[i].y - points[i].x * points[j].y;
        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);

        bool hasNegative = c1 < -0.000001f || c2 < -0.000001f || c3 < -0.000001f;
        bool hasPositive = c1 > 0.000001f || c2 > 0.000001f || c3 > 0.000001f;
        return !(hasNegative && hasPositive);
    }
}
