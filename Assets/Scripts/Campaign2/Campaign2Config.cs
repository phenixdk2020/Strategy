using UnityEngine;

/// <summary>
/// Presentation-only constants for the campaign2 Denmark 3D diorama.
/// Geographic identity remains latitude/longitude. Unity Y is never used for ETA.
/// </summary>
public static class Campaign2Config
{
    public const string BuildTag = "v00.00.19-C2";
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

    public const float MetresToVisualY = 0.018f;

    public const float CameraMinHeight = 5.5f;
    public const float CameraMaxHeight = 120f;
    public const float CameraClearance = 1.8f;
    public const float CameraMinPitch = 25f;
    public const float CameraMaxPitch = 70f;
    public const float CameraHomePitch = 50f;
    public const float CameraHomeHeight = 46f;
    public const float CameraHomeSouth = 36f;

    public static readonly Color WaterColor = new Color(0.16f, 0.32f, 0.38f, 1f);
    public static readonly Color SkyColor = new Color(0.42f, 0.48f, 0.50f, 1f);
    public static readonly Color FogColor = new Color(0.50f, 0.58f, 0.58f, 1f);
    public static readonly Color SunColor = new Color(1.00f, 0.93f, 0.80f, 1f);
}
