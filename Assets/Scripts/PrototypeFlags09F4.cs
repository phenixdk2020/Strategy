using System;
using System.Collections.Generic;
using UnityEngine;

// v00.00.09f4 flag pass.
// Danish units carry a clearly readable two-sided Dannebrog plus an SJLR regimental
// standard derived from the user-approved visual reference. The older QA flags are
// disabled before they can create duplicate/plain-red standards.
[DefaultExecutionOrder(8800)]
public sealed class PrototypeFlags09F4 : MonoBehaviour
{
    private readonly Dictionary<Regiment, GameObject> roots =
        new Dictionary<Regiment, GameObject>();

    private bool legacySuppressed;
    private Texture2D sjlrTexture;
    private Material sjlrMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFlags09F4>() != null)
            return;

        GameObject root = new GameObject("PrototypeFlags_v000009f4");
        root.AddComponent<PrototypeFlags09F4>();
    }

    private void Awake()
    {
        BuildSJLRMaterial();
        Debug.Log("FLAG-09F4|Installed=True|Dannebrog=TwoSided|SJLRReferenceTexture=True");
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
            Object.FindAnyObjectByType<PrototypeRegimentalStandards>();
        if (standards != null)
            standards.enabled = false;

        PrototypeNationalFlags09F2 national =
            Object.FindAnyObjectByType<PrototypeNationalFlags09F2>();
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
                Object.Destroy(child.gameObject);
            }
        }
    }

    private GameObject CreateFlagSet(Regiment regiment)
    {
        GameObject root = new GameObject("Flags09F4_" + regiment.RegimentName);
        root.transform.SetParent(regiment.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        if (regiment.Team == BattleTeam.Denmark)
        {
            CreateDanishNationalFlag(root.transform, new Vector3(-1.30f, 0f, 0f));
            CreateDanishRegimentalStandard(root.transform, new Vector3(1.30f, 0f, 0f));
        }
        else
        {
            CreatePrussianNationalFlag(root.transform, new Vector3(-1.30f, 0f, 0f));
            CreatePrussianRegimentalStandard(root.transform, new Vector3(1.30f, 0f, 0f));
        }

        Debug.Log(
            "FLAG-09F4|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|National=True|Regimental=True|PlaceholderRed=False");

        return root;
    }

    private static void CreateDanishNationalFlag(Transform parent, Vector3 poleOrigin)
    {
        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.31f, 0.19f, 0.07f), "09F4_FlagPole");
        Material red = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.72f, 0.02f, 0.045f), "09F4_DannebrogRed");
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.98f, 0.97f, 0.93f), "09F4_DannebrogWhite");

        CreatePole(parent, poleOrigin, poleMaterial);

        // Cloth extends to the left of the pole. Cross placement follows the familiar
        // Scandinavian offset and is duplicated front/back so camera direction cannot
        // make the flag look like a plain red rectangle.
        Vector3 center = poleOrigin + new Vector3(-1.15f, 2.95f, 0f);
        CreateBox(parent, "Dannebrog_Field", center, new Vector3(2.30f, 1.30f, 0.055f), red);

        float crossX = poleOrigin.x - 0.72f;
        CreateBox(parent, "Dannebrog_V_Front", new Vector3(crossX, 2.95f, -0.035f), new Vector3(0.20f, 1.31f, 0.025f), white);
        CreateBox(parent, "Dannebrog_H_Front", new Vector3(center.x, 2.95f, -0.035f), new Vector3(2.31f, 0.20f, 0.025f), white);
        CreateBox(parent, "Dannebrog_V_Back", new Vector3(crossX, 2.95f, 0.035f), new Vector3(0.20f, 1.31f, 0.025f), white);
        CreateBox(parent, "Dannebrog_H_Back", new Vector3(center.x, 2.95f, 0.035f), new Vector3(2.31f, 0.20f, 0.025f), white);

        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), white);
    }

    private void CreateDanishRegimentalStandard(Transform parent, Vector3 poleOrigin)
    {
        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.31f, 0.19f, 0.07f), "09F4_RegimentalPole");
        Material gold = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.79f, 0.61f, 0.20f), "09F4_RegimentalGold");
        Material navy = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.025f, 0.075f, 0.16f), "09F4_RegimentalNavyFallback");

        CreatePole(parent, poleOrigin, poleMaterial);
        Vector3 center = poleOrigin + new Vector3(1.15f, 2.95f, 0f);

        // Dark backing prevents the standard disappearing edge-on while the two
        // textured quads carry the user-approved SJLR artwork front and back.
        CreateBox(parent, "SJLR_Backing", center, new Vector3(2.30f, 1.30f, 0.045f), navy);
        CreateTexturedQuad(parent, "SJLR_Front", center + new Vector3(0f, 0f, -0.031f), Quaternion.identity, sjlrMaterial, new Vector3(2.28f, 1.28f, 1f));
        CreateTexturedQuad(parent, "SJLR_Back", center + new Vector3(0f, 0f, 0.031f), Quaternion.Euler(0f, 180f, 0f), sjlrMaterial, new Vector3(2.28f, 1.28f, 1f));

        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), gold);
    }

    private static void CreatePrussianNationalFlag(Transform parent, Vector3 poleOrigin)
    {
        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.31f, 0.19f, 0.07f), "09F4_FlagPolePrussia");
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.92f, 0.91f, 0.86f), "09F4_PrussianWhite");
        Material black = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.025f, 0.025f, 0.028f), "09F4_PrussianBlack");

        CreatePole(parent, poleOrigin, poleMaterial);
        Vector3 center = poleOrigin + new Vector3(-1.15f, 2.95f, 0f);
        CreateBox(parent, "Prussian_Field", center, new Vector3(2.30f, 1.30f, 0.055f), white);
        CreateBox(parent, "Prussian_CrossV_Front", new Vector3(center.x, 2.95f, -0.035f), new Vector3(0.20f, 0.90f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossH_Front", new Vector3(center.x, 2.95f, -0.035f), new Vector3(0.90f, 0.20f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossV_Back", new Vector3(center.x, 2.95f, 0.035f), new Vector3(0.20f, 0.90f, 0.025f), black);
        CreateBox(parent, "Prussian_CrossH_Back", new Vector3(center.x, 2.95f, 0.035f), new Vector3(0.90f, 0.20f, 0.025f), black);
        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), black);
    }

    private static void CreatePrussianRegimentalStandard(Transform parent, Vector3 poleOrigin)
    {
        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.31f, 0.19f, 0.07f), "09F4_RegimentalPolePrussia");
        Material black = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.035f, 0.035f, 0.04f), "09F4_PrussianStandardBlack");
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.90f, 0.88f, 0.80f), "09F4_PrussianStandardWhite");

        CreatePole(parent, poleOrigin, poleMaterial);
        Vector3 center = poleOrigin + new Vector3(1.15f, 2.95f, 0f);
        CreateBox(parent, "Prussian_RegimentalField", center, new Vector3(2.30f, 1.30f, 0.055f), black);
        CreateBox(parent, "Prussian_RegimentalDeviceV_Front", new Vector3(center.x, 2.95f, -0.035f), new Vector3(0.18f, 0.92f, 0.025f), white);
        CreateBox(parent, "Prussian_RegimentalDeviceH_Front", new Vector3(center.x, 2.95f, -0.035f), new Vector3(0.92f, 0.18f, 0.025f), white);
        CreateBox(parent, "Prussian_RegimentalDeviceV_Back", new Vector3(center.x, 2.95f, 0.035f), new Vector3(0.18f, 0.92f, 0.025f), white);
        CreateBox(parent, "Prussian_RegimentalDeviceH_Back", new Vector3(center.x, 2.95f, 0.035f), new Vector3(0.92f, 0.18f, 0.025f), white);
        CreateFinial(parent, poleOrigin + new Vector3(0f, 3.58f, 0f), white);
    }

    private void BuildSJLRMaterial()
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        sjlrTexture = new Texture2D(2, 2, TextureFormat.RGB24, false)
        {
            name = "SJLR_Reference_09F4",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        try
        {
            byte[] bytes = Convert.FromBase64String(SjlrReferenceJpegBase64);
            bool loaded = ImageConversion.LoadImage(sjlrTexture, bytes, false);
            if (!loaded)
                Debug.LogWarning("FLAG-09F4|SJLRTextureLoaded=False|Fallback=Navy");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("FLAG-09F4|SJLRTextureException=" + ex.GetType().Name);
        }

        sjlrMaterial = new Material(shader)
        {
            name = "SJLR_RegimentalStandard_09F4",
            mainTexture = sjlrTexture,
            color = Color.white
        };
    }

    private static void CreatePole(Transform parent, Vector3 origin, Material material)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "FlagPole";
        pole.transform.SetParent(parent, false);
        pole.transform.localPosition = origin + new Vector3(0f, 1.78f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.78f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(pole.GetComponent<Collider>());
    }

    private static void CreateFinial(Transform parent, Vector3 position, Material material)
    {
        GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        finial.name = "FlagFinial";
        finial.transform.SetParent(parent, false);
        finial.transform.localPosition = position;
        finial.transform.localScale = Vector3.one * 0.15f;
        finial.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(finial.GetComponent<Collider>());
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
        Object.Destroy(box.GetComponent<Collider>());
    }

    private static void CreateTexturedQuad(
        Transform parent,
        string name,
        Vector3 localPosition,
        Quaternion localRotation,
        Material material,
        Vector3 localScale)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localRotation = localRotation;
        quad.transform.localScale = localScale;
        quad.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(quad.GetComponent<Collider>());
    }

    // 192 px game-runtime derivative of the user-supplied SJLR.jpg reference.
    private const string SjlrReferenceJpegBase64 =
        "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";
}
