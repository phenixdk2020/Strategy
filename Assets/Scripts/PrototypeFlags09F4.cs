using System.Collections.Generic;
using UnityEngine;

// v00.00.09f4 flag pass.
// Danish units carry a clearly readable two-sided Dannebrog plus a blue/gold
// Sjællandske Livregiment prototype standard inspired by the approved reference.
// All Unity object calls are explicitly qualified to avoid System.Object ambiguity.
[DefaultExecutionOrder(8800)]
public sealed class PrototypeFlags09F4 : MonoBehaviour
{
    private readonly Dictionary<Regiment, GameObject> roots =
        new Dictionary<Regiment, GameObject>();

    private bool legacySuppressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeFlags09F4>() != null)
            return;

        GameObject root = new GameObject("PrototypeFlags_v000009f4");
        root.AddComponent<PrototypeFlags09F4>();
    }

    private void Awake()
    {
        Debug.Log("FLAG-09F4|Installed=True|Dannebrog=TwoSided|SJLR=BlueGoldPrototype|ObjectAmbiguity=False");
    }

    private void Update()
    {
        SuppressLegacyFlagSystems();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || roots.ContainsKey(regiment))
                continue;

            RemoveLegacyFlagChildren(regiment);
            roots[regiment] = CreateFlagSet(regiment);
        }
    }

    private void SuppressLegacyFlagSystems()
    {
        if (legacySuppressed)
            return;

        PrototypeRegimentalStandards standards =
            UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalStandards>();
        if (standards != null)
            standards.enabled = false;

        PrototypeNationalFlags09F2 national =
            UnityEngine.Object.FindAnyObjectByType<PrototypeNationalFlags09F2>();
        if (national != null)
            national.enabled = false;

        legacySuppressed = true;
    }

    private static void RemoveLegacyFlagChildren(Regiment regiment)
    {
        for (int i = regiment.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null)
                continue;

            if (child.name.StartsWith("NationalFlag_") ||
                child.name.StartsWith("RegimentalStandard_") ||
                child.name.StartsWith("Flags09F4_"))
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }

    private static GameObject CreateFlagSet(Regiment regiment)
    {
        GameObject root = new GameObject("Flags09F4_" + regiment.RegimentName);
        root.transform.SetParent(regiment.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        if (regiment.Team == BattleTeam.Denmark)
        {
            CreateDanishNationalFlag(root.transform, new Vector3(-1.45f, 0f, 0f));
            CreateDanishRegimentalStandard(root.transform, new Vector3(1.45f, 0f, 0f));
        }
        else
        {
            CreatePrussianNationalFlag(root.transform, new Vector3(-1.45f, 0f, 0f));
            CreatePrussianRegimentalStandard(root.transform, new Vector3(1.45f, 0f, 0f));
        }

        Debug.Log(
            "FLAG-09F4|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|National=True|Regimental=True|PlaceholderRed=False");

        return root;
    }

    private static void CreateDanishNationalFlag(Transform parent, Vector3 poleOrigin)
    {
        Material pole = Mat(new Color(0.31f, 0.19f, 0.07f), "09F4_DK_Pole");
        Material red = Mat(new Color(0.72f, 0.02f, 0.045f), "09F4_DannebrogRed");
        Material white = Mat(new Color(0.98f, 0.97f, 0.93f), "09F4_DannebrogWhite");
        Material gold = Mat(new Color(0.82f, 0.65f, 0.20f), "09F4_DK_Gold");

        CreatePole(parent, poleOrigin, pole);

        Vector3 center = poleOrigin + new Vector3(-1.15f, 2.95f, 0f);
        CreateBox(parent, "Dannebrog_Field", center, new Vector3(2.30f, 1.30f, 0.060f), red);

        float crossX = poleOrigin.x - 0.72f;
        CreateBox(parent, "Dannebrog_V_Front", new Vector3(crossX, 2.95f, -0.040f), new Vector3(0.22f, 1.31f, 0.025f), white);
        CreateBox(parent, "Dannebrog_H_Front", new Vector3(center.x, 2.95f, -0.040f), new Vector3(2.31f, 0.22f, 0.025f), white);
        CreateBox(parent, "Dannebrog_V_Back", new Vector3(crossX, 2.95f, 0.040f), new Vector3(0.22f, 1.31f, 0.025f), white);
        CreateBox(parent, "Dannebrog_H_Back", new Vector3(center.x, 2.95f, 0.040f), new Vector3(2.31f, 0.22f, 0.025f), white);

        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), gold);
    }

    private static void CreateDanishRegimentalStandard(Transform parent, Vector3 poleOrigin)
    {
        Material pole = Mat(new Color(0.31f, 0.19f, 0.07f), "09F4_SJLR_Pole");
        Material navy = Mat(new Color(0.018f, 0.055f, 0.125f), "09F4_SJLR_Navy");
        Material gold = Mat(new Color(0.86f, 0.66f, 0.18f), "09F4_SJLR_Gold");
        Material ivory = Mat(new Color(0.88f, 0.84f, 0.70f), "09F4_SJLR_Ivory");

        CreatePole(parent, poleOrigin, pole);
        Vector3 center = poleOrigin + new Vector3(1.15f, 2.95f, 0f);

        CreateBox(parent, "SJLR_Field", center, new Vector3(2.30f, 1.30f, 0.060f), navy);
        CreateBorder(parent, center, -0.042f, gold);
        CreateBorder(parent, center, 0.042f, gold);

        // Heraldic left shield block inspired by the approved reference.
        CreateBox(parent, "SJLR_Shield", center + new Vector3(-0.63f, 0.02f, -0.047f), new Vector3(0.54f, 0.72f, 0.025f), ivory);
        CreateBox(parent, "SJLR_Shield_Back", center + new Vector3(-0.63f, 0.02f, 0.047f), new Vector3(0.54f, 0.72f, 0.025f), ivory);

        for (int i = 0; i < 3; i++)
        {
            float y = center.y + 0.22f - i * 0.22f;
            CreateLionMark(parent, new Vector3(center.x - 0.63f, y, -0.064f), gold, false);
            CreateLionMark(parent, new Vector3(center.x - 0.63f, y, 0.064f), gold, true);
        }

        // Crowned monogram / wreath area on the right side.
        CreateRing(parent, center + new Vector3(0.52f, 0.06f, -0.050f), gold, false);
        CreateRing(parent, center + new Vector3(0.52f, 0.06f, 0.050f), gold, true);
        CreateText(parent, "C4", center + new Vector3(0.50f, 0.04f, -0.068f), 0.20f, gold.color, false);
        CreateText(parent, "C4", center + new Vector3(0.50f, 0.04f, 0.068f), 0.20f, gold.color, true);

        CreateText(parent, "SJÆLLANDSKE LIVREGIMENT", center + new Vector3(0f, 0.47f, -0.068f), 0.105f, gold.color, false);
        CreateText(parent, "SJÆLLANDSKE LIVREGIMENT", center + new Vector3(0f, 0.47f, 0.068f), 0.105f, gold.color, true);

        CreateMottoRibbon(parent, center, -0.052f, gold, navy, false);
        CreateMottoRibbon(parent, center, 0.052f, gold, navy, true);

        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), gold);
    }

    private static void CreateMottoRibbon(
        Transform parent,
        Vector3 center,
        float z,
        Material gold,
        Material navy,
        bool back)
    {
        Vector3 ribbonCenter = center + new Vector3(0f, -0.46f, z);
        CreateBox(parent, back ? "SJLR_Ribbon_Back" : "SJLR_Ribbon_Front", ribbonCenter, new Vector3(1.75f, 0.22f, 0.025f), gold);
        CreateText(parent, "TAPPER OG TRO", ribbonCenter + new Vector3(0f, 0f, back ? 0.020f : -0.020f), 0.13f, navy.color, back);
    }

    private static void CreateBorder(Transform parent, Vector3 center, float z, Material material)
    {
        CreateBox(parent, "BorderTop", center + new Vector3(0f, 0.59f, z), new Vector3(2.18f, 0.055f, 0.020f), material);
        CreateBox(parent, "BorderBottom", center + new Vector3(0f, -0.59f, z), new Vector3(2.18f, 0.055f, 0.020f), material);
        CreateBox(parent, "BorderLeft", center + new Vector3(-1.04f, 0f, z), new Vector3(0.055f, 1.18f, 0.020f), material);
        CreateBox(parent, "BorderRight", center + new Vector3(1.04f, 0f, z), new Vector3(0.055f, 1.18f, 0.020f), material);
    }

    private static void CreateLionMark(Transform parent, Vector3 position, Material material, bool back)
    {
        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mark.name = "SJLR_LionMark";
        mark.transform.SetParent(parent, false);
        mark.transform.localPosition = position;
        mark.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        mark.transform.localScale = new Vector3(0.055f, 0.11f, 0.055f);
        mark.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(mark.GetComponent<Collider>());
    }

    private static void CreateRing(Transform parent, Vector3 position, Material material, bool back)
    {
        const int pieces = 16;
        const float radius = 0.30f;
        for (int i = 0; i < pieces; i++)
        {
            float angle = i * Mathf.PI * 2f / pieces;
            Vector3 p = position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            CreateBox(parent, "SJLR_Wreath", p, new Vector3(0.055f, 0.055f, 0.020f), material);
        }
    }

    private static void CreatePrussianNationalFlag(Transform parent, Vector3 poleOrigin)
    {
        Material pole = Mat(new Color(0.31f, 0.19f, 0.07f), "09F4_PR_Pole");
        Material white = Mat(new Color(0.92f, 0.91f, 0.86f), "09F4_PR_White");
        Material black = Mat(new Color(0.025f, 0.025f, 0.028f), "09F4_PR_Black");

        CreatePole(parent, poleOrigin, pole);
        Vector3 center = poleOrigin + new Vector3(-1.15f, 2.95f, 0f);
        CreateBox(parent, "Prussian_Field", center, new Vector3(2.30f, 1.30f, 0.060f), white);
        CreateBox(parent, "Prussian_CrossV_Front", new Vector3(center.x, 2.95f, -0.040f), new Vector3(0.20f, 0.90f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossH_Front", new Vector3(center.x, 2.95f, -0.040f), new Vector3(0.90f, 0.20f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossV_Back", new Vector3(center.x, 2.95f, 0.040f), new Vector3(0.20f, 0.90f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossH_Back", new Vector3(center.x, 2.95f, 0.040f), new Vector3(0.90f, 0.20f, 0.025f), black);
        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), black);
    }

    private static void CreatePrussianRegimentalStandard(Transform parent, Vector3 poleOrigin)
    {
        Material pole = Mat(new Color(0.31f, 0.19f, 0.07f), "09F4_PR_RegPole");
        Material black = Mat(new Color(0.035f, 0.035f, 0.04f), "09F4_PR_RegBlack");
        Material white = Mat(new Color(0.90f, 0.88f, 0.80f), "09F4_PR_RegWhite");

        CreatePole(parent, poleOrigin, pole);
        Vector3 center = poleOrigin + new Vector3(1.15f, 2.95f, 0f);
        CreateBox(parent, "Prussian_RegimentalField", center, new Vector3(2.30f, 1.30f, 0.060f), black);
        CreateBorder(parent, center, -0.042f, white);
        CreateBorder(parent, center, 0.042f, white);
        CreateText(parent, "PREUSSEN", center + new Vector3(0f, 0f, -0.068f), 0.18f, white.color, false);
        CreateText(parent, "PREUSSEN", center + new Vector3(0f, 0f, 0.068f), 0.18f, white.color, true);
        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), white);
    }

    private static Material Mat(Color color, string name)
    {
        return PrototypeBootstrap.CreateSharedMaterial(color, name);
    }

    private static void CreatePole(Transform parent, Vector3 origin, Material material)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "FlagPole";
        pole.transform.SetParent(parent, false);
        pole.transform.localPosition = origin + new Vector3(0f, 1.78f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.78f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(pole.GetComponent<Collider>());
    }

    private static void CreateFinial(Transform parent, Vector3 position, Material material)
    {
        GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        finial.name = "FlagFinial";
        finial.transform.SetParent(parent, false);
        finial.transform.localPosition = position;
        finial.transform.localScale = Vector3.one * 0.15f;
        finial.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(finial.GetComponent<Collider>());
    }

    private static void CreateBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.Destroy(box.GetComponent<Collider>());
    }

    private static void CreateText(
        Transform parent,
        string text,
        Vector3 localPosition,
        float characterSize,
        Color color,
        bool back)
    {
        GameObject textObject = new GameObject("FlagText_" + text);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = back
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;

        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = characterSize;
        mesh.fontSize = 64;
        mesh.color = color;
    }
}
