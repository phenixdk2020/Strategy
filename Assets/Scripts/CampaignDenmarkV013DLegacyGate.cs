using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs after v13a/v13b have produced reusable settlement/construction dressing,
// but before v13c can execute its obsolete remap path.
[DefaultExecutionOrder(2200)]
public sealed class CampaignDenmarkV013DLegacyGate : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkV013DLegacyGate>() != null)
            return;

        GameObject root = new GameObject("CampaignDenmarkV013DLegacyGate");
        root.AddComponent<CampaignDenmarkV013DLegacyGate>();
    }

    private void Start()
    {
        CampaignDenmarkCleanupV013C oldCleanup = UnityEngine.Object.FindAnyObjectByType<CampaignDenmarkCleanupV013C>();
        if (oldCleanup != null)
            oldCleanup.enabled = false;

        DisableByTypeName("CampaignMapUsabilityV011");
        DisableByTypeName("CampaignMapSearchHoverV011");

        Debug.Log("CAMPAIGN-V013D|LegacyGate=True|V013CRemapDisabled=True|V011DiagUI=False|SearchHover=False");
    }

    private static void DisableByTypeName(string typeName)
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
                behaviour.enabled = false;
        }
    }
}
