using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 current visual polish for the land-clipped zone overlay.
///
/// v10n4 only rendered source segments whose snapped endpoints were exactly equal
/// in two different zone polygons. That could hide a real shared border when one
/// polygon represented the same straight border as one long segment and the other
/// represented it as two or more shorter collinear segments.
///
/// v10n5 detects geometric collinear overlap between edges owned by different
/// ZoneIds. Only the overlapping portion is rendered. This keeps coastline and
/// one-sided clipping artifacts hidden while restoring legitimate shared borders
/// such as the Thisted/Viborg prototype divide.
///
/// Historical guardrail: this polishes current prototype geometry only. It does
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

    private sealed class SourceSegment
    {
        public Vector3 A;
        public Vector3 B;
        public string ZoneId;
    }

    private sealed class SharedSegment
    {
        public Vector3 A;
        public Vector3 B;
        public readonly HashSet<string> ZoneIds = new HashSet<string>();
    }

    private const string SourceRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const string PolishRootName = "ZONE_LINES_10N5_SHARED_COLLINEAR";

    private const float SnapWorld = 0.0030f;
    private const float MinSegmentLength = 0.018f;
    private const float LineWidth = 0.026f;
    private const float RenderY = 0.755f;
    private const float CollinearDistanceTolerance = 0.045f;
    private const float ParallelCrossTolerance = 0.025f;

    private bool built;
    private Material lineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignZoneLinePolishV010N3>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_ZONE_LINE_POLISH_CURRENT");
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
        if (source == null)
            return;

        BuildPolishedLayer(source);
        built = true;
    }

    private void BuildPolishedLayer(GameObject sourceRoot)
    {
        List<SourceSegment> sourceSegments = new List<SourceSegment>();
        LineRenderer[] sourceLines = sourceRoot.GetComponentsInChildren<LineRenderer>(true);
        int rawSegments = 0;
        int tinySegmentsSkipped = 0;
        int linesWithoutZoneMetadata = 0;

        for (int l = 0; l < sourceLines.Length; l++)
        {
            LineRenderer sourceLine = sourceLines[l];
            CampaignZoneOverlayMetadataV010N metadata = sourceLine.GetComponent<CampaignZoneOverlayMetadataV010N>();

            if (metadata == null || string.IsNullOrEmpty(metadata.ZoneId))
            {
                linesWithoutZoneMetadata++;
                continue;
            }

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

                if (PlanarDistance(a, b) < MinSegmentLength)
                {
                    tinySegmentsSkipped++;
                    continue;
                }

                sourceSegments.Add(new SourceSegment
                {
                    A = a,
                    B = b,
                    ZoneId = metadata.ZoneId
                });
            }
        }

        Dictionary<SegmentKey, SharedSegment> shared = new Dictionary<SegmentKey, SharedSegment>();
        int candidatePairs = 0;
        int overlapPairs = 0;

        for (int i = 0; i < sourceSegments.Count; i++)
        {
            SourceSegment a = sourceSegments[i];
            for (int j = i + 1; j < sourceSegments.Count; j++)
            {
                SourceSegment b = sourceSegments[j];
                if (a.ZoneId == b.ZoneId)
                    continue;

                candidatePairs++;
                if (!TryGetCollinearOverlap(a.A, a.B, b.A, b.B, out Vector3 overlapA, out Vector3 overlapB))
                    continue;

                overlapPairs++;
                overlapA = Snap(overlapA);
                overlapB = Snap(overlapB);
                if (PlanarDistance(overlapA, overlapB) < MinSegmentLength)
                    continue;

                SegmentKey key = new SegmentKey(overlapA, overlapB);
                if (!shared.TryGetValue(key, out SharedSegment data))
                {
                    data = new SharedSegment { A = overlapA, B = overlapB };
                    shared[key] = data;
                }

                data.ZoneIds.Add(a.ZoneId);
                data.ZoneIds.Add(b.ZoneId);
            }
        }

        GameObject polishedRoot = new GameObject(PolishRootName);
        polishedRoot.transform.SetParent(sourceRoot.transform, false);

        int renderedShared = 0;
        foreach (KeyValuePair<SegmentKey, SharedSegment> pair in shared)
        {
            SharedSegment segment = pair.Value;
            if (segment.ZoneIds.Count < 2)
                continue;

            CreateSegment(polishedRoot.transform, segment.A, segment.B, renderedShared + 1);
            renderedShared++;
        }

        // Preserve source polygon loops as hidden geometry/data. The visible yellow
        // layer consists only of overlaps proven to be shared by two zone polygons.
        for (int i = 0; i < sourceLines.Length; i++)
        {
            if (sourceLines[i].GetComponent<CampaignZoneOverlayMetadataV010N>() != null)
                sourceLines[i].enabled = false;
        }

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|ZoneLinePolish=True" +
            "|Rule=CollinearOverlapBetweenDistinctZones" +
            "|RawSegments=" + rawSegments +
            "|SourceSegments=" + sourceSegments.Count +
            "|CandidatePairs=" + candidatePairs +
            "|OverlapPairs=" + overlapPairs +
            "|RenderedSharedInternalSegments=" + renderedShared +
            "|TinySegmentsSkipped=" + tinySegmentsSkipped +
            "|NonZoneRenderersIgnored=" + linesWithoutZoneMetadata +
            "|SnapWorld=" + SnapWorld.ToString("0.0000") +
            "|LineTolerance=" + CollinearDistanceTolerance.ToString("0.000") +
            "|Width=" + LineWidth.ToString("0.000"));
    }

    private static bool TryGetCollinearOverlap(
        Vector3 a0World,
        Vector3 a1World,
        Vector3 b0World,
        Vector3 b1World,
        out Vector3 overlapA,
        out Vector3 overlapB)
    {
        overlapA = default;
        overlapB = default;

        Vector2 a0 = new Vector2(a0World.x, a0World.z);
        Vector2 a1 = new Vector2(a1World.x, a1World.z);
        Vector2 b0 = new Vector2(b0World.x, b0World.z);
        Vector2 b1 = new Vector2(b1World.x, b1World.z);

        Vector2 da = a1 - a0;
        Vector2 db = b1 - b0;
        float lenA = da.magnitude;
        float lenB = db.magnitude;
        if (lenA < MinSegmentLength || lenB < MinSegmentLength)
            return false;

        Vector2 dirA = da / lenA;
        Vector2 dirB = db / lenB;
        float parallelCross = Mathf.Abs(Cross2(dirA, dirB));
        if (parallelCross > ParallelCrossTolerance)
            return false;

        if (DistancePointToInfiniteLine(b0, a0, dirA) > CollinearDistanceTolerance ||
            DistancePointToInfiniteLine(b1, a0, dirA) > CollinearDistanceTolerance)
        {
            return false;
        }

        float b0Projection = Vector2.Dot(b0 - a0, dirA);
        float b1Projection = Vector2.Dot(b1 - a0, dirA);
        float bMin = Mathf.Min(b0Projection, b1Projection);
        float bMax = Mathf.Max(b0Projection, b1Projection);

        float start = Mathf.Max(0f, bMin);
        float end = Mathf.Min(lenA, bMax);
        if (end - start < MinSegmentLength)
            return false;

        Vector2 p0 = a0 + dirA * start;
        Vector2 p1 = a0 + dirA * end;
        overlapA = new Vector3(p0.x, RenderY, p0.y);
        overlapB = new Vector3(p1.x, RenderY, p1.y);
        return true;
    }

    private static float DistancePointToInfiniteLine(Vector2 point, Vector2 lineStart, Vector2 lineDirectionNormalized)
    {
        Vector2 delta = point - lineStart;
        return Mathf.Abs(Cross2(lineDirectionNormalized, delta));
    }

    private static float Cross2(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private void CreateSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        GameObject go = new GameObject("ZONE_LINE_10N5_" + index.ToString("D4"));
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

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_SHARED_INTERNAL_10N5",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }
}
