using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09m11 - frontage cone + bridge ghost. No extra company APIs.
[DefaultExecutionOrder(12100)]
public sealed class PrototypeKampTacticalFeedback09M3 : MonoBehaviour
{
    public const float RiverHalfWidth = 2.10f;
    public const float BridgeZ = 22.0f;
    public const float BridgeHalfLengthX = 10.0f;
    public const float BridgeHalfWidthZ = 5.5f;
    public const float BridgeApproachOffset = 14.0f;

    private readonly Dictionary<Regiment, LineRenderer> longFans = new Dictionary<Regiment, LineRenderer>();
    private readonly Dictionary<Regiment, LineRenderer> ghostBox = new Dictionary<Regiment, LineRenderer>();
    private readonly Dictionary<Regiment, LineRenderer> ghostPath = new Dictionary<Regiment, LineRenderer>();
    private Transform visualRoot;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampTacticalFeedback09M3>() != null)
            return;
        new GameObject("PrototypeKampTacticalFeedback_v000009m11").AddComponent<PrototypeKampTacticalFeedback09M3>();
    }

    private void Awake()
    {
        visualRoot = new GameObject("KampTacticalFeedbackRoot09M11").transform;
        visualRoot.SetParent(transform, false);
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;
        if (cam == null) cam = Camera.main;

        HashSet<Regiment> selected = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> picked = control.SelectedCompanies;
        for (int i = 0; i < picked.Count; i++)
        {
            if (picked[i] != null && picked[i].ParentRegiment != null)
                selected.Add(picked[i].ParentRegiment);
        }

        Vector3 preview = Vector3.zero;
        bool hasPreview = picked.Count > 0 && Input.GetMouseButton(1) && TryPick(out preview);

        foreach (Regiment regiment in selected)
        {
            Vector3 origin;
            Vector3 forward;
            if (!TryFront(regiment, picked, out origin, out forward))
                continue;

            LineRenderer fan = GetLine(longFans, regiment, "Fan", 0.20f, new Color(1f, 0.25f, 0.05f, 0.9f));
            DrawArc(fan, origin, forward, regiment.MaximumRange, regiment.FireArcHalfAngle);

            if (!hasPreview)
            {
                Hide(ghostBox, regiment);
                Hide(ghostPath, regiment);
                continue;
            }

            Vector3 from = Vector3.zero;
            int n = 0;
            for (int i = 0; i < picked.Count; i++)
            {
                PrototypeCompanyTacticalEntity09L2 c = picked[i];
                if (c == null || c.ParentRegiment != regiment) continue;
                from += c.transform.position;
                n++;
            }
            if (n > 0) from /= n;

            float halfW = 10f;
            float halfD = 4f;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            LineRenderer box = GetLine(ghostBox, regiment, "GhostBox", 0.12f, new Color(0.78f, 0.92f, 1f, 0.7f));
            box.loop = true;
            box.positionCount = 4;
            box.SetPosition(0, Ground(preview - right * halfW - forward * halfD));
            box.SetPosition(1, Ground(preview - right * halfW + forward * halfD));
            box.SetPosition(2, Ground(preview + right * halfW + forward * halfD));
            box.SetPosition(3, Ground(preview + right * halfW - forward * halfD));
            box.enabled = true;

            List<Vector3> path = new List<Vector3>();
            BuildPath(from, preview, path);
            LineRenderer line = GetLine(ghostPath, regiment, "GhostPath", 0.10f, new Color(0.70f, 0.86f, 1f, 0.8f));
            line.loop = false;
            line.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
                line.SetPosition(i, Ground(path[i]));
            line.enabled = path.Count >= 2;
        }
    }

    private static bool TryFront(Regiment regiment, IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected, out Vector3 origin, out Vector3 forward)
    {
        origin = Vector3.zero;
        forward = Vector3.zero;
        float best = float.NegativeInfinity;
        Vector3 front = Vector3.zero;
        int count = 0;
        for (int i = 0; i < selected.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 c = selected[i];
            if (c == null || c.ParentRegiment != regiment || c.CurrentStrength <= 0)
                continue;
            Vector3 f = c.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) forward += f.normalized;
            float proj = Vector3.Dot(c.transform.position, f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward);
            if (proj > best) { best = proj; front = c.transform.position; }
            count++;
        }
        if (count <= 0) return false;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        origin = front + forward * 1.15f;
        origin.y = PrototypeBootstrap.SampleGroundHeight(origin.x, origin.z) + 0.16f;
        return true;
    }

    public static void BuildPath(Vector3 from, Vector3 to, List<Vector3> buffer)
    {
        buffer.Clear();
        from.y = 0f;
        to = SnapOutOfRiver(to);
        to.y = 0f;
        buffer.Add(from);
        if (!SegmentCrossesRiver(from, to))
        {
            buffer.Add(to);
            return;
        }
        float bridgeX = StreamCenterX(BridgeZ);
        float fromSide = Mathf.Sign(from.x - StreamCenterX(from.z));
        float toSide = Mathf.Sign(to.x - StreamCenterX(to.z));
        if (Mathf.Abs(fromSide) < 0.1f) fromSide = -1f;
        if (Mathf.Abs(toSide) < 0.1f) toSide = -fromSide;
        buffer.Add(new Vector3(bridgeX + fromSide * BridgeApproachOffset, 0f, BridgeZ));
        buffer.Add(new Vector3(bridgeX, 0f, BridgeZ));
        buffer.Add(new Vector3(bridgeX + toSide * BridgeApproachOffset, 0f, BridgeZ));
        buffer.Add(to);
    }

    public static Vector3 SnapOutOfRiver(Vector3 point)
    {
        if (IsBridgeZone(point) || !IsRiverWater(point)) return point;
        float center = StreamCenterX(point.z);
        float side = point.x >= center ? 1f : -1f;
        point.x = center + side * (RiverHalfWidth + 3.2f);
        return point;
    }

    public static bool SegmentCrossesRiver(Vector3 a, Vector3 b)
    {
        for (int i = 1; i < 48; i++)
        {
            Vector3 point = Vector3.Lerp(a, b, i / 48f);
            if (IsRiverWater(point) && !IsBridgeZone(point)) return true;
        }
        return false;
    }

    public static bool IsRiverWater(Vector3 point)
    {
        return Mathf.Abs(point.x - StreamCenterX(point.z)) <= RiverHalfWidth + 1.15f;
    }

    public static bool IsBridgeZone(Vector3 point)
    {
        float bridgeX = StreamCenterX(BridgeZ);
        return Mathf.Abs(point.z - BridgeZ) <= BridgeHalfWidthZ + 1.2f &&
               Mathf.Abs(point.x - bridgeX) <= BridgeHalfLengthX + 1.5f;
    }

    public static float StreamCenterX(float z)
    {
        return Mathf.Sin(z * 0.065f) * 4.8f;
    }

    private LineRenderer GetLine(Dictionary<Regiment, LineRenderer> map, Regiment regiment, string tag, float width, Color color)
    {
        LineRenderer line;
        if (map.TryGetValue(regiment, out line) && line != null) return line;
        GameObject go = new GameObject(tag + "_" + regiment.RegimentName);
        go.transform.SetParent(visualRoot, false);
        line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.startColor = color;
        line.endColor = color;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) line.sharedMaterial = new Material(shader) { color = color };
        map[regiment] = line;
        return line;
    }

    private static void Hide(Dictionary<Regiment, LineRenderer> map, Regiment regiment)
    {
        LineRenderer line;
        if (map.TryGetValue(regiment, out line) && line != null) line.enabled = false;
    }

    private static void DrawArc(LineRenderer line, Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        const int segs = 40;
        line.loop = false;
        line.positionCount = segs + 1;
        for (int i = 0; i <= segs; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segs);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            line.SetPosition(i, Ground(origin + dir * range));
        }
        line.enabled = true;
    }

    private bool TryPick(out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float enter)) return false;
        point = SnapOutOfRiver(ray.GetPoint(enter));
        return true;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.16f;
        return point;
    }
}
