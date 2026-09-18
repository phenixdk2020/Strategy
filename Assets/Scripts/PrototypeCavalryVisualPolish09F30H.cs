using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30h
// Lightweight visual/readability pass for 1:1 cavalry. Adds one unit label per squadron
// and a small shared-detail set per mounted figure without changing combat authority.
[DefaultExecutionOrder(43200)]
public sealed class PrototypeCavalryVisualPolish09F30H : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private bool installed;
    private float nextTry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryVisualPolish09F30H>() == null)
            new GameObject("PrototypeCavalryVisualPolish_v000009f30h")
                .AddComponent<PrototypeCavalryVisualPolish09F30H>();
    }

    private void Update()
    {
        if (installed || Time.unscaledTime < nextTry)
            return;
        nextTry = Time.unscaledTime + 0.5f;

        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed || cavalry.Gardehusar == null || cavalry.Dragon == null)
            return;

        int a = Polish(cavalry.Gardehusar);
        int b = Polish(cavalry.Dragon);
        installed = a > 0 && b > 0;

        if (installed)
            Debug.Log("VISUAL-09F30H|Installed=True|CloseLabels=True|MountedDetail=True|Gardehusar=" + a + "|Dragon=" + b);
    }

    private static int Polish(PrototypeCavalryUnit09F30 unit)
    {
        EnsureLabel(unit);

        FieldInfo field = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
        List<Transform> figures = field != null ? field.GetValue(unit) as List<Transform> : null;
        if (figures == null)
            return 0;

        Material leather = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.06f, 0.04f, 0.025f), "F30H_CavLeather_" + unit.Kind);
        Material brass = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.72f, 0.58f, 0.18f), "F30H_CavBrass_" + unit.Kind);
        Material cloth = PrototypeBootstrap.CreateSharedMaterial(
            unit.Kind == PrototypeCavalryKind09F30.Gardehusar
                ? new Color(0.55f, 0.07f, 0.055f)
                : new Color(0.08f, 0.13f, 0.22f),
            "F30H_CavCloth_" + unit.Kind);

        for (int i = 0; i < figures.Count; i++)
        {
            Transform figure = figures[i];
            if (figure == null || figure.Find("F30H_Detail") != null)
                continue;

            GameObject detail = new GameObject("F30H_Detail");
            detail.transform.SetParent(figure, false);

            CreatePart(detail.transform, "SaddleRoll",
                new Vector3(0f, 1.36f, -0.63f), new Vector3(0.52f, 0.14f, 0.18f),
                Quaternion.identity, cloth);
            CreatePart(detail.transform, "CrossBelt",
                new Vector3(0.05f, 2.14f, -0.37f), new Vector3(0.055f, 0.64f, 0.04f),
                Quaternion.Euler(0f, 0f, -28f), leather);
            CreatePart(detail.transform, "BeltBuckle",
                new Vector3(0f, 1.76f, -0.39f), new Vector3(0.13f, 0.10f, 0.035f),
                Quaternion.identity, brass);
        }

        return figures.Count;
    }

    private static void EnsureLabel(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || unit.transform.Find("F30H_CavalryLabel") != null)
            return;

        GameObject root = new GameObject("F30H_CavalryLabel");
        root.transform.SetParent(unit.transform, false);
        root.transform.localPosition = new Vector3(0f, 4.2f, 0f);

        TextMesh text = root.AddComponent<TextMesh>();
        text.text = "I  " + unit.UnitName;
        text.fontSize = 38;
        text.characterSize = 0.12f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.82f, 0.90f, 1f, 1f);
        root.AddComponent<PrototypeHigherCommandBillboard09F30B>();
    }

    private static void CreatePart(
        Transform parent, string name, Vector3 localPosition, Vector3 localScale,
        Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
    }
}
