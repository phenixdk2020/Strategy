using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 - Campaign Map Visual Lab v00.00.10f
///
/// Eleven switchable presentation candidates over the SAME campaign world.
/// Geography, cities, zones, armies and simulation state remain unchanged.
/// This file is intentionally isolated from GrandCampaignBootstrap so a rejected
/// visual direction can be removed without touching campaign-state architecture.
///
/// Controls:
/// - Click one of the 11 buttons in the bottom comparison bar.
/// - M or ] = next candidate.
/// - [ = previous candidate.
/// - Shift+F1 ... Shift+F11 = direct candidate selection.
///
/// IMPORTANT:
/// These are art-direction/runtime comparison profiles. Candidates whose final
/// design requires DEM, Cesium, ArcGIS, MapTiler, historical raster maps or real
/// administrative polygons are explicitly marked PREVIEW until those sourced
/// data layers/packages are integrated.
/// </summary>
[DefaultExecutionOrder(30000)]
public sealed class CampaignMapStyleSwitcher : MonoBehaviour
{
    public enum MapCandidate
    {
        DanishDemUnityTerrain = 0,
        CesiumWorldTerrain = 1,
        ArcGisHistoricalLayers = 2,
        MapTiler3DTerrain = 3,
        HistoricalMapOnRelief = 4,
        QgisBlenderBaked = 5,
        ProceduralLivingWorld = 6,
        OpenStreetMapFoundation = 7,
        HybridSatellite1851 = 8,
        HandmadeModelRailway = 9,
        StrategicHoi4 = 10
    }

    private sealed class StyleProfile
    {
        public string ShortName;
        public string Title;
        public string Subtitle;
        public string Status;
        public Color Sea;
        public Color Land;
        public Color Coast;
        public Color Zone;
        public Color City;
        public Color Army;
        public Color Background;
        public Color Ambient;
        public Color SunColor;
        public float SunIntensity;
        public Vector3 SunEuler;
        public Vector3 CameraPosition;
        public Vector3 CameraEuler;
        public float CameraSize;
        public float CoastWidth;
        public float ZoneScale;
        public float CityScale;
        public float LandGloss;
        public float SeaGloss;
        public bool Fog;
        public Color FogColor;
        public float FogDensity;
    }

    public static CampaignMapStyleSwitcher Instance { get; private set; }

    private readonly List<MeshRenderer> landRenderers = new List<MeshRenderer>();
    private readonly List<LineRenderer> coastRenderers = new List<LineRenderer>();
    private readonly List<Renderer> zoneRenderers = new List<Renderer>();
    private readonly List<Renderer> cityRenderers = new List<Renderer>();
    private readonly List<Renderer> armyRenderers = new List<Renderer>();
    private readonly Dictionary<Transform, Vector3> originalZoneScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> originalCityScales = new Dictionary<Transform, Vector3>();

    private Camera campaignCamera;
    private Renderer seaRenderer;
    private Light campaignSun;
    private StyleProfile[] profiles;
    private bool bound;
    private MapCandidate candidate = MapCandidate.DanishDemUnityTerrain;

    private GUIStyle titleStyle;
    private GUIStyle infoStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeButtonStyle;
    private GUIStyle smallStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignMapStyleSwitcher>() != null)
            return;

        GameObject root = new GameObject("PROJECT1864_CampaignMapStyleLab_v000010f");
        DontDestroyOnLoad(root);
        root.AddComponent<CampaignMapStyleSwitcher>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildProfiles();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!bound)
        {
            TryBindCampaignMap();
            return;
        }

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.RightBracket))
            SelectRelative(1);
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            SelectRelative(-1);

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift)
            return;

        if (Input.GetKeyDown(KeyCode.F1)) ApplyCandidate(0);
        else if (Input.GetKeyDown(KeyCode.F2)) ApplyCandidate(1);
        else if (Input.GetKeyDown(KeyCode.F3)) ApplyCandidate(2);
        else if (Input.GetKeyDown(KeyCode.F4)) ApplyCandidate(3);
        else if (Input.GetKeyDown(KeyCode.F5)) ApplyCandidate(4);
        else if (Input.GetKeyDown(KeyCode.F6)) ApplyCandidate(5);
        else if (Input.GetKeyDown(KeyCode.F7)) ApplyCandidate(6);
        else if (Input.GetKeyDown(KeyCode.F8)) ApplyCandidate(7);
        else if (Input.GetKeyDown(KeyCode.F9)) ApplyCandidate(8);
        else if (Input.GetKeyDown(KeyCode.F10)) ApplyCandidate(9);
        else if (Input.GetKeyDown(KeyCode.F11)) ApplyCandidate(10);
    }

    private void BuildProfiles()
    {
        profiles = new[]
        {
            new StyleProfile
            {
                ShortName = "1 DEM 3D",
                Title = "1. Dansk DEM -> Unity Terrain",
                Subtitle = "Naturtro dansk landskab med tydelig relief-retning",
                Status = "PREVIEW - rigtig dansk DEM/elevation er ikke indlæst endnu",
                Sea = C(25, 76, 101), Land = C(83, 121, 61), Coast = C(209, 202, 163),
                Zone = C(158, 134, 54), City = C(226, 210, 161), Army = C(158, 31, 31),
                Background = C(35, 57, 70), Ambient = C(121, 134, 119), SunColor = C(255, 235, 197),
                SunIntensity = 1.25f, SunEuler = new Vector3(47f, -32f, 0f),
                CameraPosition = new Vector3(11f, 77f, -27f), CameraEuler = new Vector3(64f, 0f, 0f),
                CameraSize = 43f, CoastWidth = 0.10f, ZoneScale = 0.78f, CityScale = 1.35f,
                LandGloss = 0.10f, SeaGloss = 0.48f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "2 CESIUM",
                Title = "2. Cesium World Terrain",
                Subtitle = "Global real-world/aerial retning med køligere atmosfære",
                Status = "PREVIEW - Cesium package/streamed terrain er ikke installeret endnu",
                Sea = C(31, 83, 111), Land = C(92, 111, 74), Coast = C(188, 193, 174),
                Zone = C(205, 163, 66), City = C(236, 226, 198), Army = C(171, 45, 38),
                Background = C(44, 65, 79), Ambient = C(128, 139, 139), SunColor = C(238, 244, 247),
                SunIntensity = 1.10f, SunEuler = new Vector3(53f, -20f, 0f),
                CameraPosition = new Vector3(11f, 86f, -18f), CameraEuler = new Vector3(72f, 0f, 0f),
                CameraSize = 47f, CoastWidth = 0.065f, ZoneScale = 0.66f, CityScale = 1.10f,
                LandGloss = 0.06f, SeaGloss = 0.62f, Fog = true, FogColor = C(111, 130, 139), FogDensity = 0.004f
            },
            new StyleProfile
            {
                ShortName = "3 ARCGIS",
                Title = "3. ArcGIS Maps SDK",
                Subtitle = "Professionel GIS-læsbarhed med historiske datalag ovenpå",
                Status = "PREVIEW - ArcGIS SDK og historiske GIS-lag er ikke integreret endnu",
                Sea = C(111, 153, 170), Land = C(171, 177, 126), Coast = C(76, 89, 70),
                Zone = C(187, 113, 50), City = C(94, 55, 35), Army = C(151, 28, 32),
                Background = C(181, 194, 192), Ambient = C(184, 187, 166), SunColor = C(255, 250, 231),
                SunIntensity = 0.95f, SunEuler = new Vector3(61f, -16f, 0f),
                CameraPosition = new Vector3(11f, 94f, -7f), CameraEuler = new Vector3(83f, 0f, 0f),
                CameraSize = 44f, CoastWidth = 0.050f, ZoneScale = 0.72f, CityScale = 0.95f,
                LandGloss = 0.02f, SeaGloss = 0.22f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "4 MAPTILER",
                Title = "4. MapTiler 3D Terrain",
                Subtitle = "Klar web-map æstetik med stærke grøn/blå terrænfarver",
                Status = "PREVIEW - MapTiler terrain/tiles er ikke koblet på endnu",
                Sea = C(48, 109, 137), Land = C(102, 143, 78), Coast = C(221, 218, 188),
                Zone = C(213, 154, 51), City = C(245, 232, 198), Army = C(174, 37, 30),
                Background = C(51, 87, 105), Ambient = C(151, 160, 134), SunColor = C(255, 242, 210),
                SunIntensity = 1.15f, SunEuler = new Vector3(50f, -36f, 0f),
                CameraPosition = new Vector3(11f, 81f, -22f), CameraEuler = new Vector3(68f, 0f, 0f),
                CameraSize = 44f, CoastWidth = 0.085f, ZoneScale = 0.74f, CityScale = 1.18f,
                LandGloss = 0.05f, SeaGloss = 0.52f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "5 HISTORISK",
                Title = "5. Historisk kort på 3D-relief",
                Subtitle = "1850'er stabskort/papir-look draperet over geografien",
                Status = "PREVIEW - source-backed ca. 1851 rasterkort mangler endnu",
                Sea = C(136, 143, 124), Land = C(177, 158, 109), Coast = C(62, 49, 31),
                Zone = C(102, 73, 38), City = C(58, 37, 22), Army = C(120, 36, 30),
                Background = C(113, 99, 68), Ambient = C(184, 165, 119), SunColor = C(255, 218, 157),
                SunIntensity = 0.82f, SunEuler = new Vector3(59f, -18f, 0f),
                CameraPosition = new Vector3(11f, 93f, -6f), CameraEuler = new Vector3(84f, 0f, 0f),
                CameraSize = 44f, CoastWidth = 0.050f, ZoneScale = 0.62f, CityScale = 0.90f,
                LandGloss = 0.00f, SeaGloss = 0.02f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "6 QGIS/BLENDER",
                Title = "6. QGIS / Blender -> Unity baked map",
                Subtitle = "Kontrolleret, malet high-end terrain med filmisk lys",
                Status = "PREVIEW - baked terrain/textures skal produceres eksternt senere",
                Sea = C(19, 62, 77), Land = C(78, 110, 55), Coast = C(219, 205, 153),
                Zone = C(191, 139, 42), City = C(229, 199, 139), Army = C(153, 25, 26),
                Background = C(27, 48, 57), Ambient = C(92, 103, 83), SunColor = C(255, 214, 158),
                SunIntensity = 1.42f, SunEuler = new Vector3(39f, -44f, 0f),
                CameraPosition = new Vector3(11f, 71f, -32f), CameraEuler = new Vector3(59f, 0f, 0f),
                CameraSize = 42f, CoastWidth = 0.085f, ZoneScale = 0.70f, CityScale = 1.30f,
                LandGloss = 0.08f, SeaGloss = 0.68f, Fog = true, FogColor = C(73, 91, 88), FogDensity = 0.003f
            },
            new StyleProfile
            {
                ShortName = "7 PROCEDURAL",
                Title = "7. Procedural 3D living world",
                Subtitle = "Spilbar levende verden med tydelig vegetation/by/landbrugs-retning",
                Status = "PREVIEW - procedural forests/fields/towns er endnu ikke genereret",
                Sea = C(39, 101, 120), Land = C(91, 142, 67), Coast = C(228, 215, 162),
                Zone = C(218, 168, 56), City = C(242, 205, 139), Army = C(170, 34, 30),
                Background = C(61, 91, 96), Ambient = C(147, 155, 121), SunColor = C(255, 229, 177),
                SunIntensity = 1.30f, SunEuler = new Vector3(45f, -30f, 0f),
                CameraPosition = new Vector3(11f, 73f, -29f), CameraEuler = new Vector3(61f, 0f, 0f),
                CameraSize = 42f, CoastWidth = 0.10f, ZoneScale = 0.68f, CityScale = 1.55f,
                LandGloss = 0.04f, SeaGloss = 0.45f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "8 OSM",
                Title = "8. OpenStreetMap foundation",
                Subtitle = "Rent, lyst og informationsorienteret kort til data-overlays",
                Status = "PREVIEW - OSM geometri bruges ikke som 1851 historisk sandhed",
                Sea = C(173, 211, 224), Land = C(216, 217, 194), Coast = C(105, 122, 111),
                Zone = C(216, 144, 56), City = C(85, 74, 67), Army = C(186, 54, 46),
                Background = C(212, 224, 226), Ambient = C(209, 211, 199), SunColor = C(255, 255, 250),
                SunIntensity = 0.72f, SunEuler = new Vector3(72f, -8f, 0f),
                CameraPosition = new Vector3(11f, 96f, 0f), CameraEuler = new Vector3(90f, 0f, 0f),
                CameraSize = 43f, CoastWidth = 0.045f, ZoneScale = 0.60f, CityScale = 0.84f,
                LandGloss = 0.00f, SeaGloss = 0.05f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "9 SATELLITE",
                Title = "9. Hybrid satellite-look + painted 1851",
                Subtitle = "Fotorealistisk mørkere jordlook med atmosfærisk kontrast",
                Status = "PREVIEW - satellite/painted 1851 texture layer er ikke produceret endnu",
                Sea = C(11, 42, 57), Land = C(67, 79, 48), Coast = C(166, 164, 129),
                Zone = C(177, 132, 41), City = C(218, 199, 158), Army = C(176, 43, 35),
                Background = C(18, 35, 43), Ambient = C(76, 83, 70), SunColor = C(223, 232, 225),
                SunIntensity = 1.05f, SunEuler = new Vector3(44f, -27f, 0f),
                CameraPosition = new Vector3(11f, 80f, -24f), CameraEuler = new Vector3(67f, 0f, 0f),
                CameraSize = 45f, CoastWidth = 0.055f, ZoneScale = 0.64f, CityScale = 1.02f,
                LandGloss = 0.02f, SeaGloss = 0.74f, Fog = true, FogColor = C(69, 82, 83), FogDensity = 0.005f
            },
            new StyleProfile
            {
                ShortName = "10 MINIATURE",
                Title = "10. Håndbygget modeljernbane / diorama",
                Subtitle = "Charmerende miniatureverden med overtydelige fysiske objekter",
                Status = "PREVIEW - 3D byer, gårde, kirker, tog og vegetation kræver assets",
                Sea = C(51, 123, 139), Land = C(111, 151, 73), Coast = C(238, 218, 160),
                Zone = C(224, 170, 54), City = C(245, 192, 119), Army = C(174, 31, 34),
                Background = C(77, 104, 104), Ambient = C(171, 161, 117), SunColor = C(255, 221, 161),
                SunIntensity = 1.38f, SunEuler = new Vector3(41f, -41f, 0f),
                CameraPosition = new Vector3(11f, 65f, -37f), CameraEuler = new Vector3(55f, 0f, 0f),
                CameraSize = 40f, CoastWidth = 0.12f, ZoneScale = 0.88f, CityScale = 1.85f,
                LandGloss = 0.16f, SeaGloss = 0.50f, Fog = false
            },
            new StyleProfile
            {
                ShortName = "11 HOI4+",
                Title = "11. Strategisk HOI4-lignende 2D / 2.5D",
                Subtitle = "Maksimal strategisk læsbarhed, høj kontrast og tydelige tokens",
                Status = "PREVIEW - historiske region/provinspolygoner er ikke kildebygget endnu",
                Sea = C(35, 60, 81), Land = C(112, 119, 86), Coast = C(195, 194, 161),
                Zone = C(202, 145, 53), City = C(233, 221, 180), Army = C(188, 38, 40),
                Background = C(42, 56, 69), Ambient = C(133, 137, 119), SunColor = C(235, 237, 225),
                SunIntensity = 0.88f, SunEuler = new Vector3(69f, -10f, 0f),
                CameraPosition = new Vector3(11f, 96f, 0f), CameraEuler = new Vector3(90f, 0f, 0f),
                CameraSize = 43f, CoastWidth = 0.095f, ZoneScale = 1.18f, CityScale = 1.08f,
                LandGloss = 0.00f, SeaGloss = 0.24f, Fog = false
            }
        };
    }

    private void TryBindCampaignMap()
    {
        GameObject cameraObject = GameObject.Find("Grand Campaign Camera");
        GameObject seaObject = GameObject.Find("Grand Campaign Sea");
        GameObject geoRoot = GameObject.Find("GEO_Denmark_NaturalEarth50m");

        if (cameraObject == null || seaObject == null || geoRoot == null)
            return;

        campaignCamera = cameraObject.GetComponent<Camera>();
        seaRenderer = seaObject.GetComponent<Renderer>();

        GameObject sunObject = GameObject.Find("Campaign Sun");
        campaignSun = sunObject != null ? sunObject.GetComponent<Light>() : null;

        landRenderers.Clear();
        coastRenderers.Clear();
        zoneRenderers.Clear();
        cityRenderers.Clear();
        armyRenderers.Clear();
        originalZoneScales.Clear();
        originalCityScales.Clear();

        MeshRenderer[] lands = geoRoot.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < lands.Length; i++)
        {
            if (lands[i].gameObject.name.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                landRenderers.Add(lands[i]);
        }

        LineRenderer[] coasts = geoRoot.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < coasts.Length; i++)
        {
            if (coasts[i].gameObject.name.StartsWith("DNK_Coast_", StringComparison.Ordinal))
                coastRenderers.Add(coasts[i]);
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string objectName = renderer.gameObject.name;

            if (objectName.StartsWith("ZONE_", StringComparison.Ordinal))
            {
                zoneRenderers.Add(renderer);
                originalZoneScales[renderer.transform] = renderer.transform.localScale;
            }
            else if (objectName.StartsWith("CITY_", StringComparison.Ordinal))
            {
                cityRenderers.Add(renderer);
                originalCityScales[renderer.transform] = renderer.transform.localScale;
            }
            else if (objectName.StartsWith("ARMY_", StringComparison.Ordinal))
            {
                armyRenderers.Add(renderer);
            }
        }

        bound = campaignCamera != null && seaRenderer != null && landRenderers.Count > 0;
        if (!bound)
            return;

        ApplyCandidate((int)candidate);
        Debug.Log(
            "CAMPAIGN-MAPSTYLE|Installed=True|Version=v00.00.10f|Candidates=11" +
            "|SharedWorld=True|SharedSimulation=True|BaseGeo=NaturalEarth50m" +
            "|RealDEM=False|ExternalSDK=False|HistoricalRaster=False|HistoricalPolygons=False");
    }

    private void SelectRelative(int delta)
    {
        int count = profiles.Length;
        int next = ((int)candidate + delta + count) % count;
        ApplyCandidate(next);
    }

    private void ApplyCandidate(int index)
    {
        if (!bound || profiles == null || profiles.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, profiles.Length - 1);
        candidate = (MapCandidate)index;
        StyleProfile p = profiles[index];

        campaignCamera.orthographic = true;
        campaignCamera.transform.position = p.CameraPosition;
        campaignCamera.transform.rotation = Quaternion.Euler(p.CameraEuler);
        campaignCamera.orthographicSize = p.CameraSize;
        campaignCamera.backgroundColor = p.Background;
        campaignCamera.clearFlags = CameraClearFlags.SolidColor;

        SetRendererColor(seaRenderer, p.Sea);
        TuneMaterial(seaRenderer != null ? seaRenderer.sharedMaterial : null, p.SeaGloss, 0f);

        for (int i = 0; i < landRenderers.Count; i++)
        {
            SetRendererColor(landRenderers[i], p.Land);
            TuneMaterial(landRenderers[i].sharedMaterial, p.LandGloss, 0f);
        }

        for (int i = 0; i < coastRenderers.Count; i++)
        {
            coastRenderers[i].widthMultiplier = p.CoastWidth;
            SetRendererColor(coastRenderers[i], p.Coast);
        }

        SetGroupColor(zoneRenderers, p.Zone);
        SetGroupColor(cityRenderers, p.City);
        SetGroupColor(armyRenderers, p.Army);
        ApplyStoredScale(originalZoneScales, p.ZoneScale);
        ApplyStoredScale(originalCityScales, p.CityScale);

        RenderSettings.ambientLight = p.Ambient;
        RenderSettings.fog = p.Fog;
        RenderSettings.fogColor = p.FogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = p.FogDensity;

        if (campaignSun != null)
        {
            campaignSun.color = p.SunColor;
            campaignSun.intensity = p.SunIntensity;
            campaignSun.transform.rotation = Quaternion.Euler(p.SunEuler);
        }

        Debug.Log(
            "CAMPAIGN-MAPSTYLE|Candidate=" + (index + 1) +
            "|Name=" + p.ShortName.Replace("|", "/") +
            "|Status=Preview|SimulationStateChanged=False");
    }

    private static void ApplyStoredScale(Dictionary<Transform, Vector3> originals, float multiplier)
    {
        foreach (KeyValuePair<Transform, Vector3> pair in originals)
        {
            if (pair.Key == null)
                continue;

            Vector3 original = pair.Value;
            pair.Key.localScale = new Vector3(original.x * multiplier, original.y, original.z * multiplier);
        }
    }

    private static void SetGroupColor(List<Renderer> renderers, Color color)
    {
        for (int i = 0; i < renderers.Count; i++)
            SetRendererColor(renderers[i], color);
    }

    private static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.sharedMaterial == null)
            return;

        renderer.sharedMaterial.color = color;
    }

    private static void TuneMaterial(Material material, float glossiness, float metallic)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", glossiness);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", glossiness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
    }

    private static Color C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void EnsureGuiStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 4, 4)
        };
        titleStyle.normal.textColor = Color.white;

        infoStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            padding = new RectOffset(8, 8, 6, 6)
        };
        infoStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        smallStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        activeButtonStyle = new GUIStyle(buttonStyle)
        {
            fontStyle = FontStyle.Bold
        };
        activeButtonStyle.normal.textColor = new Color(1f, 0.86f, 0.35f);
        activeButtonStyle.hover.textColor = new Color(1f, 0.92f, 0.55f);
    }

    private void OnGUI()
    {
        if (!bound || profiles == null)
            return;

        EnsureGuiStyles();
        GUI.depth = -900;

        StyleProfile p = profiles[(int)candidate];
        float panelWidth = Mathf.Min(470f, Screen.width - 24f);
        Rect panel = new Rect(Screen.width - panelWidth - 8f, 44f, panelWidth, 94f);
        GUI.Box(new Rect(panel.x, panel.y, panel.width, 28f), p.Title, titleStyle);
        GUI.Box(
            new Rect(panel.x, panel.y + 30f, panel.width, 64f),
            p.Subtitle + "\n" + p.Status + "\nM / [ ] = skift | Shift+F1..F11 = direkte valg",
            infoStyle);

        DrawCandidateButtons();
    }

    private void DrawCandidateButtons()
    {
        const float margin = 8f;
        const float gap = 3f;
        const int columns = 6;
        const int rows = 2;
        float usableWidth = Screen.width - margin * 2f;
        float buttonWidth = Mathf.Min(150f, (usableWidth - gap * (columns - 1)) / columns);
        float totalWidth = buttonWidth * columns + gap * (columns - 1);
        float startX = Mathf.Max(margin, (Screen.width - totalWidth) * 0.5f);
        float rowHeight = 34f;
        float startY = Screen.height - (rowHeight * rows + gap + 8f);

        for (int i = 0; i < profiles.Length; i++)
        {
            int row = i / columns;
            int col = i % columns;
            Rect rect = new Rect(
                startX + col * (buttonWidth + gap),
                startY + row * (rowHeight + gap),
                buttonWidth,
                rowHeight);

            GUIStyle style = i == (int)candidate ? activeButtonStyle : buttonStyle;
            if (GUI.Button(rect, profiles[i].ShortName, style))
                ApplyCandidate(i);
        }

        GUI.Label(
            new Rect(startX, startY - 18f, totalWidth, 16f),
            "CAMPAIGN MAP VISUAL LAB v00.00.10f — 11 kandidater / samme verden og simulation",
            smallStyle);
    }
}
