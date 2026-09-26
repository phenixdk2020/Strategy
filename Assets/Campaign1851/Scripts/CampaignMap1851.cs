using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// v00.00.14 painted campaign map of the Danish monarchy, 1851.
///
/// Runs only in the CampaignMap1851 scene, so none of the v10-v13 CampaignMap layers are
/// involved. Map art, heights and data come from tools/map1851/build_map.py
/// (Resources/Map1851). 1 Unity unit = 1 km; the map is centred on the world origin.
/// </summary>
public sealed class CampaignMap1851 : MonoBehaviour
{
    public const string SceneName = "CampaignMap1851";
    public const string Version = "v00.00.14 MALET DANMARKSKORT 1851 DEV";

    private const string ResourceFolder = "Map1851/";
    private const float HeightScale = 3f;   // ~18x vertical exaggeration; Danish relief reads at close zoom
    private const int GridX = 252;
    private const int GridZ = 342;

    public static CampaignMap1851 Instance { get; private set; }

    public Map1851Data Data { get; private set; }
    public Camera MapCamera { get; private set; }
    public CampaignMap1851Camera CameraRig { get; private set; }
    public CampaignMap1851Ui Ui { get; private set; }
    public Texture2D BornholmTexture { get; private set; }

    private readonly System.Collections.Generic.List<CampaignMap1851CityMarker> markers = new System.Collections.Generic.List<CampaignMap1851CityMarker>();
    private Texture2D heightTexture;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
            return;
        if (FindAnyObjectByType<CampaignMap1851>() != null)
            return;
        new GameObject("CampaignMap1851").AddComponent<CampaignMap1851>();
    }

    private void Awake()
    {
        Instance = this;
        if (Application.isPlaying)
            Build();
    }

    /// <summary>Builds the whole map. Called on Awake in play mode, or by the editor capture tool.</summary>
    public void Build()
    {
        if (built)
            return;
        built = true;
        Instance = this;

        var json = Resources.Load<TextAsset>(ResourceFolder + "Denmark1851_Map");
        // Fully qualified: the project has a global string-only JsonUtility shim (v13k1) that shadows Unity's.
        Data = UnityEngine.JsonUtility.FromJson<Map1851Data>(json.text);
        heightTexture = Resources.Load<Texture2D>(ResourceFolder + "Denmark1851_Height");
        BornholmTexture = Resources.Load<Texture2D>(ResourceFolder + "Bornholm1851_Color");

        BuildTerrain(Resources.Load<Texture2D>(ResourceFolder + "Denmark1851_Color"));
        BuildLighting();
        BuildCamera();
        BuildCities();

        Ui = gameObject.AddComponent<CampaignMap1851Ui>();
        Ui.Build(this);

        Debug.Log($"CAMPAIGN-1851|Version={Version}|Cities={Data.cities.Length}|SizeKm={Data.sizeKm.width:0}x{Data.sizeKm.height:0}");
    }

    // ---------------------------------------------------------------- geography

    public Vector3 Project(float lat, float lon)
    {
        Vector2 uv = Data.extent.ToUv(lat, lon);
        float x = (uv.x - 0.5f) * Data.sizeKm.width;
        float z = (uv.y - 0.5f) * Data.sizeKm.height;
        return new Vector3(x, SampleHeight(uv), z);
    }

    private float SampleHeight(Vector2 uv)
    {
        if (heightTexture == null || uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            return 0f;
        return heightTexture.GetPixelBilinear(uv.x, uv.y).r * HeightScale;
    }

    private void BuildTerrain(Texture2D colour)
    {
        int vx = GridX + 1, vz = GridZ + 1;
        var vertices = new Vector3[vx * vz];
        var uvs = new Vector2[vertices.Length];
        for (int z = 0; z < vz; z++)
        {
            for (int x = 0; x < vx; x++)
            {
                var uv = new Vector2(x / (float)GridX, z / (float)GridZ);
                int i = z * vx + x;
                uvs[i] = uv;
                vertices[i] = new Vector3((uv.x - 0.5f) * Data.sizeKm.width, SampleHeight(uv), (uv.y - 0.5f) * Data.sizeKm.height);
            }
        }

        var triangles = new int[GridX * GridZ * 6];
        int t = 0;
        for (int z = 0; z < GridZ; z++)
        {
            for (int x = 0; x < GridX; x++)
            {
                int i = z * vx + x;
                triangles[t++] = i;
                triangles[t++] = i + vx;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + vx;
                triangles[t++] = i + vx + 1;
            }
        }

        var mesh = new Mesh { name = "Denmark1851_Terrain", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var terrain = new GameObject("Denmark1851_Terrain");
        terrain.transform.SetParent(transform, false);
        terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
        var material = new Material(Shader.Find("Standard")) { name = "Denmark1851_Map" };
        material.mainTexture = colour;
        material.SetFloat("_Glossiness", 0.08f);

        // Close-zoom field patchwork: tiled detail albedo (x2 multiply), masked to the monarchy's land.
        var detail = Resources.Load<Texture2D>(ResourceFolder + "Fields1851_Detail");
        var detailMask = Resources.Load<Texture2D>(ResourceFolder + "Denmark1851_DetailMask");
        if (detail != null && detailMask != null && Data.detailTileKm > 0f)
        {
            material.EnableKeyword("_DETAIL_MULX2");
            material.SetTexture("_DetailAlbedoMap", detail);
            material.SetTextureScale("_DetailAlbedoMap", new Vector2(Data.sizeKm.width / Data.detailTileKm, Data.sizeKm.height / Data.detailTileKm));
            material.SetTexture("_DetailMask", detailMask);
        }
        material.SetFloat("_Metallic", 0f);
        // A painted map should not pick up the grey default environment reflection or specular sheen.
        material.SetFloat("_GlossyReflections", 0f);
        material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        material.SetFloat("_SpecularHighlights", 0f);
        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        var renderer = terrain.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private void BuildLighting()
    {
        var sun = new GameObject("Sun1851").AddComponent<Light>();
        sun.transform.SetParent(transform, false);
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.7f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.46f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        RenderSettings.fog = false;
        RenderSettings.skybox = null;
    }

    private void BuildCamera()
    {
        MapCamera = Camera.main;
        if (MapCamera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            MapCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        MapCamera.clearFlags = CameraClearFlags.SolidColor;
        MapCamera.backgroundColor = new Color(18f / 255f, 33f / 255f, 51f / 255f);   // matches the map's faded edge
        MapCamera.fieldOfView = 30f;
        MapCamera.nearClipPlane = 0.3f;
        MapCamera.farClipPlane = 3000f;

        CameraRig = MapCamera.GetComponent<CampaignMap1851Camera>();
        if (CameraRig == null)
            CameraRig = MapCamera.gameObject.AddComponent<CampaignMap1851Camera>();
        CameraRig.HalfExtent = new Vector2(Data.sizeKm.width * 0.5f, Data.sizeKm.height * 0.5f);
        // Frame the monarchy: the centre of the map, pulled slightly north-west off Skåne.
        CameraRig.Init(Project(55.55f, 10.45f), 960f);
    }

    // ---------------------------------------------------------------- cities

    private void BuildCities()
    {
        var root = new GameObject("Cities1851").transform;
        root.SetParent(transform, false);

        var monarchy = new Material(Shader.Find("Standard")) { name = "City1851_Red" };
        monarchy.color = new Color(0.72f, 0.1f, 0.07f);
        monarchy.SetFloat("_Glossiness", 0.65f);
        var foreign = new Material(Shader.Find("Standard")) { name = "City1851_Foreign" };
        foreign.color = new Color(0.42f, 0.4f, 0.37f);
        foreign.SetFloat("_Glossiness", 0.4f);

        foreach (var city in Data.cities)
        {
            if (!city.bornholm)
                CreateCity(root, city, monarchy, false);
        }
        foreach (var city in Data.foreignCities)
            CreateCity(root, city, foreign, true);
    }

    private void CreateCity(Transform root, Map1851City data, Material material, bool isForeign)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "City_" + data.name;
        go.transform.SetParent(root, false);
        go.transform.position = Project(data.lat, data.lon);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        var marker = go.AddComponent<CampaignMap1851CityMarker>();
        marker.Data = data;
        marker.IsForeign = isForeign;
        markers.Add(marker);
    }

    // ---------------------------------------------------------------- interaction

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || MapCamera == null)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = MapCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 5000f) && hit.collider.TryGetComponent(out CampaignMap1851CityMarker marker))
            Ui.ShowCity(marker.Data, marker.IsForeign);
        else
            Ui.ShowCity(null, false);
    }

    private void LateUpdate()
    {
        RefreshView();
    }

    /// <summary>Updates zoom-dependent markers and screen-space labels. Also called by the editor capture tool.</summary>
    public void RefreshView()
    {
        if (MapCamera == null)
            return;
        float distance = CameraRig.Distance;
        foreach (var marker in markers)
            marker.UpdateScale(distance);
        if (Ui != null)
            Ui.Refresh(distance);
    }
}
