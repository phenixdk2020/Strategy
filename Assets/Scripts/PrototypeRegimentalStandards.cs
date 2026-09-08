using System.Collections.Generic;
using UnityEngine;

// v00.00.09g TEST: improved regimental colours/standards for Battle mode.
// Still QA-readable placeholders rather than final source-validated 1864 flag art,
// but with stronger silhouette, cloth fold, cords and enlarged close-up readability.
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

        GameObject root = new GameObject("PrototypeRegimentalStandards_v000009g");
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

        CleanupDestroyedRegiments();
    }

    private void CleanupDestroyedRegiments()
    {
        if (standards.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, GameObject> pair in standards)
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
            standards.Remove(remove[i]);
    }

    private static GameObject CreateStandard(Regiment regiment)
    {
        GameObject standard = new GameObject("RegimentalStandard_" + regiment.RegimentName);
        standard.transform.SetParent(regiment.transform, false);
        standard.transform.localPosition = Vector3.zero;
        standard.transform.localRotation = Quaternion.identity;

        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.26f, 0.17f, 0.075f),
            "StandardPole");
        Material brassMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.73f, 0.57f, 0.20f),
            "StandardBrass");

        Material mainMaterial;
        Material deviceMaterial;

        if (regiment.Team == BattleTeam.Denmark)
        {
            mainMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.75f, 0.055f, 0.075f),
                "DanishStandardField");
            deviceMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.95f, 0.95f, 0.91f),
                "DanishStandardDevice");
        }
        else
        {
            mainMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.91f, 0.89f, 0.81f),
                "PrussianStandardField");
            deviceMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.065f, 0.065f, 0.075f),
                "PrussianStandardDevice");
        }

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "StandardPole";
        pole.transform.SetParent(standard.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.95f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        UnityEngine.Object.Destroy(pole.GetComponent<Collider>());

        GameObject ferrule = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ferrule.name = "StandardFerrule";
        ferrule.transform.SetParent(standard.transform, false);
        ferrule.transform.localPosition = new Vector3(0f, 3.83f, 0f);
        ferrule.transform.localScale = new Vector3(0.09f, 0.10f, 0.09f);
        ferrule.GetComponent<Renderer>().sharedMaterial = brassMaterial;
        UnityEngine.Object.Destroy(ferrule.GetComponent<Collider>());

        GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finial.name = "StandardFinial";
        finial.transform.SetParent(standard.transform, false);
        finial.transform.localPosition = new Vector3(0f, 4.10f, 0f);
        finial.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
        finial.transform.localScale = new Vector3(0.17f, 0.28f, 0.17f);
        finial.GetComponent<Renderer>().sharedMaterial = brassMaterial;
        UnityEngine.Object.Destroy(finial.GetComponent<Collider>());

        GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloth.name = "StandardCloth";
        cloth.transform.SetParent(standard.transform, false);
        cloth.transform.localPosition = new Vector3(0.92f, 3.28f, 0f);
        cloth.transform.localScale = new Vector3(1.82f, 1.08f, 0.045f);
        cloth.GetComponent<Renderer>().sharedMaterial = mainMaterial;
        UnityEngine.Object.Destroy(cloth.GetComponent<Collider>());

        // Small outer fold gives the otherwise flat QA flag a readable wind silhouette.
        GameObject fold = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fold.name = "StandardClothFold";
        fold.transform.SetParent(standard.transform, false);
        fold.transform.localPosition = new Vector3(1.86f, 3.28f, 0.10f);
        fold.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
        fold.transform.localScale = new Vector3(0.34f, 1.02f, 0.04f);
        fold.GetComponent<Renderer>().sharedMaterial = mainMaterial;
        UnityEngine.Object.Destroy(fold.GetComponent<Collider>());

        // Simple readable national device for prototype identification.
        // Final regimental colours will later be replaced by historically sourced art/data.
        if (regiment.Team == BattleTeam.Denmark)
        {
            CreateDeviceBar(
                standard.transform,
                deviceMaterial,
                new Vector3(0.59f, 3.28f, -0.031f),
                new Vector3(0.15f, 1.09f, 0.020f),
                "DannebrogVertical");
            CreateDeviceBar(
                standard.transform,
                deviceMaterial,
                new Vector3(0.92f, 3.28f, -0.031f),
                new Vector3(1.84f, 0.14f, 0.020f),
                "DannebrogHorizontal");
        }
        else
        {
            CreateDeviceBar(
                standard.transform,
                deviceMaterial,
                new Vector3(0.92f, 3.28f, -0.031f),
                new Vector3(0.18f, 0.82f, 0.020f),
                "PrussianDeviceVertical");
            CreateDeviceBar(
                standard.transform,
                deviceMaterial,
                new Vector3(0.92f, 3.28f, -0.031f),
                new Vector3(0.82f, 0.18f, 0.020f),
                "PrussianDeviceHorizontal");
        }

        CreateCord(
            standard.transform,
            new Vector3(0.04f, 3.77f, -0.08f),
            new Vector3(0.34f, 2.66f, -0.08f),
            brassMaterial,
            "StandardCordA");
        CreateCord(
            standard.transform,
            new Vector3(0.10f, 3.72f, 0.08f),
            new Vector3(0.46f, 2.56f, 0.08f),
            brassMaterial,
            "StandardCordB");

        AddRegimentRibbon(standard.transform, regiment.RegimentName);

        Debug.Log(
            "STANDARD-DIAG|Build=v00.00.09g|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Created=True|Art=EnhancedQAPlaceholder|Fold=True|Cords=True");

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

    private static void CreateCord(
        Transform parent,
        Vector3 start,
        Vector3 end,
        Material material,
        string objectName)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f)
            return;

        GameObject cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cord.name = objectName;
        cord.transform.SetParent(parent, false);
        cord.transform.localPosition = (start + end) * 0.5f;
        cord.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        cord.transform.localScale = new Vector3(0.025f, length * 0.5f, 0.025f);
        cord.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(cord.GetComponent<Collider>());
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
        ribbon.transform.localPosition = new Vector3(1.80f, 2.74f, -0.060f);
        ribbon.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
        ribbon.transform.localScale = new Vector3(0.23f, 0.30f, 0.025f);
        ribbon.GetComponent<Renderer>().sharedMaterial = ribbonMaterial;
        UnityEngine.Object.Destroy(ribbon.GetComponent<Collider>());
    }
}
