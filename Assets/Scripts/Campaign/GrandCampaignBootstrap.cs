using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-30000)]
public sealed class GrandCampaignBootstrap : MonoBehaviour
{
    public enum CampaignNation
    {
        Denmark,
        SwedenNorway,
        Prussia,
        Austria,
        France,
        UnitedKingdom,
        Russia,
        Netherlands,
        Hanover,
        Mecklenburg,
        GermanConfederationOther
    }

    private enum MapZoomBand
    {
        Close,
        Operational,
        Strategic
    }

    private sealed class Zone
    {
        public string Id;
        public string Name;
        public CampaignNation Owner;
        public float Longitude;
        public float Latitude;
        public Vector2 Position;
        public readonly List<string> Neighbours = new List<string>();
        public GameObject Visual;
    }

    private sealed class Army
    {
        public string Id;
        public string Name;
        public CampaignNation Nation;
        public string CurrentZoneId;
        public string DestinationZoneId;
        public float Progress;
        public int Strength;
        public GameObject Visual;
    }

    private sealed class City
    {
        public string Name;
        public float Longitude;
        public float Latitude;
        public GameObject Visual;
    }

    public const bool CampaignModeEnabled = true;
    public static GrandCampaignBootstrap Instance { get; private set; }

    private readonly Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
    private readonly Dictionary<string, Army> armies = new Dictionary<string, Army>();
    private readonly List<CampaignNation> playableNations = new List<CampaignNation>();
    private readonly List<City> cities = new List<City>();

    private Camera campaignCamera;
    private CampaignNation playerNation = CampaignNation.Denmark;
    private bool nationChosen;
    private Zone selectedZone;
    private Army selectedArmy;

    // Grand Campaign canonical start chosen for PROJECT 1864.
    private bool paused;
    private float campaignSpeed = 1f;
    private DateTime campaignTime = new DateTime(1851, 1, 1, 8, 0, 0);

    private MapZoomBand zoomBand = MapZoomBand.Operational;

    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle zoneStyle;
    private GUIStyle cityStyle;
    private GUIStyle mapInfoStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<GrandCampaignBootstrap>() != null)
            return;

        GameObject root = new GameObject("PROJECT1864_GrandCampaign_v000010e");
        DontDestroyOnLoad(root);
        root.AddComponent<GrandCampaignBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPlayableNationList();
        BuildCampaignWorld();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildPlayableNationList()
    {
        // v00.00.10e deliberately activates Denmark only. The enum/data contract
        // keeps the already-decided future nations, but we do not expose empty
        // countries until the Denmark vertical slice is proven.
        playableNations.Add(CampaignNation.Denmark);
    }

    private void BuildCampaignWorld()
    {
        Application.targetFrameRate = 120;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.60f, 0.62f, 0.64f);

        CreateCamera();
        CreateLight();
        CreateSeaBoard();
        CreateRealDenmarkGeography();
        CreateZones();
        CreateCities();
        CreateArmies();

        Debug.Log(string.Format(
            "CAMPAIGN-10E|Installed=True|Start=1851-01-01|Geo=WGS84|Source=NaturalEarth50m|Focus=Denmark|Zones={0}|Cities={1}|Armies={2}|DEM=False",
            zones.Count,
            cities.Count,
            armies.Count));
    }

    private void CreateCamera()
    {
        Camera existing = Camera.main;
        if (existing != null)
            existing.gameObject.SetActive(false);

        GameObject cameraObject = new GameObject("Grand Campaign Camera");
        campaignCamera = cameraObject.AddComponent<Camera>();
        campaignCamera.tag = "MainCamera";
        campaignCamera.orthographic = true;
        campaignCamera.orthographicSize = 43f;
        campaignCamera.nearClipPlane = 0.1f;
        campaignCamera.farClipPlane = 300f;
        campaignCamera.backgroundColor = new Color(0.16f, 0.29f, 0.40f);
        campaignCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(11f, 95f, 1f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void CreateLight()
    {
        GameObject lightObject = new GameObject("Campaign Sun");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        lightObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
    }

    private void CreateSeaBoard()
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "Grand Campaign Sea";
        sea.transform.position = new Vector3(10f, -0.45f, 0f);
        sea.transform.localScale = new Vector3(170f, 0.8f, 105f);
        sea.GetComponent<Renderer>().sharedMaterial =
            CreateMaterial(new Color(0.18f, 0.34f, 0.47f), "CampaignSea");
        Collider collider = sea.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void CreateRealDenmarkGeography()
    {
        Material land = CreateMaterial(new Color(0.38f, 0.47f, 0.27f), "DNK_Land_NaturalEarth50m");
        Material coast = CreateMaterial(new Color(0.84f, 0.82f, 0.69f), "DNK_Coast_NaturalEarth50m");
        CampaignDenmarkGeography.Create(land, coast);
    }

    private void CreateZones()
    {
        // Gameplay-zone centres are a v10e operational scaffold over real geography.
        // They are NOT historical administrative borders. Polygon borders come later.
        AddZone("DK-VEN", "Vendsyssel", CampaignNation.Denmark, 9.88f, 57.36f);
        AddZone("DK-NJ", "Nordjylland", CampaignNation.Denmark, 9.45f, 56.86f);
        AddZone("DK-MJ", "Midtjylland", CampaignNation.Denmark, 9.25f, 56.28f);
        AddZone("DK-VJ", "Vestjylland", CampaignNation.Denmark, 8.70f, 55.82f);
        AddZone("DK-OJ", "Østjylland", CampaignNation.Denmark, 10.03f, 56.10f);
        AddZone("DK-SJ", "Sydjylland", CampaignNation.Denmark, 9.25f, 55.25f);
        AddZone("DK-FYN", "Fyn", CampaignNation.Denmark, 10.30f, 55.34f);
        AddZone("DK-NSJ", "Nordsjælland", CampaignNation.Denmark, 12.13f, 55.94f);
        AddZone("DK-KBH", "København", CampaignNation.Denmark, 12.5683f, 55.6761f);
        AddZone("DK-SSJ", "Sydsjælland", CampaignNation.Denmark, 11.82f, 55.28f);
        AddZone("DK-LF", "Lolland-Falster", CampaignNation.Denmark, 11.62f, 54.78f);
        AddZone("DK-BOR", "Bornholm", CampaignNation.Denmark, 14.91f, 55.12f);

        Link("DK-VEN", "DK-NJ");
        Link("DK-NJ", "DK-MJ");
        Link("DK-MJ", "DK-VJ");
        Link("DK-MJ", "DK-OJ");
        Link("DK-VJ", "DK-SJ");
        Link("DK-OJ", "DK-SJ");
        Link("DK-SJ", "DK-FYN");
        Link("DK-FYN", "DK-NSJ");
        Link("DK-FYN", "DK-SSJ");
        Link("DK-NSJ", "DK-KBH");
        Link("DK-NSJ", "DK-SSJ");
        Link("DK-SSJ", "DK-LF");
        Link("DK-KBH", "DK-SSJ");

        // Bornholm is intentionally disconnected in land-march routing. Sea
        // transport becomes a separate naval/transport route system later.
    }

    private void AddZone(
        string id,
        string name,
        CampaignNation owner,
        float longitude,
        float latitude)
    {
        Vector3 world = CampaignGeoProjection.Project(longitude, latitude, 0.42f);
        Zone zone = new Zone
        {
            Id = id,
            Name = name,
            Owner = owner,
            Longitude = longitude,
            Latitude = latitude,
            Position = new Vector2(world.x, world.z)
        };

        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObject.name = "ZONE_" + id;
        markerObject.transform.position = world;
        markerObject.transform.localScale = new Vector3(1.25f, 0.11f, 1.25f);
        markerObject.GetComponent<Renderer>().sharedMaterial =
            CreateMaterial(new Color(0.76f, 0.64f, 0.24f), "ZoneMarker_" + id);
        GrandCampaignZoneMarker marker = markerObject.AddComponent<GrandCampaignZoneMarker>();
        marker.Initialize(id);
        zone.Visual = markerObject;

        zones[id] = zone;
    }

    private void Link(string a, string b)
    {
        if (!zones.ContainsKey(a) || !zones.ContainsKey(b))
            return;
        if (!zones[a].Neighbours.Contains(b)) zones[a].Neighbours.Add(b);
        if (!zones[b].Neighbours.Contains(a)) zones[b].Neighbours.Add(a);
    }

    private void CreateCities()
    {
        AddCity("Aalborg", 9.9217f, 57.0488f);
        AddCity("Viborg", 9.4020f, 56.4532f);
        AddCity("Aarhus", 10.2039f, 56.1629f);
        AddCity("Esbjerg", 8.4594f, 55.4765f);
        AddCity("Kolding", 9.4722f, 55.4904f);
        AddCity("Fredericia", 9.7526f, 55.5657f);
        AddCity("Odense", 10.4024f, 55.4038f);
        AddCity("København", 12.5683f, 55.6761f);
        AddCity("Næstved", 11.7609f, 55.2299f);
        AddCity("Rønne", 14.7066f, 55.1009f);
    }

    private void AddCity(string name, float longitude, float latitude)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "CITY_" + name;
        visual.transform.position = CampaignGeoProjection.Project(longitude, latitude, 0.48f);
        visual.transform.localScale = new Vector3(0.36f, 0.10f, 0.36f);
        visual.GetComponent<Renderer>().sharedMaterial =
            CreateMaterial(new Color(0.87f, 0.84f, 0.72f), "CityMarker_" + name);
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        cities.Add(new City
        {
            Name = name,
            Longitude = longitude,
            Latitude = latitude,
            Visual = visual
        });
    }

    private void CreateArmies()
    {
        // QA formation only: location/strength is not yet presented as researched
        // 1 January 1851 OOB. Stable ID and movement state are the test target.
        AddArmy("DK-ARMY-QA", "Dansk hær — QA", CampaignNation.Denmark, "DK-SJ", 24000);
    }

    private void AddArmy(
        string id,
        string name,
        CampaignNation nation,
        string zoneId,
        int strength)
    {
        if (!zones.TryGetValue(zoneId, out Zone zone))
            return;

        GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        token.name = "ARMY_" + id;
        token.transform.position = ZoneWorldPosition(zone) + new Vector3(0f, 1.05f, 0f);
        token.transform.localScale = new Vector3(1.65f, 0.42f, 1.65f);
        token.GetComponent<Renderer>().sharedMaterial =
            CreateMaterial(new Color(0.76f, 0.18f, 0.18f), "Army_" + id);
        GrandCampaignArmyMarker marker = token.AddComponent<GrandCampaignArmyMarker>();
        marker.Initialize(id);

        armies[id] = new Army
        {
            Id = id,
            Name = name,
            Nation = nation,
            CurrentZoneId = zoneId,
            DestinationZoneId = string.Empty,
            Strength = strength,
            Visual = token
        };
    }

    private void Update()
    {
        HandleCamera();
        HandleSelectionAndOrders();
        UpdateCampaignTime();
        UpdateArmies();
    }

    private void HandleCamera()
    {
        if (campaignCamera == null)
            return;

        Vector3 p = campaignCamera.transform.position;
        float pan = 27f * Time.unscaledDeltaTime * Mathf.Max(0.75f, campaignCamera.orthographicSize / 43f);
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) p.z += pan;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) p.z -= pan;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) p.x -= pan;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) p.x += pan;
        p.x = Mathf.Clamp(p.x, -35f, 66f);
        p.z = Mathf.Clamp(p.z, -37f, 39f);
        campaignCamera.transform.position = p;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
            campaignCamera.orthographicSize = Mathf.Clamp(campaignCamera.orthographicSize - wheel * 3.5f, 17f, 67f);

        MapZoomBand nextBand = campaignCamera.orthographicSize <= 29f
            ? MapZoomBand.Close
            : campaignCamera.orthographicSize <= 49f
                ? MapZoomBand.Operational
                : MapZoomBand.Strategic;

        if (nextBand != zoomBand)
        {
            zoomBand = nextBand;
            Debug.Log("CAMPAIGN-ZOOM|Band=" + zoomBand + "|SemanticZoomFoundation=True");
        }

        foreach (Army army in armies.Values)
        {
            if (army.Visual == null)
                continue;
            float scale = zoomBand == MapZoomBand.Close ? 1.35f : zoomBand == MapZoomBand.Operational ? 1.65f : 2.20f;
            army.Visual.transform.localScale = new Vector3(scale, 0.42f, scale);
        }
    }

    private void HandleSelectionAndOrders()
    {
        if (!nationChosen || campaignCamera == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = campaignCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                GrandCampaignArmyMarker armyMarker = hit.collider.GetComponent<GrandCampaignArmyMarker>();
                if (armyMarker != null && armies.TryGetValue(armyMarker.ArmyId, out Army army))
                {
                    if (army.Nation == playerNation)
                    {
                        selectedArmy = army;
                        selectedZone = zones[army.CurrentZoneId];
                    }
                    return;
                }

                GrandCampaignZoneMarker zoneMarker = hit.collider.GetComponent<GrandCampaignZoneMarker>();
                if (zoneMarker != null && zones.TryGetValue(zoneMarker.ZoneId, out Zone zone))
                {
                    selectedZone = zone;
                    return;
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && selectedArmy != null)
        {
            Ray ray = campaignCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f))
                return;

            GrandCampaignZoneMarker marker = hit.collider.GetComponent<GrandCampaignZoneMarker>();
            if (marker == null || !zones.TryGetValue(marker.ZoneId, out Zone destination))
                return;

            Zone current = zones[selectedArmy.CurrentZoneId];
            if (!current.Neighbours.Contains(destination.Id))
            {
                Debug.Log(string.Format(
                    "CAMPAIGN-MOVE|Army={0}|From={1}|To={2}|Accepted=False|Reason=NotAdjacentOrSeaRouteMissing",
                    selectedArmy.Id,
                    current.Id,
                    destination.Id));
                return;
            }

            selectedArmy.DestinationZoneId = destination.Id;
            selectedArmy.Progress = 0f;
            Debug.Log(string.Format(
                "CAMPAIGN-MOVE|Army={0}|From={1}|To={2}|Accepted=True|Geo=WGS84Projected",
                selectedArmy.Id,
                current.Id,
                destination.Id));
        }
    }

    private void UpdateCampaignTime()
    {
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { campaignSpeed = 1f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { campaignSpeed = 5f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { campaignSpeed = 20f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { campaignSpeed = 100f; paused = false; }

        if (paused)
            return;

        double minutes = Time.unscaledDeltaTime * 12.0 * campaignSpeed;
        campaignTime = campaignTime.AddMinutes(minutes);
    }

    private void UpdateArmies()
    {
        if (paused)
            return;

        foreach (Army army in armies.Values)
        {
            if (string.IsNullOrEmpty(army.DestinationZoneId))
                continue;
            if (!zones.TryGetValue(army.CurrentZoneId, out Zone from) ||
                !zones.TryGetValue(army.DestinationZoneId, out Zone to))
                continue;

            // First slice uses projected geographic distance only. Road/terrain,
            // rail, weather, fatigue and staff quality are later modifiers.
            float travelHours = Mathf.Max(5f, Vector2.Distance(from.Position, to.Position) * 0.85f);
            float hoursPerRealSecond = 0.2f * campaignSpeed;
            army.Progress += Time.unscaledDeltaTime * hoursPerRealSecond / travelHours;

            Vector3 start = ZoneWorldPosition(from) + new Vector3(0f, 1.05f, 0f);
            Vector3 end = ZoneWorldPosition(to) + new Vector3(0f, 1.05f, 0f);
            army.Visual.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(army.Progress));

            if (army.Progress < 1f)
                continue;

            army.CurrentZoneId = to.Id;
            army.DestinationZoneId = string.Empty;
            army.Progress = 0f;
            army.Visual.transform.position = end;

            Debug.Log(string.Format(
                "CAMPAIGN-MOVE|Army={0}|Arrived=True|Zone={1}|Time={2:yyyy-MM-dd HH:mm}",
                army.Id,
                to.Id,
                campaignTime));

            CheckForContact(army, to);
        }
    }

    private void CheckForContact(Army arriving, Zone zone)
    {
        foreach (Army other in armies.Values)
        {
            if (other == arriving || other.Nation == arriving.Nation)
                continue;
            if (other.CurrentZoneId != zone.Id || !string.IsNullOrEmpty(other.DestinationZoneId))
                continue;

            paused = true;
            Debug.Log(string.Format(
                "CAMPAIGN-CONTACT|Zone={0}|Attacker={1}|Defender={2}|BattlePrompt=True",
                zone.Id,
                arriving.Name,
                other.Name));
            return;
        }
    }

    private static Vector3 ZoneWorldPosition(Zone zone)
    {
        return new Vector3(zone.Position.x, 0.42f, zone.Position.y);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        smallStyle.normal.textColor = Color.white;

        zoneStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        zoneStyle.normal.textColor = Color.white;

        cityStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        cityStyle.normal.textColor = new Color(0.95f, 0.94f, 0.84f);

        mapInfoStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        mapInfoStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!nationChosen)
        {
            DrawNationSelection();
            return;
        }

        DrawTopBar();
        DrawSelectionPanel();
        DrawMapInfo();
        DrawZoneLabels();
        if (zoomBand == MapZoomBand.Close)
            DrawCityLabels();
    }

    private void DrawNationSelection()
    {
        float width = 430f;
        float height = 150f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, string.Empty);
        GUI.Box(new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, 34f), "GRAND CAMPAIGN — 1. JANUAR 1851", titleStyle);
        GUI.Label(new Rect(panel.x + 25f, panel.y + 50f, panel.width - 50f, 22f), "v10e bygger Danmark som første real-geography vertical slice.");

        if (GUI.Button(new Rect(panel.x + 55f, panel.y + 82f, panel.width - 110f, 34f), "START SOM DANMARK"))
        {
            playerNation = CampaignNation.Denmark;
            nationChosen = true;
            SelectFirstArmyForNation();
            Debug.Log("CAMPAIGN-NATION|Player=Denmark|Start=1851-01-01|Geo=NaturalEarth50m");
        }
    }

    private void SelectFirstArmyForNation()
    {
        foreach (Army army in armies.Values)
        {
            if (army.Nation != playerNation)
                continue;
            selectedArmy = army;
            selectedZone = zones[army.CurrentZoneId];
            return;
        }
    }

    private void DrawTopBar()
    {
        Rect bar = new Rect(230f, 8f, Mathf.Max(520f, Mathf.Min(780f, Screen.width - 240f)), 31f);
        GUI.Box(bar, string.Empty);

        string state = paused ? "PAUSE" : "x" + campaignSpeed.ToString("0");
        GUI.Label(
            new Rect(bar.x + 8f, bar.y + 6f, 330f, 20f),
            campaignTime.ToString("dd MMM yyyy HH:mm") + " | " + state + " | Danmark");

        float x = bar.x + 340f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 70f, 23f), paused ? "FORTSÆT" : "PAUSE")) paused = !paused;
        x += 74f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x1")) { campaignSpeed = 1f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x5")) { campaignSpeed = 5f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x20")) { campaignSpeed = 20f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 54f, 23f), "x100")) { campaignSpeed = 100f; paused = false; }
    }

    private void DrawSelectionPanel()
    {
        Rect panel = new Rect(8f, 42f, 310f, 144f);
        GUI.Box(panel, string.Empty);

        string zoneText = selectedZone != null
            ? string.Format(
                "Zone: {0} ({1})\nWGS84: {2:0.0000}°E, {3:0.0000}°N\nNaboer: {4}",
                selectedZone.Name,
                selectedZone.Id,
                selectedZone.Longitude,
                selectedZone.Latitude,
                string.Join(", ", selectedZone.Neighbours))
            : "Zone: ingen";
        GUI.Box(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 70f), zoneText, smallStyle);

        string armyText = selectedArmy != null
            ? string.Format(
                "Formation: {0}\nStyrke: {1:N0} [QA]\nRMB på nabozone = marchordre",
                selectedArmy.Name,
                selectedArmy.Strength)
            : "Formation: ingen valgt";
        GUI.Box(new Rect(panel.x + 6f, panel.y + 80f, panel.width - 12f, 57f), armyText, smallStyle);
    }

    private void DrawMapInfo()
    {
        Rect box = new Rect(8f, Screen.height - 76f, 395f, 68f);
        GUI.Box(
            box,
            "REAL GEO: Natural Earth 1:50m | CRS: WGS84\n" +
            "Aktiv detaljeregion: Danmark | Zoom: " + zoomBand + "\n" +
            "Zonecentre = gameplay scaffold | DEM + 1851 historiske grænser/infrastruktur følger",
            mapInfoStyle);
    }

    private void DrawZoneLabels()
    {
        if (campaignCamera == null || zoomBand == MapZoomBand.Strategic)
            return;

        foreach (Zone zone in zones.Values)
        {
            Vector3 screen = campaignCamera.WorldToScreenPoint(ZoneWorldPosition(zone) + new Vector3(0f, 0.3f, 0f));
            if (screen.z <= 0f)
                continue;

            float y = Screen.height - screen.y;
            GUI.Label(new Rect(screen.x - 58f, y - 12f, 116f, 20f), zone.Name, zoneStyle);
        }
    }

    private void DrawCityLabels()
    {
        if (campaignCamera == null)
            return;

        foreach (City city in cities)
        {
            if (city.Visual == null)
                continue;
            Vector3 screen = campaignCamera.WorldToScreenPoint(city.Visual.transform.position);
            if (screen.z <= 0f)
                continue;
            float y = Screen.height - screen.y;
            GUI.Label(new Rect(screen.x + 5f, y - 9f, 105f, 18f), city.Name, cityStyle);
        }
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}
