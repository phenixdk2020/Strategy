using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n3 visual polish for the v10n2 land-clipped zone overlay.
/// Rebuilds the visible border layer as snapped, unique INTERNAL zone segments.
/// Coastline-envelope segments are omitted because the basemap/coast itself already
/// defines the outer edge of a coastal zone. This removes double-drawn fuzzy lines
/// and the most obvious yellow fragments in water/coastal gaps.
///
/// Historical guardrail: this only polishes the current prototype geometry. It does
/// not make the centre-derived borders historically exact.
/// </summary>
[DefaultExecutionOrder(22000)]
public sealed class CampaignZoneLinePolishV010N3 : MonoBehaviour
{
    private struct SegmentKey : IEquatable<SegmentKey>
    {
        public int Ax;
        public int Az;
        public int Bx;
        public int Bz;

        public SegmentKey(Vector3 a, Vector3 b)
        {
            int ax = Quantize(a.x);
            int az = Quantize(a.z);
            int bx = Quantize(b.x);
            int bz = Quantize(b.z);

            bool swap = ax > bx || (ax == bx && az > bz);
            if (swap)
            {
                Ax = bx; Az = bz; Bx = ax; Bz = az;
            }
            else
            {
                Ax = ax; Az = az; Bx = bx; Bz = bz;
            }
        }

        public bool Equals(SegmentKey other)
        {
            return Ax == other.Ax && Az == other.Az && Bx == other.Bx && Bz == other.Bz;
        }

        public override bool Equals(object obj)
        {
            return obj is SegmentKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Ax;
                hash = hash * 397 ^ Az;
                hash = hash * 397 ^ Bx;
                hash = hash * 397 ^ Bz;
                return hash;
            }
        }
    }

    private sealed class SegmentData
    {
        public Vector3 A;
        public Vector3 B;
        public int SeenCount;
    }

    private const string SourceRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const string GeographyRootName = "GEO_Denmark_NaturalEarth50m";
    private const string PolishRootName = "ZONE_LINES_10N3_POLISHED";

    private const float SnapWorld = 0.0030f;
    private const float CoastTolerance = 0.075f;
    private const float MinSegmentLength = 0.018f;
    private const float LineWidth = 0.027f;
    private const float RenderY = 0.755f;

    private bool built;
    private Material lineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneLinePolishV010N3>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZONE_LINE_POLISH_v000010n3");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignZoneLinePolishV010N3>();
    }

    private void Awake()
    {
        lineMaterial = CreateLineMaterial();
    }

    private void Update()
    {
        if (built)
            return;

        GameObject source = GameObject.Find(SourceRootName);
        GameObject geography = GameObject.Find(GeographyRootName);
        if (source == null || geography == null)
            return;

        BuildPolishedLayer(source, geography);
        built = true;
    }

    private void BuildPolishedLayer(GameObject sourceRoot, GameObject geographyRoot)
    {
        List<Vector2[]> coastChains = ReadCoastChains(geographyRoot);
        Dictionary<SegmentKey, SegmentData> unique = new Dictionary<SegmentKey, SegmentData>();

        LineRenderer[] sourceLines = sourceRoot.GetComponentsInChildren<LineRenderer>(true);
        int rawSegments = 0;
        int coastSegmentsSkipped = 0;
        int tinySegmentsSkipped = 0;

        for (int l = 0; l < sourceLines.Length; l++)
        {
            LineRenderer sourceLine = sourceLines[l];
            int count = sourceLine.positionCount;
            if (count < 2)
                continue;

            Vector3[] positions = new Vector3[count];
            sourceLine.GetPositions(positions);
            int segmentCount = sourceLine.loop ? count : count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 a = Snap(positions[i]);
                Vector3 b = Snap(positions[(i + 1) % count]);
                rawSegments++;

                if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < MinSegmentLength)
                {
                    tinySegmentsSkipped++;
                    continue;
                }

                if (IsCoastEnvelopeSegment(a, b, coastChains))
                {
                    coastSegmentsSkipped++;
                    continue;
                }

                SegmentKey key = new SegmentKey(a, b);
                if (unique.TryGetValue(key, out SegmentData existing))
                {
                    existing.SeenCount++;
                    continue;
                }

                unique[key] = new SegmentData
                {
                    A = a,
                    B = b,
                    SeenCount = 1
                };
            }
        }

        GameObject polishedRoot = new GameObject(PolishRootName);
        polishedRoot.transform.SetParent(sourceRoot.transform, false);

        int rendered = 0;
        int deduplicated = 0;
        foreach (KeyValuePair<SegmentKey, SegmentData> pair in unique)
        {
            SegmentData segment = pair.Value;
            if (segment.SeenCount > 1)
                deduplicated += segment.SeenCount - 1;

            CreateSegment(polishedRoot.transform, segment.A, segment.B, rendered + 1);
            rendered++;
        }

        // v10n2 polygon lines remain as authoritative prototype geometry but are not
        // rendered once the polished n3 segment layer has been constructed.
        for (int i = 0; i < sourceLines.Length; i++)
            sourceLines[i].enabled = false;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|ZoneLinePolish=True" +
            "|RawSegments=" + rawSegments +
            "|RenderedInternalSegments=" + rendered +
            "|DuplicateSegmentsRemoved=" + deduplicated +
            "|CoastEnvelopeSegmentsSkipped=" + coastSegmentsSkipped +
            "|TinySegmentsSkipped=" + tinySegmentsSkipped +
            "|SnapWorld=" + SnapWorld.ToString("0.0000") +
            "|Width=" + LineWidth.ToString("0.000"));
    }

    private void CreateSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        GameObject go = new GameObject("ZONE_LINE_10N3_" + index.ToString("D4"));
        go.transform.SetParent(parent, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.positionCount = 2;
        line.widthMultiplier = LineWidth;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.sharedMaterial = lineMaterial;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.generateLightingData = false;

        a.y = RenderY;
        b.y = RenderY;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
    }

    private static List<Vector2[]> ReadCoastChains(GameObject geographyRoot)
    {
        List<Vector2[]> result = new List<Vector2[]>();
        LineRenderer[] coastLines = geographyRoot.GetComponentsInChildren<LineRenderer>(true);

        for (int i = 0; i < coastLines.Length; i++)
        {
            LineRenderer line = coastLines[i];
            if (!line.gameObject.name.StartsWith("DNK_Coast_", StringComparison.Ordinal))
                continue;

            Vector3[] world = new Vector3[line.positionCount];
            line.GetPositions(world);
            Vector2[] chain = new Vector2[world.Length];
            for (int p = 0; p < world.Length; p++)
                chain[p] = new Vector2(world[p].x, world[p].z);

            if (chain.Length >= 2)
                result.Add(chain);
        }

        return result;
    }

    private static bool IsCoastEnvelopeSegment(Vector3 a3, Vector3 b3, List<Vector2[]> coastChains)
    {
        Vector2 a = new Vector2(a3.x, a3.z);
        Vector2 b = new Vector2(b3.x, b3.z);
        Vector2 midpoint = (a + b) * 0.5f;

        for (int c = 0; c < coastChains.Count; c++)
        {
            Vector2[] chain = coastChains[c];
            for (int i = 0; i < chain.Length; i++)
            {
                Vector2 p = chain[i];
                Vector2 q = chain[(i + 1) % chain.Length];
                if (DistancePointToSegment(midpoint, p, q) <= CoastTolerance)
                {
                    Vector2 segmentDirection = (b - a).normalized;
                    Vector2 coastDirection = (q - p).normalized;
                    float parallel = Mathf.Abs(Vector2.Dot(segmentDirection, coastDirection));
                    if (parallel >= 0.92f)
                        return true;
                }
            }
        }

        return false;
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.0000001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
        return Vector2.Distance(point, a + ab * t);
    }

    private static Vector3 Snap(Vector3 point)
    {
        point.x = Mathf.Round(point.x / SnapWorld) * SnapWorld;
        point.z = Mathf.Round(point.z / SnapWorld) * SnapWorld;
        return point;
    }

    private static int Quantize(float value)
    {
        return Mathf.RoundToInt(value / SnapWorld);
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_POLISHED_10N3",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }
}
