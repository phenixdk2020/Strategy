using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// campaign2 sole visible map: one land mesh + one water mesh.
/// No OSM tiles. Legacy v13/bootstrap visuals are hidden on this track only.
/// </summary>
[DefaultExecutionOrder(200)]
public sealed class Campaign2DioramaMap : MonoBehaviour
{
    public static bool OwnsPresentation { get; private set; }

    private Camera cam;
    private Transform root;
    private bool ready;
    private string status = "bygger 3D-Danmark";
    private GUIStyle statusStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private readonly List<Landmark> landmarks = new List<Landmark>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (FindAnyObjectByType<Campaign2DioramaMap>() != null)
            return;
        OwnsPresentation = true;
        new GameObject("Campaign2DioramaMap").AddComponent<Campaign2DioramaMap>();
    }

    private void Start()
    {
        OwnsPresentation = true;
        cam = Camera.main;
        HideLegacyVisuals();
        StartCoroutine(BuildDiorama());
    }

    private void Update()
    {
        HideLegacyVisuals();
        if (ready)
            KeepCameraOnDiorama();
    }

    private IEnumerator BuildDiorama()
    {
        Campaign2Geography.EnsureInitialized();
        CreateRoot();
        ConfigureAtmosphere();
        status = "bygger vand";
        BuildWater();
        yield return null;
        yield return BuildLand();
        BuildLandmarks();
        ConfigureCamera();
        ready = true;
        status = "3D-Danmark klar · ingen OSM";
        Debug.Log("CAMPAIGN2|Diorama=True|Land=C2_Land|Water=C2_Water|OSM=False|Projection=DenmarkFirst|MapOnly=True|SimulationChanged=False");
    }

    private void CreateRoot()
    {
        GameObject old = GameObject.Find(Campaign2Config.RootName);
        if (old != null)
            Destroy(old);
        root = new GameObject(Campaign2Config.RootName).transform;
    }

    private IEnumerator BuildLand()
    {
        int gx = Campaign2Config.GridX;
        int gz = Campaign2Config.GridZ;
        int width = gx + 1;
        int height = gz + 1;
        int count = width * height;

        bool[] land = new bool[count];
        float[] inland = new float[count];
        float[] metres = new float[count];
        Vector3[] vertices = new Vector3[count];
        Vector2[] uv = new Vector2[count];
        Color[] colors = new Color[count];

        for (int z = 0; z < height; z++)
        {
            double lat = Campaign2Config.MinLatitude +
                         (Campaign2Config.MaxLatitude - Campaign2Config.MinLatitude) * (z / (double)gz);
            for (int x = 0; x < width; x++)
            {
                double lon = Campaign2Config.MinLongitude +
                             (Campaign2Config.MaxLongitude - Campaign2Config.MinLongitude) * (x / (double)gx);
                int i = z * width + x;
                land[i] = Campaign2Geography.IsLand(lat, lon);
                uv[i] = new Vector2(x / (float)gx, z / (float)gz);
            }
            if ((z & 15) == 0)
            {
                status = "klassificerer kyst " + Mathf.RoundToInt((z / (float)gz) * 50f) + "%";
                yield return null;
            }
        }

        ComputeInland(land, inland, width, height);

        for (int z = 0; z < height; z++)
        {
            double lat = Campaign2Config.MinLatitude +
                         (Campaign2Config.MaxLatitude - Campaign2Config.MinLatitude) * (z / (double)gz);
            for (int x = 0; x < width; x++)
            {
                double lon = Campaign2Config.MinLongitude +
                             (Campaign2Config.MaxLongitude - Campaign2Config.MinLongitude) * (x / (double)gx);
                int i = z * width + x;
                if (land[i])
                    metres[i] = Campaign2Geography.ElevationMeters(lat, lon, inland[i]);
            }
        }

        SmoothLand(metres, land, width, height);

        Texture2D splat = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
        {
            name = "C2_LandCover",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[count];

        for (int z = 0; z < height; z++)
        {
            double lat = Campaign2Config.MinLatitude +
                         (Campaign2Config.MaxLatitude - Campaign2Config.MinLatitude) * (z / (double)gz);
            for (int x = 0; x < width; x++)
            {
                double lon = Campaign2Config.MinLongitude +
                             (Campaign2Config.MaxLongitude - Campaign2Config.MinLongitude) * (x / (double)gx);
                int i = z * width + x;
                float y = Campaign2Config.WaterY;
                if (land[i])
                    y = Campaign2Config.BeachLift + metres[i] * Campaign2Config.MetresToVisualY;
                vertices[i] = CampaignGeoProjection.Project((float)lon, (float)lat, y);
                Color cover = land[i]
                    ? Campaign2Geography.LandCoverColor(lat, lon, inland[i], metres[i])
                    : Campaign2Config.WaterColor;
                colors[i] = cover;
                pixels[i] = cover;
            }
            if ((z & 15) == 0)
            {
                status = "bygger relief " + Mathf.RoundToInt(50f + (z / (float)gz) * 50f) + "%";
                yield return null;
            }
        }

        splat.SetPixels32(pixels);
        splat.Apply(true, false);

        List<int> triangles = new List<int>(gx * gz * 6);
        List<int> skirts = new List<int>();
        for (int z = 0; z < gz; z++)
        {
            for (int x = 0; x < gx; x++)
            {
                int i0 = z * width + x;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;
                if (land[i0] && land[i1] && land[i2] && land[i3])
                {
                    triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                    triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
                }
            }
        }

        Mesh mesh = new Mesh
        {
            name = "C2_DenmarkLandMesh",
            indexFormat = IndexFormat.UInt32
        };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject landObject = new GameObject(Campaign2Config.LandObjectName);
        landObject.transform.SetParent(root, false);
        landObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = landObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateLandMaterial(splat);
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        landObject.AddComponent<MeshCollider>().sharedMesh = mesh;

        BuildCoastSkirts(land, vertices, width, height, gx, gz);
    }

    private void BuildCoastSkirts(bool[] land, Vector3[] vertices, int width, int height, int gx, int gz)
    {
        List<Vector3> skirtVerts = new List<Vector3>();
        List<int> skirtTris = new List<int>();
        Color cliff = new Color(0.42f, 0.38f, 0.28f);

        for (int z = 0; z < gz; z++)
        {
            for (int x = 0; x < gx; x++)
            {
                int i0 = z * width + x;
                TrySkirtEdge(land, vertices, i0, i0 + 1, skirtVerts, skirtTris);
                TrySkirtEdge(land, vertices, i0, i0 + width, skirtVerts, skirtTris);
            }
        }

        if (skirtTris.Count < 3)
            return;

        Mesh mesh = new Mesh { name = "C2_CoastSkirt", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(skirtVerts);
        mesh.triangles = skirtTris.ToArray();
        Color[] colors = new Color[skirtVerts.Count];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = cliff;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject("C2_CoastSkirt");
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateLitMaterial(cliff, "C2_Cliff");
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void TrySkirtEdge(
        bool[] land,
        Vector3[] vertices,
        int a,
        int b,
        List<Vector3> verts,
        List<int> tris)
    {
        if (land[a] == land[b])
            return;
        int landIndex = land[a] ? a : b;
        int waterIndex = land[a] ? b : a;
        Vector3 landTop = vertices[landIndex];
        Vector3 waterTop = vertices[waterIndex];
        Vector3 landBottom = new Vector3(landTop.x, Campaign2Config.WaterY - 0.04f, landTop.z);
        Vector3 waterBottom = new Vector3(waterTop.x, Campaign2Config.WaterY - 0.04f, waterTop.z);

        int start = verts.Count;
        verts.Add(landTop);
        verts.Add(waterTop);
        verts.Add(landBottom);
        verts.Add(waterBottom);
        if (land[a])
        {
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start + 1); tris.Add(start + 2); tris.Add(start + 3);
        }
        else
        {
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            tris.Add(start + 1); tris.Add(start + 3); tris.Add(start + 2);
        }
    }

    private void BuildWater()
    {
        const int wx = 70;
        const int wz = 50;
        int width = wx + 1;
        int height = wz + 1;
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[vertices.Length];
        List<int> triangles = new List<int>(wx * wz * 6);

        for (int z = 0; z < height; z++)
        {
            double lat = Campaign2Config.MinLatitude - 0.15 +
                         (Campaign2Config.MaxLatitude - Campaign2Config.MinLatitude + 0.30) * (z / (double)wz);
            for (int x = 0; x < width; x++)
            {
                double lon = Campaign2Config.MinLongitude - 0.20 +
                             (Campaign2Config.MaxLongitude - Campaign2Config.MinLongitude + 0.40) * (x / (double)wx);
                int i = z * width + x;
                float chop = (Mathf.PerlinNoise((float)lon * 2.4f, (float)lat * 2.4f) - 0.5f) * 0.05f;
                vertices[i] = CampaignGeoProjection.Project((float)lon, (float)lat, Campaign2Config.WaterY + chop);
                uv[i] = new Vector2(x / (float)wx, z / (float)wz);
            }
        }

        for (int z = 0; z < wz; z++)
        {
            for (int x = 0; x < wx; x++)
            {
                int i0 = z * width + x;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;
                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh mesh = new Mesh { name = "C2_TheatreWater", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject water = new GameObject(Campaign2Config.WaterObjectName);
        water.transform.SetParent(root, false);
        water.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = water.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateLitMaterial(Campaign2Config.WaterColor, "C2_Water");
        if (renderer.sharedMaterial.HasProperty("_Smoothness"))
            renderer.sharedMaterial.SetFloat("_Smoothness", 0.72f);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private void BuildLandmarks()
    {
        Transform holder = new GameObject("C2_Landmarks").transform;
        holder.SetParent(root, false);
        AddLandmark(holder, "København", 55.676f, 12.568f, 0.55f);
        AddLandmark(holder, "Dybbøl", 54.907f, 9.758f, 0.42f);
        AddLandmark(holder, "Fredericia", 55.566f, 9.757f, 0.40f);
        AddLandmark(holder, "Flensborg", 54.782f, 9.437f, 0.40f);
        AddLandmark(holder, "Kiel", 54.323f, 10.139f, 0.42f);
        AddLandmark(holder, "Dannevirke", 54.478f, 9.486f, 0.36f);
        AddLandmark(holder, "Als", 54.980f, 9.900f, 0.32f);
        AddLandmark(holder, "Aarhus", 56.157f, 10.210f, 0.42f);
        AddLandmark(holder, "Aalborg", 57.048f, 9.919f, 0.40f);
        AddLandmark(holder, "Hamborg", 53.551f, 9.993f, 0.48f);
    }

    private void AddLandmark(Transform parent, string label, float lat, float lon, float size)
    {
        Vector3 p = CampaignGeoProjection.Project(lon, lat, 0f);
        p.y = SampleLandY(p.x, p.z) + size * 0.55f;
        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mark.name = "C2_Landmark_" + label;
        mark.transform.SetParent(parent, false);
        mark.transform.position = p;
        mark.transform.localScale = new Vector3(size, size * 1.15f, size);
        Collider collider = mark.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        mark.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(
            new Color(0.42f, 0.22f, 0.16f), "C2_Landmark_" + label);
        landmarks.Add(new Landmark(label, p));
    }

    private static float SampleLandY(float x, float z)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 80f, z), Vector3.down, 160f);
        float best = Campaign2Config.BeachLift;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != null && hits[i].collider.gameObject.name == Campaign2Config.LandObjectName)
                best = Mathf.Max(best, hits[i].point.y);
        }
        return best;
    }

    private void ConfigureCamera()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 420f;
        cam.fieldOfView = 48f;
        cam.backgroundColor = Campaign2Config.SkyColor;

        Vector3 look = CampaignGeoProjection.Project(10.12f, 55.48f, 0f);
        Vector3 home = look + new Vector3(0f, Campaign2Config.CameraHomeHeight, -Campaign2Config.CameraHomeSouth);
        Quaternion rot = Quaternion.Euler(Campaign2Config.CameraHomePitch, 0f, 0f);

        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller != null)
        {
            controller.UseLegacyTerrainFloor = false;
            controller.MinHeight = Campaign2Config.CameraMinHeight;
            controller.MaxHeight = Campaign2Config.CameraMaxHeight;
            controller.TerrainClearance = Campaign2Config.CameraClearance;
            controller.MinPitch = Campaign2Config.CameraMinPitch;
            controller.MaxPitch = Campaign2Config.CameraMaxPitch;
            controller.PanSpeed = 38f;
            controller.ZoomSpeed = 70f;
            controller.ApplyPresentationHome(home, rot);
        }
        else
        {
            cam.transform.position = home;
            cam.transform.rotation = rot;
        }
    }

    private void KeepCameraOnDiorama()
    {
        if (cam == null)
            return;
        CampaignMapCameraController controller = cam.GetComponent<CampaignMapCameraController>();
        if (controller == null)
            return;
        controller.UseLegacyTerrainFloor = false;
        controller.MinPitch = Campaign2Config.CameraMinPitch;
        controller.MaxPitch = Campaign2Config.CameraMaxPitch;
    }

    private void ConfigureAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.48f, 0.50f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.20f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Campaign2Config.FogColor;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 165f;

        Light sun = null;
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }
        if (sun == null)
        {
            GameObject go = new GameObject("C2_Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = Campaign2Config.SunColor;
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;
        sun.transform.rotation = Quaternion.Euler(34f, -32f, 0f);
    }

    private void HideLegacyVisuals()
    {
        DisableBehaviours<CampaignUnifiedDenmark3DMapV013M>();
        DisableBehaviours<CampaignHistorical3DMapV013J>();
        DisableBehaviours<CampaignLiveCartographicDrapeV013K>();
        DisableBehaviours<CampaignPremiumCartographicVisualV013L>();
        DisableBehaviours<CampaignDenmarkGisFoundationV013I>();
        DisableBehaviours<CampaignHistoricalMapLabelsV013J>();
        DisableBehaviours<CampaignMapOnlyModeV013H1>();

        HideByPrefix("V013");
        HideByPrefix("GIS_");
        HideByPrefix("GEO_");
        HideByPrefix("Outline_");
        HideByPrefix("CampaignNode_");
        HideByPrefix("CampaignFormation_");
        HideByPrefix("StrategicLink_");
        HideByName("Campaign Sea Base");
        HideByName("V013M_UNIFIED_DENMARK_3D_MAP");
        HideByName("V013J_HISTORICAL_3D_MAP");
        HideByName("V013J_SmoothTerrain");
    }

    private static void DisableBehaviours<T>() where T : Behaviour
    {
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Include);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            found[i].enabled = false;
            found[i].gameObject.SetActive(false);
        }
    }

    private static void HideByPrefix(string prefix)
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || go.name == null)
                continue;
            if (go.name.StartsWith(prefix, StringComparison.Ordinal))
                go.SetActive(false);
        }
    }

    private static void HideByName(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            go.SetActive(false);
    }

    private static void ComputeInland(bool[] land, float[] inland, int width, int height)
    {
        int[] dist = new int[land.Length];
        for (int i = 0; i < land.Length; i++)
            dist[i] = land[i] ? 999 : 0;

        for (int pass = 0; pass < 10; pass++)
        {
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = z * width + x;
                    if (!land[i])
                        continue;
                    int best = dist[i];
                    if (x > 0) best = Mathf.Min(best, dist[i - 1] + 1);
                    if (x + 1 < width) best = Mathf.Min(best, dist[i + 1] + 1);
                    if (z > 0) best = Mathf.Min(best, dist[i - width] + 1);
                    if (z + 1 < height) best = Mathf.Min(best, dist[i + width] + 1);
                    dist[i] = best;
                }
            }
        }

        for (int i = 0; i < inland.Length; i++)
            inland[i] = land[i] ? Mathf.Clamp01(dist[i] / 8f) : 0f;
    }

    private static void SmoothLand(float[] metres, bool[] land, int width, int height)
    {
        float[] buffer = new float[metres.Length];
        for (int pass = 0; pass < 2; pass++)
        {
            Array.Copy(metres, buffer, metres.Length);
            for (int z = 1; z < height - 1; z++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int i = z * width + x;
                    if (!land[i])
                        continue;
                    float sum = metres[i];
                    int n = 1;
                    int a = i - 1, b = i + 1, c = i - width, d = i + width;
                    if (land[a]) { sum += metres[a]; n++; }
                    if (land[b]) { sum += metres[b]; n++; }
                    if (land[c]) { sum += metres[c]; n++; }
                    if (land[d]) { sum += metres[d]; n++; }
                    buffer[i] = Mathf.Lerp(metres[i], sum / n, 0.35f);
                }
            }
            Array.Copy(buffer, metres, metres.Length);
        }
    }

    private static Material CreateLandMaterial(Texture2D splat)
    {
        Material material = CreateLitMaterial(Color.white, "C2_LandCoverLit");
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", splat);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", splat);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static Material CreateLitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.93f, 0.84f) }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.86f, 0.88f, 0.78f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.16f, 0.12f, 0.08f) }
            };
        }

        GUI.Label(new Rect(16f, 12f, 720f, 24f), "PROJECT 1864  |  campaign2  |  " + Campaign2Config.BuildTag, titleStyle);
        GUI.Label(new Rect(16f, 34f, 880f, 22f), status + "   ·   WASD pan  Q/E drej  PgUp/Dn hæld  hjul zoom  Home reset", statusStyle);

        if (!ready || cam == null)
            return;

        for (int i = 0; i < landmarks.Count; i++)
        {
            Vector3 screen = cam.WorldToScreenPoint(landmarks[i].World + Vector3.up * 0.8f);
            if (screen.z < 0.4f)
                continue;
            Rect rect = new Rect(screen.x - 70f, Screen.height - screen.y - 18f, 140f, 20f);
            GUI.Label(rect, landmarks[i].Name, labelStyle);
        }
    }

    private readonly struct Landmark
    {
        public readonly string Name;
        public readonly Vector3 World;

        public Landmark(string name, Vector3 world)
        {
            Name = name;
            World = world;
        }
    }
}
