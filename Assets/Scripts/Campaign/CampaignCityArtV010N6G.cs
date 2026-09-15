using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6g city-art renderer.
/// Replaces the invisible/fragile SpriteRenderer path with explicit camera-facing
/// unlit quads. Proposal-3 textures remain transparent RGBA assets in Resources.
/// A generated fallback texture is used only when a resource cannot be imported,
/// so a city marker can never silently disappear again.
/// </summary>
[DefaultExecutionOrder(23800)]
public sealed class CampaignCityArtV010N6G : MonoBehaviour
{
    private const string RootName = "CITY_ART_PROPOSAL3_10N6G";
    private const string ResourceRoot = "Campaign/CityIcons/";
    private const float GroundLift = 0.70f;
    private const float RetrySeconds = 0.75f;

    private readonly List<Transform> billboards = new List<Transform>();
    private Texture2D textureA;
    private Texture2D textureB;
    private Texture2D textureC;
    private Material materialA;
    private Material materialB;
    private Material materialC;
    private bool installed;
    private float retryAt;
    private bool fallbackA;
    private bool fallbackB;
    private bool fallbackC;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignCityArtV010N6G>() != null) return;
        GameObject go = new GameObject("PROJECT1864_CITY_ART_10N6G");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignCityArtV010N6G>();
    }

    private void Awake()
    {
        CampaignCityIconV010N6 old = Object.FindAnyObjectByType<CampaignCityIconV010N6>();
        if (old != null) old.enabled = false;
    }

    private void Update()
    {
        if (installed || GrandCampaignBootstrap.Instance == null) return;
        if (Time.unscaledTime < retryAt) return;
        retryAt = Time.unscaledTime + RetrySeconds;

        EnsureTexturesAndMaterials();

        GrandCampaignCityMarker[] markers =
            Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsInactive.Exclude);
        if (markers == null || markers.Length == 0) return;

        billboards.Clear();
        int count = 0;

        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignCityMarker marker = markers[i];
            if (marker == null || string.IsNullOrEmpty(marker.CityId)) continue;
            CampaignDenmark1851Registry.CityDef city = FindCity(marker.CityId);
            if (city == null) continue;

            if (Install(marker.gameObject, city)) count++;
        }

        if (count != CampaignDenmark1851Registry.Cities.Length)
        {
            Debug.LogWarning(CampaignBuildInfo.LogTag + "|CityArtN6G=Partial|Installed=" + count + "|Expected=" + CampaignDenmark1851Registry.Cities.Length);
            return;
        }

        installed = true;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|CityArtN6G=True|Cities=" + count +
            "|TextureA=" + Describe(textureA) +
            "|TextureB=" + Describe(textureB) +
            "|TextureC=" + Describe(textureC) +
            "|FallbackA=" + fallbackA + "|FallbackB=" + fallbackB + "|FallbackC=" + fallbackC +
            "|Renderer=UnlitBillboardQuad" +
            "|ScaleA=5.00|ScaleB=3.80|ScaleC=2.80" +
            "|GroundLift=" + GroundLift.ToString("0.00") +
            "|RoundCityRenderer=False|GreenTownGround=False");
    }

    private void LateUpdate()
    {
        if (!installed) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = billboards.Count - 1; i >= 0; i--)
        {
            Transform t = billboards[i];
            if (t == null)
            {
                billboards.RemoveAt(i);
                continue;
            }

            Vector3 toCamera = cam.transform.position - t.position;
            if (toCamera.sqrMagnitude < 0.0001f) continue;
            t.rotation = Quaternion.LookRotation(toCamera.normalized, cam.transform.up);
        }
    }

    private void EnsureTexturesAndMaterials()
    {
        if (textureA == null)
        {
            textureA = LoadTexture("City_A_Proposal3");
            if (textureA == null) { textureA = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.A); fallbackA = true; }
        }
        if (textureB == null)
        {
            textureB = LoadTexture("City_B_Proposal3");
            if (textureB == null) { textureB = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.B); fallbackB = true; }
        }
        if (textureC == null)
        {
            textureC = LoadTexture("City_C_Proposal3");
            if (textureC == null) { textureC = CreateFallbackTexture(CampaignDenmark1851Registry.CityTier.C); fallbackC = true; }
        }

        if (materialA == null) materialA = CreateMaterial(textureA, "CITY_ART_A_10N6G");
        if (materialB == null) materialB = CreateMaterial(textureB, "CITY_ART_B_10N6G");
        if (materialC == null) materialC = CreateMaterial(textureC, "CITY_ART_C_10N6G");
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + name);
        if (texture != null) return texture;

        Texture2D[] all = Resources.LoadAll<Texture2D>("Campaign/CityIcons");
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name) return all[i];
        return null;
    }

    private bool Install(GameObject marker, CampaignDenmark1851Registry.CityDef city)
    {
        Transform existing = marker.transform.Find(RootName);
        if (existing != null)
        {
            if (!billboards.Contains(existing)) billboards.Add(existing);
            return true;
        }

        List<GameObject> remove = new List<GameObject>();
        for (int i = 0; i < marker.transform.childCount; i++)
        {
            Transform child = marker.transform.GetChild(i);
            if (child != null && child.name.StartsWith("CITY_ICON_")) remove.Add(child.gameObject);
            if (child != null && child.name.StartsWith("CITY_ART_")) remove.Add(child.gameObject);
        }
        for (int i = 0; i < remove.Count; i++) Destroy(remove[i]);

        Renderer oldRenderer = marker.GetComponent<Renderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;

        Collider[] colliders = marker.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++) Destroy(colliders[i]);
        marker.transform.localScale = Vector3.one;

        float scale = Scale(city);
        BoxCollider click = marker.AddComponent<BoxCollider>();
        click.center = new Vector3(0f, 0.80f, 0f);
        click.size = new Vector3(scale * 0.88f, 2.10f, scale * 0.72f);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = RootName;
        quad.transform.SetParent(marker.transform, false);
        quad.transform.localPosition = new Vector3(0f, GroundLift, 0f);
        quad.transform.localScale = Vector3.one * scale;

        Collider qCollider = quad.GetComponent<Collider>();
        if (qCollider != null) Destroy(qCollider);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialForTier(city.Tier);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 300;

        billboards.Add(quad.transform);
        return true;
    }

    private Material MaterialForTier(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A) return materialA;
        if (tier == CampaignDenmark1851Registry.CityTier.B) return materialB;
        return materialC;
    }

    private static Material CreateMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        material.mainTexture = texture;
        material.color = Color.white;
        material.renderQueue = 4000;
        return material;
    }

    private static float Scale(CampaignDenmark1851Registry.CityDef city)
    {
        float baseScale;
        float maxBonus;
        float populationReference;

        if (city.Tier == CampaignDenmark1851Registry.CityTier.A)
        {
            baseScale = 5.00f; maxBonus = 0.15f; populationReference = 130000f;
        }
        else if (city.Tier == CampaignDenmark1851Registry.CityTier.B)
        {
            baseScale = 3.80f; maxBonus = 0.12f; populationReference = 4000f;
        }
        else
        {
            baseScale = 2.80f; maxBonus = 0.10f; populationReference = 2000f;
        }

        return baseScale * (1f + Mathf.Clamp01(city.Population1850 / populationReference) * maxBonus);
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i] != null && cities[i].Id == cityId) return cities[i];
        return null;
    }

    private static string Describe(Texture2D texture)
    {
        return texture == null ? "missing" : texture.width + "x" + texture.height;
    }

    private static Texture2D CreateFallbackTexture(CampaignDenmark1851Registry.CityTier tier)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int y = 10; y < 43; y++)
        {
            for (int x = 8; x < 56; x++)
            {
                float dx = (x - 32f) / 25f;
                float dy = (y - 27f) / 18f;
                if (dx * dx + dy * dy <= 1f)
                    pixels[y * size + x] = new Color32(214, 199, 161, 220);
            }
        }

        int houses = tier == CampaignDenmark1851Registry.CityTier.A ? 9 :
                     tier == CampaignDenmark1851Registry.CityTier.B ? 5 : 3;
        int[,] pos = { {15,22},{28,18},{41,23},{22,31},{37,32},{12,32},{47,33},{30,38},{31,26} };
        for (int h = 0; h < houses; h++)
            DrawHouse(pixels, size, pos[h,0], pos[h,1], h == 8 && tier == CampaignDenmark1851Registry.CityTier.A);

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        tex.name = "CITY_FALLBACK_" + tier;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static void DrawHouse(Color32[] pixels, int size, int cx, int cy, bool church)
    {
        Color32 wall = new Color32(225, 216, 193, 255);
        Color32 roof = new Color32(154, 72, 49, 255);
        int half = church ? 4 : 3;
        int height = church ? 12 : 7;

        for (int y = cy; y < cy + height && y < size; y++)
            for (int x = cx - half; x <= cx + half && x < size; x++)
                if (x >= 0) pixels[y * size + x] = wall;

        int roofY = cy + height;
        for (int r = 0; r <= half + 1; r++)
            for (int x = cx - (half + 1 - r); x <= cx + (half + 1 - r); x++)
                if (x >= 0 && x < size && roofY + r < size) pixels[(roofY + r) * size + x] = roof;
    }
}
