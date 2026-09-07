using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerCommander : MonoBehaviour
{
    private sealed class OrderGhostVisual
    {
        public Vector3 Destination;
        public Vector3 Facing;
        public RegimentFormation Formation;
        public LineRenderer Path;
        public LineRenderer Footprint;
    }

    private struct PendingFacingOrder
    {
        public Vector3 Position;
        public Vector3 Facing;
        public float ExpiresAt;
    }

    public static PlayerCommander Instance { get; private set; }

    private readonly List<Regiment> selected = new List<Regiment>();
    private readonly Dictionary<Regiment, PendingFacingOrder> pendingFacingOrders = new Dictionary<Regiment, PendingFacingOrder>();
    private readonly Dictionary<Regiment, OrderGhostVisual> orderGhosts = new Dictionary<Regiment, OrderGhostVisual>();

    private Camera cam;
    private bool formationDragActive;
    private Vector3 formationDragStart;
    private Vector3 formationDragEnd;
    private LineRenderer formationPreview;

    private const float FormationDragThreshold = 4f;
    private const float RegimentLineSpacing = 22f;
    private const float FacingStepDegrees = 15f;

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

        UpdatePendingFacingOrders();
        UpdateOrderGhosts();

        bool pointerOverSimulationControls = BattleManager.Instance != null &&
                                             BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(0))
            HandleSelection();

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(1))
            BeginRightMouseOrder();

        if (formationDragActive && Input.GetMouseButton(1))
            UpdateFormationDrag();

        if (formationDragActive && Input.GetMouseButtonUp(1))
            CompleteFormationDrag();

        if (Input.GetKeyDown(KeyCode.H))
        {
            ForEachSelected(regiment =>
            {
                ClearOrderGhost(regiment);
                pendingFacingOrders.Remove(regiment);

                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null && controller.AIEnabled)
                    controller.SetHoldMission();
                else
                    regiment.OrderHold();
            });
        }

        if (Input.GetKeyDown(KeyCode.F))
            ForEachSelected(r => r.SetFormation(RegimentFormation.Line));

        if (Input.GetKeyDown(KeyCode.C))
            ForEachSelected(r => r.SetFormation(RegimentFormation.Column));

        if (Input.GetKeyDown(KeyCode.T))
        {
            ForEachSelected(r =>
            {
                r.ShowRange = !r.ShowRange;
                r.RefreshRangeVisibility();
            });
        }

        if (Input.GetKeyDown(KeyCode.Z))
            RotateSelectedFacing(-FacingStepDegrees);

        if (Input.GetKeyDown(KeyCode.X))
            RotateSelectedFacing(FacingStepDegrees);
    }

    public void RotateSelectedFacing(float degrees)
    {
        ForEachSelected(regiment => RotateRegimentFacing(regiment, degrees));
    }

    private void RotateRegimentFacing(Regiment regiment, float degrees)
    {
        if (regiment == null || regiment.IsRouted)
            return;

        if (orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost))
        {
            ghost.Facing = Quaternion.Euler(0f, degrees, 0f) * ghost.Facing;
            ghost.Facing.y = 0f;
            if (ghost.Facing.sqrMagnitude < 0.01f)
                ghost.Facing = regiment.transform.forward;
            ghost.Facing.Normalize();

            if (pendingFacingOrders.TryGetValue(regiment, out PendingFacingOrder pending))
            {
                pending.Facing = ghost.Facing;
                pendingFacingOrders[regiment] = pending;
            }

            UpdateGhostFootprint(regiment, ghost);
            return;
        }

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetHoldMission();
        else
            regiment.OrderHold();

        regiment.transform.rotation = Quaternion.Euler(0f, degrees, 0f) * regiment.transform.rotation;
    }

    private void HandleSelection()
    {
        bool additive =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 900f))
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

        Regiment enemy = GetEnemyUnderMouse();
        if (enemy != null)
        {
            IssueAttackOrder(enemy);
            return;
        }

        if (!TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            return;

        formationDragActive = true;
        formationDragStart = point;
        formationDragEnd = point;
        UpdateFormationPreview();
    }

    private void UpdateFormationDrag()
    {
        if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            formationDragEnd = point;

        UpdateFormationPreview();
    }

    private void CompleteFormationDrag()
    {
        formationDragActive = false;
        SetFormationPreviewVisible(false);

        Vector3 delta = formationDragEnd - formationDragStart;
        delta.y = 0f;

        if (delta.magnitude >= FormationDragThreshold)
        {
            IssueFormationLineOrder(formationDragStart, formationDragEnd);
            return;
        }

        IssueSimpleMoveOrder(formationDragEnd);
    }

    private void IssueAttackOrder(Regiment target)
    {
        if (target == null)
            return;

        ForEachSelected(regiment =>
        {
            pendingFacingOrders.Remove(regiment);
            ClearOrderGhost(regiment);

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAttackMission(target);
            else
                regiment.OrderAttack(target);
        });
    }

    private void IssueSimpleMoveOrder(Vector3 basePoint)
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
            Vector3 point = basePoint + right * offset;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;

            Vector3 facing = point - regiment.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f)
                facing = regiment.transform.forward;
            facing.Normalize();

            pendingFacingOrders[regiment] = new PendingFacingOrder
            {
                Position = point,
                Facing = facing,
                ExpiresAt = Time.unscaledTime + 120f
            };

            CreateOrUpdateOrderGhost(regiment, point, facing, regiment.Formation);

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetMoveMission(point);
            else
                regiment.OrderMove(point);
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
        float requiredLength = Mathf.Max(requestedLength, (ordered.Count - 1) * RegimentLineSpacing);

        Vector3 start = midpoint - lineDirection * requiredLength * 0.5f;
        Vector3 end = midpoint + lineDirection * requiredLength * 0.5f;

        Vector3 facing = Vector3.Cross(Vector3.up, lineDirection).normalized;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;

        for (int i = 0; i < ordered.Count; i++)
        {
            Regiment regiment = ordered[i];
            float t = ordered.Count == 1 ? 0.5f : i / (float)(ordered.Count - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;

            regiment.SetFormation(RegimentFormation.Line);

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetMoveMission(point);
            else
                regiment.OrderMove(point);

            pendingFacingOrders[regiment] = new PendingFacingOrder
            {
                Position = point,
                Facing = facing,
                ExpiresAt = Time.unscaledTime + 120f
            };

            CreateOrUpdateOrderGhost(regiment, point, facing, RegimentFormation.Line);
        }
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

    private void UpdatePendingFacingOrders()
    {
        if (pendingFacingOrders.Count == 0)
            return;

        List<Regiment> completed = null;

        foreach (KeyValuePair<Regiment, PendingFacingOrder> pair in pendingFacingOrders)
        {
            Regiment regiment = pair.Key;
            PendingFacingOrder order = pair.Value;

            bool remove = regiment == null || Time.unscaledTime >= order.ExpiresAt;

            if (!remove)
            {
                Vector3 delta = regiment.transform.position - order.Position;
                delta.y = 0f;

                if (delta.sqrMagnitude <= 1.8f * 1.8f)
                {
                    regiment.SetFormation(regiment.Formation);

                    if (order.Facing.sqrMagnitude > 0.01f)
                        regiment.transform.rotation = Quaternion.LookRotation(order.Facing, Vector3.up);

                    remove = true;
                }
            }

            if (remove)
            {
                if (completed == null)
                    completed = new List<Regiment>();
                completed.Add(regiment);
            }
        }

        if (completed == null)
            return;

        foreach (Regiment regiment in completed)
        {
            pendingFacingOrders.Remove(regiment);
            ClearOrderGhost(regiment);
        }
    }

    private void CreateOrUpdateOrderGhost(Regiment regiment, Vector3 destination, Vector3 facing, RegimentFormation formation)
    {
        if (regiment == null)
            return;

        if (!orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost))
        {
            GameObject root = new GameObject(regiment.RegimentName + "_OrderGhost");
            root.transform.SetParent(transform, true);

            ghost = new OrderGhostVisual
            {
                Path = CreateGhostLine(root.transform, "Path", 0.12f, new Color(0.72f, 0.88f, 1f)),
                Footprint = CreateGhostLine(root.transform, "Destination", 0.22f, new Color(0.98f, 0.88f, 0.18f))
            };
            orderGhosts[regiment] = ghost;
        }

        ghost.Destination = destination;
        ghost.Facing = facing.sqrMagnitude > 0.01f ? facing.normalized : regiment.transform.forward;
        ghost.Formation = formation;

        UpdateGhostPath(regiment, ghost);
        UpdateGhostFootprint(regiment, ghost);
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
            if (pair.Key == null)
            {
                if (stale == null)
                    stale = new List<Regiment>();
                stale.Add(pair.Key);
                continue;
            }

            UpdateGhostPath(pair.Key, pair.Value);
            UpdateGhostFootprint(pair.Key, pair.Value);
        }

        if (stale != null)
        {
            foreach (Regiment regiment in stale)
                ClearOrderGhost(regiment);
        }
    }

    private void UpdateGhostPath(Regiment regiment, OrderGhostVisual ghost)
    {
        if (regiment == null || ghost == null || ghost.Path == null)
            return;

        Vector3 start = regiment.transform.position;
        Vector3 end = ghost.Destination;
        const int segments = 18;
        ghost.Path.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.38f;
            ghost.Path.SetPosition(i, p);
        }
    }

    private void UpdateGhostFootprint(Regiment regiment, OrderGhostVisual ghost)
    {
        if (regiment == null || ghost == null || ghost.Footprint == null)
            return;

        Vector3 forward = ghost.Facing;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfWidth = ghost.Formation == RegimentFormation.Line ? 9.5f : 3.0f;
        float halfDepth = ghost.Formation == RegimentFormation.Line ? 2.6f : 7.5f;

        Vector3 center = ghost.Destination;
        Vector3[] corners =
        {
            center - right * halfWidth - forward * halfDepth,
            center + right * halfWidth - forward * halfDepth,
            center + right * halfWidth + forward * halfDepth,
            center - right * halfWidth + forward * halfDepth,
            center - right * halfWidth - forward * halfDepth
        };

        ghost.Footprint.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p = corners[i];
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.42f;
            ghost.Footprint.SetPosition(i, p);
        }
    }

    private void ClearOrderGhost(Regiment regiment)
    {
        if (!orderGhosts.TryGetValue(regiment, out OrderGhostVisual ghost))
            return;

        if (ghost.Path != null)
            Destroy(ghost.Path.gameObject);
        if (ghost.Footprint != null)
            Destroy(ghost.Footprint.gameObject);

        orderGhosts.Remove(regiment);
    }

    private Regiment GetEnemyUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 900f);
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
        RaycastHit[] hits = Physics.RaycastAll(ray, 900f);
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
        if (formationPreview != null)
            return;

        GameObject previewObject = new GameObject("FormationLinePreview");
        formationPreview = previewObject.AddComponent<LineRenderer>();
        formationPreview.useWorldSpace = true;
        formationPreview.loop = false;
        formationPreview.widthMultiplier = 0.28f;
        formationPreview.numCapVertices = 2;
        formationPreview.numCornerVertices = 2;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "FormationLinePreviewMaterial",
            color = new Color(0.98f, 0.88f, 0.18f)
        };
        formationPreview.sharedMaterial = material;
        formationPreview.enabled = false;
    }

    private void UpdateFormationPreview()
    {
        EnsureFormationPreview();

        Vector3 line = formationDragEnd - formationDragStart;
        line.y = 0f;
        if (line.sqrMagnitude < 0.01f)
        {
            SetFormationPreviewVisible(false);
            return;
        }

        Vector3 direction = line.normalized;
        Vector3 midpoint = (formationDragStart + formationDragEnd) * 0.5f;
        float requiredLength = Mathf.Max(line.magnitude, Mathf.Max(0, selected.Count - 1) * RegimentLineSpacing);
        Vector3 start = midpoint - direction * requiredLength * 0.5f;
        Vector3 end = midpoint + direction * requiredLength * 0.5f;

        const int segments = 24;
        formationPreview.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.42f;
            formationPreview.SetPosition(i, p);
        }

        SetFormationPreviewVisible(true);
    }

    private void SetFormationPreviewVisible(bool visible)
    {
        if (formationPreview != null)
            formationPreview.enabled = visible;
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
