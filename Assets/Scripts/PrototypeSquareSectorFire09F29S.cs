using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29s
// Square fire hardening and presentation patch.
//
// F29/F29M already provide the tactical Square state and the physical hollow square.
// This layer deliberately does NOT become a movement owner. It only:
//   - locks the square's facing so the whole formation no longer turns toward one threat,
//   - suppresses the old square outline / 360-degree range rings,
//   - renders four fixed 90-degree fire sectors (front/right/rear/left),
//   - gives each face its own reload clock and target selection,
//   - reuses Regiment.FireVolley so existing accuracy, terrain, morale, smoke and casualty
//     processing remain authoritative.
//
// The existing square accuracy factor (0.42 applied to Regiment's normal 58% firing-men
// model) represents about 24.4% of company strength per face, i.e. approximately 25%.
[DefaultExecutionOrder(39750)]
public sealed class PrototypeSquareSectorFire09F29S : MonoBehaviour
{
    private sealed class RuntimeState
    {
        public Quaternion LockedRotation;
        public bool RotationCaptured;
        public float OriginalGlobalNextFireTime;
        public bool GlobalTimerCaptured;
        public float LastFaceShotAt = -999f;
        public readonly float[] FaceNextFireTime = new float[4];
        public GameObject VisualRoot;
        public readonly LineRenderer[] Close = new LineRenderer[4];
        public readonly LineRenderer[] Medium = new LineRenderer[4];
        public readonly LineRenderer[] Long = new LineRenderer[4];
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float LegacyGlobalTimerBlock = 3600f;
    private const float SectorHalfAngle = 45f;
    private const int ArcSegments = 14;

    private readonly Dictionary<Regiment, RuntimeState> states =
        new Dictionary<Regiment, RuntimeState>();
    private readonly HashSet<Regiment> seenThisFrame = new HashSet<Regiment>();

    private FieldInfo nextFireTimeField;
    private MethodInfo fireVolleyMethod;

    private Material closeMaterial;
    private Material mediumMaterial;
    private Material longMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareSectorFire09F29S>() == null)
        {
            new GameObject("PrototypeSquareSectorFire_v000009f29s")
                .AddComponent<PrototypeSquareSectorFire09F29S>();
        }
    }

    private void Awake()
    {
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", PrivateInstance);
        fireVolleyMethod = typeof(Regiment).GetMethod("FireVolley", PrivateInstance);

        closeMaterial = MakeMaterial(new Color(1.00f, 0.92f, 0.10f, 0.88f), "Square29S_Close");
        mediumMaterial = MakeMaterial(new Color(1.00f, 0.58f, 0.05f, 0.84f), "Square29S_Medium");
        longMaterial = MakeMaterial(new Color(1.00f, 0.16f, 0.04f, 0.82f), "Square29S_Long");

        Debug.Log(
            "SQUARE-SECTOR-09F29S|Installed=True|Faces=4|DegreesPerFace=90|" +
            "ApproxFireSharePerFace=25%|IndependentFaceReload=True|SquareRotationLocked=True|" +
            "LegacySquareOutline=False|Legacy360Rings=False|MovementOwner=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        seenThisFrame.Clear();

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            seenThisFrame.Add(unit);
            RuntimeState state = GetOrCreateState(unit);
            if (state == null)
                continue;

            // F29's first square-fire implementation turns the whole company toward
            // the current threat. F29S restores the heading after that layer has run,
            // so the four physical faces remain fixed in world-space.
            unit.transform.rotation = state.LockedRotation;

            SuppressLegacySquareVisuals(unit);
            SuppressLegacyForwardFans(unit);
            EnsureWorldSpaceSmoke(unit, true);

            if (PrototypeInfantrySquare09F29.IsSquareReady(unit))
            {
                OwnLegacyGlobalFireTimer(unit, state);
                ProcessFourFaces(unit, state);
                OwnLegacyGlobalFireTimer(unit, state);
            }

            UpdateSectorVisuals(unit, state);
        }

        CleanupExitedSquares();
    }

    private void LateUpdate()
    {
        // Run after F29/F29M visual writers. This is presentation-only cleanup and
        // heading restoration; no route, destination or movement state is written.
        foreach (KeyValuePair<Regiment, RuntimeState> pair in states)
        {
            Regiment unit = pair.Key;
            RuntimeState state = pair.Value;
            if (unit == null || state == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            unit.transform.rotation = state.LockedRotation;
            SuppressLegacySquareVisuals(unit);
            SuppressLegacyForwardFans(unit);
            UpdateSectorVisuals(unit, state);
        }
    }

    private RuntimeState GetOrCreateState(Regiment unit)
    {
        if (states.TryGetValue(unit, out RuntimeState existing))
            return existing;

        RuntimeState state = new RuntimeState
        {
            LockedRotation = unit.transform.rotation,
            RotationCaptured = true,
            OriginalGlobalNextFireTime = ReadGlobalNextFireTime(unit),
            GlobalTimerCaptured = nextFireTimeField != null
        };

        float initialTimer = Mathf.Max(Time.time, state.OriginalGlobalNextFireTime);
        for (int i = 0; i < state.FaceNextFireTime.Length; i++)
            state.FaceNextFireTime[i] = initialTimer;

        BuildSectorVisuals(unit, state);
        states.Add(unit, state);

        Debug.Log(
            "SQUARE-SECTOR-09F29S|Unit=" + unit.RegimentName +
            "|State=ENTER|Heading=" + unit.transform.eulerAngles.y.ToString("0.0") +
            "|Faces=FRONT,RIGHT,REAR,LEFT");

        return state;
    }

    private void ProcessFourFaces(Regiment unit, RuntimeState state)
    {
        if (unit == null || state == null || unit.IsRouted || unit.CurrentStrength <= 0)
            return;
        if (unit.FirePolicy == RegimentFirePolicy.HoldFire)
            return;
        if (fireVolleyMethod == null)
            return;

        float range = unit.GetFireTriggerRange();
        if (range <= 0.01f)
            return;

        for (int face = 0; face < 4; face++)
        {
            if (Time.time < state.FaceNextFireTime[face])
                continue;

            Regiment target = FindNearestEnemyOnFace(unit, state.LockedRotation, face, range);
            if (target == null)
                continue;

            if (FireFromFace(unit, state, face, target))
            {
                float next = ReadGlobalNextFireTime(unit);
                if (next <= Time.time)
                    next = Time.time + unit.CurrentReloadSeconds;
                state.FaceNextFireTime[face] = next;
                state.LastFaceShotAt = Time.time;

                Debug.Log(
                    "SQUARE-SECTOR-09F29S|Unit=" + unit.RegimentName +
                    "|Face=" + FaceName(face) +
                    "|Target=" + target.RegimentName +
                    "|Distance=" + PlanarDistance(unit.transform.position, target.transform.position).ToString("0.0") +
                    "|Policy=" + unit.GetFirePolicyLabel() +
                    "|ApproxFireShare=25%");
            }
        }
    }

    private bool FireFromFace(Regiment unit, RuntimeState state, int face, Regiment target)
    {
        if (unit == null || state == null || target == null)
            return false;

        Vector3 direction = target.transform.position - unit.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return false;

        Quaternion locked = state.LockedRotation;

        try
        {
            // FireVolley's legacy CanFireAt check is front-arc based. Rotate only for
            // the duration of this method call, then immediately restore the square.
            // No rendered frame or other movement owner sees the temporary rotation.
            unit.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            fireVolleyMethod.Invoke(unit, new object[] { target });
            return true;
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogWarning(
                "SQUARE-SECTOR-09F29S|Unit=" + unit.RegimentName +
                "|Face=" + FaceName(face) +
                "|FireInvokeFailed=" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "SQUARE-SECTOR-09F29S|Unit=" + unit.RegimentName +
                "|Face=" + FaceName(face) +
                "|FireInvokeFailed=" + ex.Message);
            return false;
        }
        finally
        {
            unit.transform.rotation = locked;
        }
    }

    private static Regiment FindNearestEnemyOnFace(
        Regiment unit,
        Quaternion lockedRotation,
        int face,
        float maxRange)
    {
        BattleManager battle = BattleManager.Instance;
        if (unit == null || battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = maxRange;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == unit || candidate.Team == unit.Team ||
                candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(unit.transform.position, candidate.transform.position);
            if (distance > best)
                continue;

            if (GetFaceIndex(lockedRotation, unit.transform.position, candidate.transform.position) != face)
                continue;

            best = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static int GetFaceIndex(Quaternion lockedRotation, Vector3 from, Vector3 to)
    {
        Vector3 world = to - from;
        world.y = 0f;
        if (world.sqrMagnitude < 0.01f)
            return 0;

        Vector3 local = Quaternion.Inverse(lockedRotation) * world.normalized;
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        int quarter = Mathf.RoundToInt(angle / 90f);
        quarter %= 4;
        if (quarter < 0)
            quarter += 4;
        return quarter; // 0 front, 1 right, 2 rear, 3 left
    }

    private void OwnLegacyGlobalFireTimer(Regiment unit, RuntimeState state)
    {
        if (nextFireTimeField == null || unit == null || state == null)
            return;

        // Regiment has one historical reload timer. Square now owns four face timers,
        // so park the legacy timer far ahead while Square is active. FireVolley itself
        // does not gate on nextFireTime, allowing this layer to call it per face.
        nextFireTimeField.SetValue(unit, Time.time + LegacyGlobalTimerBlock);
    }

    private float ReadGlobalNextFireTime(Regiment unit)
    {
        if (nextFireTimeField == null || unit == null)
            return Time.time;
        object value = nextFireTimeField.GetValue(unit);
        return value is float ? (float)value : Time.time;
    }

    private void RestoreGlobalFireTimer(Regiment unit, RuntimeState state)
    {
        if (nextFireTimeField == null || unit == null || state == null)
            return;

        float restore = state.GlobalTimerCaptured ? state.OriginalGlobalNextFireTime : Time.time;
        if (state.LastFaceShotAt > -900f)
            restore = Mathf.Max(restore, state.LastFaceShotAt + unit.CurrentReloadSeconds * 0.80f);

        nextFireTimeField.SetValue(unit, Mathf.Max(Time.time + 0.05f, restore));
    }

    private void BuildSectorVisuals(Regiment unit, RuntimeState state)
    {
        GameObject root = new GameObject("SquareSector09F29S_" + unit.RegimentName);
        root.transform.SetParent(transform, false);
        state.VisualRoot = root;

        for (int face = 0; face < 4; face++)
        {
            state.Close[face] = MakeLine(root.transform, "Close_" + FaceName(face), 0.10f, closeMaterial);
            state.Medium[face] = MakeLine(root.transform, "Medium_" + FaceName(face), 0.12f, mediumMaterial);
            state.Long[face] = MakeLine(root.transform, "Long_" + FaceName(face), 0.15f, longMaterial);
        }
    }

    private static LineRenderer MakeLine(Transform parent, string name, float width, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 0;
        line.numCornerVertices = 1;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static Material MakeMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

    private void UpdateSectorVisuals(Regiment unit, RuntimeState state)
    {
        if (unit == null || state == null)
            return;

        bool visible = unit.IsSelected && unit.ShowRange && PrototypeInfantrySquare09F29.IsSquareReady(unit);
        float half = GetSquareHalfSide(unit.CurrentStrength) + 0.65f;

        for (int face = 0; face < 4; face++)
        {
            DrawSectorArc(
                state.Close[face], unit.transform.position, state.LockedRotation,
                half, unit.CloseRange + half, face, visible, false);
            DrawSectorArc(
                state.Medium[face], unit.transform.position, state.LockedRotation,
                half, unit.EffectiveRange + half, face, visible, false);
            DrawSectorArc(
                state.Long[face], unit.transform.position, state.LockedRotation,
                half, unit.MaximumRange + half, face, visible, true);
        }
    }

    private static void DrawSectorArc(
        LineRenderer line,
        Vector3 center,
        Quaternion rotation,
        float innerHalf,
        float outerRadius,
        int face,
        bool visible,
        bool includeSideRays)
    {
        if (line == null)
            return;

        line.enabled = visible;
        if (!visible)
            return;

        float centerAngle = face * 90f;
        float startAngle = centerAngle - SectorHalfAngle;
        float endAngle = centerAngle + SectorHalfAngle;
        int count = ArcSegments + 1 + (includeSideRays ? 2 : 0);
        line.positionCount = count;
        int index = 0;

        Vector3 startDirection = RotateFlat(rotation, startAngle);
        Vector3 endDirection = RotateFlat(rotation, endAngle);

        if (includeSideRays)
        {
            Vector3 innerStart = SquareBoundaryPoint(center, rotation, innerHalf, startDirection);
            line.SetPosition(index++, GroundPoint(innerStart));
        }

        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = i / (float)ArcSegments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector3 direction = RotateFlat(rotation, angle);
            line.SetPosition(index++, GroundPoint(center + direction * outerRadius));
        }

        if (includeSideRays)
        {
            Vector3 innerEnd = SquareBoundaryPoint(center, rotation, innerHalf, endDirection);
            line.SetPosition(index++, GroundPoint(innerEnd));
        }
    }

    private static Vector3 RotateFlat(Quaternion rotation, float localAngle)
    {
        Vector3 local = Quaternion.AngleAxis(localAngle, Vector3.up) * Vector3.forward;
        Vector3 world = rotation * local;
        world.y = 0f;
        return world.sqrMagnitude > 0.001f ? world.normalized : Vector3.forward;
    }

    private static Vector3 SquareBoundaryPoint(
        Vector3 center,
        Quaternion rotation,
        float half,
        Vector3 worldDirection)
    {
        Vector3 local = Quaternion.Inverse(rotation) * worldDirection.normalized;
        float divisor = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z), 0.001f);
        float distance = half / divisor;
        return center + worldDirection.normalized * distance;
    }

    private static Vector3 GroundPoint(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.43f;
        return point;
    }

    private static float GetSquareHalfSide(int strength)
    {
        int perSidePerRank = Mathf.CeilToInt(Mathf.Max(1, strength) / 8f);
        float sideLength = Mathf.Max(7.5f, (perSidePerRank - 1) * 0.75f);
        return Mathf.Clamp(sideLength * 0.5f, 4.0f, 10.5f);
    }

    private static void SuppressLegacySquareVisuals(Regiment unit)
    {
        if (unit == null)
            return;

        GameObject root = GameObject.Find("Square29_" + unit.RegimentName);
        if (root == null)
            return;

        DisableLine(root.transform.Find("SquareOutline"));
        DisableLine(root.transform.Find("SquareClose"));
        DisableLine(root.transform.Find("SquareMedium"));
        DisableLine(root.transform.Find("SquareLong"));
    }

    private static void SuppressLegacyForwardFans(Regiment unit)
    {
        if (unit == null)
            return;

        DisableLine(unit.transform.Find("CloseRangeFan"));
        DisableLine(unit.transform.Find("MediumRangeFan"));
        DisableLine(unit.transform.Find("LongRangeFan"));
    }

    private static void DisableLine(Transform child)
    {
        if (child == null)
            return;
        LineRenderer line = child.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = false;
    }

    private static void EnsureWorldSpaceSmoke(Regiment unit, bool worldSpace)
    {
        if (unit == null)
            return;
        Transform smokeRoot = unit.transform.Find("BlackPowderSmoke");
        if (smokeRoot == null)
            return;
        ParticleSystem smoke = smokeRoot.GetComponent<ParticleSystem>();
        if (smoke == null)
            return;

        ParticleSystem.MainModule main = smoke.main;
        main.simulationSpace = worldSpace
            ? ParticleSystemSimulationSpace.World
            : ParticleSystemSimulationSpace.Local;
    }

    private void CleanupExitedSquares()
    {
        if (states.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, RuntimeState> pair in states)
        {
            Regiment unit = pair.Key;
            if (unit != null && seenThisFrame.Contains(unit) && PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(unit);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
        {
            Regiment unit = stale[i];
            if (!states.TryGetValue(unit, out RuntimeState state))
                continue;

            if (unit != null)
            {
                RestoreGlobalFireTimer(unit, state);
                EnsureWorldSpaceSmoke(unit, false);
                unit.RefreshRangeVisibility();
                Debug.Log("SQUARE-SECTOR-09F29S|Unit=" + unit.RegimentName + "|State=EXIT|LegacyFireRestored=True");
            }

            if (state.VisualRoot != null)
                Destroy(state.VisualRoot);

            states.Remove(unit);
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Regiment, RuntimeState> pair in states)
        {
            if (pair.Key != null)
            {
                RestoreGlobalFireTimer(pair.Key, pair.Value);
                EnsureWorldSpaceSmoke(pair.Key, false);
            }
            if (pair.Value != null && pair.Value.VisualRoot != null)
                Destroy(pair.Value.VisualRoot);
        }
        states.Clear();
    }

    private static string FaceName(int face)
    {
        switch (face)
        {
            case 0: return "FRONT";
            case 1: return "RIGHT";
            case 2: return "REAR";
            default: return "LEFT";
        }
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
