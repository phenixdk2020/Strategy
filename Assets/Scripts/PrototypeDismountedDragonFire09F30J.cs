using System.Collections.Generic;
using UnityEngine;

// v00.00.09f30j
// Dismounted Dragon carbine combat.
// 25% prototype horse-holders remain with the horses; ~75% form a two-rank firing line
// 18m forward. Uses the same 35/70/100m and +/-35deg visual language as infantry.
[DefaultExecutionOrder(43500)]
public sealed class PrototypeDismountedDragonFire09F30J : MonoBehaviour
{
    private sealed class State
    {
        public float NextFire;
        public float AmmoRoundsPerMan = 20f;
        public RegimentFirePolicy FirePolicy = RegimentFirePolicy.MediumRange;
        public Regiment Target;
        public ParticleSystem Smoke;
        public LineRenderer Close;
        public LineRenderer Medium;
        public LineRenderer Long;
    }

    private const float CloseRange = 35f;
    private const float MediumRange = 70f;
    private const float LongRange = 100f;
    private const float HalfArcDegrees = 35f;
    private const float ReloadSeconds = 7.0f;
    private const int ArcSegments = 32;

    private static PrototypeDismountedDragonFire09F30J instance;
    private readonly Dictionary<PrototypeCavalryUnit09F30, State> states =
        new Dictionary<PrototypeCavalryUnit09F30, State>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeDismountedDragonFire09F30J>() == null)
            new GameObject("PrototypeDismountedDragonFire_v000009f30j")
                .AddComponent<PrototypeDismountedDragonFire09F30J>();
    }

    private void Awake()
    {
        instance = this;
        Debug.Log("DRAGON-FIRE-09F30S|Installed=True|HorseHolders=25pct|CombatGroup=75pct|" +
                  "Forward=18m|Ranges=35/70/100|FirePolicies=HOLD/CLOSE/MED/LONG|" +
                  "Default=MED|Cone=70deg|Reload=7s");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static float GetAmmoRoundsPerMan(PrototypeCavalryUnit09F30 unit)
    {
        if (instance == null || unit == null || !instance.states.TryGetValue(unit, out State s))
            return 20f;
        return s.AmmoRoundsPerMan;
    }

    public static string GetTargetName(PrototypeCavalryUnit09F30 unit)
    {
        if (instance == null || unit == null || !instance.states.TryGetValue(unit, out State s) || s.Target == null)
            return "—";
        return s.Target.RegimentName;
    }

    public static RegimentFirePolicy GetFirePolicy(PrototypeCavalryUnit09F30 unit)
    {
        if (instance == null || unit == null)
            return RegimentFirePolicy.MediumRange;

        return instance.GetState(unit).FirePolicy;
    }

    public static void SetFirePolicy(
        PrototypeCavalryUnit09F30 unit,
        RegimentFirePolicy policy)
    {
        if (instance == null || unit == null)
            return;

        State state = instance.GetState(unit);
        state.FirePolicy = policy;

        if (policy == RegimentFirePolicy.HoldFire)
            state.Target = null;

        Debug.Log("DRAGON-FIRE-09F30S|Unit=" + unit.UnitName +
                  "|FirePolicy=" + FirePolicyLabel(policy) +
                  "|Range=" + GetPolicyRange(policy).ToString("0"));
    }

    public static string GetFirePolicyLabel(PrototypeCavalryUnit09F30 unit)
    {
        return FirePolicyLabel(GetFirePolicy(unit));
    }

    public static float GetSelectedRange(PrototypeCavalryUnit09F30 unit)
    {
        return GetPolicyRange(GetFirePolicy(unit));
    }

    private void Update()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed || cavalry.Dragon == null)
            return;

        PrototypeCavalryUnit09F30 dragon = cavalry.Dragon;
        State state = GetState(dragon);
        bool dismounted = dragon.Mode == PrototypeCavalryMode09F30.Dismounted;

        UpdateCones(dragon, state, dismounted && dragon.IsSelected);

        if (!dismounted ||
            PrototypeCavalryAnimation09F30I.IsTransitioning(dragon) ||
            dragon.Action != PrototypeCavalryAction09F30.Hold ||
            dragon.IsReforming ||
            state.AmmoRoundsPerMan <= 0.01f)
        {
            state.Target = null;
            return;
        }

        if (state.FirePolicy == RegimentFirePolicy.HoldFire)
        {
            state.Target = null;
            return;
        }

        state.Target = FindTarget(dragon, state.FirePolicy);
        if (state.Target == null || Time.time < state.NextFire)
            return;

        Fire(dragon, state, state.Target);
    }

    private State GetState(PrototypeCavalryUnit09F30 unit)
    {
        if (states.TryGetValue(unit, out State s))
            return s;

        s = new State
        {
            NextFire = Time.time + 1.0f,
            Close = CreateLine("DragonCloseCone09F30J", 0.15f, new Color(1.00f, 0.92f, 0.10f, 0.96f)),
            Medium = CreateLine("DragonMediumCone09F30J", 0.18f, new Color(1.00f, 0.58f, 0.05f, 0.94f)),
            Long = CreateLine("DragonLongCone09F30J", 0.22f, new Color(1.00f, 0.16f, 0.04f, 0.94f)),
            Smoke = CreateSmoke(unit)
        };
        states.Add(unit, s);
        return s;
    }

    private static Regiment FindTarget(
        PrototypeCavalryUnit09F30 dragon,
        RegimentFirePolicy policy)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null ||
            policy == RegimentFirePolicy.HoldFire)
            return null;

        float triggerRange = GetPolicyRange(policy);
        if (triggerRange <= 0.01f)
            return null;

        Vector3 origin = dragon.GetDismountedCombatCenterWorld();
        Vector3 forward = dragon.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Regiment best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team != BattleTeam.Prussia ||
                candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;

            Vector3 delta = candidate.transform.position - origin;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > triggerRange || distance >= bestDistance || distance < 0.1f)
                continue;

            float angle = Vector3.Angle(forward, delta.normalized);
            if (angle > HalfArcDegrees)
                continue;

            best = candidate;
            bestDistance = distance;
        }
        return best;
    }

    private static void Fire(PrototypeCavalryUnit09F30 dragon, State state, Regiment target)
    {
        Vector3 origin = dragon.GetDismountedCombatCenterWorld();
        float distance = PlanarDistance(origin, target.transform.position);
        int combatMen = dragon.GetDismountedCombatStrength();

        float t = Mathf.Clamp01(distance / LongRange);
        float hitRate = Mathf.Lerp(0.040f, 0.012f, t);
        int hits = Mathf.Clamp(
            Mathf.RoundToInt(combatMen * hitRate * Random.Range(0.78f, 1.22f)),
            0,
            12);
        float shock = Mathf.Lerp(3.4f, 1.0f, t);

        target.ReceiveVolley(hits, shock, null);
        state.AmmoRoundsPerMan = Mathf.Max(0f, state.AmmoRoundsPerMan - 1f);
        state.NextFire = Time.time + ReloadSeconds * Random.Range(0.90f, 1.12f);
        EmitSmoke(dragon, state);

        Debug.Log("DRAGON-FIRE-09F30R|Unit=" + dragon.UnitName +
                  "|CombatMen=" + combatMen +
                  "|HorseHolders=" + dragon.GetHorseHolderStrength() +
                  "|Target=" + target.RegimentName +
                  "|Distance=" + distance.ToString("0.0") +
                  "|Hits=" + hits +
                  "|AmmoPerMan=" + state.AmmoRoundsPerMan.ToString("0"));
    }

    private static LineRenderer CreateLine(string name, float width, Color color)
    {
        GameObject root = new GameObject(name);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        line.sharedMaterial = new Material(shader) { name = name + "_Mat", color = color };
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    private static void UpdateCones(
        PrototypeCavalryUnit09F30 dragon,
        State state,
        bool visible)
    {
        state.Close.enabled = visible;
        state.Medium.enabled = visible;
        state.Long.enabled = visible;
        if (!visible)
            return;

        BuildFan(dragon, state.Close, CloseRange);
        BuildFan(dragon, state.Medium, MediumRange);
        BuildFan(dragon, state.Long, LongRange);

        // Keep all three ranges readable like infantry, but make the selected
        // engagement band materially stronger. HOLD leaves all as reference only.
        SetConeEmphasis(
            state.Close,
            state.FirePolicy == RegimentFirePolicy.CloseRange,
            0.15f);
        SetConeEmphasis(
            state.Medium,
            state.FirePolicy == RegimentFirePolicy.MediumRange,
            0.18f);
        SetConeEmphasis(
            state.Long,
            state.FirePolicy == RegimentFirePolicy.LongRange,
            0.22f);
    }

    private static void SetConeEmphasis(
        LineRenderer line,
        bool active,
        float baseWidth)
    {
        if (line == null)
            return;

        Color baseColor = line.sharedMaterial != null
            ? line.sharedMaterial.color
            : line.startColor;

        baseColor.a = active ? 1.00f : 0.22f;
        line.startColor = baseColor;
        line.endColor = baseColor;
        line.widthMultiplier = active
            ? baseWidth * 1.55f
            : baseWidth * 0.58f;
    }

    private static void BuildFan(
        PrototypeCavalryUnit09F30 dragon,
        LineRenderer line,
        float range)
    {
        Vector3 forward = dragon.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.right;

        Vector3 center = dragon.GetDismountedCombatCenterWorld();
        int combatMen = dragon.GetDismountedCombatStrength();
        int columns = Mathf.CeilToInt(combatMen / 2f);
        float width = Mathf.Max(6f, (columns - 1) * 0.78f);
        float halfWidth = width * 0.5f;

        // F30S: build the two side rays explicitly from mirrored endpoints.
        // The old fan interpolated muzzle position and ray angle together, which
        // could make one side look kinked/skewed on sloped terrain.
        Vector3 baseCenter = center + forward * 0.65f;
        Vector3 leftFront = baseCenter - right * halfWidth;
        Vector3 rightFront = baseCenter + right * halfWidth;

        float halfArcRad = HalfArcDegrees * Mathf.Deg2Rad;
        Vector3 leftDir =
            (forward * Mathf.Cos(halfArcRad) -
             right * Mathf.Sin(halfArcRad)).normalized;
        Vector3 rightDir =
            (forward * Mathf.Cos(halfArcRad) +
             right * Mathf.Sin(halfArcRad)).normalized;

        Vector3 leftFar = leftFront + leftDir * range;
        Vector3 rightFar = rightFront + rightDir * range;

        List<Vector3> points = new List<Vector3>(ArcSegments + 5);
        points.Add(Terrain(leftFront));
        points.Add(Terrain(leftFar));

        // Far edge is generated symmetrically between the two explicit side rays.
        for (int i = 1; i < ArcSegments; i++)
        {
            float p = i / (float)ArcSegments;
            float angle =
                Mathf.Lerp(-HalfArcDegrees, HalfArcDegrees, p) *
                Mathf.Deg2Rad;
            Vector3 dir =
                forward * Mathf.Cos(angle) +
                right * Mathf.Sin(angle);

            float lateralBase =
                Mathf.Lerp(-halfWidth, halfWidth, p);
            Vector3 muzzle =
                baseCenter + right * lateralBase;

            points.Add(Terrain(muzzle + dir.normalized * range));
        }

        points.Add(Terrain(rightFar));
        points.Add(Terrain(rightFront));
        points.Add(Terrain(leftFront));

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private static ParticleSystem CreateSmoke(PrototypeCavalryUnit09F30 dragon)
    {
        GameObject go = new GameObject("DragonCarbineSmoke09F30J");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 3.0f;
        main.startSpeed = 0.9f;
        main.startSize = 1.35f;
        main.startColor = new Color(0.88f, 0.88f, 0.86f, 0.70f);
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null) renderer.material = new Material(shader);
        return ps;
    }

    private static void EmitSmoke(PrototypeCavalryUnit09F30 dragon, State state)
    {
        if (state.Smoke == null)
            return;

        Vector3 forward = dragon.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 pos = dragon.GetDismountedCombatCenterWorld() + forward * 0.8f;
        pos.y = PrototypeBootstrap.SampleGroundHeight(pos.x, pos.z) + 1.05f;
        state.Smoke.transform.position = pos;
        state.Smoke.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        var shape = state.Smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        int columns = Mathf.CeilToInt(dragon.GetDismountedCombatStrength() / 2f);
        shape.scale = new Vector3(Mathf.Max(7f, columns * 0.78f), 0.45f, 0.8f);
        state.Smoke.Emit(Random.Range(14, 24));
    }

    private static float GetPolicyRange(RegimentFirePolicy policy)
    {
        switch (policy)
        {
            case RegimentFirePolicy.CloseRange:
                return CloseRange;
            case RegimentFirePolicy.MediumRange:
                return MediumRange;
            case RegimentFirePolicy.LongRange:
                return LongRange;
            default:
                return 0f;
        }
    }

    private static string FirePolicyLabel(RegimentFirePolicy policy)
    {
        switch (policy)
        {
            case RegimentFirePolicy.HoldFire:
                return "HOLD";
            case RegimentFirePolicy.CloseRange:
                return "CLOSE";
            case RegimentFirePolicy.LongRange:
                return "LONG";
            default:
                return "MED";
        }
    }

    private static Vector3 Terrain(Vector3 p)
    {
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.42f;
        return p;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
