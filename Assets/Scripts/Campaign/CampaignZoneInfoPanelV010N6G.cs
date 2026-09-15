using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// n6g compact Amt/city info panel.
/// City clicks use canonical City.ZoneId; ordinary land clicks use the exact same
/// source parish polygons that draw the visible Amt boundaries.
/// </summary>
[DefaultExecutionOrder(24400)]
public sealed class CampaignZoneInfoPanelV010N6G : MonoBehaviour
{
    private const float CampaignPlaneY = 0.74f;

    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool legacyDisabled;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle mutedStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6G>() != null) return;
        GameObject go = new GameObject("PROJECT1864_ZONE_INFO_10N6G");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneInfoPanelV010N6G>();
    }

    private void Update()
    {
        DisableLegacyPanels();

        if (!CampaignHistoricalAmtPolygonsV010N6G.IsReady || GrandCampaignBootstrap.Instance == null || Camera.main == null)
            return;

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (IsUiArea(guiMouse)) return;

        if (!TryResolveSelection(Camera.main, out CampaignDenmark1851Registry.ZoneDef zone,
                out CampaignDenmark1851Registry.CityDef city, out string source))
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
            "|Resolver=SourceParishPolygon");
    }

    private void DisableLegacyPanels()
    {
        if (legacyDisabled) return;

        CampaignZoneInfoPanelV010N6F oldF = Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6F>();
        if (oldF != null) oldF.enabled = false;

        CampaignZoneInfoPanelV010N3 oldN3 = Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>();
        if (oldN3 != null) oldN3.enabled = false;

        legacyDisabled = true;
        Debug.Log(CampaignBuildInfo.LogTag + "|ZoneInfoN6G=True|LegacyPanelsDisabled=True");
    }

    private static bool TryResolveSelection(
        Camera camera,
        out CampaignDenmark1851Registry.ZoneDef zone,
        out CampaignDenmark1851Registry.CityDef city,
        out string source)
    {
        zone = null;
        city = null;
        source = "None";

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });

        for (int h = 0; h < hits.Length; h++)
        {
            GrandCampaignCityMarker marker = hits[h].collider.GetComponent<GrandCampaignCityMarker>();
            if (marker == null || string.IsNullOrEmpty(marker.CityId)) continue;

            city = FindCity(marker.CityId);
            if (city == null) continue;
            zone = FindZone(city.ZoneId);
            if (zone == null) continue;

            source = "CanonicalCity";
            return true;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, CampaignPlaneY, 0f));
        if (!plane.Raycast(ray, out float enter)) return false;

        Vector3 world = ray.GetPoint(enter);
        if (!CampaignHistoricalAmtPolygonsV010N6G.TryResolveWorld(world, out string zoneId))
            return false;

        zone = FindZone(zoneId);
        if (zone == null) return false;

        source = "SourceParishPolygon";
        return true;
    }

    private static CampaignDenmark1851Registry.ZoneDef FindZone(string zoneId)
    {
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
            if (zones[i] != null && zones[i].Id == zoneId) return zones[i];
        return null;
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i] != null && cities[i].Id == cityId) return cities[i];
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
        total = tierA = tierB = tierC = population = 0;
        List<string> names = new List<string>();

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef c = cities[i];
            if (c == null || c.ZoneId != zoneId) continue;
            total++;
            population += c.Population1850;
            names.Add(c.Name);
            if (c.Tier == CampaignDenmark1851Registry.CityTier.A) tierA++;
            else if (c.Tier == CampaignDenmark1851Registry.CityTier.B) tierB++;
            else tierC++;
        }

        cityNames = names.Count == 0 ? "—" : string.Join(", ", names.ToArray());
    }

    private bool IsUiArea(Vector2 mouse)
    {
        if (mouse.y <= 44f) return true;
        if (visible && new Rect(8f, 48f, 310f, selectedCity != null ? 186f : 162f).Contains(mouse)) return true;
        return false;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
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
        if (!visible || selectedZone == null || !CampaignHudStateV010N2.HudVisible) return;
        if (CampaignHudStateV010N2.SelectionVisible) return;

        EnsureStyles();
        GetZoneCityStats(selectedZone.Id, out int total, out int a, out int b, out int c,
            out int population, out string cityNames);

        float height = selectedCity != null ? 186f : 162f;
        Rect panel = new Rect(8f, 48f, 310f, height);
        GUI.Box(panel, string.Empty, panelStyle);

        GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, panel.width - 46f, 22f), selectedZone.Name, titleStyle);
        if (GUI.Button(new Rect(panel.x + panel.width - 30f, panel.y + 6f, 22f, 22f), "×"))
        {
            visible = false;
            return;
        }

        GUI.Label(new Rect(panel.x + 10f, panel.y + 31f, panel.width - 20f, 18f),
            selectedZone.Id + "  |  Kongeriget Danmark", mutedStyle);

        string summary =
            "Byer: " + total + "   A/B/C: " + a + "/" + b + "/" + c +
            "   Bybef. 1850: " + population.ToString("N0") +
            "\n" + cityNames +
            "\nLand: " + JoinOrDash(selectedZone.LandNeighbours) +
            "\nFærge: " + JoinOrDash(selectedZone.FerryNeighbours);
        GUI.Label(new Rect(panel.x + 10f, panel.y + 51f, panel.width - 20f, 94f), summary, bodyStyle);

        if (selectedCity != null)
        {
            string buildRule = selectedCity.AllowsHeavyMilitaryConstruction
                ? "tung militær udbygning mulig efter øvrige krav"
                : "ingen fri tung militær udbygning";
            GUI.Label(new Rect(panel.x + 10f, panel.y + 146f, panel.width - 20f, 34f),
                "Valgt by: " + selectedCity.Name + " [" + selectedCity.Tier + "] · " +
                selectedCity.Population1850.ToString("N0") + " indb. · " + buildRule, mutedStyle);
        }
    }

    private static string JoinOrDash(string[] items)
    {
        return items == null || items.Length == 0 ? "—" : string.Join(", ", items);
    }
}
