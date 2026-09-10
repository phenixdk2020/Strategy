using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-loads on CampaignMap. Draws version BELOW the Game-toolbar.
/// Lives on Strategy/campaign2 — the folder updater maps to Strategy-Campaign2.
/// </summary>
public sealed class CampaignVersionHud : MonoBehaviour
{
    public float offsetFromTop = 72f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", System.StringComparison.Ordinal) &&
            !scene.name.ToLowerInvariant().Contains("campaign"))
            return;
        if (Object.FindAnyObjectByType<CampaignVersionHud>() != null)
            return;
        var go = new GameObject("CampaignVersionHud_v0003");
        go.AddComponent<CampaignVersionHud>();
    }

    void OnGUI()
    {
        bool atlas = false;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb != null && mb.GetType().Name == "TheaterAtlasBootstrap")
            {
                atlas = true;
                break;
            }
        }
        string text = atlas
            ? "PROJECT 1864  ·  campaign2  ·  v00.00.03 ATLAS"
            : "PROJECT 1864  ·  campaign2  ·  UNIFIED  —  atlas kører IKKE";

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 14, 8, 8)
        };
        style.normal.textColor = atlas
            ? new Color(0.93f, 0.86f, 0.70f)
            : new Color(1f, 0.82f, 0.45f);

        GUI.Box(new Rect(12f, offsetFromTop, 720f, 36f), text, style);
    }
}
