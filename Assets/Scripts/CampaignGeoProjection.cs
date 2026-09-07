using UnityEngine;

public static class CampaignGeoProjection
{
    // Campaign coverage: Germany -> Denmark -> Norway/Sweden -> Finland.
    // Equirectangular projection is sufficient for the first real-geography MVP;
    // data remains stored as latitude/longitude so projection can later be replaced.
    public const double MinLatitude = 47.0;
    public const double MaxLatitude = 71.5;
    public const double MinLongitude = 4.0;
    public const double MaxLongitude = 32.5;

    public const float MapWidth = 520f;
    public const float MapDepth = 620f;

    public static Vector2 Project(double latitude, double longitude)
    {
        double clampedLat = System.Math.Max(MinLatitude, System.Math.Min(MaxLatitude, latitude));
        double clampedLon = System.Math.Max(MinLongitude, System.Math.Min(MaxLongitude, longitude));

        double lon01 = (clampedLon - MinLongitude) / (MaxLongitude - MinLongitude);
        double lat01 = (clampedLat - MinLatitude) / (MaxLatitude - MinLatitude);

        float x = (float)((lon01 - 0.5) * MapWidth);
        float z = (float)((lat01 - 0.5) * MapDepth);
        return new Vector2(x, z);
    }

    public static Vector3 Project3D(double latitude, double longitude, float height = 0f)
    {
        Vector2 map = Project(latitude, longitude);
        return new Vector3(map.x, height, map.y);
    }

    public static float ApproximateDistanceKm(
        double latitudeA,
        double longitudeA,
        double latitudeB,
        double longitudeB)
    {
        const double EarthRadiusKm = 6371.0;
        double lat1 = latitudeA * System.Math.PI / 180.0;
        double lat2 = latitudeB * System.Math.PI / 180.0;
        double dLat = (latitudeB - latitudeA) * System.Math.PI / 180.0;
        double dLon = (longitudeB - longitudeA) * System.Math.PI / 180.0;

        double sinLat = System.Math.Sin(dLat * 0.5);
        double sinLon = System.Math.Sin(dLon * 0.5);
        double a = sinLat * sinLat +
                   System.Math.Cos(lat1) * System.Math.Cos(lat2) * sinLon * sinLon;
        double c = 2.0 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1.0 - a));
        return (float)(EarthRadiusKm * c);
    }
}
