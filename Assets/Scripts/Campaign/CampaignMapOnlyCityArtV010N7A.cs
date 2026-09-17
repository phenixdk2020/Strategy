using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n7a - MAP ONLY + CITY ART CLEANUP.
///
/// Runtime ownership is intentionally simplified:
/// - no Amt/county boundaries,
/// - no Amt/county selection panel,
/// - no historical polygon overlays,
/// - one and only one city-art renderer,
/// - Proposal-3 textures only; no alternative city-art fallback style.
///
/// This build exists to stabilise the visual city layer before county borders are
/// reintroduced in a later version.
/// </summary>
[DefaultExecutionOrder(32000)]
public sealed class CampaignMapOnlyCityArtV010N7A : MonoBehaviour
{
    private const string RootName = "CITY_ART_PROPOSAL3_10N7A";
    private const string ResourceRoot = "Campaign/CityIcons/";
    private const float GroundLift = 0.24f;
    private const float RetrySeconds = 0.50f;

    private readonly List<Transform> billboards = new List<Transform>();
    private Texture2D textureA;
    private Texture2D textureB;
    private Texture2D textureC;
    private Material materialA;
    private Material materialB;
    private Material materialC;
    private bool installed;
    private bool legacySuppressed;
    private float retryAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignMapOnlyCityArtV010N7A>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_MAP_ONLY_CITY_ART_10N7A");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignMapOnlyCityArtV010N7A>();
    }

    private void Update()
    {
        SuppressLegacyRuntime();

        if (installed || GrandCampaignBootstrap.Instance == null)
            return;
        if (Time.unscaledTime < retryAt)
            return;
        retryAt = Time.unscaledTime + RetrySeconds;

        if (!EnsureTexturesAndMaterials())
            return;

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
                "|CityArtN7A=Partial|Installed=" + count +
                "|Expected=" + CampaignDenmark1851Registry.Cities.Length);
            return;
        }

        installed = true;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|MapOnly=True|CountyBorders=False|CountySelection=False" +
            "|CityArtN7A=True|Cities=" + count +
            "|TextureA=" + Describe(textureA) +
            "|TextureB=" + Describe(textureB) +
            "|TextureC=" + Describe(textureC) +
            "|ScaleA=2.20|ScaleB=1.60|ScaleC=1.15" +
            "|FallbackStyle=False|RoundCityRenderer=False|GreenTownGround=False");
    }

    private void LateUpdate()
    {
        SuppressLegacyRuntime();

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

            // Bottom-anchored billboard. Forward points away from camera because
            // Unity's billboard mesh front face is authored toward -Z.
            t.rotation = Quaternion.LookRotation(-toCamera.normalized, cam.transform.up);
        }
    }

    private void SuppressLegacyRuntime()
    {
        // Keep this continuous, because several older systems self-create through
        // RuntimeInitializeOnLoadMethod and can otherwise reappear after n7a.
        DisableComponent(Object.FindAnyObjectByType<CampaignHistoricalAmtPolygonsV010N6G>());
        DisableComponent(Object.FindAnyObjectByType<CampaignHistoricalAmtOverlayV010N6F>());
        DisableComponent(Object.FindAnyObjectByType<CampaignAmtBoundaryContinuityV010N6H>());
        DisableComponent(Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6G>());
        DisableComponent(Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N6F>());
        DisableComponent(Object.FindAnyObjectByType<CampaignZoneInfoPanelV010N3>());
        DisableComponent(Object.FindAnyObjectByType<CampaignCityArtV010N6H>());
        DisableComponent(Object.FindAnyObjectByType<CampaignCityArtV010N6G>());
        DisableComponent(Object.FindAnyObjectByType<CampaignCityIconV010N6>());

        DestroyNamed("ZONE_LINES_10N6G_EXACT_PARISH_POLYGONS");
        DestroyNamed("ZONE_LINES_10N6H_CONTINUITY_BRIDGES");
        DestroyNamed("ZONE_LINES_10N6F_PARISH_GRID");
        DestroyNamed("ZONE_OVERLAY_1851_LAND_CLIPPED");
        DestroyNamed("ZONE_LINES_10N5_SHARED_COLLINEAR");

        if (!legacySuppressed)
        {
            legacySuppressed = true;
            Debug.Log(
                CampaignBuildInfo.LogTag +
                "|MapOnlyRuntime=True|AmtOverlay=False|AmtInfo=False|LegacyCityRenderers=False");
        }
    }

    private static void DisableComponent(Behaviour behaviour)
    {
        if (behaviour == null)
            return;
        if (behaviour.enabled)
        {
            behaviour.StopAllCoroutines();
            behaviour.enabled = false;
        }
    }

    private static void DestroyNamed(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            Destroy(go);
    }

    private bool EnsureTexturesAndMaterials()
    {
        if (textureA == null) textureA = LoadTexture("City_A_Proposal3");
        if (textureB == null) textureB = LoadTexture("City_B_Proposal3");
        if (textureC == null) textureC = LoadTexture("City_C_Proposal3");

        if (textureA == null || textureB == null || textureC == null)
        {
            Debug.LogError(
                CampaignBuildInfo.LogTag +
                "|CityArtN7A=False|Reason=Proposal3TextureMissing" +
                "|A=" + (textureA != null) +
                "|B=" + (textureB != null) +
                "|C=" + (textureC != null) +
                "|FallbackStyle=False");
            return false;
        }

        ConfigureTexture(textureA);
        ConfigureTexture(textureB);
        ConfigureTexture(textureC);

        if (materialA == null) materialA = CreateMaterial(textureA, "CITY_ART_A_10N7A");
        if (materialB == null) materialB = CreateMaterial(textureB, "CITY_ART_B_10N7A");
        if (materialC == null) materialC = CreateMaterial(textureC, "CITY_ART_C_10N7A");
        return materialA != null && materialB != null && materialC != null;
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

    private static void ConfigureTexture(Texture2D texture)
    {
        if (texture == null)
            return;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 2;
    }

    private bool Install(GameObject marker, CampaignDenmark1851Registry.CityDef city)
    {
        Transform existing = marker.transform.Find(RootName);
        if (existing != null)
        {
            if (!billboards.Contains(existing))
                billboards.Add(existing);
            return true;
        }

        RemoveLegacyChildren(marker.transform);

        Renderer oldRenderer = marker.GetComponent<Renderer>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        // Preserve the marker's canonical world position. No x/z art offset is used.
        marker.transform.localScale = Vector3.one;

        Collider[] colliders = marker.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);

        float scale = Scale(city);
        BoxCollider click = marker.AddComponent<BoxCollider>();
        click.center = new Vector3(0f, 0.45f, 0f);
        click.size = new Vector3(Mathf.Max(0.70f, scale * 0.72f), 1.20f, Mathf.Max(0.60f, scale * 0.55f));

        GameObject art = new GameObject(RootName);
        art.transform.SetParent(marker.transform, false);
        art.transform.localPosition = new Vector3(0f, GroundLift, 0f);
        art.transform.localScale = Vector3.one * scale;

        MeshFilter filter = art.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildBottomAnchoredQuad();

        MeshRenderer renderer = art.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialForTier(city.Tier);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 350;

        billboards.Add(art.transform);
        return true;
    }

    private static void RemoveLegacyChildren(Transform marker)
    {
        List<GameObject> remove = new List<GameObject>();
        for (int i = 0; i < marker.childCount; i++)
        {
            Transform child = marker.GetChild(i);
            if (child == null)
                continue;

            if (child.name.StartsWith("CITY_ICON_") ||
                child.name.StartsWith("CITY_ART_") ||
                child.name == "CITY_ART_PROPOSAL3_10N6G" ||
                child.name == "CITY_ART_PROPOSAL3_10N6H")
                remove.Add(child.gameObject);
        }

        for (int i = 0; i < remove.Count; i++)
            Destroy(remove[i]);
    }

    private Material MaterialForTier(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A) return materialA;
        if (tier == CampaignDenmark1851Registry.CityTier.B) return materialB;
        return materialC;
    }

    private static Material CreateMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("PROJECT1864/CampaignCityBillboard");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError(CampaignBuildInfo.LogTag + "|CityArtN7A=False|Reason=NoTransparentShader");
            return null;
        }

        Material material = new Material(shader) { name = name };
        material.mainTexture = texture;
        material.color = Color.white;
        material.renderQueue = 4000;
        return material;
    }

    private static Mesh BuildBottomAnchoredQuad()
    {
        Mesh mesh = new Mesh { name = "CITY_ART_BOTTOM_ANCHORED_QUAD_10N7A" };
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
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Scale(CampaignDenmark1851Registry.CityDef city)
    {
        if (city.Tier == CampaignDenmark1851Registry.CityTier.A) return 2.20f;
        if (city.Tier == CampaignDenmark1851Registry.CityTier.B) return 1.60f;
        return 1.15f;
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
}
