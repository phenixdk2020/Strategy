using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09l2 - 1:1 infantry renderer anchored to independent company tactical centres.
// Ordinary soldiers remain GPU-instanced; only the 32 company command centres are GameObjects.
[DefaultExecutionOrder(10920)]
public sealed class PrototypeCompanyRenderer09L2 : MonoBehaviour
{
    public static PrototypeCompanyRenderer09L2 Instance { get; private set; }
    public bool Installed { get; private set; }

    private sealed class CompanyRenderState
    {
        public PrototypeCompanyTacticalEntity09L2 Company;
        public readonly List<Vector3> Slots = new List<Vector3>();
        public readonly List<float> Phase = new List<float>();
        public int LastInitialStrength = -1;
        public RegimentFormation LastFormation = (RegimentFormation)(-1);
    }

    private sealed class RegimentResources
    {
        public Regiment Regiment;
        public Material Coat;
        public Material Skin;
        public Material Headgear;
        public Material Rifle;
        public float LastObservedNextFireTime;
        public float ReloadStart;
        public float ReloadEnd;
        public bool ReloadActive;
        public float NextProfileRefresh;
    }

    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, CompanyRenderState> states =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, CompanyRenderState>();
    private readonly Dictionary<Regiment, RegimentResources> regimentResources =
        new Dictionary<Regiment, RegimentResources>();

    private readonly Matrix4x4[] bodyBatch = new Matrix4x4[1023];
    private readonly Matrix4x4[] headBatch = new Matrix4x4[1023];
    private readonly Matrix4x4[] hatBatch = new Matrix4x4[1023];
    private readonly Matrix4x4[] rifleBatch = new Matrix4x4[1023];

    private Mesh capsuleMesh;
    private Mesh sphereMesh;
    private Mesh cylinderMesh;
    private Mesh cubeMesh;
    private FieldInfo nextFireTimeField;
    private bool announced;

    private const float LineFileSpacing = 0.52f;
    private const float LineRankSpacing = 0.76f;
    private const int LineRanks = 3;
    private const int ColumnFiles = 8;
    private const float ColumnFileSpacing = 0.60f;
    private const float ColumnRankSpacing = 0.72f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCompanyRenderer09L2>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyRenderer_v000009l2");
        root.AddComponent<PrototypeCompanyRenderer09L2>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        nextFireTimeField = typeof(Regiment).GetField(
            "nextFireTime",
            BindingFlags.Instance | BindingFlags.NonPublic);
        BuildPrimitiveMeshes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        Installed = true;
        int visible = 0;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null)
                continue;

            RegimentResources resources = GetResources(company.ParentRegiment);
            RefreshResources(resources);
            UpdateReloadState(resources);

            CompanyRenderState state = GetState(company);
            if (state.LastInitialStrength != company.InitialStrength ||
                state.LastFormation != company.Formation)
            {
                RebuildSlots(state);
            }

            DrawCompany(state, resources);
            visible += Mathf.Max(0, company.CurrentStrength);
        }

        DrawFootRegimentalStaff(control);

        if (!announced && visible > 0)
        {
            announced = true;
            Debug.Log(
                "RENDER-09L2|Installed=True|Anchor=CompanyTacticalCentres|" +
                "VisualRatio=1:1|CompanyGameObjects=" + companies.Count +
                "|OrdinarySoldierGameObjects=False|VisibleCompanyPersonnel=" + visible +
                "|TerrainFollow=True");
        }
    }

    private CompanyRenderState GetState(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (!states.TryGetValue(company, out CompanyRenderState state))
        {
            state = new CompanyRenderState { Company = company };
            states[company] = state;
            RebuildSlots(state);
        }
        return state;
    }

    private RegimentResources GetResources(Regiment regiment)
    {
        if (regimentResources.TryGetValue(regiment, out RegimentResources resources))
            return resources;

        PrototypeUniformProfile09H profile = GetCurrentProfile(regiment);
        resources = new RegimentResources
        {
            Regiment = regiment,
            Coat = CreateInstancedMaterial(profile.CoatColor, "09L2_" + regiment.RegimentName + "_Coat"),
            Skin = CreateInstancedMaterial(profile.SkinColor, "09L2_" + regiment.RegimentName + "_Skin"),
            Headgear = CreateInstancedMaterial(profile.HeadgearColor, "09L2_" + regiment.RegimentName + "_Headgear"),
            Rifle = CreateInstancedMaterial(profile.EquipmentColor, "09L2_" + regiment.RegimentName + "_Rifle")
        };

        if (nextFireTimeField != null)
            resources.LastObservedNextFireTime = (float)nextFireTimeField.GetValue(regiment);

        regimentResources[regiment] = resources;
        return resources;
    }

    private static PrototypeUniformProfile09H GetCurrentProfile(Regiment regiment)
    {
        PrototypeSoldierVisualPass09H bridge = PrototypeSoldierVisualPass09H.Instance;
        if (bridge != null)
        {
            PrototypeUniformProfile09H profile = bridge.GetProfileCopy(regiment);
            if (profile != null)
                return profile;
        }

        return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
    }

    private static void RefreshResources(RegimentResources resources)
    {
        if (resources == null || resources.Regiment == null || Time.unscaledTime < resources.NextProfileRefresh)
            return;

        resources.NextProfileRefresh = Time.unscaledTime + 0.30f;
        PrototypeUniformProfile09H profile = GetCurrentProfile(resources.Regiment);
        resources.Coat.color = profile.CoatColor;
        resources.Skin.color = profile.SkinColor;
        resources.Headgear.color = profile.HeadgearColor;
        resources.Rifle.color = profile.EquipmentColor;
    }

    private void RebuildSlots(CompanyRenderState state)
    {
        state.Slots.Clear();
        state.Phase.Clear();

        PrototypeCompanyTacticalEntity09L2 company = state.Company;
        int strength = Mathf.Max(0, company.InitialStrength);

        if (company.Formation == RegimentFormation.Column)
        {
            for (int i = 0; i < strength; i++)
            {
                int row = i / ColumnFiles;
                int file = i % ColumnFiles;
                float x = (file - (ColumnFiles - 1) * 0.5f) * ColumnFileSpacing;
                float z = -row * ColumnRankSpacing;
                state.Slots.Add(new Vector3(x, 0f, z));
            }
        }
        else
        {
            int files = Mathf.Max(1, Mathf.CeilToInt(strength / (float)LineRanks));
            for (int i = 0; i < strength; i++)
            {
                int rank = i / files;
                int file = i % files;
                float x = (file - (files - 1) * 0.5f) * LineFileSpacing;
                float z = (1f - rank) * LineRankSpacing;
                state.Slots.Add(new Vector3(x, 0f, z));
            }
        }

        // Deterministic shuffle: when casualties reduce CurrentStrength, missing men are
        // distributed through the company instead of cutting one end off into an L shape.
        uint seed = (uint)(company.BattalionIndex * 7919 + company.CompanyIndex * 104729 + strength * 17 + 12345);
        for (int i = state.Slots.Count - 1; i > 0; i--)
        {
            seed = seed * 1664525u + 1013904223u;
            int j = (int)(seed % (uint)(i + 1));
            Vector3 tmp = state.Slots[i];
            state.Slots[i] = state.Slots[j];
            state.Slots[j] = tmp;
        }

        for (int i = 0; i < state.Slots.Count; i++)
            state.Phase.Add(i * 0.731f + company.BattalionIndex * 0.37f + company.CompanyIndex * 0.19f);

        state.LastInitialStrength = strength;
        state.LastFormation = company.Formation;
    }

    private void DrawCompany(CompanyRenderState state, RegimentResources resources)
    {
        PrototypeCompanyTacticalEntity09L2 company = state.Company;
        Regiment regiment = company.ParentRegiment;
        int count = Mathf.Clamp(company.CurrentStrength, 0, state.Slots.Count);
        if (count <= 0)
            return;

        bool marching = company.IsMoving;
        float reload01 = GetReloadProgress(resources);
        bool reloading = resources.ReloadActive;
        float marchTime = Time.time * 7.0f;
        Quaternion companyRotation = company.transform.rotation;
        int batchCount = 0;

        for (int i = 0; i < count; i++)
        {
            Vector3 local = state.Slots[i];
            Vector3 world = company.transform.TransformPoint(local);
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.10f;
            if (marching)
                world.y += Mathf.Sin(marchTime + state.Phase[i]) * 0.024f;

            bodyBatch[batchCount] = Matrix4x4.TRS(
                world + Vector3.up * 0.78f,
                companyRotation,
                new Vector3(0.30f, 0.62f, 0.24f));

            headBatch[batchCount] = Matrix4x4.TRS(
                world + Vector3.up * 1.43f,
                companyRotation,
                Vector3.one * 0.18f);

            Vector3 hatScale = regiment.Team == BattleTeam.Prussia
                ? new Vector3(0.20f, 0.10f, 0.20f)
                : new Vector3(0.21f, 0.075f, 0.21f);
            hatBatch[batchCount] = Matrix4x4.TRS(
                world + Vector3.up * 1.58f,
                companyRotation,
                hatScale);

            float stagger = Mathf.Sin(i * 1.37f) * 0.025f;
            float r = Mathf.Clamp01(reload01 + stagger);
            Quaternion localRifleRotation;
            Vector3 localRifleOffset;

            if (reloading)
            {
                if (regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle)
                {
                    float action = Mathf.Sin(r * Mathf.PI);
                    localRifleRotation = Quaternion.Euler(Mathf.Lerp(4f, 28f, action), 0f, 4f);
                    localRifleOffset = new Vector3(0.18f, Mathf.Lerp(0.98f, 0.84f, action), 0.22f);
                }
                else
                {
                    float vertical = Mathf.Sin(r * Mathf.PI);
                    localRifleRotation = Quaternion.Euler(Mathf.Lerp(4f, -82f, vertical), 0f, 4f);
                    localRifleOffset = new Vector3(0.16f, Mathf.Lerp(0.98f, 0.82f, vertical), 0.18f);
                }
            }
            else
            {
                localRifleRotation = Quaternion.Euler(4f, 0f, 5f);
                localRifleOffset = new Vector3(0.20f, 0.98f, 0.25f);
            }

            Vector3 rifleWorld = world + companyRotation * localRifleOffset;
            rifleBatch[batchCount] = Matrix4x4.TRS(
                rifleWorld,
                companyRotation * localRifleRotation,
                new Vector3(0.045f, 0.045f, 0.94f));

            batchCount++;
            if (batchCount == 1023)
            {
                Flush(resources, batchCount);
                batchCount = 0;
            }
        }

        if (batchCount > 0)
            Flush(resources, batchCount);
    }

    private void DrawFootRegimentalStaff(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<Regiment> drawn = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;

        for (int i = 0; i < companies.Count; i++)
        {
            Regiment regiment = companies[i] != null ? companies[i].ParentRegiment : null;
            if (regiment == null || !drawn.Add(regiment))
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob == null)
                continue;

            int companyTotal = 0;
            for (int b = 0; b < oob.Battalions.Count; b++)
                for (int c = 0; c < oob.Battalions[b].Companies.Count; c++)
                    companyTotal += oob.Battalions[b].Companies[c].CurrentStrength;

            int staffAlive = Mathf.Clamp(regiment.CurrentStrength - companyTotal, 0, oob.RegimentalStaffStrength);
            int footStaff = Mathf.Max(0, staffAlive - 3); // three mounted HQ riders are built by the retained 09k HQ layer.
            if (footStaff <= 0)
                continue;

            RegimentResources resources = GetResources(regiment);
            int batchCount = 0;
            float z = -(oob.Battalions.Count * 22f + 18f);

            for (int s = 0; s < footStaff; s++)
            {
                Vector3 local = new Vector3((s % 11 - 5f) * 0.70f, 0f, z - (s / 11) * 0.78f);
                Vector3 world = regiment.transform.TransformPoint(local);
                world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.10f;
                Quaternion rotation = regiment.transform.rotation;

                bodyBatch[batchCount] = Matrix4x4.TRS(world + Vector3.up * 0.78f, rotation, new Vector3(0.30f, 0.62f, 0.24f));
                headBatch[batchCount] = Matrix4x4.TRS(world + Vector3.up * 1.43f, rotation, Vector3.one * 0.18f);
                hatBatch[batchCount] = Matrix4x4.TRS(world + Vector3.up * 1.58f, rotation, new Vector3(0.20f, 0.09f, 0.20f));
                rifleBatch[batchCount] = Matrix4x4.TRS(
                    world + rotation * new Vector3(0.20f, 0.98f, 0.25f),
                    rotation * Quaternion.Euler(4f, 0f, 5f),
                    new Vector3(0.045f, 0.045f, 0.94f));
                batchCount++;
            }

            if (batchCount > 0)
                Flush(resources, batchCount);
        }
    }

    private void Flush(RegimentResources resources, int count)
    {
        Graphics.DrawMeshInstanced(capsuleMesh, 0, resources.Coat, bodyBatch, count);
        Graphics.DrawMeshInstanced(sphereMesh, 0, resources.Skin, headBatch, count);
        Graphics.DrawMeshInstanced(cylinderMesh, 0, resources.Headgear, hatBatch, count);
        Graphics.DrawMeshInstanced(cubeMesh, 0, resources.Rifle, rifleBatch, count);
    }

    private void UpdateReloadState(RegimentResources resources)
    {
        if (nextFireTimeField == null || resources == null || resources.Regiment == null)
            return;

        float next = (float)nextFireTimeField.GetValue(resources.Regiment);
        if (next > Time.time + 0.05f && next > resources.LastObservedNextFireTime + 0.10f)
        {
            resources.ReloadStart = Time.time;
            resources.ReloadEnd = next;
            resources.ReloadActive = true;
        }

        resources.LastObservedNextFireTime = next;
        if (resources.ReloadActive && Time.time >= resources.ReloadEnd)
            resources.ReloadActive = false;
    }

    private static float GetReloadProgress(RegimentResources resources)
    {
        if (resources == null || !resources.ReloadActive)
            return 1f;
        return Mathf.InverseLerp(resources.ReloadStart, resources.ReloadEnd, Time.time);
    }

    private static Material CreateInstancedMaterial(Color color, string name)
    {
        Material material = PrototypeBootstrap.CreateSharedMaterial(color, name);
        material.enableInstancing = true;
        return material;
    }

    private void BuildPrimitiveMeshes()
    {
        capsuleMesh = ExtractMesh(PrimitiveType.Capsule, "09L2_CapsuleMesh");
        sphereMesh = ExtractMesh(PrimitiveType.Sphere, "09L2_SphereMesh");
        cylinderMesh = ExtractMesh(PrimitiveType.Cylinder, "09L2_CylinderMesh");
        cubeMesh = ExtractMesh(PrimitiveType.Cube, "09L2_CubeMesh");
    }

    private static Mesh ExtractMesh(PrimitiveType type, string name)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh source = temp.GetComponent<MeshFilter>().sharedMesh;
        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = name;
        UnityEngine.Object.Destroy(temp);
        return mesh;
    }
}
