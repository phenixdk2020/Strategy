using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f6 tactical visual correction.
// Owns selected-unit outline/orb, terrain-following fire fans and dynamic destination footprints.
// All dimensions are derived from the active 190-man company formation rather than old fixed regiment sizes.
[DefaultExecutionOrder(33000)]
public sealed class PrototypeTacticalVisuals09F6 : MonoBehaviour
{
    private sealed class UnitVisual
    {
        public LineRenderer SelectionOutline;
        public GameObject SelectionOrb;
    }

    private readonly Dictionary<Regiment, UnitVisual> visuals =
        new Dictionary<Regiment, UnitVisual>();

    private Material lineMaterial;
    private Material orbMaterial;

    private FieldInfo routesField;
    private FieldInfo formationPreviewField;
    private FieldInfo rightDragActiveField;
    private FieldInfo rightDragStartField;
    private FieldInfo rightDragEndField;
    private FieldInfo selectedField;

    private const int EdgeSamples = 8;
    private const int ArcSegments = 48;
    private const float GroundOffset = 0.42f;
    private const float SelectionMargin = 0.65f;
    private const float FireArcHalfAngle = 60f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalVisuals09F6>() != null)
            return;

        GameObject root = new GameObject("PrototypeTacticalVisuals_v000009f6");
        root.AddComponent<PrototypeTacticalVisuals09F6>();
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
            "VISUAL-09F6|Installed=True|SelectionBox=TerrainFollowing|SelectionOrb=True|" +
            "RangeFans=FormationWidth|DestinationFootprint=FormationSize");
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

            UnitVisual visual = EnsureUnitVisual(regiment);
            Vector2 footprint = GetFootprint(regiment, regiment.Formation);

            UpdateSelection(regiment, visual, footprint);
            UpdateRangeFans(regiment, footprint);
            DisableOldSelectionOutline(regiment);
        }

        UpdateDestinationGhosts();
        UpdateLiveDragPreview();
    }

    private static void SuppressOlderVisualControllers()
    {
        PrototypeUnitFootprint09F5 oldFootprint =
            UnityEngine.Object.FindAnyObjectByType<PrototypeUnitFootprint09F5>();
        if (oldFootprint != null && oldFootprint.enabled)
            oldFootprint.enabled = false;

        PrototypeTacticalVisualCleanup09F oldCleanup =
            UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalVisualCleanup09F>();
        if (oldCleanup != null && oldCleanup.enabled)
            oldCleanup.enabled = false;
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
            name = "TacticalVisual09F6_Line",
            color = Color.white
        };

        Shader orbShader = Shader.Find("Unlit/Color");
        if (orbShader == null)
            orbShader = lineShader;

        orbMaterial = new Material(orbShader)
        {
            name = "TacticalVisual09F6_SelectionOrb",
            color = new Color(1.00f, 0.86f, 0.06f)
        };
    }

    private UnitVisual EnsureUnitVisual(Regiment regiment)
    {
        if (visuals.TryGetValue(regiment, out UnitVisual visual) && visual != null)
            return visual;

        GameObject outlineObject = new GameObject("SelectionOutline09F6");
        outlineObject.transform.SetParent(regiment.transform, false);
        LineRenderer outline = outlineObject.AddComponent<LineRenderer>();
        ConfigureLine(outline, 0.18f, new Color(1.00f, 0.82f, 0.05f, 0.98f));

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "SelectionOrb09F6";
        orb.transform.SetParent(regiment.transform, false);
        orb.transform.localPosition = new Vector3(0f, 3.15f, 0f);
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

    private void UpdateSelection(Regiment regiment, UnitVisual visual, Vector2 footprint)
    {
        bool selected = regiment.IsSelected;
        if (visual.SelectionOutline != null)
        {
            visual.SelectionOutline.enabled = selected;
            if (selected)
            {
                DrawTerrainRectangle(
                    visual.SelectionOutline,
                    regiment.transform.position,
                    regiment.transform.forward,
                    footprint.x + SelectionMargin * 2f,
                    footprint.y + SelectionMargin * 2f,
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
            }
        }
    }

    private static void DisableOldSelectionOutline(Regiment regiment)
    {
        Transform old = regiment.transform.Find("SelectionFootprint09F5");
        if (old == null)
            return;

        LineRenderer line = old.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = false;
    }

    private void UpdateRangeFans(Regiment regiment, Vector2 footprint)
    {
        bool visible = regiment.IsSelected && regiment.ShowRange;
        float halfWidth = Mathf.Max(0.8f, footprint.x * 0.5f);
        float frontOffset = footprint.y * 0.5f + 0.30f;

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("CloseRangeFan"),
            regiment.CloseRange,
            halfWidth,
            frontOffset,
            visible,
            0.16f,
            new Color(1.00f, 0.92f, 0.10f, 0.95f));

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("MediumRangeFan"),
            regiment.EffectiveRange,
            halfWidth,
            frontOffset,
            visible,
            0.20f,
            new Color(1.00f, 0.58f, 0.05f, 0.92f));

        StyleAndBuildFan(
            regiment,
            regiment.transform.Find("LongRangeFan"),
            regiment.MaximumRange,
            halfWidth,
            frontOffset,
            visible,
            0.24f,
            new Color(1.00f, 0.18f, 0.04f, 0.88f));
    }

    private void StyleAndBuildFan(
        Regiment regiment,
        Transform fanTransform,
        float range,
        float halfWidth,
        float frontOffset,
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
        Vector3 right = PlanarNormalized(regiment.transform.right, Vector3.right);
        Vector3 frontCenter = regiment.transform.position + forward * frontOffset;
        Vector3 leftFront = frontCenter - right * halfWidth;
        Vector3 rightFront = frontCenter + right * halfWidth;

        List<Vector3> points = new List<Vector3>(ArcSegments + 4);
        points.Add(TerrainPoint(leftFront, GroundOffset));

        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = i / (float)ArcSegments;
            float angle = Mathf.Lerp(-FireArcHalfAngle, FireArcHalfAngle, t) * Mathf.Deg2Rad;
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

        foreach (DictionaryEntry entry in routes)
        {
            Regiment regiment = entry.Key as Regiment;
            object route = entry.Value;
            if (regiment == null || route == null)
                continue;

            TypeInfoCache cache = TypeInfoCache.For(route.GetType());
            IList waypoints = cache.WaypointsField != null
                ? cache.WaypointsField.GetValue(route) as IList
                : null;
            if (waypoints == null || waypoints.Count == 0)
                continue;

            Vector3 center = (Vector3)waypoints[waypoints.Count - 1];
            Vector3 facing = cache.FinalFacingField != null
                ? (Vector3)cache.FinalFacingField.GetValue(route)
                : regiment.transform.forward;
            RegimentFormation formation = cache.FinalFormationField != null
                ? (RegimentFormation)cache.FinalFormationField.GetValue(route)
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

            Vector2 footprint = GetFootprint(regiment, formation);
            ConfigureLine(line, 0.24f, new Color(1.00f, 0.88f, 0.10f, 0.98f));
            DrawTerrainRectangle(
                line,
                center,
                facing,
                footprint.x + SelectionMargin * 2f,
                footprint.y + SelectionMargin * 2f,
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

        Vector3 center = (Vector3)rightDragStartField.GetValue(commander);
        Vector3 end = (Vector3)rightDragEndField.GetValue(commander);
        Vector3 drag = end - center;
        drag.y = 0f;

        Vector3 facing;
        if (drag.magnitude >= 4f)
        {
            facing = drag.normalized;
        }
        else
        {
            facing = center - regiment.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f)
                facing = regiment.transform.forward;
        }

        Vector2 footprint = GetFootprint(regiment, RegimentFormation.Line);
        ConfigureLine(preview, 0.30f, new Color(1.00f, 0.88f, 0.12f, 1f));
        DrawTerrainRectangle(
            preview,
            center,
            facing,
            footprint.x + SelectionMargin * 2f,
            footprint.y + SelectionMargin * 2f,
            GroundOffset + 0.06f);
        preview.enabled = true;
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

    private static Vector2 GetFootprint(Regiment regiment, RegimentFormation formation)
    {
        int strength = Mathf.Max(1, regiment != null ? regiment.CurrentStrength : 1);

        if (formation == RegimentFormation.Line)
        {
            int columns = Mathf.CeilToInt(strength / (float)PrototypeBattleVisuals09F5.LineRanks);
            float width = Mathf.Max(
                0.8f,
                (columns - 1) * PrototypeBattleVisuals09F5.LineSpacingX + 0.55f);
            float depth =
                (PrototypeBattleVisuals09F5.LineRanks - 1) * PrototypeBattleVisuals09F5.RankSpacingZ + 0.80f;
            return new Vector2(width, depth);
        }

        int rows = Mathf.CeilToInt(strength / (float)PrototypeBattleVisuals09F5.ColumnWidth);
        float columnWidth =
            (PrototypeBattleVisuals09F5.ColumnWidth - 1) * PrototypeBattleVisuals09F5.ColumnSpacingX + 0.55f;
        float columnDepth = Mathf.Max(
            1f,
            (rows - 1) * PrototypeBattleVisuals09F5.ColumnSpacingZ + 0.80f);
        return new Vector2(columnWidth, columnDepth);
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

        List<Vector3> points = new List<Vector3>(EdgeSamples * 4 + 1);
        for (int edge = 0; edge < 4; edge++)
        {
            Vector3 a = corners[edge];
            Vector3 b = corners[(edge + 1) % 4];
            for (int i = 0; i < EdgeSamples; i++)
            {
                float t = i / (float)EdgeSamples;
                points.Add(TerrainPoint(Vector3.Lerp(a, b, t), heightOffset));
            }
        }
        points.Add(TerrainPoint(corners[0], heightOffset));
        SetPositions(line, points);
    }

    private static Vector3 TerrainPoint(Vector3 point, float offset)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + offset;
        return point;
    }

    private static Vector3 PlanarNormalized(Vector3 value, Vector3 fallback)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.001f)
        {
            value = fallback;
            value.y = 0f;
        }
        return value.sqrMagnitude < 0.001f ? Vector3.forward : value.normalized;
    }

    private static void SetPositions(LineRenderer line, List<Vector3> points)
    {
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private sealed class TypeInfoCache
    {
        private static readonly Dictionary<System.Type, TypeInfoCache> Cache =
            new Dictionary<System.Type, TypeInfoCache>();

        public FieldInfo WaypointsField;
        public FieldInfo FinalFacingField;
        public FieldInfo FinalFormationField;

        public static TypeInfoCache For(System.Type type)
        {
            if (type == null)
                return new TypeInfoCache();

            if (Cache.TryGetValue(type, out TypeInfoCache value))
                return value;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            value = new TypeInfoCache
            {
                WaypointsField = type.GetField("Waypoints", flags),
                FinalFacingField = type.GetField("FinalFacing", flags),
                FinalFormationField = type.GetField("FinalFormation", flags)
            };
            Cache[type] = value;
            return value;
        }
    }
}
