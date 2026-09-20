using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f28
// Physical Regimental HQ / Oberstløjtnant command layer.
// The regiment commander issues mission intent to the two Majors; the Majors then
// translate that intent into company slots through the shared F27 battalion core.
[DefaultExecutionOrder(780)]
public sealed class PrototypeRegimentalHQ09F28 : MonoBehaviour
{
    private sealed class PendingRegimentalOrder
    {
        public MajorOrder09F18 Order;
        public Vector3 Objective;
        public Vector3 Facing;
        public bool Autonomous;
    }

    public static PrototypeRegimentalHQ09F28 Instance { get; private set; }
    public bool Installed { get; private set; }
    public bool Selected { get; private set; }
    public bool AIEnabled { get; private set; }
    public OfficerAIDoctrine Doctrine { get; private set; } = OfficerAIDoctrine.Balanced;
    public GameObject HqRoot { get; private set; }

    private PrototypeRegimentHierarchy09F27 hierarchy;
    private Camera cam;
    private MajorOrder09F18 pendingTargetOrder = MajorOrder09F18.None;
    private PendingRegimentalOrder currentMission;
    private string lastOrderText = "Ingen regimentsordre";
    private string lastDecisionText = "Bataljonsroller: ikke vurderet";
    private bool awaitHigherMission;
    private float nextThink;
    private bool hasHqGoal;
    private Vector3 hqGoal;

    private FieldInfo playerSelectedField;

    private LineRenderer selectionRing;
    private LineRenderer commandInnerRing;
    private LineRenderer commandOuterRing;
    private readonly List<LineRenderer> majorLinks = new List<LineRenderer>();

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;
    private GUIStyle hoverStyle;

    private bool mouseTracked;
    private bool dragging;
    private Vector2 dragStart;
    private Vector2 dragCurrent;

    private const float HudHeight = 90f;
    private const float DragThreshold = 9f;
    private const float BattalionLateral = 170f;
    private const float BattalionReserveDepth = 285f;
    private const float BattalionFlankOffset = 285f;
    private const float RegimentalHqSpeed = 7.0f;
    private const float RegimentalHqBehind = 360f;
    private const float RegimentalRelocateThreshold = 470f;
    private const float RegimentCommandInner = 800f;
    private const float RegimentCommandOuter = 1100f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalHQ09F28>() == null)
            new GameObject("PrototypeRegimentalHQ_v000009f28").AddComponent<PrototypeRegimentalHQ09F28>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerSelectedField = typeof(PlayerCommander).GetField(
            "selected", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (!Installed)
        {
            TryInstall();
            return;
        }

        UpdateRegimentalAI();
        UpdateDynamicHq();
        UpdateWorldVisuals();
        HandleWorldInput();
    }

    private void TryInstall()
    {
        hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || hierarchy.BattalionCount < 2)
            return;

        HqRoot = CreateRegimentalHq(new Vector3(-630f, 0f, 0f));
        CreateWorldVisuals();
        Installed = true;

        Debug.Log(
            "REG-HQ-09F28|Installed=True|Rank=Oberstløjtnant|Level=Regiment|" +
            "Battalions=2|Majors=2|Companies=8|AI=OFF|DirectCompanyOrders=False");
    }

    public void ToggleAI()
    {
        SetAIEnabled(!AIEnabled, "HUD_TOGGLE");
    }

    public void SetAIEnabled(bool enabled, string reason = "HIGHER_COMMAND")
    {
        AIEnabled = enabled;

        bool higherCascade =
            !string.IsNullOrEmpty(reason) &&
            reason.EndsWith("_CASCADE", System.StringComparison.Ordinal);

        // F30O authority rule:
        // Higher-HQ AI ON arms the regiment for delegated execution. It is NOT an
        // implicit mission and must not create a default attack/defence by itself.
        awaitHigherMission = enabled && higherCascade;

        if (awaitHigherMission)
        {
            hasHqGoal = false;
            lastOrderText = "AI klar | afventer ordre fra højere HQ";
            lastDecisionText = "Ingen bevægelse uden committed mission";
        }

        if (!enabled)
            awaitHigherMission = false;

        nextThink = Time.time + 0.25f;
        Debug.Log("REG-AI-09F30O|AI=" + (AIEnabled ? "ON" : "OFF") +
                  "|Doctrine=" + Doctrine +
                  "|AwaitHigherMission=" + awaitHigherMission +
                  "|Reason=" + reason);
    }

    public MajorOrder09F18 CurrentMissionOrder
    {
        get { return currentMission != null ? currentMission.Order : MajorOrder09F18.None; }
    }

    public Vector3 CurrentMissionFacing
    {
        get
        {
            if (currentMission != null)
            {
                Vector3 facing = Flat(currentMission.Facing);
                if (facing.sqrMagnitude > 0.01f)
                    return facing.normalized;
            }

            if (HqRoot != null)
            {
                Vector3 facing = Flat(HqRoot.transform.forward);
                if (facing.sqrMagnitude > 0.01f)
                    return facing.normalized;
            }

            return Vector3.forward;
        }
    }

    public bool HasActiveMissionExecutors(MajorOrder09F18 order)
    {
        return hierarchy != null && hierarchy.HasActiveMissionExecutors(order);
    }

    public bool IsMissionActive(MajorOrder09F18 order)
    {
        if (currentMission == null || currentMission.Order != order)
            return false;

        // F30S: active HUD state follows physical execution, including the
        // Regimental HQ still relocating behind its Majors.
        return hasHqGoal ||
               (hierarchy != null && hierarchy.HasActiveMissionExecutors(order));
    }

    public void SetDoctrine(OfficerAIDoctrine doctrine)
    {
        Doctrine = doctrine;
        nextThink = Time.time + 0.25f;
    }

    public void IssueRegimentalOrder(
        MajorOrder09F18 order,
        Vector3 point,
        bool autonomous = false,
        Vector3? explicitFacing = null)
    {
        if (!Installed || hierarchy == null || hierarchy.BattalionCount < 2)
            return;

        awaitHigherMission = false;

        if (order == MajorOrder09F18.HoldPosition)
        {
            for (int i = 0; i < 2; i++)
            {
                hierarchy.IssueBattalionOrderFromRegiment(
                    i,
                    MajorOrder09F18.HoldPosition,
                    hierarchy.GetBattalionCenter(i),
                    Doctrine);
            }

            currentMission = new PendingRegimentalOrder
            {
                Order = order,
                Objective = GetRegimentCenter(),
                Facing = HqRoot != null ? Flat(HqRoot.transform.forward) : Vector3.forward,
                Autonomous = autonomous
            };
            lastOrderText = "HOLD | regimentet fastholder nuværende disposition";
            lastDecisionText = "Begge bataljoner: HOLD";
            return;
        }

        Vector3 regimentCenter = GetRegimentCenter();
        Vector3 forward = explicitFacing.HasValue
            ? Flat(explicitFacing.Value)
            : Vector3.zero;

        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(point - HqRoot.transform.position);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(point - regimentCenter);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(HqRoot.transform.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;

        List<Regiment> knownEnemies = KnownEnemies(point, 1200f);
        string disposition;
        Vector3 slotA;
        Vector3 slotB;

        if (order == MajorOrder09F18.AttackHere)
        {
            // F30Q: BAL commits both battalions to the attack front. Local
            // company reserves remain available under the Majors, avoiding the
            // previous over-reserve state where an entire battalion sat 285m back.
            bool useReserve = Doctrine == OfficerAIDoctrine.Defensive;
            bool useFlank = Doctrine == OfficerAIDoctrine.Offensive && knownEnemies.Count <= 3;

            if (useFlank)
            {
                slotA = Ground(point - lateral * 110f);
                slotB = Ground(point + lateral * BattalionFlankOffset - forward * 45f);
                disposition = "1 fremme + 1 flanke";
            }
            else if (useReserve)
            {
                slotA = Ground(point);
                slotB = Ground(point - forward * BattalionReserveDepth);
                disposition = "1 fremme + 1 regimentsreserve";
            }
            else
            {
                slotA = Ground(point - lateral * BattalionLateral);
                slotB = Ground(point + lateral * BattalionLateral);
                disposition = "2 bataljoner side om side";
            }
        }
        else if (order == MajorOrder09F18.AssembleHere)
        {
            slotA = Ground(point - lateral * 105f);
            slotB = Ground(point + lateral * 105f);
            disposition = "SAML | 2 bataljoner tæt samlet";
        }
        else
        {
            slotA = Ground(point - lateral * BattalionLateral);
            slotB = Ground(point + lateral * BattalionLateral);
            disposition = order == MajorOrder09F18.DefendHere
                ? "Forsvar | 2 bataljoner side om side"
                : order == MajorOrder09F18.WithdrawHere
                    ? "Tilbagetrækning | 2 bataljoner side om side"
                    : "Fremrykning | 2 bataljoner side om side";
        }

        // Assign the nearest Major to the nearest battalion objective to reduce crossing.
        GameObject major0 = hierarchy.GetMajorHq(0);
        GameObject major1 = hierarchy.GetMajorHq(1);
        float normalCost = PlanarDistance(major0.transform.position, slotA) +
                           PlanarDistance(major1.transform.position, slotB);
        float swappedCost = PlanarDistance(major0.transform.position, slotB) +
                            PlanarDistance(major1.transform.position, slotA);

        Vector3 goal0 = normalCost <= swappedCost ? slotA : slotB;
        Vector3 goal1 = normalCost <= swappedCost ? slotB : slotA;

        hierarchy.IssueBattalionOrderFromRegiment(0, order, goal0, Doctrine, forward);
        hierarchy.IssueBattalionOrderFromRegiment(1, order, goal1, Doctrine, forward);

        currentMission = new PendingRegimentalOrder
        {
            Order = order,
            Objective = point,
            Facing = forward,
            Autonomous = autonomous
        };

        lastOrderText = Label(order) + " | kendte fjender: " + knownEnemies.Count +
                        (autonomous ? " | AI" : " | SPILLER");
        lastDecisionText = "Bataljonsroller: " + disposition;

        Debug.Log("REG-ORDER-09F30P|Order=" + order +
                  "|Objective=" + point.x.ToString("0.0") + "," + point.z.ToString("0.0") +
                  "|ExplicitFacing=" + explicitFacing.HasValue +
                  "|Facing=" + forward.x.ToString("0.00") + "," + forward.z.ToString("0.00") +
                  "|KnownEnemies=" + knownEnemies.Count +
                  "|Disposition=" + disposition +
                  "|Major0Goal=" + goal0.x.ToString("0.0") + "," + goal0.z.ToString("0.0") +
                  "|Major1Goal=" + goal1.x.ToString("0.0") + "," + goal1.z.ToString("0.0") +
                  "|Authority=OBERSTLOJTNANT_TO_MAJORS");
    }

    private void UpdateRegimentalAI()
    {
        if (!AIEnabled || Time.time < nextThink)
            return;

        nextThink = Time.time + 8f;

        // Higher-HQ cascade only arms delegated AI. Do nothing until an explicit
        // Division/Brigade mission reaches the regiment.
        if (awaitHigherMission)
            return;

        if (currentMission != null)
            return; // explicit player/regimental mission remains intent; AI does not churn it every think cycle.

        Vector3 center = GetRegimentCenter();
        Regiment nearest = FindNearestEnemy(center, 1200f);

        if (Doctrine == OfficerAIDoctrine.Offensive && nearest != null)
            IssueRegimentalOrder(MajorOrder09F18.AttackHere, nearest.transform.position, true);
        else
            IssueRegimentalOrder(MajorOrder09F18.DefendHere, center, true);
    }

    private void UpdateDynamicHq()
    {
        if (HqRoot == null)
            return;

        if (hasHqGoal)
        {
            MoveHqToward(hqGoal);
            return;
        }

        if (!AIEnabled || awaitHigherMission || currentMission == null ||
            currentMission.Order == MajorOrder09F18.HoldPosition)
            return;

        GameObject major0 = hierarchy.GetMajorHq(0);
        GameObject major1 = hierarchy.GetMajorHq(1);
        if (major0 == null || major1 == null)
            return;

        Vector3 majorMid = (major0.transform.position + major1.transform.position) * 0.5f;
        Vector3 forward = Flat(currentMission.Facing);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(currentMission.Objective - majorMid);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(HqRoot.transform.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 desired = Ground(majorMid - forward * RegimentalHqBehind);

        if (PlanarDistance(HqRoot.transform.position, desired) < RegimentalRelocateThreshold)
            return;

        if (!IsSafeHqRelocation(HqRoot.transform.position, desired))
            return;

        hqGoal = desired;
        hasHqGoal = true;

        Debug.Log("REG-HQ-09F28|DynamicMove=True|Goal=" +
                  desired.x.ToString("0.0") + "," + desired.z.ToString("0.0"));
    }

    private void MoveHqToward(Vector3 goal)
    {
        Vector3 current = HqRoot.transform.position;
        Vector3 delta = goal - current;
        delta.y = 0f;

        if (delta.magnitude <= 3f)
        {
            HqRoot.transform.position = Ground(goal);
            hasHqGoal = false;
            return;
        }

        Vector3 step = delta.normalized * RegimentalHqSpeed * Time.deltaTime;
        if (step.magnitude > delta.magnitude)
            step = delta;

        HqRoot.transform.position = Ground(current + step);
        HqRoot.transform.rotation = Quaternion.Slerp(
            HqRoot.transform.rotation,
            Quaternion.LookRotation(delta.normalized, Vector3.up),
            2.6f * Time.deltaTime);
    }

    public void MoveRegimentalHq(Vector3 point)
    {
        AIEnabled = false;
        Vector3 goal = Ground(point);
        if (!IsSafeHqRelocation(HqRoot.transform.position, goal))
        {
            Debug.LogWarning("REG-HQ-09F28|ManualMove=False|Reason=BlockedOrRiver");
            return;
        }

        hqGoal = goal;
        hasHqGoal = true;
        Debug.Log("REG-AUTHORITY-09F28|Unit=OBERSTLOJTNANT|Authority=PLAYER|AI=OFF|MoveHQ=True");
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (!Selected)
            return false;
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight).Contains(gui);
    }

    private void HandleWorldInput()
    {
        if (cam == null)
            return;

        PrototypeOfficerFacingOrder09F29G facingOrder =
            PrototypeOfficerFacingOrder09F29G.Instance;
        if (facingOrder != null && facingOrder.HasPendingOrder)
        {
            mouseTracked = false;
            dragging = false;
            return;
        }

        bool overPanel = IsPointerOverControls(Input.mousePosition);

        if (Selected && pendingTargetOrder != MajorOrder09F18.None && Input.GetMouseButtonDown(0) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
            {
                IssueRegimentalOrder(pendingTargetOrder, point, false);
                pendingTargetOrder = MajorOrder09F18.None;
            }
            mouseTracked = false;
            dragging = false;
            return;
        }

        if (Selected && pendingTargetOrder == MajorOrder09F18.None && Input.GetMouseButtonDown(1) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
                MoveRegimentalHq(point);
        }

        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            mouseTracked = true;
            dragging = false;
            dragStart = Input.mousePosition;
            dragCurrent = dragStart;
        }

        if (mouseTracked && Input.GetMouseButton(0))
        {
            dragCurrent = Input.mousePosition;
            if (!dragging && Vector2.Distance(dragStart, dragCurrent) >= DragThreshold)
                dragging = true;
        }

        if (!mouseTracked || !Input.GetMouseButtonUp(0))
            return;

        bool hit = false;
        if (dragging)
        {
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(dragStart.x, dragCurrent.x), Mathf.Min(dragStart.y, dragCurrent.y),
                Mathf.Max(dragStart.x, dragCurrent.x), Mathf.Max(dragStart.y, dragCurrent.y));
            Vector3 screen = cam.WorldToScreenPoint(HqRoot.transform.position);
            hit = screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y), true);
        }
        else
        {
            hit = RayHitsHq(Input.mousePosition);
        }

        if (hit)
            SetSelected(true);
        else if (Selected && pendingTargetOrder == MajorOrder09F18.None)
            SetSelected(false);

        mouseTracked = false;
        dragging = false;
    }

    private void SetSelected(bool value)
    {
        Selected = value;
        if (!value)
        {
            pendingTargetOrder = MajorOrder09F18.None;
            return;
        }

        hierarchy.ClearMajorSelection();
        ClearCompanySelection();
    }

    private void ClearCompanySelection()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null || playerSelectedField == null)
            return;

        List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
        if (selected == null)
            return;

        foreach (Regiment regiment in selected)
            if (regiment != null)
                regiment.SetSelected(false);
        selected.Clear();
    }

    private bool RayHitsHq(Vector3 mousePosition)
    {
        if (cam == null || HqRoot == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            Transform t = hit.collider.transform;
            if (t == HqRoot.transform || t.IsChildOf(HqRoot.transform))
                return true;
        }
        return false;
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;
            point = Ground(hit.point);
            return true;
        }
        return false;
    }

    private void CreateWorldVisuals()
    {
        Material selection = CreateUnlit(new Color(1f, 0.80f, 0.18f, 0.96f), "REG28Selection");
        Material inner = CreateUnlit(new Color(0.35f, 0.68f, 0.88f, 0.45f), "REG28CommandInner");
        Material outer = CreateUnlit(new Color(0.82f, 0.52f, 0.18f, 0.40f), "REG28CommandOuter");
        Material link = CreateUnlit(new Color(0.48f, 0.78f, 0.95f, 0.88f), "REG28MajorLink");

        selectionRing = CreateRing("RegHQSelection09F28", 0.28f, selection);
        commandInnerRing = CreateRing("RegHQCommandInner09F28", 0.12f, inner);
        commandOuterRing = CreateRing("RegHQCommandOuter09F28", 0.12f, outer);

        for (int i = 0; i < 2; i++)
        {
            GameObject root = new GameObject("RegHQMajorLink_" + i);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.17f;
            line.sharedMaterial = link;
            line.enabled = false;
            majorLinks.Add(line);
        }
    }

    private void UpdateWorldVisuals()
    {
        if (HqRoot == null)
            return;

        selectionRing.enabled = Selected;
        commandInnerRing.enabled = Selected;
        commandOuterRing.enabled = Selected;

        if (Selected)
        {
            DrawCircle(selectionRing, HqRoot.transform.position, 8.5f, 0.36f);
            DrawCircle(commandInnerRing, HqRoot.transform.position, RegimentCommandInner, 0.23f);
            DrawCircle(commandOuterRing, HqRoot.transform.position, RegimentCommandOuter, 0.25f);
        }

        for (int i = 0; i < majorLinks.Count; i++)
        {
            GameObject major = hierarchy.GetMajorHq(i);
            LineRenderer line = majorLinks[i];
            line.enabled = Selected && major != null;
            if (!line.enabled)
                continue;

            line.SetPosition(0, GroundWithOffset(HqRoot.transform.position, 0.55f));
            line.SetPosition(1, GroundWithOffset(major.transform.position, 0.55f));
        }
    }

    private GameObject CreateRegimentalHq(Vector3 position)
    {
        GameObject hq = new GameObject("DK_Regimental_HQ_Oberstlojtnant");
        hq.transform.position = Ground(position);
        hq.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        BoxCollider box = hq.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.9f, 0f);
        box.size = new Vector3(15f, 5f, 12f);

        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.25f, 0.15f, 0.09f), "RegHQHorse09F28");
        Material officer = PrototypeBootstrap.CreateSharedMaterial(new Color(0.06f, 0.13f, 0.24f), "RegHQOfficer09F28");
        Material leather = PrototypeBootstrap.CreateSharedMaterial(new Color(0.10f, 0.06f, 0.035f), "RegHQLeather09F28");
        Material brass = PrototypeBootstrap.CreateSharedMaterial(new Color(0.76f, 0.62f, 0.22f), "RegHQBrass09F28");

        CreateHorse(hq.transform, new Vector3(0f, 0f, 1.0f), 0f, true, horse, officer, leather, brass);
        CreateHorse(hq.transform, new Vector3(-3.4f, 0f, -1.6f), 7f, false, horse, officer, leather, brass);
        CreateHorse(hq.transform, new Vector3(0f, 0f, -2.2f), 0f, false, horse, officer, leather, brass);
        CreateHorse(hq.transform, new Vector3(3.4f, 0f, -1.6f), -7f, false, horse, officer, leather, brass);

        return hq;
    }

    private static void CreateHorse(Transform parent, Vector3 localPosition, float yaw, bool commander,
        Material horse, Material officer, Material leather, Material brass)
    {
        GameObject root = new GameObject(commander ? "OberstlojtnantHorse" : "RegHQHorse");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreatePart(root.transform, PrimitiveType.Cube, "HorseBody", new Vector3(0f, 1.05f, 0f), new Vector3(0.72f, 0.68f, 1.70f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseNeck", new Vector3(0f, 1.52f, 0.72f), new Vector3(0.42f, 0.92f, 0.42f), Quaternion.Euler(-22f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseHead", new Vector3(0f, 1.92f, 1.10f), new Vector3(0.42f, 0.46f, 0.68f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Saddle", new Vector3(0f, 1.46f, -0.05f), new Vector3(0.78f, 0.18f, 0.78f), Quaternion.identity, leather);

        if (!commander)
            return;

        CreatePart(root.transform, PrimitiveType.Capsule, "OberstlojtnantBody", new Vector3(0f, 2.20f, -0.05f), new Vector3(0.36f, 0.50f, 0.36f), Quaternion.identity, officer);
        CreatePart(root.transform, PrimitiveType.Sphere, "OberstlojtnantHead", new Vector3(0f, 2.94f, -0.02f), new Vector3(0.29f, 0.33f, 0.29f), Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cylinder, "OberstlojtnantCap", new Vector3(0f, 3.20f, -0.02f), new Vector3(0.31f, 0.08f, 0.31f), Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cube, "OfficerSash", new Vector3(0.18f, 2.30f, -0.24f), new Vector3(0.08f, 0.72f, 0.06f), Quaternion.Euler(0f, 0f, 24f), brass);
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, string name,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        return part;
    }

    private Vector3 GetRegimentCenter()
    {
        return (hierarchy.GetBattalionCenter(0) + hierarchy.GetBattalionCenter(1)) * 0.5f;
    }

    private List<Regiment> KnownEnemies(Vector3 point, float range)
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team == BattleTeam.Denmark || regiment.IsRouted || regiment.CurrentStrength <= 0)
                continue;
            if (PlanarDistance(point, regiment.transform.position) <= range)
                result.Add(regiment);
        }
        return result;
    }

    private Regiment FindNearestEnemy(Vector3 point, float range)
    {
        Regiment nearest = null;
        float best = range;
        foreach (Regiment regiment in KnownEnemies(point, range))
        {
            float d = PlanarDistance(point, regiment.transform.position);
            if (d < best)
            {
                best = d;
                nearest = regiment;
            }
        }
        return nearest;
    }

    private static bool IsSafeHqRelocation(Vector3 current, Vector3 goal)
    {
        if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(goal, goal - current))
            return false;
        int a = BankSide(current);
        int b = BankSide(goal);
        return !(a != 0 && b != 0 && a != b);
    }

    private static int BankSide(Vector3 point)
    {
        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        float delta = point.x - riverX;
        if (Mathf.Abs(delta) < 4f)
            return 0;
        return delta < 0f ? -1 : 1;
    }

    private LineRenderer CreateRing(string name, float width, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = width;
        line.positionCount = 72;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static void DrawCircle(LineRenderer line, Vector3 center, float radius, float yOffset)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static Material CreateUnlit(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static string Label(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB HER";
            case MajorOrder09F18.DefendHere: return "FORSVAR HER";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.AssembleHere: return "SAML";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            default: return "INGEN";
        }
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static Vector3 GroundWithOffset(Vector3 point, float offset)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + offset;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
        hoverStyle = PrototypeUiTheme09F15.Panel(9);
        hoverStyle.alignment = TextAnchor.UpperLeft;
    }

    private void OnGUI()
    {
        if (!Installed)
            return;

        EnsureStyles();

        if (!Selected && cam != null && RayHitsHq(Input.mousePosition))
        {
            Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Rect hover = new Rect(
                Mathf.Clamp(p.x + 15f, 8f, Screen.width - 300f),
                Mathf.Clamp(p.y - 20f, 38f, Screen.height - 96f), 292f, 86f);
            GUI.Box(hover,
                "OBERSTLØJTNANT | REGIMENTS HQ\n" +
                "AI: " + (AIEnabled ? "ON" : "OFF") + " | " + Doctrine + " | 4 heste | 2 bataljoner\n" +
                lastOrderText + "\n" + lastDecisionText,
                hoverStyle);
        }

        if (!Selected)
            return;

        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.depth = -900;
        GUI.Box(panel, string.Empty, panelStyle);

        float pad = 7f;
        float headerY = panel.y + 3f;
        GUI.Box(new Rect(pad, headerY, panel.width - pad * 2f, 19f),
            "REGIMENTS HQ | OBERSTLØJTNANT | 2 BATALJONER | 8 KOMPAGNIER", headerStyle);

        float aiX = panel.xMax - 274f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 70f, 17f), AIEnabled ? "AI ON" : "AI OFF",
                AIEnabled ? accentStyle : buttonStyle))
            ToggleAI();
        aiX += 73f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF", buttonStyle)) SetDoctrine(OfficerAIDoctrine.Defensive);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL", buttonStyle)) SetDoctrine(OfficerAIDoctrine.Balanced);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF", buttonStyle)) SetDoctrine(OfficerAIDoctrine.Offensive);

        float infoWidth = Mathf.Clamp(panel.width * 0.33f, 390f, 560f);
        float rowY = panel.y + 27f;
        GUI.Label(new Rect(pad + 3f, rowY, infoWidth - 8f, 15f), lastOrderText, labelStyle);
        GUI.Label(new Rect(pad + 3f, rowY + 16f, infoWidth - 8f, 15f), lastDecisionText, mutedStyle);

        float commandX = infoWidth + 8f;
        float commandWidth = panel.width - commandX - 8f;
        const float gap = 4f;
        float buttonWidth = (commandWidth - gap * 5f) / 6f;
        const float commandHeight = 27f;

        if (GUI.Button(new Rect(commandX, rowY, buttonWidth, commandHeight), "ANGRIB HER", buttonStyle)) pendingTargetOrder = MajorOrder09F18.AttackHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap), rowY, buttonWidth, commandHeight), "FORSVAR HER", buttonStyle)) pendingTargetOrder = MajorOrder09F18.DefendHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, rowY, buttonWidth, commandHeight), "RYK FREM", buttonStyle)) pendingTargetOrder = MajorOrder09F18.AdvanceHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 3f, rowY, buttonWidth, commandHeight), "TILBAGETRÆK", buttonStyle)) pendingTargetOrder = MajorOrder09F18.WithdrawHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 4f, rowY, buttonWidth, commandHeight), "SAML", buttonStyle)) pendingTargetOrder = MajorOrder09F18.AssembleHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 5f, rowY, buttonWidth, commandHeight), "HOLD", buttonStyle))
        {
            pendingTargetOrder = MajorOrder09F18.None;
            IssueRegimentalOrder(MajorOrder09F18.HoldPosition, GetRegimentCenter(), false);
        }

        if (pendingTargetOrder != MajorOrder09F18.None)
            GUI.Label(new Rect(commandX, rowY + 31f, commandWidth, 15f),
                "Klik på slagmarken: " + Label(pendingTargetOrder), mutedStyle);
    }
}
