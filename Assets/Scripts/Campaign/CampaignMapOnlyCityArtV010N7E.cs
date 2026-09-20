using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n7e - COASTAL CITY VISUAL OFFSETS.
///
/// n7e keeps the accepted n7c embedded Proposal-3 renderer and adds only
/// visual-only coastal offsets for Sæby, Helsingør and Nykøbing Sjælland.
/// Canonical WGS84 coordinates, click anchors and ZoneId are unchanged.
/// County/Amt systems remain deliberately disabled (Way 1).
/// </summary>
[DefaultExecutionOrder(32200)]
public sealed class CampaignMapOnlyCityArtV010N7E : MonoBehaviour
{
    private const string RootName = "CITY_ART_PROPOSAL3_10N7E";
    private const float GroundLift = 0.12f;
    private const float RetrySeconds = 0.50f;

    // Intentionally smaller than n7b. 128px embedded art is sufficient at this footprint.
    private const float ScaleA = 0.95f;
    private const float ScaleB = 0.72f;
    private const float ScaleC = 0.52f;

    private readonly List<Transform> billboards = new List<Transform>();

    private Texture2D textureA;
    private Texture2D textureB;
    private Texture2D textureC;
    private Material materialA;
    private Material materialB;
    private Material materialC;

    private bool installed;
    private bool legacySuppressed;
    private bool legacyCityPurged;
    private bool assetErrorLogged;
    private float retryAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!CampaignBuildInfo.IsCurrentVersion("v00.00.10n7e") &&
            !CampaignBuildInfo.IsCurrentVersion("v00.00.10n7f"))
            return;

        if (Object.FindAnyObjectByType<CampaignMapOnlyCityArtV010N7E>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_MAP_ONLY_CITY_ART_10N7E");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignMapOnlyCityArtV010N7E>();
    }

    private void Update()
    {
        SuppressLegacyRuntime();
        PurgeLegacyCityVisuals();

        if (installed || GrandCampaignBootstrap.Instance == null)
            return;

        if (Time.unscaledTime < retryAt)
            return;

        retryAt = Time.unscaledTime + RetrySeconds;

        if (!EnsureEmbeddedTexturesAndMaterials())
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
                "|CityArtN7E=Partial|Installed=" + count +
                "|Expected=" + CampaignDenmark1851Registry.Cities.Length);
            return;
        }

        installed = true;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Way1=True|MapOnly=True|CountyBorders=False|CountySelection=False" +
            "|CityArtN7E=True|Cities=" + count +
            "|AssetSource=EMBEDDED_VALIDATED_PROPOSAL3" +
            "|TextureA=" + Describe(textureA) +
            "|TextureB=" + Describe(textureB) +
            "|TextureC=" + Describe(textureC) +
            "|ScaleA=" + ScaleA.ToString("0.00") +
            "|ScaleB=" + ScaleB.ToString("0.00") +
            "|ScaleC=" + ScaleC.ToString("0.00") +
            "|FallbackStyle=False|ResourcesDependency=False" +
            "|LegacyCityPurged=True|RoundCityRenderer=False|GreenTownGround=False" +
            "|VisualOffsetCities=3|OffsetIds=SAEBY,HELSINGOR,NYKOBING_SJ");
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

            // Front face of the authored quad is toward -Z.
            t.rotation = Quaternion.LookRotation(-toCamera.normalized, cam.transform.up);
        }
    }

    private void SuppressLegacyRuntime()
    {
        DisableComponent(Object.FindAnyObjectByType<CampaignMapOnlyCityArtV010N7C>());
        DisableComponent(Object.FindAnyObjectByType<CampaignMapOnlyCityArtV010N7B>());
        DisableComponent(Object.FindAnyObjectByType<CampaignMapOnlyCityArtV010N7A>());
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
                "|Way1Runtime=True|AmtOverlay=False|AmtInfo=False" +
                "|N7B=False|N7A=False|LegacyCityRenderers=False");
        }
    }

    private void PurgeLegacyCityVisuals()
    {
        if (legacyCityPurged || GrandCampaignBootstrap.Instance == null)
            return;

        GrandCampaignCityMarker[] markers =
            Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsInactive.Include);

        if (markers == null || markers.Length == 0)
            return;

        int removed = 0;

        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignCityMarker marker = markers[i];
            if (marker == null)
                continue;

            Renderer oldRenderer = marker.GetComponent<Renderer>();
            if (oldRenderer != null)
                oldRenderer.enabled = false;

            List<GameObject> remove = new List<GameObject>();

            for (int c = 0; c < marker.transform.childCount; c++)
            {
                Transform child = marker.transform.GetChild(c);
                if (child == null)
                    continue;

                if (child.name.StartsWith("CITY_ART_") ||
                    child.name.StartsWith("CITY_ICON_") ||
                    child.name == "TownGround")
                {
                    remove.Add(child.gameObject);
                }
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Destroy(remove[r]);
                removed++;
            }
        }

        legacyCityPurged = true;

        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|LegacyCityVisualPurge=True|RemovedChildren=" + removed +
            "|MarkerRenderersHidden=True");
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

    private bool EnsureEmbeddedTexturesAndMaterials()
    {
        if (textureA == null)
            textureA = DecodeTexture(
                CampaignCityArtEmbeddedDataA_V010N7C.Base64,
                "CITY_A_PROPOSAL3_EMBEDDED_10N7E");

        if (textureB == null)
            textureB = DecodeTexture(
                CampaignCityArtEmbeddedDataB_V010N7C.Base64,
                "CITY_B_PROPOSAL3_EMBEDDED_10N7E");

        if (textureC == null)
            textureC = DecodeTexture(
                CampaignCityArtEmbeddedDataC_V010N7C.Base64,
                "CITY_C_PROPOSAL3_EMBEDDED_10N7E");

        if (textureA == null || textureB == null || textureC == null)
        {
            if (!assetErrorLogged)
            {
                assetErrorLogged = true;
                Debug.LogError(
                    CampaignBuildInfo.LogTag +
                    "|CityArtN7E=False|Reason=EmbeddedProposal3DecodeFailed" +
                    "|A=" + (textureA != null) +
                    "|B=" + (textureB != null) +
                    "|C=" + (textureC != null) +
                    "|ResourcesDependency=False|FallbackStyle=False");
            }

            return false;
        }

        assetErrorLogged = false;

        if (materialA == null)
            materialA = CreateMaterial(textureA, "CITY_ART_A_10N7E");
        if (materialB == null)
            materialB = CreateMaterial(textureB, "CITY_ART_B_10N7E");
        if (materialC == null)
            materialC = CreateMaterial(textureC, "CITY_ART_C_10N7E");

        return materialA != null && materialB != null && materialC != null;
    }

    private static Texture2D DecodeTexture(string base64, string name)
    {
        try
        {
            byte[] png = Convert.FromBase64String(base64);

            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            if (!ImageConversion.LoadImage(texture, png, true))
            {
                Object.Destroy(texture);
                return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                CampaignBuildInfo.LogTag +
                "|EmbeddedTextureException=" + ex.GetType().Name +
                "|Texture=" + name +
                "|Message=" + ex.Message);

            return null;
        }
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

        // Clear any older art again at marker level before n7c install.
        RemoveLegacyChildren(marker.transform);

        Renderer oldRenderer = marker.GetComponent<Renderer>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        marker.transform.localScale = Vector3.one;

        Collider[] colliders = marker.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);

        float scale = Scale(city);

        BoxCollider click = marker.AddComponent<BoxCollider>();
        click.center = new Vector3(0f, 0.30f, 0f);
        click.size = new Vector3(
            Mathf.Max(0.65f, scale * 1.10f),
            0.90f,
            Mathf.Max(0.55f, scale * 0.85f));

        GameObject art = new GameObject(RootName);
        art.transform.SetParent(marker.transform, false);

        Vector3 visualOffset = CampaignCityVisualOffsetsV010N7E.Get(city.Id);
        art.transform.localPosition = new Vector3(
            visualOffset.x,
            GroundLift,
            visualOffset.z);
        art.transform.localScale = Vector3.one * scale;

        MeshFilter filter = art.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildBottomAnchoredQuad();

        MeshRenderer renderer = art.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialForTier(city.Tier);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 365;

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
                child.name == "TownGround")
            {
                remove.Add(child.gameObject);
            }
        }

        for (int i = 0; i < remove.Count; i++)
            Destroy(remove[i]);
    }

    private Material MaterialForTier(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A)
            return materialA;
        if (tier == CampaignDenmark1851Registry.CityTier.B)
            return materialB;
        return materialC;
    }

    private static Material CreateMaterial(Texture2D texture, string name)
    {
        Shader shader = Shader.Find("PROJECT1864/CampaignCityBillboard");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError(
                CampaignBuildInfo.LogTag +
                "|CityArtN7E=False|Reason=NoTransparentShader");
            return null;
        }

        Material material = new Material(shader)
        {
            name = name,
            mainTexture = texture,
            color = Color.white,
            renderQueue = 4000
        };

        return material;
    }

    private static Mesh BuildBottomAnchoredQuad()
    {
        Mesh mesh = new Mesh
        {
            name = "CITY_ART_BOTTOM_ANCHORED_QUAD_10N7E"
        };

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

        mesh.triangles = new[]
        {
            0, 2, 1,
            0, 3, 2
        };

        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Scale(CampaignDenmark1851Registry.CityDef city)
    {
        if (city.Tier == CampaignDenmark1851Registry.CityTier.A)
            return ScaleA;
        if (city.Tier == CampaignDenmark1851Registry.CityTier.B)
            return ScaleB;
        return ScaleC;
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities =
            CampaignDenmark1851Registry.Cities;

        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i] != null && cities[i].Id == cityId)
                return cities[i];
        }

        return null;
    }

    private static string Describe(Texture2D texture)
    {
        return texture == null
            ? "missing"
            : texture.width + "x" + texture.height;
    }
}
