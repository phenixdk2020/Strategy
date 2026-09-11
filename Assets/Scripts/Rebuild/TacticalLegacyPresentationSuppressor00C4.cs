using UnityEngine;

namespace Project1864.Rebuild
{
    // c4 cleanup only: prevents superseded c2/c3 presentation/drill scripts from rendering/responding.
    // This component writes no tactical state and is NOT a movement/combat authority layer.
    [DefaultExecutionOrder(440)]
    public sealed class TacticalLegacyPresentationSuppressor00C4 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalLegacyPresentationSuppressor00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C4_PRESENTATION_CLEANUP");
            root.AddComponent<TacticalLegacyPresentationSuppressor00C4>();
        }

        private void Awake()
        {
            Suppress();
        }

        private void Update()
        {
            Suppress();
        }

        private static void Suppress()
        {
            TacticalCompanyRenderer00C oldC2 = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C>();
            if (oldC2 != null && oldC2.enabled)
                oldC2.enabled = false;

            TacticalCompanyRenderer00C3 oldC3Renderer = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C3>();
            if (oldC3Renderer != null && oldC3Renderer.enabled)
                oldC3Renderer.enabled = false;

            TacticalCompanyDrill00C3 oldC3Drill = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C3>();
            if (oldC3Drill != null && oldC3Drill.enabled)
                oldC3Drill.enabled = false;

            TacticalCompanyHoverInfo00C2 oldHover = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyHoverInfo00C2>();
            if (oldHover != null && oldHover.enabled)
                oldHover.enabled = false;
        }
    }
}
