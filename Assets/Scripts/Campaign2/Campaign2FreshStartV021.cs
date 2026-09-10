using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v00.00.21-C2 — start labels/layers forfra på campaign2.
/// Slukker legacy HUD, node-labels, control-cylindere og gamle v13-lag.
/// Dioramaets egne bynavne (København, Aarhus, …) er det eneste kort-lag.
/// </summary>
[DefaultExecutionOrder(33000)]
public sealed class Campaign2FreshStartV021 : MonoBehaviour
{
    static readonly string[] HidePrefixes =
    {
        "CampaignControl_", "CampaignNode_", "CampaignFormation_",
        "StrategicLink_", "CampaignRouteGhost", "V013", "GIS_", "GEO_",
        "Outline_", "CampaignMapUsability", "CampaignMapSearchHover",
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

    void Start()
    {
        Strip();
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
        Disable<CampaignUnifiedDenmark3DMapV013M>();
        Disable<CampaignHistorical3DMapV013J>();

        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || string.IsNullOrEmpty(go.name))
                continue;
            if (go.name == "Campaign2FreshStartV021" || go.name == "C2_DENMARK_DIORAMA")
                continue;
            if (go.name.StartsWith("C2_", StringComparison.Ordinal))
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
            found[i].enabled = false;
            if (found[i].gameObject.name != "CampaignMap")
                found[i].gameObject.SetActive(false);
        }
    }
}
