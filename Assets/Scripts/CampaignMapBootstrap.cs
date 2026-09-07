using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CampaignNodeView : MonoBehaviour
{
    public string NodeId;
}

public sealed class CampaignFormationView : MonoBehaviour
{
    public string FormationId;
}

public sealed class CampaignMapBootstrap : MonoBehaviour
{
    private readonly Dictionary<CampaignMapRegion, Material> regionMaterials =
        new Dictionary<CampaignMapRegion, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", System.StringComparison.Ordinal))
            return;

        if (Object.FindAnyObjectByType<CampaignMapBootstrap>() != null)
            return;

        GameObject root = new GameObject("PROJECT1864_CampaignMapBootstrap_v010");
        root.AddComponent<CampaignMapBootstrap>();
    }

    private void Awake()
    {
        CampaignSession.EnsureInitialized();
        Application.targetFrameRate = 120;
        RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.62f);

        BuildMaterials();
        CreateLighting();
        CreateSeaBase();
        CreateGeographicOutlines();
        CreateStrategicLinks();
        CreateNodes();
        CreateFormationTokens();
        CreateCamera();

        GameObject systems = new GameObject("CampaignSystems");
        systems.AddComponent<CampaignMapController>();

        Debug.Log(string.Format(
            "CAMPAIGN-MAP|Built=True|Coverage=Denmark+Sweden+Norway+Finland+Germany|Nodes={0}|Map={1:0}x{2:0}|Projection=LatLon",
            CampaignSession.Nodes.Count,
            CampaignGeoProjection.MapWidth,
            CampaignGeoProjection.MapDepth));
    }

    private void BuildMaterials()
    {
        regionMaterials[CampaignMapRegion.Denmark] = CreateMaterial(new Color(0.34f, 0.48f, 0.32f), "Map_Denmark");
        regionMaterials[CampaignMapRegion.Sweden] = CreateMaterial(new Color(0.40f, 0.50f, 0.34f), "Map_Sweden");
        regionMaterials[CampaignMapRegion.Norway] = CreateMaterial(new Color(0.34f, 0.43f, 0.31f), "Map_Norway");
        regionMaterials[CampaignMapRegion.Finland] = CreateMaterial(new Color(0.43f, 0.52f, 0.38f), "Map_Finland");
        regionMaterials[CampaignMapRegion.Germany] = CreateMaterial(new Color(0.42f, 0.43f, 0.31f), "Map_Germany");
    }

    private void CreateLighting()
    {
        GameObject lightObject = new GameObject("Campaign Sun");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.96f, 0.88f);
        lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
    }

    private void CreateSeaBase()
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "Campaign Sea Base";
        sea.transform.position = new Vector3(0f, -2.1f, 0f);
        sea.transform.localScale = new Vector3(
            CampaignGeoProjection.MapWidth + 80f,
            3.0f,
            CampaignGeoProjection.MapDepth + 80f);
        sea.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.31f, 0.39f), "CampaignSea");
    }

    private void CreateGeographicOutlines()
    {
        // Coarse real-geography outline layer. It is intentionally data-driven in
        // latitude/longitude so a higher-resolution coastline/DEM can replace it
        // without changing strategic node or formation coordinates.
        DrawOutline("Germany", CampaignMapRegion.Germany, new[]
        {
            P(47.3, 7.5), P(47.5, 10.0), P(47.3, 12.3), P(48.0, 13.8),
            P(49.5, 13.5), P(50.3, 12.5), P(51.0, 14.8), P(52.5, 14.7),
            P(53.6, 14.3), P(54.4, 13.0), P(54.8, 11.0), P(54.8, 9.4),
            P(54.4, 8.6), P(53.6, 8.0), P(53.3, 7.0), P(52.2, 6.8),
            P(51.0, 6.0), P(50.0, 6.0), P(49.0, 7.2)
        });

        DrawOutline("Denmark_Jutland", CampaignMapRegion.Denmark, new[]
        {
            P(54.6, 8.0), P(54.7, 9.7), P(55.4, 10.1), P(56.4, 10.6),
            P(57.6, 10.6), P(57.8, 9.5), P(57.2, 8.2), P(55.5, 8.0)
        });
        DrawOutline("Denmark_Funen", CampaignMapRegion.Denmark, new[]
        {
            P(55.05, 9.75), P(55.12, 10.65), P(55.65, 10.75), P(55.72, 9.85)
        });
        DrawOutline("Denmark_Zealand", CampaignMapRegion.Denmark, new[]
        {
            P(55.20, 10.95), P(55.15, 12.20), P(55.55, 12.75), P(56.10, 12.25), P(56.05, 11.20)
        });

        DrawOutline("Sweden", CampaignMapRegion.Sweden, new[]
        {
            P(55.3, 12.5), P(56.0, 14.0), P(56.0, 16.0), P(58.0, 18.0),
            P(61.0, 18.0), P(64.0, 20.0), P(67.0, 24.0), P(69.0, 21.0),
            P(68.0, 18.0), P(66.0, 15.0), P(63.0, 12.0), P(60.0, 12.0), P(58.0, 11.0)
        });

        DrawOutline("Norway", CampaignMapRegion.Norway, new[]
        {
            P(58.0, 5.0), P(59.0, 5.0), P(60.0, 5.0), P(62.0, 5.0),
            P(64.0, 6.0), P(66.0, 12.0), P(68.0, 14.0), P(70.0, 20.0),
            P(71.0, 25.0), P(69.0, 29.0), P(66.0, 24.0), P(64.0, 14.0),
            P(61.0, 12.0), P(59.0, 12.0), P(58.0, 8.0)
        });

        DrawOutline("Finland", CampaignMapRegion.Finland, new[]
        {
            P(59.7, 20.5), P(60.0, 23.0), P(60.0, 27.0), P(62.0, 30.0),
            P(65.0, 29.0), P(68.0, 27.0), P(69.5, 28.0), P(69.8, 23.0),
            P(67.0, 21.0), P(64.0, 21.0), P(62.0, 20.0)
        });
    }

    private void DrawOutline(string name, CampaignMapRegion region, Vector3[] points)
    {
        GameObject root = new GameObject("Outline_" + name);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = 1.25f;
        line.positionCount = points.Length;
        line.sharedMaterial = regionMaterials[region];

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 p = points[i];
            p.y = 0.5f;
            line.SetPosition(i, p);
        }
    }

    private void CreateStrategicLinks()
    {
        HashSet<string> created = new HashSet<string>();
        Material roadMaterial = CreateMaterial(new Color(0.58f, 0.53f, 0.41f), "CampaignStrategicLink");

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState a = pair.Value;
            if (a == null)
                continue;

            foreach (string linkedId in a.Links)
            {
                CampaignNodeState b = CampaignSession.GetNode(linkedId);
                if (b == null)
                    continue;

                string key = string.CompareOrdinal(a.Id, b.Id) < 0
                    ? a.Id + "|" + b.Id
                    : b.Id + "|" + a.Id;
                if (!created.Add(key))
                    continue;

                Vector3 start = new Vector3(a.MapPosition.x, 0.25f, a.MapPosition.y);
                Vector3 end = new Vector3(b.MapPosition.x, 0.25f, b.MapPosition.y);
                CreateLineSegment("StrategicLink_" + key, start, end, 0.55f, roadMaterial);
            }
        }
    }

    private void CreateNodes()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CampaignNode_" + node.Id;
            marker.transform.position = new Vector3(node.MapPosition.x, 1.25f, node.MapPosition.y);
            marker.transform.localScale = node.Terrain == CampaignTerrainType.Fortified
                ? new Vector3(3.2f, 0.55f, 3.2f)
                : new Vector3(2.2f, 0.45f, 2.2f);
            marker.GetComponent<Renderer>().sharedMaterial = regionMaterials[node.Region];

            CampaignNodeView view = marker.AddComponent<CampaignNodeView>();
            view.NodeId = node.Id;
        }
    }

    private void CreateFormationTokens()
    {
        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState formation = pair.Value;
            CampaignNodeState node = CampaignSession.GetNode(formation.CurrentNodeId);
            if (formation == null || node == null)
                continue;

            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cube);
            token.name = "CampaignFormation_" + formation.Id;
            token.transform.position = new Vector3(node.MapPosition.x, 4.0f, node.MapPosition.y);
            token.transform.localScale = new Vector3(5.5f, 2.2f, 3.0f);
            token.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                formation.Nation == CampaignNation.Denmark
                    ? new Color(0.15f, 0.25f, 0.45f)
                    : new Color(0.12f, 0.12f, 0.14f),
                "Formation_" + formation.Id);

            CampaignFormationView view = token.AddComponent<CampaignFormationView>();
            view.FormationId = formation.Id;
        }
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Campaign Camera");
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1800f;
        cameraObject.transform.position = new Vector3(-25f, 410f, -145f);
        cameraObject.transform.rotation = Quaternion.Euler(56f, 7f, 0f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CampaignMapCameraController>();
    }

    private static void CreateLineSegment(string name, Vector3 start, Vector3 end, float width, Material material)
    {
        GameObject root = new GameObject(name);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private static Vector3 P(double latitude, double longitude)
    {
        return CampaignGeoProjection.Project3D(latitude, longitude, 0f);
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }
}
