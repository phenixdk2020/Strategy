using UnityEngine;

// Small always-visible TEST build marker so screenshots immediately reveal
// which prototype revision is running. Bump the suffix for every TEST delivery.
[DefaultExecutionOrder(20000)]
public sealed class PrototypeBuildVersionOverlay : MonoBehaviour
{
    public const string BuildVersion = "v00.00.09l5";
    public const string BuildChannel = "TEST";

    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeBuildVersionOverlay>() != null)
            return;

        GameObject root = new GameObject("PrototypeBuildVersionOverlay_" + BuildVersion);
        root.AddComponent<PrototypeBuildVersionOverlay>();
    }

    private void EnsureStyle()
    {
        if (style != null)
            return;

        style = new GUIStyle(GUI.skin.box);
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(6, 6, 2, 2);
    }

    private void OnGUI()
    {
        EnsureStyle();
        GUI.depth = -1000;
        GUI.Box(
            new Rect(8f, 8f, 204f, 24f),
            "PROJECT 1864 | " + BuildVersion + " " + BuildChannel,
            style);
    }
}
