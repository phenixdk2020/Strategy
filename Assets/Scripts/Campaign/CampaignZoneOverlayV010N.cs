using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign3 zone overlay.
///
/// v00.00.10n introduced the visual zone-overlay architecture.
/// v00.00.10n2 used one global nearest-centre partition for the 20 zones.
/// v00.00.10n5 replaces the single-centre approximation with a MULTI-SITE
/// partition: every canonical 1850 market town plus each zone centre is an
/// influence site carrying its authoritative ZoneId. Cells belonging to the same
/// ZoneId form the visible prototype county area.
///
/// This guarantees much better regional placement and lets runtime QA verify that
/// all 68 canonical cities and all 20 zone centres fall inside their own zone.
///
/// IMPORTANT HISTORICAL GUARDRAIL:
/// Zone identities and city-to-zone relations are canonical ZONE-REG-01/CITY-REG-01
/// data, but these polygons remain prototype geometry. They are NOT a claim about
/// exact 1851 county boundaries. Production geometry remains targeted at DigDag
/// historical Amt/Region polygons.
/// </summary>
[DefaultExecutionOrder(21000)]
public sealed class CampaignZoneOverlayV010N : MonoBehaviour
{
    public const string GeometryMode = "PROTOTYPE_LAND_CLIPPED_MULTI_SITE_VORONOI";
    public const string IntendedHistoricalSource = "DigDag - Amt og Region";

    private sealed class InfluenceSite
    {
        public string ZoneId;
        public string Label;
        public Vector2 Point;
    }

    private sealed class GeneratedPart
    {
        public string ZoneId;
        public readonly List<Vector2> Polygon = new List<Vector2>();
    }

    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";
    private const float OverlayY = 0.74f;
    private const float BoundaryWidth = 0.040f;
    private const float MinPolygonArea = 0.000010f;
    private const float Epsilon = 0.000001f;
    private const float DuplicateSiteDistance = 0.0025f;

    private readonly List<GameObject> overlayObjects = new List<GameObject>();
    private GameObject overlayRoot;
    private Material boundaryMaterial;
    private bool built;
    private bool visible = true;
    private GUIStyle badgeStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneOverlayV010N>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZoneOverlay_CURRENT");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneOverlayV010N>();
    }

    private void Awake()
    {
        boundaryMaterial = CreateBoundaryMaterial();
    }

    private void Update()
    {
        if (!built && GrandCampaignBootstrap.Instance != null)
            TryBuildOverlay();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            visible = !visible;
            if (overlayRoot != null)
                overlayRoot.SetActive(visible);

            Debug.Log(
                CampaignBuildInfo.LogTag + "|ZoneOverlay=" + visible +
                "|Toggle=Z|Geometry=" + GeometryMode);
        }
    }

    private void TryBuildOverlay()
    {
        List<List<Vector2>> landRings = ReadLandRingsFromCampaignGeography();
        if (landRings.Count == 0)
            return;

        List<InfluenceSite> sites = BuildInfluenceSites();
        if (sites.Count == 0)
            return;

        built = true;
        overlayRoot = new GameObject("ZONE_OVERLAY_1851_LAND_CLIPPED");
        DontDestroyOnLoad(overlayRoot);

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        Dictionary<string, CampaignDenmark1851Registry.ZoneDef> zoneById =
            new Dictionary<string, CampaignDenmark1851Registry.ZoneDef>();
        Dictionary<string, int> zonePartCounters = new Dictionary<string, int>();
        for (int i = 0; i < zones.Length; i++)
        {
            zoneById[zones[i].Id] = zones[i];
            zonePartCounters[zones[i].Id] = 0;
        }

        HashSet<string> zonesWithGeometry = new HashSet<string>();
        List<GeneratedPart> generatedParts = new List<GeneratedPart>();
        int polygonParts = 0;
        int segments = 0;
        int tinyPartsSkipped = 0;

        for (int s = 0; s < sites.Count; s++)
        {
            InfluenceSite site = sites[s];
            if (!zoneById.TryGetValue(site.ZoneId, out CampaignDenmark1851Registry.ZoneDef zone))
                continue;

            for (int r = 0; r < landRings.Count; r++)
            {
                List<Vector2> polygon = new List<Vector2>(landRings[r]);

                // One global multi-site partition. Sites belonging to the same ZoneId
                // may divide that county internally, but those internal cell edges are
                // hidden by the shared-border renderer because both owners have the
                // same ZoneId. Between different ZoneIds the cells form the visible
                // prototype administrative divide.
                for (int o = 0; o < sites.Count; o++)
                {
                    if (o == s)
                        continue;

                    polygon = ClipToTargetHalfPlane(polygon, site.Point, sites[o].Point);
                    if (polygon.Count < 3)
                        break;
                }

                polygon = RemoveNearDuplicatePoints(polygon);
                if (polygon.Count < 3)
                    continue;

                float area = Mathf.Abs(SignedArea(polygon));
                if (area < MinPolygonArea)
                {
                    tinyPartsSkipped++;
                    continue;
                }

                int zonePart = zonePartCounters[zone.Id] + 1;
                zonePartCounters[zone.Id] = zonePart;
                CreateBoundary(zone, polygon, r + 1, zonePart, site.Label);

                GeneratedPart part = new GeneratedPart { ZoneId = zone.Id };
                part.Polygon.AddRange(polygon);
                generatedParts.Add(part);

                zonesWithGeometry.Add(zone.Id);
                polygonParts++;
                segments += polygon.Count;
            }
        }

        ValidateCanonicalAnchors(
            generatedParts,
            out int citiesCorrect,
            out int cityFailures,
            out int centresCorrect,
            out int centreFailures);

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Installed=True|Overlay=1851Zones" +
            "|Zones=" + zonesWithGeometry.Count +
            "|InfluenceSites=" + sites.Count +
            "|PolygonParts=" + polygonParts +
            "|BoundarySegments=" + segments +
            "|TinyPartsSkipped=" + tinyPartsSkipped +
            "|LandClipped=True" +
            "|AreaOverlap=FalseByMultiSiteVoronoiConstruction" +
            "|CityAnchorValidation=" + citiesCorrect + "/" + CampaignDenmark1851Registry.Cities.Length +
            "|CityAnchorFailures=" + cityFailures +
            "|ZoneCentreValidation=" + centresCorrect + "/" + zones.Length +
            "|ZoneCentreFailures=" + centreFailures +
            "|LandMask=CampaignDenmarkGeography_NaturalEarth50m" +
            "|Geometry=" + GeometryMode +
            "|HistoricalSourceTarget=DigDag_Amt_Region|Toggle=Z");
    }

    private static List<InfluenceSite> BuildInfluenceSites()
    {
        List<InfluenceSite> sites = new List<InfluenceSite>();

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            AddSiteIfDistinct(
                sites,
                zone.Id,
                "ZONECENTER_" + zone.Id,
                new Vector2(zone.Longitude, zone.Latitude));
        }

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            AddSiteIfDistinct(
                sites,
                city.ZoneId,
                "CITY_" + city.Id,
                new Vector2(city.Longitude, city.Latitude));
        }

        return sites;
    }

    private static void AddSiteIfDistinct(
        List<InfluenceSite> sites,
        string zoneId,
        string label,
        Vector2 point)
    {
        float sqrTolerance = DuplicateSiteDistance * DuplicateSiteDistance;
        for (int i = 0; i < sites.Count; i++)
        {
            InfluenceSite existing = sites[i];
            if (existing.ZoneId == zoneId && (existing.Point - point).sqrMagnitude <= sqrTolerance)
                return;
        }

        sites.Add(new InfluenceSite
        {
            ZoneId = zoneId,
            Label = label,
            Point = point
        });
    }

    private static void ValidateCanonicalAnchors(
        List<GeneratedPart> parts,
        out int citiesCorrect,
        out int cityFailures,
        out int centresCorrect,
        out int centreFailures)
    {
        citiesCorrect = 0;
        cityFailures = 0;
        centresCorrect = 0;
        centreFailures = 0;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            Vector2 point = new Vector2(city.Longitude, city.Latitude);
            if (PointIsInsideZone(parts, city.ZoneId, point))
            {
                citiesCorrect++;
            }
            else
            {
                cityFailures++;
                Debug.LogWarning(
                    CampaignBuildInfo.LogTag + "|ZoneAnchorMismatch=True|Type=City|City=" + city.Id +
                    "|ExpectedZone=" + city.ZoneId + "|Lon=" + city.Longitude.ToString("0.0000") +
                    "|Lat=" + city.Latitude.ToString("0.0000"));
            }
        }

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            Vector2 point = new Vector2(zone.Longitude, zone.Latitude);
            if (PointIsInsideZone(parts, zone.Id, point))
            {
                centresCorrect++;
            }
            else
            {
                centreFailures++;
                Debug.LogWarning(
                    CampaignBuildInfo.LogTag + "|ZoneAnchorMismatch=True|Type=ZoneCentre|Zone=" + zone.Id +
                    "|Lon=" + zone.Longitude.ToString("0.0000") +
                    "|Lat=" + zone.Latitude.ToString("0.0000"));
            }
        }
    }

    private static bool PointIsInsideZone(List<GeneratedPart> parts, string zoneId, Vector2 point)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            GeneratedPart part = parts[i];
            if (part.ZoneId != zoneId)
                continue;
            if (PointInPolygon(point, part.Polygon))
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
                if (Mathf.Abs(denominator) > Epsilon)
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

    /// <summary>
    /// Reads the already-rendered Denmark land parts instead of maintaining a
    /// second coastline dataset. This keeps the overlay coastline exactly aligned
    /// with CampaignDenmarkGeography as that scaffold evolves.
    /// </summary>
    private static List<List<Vector2>> ReadLandRingsFromCampaignGeography()
    {
        List<List<Vector2>> rings = new List<List<Vector2>>();
        GameObject root = GameObject.Find(GeographyRootName);
        if (root == null)
            return rings;

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

            ring = RemoveNearDuplicatePoints(ring);
            if (ring.Count >= 3)
                rings.Add(ring);
        }

        return rings;
    }

    private static List<Vector2> ClipToTargetHalfPlane(
        List<Vector2> input,
        Vector2 target,
        Vector2 other)
    {
        List<Vector2> output = new List<Vector2>();
        if (input == null || input.Count == 0)
            return output;

        Vector2 normal = other - target;
        if (normal.sqrMagnitude <= Epsilon * Epsilon)
            return new List<Vector2>(input);

        // Points closer to target than other satisfy:
        // dot(P, other-target) <= (|other|^2-|target|^2)/2
        float threshold = (other.sqrMagnitude - target.sqrMagnitude) * 0.5f;

        Vector2 previous = input[input.Count - 1];
        float previousValue = Vector2.Dot(previous, normal) - threshold;
        bool previousInside = previousValue <= Epsilon;

        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            float currentValue = Vector2.Dot(current, normal) - threshold;
            bool currentInside = currentValue <= Epsilon;

            if (currentInside != previousInside)
            {
                float denominator = previousValue - currentValue;
                float t = Mathf.Abs(denominator) < Epsilon
                    ? 0.5f
                    : previousValue / denominator;
                output.Add(Vector2.Lerp(previous, current, Mathf.Clamp01(t)));
            }

            if (currentInside)
                output.Add(current);

            previous = current;
            previousValue = currentValue;
            previousInside = currentInside;
        }

        return output;
    }

    private static List<Vector2> RemoveNearDuplicatePoints(List<Vector2> input)
    {
        List<Vector2> output = new List<Vector2>();
        if (input == null || input.Count == 0)
            return output;

        float sqrTolerance = Epsilon * Epsilon;
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 point = input[i];
            if (output.Count == 0 || (output[output.Count - 1] - point).sqrMagnitude > sqrTolerance)
                output.Add(point);
        }

        if (output.Count > 2 && (output[0] - output[output.Count - 1]).sqrMagnitude <= sqrTolerance)
            output.RemoveAt(output.Count - 1);

        return output;
    }

    private static float SignedArea(List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return 0f;

        float area = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    private void CreateBoundary(
        CampaignDenmark1851Registry.ZoneDef zone,
        List<Vector2> polygon,
        int landPart,
        int zonePart,
        string siteLabel)
    {
        GameObject go = new GameObject(
            "ZONE_BORDER_" + zone.Id + "_LAND_" + landPart + "_PART_" + zonePart);
        go.transform.SetParent(overlayRoot.transform, false);
        overlayObjects.Add(go);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = polygon.Count;
        line.widthMultiplier = BoundaryWidth;
        line.sharedMaterial = boundaryMaterial;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p = polygon[i];
            line.SetPosition(i, CampaignGeoProjection.Project(p.x, p.y, OverlayY));
        }

        go.AddComponent<CampaignZoneOverlayMetadataV010N>().Initialize(
            zone.Id,
            zone.Name,
            "MULTI_SITE:" + siteLabel,
            false,
            true,
            true);
    }

    private static Material CreateBoundaryMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_PROTOTYPE_MULTI_SITE",
            color = new Color(0.95f, 0.78f, 0.24f, 1.00f)
        };
        return material;
    }

    private void EnsureGuiStyle()
    {
        if (badgeStyle != null)
            return;

        badgeStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        badgeStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!CampaignHudStateV010N2.DebugVisible)
            return;

        EnsureGuiStyle();
        string state = visible ? "ON" : "OFF";
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 360f), Screen.height - 52f, 352f, 44f);
        GUI.Box(
            rect,
            CampaignBuildInfo.CurrentVersion + " | ZONES " + state + " | Z toggle\n" +
            "multi-site city+centre anchors · land-clipped · DigDag target",
            badgeStyle);
    }
}

public sealed class CampaignZoneOverlayMetadataV010N : MonoBehaviour
{
    public string ZoneId { get; private set; }
    public string ZoneName { get; private set; }
    public string GroupName { get; private set; }
    public bool HistoricallyExactGeometry { get; private set; }
    public bool LandClipped { get; private set; }
    public bool AreaOverlapFreeByConstruction { get; private set; }

    public void Initialize(
        string zoneId,
        string zoneName,
        string groupName,
        bool historicallyExactGeometry,
        bool landClipped,
        bool areaOverlapFreeByConstruction)
    {
        ZoneId = zoneId;
        ZoneName = zoneName;
        GroupName = groupName;
        HistoricallyExactGeometry = historicallyExactGeometry;
        LandClipped = landClipped;
        AreaOverlapFreeByConstruction = areaOverlapFreeByConstruction;
    }
}
