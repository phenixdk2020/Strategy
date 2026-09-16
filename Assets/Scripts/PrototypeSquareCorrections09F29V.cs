using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29v
// Corrects the F29S square presentation/fire ownership:
// - keeps the real SquareOutline visible,
// - hides the generic Major/HQ mission footprint while a company is in Square,
// - replaces the F29S sector-fire owner with four explicit 90-degree faces,
// - requires an actual valid enemy inside the selected fire range before a volley,
// - keeps independent reload clocks per face and parks Regiment's legacy global timer.
[DefaultExecutionOrder(39950)]
public sealed class PrototypeSquareCorrections09F29V : MonoBehaviour
{
    private sealed class SquareRuntime
    {
        public Quaternion LockedRotation;
        public float OriginalNextFireTime;
        public float LastShotAt = -999f;
        public readonly float[] FaceNextFire = new float[4];
        public GameObject VisualRoot;
        public readonly LineRenderer[] Close = new LineRenderer[4];
        public readonly LineRenderer[] Medium = new LineRenderer[4];
        public readonly LineRenderer[] Long = new LineRenderer[4];
    }

    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float SectorHalfAngle = 45f;
    private const int ArcSegments = 14;
    private const float GlobalTimerBlock = 3600f;

    private readonly Dictionary<Regiment, SquareRuntime> runtime = new Dictionary<Regiment, SquareRuntime>();
    private readonly HashSet<Regiment> seen = new HashSet<Regiment>();

    private FieldInfo nextFireTimeField;
    private MethodInfo fireVolleyMethod;
    private FieldInfo hierarchyVisualsField;
    private bool legacySectorDisabled;

    private Material closeMaterial;
    private Material mediumMaterial;
    private Material longMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareCorrections09F29V>() == null)
            new GameObject("PrototypeSquareCorrections_v000009f29v").AddComponent<PrototypeSquareCorrections09F29V>();
    }

    private void Awake()
    {
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", BindingFlags.Instance | BindingFlags.NonPublic);
        fireVolleyMethod = typeof(Regiment).GetMethod("FireVolley", BindingFlags.Instance | BindingFlags.NonPublic);
        hierarchyVisualsField = typeof(PrototypeRegimentHierarchy09F27).GetField("visuals", BindingFlags.Instance | BindingFlags.NonPublic);

        closeMaterial = MakeMaterial(new Color(1.00f, 0.92f, 0.10f, 0.88f), "Square29V_Close");
        mediumMaterial = MakeMaterial(new Color(1.00f, 0.58f, 0.05f, 0.84f), "Square29V_Medium");
        longMaterial = MakeMaterial(new Color(1.00f, 0.16f, 0.04f, 0.82f), "Square29V_Long");

        Debug.Log("SQUARE-09F29V|Installed=True|SquareOutline=KEEP|MissionFootprint=HIDE_IN_SQUARE|Faces=4|FireRequiresValidTarget=True");
    }

    private void Update()
    {
        DisableLegacyF29SSectorOwner();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        seen.Clear();
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            seen.Add(unit);
            SquareRuntime state = GetState(unit);
            if (state == null)
                continue;

            unit.transform.rotation = state.LockedRotation;
            SuppressLegacySquareRangeRings(unit);
            SuppressLegacyForwardFans(unit);

            if (PrototypeInfantrySquare09F29.IsSquareReady(unit))
            {
                ParkGlobalTimer(unit);
                ProcessFaces(unit, state);
                ParkGlobalTimer(unit);
            }

            UpdateSectorVisuals(unit, state);
        }

        CleanupExited();
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Regiment, SquareRuntime> pair in runtime)
        {
            Regiment unit = pair.Key;
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            unit.transform.rotation = pair.Value.LockedRotation;
            KeepSquareOutline(unit);
            SuppressLegacySquareRangeRings(unit);
            SuppressLegacyForwardFans(unit);
            SuppressMissionFootprint(unit);
            UpdateSectorVisuals(unit, pair.Value);
        }
    }

    private void DisableLegacyF29SSectorOwner()
    {
        if (legacySectorDisabled)
            return;

        PrototypeSquareSectorFire09F29S legacy = UnityEngine.Object.FindAnyObjectByType<PrototypeSquareSectorFire09F29S>();
        if (legacy == null)
            return;

        legacy.enabled = false;
        legacySectorDisabled = true;
        Debug.Log("SQUARE-09F29V|LegacyF29SSectorOwner=Disabled");
    }

    private SquareRuntime GetState(Regiment unit)
    {
        if (runtime.TryGetValue(unit, out SquareRuntime existing))
            return existing;

        float original = ReadNextFireTime(unit);
        SquareRuntime state = new SquareRuntime
        {
            LockedRotation = unit.transform.rotation,
            OriginalNextFireTime = original
        };

        float first = Mathf.Max(Time.time + 0.20f, original);
        for (int i = 0; i < 4; i++)
            state.FaceNextFire[i] = first;

        BuildSectorVisuals(unit, state);
        runtime.Add(unit, state);
        ParkGlobalTimer(unit);

        Debug.Log("SQUARE-09F29V|Unit=" + unit.RegimentName + "|State=ENTER|Heading=" + state.LockedRotation.eulerAngles.y.ToString("0.0"));
        return state;
    }

    private void ProcessFaces(Regiment unit, SquareRuntime state)
    {
        if (unit == null || state == null || unit.IsRouted || unit.CurrentStrength <= 0)
            return;
        if (unit.FirePolicy == RegimentFirePolicy.HoldFire || fireVolleyMethod == null)
            return;

        float maxRange = unit.GetFireTriggerRange();
        if (maxRange <= 0.01f)
            return;

        for (int face = 0; face < 4; face++)
        {
            if (Time.time < state.FaceNextFire[face])
                continue;

            Regiment target = FindTarget(unit, state.LockedRotation, face, maxRange);
            if (target == null)
                continue;

            if (!TryFire(unit, state, face, target))
                continue;

            float next = ReadNextFireTime(unit);
            if (next <= Time.time || next > Time.time + GlobalTimerBlock * 0.5f)
                next = Time.time + unit.CurrentReloadSeconds;
            state.FaceNextFire[face] = next;
            state.LastShotAt = Time.time;

            Debug.Log("SQUARE-09F29V|Unit=" + unit.RegimentName + "|Face=" + FaceName(face) +
                      "|Target=" + target.RegimentName + "|Distance=" + PlanarDistance(unit.transform.position, target.transform.position).ToString("0.0") +
                      "|Policy=" + unit.GetFirePolicyLabel() + "|Volley=True");
        }
    }

    private static Regiment FindTarget(Regiment unit, Quaternion rotation, int face, float maxRange)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = maxRange;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == unit || !candidate.gameObject.activeInHierarchy ||
                candidate.Team == unit.Team || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(unit.transform.position, candidate.transform.position);
            if (distance > best)
                continue;
            if (GetFaceIndex(rotation, unit.transform.position, candidate.transform.position) != face)
                continue;

            nearest = candidate;
            best = distance;
        }
        return nearest;
    }

    private bool TryFire(Regiment unit, SquareRuntime state, int face, Regiment target)
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
            // CanFireAt is the final hard gate. Temporarily face the selected square face
            // toward its target only for the validation/volley call, never for rendering.
            unit.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            if (!unit.CanFireAt(target))
                return false;

            fireVolleyMethod.Invoke(unit, new object[] { target });
            return true;
        }
        catch (Exception ex)
        {
            Exception message = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
            Debug.LogWarning("SQUARE-09F29V|Unit=" + unit.RegimentName + "|Face=" + FaceName(face) + "|VolleyFailed=" + message.Message);
            return false;
        }
        finally
        {
            unit.transform.rotation = locked;
        }
    }

    private void ParkGlobalTimer(Regiment unit)
    {
        if (unit != null && nextFireTimeField != null)
            nextFireTimeField.SetValue(unit, Time.time + GlobalTimerBlock);
    }

    private float ReadNextFireTime(Regiment unit)
    {
        if (unit == null || nextFireTimeField == null)
            return Time.time;
        object value = nextFireTimeField.GetValue(unit);
        return value is float ? (float)value : Time.time;
    }

    private void RestoreGlobalTimer(Regiment unit, SquareRuntime state)
    {
        if (unit == null || state == null || nextFireTimeField == null)
            return;
        float restore = state.OriginalNextFireTime;
        if (state.LastShotAt > -900f)
            restore = Mathf.Max(restore, state.LastShotAt + unit.CurrentReloadSeconds * 0.8f);
        nextFireTimeField.SetValue(unit, Mathf.Max(Time.time + 0.05f, restore));
    }

    private void BuildSectorVisuals(Regiment unit, SquareRuntime state)
    {
        GameObject root = new GameObject("SquareSector09F29V_" + unit.RegimentName);
        root.transform.SetParent(transform, false);
        state.VisualRoot = root;
        for (int face = 0; face < 4; face++)
        {
            state.Close[face] = MakeLine(root.transform, "Close_" + FaceName(face), 0.10f, closeMaterial);
            state.Medium[face] = MakeLine(root.transform, "Medium_" + FaceName(face), 0.12f, mediumMaterial);
            state.Long[face] = MakeLine(root.transform, "Long_" + FaceName(face), 0.15f, longMaterial);
        }
    }

    private void UpdateSectorVisuals(Regiment unit, SquareRuntime state)
    {
        bool visible = unit != null && unit.IsSelected && unit.ShowRange && PrototypeInfantrySquare09F29.IsSquareReady(unit);
        if (unit == null)
            return;

        float half = GetSquareHalfSide(unit.CurrentStrength) + 0.65f;
        for (int face = 0; face < 4; face++)
        {
            DrawArc(state.Close[face], unit.transform.position, state.LockedRotation, half, unit.CloseRange + half, face, visible, false);
            DrawArc(state.Medium[face], unit.transform.position, state.LockedRotation, half, unit.EffectiveRange + half, face, visible, false);
            DrawArc(state.Long[face], unit.transform.position, state.LockedRotation, half, unit.MaximumRange + half, face, visible, true);
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
        line.numCornerVertices = 1;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static void DrawArc(LineRenderer line, Vector3 center, Quaternion rotation, float innerHalf, float outerRadius, int face, bool visible, bool sideRays)
    {
        if (line == null)
            return;
        line.enabled = visible;
        if (!visible)
            return;

        float centerAngle = face * 90f;
        float startAngle = centerAngle - SectorHalfAngle;
        float endAngle = centerAngle + SectorHalfAngle;
        line.positionCount = ArcSegments + 1 + (sideRays ? 2 : 0);
        int index = 0;

        if (sideRays)
        {
            Vector3 dir = RotateFlat(rotation, startAngle);
            line.SetPosition(index++, GroundPoint(SquareBoundaryPoint(center, rotation, innerHalf, dir)));
        }

        for (int i = 0; i <= ArcSegments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)ArcSegments);
            Vector3 dir = RotateFlat(rotation, angle);
            line.SetPosition(index++, GroundPoint(center + dir * outerRadius));
        }

        if (sideRays)
        {
            Vector3 dir = RotateFlat(rotation, endAngle);
            line.SetPosition(index++, GroundPoint(SquareBoundaryPoint(center, rotation, innerHalf, dir)));
        }
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

    private static Vector3 SquareBoundaryPoint(Vector3 center, Quaternion rotation, float half, Vector3 worldDirection)
    {
        Vector3 local = Quaternion.Inverse(rotation) * worldDirection.normalized;
        float divisor = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z), 0.001f);
        return center + worldDirection.normalized * (half / divisor);
    }

    private static Vector3 GroundPoint(Vector3 p)
    {
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.43f;
        return p;
    }

    private static float GetSquareHalfSide(int strength)
    {
        int perSidePerRank = Mathf.CeilToInt(Mathf.Max(1, strength) / 8f);
        float sideLength = Mathf.Max(7.5f, (perSidePerRank - 1) * 0.75f);
        return Mathf.Clamp(sideLength * 0.5f, 4.0f, 10.5f);
    }

    private static Material MakeMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color };
        return material;
    }

    private static void KeepSquareOutline(Regiment unit)
    {
        GameObject root = unit != null ? GameObject.Find("Square29_" + unit.RegimentName) : null;
        if (root == null)
            return;
        Transform outline = root.transform.Find("SquareOutline");
        if (outline != null)
        {
            LineRenderer line = outline.GetComponent<LineRenderer>();
            if (line != null)
                line.enabled = unit.IsSelected;
        }
    }

    private static void SuppressLegacySquareRangeRings(Regiment unit)
    {
        GameObject root = unit != null ? GameObject.Find("Square29_" + unit.RegimentName) : null;
        if (root == null)
            return;
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

    private static void DisableLine(Transform t)
    {
        if (t == null)
            return;
        LineRenderer line = t.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = false;
    }

    private void SuppressMissionFootprint(Regiment unit)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || hierarchyVisualsField == null || unit == null)
            return;

        IDictionary visuals = hierarchyVisualsField.GetValue(hierarchy) as IDictionary;
        if (visuals == null || !visuals.Contains(unit))
            return;

        object visual = visuals[unit];
        if (visual == null)
            return;
        FieldInfo footprintField = visual.GetType().GetField("Footprint", AnyInstance);
        LineRenderer footprint = footprintField != null ? footprintField.GetValue(visual) as LineRenderer : null;
        if (footprint != null)
            footprint.enabled = false;
    }

    private void CleanupExited()
    {
        if (runtime.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, SquareRuntime> pair in runtime)
        {
            if (pair.Key != null && seen.Contains(pair.Key) && PrototypeInfantrySquare09F29.IsInSquare(pair.Key))
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }
        if (stale == null)
            return;

        foreach (Regiment unit in stale)
        {
            if (!runtime.TryGetValue(unit, out SquareRuntime state))
                continue;
            if (unit != null)
            {
                RestoreGlobalTimer(unit, state);
                unit.RefreshRangeVisibility();
            }
            if (state.VisualRoot != null)
                Destroy(state.VisualRoot);
            runtime.Remove(unit);
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Regiment, SquareRuntime> pair in runtime)
        {
            if (pair.Key != null)
            {
                RestoreGlobalTimer(pair.Key, pair.Value);
                pair.Key.RefreshRangeVisibility();
            }
            if (pair.Value != null && pair.Value.VisualRoot != null)
                Destroy(pair.Value.VisualRoot);
        }
        runtime.Clear();
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