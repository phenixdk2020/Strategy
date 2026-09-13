using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f4 battle visual pass.
// Combat strength already uses real men; this layer makes the battlefield representation
// match that number. Default is 1 visible soldier per living man and 1 fallen model per loss.
// 1:2 / 1:5 / 1:10 remain available as performance display scales from the F9 panel.
[DefaultExecutionOrder(21000)]
public sealed class PrototypeBattleVisuals09F4 : MonoBehaviour
{
    private sealed class FallenRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private sealed class RegimentVisualState
    {
        public int LastStrength;
        public int LastSoldierRatio;
        public RegimentFormation LastFormation;
        public readonly List<Vector3> LocalPositions = new List<Vector3>();
        public readonly List<FallenRecord> Fallen = new List<FallenRecord>();
        public readonly List<Matrix4x4> LivingRoots = new List<Matrix4x4>();
        public readonly List<Matrix4x4> FallenRoots = new List<Matrix4x4>();
        public bool Initialized;
    }

    public static PrototypeBattleVisuals09F4 Instance { get; private set; }

    public static int SoldierDisplayRatio { get; private set; } = 1;
    public static int CasualtyDisplayRatio { get; private set; } = 1;

    private static readonly int[] DisplayRatios = { 1, 2, 5, 10 };

    private readonly Dictionary<Regiment, RegimentVisualState> states =
        new Dictionary<Regiment, RegimentVisualState>();

    private Mesh cubeMesh;
    private Mesh sphereMesh;
    private Mesh cylinderMesh;

    private Material danishCoat;
    private Material danishTrousers;
    private Material prussianCoat;
    private Material prussianTrousers;
    private Material redTrim;
    private Material whiteLeather;
    private Material blackLeather;
    private Material brownLeather;
    private Material skin;
    private Material rifleWood;
    private Material steel;
    private Material brass;

    private readonly Matrix4x4[] drawBuffer = new Matrix4x4[1023];

    private const float FormationMoveSpeed = 3.20f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattleVisuals09F4>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattleVisuals_v000009f4");
        root.AddComponent<PrototypeBattleVisuals09F4>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreatePrimitiveMeshes();
        CreateMaterials();

        Debug.Log(
            "VISUAL-09F4|Installed=True|LivingScale=1:1|FallenScale=1:1|" +
            "Renderer=InstancedDetailed1850s|Legacy1to10Hidden=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void CycleSoldierDisplayRatio()
    {
        SetSoldierDisplayRatio(GetNextRatio(SoldierDisplayRatio));
    }

    public static void CycleCasualtyDisplayRatio()
    {
        SetCasualtyDisplayRatio(GetNextRatio(CasualtyDisplayRatio));
    }

    public static void SetSoldierDisplayRatio(int ratio)
    {
        SoldierDisplayRatio = NormalizeRatio(ratio);
        Debug.Log("VISUAL-09F4|LivingScale=1:" + SoldierDisplayRatio);
    }

    public static void SetCasualtyDisplayRatio(int ratio)
    {
        CasualtyDisplayRatio = NormalizeRatio(ratio);
        Debug.Log("VISUAL-09F4|FallenScale=1:" + CasualtyDisplayRatio);
    }

    public static int GetVisibleLivingCount(Regiment regiment)
    {
        if (regiment == null || regiment.CurrentStrength <= 0)
            return 0;

        return Mathf.CeilToInt(regiment.CurrentStrength / (float)Mathf.Max(1, SoldierDisplayRatio));
    }

    public static int GetVisibleFallenCount(Regiment regiment)
    {
        if (regiment == null)
            return 0;

        int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
        return losses <= 0
            ? 0
            : Mathf.CeilToInt(losses / (float)Mathf.Max(1, CasualtyDisplayRatio));
    }

    private static int NormalizeRatio(int ratio)
    {
        int best = DisplayRatios[0];
        int bestDistance = Mathf.Abs(ratio - best);
        for (int i = 1; i < DisplayRatios.Length; i++)
        {
            int distance = Mathf.Abs(ratio - DisplayRatios[i]);
            if (distance >= bestDistance)
                continue;
            best = DisplayRatios[i];
            bestDistance = distance;
        }
        return best;
    }

    private static int GetNextRatio(int current)
    {
        for (int i = 0; i < DisplayRatios.Length; i++)
        {
            if (DisplayRatios[i] != current)
                continue;
            return DisplayRatios[(i + 1) % DisplayRatios.Length];
        }
        return 1;
    }

    private void Update()
    {
        DisableLegacyCasualtyRenderer();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            HideLegacyRepresentativeSoldiers(regiment);

            if (!states.TryGetValue(regiment, out RegimentVisualState state))
            {
                state = new RegimentVisualState();
                states[regiment] = state;
                InitializeState(regiment, state);
            }

            RecordNewCasualties(regiment, state);
            UpdateLivingFormation(regiment, state);
        }

        Cleanup(active);
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !states.TryGetValue(regiment, out RegimentVisualState state))
                continue;

            BuildLivingRootMatrices(regiment, state);
            BuildFallenRootMatrices(regiment, state);
            DrawLiving(regiment.Team, state.LivingRoots);
            DrawFallen(regiment.Team, state.FallenRoots);
        }
    }

    private void InitializeState(Regiment regiment, RegimentVisualState state)
    {
        state.LastStrength = regiment.CurrentStrength;
        state.LastSoldierRatio = SoldierDisplayRatio;
        state.LastFormation = regiment.Formation;
        state.Initialized = true;

        int historicalLosses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
        for (int i = 0; i < historicalLosses; i++)
            state.Fallen.Add(CreateFallenRecord(regiment, i));

        int desired = GetVisibleLivingCount(regiment);
        for (int i = 0; i < desired; i++)
        {
            int represented = GetRepresentedActualIndex(i, SoldierDisplayRatio, regiment.CurrentStrength);
            state.LocalPositions.Add(GetFormationPosition(regiment.Formation, represented, regiment.CurrentStrength));
        }
    }

    private void RecordNewCasualties(Regiment regiment, RegimentVisualState state)
    {
        if (!state.Initialized)
            return;

        if (regiment.CurrentStrength < state.LastStrength)
        {
            int lost = state.LastStrength - regiment.CurrentStrength;
            int startIndex = state.Fallen.Count;
            for (int i = 0; i < lost; i++)
                state.Fallen.Add(CreateFallenRecord(regiment, startIndex + i));

            Debug.Log(
                "CASUALTY-09F4|Unit=" + regiment.RegimentName +
                "|NewLosses=" + lost +
                "|ActualFallen=" + state.Fallen.Count +
                "|DisplayScale=1:" + CasualtyDisplayRatio);
        }

        state.LastStrength = regiment.CurrentStrength;
    }

    private FallenRecord CreateFallenRecord(Regiment regiment, int casualtyIndex)
    {
        int seed = StableHash(regiment.RegimentName) + casualtyIndex * 7919;
        float side = Hash01(seed + 11) * 2f - 1f;
        float depth = Hash01(seed + 37) * 2f - 1f;

        Vector3 local = new Vector3(side * 8.5f, 0f, depth * 4.6f);
        Vector3 world = regiment.transform.TransformPoint(local);
        world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.16f;

        float yaw = Hash01(seed + 73) * 360f;
        Quaternion lying = Quaternion.Euler(0f, yaw, 90f);

        return new FallenRecord
        {
            Position = world,
            Rotation = lying
        };
    }

    private void UpdateLivingFormation(Regiment regiment, RegimentVisualState state)
    {
        int desired = GetVisibleLivingCount(regiment);
        bool ratioChanged = state.LastSoldierRatio != SoldierDisplayRatio;
        bool formationChanged = state.LastFormation != regiment.Formation;

        if (ratioChanged)
        {
            state.LocalPositions.Clear();
            for (int i = 0; i < desired; i++)
            {
                int represented = GetRepresentedActualIndex(i, SoldierDisplayRatio, regiment.CurrentStrength);
                state.LocalPositions.Add(GetFormationPosition(regiment.Formation, represented, regiment.CurrentStrength));
            }
            state.LastSoldierRatio = SoldierDisplayRatio;
        }
        else
        {
            while (state.LocalPositions.Count > desired)
                state.LocalPositions.RemoveAt(state.LocalPositions.Count - 1);

            while (state.LocalPositions.Count < desired)
            {
                int i = state.LocalPositions.Count;
                int represented = GetRepresentedActualIndex(i, SoldierDisplayRatio, regiment.CurrentStrength);
                Vector3 target = GetFormationPosition(regiment.Formation, represented, regiment.CurrentStrength);
                Vector3 start = state.LocalPositions.Count > 0
                    ? state.LocalPositions[state.LocalPositions.Count - 1]
                    : target;
                state.LocalPositions.Add(start);
            }
        }

        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            int represented = GetRepresentedActualIndex(i, SoldierDisplayRatio, regiment.CurrentStrength);
            Vector3 target = GetFormationPosition(regiment.Formation, represented, regiment.CurrentStrength);
            state.LocalPositions[i] = Vector3.MoveTowards(
                state.LocalPositions[i],
                target,
                FormationMoveSpeed * Time.deltaTime);
        }

        if (formationChanged)
        {
            state.LastFormation = regiment.Formation;
            Debug.Log(
                "VISUAL-09F4|Unit=" + regiment.RegimentName +
                "|FormationTarget=" + regiment.Formation +
                "|PhysicalReform=True|VisibleMen=" + desired);
        }
    }

    private static int GetRepresentedActualIndex(int displayIndex, int ratio, int actualCount)
    {
        if (actualCount <= 0)
            return 0;

        int index = displayIndex * Mathf.Max(1, ratio) + Mathf.Max(0, ratio - 1) / 2;
        return Mathf.Clamp(index, 0, actualCount - 1);
    }

    private static Vector3 GetFormationPosition(RegimentFormation formation, int index, int totalActual)
    {
        totalActual = Mathf.Max(1, totalActual);

        if (formation == RegimentFormation.Line)
        {
            // Six-rank prototype gives a readable 1:1 battalion footprint without
            // turning the entire 360 m QA map into one continuous line.
            const int ranks = 6;
            int columns = Mathf.CeilToInt(totalActual / (float)ranks);
            int rank = Mathf.Clamp(index / columns, 0, ranks - 1);
            int column = index % columns;
            float x = (column - (columns - 1) * 0.5f) * 0.42f;
            float z = (rank - (ranks - 1) * 0.5f) * -0.56f;
            return new Vector3(x, 0f, z);
        }

        const int columnWidth = 8;
        int columnRank = index / columnWidth;
        int columnIndex = index % columnWidth;
        return new Vector3(
            (columnIndex - (columnWidth - 1) * 0.5f) * 0.48f,
            0f,
            -columnRank * 0.52f);
    }

    private void BuildLivingRootMatrices(Regiment regiment, RegimentVisualState state)
    {
        state.LivingRoots.Clear();
        Quaternion facing = regiment.transform.rotation;

        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            Vector3 local = state.LocalPositions[i];
            Vector3 world = regiment.transform.TransformPoint(local);
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.04f;
            state.LivingRoots.Add(Matrix4x4.TRS(world, facing, Vector3.one));
        }
    }

    private void BuildFallenRootMatrices(Regiment regiment, RegimentVisualState state)
    {
        state.FallenRoots.Clear();
        int ratio = Mathf.Max(1, CasualtyDisplayRatio);

        for (int i = 0; i < state.Fallen.Count; i += ratio)
        {
            FallenRecord record = state.Fallen[i];
            if (record == null)
                continue;
            state.FallenRoots.Add(Matrix4x4.TRS(record.Position, record.Rotation, Vector3.one));
        }
    }

    private void DrawLiving(BattleTeam team, List<Matrix4x4> roots)
    {
        if (roots == null || roots.Count == 0)
            return;

        Material coat = team == BattleTeam.Denmark ? danishCoat : prussianCoat;
        Material trousers = team == BattleTeam.Denmark ? danishTrousers : prussianTrousers;
        Material trim = team == BattleTeam.Denmark ? redTrim : whiteLeather;

        DrawPart(cubeMesh, trousers, roots, Part(new Vector3(-0.105f, 0.38f, 0f), Vector3.zero, new Vector3(0.16f, 0.72f, 0.18f)));
        DrawPart(cubeMesh, trousers, roots, Part(new Vector3(0.105f, 0.38f, 0f), Vector3.zero, new Vector3(0.16f, 0.72f, 0.18f)));
        DrawPart(cubeMesh, blackLeather, roots, Part(new Vector3(-0.105f, 0.08f, 0.06f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.28f)));
        DrawPart(cubeMesh, blackLeather, roots, Part(new Vector3(0.105f, 0.08f, 0.06f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.28f)));

        DrawPart(cubeMesh, coat, roots, Part(new Vector3(0f, 1.00f, 0f), Vector3.zero, new Vector3(0.48f, 0.66f, 0.28f)));
        DrawPart(cubeMesh, coat, roots, Part(new Vector3(-0.31f, 1.00f, 0f), new Vector3(0f, 0f, -7f), new Vector3(0.13f, 0.58f, 0.15f)));
        DrawPart(cubeMesh, coat, roots, Part(new Vector3(0.31f, 1.00f, 0f), new Vector3(0f, 0f, 7f), new Vector3(0.13f, 0.58f, 0.15f)));

        DrawPart(cubeMesh, trim, roots, Part(new Vector3(0f, 1.34f, 0.15f), Vector3.zero, new Vector3(0.40f, 0.10f, 0.045f)));
        DrawPart(cubeMesh, trim, roots, Part(new Vector3(-0.31f, 0.75f, 0.06f), Vector3.zero, new Vector3(0.14f, 0.10f, 0.17f)));
        DrawPart(cubeMesh, trim, roots, Part(new Vector3(0.31f, 0.75f, 0.06f), Vector3.zero, new Vector3(0.14f, 0.10f, 0.17f)));

        DrawPart(cubeMesh, whiteLeather, roots, Part(new Vector3(-0.06f, 1.05f, 0.165f), new Vector3(0f, 0f, -25f), new Vector3(0.055f, 0.76f, 0.038f)));
        DrawPart(cubeMesh, whiteLeather, roots, Part(new Vector3(0.06f, 1.05f, 0.165f), new Vector3(0f, 0f, 25f), new Vector3(0.055f, 0.76f, 0.038f)));
        DrawPart(cubeMesh, whiteLeather, roots, Part(new Vector3(0f, 0.73f, 0.165f), Vector3.zero, new Vector3(0.52f, 0.055f, 0.04f)));

        DrawPart(cubeMesh, brownLeather, roots, Part(new Vector3(0f, 1.02f, -0.20f), Vector3.zero, new Vector3(0.43f, 0.58f, 0.20f)));
        DrawPart(cubeMesh, blackLeather, roots, Part(new Vector3(0f, 0.68f, 0.20f), Vector3.zero, new Vector3(0.38f, 0.22f, 0.10f)));

        DrawPart(sphereMesh, skin, roots, Part(new Vector3(0f, 1.57f, 0f), Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f)));
        DrawPart(cylinderMesh, blackLeather, roots, Part(new Vector3(0f, 1.85f, 0f), Vector3.zero, new Vector3(0.25f, 0.20f, 0.25f)));
        DrawPart(sphereMesh, trim, roots, Part(new Vector3(0f, 2.08f, 0f), Vector3.zero, new Vector3(0.10f, 0.13f, 0.10f)));
        DrawPart(sphereMesh, brass, roots, Part(new Vector3(0f, 1.86f, 0.235f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.025f)));

        DrawPart(cubeMesh, rifleWood, roots, Part(new Vector3(0.39f, 1.18f, 0.02f), new Vector3(0f, 0f, -4f), new Vector3(0.055f, 1.28f, 0.055f)));
        DrawPart(cubeMesh, steel, roots, Part(new Vector3(0.43f, 2.04f, 0.02f), new Vector3(0f, 0f, -4f), new Vector3(0.025f, 0.48f, 0.025f)));
    }

    private void DrawFallen(BattleTeam team, List<Matrix4x4> roots)
    {
        if (roots == null || roots.Count == 0)
            return;

        Material coat = team == BattleTeam.Denmark ? danishCoat : prussianCoat;
        Material trousers = team == BattleTeam.Denmark ? danishTrousers : prussianTrousers;
        Material trim = team == BattleTeam.Denmark ? redTrim : whiteLeather;

        DrawPart(cubeMesh, coat, roots, Part(new Vector3(0f, 1.00f, 0f), Vector3.zero, new Vector3(0.48f, 0.66f, 0.28f)));
        DrawPart(cubeMesh, trousers, roots, Part(new Vector3(-0.105f, 0.38f, 0f), Vector3.zero, new Vector3(0.16f, 0.72f, 0.18f)));
        DrawPart(cubeMesh, trousers, roots, Part(new Vector3(0.105f, 0.38f, 0f), Vector3.zero, new Vector3(0.16f, 0.72f, 0.18f)));
        DrawPart(sphereMesh, skin, roots, Part(new Vector3(0f, 1.57f, 0f), Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f)));
        DrawPart(cylinderMesh, blackLeather, roots, Part(new Vector3(0f, 1.85f, 0f), Vector3.zero, new Vector3(0.25f, 0.20f, 0.25f)));
        DrawPart(cubeMesh, whiteLeather, roots, Part(new Vector3(-0.06f, 1.05f, 0.165f), new Vector3(0f, 0f, -25f), new Vector3(0.055f, 0.76f, 0.038f)));
        DrawPart(cubeMesh, whiteLeather, roots, Part(new Vector3(0.06f, 1.05f, 0.165f), new Vector3(0f, 0f, 25f), new Vector3(0.055f, 0.76f, 0.038f)));
        DrawPart(cubeMesh, rifleWood, roots, Part(new Vector3(0.45f, 1.10f, 0.10f), new Vector3(0f, 18f, -8f), new Vector3(0.05f, 1.20f, 0.05f)));
        DrawPart(cubeMesh, trim, roots, Part(new Vector3(0f, 1.34f, 0.15f), Vector3.zero, new Vector3(0.40f, 0.10f, 0.045f)));
    }

    private static Matrix4x4 Part(Vector3 position, Vector3 euler, Vector3 scale)
    {
        return Matrix4x4.TRS(position, Quaternion.Euler(euler), scale);
    }

    private void DrawPart(Mesh mesh, Material material, List<Matrix4x4> roots, Matrix4x4 localPart)
    {
        if (mesh == null || material == null || roots == null || roots.Count == 0)
            return;

        int offset = 0;
        while (offset < roots.Count)
        {
            int count = Mathf.Min(1023, roots.Count - offset);
            for (int i = 0; i < count; i++)
                drawBuffer[i] = roots[offset + i] * localPart;

            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                drawBuffer,
                count,
                null,
                ShadowCastingMode.On,
                true,
                0,
                null,
                LightProbeUsage.BlendProbes,
                null);

            offset += count;
        }
    }

    private void HideLegacyRepresentativeSoldiers(Regiment regiment)
    {
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("Soldier_"))
                continue;

            if (child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }
    }

    private static void DisableLegacyCasualtyRenderer()
    {
        PrototypeCasualtyVisualManager legacy =
            Object.FindAnyObjectByType<PrototypeCasualtyVisualManager>();
        if (legacy != null && legacy.enabled)
            legacy.enabled = false;
    }

    private void CreatePrimitiveMeshes()
    {
        cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        sphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
        cylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
    }

    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        temp.name = "_09F4MeshSource_" + type;
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.SetActive(false);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(temp);
        return mesh;
    }

    private void CreateMaterials()
    {
        danishCoat = CreateInstancedMaterial(new Color(0.055f, 0.105f, 0.23f), "09F4_DanishDarkBlue");
        danishTrousers = CreateInstancedMaterial(new Color(0.34f, 0.47f, 0.63f), "09F4_DanishLightBlueTrousers");
        prussianCoat = CreateInstancedMaterial(new Color(0.045f, 0.065f, 0.12f), "09F4_PrussianCoat");
        prussianTrousers = CreateInstancedMaterial(new Color(0.18f, 0.20f, 0.23f), "09F4_PrussianTrousers");
        redTrim = CreateInstancedMaterial(new Color(0.58f, 0.025f, 0.035f), "09F4_RedTrim");
        whiteLeather = CreateInstancedMaterial(new Color(0.91f, 0.88f, 0.78f), "09F4_WhiteLeather");
        blackLeather = CreateInstancedMaterial(new Color(0.025f, 0.024f, 0.027f), "09F4_BlackLeather");
        brownLeather = CreateInstancedMaterial(new Color(0.19f, 0.105f, 0.055f), "09F4_BrownLeather");
        skin = CreateInstancedMaterial(new Color(0.72f, 0.49f, 0.37f), "09F4_Skin");
        rifleWood = CreateInstancedMaterial(new Color(0.22f, 0.12f, 0.055f), "09F4_RifleWood");
        steel = CreateInstancedMaterial(new Color(0.48f, 0.50f, 0.52f), "09F4_Steel");
        brass = CreateInstancedMaterial(new Color(0.72f, 0.55f, 0.18f), "09F4_Brass");
    }

    private static Material CreateInstancedMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name,
            color = color,
            enableInstancing = true
        };
        return material;
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, RegimentVisualState> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                    hash = hash * 31 + text[i];
            }
            return hash & 0x7fffffff;
        }
    }

    private static float Hash01(int seed)
    {
        unchecked
        {
            uint x = (uint)seed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0x00FFFFFF) / 16777215f;
        }
    }
}
