using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 v00.00.10j presentation cleanup.
///
/// - Keeps provider 1 as the Campaign4 basemap but hides the inherited v10g lab UI.
/// - Replaces the inherited perspective-unaware city labels with distance-based semantic zoom.
/// - Drives the legacy zoom-band proxy only to suppress overlapping zone labels at strategic
///   and very close city zooms while retaining operational labels in the middle band.
/// </summary>
[DefaultExecutionOrder(32300)]
public sealed class Campaign4PresentationV010J : MonoBehaviour
{
    private Camera mapCamera;
    private Transform cityRoot;
    private GUIStyle cityStyle;
    private GUIStyle hudStyle;
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4PresentationV010J>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_v10j_Presentation");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4PresentationV010J>();
    }

    private IEnumerator Start()
    {
        // Let the inherited v10g provider lab finish constructing and activating provider 1.
        for (int frame = 0; frame < 300; frame++)
        {
            GameObject provider = GameObject.Find("BASEMAP_01_1_DEM");
            cityRoot = GameObject.Find("CAMPAIGN4_Cities_1850_Top40")?.transform;
            mapCamera = Camera.main;

            if (provider != null && provider.activeInHierarchy && cityRoot != null && cityRoot.childCount >= 40 && mapCamera != null)
                break;

            yield return null;
        }

        CampaignMapStyleSwitcher inheritedLab = Object.FindAnyObjectByType<CampaignMapStyleSwitcher>();
        if (inheritedLab != null)
            inheritedLab.enabled = false;

        Campaign4CityLayer1850 inheritedLabels = Object.FindAnyObjectByType<Campaign4CityLayer1850>();
        if (inheritedLabels != null)
            inheritedLabels.enabled = false;

        mapCamera = Camera.main;
        cityRoot = GameObject.Find("CAMPAIGN4_Cities_1850_Top40")?.transform;
        installed = mapCamera != null && cityRoot != null;

        Debug.Log(
            "CAMPAIGN4-PRESENTATION|Version=v00.00.10j|Installed=" + installed +
            "|LegacyLabUI=False|PerspectiveSemanticLabels=True");
    }

    private void LateUpdate()
    {
        if (!installed)
            return;

        if (mapCamera == null)
            mapCamera = Camera.main;
        if (mapCamera == null)
            return;

        // GrandCampaignBootstrap still derives its label band from orthographicSize even
        // when Campaign4 owns the camera in perspective mode. Use that field only as a
        // compatibility proxy so its old zone labels do not flood the screen.
        float distance = Campaign4Camera3DController.IsActive
            ? Campaign4Camera3DController.CurrentDistance
            : 88f;

        if (distance > 58f)
            mapCamera.orthographicSize = 60f;       // Strategic: hide zone labels.
        else if (distance > 23f)
            mapCamera.orthographicSize = 40f;       // Operational: useful zone labels.
        else
            mapCamera.orthographicSize = 60f;       // Close: city layer takes over labels.
    }

    private void EnsureStyles()
    {
        if (cityStyle != null)
            return;

        cityStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        cityStyle.normal.textColor = new Color(0.97f, 0.95f, 0.86f);

        hudStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        hudStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!installed || cityRoot == null || mapCamera == null)
            return;

        EnsureStyles();

        float distance = Campaign4Camera3DController.IsActive
            ? Campaign4Camera3DController.CurrentDistance
            : 88f;

        int maxRank = distance > 58f ? 5 : distance > 27f ? 15 : 40;
        cityStyle.fontSize = distance <= 18f ? 11 : 10;

        for (int i = 0; i < cityRoot.childCount; i++)
        {
            Transform city = cityRoot.GetChild(i);
            if (city == null || !city.name.StartsWith("CITY1850_", StringComparison.Ordinal))
                continue;

            int rank = ParseRank(city.name);
            if (rank <= 0 || rank > maxRank)
                continue;

            Vector3 screen = mapCamera.WorldToScreenPoint(city.position + new Vector3(0f, 0.18f, 0f));
            if (screen.z <= 0f)
                continue;
            if (screen.x < -120f || screen.x > Screen.width + 20f ||
                screen.y < -20f || screen.y > Screen.height + 20f)
                continue;

            string displayName = ParseName(city.name);
            float y = Screen.height - screen.y;
            GUI.Label(new Rect(screen.x + 6f, y - 9f, 155f, 20f), displayName, cityStyle);
        }

        Rect hud = new Rect(Screen.width - 318f, Screen.height - 34f, 310f, 26f);
        GUI.Box(hud, "CAMPAIGN4 v00.00.10j | 3D DEM | F: Aalborg | Home: Danmark", hudStyle);
    }

    private static int ParseRank(string objectName)
    {
        const string prefix = "CITY1850_";
        if (objectName.Length < prefix.Length + 2)
            return -1;

        string value = objectName.Substring(prefix.Length, 2);
        return int.TryParse(value, out int rank) ? rank : -1;
    }

    private static string ParseName(string objectName)
    {
        int first = objectName.IndexOf('_');
        if (first < 0)
            return objectName;

        int second = objectName.IndexOf('_', first + 1);
        if (second < 0 || second + 1 >= objectName.Length)
            return objectName;

        return objectName.Substring(second + 1);
    }
}
