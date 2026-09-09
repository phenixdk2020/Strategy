using UnityEngine;

// v00.00.09l TEST - lightweight runtime scale telemetry for the 1:1 pilot.
// This is intentionally not a replacement for Unity Profiler/GPU timing; it gives a
// stable on-screen FPS/frame-ms/manpower reference for screenshots and quick QA.
[DefaultExecutionOrder(20500)]
public sealed class PrototypePerformanceOverlay09K : MonoBehaviour
{
    private float smoothedDelta = 1f / 60f;
    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypePerformanceOverlay09K>() != null)
            return;

        GameObject root = new GameObject("PrototypePerformanceOverlay_v000009l");
        root.AddComponent<PrototypePerformanceOverlay09K>();
    }

    private void Update()
    {
        float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        smoothedDelta = Mathf.Lerp(smoothedDelta, dt, 0.08f);
    }

    private void EnsureStyle()
    {
        if (style != null)
            return;

        style = new GUIStyle(GUI.skin.box);
        style.fontSize = 10;
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(7, 7, 3, 3);
    }

    private void OnGUI()
    {
        EnsureStyle();

        int infantry = 0;
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment regiment in battle.Regiments)
            {
                if (regiment == null)
                    continue;
                if (regiment.RegimentName == "1. Regiment" ||
                    regiment.RegimentName == "5. Regiment" ||
                    regiment.RegimentName == "8th Regiment" ||
                    regiment.RegimentName == "18th Regiment")
                {
                    infantry += Mathf.Max(0, regiment.CurrentStrength);
                }
            }
        }

        float fps = 1f / Mathf.Max(0.0001f, smoothedDelta);
        float ms = smoothedDelta * 1000f;
        string text =
            "09L SCALE | FPS " + fps.ToString("0") +
            " | " + ms.ToString("0.0") + " ms" +
            " | Infantry " + infantry.ToString("N0") +
            " | Cavalry 295 (275 mounted) | Art.Crew 310 | Guns 14";

        float width = Mathf.Min(590f, Screen.width - 24f);
        GUI.Box(new Rect(Screen.width - width - 10f, 8f, width, 24f), text, style);
    }
}
