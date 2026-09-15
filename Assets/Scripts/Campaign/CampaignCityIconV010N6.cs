using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n6e - Proposal 3 City Art visibility/polish pass.
///
/// Uses authored transparent A/B/C city-art textures from Resources and keeps
/// CITY-REG-01 identity, labels, CityId/ZoneId and movement semantics unchanged.
///
/// n6e specifically fixes the Play Mode report that city art was effectively not
/// visible: icon world scale is increased substantially, the billboard is lifted
/// farther above terrain, Resources lookup has a secondary folder scan, and all
/// older n6/n6b/n6c visual children are removed before installation.
/// </summary>
[DefaultExecutionOrder(23500)]
public sealed class CampaignCityIconV010N6 : MonoBehaviour
{
    private const string ProceduralIconRootName = "CITY_ICON_ISOMETRIC_10N6";
    private const string N6BIconRootName = "CITY_ICON_PROPOSAL3_10N6B";
    private const string N6CIconRootName = "CITY_ICON_PROPOSAL3_10N6C";
    private const string IconRootName = "CITY_ICON_PROPOSAL3_10N6E";
    private const string ResourceRoot = "Campaign/CityIcons/";
    private const string ResourceFolder = "Campaign/CityIcons";
    private const float GroundLift = 0.32f;
    private const float RetrySeconds = 1.0f;

    private readonly List<Transform> billboardRoots = new List<Transform>();

    private Sprite spriteA;
    private Sprite spriteB;
    private Sprite spriteC;
    private bool installed;
    private bool missingTextureLogged;
    private float nextRetryTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<CampaignCityIconV010N6>() != null)
            return;

        GameObject go = new GameObject("PROJECT1864_CITY_ART_10N6E");
        DontDestroyOnLoad(go);
        go.AddComponent<CampaignCityIconV010N6>();
    }

    private void Update()
    {
        if (installed || GrandCampaignBootstrap.Instance == null)
            return;

        if (Time.unscaledTime < nextRetryTime)
            return;

        nextRetryTime = Time.unscaledTime + RetrySeconds;
        if (!EnsureSprites())
            return;

        GrandCampaignCityMarker[] markers =
            Object.FindObjectsByType<GrandCampaignCityMarker>(FindObjectsInactive.Exclude);
        if (markers == null || markers.Length == 0)
            return;

        billboardRoots.Clear();
        int installedCount = 0;

        for (int i = 0; i < markers.Length; i++)
        {
            GrandCampaignCityMarker marker = markers[i];
            if (marker == null || string.IsNullOrEmpty(marker.CityId))
                continue;

            CampaignDenmark1851Registry.CityDef city = FindCity(marker.CityId);
            if (city == null)
                continue;

            if (InstallOnMarker(marker.gameObject, city))
                installedCount++;
        }

        int hiddenZoneCentreDiscs = HideZoneCentreDiscs();

        if (installedCount < CampaignDenmark1851Registry.Cities.Length)
        {
            Debug.LogWarning(
                CampaignBuildInfo.LogTag +
                "|Proposal3CityArt=Partial|Installed=" + installedCount +
                "|Expected=" + CampaignDenmark1851Registry.Cities.Length);
            return;
        }

        installed = true;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Proposal3CityArt=True" +
            "|Cities=" + installedCount +
            "|TierA=" + CountTier(CampaignDenmark1851Registry.CityTier.A) +
            "|TierB=" + CountTier(CampaignDenmark1851Registry.CityTier.B) +
            "|TierC=" + CountTier(CampaignDenmark1851Registry.CityTier.C) +
            "|TextureA=" + DescribeTexture(spriteA) +
            "|TextureB=" + DescribeTexture(spriteB) +
            "|TextureC=" + DescribeTexture(spriteC) +
            "|BaseScaleA=2.15|BaseScaleB=1.60|BaseScaleC=1.18" +
            "|GroundLift=" + GroundLift.ToString("0.00") +
            "|AlphaCutout=True" +
            "|GreenTownBase=False" +
            "|RoundCityRenderer=False" +
            "|ZoneCentreDiscsHidden=" + hiddenZoneCentreDiscs +
            "|StableClickCollider=True");
    }

    private void LateUpdate()
    {
        if (!installed)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        for (int i = billboardRoots.Count - 1; i >= 0; i--)
        {
            Transform root = billboardRoots[i];
            if (root == null)
            {
                billboardRoots.RemoveAt(i);
                continue;
            }

            Vector3 toCamera = cam.transform.position - root.position;
            if (toCamera.sqrMagnitude < 0.0001f)
                continue;

            root.rotation = Quaternion.LookRotation(toCamera.normalized, cam.transform.up);
        }
    }

    private bool EnsureSprites()
    {
        if (spriteA != null && spriteB != null && spriteC != null)
            return true;

        spriteA = spriteA != null ? spriteA : LoadSprite(ResourceRoot + "City_A_Proposal3", "City_A_Proposal3_Sprite");
        spriteB = spriteB != null ? spriteB : LoadSprite(ResourceRoot + "City_B_Proposal3", "City_B_Proposal3_Sprite");
        spriteC = spriteC != null ? spriteC : LoadSprite(ResourceRoot + "City_C_Proposal3", "City_C_Proposal3_Sprite");

        // Secondary lookup protects against odd importer/path state after branch
        // changes: scan the Resources folder and resolve by texture name.
        if (spriteA == null || spriteB == null || spriteC == null)
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>(ResourceFolder);
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture == null)
                    continue;

                if (spriteA == null && texture.name == "City_A_Proposal3")
                    spriteA = CreateSprite(texture, "City_A_Proposal3_Sprite");
                else if (spriteB == null && texture.name == "City_B_Proposal3")
                    spriteB = CreateSprite(texture, "City_B_Proposal3_Sprite");
                else if (spriteC == null && texture.name == "City_C_Proposal3")
                    spriteC = CreateSprite(texture, "City_C_Proposal3_Sprite");
            }
        }

        if (spriteA == null || spriteB == null || spriteC == null)
        {
            if (!missingTextureLogged)
            {
                missingTextureLogged = true;
                Debug.LogError(
                    CampaignBuildInfo.LogTag +
                    "|Proposal3CityArt=False|Reason=MissingTexture" +
                    "|A=" + (spriteA != null) +
                    "|B=" + (spriteB != null) +
                    "|C=" + (spriteC != null) +
                    "|ResourceRoot=" + ResourceRoot +
                    "|FolderScan=True" +
                    "|RetrySeconds=" + RetrySeconds);
            }
            return false;
        }

        missingTextureLogged = false;
        Debug.Log(
            CampaignBuildInfo.LogTag +
            "|Proposal3TexturesLoaded=True" +
            "|A=" + DescribeTexture(spriteA) +
            "|B=" + DescribeTexture(spriteB) +
            "|C=" + DescribeTexture(spriteC));
        return true;
    }

    private static Sprite LoadSprite(string resourcePath, string spriteName)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        return texture == null ? null : CreateSprite(texture, spriteName);
    }

    private static Sprite CreateSprite(Texture2D texture, string spriteName)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.06f),
            Mathf.Max(1f, texture.width),
            0,
            SpriteMeshType.FullRect);
        sprite.name = spriteName;
        return sprite;
    }

    private bool InstallOnMarker(GameObject markerObject, CampaignDenmark1851Registry.CityDef city)
    {
        if (markerObject == null || city == null)
            return false;

        Transform existing = markerObject.transform.Find(IconRootName);
        if (existing != null)
        {
            if (!billboardRoots.Contains(existing))
                billboardRoots.Add(existing);
            return true;
        }

        DestroyChildIfPresent(markerObject.transform, ProceduralIconRootName);
        DestroyChildIfPresent(markerObject.transform, N6BIconRootName);
        DestroyChildIfPresent(markerObject.transform, N6CIconRootName);

        Renderer oldRenderer = markerObject.GetComponent<Renderer>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        Collider[] oldColliders = markerObject.GetComponents<Collider>();
        for (int i = 0; i < oldColliders.Length; i++)
            oldColliders[i].enabled = false;

        markerObject.transform.localScale = Vector3.one;

        float iconScale = GetIconScale(city);

        BoxCollider clickBox = markerObject.AddComponent<BoxCollider>();
        clickBox.center = new Vector3(0f, 0.70f, 0f);
        clickBox.size = new Vector3(
            Mathf.Max(0.80f, iconScale * 0.95f),
            1.80f,
            Mathf.Max(0.70f, iconScale * 0.82f));

        GameObject rootObject = new GameObject(IconRootName);
        Transform root = rootObject.transform;
        root.SetParent(markerObject.transform, false);
        root.localPosition = new Vector3(0f, GroundLift, 0f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one * iconScale;

        SpriteRenderer spriteRenderer = rootObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForTier(city.Tier);
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 120;
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;

        billboardRoots.Add(root);
        return spriteRenderer.sprite != null;
    }

    private static void DestroyChildIfPresent(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            Destroy(child.gameObject);
    }

    private Sprite GetSpriteForTier(CampaignDenmark1851Registry.CityTier tier)
    {
        if (tier == CampaignDenmark1851Registry.CityTier.A)
            return spriteA;
        if (tier == CampaignDenmark1851Registry.CityTier.B)
            return spriteB;
        return spriteC;
    }

    private static int HideZoneCentreDiscs()
    {
        GrandCampaignZoneMarker[] zoneMarkers =
            Object.FindObjectsByType<GrandCampaignZoneMarker>(FindObjectsInactive.Exclude);

        int hidden = 0;
        for (int i = 0; i < zoneMarkers.Length; i++)
        {
            GrandCampaignZoneMarker zoneMarker = zoneMarkers[i];
            if (zoneMarker == null)
                continue;

            Renderer renderer = zoneMarker.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                renderer.enabled = false;
                hidden++;
            }
        }

        return hidden;
    }

    private static float GetIconScale(CampaignDenmark1851Registry.CityDef city)
    {
        float baseScale;
        float maxBonus;
        float tierPopulationReference;

        if (city.Tier == CampaignDenmark1851Registry.CityTier.A)
        {
            baseScale = 2.15f;
            maxBonus = 0.15f;
            tierPopulationReference = 130000f;
        }
        else if (city.Tier == CampaignDenmark1851Registry.CityTier.B)
        {
            baseScale = 1.60f;
            maxBonus = 0.12f;
            tierPopulationReference = 4000f;
        }
        else
        {
            baseScale = 1.18f;
            maxBonus = 0.10f;
            tierPopulationReference = 2000f;
        }

        float population01 = Mathf.Clamp01(city.Population1850 / tierPopulationReference);
        return baseScale * (1f + maxBonus * population01);
    }

    private static CampaignDenmark1851Registry.CityDef FindCity(string cityId)
    {
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i] != null && cities[i].Id == cityId)
                return cities[i];
        }
        return null;
    }

    private static int CountTier(CampaignDenmark1851Registry.CityTier tier)
    {
        int count = 0;
        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
            if (cities[i] != null && cities[i].Tier == tier)
                count++;
        return count;
    }

    private static string DescribeTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return "missing";
        return sprite.texture.width + "x" + sprite.texture.height;
    }
}
