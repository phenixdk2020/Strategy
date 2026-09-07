using UnityEngine;

/// <summary>
/// Global-ready geographic projection helper for PROJECT 1864.
/// WGS84 longitude/latitude is authoritative campaign geography.
/// Unity X/Z coordinates are only a render-space projection and may change later
/// without changing stored geographic identities.
/// </summary>
public static class CampaignGeoProjection
{
    // Denmark-first render origin. The data model remains longitude/latitude so
    // later tiles can use another local origin without rewriting campaign state.
    public const float OriginLongitude = 10.20f;
    public const float OriginLatitude = 56.10f;
    public const float UnitsPerLatitudeDegree = 22.0f;

    private static readonly float LongitudeScale =
        Mathf.Cos(OriginLatitude * Mathf.Deg2Rad) * UnitsPerLatitudeDegree;

    public static Vector3 Project(float longitude, float latitude, float y = 0f)
    {
        return new Vector3(
            (longitude - OriginLongitude) * LongitudeScale,
            y,
            (latitude - OriginLatitude) * UnitsPerLatitudeDegree);
    }

    public static Vector2 Project2D(float longitude, float latitude)
    {
        Vector3 p = Project(longitude, latitude, 0f);
        return new Vector2(p.x, p.z);
    }

    public static Vector2 Unproject(Vector3 worldPosition)
    {
        float longitude = worldPosition.x / LongitudeScale + OriginLongitude;
        float latitude = worldPosition.z / UnitsPerLatitudeDegree + OriginLatitude;
        return new Vector2(longitude, latitude);
    }
}
