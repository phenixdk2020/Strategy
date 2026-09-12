using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00d2 - Route planning + automatic march column/deployment.
    // Higher-level selection/order code issues intent only; Company remains the sole world-pose owner.
    [DefaultExecutionOrder(645)]
    public sealed class TacticalCompanyOrder00D2 : MonoBehaviour
    {
        private sealed class RoutePoint
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool Final;
        }

        private sealed class CompanyRoutePlan
        {
            public TacticalCompanyEntity00B Company;
            public readonly List<RoutePoint> Points = new List<RoutePoint>();
            public int NextIndex;
            public RebuildFormation FinalFormation;
            public bool WaitingForMarchColumn;
            public bool DeployedNearDestination;
            public bool ContactRequested;
            public bool ContactHandled;
            public bool Complete;
            public string ContactReason = string.Empty;
        }

        public static TacticalCompanyOrder00D2 Instance { get; private set; }
        public static bool GhostActive => Instance != null && Instance.finalGhostActive;
        public static bool RoutePlanning => Instance != null && Instance.routePlanning;

        // QA movement tuning. Historical/tactical balance is intentionally not source-locked here.
        private const float MarchSpeed = 1.35f;
        private const float TurnSpeed = 55f;
        private const float MarchColumnReformSeconds = 1.35f;
        private const float CombatDeploySeconds = 2.20f;
        private const float RouteSegmentLength = 8.0f;
        private const float DeployNearDestinationDistance = 35.0f;
        private const float EnemyContactDistance = 160.0f;
        private const float FacingDragThreshold = 0.55f;
        private const float MaxRayDistance = 3000f;

        // QA range-cone values. These are UI/test distances, not final historical weapon locks.
        private const float ShortRange = 80f;
        private const float MediumRange = 160f;
        private const float LongRange = 260f;
        private const float ConeHalfAngle = 30f;
        private const int ArcSegments = 28;

        private readonly List<TacticalCompanyEntity00B> companies = new List<TacticalCompanyEntity00B>();
        private readonly List<Vector3> offsets = new List<Vector3>();
        private readonly List<Quaternion> relativeRotations = new List<Quaternion>();
        private readonly List<Vector3> waypointAnchors = new List<Vector3>();
        private readonly List<CompanyRoutePlan> activePlans = new List<CompanyRoutePlan>();
        private readonly List<TacticalCompanyEntity00B> allCompanies = new List<TacticalCompanyEntity00B>();

        private readonly List<LineRenderer> ghostRects = new List<LineRenderer>();
        private readonly List<LineRenderer> ghostArrows = new List<LineRenderer>();

        private Camera cam;
        private bool routePlanning;
        private bool finalGhostActive;
        private bool committedPreviewVisible;
        private Vector3 selectionCentre;
        private Vector3 finalAnchor;
        private Quaternion referenceRotation = Quaternion.identity;
        private Quaternion finalRotation = Quaternion.identity;
        private RebuildFormation finalFormation = RebuildFormation.Line;

        private LineRenderer routeLine;
        private LineRenderer shortArc;
        private LineRenderer mediumArc;
        private LineRenderer longArc;
        private LineRenderer coneSides;

        private Material ghostMaterial;
        private Material arrowMaterial;
        private Material routeMaterial;
        private Material shortMaterial;
        private Material mediumMaterial;
        private Material longMaterial;

        private string transient = string.Empty;
        private float transientUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyOrder00D1 oldD1 = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyOrder00D1>();
            if (oldD1 != null)
                oldD1.enabled = false;

            TacticalCompanyDrill00C4 oldDrill = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C4>();
            if (oldDrill != null)
                oldDrill.enabled = false;

            TacticalQaLab00C5 oldQa = UnityEngine.Object.FindAnyObjectByType<TacticalQaLab00C5>();
            if (oldQa != null)
                oldQa.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyOrder00D2>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00D2_ROUTE_ORDER");
            root.AddComponent<TacticalCompanyOrder00D2>();
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

        private void Start()
        {
            cam = Camera.main;
            CreateMaterials();
            CreateSharedLines();
            RefreshAllCompanies();

            Debug.Log(
                "REBUILD-ORDER-00D2|Installed=True|AltRMB=WaypointRoute|RMB=FinalGhost|" +
                "DoubleLMB=RegimentSelection|AutoMarchColumn=True|NearDestinationDeploy=" +
                DeployNearDestinationDistance.ToString("0") + "m|EnemyContactDeploy=" +
                EnemyContactDistance.ToString("0") + "m|UnderFireHook=True|" +
                "RangeCone=Short" + ShortRange.ToString("0") + "_Medium" + MediumRange.ToString("0") +
                "_Long" + LongRange.ToString("0") + "|ObstacleNavigation=False");
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
            if (cam == null)
                return;

            UpdateActivePlans();

            if (Input.GetKeyDown(KeyCode.U))
                SimulateUnderFireForSelection();

            if (finalGhostActive)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelPlanning("Rute/ordre annulleret");
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F))
                    ToggleFinalFormation();

                if (Input.GetMouseButton(1))
                {
                    if (TryGetGroundPoint(out Vector3 current))
                    {
                        Vector3 forward = current - finalAnchor;
                        forward.y = 0f;
                        if (forward.magnitude >= FacingDragThreshold)
                            finalRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                    }
                    UpdateFinalGhostVisuals();
                }

                if (Input.GetMouseButtonUp(1))
                    CommitRouteOrder();

                return;
            }

            if (routePlanning)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelPlanning("Rute annulleret");
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Backspace) && waypointAnchors.Count > 0)
                {
                    waypointAnchors.RemoveAt(waypointAnchors.Count - 1);
                    UpdateRouteLinePlanning(false);
                    ShowTransient("Sidste waypoint fjernet");
                }

                if (Input.GetKeyDown(KeyCode.F))
                    ToggleFinalFormation();
            }

            if (Input.GetMouseButtonDown(1))
            {
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                if (alt)
                {
                    if (!routePlanning && !BeginRoutePlanningFromSelection())
                        return;
                    AddWaypointFromMouse();
                }
                else
                {
                    if (!routePlanning && !BeginRoutePlanningFromSelection())
                        return;
                    BeginFinalGhostFromMouse();
                }
            }
        }

        public static void NotifyUnderFire(TacticalCompanyEntity00B company)
        {
            if (Instance == null || company == null)
                return;
            Instance.RequestContactDeploy(company, "UNDER_FIRE");
        }

        private bool BeginRoutePlanningFromSelection()
        {
            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            if (selection == null || selection.Selected == null || selection.Selected.Count == 0)
                return false;

            companies.Clear();
            offsets.Clear();
            relativeRotations.Clear();
            waypointAnchors.Clear();

            selectionCentre = Vector3.zero;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;
                companies.Add(company);
                selectionCentre += company.transform.position;
            }

            if (companies.Count == 0)
                return false;

            selectionCentre /= companies.Count;
            referenceRotation = companies[0].transform.rotation;
            finalRotation = referenceRotation;
            finalFormation = companies[0].Formation == RebuildFormation.Column
                ? RebuildFormation.Column
                : RebuildFormation.Line;

            Quaternion invReference = Quaternion.Inverse(referenceRotation);
            for (int i = 0; i < companies.Count; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                offsets.Add(invReference * (company.transform.position - selectionCentre));
                relativeRotations.Add(invReference * company.transform.rotation);
            }

            routePlanning = true;
            committedPreviewVisible = false;
            EnsureGhostCount(companies.Count);
            SetFinalPreviewVisibility(false);
            UpdateRouteLinePlanning(false);

            ShowTransient("Rute: hold ALT og RMB-klik waypoints | RMB final = destination/facing");
            Debug.Log("REBUILD-ROUTE-00D2|PlanningStart=True|SelectedCompanies=" + companies.Count);
            return true;
        }

        private void AddWaypointFromMouse()
        {
            if (!TryGetGroundPoint(out Vector3 point))
                return;

            point.y = selectionCentre.y;
            Vector3 previous = waypointAnchors.Count > 0
                ? waypointAnchors[waypointAnchors.Count - 1]
                : selectionCentre;

            if (Vector3.Distance(previous, point) < 2.0f)
                return;

            waypointAnchors.Add(point);
            UpdateRouteLinePlanning(false);
            ShowTransient("Waypoint " + waypointAnchors.Count + " tilføjet — hold ALT for flere");

            Debug.Log(
                "REBUILD-ROUTE-00D2|WaypointAdded=True|Index=" + waypointAnchors.Count +
                "|Position=" + point.ToString("F2"));
        }

        private void BeginFinalGhostFromMouse()
        {
            if (!TryGetGroundPoint(out Vector3 point))
                return;

            finalAnchor = point;
            finalAnchor.y = selectionCentre.y;

            Vector3 previous = waypointAnchors.Count > 0
                ? waypointAnchors[waypointAnchors.Count - 1]
                : selectionCentre;
            Vector3 approach = finalAnchor - previous;
            approach.y = 0f;
            if (approach.sqrMagnitude > 0.25f)
                finalRotation = Quaternion.LookRotation(approach.normalized, Vector3.up);

            finalGhostActive = true;
            SetFinalPreviewVisibility(true);
            UpdateRouteLinePlanning(true);
            UpdateFinalGhostVisuals();

            ShowTransient("Final ghost: hold RMB og drej | F Line/Column | slip RMB = march");
            Debug.Log(
                "REBUILD-ROUTE-00D2|FinalGhost=True|Waypoints=" + waypointAnchors.Count +
                "|Final=" + finalAnchor.ToString("F2") + "|Formation=" + finalFormation);
        }

        private void ToggleFinalFormation()
        {
            finalFormation = finalFormation == RebuildFormation.Line
                ? RebuildFormation.Column
                : RebuildFormation.Line;
            if (finalGhostActive)
                UpdateFinalGhostVisuals();
            ShowTransient("Slutformation: " + finalFormation + " | march foregår stadig i Column");
        }

        private void CommitRouteOrder()
        {
            if (!finalGhostActive || companies.Count == 0)
                return;

            List<Vector3> centreSamples = BuildSampledCentreRoute();
            if (centreSamples.Count < 2)
            {
                CancelPlanning("Ruten er for kort");
                return;
            }

            activePlans.Clear();
            for (int companyIndex = 0; companyIndex < companies.Count; companyIndex++)
            {
                TacticalCompanyEntity00B company = companies[companyIndex];
                if (company == null)
                    continue;

                CompanyRoutePlan plan = new CompanyRoutePlan
                {
                    Company = company,
                    FinalFormation = finalFormation,
                    WaitingForMarchColumn = company.Formation != RebuildFormation.Column
                };

                for (int p = 1; p < centreSamples.Count; p++)
                {
                    bool isFinal = p == centreSamples.Count - 1;
                    Quaternion orientation;
                    if (isFinal)
                    {
                        orientation = finalRotation;
                    }
                    else
                    {
                        Vector3 dir = centreSamples[p] - centreSamples[p - 1];
                        dir.y = 0f;
                        orientation = dir.sqrMagnitude > 0.01f
                            ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                            : finalRotation;
                    }

                    Vector3 position = centreSamples[p] + orientation * offsets[companyIndex];
                    position.y = company.transform.position.y;
                    Quaternion rotation = orientation * relativeRotations[companyIndex];

                    plan.Points.Add(new RoutePoint
                    {
                        Position = position,
                        Rotation = rotation,
                        Final = isFinal
                    });
                }

                activePlans.Add(plan);
            }

            if (activePlans.Count == 0)
            {
                CancelPlanning("Ingen gyldige Companies i ordren");
                return;
            }

            // Keep destination/route ghosts visible while marching.
            committedPreviewVisible = true;
            finalGhostActive = false;
            routePlanning = false;
            UpdateRouteLineCommitted(centreSamples);
            SetFinalPreviewVisibility(true);
            UpdateFinalGhostVisuals();

            for (int i = 0; i < activePlans.Count; i++)
            {
                CompanyRoutePlan plan = activePlans[i];
                if (plan.WaitingForMarchColumn && !plan.Company.IsMovingToOrder && !plan.Company.IsReforming)
                    plan.Company.BeginFormationDrill(RebuildFormation.Column, MarchColumnReformSeconds);
            }

            Debug.Log(
                "REBUILD-ROUTE-00D2|Commit=True|Companies=" + activePlans.Count +
                "|UserWaypoints=" + waypointAnchors.Count +
                "|SampledRoutePoints=" + (centreSamples.Count - 1) +
                "|FinalFormation=" + finalFormation +
                "|AutoMarchColumn=True");

            ShowTransient("Marchordre givet — Column på ruten, deployment ved kontakt eller slutmål");
        }

        private List<Vector3> BuildSampledCentreRoute()
        {
            List<Vector3> control = new List<Vector3>();
            control.Add(selectionCentre);
            for (int i = 0; i < waypointAnchors.Count; i++)
                control.Add(waypointAnchors[i]);
            control.Add(finalAnchor);

            List<Vector3> samples = new List<Vector3>();
            samples.Add(control[0]);

            for (int i = 0; i < control.Count - 1; i++)
            {
                Vector3 a = control[i];
                Vector3 b = control[i + 1];
                float distance = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(distance / RouteSegmentLength));
                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps;
                    samples.Add(Vector3.Lerp(a, b, t));
                }
            }

            return samples;
        }

        private void UpdateActivePlans()
        {
            if (activePlans.Count == 0)
                return;

            bool anyIncomplete = false;

            for (int i = 0; i < activePlans.Count; i++)
            {
                CompanyRoutePlan plan = activePlans[i];
                TacticalCompanyEntity00B company = plan.Company;
                if (plan.Complete || company == null)
                    continue;

                anyIncomplete = true;

                if (!plan.ContactRequested && !plan.DeployedNearDestination &&
                    company.Formation == RebuildFormation.Column && IsEnemyContact(company))
                {
                    plan.ContactRequested = true;
                    plan.ContactReason = "ENEMY_CONTACT";
                    Debug.Log(
                        "REBUILD-ROUTE-00D2|ContactQueued=True|UnitID=" + company.UnitId +
                        "|Reason=ENEMY_CONTACT|DistanceThreshold=" + EnemyContactDistance.ToString("0"));
                }

                if (company.IsMovingToOrder || company.IsReforming || company.IsTurning)
                    continue;

                if (plan.ContactRequested)
                {
                    if (!plan.ContactHandled)
                    {
                        plan.ContactHandled = true;
                        plan.NextIndex = plan.Points.Count;
                        if (company.Formation != RebuildFormation.Line)
                            company.BeginFormationDrill(RebuildFormation.Line, CombatDeploySeconds);
                        ShowTransient("Kontakt! " + company.DisplayName + " deployerer fra marchkolonne");
                        Debug.Log(
                            "REBUILD-ROUTE-00D2|ContactDeploy=True|UnitID=" + company.UnitId +
                            "|Reason=" + plan.ContactReason + "|RouteAborted=True");
                        continue;
                    }

                    plan.Complete = true;
                    continue;
                }

                if (plan.WaitingForMarchColumn)
                {
                    if (company.Formation != RebuildFormation.Column)
                    {
                        company.BeginFormationDrill(RebuildFormation.Column, MarchColumnReformSeconds);
                        continue;
                    }
                    plan.WaitingForMarchColumn = false;
                }

                float remaining = CalculateRemainingDistance(plan);
                if (!plan.DeployedNearDestination && plan.FinalFormation != RebuildFormation.Column &&
                    remaining <= DeployNearDestinationDistance)
                {
                    plan.DeployedNearDestination = true;
                    if (company.Formation != plan.FinalFormation)
                    {
                        company.BeginFormationDrill(plan.FinalFormation, CombatDeploySeconds);
                        Debug.Log(
                            "REBUILD-ROUTE-00D2|NearDestinationDeploy=True|UnitID=" + company.UnitId +
                            "|Remaining=" + remaining.ToString("0.0") +
                            "|Formation=" + plan.FinalFormation);
                        continue;
                    }
                }

                if (plan.NextIndex >= plan.Points.Count)
                {
                    if (company.Formation != plan.FinalFormation)
                    {
                        company.BeginFormationDrill(plan.FinalFormation, CombatDeploySeconds);
                        plan.DeployedNearDestination = true;
                        continue;
                    }
                    plan.Complete = true;
                    Debug.Log(
                        "REBUILD-ROUTE-00D2|Complete=True|UnitID=" + company.UnitId +
                        "|FinalFormation=" + company.Formation);
                    continue;
                }

                RoutePoint point = plan.Points[plan.NextIndex];
                RebuildFormation moveFormation = plan.DeployedNearDestination
                    ? plan.FinalFormation
                    : RebuildFormation.Column;

                if (company.BeginMoveOrder(
                    point.Position,
                    point.Rotation,
                    moveFormation,
                    MarchSpeed,
                    TurnSpeed,
                    0.35f))
                {
                    plan.NextIndex++;
                }
                else
                {
                    // Point is effectively already reached; advance safely.
                    plan.NextIndex++;
                }
            }

            if (!anyIncomplete || AllPlansComplete())
            {
                activePlans.Clear();
                committedPreviewVisible = false;
                SetFinalPreviewVisibility(false);
                if (routeLine != null)
                    routeLine.enabled = false;
                Debug.Log("REBUILD-ROUTE-00D2|GroupRouteComplete=True");
            }
        }

        private float CalculateRemainingDistance(CompanyRoutePlan plan)
        {
            if (plan == null || plan.Company == null || plan.NextIndex >= plan.Points.Count)
                return 0f;

            float distance = Vector3.Distance(plan.Company.transform.position, plan.Points[plan.NextIndex].Position);
            for (int i = plan.NextIndex; i < plan.Points.Count - 1; i++)
                distance += Vector3.Distance(plan.Points[i].Position, plan.Points[i + 1].Position);
            return distance;
        }

        private bool IsEnemyContact(TacticalCompanyEntity00B company)
        {
            for (int i = 0; i < allCompanies.Count; i++)
            {
                TacticalCompanyEntity00B other = allCompanies[i];
                if (other == null || other == company || other.Nation == company.Nation)
                    continue;

                Vector3 a = company.transform.position;
                Vector3 b = other.transform.position;
                a.y = 0f;
                b.y = 0f;
                if (Vector3.Distance(a, b) <= EnemyContactDistance)
                    return true;
            }
            return false;
        }

        private void RequestContactDeploy(TacticalCompanyEntity00B company, string reason)
        {
            for (int i = 0; i < activePlans.Count; i++)
            {
                CompanyRoutePlan plan = activePlans[i];
                if (plan.Company != company || plan.Complete)
                    continue;

                plan.ContactRequested = true;
                plan.ContactReason = reason;
                Debug.Log(
                    "REBUILD-ROUTE-00D2|ContactQueued=True|UnitID=" + company.UnitId +
                    "|Reason=" + reason);
                return;
            }
        }

        private void SimulateUnderFireForSelection()
        {
            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            if (selection == null || selection.Selected == null)
                return;

            int count = 0;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null)
                    continue;
                RequestContactDeploy(company, "QA_UNDER_FIRE");
                count++;
            }

            if (count > 0)
                ShowTransient("QA: Under fire sendt til " + count + " selected Company(s)");
        }

        private bool AllPlansComplete()
        {
            for (int i = 0; i < activePlans.Count; i++)
            {
                if (!activePlans[i].Complete)
                    return false;
            }
            return true;
        }

        private void CancelPlanning(string message)
        {
            routePlanning = false;
            finalGhostActive = false;
            committedPreviewVisible = false;
            companies.Clear();
            offsets.Clear();
            relativeRotations.Clear();
            waypointAnchors.Clear();
            SetFinalPreviewVisibility(false);
            if (routeLine != null)
                routeLine.enabled = false;
            ShowTransient(message);
            Debug.Log("REBUILD-ROUTE-00D2|PlanningCancel=True");
        }

        private void RefreshAllCompanies()
        {
            allCompanies.Clear();
            TacticalCompanyEntity00B[] found = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    allCompanies.Add(found[i]);
            }
        }

        private void UpdateRouteLinePlanning(bool includeFinal)
        {
            if (routeLine == null)
                return;

            int count = 1 + waypointAnchors.Count + (includeFinal ? 1 : 0);
            routeLine.positionCount = count;
            routeLine.SetPosition(0, selectionCentre + Vector3.up * 0.22f);
            int index = 1;
            for (int i = 0; i < waypointAnchors.Count; i++)
                routeLine.SetPosition(index++, waypointAnchors[i] + Vector3.up * 0.22f);
            if (includeFinal)
                routeLine.SetPosition(index, finalAnchor + Vector3.up * 0.22f);
            routeLine.enabled = count > 1;
        }

        private void UpdateRouteLineCommitted(List<Vector3> samples)
        {
            if (routeLine == null || samples == null || samples.Count < 2)
                return;

            routeLine.positionCount = samples.Count;
            for (int i = 0; i < samples.Count; i++)
                routeLine.SetPosition(i, samples[i] + Vector3.up * 0.22f);
            routeLine.enabled = true;
        }

        private void UpdateFinalGhostVisuals()
        {
            if ((!finalGhostActive && !committedPreviewVisible) || companies.Count == 0)
                return;

            for (int i = 0; i < companies.Count; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                Vector3 centre = finalAnchor + finalRotation * offsets[i];
                centre.y = company.transform.position.y + 0.24f;
                Quaternion rotation = finalRotation * relativeRotations[i];

                company.GetFootprintDimensions(finalFormation, out float width, out float depth);
                DrawGhostRect(ghostRects[i], centre, rotation, width, depth);
                DrawFacingArrow(ghostArrows[i], centre, rotation, Mathf.Max(3.0f, depth * 0.65f + 2.0f));
            }

            DrawRangeCone(finalAnchor + Vector3.up * 0.20f, finalRotation);
        }

        private static void DrawGhostRect(LineRenderer line, Vector3 centre, Quaternion rotation, float width, float depth)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            Vector3[] local =
            {
                new Vector3(-hw, 0f, -hd),
                new Vector3(-hw, 0f, hd),
                new Vector3(hw, 0f, hd),
                new Vector3(hw, 0f, -hd)
            };

            for (int p = 0; p < 4; p++)
                line.SetPosition(p, centre + rotation * local[p]);
        }

        private static void DrawFacingArrow(LineRenderer line, Vector3 centre, Quaternion rotation, float length)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 tip = centre + forward * length;
            Vector3 wingBase = tip - forward * 0.85f;

            line.SetPosition(0, centre);
            line.SetPosition(1, tip);
            line.SetPosition(2, wingBase + right * 0.45f);
            line.SetPosition(3, tip);
            line.SetPosition(4, wingBase - right * 0.45f);
        }

        private void DrawRangeCone(Vector3 centre, Quaternion rotation)
        {
            DrawArc(shortArc, centre, rotation, ShortRange);
            DrawArc(mediumArc, centre, rotation, MediumRange);
            DrawArc(longArc, centre, rotation, LongRange);

            Vector3 left = centre + rotation * (Quaternion.Euler(0f, -ConeHalfAngle, 0f) * Vector3.forward) * LongRange;
            Vector3 right = centre + rotation * (Quaternion.Euler(0f, ConeHalfAngle, 0f) * Vector3.forward) * LongRange;
            coneSides.SetPosition(0, left);
            coneSides.SetPosition(1, centre);
            coneSides.SetPosition(2, right);
        }

        private static void DrawArc(LineRenderer line, Vector3 centre, Quaternion rotation, float radius)
        {
            for (int i = 0; i <= ArcSegments; i++)
            {
                float t = i / (float)ArcSegments;
                float angle = Mathf.Lerp(-ConeHalfAngle, ConeHalfAngle, t);
                Vector3 direction = rotation * (Quaternion.Euler(0f, angle, 0f) * Vector3.forward);
                line.SetPosition(i, centre + direction * radius);
            }
        }

        private void EnsureGhostCount(int count)
        {
            while (ghostRects.Count < count)
            {
                GameObject rectGo = new GameObject("00D2_GhostRect_" + ghostRects.Count);
                rectGo.transform.SetParent(transform, false);
                LineRenderer rect = rectGo.AddComponent<LineRenderer>();
                rect.useWorldSpace = true;
                rect.loop = true;
                rect.positionCount = 4;
                rect.startWidth = 0.16f;
                rect.endWidth = 0.16f;
                rect.material = ghostMaterial;
                ghostRects.Add(rect);

                GameObject arrowGo = new GameObject("00D2_GhostFacing_" + ghostArrows.Count);
                arrowGo.transform.SetParent(transform, false);
                LineRenderer arrow = arrowGo.AddComponent<LineRenderer>();
                arrow.useWorldSpace = true;
                arrow.loop = false;
                arrow.positionCount = 5;
                arrow.startWidth = 0.13f;
                arrow.endWidth = 0.13f;
                arrow.material = arrowMaterial;
                ghostArrows.Add(arrow);
            }
        }

        private void SetFinalPreviewVisibility(bool visible)
        {
            for (int i = 0; i < ghostRects.Count; i++)
            {
                bool active = visible && i < companies.Count;
                ghostRects[i].enabled = active;
                ghostArrows[i].enabled = active;
            }

            if (shortArc != null) shortArc.enabled = visible;
            if (mediumArc != null) mediumArc.enabled = visible;
            if (longArc != null) longArc.enabled = visible;
            if (coneSides != null) coneSides.enabled = visible;
        }

        private void CreateMaterials()
        {
            ghostMaterial = CreateLineMaterial(new Color(0.30f, 0.95f, 0.55f, 0.92f), "00D2_Ghost");
            arrowMaterial = CreateLineMaterial(new Color(0.95f, 0.90f, 0.25f, 0.98f), "00D2_Facing");
            routeMaterial = CreateLineMaterial(new Color(0.25f, 0.80f, 1.00f, 0.86f), "00D2_Route");
            shortMaterial = CreateLineMaterial(new Color(0.25f, 0.95f, 0.35f, 0.82f), "00D2_Short");
            mediumMaterial = CreateLineMaterial(new Color(1.00f, 0.80f, 0.20f, 0.82f), "00D2_Medium");
            longMaterial = CreateLineMaterial(new Color(1.00f, 0.32f, 0.22f, 0.82f), "00D2_Long");
        }

        private void CreateSharedLines()
        {
            routeLine = CreateLine("00D2_RouteLine", routeMaterial, 0.17f, 2, false);
            shortArc = CreateLine("00D2_ShortArc", shortMaterial, 0.12f, ArcSegments + 1, false);
            mediumArc = CreateLine("00D2_MediumArc", mediumMaterial, 0.12f, ArcSegments + 1, false);
            longArc = CreateLine("00D2_LongArc", longMaterial, 0.12f, ArcSegments + 1, false);
            coneSides = CreateLine("00D2_ConeSides", longMaterial, 0.09f, 3, false);

            routeLine.enabled = false;
            shortArc.enabled = false;
            mediumArc.enabled = false;
            longArc.enabled = false;
            coneSides.enabled = false;
        }

        private LineRenderer CreateLine(string name, Material material, float width, int count, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = count;
            line.startWidth = width;
            line.endWidth = width;
            line.material = material;
            return line;
        }

        private bool TryGetGroundPoint(out Vector3 point)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxRayDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                    continue;
                if (collider.GetComponentInParent<TacticalCompanyEntity00B>() != null)
                    continue;

                point = hits[i].point;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void ShowTransient(string message)
        {
            transient = message;
            transientUntil = Time.unscaledTime + 3.0f;
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(transient) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -998;
                GUI.Box(new Rect(Screen.width * 0.5f - 330f, 38f, 660f, 28f), transient);
            }

            if (!TacticalRebuildSettings00C4.DebugViewEnabled)
                return;

            GUI.depth = -962;
            Rect box = new Rect(12f, 222f, 770f, 88f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 6f, box.width - 20f, 20f),
                "D2 ROUTE | ALT+RMB add waypoint(s) | RMB final + drag facing | F final Line/Column | Backspace remove waypoint");
            GUI.Label(new Rect(box.x + 10f, box.y + 28f, box.width - 20f, 20f),
                "March = automatic Column | deploy <= " + DeployNearDestinationDistance.ToString("0") +
                "m from destination | enemy contact <= " + EnemyContactDistance.ToString("0") + "m | U = QA under-fire test");
            GUI.Label(new Rect(box.x + 10f, box.y + 50f, box.width - 20f, 20f),
                "Ghost range cone: SHORT " + ShortRange.ToString("0") + "m | MEDIUM " + MediumRange.ToString("0") +
                "m | LONG " + LongRange.ToString("0") + "m | Obstacle/bridge navigation still deferred");
        }

        private static Material CreateLineMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            return material;
        }
    }
}
