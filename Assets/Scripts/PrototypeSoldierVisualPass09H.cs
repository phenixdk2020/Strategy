using System.Collections.Generic;
using UnityEngine;

// v00.00.09h1 TEST soldier visual pass.
// Presentation-only: upgrades representative infantry visuals and gives every
// regiment an independent colour profile. No movement/combat/AI state is written.
// 09h1 compile fix: profile persistence uses PlayerPrefs floats only; no JsonUtility.
[DefaultExecutionOrder(10000)]
public sealed class PrototypeSoldierVisualPass09H : MonoBehaviour
{
    public static PrototypeSoldierVisualPass09H Instance { get; private set; }

    private sealed class UnitVisual
    {
        public PrototypeUniformProfile09H Profile;
        public Material Coat;
        public Material Trousers;
        public Material Headgear;
        public Material Trim;
        public Material Straps;
        public Material Equipment;
        public Material Skin;
    }

    private readonly Dictionary<Regiment, UnitVisual> units =
        new Dictionary<Regiment, UnitVisual>();

    private float nextStandardRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeSoldierVisualPass09H>() != null)
            return;

        GameObject root = new GameObject("PrototypeSoldierVisualPass_v000009h1");
        root.AddComponent<PrototypeSoldierVisualPass09H>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            if (!units.ContainsKey(regiment))
                InstallRegiment(regiment);
        }

        if (Time.unscaledTime >= nextStandardRefresh)
        {
            nextStandardRefresh = Time.unscaledTime + 0.50f;
            foreach (KeyValuePair<Regiment, UnitVisual> pair in units)
            {
                if (pair.Key != null)
                    ApplyStandardColors(pair.Key, pair.Value.Profile);
            }
        }

        CleanupDestroyedRegiments();
    }

    public PrototypeUniformProfile09H GetProfile(Regiment regiment)
    {
        if (regiment == null)
            return null;

        UnitVisual visual;
        if (units.TryGetValue(regiment, out visual))
            return visual.Profile;

        return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
    }

    public PrototypeUniformProfile09H GetProfileCopy(Regiment regiment)
    {
        PrototypeUniformProfile09H profile = GetProfile(regiment);
        return profile != null ? profile.Clone() : null;
    }

    public void ApplyProfile(Regiment regiment, PrototypeUniformProfile09H profile)
    {
        if (regiment == null || profile == null)
            return;

        UnitVisual visual;
        if (!units.TryGetValue(regiment, out visual))
        {
            InstallRegiment(regiment);
            if (!units.TryGetValue(regiment, out visual))
                return;
        }

        visual.Profile = profile.Clone();
        ApplyMaterialColors(visual);
        ApplyStandardColors(regiment, visual.Profile);
    }

    public void SaveProfile(Regiment regiment)
    {
        if (regiment == null)
            return;

        PrototypeUniformProfile09H profile = GetProfile(regiment);
        if (profile == null)
            return;

        string key = GetSaveKey(regiment);
        SaveColor(key, "Coat", profile.CoatColor);
        SaveColor(key, "Trousers", profile.TrouserColor);
        SaveColor(key, "Headgear", profile.HeadgearColor);
        SaveColor(key, "Trim", profile.TrimColor);
        SaveColor(key, "Straps", profile.StrapColor);
        SaveColor(key, "Equipment", profile.EquipmentColor);
        SaveColor(key, "Skin", profile.SkinColor);
        SaveColor(key, "FlagPrimary", profile.FlagPrimaryColor);
        SaveColor(key, "FlagSecondary", profile.FlagSecondaryColor);
        SaveColor(key, "Ribbon", profile.RibbonColor);
        PlayerPrefs.SetInt(key + "_Saved", 1);
        PlayerPrefs.Save();

        Debug.Log(
            "UNIFORM-09H1|Unit=" + regiment.RegimentName +
            "|Action=Save|Storage=PlayerPrefsFloat|Success=True");
    }

    public bool LoadSavedProfile(Regiment regiment)
    {
        if (regiment == null)
            return false;

        string key = GetSaveKey(regiment);
        if (PlayerPrefs.GetInt(key + "_Saved", 0) != 1)
            return false;

        PrototypeUniformProfile09H fallback =
            PrototypeUniformProfile09H.CreateRegimentDefault(regiment);

        PrototypeUniformProfile09H profile = new PrototypeUniformProfile09H
        {
            CoatColor = LoadColor(key, "Coat", fallback.CoatColor),
            TrouserColor = LoadColor(key, "Trousers", fallback.TrouserColor),
            HeadgearColor = LoadColor(key, "Headgear", fallback.HeadgearColor),
            TrimColor = LoadColor(key, "Trim", fallback.TrimColor),
            StrapColor = LoadColor(key, "Straps", fallback.StrapColor),
            EquipmentColor = LoadColor(key, "Equipment", fallback.EquipmentColor),
            SkinColor = LoadColor(key, "Skin", fallback.SkinColor),
            FlagPrimaryColor = LoadColor(key, "FlagPrimary", fallback.FlagPrimaryColor),
            FlagSecondaryColor = LoadColor(key, "FlagSecondary", fallback.FlagSecondaryColor),
            RibbonColor = LoadColor(key, "Ribbon", fallback.RibbonColor)
        };

        ApplyProfile(regiment, profile);

        Debug.Log(
            "UNIFORM-09H1|Unit=" + regiment.RegimentName +
            "|Action=Load|Storage=PlayerPrefsFloat|Success=True");
        return true;
    }

    public void ResetRegimentDefault(Regiment regiment)
    {
        if (regiment == null)
            return;

        ApplyProfile(regiment, PrototypeUniformProfile09H.CreateRegimentDefault(regiment));
    }

    public void ResetFactionDefault(Regiment regiment)
    {
        if (regiment == null)
            return;

        ApplyProfile(regiment, PrototypeUniformProfile09H.CreateFactionDefault(regiment.Team));
    }

    private void InstallRegiment(Regiment regiment)
    {
        if (regiment == null || units.ContainsKey(regiment))
            return;

        UnitVisual visual = new UnitVisual
        {
            Profile = LoadOrCreateProfile(regiment)
        };

        CreateMaterials(regiment, visual);

        int soldierIndex = 0;
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("Soldier_"))
                continue;

            UpgradeSoldier(regiment, child, soldierIndex, visual);
            soldierIndex++;
        }

        ApplyMaterialColors(visual);
        units[regiment] = visual;
        ApplyStandardColors(regiment, visual.Profile);

        Debug.Log(
            "SOLDIER-09H1|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Representatives=" + soldierIndex +
            "|Officer=True|StandardBearer=True|UniformProfile=True|MovementWrites=False");
    }

    private static PrototypeUniformProfile09H LoadOrCreateProfile(Regiment regiment)
    {
        string key = GetSaveKey(regiment);
        if (PlayerPrefs.GetInt(key + "_Saved", 0) != 1)
            return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);

        PrototypeUniformProfile09H fallback =
            PrototypeUniformProfile09H.CreateRegimentDefault(regiment);

        return new PrototypeUniformProfile09H
        {
            CoatColor = LoadColor(key, "Coat", fallback.CoatColor),
            TrouserColor = LoadColor(key, "Trousers", fallback.TrouserColor),
            HeadgearColor = LoadColor(key, "Headgear", fallback.HeadgearColor),
            TrimColor = LoadColor(key, "Trim", fallback.TrimColor),
            StrapColor = LoadColor(key, "Straps", fallback.StrapColor),
            EquipmentColor = LoadColor(key, "Equipment", fallback.EquipmentColor),
            SkinColor = LoadColor(key, "Skin", fallback.SkinColor),
            FlagPrimaryColor = LoadColor(key, "FlagPrimary", fallback.FlagPrimaryColor),
            FlagSecondaryColor = LoadColor(key, "FlagSecondary", fallback.FlagSecondaryColor),
            RibbonColor = LoadColor(key, "Ribbon", fallback.RibbonColor)
        };
    }

    private static string GetSaveKey(Regiment regiment)
    {
        return "PROJECT1864_UNIFORM_09H_" + regiment.Team + "_" + regiment.RegimentName;
    }

    private static void SaveColor(string key, string part, Color color)
    {
        string prefix = key + "_" + part;
        PlayerPrefs.SetFloat(prefix + "_R", color.r);
        PlayerPrefs.SetFloat(prefix + "_G", color.g);
        PlayerPrefs.SetFloat(prefix + "_B", color.b);
    }

    private static Color LoadColor(string key, string part, Color fallback)
    {
        string prefix = key + "_" + part;
        return new Color(
            PlayerPrefs.GetFloat(prefix + "_R", fallback.r),
            PlayerPrefs.GetFloat(prefix + "_G", fallback.g),
            PlayerPrefs.GetFloat(prefix + "_B", fallback.b),
            1f);
    }

    private static void CreateMaterials(Regiment regiment, UnitVisual visual)
    {
        string safeName = string.IsNullOrEmpty(regiment.RegimentName)
            ? "Unit"
            : regiment.RegimentName.Replace(" ", "_");
        string prefix = "09H1_" + safeName + "_";

        visual.Coat = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.CoatColor, prefix + "Coat");
        visual.Trousers = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.TrouserColor, prefix + "Trousers");
        visual.Headgear = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.HeadgearColor, prefix + "Headgear");
        visual.Trim = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.TrimColor, prefix + "Trim");
        visual.Straps = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.StrapColor, prefix + "Straps");
        visual.Equipment = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.EquipmentColor, prefix + "Equipment");
        visual.Skin = PrototypeBootstrap.CreateSharedMaterial(visual.Profile.SkinColor, prefix + "Skin");
    }

    private static void UpgradeSoldier(
        Regiment regiment,
        Transform soldier,
        int soldierIndex,
        UnitVisual visual)
    {
        if (soldier.Find("Visual09H") != null)
            return;

        GameObject visualRootObject = new GameObject("Visual09H");
        visualRootObject.transform.SetParent(soldier, false);
        Transform visualRoot = visualRootObject.transform;

        Transform legacyBody = FindLegacyChild(soldier, 0);
        Transform legacyRifle = FindLegacyChild(soldier, 1);
        Transform legacyHeadgear = FindLegacyChild(soldier, 2);

        if (legacyBody != null)
        {
            legacyBody.name = "Body_Coat";
            legacyBody.localScale = new Vector3(0.25f, 0.46f, 0.23f);
            legacyBody.localPosition = new Vector3(0f, 0.73f, 0f);
            SetRendererMaterial(legacyBody, visual.Coat);
        }

        if (legacyRifle != null)
        {
            legacyRifle.name = "Rifle";
            legacyRifle.localScale = new Vector3(0.045f, 0.045f, 0.92f);
            legacyRifle.localPosition = new Vector3(0.22f, 0.78f, 0.14f);
            legacyRifle.localRotation = Quaternion.Euler(8f, 0f, 5f);
            SetRendererMaterial(legacyRifle, visual.Equipment);
        }

        if (legacyHeadgear != null)
        {
            legacyHeadgear.name = "Headgear";
            legacyHeadgear.localPosition = new Vector3(0f, 1.37f, 0f);
            legacyHeadgear.localScale = regiment.Team == BattleTeam.Denmark
                ? new Vector3(0.19f, 0.085f, 0.19f)
                : new Vector3(0.20f, 0.075f, 0.20f);
            SetRendererMaterial(legacyHeadgear, visual.Headgear);
        }

        CreatePart(visualRoot, PrimitiveType.Sphere, "Head_Skin",
            new Vector3(0f, 1.20f, 0f), new Vector3(0.18f, 0.20f, 0.18f),
            Quaternion.identity, visual.Skin);

        CreatePart(visualRoot, PrimitiveType.Cylinder, "Leg_Left",
            new Vector3(-0.095f, 0.28f, 0f), new Vector3(0.065f, 0.28f, 0.065f),
            Quaternion.identity, visual.Trousers);
        CreatePart(visualRoot, PrimitiveType.Cylinder, "Leg_Right",
            new Vector3(0.095f, 0.28f, 0f), new Vector3(0.065f, 0.28f, 0.065f),
            Quaternion.identity, visual.Trousers);

        CreatePart(visualRoot, PrimitiveType.Cube, "Belt_Straps",
            new Vector3(0f, 0.69f, -0.01f), new Vector3(0.32f, 0.055f, 0.25f),
            Quaternion.identity, visual.Straps);

        CreatePart(visualRoot, PrimitiveType.Cube, "Pack_Equipment",
            new Vector3(0f, 0.79f, -0.20f), new Vector3(0.27f, 0.30f, 0.11f),
            Quaternion.identity, visual.Equipment);

        CreatePart(visualRoot, PrimitiveType.Cube, "Bayonet",
            new Vector3(0.22f, 0.86f, 0.67f), new Vector3(0.018f, 0.018f, 0.22f),
            Quaternion.Euler(8f, 0f, 5f), visual.Straps);

        if (regiment.Team == BattleTeam.Prussia)
        {
            CreatePart(visualRoot, PrimitiveType.Cylinder, "Headgear_Spike",
                new Vector3(0f, 1.52f, 0f), new Vector3(0.035f, 0.095f, 0.035f),
                Quaternion.identity, visual.Headgear);
        }
        else
        {
            CreatePart(visualRoot, PrimitiveType.Cube, "Headgear_Brim",
                new Vector3(0f, 1.37f, 0.105f), new Vector3(0.22f, 0.025f, 0.12f),
                Quaternion.identity, visual.Headgear);
        }

        if (soldierIndex == 0)
        {
            CreatePart(visualRoot, PrimitiveType.Cube, "Officer_Sash",
                new Vector3(0f, 0.82f, 0.20f), new Vector3(0.34f, 0.055f, 0.025f),
                Quaternion.Euler(0f, 0f, -28f), visual.Trim);
            CreatePart(visualRoot, PrimitiveType.Cube, "Officer_Sword",
                new Vector3(-0.28f, 0.56f, 0.02f), new Vector3(0.025f, 0.48f, 0.025f),
                Quaternion.Euler(0f, 0f, -13f), visual.Straps);
        }
        else if (soldierIndex == 1)
        {
            CreatePart(visualRoot, PrimitiveType.Cube, "StandardBearer_Sash",
                new Vector3(0f, 0.82f, 0.20f), new Vector3(0.34f, 0.05f, 0.025f),
                Quaternion.Euler(0f, 0f, 28f), visual.Trim);
        }
    }

    private static Transform FindLegacyChild(Transform soldier, int index)
    {
        if (soldier == null || index < 0 || index >= soldier.childCount)
            return null;

        Transform child = soldier.GetChild(index);
        if (child != null && child.name == "Visual09H")
            return null;
        return child;
    }

    private static void CreatePart(
        Transform parent,
        PrimitiveType primitive,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void SetRendererMaterial(Transform transform, Material material)
    {
        if (transform == null)
            return;

        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void ApplyMaterialColors(UnitVisual visual)
    {
        if (visual == null || visual.Profile == null)
            return;

        if (visual.Coat != null) visual.Coat.color = visual.Profile.CoatColor;
        if (visual.Trousers != null) visual.Trousers.color = visual.Profile.TrouserColor;
        if (visual.Headgear != null) visual.Headgear.color = visual.Profile.HeadgearColor;
        if (visual.Trim != null) visual.Trim.color = visual.Profile.TrimColor;
        if (visual.Straps != null) visual.Straps.color = visual.Profile.StrapColor;
        if (visual.Equipment != null) visual.Equipment.color = visual.Profile.EquipmentColor;
        if (visual.Skin != null) visual.Skin.color = visual.Profile.SkinColor;
    }

    private static void ApplyStandardColors(
        Regiment regiment,
        PrototypeUniformProfile09H profile)
    {
        if (regiment == null || profile == null)
            return;

        Transform standard = regiment.transform.Find(
            "RegimentalStandard_" + regiment.RegimentName);
        if (standard == null)
            return;

        SetChildColor(standard, "StandardCloth", profile.FlagPrimaryColor);
        SetChildColor(standard, "StandardClothFold", profile.FlagPrimaryColor);
        SetChildColor(standard, "DannebrogVertical", profile.FlagSecondaryColor);
        SetChildColor(standard, "DannebrogHorizontal", profile.FlagSecondaryColor);
        SetChildColor(standard, "PrussianDeviceVertical", profile.FlagSecondaryColor);
        SetChildColor(standard, "PrussianDeviceHorizontal", profile.FlagSecondaryColor);
        SetChildColor(standard, "RegimentalRibbon", profile.RibbonColor);
    }

    private static void SetChildColor(Transform parent, string childName, Color color)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            return;

        Renderer renderer = child.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
            renderer.sharedMaterial.color = color;
    }

    private void CleanupDestroyedRegiments()
    {
        if (units.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, UnitVisual> pair in units)
        {
            if (pair.Key != null)
                continue;

            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
            units.Remove(stale[i]);
    }
}
