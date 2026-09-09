using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09k TEST - 1:1 infantry rendering pilot.
// One visual infantryman per current manpower without one GameObject/MonoBehaviour per man.
// Uses Graphics.DrawMeshInstanced in <=1023 instance batches. Regiment remains the sole
// movement/combat simulation owner. Existing 09i representative soldier renderer is disabled.
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
        public int LastInitialStrength;
        public RegimentFormation LastFormation;
        public float LastObservedNextFireTime;
        public float ReloadStart;
        public float ReloadEnd;
        public bool ReloadActive;
        public bool LegacyHidden;
        public bool HqBuilt;
    }

    private readonly Dictionary<Regiment, UnitRenderState> states =
        new Dictionary<Regiment, UnitRenderState>();

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
    private const float CompanyGap = 2.8f;
    private const float BattalionDepthGap = 12f;

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
        DisableLegacyVisualLayers();
    }

    private void Update()
    {
        DisableLegacyVisualLayers();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int totalRendered = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            UnitRenderState state;
            if (!states.TryGetValue(regiment, out state))
            {
                state = BuildState(regiment);
                states[regiment] = state;
            }

            if (!state.LegacyHidden)
                HideLegacySoldierRenderers(regiment, state);

            if (!state.HqBuilt)
                BuildMountedRegimentalHQ(regiment, state);

            if (state.LastInitialStrength != regiment.InitialStrength ||
                state.LastFormation != regiment.Formation)
            {
                RebuildSlots(state);
            }

            UpdateReloadState(state);
            DrawRegiment(state);
            totalRendered += Mathf.Max(0, regiment.CurrentStrength);
        }

        if (!announced && totalRendered > 0)
        {
            announced = true;
            Debug.Log(
                "SCALE-09K|Installed=True|VisualRatio=1:1|Renderer=DrawMeshInstanced|" +
                "PerSoldierGameObject=False|TotalVisible=" + totalRendered +
                "|MountedHQ=3PerRegiment|MovementWrites=False");
        }
    }

    private void DisableLegacyVisualLayers()
    {
        PrototypeSoldierVisualPass09I old09i = Object.FindAnyObjectByType<PrototypeSoldierVisualPass09I>();
        if (old09i != null && old09i.enabled)
            old09i.enabled = false;

        PrototypeSoldierVisualPass09H old09h = Object.FindAnyObjectByType<PrototypeSoldierVisualPass09H>();
        if (old09h != null && old09h.enabled)
            old09h.enabled = false;

        PrototypeReloadAnimation09J oldReload = Object.FindAnyObjectByType<PrototypeReloadAnimation09J>();
        if (oldReload != null && oldReload.enabled)
            oldReload.enabled = false;
    }

    private UnitRenderState BuildState(Regiment regiment)
    {
        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        PrototypeUniformProfile09H profile = PrototypeUniformProfile09H.CreateFactionDefault(regiment.Team);
        PrototypeSoldierVisualPass09H visual09h = PrototypeSoldierVisualPass09H.Instance;
        if (visual09h != null)
        {
            PrototypeUniformProfile09H current = visual09h.GetProfileCopy(regiment);
            if (current != null)
                profile = current;
        }

        UnitRenderState state = new UnitRenderState
        {
            Regiment = regiment,
            OOB = oob,
            Coat = CreateInstancedMaterial(profile.CoatColor, "09K_" + regiment.RegimentName + "_Coat"),
            Skin = CreateInstancedMaterial(profile.SkinColor, "09K_" + regiment.RegimentName + "_Skin"),
            Headgear = CreateInstancedMaterial(profile.HeadgearColor, "09K_" + regiment.RegimentName + "_Headgear"),
            Rifle = CreateInstancedMaterial(profile.EquipmentColor, "09K_" + regiment.RegimentName + "_Rifle"),
            LastInitialStrength = -1,
            LastFormation = (RegimentFormation)(-1)
        };

        if (nextFireTimeField != null)
            state.LastObservedNextFireTime = (float)nextFireTimeField.GetValue(regiment);

        RebuildSlots(state);
        return state;
    }

    private void RebuildSlots(UnitRenderState state)
    {
        state.LocalSlots.Clear();
        state.Phase.Clear();

        Regiment regiment = state.Regiment;
        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        state.OOB = oob;

        if (oob == null || oob.Battalions == null || oob.Battalions.Count == 0)
        {
            BuildFallbackSlots(state, regiment.InitialStrength);
        }
        else if (regiment.Formation == RegimentFormation.Column)
        {
            BuildMarchColumnSlots(state, oob);
        }
        else
        {
            BuildBattalionLineSlots(state, oob);
        }

        while (state.LocalSlots.Count < regiment.InitialStrength)
        {
            int i = state.LocalSlots.Count;
            state.LocalSlots.Add(new Vector3((i % 10 - 4.5f) * 0.65f, 0f, -34f - (i / 10) * 0.72f));
            state.Phase.Add(i * 0.73f);
        }

        if (state.LocalSlots.Count > regiment.InitialStrength)
        {
            state.LocalSlots.RemoveRange(regiment.InitialStrength, state.LocalSlots.Count - regiment.InitialStrength);
            state.Phase.RemoveRange(regiment.InitialStrength, state.Phase.Count - regiment.InitialStrength);
        }

        state.LastInitialStrength = regiment.InitialStrength;
        state.LastFormation = regiment.Formation;

        Debug.Log(
            "SCALE-09K|Unit=" + regiment.RegimentName +
            "|Strength=" + regiment.InitialStrength +
            "|Slots=" + state.LocalSlots.Count +
            "|Formation=" + regiment.Formation +
            "|VisualRatio=1:1");
    }

    private void BuildBattalionLineSlots(UnitRenderState state, PrototypeRegimentOOB09K oob)
    {
        float battalionWidth = 0f;
        if (oob.Battalions.Count > 0 && oob.Battalions[0].Companies.Count > 0)
        {
            int maxCompany = 1;
            for (int c = 0; c < oob.Battalions[0].Companies.Count; c++)
                maxCompany = Mathf.Max(maxCompany, oob.Battalions[0].Companies[c].InitialStrength);
            int files = Mathf.CeilToInt(maxCompany / (float)CompanyRanks);
            float companyWidth = files * FileSpacing;
            battalionWidth = companyWidth * 4f + CompanyGap * 3f;
        }

        for (int b = 0; b < oob.Battalions.Count; b++)
        {
            PrototypeRegimentOOB09K.BattalionState battalion = oob.Battalions[b];
            int row = b;
            float zBase = -row * BattalionDepthGap;

            for (int c = 0; c < battalion.Companies.Count; c++)
            {
                PrototypeRegimentOOB09K.CompanyState company = battalion.Companies[c];
                int files = Mathf.CeilToInt(company.InitialStrength / (float)CompanyRanks);
                float companyWidth = files * FileSpacing;
                float xStart = -battalionWidth * 0.5f + c * (companyWidth + CompanyGap) + companyWidth * 0.5f;

                for (int i = 0; i < company.InitialStrength; i++)
                {
                    int rank = i / files;
                    int file = i % files;
                    float x = xStart + (file - (files - 1) * 0.5f) * FileSpacing;
                    float z = zBase - rank * RankSpacing;
                    AddSlot(state, new Vector3(x, 0f, z));
                }
            }
        }

        // Regimental staff behind the battalion blocks. First three are represented
        // separately as mounted commander/adjutant/orderly; remaining staff are foot.
        int footStaff = Mathf.Max(0, oob.RegimentalStaffStrength - 3);
        for (int i = 0; i < footStaff; i++)
        {
            float x = (i % 11 - 5f) * 0.70f;
            float z = -oob.Battalions.Count * BattalionDepthGap - 5f - (i / 11) * 0.75f;
            AddSlot(state, new Vector3(x, 0f, z));
        }

        // Three HQ men are not drawn as infantry; reserve three slots to keep total
        // visual humans exactly equal to regiment strength once mounted HQ is added.
        for (int i = 0; i < 3; i++)
            AddSlot(state, new Vector3(10000f, -10000f, 10000f));
    }

    private void BuildMarchColumnSlots(UnitRenderState state, PrototypeRegimentOOB09K oob)
    {
        const int filesAcross = 8;
        int infantryStrength = Mathf.Max(0, state.Regiment.InitialStrength - 3);
        for (int i = 0; i < infantryStrength; i++)
        {
            int row = i / filesAcross;
            int file = i % filesAcross;
            float x = (file - (filesAcross - 1) * 0.5f) * 0.68f;
            float z = -row * 0.72f;
            AddSlot(state, new Vector3(x, 0f, z));
        }

        for (int i = 0; i < 3; i++)
            AddSlot(state, new Vector3(10000f, -10000f, 10000f));
    }

    private void BuildFallbackSlots(UnitRenderState state, int strength)
    {
        int files = Mathf.CeilToInt(strength / 3f);
        for (int i = 0; i < strength; i++)
        {
            int rank = i / files;
            int file = i % files;
            AddSlot(state, new Vector3((file - (files - 1) * 0.5f) * FileSpacing, 0f, -rank * RankSpacing));
        }
    }

    private void AddSlot(UnitRenderState state, Vector3 local)
    {
        state.LocalSlots.Add(local);
        state.Phase.Add(state.LocalSlots.Count * 0.731f);
    }

    private void DrawRegiment(UnitRenderState state)
    {
        Regiment regiment = state.Regiment;
        if (regiment == null)
            return;

        int current = Mathf.Clamp(regiment.CurrentStrength, 0, state.LocalSlots.Count);
        if (current <= 0)
            return;

        Matrix4x4 root = regiment.transform.localToWorldMatrix;
        List<Matrix4x4> body = new List<Matrix4x4>(Mathf.Min(current, BatchSize));
        List<Matrix4x4> head = new List<Matrix4x4>(Mathf.Min(current, BatchSize));
        List<Matrix4x4> hat = new List<Matrix4x4>(Mathf.Min(current, BatchSize));
        List<Matrix4x4> rifle = new List<Matrix4x4>(Mathf.Min(current, BatchSize));

        float reload01 = GetReloadProgress(state);
        bool reloading = state.ReloadActive;
        float marchTime = Time.time * 7.2f;

        int actualInfantryRendered = 0;
        for (int i = 0; i < current; i++)
        {
            Vector3 slot = state.LocalSlots[i];
            if (slot.y < -1000f)
                continue; // mounted HQ replacement slots

            float bob = 0f;
            if (regiment.Formation == RegimentFormation.Column)
                bob = Mathf.Sin(marchTime + state.Phase[i]) * 0.025f;

            Vector3 pos = slot + new Vector3(0f, bob, 0f);
            Matrix4x4 localBody = Matrix4x4.TRS(
                pos + new Vector3(0f, 0.78f, 0f),
                Quaternion.identity,
                new Vector3(0.30f, 0.62f, 0.24f));
            Matrix4x4 localHead = Matrix4x4.TRS(
                pos + new Vector3(0f, 1.43f, 0f),
                Quaternion.identity,
                Vector3.one * 0.18f);
            Matrix4x4 localHat = Matrix4x4.TRS(
                pos + new Vector3(0f, 1.58f, 0f),
                Quaternion.identity,
                regiment.Team == BattleTeam.Prussia
                    ? new Vector3(0.20f, 0.10f, 0.20f)
                    : new Vector3(0.21f, 0.075f, 0.21f));

            float stagger = Mathf.Sin(i * 1.37f) * 0.025f;
            float r = Mathf.Clamp01(reload01 + stagger);
            Quaternion rifleRot;
            Vector3 riflePos;
            if (reloading)
            {
                if (regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle)
                {
                    rifleRot = Quaternion.Euler(Mathf.Lerp(4f, 28f, Mathf.Sin(r * Mathf.PI)), 0f, 4f);
                    riflePos = pos + new Vector3(0.18f, Mathf.Lerp(0.98f, 0.84f, Mathf.Sin(r * Mathf.PI)), 0.22f);
                }
                else
                {
                    float vertical = Mathf.Sin(r * Mathf.PI);
                    rifleRot = Quaternion.Euler(Mathf.Lerp(4f, -82f, vertical), 0f, 4f);
                    riflePos = pos + new Vector3(0.16f, Mathf.Lerp(0.98f, 0.82f, vertical), 0.18f);
                }
            }
            else
            {
                rifleRot = Quaternion.Euler(4f, 0f, 5f);
                riflePos = pos + new Vector3(0.20f, 0.98f, 0.25f);
            }

            Matrix4x4 localRifle = Matrix4x4.TRS(
                riflePos,
                rifleRot,
                new Vector3(0.045f, 0.045f, 0.94f));

            body.Add(root * localBody);
            head.Add(root * localHead);
            hat.Add(root * localHat);
            rifle.Add(root * localRifle);
            actualInfantryRendered++;

            if (body.Count >= BatchSize)
            {
                FlushBatch(state, body, head, hat, rifle);
            }
        }

        if (body.Count > 0)
            FlushBatch(state, body, head, hat, rifle);
    }

    private void FlushBatch(
        UnitRenderState state,
        List<Matrix4x4> body,
        List<Matrix4x4> head,
        List<Matrix4x4> hat,
        List<Matrix4x4> rifle)
    {
        Graphics.DrawMeshInstanced(capsuleMesh, 0, state.Coat, body);
        Graphics.DrawMeshInstanced(sphereMesh, 0, state.Skin, head);
        Graphics.DrawMeshInstanced(cylinderMesh, 0, state.Headgear, hat);
        Graphics.DrawMeshInstanced(cubeMesh, 0, state.Rifle, rifle);
        body.Clear();
        head.Clear();
        hat.Clear();
        rifle.Clear();
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
        }
        state.LastObservedNextFireTime = next;

        if (state.ReloadActive && Time.time >= state.ReloadEnd)
            state.ReloadActive = false;
    }

    private float GetReloadProgress(UnitRenderState state)
    {
        if (!state.ReloadActive)
            return 1f;
        return Mathf.InverseLerp(state.ReloadStart, state.ReloadEnd, Time.time);
    }

    private void HideLegacySoldierRenderers(Regiment regiment, UnitRenderState state)
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
        state.LegacyHidden = true;
    }

    private void BuildMountedRegimentalHQ(Regiment regiment, UnitRenderState state)
    {
        Transform existing = regiment.transform.Find("RegimentalHQ09K");
        if (existing != null)
        {
            state.HqBuilt = true;
            return;
        }

        GameObject root = new GameObject("RegimentalHQ09K");
        root.transform.SetParent(regiment.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, -28f);

        Color coat = regiment.Team == BattleTeam.Denmark
            ? new Color(0.12f, 0.22f, 0.38f)
            : new Color(0.10f, 0.14f, 0.24f);
        Material coatMat = PrototypeBootstrap.CreateSharedMaterial(coat, "HQ09K_Coat_" + regiment.Team);
        Material horseMat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.23f, 0.13f, 0.07f), "HQ09K_Horse");
        Material skinMat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.56f, 0.43f), "HQ09K_Skin");
        Material darkMat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.06f, 0.06f, 0.07f), "HQ09K_Dark");

        string[] roles = { "Commander", "Adjutant", "Orderly" };
        for (int i = 0; i < 3; i++)
        {
            GameObject rider = new GameObject("HQ_" + roles[i]);
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3((i - 1) * 2.2f, 0f, i == 0 ? 0f : -1.0f);

            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "HorseBody", new Vector3(0f, 0.75f, 0f), new Vector3(0.48f, 0.42f, 0.88f), Quaternion.Euler(90f, 0f, 0f), horseMat);
            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "HorseNeck", new Vector3(0f, 1.15f, 0.54f), new Vector3(0.24f, 0.42f, 0.24f), Quaternion.Euler(-20f, 0f, 0f), horseMat);
            CreatePrimitive(rider.transform, PrimitiveType.Sphere, "HorseHead", new Vector3(0f, 1.48f, 0.70f), new Vector3(0.26f, 0.22f, 0.34f), Quaternion.identity, horseMat);
            CreatePrimitive(rider.transform, PrimitiveType.Capsule, "RiderBody", new Vector3(0f, 1.82f, 0f), new Vector3(0.28f, 0.46f, 0.24f), Quaternion.identity, coatMat);
            CreatePrimitive(rider.transform, PrimitiveType.Sphere, "RiderHead", new Vector3(0f, 2.34f, 0f), Vector3.one * 0.18f, Quaternion.identity, skinMat);
            CreatePrimitive(rider.transform, PrimitiveType.Cylinder, "RiderHeadgear", new Vector3(0f, 2.50f, 0f), new Vector3(0.20f, 0.08f, 0.20f), Quaternion.identity, darkMat);
            if (i == 0)
                CreatePrimitive(rider.transform, PrimitiveType.Cube, "CommanderSword", new Vector3(-0.34f, 1.62f, 0.10f), new Vector3(0.035f, 0.55f, 0.035f), Quaternion.Euler(0f, 0f, -18f), darkMat);
        }

        state.HqBuilt = true;
        Debug.Log("HQ-09K|Unit=" + regiment.RegimentName + "|MountedGroup=3|Roles=Commander+Adjutant+Orderly");
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
            Destroy(collider);
    }

    private Material CreateInstancedMaterial(Color color, string name)
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
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        mesh.name = name;
        Destroy(temp);
        return mesh;
    }
}
