using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f7 company renderer.
// Adds readable Await -> Aim -> Fire -> Reload states, Walk / Forced March / Rout Run,
// full-front black-powder smoke, smooth Line/Column reforming and close casualty placement.
[DefaultExecutionOrder(22500)]
public sealed class PrototypeBattleVisuals09F7 : MonoBehaviour
{
    private enum SoldierPhase
    {
        Await,
        Walk,
        ForcedMarch,
        Run,
        Aim,
        Fire,
        Reload
    }

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
        public float LastNextFireTime;
        public float VolleyStartedAt = -100f;
        public ParticleSystem Smoke;
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
    private FieldInfo nextFireTimeField;
    private FieldInfo hasDestinationField;

    private const float ReformSpeed = 2.8f;
    private const float FirePoseSeconds = 0.18f;
    private const float AimLeadSeconds = 0.70f;
    private const float ReloadFraction = 0.62f;
    private const float CasualtyJitter = 0.30f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattleVisuals_v000009f7");
        root.AddComponent<PrototypeBattleVisuals09F7>();
    }

    private void Awake()
    {
        cube = GetPrimitiveMesh(PrimitiveType.Cube);
        sphere = GetPrimitiveMesh(PrimitiveType.Sphere);
        cylinder = GetPrimitiveMesh(PrimitiveType.Cylinder);
        CreateMaterials();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        Debug.Log(
            "VISUAL-09F7|Installed=True|VolleyStates=Await,Aim,Fire,Reload|" +
            "MovementStates=Walk,ForcedMarch,Run|Smoke=FullFront|" +
            "CasualtyPlacement=FormationSlot|CasualtyJitter=" + CasualtyJitter.ToString("0.00") + "m");
    }

    private void Update()
    {
        PrototypeBattleVisuals09F5 old = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F5>();
        if (old != null && old.enabled)
            old.enabled = false;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            HideLegacySoldiersAndSmoke(regiment);

            if (!states.TryGetValue(regiment, out State state))
            {
                state = new State
                {
                    LastStrength = regiment.CurrentStrength,
                    LastRatio = LivingRatio,
                    LastFormation = regiment.Formation,
                    LastNextFireTime = ReadNextFireTime(regiment)
                };
                state.Smoke = CreateSmoke(regiment);
                states[regiment] = state;
                RebuildPositions(regiment, state);
            }

            RecordCasualties(regiment, state);
            UpdateFormation(regiment, state);
            UpdateVolleyDetection(regiment, state);
            UpdateSmokeShape(regiment, state);
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

            SoldierPhase phase = GetPhase(regiment, state);
            BuildLivingRoots(regiment, state, phase);
            BuildFallenRoots(state);
            DrawLiving(regiment.Team, state.LivingRoots, phase);
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

    private float ReadNextFireTime(Regiment regiment)
    {
        if (regiment == null || nextFireTimeField == null)
            return 0f;
        object value = nextFireTimeField.GetValue(regiment);
        return value is float f ? f : 0f;
    }

    private bool IsMoving(Regiment regiment)
    {
        if (regiment == null)
            return false;

        if (PrototypeForcedMarch09F7.Instance != null)
            return PrototypeForcedMarch09F7.IsMoving(regiment);

        if (hasDestinationField == null)
            return false;

        return (bool)hasDestinationField.GetValue(regiment);
    }

    private void UpdateVolleyDetection(Regiment regiment, State state)
    {
        float next = ReadNextFireTime(regiment);
        if (next > state.LastNextFireTime + 0.05f && next > Time.time)
        {
            state.VolleyStartedAt = Time.time;
            EmitFrontSmoke(regiment, state);
            Debug.Log(
                "VOLLEY-09F7|Unit=" + regiment.RegimentName +
                "|State=FIRE|Smoke=FullFront|Reload=" + regiment.CurrentReloadSeconds.ToString("0.00"));
        }
        state.LastNextFireTime = next;
    }

    private SoldierPhase GetPhase(Regiment regiment, State state)
    {
        bool moving = IsMoving(regiment);
        if (moving)
        {
            if (regiment.IsRouted)
                return SoldierPhase.Run;
            if (PrototypeForcedMarch09F7.IsForcedMarch(regiment))
                return SoldierPhase.ForcedMarch;
            return SoldierPhase.Walk;
        }

        float sinceVolley = Time.time - state.VolleyStartedAt;
        if (sinceVolley >= 0f && sinceVolley <= FirePoseSeconds)
            return SoldierPhase.Fire;

        float reloadWindow = Mathf.Max(0.75f, regiment.CurrentReloadSeconds * ReloadFraction);
        if (sinceVolley > FirePoseSeconds && sinceVolley <= reloadWindow)
            return SoldierPhase.Reload;

        float next = ReadNextFireTime(regiment);
        float remaining = next - Time.time;
        if (next > Time.time && remaining <= AimLeadSeconds)
            return SoldierPhase.Aim;

        return SoldierPhase.Await;
    }

    private static void HideLegacySoldiersAndSmoke(Regiment regiment)
    {
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null)
                continue;

            if (child.name.StartsWith("Soldier_") && child.gameObject.activeSelf)
                child.gameObject.SetActive(false);

            if (child.name == "BlackPowderSmoke")
            {
                ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
    }

    private void RecordCasualties(Regiment regiment, State state)
    {
        if (regiment.CurrentStrength >= state.LastStrength)
        {
            state.LastStrength = regiment.CurrentStrength;
            return;
        }

        int previousStrength = state.LastStrength;
        int lost = previousStrength - regiment.CurrentStrength;

        for (int i = 0; i < lost; i++)
        {
            int seed = StableHash(regiment.RegimentName) + (state.Fallen.Count + i) * 7919;
            int fallenIndex = Mathf.Clamp(
                Mathf.FloorToInt(Hash01(seed + 5) * previousStrength),
                0,
                Mathf.Max(0, previousStrength - 1));

            Vector3 localSlot = PrototypeBattleVisuals09F5.GetFormationPosition(
                regiment.Formation,
                fallenIndex,
                previousStrength);

            localSlot.x += (Hash01(seed + 11) * 2f - 1f) * CasualtyJitter;
            localSlot.z += (Hash01(seed + 23) * 2f - 1f) * CasualtyJitter;

            Vector3 world = regiment.transform.TransformPoint(localSlot);
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.08f;

            state.Fallen.Add(new FallenRecord
            {
                Position = world,
                Rotation = Quaternion.Euler(0f, Hash01(seed + 41) * 360f, 90f)
            });
        }

        state.LastStrength = regiment.CurrentStrength;
        Debug.Log(
            "CASUALTY-09F7|Unit=" + regiment.RegimentName +
            "|New=" + lost +
            "|Placement=FormationSlot|MaxJitter=" + CasualtyJitter.ToString("0.00") + "m");
    }

    private void RebuildPositions(Regiment regiment, State state)
    {
        state.LocalPositions.Clear();
        int count = VisibleLiving(regiment);
        for (int i = 0; i < count; i++)
        {
            int actual = GetActualIndex(i, LivingRatio, regiment.CurrentStrength);
            state.LocalPositions.Add(
                PrototypeBattleVisuals09F5.GetFormationPosition(regiment.Formation, actual, regiment.CurrentStrength));
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
            Vector3 target = PrototypeBattleVisuals09F5.GetFormationPosition(
                regiment.Formation,
                actual,
                regiment.CurrentStrength);
            state.LocalPositions.Add(state.LocalPositions.Count == 0
                ? target
                : state.LocalPositions[state.LocalPositions.Count - 1]);
        }

        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            int actual = GetActualIndex(i, LivingRatio, regiment.CurrentStrength);
            Vector3 target = PrototypeBattleVisuals09F5.GetFormationPosition(
                regiment.Formation,
                actual,
                regiment.CurrentStrength);
            state.LocalPositions[i] = Vector3.MoveTowards(
                state.LocalPositions[i],
                target,
                ReformSpeed * Time.deltaTime);
        }

        if (state.LastFormation != regiment.Formation)
        {
            state.LastFormation = regiment.Formation;
            Debug.Log(
                "VISUAL-09F7|Unit=" + regiment.RegimentName +
                "|Formation=" + regiment.Formation + "|PhysicalReform=True");
        }
    }

    private static int GetActualIndex(int displayIndex, int ratio, int actualCount)
    {
        if (actualCount <= 0)
            return 0;
        return Mathf.Clamp(displayIndex * ratio + (ratio - 1) / 2, 0, actualCount - 1);
    }

    private void BuildLivingRoots(Regiment regiment, State state, SoldierPhase phase)
    {
        state.LivingRoots.Clear();

        float frequency = GetMovementFrequency(phase);
        float bobAmount = GetMovementBob(phase);
        float lean = GetMovementLean(phase);

        for (int i = 0; i < state.LocalPositions.Count; i++)
        {
            Vector3 world = regiment.transform.TransformPoint(state.LocalPositions[i]);
            float bob = 0f;
            if (frequency > 0f)
            {
                float cycle = Time.time * frequency + i * 0.47f;
                bob = Mathf.Abs(Mathf.Sin(cycle)) * bobAmount;
            }

            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.03f + bob;
            Quaternion rotation = regiment.transform.rotation * Quaternion.Euler(lean, 0f, 0f);
            state.LivingRoots.Add(Matrix4x4.TRS(world, rotation, Vector3.one));
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

    private ParticleSystem CreateSmoke(Regiment regiment)
    {
        GameObject go = new GameObject("BlackPowderSmoke09F7");
        go.transform.SetParent(regiment.transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.85f, 1.75f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.88f, 0.88f, 0.84f, 0.72f),
            new Color(0.72f, 0.73f, 0.70f, 0.48f));
        main.maxParticles = 1200;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 0.25f, 0.25f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.10f, 0.45f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material smokeMaterial = new Material(shader)
            {
                name = "BlackPowderSmoke09F7_Material"
            };
            renderer.material = smokeMaterial;
        }

        return ps;
    }

    private void UpdateSmokeShape(Regiment regiment, State state)
    {
        if (state.Smoke == null)
            return;

        FormationBounds bounds = CalculateFormationBounds(regiment.Formation, regiment.CurrentStrength);
        Transform t = state.Smoke.transform;
        t.localPosition = new Vector3(bounds.CenterX, 1.35f, bounds.MaxZ + 0.55f);
        t.localRotation = Quaternion.identity;

        ParticleSystem.ShapeModule shape = state.Smoke.shape;
        shape.scale = new Vector3(Mathf.Max(1f, bounds.Width), 0.30f, 0.30f);
    }

    private void EmitFrontSmoke(Regiment regiment, State state)
    {
        if (state.Smoke == null || regiment.Formation != RegimentFormation.Line)
            return;

        int visible = VisibleLiving(regiment);
        int count = Mathf.Clamp(Mathf.RoundToInt(visible * 1.10f), 60, 260);
        state.Smoke.Emit(count);
    }

    private struct FormationBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
        public float Width => MaxX - MinX + 0.55f;
        public float CenterX => (MinX + MaxX) * 0.5f;
    }

    private static FormationBounds CalculateFormationBounds(RegimentFormation formation, int strength)
    {
        int count = Mathf.Max(1, strength);
        Vector3 first = PrototypeBattleVisuals09F5.GetFormationPosition(formation, 0, count);
        FormationBounds bounds = new FormationBounds
        {
            MinX = first.x,
            MaxX = first.x,
            MinZ = first.z,
            MaxZ = first.z
        };

        for (int i = 1; i < count; i++)
        {
            Vector3 p = PrototypeBattleVisuals09F5.GetFormationPosition(formation, i, count);
            bounds.MinX = Mathf.Min(bounds.MinX, p.x);
            bounds.MaxX = Mathf.Max(bounds.MaxX, p.x);
            bounds.MinZ = Mathf.Min(bounds.MinZ, p.z);
            bounds.MaxZ = Mathf.Max(bounds.MaxZ, p.z);
        }
        return bounds;
    }

    private void DrawLiving(BattleTeam team, List<Matrix4x4> roots, SoldierPhase phase)
    {
        if (roots.Count == 0)
            return;

        Material coat = team == BattleTeam.Denmark ? dkCoat : prCoat;
        Material trousers = team == BattleTeam.Denmark ? dkTrousers : prTrousers;
        Material trim = team == BattleTeam.Denmark ? red : white;

        bool moving = phase == SoldierPhase.Walk || phase == SoldierPhase.ForcedMarch || phase == SoldierPhase.Run;
        float gaitFrequency = phase == SoldierPhase.Run ? 9.0f : phase == SoldierPhase.ForcedMarch ? 7.0f : 5.2f;
        float gaitAmplitude = phase == SoldierPhase.Run ? 28f : phase == SoldierPhase.ForcedMarch ? 20f : 13f;
        float swing = moving ? Mathf.Sin(Time.time * gaitFrequency) * gaitAmplitude : 0f;

        Draw(cube, trousers, roots, Part(new Vector3(-0.10f, 0.40f, 0f), new Vector3(swing, 0f, 0f), new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(cube, trousers, roots, Part(new Vector3(0.10f, 0.40f, 0f), new Vector3(-swing, 0f, 0f), new Vector3(0.17f, 0.72f, 0.18f)));
        Draw(cube, black, roots, Part(new Vector3(-0.10f, 0.09f, 0.06f), new Vector3(swing * 0.35f, 0f, 0f), new Vector3(0.18f, 0.18f, 0.28f)));
        Draw(cube, black, roots, Part(new Vector3(0.10f, 0.09f, 0.06f), new Vector3(-swing * 0.35f, 0f, 0f), new Vector3(0.18f, 0.18f, 0.28f)));

        float torsoPitch = phase == SoldierPhase.Aim ? 6f : phase == SoldierPhase.Fire ? 9f : phase == SoldierPhase.Run ? 8f : phase == SoldierPhase.ForcedMarch ? 4f : 0f;
        Draw(cube, coat, roots, Part(new Vector3(0f, 1.03f, 0f), new Vector3(torsoPitch, 0f, 0f), new Vector3(0.50f, 0.68f, 0.30f)));
        Draw(cube, trim, roots, Part(new Vector3(0f, 1.36f, 0.16f), Vector3.zero, new Vector3(0.42f, 0.10f, 0.045f)));
        Draw(cube, white, roots, Part(new Vector3(-0.065f, 1.06f, 0.17f), new Vector3(0f, 0f, -25f), new Vector3(0.055f, 0.78f, 0.040f)));
        Draw(cube, white, roots, Part(new Vector3(0.065f, 1.06f, 0.17f), new Vector3(0f, 0f, 25f), new Vector3(0.055f, 0.78f, 0.040f)));
        Draw(cube, brown, roots, Part(new Vector3(0f, 1.02f, -0.21f), Vector3.zero, new Vector3(0.44f, 0.58f, 0.21f)));
        Draw(cube, black, roots, Part(new Vector3(0f, 0.71f, 0.20f), Vector3.zero, new Vector3(0.38f, 0.22f, 0.10f)));
        Draw(sphere, skin, roots, Part(new Vector3(0f, 1.58f, 0f), Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f)));
        Draw(cylinder, black, roots, Part(new Vector3(0f, 1.84f, 0f), Vector3.zero, new Vector3(0.25f, 0.21f, 0.25f)));
        Draw(sphere, trim, roots, Part(new Vector3(0f, 2.08f, 0f), Vector3.zero, new Vector3(0.10f, 0.13f, 0.10f)));
        Draw(sphere, brass, roots, Part(new Vector3(0f, 1.86f, 0.235f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.025f)));

        DrawWeaponAndArms(coat, roots, phase, swing);
    }

    private void DrawWeaponAndArms(Material coat, List<Matrix4x4> roots, SoldierPhase phase, float swing)
    {
        if (phase == SoldierPhase.Aim || phase == SoldierPhase.Fire)
        {
            float recoil = phase == SoldierPhase.Fire ? -0.10f : 0f;
            Draw(cube, coat, roots, Part(new Vector3(-0.22f, 1.20f, 0.28f + recoil), new Vector3(72f, 0f, -18f), new Vector3(0.13f, 0.54f, 0.15f)));
            Draw(cube, coat, roots, Part(new Vector3(0.24f, 1.20f, 0.33f + recoil), new Vector3(72f, 0f, 18f), new Vector3(0.13f, 0.54f, 0.15f)));
            Draw(cube, wood, roots, Part(new Vector3(0.12f, 1.28f, 0.78f + recoil), Vector3.zero, new Vector3(0.065f, 0.065f, 1.55f)));
            Draw(cube, steel, roots, Part(new Vector3(0.12f, 1.28f, 1.82f + recoil), Vector3.zero, new Vector3(0.028f, 0.028f, 0.56f)));
            return;
        }

        if (phase == SoldierPhase.Reload)
        {
            Draw(cube, coat, roots, Part(new Vector3(-0.27f, 1.04f, 0.17f), new Vector3(0f, 0f, -12f), new Vector3(0.13f, 0.56f, 0.15f)));
            Draw(cube, coat, roots, Part(new Vector3(0.27f, 1.04f, 0.17f), new Vector3(0f, 0f, 12f), new Vector3(0.13f, 0.56f, 0.15f)));
            Draw(cube, wood, roots, Part(new Vector3(0.18f, 1.18f, 0.18f), new Vector3(0f, 0f, -4f), new Vector3(0.060f, 1.45f, 0.060f)));
            Draw(cube, steel, roots, Part(new Vector3(0.23f, 2.18f, 0.18f), new Vector3(0f, 0f, -4f), new Vector3(0.026f, 0.58f, 0.026f)));
            return;
        }

        if (phase == SoldierPhase.Walk || phase == SoldierPhase.ForcedMarch || phase == SoldierPhase.Run)
        {
            float armScale = phase == SoldierPhase.Run ? 0.75f : 0.45f;
            Draw(cube, coat, roots, Part(new Vector3(-0.31f, 1.00f, 0.02f), new Vector3(-swing * armScale, 0f, -7f), new Vector3(0.13f, 0.58f, 0.15f)));
            Draw(cube, coat, roots, Part(new Vector3(0.31f, 1.00f, 0.02f), new Vector3(swing * armScale, 0f, 7f), new Vector3(0.13f, 0.58f, 0.15f)));

            float rifleTilt = phase == SoldierPhase.Run ? -18f : phase == SoldierPhase.ForcedMarch ? -10f : -6f;
            Draw(cube, wood, roots, Part(new Vector3(0.40f, 1.18f, 0.10f), new Vector3(rifleTilt, 0f, -8f), new Vector3(0.060f, 1.42f, 0.060f)));
            Draw(cube, steel, roots, Part(new Vector3(0.50f, 2.14f, 0.22f), new Vector3(rifleTilt, 0f, -8f), new Vector3(0.026f, 0.62f, 0.026f)));
            return;
        }

        Draw(cube, coat, roots, Part(new Vector3(-0.31f, 1.01f, 0f), new Vector3(0f, 0f, -7f), new Vector3(0.13f, 0.58f, 0.15f)));
        Draw(cube, coat, roots, Part(new Vector3(0.31f, 1.01f, 0f), new Vector3(0f, 0f, 7f), new Vector3(0.13f, 0.58f, 0.15f)));
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

    private static float GetMovementFrequency(SoldierPhase phase)
    {
        switch (phase)
        {
            case SoldierPhase.Walk:
                return 5.2f;
            case SoldierPhase.ForcedMarch:
                return 7.0f;
            case SoldierPhase.Run:
                return 9.0f;
            default:
                return 0f;
        }
    }

    private static float GetMovementBob(SoldierPhase phase)
    {
        switch (phase)
        {
            case SoldierPhase.Walk:
                return 0.025f;
            case SoldierPhase.ForcedMarch:
                return 0.045f;
            case SoldierPhase.Run:
                return 0.075f;
            default:
                return 0f;
        }
    }

    private static float GetMovementLean(SoldierPhase phase)
    {
        switch (phase)
        {
            case SoldierPhase.ForcedMarch:
                return 2.5f;
            case SoldierPhase.Run:
                return 7.0f;
            default:
                return 0f;
        }
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

    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.SetActive(false);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.Destroy(temp);
        return mesh;
    }

    private void CreateMaterials()
    {
        dkCoat = Mat(new Color(0.035f, 0.085f, 0.17f), "09F7_DK_Navy");
        dkTrousers = Mat(new Color(0.30f, 0.43f, 0.58f), "09F7_DK_BlueGrey");
        prCoat = Mat(new Color(0.10f, 0.12f, 0.16f), "09F7_PR_Dark");
        prTrousers = Mat(new Color(0.18f, 0.20f, 0.24f), "09F7_PR_Trousers");
        red = Mat(new Color(0.72f, 0.04f, 0.055f), "09F7_RedTrim");
        white = Mat(new Color(0.88f, 0.86f, 0.78f), "09F7_WhiteLeather");
        black = Mat(new Color(0.035f, 0.035f, 0.04f), "09F7_BlackLeather");
        brown = Mat(new Color(0.20f, 0.12f, 0.065f), "09F7_BrownLeather");
        skin = Mat(new Color(0.66f, 0.48f, 0.38f), "09F7_Skin");
        wood = Mat(new Color(0.22f, 0.12f, 0.055f), "09F7_RifleWood");
        steel = Mat(new Color(0.48f, 0.52f, 0.55f), "09F7_SteelBayonet");
        brass = Mat(new Color(0.68f, 0.52f, 0.18f), "09F7_Brass");
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
        {
            Regiment regiment = remove[i];
            if (regiment != null && states.TryGetValue(regiment, out State state) && state.Smoke != null)
                UnityEngine.Object.Destroy(state.Smoke.gameObject);
            states.Remove(regiment);
        }
    }
}
