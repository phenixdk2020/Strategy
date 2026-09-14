using UnityEngine;

// v00.00.09f16 selection/input bridge. PlayerCommander handles normal Regiment
// selection first (execution order 300); this runs afterwards so Major behaves like
// a normal selectable unit without changing the proven company-selection code.
[DefaultExecutionOrder(360)]
public sealed class PrototypeMajorSelectionBridge09F16 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorSelectionBridge09F16>() != null)
            return;
        GameObject root = new GameObject("PrototypeMajorSelectionBridge_v000009f16");
        root.AddComponent<PrototypeMajorSelectionBridge09F16>();
    }

    private void Update()
    {
        PrototypeMajorHQ09F15 major = PrototypeMajorHQ09F15.Instance;
        if (major == null || !Input.GetMouseButtonDown(0))
            return;

        if (major.IsPointerOverControls(Input.mousePosition))
            return;

        // Target-selection click has priority while a Major order is armed.
        if (major.DebugHandlePendingTargetClick(Input.mousePosition))
            return;

        bool hitMajor = major.DebugRayHitsHQ(Input.mousePosition);
        if (hitMajor)
        {
            major.DebugSetSelected(true);
            return;
        }

        if (major.DebugIsSelected)
            major.DebugSetSelected(false);
    }
}
