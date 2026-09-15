using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6f compact Amt information panel.
///
/// Selection contract:
/// 1) exact city hit -> canonical City.ZoneId,
/// 2) ordinary land hit -> CampaignHistoricalAmtOverlayV010N6F ownership,
/// 3) the same parish ownership field therefore drives both visible Amt lines
///    and the Amt shown in this panel,
/// 4) no zone-centre collider / nearest-centre fallback is allowed.
/// </summary>
[DefaultExecutionOrder(24100)]
public sealed class CampaignZoneInfoPanelV010N6F : MonoBehaviour
{
    private const float CampaignPlaneY = 0.74f;

    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool oldPanelDisabled;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle mutedStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6F>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZONE_INFO_10N6F");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneInfoPanelV010N6F>();
    }

    private void Awake()
    {
        DisableLegacyPanel();
    }

    private void Update()
    {
        if (!oldPanelDisabled)
            DisableLegacyPanel();

        if (GrandCampaignBootstrap.Instance == null || Camera.main == null)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (IsUiArea(guiMouse))
            return;

        if (!TryResolveSelection(
                Camera.main,
                out CampaignDenmark1851Registry.ZoneDef zone,
                out CampaignDenmark1851Registry.CityDef city,
                out string source))
            return;

        selectedZone = zone;
        selectedCity = city;
        visible = true;

        if (!CampaignHudStateV010N2.HudVisible)
            CampaignHudStateV010N2.ToggleHud();

        if (CampaignHudStateV010N2.SelectionEnabled)
            CampaignHudStateV010N2.ToggleSelectionPanel();

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|ZoneInfo=True|Zone=" + selectedZone.Id +
            "|City=" + (selectedCity != null ? selectedCity.Id : "-") +
            "|Source=" + source +
            "|Resolver=SharedParishOwnershipField");
    }

    private void DisableLegacyPanel()
    {
        CampaignZoneInfoPanelV010N3 legacy = Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>();
        if (legacy != null && legacy.enabled)
            legacy.enabled = false;

        oldPanelDisabled = legacy != null;
    }

    private bool TryResolveSelection(
        Camera camera,
        out CampaignDenmark1851Registry.ZoneDef zone,
        out CampaignDenmark1851Registry.CityDef city,
        out string source)
    {
        zone = null;
        city = null;
        source = "None";

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        // Exact city identity always wins over area ownership.
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            GrandCampaignCityMarker marker = hits[i].collider.GetComponent<GrandCampaignCityMarker>();
            if (marker == null)
                continue;

            city = FindCity(marker.CityId);
            if (city == null)
                continue;

            zone = FindZone(city.ZoneId);
            if (zone == null)
                continue;

            source = "CanonicalCity";
            return true;
        }

        // Never guess an Amt before the n6f historical ownership field is ready.
        if (!CampaignHistoricalAmtOverlayV010N6F.IsReady)
            return false;

        Plane campaignPlane = new Plane(Vector3.up, new Vector3(0f, CampaignPlaneY, 0f));
        if (!campaignPlane.Raycast(ray, out float enter))
            return false;

        Vector3 world = ray.GetPoint(enter);
        if (!CampaignHistoricalAmtOverlayV010N6F.TryResolveWorld(world, out string zoneId))
            return false;

        zone = FindZone(zoneId);
        if (zone == null)
            return false;

        source = "ParishOwnershipField";
        return true;
    }

    private static CampaignDenmark1851Registry.ZoneDef FindZone(string zoneId)
    {
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
            if (zones[i].Id == zoneId)
                return zones[i];
        return null;
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i].Id == cityId)
                return cities[i];
        return null;
    }

    private static void GetZoneCityStats(
        string zoneId,
        out int total,
        out int tierA,
        out int tierB,
        out int tierC,
        out int population,
        out string cityNames)
    {
        total = 0;
        tierA = 0;
        tierB = 0;
        tierC = 0;
        population = 0;
        List<string> names = new List<string>();

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            if (city.ZoneId != zoneId)
                continue;

            total++;
            population += city.Population1850;
            names.Add(city.Name);

            if (city.Tier == CampaignDenmark1851Registry.CityTier.A) tierA++;
            else if (city.Tier == CampaignDenmark1851Registry.CityTier.B) tierB++;
            else tierC++;
        }

        cityNames = names.Count == 0 ? "—" : string.Join(", ", names.ToArray());
    }

    private static string JoinOrDash(string[] items)
    {
        return items == null || items.Length == 0 ? "—" : string.Join(", ", items);
    }

    private bool IsUiArea(Vector2 mouse)
    {
        if (mouse.y <= 44f)
            return true;

        if (visible && new Rect(8f, 48f, 330f, selectedCity != null ? 190f : 166f).Contains(mouse))
            return true;

        return false;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = Color.white;

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        bodyStyle.normal.textColor = new Color(0.94f, 0.94f, 0.90f);

        mutedStyle = new GUIStyle(bodyStyle);
        mutedStyle.fontSize = 9;
        mutedStyle.normal.textColor = new Color(0.72f, 0.77f, 0.78f);
    }

    private void OnGUI()
    {
        if (!visible || selectedZone == null || !CampaignHudStateV010N2.HudVisible)
            return;

        if (CampaignHudStateV010N2.SelectionVisible)
            return;

        EnsureStyles();
        GetZoneCityStats(
            selectedZone.Id,
            out int totalCities,
            out int tierA,
            out int tierB,
            out int tierC,
            out int population,
            out string cityNames);

        float height = selectedCity != null ? 190f : 166f;
        Rect panel = new Rect(8f, 48f, 330f, height);
        GUI.Box(panel, string.Empty, panelStyle);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, panel.width - 46f, 22f), selectedZone.Name, titleStyle);
        if (GUI.Button(new Rect(panel.x + panel.width - 30f, panel.y + 6f, 22f, 22f), "×"))
        {
            visible = false;
            return;
        }

        GUI.Label(
            new Rect(panel.x + 10f, panel.y + 31f, panel.width - 20f, 18f),
            selectedZone.Id + "  |  Kongeriget Danmark",
            mutedStyle);

        string summary =
            "Byer: " + totalCities + "   A/B/C: " + tierA + "/" + tierB + "/" + tierC +
            "   Bybef. 1850: " + population.ToString("N0") +
            "\n" + cityNames +
            "\nLand: " + JoinOrDash(selectedZone.LandNeighbours) +
            "\nFærge: " + JoinOrDash(selectedZone.FerryNeighbours);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 51f, panel.width - 20f, 96f), summary, bodyStyle);

        if (selectedCity != null)
        {
            string buildRule = selectedCity.AllowsHeavyMilitaryConstruction
                ? "tung militær udbygning mulig efter øvrige krav"
                : "ingen fri tung militær udbygning";

            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 149f, panel.width - 20f, 35f),
                "Valgt by: " + selectedCity.Name + " [" + selectedCity.Tier + "] · " +
                selectedCity.Population1850.ToString("N0") + " indb. · " + buildRule,
                mutedStyle);
        }
    }
}
