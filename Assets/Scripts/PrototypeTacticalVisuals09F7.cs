using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f7 tactical visual authority.
// Uses exact slot bounds (including the Column centre offset), compact 70-degree fire fans,
// terrain-following selection rectangles and destination footprints.
[DefaultExecutionOrder(34000)]
public sealed class PrototypeTacticalVisuals09F7 : MonoBehaviour
{
    private sealed class UnitVisual
    {
        public LineRenderer SelectionOutline;
        public GameObject SelectionOrb;
    }

    private struct FormationBounds
    {
        public float MinX;
        public float MaxX;
        public float MinZ;
        public float MaxZ;

        public float Width => Mathf.Max(0.8f, MaxX - MinX + 0.55f);
        public float Depth => Mathf.Max(0.8f, MaxZ - MinZ + 0.55f);
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;
    }

    private readonly Dictionary<Regiment, UnitVisual> visuals = new Dictionary<Regiment, UnitVisual>();

    private Material lineMaterial;
    private Material orbMaterial;

    private FieldInfo routesField;
    private FieldInfo formationPreviewField;
    private FieldInfo rightDragActiveField;
    private FieldInfo rightDragStartField;
    private FieldInfo rightDragEndField;
    private FieldInfo selectedField;

    private const int EdgeSamples = 10;
    private const int ArcSegments = 40;
    private const float GroundOffset = 0.40f;
    private const float SelectionMargin = 0.70f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalVisuals09F7>() != null)
            return;

        GameObject root = new GameObject("PrototypeTacticalVisuals_v000009f7");
        root.AddComponent<PrototypeTacticalVisuals09F7>();
    }

    private void Awake()
    {
        CreateMaterials();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        routesField = typeof(PlayerCommander).GetField("routes", flags);
        formationPreviewField = typeof(PlayerCommander).GetField("formationPreview", flags);
        rightDragActiveField = typeof(PlayerCommander).GetField("rightDragActive", flags);
        rightDragStartField = typeof(PlayerCommander).GetField("rightDragStart", flags);
        rightDragEndField = typeof(PlayerCommander).GetField("rightDragEnd", flags);
        selectedField = typeof(PlayerCommander).GetField("selected", flags);

        Debug.Log(
            "VISUAL-09F7-TACTICAL|Installed=True|Bounds=ExactFormationSlots|" +
            "ConeTotal=" + (PrototypeRangeTuning09F7.FireArcHalfAngleDegrees * 2f).ToString("0") +
            "deg|Destination=ExactFormationBounds");
    }

    private void LateUpdate()
    {
        SuppressOlderVisualControllers();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            FormationBounds bounds = CalculateBounds(regiment.Formation, regiment.CurrentStrength);
            UnitVisual visual = EnsureUnitVisual(regiment);
            UpdateSelection(regiment, visual, bounds);
            UpdateRangeFans(regiment, bounds);
        }

        UpdateDestinationGhosts();
        UpdateLiveDragPreview();
    }

    private static void SuppressOlderVisualControllers()
    {
        PrototypeTacticalVisuals09F6 oldF6 = UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalVisuals09F6>();
        if (oldF6 != null && oldF6.enabled)
            oldF6.enabled = false;

        PrototypeUnitFootprint09F5 oldF5 = UnityEngine.Object.FindAnyObjectByType<PrototypeUnitFootprint09F5>();
        if (oldF5 != null && oldF5.enabled)
            oldF5.enabled = false;
    }

    private void CreateMaterials()
    {
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
            lineShader = Shader.Find("Unlit/Color");
        if (lineShader == null)
            lineShader = Shader.Find("Standard");

        lineMaterial = new Material(lineShader)
        {
            name = "TacticalVisual09F7_Line",
            color = Color.white
        };

        Shader orbShader = Shader.Find("Unlit/Color");
        if (orbShader == null)
            orbShader = lineShader;

        orbMaterial = new Material(orbShader)
        {
            name = "TacticalVisual09F7_SelectionOrb",
            color = new Color(1.00f, 0.86f, 0.06f)
        };
    }

    private UnitVisual EnsureUnitVisual(Regiment regiment)
    {
        if (visuals.TryGetValue(regiment, out UnitVisual visual) && visual != null)
            return visual;

        GameObject outlineObject = new GameObject("SelectionOutline09F7");
        outlineObject.transform.SetParent(regiment.transform, false);
        LineRenderer outline = outlineObject.AddComponent<LineRenderer>();
        ConfigureLine(outline, 0.18f, new Color(1.00f, 0.82f, 0.05f, 0.98f));

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "SelectionOrb09F7";
        orb.transform.SetParent(regiment.transform, true);
        orb.transform.localScale = Vector3.one * 0.72f;

        Renderer renderer = orb.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = orbMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = orb.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);

        orb.SetActive(false);
        visual = new UnitVisual
        {
            SelectionOutline = outline,
            SelectionOrb = orb
        };
        visuals[regiment] = visual;
        return visual;
    }

    private void UpdateSelection(Regiment regiment, UnitVisual visual, FormationBounds bounds)
    {
        bool selected = regiment.IsSelected;
        Vector3 center = FormationCenterWorld(
            regiment.transform.position,
            regiment.transform.forward,
            bounds);

        if (visual.SelectionOutline != null)
        {
            visual.SelectionOutline.enabled = selected;
            if (selected)
            {
                DrawTerrainRectangle(
                    visual.SelectionOutline,
                    center,
                    regiment.transform.forward,
                    bounds.Width + SelectionMargin * 2f,
                    bounds.Depth + SelectionMargin * 2f,
                    GroundOffset);
            }
        }

        if (visual.SelectionOrb != null)
        {
            visual.SelectionOrb.SetActive(selected);
            if (selected)
            {
                float pulse = 0.68f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f;
                visual.SelectionOrb.transform.localScale = Vector3.one * pulse;
                visual.SelectionOrb.transform.position = center + Vector3.up * 3.15f;
            }
        }
    }

    private void UpdateRangeFans(Regiment regiment, FormationBounds bounds)
    {
        bool visible = regiment.IsSelected && regiment.ShowRange;

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("CloseRangeFan"),
            regiment.CloseRange,
            bounds,
            visible,
            0.15f,
            new Color(1.00f, 0.92f, 0.10f, 0.96f));

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("MediumRangeFan"),
            regiment.EffectiveRange,
            bounds,
            visible,
            0.18f,
            new Color(1.00f, 0.58f, 0.05f, 0.94f));

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("LongRangeFan"),
            regiment.MaximumRange,
            bounds,
            visible,
            0.22f,
            new Color(1.00f, 0.16f, 0.04f, 0.92f));
    }

    private void StyleAndBuildFan(
        Regiment regiment,
        Transform fanTransform,
        float range,
        FormationBounds bounds,
        bool visible,
        float width,
        Color color)
    {
        if (fanTransform == null)
            return;

        LineRenderer line = fanTransform.GetComponent<LineRenderer>();
        if (line == null)
            return;

        ConfigureLine(line, width, color);
        line.enabled = visible;
        if (!visible)
            return;

        Vector3 forward = PlanarNormalized(regiment.transform.forward, Vector3.forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 pivot = regiment.transform.position;
        Vector3 frontCenter =
            pivot + right * bounds.CenterX + forward * (bounds.MaxZ + 0.35f);
        float halfWidth = bounds.Width * 0.5f;
        Vector3 leftFront = frontCenter - right * halfWidth;
        Vector3 rightFront = frontCenter + right * halfWidth;

        List<Vector3> points = new List<Vector3>(ArcSegments + 4);
        points.Add(TerrainPoint(leftFront, GroundOffset));

        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = i / (float)ArcSegments;
            float angle = Mathf.Lerp(
                -PrototypeRangeTuning09F7.FireArcHalfAngleDegrees,
                PrototypeRangeTuning09F7.FireArcHalfAngleDegrees,
                t) * Mathf.Deg2Rad;
            Vector3 direction = forward * Mathf.Cos(angle) + right * Mathf.Sin(angle);
            points.Add(TerrainPoint(frontCenter + direction * range, GroundOffset));
        }

        points.Add(TerrainPoint(rightFront, GroundOffset));
        points.Add(TerrainPoint(leftFront, GroundOffset));
        SetPositions(line, points);
    }

    private void UpdateDestinationGhosts()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null || routesField == null)
            return;

        IDictionary routes = routesField.GetValue(commander) as IDictionary;
        if (routes == null)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (DictionaryEntry entry in routes)
        {
            Regiment regiment = entry.Key as Regiment;
            object route = entry.Value;
            if (regiment == null || route == null)
                continue;

            System.Type routeType = route.GetType();
            FieldInfo waypointsField = routeType.GetField("Waypoints", flags);
            FieldInfo finalFacingField = routeType.GetField("FinalFacing", flags);
            FieldInfo finalFormationField = routeType.GetField("FinalFormation", flags);

            IList waypoints = waypointsField != null ? waypointsField.GetValue(route) as IList : null;
            if (waypoints == null || waypoints.Count == 0)
                continue;

            Vector3 pivot = (Vector3)waypoints[waypoints.Count - 1];
            Vector3 facing = finalFacingField != null
                ? (Vector3)finalFacingField.GetValue(route)
                : regiment.transform.forward;
            RegimentFormation formation = finalFormationField != null
                ? (RegimentFormation)finalFormationField.GetValue(route)
                : RegimentFormation.Line;

            GameObject ghostRoot = GameObject.Find(regiment.RegimentName + "_OrderGhost");
            if (ghostRoot == null)
                continue;

            Transform destination = ghostRoot.transform.Find("Destination");
            if (destination == null)
                continue;

            LineRenderer line = destination.GetComponent<LineRenderer>();
            if (line == null)
                continue;

            FormationBounds bounds = CalculateBounds(formation, regiment.CurrentStrength);
            Vector3 center = FormationCenterWorld(pivot, facing, bounds);
            ConfigureLine(line, 0.24f, new Color(1.00f, 0.88f, 0.10f, 0.98f));
            DrawTerrainRectangle(
                line,
                center,
                facing,
                bounds.Width + SelectionMargin * 2f,
                bounds.Depth + SelectionMargin * 2f,
                GroundOffset + 0.04f);
        }
    }

    private void UpdateLiveDragPreview()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null ||
            formationPreviewField == null ||
            rightDragActiveField == null ||
            rightDragStartField == null ||
            rightDragEndField == null ||
            selectedField == null)
        {
            return;
        }

        bool active = (bool)rightDragActiveField.GetValue(commander);
        if (!active)
            return;

        IList selected = selectedField.GetValue(commander) as IList;
        if (selected == null || selected.Count != 1)
            return;

        Regiment regiment = selected[0] as Regiment;
        LineRenderer preview = formationPreviewField.GetValue(commander) as LineRenderer;
        if (regiment == null || preview == null)
            return;

        Vector3 pivot = (Vector3)rightDragStartField.GetValue(commander);
        Vector3 end = (Vector3)rightDragEndField.GetValue(commander);
        Vector3 drag = end - pivot;
        drag.y = 0f;

        Vector3 facing;
        if (drag.magnitude >= 4f)
        {
            facing = drag.normalized;
        }
        else
        {
            facing = pivot - regiment.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f)
                facing = regiment.transform.forward;
        }

        FormationBounds bounds = CalculateBounds(RegimentFormation.Line, regiment.CurrentStrength);
        Vector3 center = FormationCenterWorld(pivot, facing, bounds);
        ConfigureLine(preview, 0.30f, new Color(1.00f, 0.88f, 0.12f, 1f));
        DrawTerrainRectangle(
            preview,
            center,
            facing,
            bounds.Width + SelectionMargin * 2f,
            bounds.Depth + SelectionMargin * 2f,
            GroundOffset + 0.06f);
        preview.enabled = true;
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

    private static Vector3 FormationCenterWorld(Vector3 pivot, Vector3 facing, FormationBounds bounds)
    {
        Vector3 forward = PlanarNormalized(facing, Vector3.forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        return pivot + right * bounds.CenterX + forward * bounds.CenterZ;
    }

    private void ConfigureLine(LineRenderer line, float width, Color color)
    {
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = lineMaterial;
        line.startColor = color;
        line.endColor = color;
    }

    private static void DrawTerrainRectangle(
        LineRenderer line,
        Vector3 center,
        Vector3 facing,
        float width,
        float depth,
        float heightOffset)
    {
        if (line == null)
            return;

        Vector3 forward = PlanarNormalized(facing, Vector3.forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;

        Vector3[] corners =
        {
            center - right * halfWidth - forward * halfDepth,
            center + right * halfWidth - forward * halfDepth,
            center + right * halfWidth + forward * halfDepth,
            center - right * halfWidth + forward * halfDepth
        };

        int positions = EdgeSamples * 4 + 1;
        line.positionCount = positions;
        int output = 0;

        for (int edge = 0; edge < 4; edge++)
        {
            Vector3 a = corners[edge];
            Vector3 b = corners[(edge + 1) % 4];
            for (int i = 0; i < EdgeSamples; i++)
            {
                float t = i / (float)EdgeSamples;
                Vector3 p = Vector3.Lerp(a, b, t);
                line.SetPosition(output++, TerrainPoint(p, heightOffset));
            }
        }

        line.SetPosition(output, TerrainPoint(corners[0], heightOffset));
    }

    private static Vector3 TerrainPoint(Vector3 p, float offset)
    {
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + offset;
        return p;
    }

    private static void SetPositions(LineRenderer line, List<Vector3> points)
    {
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private static Vector3 PlanarNormalized(Vector3 vector, Vector3 fallback)
    {
        vector.y = 0f;
        if (vector.sqrMagnitude < 0.001f)
        {
            vector = fallback;
            vector.y = 0f;
        }
        if (vector.sqrMagnitude < 0.001f)
            vector = Vector3.forward;
        return vector.normalized;
    }
}
