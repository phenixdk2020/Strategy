using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Compact contextual zone information panel.
///
/// n6d selection contract:
/// 1) exact city marker -> canonical City.ZoneId,
/// 2) confirm the click is inside current Denmark zone geometry,
/// 3) resolve ownership from canonical city + zone-centre influence sites,
/// 4) never substitute a nearby zone-centre collider for an area click.
///
/// n6d deliberately embeds the ownership resolver in this file so Unity does not
/// depend on a separately imported resolver source file during compilation.
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
    private const string EmbeddedResolverMode = "CANONICAL_CITY_PLUS_ZONE_CENTRE_NEAREST_SITE_EMBEDDED";

    private readonly List<ZoneArea> zoneAreas = new List<ZoneArea>();
    private CampaignDenmark1851Registry.ZoneDef selectedZone;
    private CampaignDenmark1851Registry.CityDef selectedCity;
    private bool visible;
    private bool zoneGeometryReady;
    private bool resolverQaLogged;

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

        if (zoneGeometryReady && !resolverQaLogged)
        {
            resolverQaLogged = true;
            ValidateEmbeddedCanonicalSites(out int cityCorrect, out int centreCorrect);
            Debug.Log(
                CampaignBuildInfo.LogTag +
                "|ZoneOwnershipResolver=True" +
                "|Mode=" + EmbeddedResolverMode +
                "|CityCanonical=" + cityCorrect + "/" + CampaignDenmark1851Registry.Cities.Length +
                "|ZoneCentreCanonical=" + centreCorrect + "/" + CampaignDenmark1851Registry.Zones.Length +
                "|MarkerFallback=False" +
                "|CompileDependency=Embedded");
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

        // Normal zone clicking must never reopen the large legacy selection boxes.
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

        // A city hit is exact canonical data and always wins.
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

        // The zone polygons also act as a Denmark/land guard. Open sea must not
        // receive the nearest Amt simply because an influence site is nearby.
        bool insideZoneGeometry = false;
        for (int i = 0; i < zoneAreas.Count; i++)
        {
            if (!PointInPolygon(worldPoint, zoneAreas[i].WorldPolygon))
                continue;

            insideZoneGeometry = true;
            break;
        }

        if (!insideZoneGeometry)
            return false;

        if (!TryResolveCanonicalOwnershipWorld(world, out string resolvedZoneId))
            return false;

        zone = FindZone(resolvedZoneId);
        if (zone == null)
            return false;

        source = "CanonicalMultiSiteOwnershipEmbedded";
        return true;
    }

    /// <summary>
    /// n6d embedded resolver. Uses the same canonical city and zone-centre sites
    /// as the prototype multi-site zone partition and therefore has no dependency
    /// on CampaignZoneOwnershipResolverV010N6C being imported as a separate file.
    /// </summary>
    private static bool TryResolveCanonicalOwnershipWorld(Vector3 worldPoint, out string zoneId)
    {
        Vector2 geo = CampaignGeoProjection.Unproject(worldPoint);
        return TryResolveCanonicalOwnershipGeo(geo, out zoneId);
    }

    private static bool TryResolveCanonicalOwnershipGeo(Vector2 geoPoint, out string zoneId)
    {
        zoneId = null;
        float bestDistance = float.MaxValue;

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            Vector2 site = new Vector2(zone.Longitude, zone.Latitude);
            float distance = (site - geoPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                zoneId = zone.Id;
            }
        }

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef cityDef = cities[i];
            Vector2 site = new Vector2(cityDef.Longitude, cityDef.Latitude);
            float distance = (site - geoPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                zoneId = cityDef.ZoneId;
            }
        }

        return !string.IsNullOrEmpty(zoneId);
    }

    private static void ValidateEmbeddedCanonicalSites(out int cityCorrect, out int zoneCentreCorrect)
    {
        cityCorrect = 0;
        zoneCentreCorrect = 0;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef cityDef = cities[i];
            if (TryResolveCanonicalOwnershipGeo(
                    new Vector2(cityDef.Longitude, cityDef.Latitude),
                    out string owner) && owner == cityDef.ZoneId)
                cityCorrect++;
        }

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zoneDef = zones[i];
            if (TryResolveCanonicalOwnershipGeo(
                    new Vector2(zoneDef.Longitude, zoneDef.Latitude),
                    out string owner) && owner == zoneDef.Id)
                zoneCentreCorrect++;
        }
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
                "|Resolver=CityThenCanonicalMultiSiteEmbedded|MarkerFallback=False");
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
