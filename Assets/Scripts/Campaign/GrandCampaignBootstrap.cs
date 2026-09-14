using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-30000)]
public sealed class GrandCampaignBootstrap : MonoBehaviour
{
    public enum CampaignNation { Denmark, SwedenNorway, Prussia, Austria, France, UnitedKingdom, Russia, Netherlands, Hanover, Mecklenburg, GermanConfederationOther }
    private enum MapZoomBand { Close, Operational, Strategic }
    private enum RouteType { None, Land, Ferry }

    private sealed class Zone
    {
        public CampaignDenmark1851Registry.ZoneDef Def;
        public CampaignNation Owner;
        public Vector2 Position;
        public GameObject Visual;
        public string Id => Def.Id;
        public string Name => Def.Name;
    }

    private sealed class City
    {
        public CampaignDenmark1851Registry.CityDef Def;
        public GameObject Visual;
        public string Id => Def.Id;
        public string Name => Def.Name;
        public string ZoneId => Def.ZoneId;
    }

    private sealed class Army
    {
        public string Id;
        public string Name;
        public CampaignNation Nation;
        public string CurrentZoneId;
        public string DestinationZoneId;
        public RouteType Route;
        public float Progress;
        public int Strength;
        public GameObject Visual;
    }

    public const bool CampaignModeEnabled = true;
    public const string CampaignVersion = CampaignBuildInfo.CurrentVersion;
    public static GrandCampaignBootstrap Instance { get; private set; }

    private readonly Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
    private readonly Dictionary<string, City> citiesById = new Dictionary<string, City>();
    private readonly List<City> cities = new List<City>();
    private readonly Dictionary<string, Army> armies = new Dictionary<string, Army>();

    private Camera campaignCamera;
    private CampaignNation playerNation = CampaignNation.Denmark;
    private bool nationChosen;
    private bool paused;
    private float campaignSpeed = 1f;
    private DateTime campaignTime = new DateTime(1851, 1, 1, 8, 0, 0);
    private MapZoomBand zoomBand = MapZoomBand.Operational;
    private Zone selectedZone;
    private City selectedCity;
    private Army selectedArmy;

    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle zoneStyle;
    private GUIStyle cityStyle;
    private GUIStyle mapInfoStyle;

    private Material zoneMaterial;
    private Material cityMaterialA;
    private Material cityMaterialB;
    private Material cityMaterialC;
    private Material armyMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<GrandCampaignBootstrap>() != null) return;
        GameObject root = new GameObject("PROJECT1864_GrandCampaign_CURRENT");
        DontDestroyOnLoad(root);
        root.AddComponent<GrandCampaignBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!CampaignDenmark1851Registry.Validate(out string error))
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|RegistryValid=False|Error=" + error);
            enabled = false;
            return;
        }

        BuildCampaignWorld();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildCampaignWorld()
    {
        Application.targetFrameRate = 120;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.60f, 0.62f, 0.64f);

        CreateMaterials();
        CreateCamera();
        CreateLight();
        CreateSeaBoard();
        CreateRealDenmarkGeography();
        CreateZones();
        CreateCities();
        AddArmy("DK-ARMY-QA", "Dansk hær — QA", CampaignNation.Denmark, "DK-Z11-VEJ", 24000);

        Debug.Log(string.Format(
            CampaignBuildInfo.LogTag + "|Installed=True|Registry=ZONE-REG-01+CITY-REG-01|Zones={0}|Cities={1}|UrbanPopulation1850={2}|Start=1851-01-01",
            zones.Count, cities.Count, UrbanPopulation()));
    }

    private void CreateMaterials()
    {
        zoneMaterial = CreateMaterial(new Color(0.76f, 0.64f, 0.24f), "ZoneMarker_1851");
        cityMaterialA = CreateMaterial(new Color(0.95f, 0.88f, 0.55f), "City_A_1851");
        cityMaterialB = CreateMaterial(new Color(0.84f, 0.84f, 0.75f), "City_B_1851");
        cityMaterialC = CreateMaterial(new Color(0.70f, 0.72f, 0.68f), "City_C_1851");
        armyMaterial = CreateMaterial(new Color(0.76f, 0.18f, 0.18f), "Army_Denmark_QA");
    }

    private void CreateCamera()
    {
        Camera existing = Camera.main;
        if (existing != null) existing.gameObject.SetActive(false);

        GameObject go = new GameObject("Grand Campaign Camera");
        campaignCamera = go.AddComponent<Camera>();
        campaignCamera.tag = "MainCamera";
        campaignCamera.orthographic = true;
        campaignCamera.orthographicSize = 43f;
        campaignCamera.nearClipPlane = 0.1f;
        campaignCamera.farClipPlane = 300f;
        campaignCamera.backgroundColor = new Color(0.16f, 0.29f, 0.40f);
        campaignCamera.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(11f, 95f, 1f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void CreateLight()
    {
        GameObject go = new GameObject("Campaign Sun");
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        go.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
    }

    private void CreateSeaBoard()
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "Grand Campaign Sea";
        sea.transform.position = new Vector3(10f, -0.45f, 0f);
        sea.transform.localScale = new Vector3(170f, 0.8f, 105f);
        sea.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.34f, 0.47f), "CampaignSea");
        Collider c = sea.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    private void CreateRealDenmarkGeography()
    {
        Material land = CreateMaterial(new Color(0.38f, 0.47f, 0.27f), "DNK_Land_NaturalEarth50m");
        Material coast = CreateMaterial(new Color(0.84f, 0.82f, 0.69f), "DNK_Coast_NaturalEarth50m");
        CampaignDenmarkGeography.Create(land, coast);
    }

    private void CreateZones()
    {
        foreach (CampaignDenmark1851Registry.ZoneDef def in CampaignDenmark1851Registry.Zones)
        {
            Vector3 world = CampaignGeoProjection.Project(def.Longitude, def.Latitude, 0.42f);
            Zone zone = new Zone
            {
                Def = def,
                Owner = CampaignNation.Denmark,
                Position = new Vector2(world.x, world.z)
            };

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ZONE_" + def.Id;
            visual.transform.position = world;
            visual.transform.localScale = new Vector3(0.95f, 0.10f, 0.95f);
            visual.GetComponent<Renderer>().sharedMaterial = zoneMaterial;
            visual.AddComponent<GrandCampaignZoneMarker>().Initialize(def.Id);
            zone.Visual = visual;
            zones[def.Id] = zone;
        }
    }

    private void CreateCities()
    {
        foreach (CampaignDenmark1851Registry.CityDef def in CampaignDenmark1851Registry.Cities)
        {
            if (!zones.ContainsKey(def.ZoneId))
            {
                Debug.LogError(CampaignBuildInfo.LogTag + "|CitySkipped=True|City=" + def.Name + "|UnknownZone=" + def.ZoneId);
                continue;
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "CITY_" + def.Id;
            visual.transform.position = CampaignGeoProjection.Project(def.Longitude, def.Latitude, 0.48f);
            float scale = CityScale(def);
            visual.transform.localScale = new Vector3(scale, 0.10f, scale);
            visual.GetComponent<Renderer>().sharedMaterial = CityMaterial(def.Tier);
            visual.AddComponent<GrandCampaignCityMarker>().Initialize(def.Id);

            City city = new City { Def = def, Visual = visual };
            cities.Add(city);
            citiesById[def.Id] = city;
        }
    }

    private static float CityScale(CampaignDenmark1851Registry.CityDef def)
    {
        float baseScale = def.Tier == CampaignDenmark1851Registry.CityTier.A ? 0.52f :
                          def.Tier == CampaignDenmark1851Registry.CityTier.B ? 0.38f : 0.27f;
        float boost = Mathf.Clamp((Mathf.Log10(Mathf.Max(300, def.Population1850)) - 3f) * 0.12f, 0f, 0.28f);
        return baseScale + boost;
    }

    private Material CityMaterial(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A) return cityMaterialA;
        if (tier == CampaignDenmark1851Registry.CityTier.B) return cityMaterialB;
        return cityMaterialC;
    }

    private void AddArmy(string id, string name, CampaignNation nation, string zoneId, int strength)
    {
        if (!zones.TryGetValue(zoneId, out Zone zone))
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|ArmySkipped=True|Army=" + id + "|UnknownZone=" + zoneId);
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "ARMY_" + id;
        visual.transform.position = ZoneWorldPosition(zone) + new Vector3(0f, 1.05f, 0f);
        visual.transform.localScale = new Vector3(1.65f, 0.42f, 1.65f);
        visual.GetComponent<Renderer>().sharedMaterial = armyMaterial;
        visual.AddComponent<GrandCampaignArmyMarker>().Initialize(id);

        armies[id] = new Army
        {
            Id = id, Name = name, Nation = nation, CurrentZoneId = zoneId,
            DestinationZoneId = string.Empty, Route = RouteType.None,
            Strength = strength, Visual = visual
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
        if (campaignCamera == null) return;

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

        MapZoomBand next = campaignCamera.orthographicSize <= 29f ? MapZoomBand.Close :
                           campaignCamera.orthographicSize <= 49f ? MapZoomBand.Operational : MapZoomBand.Strategic;
        if (next != zoomBand)
        {
            zoomBand = next;
            Debug.Log(CampaignBuildInfo.LogTag + "|ZoomBand=" + zoomBand + "|SemanticZoomFoundation=True");
        }

        foreach (Army army in armies.Values)
        {
            float scale = zoomBand == MapZoomBand.Close ? 1.35f : zoomBand == MapZoomBand.Operational ? 1.65f : 2.20f;
            if (army.Visual != null) army.Visual.transform.localScale = new Vector3(scale, 0.42f, scale);
        }
    }

    private void HandleSelectionAndOrders()
    {
        if (!nationChosen || campaignCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = campaignCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                GrandCampaignArmyMarker am = hit.collider.GetComponent<GrandCampaignArmyMarker>();
                if (am != null && armies.TryGetValue(am.ArmyId, out Army army) && army.Nation == playerNation)
                {
                    selectedArmy = army;
                    selectedCity = null;
                    selectedZone = zones[army.CurrentZoneId];
                    return;
                }

                GrandCampaignCityMarker cm = hit.collider.GetComponent<GrandCampaignCityMarker>();
                if (cm != null && citiesById.TryGetValue(cm.CityId, out City city))
                {
                    selectedCity = city;
                    selectedZone = zones[city.ZoneId];
                    return;
                }

                GrandCampaignZoneMarker zm = hit.collider.GetComponent<GrandCampaignZoneMarker>();
                if (zm != null && zones.TryGetValue(zm.ZoneId, out Zone zone))
                {
                    selectedCity = null;
                    selectedZone = zone;
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && selectedArmy != null)
        {
            Ray ray = campaignCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f)) return;

            Zone destination = null;
            GrandCampaignZoneMarker marker = hit.collider.GetComponent<GrandCampaignZoneMarker>();
            if (marker != null)
                zones.TryGetValue(marker.ZoneId, out destination);

            // City colliders may sit on top of their zone marker. Treat RMB on a city
            // as RMB on the city's zone so city markers do not block march orders.
            if (destination == null)
            {
                GrandCampaignCityMarker cityMarker = hit.collider.GetComponent<GrandCampaignCityMarker>();
                if (cityMarker != null && citiesById.TryGetValue(cityMarker.CityId, out City destinationCity))
                    zones.TryGetValue(destinationCity.ZoneId, out destination);
            }

            if (destination == null) return;

            Zone current = zones[selectedArmy.CurrentZoneId];
            RouteType route = GetRouteType(current.Def, destination.Id);
            if (route == RouteType.None)
            {
                Debug.Log(CampaignBuildInfo.LogTag + "|Move|Army=" + selectedArmy.Id + "|From=" + current.Id + "|To=" + destination.Id + "|Accepted=False|Reason=NotAdjacent");
                return;
            }

            selectedArmy.DestinationZoneId = destination.Id;
            selectedArmy.Route = route;
            selectedArmy.Progress = 0f;
            Debug.Log(CampaignBuildInfo.LogTag + "|Move|Army=" + selectedArmy.Id + "|From=" + current.Id + "|To=" + destination.Id + "|Accepted=True|RouteType=" + route);
        }
    }

    private static RouteType GetRouteType(CampaignDenmark1851Registry.ZoneDef from, string destinationId)
    {
        if (Array.IndexOf(from.LandNeighbours, destinationId) >= 0) return RouteType.Land;
        if (Array.IndexOf(from.FerryNeighbours, destinationId) >= 0) return RouteType.Ferry;
        return RouteType.None;
    }

    private void UpdateCampaignTime()
    {
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { campaignSpeed = 1f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { campaignSpeed = 5f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { campaignSpeed = 20f; paused = false; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { campaignSpeed = 100f; paused = false; }
        if (!paused) campaignTime = campaignTime.AddMinutes(Time.unscaledDeltaTime * 12.0 * campaignSpeed);
    }

    private void UpdateArmies()
    {
        if (paused) return;

        foreach (Army army in armies.Values)
        {
            if (string.IsNullOrEmpty(army.DestinationZoneId)) continue;
            if (!zones.TryGetValue(army.CurrentZoneId, out Zone from) || !zones.TryGetValue(army.DestinationZoneId, out Zone to)) continue;

            float multiplier = army.Route == RouteType.Ferry ? 1.35f : 1f;
            float hours = Mathf.Max(5f, Vector2.Distance(from.Position, to.Position) * 0.85f * multiplier);
            army.Progress += Time.unscaledDeltaTime * (0.2f * campaignSpeed) / hours;

            Vector3 start = ZoneWorldPosition(from) + new Vector3(0f, 1.05f, 0f);
            Vector3 end = ZoneWorldPosition(to) + new Vector3(0f, 1.05f, 0f);
            army.Visual.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(army.Progress));

            if (army.Progress < 1f) continue;

            army.CurrentZoneId = to.Id;
            army.DestinationZoneId = string.Empty;
            army.Route = RouteType.None;
            army.Progress = 0f;
            army.Visual.transform.position = end;
            Debug.Log(CampaignBuildInfo.LogTag + "|Move|Army=" + army.Id + "|Arrived=True|Zone=" + to.Id + "|Time=" + campaignTime.ToString("yyyy-MM-dd HH:mm"));
        }
    }

    private static Vector3 ZoneWorldPosition(Zone zone) => new Vector3(zone.Position.x, 0.42f, zone.Position.y);

    public bool CanBuildHeavyMilitaryInCity(string cityId)
    {
        return citiesById.TryGetValue(cityId, out City city) && city.Def.AllowsHeavyMilitaryConstruction;
    }

    private int UrbanPopulation()
    {
        int sum = 0;
        foreach (City city in cities) sum += city.Def.Population1850;
        return sum;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.box) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;
        smallStyle = new GUIStyle(GUI.skin.box) { fontSize = 10, alignment = TextAnchor.UpperLeft, wordWrap = true };
        smallStyle.normal.textColor = Color.white;
        zoneStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        zoneStyle.normal.textColor = Color.white;
        cityStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleLeft };
        cityStyle.normal.textColor = new Color(0.95f, 0.94f, 0.84f);
        mapInfoStyle = new GUIStyle(GUI.skin.box) { fontSize = 9, alignment = TextAnchor.MiddleLeft };
        mapInfoStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (!nationChosen) { DrawNationSelection(); return; }

        // Essential controls stay available even when F1 hides the rest of the HUD.
        DrawTopBar();

        if (CampaignHudStateV010N2.SelectionVisible)
            DrawSelectionPanel();
        if (CampaignHudStateV010N2.DebugVisible)
            DrawMapInfo();

        DrawZoneLabels();
        DrawCityLabels();
    }

    private void DrawNationSelection()
    {
        Rect panel = new Rect((Screen.width - 460f) * 0.5f, (Screen.height - 160f) * 0.5f, 460f, 160f);
        GUI.Box(panel, string.Empty);
        GUI.Box(new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, 34f), "GRAND CAMPAIGN — 1. JANUAR 1851", titleStyle);
        GUI.Label(
            new Rect(panel.x + 25f, panel.y + 50f, panel.width - 50f, 35f),
            CampaignBuildInfo.CurrentVersion + ": 20 historiske zoner + alle 68 købstæder fra CITY-REG-01.");

        if (GUI.Button(new Rect(panel.x + 55f, panel.y + 96f, panel.width - 110f, 34f), "START SOM DANMARK"))
        {
            nationChosen = true;
            foreach (Army army in armies.Values)
            {
                if (army.Nation != playerNation) continue;
                selectedArmy = army;
                selectedZone = zones[army.CurrentZoneId];
                break;
            }
            Debug.Log(CampaignBuildInfo.LogTag + "|Nation|Player=Denmark|Registry=ZONE-REG-01+CITY-REG-01");
        }
    }

    private void DrawTopBar()
    {
        Rect bar = new Rect(230f, 8f, Mathf.Max(520f, Mathf.Min(960f, Screen.width - 240f)), 31f);
        GUI.Box(bar, string.Empty);
        string state = paused ? "PAUSE" : "x" + campaignSpeed.ToString("0");
        GUI.Label(
            new Rect(bar.x + 8f, bar.y + 6f, 390f, 20f),
            campaignTime.ToString("dd MMM yyyy HH:mm") + " | " + state + " | Danmark | " + CampaignBuildInfo.CurrentVersion);

        float x = bar.x + 400f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 70f, 23f), paused ? "FORTSÆT" : "PAUSE")) paused = !paused;
        x += 74f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x1")) { campaignSpeed = 1f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x5")) { campaignSpeed = 5f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 48f, 23f), "x20")) { campaignSpeed = 20f; paused = false; }
        x += 52f;
        if (GUI.Button(new Rect(x, bar.y + 4f, 54f, 23f), "x100")) { campaignSpeed = 100f; paused = false; }
        x += 58f;

        float right = bar.x + bar.width - 4f;
        if (x + 46f <= right)
        {
            if (GUI.Button(new Rect(x, bar.y + 4f, 46f, 23f), CampaignHudStateV010N2.SelectionEnabled ? "INFO✓" : "INFO"))
                CampaignHudStateV010N2.ToggleSelectionPanel();
            x += 50f;
        }
        if (x + 52f <= right)
        {
            if (GUI.Button(new Rect(x, bar.y + 4f, 52f, 23f), CampaignHudStateV010N2.DebugEnabled ? "DBG✓" : "DBG"))
                CampaignHudStateV010N2.ToggleDebugPanels();
        }
    }

    private void DrawSelectionPanel()
    {
        Rect panel = new Rect(8f, 42f, 360f, selectedCity != null ? 230f : 166f);
        GUI.Box(panel, string.Empty);

        if (GUI.Button(new Rect(panel.x + panel.width - 28f, panel.y + 6f, 22f, 22f), "×"))
        {
            CampaignHudStateV010N2.ToggleSelectionPanel();
            return;
        }

        string zoneText = selectedZone == null ? "Zone: ingen" :
            "Zone: " + selectedZone.Name + "\nID: " + selectedZone.Id +
            "\nLand: " + JoinOrDash(selectedZone.Def.LandNeighbours) +
            "\nFærge: " + JoinOrDash(selectedZone.Def.FerryNeighbours);
        GUI.Box(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 40f, 84f), zoneText, smallStyle);

        float y = panel.y + 94f;
        if (selectedCity != null)
        {
            string rule = selectedCity.Def.AllowsHeavyMilitaryConstruction
                ? "Tung militær udbygning: mulig efter øvrige krav"
                : "Tung militær udbygning: BLOKERET (fixed/special undtaget)";
            string cityText = "By: " + selectedCity.Name + " | Klasse " + selectedCity.Def.Tier +
                "\nIndbyggere 1850: " + selectedCity.Def.Population1850.ToString("N0") +
                "\nZone: " + selectedCity.ZoneId + "\n" + rule;
            GUI.Box(new Rect(panel.x + 6f, y, panel.width - 12f, 74f), cityText, smallStyle);
            y += 78f;
        }

        string armyText = selectedArmy == null ? "Formation: ingen valgt" :
            "Formation: " + selectedArmy.Name + "\nStyrke: " + selectedArmy.Strength.ToString("N0") +
            " [QA] | Zone: " + selectedArmy.CurrentZoneId + "\nRMB på direkte land-/færgezone = marchordre";
        GUI.Box(new Rect(panel.x + 6f, y, panel.width - 12f, 62f), armyText, smallStyle);
    }

    private static string JoinOrDash(string[] items) => items == null || items.Length == 0 ? "—" : string.Join(", ", items);

    private void DrawMapInfo()
    {
        Rect box = new Rect(8f, Screen.height - 82f, 485f, 74f);
        GUI.Box(box,
            "1851 DATA: ZONE-REG-01 = 20 zoner | CITY-REG-01 = 68 købstæder\n" +
            "Urban population checksum: " + UrbanPopulation().ToString("N0") + " | Zoom: " + zoomBand + "\n" +
            "A = Development City | B/C = ingen fri tung militær udbygning | F1/F2/F3/F4 = HUD",
            mapInfoStyle);
    }

    private void DrawZoneLabels()
    {
        if (campaignCamera == null || zoomBand == MapZoomBand.Strategic) return;
        foreach (Zone zone in zones.Values)
        {
            Vector3 screen = campaignCamera.WorldToScreenPoint(ZoneWorldPosition(zone) + new Vector3(0f, 0.3f, 0f));
            if (screen.z <= 0f) continue;
            GUI.Label(new Rect(screen.x - 90f, Screen.height - screen.y - 12f, 180f, 20f), zone.Name, zoneStyle);
        }
    }

    private void DrawCityLabels()
    {
        if (campaignCamera == null || zoomBand == MapZoomBand.Strategic) return;

        foreach (City city in cities)
        {
            if (city.Visual == null) continue;
            if (zoomBand == MapZoomBand.Operational && city.Def.Tier != CampaignDenmark1851Registry.CityTier.A) continue;

            Vector3 screen = campaignCamera.WorldToScreenPoint(city.Visual.transform.position);
            if (screen.z <= 0f) continue;
            string text = zoomBand == MapZoomBand.Close ? city.Name + " [" + city.Def.Tier + "]" : city.Name;
            GUI.Label(new Rect(screen.x + 5f, Screen.height - screen.y - 9f, 135f, 18f), text, cityStyle);
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
