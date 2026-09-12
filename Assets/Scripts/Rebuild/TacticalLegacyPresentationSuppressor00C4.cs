using UnityEngine;

namespace Project1864.Rebuild
{
    // Presentation/runtime cleanup retained into Gate D2.
    // It disables superseded owners only; it never writes tactical pose/combat state itself.
    [DefaultExecutionOrder(440)]
    public sealed class TacticalLegacyPresentationSuppressor00C4 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalLegacyPresentationSuppressor00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_PRESENTATION_CLEANUP");
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

            bool gateD = PrototypeBuildVersionOverlay.BuildVersion.StartsWith("v00.01.00d");
            if (!gateD)
                return;

            TacticalCompanyRenderer00C4 oldC4Renderer = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C4>();
            if (oldC4Renderer != null && oldC4Renderer.enabled)
                oldC4Renderer.enabled = false;

            TacticalCompanyDrill00C4 oldC4Drill = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C4>();
            if (oldC4Drill != null && oldC4Drill.enabled)
                oldC4Drill.enabled = false;

            TacticalSoldierDetailOverlay00C5 oldC5Detail = UnityEngine.Object.FindAnyObjectByType<TacticalSoldierDetailOverlay00C5>();
            if (oldC5Detail != null && oldC5Detail.enabled)
                oldC5Detail.enabled = false;

            bool gateD2 = PrototypeBuildVersionOverlay.BuildVersion.StartsWith("v00.01.00d2");
            if (!gateD2)
                return;

            TacticalCompanyOrder00D1 oldD1Order = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyOrder00D1>();
            if (oldD1Order != null && oldD1Order.enabled)
                oldD1Order.enabled = false;

            TacticalQaLab00C5 oldQa = UnityEngine.Object.FindAnyObjectByType<TacticalQaLab00C5>();
            if (oldQa != null && oldQa.enabled)
                oldQa.enabled = false;
        }
    }
}
