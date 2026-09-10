using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v00.00.20-C2: Stockholm-labels are IMGUI from CampaignMapController.
/// Nodes outside the Denmark theatre get parked off-camera so the label cannot sit on Aarhus.
/// </summary>
[DefaultExecutionOrder(32000)]
public sealed class Campaign2CleanupV019 : MonoBehaviour
{
    static readonly Vector2 Parked = new Vector2(-800f, -800f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (FindAnyObjectByType<Campaign2CleanupV019>() != null)
            return;
        new GameObject("Campaign2CleanupV019").AddComponent<Campaign2CleanupV019>();
    }

    void Start()
    {
        ParkForeignNodes();
    }

    void LateUpdate()
    {
        ParkForeignNodes();
    }

    static void ParkForeignNodes()
    {
        if (CampaignSession.Nodes == null)
            return;
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;
            if (Campaign2Config.InTheatre(node.Latitude, node.Longitude))
                continue;
            node.MapPosition = Parked;
        }
    }
}
