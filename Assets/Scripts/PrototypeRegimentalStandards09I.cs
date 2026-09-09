using System.Collections.Generic;
using UnityEngine;

// v00.00.09i TEST - regimental standard visual overhaul.
// Presentation-only replacement layer for the 09g QA flag. Builds a larger procedural
// cloth mesh with wind motion, improved pole/finial/cord/ribbon silhouette and keeps
// colours synced to the per-regiment Uniform Designer profile.
[DefaultExecutionOrder(11800)]
public sealed class PrototypeRegimentalStandards09I : MonoBehaviour
{
    private sealed class StandardRig
    {
        public Regiment Regiment;
        public Transform Root;
        public Mesh ClothMesh;
        public Vector3[] BaseVertices;
        public Vector3[] WorkingVertices;
        public int Columns;
        public int Rows;
        public float Width;
        public Material FieldMaterial;
        public Material DeviceMaterial;
        public Material RibbonMaterial;
        public Transform DeviceRoot;
        public Transform RibbonA;
        public Transform RibbonB;
        public float Phase;
        public float NextColorRefresh;
    }

    private readonly Dictionary<Regiment, StandardRig> standards =
        new Dictionary<Regiment, StandardRig>();

    private const int ClothColumns = 8;
    private const int ClothRows = 5;
    private const float ClothWidth = 2.35f;
    private const float ClothHeight = 1.38f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeRegimentalStandards09I>() != null)
            return;

        GameObject root = new GameObject("PrototypeRegimentalStandards_v000009i");
        root.AddComponent<PrototypeRegimentalStandards09I>();
    }

    private void Start()
    {
        Debug.Log(
            "STANDARD-09I|Installed=True|ProceduralCloth=True|WindAnimation=True|" +
            "UniformDesignerSync=True|MovementWrites=False");
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

            StandardRig rig;
            if (!standards.TryGetValue(regiment, out rig))
            {
                HideLegacyStandard(regiment);
                rig = CreateStandard(regiment);
                standards[regiment] = rig;
            }

            AnimateStandard(rig);
            if (Time.unscaledTime >= rig.NextColorRefresh)
            {
                rig.NextColorRefresh = Time.unscaledTime + 0.20f;
                ApplyProfileColors(rig);
            }
        }

        CleanupDestroyedRegiments();
    }

    private static StandardRig CreateStandard(Regiment regiment)
    {
        GameObject rootObject = new GameObject("RegimentalStandard09I_" + regiment.RegimentName);
        rootObject.transform.SetParent(regiment.transform, false);
        rootObject.transform.localPosition = new Vector3(0f, 0f, 0.10f);
        Transform root = rootObject.transform;

        Material poleMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.25f, 0.14f, 0.055f),
            "09I_StandardPole_" + regiment.RegimentName);
        Material brassMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.72f, 0.55f, 0.18f),
            "09I_StandardBrass_" + regiment.RegimentName);

        PrototypeUniformProfile09H profile = GetProfile(regiment);
        Material fieldMaterial = CreateUnlitMaterial(
            profile.FlagPrimaryColor,
            "09I_StandardField_" + regiment.RegimentName);
        Material deviceMaterial = CreateUnlitMaterial(
            profile.FlagSecondaryColor,
            "09I_StandardDevice_" + regiment.RegimentName);
        Material ribbonMaterial = CreateUnlitMaterial(
            profile.RibbonColor,
            "09I_StandardRibbon_" + regiment.RegimentName);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cylinder,
            "Pole",
            new Vector3(0f, 2.12f, 0f),
            new Vector3(0.045f, 2.12f, 0.045f),
            Quaternion.identity,
            poleMaterial);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cylinder,
            "PoleFerrule",
            new Vector3(0f, 4.12f, 0f),
            new Vector3(0.070f, 0.085f, 0.070f),
            Quaternion.identity,
            brassMaterial);

        CreateSpearFinial(root, brassMaterial);

        Mesh clothMesh;
        Vector3[] baseVertices;
        GameObject cloth = CreateCloth(
            root,
            fieldMaterial,
            out clothMesh,
            out baseVertices);
        cloth.transform.localPosition = new Vector3(0.03f, 4.00f, 0f);

        GameObject deviceRootObject = new GameObject("FlagDevice");
        deviceRootObject.transform.SetParent(root, false);
        Transform deviceRoot = deviceRootObject.transform;

        if (regiment.Team == BattleTeam.Denmark)
        {
            CreatePrimitivePart(
                deviceRoot,
                PrimitiveType.Cube,
                "DannebrogVertical",
                new Vector3(0.73f, 3.31f, -0.026f),
                new Vector3(0.16f, 1.31f, 0.022f),
                Quaternion.identity,
                deviceMaterial);
            CreatePrimitivePart(
                deviceRoot,
                PrimitiveType.Cube,
                "DannebrogHorizontal",
                new Vector3(1.20f, 3.34f, -0.028f),
                new Vector3(2.23f, 0.16f, 0.022f),
                Quaternion.identity,
                deviceMaterial);
        }
        else
        {
            CreatePrimitivePart(
                deviceRoot,
                PrimitiveType.Cube,
                "PrussianDeviceVertical",
                new Vector3(1.17f, 3.31f, -0.026f),
                new Vector3(0.18f, 0.84f, 0.022f),
                Quaternion.identity,
                deviceMaterial);
            CreatePrimitivePart(
                deviceRoot,
                PrimitiveType.Cube,
                "PrussianDeviceHorizontal",
                new Vector3(1.17f, 3.31f, -0.028f),
                new Vector3(0.86f, 0.18f, 0.022f),
                Quaternion.identity,
                deviceMaterial);
        }

        CreateCord(
            root,
            new Vector3(0.03f, 4.00f, -0.055f),
            new Vector3(0.38f, 2.96f, -0.055f),
            brassMaterial,
            "CordA");
        CreateCord(
            root,
            new Vector3(0.08f, 3.94f, 0.055f),
            new Vector3(0.48f, 2.86f, 0.055f),
            brassMaterial,
            "CordB");

        Transform ribbonA = CreateRibbon(
            root,
            new Vector3(0.30f, 3.96f, -0.085f),
            0.78f,
            ribbonMaterial,
            "RibbonA");
        Transform ribbonB = CreateRibbon(
            root,
            new Vector3(0.42f, 3.91f, 0.085f),
            0.66f,
            ribbonMaterial,
            "RibbonB");

        AddFringe(root, fieldMaterial);

        StandardRig rig = new StandardRig
        {
            Regiment = regiment,
            Root = root,
            ClothMesh = clothMesh,
            BaseVertices = baseVertices,
            WorkingVertices = (Vector3[])baseVertices.Clone(),
            Columns = ClothColumns,
            Rows = ClothRows,
            Width = ClothWidth,
            FieldMaterial = fieldMaterial,
            DeviceMaterial = deviceMaterial,
            RibbonMaterial = ribbonMaterial,
            DeviceRoot = deviceRoot,
            RibbonA = ribbonA,
            RibbonB = ribbonB,
            Phase = (regiment.RegimentName != null ? regiment.RegimentName.Length : 0) * 0.37f
        };

        ApplyProfileColors(rig);

        Debug.Log(
            "STANDARD-09I|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Created=True|ClothGrid=" + ClothColumns + "x" + ClothRows +
            "|LegacyHidden=True|MovementWrites=False");

        return rig;
    }

    private static GameObject CreateCloth(
        Transform parent,
        Material material,
        out Mesh mesh,
        out Vector3[] baseVertices)
    {
        GameObject cloth = new GameObject("AnimatedCloth");
        cloth.transform.SetParent(parent, false);

        int vertexColumns = ClothColumns + 1;
        int vertexRows = ClothRows + 1;
        baseVertices = new Vector3[vertexColumns * vertexRows];

        for (int row = 0; row < vertexRows; row++)
        {
            float y01 = row / (float)ClothRows;
            for (int column = 0; column < vertexColumns; column++)
            {
                float x01 = column / (float)ClothColumns;
                baseVertices[row * vertexColumns + column] = new Vector3(
                    x01 * ClothWidth,
                    -y01 * ClothHeight,
                    0f);
            }
        }

        List<int> triangles = new List<int>();
        for (int row = 0; row < ClothRows; row++)
        {
            for (int column = 0; column < ClothColumns; column++)
            {
                int a = row * vertexColumns + column;
                int b = a + 1;
                int c = a + vertexColumns;
                int d = c + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);

                // Reverse winding for the rear face so the cloth remains visible
                // from both sides even with a culling shader.
                triangles.Add(b); triangles.Add(c); triangles.Add(a);
                triangles.Add(d); triangles.Add(c); triangles.Add(b);
            }
        }

        mesh = new Mesh();
        mesh.name = "RegimentalCloth09I";
        mesh.vertices = baseVertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = cloth.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = cloth.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return cloth;
    }

    private static void AnimateStandard(StandardRig rig)
    {
        if (rig == null || rig.ClothMesh == null || rig.BaseVertices == null)
            return;

        float time = Time.time * 2.2f + rig.Phase;
        int vertexColumns = rig.Columns + 1;

        for (int i = 0; i < rig.BaseVertices.Length; i++)
        {
            Vector3 baseVertex = rig.BaseVertices[i];
            int column = i % vertexColumns;
            float x01 = column / (float)rig.Columns;
            float wave = Mathf.Sin(time + baseVertex.x * 2.9f + baseVertex.y * 1.1f);
            float secondary = Mathf.Sin(time * 0.65f + baseVertex.x * 5.1f);
            float amplitude = 0.045f + x01 * 0.16f;

            Vector3 v = baseVertex;
            v.z = (wave * 0.72f + secondary * 0.28f) * amplitude;
            v.y += Mathf.Sin(time * 0.8f + baseVertex.x * 2.2f) * x01 * 0.025f;
            rig.WorkingVertices[i] = v;
        }

        rig.ClothMesh.vertices = rig.WorkingVertices;
        rig.ClothMesh.RecalculateNormals();
        rig.ClothMesh.RecalculateBounds();

        float deviceYaw = Mathf.Sin(time + 0.4f) * 4.5f;
        if (rig.DeviceRoot != null)
            rig.DeviceRoot.localRotation = Quaternion.Euler(0f, deviceYaw, 0f);

        if (rig.RibbonA != null)
            rig.RibbonA.localRotation = Quaternion.Euler(8f, Mathf.Sin(time * 1.3f) * 12f, 7f);
        if (rig.RibbonB != null)
            rig.RibbonB.localRotation = Quaternion.Euler(-6f, Mathf.Sin(time * 1.1f + 1.3f) * 11f, -8f);
    }

    private static void ApplyProfileColors(StandardRig rig)
    {
        if (rig == null || rig.Regiment == null)
            return;

        PrototypeUniformProfile09H profile = GetProfile(rig.Regiment);
        if (rig.FieldMaterial != null)
            rig.FieldMaterial.color = profile.FlagPrimaryColor;
        if (rig.DeviceMaterial != null)
            rig.DeviceMaterial.color = profile.FlagSecondaryColor;
        if (rig.RibbonMaterial != null)
            rig.RibbonMaterial.color = profile.RibbonColor;
    }

    private static PrototypeUniformProfile09H GetProfile(Regiment regiment)
    {
        PrototypeSoldierVisualPass09H oldPass = PrototypeSoldierVisualPass09H.Instance;
        if (oldPass != null)
        {
            PrototypeUniformProfile09H profile = oldPass.GetProfile(regiment);
            if (profile != null)
                return profile;
        }

        return PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
    }

    private static void HideLegacyStandard(Regiment regiment)
    {
        if (regiment == null)
            return;

        Transform legacy = regiment.transform.Find("RegimentalStandard_" + regiment.RegimentName);
        if (legacy == null)
            return;

        Renderer[] renderers = legacy.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }

    private static Material CreateUnlitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

    private static void CreateSpearFinial(Transform parent, Material material)
    {
        GameObject finialRootObject = new GameObject("SpearFinial");
        finialRootObject.transform.SetParent(parent, false);
        Transform finialRoot = finialRootObject.transform;
        finialRoot.localPosition = new Vector3(0f, 4.33f, 0f);

        CreatePrimitivePart(
            finialRoot,
            PrimitiveType.Sphere,
            "FinialBase",
            Vector3.zero,
            new Vector3(0.11f, 0.11f, 0.11f),
            Quaternion.identity,
            material);

        CreatePrimitivePart(
            finialRoot,
            PrimitiveType.Cube,
            "FinialBlade",
            new Vector3(0f, 0.18f, 0f),
            new Vector3(0.10f, 0.34f, 0.045f),
            Quaternion.Euler(0f, 0f, 45f),
            material);
    }

    private static Transform CreateRibbon(
        Transform parent,
        Vector3 position,
        float length,
        Material material,
        string name)
    {
        GameObject ribbonRootObject = new GameObject(name + "_Root");
        ribbonRootObject.transform.SetParent(parent, false);
        ribbonRootObject.transform.localPosition = position;
        Transform ribbonRoot = ribbonRootObject.transform;

        CreatePrimitivePart(
            ribbonRoot,
            PrimitiveType.Cube,
            name,
            new Vector3(length * 0.42f, -0.20f, 0f),
            new Vector3(length, 0.09f, 0.028f),
            Quaternion.Euler(0f, 0f, -24f),
            material);
        return ribbonRoot;
    }

    private static void AddFringe(Transform parent, Material fieldMaterial)
    {
        Material fringeMaterial = CreateUnlitMaterial(
            new Color(0.77f, 0.62f, 0.18f),
            "09I_StandardFringe");

        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            CreatePrimitivePart(
                parent,
                PrimitiveType.Cube,
                "Fringe_" + i,
                new Vector3(2.39f, 3.95f - t * 1.30f, 0f),
                new Vector3(0.055f, 0.13f, 0.025f),
                Quaternion.Euler(0f, 0f, -8f + t * 16f),
                fringeMaterial);
        }
    }

    private static void CreateCord(
        Transform parent,
        Vector3 start,
        Vector3 end,
        Material material,
        string name)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f)
            return;

        GameObject cord = CreatePrimitivePart(
            parent,
            PrimitiveType.Cylinder,
            name,
            (start + end) * 0.5f,
            new Vector3(0.022f, length * 0.5f, 0.022f),
            Quaternion.identity,
            material);
        cord.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
    }

    private static GameObject CreatePrimitivePart(
        Transform parent,
        PrimitiveType primitive,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        return part;
    }

    private void CleanupDestroyedRegiments()
    {
        if (standards.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, StandardRig> pair in standards)
        {
            if (pair.Key != null)
                continue;

            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
            standards.Remove(stale[i]);
    }
}
