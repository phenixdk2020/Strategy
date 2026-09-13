using System.Collections.Generic;
using UnityEngine;

// v00.00.09f2: each regiment carries two distinct identifiers:
// 1) national flag (Denmark / Prussia), 2) existing regimental standard.
// Prototype geometry only; final historical colours/patterns remain source-validation work.
[DefaultExecutionOrder(9050)]
public sealed class PrototypeNationalFlags09F2 : MonoBehaviour
{
    private readonly Dictionary<Regiment, GameObject> flags =
        new Dictionary<Regiment, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNationalFlags09F2>() != null)
            return;

        GameObject root = new GameObject("PrototypeNationalFlags_v000009f2");
        root.AddComponent<PrototypeNationalFlags09F2>();
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || flags.ContainsKey(regiment))
                continue;

            flags[regiment] = CreateNationalFlag(regiment);
        }
    }

    private static GameObject CreateNationalFlag(Regiment regiment)
    {
        GameObject root = new GameObject("NationalFlag_" + regiment.RegimentName);
        root.transform.SetParent(regiment.transform, false);
        root.transform.localPosition = new Vector3(-1.55f, 0f, 0f);
        root.transform.localRotation = Quaternion.identity;

        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.28f, 0.18f, 0.08f),
            "NationalFlagPole");

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "NationalFlagPole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.75f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        Object.Destroy(pole.GetComponent<Collider>());

        if (regiment.Team == BattleTeam.Denmark)
            BuildDanishFlag(root.transform);
        else
            BuildPrussianFlag(root.transform);

        Debug.Log(
            "FLAG-09F2|Unit=" + regiment.RegimentName +
            "|Nation=" + (regiment.Team == BattleTeam.Denmark ? "Denmark" : "Prussia") +
            "|NationalFlag=True|RegimentalStandard=True");

        return root;
    }

    private static void BuildDanishFlag(Transform parent)
    {
        Material red = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.72f, 0.035f, 0.055f),
            "DannebrogRed");
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.97f, 0.97f, 0.94f),
            "DannebrogWhite");

        CreateCloth(parent, "DannebrogField", new Vector3(-0.82f, 2.95f, 0f), new Vector3(1.65f, 0.95f, 0.055f), red);
        CreateCloth(parent, "DannebrogVertical", new Vector3(-0.52f, 2.95f, -0.034f), new Vector3(0.15f, 0.96f, 0.025f), white);
        CreateCloth(parent, "DannebrogHorizontal", new Vector3(-0.82f, 2.95f, -0.034f), new Vector3(1.66f, 0.15f, 0.025f), white);
    }

    private static void BuildPrussianFlag(Transform parent)
    {
        Material white = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.93f, 0.92f, 0.86f),
            "PrussianNationalWhite");
        Material black = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.055f, 0.055f, 0.06f),
            "PrussianNationalBlack");

        CreateCloth(parent, "PrussianNationalField", new Vector3(-0.82f, 2.95f, 0f), new Vector3(1.65f, 0.95f, 0.055f), white);
        CreateCloth(parent, "PrussianCrossVertical", new Vector3(-0.82f, 2.95f, -0.034f), new Vector3(0.16f, 0.70f, 0.025f), black);
        CreateCloth(parent, "PrussianCrossHorizontal", new Vector3(-0.82f, 2.95f, -0.034f), new Vector3(0.70f, 0.16f, 0.025f), black);
    }

    private static void CreateCloth(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(part.GetComponent<Collider>());
    }
}
