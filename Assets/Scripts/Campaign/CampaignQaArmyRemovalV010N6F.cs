using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6f cleanup.
/// Removes the temporary DK-ARMY-QA campaign marker that was used during early
/// movement tests. The underlying prototype bootstrap can keep its legacy test
/// record for rollback compatibility, but no QA army is visible or selectable in
/// normal campaign play.
/// </summary>
[DefaultExecutionOrder(23700)]
public sealed class CampaignQaArmyRemovalV010N6F : MonoBehaviour
{
    private const string QaArmyId = "DK-ARMY-QA";
    private bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignQaArmyRemovalV010N6F>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_QA_ARMY_CLEANUP_10N6F");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignQaArmyRemovalV010N6F>();
    }

    private void Update()
    {
        if (done || GrandCampaignBootstrap.Instance == null)
            return;

        GrandCampaignArmyMarker[] markers =
            Object.FindObjectsByType<GrandCampaignArmyMarker>(FindObjectsInactive.Include);

        bool found = false;
        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignArmyMarker marker = markers[i];
            if (marker == null || marker.ArmyId != QaArmyId)
                continue;

            found = true;
            Destroy(marker.gameObject);
        }

        // Bootstrap creates the QA marker synchronously in Awake, so one scan after
        // GrandCampaignBootstrap is present is sufficient.
        done = true;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|QaArmyRemoved=" + found +
            "|Army=" + QaArmyId +
            "|VisibleQaArmy=False");
    }
}
