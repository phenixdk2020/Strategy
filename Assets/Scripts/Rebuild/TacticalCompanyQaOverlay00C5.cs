using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c5 - read-only selected Company facing overlay.
    // Shows the Company's forward direction and footprint centre without changing tactical state.
    [DefaultExecutionOrder(735)]
    public sealed class TacticalCompanyQaOverlay00C5 : MonoBehaviour
    {
        private sealed class ArrowVisual
        {
            public TacticalCompanyEntity00B Company;
            public GameObject Root;
            public LineRenderer Shaft;
            public LineRenderer Head;
        }

        public static bool DirectionOverlayEnabled { get; set; } = true;

        private readonly List<ArrowVisual> arrows = new List<ArrowVisual>();
        private Material arrowMaterial;
        private bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyQaOverlay00C5>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C5_QA_FACING_OVERLAY");
            root.AddComponent<TacticalCompanyQaOverlay00C5>();
        }

        private void Start()
        {
            CreateMaterial();
            TryInstall();
        }

        private void Update()
        {
            if (!installed)
            {
                TryInstall();
                if (!installed)
                    return;
            }

            for (int i = 0; i < arrows.Count; i++)
                UpdateArrow(arrows[i]);
        }

        private void TryInstall()
        {
            TacticalCompanyEntity00B[] companies = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            if (companies == null || companies.Length != 8)
                return;

            for (int i = 0; i < arrows.Count; i++)
            {
                if (arrows[i] != null && arrows[i].Root != null)
                    Destroy(arrows[i].Root);
            }
            arrows.Clear();

            for (int i = 0; i < companies.Length; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                GameObject root = new GameObject("QA_FacingArrow_" + company.UnitId);
                root.transform.SetParent(transform, false);

                LineRenderer shaft = root.AddComponent<LineRenderer>();
                ConfigureLine(shaft, 2, 0.16f);

                GameObject headObject = new GameObject("Head");
                headObject.transform.SetParent(root.transform, false);
                LineRenderer head = headObject.AddComponent<LineRenderer>();
                ConfigureLine(head, 3, 0.16f);

                arrows.Add(new ArrowVisual
                {
                    Company = company,
                    Root = root,
                    Shaft = shaft,
                    Head = head
                });
            }

            installed = arrows.Count == 8;
            Debug.Log(
                "REBUILD-QA-OVERLAY-00C5|Installed=" + installed +
                "|Companies=" + arrows.Count + "|FacingArrows=True|WritesTacticalState=False");
        }

        private void ConfigureLine(LineRenderer line, int count, float width)
        {
            line.useWorldSpace = true;
            line.positionCount = count;
            line.startWidth = width;
            line.endWidth = width;
            line.material = arrowMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            line.enabled = false;
        }

        private void UpdateArrow(ArrowVisual visual)
        {
            TacticalCompanyEntity00B company = visual.Company;
            bool show = DirectionOverlayEnabled && company != null && company.IsSelected && company.gameObject.activeInHierarchy;
            visual.Shaft.enabled = show;
            visual.Head.enabled = show;
            if (!show)
                return;

            Vector3 start = company.transform.position + Vector3.up * 0.28f;
            Vector3 forward = company.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            float length = Mathf.Max(7f, company.FootprintDepth * 0.5f + 5.5f);
            Vector3 tip = start + forward * length;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float headLength = Mathf.Clamp(length * 0.18f, 1.1f, 2.2f);
            float headWidth = headLength * 0.72f;
            Vector3 headBase = tip - forward * headLength;

            visual.Shaft.SetPosition(0, start);
            visual.Shaft.SetPosition(1, tip);

            visual.Head.SetPosition(0, headBase + right * headWidth);
            visual.Head.SetPosition(1, tip);
            visual.Head.SetPosition(2, headBase - right * headWidth);
        }

        private void CreateMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            arrowMaterial = new Material(shader);
            arrowMaterial.name = "00C5_QA_FacingArrow";
            arrowMaterial.color = new Color(1f, 0.78f, 0.12f, 0.92f);
        }
    }
}
