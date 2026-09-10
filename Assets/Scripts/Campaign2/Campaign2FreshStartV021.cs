using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(33000)]
public sealed class Campaign2FreshStartV021 : MonoBehaviour
{
    static readonly string[] HidePrefixes =
    {
        "CampaignControl_", "CampaignNode_", "CampaignFormation_",
        "StrategicLink_", "CampaignRouteGhost",
        "CampaignMapUsability", "CampaignMapSearchHover",
        "CampaignHistoricalMapLabels"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (FindAnyObjectByType<Campaign2FreshStartV021>() != null)
            return;
        new GameObject("Campaign2FreshStartV021").AddComponent<Campaign2FreshStartV021>();
    }

    void LateUpdate()
    {
        Strip();
    }

    static void Strip()
    {
        Disable<CampaignMapUsabilityV011>();
        Disable<CampaignMapSearchHoverV011>();
        Disable<CampaignHistoricalMapLabelsV013J>();
        Disable<CampaignVisualPolishV013A>();
        Disable<CampaignVisualPolishV013B>();
        Disable<CampaignLayerManagerV013>();
        Disable<CampaignMapOnlyModeV013H1>();
        Disable<CampaignVersionHud>();
        Disable<CampaignBuildVersionOverlay>();

        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || string.IsNullOrEmpty(go.name))
                continue;
            if (go.GetComponent<Camera>() != null)
                continue;
            if (go.name.StartsWith("C2_", StringComparison.Ordinal))
                continue;
            if (go.name.StartsWith("Campaign2", StringComparison.Ordinal))
                continue;
            for (int p = 0; p < HidePrefixes.Length; p++)
            {
                if (go.name.StartsWith(HidePrefixes[p], StringComparison.Ordinal))
                {
                    go.SetActive(false);
                    break;
                }
            }
        }
    }

    static void Disable<T>() where T : Behaviour
    {
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Include);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            if (found[i].GetComponent<Camera>() != null)
                continue;
            found[i].enabled = false;
        }
    }
}
