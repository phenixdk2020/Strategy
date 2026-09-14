using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 compact contextual zone information panel.
///
/// v10n5 selection priority:
/// 1) exact city marker (city identity is authoritative),
/// 2) actual visible zone polygon under the mouse,
/// 3) zone-centre marker only as a fallback if polygon resolution fails.
///
/// This prevents a nearby zone-centre collider from reporting Hjørring/Aalborg
/// when the mouse is visibly inside the neighbouring Thisted/Viborg polygon.
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

    private readonly List<ZoneArea> zoneAreas = new List<ZoneArea>();
    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool zoneGeometryReady;

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

        if (GrandCampaignBootstrap.Instance == null || Camera.main == null)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (IsUiArea(guiMouse))
            return;

        if (TryResolveSelection(Camera.main, out CampaignDenmark1851Registry.ZoneDef zone, out CampaignDenmark1851Registry.CityDef city))
        {
            selectedZone = zone;
            selectedCity = city;
            visible = true;

            if (!CampaignHudStateV010N2.HudVisible)
                CampaignHudStateV010N2.ToggleHud();

            // The large legacy INFO boxes stay closed during normal map clicking.
            if (CampaignHudStateV010N2.SelectionEnabled)
                CampaignHudStateV010N2.ToggleSelectionPanel();

            Debug.Log(
                CampaignBuildInfo.LogTag + "|ZoneInfo=True|Zone=" + selectedZone.Id +
                "|City=" + (selectedCity != null ? selectedCity.Id : "-") +
                "|Source=PolygonFirstMapClick");
        }
    }

    private bool TryResolveSelection(
        Camera camera,
        out CampaignDenmark1851Registry.ZoneDef zone,
        out CampaignDenmark1851Registry.CityDef city)
    {
        zone = null;
        city = null;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        GrandCampaignZoneMarker fallbackZoneMarker = null;

        // City markers remain authoritative because City.ZoneId is canonical data.
        // Zone-centre markers are remembered only as a last-resort fallback; they
        // must not override the area polygon under the mouse.
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int h = 0; h < hits.Length; h++)
        {
            RaycastHit hit = hits[h];
            GrandCampaignCityMarker cityMarker = hit.collider.GetComponent<GrandCampaignCityMarker>();
            if (cityMarker != null)
            {
                city = FindCity(cityMarker.CityId);
                if (city != null)
                {
                    zone = FindZone(city.ZoneId);
                    return zone != null;
                }
            }

            if (fallbackZoneMarker == null)
                fallbackZoneMarker = hit.collider.GetComponent<GrandCampaignZoneMarker>();
        }

        if (zoneGeometryReady && zoneAreas.Count > 0)
        {
            Plane campaignPlane = new Plane(Vector3.up, new Vector3(0f, 0.74f, 0f));
            if (campaignPlane.Raycast(ray, out float enter))
            {
                Vector3 world = ray.GetPoint(enter);
                Vector2 point = new Vector2(world.x, world.z);

                for (int i = 0; i < zoneAreas.Count; i++)
                {
                    ZoneArea area = zoneAreas[i];
                    if (!PointInPolygon(point, area.WorldPolygon))
                        continue;

                    zone = FindZone(area.ZoneId);
                    if (zone != null)
                        return true;
                }
            }
        }

        // Fallback only. This preserves the ability to click an isolated zone-centre
        // marker if polygon geometry is temporarily unavailable during startup.
        if (fallbackZoneMarker != null)
        {
            zone = FindZone(fallbackZoneMarker.ZoneId);
            return zone != null;
        }

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
                CampaignBuildInfo.LogTag + "|ZoneInfoGeometry=True|PolygonParts=" + zoneAreas.Count +
                "|Resolver=CityThenPointInActualOverlayPolygonThenMarkerFallback");
        }
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
