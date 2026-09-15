using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Compact contextual zone information panel.
///
/// n6e selection contract:
/// 1) exact city marker -> canonical City.ZoneId,
/// 2) ordinary land click -> ZoneId stored on the actual visible/source polygon
///    containing the click,
/// 3) no nearest-centre/site ownership resolver is allowed for ordinary area
///    selection,
/// 4) no GrandCampaignZoneMarker collider fallback.
///
/// This guarantees that the Amt shown by the info panel is the Amt represented by
/// the polygon under the pointer. Historical accuracy of the polygon itself is a
/// separate geometry/data question handled by CampaignZoneOverlayV010N.
/// </summary>
[DefaultExecutionOrder(24000)]
public sealed class CampaignZoneInfoPanelV010N3 : MonoBehaviour
{
    private sealed class ZoneArea
    {
        public string ZoneId;
        public readonly List<Vector2> WorldPolygon = new List<Vector2>();
    }

    private const string ZoneOverlayRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const float CampaignPlaneY = 0.74f;

    private readonly List<ZoneArea> zoneAreas = new List<ZoneArea>();
    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool zoneGeometryReady;
    private bool polygonQaLogged;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle mutedStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZONE_INFO_CURRENT");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneInfoPanelV010N3>();
    }

    private void Update()
    {
        if (!zoneGeometryReady)
            TryLoadZoneAreas();

        if (zoneGeometryReady && !polygonQaLogged)
        {
            polygonQaLogged = true;
            ValidatePolygonCoverage(out int cityCorrect, out int centreCorrect);
            Debug.Log(
                CampaignBuildInfo.LogTag +
                "|ZoneOwnershipResolver=True" +
                "|Mode=VISIBLE_POLYGON_METADATA" +
                "|CityInsideExpectedPolygon=" + cityCorrect + "/" + CampaignDenmark1851Registry.Cities.Length +
                "|ZoneCentreInsideExpectedPolygon=" + centreCorrect + "/" + CampaignDenmark1851Registry.Zones.Length +
                "|NearestSiteAreaFallback=False" +
                "|MarkerFallback=False");
        }

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
            CampaignBuildInfo.LogTag + "|ZoneInfo=True|Zone=" + selectedZone.Id +
            "|City=" + (selectedCity != null ? selectedCity.Id : "-") +
            "|Source=" + source);
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

        // City identity is canonical data and takes priority over area geometry.
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int h = 0; h < hits.Length; h++)
        {
            GrandCampaignCityMarker cityMarker = hits[h].collider.GetComponent<GrandCampaignCityMarker>();
            if (cityMarker == null)
                continue;

            city = FindCity(cityMarker.CityId);
            if (city == null)
                continue;

            zone = FindZone(city.ZoneId);
            if (zone != null)
            {
                source = "CanonicalCity";
                return true;
            }
        }

        if (!zoneGeometryReady || zoneAreas.Count == 0)
            return false;

        Plane campaignPlane = new Plane(Vector3.up, new Vector3(0f, CampaignPlaneY, 0f));
        if (!campaignPlane.Raycast(ray, out float enter))
            return false;

        Vector3 world = ray.GetPoint(enter);
        Vector2 worldPoint = new Vector2(world.x, world.z);

        // The polygon is authoritative for area selection in n6e. This is the key
        // rule: what the player sees and what the panel reports are the same area.
        for (int i = 0; i < zoneAreas.Count; i++)
        {
            ZoneArea area = zoneAreas[i];
            if (!PointInPolygon(worldPoint, area.WorldPolygon))
                continue;

            zone = FindZone(area.ZoneId);
            if (zone == null)
                continue;

            source = "VisibleZonePolygon";
            return true;
        }

        // No polygon means open sea or uncovered geometry. Do not guess a county.
        return false;
    }

    private void TryLoadZoneAreas()
    {
        GameObject root = GameObject.Find(ZoneOverlayRootName);
        if (root == null)
            return;

        zoneAreas.Clear();
        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);

        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            CampaignZoneOverlayMetadataV010N metadata = line.GetComponent<CampaignZoneOverlayMetadataV010N>();
            if (metadata == null || string.IsNullOrEmpty(metadata.ZoneId) || line.positionCount < 3)
                continue;

            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);

            ZoneArea area = new ZoneArea { ZoneId = metadata.ZoneId };
            for (int p = 0; p < positions.Length; p++)
                area.WorldPolygon.Add(new Vector2(positions[p].x, positions[p].z));

            if (area.WorldPolygon.Count >= 3)
                zoneAreas.Add(area);
        }

        zoneGeometryReady = zoneAreas.Count > 0;
        if (zoneGeometryReady)
        {
            Debug.Log(
                CampaignBuildInfo.LogTag +
                "|ZoneInfoGeometry=True|PolygonParts=" + zoneAreas.Count +
                "|Resolver=VisiblePolygonMetadata|MarkerFallback=False");
        }
    }

    private void ValidatePolygonCoverage(out int cityCorrect, out int zoneCentreCorrect)
    {
        cityCorrect = 0;
        zoneCentreCorrect = 0;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            Vector3 world = CampaignGeoProjection.Project(city.Longitude, city.Latitude, CampaignPlaneY);
            if (PointInsideExpectedZone(new Vector2(world.x, world.z), city.ZoneId))
                cityCorrect++;
        }

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            Vector3 world = CampaignGeoProjection.Project(zone.Longitude, zone.Latitude, CampaignPlaneY);
            if (PointInsideExpectedZone(new Vector2(world.x, world.z), zone.Id))
                zoneCentreCorrect++;
        }
    }

    private bool PointInsideExpectedZone(Vector2 point, string expectedZoneId)
    {
        for (int i = 0; i < zoneAreas.Count; i++)
        {
            ZoneArea area = zoneAreas[i];
            if (area.ZoneId != expectedZoneId)
                continue;
            if (PointInPolygon(point, area.WorldPolygon))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return false;

        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool yCross = (pi.y > point.y) != (pj.y > point.y);
            if (yCross)
            {
                float denominator = pj.y - pi.y;
                if (Mathf.Abs(denominator) > 0.0000001f)
                {
                    float xCross = (pj.x - pi.x) * (point.y - pi.y) / denominator + pi.x;
                    if (point.x < xCross)
                        inside = !inside;
                }
            }
            j = i;
        }
        return inside;
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
            CampaignDenmark1851Registry.CityDef cityDef = cities[i];
            if (cityDef.ZoneId != zoneId)
                continue;

            total++;
            population += cityDef.Population1850;
            names.Add(cityDef.Name);

            if (cityDef.Tier == CampaignDenmark1851Registry.CityTier.A) tierA++;
            else if (cityDef.Tier == CampaignDenmark1851Registry.CityTier.B) tierB++;
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

        if (visible && new Rect(8f, 48f, 310f, selectedCity != null ? 186f : 162f).Contains(mouse))
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

        float height = selectedCity != null ? 186f : 162f;
        Rect panel = new Rect(8f, 48f, 310f, height);
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

        GUI.Label(new Rect(panel.x + 10f, panel.y + 51f, panel.width - 20f, 94f), summary, bodyStyle);

        if (selectedCity != null)
        {
            string buildRule = selectedCity.AllowsHeavyMilitaryConstruction
                ? "tung militær udbygning mulig efter øvrige krav"
                : "ingen fri tung militær udbygning";
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 146f, panel.width - 20f, 34f),
                "Valgt by: " + selectedCity.Name + " [" + selectedCity.Tier + "] · " +
                selectedCity.Population1850.ToString("N0") + " indb. · " + buildRule,
                mutedStyle);
        }
    }
}
