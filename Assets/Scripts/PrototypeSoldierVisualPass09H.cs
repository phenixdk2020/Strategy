using System.Collections.Generic;
using UnityEngine;

// v00.00.09h TEST soldier visual pass.
// Presentation-only: upgrades the existing representative infantry visuals and gives
// every regiment an independent colour profile. It never writes Regiment movement,
// formation, combat, morale, cohesion or Officer AI state.
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
        public readonly List<Renderer> CoatRenderers = new List<Renderer>();
        public readonly List<Renderer> TrouserRenderers = new List<Renderer>();
        public readonly List<Renderer> HeadgearRenderers = new List<Renderer>();
        public readonly List<Renderer> TrimRenderers = new List<Renderer>();
        public readonly List<Renderer> StrapRenderers = new List<Renderer>();
        public readonly List<Renderer> EquipmentRenderers = new List<Renderer>();
        public readonly List<Renderer> SkinRenderers = new List<Renderer>();
        public bool StandardApplied;
    }

    private readonly Dictionary<Regiment, UnitVisual> units =
        new Dictionary<Regiment, UnitVisual>();

    private float nextStandardRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeSoldierVisualPass09H>() != null)
            return;

        GameObject root = new GameObject("PrototypeSoldierVisualPass_v000009h");
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
                    ApplyStandardColors(pair.Key, pair.Value);
            }
        }

        CleanupDestroyedRegiments();
    }

    public PrototypeUniformProfile09H GetProfile(Regiment regiment)
    {
        UnitVisual visual;
        if (regiment == null || !units.TryGetValue(regiment, out visual))
            return regiment == null ? null : PrototypeUniformProfile09H.CreateRegimentDefault(regiment);

        return visual.Profile;
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
        visual.StandardApplied = false;
        ApplyStandardColors(regiment, visual);
    }

    public void SaveProfile(Regiment regiment)
    {
        PrototypeUniformProfile09H profile = GetProfile(regiment);
        if (regiment == null || profile == null)
            return;

        PlayerPrefs.SetString(GetSaveKey(regiment), JsonUtility.ToJson(profile));
        PlayerPrefs.Save();
        Debug.Log("UNIFORM-09H|Unit=" + regiment.RegimentName + "|Action=Save|Success=True");
    }

    public bool LoadSavedProfile(Regiment regiment)
    {
        if (regiment == null)
            return false;

        string key = GetSaveKey(regiment);
        if (!PlayerPrefs.HasKey(key))
            return false;

        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        PrototypeUniformProfile09H profile =
            JsonUtility.FromJson<PrototypeUniformProfile09H>(json);
        if (profile == null)
            return false;

        ApplyProfile(regiment, profile);
        Debug.Log("UNIFORM-09H|Unit=" + regiment.RegimentName + "|Action=Load|Success=True");
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
        UnitVisual visual = new UnitVisual();
        visual.Profile = LoadOrCreateProfile(regiment);
        CreateMaterials(regiment, visual);

        int soldierIndex = 0;
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform soldier = regiment.transform.GetChild(i);
            if (soldier == null || !soldier.name.StartsWith("Soldier_"))
                continue;

            UpgradeSoldier(regiment, soldier, soldierIndex, visual);
            soldierIndex++;
        }

        ApplyMaterialColors(visual);
        units[regiment] = visual;
        ApplyStandardColors(regiment, visual);

        Debug.Log(
            "SOLDIER-09H|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Representatives=" + soldierIndex +
            "|Officer=True|StandardBearer=True|UniformProfile=True|MovementWrites=False");
    }

    private static PrototypeUniformProfile09H LoadOrCreateProfile(Regiment regiment)
    {
        string key = GetSaveKey(regiment);
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                PrototypeUniformProfile09H saved =
                    JsonUtility.FromJson<PrototypeUniformProfile09H>(json);
                if (saved != null)
                    return saved;
            }
        }

        return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
    }

    private static string GetSaveKey(Regiment regiment)
    {
        return "PROJECT1864_UNIFORM_09H_" + regiment.Team + "_" + regiment.RegimentName;
    }

    private static void CreateMaterials(Regiment regiment, UnitVisual visual)
    {
        string prefix = "09H_" + regiment.RegimentName.Replace(" ", "_") + "_";
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
        Transform marker = soldier.Find("Visual09H");
        if (marker != null)
        {
            RegisterExistingRenderers(marker, visual);
            RegisterLegacyRenderers(soldier, visual);
            return;
        }

        GameObject markerObject = new GameObject("Visual09H");
        markerObject.transform.SetParent(soldier, false);
        marker = markerObject.transform;

        Transform legacyBody = soldier.childCount > 1 ? soldier.GetChild(0) : null;
        Transform legacyRifle = soldier.childCount > 2 ? soldier.GetChild(1) : null;
        Transform legacyHeadgear = soldier.childCount > 3 ? soldier.GetChild(2) : null;

        if (legacyBody != null && legacyBody != marker)
        {
            legacyBody.name = "Body_Coat";
            legacyBody.localScale = new Vector3(0.25f, 0.46f, 0.23f);
            legacyBody.localPosition = new Vector3(0f, 0.73f, 0f);
            AddRenderer(legacyBody, visual.Coat, visual.CoatRenderers);
        }

        if (legacyRifle != null && legacyRifle != marker)
        {
            legacyRifle.name = "Rifle";
            legacyRifle.localScale = new Vector3(0.045f, 0.045f, 0.92f);
            legacyRifle.localPosition = new Vector3(0.22f, 0.78f, 0.14f);
            legacyRifle.localRotation = Quaternion.Euler(8f, 0f, 5f);
            AddRenderer(legacyRifle, visual.Equipment, visual.EquipmentRenderers);
        }

        if (legacyHeadgear != null && legacyHeadgear != marker)
        {
            legacyHeadgear.name = "Headgear";
            legacyHeadgear.localPosition = new Vector3(0f, 1.37f, 0f);
            legacyHeadgear.localScale = regiment.Team == BattleTeam.Denmark
                ? new Vector3(0.19f, 0.085f, 0.19f)
                : new Vector3(0.20f, 0.075f, 0.20f);
            AddRenderer(legacyHeadgear, visual.Headgear, visual.HeadgearRenderers);
        }

        CreatePart(
            marker,
            PrimitiveType.Sphere,
            "Head_Skin",
            new Vector3(0f, 1.20f, 0f),
            new Vector3(0.18f, 0.20f, 0.18f),
            Quaternion.identity,
            visual.Skin,
            visual.SkinRenderers);

        CreatePart(
            marker,
            PrimitiveType.Cylinder,
            "Leg_Left",
            new Vector3(-0.095f, 0.28f, 0f),
            new Vector3(0.065f, 0.28f, 0.065f),
            Quaternion.identity,
            visual.Trousers,
            visual.TrouserRenderers);
        CreatePart(
            marker,
            PrimitiveType.Cylinder,
            "Leg_Right",
            new Vector3(0.095f, 0.28f, 0f),
            new Vector3(0.065f, 0.28f, 0.065f),
            Quaternion.identity,
            visual.Trousers,
            visual.TrouserRenderers);

        CreatePart(
            marker,
            PrimitiveType.Cube,
            "Belt_Straps",
            new Vector3(0f, 0.69f, -0.01f),
            new Vector3(0.32f, 0.055f, 0.25f),
            Quaternion.identity,
            visual.Straps,
            visual.StrapRenderers);

        CreatePart(
            marker,
            PrimitiveType.Cube,
            "Pack_Equipment",
            new Vector3(0f, 0.79f, -0.20f),
            new Vector3(0.27f, 0.30f, 0.11f),
            Quaternion.identity,
            visual.Equipment,
            visual.EquipmentRenderers);

        // Small bayonet gives the rifle a readable silhouette without adding a
        // projectile/combat dependency.
        CreatePart(
            marker,
            PrimitiveType.Cube,
            "Bayonet",
            new Vector3(0.22f, 0.86f, 0.67f),
            new Vector3(0.018f, 0.018f, 0.22f),
            Quaternion.Euler(8f, 0f, 5f),
            visual.Straps,
            visual.StrapRenderers);

        if (regiment.Team == BattleTeam.Prussia)
        {
            CreatePart(
                marker,
                PrimitiveType.Cylinder,
                "Headgear_Spike",
                new Vector3(0f, 1.52f, 0f),
                new Vector3(0.035f, 0.095f, 0.035f),
                Quaternion.identity,
                visual.Headgear,
                visual.HeadgearRenderers);
        }
        else
        {
            CreatePart(
                marker,
                PrimitiveType.Cube,
                "Headgear_Brim",
                new Vector3(0f, 1.37f, 0.105f),
                new Vector3(0.22f, 0.025f, 0.12f),
                Quaternion.identity,
                visual.Headgear,
                visual.HeadgearRenderers);
        }

        if (soldierIndex == 0)
            AddOfficerDetails(marker, visual);
        else if (soldierIndex == 1)
            AddStandardBearerDetails(marker, visual);
    }

    private static void AddOfficerDetails(Transform marker, UnitVisual visual)
    {
        CreatePart(
            marker,
            PrimitiveType.Cube,
            "Officer_Sash",
            new Vector3(0f, 0.82f, 0.20f),
            new Vector3(0.34f, 0.055f, 0.025f),
            Quaternion.Euler(0f, 0f, -28f),
            visual.Trim,
            visual.TrimRenderers);

        CreatePart(
            marker,
            PrimitiveType.Cube,
            "Officer_Sword",
            new Vector3(-0.28f, 0.56f, 0.02f),
            new Vector3(0.025f, 0.48f, 0.025f),
            Quaternion.Euler(0f, 0f, -13f),
            visual.Straps,
            visual.StrapRenderers);
    }

    private static void AddStandardBearerDetails(Transform marker, UnitVisual visual)
    {
        CreatePart(
            marker,
            PrimitiveType.Cube,
            "StandardBearer_Sash",
            new Vector3(0f, 0.82f, 0.20f),
            new Vector3(0.34f, 0.05f, 0.025f),
            Quaternion.Euler(0f, 0f, 28f),
            visual.Trim,
            visual.TrimRenderers);
    }

    private static Renderer CreatePart(
        Transform parent,
        PrimitiveType primitive,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material,
        List<Renderer> bucket)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            bucket.Add(renderer);
        }

        return renderer;
    }

    private static void RegisterLegacyRenderers(Transform soldier, UnitVisual visual)
    {
        for (int i = 0; i < soldier.childCount; i++)
        {
            Transform child = soldier.GetChild(i);
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            if (child.name == "Body_Coat")
                AddRenderer(child, visual.Coat, visual.CoatRenderers);
            else if (child.name == "Headgear")
                AddRenderer(child, visual.Headgear, visual.HeadgearRenderers);
            else if (child.name == "Rifle")
                AddRenderer(child, visual.Equipment, visual.EquipmentRenderers);
        }
    }

    private static void RegisterExistingRenderers(Transform marker, UnitVisual visual)
    {
        Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string name = renderer.gameObject.name;

            if (name.Contains("Leg"))
                Assign(renderer, visual.Trousers, visual.TrouserRenderers);
            else if (name.Contains("Head_Skin"))
                Assign(renderer, visual.Skin, visual.SkinRenderers);
            else if (name.Contains("Headgear"))
                Assign(renderer, visual.Headgear, visual.HeadgearRenderers);
            else if (name.Contains("Trim") || name.Contains("Sash"))
                Assign(renderer, visual.Trim, visual.TrimRenderers);
            else if (name.Contains("Belt") || name.Contains("Bayonet") || name.Contains("Sword"))
                Assign(renderer, visual.Straps, visual.StrapRenderers);
            else
                Assign(renderer, visual.Equipment, visual.EquipmentRenderers);
        }
    }

    private static void AddRenderer(Transform transform, Material material, List<Renderer> bucket)
    {
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
            Assign(renderer, material, bucket);
    }

    private static void Assign(Renderer renderer, Material material, List<Renderer> bucket)
    {
        renderer.sharedMaterial = material;
        if (!bucket.Contains(renderer))
            bucket.Add(renderer);
    }

    private static void ApplyMaterialColors(UnitVisual visual)
    {
        visual.Coat.color = visual.Profile.CoatColor;
        visual.Trousers.color = visual.Profile.TrouserColor;
        visual.Headgear.color = visual.Profile.HeadgearColor;
        visual.Trim.color = visual.Profile.TrimColor;
        visual.Straps.color = visual.Profile.StrapColor;
        visual.Equipment.color = visual.Profile.EquipmentColor;
        visual.Skin.color = visual.Profile.SkinColor;
    }

    private static void ApplyStandardColors(Regiment regiment, UnitVisual visual)
    {
        Transform standard = regiment.transform.Find("RegimentalStandard_" + regiment.RegimentName);
        if (standard == null)
            return;

        SetChildColor(standard, "StandardCloth", visual.Profile.FlagPrimaryColor);
        SetChildColor(standard, "StandardClothFold", visual.Profile.FlagPrimaryColor);
        SetChildColor(standard, "DannebrogVertical", visual.Profile.FlagSecondaryColor);
        SetChildColor(standard, "DannebrogHorizontal", visual.Profile.FlagSecondaryColor);
        SetChildColor(standard, "PrussianDeviceVertical", visual.Profile.FlagSecondaryColor);
        SetChildColor(standard, "PrussianDeviceHorizontal", visual.Profile.FlagSecondaryColor);
        SetChildColor(standard, "RegimentalRibbon", visual.Profile.RibbonColor);
        visual.StandardApplied = true;
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

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, UnitVisual> pair in units)
        {
            if (pair.Key != null)
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            units.Remove(remove[i]);
    }
}
