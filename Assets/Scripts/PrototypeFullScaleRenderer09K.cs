using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09k TEST - full-scale 1:1 infantry rendering pilot.
// One visual human per current regiment manpower, but no GameObject/MonoBehaviour/
// collider/NavMeshAgent/AI per ordinary soldier. The Regiment remains simulation owner.
[DefaultExecutionOrder(10850)]
public sealed class PrototypeFullScaleRenderer09K : MonoBehaviour
{
    private sealed class UnitRenderState
    {
        public Regiment Regiment;
        public PrototypeRegimentOOB09K OOB;
        public readonly List<Vector3> LocalSlots = new List<Vector3>();
        public readonly List<float> Phase = new List<float>();
        public Material Coat;
        public Material Skin;
        public Material Headgear;
        public Material Rifle;
        public int LastInitialStrength = -1;
        public RegimentFormation LastFormation = (RegimentFormation)(-1);
        public float LastObservedNextFireTime;
        public float ReloadStart;
        public float ReloadEnd;
        public bool ReloadActive;
        public bool HqBuilt;
        public Transform HqRoot;
        public float NextLegacyHide;
        public float NextProfileRefresh;
    }

    private readonly Dictionary<Regiment, UnitRenderState> states =
        new Dictionary<Regiment, UnitRenderState>();

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

    private const int BatchSize = 1023;
    private const float FileSpacing = 0.64f;
    private const float RankSpacing = 0.78f;
    private const int CompanyRanks = 3;
    private const float CompanyGap = 3.2f;
    private const float CompanyRowGap = 4.5f;
    private const float BattalionDepthGap = 8.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFullScaleRenderer09K>() != null)
            return;

        GameObject root = new GameObject("PrototypeFullScaleRenderer_v000009k");
        root.AddComponent<PrototypeFullScaleRenderer09K>();
    }

    private void Awake()
    {
        nextFireTimeField = typeof(Regiment).GetField(
            "nextFireTime",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BuildPrimitiveMeshes();
    }

    private void Start()
    {
        DisableRepresentativeVisualLayers();
    }

    private void Update()
    {
        DisableRepresentativeVisualLayers();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int totalVisible = 0;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !IsFullScalePilotRegiment(regiment.RegimentName))
                continue;

            UnitRenderState state;
            if (!states.TryGetValue(regiment, out state))
            {
                state = BuildState(regiment);
                states[regiment] = state;
            }

            if (Time.unscaledTime >= state.NextLegacyHide)
            {
                state.NextLegacyHide = Time.unscaledTime + 0.40f;
                HideRepresentativeSoldierRenderers(regiment);
            }

            if (Time.unscaledTime >= state.NextProfileRefresh)
            {
                state.NextProfileRefresh = Time.unscaledTime + 0.25f;
                RefreshUniformColors(state);
            }

            if (!state.HqBuilt)
                BuildMountedRegimentalHQ(state);

            if (state.LastInitialStrength != regiment.InitialStrength ||
                state.LastFormation != regiment.Formation)
            {
                RebuildSlots(state);
            }

            UpdateReloadState(state);
            DrawRegiment(state);
            UpdateHqVisibility(state);
            totalVisible += Mathf.Max(0, regiment.CurrentStrength);
        }

        if (!announced && totalVisible > 0)
        {
            announced = true;
            Debug.Log(
                "SCALE-09K|Installed=True|VisualRatio=1:1|Renderer=DrawMeshInstanced|" +
                "PerSoldierGameObject=False|TotalVisible=" + totalVisible +
                "|MountedHQ=3PerRegiment|CompanyBlocks=True|MovementWrites=False");
        }
    }

    private static bool IsFullScalePilotRegiment(string name)
    {
        return name == "1. Regiment" ||
               name == "5. Regiment" ||
               name == "8th Regiment" ||
               name == "18th Regiment";
    }

    private void DisableRepresentativeVisualLayers()
    {
        PrototypeSoldierVisualPass09I old09i = Object.FindAnyObjectByType<PrototypeSoldierVisualPass09I>();
        if (old09i != null && old09i.enabled)
            old09i.enabled = false;

        PrototypeSoldierVisualPass09H old09h = Object.FindAnyObjectByType<PrototypeSoldierVisualPass09H>();
        if (old09h != null && old09h.enabled)
            old09h.enabled = false;

        // 09k has its own 1:1 reload rifle pose. Keep 09j code in the repo as the
        // detailed representative reference, but do not let it fight over hidden rigs.
        PrototypeReloadAnimation09J oldReload = Object.FindAnyObjectByType<PrototypeReloadAnimation09J>();
        if (oldReload != null && oldReload.enabled)
            oldReload.enabled = false;
    }

    private UnitRenderState BuildState(Regiment regiment)
    {
        PrototypeUniformProfile09H profile = GetCurrentProfile(regiment);
        UnitRenderState state = new UnitRenderState
        {
            Regiment = regiment,
            OOB = regiment.GetComponent<PrototypeRegimentOOB09K>(),
            Coat = CreateInstancedMaterial(profile.CoatColor, "09K_" + regiment.RegimentName + "_Coat"),
            Skin = CreateInstancedMaterial(profile.SkinColor, "09K_" + regiment.RegimentName + "_Skin"),
            Headgear = CreateInstancedMaterial(profile.HeadgearColor, "09K_" + regiment.RegimentName + "_Headgear"),
            Rifle = CreateInstancedMaterial(profile.EquipmentColor, "09K_" + regiment.RegimentName + "_Rifle")
        };

        if (nextFireTimeField != null)
            state.LastObservedNextFireTime = (float)nextFireTimeField.GetValue(regiment);

        RebuildSlots(state);
        return state;
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

    private static void RefreshUniformColors(UnitRenderState state)
    {
        if (state == null || state.Regiment == null)
            return;

        PrototypeUniformProfile09H profile = GetCurrentProfile(state.Regiment);
        if (profile == null)
            return;

        state.Coat.color = profile.CoatColor;
        state.Skin.color = profile.SkinColor;
        state.Headgear.color = profile.HeadgearColor;
        state.Rifle.color = profile.EquipmentColor;
    }

    private void RebuildSlots(UnitRenderState state)
    {
        state.LocalSlots.Clear();
        state.Phase.Clear();

        Regiment regiment = state.Regiment;
        state.OOB = regiment.GetComponent<PrototypeRegimentOOB09K>();

        int footTarget = Mathf.Max(0, regiment.InitialStrength - 3);

        if (state.OOB == null || state.OOB.Battalions == null || state.OOB.Battalions.Count == 0)
        {
            BuildFallbackSlots(state, footTarget);
        }
        else if (regiment.Formation == RegimentFormation.Column)
        {
            BuildMarchColumnSlots(state, footTarget);
        }
        else
        {
            BuildBattalionLineSlots(state, state.OOB);
        }

        while (state.LocalSlots.Count < footTarget)
        {
            int i = state.LocalSlots.Count;
            AddSlot(state, new Vector3((i % 12 - 5.5f) * 0.66f, 0f, -60f - (i / 12) * 0.72f));
        }

        if (state.LocalSlots.Count > footTarget)
        {
            state.LocalSlots.RemoveRange(footTarget, state.LocalSlots.Count - footTarget);
            state.Phase.RemoveRange(footTarget, state.Phase.Count - footTarget);
        }

        state.LastInitialStrength = regiment.InitialStrength;
        state.LastFormation = regiment.Formation;

        Debug.Log(
            "SCALE-09K|Unit=" + regiment.RegimentName +
            "|Strength=" + regiment.InitialStrength +
            "|FootSlots=" + state.LocalSlots.Count +
            "|MountedHQ=3|Formation=" + regiment.Formation +
            "|VisualRatio=1:1");
    }

    private void BuildBattalionLineSlots(UnitRenderState state, PrototypeRegimentOOB09K oob)
    {
        int maxCompanyStrength = 1;
        for (int b = 0; b < oob.Battalions.Count; b++)
        {
            for (int c = 0; c < oob.Battalions[b].Companies.Count; c++)
                maxCompanyStrength = Mathf.Max(maxCompanyStrength, oob.Battalions[b].Companies[c].InitialStrength);
        }

        int maxFiles = Mathf.CeilToInt(maxCompanyStrength / (float)CompanyRanks);
        float maxCompanyWidth = Mathf.Max(FileSpacing, (maxFiles - 1) * FileSpacing);
        float companyRowDepth = (CompanyRanks - 1) * RankSpacing + CompanyRowGap;
        float battalionDepth = companyRowDepth * 2f;

        for (int b = 0; b < oob.Battalions.Count; b++)
        {
            PrototypeRegimentOOB09K.BattalionState battalion = oob.Battalions[b];
            float battalionZ = -b * (battalionDepth + BattalionDepthGap);

            for (int c = 0; c < battalion.Companies.Count; c++)
            {
                PrototypeRegimentOOB09K.CompanyState company = battalion.Companies[c];
                int files = Mathf.CeilToInt(company.InitialStrength / (float)CompanyRanks);
                float companyWidth = Mathf.Max(FileSpacing, (files - 1) * FileSpacing);
                int companyColumn = c % 2;
                int companyRow = c / 2;

                float xCenter = companyColumn == 0
                    ? -(maxCompanyWidth + CompanyGap) * 0.5f
                    : (maxCompanyWidth + CompanyGap) * 0.5f;
                float zBase = battalionZ - companyRow * companyRowDepth;

                for (int i = 0; i < company.InitialStrength; i++)
                {
                    int rank = i / files;
                    int file = i % files;
                    float x = xCenter + (file - (files - 1) * 0.5f) * FileSpacing;
                    float z = zBase - rank * RankSpacing;
                    AddSlot(state, new Vector3(x, 0f, z));
                }
            }
        }

        // Company manpower excludes 25 regimental staff. Three staff are the mounted
        // HQ group, so only 22 staff slots are rendered here on foot.
        int footStaff = Mathf.Max(0, oob.RegimentalStaffStrength - 3);
        float staffZ = -oob.Battalions.Count * (battalionDepth + BattalionDepthGap) - 2.5f;
        for (int i = 0; i < footStaff; i++)
        {
            float x = (i % 11 - 5f) * 0.70f;
            float z = staffZ - (i / 11) * 0.75f;
            AddSlot(state, new Vector3(x, 0f, z));
        }
    }

    private void BuildMarchColumnSlots(UnitRenderState state, int footTarget)
    {
        const int filesAcross = 12;
        for (int i = 0; i < footTarget; i++)
        {
            int row = i / filesAcross;
            int file = i % filesAcross;
            float x = (file - (filesAcross - 1) * 0.5f) * 0.66f;
            float z = -row * 0.72f;
            AddSlot(state, new Vector3(x, 0f, z));
        }
    }

    private void BuildFallbackSlots(UnitRenderState state, int strength)
    {
        int files = Mathf.Max(1, Mathf.CeilToInt(strength / 3f));
        for (int i = 0; i < strength; i++)
        {
            int rank = i / files;
            int file = i % files;
            AddSlot(state, new Vector3((file - (files - 1) * 0.5f) * FileSpacing, 0f, -rank * RankSpacing));
        }
    }

    private static void AddSlot(UnitRenderState state, Vector3 local)
    {
        state.LocalSlots.Add(local);
        state.Phase.Add(state.LocalSlots.Count * 0.731f);
    }

    private void DrawRegiment(UnitRenderState state)
    {
        Regiment regiment = state.Regiment;
        if (regiment == null)
            return;

        int footCurrent = Mathf.Clamp(regiment.CurrentStrength - 3, 0, state.LocalSlots.Count);
        if (footCurrent <= 0)
            return;

        Matrix4x4 root = regiment.transform.localToWorldMatrix;
        float reload01 = GetReloadProgress(state);
        bool reloading = state.ReloadActive;
        bool marching = regiment.Formation == RegimentFormation.Column;
        float marchTime = Time.time * 7.2f;
        int batchCount = 0;

        for (int i = 0; i < footCurrent; i++)
        {
            Vector3 slot = state.LocalSlots[i];
            float bob = marching ? Mathf.Sin(marchTime + state.Phase[i]) * 0.025f : 0f;
            Vector3 pos = slot + new Vector3(0f, bob, 0f);

            bodyBatch[batchCount] = root * Matrix4x4.TRS(
                pos + new Vector3(0f, 0.78f, 0f),
                Quaternion.identity,
                new Vector3(0.30f, 0.62f, 0.24f));

            headBatch[batchCount] = root * Matrix4x4.TRS(
                pos + new Vector3(0f, 1.43f, 0f),
                Quaternion.identity,
                Vector3.one * 0.18f);

            Vector3 hatScale = regiment.Team == BattleTeam.Prussia
                ? new Vector3(0.20f, 0.10f, 0.20f)
                : new Vector3(0.21f, 0.075f, 0.21f);
            hatBatch[batchCount] = root * Matrix4x4.TRS(
                pos + new Vector3(0f, 1.58f, 0f),
                Quaternion.identity,
                hatScale);

            float stagger = Mathf.Sin(i * 1.37f) * 0.025f;
            float r = Mathf.Clamp01(reload01 + stagger);
            Quaternion rifleRotation;
            Vector3 riflePosition;

            if (reloading)
            {
                if (regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle)
                {
                    float action = Mathf.Sin(r * Mathf.PI);
                    rifleRotation = Quaternion.Euler(Mathf.Lerp(4f, 28f, action), 0f, 4f);
                    riflePosition = pos + new Vector3(0.18f, Mathf.Lerp(0.98f, 0.84f, action), 0.22f);
                }
                else
                {
                    float vertical = Mathf.Sin(r * Mathf.PI);
                    rifleRotation = Quaternion.Euler(Mathf.Lerp(4f, -82f, vertical), 0f, 4f);
                    riflePosition = pos + new Vector3(0.16f, Mathf.Lerp(0.98f, 0.82f, vertical), 0.18f);
                }
            }
            else
            {
                rifleRotation = Quaternion.Euler(4f, 0f, 5f);
                riflePosition = pos + new Vector3(0.20f, 0.98f, 0.25f);
            }

            rifleBatch[batchCount] = root * Matrix4x4.TRS(
                riflePosition,
                rifleRotation,
                new Vector3(0.045f, 0.045f, 0.94f));

            batchCount++;
            if (batchCount == BatchSize)
            {
                FlushBatch(state, batchCount);
                batchCount = 0;
            }
        }

        if (batchCount > 0)
            FlushBatch(state, batchCount);
    }

    private void FlushBatch(UnitRenderState state, int count)
    {
        Graphics.DrawMeshInstanced(capsuleMesh, 0, state.Coat, bodyBatch, count);
        Graphics.DrawMeshInstanced(sphereMesh, 0, state.Skin, headBatch, count);
        Graphics.DrawMeshInstanced(cylinderMesh, 0, state.Headgear, hatBatch, count);
        Graphics.DrawMeshInstanced(cubeMesh, 0, state.Rifle, rifleBatch, count);
    }

    private void UpdateReloadState(UnitRenderState state)
    {
        if (nextFireTimeField == null || state.Regiment == null)
            return;

        float next = (float)nextFireTimeField.GetValue(state.Regiment);
        if (next > Time.time + 0.05f && next > state.LastObservedNextFireTime + 0.10f)
        {
            state.ReloadStart = Time.time;
            state.ReloadEnd = next;
            state.ReloadActive = true;

            Debug.Log(
                "SCALE-09K|Unit=" + state.Regiment.RegimentName +
                "|ReloadVisual=True|Weapon=" + state.Regiment.WeaponType +
                "|Window=" + (state.ReloadEnd - state.ReloadStart).ToString("0.00"));
        }

        state.LastObservedNextFireTime = next;
        if (state.ReloadActive && Time.time >= state.ReloadEnd)
            state.ReloadActive = false;
    }

    private static float GetReloadProgress(UnitRenderState state)
    {
        if (!state.ReloadActive)
            return 1f;
        return Mathf.InverseLerp(state.ReloadStart, state.ReloadEnd, Time.time);
    }

    private static void HideRepresentativeSoldierRenderers(Regiment regiment)
    {
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("Soldier_"))
                continue;

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
                renderers[r].enabled = false;
        }
    }

    private void BuildMountedRegimentalHQ(UnitRenderState state)
    {
        Regiment regiment = state.Regiment;
        if (regiment == null)
            return;

        Transform existing = regiment.transform.Find("RegimentalHQ09K");
        if (existing != null)
        {
            state.HqRoot = existing;
            state.HqBuilt = true;
            return;
        }

        GameObject root = new GameObject("RegimentalHQ09K");
        root.transform.SetParent(regiment.transform, false);

        int battalionCount = state.OOB != null ? state.OOB.Battalions.Count : 2;
        root.transform.localPosition = new Vector3(0f, 0f, -(battalionCount * 18f + 10f));

        PrototypeUniformProfile09H profile = GetCurrentProfile(regiment);
        Material coat = PrototypeBootstrap.CreateSharedMaterial(profile.CoatColor, "HQ09K_Coat_" + regiment.RegimentName);
        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.23f, 0.13f, 0.07f), "HQ09K_Horse_" + regiment.RegimentName);
        Material skin = PrototypeBootstrap.CreateSharedMaterial(profile.SkinColor, "HQ09K_Skin_" + regiment.RegimentName);
        Material dark = PrototypeBootstrap.CreateSharedMaterial(profile.HeadgearColor, "HQ09K_Dark_" + regiment.RegimentName);

        string[] roles = { "Commander", "Adjutant", "Orderly" };
        for (int i = 0; i < 3; i++)
        {
            GameObject rider = new GameObject("HQ_" + roles[i]);
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3((i - 1) * 2.2f, 0f, i == 0 ? 0f : -1.0f);

            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "HorseBody", new Vector3(0f, 0.75f, 0f), new Vector3(0.48f, 0.42f, 0.88f), Quaternion.Euler(90f, 0f, 0f), horse);
            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "HorseNeck", new Vector3(0f, 1.15f, 0.54f), new Vector3(0.24f, 0.42f, 0.24f), Quaternion.Euler(-20f, 0f, 0f), horse);
            CreatePrimitive(rider.transform, PrimitiveType.Sphere, "HorseHead", new Vector3(0f, 1.48f, 0.70f), new Vector3(0.26f, 0.22f, 0.34f), Quaternion.identity, horse);
            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "RiderBody", new Vector3(0f, 1.82f, 0f), new Vector3(0.28f, 0.46f, 0.24f), Quaternion.identity, coat);
            CreatePrimitive(rider.transform, PrimitiveType.Sphere, "RiderHead", new Vector3(0f, 2.34f, 0f), Vector3.one * 0.18f, Quaternion.identity, skin);
            CreatePrimitive(rider.transform, PrimitiveType.Cylinder, "RiderHeadgear", new Vector3(0f, 2.50f, 0f), new Vector3(0.20f, 0.08f, 0.20f), Quaternion.identity, dark);

            if (i == 0)
                CreatePrimitive(rider.transform, PrimitiveType.Cube, "CommanderSword", new Vector3(-0.34f, 1.62f, 0.10f), new Vector3(0.035f, 0.55f, 0.035f), Quaternion.Euler(0f, 0f, -18f), dark);
        }

        state.HqRoot = root.transform;
        state.HqBuilt = true;
        Debug.Log("HQ-09K|Unit=" + regiment.RegimentName + "|MountedGroup=3|Roles=Commander+Adjutant+Orderly");
    }

    private static void UpdateHqVisibility(UnitRenderState state)
    {
        if (state.HqRoot == null || state.Regiment == null)
            return;

        int visible = Mathf.Clamp(state.Regiment.CurrentStrength, 0, 3);
        for (int i = 0; i < state.HqRoot.childCount; i++)
            state.HqRoot.GetChild(i).gameObject.SetActive(i < visible);
    }

    private static void CreatePrimitive(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localRotation = localRotation;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
    }

    private static Material CreateInstancedMaterial(Color color, string name)
    {
        Material material = PrototypeBootstrap.CreateSharedMaterial(color, name);
        material.enableInstancing = true;
        return material;
    }

    private void BuildPrimitiveMeshes()
    {
        capsuleMesh = ExtractMesh(PrimitiveType.Capsule, "09K_CapsuleMesh");
        sphereMesh = ExtractMesh(PrimitiveType.Sphere, "09K_SphereMesh");
        cylinderMesh = ExtractMesh(PrimitiveType.Cylinder, "09K_CylinderMesh");
        cubeMesh = ExtractMesh(PrimitiveType.Cube, "09K_CubeMesh");
    }

    private static Mesh ExtractMesh(PrimitiveType type, string name)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh source = temp.GetComponent<MeshFilter>().sharedMesh;
        Mesh mesh = Object.Instantiate(source);
        mesh.name = name;
        Object.Destroy(temp);
        return mesh;
    }
}
