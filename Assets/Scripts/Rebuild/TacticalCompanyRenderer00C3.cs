using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c3 - sharper presentation-only 1:1 soldier renderer.
    // No tactical translation, navigation, combat, AI or OOB writes.
    [DefaultExecutionOrder(520)]
    public sealed class TacticalCompanyRenderer00C3 : MonoBehaviour
    {
        private enum DetailLevel { Full, Medium, Far }

        private sealed class CompanyRenderState
        {
            public TacticalCompanyEntity00B Company;
            public Vector3[] Slots;
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
            public Matrix4x4[] Pack;
            public Matrix4x4[] RifleStock;
            public Matrix4x4[] RifleBarrel;
            public Matrix4x4[] Bayonet;
            public RebuildFormation CachedFormation;
            public int CachedStrength;
        }

        private const float LineFileSpacing = 0.52f;
        private const float LineRankSpacing = 0.76f;
        private const int LineRanks = 3;
        private const int ColumnFiles = 8;
        private const float ColumnFileSpacing = 0.60f;
        private const float ColumnRowSpacing = 0.72f;
        private const float FullDetailDistance = 260f;
        private const float MediumDetailDistance = 520f;
        private const float MaximumRenderDistance = 900f;

        private readonly List<CompanyRenderState> states = new List<CompanyRenderState>();

        private Mesh cubeMesh;
        private Mesh sphereMesh;
        private Mesh cylinderMesh;
        private Mesh capsuleMesh;

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
        private int visibleSoldiers;
        private int fullDetailCompanies;
        private int mediumDetailCompanies;
        private int farDetailCompanies;
        private int drawCalls;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            // c3 supersedes the c2 renderer cleanly by disabling only presentation ownership.
            TacticalCompanyRenderer00C old = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C>();
            if (old != null)
                old.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C3>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C3_SOLDIER_RENDERER");
            root.AddComponent<TacticalCompanyRenderer00C3>();
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

            CreatePrimitiveMeshes();
            CreateMaterials();
            states.Clear();

            int total = 0;
            for (int i = 0; i < companies.Length; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                CompanyRenderState state = BuildState(company);
                states.Add(state);
                total += company.PresentStrength;
            }

            mainCamera = Camera.main;
            installed = states.Count == 8;
            if (!installed)
                return;

            Debug.Log(
                "REBUILD-RENDER-00C3|Installed=True|Owner=PresentationOnly|Companies=" + states.Count +
                "|VisibleManpower=" + total +
                "|VisualRatio=1:1|HumanProportions=V3|Boots=True|Hands=True|" +
                "Rifle=Stock+Barrel+Bayonet|FormationAware=True|MovementWrites=False|CombatWrites=False|" +
                "LOD=Full260_Medium520_Far900|WorldLabels=False|Flags=False");
        }

        private CompanyRenderState BuildState(TacticalCompanyEntity00B company)
        {
            int strength = Mathf.Max(0, company.PresentStrength);
            CompanyRenderState state = new CompanyRenderState
            {
                Company = company,
                Slots = BuildSlots(company.Formation, strength),
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
                Pack = new Matrix4x4[strength],
                RifleStock = new Matrix4x4[strength],
                RifleBarrel = new Matrix4x4[strength],
                Bayonet = new Matrix4x4[strength],
                CachedFormation = company.Formation,
                CachedStrength = strength
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

        private static Vector3[] BuildSlots(RebuildFormation formation, int strength)
        {
            return formation == RebuildFormation.Column
                ? BuildColumnSlots(strength)
                : BuildLineSlots(strength);
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
                float x = -width * 0.5f + file * LineFileSpacing;
                float z = -rank * LineRankSpacing;
                slots[i] = new Vector3(
                    x + HashSigned(i * 17 + 3) * 0.018f,
                    0f,
                    z + HashSigned(i * 31 + 11) * 0.014f);
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
                float x = -width * 0.5f + file * ColumnFileSpacing;
                float z = depth * 0.5f - row * ColumnRowSpacing;
                slots[i] = new Vector3(
                    x + HashSigned(i * 17 + 3) * 0.018f,
                    0f,
                    z + HashSigned(i * 31 + 11) * 0.014f);
            }

            return slots;
        }

        private void RenderCompanies()
        {
            visibleSoldiers = 0;
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

                RefreshSlotsIfNeeded(state);

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

                FillMatrices(state, detail);
                DrawCompany(state, detail);
                visibleSoldiers += company.PresentStrength;
            }
        }

        private static void RefreshSlotsIfNeeded(CompanyRenderState state)
        {
            int currentStrength = Mathf.Max(0, state.Company.PresentStrength);
            if (state.CachedFormation == state.Company.Formation &&
                state.CachedStrength == currentStrength &&
                state.Slots != null &&
                state.Slots.Length == currentStrength)
                return;

            state.Slots = BuildSlots(state.Company.Formation, currentStrength);
            state.CachedFormation = state.Company.Formation;
            state.CachedStrength = currentStrength;
        }

        private void FillMatrices(CompanyRenderState state, DetailLevel detail)
        {
            Transform root = state.Company.transform;
            int count = Mathf.Min(state.Company.PresentStrength, state.Slots.Length);
            bool danish = state.Company.Nation == RebuildNation.Denmark;

            for (int i = 0; i < count; i++)
            {
                Vector3 slot = state.Slots[i];
                float h = 1f + HashSigned(i * 13 + 5) * 0.018f;
                float stance = HashSigned(i * 43 + 9) * 1.1f;

                state.Torso[i] = PartMatrix(root, slot + new Vector3(0f, 1.03f, 0f),
                    Quaternion.Euler(stance, 0f, 0f), new Vector3(0.30f, 0.54f * h, 0.20f));
                state.CoatSkirt[i] = PartMatrix(root, slot + new Vector3(0f, 0.71f, -0.02f),
                    Quaternion.identity, new Vector3(0.34f, 0.25f, 0.23f));

                float hatHeight = danish ? 0.12f : 0.16f;
                state.Headgear[i] = PartMatrix(root, slot + new Vector3(0f, 1.75f * h, 0f),
                    Quaternion.identity, new Vector3(0.20f, hatHeight, 0.20f));

                if (detail == DetailLevel.Far)
                    continue;

                state.Head[i] = PartMatrix(root, slot + new Vector3(0f, 1.52f * h, 0.02f),
                    Quaternion.identity, new Vector3(0.205f, 0.225f, 0.195f));

                state.RifleStock[i] = PartMatrix(root, slot + new Vector3(0.15f, 0.94f, 0.05f),
                    Quaternion.Euler(12f, 0f, 0f), new Vector3(0.095f, 0.11f, 0.44f));
                state.RifleBarrel[i] = PartMatrix(root, slot + new Vector3(0.15f, 1.10f, 0.66f),
                    Quaternion.Euler(12f, 0f, 0f), new Vector3(0.032f, 0.032f, 0.95f));

                if (detail == DetailLevel.Medium)
                    continue;

                state.LeftLeg[i] = PartMatrix(root, slot + new Vector3(-0.085f, 0.36f, 0f),
                    Quaternion.Euler(0f, 0f, -2.0f), new Vector3(0.095f, 0.30f * h, 0.095f));
                state.RightLeg[i] = PartMatrix(root, slot + new Vector3(0.085f, 0.36f, 0f),
                    Quaternion.Euler(0f, 0f, 2.0f), new Vector3(0.095f, 0.30f * h, 0.095f));
                state.LeftBoot[i] = PartMatrix(root, slot + new Vector3(-0.085f, 0.10f, 0.055f),
                    Quaternion.identity, new Vector3(0.105f, 0.11f, 0.18f));
                state.RightBoot[i] = PartMatrix(root, slot + new Vector3(0.085f, 0.10f, 0.055f),
                    Quaternion.identity, new Vector3(0.105f, 0.11f, 0.18f));

                state.LeftArm[i] = PartMatrix(root, slot + new Vector3(-0.17f, 1.10f, 0.10f),
                    Quaternion.Euler(58f, 0f, -12f), new Vector3(0.075f, 0.25f, 0.075f));
                state.RightArm[i] = PartMatrix(root, slot + new Vector3(0.17f, 1.08f, 0.12f),
                    Quaternion.Euler(54f, 0f, 12f), new Vector3(0.075f, 0.25f, 0.075f));
                state.LeftHand[i] = PartMatrix(root, slot + new Vector3(-0.08f, 0.94f, 0.24f),
                    Quaternion.identity, new Vector3(0.07f, 0.07f, 0.07f));
                state.RightHand[i] = PartMatrix(root, slot + new Vector3(0.13f, 0.95f, 0.25f),
                    Quaternion.identity, new Vector3(0.07f, 0.07f, 0.07f));

                state.CrossBelt[i] = PartMatrix(root, slot + new Vector3(-0.01f, 1.05f, -0.105f),
                    Quaternion.Euler(0f, 0f, 29f), new Vector3(0.043f, 0.48f, 0.025f));
                state.Pack[i] = PartMatrix(root, slot + new Vector3(0f, 1.00f, -0.17f),
                    Quaternion.identity, new Vector3(0.27f, 0.32f, 0.13f));
                state.Bayonet[i] = PartMatrix(root, slot + new Vector3(0.15f, 1.26f, 1.27f),
                    Quaternion.Euler(12f, 0f, 0f), new Vector3(0.015f, 0.015f, 0.30f));
            }
        }

        private void DrawCompany(CompanyRenderState state, DetailLevel detail)
        {
            TacticalCompanyEntity00B company = state.Company;
            int count = Mathf.Min(company.PresentStrength, state.Slots.Length);
            if (count <= 0)
                return;

            bool danish = company.Nation == RebuildNation.Denmark;
            Material coat = danish ? dkCoat : prCoat;
            Material trousers = danish ? dkTrousers : prTrousers;
            Material headgear = danish ? dkHeadgear : prHeadgear;

            DrawInstanced(cubeMesh, coat, state.Torso, count);
            DrawInstanced(cubeMesh, coat, state.CoatSkirt, count);
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
            DrawInstanced(cubeMesh, leather, state.Pack, count);
            DrawInstanced(cubeMesh, metal, state.Bayonet, count);
        }

        private void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
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

        private void CreatePrimitiveMeshes()
        {
            cubeMesh = CapturePrimitiveMesh(PrimitiveType.Cube);
            sphereMesh = CapturePrimitiveMesh(PrimitiveType.Sphere);
            cylinderMesh = CapturePrimitiveMesh(PrimitiveType.Cylinder);
            capsuleMesh = CapturePrimitiveMesh(PrimitiveType.Capsule);
        }

        private static Mesh CapturePrimitiveMesh(PrimitiveType type)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
            temporary.name = "TEMP_REBUILD_00C3_MESH_SOURCE";
            temporary.SetActive(false);
            MeshFilter filter = temporary.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            UnityEngine.Object.Destroy(temporary);
            return mesh;
        }

        private void CreateMaterials()
        {
            dkCoat = CreateMaterial(new Color(0.54f, 0.055f, 0.05f), "00C3_DK_Coat");
            dkTrousers = CreateMaterial(new Color(0.075f, 0.095f, 0.145f), "00C3_DK_Trousers");
            dkHeadgear = CreateMaterial(new Color(0.025f, 0.03f, 0.04f), "00C3_DK_Headgear");

            prCoat = CreateMaterial(new Color(0.035f, 0.075f, 0.15f), "00C3_PR_Coat");
            prTrousers = CreateMaterial(new Color(0.34f, 0.35f, 0.36f), "00C3_PR_Trousers");
            prHeadgear = CreateMaterial(new Color(0.018f, 0.018f, 0.02f), "00C3_PR_Headgear");

            skin = CreateMaterial(new Color(0.73f, 0.56f, 0.44f), "00C3_Skin");
            leather = CreateMaterial(new Color(0.15f, 0.085f, 0.04f), "00C3_Leather");
            strap = CreateMaterial(new Color(0.82f, 0.80f, 0.69f), "00C3_Strap");
            rifleWood = CreateMaterial(new Color(0.22f, 0.095f, 0.03f), "00C3_RifleWood");
            metal = CreateMaterial(new Color(0.20f, 0.22f, 0.24f), "00C3_Metal");
            boots = CreateMaterial(new Color(0.03f, 0.025f, 0.02f), "00C3_Boots");
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
            if (!installed || !TacticalCompanyDrill00C3.DebugViewEnabled)
                return;

            float fps = smoothedDelta > 0f ? 1f / smoothedDelta : 0f;
            float ms = smoothedDelta > 0f ? smoothedDelta * 1000f : 0f;

            GUI.depth = -950;
            GUI.Box(new Rect(12f, 42f, 550f, 86f), string.Empty);
            GUI.Label(new Rect(22f, 49f, 530f, 22f),
                "GATE A+B+C3 | 8 Companies | 1:1 renderer | Stable UnitID | No labels/flags");
            GUI.Label(new Rect(22f, 70f, 530f, 20f),
                "LMB select | Shift add | Ctrl toggle | Drag box | F Line/Column | Z/X facing");
            GUI.Label(new Rect(22f, 90f, 530f, 18f),
                "V Clean/Debug | H Hover | Movement/combat intentionally OFF");
            GUI.Label(new Rect(22f, 108f, 530f, 18f),
                "Visible " + visibleSoldiers + " | FPS " + fps.ToString("0") + " | " + ms.ToString("0.0") + " ms");

            Rect box = new Rect(Mathf.Max(8f, Screen.width - 405f), 8f, 395f, 76f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "C3 RENDER | DrawCalls " + drawCalls + " | Soldiers " + visibleSoldiers);
            GUI.Label(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, 20f),
                "LOD Companies Full " + fullDetailCompanies + " / Med " + mediumDetailCompanies + " / Far " + farDetailCompanies);
            GUI.Label(new Rect(box.x + 10f, box.y + 49f, box.width - 20f, 18f),
                "Clean architecture: renderer writes presentation only");
        }
    }
}
