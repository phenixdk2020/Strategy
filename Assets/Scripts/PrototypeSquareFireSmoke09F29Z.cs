using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29z
// Authoritative infantry-square face fire + directional black-powder smoke.
// Keeps F29V's proven sector visuals but disables its legacy FireVolley reflection path.
// Each of the four 90-degree faces owns its own reload clock and approximately 25% of
// normal company firepower. Only the face that actually fires emits smoke.
[DefaultExecutionOrder(39940)]
public sealed class PrototypeSquareFireSmoke09F29Z : MonoBehaviour
{
    private sealed class UnitState
    {
        public Quaternion LockedRotation;
        public readonly float[] NextFire = new float[4];
        public ParticleSystem Smoke;
    }

    private const float FaceFirepowerFraction = 0.25f;
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly Dictionary<Regiment, UnitState> states = new Dictionary<Regiment, UnitState>();
    private readonly HashSet<Regiment> seen = new HashSet<Regiment>();
    private FieldInfo legacyFireMethodField;
    private FieldInfo legacyRuntimeField;
    private bool legacySuppressionLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareFireSmoke09F29Z>() == null)
            new GameObject("PrototypeSquareFireSmoke_v000009f29z")
                .AddComponent<PrototypeSquareFireSmoke09F29Z>();
    }

    private void Awake()
    {
        legacyFireMethodField = typeof(PrototypeSquareCorrections09F29V)
            .GetField("fireVolleyMethod", AnyInstance);
        legacyRuntimeField = typeof(PrototypeSquareCorrections09F29V)
            .GetField("runtime", AnyInstance);

        Debug.Log(
            "SQUARE-FIRE-09F29Z|Installed=True|Faces=4|FaceArc=90|" +
            "FirepowerPerFace=0.25|DirectionalSmoke=True|LegacyF29VFire=False|" +
            "F29VVisualsRetained=True|HeadingAuthority=F29VLockedRotation");
    }

    private void Update()
    {
        SuppressF29VFireOnly();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        seen.Clear();

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            seen.Add(unit);
            UnitState state = GetState(unit);
            if (state == null)
                continue;

            // F29V is the heading authority. The older F29 square layer may briefly turn
            // the Regiment transform toward a threat earlier in Update; using F29V's
            // locked heading prevents FRONT/RIGHT/REAR/LEFT from rotating with the target.
            state.LockedRotation = ReadF29VLockedRotation(unit, state.LockedRotation);

            if (!PrototypeInfantrySquare09F29.IsSquareReady(unit))
                continue;
            if (unit.IsRouted || unit.CurrentStrength <= 0)
                continue;
            if (unit.FirePolicy == RegimentFirePolicy.HoldFire)
                continue;
            if (PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(unit) <= 0)
                continue;

            ProcessFaces(unit, state);
        }

        CleanupExited();
    }

    // F29V continues drawing the four coloured sectors, but its private reflected
    // Regiment.FireVolley handle is nulled before F29V.Update runs. This prevents
    // duplicate full-company volleys while preserving all of its visual code.
    private void SuppressF29VFireOnly()
    {
        PrototypeSquareCorrections09F29V owner =
            UnityEngine.Object.FindAnyObjectByType<PrototypeSquareCorrections09F29V>();
        if (owner == null || legacyFireMethodField == null)
            return;

        object current = legacyFireMethodField.GetValue(owner);
        if (current != null)
            legacyFireMethodField.SetValue(owner, null);

        if (!legacySuppressionLogged)
        {
            legacySuppressionLogged = true;
            Debug.Log("SQUARE-FIRE-09F29Z|F29VFireResolver=Suppressed|F29VSectorVisuals=Active");
        }
    }

    private Quaternion ReadF29VLockedRotation(Regiment unit, Quaternion fallback)
    {
        PrototypeSquareCorrections09F29V owner =
            UnityEngine.Object.FindAnyObjectByType<PrototypeSquareCorrections09F29V>();
        if (owner == null || legacyRuntimeField == null || unit == null)
            return fallback;

        IDictionary runtime = legacyRuntimeField.GetValue(owner) as IDictionary;
        if (runtime == null || !runtime.Contains(unit))
            return fallback;

        object legacyState = runtime[unit];
        if (legacyState == null)
            return fallback;

        FieldInfo rotationField = legacyState.GetType().GetField("LockedRotation", AnyInstance);
        if (rotationField == null)
            return fallback;

        object value = rotationField.GetValue(legacyState);
        return value is Quaternion ? (Quaternion)value : fallback;
    }

    private UnitState GetState(Regiment unit)
    {
        if (states.TryGetValue(unit, out UnitState state))
            return state;

        state = new UnitState
        {
            LockedRotation = ReadF29VLockedRotation(unit, unit.transform.rotation),
            Smoke = CreateSmoke(unit)
        };

        float first = Time.time + 0.20f;
        for (int i = 0; i < 4; i++)
            state.NextFire[i] = first;

        states.Add(unit, state);
        Debug.Log("SQUARE-FIRE-09F29Z|Unit=" + unit.RegimentName + "|State=ENTER");
        return state;
    }

    private void ProcessFaces(Regiment unit, UnitState state)
    {
        float maxRange = unit.GetFireTriggerRange();
        if (maxRange <= 0.01f)
            return;

        for (int face = 0; face < 4; face++)
        {
            if (Time.time < state.NextFire[face])
                continue;

            Regiment target = FindTarget(unit, state.LockedRotation, face, maxRange);
            if (target == null)
                continue;

            int hits = ResolveFaceVolley(unit, target);
            EmitFaceSmoke(unit, state, face);
            PrototypeCropFieldTerrain09F29R.NotifyVolley(unit);
            PrototypeCombatStatusManager.RegisterExternalVolley(
                unit,
                target,
                hits,
                FaceFirepowerFraction,
                "SQUARE_" + FaceName(face));

            state.NextFire[face] = Time.time +
                unit.CurrentReloadSeconds * Random.Range(0.90f, 1.12f);

            Debug.Log(
                "SQUARE-FIRE-09F29Z|Unit=" + unit.RegimentName +
                "|Face=" + FaceName(face) +
                "|Target=" + target.RegimentName +
                "|Hits=" + hits +
                "|Smoke=True|Fraction=" + FaceFirepowerFraction.ToString("0.00") +
                "|ReloadUntil=" + state.NextFire[face].ToString("0.00"));
        }
    }

    private static Regiment FindTarget(Regiment shooter, Quaternion rotation, int face, float maxRange)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == shooter || !candidate.gameObject.activeInHierarchy ||
                candidate.Team == shooter.Team || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(shooter.transform.position, candidate.transform.position);
            float visibility = PrototypeCropFieldTerrain09F29R.GetVisibilityRangeMultiplier(candidate);
            float visibleRange = maxRange * Mathf.Clamp(visibility, 0.50f, 1f);

            if (distance > visibleRange || distance >= best)
                continue;
            if (GetFaceIndex(rotation, shooter.transform.position, candidate.transform.position) != face)
                continue;

            nearest = candidate;
            best = distance;
        }

        return nearest;
    }

    private static int ResolveFaceVolley(Regiment shooter, Regiment target)
    {
        float distance = PlanarDistance(shooter.transform.position, target.transform.position);
        float expected = PrototypeCombatTuningManager.GetExpectedHitsPreview(shooter, distance);
        expected *= FaceFirepowerFraction;
        expected *= PrototypeCropFieldCombat09F29R.GetCombinedTargetMultiplier(shooter, target, distance);

        int maxHits = Mathf.Max(1, Mathf.RoundToInt(16f * FaceFirepowerFraction));
        int hits = Mathf.Clamp(
            Mathf.RoundToInt(expected * Random.Range(0.72f, 1.28f)),
            0,
            maxHits);

        float fullShock = Mathf.Lerp(
            5.0f,
            1.5f,
            Mathf.Clamp01(distance / Mathf.Max(1f, shooter.MaximumRange)));
        float shock = fullShock * FaceFirepowerFraction;

        target.ReceiveVolley(hits, shock, shooter);
        return hits;
    }

    private static ParticleSystem CreateSmoke(Regiment unit)
    {
        GameObject go = new GameObject("SquareFaceSmoke09F29Z");
        go.transform.SetParent(unit.transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 3.2f;
        main.startSpeed = 1.0f;
        main.startSize = 1.65f;
        main.startColor = new Color(0.88f, 0.88f, 0.86f, 0.72f);
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 0.45f, 0.8f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
            renderer.material = new Material(shader);

        return ps;
    }

    private static void EmitFaceSmoke(Regiment unit, UnitState state, int face)
    {
        if (unit == null || state == null || state.Smoke == null)
            return;

        float half = GetSquareHalfSide(unit.CurrentStrength);
        Vector3 outward = RotateFlat(state.LockedRotation, face * 90f);
        Vector3 faceCenter = unit.transform.position + outward * (half + 0.75f);
        faceCenter.y = PrototypeBootstrap.SampleGroundHeight(faceCenter.x, faceCenter.z) + 1.05f;

        Transform smokeTransform = state.Smoke.transform;
        smokeTransform.position = faceCenter;
        smokeTransform.rotation = Quaternion.LookRotation(outward, Vector3.up);

        var shape = state.Smoke.shape;
        shape.scale = new Vector3(Mathf.Max(5f, half * 2f), 0.45f, 0.8f);

        // Standard line volley uses 18-34 particles. One square face is ~25% of the
        // available firepower, so 5-9 particles gives comparable visual density.
        state.Smoke.Emit(Random.Range(5, 10));
    }

    private static int GetFaceIndex(Quaternion rotation, Vector3 from, Vector3 to)
    {
        Vector3 world = to - from;
        world.y = 0f;
        if (world.sqrMagnitude < 0.01f)
            return 0;

        Vector3 local = Quaternion.Inverse(rotation) * world.normalized;
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        int quarter = Mathf.RoundToInt(angle / 90f) % 4;
        return quarter < 0 ? quarter + 4 : quarter;
    }

    private static Vector3 RotateFlat(Quaternion rotation, float localAngle)
    {
        Vector3 local = Quaternion.AngleAxis(localAngle, Vector3.up) * Vector3.forward;
        Vector3 world = rotation * local;
        world.y = 0f;
        return world.sqrMagnitude > 0.001f ? world.normalized : Vector3.forward;
    }

    private static float GetSquareHalfSide(int strength)
    {
        int perSidePerRank = Mathf.CeilToInt(Mathf.Max(1, strength) / 8f);
        float sideLength = Mathf.Max(7.5f, (perSidePerRank - 1) * 0.75f);
        return Mathf.Clamp(sideLength * 0.5f, 4.0f, 10.5f);
    }

    private void CleanupExited()
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, UnitState> pair in states)
        {
            Regiment unit = pair.Key;
            if (unit != null && seen.Contains(unit))
                continue;

            if (pair.Value != null && pair.Value.Smoke != null)
                Destroy(pair.Value.Smoke.gameObject);

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(unit);
        }

        if (remove == null)
            return;
        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }

    private static string FaceName(int face)
    {
        switch (face)
        {
            case 1: return "RIGHT";
            case 2: return "REAR";
            case 3: return "LEFT";
            default: return "FRONT";
        }
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
