using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f5: 1:1 company-scale renderer.
// 1851 Danish line infantry defaults to three ranks; two-rank line becomes a later/doctrine option.
[DefaultExecutionOrder(22000)]
public sealed class PrototypeBattleVisuals09F5 : MonoBehaviour
{
    private sealed class FallenRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private sealed class State
    {
        public int LastStrength;
        public int LastRatio;
        public RegimentFormation LastFormation;
        public readonly List<Vector3> LocalPositions = new List<Vector3>();
        public readonly List<FallenRecord> Fallen = new List<FallenRecord>();
        public readonly List<Matrix4x4> LivingRoots = new List<Matrix4x4>();
        public readonly List<Matrix4x4> FallenRoots = new List<Matrix4x4>();
    }

    private readonly Dictionary<Regiment, State> states = new Dictionary<Regiment, State>();
    private readonly Matrix4x4[] drawBuffer = new Matrix4x4[1023];

    private Mesh cube;
    private Mesh sphere;
    private Mesh cylinder;
    private Material dkCoat, dkTrousers, prCoat, prTrousers;
    private Material red, white, black, brown, skin, wood, steel, brass;

    public const int LineRanks = 3;
    public const float LineSpacingX = 0.75f;
    public const float RankSpacingZ = 0.90f;
    public const int ColumnWidth = 6;
    public const float ColumnSpacingX = 0.75f;
    public const float ColumnSpacingZ = 0.82f;
    private const float ReformSpeed = 2.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattleVisuals09F5>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattleVisuals_v000009f5");
        root.AddComponent<PrototypeBattleVisuals09F5>();
    }

    private void Awake()
    {
        cube = GetPrimitiveMesh(PrimitiveType.Cube);
        sphere = GetPrimitiveMesh(PrimitiveType.Sphere);
        cylinder = GetPrimitiveMesh(PrimitiveType.Cylinder);
        CreateMaterials();
        Debug.Log("VISUAL-09F5|Installed=True|Company=190|LineRanks=3|Scale=1u=1m|Rifle=True|Bayonet=True");
    }

    private void Update()
    {
        DisableOlderVisualLayers();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            HideLegacySoldiers(regiment);

            if (!states.TryGetValue(regiment, out State state))
            {
                state = new State
                {
                    LastStrength = regiment.CurrentStrength,
                    LastRatio = LivingRatio,
                    LastFormation = regiment.Formation
                };
                states[regiment] = state;
                RebuildPositions(regiment, state);
            }

            RecordCasualties(regiment, state);
            UpdateFormation(regiment, state);
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
            if (regiment == null || !states.TryGetValue(regiment, out State state))
                continue;

            BuildLivingRoots(regiment, state);
            BuildFallenRoots(state);
            DrawLiving(regiment.Team, state.LivingRoots);
            DrawFallen(regiment.Team, state.FallenRoots);
        }
    }

    private static int LivingRatio => Mathf.Max(1, PrototypeBattleVisuals09F4.SoldierDisplayRatio);
    private static int FallenRatio => Mathf.Max(1, PrototypeBattleVisuals09F4.CasualtyDisplayRatio);

    private static int VisibleLiving(Regiment regiment)
    {
        return regiment == null || regiment.CurrentStrength <= 0
            ? 0
            : Mathf.CeilToInt(regiment.CurrentStrength / (float)LivingRatio);
    }

    private static void DisableOlderVisualLayers()
    {
        PrototypeBattleVisuals09F4 old = Object.FindAnyObjectByType<PrototypeBattleVisuals09F4>();
        if (old != null && old.enabled)
            old.enabled = false;

        PrototypeCasualtyVisualManager oldCasualties = Object.FindAnyObjectByType<PrototypeCasualtyVisualManager>();
        if (oldCasualties != null && oldCasualties.enabled)
            oldCasualties.enabled = false;
    }

    private static void HideLegacySoldiers(Regiment regiment)
    {
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child != null && child.name.StartsWith("Soldier_") && child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }
    }

    private void RecordCasualties(Regiment regiment, State state)
    {
        if (regiment.CurrentStrength >= state.LastStrength)
        {
            state.LastStrength = regiment.CurrentStrength;
            return;
        }

        int lost = state.LastStrength - regiment.CurrentStrength;
        int first = state.Fallen.Count;
        for (int i = 0; i < lost; i++)
            state.Fallen.Add(CreateFallen(regiment, first + i));

        state.LastStrength = regiment.CurrentStrength;
        Debug.Log("CASUALTY-09F5|Unit=" + regiment.RegimentName + "|New=" + lost + "|Total=" + state.Fallen.Count);
    }

    private FallenRecord CreateFallen(Regiment regiment, int index)
    {
        int seed = StableHash(regiment.RegimentName) + index * 7919;
        float halfWidth = Mathf.Max(2f, GetLineWidth(regiment.InitialStrength) * 0.48f);
        Vector3 local = new Vector3(
            (Hash01(seed + 7) * 2f - 1f) * halfWidth,
            0f,
            (Hash01(seed + 19) * 2f - 1f) * 2.2f);

        Vector3 world = regiment.transform.TransformPoint(local);
        world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.10f;

        return new FallenRecord
        {
            Position = world,
            Rotation = Quaternion.Euler(0f, Hash01(seed + 31) * 360f, 90f)
        };
    }

    private void RebuildPositions(Regiment regiment, State state)
    {
        state.LocalPositions.Clear();
        int count = VisibleLiving(regiment);
        for (int i = 0; i < count; i++)
        {
            int actual = GetActualIndex(i, LivingRatio, regiment.CurrentStrength);
            state.LocalPositions.Add(GetFormationPosition(regiment.Formation, actual, regiment.CurrentStrength));
        }
        state.LastRatio = LivingRatio;
        state.LastFormation = regiment.Formation;
    }

    private void UpdateFormation(Regiment regiment, State state)
    {
        if (state.LastRatio != LivingRatio)
        {
            RebuildPositions(regiment, state);
            return;
        }

        int desired = VisibleLiving(regiment);
        while (state.LocalPositions.Count > desired)
            state.LocalPositions.RemoveAt(state.LocalPositions.Count - 1);
        while (state.LocalPositions.Count < desired)
        {
            int i = state.LocalPositions.Count;
            int actual = GetActualIndex(i, LivingRatio, regiment.CurrentStrength);
            Vector3 target = GetFormationPosition(regiment.Formation, actual, regiment.CurrentStrength);
            state.LocalPositions.Add(state.LocalPositions.Count == 0 ? target : state.LocalPositions[state.LocalPositions.Count - 1]);
        }

        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            int actual = GetActualIndex(i, LivingRatio, regiment.CurrentStrength);
            Vector3 target = GetFormationPosition(regiment.Formation, actual, regiment.CurrentStrength);
            state.LocalPositions[i] = Vector3.MoveTowards(state.LocalPositions[i], target, ReformSpeed * Time.deltaTime);
        }

        if (state.LastFormation != regiment.Formation)
        {
            state.LastFormation = regiment.Formation;
            Debug.Log("VISUAL-09F5|Unit=" + regiment.RegimentName + "|Formation=" + regiment.Formation + "|PhysicalReform=True");
        }
    }

    private static int GetActualIndex(int displayIndex, int ratio, int actualCount)
    {
        if (actualCount <= 0)
            return 0;
        return Mathf.Clamp(displayIndex * ratio + (ratio - 1) / 2, 0, actualCount - 1);
    }

    public static Vector3 GetFormationPosition(RegimentFormation formation, int index, int total)
    {
        total = Mathf.Max(1, total);

        if (formation == RegimentFormation.Line)
        {
            int columns = Mathf.CeilToInt(total / (float)LineRanks);
            int rank = Mathf.Clamp(index / columns, 0, LineRanks - 1);
            int column = index % columns;
            float x = (column - (columns - 1) * 0.5f) * LineSpacingX;
            float z = ((LineRanks - 1) * 0.5f - rank) * RankSpacingZ;
            return new Vector3(x, 0f, z);
        }

        int row = index / ColumnWidth;
        int col = index % ColumnWidth;
        return new Vector3(
            (col - (ColumnWidth - 1) * 0.5f) * ColumnSpacingX,
            0f,
            -row * ColumnSpacingZ);
    }

    public static float GetLineWidth(int strength)
    {
        int columns = Mathf.CeilToInt(Mathf.Max(1, strength) / (float)LineRanks);
        return Mathf.Max(0.8f, (columns - 1) * LineSpacingX + 0.55f);
    }

    public static Vector2 GetFootprint(Regiment regiment)
    {
        if (regiment == null)
            return new Vector2(1f, 1f);

        int strength = Mathf.Max(1, regiment.CurrentStrength);
        if (regiment.Formation == RegimentFormation.Line)
        {
            float width = GetLineWidth(strength);
            float depth = (LineRanks - 1) * RankSpacingZ + 0.8f;
            return new Vector2(width, depth);
        }

        int rows = Mathf.CeilToInt(strength / (float)ColumnWidth);
        float columnWidth = (ColumnWidth - 1) * ColumnSpacingX + 0.55f;
        float columnDepth = Mathf.Max(1f, (rows - 1) * ColumnSpacingZ + 0.8f);
        return new Vector2(columnWidth, columnDepth);
    }

    private void BuildLivingRoots(Regiment regiment, State state)
    {
        state.LivingRoots.Clear();
        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            Vector3 world = regiment.transform.TransformPoint(state.LocalPositions[i]);
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.03f;
            state.LivingRoots.Add(Matrix4x4.TRS(world, regiment.transform.rotation, Vector3.one));
        }
    }

    private static void BuildFallenRoots(State state)
    {
        state.FallenRoots.Clear();
        for (int i = 0; i < state.Fallen.Count; i += FallenRatio)
        {
            FallenRecord f = state.Fallen[i];
            state.FallenRoots.Add(Matrix4x4.TRS(f.Position, f.Rotation, Vector3.one));
        }
    }

    private void DrawLiving(BattleTeam team, List<Matrix4x4> roots)
    {
        if (roots.Count == 0)
            return;

        Material coat = team == BattleTeam.Denmark ? dkCoat : prCoat;
        Material trousers = team == BattleTeam.Denmark ? dkTrousers : prTrousers;
        Material trim = team == BattleTeam.Denmark ? red : white;

        Draw(cube, trousers, roots, Part(new Vector3(-0.10f, 0.40f, 0f), Vector3.zero, new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(cube, trousers, roots, Part(new Vector3(0.10f, 0.40f, 0f), Vector3.zero, new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(cube, black, roots, Part(new Vector3(-0.10f, 0.09f, 0.06f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.28f)));
        Draw(cube, black, roots, Part(new Vector3(0.10f, 0.09f, 0.06f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.28f)));
        Draw(cube, coat, roots, Part(new Vector3(0f, 1.03f, 0f), Vector3.zero, new Vector3(0.50f, 0.68f, 0.30f)));
        Draw(cube, coat, roots, Part(new Vector3(-0.31f, 1.01f, 0f), new Vector3(0f, 0f, -7f), new Vector3(0.13f, 0.58f, 0.15f)));
        Draw(cube, coat, roots, Part(new Vector3(0.31f, 1.01f, 0f), new Vector3(0f, 0f, 7f), new Vector3(0.13f, 0.58f, 0.15f)));
        Draw(cube, trim, roots, Part(new Vector3(0f, 1.36f, 0.16f), Vector3.zero, new Vector3(0.42f, 0.10f, 0.045f)));
        Draw(cube, white, roots, Part(new Vector3(-0.065f, 1.06f, 0.17f), new Vector3(0f, 0f, -25f), new Vector3(0.055f, 0.78f, 0.040f)));
        Draw(cube, white, roots, Part(new Vector3(0.065f, 1.06f, 0.17f), new Vector3(0f, 0f, 25f), new Vector3(0.055f, 0.78f, 0.040f)));
        Draw(cube, brown, roots, Part(new Vector3(0f, 1.02f, -0.21f), Vector3.zero, new Vector3(0.44f, 0.58f, 0.21f)));
        Draw(cube, black, roots, Part(new Vector3(0f, 0.71f, 0.20f), Vector3.zero, new Vector3(0.38f, 0.22f, 0.10f)));
        Draw(sphere, skin, roots, Part(new Vector3(0f, 1.58f, 0f), Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f)));
        Draw(cylinder, black, roots, Part(new Vector3(0f, 1.84f, 0f), Vector3.zero, new Vector3(0.25f, 0.21f, 0.25f)));
        Draw(sphere, trim, roots, Part(new Vector3(0f, 2.08f, 0f), Vector3.zero, new Vector3(0.10f, 0.13f, 0.10f)));
        Draw(sphere, brass, roots, Part(new Vector3(0f, 1.86f, 0.235f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.025f)));

        // Long musket plus fixed socket bayonet, deliberately oversized just enough to read from RTS camera distance.
        Draw(cube, wood, roots, Part(new Vector3(0.39f, 1.24f, 0.03f), new Vector3(0f, 0f, -5f), new Vector3(0.060f, 1.42f, 0.060f)));
        Draw(cube, steel, roots, Part(new Vector3(0.44f, 2.23f, 0.03f), new Vector3(0f, 0f, -5f), new Vector3(0.026f, 0.62f, 0.026f)));
    }

    private void DrawFallen(BattleTeam team, List<Matrix4x4> roots)
    {
        if (roots.Count == 0)
            return;

        Material coat = team == BattleTeam.Denmark ? dkCoat : prCoat;
        Material trousers = team == BattleTeam.Denmark ? dkTrousers : prTrousers;
        Draw(cube, coat, roots, Part(new Vector3(0f, 1.03f, 0f), Vector3.zero, new Vector3(0.50f, 0.68f, 0.30f)));
        Draw(cube, trousers, roots, Part(new Vector3(-0.10f, 0.40f, 0f), Vector3.zero, new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(cube, trousers, roots, Part(new Vector3(0.10f, 0.40f, 0f), Vector3.zero, new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(sphere, skin, roots, Part(new Vector3(0f, 1.58f, 0f), Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f)));
        Draw(cube, wood, roots, Part(new Vector3(0.48f, 1.12f, 0.12f), new Vector3(0f, 18f, -8f), new Vector3(0.055f, 1.35f, 0.055f)));
        Draw(cube, steel, roots, Part(new Vector3(0.68f, 2.00f, 0.12f), new Vector3(0f, 18f, -8f), new Vector3(0.024f, 0.55f, 0.024f)));
    }

    private static Matrix4x4 Part(Vector3 position, Vector3 euler, Vector3 scale)
    {
        return Matrix4x4.TRS(position, Quaternion.Euler(euler), scale);
    }

    private void Draw(Mesh mesh, Material material, List<Matrix4x4> roots, Matrix4x4 local)
    {
        if (mesh == null || material == null || roots.Count == 0)
            return;

        int offset = 0;
        while (offset < roots.Count)
        {
            int count = Mathf.Min(1023, roots.Count - offset);
            for (int i = 0; i < count; i++)
                drawBuffer[i] = roots[offset + i] * local;

            Graphics.DrawMeshInstanced(mesh, 0, material, drawBuffer, count, null,
                ShadowCastingMode.On, true, 0, null, LightProbeUsage.BlendProbes, null);
            offset += count;
        }
    }

    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.SetActive(false);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(temp);
        return mesh;
    }

    private void CreateMaterials()
    {
        dkCoat = Mat(new Color(0.035f, 0.085f, 0.17f), "09F5_DK_Navy");
        dkTrousers = Mat(new Color(0.30f, 0.43f, 0.58f), "09F5_DK_BlueGrey");
        prCoat = Mat(new Color(0.10f, 0.12f, 0.16f), "09F5_PR_Dark");
        prTrousers = Mat(new Color(0.18f, 0.20f, 0.24f), "09F5_PR_Trousers");
        red = Mat(new Color(0.72f, 0.04f, 0.055f), "09F5_RedTrim");
        white = Mat(new Color(0.88f, 0.86f, 0.78f), "09F5_WhiteLeather");
        black = Mat(new Color(0.035f, 0.035f, 0.04f), "09F5_BlackLeather");
        brown = Mat(new Color(0.20f, 0.12f, 0.065f), "09F5_BrownLeather");
        skin = Mat(new Color(0.66f, 0.48f, 0.38f), "09F5_Skin");
        wood = Mat(new Color(0.22f, 0.12f, 0.055f), "09F5_RifleWood");
        steel = Mat(new Color(0.48f, 0.52f, 0.55f), "09F5_SteelBayonet");
        brass = Mat(new Color(0.68f, 0.52f, 0.18f), "09F5_Brass");
    }

    private static Material Mat(Color color, string name)
    {
        Material m = PrototypeBootstrap.CreateSharedMaterial(color, name);
        if (m != null)
            m.enableInstancing = true;
        return m;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(text))
                for (int i = 0; i < text.Length; i++)
                    hash = hash * 31 + text[i];
            return hash;
        }
    }

    private static float Hash01(int value)
    {
        unchecked
        {
            uint x = (uint)value;
            x ^= x >> 17;
            x *= 0xed5ad4bbU;
            x ^= x >> 11;
            x *= 0xac4c1b51U;
            x ^= x >> 15;
            return (x & 0x00FFFFFF) / 16777215f;
        }
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, State> pair in states)
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
}
