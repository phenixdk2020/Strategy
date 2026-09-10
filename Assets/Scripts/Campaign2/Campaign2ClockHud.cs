using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal clock/pause bar. Replaces CampaignMapController OnGUI on campaign2
/// so node-labels are not drawn.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class Campaign2ClockHud : MonoBehaviour
{
    bool paused = true;
    float speed = 1f;
    GUIStyle topStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != "CampaignMap")
            return;
        if (FindAnyObjectByType<Campaign2ClockHud>() != null)
            return;
        new GameObject("Campaign2ClockHud").AddComponent<Campaign2ClockHud>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;
    }

    void OnGUI()
    {
        if (topStyle == null)
        {
            topStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            topStyle.normal.textColor = Color.white;
        }

        Rect top = new Rect(Screen.width * 0.5f - 360f, 18f, 720f, 34f);
        GUI.Box(top, string.Empty);
        string state = paused ? "PAUSE" : "x" + speed.ToString("0");
        string clock = CampaignSession.CurrentDateTime.ToString("d MMM yyyy HH:mm") + " | " + state;
        GUI.Box(new Rect(top.x + 4f, top.y + 4f, 255f, 26f), clock, topStyle);
        if (GUI.Button(new Rect(top.x + 264f, top.y + 4f, 100f, 26f), paused ? "FORTSÆT" : "PAUSE"))
            paused = !paused;
        if (GUI.Button(new Rect(top.x + 369f, top.y + 4f, 80f, 26f), speed == 1f && !paused ? "[1]" : "1"))
        { speed = 1f; paused = false; }
        if (GUI.Button(new Rect(top.x + 454f, top.y + 4f, 80f, 26f), speed == 5f && !paused ? "[5]" : "5"))
        { speed = 5f; paused = false; }
        if (GUI.Button(new Rect(top.x + 539f, top.y + 4f, 80f, 26f), speed == 20f && !paused ? "[20]" : "20"))
        { speed = 20f; paused = false; }
    }
}
