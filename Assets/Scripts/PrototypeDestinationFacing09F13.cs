using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f13 destination-facing indicator.
// Draws a small yellow chevron on the destination footprint so the player can
// read which direction the formation will face when the route finishes.
[DefaultExecutionOrder(34500)]
public sealed class PrototypeDestinationFacing09F13 : MonoBehaviour
{
    private struct FormationBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
        public float Width => Mathf.Max(0.8f, MaxX - MinX + 0.55f);
        public float Depth => Mathf.Max(0.8f, MaxZ - MinZ + 0.55f);
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;
    }

    private readonly Dictionary<Regiment, LineRenderer> arrows =
        new Dictionary<Regiment, LineRenderer>();

    private FieldInfo routesField;
    private Material arrowMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeDestinationFacing09F13>() != null)
            return;

        GameObject root = new GameObject("PrototypeDestinationFacing_v000009f13");
        root.AddComponent<PrototypeDestinationFacing09F13>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        routesField = typeof(PlayerCommander).GetField("routes", flags);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader != null)
        {
            arrowMaterial = new Material(shader)
            {
                name = "DestinationFacing09F13",
                color = new Color(1.00f, 0.88f, 0.08f, 1.00f)
            };
        }

        Debug.Log("DEST-FACING-09F13|Installed=True|Shape=Chevron|Purpose=FinalFacingPreview");
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Regiment, LineRenderer> pair in arrows)
        {
            if (pair.Value != null)
                pair.Value.enabled = false;
        }

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
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f)
                facing = regiment.transform.forward;
            facing.y = 0f;
            facing.Normalize();

            RegimentFormation formation = finalFormationField != null
                ? (RegimentFormation)finalFormationField.GetValue(route)
                : RegimentFormation.Line;

            LineRenderer arrow = EnsureArrow(regiment);
            if (arrow == null)
                continue;

            FormationBounds bounds = CalculateBounds(formation, regiment.CurrentStrength);
            Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
            Vector3 center = pivot + right * bounds.CenterX + facing * bounds.CenterZ;
            Vector3 front = center + facing * (bounds.Depth * 0.5f + 0.55f);

            // Small ">"/chevron at the front edge of the destination rectangle.
            Vector3 leftWing = front - facing * 0.35f - right * 1.15f;
            Vector3 tip = front + facing * 2.15f;
            Vector3 rightWing = front - facing * 0.35f + right * 1.15f;

            arrow.SetPosition(0, GroundPoint(leftWing));
            arrow.SetPosition(1, GroundPoint(tip));
            arrow.SetPosition(2, GroundPoint(rightWing));
            arrow.enabled = true;
        }
    }

    private LineRenderer EnsureArrow(Regiment regiment)
    {
        if (arrows.TryGetValue(regiment, out LineRenderer existing) && existing != null)
            return existing;

        GameObject go = new GameObject(regiment.RegimentName + "_DestinationFacingChevron09F13");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.positionCount = 3;
        line.widthMultiplier = 0.22f;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        if (arrowMaterial != null)
            line.sharedMaterial = arrowMaterial;
        line.startColor = new Color(1.00f, 0.88f, 0.08f, 1.00f);
        line.endColor = line.startColor;
        line.enabled = false;

        arrows[regiment] = line;
        return line;
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

    private static Vector3 GroundPoint(Vector3 p)
    {
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
        return p;
    }
}
