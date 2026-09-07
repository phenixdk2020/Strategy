using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
public sealed class PlayerCommander : MonoBehaviour
{
    private sealed class MovementRoute
    {
        public readonly List<Vector3> Waypoints = new List<Vector3>();
        public int CurrentIndex;
        public Vector3 FinalFacing;
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

    private const float FormationDragThreshold = 4f;
    private const float RegimentLineSpacing = 22f;
    private const float FacingStepDegrees = 15f;
    private const float RouteArrivalDistance = 3.25f;

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

        // PlayerCommander executes after OfficerAIController. An explicit player route
        // therefore remains authoritative instead of an officer decision silently
        // cancelling a waypoint while the route is still active.
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

            regiment.transform.rotation =
                Quaternion.Euler(0f, degrees, 0f) * regiment.transform.rotation;
        });
    }

    private void SetSelectedFormation(RegimentFormation formation)
    {
        ForEachSelected(regiment =>
        {
            regiment.SetFormation(formation);

            // A route may be marched in column, but the destination footprint is
            // deliberately a deployed line unless the destination system is later
            // expanded with an explicit final-formation selector.
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

        bool altHeld =
            Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

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

        Vector3 facingDelta = rightDragEnd - rightDragStart;
        facingDelta.y = 0f;
        bool dragged = facingDelta.magnitude >= FormationDragThreshold;

        if (rightDragAlt)
        {
            Vector3 explicitFacing = dragged ? facingDelta.normalized : Vector3.zero;
            AppendWaypointOrder(rightDragStart, explicitFacing, dragged);
            return;
        }

        if (selected.Count == 1)
        {
            Vector3 explicitFacing = dragged ? facingDelta.normalized : Vector3.zero;
            IssueSingleMoveOrder(selected[0], rightDragStart, explicitFacing, dragged);
            return;
        }

        if (dragged)
        {
            IssueFormationLineOrder(rightDragStart, rightDragEnd);
            return;
        }

        IssueSimpleGroupMoveOrder(rightDragStart);
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

    private void IssueSingleMoveOrder(
        Regiment regiment,
        Vector3 destination,
        Vector3 explicitFacing,
        bool hasExplicitFacing)
    {
        if (regiment == null)
            return;

        destination.y =
            PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

        Vector3 travelFacing = destination - regiment.transform.position;
        travelFacing.y = 0f;
        if (travelFacing.sqrMagnitude < 0.01f)
            travelFacing = regiment.transform.forward;
        travelFacing.Normalize();

        ReplaceRoute(
            regiment,
            destination,
            hasExplicitFacing ? explicitFacing : travelFacing,
            RegimentFormation.Line,
            hasExplicitFacing);
    }

    private void IssueSimpleGroupMoveOrder(Vector3 basePoint)
    {
        Vector3 right = cam.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.right;
        right.Normalize();

        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (regiment == null)
                continue;

            float offset = (i - (selected.Count - 1) * 0.5f) * 8f;
            Vector3 destination = basePoint + right * offset;
            destination.y =
                PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

            Vector3 facing = destination - regiment.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f)
                facing = regiment.transform.forward;
            facing.Normalize();

            ReplaceRoute(
                regiment,
                destination,
                facing,
                RegimentFormation.Line,
                false);
        }
    }

    private void IssueFormationLineOrder(Vector3 rawStart, Vector3 rawEnd)
    {
        List<Regiment> ordered = GetSelectedInLineOrder(rawStart, rawEnd);
        if (ordered.Count == 0)
            return;

        Vector3 line = rawEnd - rawStart;
        line.y = 0f;
        if (line.sqrMagnitude < 0.01f)
            return;

        Vector3 lineDirection = line.normalized;
        Vector3 midpoint = (rawStart + rawEnd) * 0.5f;
        float requestedLength = line.magnitude;
        float requiredLength =
            Mathf.Max(requestedLength, (ordered.Count - 1) * RegimentLineSpacing);

        Vector3 start = midpoint - lineDirection * requiredLength * 0.5f;
        Vector3 end = midpoint + lineDirection * requiredLength * 0.5f;

        Vector3 facing = Vector3.Cross(Vector3.up, lineDirection).normalized;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;

        for (int i = 0; i < ordered.Count; i++)
        {
            Regiment regiment = ordered[i];
            float t = ordered.Count == 1 ? 0.5f : i / (float)(ordered.Count - 1);
            Vector3 destination = Vector3.Lerp(start, end, t);
            destination.y =
                PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

            ReplaceRoute(
                regiment,
                destination,
                facing,
                RegimentFormation.Line,
                true);
        }
    }

    private void AppendWaypointOrder(
        Vector3 basePoint,
        Vector3 explicitFacing,
        bool hasExplicitFacing)
    {
        Vector3 right = cam.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.right;
        right.Normalize();

        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (regiment == null)
                continue;

            float offset = (i - (selected.Count - 1) * 0.5f) * 8f;
            Vector3 waypoint = basePoint + right * offset;
            waypoint.y =
                PrototypeBootstrap.SampleGroundHeight(waypoint.x, waypoint.z) + 0.10f;

            AppendRouteWaypoint(
                regiment,
                waypoint,
                explicitFacing,
                hasExplicitFacing);
        }
    }

    private void ReplaceRoute(
        Regiment regiment,
        Vector3 destination,
        Vector3 finalFacing,
        RegimentFormation finalFormation,
        bool hasExplicitFacing)
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

    private void AppendRouteWaypoint(
        Regiment regiment,
        Vector3 waypoint,
        Vector3 explicitFacing,
        bool hasExplicitFacing)
    {
        bool hadRoute = routes.TryGetValue(regiment, out MovementRoute route);

        if (!hadRoute)
        {
            route = new MovementRoute
            {
                CurrentIndex = 0,
                FinalFormation = RegimentFormation.Line
            };
            routes[regiment] = route;
        }

        Vector3 from = route.Waypoints.Count > 0
            ? route.Waypoints[route.Waypoints.Count - 1]
            : regiment.transform.position;

        route.Waypoints.Add(waypoint);

        Vector3 automaticFacing = waypoint - from;
        automaticFacing.y = 0f;
        if (automaticFacing.sqrMagnitude < 0.01f)
            automaticFacing = regiment.transform.forward;

        route.FinalFacing = hasExplicitFacing
            ? NormalizedFacing(explicitFacing, automaticFacing)
            : automaticFacing.normalized;
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

            if (regiment == null || regiment.IsRouted)
            {
                if (completed == null)
                    completed = new List<Regiment>();
                completed.Add(regiment);
                continue;
            }

            if (route.CurrentIndex < 0 || route.CurrentIndex >= route.Waypoints.Count)
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
                // Enforce the explicit route after Officer AI has made its frame decision.
                // The officer can still choose march formation, but cannot silently drop
                // the waypoint because of an unrelated local attack decision.
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
            {
                regiment.transform.rotation =
                    Quaternion.LookRotation(route.FinalFacing.normalized, Vector3.up);
            }

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
        if (regiment == null ||
            route == null ||
            route.CurrentIndex < 0 ||
            route.CurrentIndex >= route.Waypoints.Count)
        {
            return;
        }

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

    private List<Regiment> GetSelectedInLineOrder(Vector3 rawStart, Vector3 rawEnd)
    {
        Vector3 lineDirection = rawEnd - rawStart;
        lineDirection.y = 0f;

        List<Regiment> ordered = new List<Regiment>();
        foreach (Regiment regiment in selected)
        {
            if (regiment != null)
                ordered.Add(regiment);
        }

        if (lineDirection.sqrMagnitude < 0.01f)
            return ordered;

        lineDirection.Normalize();
        ordered.Sort((a, b) =>
        {
            float aProjection = Vector3.Dot(a.transform.position, lineDirection);
            float bProjection = Vector3.Dot(b.transform.position, lineDirection);
            return aProjection.CompareTo(bProjection);
        });

        return ordered;
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
                Path = CreateGhostLine(
                    root.transform,
                    "Path",
                    0.12f,
                    new Color(0.72f, 0.88f, 1f)),
                Footprint = CreateGhostLine(
                    root.transform,
                    "Destination",
                    0.22f,
                    new Color(0.98f, 0.88f, 0.18f))
            };

            orderGhosts[regiment] = ghost;
        }

        UpdateGhostPath(regiment);
        UpdateGhostFootprint(regiment);
    }

    private LineRenderer CreateGhostLine(
        Transform parent,
        string name,
        float width,
        Color color)
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
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "OrderGhost_" + name,
            color = color
        };

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
                if (stale == null)
                    stale = new List<Regiment>();

                stale.Add(pair.Key);
                continue;
            }

            UpdateGhostPath(pair.Key);
            UpdateGhostFootprint(pair.Key);
        }

        if (stale == null)
            return;

        foreach (Regiment regiment in stale)
            ClearOrderGhost(regiment);
    }

    private void UpdateGhostPath(Regiment regiment)
    {
        if (regiment == null ||
            !routes.TryGetValue(regiment, out MovementRoute route) ||
            !orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost) ||
            ghost.Path == null)
        {
            return;
        }

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
        if (regiment == null ||
            !routes.TryGetValue(regiment, out MovementRoute route) ||
            route.Waypoints.Count == 0 ||
            !orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost) ||
            ghost.Footprint == null)
        {
            return;
        }

        Vector3 center = route.Waypoints[route.Waypoints.Count - 1];
        DrawFootprint(
            ghost.Footprint,
            center,
            route.FinalFacing,
            route.FinalFormation,
            regiment.transform.forward);
    }

    private void ClearRoute(Regiment regiment)
    {
        if (regiment != null)
            routes.Remove(regiment);

        ClearOrderGhost(regiment);
    }

    private void ClearOrderGhost(Regiment regiment)
    {
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
            point.y =
                PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.12f;
            return true;
        }

        point = default;
        return false;
    }

    private void EnsureFormationPreview()
    {
        if (formationPreview != null && facingGuidePreview != null)
            return;

        GameObject previewRoot = new GameObject("FormationOrderPreview");
        previewRoot.transform.SetParent(transform, true);

        formationPreview = CreateGhostLine(
            previewRoot.transform,
            "DestinationPreview",
            0.30f,
            new Color(1f, 0.88f, 0.12f));

        facingGuidePreview = CreateGhostLine(
            previewRoot.transform,
            "FacingGuidePreview",
            0.15f,
            new Color(0.98f, 0.98f, 0.72f));

        formationPreview.enabled = false;
        facingGuidePreview.enabled = false;
    }

    private void UpdateFormationPreview()
    {
        EnsureFormationPreview();

        if (selected.Count == 0)
        {
            SetFormationPreviewVisible(false);
            return;
        }

        Vector3 delta = rightDragEnd - rightDragStart;
        delta.y = 0f;
        bool hasFacingDrag = delta.magnitude >= FormationDragThreshold;

        // Single regiment or Alt-waypoint: click position is the destination/waypoint.
        // While RMB is held, the yellow destination footprint itself rotates live.
        // Destination footprint is always deployed LINE, even if the regiment is
        // currently marching in COLUMN.
        if (selected.Count == 1 || rightDragAlt)
        {
            Regiment reference = selected[0];
            Vector3 fallbackFacing = reference != null
                ? reference.transform.forward
                : Vector3.forward;

            Vector3 facing = hasFacingDrag
                ? delta.normalized
                : NormalizedFacing(fallbackFacing, Vector3.forward);

            DrawFootprint(
                formationPreview,
                rightDragStart,
                facing,
                RegimentFormation.Line,
                fallbackFacing);

            if (hasFacingDrag)
            {
                Vector3 start = rightDragStart;
                start.y = PrototypeBootstrap.SampleGroundHeight(start.x, start.z) + 0.52f;

                Vector3 end = rightDragStart + facing * 11f;
                end.y = PrototypeBootstrap.SampleGroundHeight(end.x, end.z) + 0.52f;

                facingGuidePreview.positionCount = 2;
                facingGuidePreview.SetPosition(0, start);
                facingGuidePreview.SetPosition(1, end);
                facingGuidePreview.enabled = true;
            }
            else
            {
                facingGuidePreview.enabled = false;
            }

            formationPreview.enabled = true;
            return;
        }

        facingGuidePreview.enabled = false;

        if (delta.sqrMagnitude < 0.04f)
        {
            formationPreview.enabled = false;
            return;
        }

        // Several selected units: drag defines the deployment line.
        Vector3 direction = delta.normalized;
        Vector3 midpoint = (rightDragStart + rightDragEnd) * 0.5f;
        float requiredLength =
            Mathf.Max(delta.magnitude, Mathf.Max(0, selected.Count - 1) * RegimentLineSpacing);

        Vector3 startLine = midpoint - direction * requiredLength * 0.5f;
        Vector3 endLine = midpoint + direction * requiredLength * 0.5f;

        const int lineSegments = 24;
        formationPreview.positionCount = lineSegments + 1;

        for (int i = 0; i <= lineSegments; i++)
        {
            float t = i / (float)lineSegments;
            Vector3 p = Vector3.Lerp(startLine, endLine, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
            formationPreview.SetPosition(i, p);
        }

        formationPreview.enabled = true;
    }

    private static void DrawFootprint(
        LineRenderer line,
        Vector3 center,
        Vector3 facing,
        RegimentFormation formation,
        Vector3 fallbackFacing)
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
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.50f;
            line.SetPosition(i, p);
        }
    }

    private void SetFormationPreviewVisible(bool visible)
    {
        if (formationPreview != null)
            formationPreview.enabled = visible;
        if (facingGuidePreview != null)
            facingGuidePreview.enabled = visible && facingGuidePreview.positionCount > 0;
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
        {
            if (regiment != null)
                regiment.SetSelected(false);
        }

        selected.Clear();
    }
}
