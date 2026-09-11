using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c5 - extra close-range presentation detail layered on top of c4 renderer.
    // Keeps the c4 simulation/render ratio contract intact and writes no tactical state.
    [DefaultExecutionOrder(545)]
    public sealed class TacticalSoldierDetailOverlay00C5 : MonoBehaviour
    {
        private sealed class DetailState
        {
            public TacticalCompanyEntity00B Company;
            public Vector3[] LineSlots;
            public Vector3[] ColumnSlots;
            public Matrix4x4[] Brim;
            public Matrix4x4[] Badge;
            public Matrix4x4[] Spike;
            public Matrix4x4[] Collar;
            public Matrix4x4[] LeftShoulder;
            public Matrix4x4[] RightShoulder;
        }

        private const float LineFileSpacing = 0.52f;
        private const float LineRankSpacing = 0.76f;
        private const int LineRanks = 3;
        private const int ColumnFiles = 8;
        private const float ColumnFileSpacing = 0.60f;
        private const float ColumnRowSpacing = 0.72f;
        private const float DetailDistance = 300f;

        private readonly List<DetailState> states = new List<DetailState>();

        private Mesh cubeMesh;
        private Mesh cylinderMesh;
        private Material darkMetal;
        private Material brass;
        private Material dkTrim;
        private Material prTrim;
        private Camera cam;
        private bool installed;
        private int drawCalls;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalSoldierDetailOverlay00C5>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C5_SOLDIER_DETAIL_OVERLAY");
            root.AddComponent<TacticalSoldierDetailOverlay00C5>();
        }

        private void Update()
        {
            if (!installed)
            {
                TryInstall();
                if (!installed)
                    return;
            }

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            RenderDetails();
        }

        private void TryInstall()
        {
            TacticalCompanyEntity00B[] companies = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            if (companies == null || companies.Length != 8)
                return;

            CreateMeshes();
            CreateMaterials();
            states.Clear();

            for (int i = 0; i < companies.Length; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                int strength = Mathf.Max(0, company.AuthorizedStrength);
                states.Add(new DetailState
                {
                    Company = company,
                    LineSlots = BuildLineSlots(strength),
                    ColumnSlots = BuildColumnSlots(strength),
                    Brim = new Matrix4x4[strength],
                    Badge = new Matrix4x4[strength],
                    Spike = new Matrix4x4[strength],
                    Collar = new Matrix4x4[strength],
                    LeftShoulder = new Matrix4x4[strength],
                    RightShoulder = new Matrix4x4[strength]
                });
            }

            cam = Camera.main;
            installed = states.Count == 8;

            Debug.Log(
                "REBUILD-SOLDIER-DETAIL-00C5|Installed=" + installed +
                "|CloseDetailDistance=" + DetailDistance.ToString("0") +
                "|HeadgearBrim=True|FrontBadge=True|PrussianSpike=True|Collar=True|ShoulderDetail=True|" +
                "RatioAware=True|FormationTransitionAware=True|WritesTacticalState=False");
        }

        private void RenderDetails()
        {
            drawCalls = 0;
            Vector3 cameraPosition = cam.transform.position;

            for (int s = 0; s < states.Count; s++)
            {
                DetailState state = states[s];
                TacticalCompanyEntity00B company = state.Company;
                if (company == null || !company.gameObject.activeInHierarchy)
                    continue;

                if (Vector3.Distance(cameraPosition, company.transform.position) > DetailDistance)
                    continue;

                int actual = Mathf.Min(company.PresentStrength, state.LineSlots.Length);
                if (actual <= 0)
                    continue;

                int visible = TacticalRebuildSettings00C4.GetVisibleRepresentativeCount(actual);
                FillMatrices(state, actual, visible);
                DrawState(state, visible);
            }
        }

        private void FillMatrices(DetailState state, int actual, int visible)
        {
            TacticalCompanyEntity00B company = state.Company;
            Transform root = company.transform;
            bool danish = company.Nation == RebuildNation.Denmark;

            for (int visualIndex = 0; visualIndex < visible; visualIndex++)
            {
                int sourceIndex = SampleSourceIndex(visualIndex, visible, actual);
                Vector3 slot = GetAnimatedSlot(state, sourceIndex);
                float h = 1f + HashSigned(sourceIndex * 13 + 5) * 0.016f;

                // Headgear visor/brim gives the head silhouette a front/back read.
                state.Brim[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.68f * h, 0.105f),
                    Quaternion.identity,
                    new Vector3(0.245f, 0.024f, 0.115f));

                // Small front badge/plate: intentionally readable rather than historically final art.
                state.Badge[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.765f * h, 0.108f),
                    Quaternion.identity,
                    danish
                        ? new Vector3(0.050f, 0.060f, 0.018f)
                        : new Vector3(0.070f, 0.055f, 0.018f));

                // Prussian headgear gets an extra spike silhouette; Danish matrix is harmless and not drawn.
                state.Spike[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.94f * h, 0f),
                    Quaternion.identity,
                    new Vector3(0.030f, 0.105f, 0.030f));

                state.Collar[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.305f, 0.108f),
                    Quaternion.identity,
                    new Vector3(0.255f, 0.070f, 0.026f));

                state.LeftShoulder[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(-0.155f, 1.285f, 0.010f),
                    Quaternion.Euler(0f, 0f, -6f),
                    new Vector3(0.105f, 0.030f, 0.150f));

                state.RightShoulder[visualIndex] = PartMatrix(
                    root,
                    slot + new Vector3(0.155f, 1.285f, 0.010f),
                    Quaternion.Euler(0f, 0f, 6f),
                    new Vector3(0.105f, 0.030f, 0.150f));
            }
        }

        private void DrawState(DetailState state, int count)
        {
            if (count <= 0)
                return;

            bool danish = state.Company.Nation == RebuildNation.Denmark;
            Material trim = danish ? dkTrim : prTrim;

            DrawInstanced(cubeMesh, darkMetal, state.Brim, count);
            DrawInstanced(cubeMesh, brass, state.Badge, count);
            DrawInstanced(cubeMesh, trim, state.Collar, count);
            DrawInstanced(cubeMesh, trim, state.LeftShoulder, count);
            DrawInstanced(cubeMesh, trim, state.RightShoulder, count);

            if (!danish)
                DrawInstanced(cylinderMesh, darkMetal, state.Spike, count);
        }

        private static Vector3 GetAnimatedSlot(DetailState state, int sourceIndex)
        {
            TacticalCompanyEntity00B company = state.Company;
            Vector3 line = state.LineSlots[sourceIndex];
            Vector3 column = state.ColumnSlots[sourceIndex];

            if (company.IsReforming)
            {
                Vector3 from = company.ReformFromFormation == RebuildFormation.Column ? column : line;
                Vector3 to = company.ReformTargetFormation == RebuildFormation.Column ? column : line;
                return Vector3.Lerp(from, to, company.ReformProgress);
            }

            return company.Formation == RebuildFormation.Column ? column : line;
        }

        private static int SampleSourceIndex(int visualIndex, int visibleCount, int actualCount)
        {
            if (actualCount <= 1)
                return 0;
            if (visibleCount <= 1)
                return actualCount / 2;

            float t = visualIndex / (float)(visibleCount - 1);
            return Mathf.Clamp(Mathf.RoundToInt(t * (actualCount - 1)), 0, actualCount - 1);
        }

        private static Vector3[] BuildLineSlots(int strength)
        {
            Vector3[] slots = new Vector3[strength];
            int files = Mathf.Max(1, Mathf.CeilToInt(strength / (float)LineRanks));
            float width = (files - 1) * LineFileSpacing;
            for (int i = 0; i < strength; i++)
            {
                int rank = i % LineRanks;
                int file = i / LineRanks;
                slots[i] = new Vector3(
                    -width * 0.5f + file * LineFileSpacing + HashSigned(i * 17 + 3) * 0.012f,
                    0f,
                    -rank * LineRankSpacing + HashSigned(i * 31 + 11) * 0.010f);
            }
            return slots;
        }

        private static Vector3[] BuildColumnSlots(int strength)
        {
            Vector3[] slots = new Vector3[strength];
            int rows = Mathf.Max(1, Mathf.CeilToInt(strength / (float)ColumnFiles));
            float width = (ColumnFiles - 1) * ColumnFileSpacing;
            float depth = (rows - 1) * ColumnRowSpacing;
            for (int i = 0; i < strength; i++)
            {
                int file = i % ColumnFiles;
                int row = i / ColumnFiles;
                slots[i] = new Vector3(
                    -width * 0.5f + file * ColumnFileSpacing + HashSigned(i * 17 + 3) * 0.012f,
                    0f,
                    depth * 0.5f - row * ColumnRowSpacing + HashSigned(i * 31 + 11) * 0.010f);
            }
            return slots;
        }

        private static Matrix4x4 PartMatrix(Transform companyTransform, Vector3 localPosition,
            Quaternion localRotation, Vector3 scale)
        {
            return Matrix4x4.TRS(
                companyTransform.TransformPoint(localPosition),
                companyTransform.rotation * localRotation,
                scale);
        }

        private void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (mesh == null || material == null || matrices == null || count <= 0)
                return;

            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count);
            drawCalls++;
        }

        private void CreateMeshes()
        {
            cubeMesh = CapturePrimitiveMesh(PrimitiveType.Cube);
            cylinderMesh = CapturePrimitiveMesh(PrimitiveType.Cylinder);
        }

        private static Mesh CapturePrimitiveMesh(PrimitiveType type)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
            temporary.name = "TEMP_REBUILD_00C5_DETAIL_MESH";
            temporary.SetActive(false);
            MeshFilter filter = temporary.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            UnityEngine.Object.Destroy(temporary);
            return mesh;
        }

        private void CreateMaterials()
        {
            darkMetal = CreateMaterial(new Color(0.018f, 0.020f, 0.024f), "00C5_HeadgearDark");
            brass = CreateMaterial(new Color(0.58f, 0.42f, 0.12f), "00C5_Brass");
            dkTrim = CreateMaterial(new Color(0.66f, 0.055f, 0.050f), "00C5_DK_Trim");
            prTrim = CreateMaterial(new Color(0.48f, 0.045f, 0.045f), "00C5_PR_Trim");
        }

        private static Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.name = materialName;
            material.color = color;
            material.enableInstancing = true;
            return material;
        }

        private static float HashSigned(int value)
        {
            uint x = unchecked((uint)value);
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0x00FFFFFF) / 8388607.5f - 1f;
        }

        private void OnGUI()
        {
            if (!TacticalRebuildSettings00C4.DebugViewEnabled || !installed)
                return;

            GUI.depth = -958;
            GUI.Label(new Rect(Mathf.Max(8f, Screen.width - 385f), 82f, 375f, 20f),
                "C5 close-detail overlay <= " + DetailDistance.ToString("0") + "m | extra draw calls: " + drawCalls);
        }
    }
}
