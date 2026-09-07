using System.Collections.Generic;
using UnityEngine;

// Shared battle-layer regimental colours/standards.
// These are deliberately readable QA visuals, not final source-validated 1864 flag art.
[DefaultExecutionOrder(9000)]
public sealed class PrototypeRegimentalStandards : MonoBehaviour
{
    private readonly Dictionary<Regiment, GameObject> standards =
        new Dictionary<Regiment, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalStandards>() != null)
            return;

        GameObject root = new GameObject("PrototypeRegimentalStandards");
        root.AddComponent<PrototypeRegimentalStandards>();
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || standards.ContainsKey(regiment))
                continue;

            standards[regiment] = CreateStandard(regiment);
        }
    }

    private static GameObject CreateStandard(Regiment regiment)
    {
        GameObject standard = new GameObject("RegimentalStandard_" + regiment.RegimentName);
        standard.transform.SetParent(regiment.transform, false);
        standard.transform.localPosition = Vector3.zero;
        standard.transform.localRotation = Quaternion.identity;

        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.28f, 0.18f, 0.08f),
            "StandardPole");

        Material mainMaterial;
        Material deviceMaterial;

        if (regiment.Team == BattleTeam.Denmark)
        {
            mainMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.72f, 0.08f, 0.10f),
                "DanishStandardField");
            deviceMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.94f, 0.94f, 0.90f),
                "DanishStandardDevice");
        }
        else
        {
            mainMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.90f, 0.88f, 0.80f),
                "PrussianStandardField");
            deviceMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.08f, 0.08f, 0.09f),
                "PrussianStandardDevice");
        }

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "StandardPole";
        pole.transform.SetParent(standard.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.70f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.70f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        UnityEngine.Object.Destroy(pole.GetComponent<Collider>());

        GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        finial.name = "StandardFinial";
        finial.transform.SetParent(standard.transform, false);
        finial.transform.localPosition = new Vector3(0f, 3.43f, 0f);
        finial.transform.localScale = Vector3.one * 0.16f;
        finial.GetComponent<Renderer>().sharedMaterial = deviceMaterial;
        UnityEngine.Object.Destroy(finial.GetComponent<Collider>());

        GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloth.name = "StandardCloth";
        cloth.transform.SetParent(standard.transform, false);
        cloth.transform.localPosition = new Vector3(0.78f, 2.90f, 0f);
        cloth.transform.localScale = new Vector3(1.55f, 0.90f, 0.055f);
        cloth.GetComponent<Renderer>().sharedMaterial = mainMaterial;
        UnityEngine.Object.Destroy(cloth.GetComponent<Collider>());

        if (regiment.Team == BattleTeam.Denmark)
        {
            CreateDeviceBar(standard.transform, deviceMaterial,
                new Vector3(0.52f, 2.90f, -0.033f),
                new Vector3(0.14f, 0.91f, 0.025f),
                "DannebrogVertical");
            CreateDeviceBar(standard.transform, deviceMaterial,
                new Vector3(0.78f, 2.90f, -0.033f),
                new Vector3(1.56f, 0.13f, 0.025f),
                "DannebrogHorizontal");
        }
        else
        {
            CreateDeviceBar(standard.transform, deviceMaterial,
                new Vector3(0.78f, 2.90f, -0.033f),
                new Vector3(0.16f, 0.72f, 0.025f),
                "PrussianDeviceVertical");
            CreateDeviceBar(standard.transform, deviceMaterial,
                new Vector3(0.78f, 2.90f, -0.033f),
                new Vector3(0.72f, 0.16f, 0.025f),
                "PrussianDeviceHorizontal");
        }

        AddRegimentRibbon(standard.transform, regiment.RegimentName);

        Debug.Log("STANDARD-DIAG|Unit=" + regiment.RegimentName +
                  "|Team=" + regiment.Team +
                  "|Created=True|Art=QAPlaceholder");

        return standard;
    }

    private static void CreateDeviceBar(
        Transform parent,
        Material material,
        Vector3 localPosition,
        Vector3 localScale,
        string objectName)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = objectName;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localScale = localScale;
        bar.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
    }

    private static void AddRegimentRibbon(Transform parent, string regimentName)
    {
        int seed = 0;
        if (!string.IsNullOrEmpty(regimentName))
        {
            for (int i = 0; i < regimentName.Length; i++)
                seed = (seed * 31 + regimentName[i]) & 0x7fffffff;
        }

        float hue = (seed % 360) / 360f;
        Color ribbonColor = Color.HSVToRGB(hue, 0.55f, 0.95f);
        Material ribbonMaterial = PrototypeBootstrap.CreateSharedMaterial(
            ribbonColor,
            "RegimentalRibbon_" + regimentName);

        GameObject ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ribbon.name = "RegimentalRibbon";
        ribbon.transform.SetParent(parent, false);
        ribbon.transform.localPosition = new Vector3(1.48f, 2.54f, -0.035f);
        ribbon.transform.localScale = new Vector3(0.16f, 0.22f, 0.03f);
        ribbon.GetComponent<Renderer>().sharedMaterial = ribbonMaterial;
        UnityEngine.Object.Destroy(ribbon.GetComponent<Collider>());
    }
}
