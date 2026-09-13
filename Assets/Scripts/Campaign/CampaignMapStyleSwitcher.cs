using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10k
/// IMAGERY ONLY + STREAMED 3D TERRAIN.
///
/// World Imagery remains the sole Campaign3 basemap. The v10k terrain layer is
/// independent of gameplay state and deforms the streamed imagery meshes after
/// their image tiles exist. WGS84 gameplay remains authoritative.
/// </summary>
[DefaultExecutionOrder(-29000)]
public sealed class CampaignMapStyleSwitcher : MonoBehaviour
{
    public static CampaignMapStyleSwitcher Instance { get; private set; }

    private const string RootName = "PROJECT1864_IMAGERY_TERRAIN_v000010k";
    private const string ImageryRootName = "BASEMAP_09_9_Imagery";
    private const string ImageryUrl =
        "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}";

    private Camera campaignCamera;
    private GameObject imageryRoot;
    private CampaignRasterBasemapV010G imagery;
    private bool ready;

    public CampaignRasterBasemapV010G Imagery { get { return imagery; } }
    public bool Ready { get { return ready; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignMapStyleSwitcher>() != null)
            return;

        GameObject go = new GameObject(RootName);
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignMapStyleSwitcher>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 240; i++)
        {
            campaignCamera = Camera.main;
            if (campaignCamera != null &&
                GameObject.Find("GEO_Denmark_NaturalEarth50m") != null)
            {
                break;
            }

            yield return null;
        }

        if (campaignCamera == null)
        {
            Debug.LogError("CAMPAIGN-10K|Installed=False|Reason=MainCameraMissing");
            yield break;
        }

        HideLegacyBasemap();
        CreateImageryBasemap();

        if (imagery == null)
        {
            Debug.LogError("CAMPAIGN-10K|Installed=False|Reason=ImageryProviderCreateFailed");
            yield break;
        }

        ready = true;
        imagery.EnsureLoaded();

        Debug.Log(
            "CAMPAIGN-10K|Installed=True|Basemap=WorldImageryOnly|" +
            "WorldStreaming=True|TerrainLayer=TerrariumElevation|" +
            "WGS84GameplayShared=True|OtherProvidersInstantiated=False");
    }

    private void CreateImageryBasemap()
    {
        imageryRoot = new GameObject(ImageryRootName);
        imageryRoot.transform.SetParent(transform, false);

        imagery = imageryRoot.AddComponent<CampaignRasterBasemapV010G>();
        imagery.Configure(
            9,
            "World Imagery",
            ImageryUrl,
            8,
            string.Empty,
            string.Empty,
            "Esri and imagery/data providers.");
    }

    private static void HideLegacyBasemap()
    {
        SetRenderers("GEO_Denmark_NaturalEarth50m", false);
        SetRenderers("Grand Campaign Sea", false);
    }

    private static void SetRenderers(string rootName, bool enabled)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
        }
    }

    private void LateUpdate()
    {
        if (ready)
            HideLegacyBasemap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
