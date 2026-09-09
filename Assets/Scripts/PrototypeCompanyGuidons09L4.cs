using System.Collections.Generic;
using UnityEngine;

// v00.00.09l4 - tactical company identification markers.
// The historically significant full national/regimental standards remain with the
// regiment HQ. Each company receives two smaller QA guidons so every independently
// controllable formation has a visible identity anchor: national + regiment/company.
[DefaultExecutionOrder(11120)]
public sealed class PrototypeCompanyGuidons09L4 : MonoBehaviour
{
    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, Transform> roots =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, Transform>();
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyGuidons09L4>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyGuidons_v000009l4");
        root.AddComponent<PrototypeCompanyGuidons09L4>();
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        int active = 0;
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || !company.gameObject.activeInHierarchy)
                continue;

            active++;
            if (!roots.ContainsKey(company) || roots[company] == null)
                roots[company] = BuildGuidons(company);

            Transform root = roots[company];
            if (root != null)
                root.localRotation = Quaternion.identity;
        }

        if (!announced && active > 0)
        {
            announced = true;
            Debug.Log(
                "GUIDON-09L4|Installed=True|Companies=" + active +
                "|PerCompany=2|NationalMarker=True|RegimentCompanyMarker=True|" +
                "HistoricalFullStandardsRemainAtRegimentHQ=True");
        }
    }

    private static Transform BuildGuidons(PrototypeCompanyTacticalEntity09L2 company)
    {
        GameObject root = new GameObject("CompanyGuidons09L4");
        root.transform.SetParent(company.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, 0f);

        Regiment regiment = company.ParentRegiment;
        PrototypeRegimentOOB09K oob = company.ParentOOB;

        Material pole = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.25f, 0.16f, 0.07f),
            "09L4_GuidonPole_" + company.CompanyId);

        Material nationalField;
        Material nationalDevice;
        Material companyField;
        Material companyDevice;

        if (regiment.Team == BattleTeam.Denmark)
        {
            nationalField = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.76f, 0.045f, 0.06f),
                "09L4_DK_National_" + company.CompanyId);
            nationalDevice = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.96f, 0.96f, 0.93f),
                "09L4_DK_White_" + company.CompanyId);
            companyField = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.58f, 0.035f, 0.05f),
                "09L4_DK_Company_" + company.CompanyId);
            companyDevice = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.94f, 0.76f, 0.20f),
                "09L4_DK_Gold_" + company.CompanyId);
        }
        else
        {
            nationalField = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.92f, 0.92f, 0.89f),
                "09L4_PR_National_" + company.CompanyId);
            nationalDevice = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.05f, 0.05f, 0.06f),
                "09L4_PR_Black_" + company.CompanyId);
            companyField = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.10f, 0.10f, 0.12f),
                "09L4_PR_Company_" + company.CompanyId);
            companyDevice = PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.90f, 0.90f, 0.84f),
                "09L4_PR_Light_" + company.CompanyId);
        }

        BuildSmallFlag(
            root.transform,
            "NationalGuidon",
            new Vector3(-0.85f, 0f, 0f),
            regiment.Team,
            true,
            string.Empty,
            pole,
            nationalField,
            nationalDevice);

        string regimentId = oob != null && !string.IsNullOrEmpty(oob.RegimentalRomanNumeral)
            ? oob.RegimentalRomanNumeral
            : regiment.RegimentName;
        string marker = regimentId + "-" + (company.CompanyIndex + 1);

        BuildSmallFlag(
            root.transform,
            "CompanyGuidon",
            new Vector3(0.85f, 0f, 0f),
            regiment.Team,
            false,
            marker,
            pole,
            companyField,
            companyDevice);

        return root.transform;
    }

    private static void BuildSmallFlag(
        Transform parent,
        string name,
        Vector3 offset,
        BattleTeam team,
        bool national,
        string marker,
        Material poleMaterial,
        Material fieldMaterial,
        Material deviceMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = offset;

        CreatePart(
            root.transform,
            PrimitiveType.Cylinder,
            "Pole",
            new Vector3(0f, 1.65f, 0f),
            new Vector3(0.025f, 1.65f, 0.025f),
            Quaternion.identity,
            poleMaterial);

        CreatePart(
            root.transform,
            PrimitiveType.Cube,
            "Cloth",
            new Vector3(0.62f, 2.78f, 0f),
            new Vector3(1.22f, 0.68f, 0.035f),
            Quaternion.identity,
            fieldMaterial);

        if (national && team == BattleTeam.Denmark)
        {
            CreatePart(root.transform, PrimitiveType.Cube, "CrossV", new Vector3(0.43f, 2.78f, -0.026f), new Vector3(0.10f, 0.69f, 0.012f), Quaternion.identity, deviceMaterial);
            CreatePart(root.transform, PrimitiveType.Cube, "CrossH", new Vector3(0.62f, 2.78f, -0.026f), new Vector3(1.23f, 0.09f, 0.012f), Quaternion.identity, deviceMaterial);
        }
        else if (national && team == BattleTeam.Prussia)
        {
            CreatePart(root.transform, PrimitiveType.Cube, "CrossV", new Vector3(0.62f, 2.78f, -0.026f), new Vector3(0.10f, 0.50f, 0.012f), Quaternion.identity, deviceMaterial);
            CreatePart(root.transform, PrimitiveType.Cube, "CrossH", new Vector3(0.62f, 2.78f, -0.026f), new Vector3(0.72f, 0.09f, 0.012f), Quaternion.identity, deviceMaterial);
        }

        if (!national && !string.IsNullOrEmpty(marker))
        {
            GameObject label = new GameObject("Marker");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0.62f, 2.77f, -0.055f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMesh tm = label.AddComponent<TextMesh>();
            tm.text = marker;
            tm.fontSize = 48;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = team == BattleTeam.Denmark
                ? new Color(0.95f, 0.78f, 0.22f)
                : new Color(0.94f, 0.94f, 0.90f);
        }
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
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localRotation = localRotation;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        return go;
    }
}
