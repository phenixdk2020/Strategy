using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30l
// Cavalry visual-fidelity pass. Reuses the existing 1:1 F30E figures, reshapes their
// horse/rider silhouette and adds a deliberately small close-detail layer.
// Added details are LOD-hidden at higher camera altitude; tactical figure count remains 1:1.
[DefaultExecutionOrder(43350)]
public sealed class PrototypeCavalryVisualFidelity09F30L : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DetailCameraHeight = 210f;

    private readonly List<GameObject> closeDetailRoots = new List<GameObject>();
    private bool installed;
    private bool closeDetailsVisible = true;
    private float nextTry;
    private float nextLodCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryVisualFidelity09F30L>() == null)
            new GameObject("PrototypeCavalryVisualFidelity_v000009f30l")
                .AddComponent<PrototypeCavalryVisualFidelity09F30L>();
    }

    private void Update()
    {
        if (!installed)
        {
            if (Time.unscaledTime < nextTry)
                return;
            nextTry = Time.unscaledTime + 0.5f;

            PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
            if (cavalry == null || !cavalry.Installed ||
                cavalry.Gardehusar == null || cavalry.Dragon == null)
                return;

            if (cavalry.Gardehusar.transform.Find("F30E_OneToOneMarker") == null ||
                cavalry.Dragon.transform.Find("F30E_OneToOneMarker") == null)
                return;

            int garde = PolishUnit(cavalry.Gardehusar);
            int dragon = PolishUnit(cavalry.Dragon);
            installed = garde == cavalry.Gardehusar.InitialStrength &&
                        dragon == cavalry.Dragon.InitialStrength;

            if (installed)
            {
                Debug.Log("VISUAL-09F30L|Installed=True|HorseSilhouette=Enhanced|" +
                          "RiderIdentity=Enhanced|CloseDetailLOD=True|Gardehusar=" + garde +
                          "|Dragon=" + dragon + "|OneToOnePreserved=True");
            }
        }

        UpdateDetailLod();
    }

    private int PolishUnit(PrototypeCavalryUnit09F30 unit)
    {
        FieldInfo field = typeof(PrototypeCavalryUnit09F30)
            .GetField("mountedFigures", PrivateInstance);
        List<Transform> figures = field != null
            ? field.GetValue(unit) as List<Transform> : null;
        if (figures == null)
            return 0;

        Material[] horses = CreateHorsePalette(unit.Kind);
        Material coat = PrototypeBootstrap.CreateSharedMaterial(
            unit.Kind == PrototypeCavalryKind09F30.Gardehusar
                ? new Color(0.27f, 0.50f, 0.69f)
                : new Color(0.035f, 0.065f, 0.12f),
            "F30L_Coat_" + unit.Kind);
        Material leather = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.045f, 0.028f, 0.016f), "F30L_Leather_" + unit.Kind);
        Material dark = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.018f, 0.018f, 0.016f), "F30L_Dark_" + unit.Kind);
        Material red = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.58f, 0.045f, 0.035f), "F30L_Red_" + unit.Kind);
        Material silver = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.80f, 0.82f, 0.78f), "F30L_Silver_" + unit.Kind);
        Material brass = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.70f, 0.52f, 0.15f), "F30L_Brass_" + unit.Kind);
        Material marking = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.88f, 0.84f, 0.72f), "F30L_HorseMarking_" + unit.Kind);

        for (int i = 0; i < figures.Count; i++)
        {
            Transform figure = figures[i];
            if (figure == null)
                continue;

            ReshapeExistingFigure(figure, unit.Kind, i, horses[i % horses.Length], coat, dark);

            if (figure.Find("F30L_CloseDetail") == null)
            {
                GameObject detail = new GameObject("F30L_CloseDetail");
                detail.transform.SetParent(figure, false);
                BuildCloseDetail(detail.transform, unit.Kind, i, leather, dark, red, silver, brass, marking);
                closeDetailRoots.Add(detail);
            }
            else
            {
                closeDetailRoots.Add(figure.Find("F30L_CloseDetail").gameObject);
            }
        }

        return figures.Count;
    }

    private static Material[] CreateHorsePalette(PrototypeCavalryKind09F30 kind)
    {
        return new[]
        {
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.105f, 0.055f, 0.028f), "F30L_Horse_BlackBay_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.18f, 0.085f, 0.035f), "F30L_Horse_DarkBay_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.29f, 0.14f, 0.050f), "F30L_Horse_Bay_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.37f, 0.19f, 0.070f), "F30L_Horse_Chestnut_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.235f, 0.155f, 0.095f), "F30L_Horse_Brown_" + kind),
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.095f, 0.070f, 0.052f), "F30L_Horse_Seal_" + kind)
        };
    }

    private static void ReshapeExistingFigure(
        Transform figure,
        PrototypeCavalryKind09F30 kind,
        int index,
        Material horse,
        Material coat,
        Material dark)
    {
        Transform body = FindDescendant(figure, "HorseBody");
        Transform chest = FindDescendant(figure, "HorseChest");
        Transform neck = FindDescendant(figure, "HorseNeck");
        Transform head = FindDescendant(figure, "HorseHead");
        Transform muzzle = FindDescendant(figure, "Muzzle");
        Transform tail = FindDescendant(figure, "Tail");
        Transform mane = FindDescendant(figure, "Mane");

        SetScale(body, new Vector3(0.60f, 0.68f, 0.90f));
        SetPosition(body, new Vector3(0f, 0.95f, -0.05f));
        SetScale(chest, new Vector3(0.58f, 0.73f, 0.54f));
        SetPosition(chest, new Vector3(0f, 1.03f, 0.58f));
        SetScale(neck, new Vector3(0.34f, 0.94f, 0.34f));
        SetRotation(neck, Quaternion.Euler(-29f, 0f, 0f));
        SetScale(head, new Vector3(0.35f, 0.42f, 0.58f));
        SetPosition(head, new Vector3(0f, 1.91f, 1.17f));
        SetScale(muzzle, new Vector3(0.27f, 0.23f, 0.40f));
        SetPosition(muzzle, new Vector3(0f, 1.74f, 1.52f));
        SetScale(tail, new Vector3(0.11f, 0.13f, 0.90f));
        SetScale(mane, new Vector3(0.095f, 0.74f, 0.20f));

        ApplyMaterial(body, horse);
        ApplyMaterial(chest, horse);
        ApplyMaterial(neck, horse);
        ApplyMaterial(head, horse);
        ApplyMaterial(muzzle, horse);

        for (int leg = 0; leg < 4; leg++)
        {
            Transform upper = FindDescendant(figure, "Leg" + leg);
            Transform hoof = FindDescendant(figure, "Hoof" + leg);
            if (upper != null)
            {
                upper.localScale = new Vector3(0.095f, 0.46f, 0.095f);
                ApplyMaterial(upper, horse);
            }
            if (hoof != null)
            {
                hoof.localScale = new Vector3(0.145f, 0.085f, 0.22f);
                ApplyMaterial(hoof, dark);
            }
        }

        Transform riderTorso = FindDescendant(figure, "Torso");
        if (riderTorso != null)
        {
            riderTorso.localScale = new Vector3(0.32f, 0.52f, 0.30f);
            ApplyMaterial(riderTorso, coat);
        }

        // Deterministic micro-variation avoids cloned silhouettes without changing slots.
        float yaw = ((index * 17) % 11 - 5) * 0.7f;
        if (head != null)
            head.localRotation = Quaternion.Euler(4f, yaw, 0f);
        if (tail != null)
            tail.localRotation = Quaternion.Euler(22f, ((index * 13) % 9 - 4) * 2.0f, 0f);

        Transform rider = FindDescendant(figure, "F30E_IdentityDetails");
        if (rider == null)
            rider = FindDescendant(figure, "Rider");
        if (rider != null)
            rider.localPosition = new Vector3(0f, 0.02f, -0.04f);
    }

    private static void BuildCloseDetail(
        Transform parent,
        PrototypeCavalryKind09F30 kind,
        int index,
        Material leather,
        Material dark,
        Material red,
        Material silver,
        Material brass,
        Material marking)
    {
        CreatePart(parent, PrimitiveType.Cube, "BreastCollar",
            new Vector3(0f, 1.18f, 0.67f), new Vector3(0.64f, 0.075f, 0.075f),
            Quaternion.Euler(12f, 0f, 0f), leather);
        CreatePart(parent, PrimitiveType.Cube, "Girth",
            new Vector3(0f, 1.04f, -0.10f), new Vector3(0.68f, 0.055f, 0.13f),
            Quaternion.identity, leather);

        CreatePart(parent, PrimitiveType.Cube, "StirrupL",
            new Vector3(-0.48f, 1.08f, -0.08f), new Vector3(0.07f, 0.25f, 0.16f),
            Quaternion.Euler(0f, 0f, -8f), dark);
        CreatePart(parent, PrimitiveType.Cube, "StirrupR",
            new Vector3(0.48f, 1.08f, -0.08f), new Vector3(0.07f, 0.25f, 0.16f),
            Quaternion.Euler(0f, 0f, 8f), dark);

        if (kind == PrototypeCavalryKind09F30.Gardehusar)
        {
            CreatePart(parent, PrimitiveType.Cube, "Sabretache",
                new Vector3(0.48f, 1.47f, -0.44f), new Vector3(0.28f, 0.35f, 0.08f),
                Quaternion.Euler(0f, 0f, 8f), red);
            CreatePart(parent, PrimitiveType.Cube, "SabretacheTrim",
                new Vector3(0.48f, 1.47f, -0.49f), new Vector3(0.20f, 0.025f, 0.025f),
                Quaternion.identity, silver);
        }
        else
        {
            CreatePart(parent, PrimitiveType.Cube, "CartridgeBox",
                new Vector3(-0.40f, 1.83f, -0.35f), new Vector3(0.30f, 0.24f, 0.10f),
                Quaternion.Euler(0f, 0f, -7f), dark);
            CreatePart(parent, PrimitiveType.Cube, "CartridgeBadge",
                new Vector3(-0.40f, 1.83f, -0.41f), new Vector3(0.08f, 0.08f, 0.025f),
                Quaternion.identity, brass);
        }

        // Sparse horse markings add variation without multiplying detail on every horse.
        if ((index % 9) == 0)
        {
            CreatePart(parent, PrimitiveType.Cube, "FaceBlaze",
                new Vector3(0f, 1.91f, 1.485f), new Vector3(0.10f, 0.28f, 0.025f),
                Quaternion.Euler(5f, 0f, 0f), marking);
        }
        if ((index % 13) == 0)
        {
            CreatePart(parent, PrimitiveType.Cube, "SockFL",
                new Vector3(-0.22f, 0.18f, 0.55f), new Vector3(0.13f, 0.16f, 0.13f),
                Quaternion.identity, marking);
        }
    }

    private void UpdateDetailLod()
    {
        if (!installed || Time.unscaledTime < nextLodCheck)
            return;
        nextLodCheck = Time.unscaledTime + 0.35f;

        Camera cam = Camera.main;
        bool visible = cam == null || cam.transform.position.y <= DetailCameraHeight;
        if (visible == closeDetailsVisible)
            return;

        closeDetailsVisible = visible;
        for (int i = 0; i < closeDetailRoots.Count; i++)
            if (closeDetailRoots[i] != null)
                closeDetailRoots[i].SetActive(visible);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name)
                return all[i];
        return null;
    }

    private static void SetScale(Transform t, Vector3 scale)
    {
        if (t != null) t.localScale = scale;
    }

    private static void SetPosition(Transform t, Vector3 position)
    {
        if (t != null) t.localPosition = position;
    }

    private static void SetRotation(Transform t, Quaternion rotation)
    {
        if (t != null) t.localRotation = rotation;
    }

    private static void ApplyMaterial(Transform t, Material material)
    {
        if (t == null || material == null)
            return;
        Renderer renderer = t.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void CreatePart(
        Transform parent,
        PrimitiveType primitive,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
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
    }
}
