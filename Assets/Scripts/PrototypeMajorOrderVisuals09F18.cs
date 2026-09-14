using System.Collections.Generic;
using UnityEngine;

// v00.00.09f24: command links + Major destination/path visualization.
// Major-selected view still shows the complete battalion plan, while selecting one or
// more subordinate companies now keeps each selected company's current Major mission
// path and destination footprint visible as well.
[DefaultExecutionOrder(430)]
public sealed class PrototypeMajorOrderVisuals09F18 : MonoBehaviour
{
    private sealed class Visual
    {
        public GameObject Root;
        public LineRenderer Path;
        public LineRenderer Footprint;
        public Vector3 Goal;
        public Vector3 Facing;
        public MajorOrder09F18 Order;
        public bool Arrived;
    }

    public static PrototypeMajorOrderVisuals09F18 Instance { get; private set; }

    private readonly Dictionary<Regiment, LineRenderer> links = new Dictionary<Regiment, LineRenderer>();
    private readonly Dictionary<Regiment, Visual> missions = new Dictionary<Regiment, Visual>();
    private Material linkMaterial;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorOrderVisuals09F18>() == null)
            new GameObject("PrototypeMajorOrderVisuals_v000009f24").AddComponent<PrototypeMajorOrderVisuals09F18>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return;

        if (!built || links.Count != major.Companies.Count)
            RebuildLinks(major);

        UpdateLinks(major);
        UpdateMissionVisuals(major.Selected);
    }

    private void RebuildLinks(PrototypeMajorBattalion09F18 major)
    {
        foreach (LineRenderer line in links.Values)
            if (line != null)
                Destroy(line.gameObject);
        links.Clear();

        if (linkMaterial == null)
            linkMaterial = CreateMaterial(new Color(0.82f, 0.69f, 0.29f, 0.88f), "HQ18CommandLink");

        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null)
                continue;
            GameObject root = new GameObject("Major18Link_" + regiment.RegimentName);
            root.transform.SetParent(transform, false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 20;
            line.widthMultiplier = 0.14f;
            line.numCapVertices = 2;
            line.sharedMaterial = linkMaterial;
            links[regiment] = line;
        }

        built = true;
    }

    private void UpdateLinks(PrototypeMajorBattalion09F18 major)
    {
        if (major.HqRoot == null)
            return;

        foreach (KeyValuePair<Regiment, LineRenderer> pair in links)
        {
            Regiment regiment = pair.Key;
            LineRenderer line = pair.Value;
            if (line == null)
                continue;

            line.enabled = major.Selected && regiment != null;
            if (!line.enabled)
                continue;

            Vector3 a = major.HqRoot.transform.position;
            Vector3 b = regiment.transform.position;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.50f;
                line.SetPosition(i, p);
            }
        }
    }

    public void SetMission(
        Regiment regiment,
        Vector3 goal,
        Vector3 facing,
        MajorOrder09F18 order,
        bool reserve,
        bool flank)
    {
        if (regiment == null)
            return;

        if (!missions.TryGetValue(regiment, out Visual visual))
        {
            GameObject root = new GameObject("Major18Order_" + regiment.RegimentName);
            root.transform.SetParent(transform, false);
            visual = new Visual
            {
                Root = root,
                Path = CreateLine(root.transform, "Path", 0.12f, ColorFor(order)),
                Footprint = CreateLine(root.transform, "Destination", 0.23f, ColorFor(order))
            };
            missions[regiment] = visual;
        }

        visual.Goal = goal;
        visual.Facing = facing;
        visual.Order = order;
        visual.Arrived = false;
        SetColor(visual.Path, ColorFor(order));
        SetColor(visual.Footprint, ColorFor(order));
        DrawFootprint(visual.Footprint, goal, facing);
    }

    public void MarkArrived(Regiment regiment)
    {
        if (regiment == null || !missions.TryGetValue(regiment, out Visual visual))
            return;
        visual.Arrived = true;
        if (visual.Path != null)
            visual.Path.positionCount = 0;
    }

    public void ClearMission(Regiment regiment)
    {
        if (regiment == null || !missions.TryGetValue(regiment, out Visual visual))
            return;
        if (visual.Root != null)
            Destroy(visual.Root);
        missions.Remove(regiment);
    }

    public void ClearAllMissions()
    {
        foreach (Visual visual in missions.Values)
            if (visual != null && visual.Root != null)
                Destroy(visual.Root);
        missions.Clear();
    }

    private void UpdateMissionVisuals(bool majorSelected)
    {
        foreach (KeyValuePair<Regiment, Visual> pair in missions)
        {
            Regiment regiment = pair.Key;
            Visual visual = pair.Value;
            if (regiment == null || visual == null)
                continue;

            // f24: a selected subordinate must be able to inspect its own current
            // destination even after selection authority has moved away from the Major.
            bool show = majorSelected || regiment.IsSelected;

            if (visual.Footprint != null)
                visual.Footprint.enabled = show;
            if (visual.Path != null)
                visual.Path.enabled = show && !visual.Arrived;

            if (!show)
                continue;

            if (!visual.Arrived && visual.Path != null)
            {
                visual.Path.positionCount = 2;
                Vector3 a = regiment.transform.position;
                Vector3 b = visual.Goal;
                a.y = PrototypeBootstrap.SampleGroundHeight(a.x, a.z) + 0.46f;
                b.y = PrototypeBootstrap.SampleGroundHeight(b.x, b.z) + 0.46f;
                visual.Path.SetPosition(0, a);
                visual.Path.SetPosition(1, b);
            }

            if (visual.Footprint != null)
                DrawFootprint(visual.Footprint, visual.Goal, visual.Facing);
        }
    }

    private static LineRenderer CreateLine(Transform parent, string name, float width, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sharedMaterial = CreateMaterial(color, "HQ18_" + name);
        return line;
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static void SetColor(LineRenderer line, Color color)
    {
        if (line != null && line.sharedMaterial != null)
            line.sharedMaterial.color = color;
    }

    private static Color ColorFor(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder09F18.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder09F18.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    private static void DrawFootprint(LineRenderer line, Vector3 center, Vector3 facing)
    {
        if (line == null)
            return;

        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;
        facing.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
        const float halfWidth = 24f;
        const float halfDepth = 3.2f;

        Vector3[] points =
        {
            center - right * halfWidth - facing * halfDepth,
            center + right * halfWidth - facing * halfDepth,
            center + right * halfWidth + facing * halfDepth,
            center - right * halfWidth + facing * halfDepth,
            center - right * halfWidth - facing * halfDepth
        };

        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 p = points[i];
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
            line.SetPosition(i, p);
        }
    }
}
