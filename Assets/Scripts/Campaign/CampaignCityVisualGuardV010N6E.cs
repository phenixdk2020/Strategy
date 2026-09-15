using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6e visual guard.
/// Hides legacy round city/zone renderers as soon as the campaign bootstrap exists,
/// independently of Proposal 3 texture import timing. This prevents the old discs
/// from flashing or remaining visible while authored city art is being loaded.
/// Colliders are deliberately untouched here; CampaignCityIconV010N6 owns the
/// final city click collider replacement.
/// </summary>
[DefaultExecutionOrder(23200)]
public sealed class CampaignCityVisualGuardV010N6E : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignCityVisualGuardV010N6E>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_CITY_VISUAL_GUARD_10N6E");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignCityVisualGuardV010N6E>();
    }

    private void Update()
    {
        if (applied || GrandCampaignBootstrap.Instance == null)
            return;

        int hiddenCityRenderers = 0;
        GrandCampaignCityMarker[] cities =
            Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i] == null)
                continue;

            Renderer renderer = cities[i].GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                renderer.enabled = false;
                hiddenCityRenderers++;
            }
        }

        int hiddenZoneRenderers = 0;
        GrandCampaignZoneMarker[] zones =
            Object.FindObjectsByType<GrandCampaignZoneMarker>(FindObjectsInactive.Exclude);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null)
                continue;

            Renderer renderer = zones[i].GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                renderer.enabled = false;
                hiddenZoneRenderers++;
            }
        }

        applied = true;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|LegacyRoundVisualGuard=True" +
            "|CityRenderersHidden=" + hiddenCityRenderers +
            "|ZoneCentreRenderersHidden=" + hiddenZoneRenderers);
    }
}
