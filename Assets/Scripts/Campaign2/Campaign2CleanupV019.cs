using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v00.00.19-C2 presentation cleanup on campaign2 only.
/// Hides Nordic nodes that the Denmark-first projection dumps onto Jutland.
/// </summary>
[DefaultExecutionOrder(32000)]
public sealed class Campaign2CleanupV019 : MonoBehaviour
{
    static readonly string[] HideIds =
    {
        "STOCKHOLM", "ABO", "HELSINGFORS", "CHRISTIANIA", "TAMMERFORS",
        "VASA", "BJORNEBORG", "TAVASTEHUS", "DRAMMEN", "FREDRIKSHALD",
        "GOTEBORG", "JONKOPING"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (FindAnyObjectByType<Campaign2CleanupV019>() != null)
            return;
        new GameObject("Campaign2CleanupV019").AddComponent<Campaign2CleanupV019>();
    }

    void LateUpdate()
    {
        HideNamed("CampaignVersionHud_v0003");
        HideNamed("CampaignBuildVersionOverlay_v0018c2");
        HideNamed("CampaignBuildVersionOverlay_v0013m");

        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || string.IsNullOrEmpty(go.name))
                continue;
            for (int k = 0; k < HideIds.Length; k++)
            {
                if (go.name.IndexOf(HideIds[k], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    go.SetActive(false);
                    break;
                }
            }
        }
    }

    static void HideNamed(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            go.SetActive(false);
    }
}
