using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n4 visual polish for the land-clipped zone overlay.
/// Rebuilds the visible border layer as snapped INTERNAL borders shared by at least
/// two different ZONE-REG-01 polygons. Single-owner polygon edges are deliberately
/// omitted, which removes coast/water fragments and long one-sided spur segments.
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
        public readonly HashSet<string> ZoneIds = new HashSet<string>();
        public int RawOccurrences;
    }

    private const string SourceRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const string PolishRootName = "ZONE_LINES_10N4_SHARED_INTERNAL";

    private const float SnapWorld = 0.0030f;
    private const float MinSegmentLength = 0.018f;
    private const float LineWidth = 0.026f;
    private const float RenderY = 0.755f;

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
        Dictionary<SegmentKey, SegmentData> unique = new Dictionary<SegmentKey, SegmentData>();

        LineRenderer[] sourceLines = sourceRoot.GetComponentsInChildren<LineRenderer>(true);
        int rawSegments = 0;
        int tinySegmentsSkipped = 0;
        int linesWithoutZoneMetadata = 0;

        for (int l = 0; l < sourceLines.Length; l++)
        {
            LineRenderer sourceLine = sourceLines[l];
            CampaignZoneOverlayMetadataV010N metadata = sourceLine.GetComponent<CampaignZoneOverlayMetadataV010N>();

            // Ignore previously generated polish layers or any unrelated renderer.
            // Only authoritative v10n2 polygon loops carry zone metadata.
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

                if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < MinSegmentLength)
                {
                    tinySegmentsSkipped++;
                    continue;
                }

                SegmentKey key = new SegmentKey(a, b);
                if (!unique.TryGetValue(key, out SegmentData data))
                {
                    data = new SegmentData { A = a, B = b };
                    unique[key] = data;
                }

                data.RawOccurrences++;
                data.ZoneIds.Add(metadata.ZoneId);
            }
        }

        GameObject polishedRoot = new GameObject(PolishRootName);
        polishedRoot.transform.SetParent(sourceRoot.transform, false);

        int renderedShared = 0;
        int singleOwnerSkipped = 0;
        int duplicateOccurrences = 0;

        foreach (KeyValuePair<SegmentKey, SegmentData> pair in unique)
        {
            SegmentData segment = pair.Value;
            duplicateOccurrences += Mathf.Max(0, segment.RawOccurrences - 1);

            // The crucial v10n4 rule: an internal administrative border must be
            // owned by at least two DIFFERENT zone polygons. Single-owner edges are
            // polygon exteriors/coast fragments or one-sided clipping artifacts.
            if (segment.ZoneIds.Count < 2)
            {
                singleOwnerSkipped++;
                continue;
            }

            CreateSegment(polishedRoot.transform, segment.A, segment.B, renderedShared + 1);
            renderedShared++;
        }

        // Keep source polygons as geometry/data, but render only the n4 shared-border layer.
        for (int i = 0; i < sourceLines.Length; i++)
        {
            if (sourceLines[i].GetComponent<CampaignZoneOverlayMetadataV010N>() != null)
                sourceLines[i].enabled = false;
        }

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|ZoneLinePolish=True" +
            "|Rule=SharedByTwoDistinctZones" +
            "|RawSegments=" + rawSegments +
            "|RenderedSharedInternalSegments=" + renderedShared +
            "|SingleOwnerSegmentsSkipped=" + singleOwnerSkipped +
            "|DuplicateOccurrences=" + duplicateOccurrences +
            "|TinySegmentsSkipped=" + tinySegmentsSkipped +
            "|NonZoneRenderersIgnored=" + linesWithoutZoneMetadata +
            "|SnapWorld=" + SnapWorld.ToString("0.0000") +
            "|Width=" + LineWidth.ToString("0.000"));
    }

    private void CreateSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        GameObject go = new GameObject("ZONE_LINE_10N4_" + index.ToString("D4"));
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

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "ZONE_BOUNDARY_1851_SHARED_INTERNAL_10N4",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }
}
