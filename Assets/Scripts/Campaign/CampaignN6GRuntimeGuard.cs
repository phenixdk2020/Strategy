using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// n6g compatibility guard. Older n6f/n6e runtime components still exist in the
/// branch for rollback, but they must never re-enable their grid borders, legacy
/// city sprites or old info panels after n6g has taken ownership.
/// </summary>
[DefaultExecutionOrder(30000)]
public sealed class CampaignN6GRuntimeGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignN6GRuntimeGuard>() != null) return;
        GameObject go = new GameObject("PROJECT1864_N6G_RUNTIME_GUARD");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignN6GRuntimeGuard>();
    }

    private void LateUpdate()
    {
        CampaignHistoricalAmtOverlayV010N6F oldGrid = Object.FindAnyObjectByType<CampaignHistoricalAmtOverlayV010N6F>();
        if (oldGrid != null && oldGrid.enabled)
        {
            oldGrid.StopAllCoroutines();
            oldGrid.enabled = false;
        }

        GameObject gridRoot = GameObject.Find("ZONE_LINES_10N6F_PARISH_GRID");
        if (gridRoot != null) Destroy(gridRoot);

        CampaignCityIconV010N6 oldCity = Object.FindAnyObjectByType<CampaignCityIconV010N6>();
        if (oldCity != null && oldCity.enabled) oldCity.enabled = false;

        CampaignZoneInfoPanelV010N6F oldInfoF = Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6F>();
        if (oldInfoF != null && oldInfoF.enabled) oldInfoF.enabled = false;

        CampaignZoneInfoPanelV010N3 oldInfoN3 = Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>();
        if (oldInfoN3 != null && oldInfoN3.enabled) oldInfoN3.enabled = false;
    }
}
