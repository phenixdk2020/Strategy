using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign3 v00.00.10n
///
/// Visual foundation for the canonical ZONE-REG-01 1851 zones.
///
/// IMPORTANT HISTORICAL GUARDRAIL:
/// The zone identities/names are canonical 1851 data, but the boundary geometry in
/// v10n is a temporary centre-derived sector approximation. It must NOT be treated
/// as historically exact county geometry. Production polygon geometry is targeted
/// at the DigDag historical-administrative GIS dataset (Amt og Region).
///
/// v10n therefore solves presentation/interaction architecture without baking a
/// fabricated border into the historical source-of-truth layer.
/// </summary>
[DefaultExecutionOrder(21000)]
public sealed class CampaignZoneOverlayV010N : MonoBehaviour
{
    private sealed class ZoneGroup
    {
        public string Name;
        public float West;
        public float East;
        public float South;
        public float North;
        public readonly List<CampaignDenmark1851Registry.ZoneDef> Zones =
            new List<CampaignDenmark1851Registry.ZoneDef>();
    }

    public const string Version = "v00.00.10n";
    public const string GeometryMode = "PROTOTYPE_CENTRE_DERIVED_SECTORS";
    public const string IntendedHistoricalSource = "DigDag - Amt og Region";

    private const float OverlayY = 0.74f;
    private const float BoundaryWidth = 0.055f;

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

        GameObject go = new GameObject("PROJECT1864_ZoneOverlay_v000010n");
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
            BuildOverlay();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            visible = !visible;
            if (overlayRoot != null)
                overlayRoot.SetActive(visible);

            Debug.Log("CAMPAIGN-10N|ZoneOverlay=" + visible + "|Toggle=Z|Geometry=" + GeometryMode);
        }
    }

    private void BuildOverlay()
    {
        built = true;
        overlayRoot = new GameObject("ZONE_OVERLAY_1851_PROTOTYPE");
        DontDestroyOnLoad(overlayRoot);

        List<ZoneGroup> groups = BuildGroups();
        int polygons = 0;
        int segments = 0;

        for (int g = 0; g < groups.Count; g++)
        {
            ZoneGroup group = groups[g];
            for (int z = 0; z < group.Zones.Count; z++)
            {
                CampaignDenmark1851Registry.ZoneDef zone = group.Zones[z];
                List<Vector2> polygon = BuildVoronoiCell(zone, group);
                if (polygon.Count < 3)
                    continue;

                CreateBoundary(zone, polygon, group.Name);
                polygons++;
                segments += polygon.Count;
            }
        }

        Debug.Log(
            "CAMPAIGN-10N|Installed=True|Overlay=1851Zones|Zones=" + polygons +
            "|BoundarySegments=" + segments +
            "|Geometry=" + GeometryMode +
            "|HistoricalSourceTarget=DigDag_Amt_Region|Toggle=Z");
    }

    private static List<ZoneGroup> BuildGroups()
    {
        ZoneGroup jutland = Group("Jylland", 7.95f, 11.05f, 54.70f, 57.82f);
        ZoneGroup funen = Group("Fyn-Langeland", 9.72f, 10.98f, 54.68f, 55.68f);
        ZoneGroup zealand = Group("Sjælland-Møn", 10.92f, 12.72f, 54.84f, 56.20f);
        ZoneGroup lolland = Group("Lolland-Falster", 10.98f, 12.62f, 54.55f, 55.08f);
        ZoneGroup bornholm = Group("Bornholm", 14.58f, 15.22f, 54.94f, 55.36f);

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            if (zone.Id == "DK-Z08-BOR")
                bornholm.Zones.Add(zone);
            else if (zone.Id == "DK-Z07-MAR")
                lolland.Zones.Add(zone);
            else if (zone.Id == "DK-Z09-ODE" || zone.Id == "DK-Z10-SVE")
                funen.Zones.Add(zone);
            else if (zone.Id.StartsWith("DK-Z0", StringComparison.Ordinal) &&
                     zone.Id != "DK-Z07-MAR" && zone.Id != "DK-Z08-BOR" &&
                     zone.Id != "DK-Z09-ODE")
                zealand.Zones.Add(zone);
            else
                jutland.Zones.Add(zone);
        }

        return new List<ZoneGroup> { jutland, funen, zealand, lolland, bornholm };
    }

    private static ZoneGroup Group(string name, float west, float east, float south, float north)
    {
        return new ZoneGroup
        {
            Name = name,
            West = west,
            East = east,
            South = south,
            North = north
        };
    }

    private static List<Vector2> BuildVoronoiCell(
        CampaignDenmark1851Registry.ZoneDef target,
        ZoneGroup group)
    {
        List<Vector2> polygon = new List<Vector2>
        {
            new Vector2(group.West, group.South),
            new Vector2(group.East, group.South),
            new Vector2(group.East, group.North),
            new Vector2(group.West, group.North)
        };

        Vector2 targetPoint = new Vector2(target.Longitude, target.Latitude);
        for (int i = 0; i < group.Zones.Count; i++)
        {
            CampaignDenmark1851Registry.ZoneDef other = group.Zones[i];
            if (ReferenceEquals(other, target) || other.Id == target.Id)
                continue;

            Vector2 otherPoint = new Vector2(other.Longitude, other.Latitude);
            polygon = ClipToTargetHalfPlane(polygon, targetPoint, otherPoint);
            if (polygon.Count < 3)
                break;
        }

        return polygon;
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
        bool previousInside = previousValue <= 0.000001f;

        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            float currentValue = Vector2.Dot(current, normal) - threshold;
            bool currentInside = currentValue <= 0.000001f;

            if (currentInside != previousInside)
            {
                float denominator = previousValue - currentValue;
                float t = Mathf.Abs(denominator) < 0.000001f
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

    private void CreateBoundary(
        CampaignDenmark1851Registry.ZoneDef zone,
        List<Vector2> polygon,
        string groupName)
    {
        GameObject go = new GameObject("ZONE_BORDER_" + zone.Id);
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
            groupName,
            false);
    }

    private static Material CreateBoundaryMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_PROTOTYPE_10N",
            color = new Color(0.95f, 0.78f, 0.24f, 0.90f)
        };
        return material;
    }

    private void EnsureGuiStyle()
    {
        if (badgeStyle != null)
            return;

        badgeStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        badgeStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureGuiStyle();
        string state = visible ? "ON" : "OFF";
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 365f), Screen.height - 58f, 357f, 50f);
        GUI.Box(
            rect,
            Version + " | ZONE OVERLAY " + state + " | Z = toggle\n" +
            "1851 zone identities · prototype geometry · DigDag target",
            badgeStyle);
    }
}

public sealed class CampaignZoneOverlayMetadataV010N : MonoBehaviour
{
    public string ZoneId { get; private set; }
    public string ZoneName { get; private set; }
    public string GroupName { get; private set; }
    public bool HistoricallyExactGeometry { get; private set; }

    public void Initialize(
        string zoneId,
        string zoneName,
        string groupName,
        bool historicallyExactGeometry)
    {
        ZoneId = zoneId;
        ZoneName = zoneName;
        GroupName = groupName;
        HistoricallyExactGeometry = historicallyExactGeometry;
    }
}
