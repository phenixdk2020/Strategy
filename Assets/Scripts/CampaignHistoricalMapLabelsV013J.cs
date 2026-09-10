using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// campaign2: legacy labels disabled. Class kept so old scene refs still compile.
/// </summary>
[DefaultExecutionOrder(20500)]
public sealed class CampaignHistoricalMapLabelsV013J : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        // Do not spawn. campaign2 owns presentation.
    }

    private void Awake()
    {
        enabled = false;
    }

    private void OnGUI()
    {
    }
}
