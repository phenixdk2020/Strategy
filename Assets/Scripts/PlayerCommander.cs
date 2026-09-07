using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
public sealed class PlayerCommander : MonoBehaviour
{
    private sealed class MovementRoute
    {
        public readonly List<Vector3> Waypoints = new List<Vector3>();
        public int CurrentIndex;
        public Vector3 FinalFacing = Vector3.forward;
        public RegimentFormation FinalFormation = RegimentFormation.Line;
        public bool HasExplicitFinalFacing;
    }

    private sealed class OrderGhostVisual
    {
        public GameObject Root;
        public LineRenderer Path;
        public LineRenderer Footprint;
    }

    public static PlayerCommander Instance { get; private set; }

    private readonly List<Regiment> selected = new List<Regiment>();
    private readonly Dictionary<Regiment, MovementRoute> routes = new Dictionary<Regiment, MovementRoute>();
    private readonly Dictionary<Regiment, OrderGhostVisual> orderGhosts = new Dictionary<Regiment, OrderGhostVisual>();

    private Camera cam;
    private bool rightDragActive;
    private bool rightDragAlt;
    private Vector3 rightDragStart;
    private Vector3 rightDragEnd;
    private LineRenderer formationPreview;
    private LineRenderer facingGuidePreview;
    private GameObject groupCenterPreview;

    private const float FormationDragThreshold = 4f;
    private const float RegimentLineSpacing = 22f;
    private const float FacingStepDegrees = 15f;
    private const float RouteArrivalDistance = 3.25f;
    private const float RegimentPreviewWidth = 19f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        cam = Camera.main;
        EnsureFormationPreview();
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
                return;
        }

        bool pointerOverSimulationControls =
            BattleManager.Instance != null &&
            BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(0))
            HandleSelection();

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(1))
            BeginRightMouseOrder();

        if (rightDragActive && Input.GetMouseButton(1))
            UpdateRightMouseOrder();

        if (rightDragActive && Input.GetMouseButtonUp(1))
            CompleteRightMouseOrder();

        if (Input.GetKeyDown(KeyCode.H))
        {
            ForEachSelected(regiment =>
            {
                ClearRoute(regiment);

                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null && controller.AIEnabled)
                    controller.SetHoldMission();
                else
                    regiment.OrderHold();
            });
        }

        if (Input.GetKeyDown(KeyCode.F))
            SetSelectedFormation(RegimentFormation.Line);

        if (Input.GetKeyDown(KeyCode.C))
            SetSelectedFormation(RegimentFormation.Column);

        if (Input.GetKeyDown(KeyCode.T))
        {
            ForEachSelected(regiment =>
            {
                regiment.ShowRange = !regiment.ShowRange;
                regiment.RefreshRangeVisibility();
            });
        }

        if (Input.GetKeyDown(KeyCode.Z))
            RotateSelectedFacing(-FacingStepDegrees);

        if (Input.GetKeyDown(KeyCode.X))
            RotateSelectedFacing(FacingStepDegrees);

        UpdateRoutes();
        UpdateOrderGhosts();
    }

    public void RotateSelectedFacing(float degrees)
    {
        ForEachSelected(regiment =>
        {
            if (regiment == null || regiment.IsRouted)
                return;

            if (routes.TryGetValue(regiment, out MovementRoute route))
            {
                Vector3 facing = Quaternion.Euler(0f, degrees, 0f) * route.FinalFacing;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.01f)
                    facing = regiment.transform.forward;

                route.FinalFacing = facing.normalized;
                route.HasExplicitFinalFacing = true;
                UpdateGhostFootprint(regiment);
                return;
            }

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetHoldMission();
            else
                regiment.OrderHold();

            regiment.transform.rotation = Quaternion.Euler(0f, degrees, 0f) * regiment.transform.rotation;
        });
    }

    private void SetSelectedFormation(RegimentFormation formation)
    {
        ForEachSelected(regiment =>
        {
            regiment.SetFormation(formation);

            if (routes.TryGetValue(regiment, out MovementRoute route))
            {
                route.FinalFormation = RegimentFormation.Line;
                UpdateGhostFootprint(regiment);
            }
        });
    }

    private void HandleSelection()
    {
        bool additive =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Regiment regiment = hit.collider.GetComponentInParent<Regiment>();
            if (regiment != null && regiment.Team == BattleTeam.Denmark)
            {
                if (!additive)
                    ClearSelection();

                if (selected.Contains(regiment) && additive)
                {
                    selected.Remove(regiment);
                    regiment.SetSelected(false);
                }
                else if (!selected.Contains(regiment))
                {
                    selected.Add(regiment);
                    regiment.SetSelected(true);
                }

                return;
            }
        }

        if (!additive)
            ClearSelection();
    }

    private void BeginRightMouseOrder()
    {
        if (selected.Count == 0)
            return;

        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (!altHeld)
        {
            Regiment enemy = GetEnemyUnderMouse();
            if (enemy != null)
            {
                IssueAttackOrder(enemy);
                return;
            }
        }

        if (!TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            return;

        rightDragActive = true;
        rightDragAlt = altHeld;
        rightDragStart = point;
        rightDragEnd = point;
        UpdateFormationPreview();
    }

    private void UpdateRightMouseOrder()
    {
        if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            rightDragEnd = point;

        UpdateFormationPreview();
    }

    private void CompleteRightMouseOrder()
    {
        rightDragActive = false;
        SetFormationPreviewVisible(false);

        Vector3 drag = rightDragEnd - rightDragStart;
        drag.y = 0f;
        bool hasFacingDrag = drag.magnitude >= FormationDragThreshold;
        Vector3 groupFacing = hasFacingDrag ? drag.normalized : GetAutomaticGroupFacing(rightDragStart);

        if (rightDragAlt)
        {
            AppendGroupWaypointOrder(rightDragStart, groupFacing, hasFacingDrag);
            return;
        }

        if (selected.Count == 1)
        {
            IssueSingleMoveOrder(selected[0], rightDragStart, groupFacing, hasFacingDrag);
            return;
        }

        IssueCenteredGroupLineOrder(rightDragStart, groupFacing, hasFacingDrag);
    }

    private void IssueAttackOrder(Regiment target)
    {
        if (target == null)
            return;

        ForEachSelected(regiment =>
        {
            ClearRoute(regiment);
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAttackMission(target);
            else
                regiment.OrderAttack(target);
        });
    }

    private void IssueSingleMoveOrder(Regiment regiment, Vector3 destination, Vector3 finalFacing, bool hasExplicitFacing)
    {
        if (regiment == null)
            return;

        destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;
        Vector3 travelFacing = destination - regiment.transform.position;
        travelFacing.y = 0f;
        if (travelFacing.sqrMagnitude < 0.01f)
            travelFacing = regiment.transform.forward;
        travelFacing.Normalize();

        ReplaceRoute(
            regiment,
            destination,
            hasExplicitFacing ? finalFacing : travelFacing,
            RegimentFormation.Line,
            hasExplicitFacing);
    }

    private void IssueCenteredGroupLineOrder(Vector3 center, Vector3 requestedFacing, bool hasExplicitFacing)
    {
        if (selected.Count == 0)
            return;

        Vector3 facing = NormalizedFacing(requestedFacing, GetAutomaticGroupFacing(center));
        Vector3 lineDirection = Vector3.Cross(Vector3.up, facing).normalized;
        if (lineDirection.sqrMagnitude < 0.01f)
            lineDirection = Vector3.right;

        List<Regiment> ordered = GetSelectedInLineOrder(lineDirection);
        float centerIndex = (ordered.Count - 1) * 0.5f;

        for (int i = 0; i < ordered.Count; i++)
        {
            Regiment regiment = ordered[i];
            float offset = (i - centerIndex) * RegimentLineSpacing;
            Vector3 destination = center + lineDirection * offset;
            destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

            ReplaceRoute(regiment, destination, facing, RegimentFormation.Line, hasExplicitFacing);
        }
    }

    private void AppendGroupWaypointOrder(Vector3 center, Vector3 requestedFacing, bool hasExplicitFacing)
    {
        if (selected.Count == 0)
            return;

        Vector3 facing = NormalizedFacing(requestedFacing, GetAutomaticGroupFacing(center));
        Vector3 lineDirection = Vector3.Cross(Vector3.up, facing).normalized;
        if (lineDirection.sqrMagnitude < 0.01f)
            lineDirection = Vector3.right;

        List<Regiment> ordered = GetSelectedInLineOrder(lineDirection);
        float centerIndex = (ordered.Count - 1) * 0.5f;

        for (int i = 0; i < ordered.Count; i++)
        {
            Regiment regiment = ordered[i];
            float offset = (i - centerIndex) * RegimentLineSpacing;
            Vector3 waypoint = center + lineDirection * offset;
            waypoint.y = PrototypeBootstrap.SampleGroundHeight(waypoint.x, waypoint.z) + 0.10f;
            AppendRouteWaypoint(regiment, waypoint, facing, hasExplicitFacing);
        }
    }

    private Vector3 GetAutomaticGroupFacing(Vector3 destinationCenter)
    {
        Vector3 center = GetSelectedWorldCenter();
        Vector3 facing = destinationCenter - center;
        facing.y = 0f;
        if (facing.sqrMagnitude >= 0.01f)
            return facing.normalized;

        Vector3 averageForward = Vector3.zero;
        foreach (Regiment regiment in selected)
        {
            if (regiment == null)
                continue;
            Vector3 forward = regiment.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
                averageForward += forward.normalized;
        }

        if (averageForward.sqrMagnitude < 0.01f)
            averageForward = Vector3.forward;
        return averageForward.normalized;
    }

    private Vector3 GetSelectedWorldCenter()
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in selected)
        {
            if (regiment == null)
                continue;
            total += regiment.transform.position;
            count++;
        }
        return count == 0 ? Vector3.zero : total / count;
    }

    private List<Regiment> GetSelectedInLineOrder(Vector3 lineDirection)
    {
        List<Regiment> ordered = new List<Regiment>();
        foreach (Regiment regiment in selected)
            if (regiment != null)
                ordered.Add(regiment);

        if (lineDirection.sqrMagnitude < 0.01f)
            return ordered;

        Vector3 normalized = lineDirection.normalized;
        ordered.Sort((a, b) =>
        {
            float aProjection = Vector3.Dot(a.transform.position, normalized);
            float bProjection = Vector3.Dot(b.transform.position, normalized);
            return aProjection.CompareTo(bProjection);
        });
        return ordered;
    }

    private void ReplaceRoute(Regiment regiment, Vector3 destination, Vector3 finalFacing, RegimentFormation finalFormation, bool hasExplicitFacing)
    {
        ClearRoute(regiment);
        MovementRoute route = new MovementRoute
        {
            CurrentIndex = 0,
            FinalFacing = NormalizedFacing(finalFacing, regiment.transform.forward),
            FinalFormation = finalFormation,
            HasExplicitFinalFacing = hasExplicitFacing
        };

        route.Waypoints.Add(destination);
        routes[regiment] = route;
        CreateOrUpdateOrderGhost(regiment);
        IssueCurrentRouteLeg(regiment, route);
    }

    private void AppendRouteWaypoint(Regiment regiment, Vector3 waypoint, Vector3 explicitFacing, bool hasExplicitFacing)
    {
        bool hadRoute = routes.TryGetValue(regiment, out MovementRoute route);
        if (!hadRoute)
        {
            route = new MovementRoute { CurrentIndex = 0, FinalFormation = RegimentFormation.Line };
            routes[regiment] = route;
        }

        Vector3 from = route.Waypoints.Count > 0 ? route.Waypoints[route.Waypoints.Count - 1] : regiment.transform.position;
        route.Waypoints.Add(waypoint);

        Vector3 automaticFacing = waypoint - from;
        automaticFacing.y = 0f;
        if (automaticFacing.sqrMagnitude < 0.01f)
            automaticFacing = regiment.transform.forward;

        route.FinalFacing = hasExplicitFacing ? NormalizedFacing(explicitFacing, automaticFacing) : automaticFacing.normalized;
        route.HasExplicitFinalFacing = hasExplicitFacing;
        route.FinalFormation = RegimentFormation.Line;
        CreateOrUpdateOrderGhost(regiment);

        if (!hadRoute || route.Waypoints.Count == 1)
            IssueCurrentRouteLeg(regiment, route);
    }

    private void UpdateRoutes()
    {
        if (routes.Count == 0)
            return;

        List<Regiment> completed = null;
        foreach (KeyValuePair<Regiment, MovementRoute> pair in routes)
        {
            Regiment regiment = pair.Key;
            MovementRoute route = pair.Value;

            if (regiment == null || regiment.IsRouted || route.CurrentIndex < 0 || route.CurrentIndex >= route.Waypoints.Count)
            {
                if (completed == null)
                    completed = new List<Regiment>();
                completed.Add(regiment);
                continue;
            }

            Vector3 target = route.Waypoints[route.CurrentIndex];
            Vector3 delta = regiment.transform.position - target;
            delta.y = 0f;

            if (delta.sqrMagnitude > RouteArrivalDistance * RouteArrivalDistance)
            {
                regiment.OrderMove(target);
                continue;
            }

            route.CurrentIndex++;
            if (route.CurrentIndex < route.Waypoints.Count)
            {
                IssueCurrentRouteLeg(regiment, route);
                continue;
            }

            regiment.SetFormation(route.FinalFormation);
            regiment.OrderHold();
            if (route.FinalFacing.sqrMagnitude > 0.01f)
                regiment.transform.rotation = Quaternion.LookRotation(route.FinalFacing.normalized, Vector3.up);

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetHoldMission();

            if (completed == null)
                completed = new List<Regiment>();
            completed.Add(regiment);
        }

        if (completed == null)
            return;
        foreach (Regiment regiment in completed)
            ClearRoute(regiment);
    }

    private void IssueCurrentRouteLeg(Regiment regiment, MovementRoute route)
    {
        if (regiment == null || route == null || route.CurrentIndex < 0 || route.CurrentIndex >= route.Waypoints.Count)
            return;

        Vector3 waypoint = route.Waypoints[route.CurrentIndex];
        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetMoveMission(waypoint);
        else
            regiment.OrderMove(waypoint);
    }

    private static Vector3 NormalizedFacing(Vector3 facing, Vector3 fallback)
    {
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
        {
            facing = fallback;
            facing.y = 0f;
        }
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;
        return facing.normalized;
    }

    private void CreateOrUpdateOrderGhost(Regiment regiment)
    {
        if (regiment == null || !routes.ContainsKey(regiment))
            return;

        if (!orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost))
        {
            GameObject root = new GameObject(regiment.RegimentName + "_OrderGhost");
            root.transform.SetParent(transform, true);
            ghost = new OrderGhostVisual
            {
                Root = root,
                Path = CreateGhostLine(root.transform, "Path", 0.12f, new Color(0.72f, 0.88f, 1f)),
                Footprint = CreateGhostLine(root.transform, "Destination", 0.22f, new Color(0.98f, 0.88f, 0.18f))
            };
            orderGhosts[regiment] = ghost;
        }

        UpdateGhostPath(regiment);
        UpdateGhostFootprint(regiment);
    }

    private LineRenderer CreateGhostLine(Transform parent, string name, float width, Color color)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = "OrderGhost_" + name, color = color };
        line.sharedMaterial = material;
        return line;
    }

    private void UpdateOrderGhosts()
    {
        if (orderGhosts.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, OrderGhostVisual> pair in orderGhosts)
        {
            if (pair.Key == null || !routes.ContainsKey(pair.Key))
            {
                if (stale == null) stale = new List<Regiment>();
                stale.Add(pair.Key);
                continue;
            }
            UpdateGhostPath(pair.Key);
            UpdateGhostFootprint(pair.Key);
        }

        if (stale == null) return;
        foreach (Regiment regiment in stale)
            ClearOrderGhost(regiment);
    }

    private void UpdateGhostPath(Regiment regiment)
    {
        if (regiment == null || !routes.TryGetValue(regiment, out MovementRoute route) || !orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost) || ghost.Path == null)
            return;

        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(regiment.transform.position);
        for (int i = route.CurrentIndex; i < route.Waypoints.Count; i++)
            controlPoints.Add(route.Waypoints[i]);

        if (controlPoints.Count < 2)
        {
            ghost.Path.positionCount = 0;
            return;
        }

        const int samplesPerLeg = 8;
        int totalPositions = (controlPoints.Count - 1) * samplesPerLeg + 1;
        ghost.Path.positionCount = totalPositions;
        int outputIndex = 0;

        for (int leg = 0; leg < controlPoints.Count - 1; leg++)
        {
            Vector3 a = controlPoints[leg];
            Vector3 b = controlPoints[leg + 1];
            for (int i = 0; i < samplesPerLeg; i++)
            {
                float t = i / (float)samplesPerLeg;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.38f;
                ghost.Path.SetPosition(outputIndex++, p);
            }
        }

        Vector3 last = controlPoints[controlPoints.Count - 1];
        last.y = PrototypeBootstrap.SampleGroundHeight(last.x, last.z) + 0.38f;
        ghost.Path.SetPosition(outputIndex, last);
    }

    private void UpdateGhostFootprint(Regiment regiment)
    {
        if (regiment == null || !routes.TryGetValue(regiment, out MovementRoute route) || route.Waypoints.Count == 0 || !orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost) || ghost.Footprint == null)
            return;

        Vector3 center = route.Waypoints[route.Waypoints.Count - 1];
        DrawFootprint(ghost.Footprint, center, route.FinalFacing, route.FinalFormation, regiment.transform.forward);
    }

    private void ClearRoute(Regiment regiment)
    {
        if (regiment != null)
            routes.Remove(regiment);
        ClearOrderGhost(regiment);
    }

    private void ClearOrderGhost(Regiment regiment)
    {
        if (regiment == null)
            return;
        if (!orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost))
            return;
        if (ghost.Root != null)
            Destroy(ghost.Root);
        orderGhosts.Remove(regiment);
    }

    private Regiment GetEnemyUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Regiment target = hit.collider.GetComponentInParent<Regiment>();
            if (target != null && target.Team == BattleTeam.Prussia)
                return target;
        }
        return null;
    }

    private bool TryGetGroundPoint(Vector3 screenPoint, out Vector3 point)
    {
        Ray ray = cam.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.12f;
            return true;
        }
        point = default;
        return false;
    }

    private void EnsureFormationPreview()
    {
        if (formationPreview != null && facingGuidePreview != null && groupCenterPreview != null)
            return;

        GameObject previewRoot = new GameObject("FormationOrderPreview");
        previewRoot.transform.SetParent(transform, true);

        formationPreview = CreateGhostLine(previewRoot.transform, "DestinationPreview", 0.30f, new Color(1f, 0.88f, 0.12f));
        facingGuidePreview = CreateGhostLine(previewRoot.transform, "FacingGuidePreview", 0.15f, new Color(0.98f, 0.98f, 0.72f));

        groupCenterPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        groupCenterPreview.name = "GroupOrderCenter";
        groupCenterPreview.transform.SetParent(previewRoot.transform, true);
        groupCenterPreview.transform.localScale = Vector3.one * 0.75f;

        Renderer centerRenderer = groupCenterPreview.GetComponent<Renderer>();
        if (centerRenderer != null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            centerRenderer.sharedMaterial = new Material(shader)
            {
                name = "GroupOrderCenterMaterial",
                color = new Color(1f, 0.88f, 0.12f)
            };
        }

        Collider centerCollider = groupCenterPreview.GetComponent<Collider>();
        if (centerCollider != null)
            Destroy(centerCollider);

        SetFormationPreviewVisible(false);
    }

    private void UpdateFormationPreview()
    {
        EnsureFormationPreview();
        if (selected.Count == 0)
        {
            SetFormationPreviewVisible(false);
            return;
        }

        Vector3 drag = rightDragEnd - rightDragStart;
        drag.y = 0f;
        bool hasFacingDrag = drag.magnitude >= FormationDragThreshold;
        Vector3 facing = hasFacingDrag ? drag.normalized : GetAutomaticGroupFacing(rightDragStart);

        ShowPreviewCenter(rightDragStart);

        if (selected.Count == 1)
        {
            Regiment reference = selected[0];
            Vector3 fallback = reference != null ? reference.transform.forward : Vector3.forward;
            DrawFootprint(formationPreview, rightDragStart, facing, RegimentFormation.Line, fallback);
            DrawFacingGuide(rightDragStart, facing);
            formationPreview.enabled = true;
            return;
        }

        Vector3 lineDirection = Vector3.Cross(Vector3.up, facing).normalized;
        if (lineDirection.sqrMagnitude < 0.01f)
            lineDirection = Vector3.right;

        float frontage = RegimentPreviewWidth + Mathf.Max(0, selected.Count - 1) * RegimentLineSpacing;
        Vector3 start = rightDragStart - lineDirection * frontage * 0.5f;
        Vector3 end = rightDragStart + lineDirection * frontage * 0.5f;

        SetTerrainFollowingLine(formationPreview, start, end, 24, 0.48f);
        formationPreview.enabled = true;
        DrawFacingGuide(rightDragStart, facing);
    }

    private void DrawFacingGuide(Vector3 center, Vector3 facing)
    {
        Vector3 normalized = NormalizedFacing(facing, Vector3.forward);
        Vector3 start = center;
        start.y = PrototypeBootstrap.SampleGroundHeight(start.x, start.z) + 0.52f;
        Vector3 end = center + normalized * 12f;
        end.y = PrototypeBootstrap.SampleGroundHeight(end.x, end.z) + 0.52f;
        facingGuidePreview.positionCount = 2;
        facingGuidePreview.SetPosition(0, start);
        facingGuidePreview.SetPosition(1, end);
        facingGuidePreview.enabled = true;
    }

    private void ShowPreviewCenter(Vector3 center)
    {
        if (groupCenterPreview == null)
            return;
        center.y = PrototypeBootstrap.SampleGroundHeight(center.x, center.z) + 0.48f;
        groupCenterPreview.transform.position = center;
        groupCenterPreview.SetActive(true);
    }

    private static void SetTerrainFollowingLine(LineRenderer line, Vector3 start, Vector3 end, int segments, float heightOffset)
    {
        if (line == null)
            return;
        int safeSegments = Mathf.Max(1, segments);
        line.positionCount = safeSegments + 1;
        for (int i = 0; i <= safeSegments; i++)
        {
            float t = i / (float)safeSegments;
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + heightOffset;
            line.SetPosition(i, p);
        }
    }

    private static void DrawFootprint(LineRenderer line, Vector3 center, Vector3 facing, RegimentFormation formation, Vector3 fallbackFacing)
    {
        if (line == null)
            return;

        Vector3 forward = NormalizedFacing(facing, fallbackFacing);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfWidth = formation == RegimentFormation.Line ? 9.5f : 3.0f;
        float halfDepth = formation == RegimentFormation.Line ? 2.6f : 7.5f;

        Vector3[] corners =
        {
            center - right * halfWidth - forward * halfDepth,
            center + right * halfWidth - forward * halfDepth,
            center + right * halfWidth + forward * halfDepth,
            center - right * halfWidth + forward * halfDepth,
            center - right * halfWidth - forward * halfDepth
        };

        line.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p = corners[i];
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
            line.SetPosition(i, p);
        }
    }

    private void SetFormationPreviewVisible(bool visible)
    {
        if (formationPreview != null)
            formationPreview.enabled = visible;
        if (facingGuidePreview != null)
            facingGuidePreview.enabled = visible;
        if (groupCenterPreview != null)
            groupCenterPreview.SetActive(visible);
    }

    private void ForEachSelected(System.Action<Regiment> action)
    {
        for (int i = selected.Count - 1; i >= 0; i--)
        {
            if (selected[i] == null)
            {
                selected.RemoveAt(i);
                continue;
            }
            action(selected[i]);
        }
    }

    private void ClearSelection()
    {
        foreach (Regiment regiment in selected)
            if (regiment != null)
                regiment.SetSelected(false);
        selected.Clear();
    }
}
