using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Land/water classification and visual elevation for campaign2.
/// v00.00.19-C2: low readable relief, no fake inland volcanoes.
/// </summary>
public static class Campaign2Geography
{
    private static Vector2[][] landRings;
    private static Vector2[][] holeRings;
    private static RingBounds[] landBounds;
    private static RingBounds[] holeBounds;

    public static void EnsureInitialized()
    {
        if (landRings != null)
            return;

        List<Vector2[]> lands = new List<Vector2[]>();
        Vector2[][] denmark = ReadDenmarkRings();
        if (denmark != null)
        {
            for (int i = 0; i < denmark.Length; i++)
                lands.Add(denmark[i]);
        }

        lands.Add(SchleswigHolsteinRing());
        lands.Add(FehmarnRing());
        landRings = lands.ToArray();
        landBounds = BuildBounds(landRings);

        holeRings = new[] { LimfjordHoleRing() };
        holeBounds = BuildBounds(holeRings);
    }

    public static bool IsLand(double latitude, double longitude)
    {
        EnsureInitialized();
        Vector2 p = new Vector2((float)longitude, (float)latitude);
        if (InsideAny(p, holeRings, holeBounds))
            return false;
        return InsideAny(p, landRings, landBounds);
    }

    public static float ElevationMeters(double latitude, double longitude, float inland01)
    {
        float lat = (float)latitude;
        float lon = (float)longitude;
        float h = 8f + inland01 * 18f;
        h += 22f * Peak(lat, lon, 56.00f, 9.58f, 0.42f);
        h += 14f * Peak(lat, lon, 56.16f, 9.92f, 0.22f);
        h += 16f * Peak(lat, lon, 55.34f, 10.18f, 0.18f);
        h += 14f * Peak(lat, lon, 55.53f, 11.68f, 0.26f);
        h += 18f * Peak(lat, lon, 54.907f, 9.758f, 0.06f);
        h += 12f * Peak(lat, lon, 55.57f, 9.75f, 0.08f);
        h += 28f * Peak(lat, lon, 55.13f, 14.92f, 0.14f);
        h += 16f * Peak(lat, lon, 54.20f, 9.80f, 0.32f);
        h += 12f * Peak(lat, lon, 54.47f, 9.46f, 0.14f);
        float detail = Mathf.PerlinNoise(lon * 3.1f + 4.2f, lat * 3.1f + 1.7f);
        h += (detail - 0.5f) * 6f;
        return Mathf.Clamp(h, 2.5f, 48f);
    }

    public static Color LandCoverColor(double latitude, double longitude, float inland01, float elevationMeters)
    {
        float lat = (float)latitude;
        float lon = (float)longitude;
        Color sand = new Color(0.72f, 0.66f, 0.48f);
        Color field = new Color(0.58f, 0.56f, 0.38f);
        Color rich = new Color(0.40f, 0.48f, 0.30f);
        Color heath = new Color(0.52f, 0.44f, 0.32f);
        Color highland = new Color(0.56f, 0.50f, 0.36f);

        Color c = Color.Lerp(field, rich, inland01);
        if (lon < 9.15f && lat > 55.3f && lat < 57.3f)
            c = Color.Lerp(c, heath, 0.45f);
        if (elevationMeters > 28f)
            c = Color.Lerp(c, highland, Mathf.InverseLerp(28f, 48f, elevationMeters));
        if (inland01 < 0.34f)
            c = Color.Lerp(sand, c, inland01 / 0.34f);
        float noise = Mathf.PerlinNoise(lon * 8.4f, lat * 8.4f);
        c *= 0.94f + (noise - 0.5f) * 0.08f;
        return c;
    }

    private static Vector2[][] ReadDenmarkRings()
    {
        FieldInfo field = typeof(CampaignDenmarkGeography).GetField(
            "DenmarkRings",
            BindingFlags.Static | BindingFlags.NonPublic);
        return field != null ? field.GetValue(null) as Vector2[][] : null;
    }

    private static Vector2[] SchleswigHolsteinRing()
    {
        return Ring(new float[]
        {
            8.68f,54.90f, 8.52f,54.72f, 8.40f,54.50f, 8.32f,54.32f,
            8.48f,54.20f, 8.66f,54.10f, 8.78f,53.96f, 8.95f,53.88f,
            9.25f,53.80f, 9.55f,53.70f, 9.82f,53.58f, 10.00f,53.54f,
            10.22f,53.57f, 10.48f,53.68f, 10.72f,53.86f, 10.88f,54.02f,
            10.96f,54.18f, 10.84f,54.34f, 10.52f,54.42f, 10.28f,54.40f,
            10.18f,54.34f, 10.12f,54.36f, 10.14f,54.46f, 10.08f,54.50f,
            9.82f,54.52f, 9.58f,54.50f, 9.46f,54.62f, 9.40f,54.76f,
            9.48f,54.82f, 9.68f,54.84f, 9.42f,54.86f, 9.12f,54.88f,
            8.88f,54.90f
        });
    }

    private static Vector2[] FehmarnRing()
    {
        return Ring(new float[]
        {
            11.05f,54.42f, 11.08f,54.48f, 11.18f,54.53f, 11.30f,54.51f,
            11.35f,54.45f, 11.22f,54.41f, 11.12f,54.40f
        });
    }

    private static Vector2[] LimfjordHoleRing()
    {
        return Ring(new float[]
        {
            8.38f,56.72f, 8.22f,56.80f, 8.20f,56.92f, 8.38f,56.99f,
            8.78f,56.97f, 9.25f,56.94f, 9.70f,56.98f, 10.05f,57.05f,
            10.22f,57.00f, 10.18f,56.92f, 9.85f,56.88f, 9.35f,56.86f,
            8.82f,56.80f, 8.52f,56.74f
        });
    }

    private static Vector2[] Ring(float[] values)
    {
        Vector2[] result = new Vector2[values.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
        return result;
    }

    private static RingBounds[] BuildBounds(Vector2[][] rings)
    {
        RingBounds[] bounds = new RingBounds[rings.Length];
        for (int r = 0; r < rings.Length; r++)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            Vector2[] ring = rings[r];
            for (int i = 0; i < ring.Length; i++)
            {
                minX = Mathf.Min(minX, ring[i].x);
                maxX = Mathf.Max(maxX, ring[i].x);
                minY = Mathf.Min(minY, ring[i].y);
                maxY = Mathf.Max(maxY, ring[i].y);
            }
            bounds[r] = new RingBounds(minX, maxX, minY, maxY);
        }
        return bounds;
    }

    private static bool InsideAny(Vector2 p, Vector2[][] rings, RingBounds[] bounds)
    {
        for (int r = 0; r < rings.Length; r++)
        {
            if (!bounds[r].Contains(p))
                continue;
            if (PointInRing(p, rings[r]))
                return true;
        }
        return false;
    }

    private static bool PointInRing(Vector2 p, Vector2[] ring)
    {
        bool inside = false;
        int j = ring.Length - 1;
        for (int i = 0; i < ring.Length; i++)
        {
            Vector2 a = ring[i];
            Vector2 b = ring[j];
            if (((a.y > p.y) != (b.y > p.y)) &&
                (p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + 0.0000001f) + a.x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private static float Peak(float lat, float lon, float cLat, float cLon, float radiusDeg)
    {
        float d = Mathf.Sqrt((lat - cLat) * (lat - cLat) + (lon - cLon) * (lon - cLon));
        float t = Mathf.Clamp01(1f - d / radiusDeg);
        return t * t * (3f - 2f * t);
    }

    private readonly struct RingBounds
    {
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinY;
        public readonly float MaxY;

        public RingBounds(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public bool Contains(Vector2 p)
        {
            return p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY;
        }
    }
}
