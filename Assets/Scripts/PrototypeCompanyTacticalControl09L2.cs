using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum PrototypeCompanyRole09L2
{
    Engaged,
    Support,
    Reserve,
    Manoeuvre,
    Withdraw
}

// v00.00.09l2 - one tactical centre per company.
// Companies are lightweight command/movement objects only; ordinary soldiers remain GPU-instanced.
public sealed class PrototypeCompanyTacticalEntity09L2 : MonoBehaviour
{
    public Regiment ParentRegiment { get; private set; }
    public PrototypeRegimentOOB09K ParentOOB { get; private set; }
    public PrototypeRegimentOOB09K.BattalionState Battalion { get; private set; }
    public PrototypeRegimentOOB09K.CompanyState Company { get; private set; }
    public int BattalionIndex { get; private set; }
    public int CompanyIndex { get; private set; }
    public RegimentFormation Formation { get; private set; } = RegimentFormation.Line;
    public PrototypeCompanyRole09L2 Role { get; set; } = PrototypeCompanyRole09L2.Engaged;
    public bool IsSelected { get; private set; }
    public bool IsMoving => waypoints.Count > 0;
    public Vector3 DefaultLocalPosition { get; private set; }
    public string CompanyId { get; private set; }

    public string DisplayName
    {
        get
        {
            string regimentName = ParentOOB != null ? ParentOOB.DisplayName : (ParentRegiment != null ? ParentRegiment.RegimentName : "Regiment");
            string battalionName = Battalion != null ? Battalion.Name : "Bataljon";
            string companyName = Company != null ? Company.Name : "Kompagni";
            return regimentName + " | " + battalionName + " | " + companyName;
        }
    }

    public int InitialStrength => Company != null ? Company.InitialStrength : 0;
    public int CurrentStrength => Company != null ? Company.CurrentStrength : 0;

    private sealed class CompanyWaypoint
    {
        public Vector3 WorldPoint;
        public Vector3 Facing;
        public bool HasFacing;
    }

    private readonly List<CompanyWaypoint> waypoints = new List<CompanyWaypoint>();
    private LineRenderer selectionOutline;
    private BoxCollider hitBox;
    private float moveSpeed;

    public void Configure(
        Regiment regiment,
        PrototypeRegimentOOB09K oob,
        PrototypeRegimentOOB09K.BattalionState battalion,
        PrototypeRegimentOOB09K.CompanyState company,
        int battalionIndex,
        int companyIndex,
        Vector3 defaultLocalPosition)
    {
        ParentRegiment = regiment;
        ParentOOB = oob;
        Battalion = battalion;
        Company = company;
        BattalionIndex = battalionIndex;
        CompanyIndex = companyIndex;
        DefaultLocalPosition = defaultLocalPosition;
        CompanyId = regiment.RegimentName + ":B" + (battalionIndex + 1) + ":C" + (companyIndex + 1);
        moveSpeed = regiment.Team == BattleTeam.Denmark ? 3.2f : 3.35f;

        transform.SetParent(regiment.transform, false);
        transform.localPosition = defaultLocalPosition;
        transform.localRotation = Quaternion.identity;

        hitBox = gameObject.AddComponent<BoxCollider>();
        hitBox.center = new Vector3(0f, 0.9f, 0f);

        BuildSelectionOutline();
        RefreshFootprint();
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;
        if (selectionOutline != null)
            selectionOutline.enabled = value;
    }

    public void SetFormation(RegimentFormation formation)
    {
        Formation = formation;
        RefreshFootprint();
    }

    public float GetFootprintWidth()
    {
        if (Formation == RegimentFormation.Column)
            return 5.2f;

        int ranks = 3;
        int files = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, InitialStrength) / (float)ranks));
        return Mathf.Max(2.5f, (files - 1) * 0.52f + 1.1f);
    }

    public float GetFootprintDepth()
    {
        if (Formation == RegimentFormation.Column)
        {
            const int filesAcross = 8;
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, InitialStrength) / (float)filesAcross));
            return Mathf.Max(4f, (rows - 1) * 0.72f + 1.1f);
        }

        return 2.7f;
    }

    public void IssueMove(Vector3 worldPoint, Vector3 facing, bool hasFacing, bool append)
    {
        worldPoint.y = PrototypeBootstrap.SampleGroundHeight(worldPoint.x, worldPoint.z) + 0.10f;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            facing = transform.forward;

        if (!append)
            waypoints.Clear();

        waypoints.Add(new CompanyWaypoint
        {
            WorldPoint = worldPoint,
            Facing = facing.normalized,
            HasFacing = hasFacing
        });
    }

    public void Hold()
    {
        waypoints.Clear();
    }

    public void RotateFacing(float degrees)
    {
        Vector3 worldForward = Quaternion.Euler(0f, degrees, 0f) * transform.forward;
        worldForward.y = 0f;
        if (worldForward.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(worldForward.normalized, Vector3.up);
    }

    public void ReturnToDefaultPosition()
    {
        if (ParentRegiment == null)
            return;

        Vector3 world = ParentRegiment.transform.TransformPoint(DefaultLocalPosition);
        IssueMove(world, ParentRegiment.transform.forward, false, false);
    }

    private void Update()
    {
        if (ParentRegiment == null || ParentRegiment.IsRouted || waypoints.Count == 0)
            return;

        CompanyWaypoint waypoint = waypoints[0];
        Vector3 here = transform.position;
        Vector3 target = waypoint.WorldPoint;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;

        Vector3 planar = target - here;
        planar.y = 0f;

        if (planar.magnitude <= 0.55f)
        {
            transform.position = target;
            waypoints.RemoveAt(0);

            if (waypoints.Count == 0 && waypoint.HasFacing)
            {
                Vector3 finalFacing = waypoint.Facing;
                finalFacing.y = 0f;
                if (finalFacing.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(finalFacing.normalized, Vector3.up);
            }
            return;
        }

        float cohesionFactor = ParentRegiment != null
            ? Mathf.Lerp(0.72f, 1f, ParentRegiment.Cohesion / 100f)
            : 1f;

        Vector3 step = planar.normalized * moveSpeed * cohesionFactor * Time.deltaTime;
        if (step.magnitude > planar.magnitude)
            step = planar;

        Vector3 next = here + step;
        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
        transform.position = next;

        Quaternion desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 4.5f * Time.deltaTime);
    }

    private void RefreshFootprint()
    {
        float width = GetFootprintWidth();
        float depth = GetFootprintDepth();

        if (hitBox != null)
            hitBox.size = new Vector3(width + 1.5f, 2.3f, depth + 1.5f);

        if (selectionOutline == null)
            return;

        float halfW = width * 0.5f + 0.7f;
        float halfD = depth * 0.5f + 0.7f;
        selectionOutline.positionCount = 4;
        selectionOutline.SetPosition(0, new Vector3(-halfW, 0.13f, -halfD));
        selectionOutline.SetPosition(1, new Vector3(-halfW, 0.13f, halfD));
        selectionOutline.SetPosition(2, new Vector3(halfW, 0.13f, halfD));
        selectionOutline.SetPosition(3, new Vector3(halfW, 0.13f, -halfD));
    }

    private void BuildSelectionOutline()
    {
        GameObject outline = new GameObject("CompanySelectionOutline09L2");
        outline.transform.SetParent(transform, false);
        selectionOutline = outline.AddComponent<LineRenderer>();
        selectionOutline.useWorldSpace = false;
        selectionOutline.loop = true;
        selectionOutline.widthMultiplier = 0.13f;
        selectionOutline.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.98f, 0.80f, 0.16f),
            "CompanySelection09L2");
        selectionOutline.enabled = false;
    }
}

// v00.00.09l2 - owns company selection and company-level orders.
// Whole-regiment control remains in PlayerCommander; double-clicking a company returns to regiment control.
[DefaultExecutionOrder(250)]
public sealed class PrototypeCompanyTacticalControl09L2 : MonoBehaviour
{
    public static PrototypeCompanyTacticalControl09L2 Instance { get; private set; }
    public bool Installed { get; private set; }
    public bool HasCompanySelection => selectedCompanies.Count > 0;
    public IReadOnlyList<PrototypeCompanyTacticalEntity09L2> Companies => companies;
    public IReadOnlyList<PrototypeCompanyTacticalEntity09L2> SelectedCompanies => selectedCompanies;

    private const float DragThresholdPixels = 9f;
    private const float RightFacingThreshold = 4f;
    private const float DoubleClickSeconds = 0.32f;

    private readonly List<PrototypeCompanyTacticalEntity09L2> companies =
        new List<PrototypeCompanyTacticalEntity09L2>();
    private readonly List<PrototypeCompanyTacticalEntity09L2> selectedCompanies =
        new List<PrototypeCompanyTacticalEntity09L2>();
    private readonly Dictionary<Regiment, int> lastRegimentStrength =
        new Dictionary<Regiment, int>();

    private Camera cam;
    private FieldInfo playerSelectedField;
    private FieldInfo playerRoutesField;
    private IList regimentSelection;
    private IDictionary regimentRoutes;
    private PlayerCommander commander;
    private bool commanderSuppressed;

    private bool leftTracking;
    private bool leftDragging;
    private bool leftShift;
    private bool leftCtrl;
    private Vector2 leftStart;
    private Vector2 leftCurrent;

    private bool rightTracking;
    private bool rightAppend;
    private Vector3 rightStartWorld;
    private Vector3 rightCurrentWorld;

    private PrototypeCompanyTacticalEntity09L2 hoveredCompany;
    private PrototypeCompanyTacticalEntity09L2 lastClickedCompany;
    private float lastCompanyClickTime;
    private GUIStyle hoverStyle;
    private GUIStyle marqueeStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCompanyTacticalControl09L2>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyTacticalControl_v000009l2");
        root.AddComponent<PrototypeCompanyTacticalControl09L2>();
    }

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
        ResolvePlayerCommanderInternals();
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (!Installed)
        {
            TryInstall();
            if (!Installed)
                return;
        }

        ResolvePlayerCommanderInternals();
        SyncCompanyStrengths();
        hoveredCompany = GetCompanyUnderMouse();

        bool overUi = BattleManager.Instance != null &&
                      BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!overUi && Input.GetMouseButtonDown(0))
            BeginLeftSelection();

        if (leftTracking && Input.GetMouseButton(0))
            UpdateLeftSelection();

        if (leftTracking && Input.GetMouseButtonUp(0))
            CompleteLeftSelection();

        if (!overUi && HasCompanySelection && Input.GetMouseButtonDown(1))
            BeginCompanyOrder();

        if (rightTracking && Input.GetMouseButton(1))
            UpdateCompanyOrder();

        if (rightTracking && Input.GetMouseButtonUp(1))
            CompleteCompanyOrder();

        if (HasCompanySelection)
            HandleCompanyHotkeys();
    }

    private void LateUpdate()
    {
        if (commanderSuppressed && commander != null)
        {
            commander.enabled = true;
            commanderSuppressed = false;
        }
    }

    private void TryInstall()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        List<Regiment> pilot = new List<Regiment>();
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            if (regiment.RegimentName == "1. Regiment" ||
                regiment.RegimentName == "5. Regiment" ||
                regiment.RegimentName == "8th Regiment" ||
                regiment.RegimentName == "18th Regiment")
            {
                PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
                if (oob == null || oob.Battalions == null || oob.Battalions.Count == 0)
                    return;
                pilot.Add(regiment);
            }
        }

        if (pilot.Count != 4)
            return;

        PrototypeBoxSelection09H oldBox = UnityEngine.Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
        if (oldBox != null)
            oldBox.enabled = false;

        companies.Clear();
        lastRegimentStrength.Clear();

        foreach (Regiment regiment in pilot)
        {
            BuildCompaniesForRegiment(regiment);
            lastRegimentStrength[regiment] = regiment.CurrentStrength;
        }

        Installed = companies.Count > 0;
        if (Installed)
        {
            Debug.Log(
                "COMPANY-09L2|Installed=True|Companies=" + companies.Count +
                "|Selection=Click+Box|Orders=RMB+AltWaypoint|Formation=Line+Column|" +
                "WholeRegimentControl=DoubleClickOrHQ|PerSoldierGameObject=False");
        }
    }

    private void BuildCompaniesForRegiment(Regiment regiment)
    {
        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        if (oob == null)
            return;

        int battalionCount = oob.Battalions.Count;
        float battalionCenter = (battalionCount - 1) * 0.5f;

        for (int b = 0; b < battalionCount; b++)
        {
            PrototypeRegimentOOB09K.BattalionState battalion = oob.Battalions[b];
            float battalionZ = (battalionCenter - b) * 31f;
            float companyCenter = (battalion.Companies.Count - 1) * 0.5f;

            for (int c = 0; c < battalion.Companies.Count; c++)
            {
                PrototypeRegimentOOB09K.CompanyState company = battalion.Companies[c];
                float x = (c - companyCenter) * 38f;

                GameObject go = new GameObject(
                    "CompanyTactical09L2_B" + (b + 1) + "_C" + (c + 1));
                PrototypeCompanyTacticalEntity09L2 entity =
                    go.AddComponent<PrototypeCompanyTacticalEntity09L2>();
                entity.Configure(
                    regiment,
                    oob,
                    battalion,
                    company,
                    b,
                    c,
                    new Vector3(x, 0f, battalionZ));
                companies.Add(entity);
            }
        }

        Debug.Log(
            "COMPANY-09L2|Unit=" + oob.DisplayName +
            "|Battalions=" + battalionCount +
            "|Companies=" + oob.CompanyCount +
            "|Layout=BattalionLines|CompanyCentres=True");
    }

    private void ResolvePlayerCommanderInternals()
    {
        if (commander == null)
            commander = PlayerCommander.Instance;
        if (commander == null)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        if (playerSelectedField == null)
            playerSelectedField = typeof(PlayerCommander).GetField("selected", flags);
        if (playerRoutesField == null)
            playerRoutesField = typeof(PlayerCommander).GetField("routes", flags);

        if (regimentSelection == null && playerSelectedField != null)
            regimentSelection = playerSelectedField.GetValue(commander) as IList;
        if (regimentRoutes == null && playerRoutesField != null)
            regimentRoutes = playerRoutesField.GetValue(commander) as IDictionary;
    }

    private void BeginLeftSelection()
    {
        leftTracking = true;
        leftDragging = false;
        leftStart = Input.mousePosition;
        leftCurrent = leftStart;
        leftShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        leftCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // PlayerCommander normally selects a Regiment on this same mouse-down frame.
        // 09l2 owns LMB selection, so suppress only this frame and re-enable in LateUpdate.
        SuppressCommanderThisFrame();
    }

    private void UpdateLeftSelection()
    {
        leftCurrent = Input.mousePosition;
        if (!leftDragging && Vector2.Distance(leftCurrent, leftStart) >= DragThresholdPixels)
            leftDragging = true;
    }

    private void CompleteLeftSelection()
    {
        leftCurrent = Input.mousePosition;

        if (leftDragging)
            ApplyCompanyBoxSelection();
        else
            ApplyPointSelection();

        leftTracking = false;
        leftDragging = false;
    }

    private void ApplyPointSelection()
    {
        PrototypeCompanyTacticalEntity09L2 company;
        Regiment regiment;
        GetSelectableUnderMouse(out company, out regiment);

        if (company != null && company.ParentRegiment != null && company.ParentRegiment.Team == BattleTeam.Denmark)
        {
            bool doubleClick =
                company == lastClickedCompany &&
                Time.unscaledTime - lastCompanyClickTime <= DoubleClickSeconds &&
                !leftShift && !leftCtrl;

            lastClickedCompany = company;
            lastCompanyClickTime = Time.unscaledTime;

            if (doubleClick)
            {
                ClearCompanySelection();
                SelectRegiment(company.ParentRegiment, false, false);
                Debug.Log("COMPANY-09L2|DoubleClick=True|WholeRegiment=" + GetRegimentDisplayName(company.ParentRegiment));
                return;
            }

            ClearRegimentSelection();
            if (!leftShift && !leftCtrl)
                ClearCompanySelection();

            if (leftCtrl && selectedCompanies.Contains(company))
                RemoveCompanySelection(company);
            else
                AddCompanySelection(company);

            return;
        }

        if (regiment != null && regiment.Team == BattleTeam.Denmark)
        {
            ClearCompanySelection();
            SelectRegiment(regiment, leftShift || leftCtrl, leftCtrl);
            return;
        }

        if (!leftShift && !leftCtrl)
        {
            ClearCompanySelection();
            ClearRegimentSelection();
        }
    }

    private void ApplyCompanyBoxSelection()
    {
        Rect rect = GetScreenRectBottomLeft(leftStart, leftCurrent);
        List<PrototypeCompanyTacticalEntity09L2> inside = new List<PrototypeCompanyTacticalEntity09L2>();

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null ||
                company.ParentRegiment.Team != BattleTeam.Denmark ||
                company.ParentRegiment.IsRouted)
                continue;

            Vector3 screen = cam.WorldToScreenPoint(company.transform.position);
            if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y), true))
                inside.Add(company);
        }

        ClearRegimentSelection();
        if (!leftShift && !leftCtrl)
            ClearCompanySelection();

        for (int i = 0; i < inside.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = inside[i];
            if (leftCtrl && selectedCompanies.Contains(company))
                RemoveCompanySelection(company);
            else
                AddCompanySelection(company);
        }

        Debug.Log(
            "COMPANY-09L2|Box=True|Inside=" + inside.Count +
            "|Selected=" + selectedCompanies.Count +
            "|Mode=" + (leftCtrl ? "Toggle" : leftShift ? "Add" : "Replace"));
    }

    private void BeginCompanyOrder()
    {
        if (!TryGetGroundPoint(Input.mousePosition, out rightStartWorld))
            return;

        rightTracking = true;
        rightCurrentWorld = rightStartWorld;
        rightAppend = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    private void UpdateCompanyOrder()
    {
        if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            rightCurrentWorld = point;
    }

    private void CompleteCompanyOrder()
    {
        rightTracking = false;
        if (selectedCompanies.Count == 0)
            return;

        Vector3 facing = rightCurrentWorld - rightStartWorld;
        facing.y = 0f;
        bool explicitFacing = facing.magnitude >= RightFacingThreshold;
        if (!explicitFacing)
            facing = GetAverageCompanyFacing();
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;
        facing.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, facing).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        List<PrototypeCompanyTacticalEntity09L2> ordered =
            new List<PrototypeCompanyTacticalEntity09L2>(selectedCompanies);
        ordered.Sort((a, b) =>
            Vector3.Dot(a.transform.position, lateral).CompareTo(Vector3.Dot(b.transform.position, lateral)));

        float maxWidth = 0f;
        for (int i = 0; i < ordered.Count; i++)
            maxWidth = Mathf.Max(maxWidth, ordered[i].GetFootprintWidth());
        float spacing = Mathf.Max(8f, maxWidth + 4f);
        float centerIndex = (ordered.Count - 1) * 0.5f;

        HashSet<Regiment> prepared = new HashSet<Regiment>();
        for (int i = 0; i < ordered.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = ordered[i];
            if (prepared.Add(company.ParentRegiment))
                PrepareRegimentForManualCompanyControl(company.ParentRegiment);

            Vector3 destination = rightStartWorld + lateral * ((i - centerIndex) * spacing);
            destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;
            company.IssueMove(destination, facing, explicitFacing, rightAppend);
        }

        Debug.Log(
            "COMPANY-09L2|Order=Move|Companies=" + ordered.Count +
            "|Append=" + rightAppend +
            "|Facing=" + (explicitFacing ? "Explicit" : "Auto") +
            "|Parents=" + prepared.Count);
    }

    private void HandleCompanyHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            for (int i = 0; i < selectedCompanies.Count; i++)
                selectedCompanies[i].Hold();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            for (int i = 0; i < selectedCompanies.Count; i++)
                selectedCompanies[i].SetFormation(RegimentFormation.Line);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            for (int i = 0; i < selectedCompanies.Count; i++)
                selectedCompanies[i].SetFormation(RegimentFormation.Column);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            for (int i = 0; i < selectedCompanies.Count; i++)
                selectedCompanies[i].RotateFacing(-15f);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            for (int i = 0; i < selectedCompanies.Count; i++)
                selectedCompanies[i].RotateFacing(15f);
        }
    }

    private void PrepareRegimentForManualCompanyControl(Regiment regiment)
    {
        if (regiment == null)
            return;

        OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
        if (officer != null && officer.AIEnabled)
            officer.SetAIEnabled(false);

        if (regimentRoutes != null && regimentRoutes.Contains(regiment))
            regimentRoutes.Remove(regiment);

        regiment.OrderHold();
        regiment.SetSelected(false);

        Debug.Log(
            "COMPANY-09L2|ManualOverride=True|Unit=" + GetRegimentDisplayName(regiment) +
            "|OfficerAI=False|RegimentRouteCleared=True");
    }

    private void SyncCompanyStrengths()
    {
        HashSet<Regiment> processed = new HashSet<Regiment>();
        for (int i = 0; i < companies.Count; i++)
        {
            Regiment regiment = companies[i] != null ? companies[i].ParentRegiment : null;
            if (regiment == null || !processed.Add(regiment))
                continue;

            if (!lastRegimentStrength.TryGetValue(regiment, out int previous))
                previous = regiment.CurrentStrength;

            if (previous == regiment.CurrentStrength)
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob != null)
                ReconcileCompanyStrengths(regiment, oob);

            lastRegimentStrength[regiment] = regiment.CurrentStrength;
        }
    }

    private static void ReconcileCompanyStrengths(Regiment regiment, PrototypeRegimentOOB09K oob)
    {
        List<PrototypeRegimentOOB09K.CompanyState> all = new List<PrototypeRegimentOOB09K.CompanyState>();
        int currentCompanyTotal = 0;

        for (int b = 0; b < oob.Battalions.Count; b++)
        {
            for (int c = 0; c < oob.Battalions[b].Companies.Count; c++)
            {
                PrototypeRegimentOOB09K.CompanyState company = oob.Battalions[b].Companies[c];
                all.Add(company);
                currentCompanyTotal += company.CurrentStrength;
            }
        }

        int staffAlive = Mathf.Min(oob.RegimentalStaffStrength, Mathf.Max(0, regiment.CurrentStrength));
        int targetCompanyTotal = Mathf.Max(0, regiment.CurrentStrength - staffAlive);
        int delta = targetCompanyTotal - currentCompanyTotal;

        while (delta < 0)
        {
            PrototypeRegimentOOB09K.CompanyState candidate = null;
            float bestFraction = -1f;
            for (int i = 0; i < all.Count; i++)
            {
                PrototypeRegimentOOB09K.CompanyState company = all[i];
                if (company.CurrentStrength <= 0)
                    continue;

                float fraction = company.InitialStrength > 0
                    ? company.CurrentStrength / (float)company.InitialStrength
                    : 0f;
                if (fraction > bestFraction)
                {
                    bestFraction = fraction;
                    candidate = company;
                }
            }

            if (candidate == null)
                break;
            candidate.CurrentStrength--;
            delta++;
        }

        while (delta > 0)
        {
            PrototypeRegimentOOB09K.CompanyState candidate = null;
            float lowestFraction = 2f;
            for (int i = 0; i < all.Count; i++)
            {
                PrototypeRegimentOOB09K.CompanyState company = all[i];
                if (company.CurrentStrength >= company.InitialStrength)
                    continue;

                float fraction = company.InitialStrength > 0
                    ? company.CurrentStrength / (float)company.InitialStrength
                    : 1f;
                if (fraction < lowestFraction)
                {
                    lowestFraction = fraction;
                    candidate = company;
                }
            }

            if (candidate == null)
                break;
            candidate.CurrentStrength++;
            delta--;
        }
    }

    private void SuppressCommanderThisFrame()
    {
        if (commander == null)
            commander = PlayerCommander.Instance;
        if (commander == null || !commander.enabled)
            return;

        commander.enabled = false;
        commanderSuppressed = true;
    }

    private void ClearCompanySelection()
    {
        for (int i = selectedCompanies.Count - 1; i >= 0; i--)
            if (selectedCompanies[i] != null)
                selectedCompanies[i].SetSelected(false);
        selectedCompanies.Clear();
    }

    private void AddCompanySelection(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (company == null || company.ParentRegiment == null || company.ParentRegiment.Team != BattleTeam.Denmark)
            return;

        if (!selectedCompanies.Contains(company))
            selectedCompanies.Add(company);
        company.SetSelected(true);
    }

    private void RemoveCompanySelection(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (company == null)
            return;
        selectedCompanies.Remove(company);
        company.SetSelected(false);
    }

    private void ClearRegimentSelection()
    {
        ResolvePlayerCommanderInternals();
        if (regimentSelection == null)
            return;

        for (int i = regimentSelection.Count - 1; i >= 0; i--)
        {
            Regiment regiment = regimentSelection[i] as Regiment;
            if (regiment != null)
                regiment.SetSelected(false);
        }
        regimentSelection.Clear();
    }

    private void SelectRegiment(Regiment regiment, bool additive, bool toggle)
    {
        ResolvePlayerCommanderInternals();
        if (regiment == null || regiment.Team != BattleTeam.Denmark || regimentSelection == null)
            return;

        if (!additive)
            ClearRegimentSelection();

        if (toggle && regimentSelection.Contains(regiment))
        {
            regimentSelection.Remove(regiment);
            regiment.SetSelected(false);
        }
        else if (!regimentSelection.Contains(regiment))
        {
            regimentSelection.Add(regiment);
            regiment.SetSelected(true);
        }
    }

    private PrototypeCompanyTacticalEntity09L2 GetCompanyUnderMouse()
    {
        GetSelectableUnderMouse(out PrototypeCompanyTacticalEntity09L2 company, out Regiment ignored);
        return company;
    }

    private void GetSelectableUnderMouse(
        out PrototypeCompanyTacticalEntity09L2 company,
        out Regiment regiment)
    {
        company = null;
        regiment = null;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1600f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            PrototypeCompanyTacticalEntity09L2 hitCompany =
                hits[i].collider.GetComponentInParent<PrototypeCompanyTacticalEntity09L2>();
            if (hitCompany != null && hitCompany.ParentRegiment != null &&
                hitCompany.ParentRegiment.Team == BattleTeam.Denmark)
            {
                company = hitCompany;
                return;
            }
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Regiment hitRegiment = hits[i].collider.GetComponentInParent<Regiment>();
            if (hitRegiment != null && hitRegiment.Team == BattleTeam.Denmark)
            {
                regiment = hitRegiment;
                return;
            }
        }
    }

    private bool TryGetGroundPoint(Vector3 screenPoint, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 2000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            if (collider.gameObject.name == "Battlefield Ground")
            {
                point = hits[i].point;
                point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
                return true;
            }
        }

        return false;
    }

    private Vector3 GetAverageCompanyFacing()
    {
        Vector3 facing = Vector3.zero;
        for (int i = 0; i < selectedCompanies.Count; i++)
            if (selectedCompanies[i] != null)
                facing += selectedCompanies[i].transform.forward;
        facing.y = 0f;
        return facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward;
    }

    private static string GetRegimentDisplayName(Regiment regiment)
    {
        if (regiment == null)
            return "None";
        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        return oob != null ? oob.DisplayName : regiment.RegimentName;
    }

    private static Rect GetScreenRectBottomLeft(Vector2 a, Vector2 b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.x, b.x),
            Mathf.Min(a.y, b.y),
            Mathf.Max(a.x, b.x),
            Mathf.Max(a.y, b.y));
    }

    private static Rect GetGuiRectTopLeft(Vector2 a, Vector2 b)
    {
        Vector2 ga = new Vector2(a.x, Screen.height - a.y);
        Vector2 gb = new Vector2(b.x, Screen.height - b.y);
        return Rect.MinMaxRect(
            Mathf.Min(ga.x, gb.x),
            Mathf.Min(ga.y, gb.y),
            Mathf.Max(ga.x, gb.x),
            Mathf.Max(ga.y, gb.y));
    }

    private void EnsureStyles()
    {
        if (hoverStyle != null)
            return;

        hoverStyle = new GUIStyle(GUI.skin.box);
        hoverStyle.fontSize = 9;
        hoverStyle.alignment = TextAnchor.MiddleLeft;
        hoverStyle.normal.textColor = Color.white;
        hoverStyle.padding = new RectOffset(6, 6, 3, 3);

        marqueeStyle = new GUIStyle(GUI.skin.label);
        marqueeStyle.fontSize = 9;
        marqueeStyle.fontStyle = FontStyle.Bold;
        marqueeStyle.normal.textColor = new Color(0.90f, 0.95f, 1f, 0.95f);
    }

    private void OnGUI()
    {
        if (!Installed)
            return;

        EnsureStyles();

        if (leftDragging)
        {
            Rect rect = GetGuiRectTopLeft(leftStart, leftCurrent);
            Color old = GUI.color;
            GUI.color = new Color(0.30f, 0.70f, 1f, 0.12f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.60f, 0.86f, 1f, 0.90f);
            const float border = 1.4f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - border, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(rect.x + 5f, rect.y + 4f, 120f, 18f), "COMPANY SELECT", marqueeStyle);
        }

        if (hoveredCompany != null && !hoveredCompany.IsSelected)
        {
            Vector2 mouse = Event.current.mousePosition;
            string text = hoveredCompany.DisplayName + "\n" +
                          hoveredCompany.CurrentStrength + "/" + hoveredCompany.InitialStrength +
                          " | " + hoveredCompany.Formation +
                          " | " + hoveredCompany.Role;
            float width = 300f;
            float x = Mathf.Clamp(mouse.x + 16f, 8f, Screen.width - width - 8f);
            float y = Mathf.Clamp(mouse.y + 16f, 104f, Screen.height - 52f);
            GUI.Box(new Rect(x, y, width, 38f), text, hoverStyle);
        }
    }
}
