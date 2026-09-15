using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29p
// Lightweight visual polish pass. Adds readable company captains and distinct HQ staff
// figures plus subtle field boundaries so the enlarged battlefield has more visual scale.
// Everything in this pass is presentation-only: no colliders, navigation or combat rules.
[DefaultExecutionOrder(9100)]
public sealed class PrototypeVisualPolish09F29P : MonoBehaviour
{
    private readonly HashSet<Regiment> decoratedCompanies = new HashSet<Regiment>();
    private readonly HashSet<GameObject> decoratedHqs = new HashSet<GameObject>();

    private Material dkOfficerCoat;
    private Material prOfficerCoat;
    private Material staffCoat;
    private Material black;
    private Material brown;
    private Material skin;
    private Material gold;
    private Material steel;
    private Material fieldBorder;
    private bool battlefieldDecorated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeVisualPolish09F29P>() == null)
            new GameObject("PrototypeVisualPolish_v000009f29p")
                .AddComponent<PrototypeVisualPolish09F29P>();
    }

    private void Awake()
    {
        dkOfficerCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.055f, 0.12f, 0.25f), "09F29P_DKOfficer");
        prOfficerCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.055f, 0.055f, 0.065f), "09F29P_PROfficer");
        staffCoat = PrototypeBootstrap.CreateSharedMaterial(new Color(0.12f, 0.18f, 0.12f), "09F29P_Staff");
        black = PrototypeBootstrap.CreateSharedMaterial(new Color(0.025f, 0.025f, 0.025f), "09F29P_Black");
        brown = PrototypeBootstrap.CreateSharedMaterial(new Color(0.23f, 0.13f, 0.055f), "09F29P_Brown");
        skin = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.54f, 0.39f), "09F29P_Skin");
        gold = PrototypeBootstrap.CreateSharedMaterial(new Color(0.82f, 0.63f, 0.16f), "09F29P_Gold");
        steel = PrototypeBootstrap.CreateSharedMaterial(new Color(0.43f, 0.46f, 0.47f), "09F29P_Steel");
        fieldBorder = PrototypeBootstrap.CreateSharedMaterial(new Color(0.42f, 0.34f, 0.16f), "09F29P_FieldBorder");

        Debug.Log(
            "VISUAL-POLISH-09F29P|Installed=True|CompanyCaptains=True|HQStaff=True|" +
            "FieldBoundaries=True|Colliders=False|NavigationChanged=False");
    }

    private void Update()
    {
        DecorateCompanies();
        DecorateHeadquarters();

        if (!battlefieldDecorated && BattleManager.Instance != null)
        {
            battlefieldDecorated = true;
            CreateBattlefieldDressing();
        }
    }

    private void DecorateCompanies()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || decoratedCompanies.Contains(unit))
                continue;

            Transform existing = unit.transform.Find("CompanyOfficer09F29P");
            if (existing == null)
            {
                GameObject officer = CreateOfficerFigure(
                    "CompanyOfficer09F29P",
                    unit.Team == BattleTeam.Denmark ? dkOfficerCoat : prOfficerCoat,
                    false);
                officer.transform.SetParent(unit.transform, false);
                officer.transform.localPosition = new Vector3(0f, 0f, -1.7f);
                officer.transform.localRotation = Quaternion.identity;
            }

            decoratedCompanies.Add(unit);
        }
    }

    private void DecorateHeadquarters()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq != null)
                    DecorateMajorHq(hq, i);
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
            DecorateRegimentalHq(regimental.HqRoot);
    }

    private void DecorateMajorHq(GameObject hq, int index)
    {
        if (decoratedHqs.Contains(hq))
            return;

        GameObject root = new GameObject("MajorStaffVisual09F29P");
        root.transform.SetParent(hq.transform, false);
        root.transform.localPosition = Vector3.zero;

        GameObject major = CreateOfficerFigure("Major", dkOfficerCoat, true);
        major.transform.SetParent(root.transform, false);
        major.transform.localPosition = new Vector3(0f, 0f, 0f);

        GameObject aide = CreateOfficerFigure("Aide", staffCoat, false);
        aide.transform.SetParent(root.transform, false);
        aide.transform.localPosition = new Vector3(index == 0 ? -1.5f : 1.5f, 0f, -0.8f);
        aide.transform.localScale = Vector3.one * 0.90f;

        CreateGroundDisc(root.transform, 2.1f, new Color(0.12f, 0.48f, 0.72f, 0.50f));
        decoratedHqs.Add(hq);
    }

    private void DecorateRegimentalHq(GameObject hq)
    {
        if (decoratedHqs.Contains(hq))
            return;

        GameObject root = new GameObject("RegimentalStaffVisual09F29P");
        root.transform.SetParent(hq.transform, false);
        root.transform.localPosition = Vector3.zero;

        GameObject colonel = CreateOfficerFigure("Oberstlojtnant", dkOfficerCoat, true);
        colonel.transform.SetParent(root.transform, false);
        colonel.transform.localPosition = Vector3.zero;
        colonel.transform.localScale = Vector3.one * 1.08f;

        GameObject aideLeft = CreateOfficerFigure("StaffLeft", staffCoat, false);
        aideLeft.transform.SetParent(root.transform, false);
        aideLeft.transform.localPosition = new Vector3(-1.7f, 0f, -0.7f);
        aideLeft.transform.localScale = Vector3.one * 0.92f;

        GameObject aideRight = CreateOfficerFigure("StaffRight", staffCoat, false);
        aideRight.transform.SetParent(root.transform, false);
        aideRight.transform.localPosition = new Vector3(1.7f, 0f, -0.7f);
        aideRight.transform.localScale = Vector3.one * 0.92f;

        CreateGroundDisc(root.transform, 2.7f, new Color(0.78f, 0.56f, 0.12f, 0.48f));
        decoratedHqs.Add(hq);
    }

    private GameObject CreateOfficerFigure(string name, Material coat, bool senior)
    {
        GameObject root = new GameObject(name);

        CreatePrimitive(root.transform, PrimitiveType.Cube, "LegL",
            new Vector3(-0.11f, 0.38f, 0f), new Vector3(0.18f, 0.70f, 0.18f), Vector3.zero, black);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "LegR",
            new Vector3(0.11f, 0.38f, 0f), new Vector3(0.18f, 0.70f, 0.18f), Vector3.zero, black);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Torso",
            new Vector3(0f, 1.00f, 0f), new Vector3(0.54f, 0.70f, 0.32f), Vector3.zero, coat);
        CreatePrimitive(root.transform, PrimitiveType.Sphere, "Head",
            new Vector3(0f, 1.57f, 0f), new Vector3(0.29f, 0.29f, 0.29f), Vector3.zero, skin);
        CreatePrimitive(root.transform, PrimitiveType.Cylinder, "Headgear",
            new Vector3(0f, 1.84f, 0f), new Vector3(0.28f, senior ? 0.22f : 0.17f, 0.28f), Vector3.zero, black);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Sash",
            new Vector3(0f, 1.03f, 0.18f), new Vector3(0.58f, 0.09f, 0.04f), new Vector3(0f, 0f, -18f), gold);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Sword",
            new Vector3(0.37f, 0.78f, 0.05f), new Vector3(0.045f, 0.80f, 0.045f), new Vector3(0f, 0f, -8f), steel);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Scabbard",
            new Vector3(-0.35f, 0.73f, -0.08f), new Vector3(0.055f, 0.86f, 0.055f), new Vector3(0f, 0f, 10f), brown);

        return root;
    }

    private static void CreatePrimitive(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }

    private static void CreateGroundDisc(Transform parent, float radius, Color color)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "HQDisc09F29P";
        disc.transform.SetParent(parent, false);
        disc.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        disc.transform.localScale = new Vector3(radius, 0.018f, radius);
        Renderer renderer = disc.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(color, "09F29P_HQDisc");
        Collider collider = disc.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }

    private void CreateBattlefieldDressing()
    {
        GameObject root = new GameObject("BattlefieldDressing09F29P");

        CreateFieldBoundary(root.transform, new Vector2(-390f, -230f), 250f, 135f, -7f);
        CreateFieldBoundary(root.transform, new Vector2(-340f, 245f), 220f, 120f, 5f);
        CreateFieldBoundary(root.transform, new Vector2(370f, 250f), 245f, 135f, -4f);
        CreateFieldBoundary(root.transform, new Vector2(430f, -235f), 260f, 145f, 8f);
        CreateFieldBoundary(root.transform, new Vector2(-60f, 330f), 190f, 105f, 3f);
        CreateFieldBoundary(root.transform, new Vector2(95f, -350f), 210f, 115f, -5f);
    }

    private void CreateFieldBoundary(Transform parent, Vector2 center, float width, float depth, float yawDegrees)
    {
        GameObject go = new GameObject("FieldBoundary09F29P");
        go.transform.SetParent(parent, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 4;
        line.widthMultiplier = 0.32f;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = fieldBorder;
        line.startColor = new Color(0.53f, 0.42f, 0.20f, 0.72f);
        line.endColor = line.startColor;

        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3[] local =
        {
            new Vector3(-width * 0.5f, 0f, -depth * 0.5f),
            new Vector3(width * 0.5f, 0f, -depth * 0.5f),
            new Vector3(width * 0.5f, 0f, depth * 0.5f),
            new Vector3(-width * 0.5f, 0f, depth * 0.5f)
        };

        for (int i = 0; i < local.Length; i++)
        {
            Vector3 p = rotation * local[i] + new Vector3(center.x, 0f, center.y);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.16f;
            line.SetPosition(i, p);
        }
    }
}
