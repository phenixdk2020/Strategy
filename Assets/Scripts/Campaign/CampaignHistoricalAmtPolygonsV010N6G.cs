using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6g - source-backed historical Amt geometry.
///
/// n6g stops generating Amt borders from centres, Voronoi cells or sampled grids.
/// It downloads the DigDag-derived 1 Jan 1820 parish SHP + DBF used by
/// christianvedels/A_perfect_storm, parses the real parish polygon edges and draws
/// only edges shared by parishes belonging to different Amt. The same polygons are
/// used for land-click ownership.
///
/// Historical guardrail: source geometry is 1820 parish geography, not an exact
/// 1 Jan 1851 Amt/Region export. Therefore HistoricallyExact1851 remains false.
/// </summary>
[DefaultExecutionOrder(21900)]
public sealed class CampaignHistoricalAmtPolygonsV010N6G : MonoBehaviour
{
    private const string ShpUrl =
        "https://raw.githubusercontent.com/christianvedels/A_perfect_storm/main/Data/sogne_shape/sogne.shp";
    private const string DbfUrl =
        "https://raw.githubusercontent.com/christianvedels/A_perfect_storm/main/Data/sogne_shape/sogne.dbf";

    private const string ShpCacheName = "PROJECT1864_DigDag1820_sogne.shp";
    private const string DbfCacheName = "PROJECT1864_DigDag1820_sogne.dbf";
    private const string BoundaryRootName = "ZONE_LINES_10N6G_EXACT_PARISH_POLYGONS";
    private const string LegacyGridRootName = "ZONE_LINES_10N6F_PARISH_GRID";
    private const string LegacyOverlayRootName = "ZONE_OVERLAY_1851_LAND_CLIPPED";
    private const string LegacyPolishRootName = "ZONE_LINES_10N5_SHARED_COLLINEAR";

    private const float RenderY = 0.775f;
    private const float BoundaryWidth = 0.026f;
    private const double EdgeQuantize = 100000.0;
    private const int MinimumParsedParishes = 500;

    private sealed class DbfField
    {
        public string Name;
        public int Offset;
        public int Length;
    }

    private sealed class DbfRecord
    {
        public string Amt;
        public string Parish;
        public string Hundred;
        public bool Deleted;
    }

    private sealed class ParishArea
    {
        public string ZoneId;
        public string ParishName;
        public readonly List<List<Vector2>> Rings = new List<List<Vector2>>();
        public float MinX = float.MaxValue;
        public float MinY = float.MaxValue;
        public float MaxX = float.MinValue;
        public float MaxY = float.MinValue;

        public bool BoundsContains(Vector2 p)
        {
            return p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY;
        }
    }

    private struct PointKey : IEquatable<PointKey>
    {
        public long X;
        public long Y;

        public PointKey(Vector2 p)
        {
            X = (long)Math.Round(p.x * EdgeQuantize);
            Y = (long)Math.Round(p.y * EdgeQuantize);
        }

        public bool Equals(PointKey other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is PointKey other && Equals(other); }
        public override int GetHashCode()
        {
            unchecked { return ((int)X * 397) ^ (int)(X >> 32) ^ ((int)Y * 31) ^ (int)(Y >> 32); }
        }
    }

    private struct EdgeKey : IEquatable<EdgeKey>
    {
        public PointKey A;
        public PointKey B;

        public EdgeKey(Vector2 a, Vector2 b)
        {
            PointKey ka = new PointKey(a);
            PointKey kb = new PointKey(b);
            if (Compare(ka, kb) <= 0) { A = ka; B = kb; }
            else { A = kb; B = ka; }
        }

        private static int Compare(PointKey a, PointKey b)
        {
            if (a.X < b.X) return -1;
            if (a.X > b.X) return 1;
            if (a.Y < b.Y) return -1;
            if (a.Y > b.Y) return 1;
            return 0;
        }

        public bool Equals(EdgeKey other) { return A.Equals(other.A) && B.Equals(other.B); }
        public override bool Equals(object obj) { return obj is EdgeKey other && Equals(other); }
        public override int GetHashCode()
        {
            unchecked { return (A.GetHashCode() * 397) ^ B.GetHashCode(); }
        }
    }

    private sealed class EdgeInfo
    {
        public Vector2 A;
        public Vector2 B;
        public string OwnerA;
        public string OwnerB;

        public bool IsAmtBoundary
        {
            get { return !string.IsNullOrEmpty(OwnerA) && !string.IsNullOrEmpty(OwnerB) && OwnerA != OwnerB; }
        }
    }

    private sealed class BoundarySegment
    {
        public Vector2 A;
        public Vector2 B;
        public PointKey KeyA;
        public PointKey KeyB;
    }

    public const string GeometryMode = "DIGDAG_1820_SOURCE_PARISH_POLYGON_EDGES";
    public static CampaignHistoricalAmtPolygonsV010N6G Instance { get; private set; }
    public static bool IsReady { get { return Instance != null && Instance.ready; } }

    private readonly List<ParishArea> parishes = new List<ParishArea>();
    private GameObject boundaryRoot;
    private Material boundaryMaterial;
    private bool ready;
    private bool visible = true;
    private string sourceMode = "not-loaded";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignHistoricalAmtPolygonsV010N6G>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_HISTORICAL_AMT_POLYGONS_10N6G");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignHistoricalAmtPolygonsV010N6G>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        boundaryMaterial = CreateBoundaryMaterial();

        CampaignHistoricalAmtOverlayV010N6F legacy = Object.FindAnyObjectByType<CampaignHistoricalAmtOverlayV010N6F>();
        if (legacy != null)
        {
            legacy.StopAllCoroutines();
            legacy.enabled = false;
        }

        DestroyNamedObject(LegacyGridRootName);
        StartCoroutine(Initialize());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (ready && Input.GetKeyDown(KeyCode.Z))
        {
            visible = !visible;
            if (boundaryRoot != null) boundaryRoot.SetActive(visible);
            Debug.Log(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=" + visible + "|Toggle=Z");
        }
    }

    private IEnumerator Initialize()
    {
        while (GrandCampaignBootstrap.Instance == null)
            yield return null;

        byte[] shp = null;
        byte[] dbf = null;
        string shpCache = Path.Combine(Application.persistentDataPath, ShpCacheName);
        string dbfCache = Path.Combine(Application.persistentDataPath, DbfCacheName);

        if (TryReadCache(shpCache, dbfCache, out shp, out dbf))
        {
            sourceMode = "PersistentCache";
        }
        else
        {
            yield return DownloadBytes(ShpUrl, delegate(byte[] bytes) { shp = bytes; });
            if (shp == null || shp.Length < 100)
            {
                Debug.LogError(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=False|Reason=ShpDownloadFailed");
                yield break;
            }

            yield return DownloadBytes(DbfUrl, delegate(byte[] bytes) { dbf = bytes; });
            if (dbf == null || dbf.Length < 64)
            {
                Debug.LogError(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=False|Reason=DbfDownloadFailed");
                yield break;
            }

            sourceMode = "DigDagDerived1820ShpDbfDownload";
            TryWriteCache(shpCache, shp);
            TryWriteCache(dbfCache, dbf);
        }

        List<DbfRecord> records;
        if (!TryParseDbf(dbf, out records))
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=False|Reason=DbfParseFailed");
            yield break;
        }

        Dictionary<EdgeKey, EdgeInfo> edges = new Dictionary<EdgeKey, EdgeInfo>();
        if (!TryParseShp(shp, records, edges))
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=False|Reason=ShpParseFailed|Parishes=" + parishes.Count);
            yield break;
        }

        yield return BuildBoundaryLines(edges);
        if (boundaryRoot == null)
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|AmtPolygonOverlay=False|Reason=NoBoundaryLines");
            yield break;
        }

        SuppressLegacyZoneLines();
        ready = true;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|AmtPolygonOverlay=True" +
            "|Geometry=" + GeometryMode +
            "|Parishes=" + parishes.Count +
            "|Source=" + sourceMode +
            "|ClickResolver=SameSourceParishPolygons" +
            "|HistoricallyExact1851=False");
    }

    private static IEnumerator DownloadBytes(string url, Action<byte[]> completed)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success && request.downloadHandler != null)
                completed(request.downloadHandler.data);
            else
            {
                Debug.LogWarning(CampaignBuildInfo.LogTag + "|HistoricalBinaryDownload=False|Url=" + url + "|Error=" + request.error);
                completed(null);
            }
        }
    }

    private static bool TryReadCache(string shpPath, string dbfPath, out byte[] shp, out byte[] dbf)
    {
        shp = null;
        dbf = null;
        try
        {
            if (!File.Exists(shpPath) || !File.Exists(dbfPath)) return false;
            shp = File.ReadAllBytes(shpPath);
            dbf = File.ReadAllBytes(dbfPath);
            return shp.Length >= 100 && dbf.Length >= 64;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(CampaignBuildInfo.LogTag + "|HistoricalBinaryCacheRead=False|Error=" + ex.Message);
            shp = null;
            dbf = null;
            return false;
        }
    }

    private static void TryWriteCache(string path, byte[] bytes)
    {
        if (bytes == null) return;
        try { File.WriteAllBytes(path, bytes); }
        catch (Exception ex)
        {
            Debug.LogWarning(CampaignBuildInfo.LogTag + "|HistoricalBinaryCacheWrite=False|File=" + Path.GetFileName(path) + "|Error=" + ex.Message);
        }
    }

    private static bool TryParseDbf(byte[] data, out List<DbfRecord> records)
    {
        records = new List<DbfRecord>();
        if (data == null || data.Length < 64) return false;

        int recordCount = ReadInt32LE(data, 4);
        int headerLength = ReadUInt16LE(data, 8);
        int recordLength = ReadUInt16LE(data, 10);
        if (recordCount <= 0 || headerLength <= 32 || recordLength <= 1 || headerLength >= data.Length)
            return false;

        List<DbfField> fields = new List<DbfField>();
        int runningOffset = 1;
        for (int p = 32; p + 31 < headerLength && data[p] != 0x0D; p += 32)
        {
            string name = ReadAsciiName(data, p, 11).Trim().ToUpperInvariant();
            int len = data[p + 16];
            fields.Add(new DbfField { Name = name, Offset = runningOffset, Length = len });
            runningOffset += len;
        }

        DbfField amtField = FindField(fields, "AMT");
        DbfField parishField = FindField(fields, "SOGN");
        DbfField hundredField = FindField(fields, "HERRED");
        if (amtField == null) return false;

        for (int r = 0; r < recordCount; r++)
        {
            int start = headerLength + r * recordLength;
            if (start < 0 || start + recordLength > data.Length) break;

            bool deleted = data[start] == (byte)'*';
            records.Add(new DbfRecord
            {
                Deleted = deleted,
                Amt = ReadLatin1Field(data, start + amtField.Offset, amtField.Length).Trim(),
                Parish = parishField == null ? string.Empty : ReadLatin1Field(data, start + parishField.Offset, parishField.Length).Trim(),
                Hundred = hundredField == null ? string.Empty : ReadLatin1Field(data, start + hundredField.Offset, hundredField.Length).Trim()
            });
        }

        return records.Count > 0;
    }

    private bool TryParseShp(byte[] data, List<DbfRecord> records, Dictionary<EdgeKey, EdgeInfo> edges)
    {
        parishes.Clear();
        if (data == null || data.Length < 100 || records == null || records.Count == 0)
            return false;

        int offset = 100;
        int recordIndex = 0;
        while (offset + 8 <= data.Length && recordIndex < records.Count)
        {
            int contentWords = ReadInt32BE(data, offset + 4);
            int contentBytes = contentWords * 2;
            int contentStart = offset + 8;
            int contentEnd = contentStart + contentBytes;
            if (contentBytes < 4 || contentEnd > data.Length) break;

            DbfRecord dbf = records[recordIndex];
            recordIndex++;

            int shapeType = ReadInt32LE(data, contentStart);
            if (!dbf.Deleted && (shapeType == 5 || shapeType == 15 || shapeType == 25))
                ParsePolygonRecord(data, contentStart, contentEnd, dbf, edges);

            offset = contentEnd;
        }

        return parishes.Count >= MinimumParsedParishes;
    }

    private void ParsePolygonRecord(byte[] data, int start, int end, DbfRecord dbf, Dictionary<EdgeKey, EdgeInfo> edges)
    {
        if (start + 44 > end) return;
        int numParts = ReadInt32LE(data, start + 36);
        int numPoints = ReadInt32LE(data, start + 40);
        if (numParts <= 0 || numPoints < 3) return;

        int partsOffset = start + 44;
        int pointsOffset = partsOffset + numParts * 4;
        if (pointsOffset < 0 || pointsOffset + numPoints * 16 > end) return;

        string zoneId;
        if (!TryMapCountyToZone(dbf.Amt, out zoneId)) return;

        int[] partStarts = new int[numParts + 1];
        for (int i = 0; i < numParts; i++)
            partStarts[i] = ReadInt32LE(data, partsOffset + i * 4);
        partStarts[numParts] = numPoints;

        ParishArea area = new ParishArea { ZoneId = zoneId, ParishName = dbf.Parish };

        for (int part = 0; part < numParts; part++)
        {
            int from = partStarts[part];
            int to = partStarts[part + 1];
            if (from < 0 || to > numPoints || to - from < 3) continue;

            List<Vector2> ring = new List<Vector2>(to - from);
            for (int p = from; p < to; p++)
            {
                int pointOffset = pointsOffset + p * 16;
                double x = ReadDoubleLE(data, pointOffset);
                double y = ReadDoubleLE(data, pointOffset + 8);
                Vector2 v = new Vector2((float)x, (float)y);
                if (ring.Count == 0 || (ring[ring.Count - 1] - v).sqrMagnitude > 0.0000000001f)
                    ring.Add(v);
            }

            if (ring.Count >= 2 && (ring[0] - ring[ring.Count - 1]).sqrMagnitude <= 0.0000000001f)
                ring.RemoveAt(ring.Count - 1);
            if (ring.Count < 3) continue;

            if (zoneId == "DK-Z02-KBH-AMT" && RingContainsCanonicalCopenhagen(ring))
                area.ZoneId = "DK-Z01-KBH";

            area.Rings.Add(ring);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 v = ring[i];
                area.MinX = Mathf.Min(area.MinX, v.x);
                area.MinY = Mathf.Min(area.MinY, v.y);
                area.MaxX = Mathf.Max(area.MaxX, v.x);
                area.MaxY = Mathf.Max(area.MaxY, v.y);
            }
        }

        if (area.Rings.Count == 0) return;
        parishes.Add(area);

        for (int r = 0; r < area.Rings.Count; r++)
        {
            List<Vector2> ring = area.Rings[r];
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i];
                Vector2 b = ring[(i + 1) % ring.Count];
                if ((a - b).sqrMagnitude < 0.0000000001f) continue;

                EdgeKey key = new EdgeKey(a, b);
                EdgeInfo info;
                if (!edges.TryGetValue(key, out info))
                {
                    info = new EdgeInfo { A = a, B = b, OwnerA = area.ZoneId };
                    edges[key] = info;
                }
                else if (info.OwnerA != area.ZoneId && string.IsNullOrEmpty(info.OwnerB))
                {
                    info.OwnerB = area.ZoneId;
                }
            }
        }
    }

    private static bool RingContainsCanonicalCopenhagen(List<Vector2> ring)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i].ZoneId == "DK-Z01-KBH")
                return PointInRing(new Vector2(cities[i].Longitude, cities[i].Latitude), ring);
        }
        return false;
    }

    private IEnumerator BuildBoundaryLines(Dictionary<EdgeKey, EdgeInfo> edges)
    {
        List<BoundarySegment> segments = new List<BoundarySegment>();
        foreach (KeyValuePair<EdgeKey, EdgeInfo> pair in edges)
        {
            EdgeInfo e = pair.Value;
            if (!e.IsAmtBoundary) continue;
            segments.Add(new BoundarySegment
            {
                A = e.A,
                B = e.B,
                KeyA = new PointKey(e.A),
                KeyB = new PointKey(e.B)
            });
        }

        if (segments.Count == 0) yield break;

        Dictionary<PointKey, List<int>> adjacency = new Dictionary<PointKey, List<int>>();
        for (int i = 0; i < segments.Count; i++)
        {
            AddAdjacency(adjacency, segments[i].KeyA, i);
            AddAdjacency(adjacency, segments[i].KeyB, i);
        }

        bool[] used = new bool[segments.Count];
        List<List<Vector2>> chains = new List<List<Vector2>>();

        for (int i = 0; i < segments.Count; i++)
        {
            if (used[i]) continue;
            BoundarySegment s = segments[i];
            int degreeA = adjacency[s.KeyA].Count;
            int degreeB = adjacency[s.KeyB].Count;
            if (degreeA == 2 && degreeB == 2) continue;

            PointKey startKey = degreeA != 2 ? s.KeyA : s.KeyB;
            chains.Add(TraceChain(i, startKey, segments, adjacency, used));
        }

        for (int i = 0; i < segments.Count; i++)
        {
            if (!used[i])
                chains.Add(TraceChain(i, segments[i].KeyA, segments, adjacency, used));
        }

        DestroyNamedObject(BoundaryRootName);
        boundaryRoot = new GameObject(BoundaryRootName);
        DontDestroyOnLoad(boundaryRoot);

        int renderedChains = 0;
        int renderedPoints = 0;
        for (int i = 0; i < chains.Count; i++)
        {
            List<Vector2> chain = RemoveNearDuplicatePoints(chains[i]);
            if (chain.Count < 2) continue;

            GameObject go = new GameObject("AMT_BORDER_" + renderedChains.ToString("D4"));
            go.transform.SetParent(boundaryRoot.transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = chain.Count;
            line.widthMultiplier = BoundaryWidth;
            line.sharedMaterial = boundaryMaterial;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            for (int p = 0; p < chain.Count; p++)
                line.SetPosition(p, CampaignGeoProjection.Project(chain[p].x, chain[p].y, RenderY));

            renderedChains++;
            renderedPoints += chain.Count;
            if ((renderedChains & 63) == 0) yield return null;
        }

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|AmtBoundarySourceEdges=True" +
            "|RawSharedSegments=" + segments.Count +
            "|Chains=" + renderedChains +
            "|Points=" + renderedPoints +
            "|Width=" + BoundaryWidth.ToString("0.000") +
            "|Grid=False|Voronoi=False");
    }

    private static List<Vector2> TraceChain(
        int firstIndex,
        PointKey startKey,
        List<BoundarySegment> segments,
        Dictionary<PointKey, List<int>> adjacency,
        bool[] used)
    {
        List<Vector2> points = new List<Vector2>();
        int currentIndex = firstIndex;
        PointKey currentKey = startKey;
        int guard = 0;

        while (currentIndex >= 0 && !used[currentIndex] && guard++ < segments.Count + 4)
        {
            BoundarySegment s = segments[currentIndex];
            bool fromA = s.KeyA.Equals(currentKey);
            Vector2 start = fromA ? s.A : s.B;
            Vector2 end = fromA ? s.B : s.A;
            PointKey nextKey = fromA ? s.KeyB : s.KeyA;

            if (points.Count == 0) points.Add(start);
            points.Add(end);
            used[currentIndex] = true;

            List<int> nextList = adjacency[nextKey];
            int next = -1;
            if (nextList.Count == 2)
            {
                for (int i = 0; i < nextList.Count; i++)
                {
                    if (!used[nextList[i]]) { next = nextList[i]; break; }
                }
            }
            else
            {
                break;
            }

            currentKey = nextKey;
            currentIndex = next;
        }

        return points;
    }

    private static void AddAdjacency(Dictionary<PointKey, List<int>> map, PointKey key, int index)
    {
        List<int> list;
        if (!map.TryGetValue(key, out list))
        {
            list = new List<int>();
            map[key] = list;
        }
        list.Add(index);
    }

    public static bool TryResolveWorld(Vector3 worldPoint, out string zoneId)
    {
        zoneId = null;
        if (!IsReady) return false;
        return Instance.TryResolveGeo(CampaignGeoProjection.Unproject(worldPoint), out zoneId);
    }

    public bool TryResolveGeo(Vector2 geo, out string zoneId)
    {
        zoneId = null;
        if (!ready) return false;

        for (int i = 0; i < parishes.Count; i++)
        {
            ParishArea area = parishes[i];
            if (!area.BoundsContains(geo)) continue;
            if (!PointInRingsEvenOdd(geo, area.Rings)) continue;
            zoneId = area.ZoneId;
            return !string.IsNullOrEmpty(zoneId);
        }

        return false;
    }

    private void SuppressLegacyZoneLines()
    {
        DestroyNamedObject(LegacyGridRootName);

        GameObject oldOverlay = GameObject.Find(LegacyOverlayRootName);
        if (oldOverlay != null)
        {
            LineRenderer[] lines = oldOverlay.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++) lines[i].enabled = false;
        }

        GameObject oldPolish = GameObject.Find(LegacyPolishRootName);
        if (oldPolish != null) oldPolish.SetActive(false);
    }

    private static void DestroyNamedObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Destroy(go);
    }

    private static Material CreateBoundaryMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader)
        {
            name = "AMT_BOUNDARY_DIGDAG_PARISH_SOURCE_10N6G",
            color = new Color(1.00f, 0.78f, 0.10f, 1.00f)
        };
    }

    private static List<Vector2> RemoveNearDuplicatePoints(List<Vector2> input)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            if (output.Count == 0 || (output[output.Count - 1] - input[i]).sqrMagnitude > 0.0000000001f)
                output.Add(input[i]);
        }
        return output;
    }

    private static bool PointInRingsEvenOdd(Vector2 point, List<List<Vector2>> rings)
    {
        bool inside = false;
        for (int i = 0; i < rings.Count; i++)
            if (PointInRing(point, rings[i])) inside = !inside;
        return inside;
    }

    private static bool PointInRing(Vector2 point, List<Vector2> ring)
    {
        if (ring == null || ring.Count < 3) return false;
        bool inside = false;
        int j = ring.Count - 1;
        for (int i = 0; i < ring.Count; i++)
        {
            Vector2 pi = ring[i];
            Vector2 pj = ring[j];
            if (((pi.y > point.y) != (pj.y > point.y)) &&
                point.x < (pj.x - pi.x) * (point.y - pi.y) / ((pj.y - pi.y) + 0.0000000001f) + pi.x)
                inside = !inside;
            j = i;
        }
        return inside;
    }

    private static DbfField FindField(List<DbfField> fields, string name)
    {
        for (int i = 0; i < fields.Count; i++)
            if (fields[i].Name == name) return fields[i];
        return null;
    }

    private static string ReadAsciiName(byte[] data, int offset, int length)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < length && offset + i < data.Length; i++)
        {
            byte b = data[offset + i];
            if (b == 0) break;
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    private static string ReadLatin1Field(byte[] data, int offset, int length)
    {
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length && offset + i < data.Length; i++)
        {
            byte b = data[offset + i];
            if (b == 0) break;
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    private static int ReadInt32LE(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static int ReadUInt16LE(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadInt32BE(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static double ReadDoubleLE(byte[] data, int offset)
    {
        byte[] tmp = new byte[8];
        Buffer.BlockCopy(data, offset, tmp, 0, 8);
        if (!BitConverter.IsLittleEndian) Array.Reverse(tmp);
        return BitConverter.ToDouble(tmp, 0);
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        value = value.Trim().ToLowerInvariant();
        value = value.Replace("æ", "ae").Replace("ø", "oe").Replace("å", "aa");
        value = value.Replace("ä", "a").Replace("ö", "o").Replace("ü", "u");
        StringBuilder sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool TryMapCountyToZone(string county, out string zoneId)
    {
        zoneId = null;
        switch (NormalizeKey(county))
        {
            case "aalborg": zoneId = "DK-Z16-AAL"; return true;
            case "aarhus": zoneId = "DK-Z13-AAR"; return true;
            case "bornholm": zoneId = "DK-Z08-BOR"; return true;
            case "frederiksborg": zoneId = "DK-Z03-FRB"; return true;
            case "holbaek": zoneId = "DK-Z04-HOL"; return true;
            case "hjoerring": zoneId = "DK-Z17-HJO"; return true;
            case "koebenhavn": zoneId = "DK-Z02-KBH-AMT"; return true;
            case "maribo": zoneId = "DK-Z07-MAR"; return true;
            case "odense": zoneId = "DK-Z09-ODE"; return true;
            case "praestoe": zoneId = "DK-Z06-PRA"; return true;
            case "randers": zoneId = "DK-Z14-RAN"; return true;
            case "ribe": zoneId = "DK-Z20-RIB"; return true;
            case "ringkoebing": zoneId = "DK-Z19-RIN"; return true;
            case "skanderborg": zoneId = "DK-Z12-SKA"; return true;
            case "soroe": zoneId = "DK-Z05-SOR"; return true;
            case "svendborg": zoneId = "DK-Z10-SVE"; return true;
            case "thisted": zoneId = "DK-Z18-THI"; return true;
            case "vejle": zoneId = "DK-Z11-VEJ"; return true;
            case "viborg": zoneId = "DK-Z15-VIB"; return true;
            default: return false;
        }
    }
}
