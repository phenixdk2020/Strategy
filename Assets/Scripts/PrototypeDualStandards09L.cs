using System.Collections.Generic;
using UnityEngine;

// v00.00.09l1 TEST - two visible identity standards per infantry regiment.
// Standard A = national/army identity. Standard B = regiment identity using the 1864
// OOB metadata (Roman numeral + official/traditional name). Older single-standard
// prototype renderers are hidden, not deleted, so rollback remains safe.
[DefaultExecutionOrder(11050)]
public sealed class PrototypeDualStandards09L : MonoBehaviour
{
    private sealed class StandardState
    {
        public Regiment Regiment;
        public Transform NationalCloth;
        public Transform RegimentalCloth;
        public float Phase;
    }

    private readonly Dictionary<Regiment, StandardState> states =
        new Dictionary<Regiment, StandardState>();
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeDualStandards09L>() != null)
            return;

        GameObject root = new GameObject("PrototypeDualStandards_v000009l1");
        root.AddComponent<PrototypeDualStandards09L>();
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob == null)
                continue;

            StandardState state;
            if (!states.TryGetValue(regiment, out state))
            {
                HideLegacyStandards(regiment);
                state = BuildDualStandards(regiment, oob);
                states[regiment] = state;
            }

            Animate(state);
        }

        if (!announced && states.Count >= 4)
        {
            announced = true;
            Debug.Log(
                "STANDARD-09L1|Installed=True|Mode=Dual|NationalFlag=True|RegimentalFlag=True|" +
                "RomanNumerals=True|TraditionLabels=True|LegacySingleStandardsHidden=True");
        }
    }

    private static void HideLegacyStandards(Regiment regiment)
    {
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || child.name == "DualStandards09L")
                continue;

            bool oldStandard =
                child.name.StartsWith("RegimentalStandard_") ||
                child.name.Contains("Standard09I") ||
                child.name.Contains("RegimentalStandard09I");

            if (!oldStandard)
                continue;

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
                renderers[r].enabled = false;
        }
    }

    private static StandardState BuildDualStandards(Regiment regiment, PrototypeRegimentOOB09K oob)
    {
        GameObject root = new GameObject("DualStandards09L");
        root.transform.SetParent(regiment.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, -2.4f);

        Material pole = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.27f, 0.17f, 0.075f),
            "09L_StandardPole_" + regiment.RegimentName);
        Material brass = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.75f, 0.58f, 0.20f),
            "09L_StandardBrass_" + regiment.RegimentName);

        Transform national = BuildFlag(
            root.transform,
            "NationalStandard",
            new Vector3(-2.3f, 0f, 0f),
            regiment.Team,
            true,
            string.Empty,
            string.Empty,
            pole,
            brass);

        string tradition = string.IsNullOrEmpty(oob.TraditionalName)
            ? oob.OfficialName1864
            : oob.TraditionalName;

        Transform regimental = BuildFlag(
            root.transform,
            "RegimentalStandard",
            new Vector3(2.3f, 0f, 0f),
            regiment.Team,
            false,
            oob.RegimentalRomanNumeral,
            tradition,
            pole,
            brass);

        Debug.Log(
            "STANDARD-09L1|Unit=" + oob.OfficialName1864 +
            "|Tradition=" + (string.IsNullOrEmpty(oob.TraditionalName) ? "None" : oob.TraditionalName) +
            "|National=True|Regimental=True|Roman=" + oob.RegimentalRomanNumeral);

        return new StandardState
        {
            Regiment = regiment,
            NationalCloth = national,
            RegimentalCloth = regimental,
            // Unity 6.6 obsoletes GetInstanceID(). A stable visual phase does not need an
            // engine object identifier, so derive it from the regiment's starting position.
            Phase = Mathf.Abs(regiment.transform.position.x * 0.017f + regiment.transform.position.z * 0.013f)
        };
    }

    private static Transform BuildFlag(
        Transform parent,
        string flagName,
        Vector3 localOffset,
        BattleTeam team,
        bool national,
        string numeral,
        string traditionLabel,
        Material poleMaterial,
        Material brassMaterial)
    {
        GameObject flag = new GameObject(flagName);
        flag.transform.SetParent(parent, false);
        flag.transform.localPosition = localOffset;

        CreatePart(flag.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 2.25f, 0f), new Vector3(0.055f, 2.25f, 0.055f), Quaternion.identity, poleMaterial);
        CreatePart(flag.transform, PrimitiveType.Cube, "Finial", new Vector3(0f, 4.62f, 0f), new Vector3(0.17f, 0.28f, 0.17f), Quaternion.Euler(0f, 45f, 45f), brassMaterial);

        Color field;
        Color device;
        if (team == BattleTeam.Denmark)
        {
            field = new Color(0.75f, 0.045f, 0.065f);
            device = new Color(0.96f, 0.96f, 0.93f);
        }
        else
        {
            field = national
                ? new Color(0.92f, 0.92f, 0.89f)
                : new Color(0.88f, 0.86f, 0.78f);
            device = new Color(0.055f, 0.055f, 0.065f);
        }

        Material fieldMaterial = PrototypeBootstrap.CreateSharedMaterial(field, "09L_" + flagName + "Field");
        Material deviceMaterial = PrototypeBootstrap.CreateSharedMaterial(device, "09L_" + flagName + "Device");

        GameObject clothRoot = new GameObject("ClothRoot");
        clothRoot.transform.SetParent(flag.transform, false);
        clothRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

        CreatePart(
            clothRoot.transform,
            PrimitiveType.Cube,
            "Cloth",
            new Vector3(1.20f, 3.72f, 0f),
            new Vector3(2.38f, 1.42f, 0.045f),
            Quaternion.identity,
            fieldMaterial);

        if (team == BattleTeam.Denmark)
        {
            CreatePart(clothRoot.transform, PrimitiveType.Cube, "DannebrogVertical", new Vector3(0.72f, 3.72f, -0.032f), new Vector3(0.18f, 1.43f, 0.018f), Quaternion.identity, deviceMaterial);
            CreatePart(clothRoot.transform, PrimitiveType.Cube, "DannebrogHorizontal", new Vector3(1.20f, 3.72f, -0.032f), new Vector3(2.40f, 0.17f, 0.018f), Quaternion.identity, deviceMaterial);
        }
        else
        {
            CreatePart(clothRoot.transform, PrimitiveType.Cube, "PrussianVertical", new Vector3(1.20f, 3.72f, -0.032f), new Vector3(0.18f, 1.05f, 0.018f), Quaternion.identity, deviceMaterial);
            CreatePart(clothRoot.transform, PrimitiveType.Cube, "PrussianHorizontal", new Vector3(1.20f, 3.72f, -0.032f), new Vector3(1.05f, 0.18f, 0.018f), Quaternion.identity, deviceMaterial);
        }

        if (!national && !string.IsNullOrEmpty(numeral))
        {
            AddText(
                clothRoot.transform,
                "RegimentNumeral",
                numeral,
                new Vector3(1.56f, 3.69f, -0.070f),
                0.15f,
                team == BattleTeam.Denmark ? new Color(0.94f, 0.78f, 0.22f) : Color.black);
        }

        if (!national && !string.IsNullOrEmpty(traditionLabel))
        {
            AddText(
                flag.transform,
                "TraditionLabel",
                traditionLabel,
                new Vector3(1.15f, 2.76f, -0.08f),
                0.065f,
                Color.white);
        }

        // Small fold at the fly edge makes the pair easier to read as two flags.
        GameObject fold = CreatePart(
            clothRoot.transform,
            PrimitiveType.Cube,
            "FlyFold",
            new Vector3(2.34f, 3.72f, 0.11f),
            new Vector3(0.32f, 1.34f, 0.04f),
            Quaternion.Euler(0f, 14f, 0f),
            fieldMaterial);
        fold.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);

        return clothRoot.transform;
    }

    private static void AddText(
        Transform parent,
        string name,
        string text,
        Vector3 localPosition,
        float characterSize,
        Color color)
    {
        GameObject label = new GameObject(name);
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = characterSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
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
            Object.Destroy(collider);
        return part;
    }

    private static void Animate(StandardState state)
    {
        if (state == null)
            return;

        float waveA = Mathf.Sin(Time.time * 2.4f + state.Phase) * 2.4f;
        float waveB = Mathf.Sin(Time.time * 2.1f + state.Phase + 1.1f) * 2.8f;

        if (state.NationalCloth != null)
            state.NationalCloth.localRotation = Quaternion.Euler(0f, waveA, 0f);
        if (state.RegimentalCloth != null)
            state.RegimentalCloth.localRotation = Quaternion.Euler(0f, waveB, 0f);
    }
}
