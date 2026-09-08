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
            campaignSun = sunObject.GetComponent<Light>();
    }

    private void Update()
    {
        DateTime now = CampaignSession.CurrentDateTime;
        float hour = (float)now.TimeOfDay.TotalHours;
        float sunWave = Mathf.Sin((hour - 6f) / 24f * Mathf.PI * 2f);
        float daylight = Mathf.Clamp01((sunWave + 0.18f) / 1.18f);

        if (campaignSun != null)
        {
            campaignSun.intensity = Mathf.Lerp(0.08f, 1.05f, daylight);
            campaignSun.color = Color.Lerp(new Color(0.44f, 0.50f, 0.66f), new Color(1f, 0.96f, 0.88f), daylight);
            float pitch = Mathf.Lerp(12f, 62f, daylight);
            float yaw = Mathf.Repeat((hour / 24f) * 360f - 70f, 360f);
            campaignSun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.15f, 0.17f, 0.22f),
            new Color(0.58f, 0.60f, 0.62f),
            daylight);

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(
            new Color(0.12f, 0.15f, 0.20f),
            new Color(0.63f, 0.68f, 0.70f),
            daylight);
        RenderSettings.fogDensity = Mathf.Lerp(0.0035f, 0.0011f, daylight);
    }
}
