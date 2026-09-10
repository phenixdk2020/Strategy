using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project1864.Rebuild
{
    // v00.01.00c / Gate C
    // 1:1 presentation layer driven exclusively from TacticalCompanyEntity00B state.
    // This renderer never writes Company position, facing, selection, movement, combat or OOB state.
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
            public Matrix4x4[] Head;
            public Matrix4x4[] Headgear;
            public Matrix4x4[] Pack;
            public Matrix4x4[] Rifle;
        }

        private const float FileSpacing = 0.52f;
        private const float RankSpacing = 0.76f;
        private const int LineRanks = 3;
        private const float FullDetailDistance = 260f;
        private const float MediumDetailDistance = 520f;
        private const float MaximumRenderDistance = 850f;

        private readonly List<CompanyRenderState> states = new List<CompanyRenderState>();

        private Mesh cubeMesh;
        private Mesh sphereMesh;
        private Mesh cylinderMesh;

        private Material dkCoat;
        private Material dkTrousers;
        private Material dkHeadgear;
        private Material prCoat;
        private Material prTrousers;
        private Material prHeadgear;
        private Material skin;
        private Material leather;
        private Material rifleWood;

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

            GameObject root = new GameObject("REBUILD_00C_1TO1_RENDERER");
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

                CompanyRenderState state = BuildState(company);
                states.Add(state);
                total += company.PresentStrength;
            }

            mainCamera = Camera.main;
            installed = states.Count == 8;

            if (!installed)
                return;

            Debug.Log(
                "REBUILD-RENDER-00C|Installed=True|Owner=PresentationOnly|Companies=" + states.Count +
                "|VisibleManpower=" + total +
                "|VisualRatio=1:1|PerSoldierGameObject=False|MovementWrites=False|CombatWrites=False|" +
                "LOD=Full260_Medium520_Far850");
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
                Head = new Matrix4x4[strength],
                Headgear = new Matrix4x4[strength],
                Pack = new Matrix4x4[strength],
                Rifle = new Matrix4x4[strength]
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

                // Tiny deterministic positional variance prevents a perfectly synthetic grid
                // without changing Company frontage or tactical footprint.
                float jitterX = HashSigned(i * 17 + 3) * 0.035f;
                float jitterZ = HashSigned(i * 31 + 11) * 0.025f;
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
            Transform companyTransform = state.Company.transform;
            int count = Mathf.Min(state.Company.PresentStrength, state.SoldierSlots.Length);

            for (int i = 0; i < count; i++)
            {
                Vector3 slot = state.SoldierSlots[i];
                float heightVariance = 1f + HashSigned(i * 13 + 5) * 0.025f;

                state.Torso[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0f, 0.78f, 0f),
                    Quaternion.identity,
                    new Vector3(0.42f, 0.72f * heightVariance, 0.25f));

                state.Headgear[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0f, 1.58f * heightVariance, 0f),
                    Quaternion.identity,
                    new Vector3(0.22f, 0.095f, 0.22f));

                if (detail == DetailLevel.Far)
                    continue;

                state.Head[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0f, 1.40f * heightVariance, 0f),
                    Quaternion.identity,
                    new Vector3(0.26f, 0.26f, 0.26f));

                state.Rifle[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0.31f, 0.83f, 0.03f),
                    Quaternion.Euler(0f, 0f, -7f),
                    new Vector3(0.055f, 1.28f, 0.055f));

                if (detail == DetailLevel.Medium)
                    continue;

                state.LeftLeg[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(-0.105f, 0.20f, 0f),
                    Quaternion.identity,
                    new Vector3(0.16f, 0.70f * heightVariance, 0.18f));

                state.RightLeg[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0.105f, 0.20f, 0f),
                    Quaternion.identity,
                    new Vector3(0.16f, 0.70f * heightVariance, 0.18f));

                state.Pack[i] = PartMatrix(
                    companyTransform,
                    slot + new Vector3(0f, 0.86f, -0.19f),
                    Quaternion.identity,
                    new Vector3(0.34f, 0.42f, 0.16f));
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
            DrawInstanced(cylinderMesh, headgear, state.Headgear, count);

            if (detail == DetailLevel.Far)
                return;

            DrawInstanced(sphereMesh, skin, state.Head, count);
            DrawInstanced(cubeMesh, rifleWood, state.Rifle, count);

            if (detail == DetailLevel.Medium)
                return;

            DrawInstanced(cubeMesh, trousers, state.LeftLeg, count);
            DrawInstanced(cubeMesh, trousers, state.RightLeg, count);
            DrawInstanced(cubeMesh, leather, state.Pack, count);
        }

        private void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (mesh == null || material == null || matrices == null || count <= 0)
                return;

            int start = 0;
            Matrix4x4[] batch = new Matrix4x4[1023];

            while (start < count)
            {
                int batchCount = Mathf.Min(1023, count - start);
                for (int i = 0; i < batchCount; i++)
                    batch[i] = matrices[start + i];

                Graphics.DrawMeshInstanced(
                    mesh,
                    0,
                    material,
                    batch,
                    batchCount,
                    null,
                    ShadowCastingMode.On,
                    true,
                    0,
                    mainCamera,
                    LightProbeUsage.Off,
                    null);

                drawCalls++;
                start += batchCount;
            }
        }

        private static Matrix4x4 PartMatrix(
            Transform companyTransform,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 scale)
        {
            Vector3 worldPosition = companyTransform.TransformPoint(localPosition);
            Quaternion worldRotation = companyTransform.rotation * localRotation;
            return Matrix4x4.TRS(worldPosition, worldRotation, scale);
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
            cubeMesh = CapturePrimitiveMesh(PrimitiveType.Cube, "REBUILD_00C_MeshCube");
            sphereMesh = CapturePrimitiveMesh(PrimitiveType.Sphere, "REBUILD_00C_MeshSphere");
            cylinderMesh = CapturePrimitiveMesh(PrimitiveType.Cylinder, "REBUILD_00C_MeshCylinder");
        }

        private static Mesh CapturePrimitiveMesh(PrimitiveType type, string meshName)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
            temporary.name = "TEMP_" + meshName;
            temporary.SetActive(false);

            MeshFilter filter = temporary.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh != null)
                mesh.name = meshName;

            UnityEngine.Object.Destroy(temporary);
            return mesh;
        }

        private void CreateMaterials()
        {
            // QA palette only. Historical uniform art remains a later dedicated visual gate.
            dkCoat = CreateMaterial(new Color(0.50f, 0.07f, 0.07f), "00C_DK_Coat");
            dkTrousers = CreateMaterial(new Color(0.08f, 0.11f, 0.17f), "00C_DK_Trousers");
            dkHeadgear = CreateMaterial(new Color(0.04f, 0.05f, 0.07f), "00C_DK_Headgear");

            prCoat = CreateMaterial(new Color(0.05f, 0.10f, 0.18f), "00C_PR_Coat");
            prTrousers = CreateMaterial(new Color(0.30f, 0.31f, 0.31f), "00C_PR_Trousers");
            prHeadgear = CreateMaterial(new Color(0.025f, 0.025f, 0.025f), "00C_PR_Headgear");

            skin = CreateMaterial(new Color(0.71f, 0.54f, 0.42f), "00C_Skin");
            leather = CreateMaterial(new Color(0.19f, 0.12f, 0.07f), "00C_Leather");
            rifleWood = CreateMaterial(new Color(0.17f, 0.085f, 0.035f), "00C_Rifle");
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

            GUI.depth = -880;
            Rect box = new Rect(Mathf.Max(8f, Screen.width - 405f), 8f, 395f, 76f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "GATE C | 1:1 COMPANY RENDERER | " + visibleSoldiers + " visible soldiers");
            GUI.Label(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, 20f),
                "FPS " + fps.ToString("0") + " | " + ms.ToString("0.0") + " ms | Draw calls " + drawCalls);
            GUI.Label(new Rect(box.x + 10f, box.y + 49f, box.width - 20f, 20f),
                "LOD companies F/M/F: " + fullDetailCompanies + "/" + mediumDetailCompanies + "/" + farDetailCompanies +
                " | Movement/Combat OFF");
        }
    }
}
