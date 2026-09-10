using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09m - Strategy-Kamp soldier visual.
// 1:1 GPU-instanced 1864 infantry at company centres.
[DefaultExecutionOrder(10925)]
public sealed class PrototypeKampSoldierVisual09M : MonoBehaviour
{
    public static PrototypeKampSoldierVisual09M Instance { get; private set; }

    private sealed class CompanyRenderState
    {
        public PrototypeCompanyTacticalEntity09L2 Company;
        public readonly List<Vector3> Slots = new List<Vector3>();
        public readonly List<float> Phase = new List<float>();
        public int LastInitialStrength = -1;
        public RegimentFormation LastFormation = (RegimentFormation)(-1);
    }

    private sealed class Kit
    {
        public Regiment Regiment;
        public Material Coat;
        public Material CoatShadow;
        public Material Trousers;
        public Material Skin;
        public Material Headgear;
        public Material Trim;
        public Material Strap;
        public Material Pack;
        public Material Wood;
        public Material Iron;
        public Material Boot;
        public float LastObservedNextFireTime;
        public float ReloadStart;
        public float ReloadEnd;
        public bool ReloadActive;
        public float NextProfileRefresh;
        public float LastCompanyVolley;
    }

    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, CompanyRenderState> states =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, CompanyRenderState>();
    private readonly Dictionary<Regiment, Kit> kits = new Dictionary<Regiment, Kit>();
    private readonly Matrix4x4[] batchA = new Matrix4x4[1023];
    private readonly Matrix4x4[] batchB = new Matrix4x4[1023];

    private Mesh cube;
    private Mesh sphere;
    private Mesh cylinder;
    private Mesh capsule;
    private FieldInfo nextFireTimeField;
    private bool announced;
    private bool hidLegacySoldiers;

    private const float LineFileSpacing = 0.52f;
    private const float LineRankSpacing = 0.76f;
    private const int LineRanks = 3;
    private const int ColumnFiles = 8;
    private const float ColumnFileSpacing = 0.60f;
    private const float ColumnRankSpacing = 0.72f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampSoldierVisual09M>() != null)
            return;
        GameObject root = new GameObject("PrototypeKampSoldierVisual_v000009m");
        root.AddComponent<PrototypeKampSoldierVisual09M>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", BindingFlags.Instance | BindingFlags.NonPublic);
        cube = Extract(PrimitiveType.Cube, "09M_Cube");
        sphere = Extract(PrimitiveType.Sphere, "09M_Sphere");
        cylinder = Extract(PrimitiveType.Cylinder, "09M_Cylinder");
        capsule = Extract(PrimitiveType.Capsule, "09M_Capsule");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        SuppressLegacyVisuals();
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed) return;
        int visible = 0;
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null) continue;
            Kit kit = GetKit(company.ParentRegiment);
            RefreshKit(kit);
            UpdateReload(kit);
            CompanyRenderState state = GetState(company);
            if (state.LastInitialStrength != company.InitialStrength || state.LastFormation != company.Formation)
                RebuildSlots(state);
            DrawCompany(state, kit);
            visible += Mathf.Max(0, company.CurrentStrength);
        }
        if (!announced && visible > 0)
        {
            announced = true;
            Debug.Log("KAMP-VISUAL-09M|Installed=True|Figure=1864Infantry|Parts=Coat+Skirt+Belt+Legs+Boots+Arms+Hands+Head+Headgear+Pack+Straps+Box+Rifle+Bayonet|Instanced=True|VisiblePersonnel=" + visible);
        }
    }

    private void SuppressLegacyVisuals()
    {
        PrototypeCompanyRenderer09L2 old = PrototypeCompanyRenderer09L2.Instance;
        if (old != null && old.enabled) old.enabled = false;
        PrototypeSoldierVisualPass09I passI = Object.FindAnyObjectByType<PrototypeSoldierVisualPass09I>();
        if (passI != null && passI.enabled) passI.enabled = false;
        if (!hidLegacySoldiers) HideLegacyRegimentSoldiers();
    }

    private static void HideLegacyRegimentSoldiers()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return;
        int hidden = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null) continue;
            Transform root = regiment.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name.StartsWith("Soldier_"))
                {
                    child.gameObject.SetActive(false);
                    hidden++;
                }
            }
        }
        if (hidden > 0 && Instance != null) Instance.hidLegacySoldiers = true;
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

    private Kit GetKit(Regiment regiment)
    {
        if (kits.TryGetValue(regiment, out Kit kit)) return kit;
        PrototypeUniformProfile09H profile = GetProfile(regiment);
        kit = new Kit
        {
            Regiment = regiment,
            Coat = Mat(profile.CoatColor, 0.16f, "09M_Coat_" + regiment.RegimentName),
            CoatShadow = Mat(Darken(profile.CoatColor, 0.82f), 0.14f, "09M_Skirt_" + regiment.RegimentName),
            Trousers = Mat(profile.TrouserColor, 0.14f, "09M_Trousers_" + regiment.RegimentName),
            Skin = Mat(profile.SkinColor, 0.22f, "09M_Skin_" + regiment.RegimentName),
            Headgear = Mat(profile.HeadgearColor, 0.10f, "09M_Hat_" + regiment.RegimentName),
            Trim = Mat(profile.TrimColor, 0.28f, "09M_Trim_" + regiment.RegimentName),
            Strap = Mat(profile.StrapColor, 0.18f, "09M_Strap_" + regiment.RegimentName),
            Pack = Mat(profile.EquipmentColor, 0.12f, "09M_Pack_" + regiment.RegimentName),
            Wood = Mat(new Color(0.30f, 0.18f, 0.08f), 0.20f, "09M_Wood_" + regiment.RegimentName),
            Iron = Mat(new Color(0.22f, 0.22f, 0.24f), 0.48f, "09M_Iron_" + regiment.RegimentName),
            Boot = Mat(new Color(0.07f, 0.05f, 0.04f), 0.08f, "09M_Boot_" + regiment.RegimentName)
        };
        if (nextFireTimeField != null)
            kit.LastObservedNextFireTime = (float)nextFireTimeField.GetValue(regiment);
        kits[regiment] = kit;
        return kit;
    }

    private static PrototypeUniformProfile09H GetProfile(Regiment regiment)
    {
        PrototypeSoldierVisualPass09H bridge = PrototypeSoldierVisualPass09H.Instance;
        if (bridge != null)
        {
            PrototypeUniformProfile09H profile = bridge.GetProfileCopy(regiment);
            if (profile != null) return profile;
        }
        return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
    }

    private static void RefreshKit(Kit kit)
    {
        if (kit == null || kit.Regiment == null || Time.unscaledTime < kit.NextProfileRefresh) return;
        kit.NextProfileRefresh = Time.unscaledTime + 0.35f;
        PrototypeUniformProfile09H profile = GetProfile(kit.Regiment);
        kit.Coat.color = profile.CoatColor;
        kit.CoatShadow.color = Darken(profile.CoatColor, 0.82f);
        kit.Trousers.color = profile.TrouserColor;
        kit.Skin.color = profile.SkinColor;
        kit.Headgear.color = profile.HeadgearColor;
        kit.Trim.color = profile.TrimColor;
        kit.Strap.color = profile.StrapColor;
        kit.Pack.color = profile.EquipmentColor;
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

    private void DrawCompany(CompanyRenderState state, Kit kit)
    {
        PrototypeCompanyTacticalEntity09L2 company = state.Company;
        Regiment regiment = company.ParentRegiment;
        int count = Mathf.Clamp(company.CurrentStrength, 0, state.Slots.Count);
        if (count <= 0 && company.InitialStrength <= 0) return;
        bool marching = company.IsMoving;
        float reload01 = GetReloadProgress(kit);
        bool reloading = kit.ReloadActive;
        bool firing = Time.time - kit.LastCompanyVolley < 0.28f || regiment.HasHitFeedback;
        float marchTime = Time.time * 7.2f;
        Quaternion facing = company.transform.rotation;
        bool prussian = regiment.Team == BattleTeam.Prussia;
        DrawLiving(state, kit, count, marching, reloading, firing, reload01, marchTime, facing, prussian);
        int missing = Mathf.Clamp(company.InitialStrength - company.CurrentStrength, 0, state.Slots.Count - count);
        int fallen = Mathf.Min(missing, 28);
        if (fallen > 0) DrawFallen(state, kit, count, fallen, facing);
    }

    private void DrawLiving(CompanyRenderState state, Kit kit, int count, bool marching, bool reloading, bool firing, float reload01, float marchTime, Quaternion facing, bool prussian)
    {
        DrawPaired(state, count, marching, marchTime, facing, (world, bodyRot, i) =>
        {
            batchA[i] = Matrix4x4.TRS(world + Vector3.up * 0.96f, bodyRot, new Vector3(0.38f, 0.46f, 0.26f));
            batchB[i] = Matrix4x4.TRS(world + Vector3.up * 0.62f, bodyRot, new Vector3(0.42f, 0.28f, 0.28f));
        }, cube, kit.Coat, cube, kit.CoatShadow);
        DrawPaired(state, count, marching, marchTime, facing, (world, bodyRot, i) =>
        {
            batchA[i] = Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 0.74f, 0.01f), bodyRot, new Vector3(0.40f, 0.055f, 0.29f));
            batchB[i] = Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 1.16f, 0.02f), bodyRot, new Vector3(0.34f, 0.07f, 0.24f));
        }, cube, kit.Strap, cube, kit.Trim);
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 world = Grounded(state, i, marching, marchTime);
            float step = marching ? Mathf.Sin(marchTime + state.Phase[i]) : 0f;
            Quaternion leftLeg = facing * Quaternion.Euler(step * 20f, 0f, 3f);
            Quaternion rightLeg = facing * Quaternion.Euler(-step * 20f, 0f, -3f);
            batchA[n] = Matrix4x4.TRS(world + leftLeg * new Vector3(-0.10f, 0.28f, 0.00f), leftLeg, new Vector3(0.10f, 0.28f, 0.10f));
            batchB[n] = Matrix4x4.TRS(world + rightLeg * new Vector3(0.10f, 0.28f, 0.00f), rightLeg, new Vector3(0.10f, 0.28f, 0.10f));
            n++;
            if (n == 1023) { Flush(capsule, kit.Trousers, batchA, 1023); Flush(capsule, kit.Trousers, batchB, 1023); n = 0; }
        }
        if (n > 0) { Flush(capsule, kit.Trousers, batchA, n); Flush(capsule, kit.Trousers, batchB, n); }
        n = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 world = Grounded(state, i, marching, marchTime);
            float step = marching ? Mathf.Sin(marchTime + state.Phase[i]) : 0f;
            Quaternion leftLeg = facing * Quaternion.Euler(step * 20f, 0f, 3f);
            Quaternion rightLeg = facing * Quaternion.Euler(-step * 20f, 0f, -3f);
            batchA[n] = Matrix4x4.TRS(world + leftLeg * new Vector3(-0.10f, 0.07f, 0.05f), leftLeg, new Vector3(0.12f, 0.10f, 0.20f));
            batchB[n] = Matrix4x4.TRS(world + rightLeg * new Vector3(0.10f, 0.07f, 0.05f), rightLeg, new Vector3(0.12f, 0.10f, 0.20f));
            n++;
            if (n == 1023) { Flush(cube, kit.Boot, batchA, 1023); Flush(cube, kit.Boot, batchB, 1023); n = 0; }
        }
        if (n > 0) { Flush(cube, kit.Boot, batchA, n); Flush(cube, kit.Boot, batchB, n); }
        n = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 world = Grounded(state, i, marching, marchTime);
            float r = Mathf.Clamp01(reload01 + Mathf.Sin(i * 1.37f) * 0.03f);
            Quaternion leftArm;
            Quaternion rightArm;
            if (firing && !reloading)
            {
                leftArm = facing * Quaternion.Euler(-18f, 6f, 18f);
                rightArm = facing * Quaternion.Euler(-8f, -4f, -16f);
            }
            else if (reloading)
            {
                float action = Mathf.Sin(r * Mathf.PI);
                leftArm = facing * Quaternion.Euler(Mathf.Lerp(10f, -12f, action), 0f, 16f);
                rightArm = facing * Quaternion.Euler(Mathf.Lerp(8f, -72f, action), 8f, -12f);
            }
            else
            {
                float step = marching ? Mathf.Sin(marchTime + state.Phase[i]) : 0f;
                leftArm = facing * Quaternion.Euler(step * 10f + 8f, 0f, 16f);
                rightArm = facing * Quaternion.Euler(-step * 8f + 6f, 6f, -14f);
            }
            batchA[n] = Matrix4x4.TRS(world + leftArm * new Vector3(-0.22f, 1.08f, 0.02f), leftArm, new Vector3(0.075f, 0.26f, 0.075f));
            batchB[n] = Matrix4x4.TRS(world + rightArm * new Vector3(0.22f, 1.08f, 0.04f), rightArm, new Vector3(0.075f, 0.26f, 0.075f));
            n++;
            if (n == 1023) { Flush(capsule, kit.Coat, batchA, 1023); Flush(capsule, kit.Coat, batchB, 1023); n = 0; }
        }
        if (n > 0) { Flush(capsule, kit.Coat, batchA, n); Flush(capsule, kit.Coat, batchB, n); }
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + Vector3.up * 1.30f, bodyRot, new Vector3(0.08f, 0.07f, 0.08f)), cylinder, kit.Skin);
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + Vector3.up * 1.47f, bodyRot, new Vector3(0.17f, 0.20f, 0.16f)), sphere, kit.Skin);
        if (prussian)
        {
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + Vector3.up * 1.62f, bodyRot, new Vector3(0.21f, 0.13f, 0.21f)), sphere, kit.Headgear);
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + Vector3.up * 1.76f, bodyRot, new Vector3(0.035f, 0.12f, 0.035f)), cylinder, kit.Iron);
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 1.58f, 0.10f), bodyRot, new Vector3(0.16f, 0.03f, 0.08f)), cube, kit.Iron);
        }
        else
        {
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + Vector3.up * 1.61f, bodyRot, new Vector3(0.20f, 0.09f, 0.22f)), cylinder, kit.Headgear);
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 1.555f, 0.10f), bodyRot, new Vector3(0.21f, 0.025f, 0.11f)), cube, kit.Headgear);
            DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 1.60f, 0.02f), bodyRot, new Vector3(0.22f, 0.025f, 0.06f)), cube, kit.Trim);
        }
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0f, 1.00f, -0.18f), bodyRot, new Vector3(0.24f, 0.30f, 0.12f)), cube, kit.Pack);
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0.20f, 0.68f, 0.08f), bodyRot, new Vector3(0.16f, 0.14f, 0.08f)), cube, kit.Pack);
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(-0.04f, 0.98f, 0.14f), bodyRot * Quaternion.Euler(0f, 0f, -24f), new Vector3(0.05f, 0.52f, 0.025f)), cube, kit.Strap);
        DrawSingle(state, count, marching, marchTime, facing, (world, bodyRot, i) => Matrix4x4.TRS(world + bodyRot * new Vector3(0.04f, 0.98f, 0.14f), bodyRot * Quaternion.Euler(0f, 0f, 25f), new Vector3(0.045f, 0.48f, 0.024f)), cube, kit.Strap);
        n = 0; int nBarrel = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 world = Grounded(state, i, marching, marchTime);
            float r = Mathf.Clamp01(reload01 + Mathf.Sin(i * 1.37f) * 0.03f);
            Quaternion rifleRot; Vector3 rifleOff;
            if (reloading)
            {
                float action = Mathf.Sin(r * Mathf.PI);
                if (kit.Regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle)
                {
                    rifleRot = facing * Quaternion.Euler(Mathf.Lerp(6f, 26f, action), 0f, 3f);
                    rifleOff = new Vector3(0.20f, Mathf.Lerp(1.00f, 0.86f, action), 0.22f);
                }
                else
                {
                    rifleRot = facing * Quaternion.Euler(Mathf.Lerp(6f, -78f, action), 0f, 3f);
                    rifleOff = new Vector3(0.18f, Mathf.Lerp(1.00f, 0.84f, action), 0.16f);
                }
            }
            else if (firing)
            {
                rifleRot = facing * Quaternion.Euler(-2f, 0f, 2f);
                rifleOff = new Vector3(0.10f, 1.10f, 0.46f);
            }
            else
            {
                rifleRot = facing * Quaternion.Euler(6f, 0f, 4f);
                rifleOff = new Vector3(0.22f, 1.00f, 0.28f);
            }
            Vector3 riflePos = world + facing * rifleOff;
            batchA[n] = Matrix4x4.TRS(riflePos, rifleRot, new Vector3(0.045f, 0.045f, 0.78f));
            batchB[nBarrel] = Matrix4x4.TRS(riflePos + rifleRot * new Vector3(0f, 0.01f, 0.42f), rifleRot, new Vector3(0.026f, 0.026f, 0.50f));
            n++; nBarrel++;
            if (n == 1023) { Flush(cube, kit.Wood, batchA, 1023); Flush(cylinder, kit.Iron, batchB, 1023); n = 0; nBarrel = 0; }
        }
        if (n > 0) { Flush(cube, kit.Wood, batchA, n); Flush(cylinder, kit.Iron, batchB, nBarrel); }
        int nBayonet = 0;
        for (int i = 0; i < count; i++)
        {
            if (reloading) continue;
            Vector3 world = Grounded(state, i, marching, marchTime);
            Quaternion rifleRot = firing ? facing * Quaternion.Euler(-2f, 0f, 2f) : facing * Quaternion.Euler(6f, 0f, 4f);
            Vector3 rifleOff = firing ? new Vector3(0.10f, 1.10f, 0.46f) : new Vector3(0.22f, 1.00f, 0.28f);
            Vector3 riflePos = world + facing * rifleOff;
            batchA[nBayonet] = Matrix4x4.TRS(riflePos + rifleRot * new Vector3(0f, 0.01f, 0.78f), rifleRot, new Vector3(0.012f, 0.012f, 0.28f));
            nBayonet++;
            if (nBayonet == 1023) { Flush(cube, kit.Iron, batchA, 1023); nBayonet = 0; }
        }
        if (nBayonet > 0) Flush(cube, kit.Iron, batchA, nBayonet);
    }

    private void DrawFallen(CompanyRenderState state, Kit kit, int livingStart, int fallen, Quaternion facing)
    {
        int n = 0;
        Quaternion down = facing * Quaternion.Euler(88f, 12f, 0f);
        for (int k = 0; k < fallen; k++)
        {
            int slot = livingStart + k;
            if (slot >= state.Slots.Count) break;
            Vector3 world = state.Company.transform.TransformPoint(state.Slots[slot]);
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.06f;
            batchA[n] = Matrix4x4.TRS(world + Vector3.up * 0.10f, down, new Vector3(0.36f, 0.50f, 0.22f));
            batchB[n] = Matrix4x4.TRS(world + down * new Vector3(0f, 0.42f, 0f), down, Vector3.one * 0.16f);
            n++;
            if (n == 1023) { Flush(capsule, kit.CoatShadow, batchA, 1023); Flush(sphere, kit.Skin, batchB, 1023); n = 0; }
        }
        if (n > 0) { Flush(capsule, kit.CoatShadow, batchA, n); Flush(sphere, kit.Skin, batchB, n); }
    }

    private delegate void PairWriter(Vector3 world, Quaternion bodyRot, int writeIndex);
    private delegate Matrix4x4 SingleWriter(Vector3 world, Quaternion bodyRot, int sourceIndex);

    private void DrawPaired(CompanyRenderState state, int count, bool marching, float marchTime, Quaternion facing, PairWriter writer, Mesh meshA, Material matA, Mesh meshB, Material matB)
    {
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            writer(Grounded(state, i, marching, marchTime), facing, n);
            n++;
            if (n == 1023) { Flush(meshA, matA, batchA, 1023); Flush(meshB, matB, batchB, 1023); n = 0; }
        }
        if (n > 0) { Flush(meshA, matA, batchA, n); Flush(meshB, matB, batchB, n); }
    }

    private void DrawSingle(CompanyRenderState state, int count, bool marching, float marchTime, Quaternion facing, SingleWriter writer, Mesh mesh, Material material)
    {
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            batchA[n] = writer(Grounded(state, i, marching, marchTime), facing, i);
            n++;
            if (n == 1023) { Flush(mesh, material, batchA, 1023); n = 0; }
        }
        if (n > 0) Flush(mesh, material, batchA, n);
    }

    private Vector3 Grounded(CompanyRenderState state, int i, bool marching, float marchTime)
    {
        Vector3 world = state.Company.transform.TransformPoint(state.Slots[i]);
        world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.02f;
        if (marching) world.y += Mathf.Sin(marchTime + state.Phase[i]) * 0.028f;
        return world;
    }

    private void UpdateReload(Kit kit)
    {
        if (kit == null || kit.Regiment == null) return;
        PrototypeKampCompanyFire09M fire = PrototypeKampCompanyFire09M.Instance;
        if (fire != null && fire.TryGetLastVolley(kit.Regiment, out float volleyTime))
            kit.LastCompanyVolley = volleyTime;
        if (fire != null && fire.TryGetReloadWindow(kit.Regiment, out float start, out float end))
        {
            if (end > kit.LastObservedNextFireTime + 0.05f)
            {
                kit.ReloadStart = start;
                kit.ReloadEnd = end;
                kit.ReloadActive = Time.time < end;
                kit.LastObservedNextFireTime = end;
            }
        }
        else if (nextFireTimeField != null)
        {
            float next = (float)nextFireTimeField.GetValue(kit.Regiment);
            if (next > Time.time + 0.05f && next > kit.LastObservedNextFireTime + 0.10f)
            {
                kit.ReloadStart = Time.time;
                kit.ReloadEnd = next;
                kit.ReloadActive = true;
            }
            kit.LastObservedNextFireTime = next;
        }
        if (kit.ReloadActive && Time.time >= kit.ReloadEnd) kit.ReloadActive = false;
    }

    private static float GetReloadProgress(Kit kit)
    {
        if (kit == null || !kit.ReloadActive) return 1f;
        return Mathf.InverseLerp(kit.ReloadStart, kit.ReloadEnd, Time.time);
    }

    private static void Flush(Mesh mesh, Material material, Matrix4x4[] batch, int count)
    {
        if (count <= 0 || mesh == null || material == null) return;
        Graphics.DrawMeshInstanced(mesh, 0, material, batch, count);
    }

    private static Material Mat(Color color, float smoothness, string name)
    {
        Material material = PrototypeBootstrap.CreateSharedMaterial(color, name);
        material.enableInstancing = true;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Color Darken(Color color, float factor)
    {
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }

    private static Mesh Extract(PrimitiveType type, string name)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh source = temp.GetComponent<MeshFilter>().sharedMesh;
        Mesh mesh = Object.Instantiate(source);
        mesh.name = name;
        Object.Destroy(temp);
        return mesh;
    }
}
