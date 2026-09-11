using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-29000)]
public sealed class CampaignMapStyleSwitcher : MonoBehaviour
{
    private sealed class MapProfile
    {
        public string ShortName;
        public string Name;
        public string Description;
        public Color Land;
        public Color Sea;
        public Color Coast;
        public Color Zone;
        public Color City;
        public Color Army;
        public Color Ambient;
        public Color Sun;
        public Color Fog;
        public float SunIntensity;
        public float CoastWidth;
        public float Relief;
        public float Tilt;
        public float OrthoSize;
        public float ZoneScale;
        public float CityScale;
        public bool FogEnabled;
        public float FogDensity;
        public bool Grid;
        public bool TileGrid;
        public bool Adjacency;
    }

    private sealed class MeshSnapshot
    {
        public MeshFilter Filter;
        public Vector3[] OriginalVertices;
    }

    private sealed class ScaleSnapshot
    {
        public Transform Transform;
        public Vector3 OriginalScale;
        public string Kind;
    }

    private readonly List<MapProfile> profiles = new List<MapProfile>();
    private readonly List<MeshSnapshot> landMeshes = new List<MeshSnapshot>();
    private readonly List<ScaleSnapshot> markerScales = new List<ScaleSnapshot>();

    private Camera campaignCamera;
    private Light campaignSun;
    private GameObject gridRoot;
    private GameObject tileGridRoot;
    private GameObject adjacencyRoot;
    private int activeProfile;
    private bool ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignMapStyleSwitcher>() != null)
            return;

        GameObject root = new GameObject("PROJECT1864_CampaignMapVisualLab_v000010f");
        DontDestroyOnLoad(root);
        root.AddComponent<CampaignMapStyleSwitcher>();
    }

    private IEnumerator Start()
    {
        BuildProfiles();

        // GrandCampaignBootstrap creates the v10e geography at runtime.
        // Wait a few frames so this visual-only layer can safely snapshot it.
        for (int i = 0; i < 120; i++)
        {
            campaignCamera = Camera.main;
            if (campaignCamera != null && GameObject.Find("GEO_Denmark_NaturalEarth50m") != null)
                break;
            yield return null;
        }

        CaptureScene();
        BuildOverlayLayers();

        ready = campaignCamera != null && landMeshes.Count > 0;
        if (ready)
        {
            ApplyProfile(0);
            Debug.Log("CAMPAIGN-MAPLAB11|Installed=True|Base=v00.00.10e|Profiles=11|Version=v00.00.10f|Branch=channel-campaign3");
        }
        else
        {
            Debug.LogWarning("CAMPAIGN-MAPLAB11|Installed=False|Reason=V10E_GeographyOrCameraNotFound");
        }
    }

    private void BuildProfiles()
    {
        profiles.Clear();

        profiles.Add(new MapProfile {
            ShortName = "1 DEM",
            Name = "1. Dansk højdemodel -> Unity 3D Terrain",
            Description = "Preview af detaljeret dansk relief: hævet terræn, naturlige farver og skråt strategikamera.",
            Land = C(0.34f,0.47f,0.24f), Sea = C(0.11f,0.31f,0.43f), Coast = C(0.82f,0.80f,0.65f),
            Zone = C(0.78f,0.63f,0.20f), City = C(0.92f,0.88f,0.74f), Army = C(0.75f,0.16f,0.14f),
            Ambient = C(0.52f,0.57f,0.50f), Sun = C(1.00f,0.95f,0.82f), Fog = C(0.54f,0.63f,0.66f),
            SunIntensity = 1.20f, CoastWidth = 0.07f, Relief = 1.25f, Tilt = 28f, OrthoSize = 42f,
            ZoneScale = 0.92f, CityScale = 1.00f, FogEnabled = true, FogDensity = 0.0022f
        });

        profiles.Add(new MapProfile {
            ShortName = "2 Cesium",
            Name = "2. Cesium World Terrain + egne 1851-lag",
            Description = "Cesium-inspireret world-terrain preview med høj dybde, kølig atmosfære og diskrete gameplay-markører.",
            Land = C(0.29f,0.41f,0.23f), Sea = C(0.08f,0.23f,0.34f), Coast = C(0.70f,0.76f,0.68f),
            Zone = C(0.86f,0.69f,0.26f), City = C(0.92f,0.91f,0.82f), Army = C(0.78f,0.17f,0.15f),
            Ambient = C(0.46f,0.52f,0.55f), Sun = C(1.00f,0.97f,0.90f), Fog = C(0.42f,0.55f,0.63f),
            SunIntensity = 1.35f, CoastWidth = 0.05f, Relief = 1.65f, Tilt = 34f, OrthoSize = 45f,
            ZoneScale = 0.80f, CityScale = 0.92f, FogEnabled = true, FogDensity = 0.0030f
        });

        profiles.Add(new MapProfile {
            ShortName = "3 ArcGIS",
            Name = "3. ArcGIS Maps SDK + historiske lag",
            Description = "Professionel GIS-preview med ren kartografi, tydelige lag og koordinatnet.",
            Land = C(0.55f,0.62f,0.43f), Sea = C(0.48f,0.67f,0.78f), Coast = C(0.16f,0.24f,0.20f),
            Zone = C(0.86f,0.52f,0.12f), City = C(0.28f,0.20f,0.16f), Army = C(0.68f,0.10f,0.10f),
            Ambient = C(0.72f,0.72f,0.68f), Sun = C(1.00f,1.00f,0.96f), Fog = C(0.75f,0.82f,0.84f),
            SunIntensity = 0.95f, CoastWidth = 0.09f, Relief = 0.45f, Tilt = 12f, OrthoSize = 43f,
            ZoneScale = 0.88f, CityScale = 1.05f, FogEnabled = false, Grid = true
        });

        profiles.Add(new MapProfile {
            ShortName = "4 MapTiler",
            Name = "4. MapTiler 3D Terrain + Cesium",
            Description = "Moderne terrain/vector-hybrid preview: skarpe kyster, diskret relief og tile-orienteret læsbarhed.",
            Land = C(0.42f,0.54f,0.31f), Sea = C(0.21f,0.43f,0.57f), Coast = C(0.90f,0.89f,0.79f),
            Zone = C(0.92f,0.68f,0.18f), City = C(0.95f,0.93f,0.84f), Army = C(0.78f,0.13f,0.13f),
            Ambient = C(0.60f,0.64f,0.61f), Sun = C(1.00f,0.97f,0.88f), Fog = C(0.65f,0.73f,0.75f),
            SunIntensity = 1.12f, CoastWidth = 0.07f, Relief = 0.85f, Tilt = 22f, OrthoSize = 43f,
            ZoneScale = 0.88f, CityScale = 0.98f, FogEnabled = false, TileGrid = true
        });

        profiles.Add(new MapProfile {
            ShortName = "5 1850-kort",
            Name = "5. Historisk 1850-kort draperet over 3D-relief",
            Description = "Historisk papir/sepia art-direction preview over let relief. Ingen historiske rasterdata er endnu indlæst.",
            Land = C(0.63f,0.55f,0.36f), Sea = C(0.42f,0.53f,0.52f), Coast = C(0.25f,0.20f,0.13f),
            Zone = C(0.52f,0.24f,0.10f), City = C(0.20f,0.14f,0.09f), Army = C(0.55f,0.08f,0.07f),
            Ambient = C(0.70f,0.62f,0.48f), Sun = C(1.00f,0.89f,0.66f), Fog = C(0.70f,0.63f,0.50f),
            SunIntensity = 0.82f, CoastWidth = 0.11f, Relief = 0.55f, Tilt = 16f, OrthoSize = 42f,
            ZoneScale = 0.85f, CityScale = 1.08f, FogEnabled = true, FogDensity = 0.0012f, Adjacency = true
        });

        profiles.Add(new MapProfile {
            ShortName = "6 QGIS",
            Name = "6. QGIS/Blender -> baked terrain tiles",
            Description = "Baked-tile preview med tydeligt relief, kraftigere lys/skygge og teknisk tile-grid.",
            Land = C(0.37f,0.49f,0.28f), Sea = C(0.13f,0.34f,0.47f), Coast = C(0.80f,0.81f,0.67f),
            Zone = C(0.88f,0.62f,0.16f), City = C(0.94f,0.90f,0.78f), Army = C(0.76f,0.12f,0.11f),
            Ambient = C(0.43f,0.46f,0.42f), Sun = C(1.00f,0.93f,0.78f), Fog = C(0.55f,0.63f,0.64f),
            SunIntensity = 1.50f, CoastWidth = 0.07f, Relief = 1.55f, Tilt = 32f, OrthoSize = 43f,
            ZoneScale = 0.86f, CityScale = 0.98f, FogEnabled = false, TileGrid = true
        });

        profiles.Add(new MapProfile {
            ShortName = "7 Procedural",
            Name = "7. DEM + procedural marker/skov/hede/by",
            Description = "Gameplay-favorit preview med tydeligt terræn og høj kontrast mellem geografi og simulation.",
            Land = C(0.31f,0.45f,0.22f), Sea = C(0.10f,0.30f,0.40f), Coast = C(0.75f,0.78f,0.62f),
            Zone = C(0.95f,0.72f,0.15f), City = C(0.96f,0.90f,0.73f), Army = C(0.82f,0.12f,0.10f),
            Ambient = C(0.49f,0.55f,0.46f), Sun = C(1.00f,0.95f,0.81f), Fog = C(0.50f,0.60f,0.60f),
            SunIntensity = 1.25f, CoastWidth = 0.08f, Relief = 1.20f, Tilt = 25f, OrthoSize = 42f,
            ZoneScale = 1.10f, CityScale = 1.12f, FogEnabled = false, Adjacency = true
        });

        profiles.Add(new MapProfile {
            ShortName = "8 OSM",
            Name = "8. OpenStreetMap-geometri + historisk korrektion",
            Description = "OSM-inspireret geometrisk preview med høj netværkslæsbarhed. Moderne OSM-data er ikke indlæst her.",
            Land = C(0.72f,0.75f,0.61f), Sea = C(0.64f,0.79f,0.86f), Coast = C(0.28f,0.34f,0.28f),
            Zone = C(0.86f,0.48f,0.08f), City = C(0.22f,0.20f,0.17f), Army = C(0.70f,0.08f,0.08f),
            Ambient = C(0.78f,0.78f,0.74f), Sun = C(1.00f,1.00f,0.98f), Fog = C(0.78f,0.84f,0.84f),
            SunIntensity = 0.80f, CoastWidth = 0.10f, Relief = 0.15f, Tilt = 6f, OrthoSize = 43f,
            ZoneScale = 0.90f, CityScale = 1.12f, FogEnabled = false, Grid = true, Adjacency = true
        });

        profiles.Add(new MapProfile {
            ShortName = "9 Painted",
            Name = "9. Hybrid satellit/terrain-look + malet 1851-overflade",
            Description = "Dramatisk painterly terrain preview med dybere hav, mørkere land og kraftigere relief.",
            Land = C(0.24f,0.34f,0.20f), Sea = C(0.055f,0.17f,0.25f), Coast = C(0.64f,0.67f,0.53f),
            Zone = C(0.90f,0.66f,0.16f), City = C(0.90f,0.86f,0.70f), Army = C(0.82f,0.16f,0.12f),
            Ambient = C(0.31f,0.36f,0.34f), Sun = C(1.00f,0.86f,0.68f), Fog = C(0.25f,0.35f,0.39f),
            SunIntensity = 1.60f, CoastWidth = 0.06f, Relief = 1.85f, Tilt = 36f, OrthoSize = 44f,
            ZoneScale = 0.92f, CityScale = 0.98f, FogEnabled = true, FogDensity = 0.0040f
        });

        profiles.Add(new MapProfile {
            ShortName = "10 Diorama",
            Name = "10. Håndbygget modeljernbane/diorama-stil",
            Description = "Miniature/diorama-preview: kraftigt relief, varm belysning, tydelig kyst og større fysiske markører.",
            Land = C(0.40f,0.52f,0.25f), Sea = C(0.16f,0.38f,0.50f), Coast = C(0.92f,0.86f,0.67f),
            Zone = C(0.98f,0.72f,0.17f), City = C(0.98f,0.93f,0.78f), Army = C(0.82f,0.14f,0.11f),
            Ambient = C(0.58f,0.52f,0.41f), Sun = C(1.00f,0.83f,0.60f), Fog = C(0.60f,0.64f,0.58f),
            SunIntensity = 1.75f, CoastWidth = 0.13f, Relief = 2.20f, Tilt = 42f, OrthoSize = 42f,
            ZoneScale = 1.18f, CityScale = 1.30f, FogEnabled = false
        });

        profiles.Add(new MapProfile {
            ShortName = "11 HOI4",
            Name = "11. HOI4-lignende polygon/regionkort 2D/2.5D",
            Description = "Strategisk 2.5D-preview med fladt kort, maksimal kontrast, tydelige zoner og gameplay-adjacency.",
            Land = C(0.36f,0.43f,0.27f), Sea = C(0.08f,0.19f,0.29f), Coast = C(0.76f,0.72f,0.55f),
            Zone = C(0.96f,0.73f,0.17f), City = C(0.96f,0.91f,0.75f), Army = C(0.88f,0.13f,0.10f),
            Ambient = C(0.57f,0.59f,0.55f), Sun = C(1.00f,0.97f,0.88f), Fog = C(0.30f,0.35f,0.37f),
            SunIntensity = 0.92f, CoastWidth = 0.12f, Relief = 0.08f, Tilt = 4f, OrthoSize = 43f,
            ZoneScale = 1.25f, CityScale = 1.15f, FogEnabled = false, Adjacency = true
        });
    }

    private void CaptureScene()
    {
        campaignCamera = Camera.main;

        Light[] lights = Object.FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                campaignSun = lights[i];
                break;
            }
        }

        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;
            if (!filter.gameObject.name.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                continue;

            landMeshes.Add(new MeshSnapshot {
                Filter = filter,
                OriginalVertices = filter.sharedMesh.vertices
            });
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            string n = r.gameObject.name;
            if (n.StartsWith("ZONE_", StringComparison.Ordinal))
                markerScales.Add(new ScaleSnapshot { Transform = r.transform, OriginalScale = r.transform.localScale, Kind = "ZONE" });
            else if (n.StartsWith("CITY_", StringComparison.Ordinal))
                markerScales.Add(new ScaleSnapshot { Transform = r.transform, OriginalScale = r.transform.localScale, Kind = "CITY" });
        }
    }

    private void BuildOverlayLayers()
    {
        gridRoot = new GameObject("MAPLAB_Grid");
        gridRoot.transform.SetParent(transform, false);
        tileGridRoot = new GameObject("MAPLAB_TileGrid");
        tileGridRoot.transform.SetParent(transform, false);
        adjacencyRoot = new GameObject("MAPLAB_Adjacency");
        adjacencyRoot.transform.SetParent(transform, false);

        Material gridMaterial = CreateRuntimeMaterial(new Color(1f, 1f, 1f, 0.28f), "MAPLAB_GridMat");
        Material tileMaterial = CreateRuntimeMaterial(new Color(0.15f, 0.18f, 0.16f, 0.30f), "MAPLAB_TileMat");
        Material adjacencyMaterial = CreateRuntimeMaterial(new Color(0.96f, 0.78f, 0.30f, 0.82f), "MAPLAB_AdjacencyMat");

        for (int x = -30; x <= 55; x += 5)
            CreateLine(gridRoot.transform, "GridX_" + x, new Vector3(x, 0.70f, -35f), new Vector3(x, 0.70f, 35f), 0.035f, gridMaterial);
        for (int z = -35; z <= 35; z += 5)
            CreateLine(gridRoot.transform, "GridZ_" + z, new Vector3(-30f, 0.70f, z), new Vector3(55f, 0.70f, z), 0.035f, gridMaterial);

        for (int x = -30; x <= 60; x += 15)
            CreateLine(tileGridRoot.transform, "TileX_" + x, new Vector3(x, 0.72f, -40f), new Vector3(x, 0.72f, 40f), 0.075f, tileMaterial);
        for (int z = -40; z <= 40; z += 15)
            CreateLine(tileGridRoot.transform, "TileZ_" + z, new Vector3(-30f, 0.72f, z), new Vector3(60f, 0.72f, z), 0.075f, tileMaterial);

        string[,] links = {
            {"DK-VEN","DK-NJ"},{"DK-NJ","DK-MJ"},{"DK-MJ","DK-VJ"},{"DK-MJ","DK-OJ"},
            {"DK-VJ","DK-SJ"},{"DK-OJ","DK-SJ"},{"DK-SJ","DK-FYN"},{"DK-FYN","DK-NSJ"},
            {"DK-FYN","DK-SSJ"},{"DK-NSJ","DK-KBH"},{"DK-NSJ","DK-SSJ"},{"DK-SSJ","DK-LF"},
            {"DK-KBH","DK-SSJ"}
        };

        for (int i = 0; i < links.GetLength(0); i++)
        {
            GameObject a = GameObject.Find("ZONE_" + links[i,0]);
            GameObject b = GameObject.Find("ZONE_" + links[i,1]);
            if (a == null || b == null)
                continue;

            Vector3 from = a.transform.position + Vector3.up * 0.45f;
            Vector3 to = b.transform.position + Vector3.up * 0.45f;
            CreateLine(adjacencyRoot.transform, "Adj_" + links[i,0] + "_" + links[i,1], from, to, 0.12f, adjacencyMaterial);
        }

        gridRoot.SetActive(false);
        tileGridRoot.SetActive(false);
        adjacencyRoot.SetActive(false);
    }

    private static void CreateLine(Transform parent, string name, Vector3 from, Vector3 to, float width, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    private void Update()
    {
        if (!ready || profiles.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.RightBracket))
            ApplyProfile((activeProfile + 1) % profiles.Count);
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            ApplyProfile((activeProfile - 1 + profiles.Count) % profiles.Count);

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift)
            return;

        KeyCode[] keys = {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11
        };

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                ApplyProfile(i);
                break;
            }
        }
    }

    private void ApplyProfile(int index)
    {
        if (index < 0 || index >= profiles.Count)
            return;

        activeProfile = index;
        MapProfile p = profiles[index];

        ApplyCamera(p);
        ApplyLighting(p);
        ApplyRelief(p.Relief);
        ApplyRenderColors(p);
        ApplyMarkerScale(p);

        if (gridRoot != null) gridRoot.SetActive(p.Grid);
        if (tileGridRoot != null) tileGridRoot.SetActive(p.TileGrid);
        if (adjacencyRoot != null) adjacencyRoot.SetActive(p.Adjacency);

        Debug.Log(string.Format(
            "CAMPAIGN-MAPLAB11|Profile={0:00}|Name={1}|Relief={2:0.00}|Tilt={3:0}|Grid={4}|TileGrid={5}|Adjacency={6}",
            index + 1, p.Name, p.Relief, p.Tilt, p.Grid, p.TileGrid, p.Adjacency));
    }

    private void ApplyCamera(MapProfile p)
    {
        if (campaignCamera == null)
            return;

        campaignCamera.orthographic = true;
        campaignCamera.orthographicSize = p.OrthoSize;
        campaignCamera.backgroundColor = p.Sea * 0.72f;

        Vector3 target = new Vector3(10f, 0f, 0f);
        float radians = p.Tilt * Mathf.Deg2Rad;
        float distance = 95f;
        float zOffset = Mathf.Sin(radians) * distance * 0.62f;
        float y = Mathf.Cos(radians) * distance;
        campaignCamera.transform.position = new Vector3(target.x, Mathf.Max(35f, y), target.z - zOffset);
        campaignCamera.transform.LookAt(target, Vector3.forward);
    }

    private void ApplyLighting(MapProfile p)
    {
        RenderSettings.ambientLight = p.Ambient;
        RenderSettings.fog = p.FogEnabled;
        RenderSettings.fogColor = p.Fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = p.FogDensity;

        if (campaignSun != null)
        {
            campaignSun.color = p.Sun;
            campaignSun.intensity = p.SunIntensity;
            campaignSun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        }
    }

    private void ApplyRelief(float amount)
    {
        for (int i = 0; i < landMeshes.Count; i++)
        {
            MeshSnapshot snap = landMeshes[i];
            if (snap.Filter == null || snap.Filter.sharedMesh == null || snap.OriginalVertices == null)
                continue;

            Vector3[] vertices = new Vector3[snap.OriginalVertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 original = snap.OriginalVertices[v];
                float n1 = Mathf.PerlinNoise((original.x + 42f) * 0.095f, (original.z + 37f) * 0.095f);
                float n2 = Mathf.PerlinNoise((original.x - 13f) * 0.23f, (original.z + 11f) * 0.23f);
                float relief = Mathf.Max(0f, (n1 * 0.75f + n2 * 0.25f) - 0.36f);
                original.y += relief * amount;
                vertices[v] = original;
            }

            snap.Filter.sharedMesh.vertices = vertices;
            snap.Filter.sharedMesh.RecalculateNormals();
            snap.Filter.sharedMesh.RecalculateBounds();
        }
    }

    private void ApplyRenderColors(MapProfile p)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.sharedMaterial == null)
                continue;

            string n = r.gameObject.name;
            if (n.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                SetMaterialColor(r.sharedMaterial, p.Land);
            else if (n == "Grand Campaign Sea")
                SetMaterialColor(r.sharedMaterial, p.Sea);
            else if (n.StartsWith("ZONE_", StringComparison.Ordinal))
                SetMaterialColor(r.sharedMaterial, p.Zone);
            else if (n.StartsWith("CITY_", StringComparison.Ordinal))
                SetMaterialColor(r.sharedMaterial, p.City);
            else if (n.StartsWith("ARMY_", StringComparison.Ordinal))
                SetMaterialColor(r.sharedMaterial, p.Army);
        }

        GameObject geography = GameObject.Find("GEO_Denmark_NaturalEarth50m");
        if (geography != null)
        {
            LineRenderer[] lines = geography.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null || lines[i].sharedMaterial == null)
                    continue;
                SetMaterialColor(lines[i].sharedMaterial, p.Coast);
                lines[i].widthMultiplier = p.CoastWidth;
            }
        }
    }

    private void ApplyMarkerScale(MapProfile p)
    {
        for (int i = 0; i < markerScales.Count; i++)
        {
            ScaleSnapshot snap = markerScales[i];
            if (snap.Transform == null)
                continue;

            float factor = snap.Kind == "ZONE" ? p.ZoneScale : p.CityScale;
            Vector3 s = snap.OriginalScale;
            snap.Transform.localScale = new Vector3(s.x * factor, s.y, s.z * factor);
        }
    }

    private void OnGUI()
    {
        if (!ready || profiles.Count == 0)
            return;

        MapProfile p = profiles[activeProfile];

        GUIStyle box = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            wordWrap = true
        };
        box.normal.textColor = Color.white;

        GUIStyle title = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        title.normal.textColor = Color.white;

        float infoWidth = Mathf.Min(500f, Screen.width - 16f);
        float infoX = Screen.width - infoWidth - 8f;
        GUI.Box(new Rect(infoX, 44f, infoWidth, 28f),
            "PROJECT 1864 | v00.00.10f MAP LAB 11 | " + (activeProfile + 1) + "/11", title);
        GUI.Box(new Rect(infoX, 74f, infoWidth, 66f),
            p.Name + "\n" + p.Description +
            "\n1-10 er visuelle previews; eksterne DEM/GIS/raster-data er ikke integreret endnu.", box);

        int columns = Screen.width >= 1700 ? 11 : 6;
        int rows = Mathf.CeilToInt(profiles.Count / (float)columns);
        float gap = 3f;
        float margin = 8f;
        float buttonHeight = 26f;
        float usable = Screen.width - margin * 2f - gap * (columns - 1);
        float buttonWidth = usable / columns;
        float startY = Screen.height - margin - rows * buttonHeight - (rows - 1) * gap;

        for (int i = 0; i < profiles.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            Rect r = new Rect(
                margin + col * (buttonWidth + gap),
                startY + row * (buttonHeight + gap),
                buttonWidth,
                buttonHeight);

            GUI.enabled = i != activeProfile;
            if (GUI.Button(r, profiles[i].ShortName))
                ApplyProfile(i);
            GUI.enabled = true;
        }

        GUI.Label(new Rect(10f, startY - 22f, Mathf.Min(560f, Screen.width - 20f), 20f),
            "M / ] = næste | [ = forrige | Shift+F1...F11 = direkte valg");
    }

    private static Material CreateRuntimeMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        SetMaterialColor(material, color);
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static Color C(float r, float g, float b)
    {
        return new Color(r, g, b, 1f);
    }
}
