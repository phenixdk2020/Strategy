using System.Collections.Generic;
using UnityEngine;

// v00.00.09m8 - one national flag + one regiment flag behind each living regiment.
[DefaultExecutionOrder(12120)]
public sealed class PrototypeKampRegimentFlags09M8 : MonoBehaviour
{
    private sealed class FlagPair
    {
        public Transform Root;
        public Regiment Regiment;
    }

    private readonly Dictionary<Regiment, FlagPair> pairs = new Dictionary<Regiment, FlagPair>();
    private Transform visualRoot;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampRegimentFlags09M8>() != null)
            return;
        GameObject root = new GameObject("PrototypeKampRegimentFlags_v000009m8");
        root.AddComponent<PrototypeKampRegimentFlags09M8>();
    }

    private void Awake()
    {
        visualRoot = new GameObject("RegimentFlagsRoot09M8").transform;
        visualRoot.SetParent(transform, false);
    }

    private void LateUpdate()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        HashSet<Regiment> live = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || company.CurrentStrength <= 0)
                continue;
            live.Add(company.ParentRegiment);
        }

        foreach (Regiment regiment in live)
            PlacePair(regiment, companies);

        if (!announced)
        {
            announced = true;
            Debug.Log("KAMP-FLAGS-09M8|Installed=True|National+Regiment=PerRegiment|Planted=BehindLine");
        }
    }

    private void PlacePair(Regiment regiment, IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies)
    {
        Vector3 centroid = Vector3.zero;
        Vector3 facing = Vector3.zero;
        int count = 0;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment != regiment || company.CurrentStrength <= 0)
                continue;
            centroid += company.transform.position;
            Vector3 f = company.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f)
                facing += f.normalized;
            count++;
        }
        if (count <= 0)
            return;
        centroid /= count;
        if (facing.sqrMagnitude < 0.01f)
            facing = regiment.transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;
        facing.Normalize();
        Vector3 pos = centroid - facing * 11.5f;
        pos.y = PrototypeBootstrap.SampleGroundHeight(pos.x, pos.z);
        FlagPair pair;
        if (!pairs.TryGetValue(regiment, out pair) || pair.Root == null)
        {
            pair = new FlagPair { Regiment = regiment, Root = BuildPair(regiment) };
            pairs[regiment] = pair;
        }
        pair.Root.gameObject.SetActive(true);
        pair.Root.position = pos;
        pair.Root.rotation = Quaternion.LookRotation(facing, Vector3.up);
        pair.Root.GetChild(0).localPosition = new Vector3(-1.6f, 0f, 0f);
        pair.Root.GetChild(1).localPosition = new Vector3(1.6f, 0f, 0f);
    }

    private Transform BuildPair(Regiment regiment)
    {
        GameObject root = new GameObject("Flags09M8_" + regiment.RegimentName);
        root.transform.SetParent(visualRoot, false);
        BuildFlag(root.transform, regiment, true);
        BuildFlag(root.transform, regiment, false);
        return root.transform;
    }

    private static void BuildFlag(Transform parent, Regiment regiment, bool national)
    {
        GameObject flag = new GameObject(national ? "National" : "Regiment");
        flag.transform.SetParent(parent, false);
        Material pole = PrototypeBootstrap.CreateSharedMaterial(new Color(0.28f, 0.17f, 0.07f), "09M8_Pole");
        Material brass = PrototypeBootstrap.CreateSharedMaterial(new Color(0.76f, 0.58f, 0.18f), "09M8_Brass");
        Color field;
        Color device;
        if (regiment.Team == BattleTeam.Denmark)
        {
            field = new Color(0.78f, 0.05f, 0.08f);
            device = new Color(0.97f, 0.97f, 0.95f);
        }
        else if (national)
        {
            field = new Color(0.08f, 0.08f, 0.09f);
            device = new Color(0.93f, 0.93f, 0.90f);
        }
        else
        {
            field = new Color(0.90f, 0.90f, 0.86f);
            device = new Color(0.08f, 0.08f, 0.09f);
        }
        Material fieldMat = PrototypeBootstrap.CreateSharedMaterial(field, "09M8_Field_" + regiment.RegimentName + (national ? "_N" : "_R"));
        Material deviceMat = PrototypeBootstrap.CreateSharedMaterial(device, "09M8_Device_" + regiment.RegimentName + (national ? "_N" : "_R"));
        CreatePart(flag.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 3.1f, 0f), new Vector3(0.07f, 3.1f, 0.07f), Quaternion.identity, pole);
        CreatePart(flag.transform, PrimitiveType.Cube, "Finial", new Vector3(0f, 6.25f, 0f), new Vector3(0.20f, 0.28f, 0.20f), Quaternion.Euler(0f, 45f, 45f), brass);
        CreatePart(flag.transform, PrimitiveType.Cube, "Cloth", new Vector3(1.70f, 4.85f, 0f), new Vector3(3.40f, 1.90f, 0.06f), Quaternion.identity, fieldMat);
        if (regiment.Team == BattleTeam.Denmark)
        {
            CreatePart(flag.transform, PrimitiveType.Cube, "CrossV", new Vector3(0.95f, 4.85f, -0.04f), new Vector3(0.28f, 1.92f, 0.03f), Quaternion.identity, deviceMat);
            CreatePart(flag.transform, PrimitiveType.Cube, "CrossH", new Vector3(1.70f, 4.85f, -0.04f), new Vector3(3.42f, 0.28f, 0.03f), Quaternion.identity, deviceMat);
        }
        else
        {
            CreatePart(flag.transform, PrimitiveType.Cube, "Bar1", new Vector3(1.70f, 5.35f, -0.04f), new Vector3(3.42f, 0.32f, 0.03f), Quaternion.identity, deviceMat);
            CreatePart(flag.transform, PrimitiveType.Cube, "Bar2", new Vector3(1.70f, 4.35f, -0.04f), new Vector3(3.42f, 0.32f, 0.03f), Quaternion.identity, deviceMat);
        }
    }

    private static void CreatePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
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
        if (collider != null) Object.Destroy(collider);
    }
}
