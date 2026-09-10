using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// PROJECT 1864 Campaign v00.00.13m1
// Startup isolation gate for the unified Denmark renderer.
// Purpose: old campaign presentation layers may still need to execute long enough to provide
// the DEM collider/cache, but they must never be visible while v13m is loading.
[DefaultExecutionOrder(4640)]
public sealed class CampaignUnifiedMapStartupGateV013M1 : MonoBehaviour
{
    public const string BuildTag = "v00.00.13m1";

    private const string UnifiedBaseSurfaceName = "V013M_UnifiedDenmarkSurface";
    private bool unifiedVisible;
    private GUIStyle loadingStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignUnifiedMapStartupGateV013M1>() != null)
            return;

        new GameObject("CampaignUnifiedMapStartupGateV013M1").AddComponent<CampaignUnifiedMapStartupGateV013M1>();
    }

    private void Awake()
    {
        DisableNonEssentialOldGuiOwners();
        SuppressLegacyRenderers();
    }

    private void Start()
    {
        DisableNonEssentialOldGuiOwners();
        SuppressLegacyRenderers();
    }

    private void Update()
    {
        DisableNonEssentialOldGuiOwners();
        SuppressLegacyRenderers();
        GateUnifiedBaseSurface();
    }

    private static void DisableNonEssentialOldGuiOwners()
    {
        // These layers are presentation-only predecessors. They are not needed to create
        // the v13j DEM collider used by v13m and must not paint labels/status/tiles on top.
        CampaignLiveCartographicDrapeV013K k = UnityEngine.Object.FindAnyObjectByType<CampaignLiveCartographicDrapeV013K>();
        if (k != null) k.enabled = false;

        CampaignPremiumCartographicVisualV013L l = UnityEngine.Object.FindAnyObjectByType<CampaignPremiumCartographicVisualV013L>();
        if (l != null) l.enabled = false;

        CampaignHistoricalMapLabelsV013J labels = UnityEngine.Object.FindAnyObjectByType<CampaignHistoricalMapLabelsV013J>();
        if (labels != null) labels.enabled = false;
    }

    private void SuppressLegacyRenderers()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name;
            if (string.Equals(n, UnifiedBaseSurfaceName, StringComparison.Ordinal) ||
                n.StartsWith("V013M_", StringComparison.Ordinal))
                continue;

            if (IsLegacyPresentationName(n))
                renderer.enabled = false;
        }
    }

    private static bool IsLegacyPresentationName(string n)
    {
        if (string.IsNullOrEmpty(n)) return false;

        return n.StartsWith("V013A_", StringComparison.Ordinal) ||
               n.StartsWith("V013B_", StringComparison.Ordinal) ||
               n.StartsWith("V013C_", StringComparison.Ordinal) ||
               n.StartsWith("V013D_", StringComparison.Ordinal) ||
               n.StartsWith("V013E_", StringComparison.Ordinal) ||
               n.StartsWith("V013F_", StringComparison.Ordinal) ||
               n.StartsWith("V013G_", StringComparison.Ordinal) ||
               n.StartsWith("V013H_", StringComparison.Ordinal) ||
               n.StartsWith("V013I_", StringComparison.Ordinal) ||
               n.StartsWith("V013J_", StringComparison.Ordinal) ||
               n.StartsWith("V013K_", StringComparison.Ordinal) ||
               n.StartsWith("V013L_", StringComparison.Ordinal) ||
               n.StartsWith("GIS_", StringComparison.Ordinal) ||
               n.StartsWith("Outline_", StringComparison.Ordinal) ||
               n.StartsWith("StrategicLink_", StringComparison.Ordinal) ||
               n.StartsWith("CampaignNode_", StringComparison.Ordinal) ||
               n.StartsWith("CampaignControl_", StringComparison.Ordinal) ||
               n.StartsWith("Settlement3D_", StringComparison.Ordinal);
    }

    private void GateUnifiedBaseSurface()
    {
        GameObject go = GameObject.Find(UnifiedBaseSurfaceName);
        if (go == null)
        {
            unifiedVisible = false;
            return;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            unifiedVisible = false;
            return;
        }

        Material material = renderer.sharedMaterial;
        bool hasTexture = false;
        if (material != null)
        {
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                hasTexture = true;
            else if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                hasTexture = true;
        }

        renderer.enabled = hasTexture;
        unifiedVisible = hasTexture;

        // Once the collider exists, v13j no longer needs to continue building visible legacy content.
        GameObject oldTerrain = GameObject.Find("V013J_SmoothTerrain");
        if (oldTerrain != null && oldTerrain.GetComponent<MeshCollider>() != null)
        {
            CampaignHistorical3DMapV013J j = UnityEngine.Object.FindAnyObjectByType<CampaignHistorical3DMapV013J>();
            if (j != null && j.enabled)
                j.enabled = false;
        }
    }

    private void OnGUI()
    {
        if (unifiedVisible)
            return;

        if (loadingStyle == null)
        {
            loadingStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            loadingStyle.normal.textColor = new Color(0.93f, 0.92f, 0.86f);
        }

        const float width = 390f;
        const float height = 38f;
        GUI.Box(new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - height * 0.5f, width, height),
            "Indlæser samlet 3D Danmark-kort …", loadingStyle);
    }
}
