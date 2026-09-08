using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(20000)]
public sealed class CampaignBuildVersionOverlay : MonoBehaviour
{
    public const string CampaignVersion = "v00.00.13";
    public const string CampaignChannel = "3D DEV";

    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", System.StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignBuildVersionOverlay>() != null)
            return;

        GameObject root = new GameObject("CampaignBuildVersionOverlay_v0013");
        root.AddComponent<CampaignBuildVersionOverlay>();
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.white;
        }

        GUI.Label(
            new Rect(12f, 10f, 520f, 28f),
            "PROJECT 1864 CAMPAIGN | " + CampaignVersion + " " + CampaignChannel,
            style);
    }
}
