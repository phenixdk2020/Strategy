using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public sealed class Campaign2FreshStartV021 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void MuteControllerEarly()
    {
        // BeforeSceneLoad cannot find instances yet.
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (FindAnyObjectByType<Campaign2FreshStartV021>() != null)
            return;
        new GameObject("Campaign2FreshStartV021").AddComponent<Campaign2FreshStartV021>();
    }

    void Awake()
    {
        Strip();
    }

    void LateUpdate()
    {
        Strip();
    }

    static void Strip()
    {
        DisableKeepObject<CampaignMapController>();
        DisableKeepObject<CampaignMapUsabilityV011>();
        DisableKeepObject<CampaignMapSearchHoverV011>();
        DisableKeepObject<CampaignHistoricalMapLabelsV013J>();
        DisableKeepObject<CampaignVisualPolishV013A>();
        DisableKeepObject<CampaignVisualPolishV013B>();
        DisableKeepObject<CampaignLayerManagerV013>();
        DisableKeepObject<CampaignMapOnlyModeV013H1>();
        DisableKeepObject<CampaignVersionHud>();
        DisableKeepObject<CampaignBuildVersionOverlay>();
    }

    static void DisableKeepObject<T>() where T : Behaviour
    {
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Include);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            found[i].enabled = false;
        }
    }
}
