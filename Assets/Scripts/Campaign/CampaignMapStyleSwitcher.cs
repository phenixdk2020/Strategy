using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10g
/// TRUE 11 BASEMAP LAB.
///
/// v10f compared eleven visual styles on one Natural Earth 1:50m mesh.
/// v10g instead owns eleven independent basemap provider roots. Providers may
/// use streamed raster tiles, streamed DEM, Cesium, generated tactical terrain,
/// local historical/QGIS assets, or a province-map generator.
///
/// WGS84 longitude/latitude remains authoritative. Gameplay markers are kept
/// separate from basemap presentation.
/// </summary>
[DefaultExecutionOrder(-29000)]
public sealed class CampaignMapStyleSwitcher : MonoBehaviour
{
    private enum ProviderKind
    {
        Generated,
        RasterXyz,
        CesiumIon,
        CesiumMapTiler,
        HistoricalWms,
        LocalBaked
    }

    private sealed class Provider
    {
        public int Id;
        public string Button;
        public string Name;
        public string Source;
        public string Attribution;
        public string Requirement;
        public ProviderKind Kind;
        public CampaignGeneratedBasemapV010G.GeneratedKind GeneratedKind;
        public string UrlTemplate;
        public string KeyEnvironment;
        public string KeyFile;
        public int Zoom;
        public GameObject Root;
        public CampaignRasterBasemapV010G Raster;
        public CampaignGeneratedBasemapV010G Generated;
        public CampaignCesiumBasemapV010G Cesium;
        public CampaignHistoricalWmsV010G Historical;
        public CampaignLocalBakedBasemapV010G LocalBaked;
    }

    private readonly List<Provider> providers = new List<Provider>();
    private Camera localCamera;
    private int activeIndex = -1;
    private bool ready;
    private GUIStyle titleStyle;
    private GUIStyle infoStyle;
    private GUIStyle smallStyle;

    private const string LabRootName = "PROJECT1864_TRUE_11_BASEMAP_LAB_v000010g";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignMapStyleSwitcher>() != null)
            return;

        GameObject go = new GameObject(LabRootName);
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignMapStyleSwitcher>();
    }

    private IEnumerator Start()
    {
        // GrandCampaignBootstrap builds the local WGS84 campaign first.
        for (int i = 0; i < 180; i++)
        {
            localCamera = Camera.main;
            if (localCamera != null &&
                GameObject.Find("GEO_Denmark_NaturalEarth50m") != null)
            {
                break;
            }

            yield return null;
        }

        BuildProviderDefinitions();
        CreateProviderRoots();
        HideLegacyBasemap();

        ready = localCamera != null && providers.Count == 11;
        if (!ready)
        {
            Debug.LogError("CAMPAIGN-10G|Installed=False|Reason=CampaignCameraOrProviderInitFailed");
            yield break;
        }

        // Start on provider 1 because it is the first genuinely independent
        // terrain generator and includes the Limfjord hydrology QA cut.
        ActivateProvider(0);

        Debug.Log(
            "CAMPAIGN-10G|Installed=True|Mode=True11Basemaps|Providers=11|" +
            "WGS84=True|GameplayLayerShared=True|Version=v00.00.10g");
    }

    private void BuildProviderDefinitions()
    {
        providers.Clear();

        providers.Add(new Provider
        {
            Id = 1,
            Button = "1 DEM",
            Name = "1. DHM/GeoDanmark target — streamed DEM pilot",
            Source = "Mapzen Terrain Tiles / AWS Terrarium for elevation; v10g hydrology cut; DHM/GeoDanmark adapter target",
            Attribution = "Terrain Tiles: Mapzen / AWS Open Data. Official Danish DHM/GeoDanmark requires Datafordeler credentials.",
            Requirement = "Runs without key in pilot mode. DATAFORDELER_API_KEY will be used by later official-DHM adapter.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.DemHydrology
        });

        providers.Add(new Provider
        {
            Id = 2,
            Button = "2 Cesium",
            Name = "2. Cesium World Terrain + imagery",
            Source = "Cesium World Terrain (ion asset 1) + Bing Maps Aerial (ion asset 2)",
            Attribution = "Cesium ion / Bing Maps imagery credits are rendered by Cesium.",
            Requirement = "CESIUM_ION_TOKEN env, local PROJECT1864/Cesium/ion-token.txt, or Cesium Project Default Token",
            Kind = ProviderKind.CesiumIon
        });

        providers.Add(new Provider
        {
            Id = 3,
            Button = "3 ArcGIS",
            Name = "3. ArcGIS World Topographic basemap",
            Source = "Esri ArcGIS World_Topo_Map cached XYZ service",
            Attribution = "Esri and contributing data providers.",
            Requirement = "Network access. Comparison provider only; production licensing/API configuration must be reviewed.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 4,
            Button = "4 MapTiler",
            Name = "4. MapTiler 3D Terrain + Cesium",
            Source = "MapTiler quantized-mesh-v2 terrain loaded through Cesium from URL",
            Attribution = "MapTiler data/terrain attribution applies.",
            Requirement = "MAPTILER_API_KEY env or PROJECT1864/Keys/maptiler.txt",
            Kind = ProviderKind.CesiumMapTiler
        });

        providers.Add(new Provider
        {
            Id = 5,
            Button = "5 1842-99",
            Name = "5. Historical Danish high table sheets",
            Source = "Datafordeler Høje målebordsblade WMS, 1:20,000, 1842–1899 survey/issue period",
            Attribution = "Klimadatastyrelsen / Datafordeler historical map service.",
            Requirement = "DATAFORDELER_API_KEY env or PROJECT1864/Keys/datafordeler.txt",
            Kind = ProviderKind.HistoricalWms
        });

        providers.Add(new Provider
        {
            Id = 6,
            Button = "6 QGIS",
            Name = "6. QGIS/Blender baked terrain tiles",
            Source = "Offline baked Unity basemap supplied through StreamingAssets/PROJECT1864/Basemaps/QGIS",
            Attribution = "Depends on the source layers used in the baked package.",
            Requirement = "Requires qgis-denmark.png plus qgis-denmark.bounds in StreamingAssets.",
            Kind = ProviderKind.LocalBaked
        });

        providers.Add(new Provider
        {
            Id = 7,
            Button = "7 Procedural",
            Name = "7. Independent procedural Denmark terrain",
            Source = "Runtime procedural landcover/relief generator with hydrology mask",
            Attribution = "PROJECT 1864 generated presentation; coastline mask from project geography scaffold.",
            Requirement = "No external key.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.Procedural
        });

        providers.Add(new Provider
        {
            Id = 8,
            Button = "8 OSM",
            Name = "8. OpenStreetMap Standard basemap",
            Source = "OpenStreetMap Standard raster tiles for the currently viewed Denmark overview",
            Attribution = "© OpenStreetMap contributors",
            Requirement = "Network access; cached locally for at least seven days; no offline bulk downloader.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 9,
            Button = "9 Imagery",
            Name = "9. Realistic satellite/terrain hybrid candidate",
            Source = "Esri World Imagery cached XYZ service",
            Attribution = "Esri and imagery/data providers.",
            Requirement = "Network access. Used as comparison imagery, not as 1851 historical truth.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 10,
            Button = "10 Diorama",
            Name = "10. Independent 3D diorama/model terrain",
            Source = "PROJECT 1864 generated miniature terrain mesh + physical landscape dressing",
            Attribution = "PROJECT 1864 generated presentation; geography scaffold is documented separately.",
            Requirement = "No external key.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.Diorama
        });

        providers.Add(new Provider
        {
            Id = 11,
            Button = "11 HOI4",
            Name = "11. Independent province/region strategic map",
            Source = "Runtime province cells generated from campaign-zone centres and a Denmark land/water mask",
            Attribution = "PROJECT 1864 strategic simulation layer; not historical administrative borders.",
            Requirement = "No external key.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.Hoi4
        });
    }

    private void CreateProviderRoots()
    {
        for (int i = 0; i < providers.Count; i++)
        {
            Provider p = providers[i];
            p.Root = new GameObject(string.Format("BASEMAP_{0:00}_{1}", p.Id, Sanitize(p.Button)));
            p.Root.transform.SetParent(transform, false);
            p.Root.SetActive(false);

            switch (p.Kind)
            {
                case ProviderKind.Generated:
                    p.Generated = p.Root.AddComponent<CampaignGeneratedBasemapV010G>();
                    p.Generated.Configure(p.GeneratedKind, p.Id);
                    break;

                case ProviderKind.RasterXyz:
                    p.Raster = p.Root.AddComponent<CampaignRasterBasemapV010G>();
                    p.Raster.Configure(
                        p.Id,
                        p.Name,
                        p.UrlTemplate,
                        p.Zoom,
                        p.KeyEnvironment,
                        p.KeyFile,
                        p.Attribution);
                    break;

                case ProviderKind.CesiumIon:
                    p.Cesium = p.Root.AddComponent<CampaignCesiumBasemapV010G>();
                    p.Cesium.Configure(CampaignCesiumBasemapV010G.CesiumMode.IonWorldTerrain, p.Id);
                    break;

                case ProviderKind.CesiumMapTiler:
                    p.Cesium = p.Root.AddComponent<CampaignCesiumBasemapV010G>();
                    p.Cesium.Configure(CampaignCesiumBasemapV010G.CesiumMode.MapTilerTerrain, p.Id);
                    break;

                case ProviderKind.HistoricalWms:
                    p.Historical = p.Root.AddComponent<CampaignHistoricalWmsV010G>();
                    p.Historical.Configure(p.Id);
                    break;

                case ProviderKind.LocalBaked:
                    p.LocalBaked = p.Root.AddComponent<CampaignLocalBakedBasemapV010G>();
                    p.LocalBaked.Configure(p.Id);
                    break;
            }
        }
    }

    private void Update()
    {
        if (!ready)
            return;

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.RightBracket))
            ActivateProvider((activeIndex + 1) % providers.Count);

        if (Input.GetKeyDown(KeyCode.LeftBracket))
            ActivateProvider((activeIndex - 1 + providers.Count) % providers.Count);

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift)
            return;

        KeyCode[] keys =
        {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11
        };

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                ActivateProvider(i);
                break;
            }
        }
    }

    private void ActivateProvider(int index)
    {
        if (index < 0 || index >= providers.Count)
            return;

        // Save/restore the local camera around globe providers.
        if (activeIndex >= 0 && IsCesium(providers[activeIndex]))
            RestoreLocalCamera();

        for (int i = 0; i < providers.Count; i++)
            if (providers[i].Root != null)
                providers[i].Root.SetActive(i == index);

        activeIndex = index;
        Provider p = providers[index];

        HideLegacyBasemap();

        if (p.Generated != null)
            p.Generated.EnsureLoaded();
        if (p.Raster != null)
            p.Raster.EnsureLoaded();
        if (p.Historical != null)
            p.Historical.EnsureLoaded();
        if (p.LocalBaked != null)
            p.LocalBaked.EnsureLoaded();
        if (p.Cesium != null)
        {
            HideLocalGameplayMarkers(true);
            p.Cesium.EnsureLoaded(localCamera);
        }
        else
        {
            HideLocalGameplayMarkers(false);
            if (localCamera != null && !localCamera.gameObject.activeSelf)
                localCamera.gameObject.SetActive(true);
        }

        Debug.Log(
            "CAMPAIGN-10G|Basemap=" + p.Id.ToString("00") +
            "|Name=" + p.Name +
            "|Source=" + p.Source);
    }

    private void RestoreLocalCamera()
    {
        CampaignCesiumBasemapV010G[] cesiumProviders = Object.FindObjectsByType<CampaignCesiumBasemapV010G>();
        for (int i = 0; i < cesiumProviders.Length; i++)
            if (cesiumProviders[i] != null)
                cesiumProviders[i].DeactivateCamera();

        if (localCamera != null)
        {
            localCamera.gameObject.SetActive(true);
            localCamera.tag = "MainCamera";
        }

        HideLocalGameplayMarkers(false);
    }

    private static bool IsCesium(Provider p)
    {
        return p != null &&
               (p.Kind == ProviderKind.CesiumIon || p.Kind == ProviderKind.CesiumMapTiler);
    }

    private static void HideLegacyBasemap()
    {
        SetRenderers("GEO_Denmark_NaturalEarth50m", false);
        SetRenderers("Grand Campaign Sea", false);
    }

    private static void SetRenderers(string rootName, bool enabled)
    {
        GameObject go = GameObject.Find(rootName);
        if (go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
    }

    private static void HideLocalGameplayMarkers(bool hidden)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            string n = r.gameObject.name;
            if (n.StartsWith("ZONE_", StringComparison.Ordinal) ||
                n.StartsWith("CITY_", StringComparison.Ordinal) ||
                n.StartsWith("ARMY_", StringComparison.Ordinal))
            {
                r.enabled = !hidden;
            }
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "MAP";

        return value.Replace(" ", "_").Replace("/", "_").Replace(".", "_");
    }

    private string ProviderStatus(Provider p)
    {
        if (p == null)
            return "UNKNOWN";

        if (p.Raster != null)
            return p.Raster.Status;
        if (p.Generated != null)
            return p.Generated.Status;
        if (p.Cesium != null)
            return p.Cesium.Status;
        if (p.Historical != null)
            return p.Historical.Status;
        if (p.LocalBaked != null)
            return p.LocalBaked.Status;

        return "INITIALISING";
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        infoStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            wordWrap = true
        };
        infoStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 10
        };
        smallStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!ready || providers.Count != 11 || activeIndex < 0)
            return;

        EnsureStyles();
        Provider p = providers[activeIndex];

        float infoWidth = Mathf.Min(690f, Screen.width - 16f);
        float infoX = Screen.width - infoWidth - 8f;

        GUI.Box(
            new Rect(infoX, 42f, infoWidth, 28f),
            "PROJECT 1864 | v00.00.10g TRUE 11 BASEMAPS | " +
            (activeIndex + 1) + "/11 | " + ProviderStatus(p),
            titleStyle);

        GUI.Box(
            new Rect(infoX, 72f, infoWidth, 104f),
            p.Name + "\n" +
            "SOURCE: " + p.Source + "\n" +
            "REQUIREMENT: " + p.Requirement + "\n" +
            "ATTRIBUTION: " + p.Attribution,
            infoStyle);

        int columns = Screen.width >= 1700 ? 11 : 6;
        int rows = Mathf.CeilToInt(providers.Count / (float)columns);
        float margin = 8f;
        float gap = 3f;
        float buttonHeight = 27f;
        float usable = Screen.width - margin * 2f - gap * (columns - 1);
        float buttonWidth = usable / columns;
        float startY = Screen.height - margin - rows * buttonHeight - (rows - 1) * gap;

        for (int i = 0; i < providers.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            Rect rect = new Rect(
                margin + col * (buttonWidth + gap),
                startY + row * (buttonHeight + gap),
                buttonWidth,
                buttonHeight);

            GUI.enabled = i != activeIndex;
            if (GUI.Button(rect, providers[i].Button))
                ActivateProvider(i);
            GUI.enabled = true;
        }

        GUI.Label(
            new Rect(10f, startY - 40f, Mathf.Min(920f, Screen.width - 20f), 18f),
            "Aalborg/Limfjord er obligatorisk QA-region. Manglende provider-data vises som MISSING/KEY REQUIRED — ingen skjult Natural Earth fallback.",
            smallStyle);

        GUI.Label(
            new Rect(10f, startY - 21f, Mathf.Min(720f, Screen.width - 20f), 18f),
            "M / ] = næste | [ = forrige | Shift+F1...F11 = direkte valg",
            smallStyle);
    }
}
