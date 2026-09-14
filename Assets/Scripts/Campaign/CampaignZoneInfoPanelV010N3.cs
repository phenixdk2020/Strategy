using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n3 compact contextual zone information panel.
/// Clicking a zone marker, city marker, or Danish land area resolves a ZONE-REG-01
/// zone and opens a small panel in the upper-left corner.
/// </summary>
[DefaultExecutionOrder(24000)]
public sealed class CampaignZoneInfoPanelV010N3 : MonoBehaviour
{
    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";

    private readonly List<List<Vector2>> landRings = new List<List<Vector2>>();
    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool geographyReady;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle mutedStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZONE_INFO_v000010n3");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneInfoPanelV010N3>();
    }

    private void Update()
    {
        if (!geographyReady)
            TryLoadLandRings();

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

            // v10n3 compact card replaces the large legacy INFO panel during normal
            // map selection. INFO/F2 can still reopen the detailed legacy panel.
            if (CampaignHudStateV010N2.SelectionEnabled)
                CampaignHudStateV010N2.ToggleSelectionPanel();

            Debug.Log(
                CampaignBuildInfo.LogTag + "|ZoneInfo=True|Zone=" + selectedZone.Id +
                "|City=" + (selectedCity != null ? selectedCity.Id : "-") +
                "|Source=MapClick");
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
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
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

            GrandCampaignZoneMarker zoneMarker = hit.collider.GetComponent<GrandCampaignZoneMarker>();
            if (zoneMarker != null)
            {
                zone = FindZone(zoneMarker.ZoneId);
                return zone != null;
            }
        }

        // Area selection uses the same current Denmark land scaffold as v10n2.
        // The nearest-centre rule matches the prototype zone partition.
        Plane campaignPlane = new Plane(Vector3.up, new Vector3(0f, 0.18f, 0f));
        if (!campaignPlane.Raycast(ray, out float enter))
            return false;

        Vector3 world = ray.GetPoint(enter);
        Vector2 lonLat = CampaignGeoProjection.Unproject(world);
        if (!PointInAnyLandRing(lonLat))
            return false;

        zone = FindNearestZone(lonLat);
        return zone != null;
    }

    private void TryLoadLandRings()
    {
        GameObject root = GameObject.Find(GeographyRootName);
        if (root == null)
            return;

        landRings.Clear();
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (!child.name.StartsWith("DNK_LandPart_", StringComparison.Ordinal))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Vector3[] vertices = filter.sharedMesh.vertices;
            if (vertices == null || vertices.Length < 3)
                continue;

            List<Vector2> ring = new List<Vector2>(vertices.Length);
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 world = child.TransformPoint(vertices[v]);
                ring.Add(CampaignGeoProjection.Unproject(world));
            }

            if (ring.Count >= 3)
                landRings.Add(ring);
        }

        geographyReady = landRings.Count > 0;
    }

    private bool PointInAnyLandRing(Vector2 point)
    {
        for (int r = 0; r < landRings.Count; r++)
        {
            if (PointInPolygon(point, landRings[r]))
                return true;
        }
        return false;
    }

    private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool crosses = ((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) /
                Mathf.Max(0.0000001f, pj.y - pi.y) + pi.x);
            if (crosses)
                inside = !inside;
            j = i;
        }
        return inside;
    }

    private static CampaignDenmark1851Registry.ZoneDef FindNearestZone(Vector2 lonLat)
    {
        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        CampaignDenmark1851Registry.ZoneDef best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < zones.Length; i++)
        {
            Vector2 p = new Vector2(zones[i].Longitude, zones[i].Latitude);
            float distance = (p - lonLat).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = zones[i];
            }
        }

        return best;
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

        // If the user explicitly opens the old detailed INFO panel, let that panel
        // replace the compact card rather than drawing both on top of each other.
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
