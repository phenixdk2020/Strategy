using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29g
// Unified officer point-order input.
// Click places the command objective. Hold and drag from that point to define the
// direction the formation must face. A simple click keeps the existing automatic
// threat-facing behaviour. Used by both Major and Oberstløjtnant HUDs.
[DefaultExecutionOrder(900)]
public sealed class PrototypeOfficerFacingOrder09F29G : MonoBehaviour
{
    private enum CommandLevel
    {
        None,
        Battalion,
        Regiment
    }

    public static PrototypeOfficerFacingOrder09F29G Instance { get; private set; }
    public bool HasPendingOrder => level != CommandLevel.None;

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DragFacingThreshold = 5f;
    private const int CircleSamples = 72;

    private CommandLevel level;
    private int battalionIndex = -1;
    private MajorOrder09F18 order = MajorOrder09F18.None;
    private bool anchorPlaced;
    private Vector3 anchor;
    private Vector3 dragPoint;
    private Camera cam;

    private FieldInfo battalionsField;
    private FieldInfo majorMouseTrackedField;
    private FieldInfo majorDraggingField;
    private FieldInfo majorPendingField;
    private FieldInfo regimentMouseTrackedField;
    private FieldInfo regimentDraggingField;
    private FieldInfo regimentPendingField;
    private MethodInfo cancelWithdrawalMethod;

    private LineRenderer circle;
    private LineRenderer arrow;
    private LineRenderer arrowLeft;
    private LineRenderer arrowRight;
    private Material visualMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeOfficerFacingOrder09F29G>() == null)
            new GameObject("PrototypeOfficerFacingOrder_v000009f29g")
                .AddComponent<PrototypeOfficerFacingOrder09F29G>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", PrivateInstance);
        majorMouseTrackedField = typeof(PrototypeRegimentHierarchy09F27).GetField("mouseTracked", PrivateInstance);
        majorDraggingField = typeof(PrototypeRegimentHierarchy09F27).GetField("dragging", PrivateInstance);
        majorPendingField = typeof(PrototypeRegimentHierarchy09F27).GetField("pendingOrder", PrivateInstance);
        regimentMouseTrackedField = typeof(PrototypeRegimentalHQ09F28).GetField("mouseTracked", PrivateInstance);
        regimentDraggingField = typeof(PrototypeRegimentalHQ09F28).GetField("dragging", PrivateInstance);
        regimentPendingField = typeof(PrototypeRegimentalHQ09F28).GetField("pendingTargetOrder", PrivateInstance);
        cancelWithdrawalMethod = typeof(PrototypeFightingWithdrawal09F10).GetMethod("Cancel", PrivateInstance);

        CreateVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BeginBattalionOrder(int index, MajorOrder09F18 requestedOrder)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || index < 0 || index >= hierarchy.BattalionCount)
            return;

        if (requestedOrder == MajorOrder09F18.HoldPosition)
        {
            CancelWithdrawalsForBattalion(index);
            hierarchy.IssueBattalionOrder(index, requestedOrder, hierarchy.GetBattalionCenter(index), false);
            CancelPending();
            return;
        }

        level = CommandLevel.Battalion;
        battalionIndex = index;
        order = requestedOrder;
        anchorPlaced = false;
        ClearLegacyPending();
        SetVisualColor(requestedOrder);
        Debug.Log("OFFICER-FACING-09F29G|Pending=True|Level=BATTALION|Battalion=" + (index + 1) + "|Order=" + requestedOrder);
    }

    public void BeginRegimentalOrder(MajorOrder09F18 requestedOrder)
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed)
            return;

        if (requestedOrder == MajorOrder09F18.HoldPosition)
        {
            CancelWithdrawalsForBattalion(0);
            CancelWithdrawalsForBattalion(1);
            Vector3 p = regimental.HqRoot != null ? regimental.HqRoot.transform.position : Vector3.zero;
            regimental.IssueRegimentalOrder(requestedOrder, p, false);
            CancelPending();
            return;
        }

        level = CommandLevel.Regiment;
        battalionIndex = -1;
        order = requestedOrder;
        anchorPlaced = false;
        ClearLegacyPending();
        SetVisualColor(requestedOrder);
        Debug.Log("OFFICER-FACING-09F29G|Pending=True|Level=REGIMENT|Order=" + requestedOrder);
    }

    public void CancelPending()
    {
        level = CommandLevel.None;
        battalionIndex = -1;
        order = MajorOrder09F18.None;
        anchorPlaced = false;
        SetVisuals(false);
        ClearLegacyPending();
    }

    private void Update()
    {
        if (level == CommandLevel.None)
        {
            SetVisuals(false);
            return;
        }

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        ClearLegacyPending();
        SuppressLegacySelectionDrag();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPending();
            return;
        }

        if (PointerOverBottomHud())
            return;

        if (!anchorPlaced)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 hover))
            {
                anchor = hover;
                DrawCircle(anchor, level == CommandLevel.Regiment ? 46f : 30f);
                circle.enabled = true;
            }
            else
            {
                circle.enabled = false;
            }

            if (Input.GetMouseButtonDown(0) && TryGetGround(Input.mousePosition, out Vector3 start))
            {
                anchor = start;
                dragPoint = start;
                anchorPlaced = true;
                DrawCircle(anchor, level == CommandLevel.Regiment ? 46f : 30f);
                circle.enabled = true;
            }

            arrow.enabled = arrowLeft.enabled = arrowRight.enabled = false;
            return;
        }

        if (Input.GetMouseButton(0) && TryGetGround(Input.mousePosition, out Vector3 current))
        {
            dragPoint = current;
            DrawArrow(anchor, dragPoint);
        }

        if (!Input.GetMouseButtonUp(0))
            return;

        if (TryGetGround(Input.mousePosition, out Vector3 end))
            dragPoint = end;

        Vector3 facing = dragPoint - anchor;
        facing.y = 0f;
        bool hasFacing = facing.magnitude >= DragFacingThreshold;
        if (hasFacing)
            facing.Normalize();

        CommitOrder(anchor, hasFacing ? (Vector3?)facing : null);
        CancelPending();
    }

    private void CommitOrder(Vector3 objective, Vector3? explicitFacing)
    {
        if (level == CommandLevel.Battalion)
        {
            PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
            if (hierarchy == null || !hierarchy.Installed)
                return;

            CancelWithdrawalsForBattalion(battalionIndex);
            hierarchy.IssueBattalionOrder(battalionIndex, order, objective, false);
            if (explicitFacing.HasValue)
                ApplyFacingToBattalion(battalionIndex, explicitFacing.Value);
        }
        else if (level == CommandLevel.Regiment)
        {
            PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
            if (regimental == null || !regimental.Installed)
                return;

            CancelWithdrawalsForBattalion(0);
            CancelWithdrawalsForBattalion(1);
            regimental.IssueRegimentalOrder(order, objective, false);
            if (explicitFacing.HasValue)
            {
                ApplyFacingToBattalion(0, explicitFacing.Value);
                ApplyFacingToBattalion(1, explicitFacing.Value);
            }
        }

        Debug.Log("OFFICER-FACING-09F29G|Committed=True|Level=" + level +
                  "|Order=" + order +
                  "|Objective=" + objective.x.ToString("0.0") + "," + objective.z.ToString("0.0") +
                  "|ExplicitFacing=" + (explicitFacing.HasValue ? "YES" : "AUTO") +
                  (explicitFacing.HasValue
                      ? "|Facing=" + explicitFacing.Value.x.ToString("0.00") + "," + explicitFacing.Value.z.ToString("0.00")
                      : string.Empty));
    }

    private void ApplyFacingToBattalion(int index, Vector3 facing)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null || index < 0 || index >= battalions.Count)
            return;

        object battalion = battalions[index];
        FieldInfo missionsField = battalion.GetType().GetField("Missions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        IDictionary missions = missionsField != null ? missionsField.GetValue(battalion) as IDictionary : null;
        if (missions == null)
            return;

        foreach (DictionaryEntry entry in missions)
        {
            object mission = entry.Value;
            if (mission == null)
                continue;
            FieldInfo facingField = mission.GetType().GetField("Facing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (facingField != null)
                facingField.SetValue(mission, facing);
        }
    }

    private void CancelWithdrawalsForBattalion(int index)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeFightingWithdrawal09F10 withdrawal = PrototypeFightingWithdrawal09F10.Instance;
        if (hierarchy == null || withdrawal == null || cancelWithdrawalMethod == null ||
            index < 0 || index >= hierarchy.BattalionCount)
            return;

        IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(index);
        if (companies == null)
            return;

        for (int i = 0; i < companies.Count; i++)
        {
            Regiment unit = companies[i];
            if (unit != null && withdrawal.IsWithdrawing(unit))
                cancelWithdrawalMethod.Invoke(withdrawal, new object[] { unit, true, "HIGHER_COMMAND_NEW_ORDER" });
        }
    }

    private void ClearLegacyPending()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && majorPendingField != null)
            majorPendingField.SetValue(hierarchy, MajorOrder09F18.None);

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimentPendingField != null)
            regimentPendingField.SetValue(regimental, MajorOrder09F18.None);
    }

    private void SuppressLegacySelectionDrag()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
        {
            if (majorMouseTrackedField != null) majorMouseTrackedField.SetValue(hierarchy, false);
            if (majorDraggingField != null) majorDraggingField.SetValue(hierarchy, false);
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null)
        {
            if (regimentMouseTrackedField != null) regimentMouseTrackedField.SetValue(regimental, false);
            if (regimentDraggingField != null) regimentDraggingField.SetValue(regimental, false);
        }
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 8000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.12f;
            return true;
        }
        return false;
    }

    private static bool PointerOverBottomHud()
    {
        return Input.mousePosition.y <= 96f;
    }

    private void CreateVisuals()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        visualMaterial = new Material(shader) { name = "OfficerFacing09F29G", color = PrototypeUiTheme09F15.Move };

        circle = CreateLine("OfficerFacingCircle09F29G", true, 0.34f);
        arrow = CreateLine("OfficerFacingArrow09F29G", false, 0.38f);
        arrowLeft = CreateLine("OfficerFacingArrowLeft09F29G", false, 0.38f);
        arrowRight = CreateLine("OfficerFacingArrowRight09F29G", false, 0.38f);
        SetVisuals(false);
    }

    private LineRenderer CreateLine(string name, bool loop, float width)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sharedMaterial = visualMaterial;
        line.enabled = false;
        return line;
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        circle.positionCount = CircleSamples;
        for (int i = 0; i < CircleSamples; i++)
        {
            float angle = i / (float)CircleSamples * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.92f;
            circle.SetPosition(i, p);
        }
    }

    private void DrawArrow(Vector3 start, Vector3 end)
    {
        Vector3 flat = end - start;
        flat.y = 0f;
        if (flat.magnitude < DragFacingThreshold)
        {
            arrow.enabled = arrowLeft.enabled = arrowRight.enabled = false;
            return;
        }

        Vector3 dir = flat.normalized;
        Vector3 tip = end;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        float head = Mathf.Clamp(flat.magnitude * 0.22f, 6f, 18f);
        Vector3 basePoint = tip - dir * head;
        Vector3 left = basePoint + side * head * 0.55f;
        Vector3 right = basePoint - side * head * 0.55f;

        SetTwoPointLine(arrow, start, tip);
        SetTwoPointLine(arrowLeft, tip, left);
        SetTwoPointLine(arrowRight, tip, right);
        arrow.enabled = arrowLeft.enabled = arrowRight.enabled = true;
    }

    private static void SetTwoPointLine(LineRenderer line, Vector3 a, Vector3 b)
    {
        line.positionCount = 2;
        a.y = PrototypeBootstrap.SampleGroundHeight(a.x, a.z) + 1.02f;
        b.y = PrototypeBootstrap.SampleGroundHeight(b.x, b.z) + 1.02f;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
    }

    private void SetVisualColor(MajorOrder09F18 value)
    {
        if (visualMaterial == null)
            return;
        switch (value)
        {
            case MajorOrder09F18.AttackHere: visualMaterial.color = PrototypeUiTheme09F15.Attack; break;
            case MajorOrder09F18.DefendHere: visualMaterial.color = PrototypeUiTheme09F15.Defend; break;
            case MajorOrder09F18.WithdrawHere: visualMaterial.color = PrototypeUiTheme09F15.Withdraw; break;
            default: visualMaterial.color = PrototypeUiTheme09F15.Move; break;
        }
    }

    private void SetVisuals(bool value)
    {
        if (circle != null) circle.enabled = value;
        if (arrow != null) arrow.enabled = value;
        if (arrowLeft != null) arrowLeft.enabled = value;
        if (arrowRight != null) arrowRight.enabled = value;
    }
}
