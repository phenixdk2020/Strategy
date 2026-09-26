using UnityEngine;

/// <summary>A clickable city ball. Keeps a roughly constant on-screen size with a gentle growth when zoomed in.</summary>
public sealed class CampaignMap1851CityMarker : MonoBehaviour
{
    public Map1851City Data;
    public bool IsForeign;

    public static float SizeClass(int population)
    {
        if (population > 50000) return 1.9f;
        if (population > 20000) return 1.5f;
        if (population > 10000) return 1.25f;
        if (population > 5000) return 1.0f;
        return 0.75f;
    }

    public void UpdateScale(float cameraDistance)
    {
        // Diameter in km: ~9 px for a 5-10k town on a 1080p screen, growing modestly when close.
        float zoomGrowth = Mathf.Lerp(1.8f, 1f, Mathf.InverseLerp(25f, 400f, cameraDistance));
        float diameter = 0.0042f * cameraDistance * SizeClass(Data.pop) * zoomGrowth;
        transform.localScale = Vector3.one * diameter;
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, BaseHeight + diameter * 0.35f, p.z);
    }

    private float? baseHeight;
    private float BaseHeight => baseHeight ?? (baseHeight = transform.position.y).Value;
}
