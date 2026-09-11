using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c4 - ratio-aware, formation-transition-aware 1:1/1:N presentation renderer.
    // Actual OOB/manpower always remains untouched. Only visible representatives change.
    [DefaultExecutionOrder(530)]
    public sealed class TacticalCompanyRenderer00C4 : MonoBehaviour
    {
        private enum DetailLevel { Full, Medium, Far }

        private sealed class CompanyRenderState
        {
            public TacticalCompanyEntity00B Company;
            public Vector3[] LineSlots;
            public Vector3[] ColumnSlots;
            public Renderer QaFootprintRenderer;

            public Matrix4x4[] LeftLeg;
            public Matrix4x4[] RightLeg;
            public Matrix4x4[] LeftBoot;
            public Matrix4x4[] RightBoot;
            public Matrix4x4[] Torso;
            public Matrix4x4[] CoatSkirt;
            public Matrix4x4[] Head;
            public Matrix4x4[] Headgear;
            public Matrix4x4[] LeftArm;
            public Matrix4x4[] RightArm;
            public Matrix4x4[] LeftHand;
            public Matrix4x4[] RightHand;
            public Matrix4x4[] CrossBelt;
            public Matrix4x4[] WaistBelt;
            public Matrix4x4[] Pack;
            public Matrix4x4[] CartridgeBox;
            public Matrix4x4[] RifleStock;
            public Matrix4x4[] RifleBarrel;
            public Matrix4x4[] Bayonet;
        }

        private const float LineFileSpacing = 0.52f;
        private const float LineRankSpacing = 0.76f;
        private const int LineRanks = 3;
        private const int ColumnFiles = 8;
        private const float ColumnFileSpacing = 0.60f;
        private const float ColumnRowSpacing = 0.72f;

        private const float FullDetailDistance = 280f;
        private const float MediumDetailDistance = 560f;
        private const float MaximumRenderDistance = 950f;

        private readonly List<CompanyRenderState> states = new List<CompanyRenderState>();

        private Mesh cubeMesh;
        private Mesh sphereMesh;
        private Mesh cylinderMesh;
        private Mesh capsuleMesh;
        private Mesh taperedTorsoMesh;
        private Mesh taperedSkirtMesh;

        private Material dkCoat;
        private Material dkTrousers;
        private Material dkHeadgear;
        private Material prCoat;
        private Material prTrousers;
        private Material prHeadgear;
        private Material skin;
        private Material leather;
        private Material strap;
        private Material rifleWood;
        private Material metal;
        private Material boots;

        private Camera mainCamera;
        private bool installed;
        private float smoothedDelta;
        private int visibleRepresentatives;
        private int actualManpower;
        private int fullDetailCompanies;
        private int mediumDetailCompanies;
        private int farDetailCompanies;
        private int drawCalls;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyRenderer00C oldC2 = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C>();
            if (oldC2 != null)
                oldC2.enabled = false;

            TacticalCompanyRenderer00C3 oldC3 = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C3>();
            if (oldC3 != null)
                oldC3.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C4_SOLDIER_RENDERER");
            root.AddComponent<TacticalCompanyRenderer00C4>();
        }

        private void Update()
        {
            if (!installed)
            {
                TryInstall();
                if (!installed)
                    return;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RenderCompanies();

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smoothedDelta = smoothedDelta <= 0f ? dt : Mathf.Lerp(smoothedDelta, dt, 0.08f);
        }

        private void TryInstall()
        {
            TacticalCompanyEntity00B[] companies = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            if (companies == null || companies.Length != 8)
                return;

            CreateMeshes();
            CreateMaterials();
            states.Clear();

            int total = 0;
            for (int i = 0; i < companies.Length; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                states.Add(BuildState(company));
                total += company.PresentStrength;
            }

            actualManpower = total;
            mainCamera = Camera.main;
            installed = states.Count == 8;
            if (!installed)
                return;

            Debug.Log(
                "REBUILD-RENDER-00C4|Installed=True|Companies=" + states.Count +
                "|ActualManpower=" + total +
                "|VisualRatio=1:" + TacticalRebuildSettings00C4.SoldierVisualDenominator +
                "|RatioAffectsSimulation=False|AnimatedFormation=True|HumanProportions=V4|" +
                "TaperedTorso=True|Hands=True|Boots=True|Equipment=True|" +
                "MovementWrites=False|CombatWrites=False|LOD=Full280_Medium560_Far950");
        }

        private CompanyRenderState BuildState(TacticalCompanyEntity00B company)
        {
            int strength = Mathf.Max(0, company.PresentStrength);
            CompanyRenderState state = new CompanyRenderState
            {
                Company = company,
                LineSlots = BuildLineSlots(strength),
                ColumnSlots = BuildColumnSlots(strength),
                LeftLeg = new Matrix4x4[strength],
                RightLeg = new Matrix4x4[strength],
                LeftBoot = new Matrix4x4[strength],
                RightBoot = new Matrix4x4[strength],
                Torso = new Matrix4x4[strength],
                CoatSkirt = new Matrix4x4[strength],
                Head = new Matrix4x4[strength],
                Headgear = new Matrix4x4[strength],
                LeftArm = new Matrix4x4[strength],
                RightArm = new Matrix4x4[strength],
                LeftHand = new Matrix4x4[strength],
                RightHand = new Matrix4x4[strength],
                CrossBelt = new Matrix4x4[strength],
                WaistBelt = new Matrix4x4[strength],
                Pack = new Matrix4x4[strength],
                CartridgeBox = new Matrix4x4[strength],
                RifleStock = new Matrix4x4[strength],
                RifleBarrel = new Matrix4x4[strength],
                Bayonet = new Matrix4x4[strength]
            };

            Transform footprint = company.transform.Find("QA_Footprint_" + company.UnitId);
            if (footprint != null)
            {
                state.QaFootprintRenderer = footprint.GetComponent<Renderer>();
                if (state.QaFootprintRenderer != null)
                    state.QaFootprintRenderer.enabled = false;
            }

            return state;
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

        private void RenderCompanies()
        {
            visibleRepresentatives = 0;
            actualManpower = 0;
            fullDetailCompanies = 0;
            mediumDetailCompanies = 0;
            farDetailCompanies = 0;
            drawCalls = 0;

            Vector3 cameraPosition = mainCamera.transform.position;

            for (int s = 0; s < states.Count; s++)
            {
                CompanyRenderState state = states[s];
                TacticalCompanyEntity00B company = state.Company;
                if (company == null || !company.gameObject.activeInHierarchy)
                    continue;

                int actual = Mathf.Min(company.PresentStrength, state.LineSlots.Length);
                actualManpower += actual;
                if (actual <= 0)
                    continue;

                float distance = Vector3.Distance(cameraPosition, company.transform.position);
                if (distance > MaximumRenderDistance)
                    continue;

                DetailLevel detail;
                if (distance <= FullDetailDistance)
                {
                    detail = DetailLevel.Full;
                    fullDetailCompanies++;
                }
                else if (distance <= MediumDetailDistance)
                {
                    detail = DetailLevel.Medium;
                    mediumDetailCompanies++;
                }
                else
                {
                    detail = DetailLevel.Far;
                    farDetailCompanies++;
                }

                int visible = TacticalRebuildSettings00C4.GetVisibleRepresentativeCount(actual);
                FillMatrices(state, detail, actual, visible);
                DrawCompany(state, detail, visible);
                visibleRepresentatives += visible;
            }
        }

        private void FillMatrices(CompanyRenderState state, DetailLevel detail, int actual, int visible)
        {
            Transform root = state.Company.transform;
            bool danish = state.Company.Nation == RebuildNation.Denmark;

            for (int visualIndex = 0; visualIndex < visible; visualIndex++)
            {
                int sourceIndex = SampleSourceIndex(visualIndex, visible, actual);
                Vector3 slot = GetAnimatedSlot(state, sourceIndex);
                float h = 1f + HashSigned(sourceIndex * 13 + 5) * 0.016f;
                float stance = HashSigned(sourceIndex * 43 + 9) * 0.9f;

                state.Torso[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.03f, 0f),
                    Quaternion.Euler(stance, 0f, 0f), new Vector3(1f, 1f, 1f));
                state.CoatSkirt[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 0.69f, -0.018f),
                    Quaternion.identity, new Vector3(1f, 1f, 1f));

                float hatHeight = danish ? 0.12f : 0.16f;
                state.Headgear[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.76f * h, 0f),
                    Quaternion.identity, new Vector3(0.195f, hatHeight, 0.195f));

                if (detail == DetailLevel.Far)
                    continue;

                state.Head[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.53f * h, 0.018f),
                    Quaternion.identity, new Vector3(0.20f, 0.225f, 0.19f));

                // Musket carried diagonally across the body: stock low/rear, barrel forward/high.
                state.RifleStock[visualIndex] = PartMatrix(root, slot + new Vector3(0.18f, 0.88f, 0.02f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.095f, 0.10f, 0.42f));
                state.RifleBarrel[visualIndex] = PartMatrix(root, slot + new Vector3(0.14f, 1.15f, 0.63f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.030f, 0.030f, 1.02f));

                if (detail == DetailLevel.Medium)
                    continue;

                state.LeftLeg[visualIndex] = PartMatrix(root, slot + new Vector3(-0.082f, 0.38f, 0f),
                    Quaternion.Euler(0f, 0f, -2.0f), new Vector3(0.09f, 0.29f * h, 0.09f));
                state.RightLeg[visualIndex] = PartMatrix(root, slot + new Vector3(0.082f, 0.38f, 0f),
                    Quaternion.Euler(0f, 0f, 2.0f), new Vector3(0.09f, 0.29f * h, 0.09f));
                state.LeftBoot[visualIndex] = PartMatrix(root, slot + new Vector3(-0.082f, 0.105f, 0.045f),
                    Quaternion.identity, new Vector3(0.10f, 0.105f, 0.175f));
                state.RightBoot[visualIndex] = PartMatrix(root, slot + new Vector3(0.082f, 0.105f, 0.045f),
                    Quaternion.identity, new Vector3(0.10f, 0.105f, 0.175f));

                state.LeftArm[visualIndex] = PartMatrix(root, slot + new Vector3(-0.155f, 1.10f, 0.08f),
                    Quaternion.Euler(58f, 0f, -13f), new Vector3(0.07f, 0.245f, 0.07f));
                state.RightArm[visualIndex] = PartMatrix(root, slot + new Vector3(0.155f, 1.08f, 0.105f),
                    Quaternion.Euler(54f, 0f, 13f), new Vector3(0.07f, 0.245f, 0.07f));
                state.LeftHand[visualIndex] = PartMatrix(root, slot + new Vector3(-0.060f, 0.95f, 0.22f),
                    Quaternion.identity, new Vector3(0.062f, 0.062f, 0.062f));
                state.RightHand[visualIndex] = PartMatrix(root, slot + new Vector3(0.115f, 0.97f, 0.24f),
                    Quaternion.identity, new Vector3(0.062f, 0.062f, 0.062f));

                state.CrossBelt[visualIndex] = PartMatrix(root, slot + new Vector3(-0.012f, 1.06f, -0.108f),
                    Quaternion.Euler(0f, 0f, 29f), new Vector3(0.038f, 0.47f, 0.023f));
                state.WaistBelt[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 0.82f, -0.108f),
                    Quaternion.identity, new Vector3(0.31f, 0.035f, 0.025f));
                state.Pack[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.01f, -0.17f),
                    Quaternion.identity, new Vector3(0.265f, 0.31f, 0.125f));
                state.CartridgeBox[visualIndex] = PartMatrix(root, slot + new Vector3(0.20f, 0.80f, -0.105f),
                    Quaternion.identity, new Vector3(0.14f, 0.12f, 0.065f));
                state.Bayonet[visualIndex] = PartMatrix(root, slot + new Vector3(0.05f, 1.45f, 1.30f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.013f, 0.013f, 0.30f));
            }
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

        private static Vector3 GetAnimatedSlot(CompanyRenderState state, int sourceIndex)
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

        private void DrawCompany(CompanyRenderState state, DetailLevel detail, int count)
        {
            if (count <= 0)
                return;

            bool danish = state.Company.Nation == RebuildNation.Denmark;
            Material coat = danish ? dkCoat : prCoat;
            Material trousers = danish ? dkTrousers : prTrousers;
            Material headgear = danish ? dkHeadgear : prHeadgear;

            DrawInstanced(taperedTorsoMesh, coat, state.Torso, count);
            DrawInstanced(taperedSkirtMesh, coat, state.CoatSkirt, count);
            DrawInstanced(cylinderMesh, headgear, state.Headgear, count);

            if (detail == DetailLevel.Far)
                return;

            DrawInstanced(sphereMesh, skin, state.Head, count);
            DrawInstanced(cubeMesh, rifleWood, state.RifleStock, count);
            DrawInstanced(cubeMesh, metal, state.RifleBarrel, count);

            if (detail == DetailLevel.Medium)
                return;

            DrawInstanced(capsuleMesh, trousers, state.LeftLeg, count);
            DrawInstanced(capsuleMesh, trousers, state.RightLeg, count);
            DrawInstanced(cubeMesh, boots, state.LeftBoot, count);
            DrawInstanced(cubeMesh, boots, state.RightBoot, count);
            DrawInstanced(capsuleMesh, coat, state.LeftArm, count);
            DrawInstanced(capsuleMesh, coat, state.RightArm, count);
            DrawInstanced(sphereMesh, skin, state.LeftHand, count);
            DrawInstanced(sphereMesh, skin, state.RightHand, count);
            DrawInstanced(cubeMesh, strap, state.CrossBelt, count);
            DrawInstanced(cubeMesh, leather, state.WaistBelt, count);
            DrawInstanced(cubeMesh, leather, state.Pack, count);
            DrawInstanced(cubeMesh, leather, state.CartridgeBox, count);
            DrawInstanced(cubeMesh, metal, state.Bayonet, count);
        }

        private void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (mesh == null || material == null || matrices == null || count <= 0)
                return;

            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count);
            drawCalls++;
        }

        private static Matrix4x4 PartMatrix(Transform companyTransform, Vector3 localPosition,
            Quaternion localRotation, Vector3 scale)
        {
            return Matrix4x4.TRS(
                companyTransform.TransformPoint(localPosition),
                companyTransform.rotation * localRotation,
                scale);
        }

        private static float HashSigned(int value)
        {
            uint x = unchecked((uint)value);
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0x00FFFFFF) / 8388607.5f - 1f;
        }

        private void CreateMeshes()
        {
            cubeMesh = CapturePrimitiveMesh(PrimitiveType.Cube);
            sphereMesh = CapturePrimitiveMesh(PrimitiveType.Sphere);
            cylinderMesh = CapturePrimitiveMesh(PrimitiveType.Cylinder);
            capsuleMesh = CapturePrimitiveMesh(PrimitiveType.Capsule);
            taperedTorsoMesh = CreateTaperedBoxMesh(0.41f, 0.30f, 0.23f, 0.58f, "00C4_TaperedTorso");
            taperedSkirtMesh = CreateTaperedBoxMesh(0.31f, 0.38f, 0.25f, 0.27f, "00C4_CoatSkirt");
        }

        private static Mesh CapturePrimitiveMesh(PrimitiveType type)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
            temporary.name = "TEMP_REBUILD_00C4_MESH_SOURCE";
            temporary.SetActive(false);
            MeshFilter filter = temporary.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            UnityEngine.Object.Destroy(temporary);
            return mesh;
        }

        private static Mesh CreateTaperedBoxMesh(float topWidth, float bottomWidth, float depth, float height, string name)
        {
            float tw = topWidth * 0.5f;
            float bw = bottomWidth * 0.5f;
            float d = depth * 0.5f;
            float h = height * 0.5f;

            Vector3[] v =
            {
                new Vector3(-bw,-h,-d), new Vector3(bw,-h,-d), new Vector3(bw,-h,d), new Vector3(-bw,-h,d),
                new Vector3(-tw,h,-d), new Vector3(tw,h,-d), new Vector3(tw,h,d), new Vector3(-tw,h,d)
            };
            int[] t =
            {
                0,5,4, 0,1,5, 1,6,5, 1,2,6, 2,7,6, 2,3,7, 3,4,7, 3,0,4,
                4,5,6, 4,6,7, 0,3,2, 0,2,1
            };

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateMaterials()
        {
            dkCoat = CreateMaterial(new Color(0.43f, 0.045f, 0.045f), "00C4_DK_Coat");
            dkTrousers = CreateMaterial(new Color(0.085f, 0.105f, 0.145f), "00C4_DK_Trousers");
            dkHeadgear = CreateMaterial(new Color(0.025f, 0.027f, 0.032f), "00C4_DK_Headgear");

            prCoat = CreateMaterial(new Color(0.032f, 0.070f, 0.125f), "00C4_PR_Coat");
            prTrousers = CreateMaterial(new Color(0.26f, 0.27f, 0.28f), "00C4_PR_Trousers");
            prHeadgear = CreateMaterial(new Color(0.018f, 0.018f, 0.020f), "00C4_PR_Headgear");

            skin = CreateMaterial(new Color(0.66f, 0.48f, 0.35f), "00C4_Skin");
            leather = CreateMaterial(new Color(0.105f, 0.060f, 0.030f), "00C4_Leather");
            strap = CreateMaterial(new Color(0.82f, 0.80f, 0.70f), "00C4_Straps");
            rifleWood = CreateMaterial(new Color(0.115f, 0.050f, 0.018f), "00C4_RifleWood");
            metal = CreateMaterial(new Color(0.25f, 0.27f, 0.29f), "00C4_Metal");
            boots = CreateMaterial(new Color(0.025f, 0.023f, 0.020f), "00C4_Boots");
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

        private void OnGUI()
        {
            if (!installed || !TacticalRebuildSettings00C4.DebugViewEnabled)
                return;

            float fps = smoothedDelta > 0f ? 1f / smoothedDelta : 0f;
            float ms = smoothedDelta > 0f ? smoothedDelta * 1000f : 0f;

            GUI.depth = -950;
            Rect box = new Rect(Mathf.Max(8f, Screen.width - 450f), 8f, 440f, 82f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "C4 RENDER | Ratio 1:" + TacticalRebuildSettings00C4.SoldierVisualDenominator +
                " | Actual " + actualManpower + " | Visible " + visibleRepresentatives);
            GUI.Label(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, 20f),
                "FPS " + fps.ToString("0") + " | " + ms.ToString("0.0") + " ms | Draw calls " + drawCalls);
            GUI.Label(new Rect(box.x + 10f, box.y + 49f, box.width - 20f, 20f),
                "LOD Company: Full " + fullDetailCompanies + " | Medium " + mediumDetailCompanies + " | Far " + farDetailCompanies);
        }
    }
}
