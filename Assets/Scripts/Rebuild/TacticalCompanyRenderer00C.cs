using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c2 - Soldier Visual Upgrade.
    // Presentation-only 1:1 renderer. It never writes tactical position, facing,
    // selection, movement, combat, casualties or OOB state.
    [DefaultExecutionOrder(500)]
    public sealed class TacticalCompanyRenderer00C : MonoBehaviour
    {
        private enum DetailLevel
        {
            Full,
            Medium,
            Far
        }

        private sealed class CompanyRenderState
        {
            public TacticalCompanyEntity00B Company;
            public Vector3[] SoldierSlots;
            public Renderer QaFootprintRenderer;
            public Matrix4x4[] LeftLeg;
            public Matrix4x4[] RightLeg;
            public Matrix4x4[] Torso;
            public Matrix4x4[] CoatSkirt;
            public Matrix4x4[] Head;
            public Matrix4x4[] Headgear;
            public Matrix4x4[] LeftArm;
            public Matrix4x4[] RightArm;
            public Matrix4x4[] CrossBelt;
            public Matrix4x4[] Pack;
            public Matrix4x4[] RifleStock;
            public Matrix4x4[] RifleBarrel;
            public Matrix4x4[] Bayonet;
        }

        private const float FileSpacing = 0.52f;
        private const float RankSpacing = 0.76f;
        private const int LineRanks = 3;
        private const float FullDetailDistance = 250f;
        private const float MediumDetailDistance = 500f;
        private const float MaximumRenderDistance = 850f;

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
            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyRenderer00C>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C2_SOLDIER_RENDERER");
            root.AddComponent<TacticalCompanyRenderer00C>();
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

                states.Add(BuildState(company));
                total += company.PresentStrength;
            }

            mainCamera = Camera.main;
            installed = states.Count == 8;
            if (!installed)
                return;

            Debug.Log(
                "REBUILD-RENDER-00C2|Installed=True|Owner=PresentationOnly|Companies=" + states.Count +
                "|VisibleManpower=" + total +
                "|VisualRatio=1:1|HumanProportions=V2|RifleSilhouette=Stock+Barrel+Bayonet|" +
                "PerSoldierGameObject=False|MovementWrites=False|CombatWrites=False|" +
                "LOD=Full250_Medium500_Far850|WorldLabels=False");
        }

        private CompanyRenderState BuildState(TacticalCompanyEntity00B company)
        {
            int strength = Mathf.Max(0, company.PresentStrength);
            CompanyRenderState state = new CompanyRenderState
            {
                Company = company,
                SoldierSlots = BuildLineSlots(strength),
                LeftLeg = new Matrix4x4[strength],
                RightLeg = new Matrix4x4[strength],
                Torso = new Matrix4x4[strength],
                CoatSkirt = new Matrix4x4[strength],
                Head = new Matrix4x4[strength],
                Headgear = new Matrix4x4[strength],
                LeftArm = new Matrix4x4[strength],
                RightArm = new Matrix4x4[strength],
                CrossBelt = new Matrix4x4[strength],
                Pack = new Matrix4x4[strength],
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
            float width = (files - 1) * FileSpacing;

            for (int i = 0; i < strength; i++)
            {
                int rank = i % LineRanks;
                int file = i / LineRanks;
                float x = -width * 0.5f + file * FileSpacing;
                float z = -rank * RankSpacing;
                float jitterX = HashSigned(i * 17 + 3) * 0.025f;
                float jitterZ = HashSigned(i * 31 + 11) * 0.018f;
                slots[i] = new Vector3(x + jitterX, 0f, z + jitterZ);
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

        private void FillMatrices(CompanyRenderState state, DetailLevel detail)
        {
            Transform root = state.Company.transform;
            int count = Mathf.Min(state.Company.PresentStrength, state.SoldierSlots.Length);
            bool danish = state.Company.Nation == RebuildNation.Denmark;

            for (int i = 0; i < count; i++)
            {
                Vector3 slot = state.SoldierSlots[i];
                float h = 1f + HashSigned(i * 13 + 5) * 0.022f;
                float lean = HashSigned(i * 43 + 9) * 1.6f;

                state.Torso[i] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.00f, 0f),
                    Quaternion.Euler(lean, 0f, 0f),
                    new Vector3(0.34f, 0.58f * h, 0.22f));

                state.CoatSkirt[i] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 0.66f, -0.015f),
                    Quaternion.identity,
                    new Vector3(0.38f, 0.24f, 0.25f));

                float hatHeight = danish ? 0.12f : 0.15f;
                state.Headgear[i] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.72f * h, 0f),
                    Quaternion.identity,
                    new Vector3(0.205f, hatHeight, 0.205f));

                if (detail == DetailLevel.Far)
                    continue;

                state.Head[i] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 1.49f * h, 0.015f),
                    Quaternion.identity,
                    new Vector3(0.215f, 0.235f, 0.205f));

                state.RifleStock[i] = PartMatrix(
                    root,
                    slot + new Vector3(0.20f, 0.91f, -0.18f),
                    Quaternion.Euler(-4f, 0f, 0f),
                    new Vector3(0.105f, 0.13f, 0.34f));

                state.RifleBarrel[i] = PartMatrix(
                    root,
                    slot + new Vector3(0.20f, 0.96f, 0.50f),
                    Quaternion.Euler(-4f, 0f, 0f),
                    new Vector3(0.040f, 0.040f, 1.05f));

                if (detail == DetailLevel.Medium)
                    continue;

                state.LeftLeg[i] = PartMatrix(
                    root,
                    slot + new Vector3(-0.095f, 0.31f, 0f),
                    Quaternion.Euler(0f, 0f, -1.8f),
                    new Vector3(0.105f, 0.31f * h, 0.105f));

                state.RightLeg[i] = PartMatrix(
                    root,
                    slot + new Vector3(0.095f, 0.31f, 0f),
                    Quaternion.Euler(0f, 0f, 1.8f),
                    new Vector3(0.105f, 0.31f * h, 0.105f));

                state.LeftArm[i] = PartMatrix(
                    root,
                    slot + new Vector3(-0.18f, 1.03f, 0.11f),
                    Quaternion.Euler(48f, 0f, -12f),
                    new Vector3(0.085f, 0.27f, 0.085f));

                state.RightArm[i] = PartMatrix(
                    root,
                    slot + new Vector3(0.18f, 1.02f, 0.12f),
                    Quaternion.Euler(50f, 0f, 12f),
                    new Vector3(0.085f, 0.27f, 0.085f));

                state.CrossBelt[i] = PartMatrix(
                    root,
                    slot + new Vector3(-0.015f, 1.03f, -0.118f),
                    Quaternion.Euler(0f, 0f, 28f),
                    new Vector3(0.055f, 0.52f, 0.035f));

                state.Pack[i] = PartMatrix(
                    root,
                    slot + new Vector3(0f, 0.98f, -0.18f),
                    Quaternion.identity,
                    new Vector3(0.30f, 0.35f, 0.14f));

                state.Bayonet[i] = PartMatrix(
                    root,
                    slot + new Vector3(0.20f, 0.915f, 1.16f),
                    Quaternion.Euler(-4f, 0f, 0f),
                    new Vector3(0.018f, 0.018f, 0.28f));
            }
        }

        private void DrawCompany(CompanyRenderState state, DetailLevel detail)
        {
            TacticalCompanyEntity00B company = state.Company;
            int count = Mathf.Min(company.PresentStrength, state.SoldierSlots.Length);
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
            DrawInstanced(capsuleMesh, coat, state.LeftArm, count);
            DrawInstanced(capsuleMesh, coat, state.RightArm, count);
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

        private static Matrix4x4 PartMatrix(
            Transform companyTransform,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 scale)
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
            temporary.name = "TEMP_REBUILD_00C2_MESH_SOURCE";
            temporary.SetActive(false);

            MeshFilter filter = temporary.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            UnityEngine.Object.Destroy(temporary);
            return mesh;
        }

        private void CreateMaterials()
        {
            dkCoat = CreateMaterial(new Color(0.48f, 0.055f, 0.050f), "00C2_DK_Coat");
            dkTrousers = CreateMaterial(new Color(0.055f, 0.075f, 0.115f), "00C2_DK_Trousers");
            dkHeadgear = CreateMaterial(new Color(0.025f, 0.030f, 0.038f), "00C2_DK_Headgear");

            prCoat = CreateMaterial(new Color(0.035f, 0.075f, 0.145f), "00C2_PR_Coat");
            prTrousers = CreateMaterial(new Color(0.27f, 0.28f, 0.29f), "00C2_PR_Trousers");
            prHeadgear = CreateMaterial(new Color(0.018f, 0.018f, 0.020f), "00C2_PR_Headgear");

            skin = CreateMaterial(new Color(0.72f, 0.54f, 0.41f), "00C2_Skin");
            leather = CreateMaterial(new Color(0.13f, 0.075f, 0.035f), "00C2_Leather");
            strap = CreateMaterial(new Color(0.78f, 0.75f, 0.64f), "00C2_Straps");
            rifleWood = CreateMaterial(new Color(0.22f, 0.105f, 0.035f), "00C2_RifleWood");
            metal = CreateMaterial(new Color(0.24f, 0.26f, 0.27f), "00C2_Metal");
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
            if (!installed)
                return;

            float fps = smoothedDelta > 0f ? 1f / smoothedDelta : 0f;
            float ms = smoothedDelta > 0f ? smoothedDelta * 1000f : 0f;

            GUI.depth = -950;
            GUI.Box(new Rect(12f, 42f, 575f, 68f), string.Empty);
            GUI.Label(new Rect(22f, 49f, 555f, 22f),
                "GATE A+B+C2 | 8 Companies | Improved 1:1 soldiers | Hover info | Stable UnitID");
            GUI.Label(new Rect(22f, 70f, 555f, 20f),
                "LMB select | Shift add | Ctrl toggle | LMB drag box | Esc clear");
            GUI.Label(new Rect(22f, 89f, 555f, 18f),
                "World labels OFF | Movement/combat intentionally OFF in v00.01.00c2");

            Rect box = new Rect(Mathf.Max(8f, Screen.width - 405f), 8f, 395f, 76f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "GATE C2 | HUMAN 1:1 RENDERER | " + visibleSoldiers + " visible soldiers");
            GUI.Label(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, 20f),
                "FPS " + fps.ToString("0") + " | " + ms.ToString("0.0") + " ms | Draw calls " + drawCalls);
            GUI.Label(new Rect(box.x + 10f, box.y + 49f, box.width - 20f, 20f),
                "LOD companies F/M/F: " + fullDetailCompanies + "/" + mediumDetailCompanies + "/" + farDetailCompanies +
                " | Move/Combat OFF");
        }
    }
}
