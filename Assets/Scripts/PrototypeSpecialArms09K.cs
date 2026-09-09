using System.Collections.Generic;
using UnityEngine;

public enum PrototypeSpecialArmType09K
{
    Dragoons,
    FieldArtillery
}

// v00.00.09k TEST - first special-arms visual/data pilot.
// Adds one dragoon squadron and one six-gun field battery per side. These units are
// deliberately not connected to the infantry Regiment combat model yet; they establish
// scale, silhouettes and OOB presence without pretending artillery/cavalry are reskinned infantry.
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

        GameObject root = new GameObject("PrototypeSpecialArms_v000009k");
        root.AddComponent<PrototypeSpecialArms09K>();
    }

    private void Update()
    {
        if (installed || BattleManager.Instance == null)
            return;

        CreateMaterials();

        // The two full-scale infantry regiments per side occupy north/south lanes.
        // Keep special arms in the centre gap and slightly behind the infantry front.
        CreateDragoonSquadron(
            "Danish Dragoon Squadron (QA)",
            BattleTeam.Denmark,
            160,
            new Vector3(-150f, 0f, -25f),
            Quaternion.Euler(0f, 90f, 0f));

        CreateDragoonSquadron(
            "Prussian Dragoon Squadron (QA)",
            BattleTeam.Prussia,
            160,
            new Vector3(150f, 0f, -25f),
            Quaternion.Euler(0f, -90f, 0f));

        CreateFieldBattery(
            "Danish Field Battery (QA)",
            BattleTeam.Denmark,
            120,
            6,
            new Vector3(-150f, 0f, 25f),
            Quaternion.Euler(0f, 90f, 0f));

        CreateFieldBattery(
            "Prussian Field Battery (QA)",
            BattleTeam.Prussia,
            120,
            6,
            new Vector3(150f, 0f, 25f),
            Quaternion.Euler(0f, -90f, 0f));

        installed = true;
        Debug.Log(
            "ARMS-09K|Installed=True|Dragoons=2x160|FieldBatteries=2x6Guns+120Crew|" +
            "CombatIntegration=Future|InfantryReskin=False");
    }

    private void CreateMaterials()
    {
        danishCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.12f, 0.22f, 0.38f), "09K_Dragoon_DK");
        prussianCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.09f, 0.13f, 0.23f), "09K_Dragoon_PR");
        horseBrown = PrototypeBootstrap.CreateSharedMaterial(new Color(0.25f, 0.14f, 0.075f), "09K_HorseBrown");
        skin = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.56f, 0.43f), "09K_Skin");
        wood = PrototypeBootstrap.CreateSharedMaterial(new Color(0.28f, 0.17f, 0.08f), "09K_ArtilleryWood");
        metal = PrototypeBootstrap.CreateSharedMaterial(new Color(0.16f, 0.17f, 0.16f), "09K_ArtilleryMetal");
    }

    private void CreateDragoonSquadron(
        string unitName,
        BattleTeam team,
        int strength,
        Vector3 position,
        Quaternion rotation)
    {
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject root = new GameObject(unitName);
        root.transform.position = position;
        root.transform.rotation = rotation;

        Material coat = team == BattleTeam.Denmark ? danishCoat : prussianCoat;

        // 1:1 riders for the small squadron. 160 mounted men is still cheap enough for
        // this first visual pilot; the future full cavalry renderer will be instanced.
        const int filesAcross = 8;
        for (int i = 0; i < strength; i++)
        {
            int row = i / filesAcross;
            int file = i % filesAcross;
            float x = (file - (filesAcross - 1) * 0.5f) * 1.18f;
            float z = -row * 1.55f;

            GameObject rider = new GameObject("Dragoon_" + (i + 1));
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

        units.Add(new SpecialUnit
        {
            Name = unitName,
            Team = team,
            Type = PrototypeSpecialArmType09K.Dragoons,
            Strength = strength,
            Root = root
        });

        Debug.Log("ARMS-09K|Unit=" + unitName + "|Type=Dragoons|Strength=" + strength + "|Mounted=True|Dismount=Future");
    }

    private void CreateFieldBattery(
        string unitName,
        BattleTeam team,
        int crewStrength,
        int gunCount,
        Vector3 position,
        Quaternion rotation)
    {
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject root = new GameObject(unitName);
        root.transform.position = position;
        root.transform.rotation = rotation;

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
            "ARMS-09K|Unit=" + unitName +
            "|Type=FieldArtillery|Guns=" + gunCount +
            "|Crew=" + crewStrength +
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
