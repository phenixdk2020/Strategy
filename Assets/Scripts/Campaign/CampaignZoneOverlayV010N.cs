using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign3 zone overlay.
///
/// v00.00.10n introduced the visual zone-overlay architecture.
/// v00.00.10n2 replaces the old overlapping geographic group rectangles with one
/// global nearest-centre partition for all 20 zones and clips every resulting zone
/// part against the same Denmark land mesh used by the campaign geography layer.
///
/// IMPORTANT HISTORICAL GUARDRAIL:
/// Zone identities are canonical ZONE-REG-01 data, but this remains prototype
/// centre-derived geometry. It is NOT a claim about exact 1851 county boundaries.
/// Production geometry remains targeted at DigDag historical Amt/Region polygons.
/// </summary>
[DefaultExecutionOrder(21000)]
public sealed class CampaignZoneOverlayV010N : MonoBehaviour
{
    public const string GeometryMode = "PROTOTYPE_LAND_CLIPPED_GLOBAL_VORONOI";
    public const string IntendedHistoricalSource = "DigDag - Amt og Region";

    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";
    private const float OverlayY = 0.74f;
    private const float BoundaryWidth = 0.040f;
    private const float MinPolygonArea = 0.000010f;
    private const float Epsilon = 0.000001f;

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

        built = true;
        overlayRoot = new GameObject("ZONE_OVERLAY_1851_LAND_CLIPPED");
        DontDestroyOnLoad(overlayRoot);

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        HashSet<string> zonesWithGeometry = new HashSet<string>();
        int polygonParts = 0;
        int segments = 0;
        int tinyPartsSkipped = 0;

        for (int z = 0; z < zones.Length; z++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[z];
            Vector2 targetPoint = new Vector2(zone.Longitude, zone.Latitude);
            int zonePart = 0;

            for (int r = 0; r < landRings.Count; r++)
            {
                List<Vector2> polygon = new List<Vector2>(landRings[r]);

                // A single global nearest-centre partition means every point can
                // belong to only one zone interior. There are no overlapping group
                // rectangles as in the original v10n prototype.
                for (int o = 0; o < zones.Length; o++)
                {
                    CampaignDenmark1851Registry.ZoneDef other = zones[o];
                    if (other.Id == zone.Id)
                        continue;

                    Vector2 otherPoint = new Vector2(other.Longitude, other.Latitude);
                    polygon = ClipToTargetHalfPlane(polygon, targetPoint, otherPoint);
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

                zonePart++;
                CreateBoundary(zone, polygon, r + 1, zonePart);
                zonesWithGeometry.Add(zone.Id);
                polygonParts++;
                segments += polygon.Count;
            }
        }

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Installed=True|Overlay=1851Zones" +
            "|Zones=" + zonesWithGeometry.Count +
            "|PolygonParts=" + polygonParts +
            "|BoundarySegments=" + segments +
            "|TinyPartsSkipped=" + tinyPartsSkipped +
            "|LandClipped=True" +
            "|AreaOverlap=FalseByGlobalVoronoiConstruction" +
            "|LandMask=CampaignDenmarkGeography_NaturalEarth50m" +
            "|Geometry=" + GeometryMode +
            "|HistoricalSourceTarget=DigDag_Amt_Region|Toggle=Z");
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

        // Points closer to target than other satisfy:
        // dot(P, other-target) <= (|other|^2-|target|^2)/2
        Vector2 normal = other - target;
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
        int zonePart)
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
            "GLOBAL_LAND_PARTITION",
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
            name = "ZONE_BOUNDARY_1851_PROTOTYPE_10N2",
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
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 340f), Screen.height - 52f, 332f, 44f);
        GUI.Box(
            rect,
            CampaignBuildInfo.CurrentVersion + " | ZONES " + state + " | Z toggle\n" +
            "land-clipped · global non-overlap partition · DigDag target",
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
