using System.Collections.Generic;
using UnityEngine;

// v00.00.09f30i
// Authoritative cavalry selection footprint and persistent movement order visualization.
[DefaultExecutionOrder(43300)]
public sealed class PrototypeCavalryOrderVisuals09F30I : MonoBehaviour
{
    private sealed class State
    {
        public PrototypeCavalryUnit09F30 Unit;
        public LineRenderer Selection;
        public LineRenderer HorseSelection;
        public LineRenderer DismountLink;
        public LineRenderer Path;
        public LineRenderer Destination;
        public Vector2 SmoothedSize;
    }

    private readonly Dictionary<PrototypeCavalryUnit09F30, State> states =
        new Dictionary<PrototypeCavalryUnit09F30, State>();
    private float nextTry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryOrderVisuals09F30I>() == null)
            new GameObject("PrototypeCavalryOrderVisuals_v000009f30i")
                .AddComponent<PrototypeCavalryOrderVisuals09F30I>();
    }

    private void Update()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed)
            return;

        Ensure(cavalry.Gardehusar);
        Ensure(cavalry.Dragon);
        UpdateState(cavalry.Gardehusar);
        UpdateState(cavalry.Dragon);
    }

    private void Ensure(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || states.ContainsKey(unit))
            return;

        State s = new State
        {
            Unit = unit,
            Selection = CreateLine("Selection_" + unit.UnitName, 0.24f, new Color(1f, 0.82f, 0.10f)),
            HorseSelection = CreateLine("HorseSelection_" + unit.UnitName, 0.18f, new Color(1f, 0.82f, 0.10f)),
            DismountLink = CreateLine("DismountLink_" + unit.UnitName, 0.10f, new Color(1f, 0.82f, 0.10f)),
            Path = CreateLine("OrderPath_" + unit.UnitName, 0.13f, new Color(0.76f, 0.90f, 1f)),
            Destination = CreateLine("Destination_" + unit.UnitName, 0.22f, new Color(1f, 0.82f, 0.10f)),
            SmoothedSize = unit.GetCurrentFootprintSize()
        };
        states[unit] = s;
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

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name + "_Mat", color = color };
        line.sharedMaterial = material;
        return line;
    }

    private void UpdateState(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || !states.TryGetValue(unit, out State s))
            return;

        bool selected = unit.IsSelected;
        bool higherOrderVisible = IsVisibleFromHigherSelection(unit);
        bool showOrders = selected || higherOrderVisible;
        bool splitDismounted = selected &&
                               unit.Kind == PrototypeCavalryKind09F30.Dragon &&
                               unit.Mode == PrototypeCavalryMode09F30.Dismounted;

        s.Selection.enabled = selected;
        s.HorseSelection.enabled = splitDismounted;
        s.DismountLink.enabled = splitDismounted;
        s.Path.enabled = showOrders && unit.HasDestination;
        s.Destination.enabled = showOrders && unit.HasDestination;
        if (!selected && !showOrders)
            return;

        if (selected)
        {
            if (splitDismounted)
            {
                DrawBox(s.Selection, unit.GetDismountedCombatCenterWorld(), unit.transform.forward,
                    unit.GetDismountedCombatFootprintSize(), 0.34f);
                DrawBox(s.HorseSelection, unit.GetDismountedHorseHolderCenterWorld(), unit.transform.forward,
                    unit.GetDismountedHorseHolderFootprintSize(), 0.34f);
                DrawLink(s.DismountLink, unit.GetDismountedHorseHolderCenterWorld(),
                    unit.GetDismountedCombatCenterWorld(), 0.39f);
            }
            else
            {
                Vector2 targetSize = unit.GetCurrentFootprintSize();
                s.SmoothedSize = Vector2.Lerp(s.SmoothedSize, targetSize, 6f * Time.deltaTime);
                DrawBox(s.Selection, unit.GetCurrentFootprintCenterWorld(), unit.transform.forward, s.SmoothedSize, 0.34f);
            }
        }

        if (!unit.HasDestination)
            return;

        Vector3 steering = unit.CurrentSteeringTarget;
        Vector3 final = unit.FinalDestination;
        DrawPath(s.Path, unit.transform.position, steering, final);

        Vector3 facing = unit.PlannedDestinationFacing;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            facing = unit.transform.forward;
        facing.Normalize();
        DrawBox(s.Destination, unit.GetDestinationFootprintCenterWorld(facing),
            facing, unit.GetDestinationFootprintSize(), 0.42f);
    }

    private static void DrawPath(LineRenderer line, Vector3 start, Vector3 steering, Vector3 final)
    {
        bool intermediate = Vector3.Distance(steering, final) > 2f;
        line.positionCount = intermediate ? 3 : 2;
        line.SetPosition(0, Ground(start, 0.52f));
        if (intermediate)
        {
            line.SetPosition(1, Ground(steering, 0.52f));
            line.SetPosition(2, Ground(final, 0.52f));
        }
        else
        {
            line.SetPosition(1, Ground(final, 0.52f));
        }
    }

    private static bool IsVisibleFromHigherSelection(PrototypeCavalryUnit09F30 unit)
    {
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (unit == null || higher == null || !higher.Installed ||
            higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.None)
            return false;

        PrototypeCommandAttachment09F30B attachment =
            unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment == null)
            return false;

        string parent = attachment.CurrentCommandParent;
        if (higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Division)
            return true;

        return parent == PrototypeHigherCommandHQ09F30B.BrigadeId ||
               parent == PrototypeHigherCommandHQ09F30B.RegimentId ||
               parent == PrototypeCavalryCommandControl09F30C.MajorAId ||
               parent == PrototypeCavalryCommandControl09F30C.MajorBId;
    }

    private static void DrawLink(LineRenderer line, Vector3 a, Vector3 b, float y)
    {
        line.positionCount = 2;
        line.SetPosition(0, Ground(a, y));
        line.SetPosition(1, Ground(b, y));
    }

    private static void DrawBox(LineRenderer line, Vector3 center, Vector3 forward, Vector2 size, float y)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float hw = Mathf.Max(1f, size.x * 0.5f);
        float hd = Mathf.Max(1f, size.y * 0.5f);
        Vector3[] p =
        {
            center - right * hw - forward * hd,
            center + right * hw - forward * hd,
            center + right * hw + forward * hd,
            center - right * hw + forward * hd,
            center - right * hw - forward * hd
        };

        line.positionCount = 5;
        for (int i = 0; i < p.Length; i++)
            line.SetPosition(i, Ground(p[i], y));
    }

    private static Vector3 Ground(Vector3 p, float offset)
    {
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + offset;
        return p;
    }
}
