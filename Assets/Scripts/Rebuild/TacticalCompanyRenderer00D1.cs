using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00d1 - Gate D1 visual renderer.
    // Adds a much more articulated procedural infantry silhouette plus marching gait.
    // Presentation only: tactical world pose is owned by TacticalCompanyEntity00B.
    [DefaultExecutionOrder(550)]
    public sealed class TacticalCompanyRenderer00D1 : MonoBehaviour
    {
        private enum DetailLevel { Full, Medium, Far }

        private sealed class CompanyRenderState
        {
            public TacticalCompanyEntity00B Company;
            public Vector3[] LineSlots;
            public Vector3[] ColumnSlots;
            public Renderer QaFootprintRenderer;

            public Matrix4x4[] LeftThigh;
            public Matrix4x4[] RightThigh;
            public Matrix4x4[] LeftShin;
            public Matrix4x4[] RightShin;
            public Matrix4x4[] LeftBoot;
            public Matrix4x4[] RightBoot;
            public Matrix4x4[] Torso;
            public Matrix4x4[] CoatSkirt;
            public Matrix4x4[] Neck;
            public Matrix4x4[] Head;
            public Matrix4x4[] Nose;
            public Matrix4x4[] HatCrown;
            public Matrix4x4[] HatBrim;
            public Matrix4x4[] HatSpike;
            public Matrix4x4[] LeftUpperArm;
            public Matrix4x4[] RightUpperArm;
            public Matrix4x4[] LeftForearm;
            public Matrix4x4[] RightForearm;
            public Matrix4x4[] LeftHand;
            public Matrix4x4[] RightHand;
            public Matrix4x4[] LeftShoulder;
            public Matrix4x4[] RightShoulder;
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

        private const float FullDetailDistance = 250f;
        private const float MediumDetailDistance = 520f;
        private const float MaximumRenderDistance = 950f;

        private readonly List<CompanyRenderState> states = new List<CompanyRenderState>();

        private Mesh cubeMesh;
        private Mesh sphereMesh;
        private Mesh cylinderMesh;
        private Mesh capsuleMesh;
        private Mesh torsoMesh;
        private Mesh skirtMesh;
        private Mesh coneMesh;

        private Material dkCoat;
        private Material dkTrousers;
        private Material dkHeadgear;
        private Material dkTrim;
        private Material prCoat;
        private Material prTrousers;
        private Material prHeadgear;
        private Material prTrim;
        private Material skin;
        private Material leather;
        private Material strap;
        private Material rifleWood;
        private Material metal;
        private Material boots;
        private Material brass;

        private Camera cam;
        private bool installed;
        private float smoothedDelta;
        private int visibleRepresentatives;
        private int actualManpower;
        private int movingCompanies;
        private int drawCalls;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyRenderer00C4 c4 = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C4>();
            if (c4 != null)
                c4.enabled = false;

            TacticalSoldierDetailOverlay00C5 c5 = UnityEngine.Object.FindAnyObjectByType<TacticalSoldierDetailOverlay00C5>();
            if (c5 != null)
                c5.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00D1>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00D1_SOLDIER_RENDERER");
            root.AddComponent<TacticalCompanyRenderer00D1>();
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

            for (int i = 0; i < companies.Length; i++)
            {
                if (companies[i] != null)
                    states.Add(BuildState(companies[i]));
            }

            cam = Camera.main;
            installed = states.Count == 8;
            if (!installed)
                return;

            Debug.Log(
                "REBUILD-RENDER-00D1|Installed=True|Companies=8|HumanProportions=V5|" +
                "ArticulatedLimbs=True|Neck=True|Nose=True|DistinctHeadgear=True|" +
                "MarchGait=True|VisualRatio=1:N|MovementWrites=False|CombatWrites=False");
        }

        private CompanyRenderState BuildState(TacticalCompanyEntity00B company)
        {
            int n = Mathf.Max(0, company.PresentStrength);
            CompanyRenderState s = new CompanyRenderState
            {
                Company = company,
                LineSlots = BuildLineSlots(n),
                ColumnSlots = BuildColumnSlots(n),
                LeftThigh = new Matrix4x4[n], RightThigh = new Matrix4x4[n],
                LeftShin = new Matrix4x4[n], RightShin = new Matrix4x4[n],
                LeftBoot = new Matrix4x4[n], RightBoot = new Matrix4x4[n],
                Torso = new Matrix4x4[n], CoatSkirt = new Matrix4x4[n],
                Neck = new Matrix4x4[n], Head = new Matrix4x4[n], Nose = new Matrix4x4[n],
                HatCrown = new Matrix4x4[n], HatBrim = new Matrix4x4[n], HatSpike = new Matrix4x4[n],
                LeftUpperArm = new Matrix4x4[n], RightUpperArm = new Matrix4x4[n],
                LeftForearm = new Matrix4x4[n], RightForearm = new Matrix4x4[n],
                LeftHand = new Matrix4x4[n], RightHand = new Matrix4x4[n],
                LeftShoulder = new Matrix4x4[n], RightShoulder = new Matrix4x4[n],
                CrossBelt = new Matrix4x4[n], WaistBelt = new Matrix4x4[n],
                Pack = new Matrix4x4[n], CartridgeBox = new Matrix4x4[n],
                RifleStock = new Matrix4x4[n], RifleBarrel = new Matrix4x4[n], Bayonet = new Matrix4x4[n]
            };

            Transform footprint = company.transform.Find("QA_Footprint_" + company.UnitId);
            if (footprint != null)
            {
                s.QaFootprintRenderer = footprint.GetComponent<Renderer>();
                if (s.QaFootprintRenderer != null)
                    s.QaFootprintRenderer.enabled = false;
            }
            return s;
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
                    -width * 0.5f + file * LineFileSpacing + HashSigned(i * 17 + 3) * 0.010f,
                    0f,
                    -rank * LineRankSpacing + HashSigned(i * 31 + 11) * 0.009f);
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
                    -width * 0.5f + file * ColumnFileSpacing + HashSigned(i * 17 + 3) * 0.010f,
                    0f,
                    depth * 0.5f - row * ColumnRowSpacing + HashSigned(i * 31 + 11) * 0.009f);
            }
            return slots;
        }

        private void RenderCompanies()
        {
            visibleRepresentatives = 0;
            actualManpower = 0;
            movingCompanies = 0;
            drawCalls = 0;

            Vector3 cameraPosition = cam.transform.position;
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

                if (company.IsMovingToOrder)
                    movingCompanies++;

                float distance = Vector3.Distance(cameraPosition, company.transform.position);
                if (distance > MaximumRenderDistance)
                    continue;

                DetailLevel detail = distance <= FullDetailDistance
                    ? DetailLevel.Full
                    : distance <= MediumDetailDistance ? DetailLevel.Medium : DetailLevel.Far;

                int visible = TacticalRebuildSettings00C4.GetVisibleRepresentativeCount(actual);
                FillMatrices(state, detail, actual, visible);
                DrawCompany(state, detail, visible);
                visibleRepresentatives += visible;
            }
        }

        private void FillMatrices(CompanyRenderState state, DetailLevel detail, int actual, int visible)
        {
            TacticalCompanyEntity00B company = state.Company;
            Transform root = company.transform;
            bool danish = company.Nation == RebuildNation.Denmark;
            bool walking = company.IsVisualWalking;

            for (int visualIndex = 0; visualIndex < visible; visualIndex++)
            {
                int sourceIndex = SampleSourceIndex(visualIndex, visible, actual);
                Vector3 slot = GetAnimatedSlot(state, sourceIndex);

                float h = 1f + HashSigned(sourceIndex * 13 + 5) * 0.016f;
                float phase = Time.unscaledTime * 7.2f + sourceIndex * 0.61f;
                float step = walking ? Mathf.Sin(phase) : 0f;
                float bob = walking ? Mathf.Abs(Mathf.Sin(phase)) * 0.018f : 0f;
                float lean = walking ? 3.0f : HashSigned(sourceIndex * 43 + 9) * 0.8f;

                state.Torso[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.04f + bob, 0f),
                    Quaternion.Euler(lean, 0f, 0f), Vector3.one);
                state.CoatSkirt[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 0.71f + bob, -0.018f),
                    Quaternion.Euler(lean * 0.5f, 0f, 0f), Vector3.one);

                state.Neck[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.39f + bob, 0.005f),
                    Quaternion.identity, new Vector3(0.095f, 0.09f, 0.09f));
                state.Head[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.54f * h + bob, 0.018f),
                    Quaternion.identity, new Vector3(0.195f, 0.215f, 0.185f));
                state.Nose[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.54f * h + bob, 0.118f),
                    Quaternion.identity, new Vector3(0.045f, 0.055f, 0.055f));

                float crownHeight = danish ? 0.13f : 0.16f;
                state.HatCrown[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.76f * h + bob, 0f),
                    Quaternion.identity, new Vector3(danish ? 0.19f : 0.205f, crownHeight, danish ? 0.19f : 0.205f));
                state.HatBrim[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.68f * h + bob, 0.085f),
                    Quaternion.identity, new Vector3(0.23f, 0.025f, 0.12f));
                state.HatSpike[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.94f * h + bob, 0f),
                    Quaternion.identity, danish ? new Vector3(0.001f, 0.001f, 0.001f) : new Vector3(0.055f, 0.16f, 0.055f));

                if (detail == DetailLevel.Far)
                    continue;

                float legSwing = step * 24f;
                float armSwing = -step * 16f;

                state.LeftThigh[visualIndex] = PartMatrix(root, slot + new Vector3(-0.085f, 0.55f, 0f),
                    Quaternion.Euler(legSwing, 0f, -2f), new Vector3(0.09f, 0.21f * h, 0.09f));
                state.RightThigh[visualIndex] = PartMatrix(root, slot + new Vector3(0.085f, 0.55f, 0f),
                    Quaternion.Euler(-legSwing, 0f, 2f), new Vector3(0.09f, 0.21f * h, 0.09f));
                state.LeftShin[visualIndex] = PartMatrix(root, slot + new Vector3(-0.085f, 0.27f, walking ? step * 0.035f : 0f),
                    Quaternion.Euler(legSwing * 0.55f, 0f, -1f), new Vector3(0.082f, 0.19f * h, 0.082f));
                state.RightShin[visualIndex] = PartMatrix(root, slot + new Vector3(0.085f, 0.27f, walking ? -step * 0.035f : 0f),
                    Quaternion.Euler(-legSwing * 0.55f, 0f, 1f), new Vector3(0.082f, 0.19f * h, 0.082f));
                state.LeftBoot[visualIndex] = PartMatrix(root, slot + new Vector3(-0.085f, 0.095f, 0.055f + step * 0.035f),
                    Quaternion.Euler(legSwing * 0.35f, 0f, 0f), new Vector3(0.10f, 0.10f, 0.18f));
                state.RightBoot[visualIndex] = PartMatrix(root, slot + new Vector3(0.085f, 0.095f, 0.055f - step * 0.035f),
                    Quaternion.Euler(-legSwing * 0.35f, 0f, 0f), new Vector3(0.10f, 0.10f, 0.18f));

                state.LeftShoulder[visualIndex] = PartMatrix(root, slot + new Vector3(-0.205f, 1.27f + bob, 0f),
                    Quaternion.identity, new Vector3(0.10f, 0.055f, 0.12f));
                state.RightShoulder[visualIndex] = PartMatrix(root, slot + new Vector3(0.205f, 1.27f + bob, 0f),
                    Quaternion.identity, new Vector3(0.10f, 0.055f, 0.12f));

                state.LeftUpperArm[visualIndex] = PartMatrix(root, slot + new Vector3(-0.20f, 1.12f + bob, 0.04f),
                    Quaternion.Euler(armSwing + 8f, 0f, -8f), new Vector3(0.072f, 0.16f, 0.072f));
                state.RightUpperArm[visualIndex] = PartMatrix(root, slot + new Vector3(0.20f, 1.12f + bob, 0.04f),
                    Quaternion.Euler(-armSwing + 8f, 0f, 8f), new Vector3(0.072f, 0.16f, 0.072f));
                state.LeftForearm[visualIndex] = PartMatrix(root, slot + new Vector3(-0.16f, 0.94f + bob, 0.16f),
                    Quaternion.Euler(42f + armSwing * 0.35f, 0f, -8f), new Vector3(0.064f, 0.14f, 0.064f));
                state.RightForearm[visualIndex] = PartMatrix(root, slot + new Vector3(0.15f, 0.95f + bob, 0.17f),
                    Quaternion.Euler(40f - armSwing * 0.35f, 0f, 8f), new Vector3(0.064f, 0.14f, 0.064f));
                state.LeftHand[visualIndex] = PartMatrix(root, slot + new Vector3(-0.10f, 0.88f + bob, 0.25f),
                    Quaternion.identity, new Vector3(0.058f, 0.058f, 0.058f));
                state.RightHand[visualIndex] = PartMatrix(root, slot + new Vector3(0.12f, 0.90f + bob, 0.26f),
                    Quaternion.identity, new Vector3(0.058f, 0.058f, 0.058f));

                state.RifleStock[visualIndex] = PartMatrix(root, slot + new Vector3(0.17f, 0.87f + bob, 0.02f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.095f, 0.10f, 0.42f));
                state.RifleBarrel[visualIndex] = PartMatrix(root, slot + new Vector3(0.13f, 1.15f + bob, 0.63f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.028f, 0.48f, 0.028f));
                state.Bayonet[visualIndex] = PartMatrix(root, slot + new Vector3(0.045f, 1.44f + bob, 1.28f),
                    Quaternion.Euler(24f, 0f, -5f), new Vector3(0.012f, 0.15f, 0.012f));

                if (detail == DetailLevel.Medium)
                    continue;

                state.CrossBelt[visualIndex] = PartMatrix(root, slot + new Vector3(-0.012f, 1.07f + bob, -0.108f),
                    Quaternion.Euler(0f, 0f, 29f), new Vector3(0.038f, 0.47f, 0.023f));
                state.WaistBelt[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 0.83f + bob, -0.108f),
                    Quaternion.identity, new Vector3(0.31f, 0.035f, 0.025f));
                state.Pack[visualIndex] = PartMatrix(root, slot + new Vector3(0f, 1.01f + bob, -0.17f),
                    Quaternion.identity, new Vector3(0.265f, 0.31f, 0.125f));
                state.CartridgeBox[visualIndex] = PartMatrix(root, slot + new Vector3(0.20f, 0.80f + bob, -0.105f),
                    Quaternion.identity, new Vector3(0.14f, 0.12f, 0.065f));
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

            if (company.IsMovingToOrder && company.OrderFromFormation != company.OrderTargetFormation)
            {
                Vector3 from = company.OrderFromFormation == RebuildFormation.Column ? column : line;
                Vector3 to = company.OrderTargetFormation == RebuildFormation.Column ? column : line;
                return Vector3.Lerp(from, to, company.OrderProgress);
            }

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
            Material trim = danish ? dkTrim : prTrim;

            Draw(torsoMesh, coat, state.Torso, count);
            Draw(skirtMesh, coat, state.CoatSkirt, count);
            Draw(capsuleMesh, skin, state.Neck, count);
            Draw(sphereMesh, skin, state.Head, count);
            Draw(cubeMesh, skin, state.Nose, count);
            Draw(cylinderMesh, headgear, state.HatCrown, count);
            Draw(cubeMesh, headgear, state.HatBrim, count);
            if (!danish)
                Draw(coneMesh, darkOr(headgear), state.HatSpike, count);

            if (detail == DetailLevel.Far)
                return;

            Draw(capsuleMesh, trousers, state.LeftThigh, count);
            Draw(capsuleMesh, trousers, state.RightThigh, count);
            Draw(capsuleMesh, trousers, state.LeftShin, count);
            Draw(capsuleMesh, trousers, state.RightShin, count);
            Draw(cubeMesh, boots, state.LeftBoot, count);
            Draw(cubeMesh, boots, state.RightBoot, count);
            Draw(cubeMesh, trim, state.LeftShoulder, count);
            Draw(cubeMesh, trim, state.RightShoulder, count);
            Draw(capsuleMesh, coat, state.LeftUpperArm, count);
            Draw(capsuleMesh, coat, state.RightUpperArm, count);
            Draw(capsuleMesh, coat, state.LeftForearm, count);
            Draw(capsuleMesh, coat, state.RightForearm, count);
            Draw(sphereMesh, skin, state.LeftHand, count);
            Draw(sphereMesh, skin, state.RightHand, count);
            Draw(cubeMesh, rifleWood, state.RifleStock, count);
            Draw(cylinderMesh, metal, state.RifleBarrel, count);
            Draw(cylinderMesh, metal, state.Bayonet, count);

            if (detail == DetailLevel.Medium)
                return;

            Draw(cubeMesh, strap, state.CrossBelt, count);
            Draw(cubeMesh, leather, state.WaistBelt, count);
            Draw(cubeMesh, leather, state.Pack, count);
            Draw(cubeMesh, leather, state.CartridgeBox, count);
        }

        private Material darkOr(Material fallback)
        {
            return fallback != null ? fallback : metal;
        }

        private void Draw(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (mesh == null || material == null || matrices == null || count <= 0)
                return;
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count);
            drawCalls++;
        }

        private static Matrix4x4 PartMatrix(Transform root, Vector3 localPosition, Quaternion localRotation, Vector3 scale)
        {
            return Matrix4x4.TRS(root.TransformPoint(localPosition), root.rotation * localRotation, scale);
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
            torsoMesh = CreateTaperedBoxMesh(0.43f, 0.28f, 0.22f, 0.59f, "00D1_Torso");
            skirtMesh = CreateTaperedBoxMesh(0.30f, 0.39f, 0.24f, 0.27f, "00D1_Skirt");
            coneMesh = CreateConeMesh(10, 0.055f, 0.18f, "00D1_HelmetSpike");
        }

        private static Mesh CapturePrimitiveMesh(PrimitiveType type)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
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
            Mesh mesh = new Mesh { name = name, vertices = v, triangles = t };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateConeMesh(int sides, float radius, float height, string name)
        {
            Vector3[] vertices = new Vector3[sides + 2];
            vertices[0] = new Vector3(0f, height, 0f);
            vertices[1] = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                vertices[i + 2] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }

            int[] triangles = new int[sides * 6];
            int k = 0;
            for (int i = 0; i < sides; i++)
            {
                int a = i + 2;
                int b = ((i + 1) % sides) + 2;
                triangles[k++] = 0; triangles[k++] = b; triangles[k++] = a;
                triangles[k++] = 1; triangles[k++] = a; triangles[k++] = b;
            }

            Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateMaterials()
        {
            dkCoat = CreateMaterial(new Color(0.43f, 0.045f, 0.045f), "00D1_DK_Coat");
            dkTrousers = CreateMaterial(new Color(0.085f, 0.105f, 0.145f), "00D1_DK_Trousers");
            dkHeadgear = CreateMaterial(new Color(0.025f, 0.027f, 0.032f), "00D1_DK_Headgear");
            dkTrim = CreateMaterial(new Color(0.72f, 0.68f, 0.55f), "00D1_DK_Trim");

            prCoat = CreateMaterial(new Color(0.032f, 0.070f, 0.125f), "00D1_PR_Coat");
            prTrousers = CreateMaterial(new Color(0.26f, 0.27f, 0.28f), "00D1_PR_Trousers");
            prHeadgear = CreateMaterial(new Color(0.018f, 0.018f, 0.020f), "00D1_PR_Headgear");
            prTrim = CreateMaterial(new Color(0.68f, 0.58f, 0.20f), "00D1_PR_Trim");

            skin = CreateMaterial(new Color(0.66f, 0.48f, 0.35f), "00D1_Skin");
            leather = CreateMaterial(new Color(0.105f, 0.060f, 0.030f), "00D1_Leather");
            strap = CreateMaterial(new Color(0.82f, 0.80f, 0.70f), "00D1_Straps");
            rifleWood = CreateMaterial(new Color(0.115f, 0.050f, 0.018f), "00D1_RifleWood");
            metal = CreateMaterial(new Color(0.25f, 0.27f, 0.29f), "00D1_Metal");
            boots = CreateMaterial(new Color(0.025f, 0.023f, 0.020f), "00D1_Boots");
            brass = CreateMaterial(new Color(0.55f, 0.43f, 0.15f), "00D1_Brass");
        }

        private static Material CreateMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            material.enableInstancing = true;
            return material;
        }

        private void OnGUI()
        {
            if (!installed || !TacticalRebuildSettings00C4.DebugViewEnabled)
                return;

            float fps = smoothedDelta > 0f ? 1f / smoothedDelta : 0f;
            float ms = smoothedDelta * 1000f;
            GUI.depth = -951;
            Rect box = new Rect(Mathf.Max(8f, Screen.width - 465f), 8f, 455f, 82f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "D1 RENDER V5 | Ratio 1:" + TacticalRebuildSettings00C4.SoldierVisualDenominator +
                " | Actual " + actualManpower + " | Visible " + visibleRepresentatives);
            GUI.Label(new Rect(box.x + 10f, box.y + 29f, box.width - 20f, 20f),
                "Moving Companies " + movingCompanies + " | FPS " + fps.ToString("0") + " | " + ms.ToString("0.0") + " ms");
            GUI.Label(new Rect(box.x + 10f, box.y + 51f, box.width - 20f, 20f),
                "Articulated gait + neck/face/headgear + upper/lower limbs + equipment | Draw " + drawCalls);
        }
    }
}
