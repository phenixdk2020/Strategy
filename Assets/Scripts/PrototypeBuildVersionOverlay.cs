using UnityEngine;

[DefaultExecutionOrder(20000)]
public sealed class PrototypeBuildVersionOverlay : MonoBehaviour
{
    public const string BuildVersion = "v00.00.09f30e";
    public const string BuildChannel = "TEST";

    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeBuildVersionOverlay>() != null)
            return;
        new GameObject("PrototypeBuildVersionOverlay_" + BuildVersion)
            .AddComponent<PrototypeBuildVersionOverlay>();
    }

    private void EnsureStyle()
    {
        if (style != null)
            return;
        style = PrototypeUiTheme09F15.Header(11);
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(6, 6, 2, 2);
    }

    private void OnGUI()
    {
        EnsureStyle();
        GUI.depth = -1000;
        GUI.Box(new Rect(8f, 8f, 220f, 24f),
            "PROJECT 1864 | " + BuildVersion + " " + BuildChannel, style);
    }
}
