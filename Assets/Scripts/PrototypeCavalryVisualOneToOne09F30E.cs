using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30e
// 1:1 cavalry tactical visual pass.
// Every cavalryman has one mounted visual figure: Gardehusar 120/120, Dragon 140/140.
// Dragon also receives 1:1 dismounted figures. Existing F30 movement/combat authority is unchanged.
[DefaultExecutionOrder(43100)]
public sealed class PrototypeCavalryVisualOneToOne09F30E : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private float nextTry;
    private bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryVisualOneToOne09F30E>() == null)
            new GameObject("PrototypeCavalryVisualOneToOne_v000009f30e")
                .AddComponent<PrototypeCavalryVisualOneToOne09F30E>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTry)
            return;
        nextTry = Time.unscaledTime + 0.35f;

        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (cavalry == null || !cavalry.Installed || higher == null || !higher.Installed ||
            cavalry.Gardehusar == null || cavalry.Dragon == null)
            return;

        if (!done)
        {
            int garde = EnhanceUnit(cavalry.Gardehusar);
            int dragon = EnhanceUnit(cavalry.Dragon);
            EnhanceHigherHq(higher.BrigadeHqRoot);
            EnhanceHigherHq(higher.DivisionHqRoot);

            done = garde == cavalry.Gardehusar.InitialStrength &&
                   dragon == cavalry.Dragon.InitialStrength;

            if (done)
            {
                Debug.Log(
                    "VISUAL-09F30E|OneToOne=True|Gardehusar=" + garde + "/" + cavalry.Gardehusar.InitialStrength +
                    "|Dragon=" + dragon + "/" + cavalry.Dragon.InitialStrength +
                    "|DragonDismounted=1to1|HQMountedStaffPolish=True");
            }
        }

        // F30 core recalculates a small QA collider when formation/mode changes.
        // Reassert the real 1:1 footprint after Line/Column and mounted/dismounted transitions.
        MaintainOneToOneCollider(cavalry.Gardehusar);
        MaintainOneToOneCollider(cavalry.Dragon);
    }

    private static int EnhanceUnit(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return 0;

        FieldInfo mountedFiguresField = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
        FieldInfo mountedRidersField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRiders", PrivateInstance);
        FieldInfo footFiguresField = typeof(PrototypeCavalryUnit09F30).GetField("footFigures", PrivateInstance);
        FieldInfo mountedRootField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRoot", PrivateInstance);
        FieldInfo footRootField = typeof(PrototypeCavalryUnit09F30).GetField("footRoot", PrivateInstance);
        FieldInfo unitColliderField = typeof(PrototypeCavalryUnit09F30).GetField("unitCollider", PrivateInstance);

        List<Transform> mountedFigures = mountedFiguresField != null
            ? mountedFiguresField.GetValue(unit) as List<Transform> : null;
        List<GameObject> mountedRiders = mountedRidersField != null
            ? mountedRidersField.GetValue(unit) as List<GameObject> : null;
        List<Transform> footFigures = footFiguresField != null
            ? footFiguresField.GetValue(unit) as List<Transform> : null;
        Transform mountedRoot = mountedRootField != null ? mountedRootField.GetValue(unit) as Transform : null;
        Transform footRoot = footRootField != null ? footRootField.GetValue(unit) as Transform : null;
        BoxCollider unitCollider = unitColliderField != null ? unitColliderField.GetValue(unit) as BoxCollider : null;

        if (mountedFigures == null || mountedRiders == null || footFigures == null ||
            mountedRoot == null || footRoot == null)
            return 0;

        Material[] horseMaterials = CreateHorseMaterials(unit.Kind);
        Material uniform = PrototypeBootstrap.CreateSharedMaterial(
            unit.Kind == PrototypeCavalryKind09F30.Gardehusar
                ? new Color(0.38f, 0.62f, 0.78f)
                : new Color(0.055f, 0.09f, 0.16f),
            "F30E_Uniform_" + unit.Kind);
        Material red = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.55f, 0.055f, 0.045f), "F30E_Red_" + unit.Kind);
        Material black = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.025f, 0.025f, 0.022f), "F30E_Black_" + unit.Kind);
        Material metal = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.68f, 0.66f, 0.54f), "F30E_Metal_" + unit.Kind);
        Material silver = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.78f, 0.80f, 0.78f), "F30E_Silver_" + unit.Kind);
        Material skin = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.62f, 0.45f, 0.34f), "F30E_Skin_" + unit.Kind);
        Material leather = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.055f, 0.038f, 0.022f), "F30E_Leather_" + unit.Kind);
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.90f, 0.90f, 0.84f), "F30E_White_" + unit.Kind);

        int existingMountedCount = mountedFigures.Count;
        for (int i = 0; i < existingMountedCount; i++)
        {
            GameObject riderDetail = EnhanceExistingMountedFigure(
                mountedFigures[i], unit.Kind, i, horseMaterials,
                uniform, red, black, metal, silver, skin, leather, white);
            if (riderDetail != null)
                mountedRiders.Add(riderDetail);
        }

        if (unit.Kind == PrototypeCavalryKind09F30.Dragon)
        {
            for (int i = 0; i < footFigures.Count; i++)
                EnhanceExistingDismountedDragon(footFigures[i], red, black, metal, leather);
        }

        int targetMounted = Mathf.Max(1, unit.InitialStrength);
        while (mountedFigures.Count < targetMounted)
        {
            int index = mountedFigures.Count;
            GameObject riderRoot;
            Transform mounted = CreateMountedFigure(
                mountedRoot, unit.Kind, index, horseMaterials[index % horseMaterials.Length],
                uniform, red, black, metal, silver, skin, leather, white, out riderRoot);
            mountedFigures.Add(mounted);
            mountedRiders.Add(riderRoot);
        }

        if (unit.Kind == PrototypeCavalryKind09F30.Dragon)
        {
            int targetFoot = Mathf.Max(1, unit.InitialStrength);
            while (footFigures.Count < targetFoot)
            {
                int index = footFigures.Count;
                footFigures.Add(CreateDismountedDragon(
                    footRoot, index, uniform, red, black, metal, skin, leather));
            }
        }

        ResizeOneToOneCollider(unit, unitCollider, mountedFigures.Count);
        unit.SnapVisualFormationForInitialization();

        if (unit.transform.Find("F30E_OneToOneMarker") == null)
        {
            GameObject marker = new GameObject("F30E_OneToOneMarker");
            marker.transform.SetParent(unit.transform, false);
        }

        return mountedFigures.Count;
    }

    private static Material[] CreateHorseMaterials(PrototypeCavalryKind09F30 kind)
    {
        return new[]
        {
            PrototypeBootstrap.CreateSharedMaterial(
                kind == PrototypeCavalryKind09F30.Gardehusar
                    ? new Color(0.17f, 0.09f, 0.04f)
                    : new Color(0.20f, 0.12f, 0.06f),
                "F30E_HorseDark_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.31f, 0.18f, 0.08f), "F30E_HorseBay_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.24f, 0.13f, 0.055f), "F30E_HorseChestnut_" + kind)
        };
    }

    private static Transform CreateMountedFigure(
        Transform parent,
        PrototypeCavalryKind09F30 kind,
        int index,
        Material horse,
        Material uniform,
        Material red,
        Material black,
        Material metal,
        Material silver,
        Material skin,
        Material leather,
        Material white,
        out GameObject riderRoot)
    {
        GameObject root = new GameObject("Mounted_F30E_" + (index + 1));
        root.transform.SetParent(parent, false);

        CreatePart(root.transform, PrimitiveType.Capsule, "HorseBody",
            new Vector3(0f, 0.92f, 0f), new Vector3(0.54f, 0.63f, 0.82f),
            Quaternion.Euler(90f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseChest",
            new Vector3(0f, 1.02f, 0.57f), new Vector3(0.55f, 0.72f, 0.52f),
            Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseNeck",
            new Vector3(0f, 1.45f, 0.78f), new Vector3(0.38f, 0.88f, 0.38f),
            Quaternion.Euler(-24f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseHead",
            new Vector3(0f, 1.88f, 1.13f), new Vector3(0.38f, 0.46f, 0.62f),
            Quaternion.Euler(5f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Muzzle",
            new Vector3(0f, 1.72f, 1.48f), new Vector3(0.31f, 0.26f, 0.38f),
            Quaternion.identity, horse);
        AddHorseLegSet(root.transform, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Mane",
            new Vector3(0f, 1.53f, 0.63f), new Vector3(0.11f, 0.72f, 0.24f),
            Quaternion.Euler(-24f, 0f, 0f), black);
        CreatePart(root.transform, PrimitiveType.Cube, "Tail",
            new Vector3(0f, 0.93f, -1.02f), new Vector3(0.13f, 0.15f, 0.78f),
            Quaternion.Euler(24f, 0f, 0f), black);
        CreatePart(root.transform, PrimitiveType.Cube, "EarL",
            new Vector3(-0.12f, 2.15f, 1.10f), new Vector3(0.08f, 0.25f, 0.09f),
            Quaternion.Euler(-8f, 0f, 8f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "EarR",
            new Vector3(0.12f, 2.15f, 1.10f), new Vector3(0.08f, 0.25f, 0.09f),
            Quaternion.Euler(-8f, 0f, -8f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Saddle",
            new Vector3(0f, 1.32f, -0.08f), new Vector3(0.70f, 0.18f, 0.78f),
            Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cube, "SaddleCloth",
            new Vector3(0f, 1.22f, -0.18f), new Vector3(0.76f, 0.07f, 1.02f),
            Quaternion.identity, kind == PrototypeCavalryKind09F30.Gardehusar ? red : uniform);

        CreatePart(root.transform, PrimitiveType.Cube, "Bridle",
            new Vector3(0f, 1.88f, 1.31f), new Vector3(0.43f, 0.045f, 0.045f),
            Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cube, "ReinL",
            new Vector3(-0.17f, 1.74f, 0.72f), new Vector3(0.035f, 0.035f, 0.95f),
            Quaternion.Euler(-20f, 0f, 0f), leather);
        CreatePart(root.transform, PrimitiveType.Cube, "ReinR",
            new Vector3(0.17f, 1.74f, 0.72f), new Vector3(0.035f, 0.035f, 0.95f),
            Quaternion.Euler(-20f, 0f, 0f), leather);

        riderRoot = new GameObject("Rider");
        riderRoot.transform.SetParent(root.transform, false);
        BuildRider(riderRoot.transform, kind, uniform, red, black, metal, silver, skin, leather, white);

        return root.transform;
    }

    private static GameObject EnhanceExistingMountedFigure(
        Transform figure,
        PrototypeCavalryKind09F30 kind,
        int index,
        Material[] horses,
        Material uniform,
        Material red,
        Material black,
        Material metal,
        Material silver,
        Material skin,
        Material leather,
        Material white)
    {
        if (figure == null || figure.Find("F30E_IdentityDetails") != null)
            return null;

        Material horse = horses[index % horses.Length];
        Transform[] children = figure.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform t = children[i];
            Renderer r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null)
                continue;

            string n = t.name.ToLowerInvariant();
            if (n.Contains("horse") || n.Contains("leg") || n.Contains("hoof") ||
                n.Contains("muzzle") || n.Contains("neck") || n.Contains("ear") || n.Contains("tail"))
                r.sharedMaterial = horse;
            else if (n == "rider" || n == "body")
            {
                r.sharedMaterial = uniform;
                r.enabled = false;
            }
        }

        GameObject detail = new GameObject("F30E_IdentityDetails");
        detail.transform.SetParent(figure, false);
        BuildRider(detail.transform, kind, uniform, red, black, metal, silver, skin, leather, white);
        return detail;
    }

    private static void EnhanceExistingDismountedDragon(
        Transform figure,
        Material red,
        Material black,
        Material metal,
        Material leather)
    {
        if (figure == null || figure.Find("F30E_DismountedDetail") != null)
            return;

        GameObject detail = new GameObject("F30E_DismountedDetail");
        detail.transform.SetParent(figure, false);
        CreatePart(detail.transform, PrimitiveType.Cube, "RedCollar",
            new Vector3(0f, 1.25f, -0.02f), new Vector3(0.42f, 0.10f, 0.32f),
            Quaternion.identity, red);
        CreatePart(detail.transform, PrimitiveType.Sphere, "HelmetBowl",
            new Vector3(0f, 1.88f, 0f), new Vector3(0.31f, 0.24f, 0.33f),
            Quaternion.identity, black);
        CreatePart(detail.transform, PrimitiveType.Cube, "HelmetCrest",
            new Vector3(0f, 2.12f, -0.04f), new Vector3(0.11f, 0.28f, 0.32f),
            Quaternion.identity, metal);
        CreatePart(detail.transform, PrimitiveType.Cube, "CarbineDetail",
            new Vector3(0.28f, 0.88f, 0.18f), new Vector3(0.055f, 0.055f, 0.80f),
            Quaternion.Euler(-8f, 0f, -4f), leather);
    }

    private static void BuildRider(
        Transform parent,
        PrototypeCavalryKind09F30 kind,
        Material uniform,
        Material red,
        Material black,
        Material metal,
        Material silver,
        Material skin,
        Material leather,
        Material white)
    {
        CreatePart(parent, PrimitiveType.Capsule, "Torso",
            new Vector3(0f, 2.00f, -0.08f), new Vector3(0.34f, 0.50f, 0.34f),
            Quaternion.identity, uniform);
        CreatePart(parent, PrimitiveType.Sphere, "Head",
            new Vector3(0f, 2.78f, -0.02f), new Vector3(0.26f, 0.30f, 0.26f),
            Quaternion.identity, skin);
        CreatePart(parent, PrimitiveType.Capsule, "LegL",
            new Vector3(-0.34f, 1.48f, -0.03f), new Vector3(0.12f, 0.42f, 0.12f),
            Quaternion.Euler(0f, 0f, -15f), uniform);
        CreatePart(parent, PrimitiveType.Capsule, "LegR",
            new Vector3(0.34f, 1.48f, -0.03f), new Vector3(0.12f, 0.42f, 0.12f),
            Quaternion.Euler(0f, 0f, 15f), uniform);
        CreatePart(parent, PrimitiveType.Cube, "BootL",
            new Vector3(-0.43f, 1.08f, 0.11f), new Vector3(0.18f, 0.46f, 0.20f),
            Quaternion.Euler(0f, 0f, -10f), black);
        CreatePart(parent, PrimitiveType.Cube, "BootR",
            new Vector3(0.43f, 1.08f, 0.11f), new Vector3(0.18f, 0.46f, 0.20f),
            Quaternion.Euler(0f, 0f, 10f), black);
        CreatePart(parent, PrimitiveType.Capsule, "ArmL",
            new Vector3(-0.34f, 2.05f, 0.10f), new Vector3(0.10f, 0.32f, 0.10f),
            Quaternion.Euler(32f, 0f, -22f), uniform);
        CreatePart(parent, PrimitiveType.Capsule, "ArmR",
            new Vector3(0.34f, 2.05f, 0.10f), new Vector3(0.10f, 0.32f, 0.10f),
            Quaternion.Euler(32f, 0f, 22f), uniform);
        CreatePart(parent, PrimitiveType.Sphere, "HandL",
            new Vector3(-0.22f, 1.78f, 0.28f), new Vector3(0.10f, 0.10f, 0.10f),
            Quaternion.identity, skin);
        CreatePart(parent, PrimitiveType.Sphere, "HandR",
            new Vector3(0.22f, 1.78f, 0.28f), new Vector3(0.10f, 0.10f, 0.10f),
            Quaternion.identity, skin);
        CreatePart(parent, PrimitiveType.Cube, "SabreBlade",
            new Vector3(0.56f, 1.49f, 0.30f), new Vector3(0.045f, 0.045f, 1.20f),
            Quaternion.Euler(-18f, 8f, -5f), metal);
        CreatePart(parent, PrimitiveType.Cube, "SabreGrip",
            new Vector3(0.51f, 1.76f, -0.24f), new Vector3(0.12f, 0.12f, 0.28f),
            Quaternion.Euler(-18f, 8f, -5f), leather);

        if (kind == PrototypeCavalryKind09F30.Gardehusar)
        {
            for (int row = 0; row < 5; row++)
            {
                float y = 2.31f - row * 0.14f;
                CreatePart(parent, PrimitiveType.Cube, "HussarBraid" + row,
                    new Vector3(0f, y, -0.37f), new Vector3(0.54f, 0.035f, 0.035f),
                    Quaternion.identity, silver);
            }
            CreatePart(parent, PrimitiveType.Cube, "Pelisse",
                new Vector3(0.34f, 2.04f, -0.23f), new Vector3(0.32f, 0.80f, 0.16f),
                Quaternion.Euler(0f, 0f, -8f), red);
            CreatePart(parent, PrimitiveType.Cylinder, "Busby",
                new Vector3(0f, 3.12f, -0.02f), new Vector3(0.36f, 0.36f, 0.36f),
                Quaternion.identity, black);
            CreatePart(parent, PrimitiveType.Capsule, "PlumeWhite",
                new Vector3(0.08f, 3.56f, -0.02f), new Vector3(0.10f, 0.30f, 0.10f),
                Quaternion.Euler(0f, 0f, -8f), white);
            CreatePart(parent, PrimitiveType.Capsule, "PlumeRed",
                new Vector3(0.20f, 3.39f, -0.02f), new Vector3(0.10f, 0.26f, 0.10f),
                Quaternion.Euler(0f, 0f, 28f), red);
        }
        else
        {
            CreatePart(parent, PrimitiveType.Cube, "RedCollar",
                new Vector3(0f, 2.46f, -0.04f), new Vector3(0.48f, 0.11f, 0.38f),
                Quaternion.identity, red);
            CreatePart(parent, PrimitiveType.Cube, "CuffL",
                new Vector3(-0.43f, 1.88f, 0.18f), new Vector3(0.16f, 0.20f, 0.16f),
                Quaternion.identity, red);
            CreatePart(parent, PrimitiveType.Cube, "CuffR",
                new Vector3(0.43f, 1.88f, 0.18f), new Vector3(0.16f, 0.20f, 0.16f),
                Quaternion.identity, red);
            CreatePart(parent, PrimitiveType.Sphere, "HelmetBowl",
                new Vector3(0f, 3.06f, -0.02f), new Vector3(0.36f, 0.28f, 0.38f),
                Quaternion.identity, black);
            CreatePart(parent, PrimitiveType.Cube, "HelmetCrest",
                new Vector3(0f, 3.37f, -0.07f), new Vector3(0.13f, 0.36f, 0.42f),
                Quaternion.Euler(0f, 0f, -5f), metal);
            CreatePart(parent, PrimitiveType.Cube, "Carbine",
                new Vector3(-0.48f, 1.79f, -0.14f), new Vector3(0.07f, 0.07f, 1.02f),
                Quaternion.Euler(-12f, 0f, 8f), leather);
        }
    }

    private static Transform CreateDismountedDragon(
        Transform parent,
        int index,
        Material uniform,
        Material red,
        Material black,
        Material metal,
        Material skin,
        Material leather)
    {
        GameObject root = new GameObject("Dismounted_F30E_" + (index + 1));
        root.transform.SetParent(parent, false);
        CreatePart(root.transform, PrimitiveType.Capsule, "Torso",
            new Vector3(0f, 0.86f, 0f), new Vector3(0.30f, 0.50f, 0.30f),
            Quaternion.identity, uniform);
        CreatePart(root.transform, PrimitiveType.Sphere, "Head",
            new Vector3(0f, 1.59f, 0f), new Vector3(0.23f, 0.27f, 0.23f),
            Quaternion.identity, skin);
        CreatePart(root.transform, PrimitiveType.Cube, "LegL",
            new Vector3(-0.13f, 0.33f, 0f), new Vector3(0.16f, 0.60f, 0.16f),
            Quaternion.identity, black);
        CreatePart(root.transform, PrimitiveType.Cube, "LegR",
            new Vector3(0.13f, 0.33f, 0f), new Vector3(0.16f, 0.60f, 0.16f),
            Quaternion.identity, black);
        CreatePart(root.transform, PrimitiveType.Cube, "RedCollar",
            new Vector3(0f, 1.25f, -0.02f), new Vector3(0.42f, 0.10f, 0.32f),
            Quaternion.identity, red);
        CreatePart(root.transform, PrimitiveType.Sphere, "HelmetBowl",
            new Vector3(0f, 1.88f, 0f), new Vector3(0.31f, 0.24f, 0.33f),
            Quaternion.identity, black);
        CreatePart(root.transform, PrimitiveType.Cube, "HelmetCrest",
            new Vector3(0f, 2.12f, -0.04f), new Vector3(0.11f, 0.28f, 0.32f),
            Quaternion.identity, metal);
        CreatePart(root.transform, PrimitiveType.Cube, "Carbine",
            new Vector3(0.28f, 0.88f, 0.18f), new Vector3(0.055f, 0.055f, 0.80f),
            Quaternion.Euler(-8f, 0f, -4f), leather);
        return root.transform;
    }

    private static void AddHorseLegSet(Transform parent, Material horse)
    {
        Vector3[] positions =
        {
            new Vector3(-0.22f, 0.42f, 0.55f),
            new Vector3(0.22f, 0.42f, 0.55f),
            new Vector3(-0.22f, 0.42f, -0.55f),
            new Vector3(0.22f, 0.42f, -0.55f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            CreatePart(parent, PrimitiveType.Capsule, "Leg" + i,
                positions[i], new Vector3(0.11f, 0.42f, 0.11f),
                Quaternion.identity, horse);
            CreatePart(parent, PrimitiveType.Cube, "Hoof" + i,
                new Vector3(positions[i].x, 0.065f, positions[i].z + 0.025f),
                new Vector3(0.16f, 0.10f, 0.24f),
                Quaternion.identity, horse);
        }
    }

    private static void EnhanceHigherHq(GameObject hq)
    {
        if (hq == null || hq.transform.Find("F30E_HQMountedPolish") != null)
            return;

        Material darkBlue = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.055f, 0.09f, 0.16f), "F30E_HQDarkBlue");
        Material red = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.55f, 0.055f, 0.045f), "F30E_HQRed");
        Material black = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.025f, 0.025f, 0.022f), "F30E_HQBlack");
        Material metal = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.70f, 0.66f, 0.44f), "F30E_HQMetal");
        Material skin = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.62f, 0.45f, 0.34f), "F30E_HQSkin");

        Transform[] all = hq.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform mount = all[i];
            if (mount == null ||
                (!mount.name.StartsWith("HigherHQ_Commander") && !mount.name.StartsWith("HigherHQ_Staff")))
                continue;
            if (mount.Find("F30E_OfficerDetail") != null)
                continue;

            GameObject detail = new GameObject("F30E_OfficerDetail");
            detail.transform.SetParent(mount, false);

            CreatePart(detail.transform, PrimitiveType.Capsule, "OfficerTorso",
                new Vector3(0f, 2.25f, -0.05f), new Vector3(0.34f, 0.50f, 0.34f),
                Quaternion.identity, darkBlue);
            CreatePart(detail.transform, PrimitiveType.Capsule, "ArmL",
                new Vector3(-0.34f, 2.20f, 0.08f), new Vector3(0.10f, 0.31f, 0.10f),
                Quaternion.Euler(28f, 0f, -20f), darkBlue);
            CreatePart(detail.transform, PrimitiveType.Capsule, "ArmR",
                new Vector3(0.34f, 2.20f, 0.08f), new Vector3(0.10f, 0.31f, 0.10f),
                Quaternion.Euler(28f, 0f, 20f), darkBlue);
            CreatePart(detail.transform, PrimitiveType.Cube, "BootL",
                new Vector3(-0.42f, 1.27f, 0.08f), new Vector3(0.18f, 0.50f, 0.20f),
                Quaternion.Euler(0f, 0f, -10f), black);
            CreatePart(detail.transform, PrimitiveType.Cube, "BootR",
                new Vector3(0.42f, 1.27f, 0.08f), new Vector3(0.18f, 0.50f, 0.20f),
                Quaternion.Euler(0f, 0f, 10f), black);
            CreatePart(detail.transform, PrimitiveType.Sphere, "Head",
                new Vector3(0f, 2.96f, -0.02f), new Vector3(0.27f, 0.31f, 0.27f),
                Quaternion.identity, skin);
            CreatePart(detail.transform, PrimitiveType.Sphere, "Helmet",
                new Vector3(0f, 3.20f, -0.02f), new Vector3(0.33f, 0.23f, 0.35f),
                Quaternion.identity, black);
            CreatePart(detail.transform, PrimitiveType.Cube, "HelmetCrest",
                new Vector3(0f, 3.43f, -0.06f), new Vector3(0.11f, 0.28f, 0.34f),
                Quaternion.identity, metal);
            CreatePart(detail.transform, PrimitiveType.Cube, "Collar",
                new Vector3(0f, 2.55f, -0.03f), new Vector3(0.44f, 0.10f, 0.34f),
                Quaternion.identity, red);
            CreatePart(detail.transform, PrimitiveType.Cube, "Sabre",
                new Vector3(0.56f, 1.72f, 0.28f), new Vector3(0.045f, 0.045f, 1.20f),
                Quaternion.Euler(-18f, 8f, -5f), metal);
        }

        GameObject marker = new GameObject("F30E_HQMountedPolish");
        marker.transform.SetParent(hq.transform, false);
    }

    private static void MaintainOneToOneCollider(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return;

        FieldInfo mountedFiguresField = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
        FieldInfo unitColliderField = typeof(PrototypeCavalryUnit09F30).GetField("unitCollider", PrivateInstance);
        List<Transform> mountedFigures = mountedFiguresField != null
            ? mountedFiguresField.GetValue(unit) as List<Transform> : null;
        BoxCollider collider = unitColliderField != null
            ? unitColliderField.GetValue(unit) as BoxCollider : null;

        ResizeOneToOneCollider(unit, collider, mountedFigures != null ? mountedFigures.Count : 0);
    }

    private static void ResizeOneToOneCollider(
        PrototypeCavalryUnit09F30 unit,
        BoxCollider collider,
        int visibleCount)
    {
        if (unit == null || collider == null || visibleCount <= 0)
            return;

        if (unit.Mode == PrototypeCavalryMode09F30.Dismounted)
        {
            int columns = Mathf.CeilToInt(visibleCount / 2f);
            collider.size = unit.Formation == PrototypeCavalryFormation09F30.Line
                ? new Vector3(Mathf.Max(18f, columns * 0.78f + 2f), 2.5f, 5f)
                : new Vector3(6f, 2.5f, Mathf.Max(16f, Mathf.CeilToInt(visibleCount / 4f) * 0.85f + 2f));
            return;
        }

        if (unit.IsBridgeRouteActive)
        {
            int rows = Mathf.CeilToInt(visibleCount / 2f);
            collider.size = new Vector3(5.2f, 3.2f, Mathf.Max(24f, rows * 2.15f + 3f));
        }
        else if (unit.Formation == PrototypeCavalryFormation09F30.Line)
        {
            int columns = Mathf.CeilToInt(visibleCount / 4f);
            collider.size = new Vector3(Mathf.Max(22f, columns * 1.70f + 3f), 3.2f, 10f);
        }
        else
        {
            int rows = Mathf.CeilToInt(visibleCount / 4f);
            collider.size = new Vector3(8.2f, 3.2f, Mathf.Max(24f, rows * 2.15f + 3f));
        }

        Vector3 worldCenter = unit.GetCurrentFootprintCenterWorld();
        Vector3 localCenter = unit.transform.InverseTransformPoint(worldCenter);
        collider.center = new Vector3(0f, 1.2f, localCenter.z);
    }

    private static GameObject CreatePart(
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
            UnityEngine.Object.Destroy(collider);

        return part;
    }
}
