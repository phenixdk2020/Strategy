using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09m3 - regiment-front range fans, river-only-via-bridge for detached
// companies, and a ghost destination box + path line for selected orders.
[DefaultExecutionOrder(12100)]
public sealed class PrototypeKampTacticalFeedback09M3 : MonoBehaviour
{
    public const float RiverHalfWidth = 2.10f;
    public const float BridgeZ = 22.0f;
    public const float BridgeHalfLengthX = 8.0f;
    public const float BridgeHalfWidthZ = 4.0f;
    public const float BridgeApproachOffset = 12.0f;

    private sealed class RegimentFan
    {
        public LineRenderer Close;
        public LineRenderer Medium;
        public LineRenderer Long;
    }

    private sealed class GhostSet
    {
        public LineRenderer Box;
        public LineRenderer Path;
    }

    private readonly Dictionary<Regiment, RegimentFan> fans = new Dictionary<Regiment, RegimentFan>();
    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, GhostSet> ghosts =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, GhostSet>();
    private readonly List<Vector3> pathScratch = new List<Vector3>(8);
    private readonly List<Vector3> boxScratch = new List<Vector3>(8);
    private Transform visualRoot;
    private bool announced;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeKampTacticalFeedback09M3>() != null)
            return;

        GameObject root = new GameObject("PrototypeKampTacticalFeedback_v000009m3");
        root.AddComponent<PrototypeKampTacticalFeedback09M3>();
    }

    private void Awake()
    {
        visualRoot = new GameObject("KampTacticalFeedbackRoot09M3").transform;
        visualRoot.SetParent(transform, false);
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        if (cam == null)
            cam = Camera.main;

        HideOfficerAndPivotFans();
        RerouteCompaniesAcrossRiver(control);
        UpdateRegimentRangeFans(control);
        UpdateGhostOrders(control);

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "KAMP-FEEDBACK-09M3|Installed=True|RangeCone=SelectedRegimentFrontage|" +
                "River=BridgeOnly|Ghost=DestinationBox+Path");
        }
    }

    private void HideOfficerAndPivotFans()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            for (int i = 0; i < battle.Regiments.Count; i++)
            {
                Regiment regiment = battle.Regiments[i];
                if (regiment == null)
                    continue;

                if (regiment.ShowRange)
                {
                    regiment.ShowRange = false;
                    regiment.RefreshRangeVisibility();
                }

                DisableNamedFans(regiment.transform);
            }
        }

        PrototypeKampCommandQa09M1 qa = UnityEngine.Object.FindAnyObjectByType<PrototypeKampCommandQa09M1>();
        if (qa == null)
            return;

        Transform[] children = qa.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
                continue;
            if (child.name != "CloseFan" && child.name != "MediumFan" && child.name != "LongFan")
                continue;
            LineRenderer line = child.GetComponent<LineRenderer>();
            if (line != null && line.enabled)
                line.enabled = false;
        }
    }

    private static void DisableNamedFans(Transform root)
    {
        if (root == null)
            return;
        DisableFan(root.Find("CloseRangeFan"));
        DisableFan(root.Find("MediumRangeFan"));
        DisableFan(root.Find("LongRangeFan"));
    }

    private static void DisableFan(Transform fan)
    {
        if (fan == null)
            return;
        LineRenderer line = fan.GetComponent<LineRenderer>();
        if (line != null && line.enabled)
            line.enabled = false;
    }

    private void RerouteCompaniesAcrossRiver(PrototypeCompanyTacticalControl09L2 control)
    {
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || !company.IsMoving)
                continue;
            company.EnsureRiverSafeRoute();
        }
    }

    private void UpdateRegimentRangeFans(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<Regiment> selectedParents = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i] != null && selected[i].ParentRegiment != null)
                selectedParents.Add(selected[i].ParentRegiment);
        }

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        foreach (Regiment regiment in selectedParents)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            Vector3 origin;
            Vector3 forward;
            float muzzleHalf;
            if (!TryGetRegimentFront(regiment, companies, selected, out origin, out forward, out muzzleHalf))
                continue;

            RegimentFan fan = GetOrCreateFan(regiment);
            DrawArc(fan.Close, origin, forward, regiment.CloseRange, regiment.FireArcHalfAngle, muzzleHalf, true);
            DrawArc(fan.Medium, origin, forward, regiment.EffectiveRange, regiment.FireArcHalfAngle, muzzleHalf, true);
            DrawOuterFan(fan.Long, origin, forward, regiment.MaximumRange, regiment.FireArcHalfAngle, muzzleHalf, true);
        }

        foreach (KeyValuePair<Regiment, RegimentFan> pair in fans)
        {
            bool show = selectedParents.Contains(pair.Key);
            if (pair.Value.Close != null) pair.Value.Close.enabled = show;
            if (pair.Value.Medium != null) pair.Value.Medium.enabled = show;
            if (pair.Value.Long != null) pair.Value.Long.enabled = show;
        }
    }

    private static bool TryGetRegimentFront(
        Regiment regiment,
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies,
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected,
        out Vector3 origin,
        out Vector3 forward,
        out float muzzleHalf)
    {
        origin = Vector3.zero;
        forward = Vector3.zero;
        muzzleHalf = 8.5f;
        Vector3 centroid = Vector3.zero;
        Vector3 facing = Vector3.zero;
        float width = 0f;
        int count = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = selected[i];
            if (company == null || company.ParentRegiment != regiment || company.CurrentStrength <= 0)
                continue;
            centroid += company.transform.position;
            Vector3 f = company.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) facing += f.normalized;
            width += company.GetFootprintWidth();
            count++;
        }

        if (count <= 0)
            return false;

        centroid /= count;
        if (facing.sqrMagnitude < 0.01f) facing = regiment.transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
        facing.Normalize();
        muzzleHalf = Mathf.Clamp(width / Mathf.Max(1, count) * 0.45f, 4.5f, 18f);
        origin = centroid + facing * 1.35f;
        origin.y = PrototypeBootstrap.SampleGroundHeight(origin.x, origin.z) + 0.16f;
        forward = facing;
        return true;
    }

    private RegimentFan GetOrCreateFan(Regiment regiment)
    {
        RegimentFan fan;
        if (fans.TryGetValue(regiment, out fan) && fan.Long != null)
            return fan;
        fan = new RegimentFan
        {
            Close = CreateLine("RegimentRangeClose_" + regiment.RegimentName, 0.09f, new Color(1.00f, 0.94f, 0.18f, 0.55f)),
            Medium = CreateLine("RegimentRangeMedium_" + regiment.RegimentName, 0.11f, new Color(1.00f, 0.58f, 0.06f, 0.62f)),
            Long = CreateLine("RegimentRangeLong_" + regiment.RegimentName, 0.14f, new Color(1.00f, 0.20f, 0.05f, 0.70f))
        };
        fans[regiment] = fan;
        return fan;
    }

    private void UpdateGhostOrders(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<PrototypeCompanyTacticalEntity09L2> live = new HashSet<PrototypeCompanyTacticalEntity09L2>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        bool previewing = Input.GetMouseButton(1) && selected.Count > 0;
        Vector3 previewPoint = Vector3.zero;
        bool hasPreview = previewing && TryGetGroundPoint(Input.mousePosition, out previewPoint);

        for (int i = 0; i < selected.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = selected[i];
            if (company == null || company.CurrentStrength <= 0)
                continue;

            Vector3 destination;
            bool hasDest = company.TryGetFinalDestination(out destination);
            if (!hasDest && !hasPreview)
            {
                HideGhost(company);
                continue;
            }
            if (hasPreview) destination = previewPoint;

            live.Add(company);
            GhostSet ghost = GetOrCreateGhost(company);
            float halfW = company.GetFootprintWidth() * 0.5f + 0.35f;
            float halfD = company.GetFootprintDepth() * 0.5f + 0.35f;
            Vector3 facing = company.transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
            facing.Normalize();
            Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
            boxScratch.Clear();
            boxScratch.Add(destination + rot * new Vector3(-halfW, 0f, -halfD));
            boxScratch.Add(destination + rot * new Vector3(-halfW, 0f, halfD));
            boxScratch.Add(destination + rot * new Vector3(halfW, 0f, halfD));
            boxScratch.Add(destination + rot * new Vector3(halfW, 0f, -halfD));
            DrawGroundLoop(ghost.Box, boxScratch, true);
            pathScratch.Clear();
            if (hasPreview) BuildPath(company.transform.position, destination, pathScratch);
            else company.CopyRoute(pathScratch);
            DrawGroundLine(ghost.Path, pathScratch, true);
        }

        List<PrototypeCompanyTacticalEntity09L2> stale = new List<PrototypeCompanyTacticalEntity09L2>();
        foreach (KeyValuePair<PrototypeCompanyTacticalEntity09L2, GhostSet> pair in ghosts)
        {
            if (!live.Contains(pair.Key)) stale.Add(pair.Key);
        }
        for (int i = 0; i < stale.Count; i++) HideGhost(stale[i]);
    }

    private GhostSet GetOrCreateGhost(PrototypeCompanyTacticalEntity09L2 company)
    {
        GhostSet ghost;
        if (ghosts.TryGetValue(company, out ghost) && ghost.Box != null)
            return ghost;
        ghost = new GhostSet
        {
            Box = CreateLine("GhostDestBox_" + company.CompanyId, 0.12f, new Color(0.78f, 0.92f, 1f, 0.55f)),
            Path = CreateLine("GhostDestPath_" + company.CompanyId, 0.10f, new Color(0.70f, 0.86f, 1f, 0.70f))
        };
        ghost.Box.loop = true;
        ghosts[company] = ghost;
        return ghost;
    }

    private void HideGhost(PrototypeCompanyTacticalEntity09L2 company)
    {
        GhostSet ghost;
        if (!ghosts.TryGetValue(company, out ghost)) return;
        if (ghost.Box != null) ghost.Box.enabled = false;
        if (ghost.Path != null) ghost.Path.enabled = false;
    }

    private LineRenderer CreateLine(string name, float width, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(visualRoot, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startColor = color;
        line.endColor = color;
        line.enabled = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material material = new Material(shader) { name = name + "_Mat", color = color };
            line.sharedMaterial = material;
        }
        return line;
    }

    private static void DrawArc(LineRenderer line, Vector3 origin, Vector3 forward, float range, float halfAngle, float muzzleHalf, bool enabled)
    {
        if (line == null) return;
        const int arcSegments = 40;
        line.loop = false;
        line.positionCount = arcSegments + 1;
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            line.SetPosition(i, Ground(origin + direction * range));
        }
        line.enabled = enabled;
    }

    private static void DrawOuterFan(LineRenderer line, Vector3 origin, Vector3 forward, float range, float halfAngle, float muzzleHalf, bool enabled)
    {
        if (line == null) return;
        const int sideSegments = 8;
        const int arcSegments = 40;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 leftMuzzle = origin - right * muzzleHalf + forward * 0.4f;
        Vector3 rightMuzzle = origin + right * muzzleHalf + forward * 0.4f;
        Vector3 leftEdge = origin + Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * range;
        Vector3 rightEdge = origin + Quaternion.AngleAxis(halfAngle, Vector3.up) * forward * range;
        line.loop = false;
        line.positionCount = 1 + sideSegments + arcSegments + sideSegments;
        int index = 0;
        line.SetPosition(index++, Ground(leftMuzzle));
        for (int i = 1; i <= sideSegments; i++)
            line.SetPosition(index++, Ground(Vector3.Lerp(leftMuzzle, leftEdge, i / (float)sideSegments)));
        for (int i = 1; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            line.SetPosition(index++, Ground(origin + direction * range));
        }
        for (int i = 1; i <= sideSegments; i++)
            line.SetPosition(index++, Ground(Vector3.Lerp(rightEdge, rightMuzzle, i / (float)sideSegments)));
        line.enabled = enabled;
    }

    private static void DrawGroundLoop(LineRenderer line, List<Vector3> points, bool enabled)
    {
        if (line == null) return;
        line.loop = true;
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) line.SetPosition(i, Ground(points[i]));
        line.enabled = enabled && points.Count >= 3;
    }

    private static void DrawGroundLine(LineRenderer line, List<Vector3> points, bool enabled)
    {
        if (line == null) return;
        line.loop = false;
        line.positionCount = Mathf.Max(0, points.Count);
        for (int i = 0; i < points.Count; i++) line.SetPosition(i, Ground(points[i]));
        line.enabled = enabled && points.Count >= 2;
    }

    public static void BuildPath(Vector3 from, Vector3 to, List<Vector3> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        from.y = 0f;
        to = SnapOutOfRiver(to);
        to.y = 0f;
        buffer.Add(from);
        if (!SegmentCrossesRiver(from, to))
        {
            buffer.Add(Ground(to));
            return;
        }
        float bridgeX = StreamCenterX(BridgeZ);
        float fromSide = BankSide(from);
        float toSide = BankSide(to);
        if (Mathf.Abs(fromSide) < 0.1f) fromSide = toSide >= 0f ? -1f : 1f;
        if (Mathf.Abs(toSide) < 0.1f) toSide = -fromSide;
        Vector3 approach = new Vector3(bridgeX + fromSide * BridgeApproachOffset, 0f, BridgeZ);
        Vector3 deck = new Vector3(bridgeX, 0f, BridgeZ);
        Vector3 exit = new Vector3(bridgeX + toSide * BridgeApproachOffset, 0f, BridgeZ);
        AppendIfFar(buffer, approach);
        AppendIfFar(buffer, deck);
        AppendIfFar(buffer, exit);
        AppendIfFar(buffer, to);
    }

    public static Vector3 SnapOutOfRiver(Vector3 point)
    {
        if (IsBridgeZone(point) || !IsRiverWater(point)) return point;
        float center = StreamCenterX(point.z);
        float side = point.x >= center ? 1f : -1f;
        point.x = center + side * (RiverHalfWidth + 3.2f);
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    public static bool SegmentCrossesRiver(Vector3 a, Vector3 b)
    {
        const int samples = 48;
        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector3 point = Vector3.Lerp(a, b, t);
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

    public static float BankSide(Vector3 point)
    {
        return Mathf.Sign(point.x - StreamCenterX(point.z));
    }

    private static void AppendIfFar(List<Vector3> buffer, Vector3 point)
    {
        point = Ground(point);
        if (buffer.Count == 0) { buffer.Add(point); return; }
        Vector3 last = buffer[buffer.Count - 1]; last.y = 0f;
        Vector3 flat = point; flat.y = 0f;
        if (Vector3.Distance(last, flat) < 2.2f) return;
        buffer.Add(point);
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.16f;
        return point;
    }

    private bool TryGetGroundPoint(Vector3 screenPosition, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float enter)) return false;
        point = ray.GetPoint(enter);
        point = SnapOutOfRiver(point);
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return true;
    }
}
