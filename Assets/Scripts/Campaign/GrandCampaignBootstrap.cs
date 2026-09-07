using System;
using System.Collections.Generic;
using UnityEngine;

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

    private sealed class Zone
    {
        public string Id;
        public string Name;
        public CampaignNation Owner;
        public Vector2 Position;
        public Vector2 Size;
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

    public const bool CampaignModeEnabled = true;
    public static GrandCampaignBootstrap Instance { get; private set; }

    private readonly Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
    private readonly Dictionary<string, Army> armies = new Dictionary<string, Army>();
    private readonly List<CampaignNation> playableNations = new List<CampaignNation>();

    private Camera campaignCamera;
    private CampaignNation playerNation = CampaignNation.Denmark;
    private bool nationChosen;
    private Zone selectedZone;
    private Army selectedArmy;

    private bool paused;
    private float campaignSpeed = 1f;
    private DateTime campaignTime = new DateTime(1864, 2, 1, 8, 0, 0);

    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle zoneStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<GrandCampaignBootstrap>() != null)
            return;

        GameObject root = new GameObject("PROJECT1864_GrandCampaign_v000010b");
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
        playableNations.Add(CampaignNation.Denmark);
        playableNations.Add(CampaignNation.SwedenNorway);
        playableNations.Add(CampaignNation.Prussia);
        playableNations.Add(CampaignNation.Austria);
        playableNations.Add(CampaignNation.France);
        playableNations.Add(CampaignNation.UnitedKingdom);
        playableNations.Add(CampaignNation.Russia);
        playableNations.Add(CampaignNation.Netherlands);
        playableNations.Add(CampaignNation.Hanover);
        playableNations.Add(CampaignNation.Mecklenburg);
        playableNations.Add(CampaignNation.GermanConfederationOther);
    }

    private void BuildCampaignWorld()
    {
        Application.targetFrameRate = 120;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.66f);

        CreateCamera();
        CreateLight();
        CreateSeaBoard();
        CreateZones();
        CreateArmies();

        Debug.Log(string.Format(
            "CAMPAIGN-10B|Installed=True|PlayableNations={0}|Zones={1}|Armies={2}|Map=QA-Geographic-Scaffold",
            playableNations.Count,
            zones.Count,
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
        campaignCamera.orthographicSize = 60f;
        campaignCamera.nearClipPlane = 0.1f;
        campaignCamera.farClipPlane = 300f;
        cameraObject.transform.position = new Vector3(8f, 95f, 4f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void CreateLight()
    {
        GameObject lightObject = new GameObject("Campaign Sun");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
    }

    private void CreateSeaBoard()
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "Grand Campaign Sea Board";
        sea.transform.position = new Vector3(0f, -1.0f, 0f);
        sea.transform.localScale = new Vector3(180f, 1f, 125f);
        sea.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.20f, 0.34f, 0.46f), "CampaignSea");
        Collider collider = sea.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void CreateZones()
    {
        // QA geographic scaffold only. Positions are intentionally coarse and will
        // be replaced by georeferenced polygons/DEM without changing the ZoneId model.
        AddZone("DK-NJ", "Nordjylland", CampaignNation.Denmark, -12, 32, 10, 10);
        AddZone("DK-CJ", "Midtjylland", CampaignNation.Denmark, -11, 21, 10, 10);
        AddZone("DK-SJ", "Sydjylland", CampaignNation.Denmark, -10, 10, 10, 10);
        AddZone("DK-FYN", "Fyn", CampaignNation.Denmark, 1, 12, 7, 7);
        AddZone("DK-SJL", "Sjælland", CampaignNation.Denmark, 10, 14, 9, 8);
        AddZone("DK-BOR", "Bornholm", CampaignNation.Denmark, 25, 16, 5, 5);

        AddZone("SN-SCA", "Skåne", CampaignNation.SwedenNorway, 13, 27, 10, 9);
        AddZone("SN-GOT", "Götaland", CampaignNation.SwedenNorway, 17, 39, 12, 11);
        AddZone("SN-SVE", "Svealand", CampaignNation.SwedenNorway, 22, 52, 13, 12);
        AddZone("SN-OSL", "Oslofjord", CampaignNation.SwedenNorway, -2, 46, 11, 10);
        AddZone("SN-NOR", "Sydnorge", CampaignNation.SwedenNorway, -11, 55, 13, 11);

        AddZone("PR-SH", "Schleswig", CampaignNation.Prussia, -8, 0, 11, 8);
        AddZone("PR-HOL", "Holstein", CampaignNation.Prussia, -6, -9, 12, 8);
        AddZone("PR-BRA", "Brandenburg", CampaignNation.Prussia, 20, -7, 13, 10);
        AddZone("PR-POM", "Pommern", CampaignNation.Prussia, 31, 2, 13, 9);
        AddZone("PR-SIL", "Schlesien", CampaignNation.Prussia, 33, -17, 13, 10);
        AddZone("PR-RHI", "Rheinprovinz", CampaignNation.Prussia, -23, -20, 13, 11);

        AddZone("AT-BOH", "Böhmen", CampaignNation.Austria, 25, -31, 14, 11);
        AddZone("AT-MOR", "Mähren", CampaignNation.Austria, 40, -31, 11, 10);
        AddZone("AT-LAU", "Niederösterreich", CampaignNation.Austria, 43, -43, 13, 10);
        AddZone("AT-TYR", "Tirol", CampaignNation.Austria, 21, -47, 14, 9);

        AddZone("FR-N", "Nordfrankrig", CampaignNation.France, -49, -24, 15, 12);
        AddZone("FR-PAR", "Île-de-France", CampaignNation.France, -48, -38, 14, 11);
        AddZone("FR-E", "Østfrankrig", CampaignNation.France, -31, -38, 15, 11);
        AddZone("FR-S", "Sydfrankrig", CampaignNation.France, -45, -53, 20, 10);

        AddZone("UK-S", "Sydengland", CampaignNation.UnitedKingdom, -72, -18, 14, 10);
        AddZone("UK-N", "Nordengland", CampaignNation.UnitedKingdom, -73, -5, 13, 11);
        AddZone("UK-SCO", "Skotland", CampaignNation.UnitedKingdom, -75, 10, 13, 13);
        AddZone("UK-WAL", "Wales", CampaignNation.UnitedKingdom, -83, -11, 8, 10);

        AddZone("NL-HOL", "Holland", CampaignNation.Netherlands, -28, -7, 8, 8);
        AddZone("NL-N", "Nordnederlandene", CampaignNation.Netherlands, -27, 2, 8, 8);

        AddZone("HA-W", "Hannover Vest", CampaignNation.Hanover, -15, -8, 10, 9);
        AddZone("HA-E", "Hannover Øst", CampaignNation.Hanover, -4, -8, 10, 9);

        AddZone("ME-S", "Mecklenburg-Schwerin", CampaignNation.Mecklenburg, 12, 2, 10, 8);
        AddZone("ME-ST", "Mecklenburg-Strelitz", CampaignNation.Mecklenburg, 20, 2, 7, 7);

        AddZone("DE-C", "Tyske Forbund - Midt", CampaignNation.GermanConfederationOther, -3, -25, 15, 11);
        AddZone("DE-S", "Tyske Forbund - Syd", CampaignNation.GermanConfederationOther, 2, -40, 18, 12);

        AddZone("RU-BAL", "Russiske Østersøprovinser", CampaignNation.Russia, 57, 18, 17, 12);
        AddZone("RU-POL", "Kongeriget Polen", CampaignNation.Russia, 55, -8, 16, 13);
        AddZone("RU-STP", "Sankt Petersborg", CampaignNation.Russia, 73, 32, 16, 13);
        AddZone("RU-W", "Vestlige Rusland", CampaignNation.Russia, 77, 4, 18, 16);

        Link("DK-NJ", "DK-CJ");
        Link("DK-CJ", "DK-SJ");
        Link("DK-SJ", "PR-SH");
        Link("DK-SJ", "DK-FYN");
        Link("DK-FYN", "DK-SJL");
        Link("DK-SJL", "SN-SCA");
        Link("DK-SJL", "DK-BOR");

        Link("SN-SCA", "SN-GOT");
        Link("SN-GOT", "SN-SVE");
        Link("SN-GOT", "SN-OSL");
        Link("SN-OSL", "SN-NOR");

        Link("PR-SH", "PR-HOL");
        Link("PR-HOL", "HA-E");
        Link("PR-HOL", "ME-S");
        Link("ME-S", "ME-ST");
        Link("ME-ST", "PR-BRA");
        Link("PR-BRA", "PR-POM");
        Link("PR-BRA", "PR-SIL");
        Link("PR-BRA", "DE-C");
        Link("PR-RHI", "NL-HOL");
        Link("PR-RHI", "DE-C");
        Link("HA-W", "HA-E");
        Link("HA-W", "NL-HOL");
        Link("HA-E", "DE-C");
        Link("DE-C", "DE-S");
        Link("DE-S", "AT-BOH");
        Link("AT-BOH", "AT-MOR");
        Link("AT-MOR", "AT-LAU");
        Link("AT-BOH", "AT-TYR");
        Link("FR-N", "FR-PAR");
        Link("FR-PAR", "FR-E");
        Link("FR-PAR", "FR-S");
        Link("FR-E", "PR-RHI");
        Link("FR-N", "NL-HOL");
        Link("NL-HOL", "NL-N");
        Link("UK-S", "UK-N");
        Link("UK-N", "UK-SCO");
        Link("UK-S", "UK-WAL");
        Link("PR-POM", "RU-BAL");
        Link("RU-BAL", "RU-STP");
        Link("RU-BAL", "RU-POL");
        Link("RU-POL", "RU-W");
        Link("RU-STP", "RU-W");
    }

    private void AddZone(string id, string name, CampaignNation owner, float x, float z, float width, float depth)
    {
        Zone zone = new Zone
        {
            Id = id,
            Name = name,
            Owner = owner,
            Position = new Vector2(x, z),
            Size = new Vector2(width, depth)
        };

        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "ZONE_" + id;
        tile.transform.position = new Vector3(x, 0f, z);
        tile.transform.localScale = new Vector3(width - 0.35f, 1.1f, depth - 0.35f);
        tile.GetComponent<Renderer>().sharedMaterial = CreateMaterial(GetNationColor(owner), "Zone_" + id);
        GrandCampaignZoneMarker marker = tile.AddComponent<GrandCampaignZoneMarker>();
        marker.Initialize(id);
        zone.Visual = tile;

        zones[id] = zone;
    }

    private void Link(string a, string b)
    {
        if (!zones.ContainsKey(a) || !zones.ContainsKey(b))
            return;
        if (!zones[a].Neighbours.Contains(b)) zones[a].Neighbours.Add(b);
        if (!zones[b].Neighbours.Contains(a)) zones[b].Neighbours.Add(a);
    }

    private void CreateArmies()
    {
        AddArmy("DK-ARMY", "Den danske hær", CampaignNation.Denmark, "DK-SJ", 38000);
        AddArmy("SN-ARMY", "Svensk-norsk feltstyrke", CampaignNation.SwedenNorway, "SN-SCA", 42000);
        AddArmy("PR-ARMY", "Preussisk hær", CampaignNation.Prussia, "PR-HOL", 62000);
        AddArmy("AT-ARMY", "Østrigsk korps", CampaignNation.Austria, "AT-BOH", 52000);
        AddArmy("FR-ARMY", "Fransk feltarmé", CampaignNation.France, "FR-PAR", 85000);
        AddArmy("UK-ARMY", "Britisk feltstyrke", CampaignNation.UnitedKingdom, "UK-S", 45000);
        AddArmy("RU-ARMY", "Russisk vestarmé", CampaignNation.Russia, "RU-POL", 90000);
        AddArmy("NL-ARMY", "Nederlandsk feltstyrke", CampaignNation.Netherlands, "NL-HOL", 25000);
        AddArmy("HA-ARMY", "Hannoveransk hær", CampaignNation.Hanover, "HA-E", 30000);
        AddArmy("ME-ARMY", "Mecklenburgsk kontingent", CampaignNation.Mecklenburg, "ME-S", 12000);
        AddArmy("DE-ARMY", "Tysk forbundskontingent", CampaignNation.GermanConfederationOther, "DE-C", 40000);
    }

    private void AddArmy(string id, string name, CampaignNation nation, string zoneId, int strength)
    {
        if (!zones.TryGetValue(zoneId, out Zone zone))
            return;

        GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        token.name = "ARMY_" + id;
        token.transform.position = ZoneWorldPosition(zone) + new Vector3(0f, 1.25f, 0f);
        token.transform.localScale = new Vector3(2.3f, 0.65f, 2.3f);
        token.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.Lerp(GetNationColor(nation), Color.white, 0.35f), "Army_" + id);
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
        float pan = 32f * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) p.z += pan;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) p.z -= pan;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) p.x -= pan;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) p.x += pan;
        p.x = Mathf.Clamp(p.x, -78f, 78f);
        p.z = Mathf.Clamp(p.z, -52f, 52f);
        campaignCamera.transform.position = p;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
            campaignCamera.orthographicSize = Mathf.Clamp(campaignCamera.orthographicSize - wheel * 4f, 22f, 78f);
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
                    "CAMPAIGN-MOVE|Army={0}|From={1}|To={2}|Accepted=False|Reason=NotAdjacent",
                    selectedArmy.Id,
                    current.Id,
                    destination.Id));
                return;
            }

            selectedArmy.DestinationZoneId = destination.Id;
            selectedArmy.Progress = 0f;
            Debug.Log(string.Format(
                "CAMPAIGN-MOVE|Army={0}|From={1}|To={2}|Accepted=True",
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

            float travelHours = Mathf.Max(6f, Vector2.Distance(from.Position, to.Position) * 1.2f);
            float hoursPerRealSecond = 0.2f * campaignSpeed;
            army.Progress += Time.unscaledDeltaTime * hoursPerRealSecond / travelHours;

            Vector3 start = ZoneWorldPosition(from) + new Vector3(0f, 1.25f, 0f);
            Vector3 end = ZoneWorldPosition(to) + new Vector3(0f, 1.25f, 0f);
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
        return new Vector3(zone.Position.x, 0f, zone.Position.y);
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
        DrawZoneLabels();
    }

    private void DrawNationSelection()
    {
        float width = 430f;
        float height = 390f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, string.Empty);
        GUI.Box(new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, 34f), "VÆLG LAND — GRAND CAMPAIGN 1864", titleStyle);

        float y = panel.y + 54f;
        foreach (CampaignNation nation in playableNations)
        {
            if (GUI.Button(new Rect(panel.x + 35f, y, panel.width - 70f, 25f), NationLabel(nation)))
            {
                playerNation = nation;
                nationChosen = true;
                SelectFirstArmyForNation();
                Debug.Log("CAMPAIGN-NATION|Player=" + nation);
            }
            y += 28f;
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
        Rect bar = new Rect(230f, 8f, Mathf.Min(760f, Screen.width - 240f), 31f);
        GUI.Box(bar, string.Empty);

        string state = paused ? "PAUSE" : "x" + campaignSpeed.ToString("0");
        GUI.Label(new Rect(bar.x + 8f, bar.y + 6f, 310f, 20f), campaignTime.ToString("dd MMM yyyy HH:mm") + " | " + state + " | " + NationLabel(playerNation));

        float x = bar.x + 330f;
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
        Rect panel = new Rect(8f, 42f, 300f, 132f);
        GUI.Box(panel, string.Empty);

        string zoneText = selectedZone != null
            ? string.Format("Zone: {0} ({1})\nEjer: {2}\nNaboer: {3}", selectedZone.Name, selectedZone.Id, NationLabel(selectedZone.Owner), string.Join(", ", selectedZone.Neighbours))
            : "Zone: ingen";
        GUI.Box(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 62f), zoneText, smallStyle);

        string armyText = selectedArmy != null
            ? string.Format("Formation: {0}\nStyrke: {1:N0} | Position: {2}\nRMB på nabozone = marchordre", selectedArmy.Name, selectedArmy.Strength, selectedArmy.CurrentZoneId)
            : "Formation: ingen valgt";
        GUI.Box(new Rect(panel.x + 6f, panel.y + 72f, panel.width - 12f, 54f), armyText, smallStyle);
    }

    private void DrawZoneLabels()
    {
        if (campaignCamera == null)
            return;

        foreach (Zone zone in zones.Values)
        {
            Vector3 screen = campaignCamera.WorldToScreenPoint(ZoneWorldPosition(zone) + new Vector3(0f, 1.2f, 0f));
            if (screen.z <= 0f)
                continue;

            float y = Screen.height - screen.y;
            GUI.Label(new Rect(screen.x - 55f, y - 10f, 110f, 20f), zone.Name, zoneStyle);
        }
    }

    private static string NationLabel(CampaignNation nation)
    {
        switch (nation)
        {
            case CampaignNation.Denmark: return "Danmark";
            case CampaignNation.SwedenNorway: return "Sverige-Norge";
            case CampaignNation.Prussia: return "Preussen";
            case CampaignNation.Austria: return "Østrig";
            case CampaignNation.France: return "Frankrig";
            case CampaignNation.UnitedKingdom: return "Storbritannien";
            case CampaignNation.Russia: return "Rusland";
            case CampaignNation.Netherlands: return "Nederlandene";
            case CampaignNation.Hanover: return "Kongeriget Hannover";
            case CampaignNation.Mecklenburg: return "Mecklenburg";
            default: return "Øvrige tyske forbundsstater";
        }
    }

    private static Color GetNationColor(CampaignNation nation)
    {
        switch (nation)
        {
            case CampaignNation.Denmark: return new Color(0.62f, 0.18f, 0.18f);
            case CampaignNation.SwedenNorway: return new Color(0.22f, 0.45f, 0.67f);
            case CampaignNation.Prussia: return new Color(0.22f, 0.24f, 0.28f);
            case CampaignNation.Austria: return new Color(0.78f, 0.74f, 0.64f);
            case CampaignNation.France: return new Color(0.28f, 0.38f, 0.67f);
            case CampaignNation.UnitedKingdom: return new Color(0.58f, 0.20f, 0.26f);
            case CampaignNation.Russia: return new Color(0.34f, 0.52f, 0.42f);
            case CampaignNation.Netherlands: return new Color(0.80f, 0.42f, 0.16f);
            case CampaignNation.Hanover: return new Color(0.70f, 0.58f, 0.30f);
            case CampaignNation.Mecklenburg: return new Color(0.46f, 0.52f, 0.32f);
            default: return new Color(0.52f, 0.42f, 0.33f);
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
