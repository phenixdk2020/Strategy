using System.Collections.Generic;
using UnityEngine;

public enum PrototypeSpecialArmType09K
{
    Dragoons,
    FieldArtillery
}

// Persistent identity/state bridge for Brigade HQ and later OOB Designer.
public sealed class PrototypeSpecialArmIdentity09L : MonoBehaviour
{
    public string UnitName { get; private set; }
    public BattleTeam Team { get; private set; }
    public PrototypeSpecialArmType09K Type { get; private set; }
    public int Personnel { get; private set; }
    public int HorsesAvailable { get; private set; }
    public int MountedEffective { get; private set; }
    public int GunsAuthorized { get; private set; }
    public int GunsOperational { get; private set; }
    public string ParentFormation { get; private set; }

    public void Configure(
        string unitName,
        BattleTeam team,
        PrototypeSpecialArmType09K type,
        int personnel,
        int horsesAvailable,
        int mountedEffective,
        int gunsAuthorized,
        int gunsOperational,
        string parentFormation)
    {
        UnitName = unitName;
        Team = team;
        Type = type;
        Personnel = Mathf.Max(0, personnel);
        HorsesAvailable = Mathf.Max(0, horsesAvailable);
        MountedEffective = Mathf.Clamp(mountedEffective, 0, Mathf.Min(Personnel, HorsesAvailable));
        GunsAuthorized = Mathf.Max(0, gunsAuthorized);
        GunsOperational = Mathf.Clamp(gunsOperational, 0, GunsAuthorized);
        ParentFormation = parentFormation;
    }
}

// v00.00.09l TEST - special arms visual/data pilot.
// Danish cavalry uses separate manpower/horse state and the Danish field battery is
// corrected to the current historical working-paper baseline: 8 guns / about 190 men.
// Preussian values remain explicitly QA until the matching date-specific OOB is sourced.
[DefaultExecutionOrder(-9000)]
public sealed class PrototypeSpecialArms09K : MonoBehaviour
{
    private sealed class SpecialUnit
    {
        public string Name;
        public BattleTeam Team;
        public PrototypeSpecialArmType09K Type;
        public int Strength;
        public int GunCount;
        public int HorsesAvailable;
        public int MountedEffective;
        public GameObject Root;
    }

    private readonly List<SpecialUnit> units = new List<SpecialUnit>();
    private Material danishCoat;
    private Material prussianCoat;
    private Material horseBrown;
    private Material skin;
    private Material wood;
    private Material metal;
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeSpecialArms09K>() != null)
            return;

        GameObject root = new GameObject("PrototypeSpecialArms_v000009l");
        root.AddComponent<PrototypeSpecialArms09K>();
    }

    private void Update()
    {
        if (installed || BattleManager.Instance == null)
            return;

        CreateMaterials();

        // Danish cavalry pilot is sized as a field squadron. Personnel and serviceable
        // horses are separate; only MountedEffective riders are shown mounted.
        CreateMountedSquadron(
            "Danish Gardehusar Squadron (QA)",
            BattleTeam.Denmark,
            135,
            120,
            new Vector3(-150f, 0f, -25f),
            Quaternion.Euler(0f, 90f, 0f),
            "7. Brigade (attached)");

        CreateMountedSquadron(
            "Prussian Dragoon Squadron (QA)",
            BattleTeam.Prussia,
            160,
            155,
            new Vector3(150f, 0f, -25f),
            Quaternion.Euler(0f, -90f, 0f),
            "Prussian Brigade (QA attached)");

        CreateFieldBattery(
            "Danish Field Battery (09L)",
            BattleTeam.Denmark,
            190,
            8,
            new Vector3(-150f, 0f, 25f),
            Quaternion.Euler(0f, 90f, 0f),
            "7. Brigade (attached)");

        CreateFieldBattery(
            "Prussian Field Battery (QA)",
            BattleTeam.Prussia,
            120,
            6,
            new Vector3(150f, 0f, 25f),
            Quaternion.Euler(0f, -90f, 0f),
            "Prussian Brigade (QA attached)");

        installed = true;
        Debug.Log(
            "ARMS-09L|Installed=True|DKCavalry=135Personnel/120Horses/120Mounted|" +
            "DKBattery=8Guns/190Personnel|PRCavalry=160/155MountedQA|PRBattery=6Guns/120QA|" +
            "HorseStateSeparated=True|CombatIntegration=Future");
    }

    private void CreateMaterials()
    {
        danishCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.12f, 0.22f, 0.38f), "09L_Cavalry_DK");
        prussianCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.09f, 0.13f, 0.23f), "09L_Cavalry_PR");
        horseBrown = PrototypeBootstrap.CreateSharedMaterial(new Color(0.25f, 0.14f, 0.075f), "09L_HorseBrown");
        skin = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.56f, 0.43f), "09L_Skin");
        wood = PrototypeBootstrap.CreateSharedMaterial(new Color(0.28f, 0.17f, 0.08f), "09L_ArtilleryWood");
        metal = PrototypeBootstrap.CreateSharedMaterial(new Color(0.16f, 0.17f, 0.16f), "09L_ArtilleryMetal");
    }

    private void CreateMountedSquadron(
        string unitName,
        BattleTeam team,
        int personnel,
        int horsesAvailable,
        Vector3 position,
        Quaternion rotation,
        string parentFormation)
    {
        int mountedEffective = Mathf.Min(personnel, horsesAvailable);
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject root = new GameObject(unitName);
        root.transform.position = position;
        root.transform.rotation = rotation;

        PrototypeSpecialArmIdentity09L identity = root.AddComponent<PrototypeSpecialArmIdentity09L>();
        identity.Configure(
            unitName,
            team,
            PrototypeSpecialArmType09K.Dragoons,
            personnel,
            horsesAvailable,
            mountedEffective,
            0,
            0,
            parentFormation);

        Material coat = team == BattleTeam.Denmark ? danishCoat : prussianCoat;
        const int filesAcross = 8;

        for (int i = 0; i < mountedEffective; i++)
        {
            int row = i / filesAcross;
            int file = i % filesAcross;
            float x = (file - (filesAcross - 1) * 0.5f) * 1.18f;
            float z = -row * 1.55f;

            GameObject rider = new GameObject("Mounted_" + (i + 1));
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3(x, 0f, z);

            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseBody", new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.40f, 0.86f), Quaternion.Euler(90f, 0f, 0f), horseBrown);
            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseNeck", new Vector3(0f, 1.10f, 0.50f), new Vector3(0.22f, 0.38f, 0.22f), Quaternion.Euler(-20f, 0f, 0f), horseBrown);
            CreatePart(rider.transform, PrimitiveType.Sphere, "HorseHead", new Vector3(0f, 1.40f, 0.64f), new Vector3(0.24f, 0.20f, 0.30f), Quaternion.identity, horseBrown);
            CreatePart(rider.transform, PrimitiveType.Capsule, "RiderBody", new Vector3(0f, 1.72f, 0f), new Vector3(0.25f, 0.42f, 0.22f), Quaternion.identity, coat);
            CreatePart(rider.transform, PrimitiveType.Sphere, "RiderHead", new Vector3(0f, 2.17f, 0f), Vector3.one * 0.16f, Quaternion.identity, skin);
            CreatePart(rider.transform, PrimitiveType.Cylinder, "Headgear", new Vector3(0f, 2.31f, 0f), new Vector3(0.18f, 0.075f, 0.18f), Quaternion.identity, metal);
            CreatePart(rider.transform, PrimitiveType.Cube, "Carbine", new Vector3(0.24f, 1.72f, 0.10f), new Vector3(0.035f, 0.035f, 0.62f), Quaternion.Euler(10f, 0f, 10f), wood);
        }

        // Personnel without a serviceable horse are visible as a small rear dismounted group.
        int unmounted = Mathf.Max(0, personnel - mountedEffective);
        for (int i = 0; i < unmounted; i++)
        {
            GameObject foot = new GameObject("Unmounted_" + (i + 1));
            foot.transform.SetParent(root.transform, false);
            foot.transform.localPosition = new Vector3((i % 5 - 2) * 0.72f, 0f, -mountedEffective / 8f * 1.55f - 3f - (i / 5) * 0.78f);
            CreatePart(foot.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.72f, 0f), new Vector3(0.25f, 0.46f, 0.22f), Quaternion.identity, coat);
            CreatePart(foot.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.22f, 0f), Vector3.one * 0.16f, Quaternion.identity, skin);
        }

        units.Add(new SpecialUnit
        {
            Name = unitName,
            Team = team,
            Type = PrototypeSpecialArmType09K.Dragoons,
            Strength = personnel,
            HorsesAvailable = horsesAvailable,
            MountedEffective = mountedEffective,
            Root = root
        });

        Debug.Log(
            "ARMS-09L|Unit=" + unitName +
            "|Type=Cavalry|Personnel=" + personnel +
            "|HorsesAvailable=" + horsesAvailable +
            "|MountedEffective=" + mountedEffective +
            "|Unmounted=" + unmounted +
            "|Parent=" + parentFormation);
    }

    private void CreateFieldBattery(
        string unitName,
        BattleTeam team,
        int crewStrength,
        int gunCount,
        Vector3 position,
        Quaternion rotation,
        string parentFormation)
    {
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject root = new GameObject(unitName);
        root.transform.position = position;
        root.transform.rotation = rotation;

        PrototypeSpecialArmIdentity09L identity = root.AddComponent<PrototypeSpecialArmIdentity09L>();
        identity.Configure(
            unitName,
            team,
            PrototypeSpecialArmType09K.FieldArtillery,
            crewStrength,
            0,
            0,
            gunCount,
            gunCount,
            parentFormation);

        Material coat = team == BattleTeam.Denmark ? danishCoat : prussianCoat;

        for (int gun = 0; gun < gunCount; gun++)
        {
            float x = (gun - (gunCount - 1) * 0.5f) * 5.5f;
            GameObject piece = new GameObject("Gun_" + (gun + 1));
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = new Vector3(x, 0f, 0f);

            CreatePart(piece.transform, PrimitiveType.Cylinder, "WheelL", new Vector3(-0.72f, 0.52f, 0f), new Vector3(0.52f, 0.12f, 0.52f), Quaternion.Euler(0f, 0f, 90f), wood);
            CreatePart(piece.transform, PrimitiveType.Cylinder, "WheelR", new Vector3(0.72f, 0.52f, 0f), new Vector3(0.52f, 0.12f, 0.52f), Quaternion.Euler(0f, 0f, 90f), wood);
            CreatePart(piece.transform, PrimitiveType.Cube, "Carriage", new Vector3(0f, 0.50f, -0.45f), new Vector3(0.42f, 0.18f, 1.45f), Quaternion.identity, wood);
            CreatePart(piece.transform, PrimitiveType.Cylinder, "Barrel", new Vector3(0f, 0.82f, 0.62f), new Vector3(0.12f, 1.15f, 0.12f), Quaternion.Euler(90f, 0f, 0f), metal);
        }

        int crewPerGun = Mathf.Max(1, crewStrength / gunCount);
        for (int i = 0; i < crewStrength; i++)
        {
            int gun = Mathf.Min(gunCount - 1, i / crewPerGun);
            int localIndex = i % crewPerGun;
            int row = localIndex / 5;
            int file = localIndex % 5;
            float gunX = (gun - (gunCount - 1) * 0.5f) * 5.5f;
            float x = gunX + (file - 2f) * 0.70f;
            float z = -2.6f - row * 0.72f;

            GameObject crew = new GameObject("Crew_" + (i + 1));
            crew.transform.SetParent(root.transform, false);
            crew.transform.localPosition = new Vector3(x, 0f, z);
            CreatePart(crew.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.72f, 0f), new Vector3(0.25f, 0.46f, 0.22f), Quaternion.identity, coat);
            CreatePart(crew.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.22f, 0f), Vector3.one * 0.16f, Quaternion.identity, skin);
            CreatePart(crew.transform, PrimitiveType.Cylinder, "Headgear", new Vector3(0f, 1.36f, 0f), new Vector3(0.18f, 0.07f, 0.18f), Quaternion.identity, metal);
        }

        units.Add(new SpecialUnit
        {
            Name = unitName,
            Team = team,
            Type = PrototypeSpecialArmType09K.FieldArtillery,
            Strength = crewStrength,
            GunCount = gunCount,
            Root = root
        });

        Debug.Log(
            "ARMS-09L|Unit=" + unitName +
            "|Type=FieldArtillery|Guns=" + gunCount +
            "|Personnel=" + crewStrength +
            "|Parent=" + parentFormation +
            "|Limber=Future|FireModel=Future");
    }

    private static void CreatePart(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
    }
}
