using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(20000)]
public sealed class CampaignBuildVersionOverlay : MonoBehaviour
{
    public const string CampaignVersion = "v00.00.13m";
    public const string CampaignChannel = "UNIFIED DENMARK 3D MAP DEV";

    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", System.StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignBuildVersionOverlay>() != null)
            return;

        GameObject root = new GameObject("CampaignBuildVersionOverlay_v0013m");
        root.AddComponent<CampaignBuildVersionOverlay>();
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = new Color(0.96f, 0.94f, 0.86f);
        }

        GUI.Label(
            new Rect(12f, 10f, 1050f, 24f),
            "PROJECT 1864 CAMPAIGN | " + CampaignVersion + " " + CampaignChannel,
            style);
    }
}
