using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(300)]
public sealed class CampaignAtmosphereV013 : MonoBehaviour
{
    private Light campaignSun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignAtmosphereV013>() != null)
            return;

        GameObject root = new GameObject("CampaignAtmosphereV013");
        root.transform.SetParent(CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootAtmosphere).transform, false);
        root.AddComponent<CampaignAtmosphereV013>();
    }

    private void Start()
    {
        GameObject sunObject = GameObject.Find("Campaign Sun");
        if (sunObject != null)
        {
            campaignSun = sunObject.GetComponent<Light>();
            if (campaignSun != null)
            {
                campaignSun.shadows = LightShadows.Soft;
                campaignSun.shadowStrength = 0.58f;
                campaignSun.shadowBias = 0.045f;
                campaignSun.shadowNormalBias = 0.28f;
            }
        }
    }

    private void Update()
    {
        DateTime now = CampaignSession.CurrentDateTime;
        float hour = (float)now.TimeOfDay.TotalHours;
        float sunWave = Mathf.Sin((hour - 6f) / 24f * Mathf.PI * 2f);
        float daylight = Mathf.Clamp01((sunWave + 0.16f) / 1.16f);
        float golden = Mathf.Clamp01(1f - Mathf.Abs(hour - 18f) / 4.5f) + Mathf.Clamp01(1f - Mathf.Abs(hour - 7f) / 3.5f);
        golden = Mathf.Clamp01(golden);

        if (campaignSun != null)
        {
            campaignSun.intensity = Mathf.Lerp(0.06f, 0.95f, daylight);
            Color daylightColor = Color.Lerp(
                new Color(0.43f, 0.49f, 0.62f),
                new Color(1.00f, 0.94f, 0.82f),
                daylight);
            campaignSun.color = Color.Lerp(daylightColor, new Color(1.00f, 0.72f, 0.48f), golden * 0.22f);

            float pitch = Mathf.Lerp(10f, 58f, daylight);
            float yaw = Mathf.Repeat((hour / 24f) * 360f - 70f, 360f);
            campaignSun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Color nightAmbient = new Color(0.105f, 0.125f, 0.17f);
        Color dayAmbient = new Color(0.46f, 0.49f, 0.45f);
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(
            new Color(0.10f, 0.13f, 0.18f),
            new Color(0.57f, 0.64f, 0.64f),
            daylight);
        RenderSettings.fogDensity = Mathf.Lerp(0.0032f, 0.00082f, daylight);

        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 320f);
    }
}
