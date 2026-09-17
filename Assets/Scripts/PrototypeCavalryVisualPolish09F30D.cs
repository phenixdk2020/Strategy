using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30d
// Visual density/polish pass for cavalry and higher HQ horses.
[DefaultExecutionOrder(43000)]
public sealed class PrototypeCavalryVisualPolish09F30D : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private float nextTry;
    private bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryVisualPolish09F30D>() == null)
            new GameObject("PrototypeCavalryVisualPolish_v000009f30d").AddComponent<PrototypeCavalryVisualPolish09F30D>();
    }

    private void Update()
    {
        if (done || Time.unscaledTime < nextTry)
            return;
        nextTry = Time.unscaledTime + 0.5f;

        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (cavalry == null || !cavalry.Installed || higher == null || !higher.Installed)
            return;

        EnhanceUnit(cavalry.Gardehusar);
        EnhanceUnit(cavalry.Dragon);
        EnhanceHigherHq(higher.BrigadeHqRoot);
        EnhanceHigherHq(higher.DivisionHqRoot);

        done = true;
        Debug.Log("VISUAL-09F30D|CavalryDensity=1FigurePer5Men|HQHorseLegs=True|HorseDetail=True");
    }

    private static void EnhanceUnit(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || unit.transform.Find("F30D_VisualMarker") != null)
            return;

        FieldInfo mountedFiguresField = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
        FieldInfo mountedRidersField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRiders", PrivateInstance);
        FieldInfo footFiguresField = typeof(PrototypeCavalryUnit09F30).GetField("footFigures", PrivateInstance);
        FieldInfo mountedRootField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRoot", PrivateInstance);
        FieldInfo footRootField = typeof(PrototypeCavalryUnit09F30).GetField("footRoot", PrivateInstance);

        List<Transform> mountedFigures = mountedFiguresField != null ? mountedFiguresField.GetValue(unit) as List<Transform> : null;
        List<GameObject> mountedRiders = mountedRidersField != null ? mountedRidersField.GetValue(unit) as List<GameObject> : null;
        List<Transform> footFigures = footFiguresField != null ? footFiguresField.GetValue(unit) as List<Transform> : null;
        Transform mountedRoot = mountedRootField != null ? mountedRootField.GetValue(unit) as Transform : null;
        Transform footRoot = footRootField != null ? footRootField.GetValue(unit) as Transform : null;
        if (mountedFigures == null || mountedRiders == null || footFigures == null || mountedRoot == null || footRoot == null)
            return;

        int originalCount = mountedFigures.Count;
        for (int i = 0; i < originalCount; i++)
            AddHorseDetails(mountedFigures[i]);

        int targetCount = Mathf.Clamp(Mathf.CeilToInt(unit.InitialStrength / 5f), 20, 32);
        Material horse = PrototypeBootstrap.CreateSharedMaterial(
            unit.Kind == PrototypeCavalryKind09F30.Gardehusar ? new Color(0.22f, 0.12f, 0.055f) : new Color(0.28f, 0.18f, 0.09f),
            "F30D_Horse_" + unit.Kind);
        Material uniform = PrototypeBootstrap.CreateSharedMaterial(
            unit.Kind == PrototypeCavalryKind09F30.Gardehusar ? new Color(0.08f, 0.14f, 0.28f) : new Color(0.13f, 0.20f, 0.30f),
            "F30D_Uniform_" + unit.Kind);
        Material leather = PrototypeBootstrap.CreateSharedMaterial(new Color(0.06f, 0.045f, 0.03f), "F30D_Leather");

        for (int i = originalCount; i < targetCount; i++)
        {
            GameObject rider;
            Transform mounted = CreateMountedFigure(mountedRoot, i, horse, uniform, leather, out rider);
            Transform foot = CreateFootFigure(footRoot, i, uniform, leather);
            mountedFigures.Add(mounted);
            mountedRiders.Add(rider);
            footFigures.Add(foot);
        }

        GameObject marker = new GameObject("F30D_VisualMarker");
        marker.transform.SetParent(unit.transform, false);
    }

    private static Transform CreateMountedFigure(Transform parent, int index, Material horse, Material uniform, Material leather, out GameObject riderRoot)
    {
        GameObject root = new GameObject("Mounted_F30D_" + (index + 1));
        root.transform.SetParent(parent, false);

        CreatePart(root.transform, PrimitiveType.Cube, "HorseBody", new Vector3(0f, 0.82f, 0f), new Vector3(0.62f, 0.70f, 1.68f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseNeck", new Vector3(0f, 1.34f, 0.70f), new Vector3(0.42f, 0.86f, 0.42f), Quaternion.Euler(-22f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseHead", new Vector3(0f, 1.72f, 1.06f), new Vector3(0.42f, 0.48f, 0.66f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Muzzle", new Vector3(0f, 1.58f, 1.42f), new Vector3(0.32f, 0.28f, 0.38f), Quaternion.identity, horse);
        AddHorseLegSet(root.transform, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Tail", new Vector3(0f, 0.92f, -1.02f), new Vector3(0.12f, 0.12f, 0.72f), Quaternion.Euler(22f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "EarL", new Vector3(-0.12f, 2.02f, 1.05f), new Vector3(0.09f, 0.26f, 0.09f), Quaternion.Euler(-8f, 0f, 8f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "EarR", new Vector3(0.12f, 2.02f, 1.05f), new Vector3(0.09f, 0.26f, 0.09f), Quaternion.Euler(-8f, 0f, -8f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Saddle", new Vector3(0f, 1.25f, -0.08f), new Vector3(0.68f, 0.16f, 0.78f), Quaternion.identity, leather);

        riderRoot = new GameObject("Rider");
        riderRoot.transform.SetParent(root.transform, false);
        CreatePart(riderRoot.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 1.92f, -0.08f), new Vector3(0.34f, 0.48f, 0.34f), Quaternion.identity, uniform);
        CreatePart(riderRoot.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 2.68f, -0.03f), new Vector3(0.27f, 0.31f, 0.27f), Quaternion.identity, leather);
        CreatePart(riderRoot.transform, PrimitiveType.Cube, "Weapon", new Vector3(0.42f, 1.91f, 0.12f), new Vector3(0.06f, 0.06f, 1.10f), Quaternion.Euler(0f, 0f, -6f), leather);
        return root.transform;
    }

    private static Transform CreateFootFigure(Transform parent, int index, Material uniform, Material leather)
    {
        GameObject root = new GameObject("Dismounted_F30D_" + (index + 1));
        root.transform.SetParent(parent, false);
        CreatePart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.74f, 0f), new Vector3(0.29f, 0.48f, 0.29f), Quaternion.identity, uniform);
        CreatePart(root.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.48f, 0f), new Vector3(0.23f, 0.27f, 0.23f), Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cube, "Carbine", new Vector3(0.24f, 0.80f, 0.18f), new Vector3(0.055f, 0.055f, 0.72f), Quaternion.identity, leather);
        return root.transform;
    }

    private static void AddHorseDetails(Transform figure)
    {
        if (figure == null || figure.Find("F30D_HorseDetails") != null)
            return;

        Renderer renderer = figure.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;
        Material horse = renderer.sharedMaterial;
        GameObject detail = new GameObject("F30D_HorseDetails");
        detail.transform.SetParent(figure, false);
        AddHorseLegSet(detail.transform, horse);
        CreatePart(detail.transform, PrimitiveType.Cube, "Tail", new Vector3(0f, 0.84f, -0.98f), new Vector3(0.11f, 0.11f, 0.68f), Quaternion.Euler(24f, 0f, 0f), horse);
        CreatePart(detail.transform, PrimitiveType.Cube, "Neck", new Vector3(0f, 1.18f, 0.62f), new Vector3(0.35f, 0.65f, 0.35f), Quaternion.Euler(-22f, 0f, 0f), horse);
    }

    private static void AddHorseLegSet(Transform parent, Material horse)
    {
        Vector3[] pos =
        {
            new Vector3(-0.22f, 0.40f, 0.52f), new Vector3(0.22f, 0.40f, 0.52f),
            new Vector3(-0.22f, 0.40f, -0.52f), new Vector3(0.22f, 0.40f, -0.52f)
        };
        for (int i = 0; i < pos.Length; i++)
        {
            CreatePart(parent, PrimitiveType.Capsule, "Leg" + i, pos[i], new Vector3(0.12f, 0.40f, 0.12f), Quaternion.identity, horse);
            CreatePart(parent, PrimitiveType.Cube, "Hoof" + i, new Vector3(pos[i].x, 0.06f, pos[i].z + 0.02f), new Vector3(0.16f, 0.10f, 0.24f), Quaternion.identity, horse);
        }
    }

    private static void EnhanceHigherHq(GameObject root)
    {
        if (root == null || root.transform.Find("F30D_HQVisualMarker") != null)
            return;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform mount = all[i];
            if (mount == null || (!mount.name.StartsWith("HigherHQ_Commander") && !mount.name.StartsWith("HigherHQ_Staff")))
                continue;
            if (mount.Find("F30D_HorseDetails") != null)
                continue;

            Transform horseBody = mount.Find("HorseBody");
            Renderer renderer = horseBody != null ? horseBody.GetComponent<Renderer>() : null;
            if (renderer == null)
                continue;

            Material horse = renderer.sharedMaterial;
            GameObject detail = new GameObject("F30D_HorseDetails");
            detail.transform.SetParent(mount, false);
            AddHorseLegSet(detail.transform, horse);
            CreatePart(detail.transform, PrimitiveType.Cube, "Tail", new Vector3(0f, 1.03f, -1.02f), new Vector3(0.12f, 0.12f, 0.72f), Quaternion.Euler(23f, 0f, 0f), horse);
            CreatePart(detail.transform, PrimitiveType.Cube, "EarL", new Vector3(-0.12f, 2.20f, 1.09f), new Vector3(0.09f, 0.26f, 0.09f), Quaternion.identity, horse);
            CreatePart(detail.transform, PrimitiveType.Cube, "EarR", new Vector3(0.12f, 2.20f, 1.09f), new Vector3(0.09f, 0.26f, 0.09f), Quaternion.identity, horse);
        }

        GameObject marker = new GameObject("F30D_HQVisualMarker");
        marker.transform.SetParent(root.transform, false);
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
        Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return part;
    }
}
