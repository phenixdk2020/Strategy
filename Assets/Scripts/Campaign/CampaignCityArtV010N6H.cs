using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6h city-art visibility hotfix.
/// Guarantees that authored Proposal-3 city art remains visible above terrain.
/// Uses a bottom-anchored billboard mesh, Cull Off, ZWrite Off and ZTest Always.
/// If Resources loading fails, a visible generated fallback town is used.
/// </summary>
[DefaultExecutionOrder(23950)]
public sealed class CampaignCityArtV010N6H : MonoBehaviour
{
    private const string RootName = "CITY_ART_PROPOSAL3_10N6H";
    private const string ResourceRoot = "Campaign/CityIcons/";
    private const float GroundLift = 0.28f;
    private const float RetrySeconds = 0.60f;

    private readonly List<Transform> billboards = new List<Transform>();

    private Texture2D textureA;
    private Texture2D textureB;
    private Texture2D textureC;
    private Material materialA;
    private Material materialB;
    private Material materialC;
    private Mesh billboardMesh;

    private bool installed;
    private float retryAt;
    private bool fallbackA;
    private bool fallbackB;
    private bool fallbackC;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignCityArtV010N6H>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_CITY_ART_10N6H");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignCityArtV010N6H>();
    }

    private void Awake()
    {
        CampaignCityArtV010N6G oldG = Object.FindAnyObjectByType<CampaignCityArtV010N6G>();
        if (oldG != null)
            oldG.enabled = false;

        CampaignCityIconV010N6 oldN6 = Object.FindAnyObjectByType<CampaignCityIconV010N6>();
        if (oldN6 != null)
            oldN6.enabled = false;

        billboardMesh = CreateBottomAnchoredQuad();
    }

    private void Update()
    {
        if (installed || GrandCampaignBootstrap.Instance == null)
            return;

        if (Time.unscaledTime < retryAt)
            return;

        retryAt = Time.unscaledTime + RetrySeconds;
        EnsureTexturesAndMaterials();

        GrandCampaignCityMarker[] markers =
            Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsInactive.Exclude);

        if (markers == null || markers.Length == 0)
            return;

        billboards.Clear();
        int count = 0;

        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignCityMarker marker = markers[i];
            if (marker == null || string.IsNullOrEmpty(marker.CityId))
                continue;

            CampaignDenmark1851Registry.CityDef city = FindCity(marker.CityId);
            if (city == null)
                continue;

            if (Install(marker.gameObject, city))
                count++;
        }

        if (count != CampaignDenmark1851Registry.Cities.Length)
        {
            Debug.LogWarning(
                CampaignBuildInfo.LogTag +
                "|CityArtN6H=Partial|Installed=" + count +
                "|Expected=" + CampaignDenmark1851Registry.Cities.Length);
            return;
        }

        installed = true;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|CityArtN6H=True|Cities=" + count +
            "|TextureA=" + Describe(textureA) +
            "|TextureB=" + Describe(textureB) +
            "|TextureC=" + Describe(textureC) +
            "|FallbackA=" + fallbackA +
            "|FallbackB=" + fallbackB +
            "|FallbackC=" + fallbackC +
            "|Renderer=BottomAnchoredBillboardMesh" +
            "|Shader=PROJECT1864/CampaignCityBillboard" +
            "|Cull=Off|ZWrite=Off|ZTest=Always" +
            "|ScaleA=4.80|ScaleB=3.65|ScaleC=2.65" +
            "|RoundCityRenderer=False|GreenTownGround=False");
    }

    private void LateUpdate()
    {
        if (!installed)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        for (int i = billboards.Count - 1; i >= 0; i--)
        {
            Transform t = billboards[i];
            if (t == null)
            {
                billboards.RemoveAt(i);
                continue;
            }

            Vector3 toCamera = cam.transform.position - t.position;
            if (toCamera.sqrMagnitude < 0.0001f)
                continue;

            // Local +Z faces the camera. The custom shader is Cull Off as an
            // additional guarantee, so a winding/camera-angle mismatch cannot
            // hide the city art.
            t.rotation = Quaternion.LookRotation(toCamera.normalized, cam.transform.up);
        }
    }

    private void EnsureTexturesAndMaterials()
    {
        if (textureA == null)
        {
            textureA = LoadTexture("City_A_Proposal3");
            if (textureA == null)
            {
                textureA = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.A);
                fallbackA = true;
            }
        }

        if (textureB == null)
        {
            textureB = LoadTexture("City_B_Proposal3");
            if (textureB == null)
            {
                textureB = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.B);
                fallbackB = true;
            }
        }

        if (textureC == null)
        {
            textureC = LoadTexture("City_C_Proposal3");
            if (textureC == null)
            {
                textureC = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.C);
                fallbackC = true;
            }
        }

        if (materialA == null) materialA = CreateMaterial(textureA, "CITY_ART_A_10N6H");
        if (materialB == null) materialB = CreateMaterial(textureB, "CITY_ART_B_10N6H");
        if (materialC == null) materialC = CreateMaterial(textureC, "CITY_ART_C_10N6H");
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + name);
        if (texture != null)
            return texture;

        Texture2D[] all = Resources.LoadAll<Texture2D>("Campaign/CityIcons");
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name)
                return all[i];

        return null;
    }

    private bool Install(GameObject marker, CampaignDenmark1851Registry.CityDef city)
    {
        if (marker == null || city == null)
            return false;

        Transform existing = marker.transform.Find(RootName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            if (!billboards.Contains(existing))
                billboards.Add(existing);
            return true;
        }

        List<GameObject> remove = new List<GameObject>();
        for (int i = 0; i < marker.transform.childCount; i++)
        {
            Transform child = marker.transform.GetChild(i);
            if (child == null)
                continue;

            if (child.name.StartsWith("CITY_ICON_") || child.name.StartsWith("CITY_ART_"))
                remove.Add(child.gameObject);
        }

        for (int i = 0; i < remove.Count; i++)
            Destroy(remove[i]);

        Renderer oldRenderer = marker.GetComponent<Renderer>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        Collider[] oldColliders = marker.GetComponents<Collider>();
        for (int i = 0; i < oldColliders.Length; i++)
            Destroy(oldColliders[i]);

        marker.transform.localScale = Vector3.one;

        float scale = Scale(city);

        BoxCollider click = marker.AddComponent<BoxCollider>();
        click.center = new Vector3(0f, 0.75f, 0f);
        click.size = new Vector3(scale * 0.92f, 2.2f, scale * 0.78f);

        GameObject go = new GameObject(RootName);
        go.transform.SetParent(marker.transform, false);
        go.transform.localPosition = new Vector3(0f, GroundLift, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.layer = marker.layer;

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = billboardMesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialForTier(city.Tier);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = 500;
        renderer.forceRenderingOff = false;
        renderer.enabled = true;

        billboards.Add(go.transform);
        return true;
    }

    private Material MaterialForTier(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A) return materialA;
        if (tier == CampaignDenmark1851Registry.CityTier.B) return materialB;
        return materialC;
    }

    private static Material CreateMaterial(Texture2D texture, string materialName)
    {
        Shader shader = Shader.Find("PROJECT1864/CampaignCityBillboard");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = materialName };
        material.mainTexture = texture;
        material.color = Color.white;
        material.renderQueue = 4250;

        if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_ZTest")) material.SetInt("_ZTest", (int)CompareFunction.Always);

        return material;
    }

    private static Mesh CreateBottomAnchoredQuad()
    {
        Mesh mesh = new Mesh { name = "CITY_BILLBOARD_BOTTOM_ANCHORED_10N6H" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3( 0.5f, 0f, 0f),
            new Vector3( 0.5f, 1f, 0f),
            new Vector3(-0.5f, 1f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Scale(CampaignDenmark1851Registry.CityDef city)
    {
        float baseScale;
        float maxBonus;
        float populationReference;

        if (city.Tier == CampaignDenmark1851Registry.CityTier.A)
        {
            baseScale = 4.80f;
            maxBonus = 0.15f;
            populationReference = 130000f;
        }
        else if (city.Tier == CampaignDenmark1851Registry.CityTier.B)
        {
            baseScale = 3.65f;
            maxBonus = 0.12f;
            populationReference = 4000f;
        }
        else
        {
            baseScale = 2.65f;
            maxBonus = 0.10f;
            populationReference = 2000f;
        }

        return baseScale * (1f + Mathf.Clamp01(city.Population1850 / populationReference) * maxBonus);
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i] != null && cities[i].Id == cityId)
                return cities[i];

        return null;
    }

    private static string Describe(Texture2D texture)
    {
        return texture == null ? "missing" : texture.width + "x" + texture.height;
    }

    private static Texture2D CreateFallbackTexture(CampaignDenmark1851Registry.CityTier tier)
    {
        const int size = 96;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Light neutral footprint so the fallback is impossible to miss.
        for (int y = 12; y < 60; y++)
        {
            for (int x = 9; x < 87; x++)
            {
                float dx = (x - 48f) / 39f;
                float dy = (y - 36f) / 25f;
                if (dx * dx + dy * dy <= 1f)
                    pixels[y * size + x] = new Color32(221, 207, 170, 235);
            }
        }

        int houses = tier == CampaignDenmark1851Registry.CityTier.A ? 11 :
                     tier == CampaignDenmark1851Registry.CityTier.B ? 6 : 3;

        int[,] pos =
        {
            {20,25},{39,22},{59,26},{28,39},{51,39},{70,38},
            {17,43},{77,46},{39,49},{58,52},{48,31}
        };

        for (int h = 0; h < houses; h++)
            DrawHouse(pixels, size, pos[h, 0], pos[h, 1],
                h == houses - 1 && tier != CampaignDenmark1851Registry.CityTier.C);

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        tex.name = "CITY_FALLBACK_N6H_" + tier;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static void DrawHouse(Color32[] pixels, int size, int cx, int cy, bool church)
    {
        Color32 wall = new Color32(230, 218, 191, 255);
        Color32 roof = new Color32(151, 69, 47, 255);
        Color32 dark = new Color32(74, 65, 56, 255);

        int half = church ? 5 : 4;
        int height = church ? 18 : 10;

        for (int y = cy; y < cy + height && y < size; y++)
        {
            for (int x = cx - half; x <= cx + half; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = wall;
            }
        }

        int roofY = cy + height;
        for (int r = 0; r <= half + 2; r++)
        {
            int span = half + 2 - r;
            for (int x = cx - span; x <= cx + span; x++)
            {
                int y = roofY + r;
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = roof;
            }
        }

        if (church)
        {
            for (int y = roofY + half + 2; y < roofY + half + 9 && y < size; y++)
            {
                if (cx >= 0 && cx < size)
                    pixels[y * size + cx] = dark;
            }
        }
    }
}
