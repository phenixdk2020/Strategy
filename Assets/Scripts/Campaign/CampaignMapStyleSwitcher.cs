using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PROJECT 1864 Campaign v00.00.10h
/// TRUE 11 BASEMAP LAB — provider switcher and unified QA UI.
///
/// WGS84 longitude/latitude remains authoritative campaign geography. Basemap
/// providers are presentation layers; city/zone/army state remains shared.
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
    private GUIStyle loadingStyle;

    private const string LabRootName = "PROJECT1864_TRUE_11_BASEMAP_LAB_v000010h";

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
        // GrandCampaignBootstrap creates the local WGS84 campaign camera/geography.
        for (int i = 0; i < 180; i++)
        {
            localCamera = Camera.main;
            if (localCamera != null && GameObject.Find("GEO_Denmark_NaturalEarth50m") != null)
                break;
            yield return null;
        }

        BuildProviderDefinitions();
        CreateProviderRoots();
        HideLegacyBasemap();

        ready = localCamera != null && providers.Count == 11;
        if (!ready)
        {
            Debug.LogError("CAMPAIGN-10H|Installed=False|Reason=CampaignCameraOrProviderInitFailed");
            yield break;
        }

        ActivateProvider(0);

        Debug.Log(
            "CAMPAIGN-10H|Installed=True|Mode=True11Basemaps|Providers=11|" +
            "WGS84=True|GameplayLayerShared=True|RasterResume=True|AtomicRasterReveal=True");
    }

    private void BuildProviderDefinitions()
    {
        providers.Clear();

        providers.Add(new Provider
        {
            Id = 1,
            Button = "1 DEM",
            Name = "1. DHM/GeoDanmark target — streamed DEM pilot",
            Source = "Mapzen Terrain Tiles / AWS Terrarium elevation + v10g/v10h hydrology cut",
            Attribution = "Terrain Tiles: Mapzen / AWS Open Data.",
            Requirement = "No key for pilot. Official Danish DHM/GeoDanmark remains production target.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.DemHydrology
        });

        providers.Add(new Provider
        {
            Id = 2,
            Button = "2 Cesium",
            Name = "2. Cesium World Terrain + imagery",
            Source = "Cesium World Terrain ion asset 1 + Bing Maps Aerial ion asset 2",
            Attribution = "Cesium ion / Bing Maps credits rendered by Cesium.",
            Requirement = "CESIUM_ION_TOKEN, local ion-token.txt, or Cesium project default token.",
            Kind = ProviderKind.CesiumIon
        });

        providers.Add(new Provider
        {
            Id = 3,
            Button = "3 ArcGIS",
            Name = "3. ArcGIS World Topographic basemap",
            Source = "Esri ArcGIS World_Topo_Map XYZ service",
            Attribution = "Esri and contributing data providers.",
            Requirement = "Network access. Comparison provider; not 1851 historical truth.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 4,
            Button = "4 MapTiler",
            Name = "4. MapTiler 3D Terrain + Cesium",
            Source = "MapTiler quantized-mesh-v2 terrain loaded through Cesium",
            Attribution = "MapTiler data/terrain attribution applies.",
            Requirement = "MAPTILER_API_KEY or PROJECT1864/Keys/maptiler.txt.",
            Kind = ProviderKind.CesiumMapTiler
        });

        providers.Add(new Provider
        {
            Id = 5,
            Button = "5 1842-99",
            Name = "5. Historical Danish high table sheets",
            Source = "Datafordeler Høje målebordsblade WMS, 1:20,000, 1842–1899 survey/issue period",
            Attribution = "Klimadatastyrelsen / Datafordeler historical map service.",
            Requirement = "DATAFORDELER_API_KEY or PROJECT1864/Keys/datafordeler.txt.",
            Kind = ProviderKind.HistoricalWms
        });

        providers.Add(new Provider
        {
            Id = 6,
            Button = "6 QGIS",
            Name = "6. QGIS/Blender baked terrain tiles",
            Source = "Offline baked basemap in StreamingAssets/PROJECT1864/Basemaps/QGIS",
            Attribution = "Depends on layers used in the baked package.",
            Requirement = "qgis-denmark.png + qgis-denmark.bounds required.",
            Kind = ProviderKind.LocalBaked
        });

        providers.Add(new Provider
        {
            Id = 7,
            Button = "7 Procedural",
            Name = "7. Independent procedural Denmark terrain",
            Source = "Runtime procedural landcover/relief generator with hydrology mask",
            Attribution = "PROJECT 1864 generated presentation.",
            Requirement = "No external key.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.Procedural
        });

        providers.Add(new Provider
        {
            Id = 8,
            Button = "8 OSM",
            Name = "8. OpenStreetMap Standard basemap",
            Source = "OpenStreetMap Standard XYZ tiles",
            Attribution = "© OpenStreetMap contributors",
            Requirement = "Network access; local seven-day tile cache; no bulk downloader.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 9,
            Button = "9 Imagery",
            Name = "9. Realistic satellite/terrain comparison",
            Source = "Esri World Imagery XYZ service",
            Attribution = "Esri and imagery/data providers.",
            Requirement = "Network access. Modern comparison imagery, not 1851 historical truth.",
            Kind = ProviderKind.RasterXyz,
            UrlTemplate = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
            Zoom = 8
        });

        providers.Add(new Provider
        {
            Id = 10,
            Button = "10 Diorama",
            Name = "10. Independent 3D diorama/model terrain",
            Source = "PROJECT 1864 generated miniature terrain + physical landscape dressing",
            Attribution = "PROJECT 1864 generated presentation.",
            Requirement = "No external key.",
            Kind = ProviderKind.Generated,
            GeneratedKind = CampaignGeneratedBasemapV010G.GeneratedKind.Diorama
        });

        providers.Add(new Provider
        {
            Id = 11,
            Button = "11 HOI4",
            Name = "11. Independent province/region strategic map",
            Source = "Runtime province cells from campaign-zone centres + Denmark land/water mask",
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

        if (activeIndex >= 0 && IsCesium(providers[activeIndex]))
            RestoreLocalCamera();

        for (int i = 0; i < providers.Count; i++)
        {
            if (providers[i].Root != null)
                providers[i].Root.SetActive(i == index);
        }

        activeIndex = index;
        Provider p = providers[index];
        HideLegacyBasemap();

        if (p.Generated != null) p.Generated.EnsureLoaded();
        if (p.Raster != null) p.Raster.EnsureLoaded();
        if (p.Historical != null) p.Historical.EnsureLoaded();
        if (p.LocalBaked != null) p.LocalBaked.EnsureLoaded();

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
            "CAMPAIGN-10H|Basemap=" + p.Id.ToString("00") +
            "|Name=" + p.Name +
            "|Source=" + p.Source);
    }

    private void RestoreLocalCamera()
    {
        CampaignCesiumBasemapV010G[] cesiumProviders = Object.FindObjectsByType<CampaignCesiumBasemapV010G>();
        for (int i = 0; i < cesiumProviders.Length; i++)
        {
            if (cesiumProviders[i] != null)
                cesiumProviders[i].DeactivateCamera();
        }

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
        {
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
        }
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
        if (p == null) return "UNKNOWN";
        if (p.Raster != null) return p.Raster.Status;
        if (p.Generated != null) return p.Generated.Status;
        if (p.Cesium != null) return p.Cesium.Status;
        if (p.Historical != null) return p.Historical.Status;
        if (p.LocalBaked != null) return p.LocalBaked.Status;
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

        loadingStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        loadingStyle.normal.textColor = Color.white;
    }

    private static void DrawOpaque(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void OnGUI()
    {
        if (!ready || providers.Count != 11 || activeIndex < 0)
            return;

        // Lower GUI.depth draws over legacy v10e IMGUI. This lets v10h reserve a
        // clean build badge and bottom provider toolbar without disabling gameplay.
        GUI.depth = -1000;
        EnsureStyles();

        Provider p = providers[activeIndex];
        string status = ProviderStatus(p);

        // Mask the obsolete v10e build badge only. The campaign clock/speed bar at
        // x >= 230 remains fully visible and interactive.
        DrawOpaque(new Rect(0f, 0f, 228f, 42f), new Color(0.035f, 0.075f, 0.095f, 0.98f));
        GUI.Box(
            new Rect(8f, 8f, 212f, 31f),
            "PROJECT 1864 | v00.00.10h",
            titleStyle);

        float infoWidth = Mathf.Min(690f, Screen.width - 340f);
        float infoX = Screen.width - infoWidth - 8f;

        GUI.Box(
            new Rect(infoX, 42f, infoWidth, 28f),
            "TRUE 11 BASEMAPS | " + (activeIndex + 1) + "/11 | " + status,
            titleStyle);

        GUI.Box(
            new Rect(infoX, 72f, infoWidth, 122f),
            p.Name + "\n" +
            "SOURCE: " + p.Source + "\n" +
            "REQUIREMENT: " + p.Requirement + "\n" +
            "ATTRIBUTION: " + p.Attribution + "\n" +
            "QA: Aalborg/Limfjord skal være geografisk sammenhængende og uden skjult Natural Earth fallback.",
            infoStyle);

        int columns = Screen.width >= 1700 ? 11 : 6;
        int rows = Mathf.CeilToInt(providers.Count / (float)columns);
        float margin = 8f;
        float gap = 3f;
        float buttonHeight = 27f;
        float usable = Screen.width - margin * 2f - gap * (columns - 1);
        float buttonWidth = usable / columns;
        float startY = Screen.height - margin - rows * buttonHeight - (rows - 1) * gap;
        float footerTop = startY - 25f;

        // Opaque toolbar masks the obsolete v10e Natural Earth map-info box and
        // prevents city labels/map-info text from being painted through buttons.
        DrawOpaque(
            new Rect(0f, footerTop - 3f, Screen.width, Screen.height - footerTop + 3f),
            new Color(0.035f, 0.070f, 0.085f, 0.97f));

        GUI.Label(
            new Rect(10f, footerTop, Screen.width - 20f, 20f),
            "Aalborg/Limfjord QA | M / ] næste | [ forrige | Shift+F1…F11 direkte valg | Rasterkort vises samlet når alle tiles er færdige",
            smallStyle);

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

        if (status.StartsWith("LOADING", StringComparison.Ordinal) ||
            status.StartsWith("PAUSED", StringComparison.Ordinal))
        {
            float w = Mathf.Min(440f, Screen.width - 40f);
            GUI.Box(
                new Rect((Screen.width - w) * 0.5f, Screen.height * 0.44f, w, 48f),
                p.Button + " · " + status + "\nKortet vises samlet, når overview-sættet er klar.",
                loadingStyle);
        }
    }
}
