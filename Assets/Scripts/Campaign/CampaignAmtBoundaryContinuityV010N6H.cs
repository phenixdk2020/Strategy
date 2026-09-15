using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6h visual continuity pass for the source-backed Amt lines.
/// Some neighbouring parish polygons describe the same administrative boundary
/// with different vertex subdivision. n6g therefore occasionally produced two
/// line chains with a small gap between them. This component bridges only short,
/// directionally compatible endpoint pairs. It does not change Amt ownership or
/// the polygon-backed click resolver.
/// </summary>
[DefaultExecutionOrder(22150)]
public sealed class CampaignAmtBoundaryContinuityV010N6H : MonoBehaviour
{
    private const string SourceRootName = "ZONE_LINES_10N6G_EXACT_PARISH_POLYGONS";
    private const string BridgeRootName = "ZONE_LINES_10N6H_CONTINUITY_BRIDGES";
    private const float MaxGapDegrees = 0.040f;
    private const float MinDirectionDot = 0.72f;
    private const float RenderY = 0.778f;
    private const float Width = 0.026f;

    private sealed class EndPoint
    {
        public LineRenderer Line;
        public Vector2 Geo;
        public Vector2 Extension;
        public int EndIndex;
    }

    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignAmtBoundaryContinuityV010N6H>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_AMT_CONTINUITY_10N6H");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignAmtBoundaryContinuityV010N6H>();
    }

    private void Update()
    {
        if (built || !CampaignHistoricalAmtPolygonsV010N6G.IsReady)
            return;

        GameObject source = GameObject.Find(SourceRootName);
        if (source == null)
            return;

        BuildBridges(source);
        built = true;
    }

    private static void BuildBridges(GameObject source)
    {
        LineRenderer[] lines = source.GetComponentsInChildren<LineRenderer>(true);
        List<EndPoint> ends = new List<EndPoint>();

        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            if (line == null || line.positionCount < 2)
                continue;

            Vector3 p0 = line.GetPosition(0);
            Vector3 p1 = line.GetPosition(1);
            Vector3 pn = line.GetPosition(line.positionCount - 1);
            Vector3 pp = line.GetPosition(line.positionCount - 2);

            Vector2 g0 = CampaignGeoProjection.Unproject(p0);
            Vector2 g1 = CampaignGeoProjection.Unproject(p1);
            Vector2 gn = CampaignGeoProjection.Unproject(pn);
            Vector2 gp = CampaignGeoProjection.Unproject(pp);

            Vector2 startExtension = (g0 - g1).normalized;
            Vector2 endExtension = (gn - gp).normalized;

            ends.Add(new EndPoint { Line = line, Geo = g0, Extension = startExtension, EndIndex = 0 });
            ends.Add(new EndPoint { Line = line, Geo = gn, Extension = endExtension, EndIndex = 1 });
        }

        bool[] used = new bool[ends.Count];
        List<Vector2[]> bridges = new List<Vector2[]>();
        float maxGapSqr = MaxGapDegrees * MaxGapDegrees;

        for (int i = 0; i < ends.Count; i++)
        {
            if (used[i])
                continue;

            EndPoint a = ends[i];
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int j = i + 1; j < ends.Count; j++)
            {
                if (used[j])
                    continue;

                EndPoint b = ends[j];
                if (ReferenceEquals(a.Line, b.Line))
                    continue;

                Vector2 delta = b.Geo - a.Geo;
                float d2 = delta.sqrMagnitude;
                if (d2 <= 0.00000001f || d2 > maxGapSqr)
                    continue;

                Vector2 dirAB = delta.normalized;
                Vector2 dirBA = -dirAB;

                float alignA = Vector2.Dot(a.Extension, dirAB);
                float alignB = Vector2.Dot(b.Extension, dirBA);
                if (alignA < MinDirectionDot || alignB < MinDirectionDot)
                    continue;

                if (d2 < bestDistance)
                {
                    bestDistance = d2;
                    bestIndex = j;
                }
            }

            if (bestIndex < 0)
                continue;

            used[i] = true;
            used[bestIndex] = true;
            bridges.Add(new[] { a.Geo, ends[bestIndex].Geo });
        }

        GameObject old = GameObject.Find(BridgeRootName);
        if (old != null)
            Destroy(old);

        GameObject root = new GameObject(BridgeRootName);
        DontDestroyOnLoad(root);

        Material material = CreateMaterial();
        for (int i = 0; i < bridges.Count; i++)
        {
            GameObject go = new GameObject("AMT_GAP_BRIDGE_" + i.ToString("D3"));
            go.transform.SetParent(root.transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = Width;
            line.sharedMaterial = material;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.SetPosition(0, CampaignGeoProjection.Project(bridges[i][0].x, bridges[i][0].y, RenderY));
            line.SetPosition(1, CampaignGeoProjection.Project(bridges[i][1].x, bridges[i][1].y, RenderY));
        }

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|AmtBoundaryContinuity=True" +
            "|SourceChains=" + lines.Length +
            "|Endpoints=" + ends.Count +
            "|Bridges=" + bridges.Count +
            "|MaxGapDegrees=" + MaxGapDegrees.ToString("0.000") +
            "|OwnershipChanged=False");
    }

    private static Material CreateMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = "AMT_BOUNDARY_CONTINUITY_10N6H",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }
}
