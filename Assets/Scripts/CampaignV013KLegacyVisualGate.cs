using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// v00.00.13k hard gate for prototype render trees that conflict with live cartography.
[DefaultExecutionOrder(4460)]
public sealed class CampaignV013KLegacyVisualGate : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (UnityEngine.Object.FindAnyObjectByType<CampaignV013KLegacyVisualGate>() != null)
            return;
        new GameObject("CampaignV013KLegacyVisualGate").AddComponent<CampaignV013KLegacyVisualGate>();
    }

    private void Update()
    {
        CampaignHistoricalMapLabelsV013J labels = UnityEngine.Object.FindAnyObjectByType<CampaignHistoricalMapLabelsV013J>();
        if (labels != null) labels.enabled = false;

        HideChildTree("V013J_HISTORICAL_3D_MAP", "L5_SMALL_SETTLEMENTS");
        HideChildTree("V013J_HISTORICAL_3D_MAP", "L6_SMALL_CONSTRUCTION");
        HideChildTree("V013I_DENMARK_GIS_FOUNDATION", "L4_HISTORICAL_INFRASTRUCTURE_PILOT");
        HideChildTree("V013I_DENMARK_GIS_FOUNDATION", "L5_SETTLEMENTS");
        HideChildTree("V013I_DENMARK_GIS_FOUNDATION", "L7_DIORAMA_DETAILS");
    }

    private static void HideChildTree(string rootName, string childName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) return;
        Transform child = root.transform.Find(childName);
        if (child == null) return;

        Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = false;
    }
}
