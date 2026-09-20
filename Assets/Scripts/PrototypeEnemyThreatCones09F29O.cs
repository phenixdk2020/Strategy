using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f29o
// Local QA/readability overlay: when a Danish company is selected, nearby Prussian
// companies show their actual fire-ready Close/Medium/Long sectors even though the
// enemy itself is not selected. This is visual-only and runs after the normal F8 cone
// authority so it can temporarily expose enemy threat geometry without changing combat.
[DefaultExecutionOrder(49950)]
public sealed class PrototypeEnemyThreatCones09F29O : MonoBehaviour
{
    private struct FormationBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
        public float Width => Mathf.Max(0.8f, MaxX - MinX + 0.55f);
        public float CenterX => (MinX + MaxX) * 0.5f;
    }

    private const float PreviewDistance = 180f;
    private const int ArcSegments = 40;
    private const float GroundOffset = 0.43f;

    private Material lineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeEnemyThreatCones09F29O>() != null)
            return;
        new GameObject("PrototypeEnemyThreatCones_v000009f29o")
            .AddComponent<PrototypeEnemyThreatCones09F29O>();
    }

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        lineMaterial = new Material(shader)
        {
            name = "EnemyThreatCone09F29O",
            color = Color.white
        };

        Debug.Log("ENEMY-CONE-09F30W|Installed=True|TestAlwaysVisible=True|SelectionRequired=False|DistanceGate=False|FormationReadyGate=False|SimulationChanged=False");
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment enemy in battle.Regiments)
        {
            if (enemy == null || enemy.Team != BattleTeam.Prussia)
                continue;

            // F30W TEST/QA authority:
            // Every living Prussian infantry unit shows its complete fire geometry
            // at all times. No Danish selection, preview-distance, formation-ready
            // or LOS gate may hide the QA cone. This is visual-only and does not
            // grant targeting/firing authority.
            bool shouldShow =
                enemy.CurrentStrength > 0 &&
                !enemy.IsRouted;

            ApplyEnemyFans(enemy, shouldShow);
        }
    }

    private List<Regiment> GetSelectedDanishCompanies(BattleManager battle)
    {
        List<Regiment> result = new List<Regiment>();
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit != null && unit.Team == BattleTeam.Denmark && unit.IsSelected &&
                unit.ShowRange && !unit.IsRouted)
                result.Add(unit);
        }
        return result;
    }

    private static bool IsNearAnySelected(Regiment enemy, List<Regiment> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment friendly = selected[i];
            if (friendly != null && PlanarDistance(friendly.transform.position, enemy.transform.position) <= PreviewDistance)
                return true;
        }
        return false;
    }

    private void ApplyEnemyFans(Regiment enemy, bool visible)
    {
        if (enemy == null)
            return;

        FormationBounds bounds = CalculateBounds(enemy.Formation, enemy.CurrentStrength);

        BuildFan(
            enemy,
            enemy.transform.Find("CloseRangeFan"),
            enemy.CloseRange,
            bounds,
            visible,
            enemy.FirePolicy == RegimentFirePolicy.CloseRange,
            0.11f,
            new Color(1.00f, 0.36f, 0.34f, 1.00f));

        BuildFan(
            enemy,
            enemy.transform.Find("MediumRangeFan"),
            enemy.EffectiveRange,
            bounds,
            visible,
            enemy.FirePolicy == RegimentFirePolicy.MediumRange,
            0.14f,
            new Color(1.00f, 0.18f, 0.10f, 1.00f));

        BuildFan(
            enemy,
            enemy.transform.Find("LongRangeFan"),
            enemy.MaximumRange,
            bounds,
            visible,
            enemy.FirePolicy == RegimentFirePolicy.LongRange,
            0.17f,
            new Color(0.86f, 0.04f, 0.04f, 1.00f));
    }

    private void BuildFan(
        Regiment regiment,
        Transform fanTransform,
        float range,
        FormationBounds bounds,
        bool visible,
        bool activeRange,
        float width,
        Color color)
    {
        if (fanTransform == null)
            return;

        LineRenderer line = fanTransform.GetComponent<LineRenderer>();
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.loop = false;

        // Match the Danish/Dragon QA language: selected fire-policy band is
        // strong, the other physical ranges remain faint references.
        color.a = activeRange ? 0.98f : 0.18f;
        line.widthMultiplier =
            width * (activeRange ? 1.55f : 0.55f);
        line.numCapVertices = activeRange ? 3 : 1;
        line.numCornerVertices = activeRange ? 3 : 1;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = lineMaterial;
        line.startColor = color;
        line.endColor = color;
        line.enabled = visible;

        if (!visible)
            return;

        Vector3 forward = Flat(regiment.transform.forward);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 pivot = regiment.transform.position;
        Vector3 frontCenter = pivot + right * bounds.CenterX + forward * (bounds.MaxZ + 0.35f);
        float halfWidth = bounds.Width * 0.5f;
        Vector3 leftFront = frontCenter - right * halfWidth;
        Vector3 rightFront = frontCenter + right * halfWidth;

        List<Vector3> points = new List<Vector3>(ArcSegments + 4);
        points.Add(TerrainPoint(leftFront));

        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = i / (float)ArcSegments;
            Vector3 muzzle = Vector3.Lerp(leftFront, rightFront, t);
            float angle = Mathf.Lerp(
                -PrototypeRangeTuning09F8.FireArcHalfAngleDegrees,
                PrototypeRangeTuning09F8.FireArcHalfAngleDegrees,
                t) * Mathf.Deg2Rad;
            Vector3 direction = forward * Mathf.Cos(angle) + right * Mathf.Sin(angle);
            points.Add(TerrainPoint(muzzle + direction * range));
        }

        points.Add(TerrainPoint(rightFront));
        points.Add(TerrainPoint(leftFront));

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private static FormationBounds CalculateBounds(RegimentFormation formation, int strength)
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

    private static Vector3 TerrainPoint(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + GroundOffset;
        return point;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude < 0.0001f ? Vector3.zero : value.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
