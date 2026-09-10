using UnityEngine;

/// <summary>
/// Presentation-only constants for the campaign2 Denmark 3D diorama.
/// Geographic identity remains latitude/longitude. Unity Y is never used for ETA.
/// </summary>
public static class Campaign2Config
{
    public const string BuildTag = "v00.00.16-C2";
    public const string BuildName = "CAMPAIGN2 3D DENMARK DIORAMA";
    public const string RootName = "C2_DENMARK_DIORAMA";
    public const string LandObjectName = "C2_Land";
    public const string WaterObjectName = "C2_Water";

    // 1864 theatre: Denmark + Schleswig-Holstein + Bornholm.
    public const double MinLatitude = 53.38;
    public const double MaxLatitude = 57.90;
    public const double MinLongitude = 8.05;
    public const double MaxLongitude = 15.22;

    public const int GridX = 360;
    public const int GridZ = 260;

    public const float WaterY = 0.00f;
    public const float BeachLift = 0.12f;

    // Visual-only scale. 170 m real relief * 0.028 = 4.76 Unity units.
    public const float MetresToVisualY = 0.028f;

    public const float CameraMinHeight = 5.5f;
    public const float CameraMaxHeight = 120f;
    public const float CameraClearance = 1.8f;
    public const float CameraMinPitch = 25f;
    public const float CameraMaxPitch = 70f;
    public const float CameraHomePitch = 50f;
    public const float CameraHomeHeight = 46f;
    public const float CameraHomeSouth = 36f;

    public static readonly Color WaterColor = new Color(0.18f, 0.38f, 0.46f, 1f);
    public static readonly Color SkyColor = new Color(0.55f, 0.70f, 0.78f, 1f);
    public static readonly Color FogColor = new Color(0.62f, 0.73f, 0.76f, 1f);
    public static readonly Color SunColor = new Color(1.00f, 0.93f, 0.80f, 1f);
}
