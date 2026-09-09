using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09l5 TEST
// Decouples company tactical centres from the Regiment transform so a regiment no longer
// behaves as one rigid body. Multi-company RMB orders preserve relative company spacing
// around the selected group centroid instead of rebuilding one giant lateral line.
[DefaultExecutionOrder(430)]
public sealed class PrototypeIndependentCompanyMovement09L5 : MonoBehaviour
{
    private sealed class Snapshot
    {
        public PrototypeCompanyTacticalEntity09L2 Company;
        public Vector3 Position;
        public Vector3 Forward;
    }

    private readonly List<Snapshot> snapshots = new List<Snapshot>();
    private Transform detachedRoot;
    private Camera cam;
    private bool installed;
    private bool rightTracking;
    private bool rightAppend;
    private Vector3 orderAnchor;
    private Vector3 orderDragPoint;
    private FieldInfo selectedCompaniesField;
    private FieldInfo regimentSelectedField;
    private IList regimentSelection;
    private IList selectedCompanies;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeIndependentCompanyMovement09L5>() != null)
            return;

        GameObject root = new GameObject("PrototypeIndependentCompanyMovement_v000009l5");
        root.AddComponent<PrototypeIndependentCompanyMovement09L5>();
    }

    private void Awake()
    {
        GameObject root = new GameObject("DetachedCompanyTacticalRoot_09L5");
        detachedRoot = root.transform;
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        EnsureReflection(control);
        DetachCompanies(control);
        ConvertWholeRegimentSelectionToCompanies(control);
        SuppressParentMovementWriters(control);

        bool overUi = BattleManager.Instance != null &&
                      BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!overUi && control.HasCompanySelection && Input.GetMouseButtonDown(1))
            BeginGroupOrder(control);

        if (rightTracking && Input.GetMouseButton(1))
            UpdateGroupOrder();

        if (rightTracking && Input.GetMouseButtonUp(1))
            CompleteGroupOrder();

        if (!installed)
        {
            installed = true;
            Debug.Log(
                "COMPANY-AUTH-09L5|Installed=True|TransformParent=IndependentWorldSpace|" +
                "GroupOrders=PreserveRelativeCompanyOffsets|RegimentPivotMovement=False|" +
                "WholeRegimentSelection=SelectAllCompanies");
        }
    }

    private void EnsureReflection(PrototypeCompanyTacticalControl09L2 control)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        if (selectedCompaniesField == null)
            selectedCompaniesField = typeof(PrototypeCompanyTacticalControl09L2).GetField("selectedCompanies", flags);
        if (selectedCompanies == null && selectedCompaniesField != null)
            selectedCompanies = selectedCompaniesField.GetValue(control) as IList;

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null)
        {
            if (regimentSelectedField == null)
                regimentSelectedField = typeof(PlayerCommander).GetField("selected", flags);
            if (regimentSelection == null && regimentSelectedField != null)
                regimentSelection = regimentSelectedField.GetValue(commander) as IList;
        }
    }

    private void DetachCompanies(PrototypeCompanyTacticalControl09L2 control)
    {
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || !company.gameObject.activeInHierarchy)
                continue;

            if (company.transform.parent == detachedRoot)
                continue;

            Vector3 worldPosition = company.transform.position;
            Quaternion worldRotation = company.transform.rotation;
            company.transform.SetParent(detachedRoot, true);
            company.transform.position = worldPosition;
            company.transform.rotation = worldRotation;
        }
    }

    private void ConvertWholeRegimentSelectionToCompanies(PrototypeCompanyTacticalControl09L2 control)
    {
        if (regimentSelection == null || regimentSelection.Count == 0 || selectedCompanies == null)
            return;

        List<Regiment> parents = new List<Regiment>();
        for (int i = 0; i < regimentSelection.Count; i++)
        {
            Regiment regiment = regimentSelection[i] as Regiment;
            if (regiment != null && regiment.Team == BattleTeam.Denmark)
                parents.Add(regiment);
        }

        if (parents.Count == 0)
            return;

        for (int i = regimentSelection.Count - 1; i >= 0; i--)
        {
            Regiment regiment = regimentSelection[i] as Regiment;
            if (regiment != null)
                regiment.SetSelected(false);
        }
        regimentSelection.Clear();

        for (int i = 0; i < selectedCompanies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 existing = selectedCompanies[i] as PrototypeCompanyTacticalEntity09L2;
            if (existing != null)
                existing.SetSelected(false);
        }
        selectedCompanies.Clear();

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int p = 0; p < parents.Count; p++)
        {
            Regiment parent = parents[p];
            for (int i = 0; i < companies.Count; i++)
            {
                PrototypeCompanyTacticalEntity09L2 company = companies[i];
                if (company == null || company.ParentRegiment != parent)
                    continue;

                selectedCompanies.Add(company);
                company.SetSelected(true);
            }
        }

        Debug.Log(
            "COMPANY-AUTH-09L5|WholeRegimentSelectionConverted=True|Parents=" + parents.Count +
            "|Companies=" + selectedCompanies.Count);
    }

    private static void SuppressParentMovementWriters(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<Regiment> parents = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;

        for (int i = 0; i < companies.Count; i++)
        {
            Regiment regiment = companies[i] != null ? companies[i].ParentRegiment : null;
            if (regiment == null || !regiment.gameObject.activeInHierarchy || !parents.Add(regiment))
                continue;

            OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
            if (officer != null && officer.AIEnabled)
                officer.SetAIEnabled(false);

            regiment.OrderHold();
        }
    }

    private void BeginGroupOrder(PrototypeCompanyTacticalControl09L2 control)
    {
        snapshots.Clear();
        if (!TryGetGroundPoint(Input.mousePosition, out orderAnchor))
            return;

        orderDragPoint = orderAnchor;
        rightAppend = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        for (int i = 0; i < selected.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = selected[i];
            if (company == null)
                continue;

            snapshots.Add(new Snapshot
            {
                Company = company,
                Position = company.transform.position,
                Forward = company.transform.forward
            });
        }

        rightTracking = snapshots.Count > 1;
    }

    private void UpdateGroupOrder()
    {
        if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            orderDragPoint = point;
    }

    private void CompleteGroupOrder()
    {
        rightTracking = false;
        if (snapshots.Count <= 1)
            return;

        Vector3 centroid = Vector3.zero;
        Vector3 averageForward = Vector3.zero;
        for (int i = 0; i < snapshots.Count; i++)
        {
            centroid += snapshots[i].Position;
            averageForward += snapshots[i].Forward;
        }
        centroid /= snapshots.Count;
        centroid.y = PrototypeBootstrap.SampleGroundHeight(centroid.x, centroid.z) + 0.10f;
        averageForward.y = 0f;
        if (averageForward.sqrMagnitude < 0.01f)
            averageForward = Vector3.forward;
        averageForward.Normalize();

        Vector3 dragFacing = orderDragPoint - orderAnchor;
        dragFacing.y = 0f;
        bool explicitFacing = dragFacing.magnitude >= 4f;
        if (explicitFacing)
            dragFacing.Normalize();
        else
            dragFacing = averageForward;

        Quaternion rotateOffsets = Quaternion.FromToRotation(averageForward, dragFacing);

        for (int i = 0; i < snapshots.Count; i++)
        {
            Snapshot snapshot = snapshots[i];
            if (snapshot.Company == null)
                continue;

            Vector3 offset = snapshot.Position - centroid;
            offset.y = 0f;
            if (explicitFacing)
                offset = rotateOffsets * offset;

            Vector3 destination = orderAnchor + offset;
            destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

            Vector3 finalFacing = explicitFacing ? dragFacing : snapshot.Forward;
            finalFacing.y = 0f;
            if (finalFacing.sqrMagnitude < 0.01f)
                finalFacing = averageForward;

            // Overrides the legacy 09l2 "one giant line" group destination written earlier
            // in the same mouse-up frame. Every company receives its own translated target.
            snapshot.Company.IssueMove(destination, finalFacing.normalized, explicitFacing, rightAppend);
        }

        Debug.Log(
            "COMPANY-AUTH-09L5|Order=GroupMove|Companies=" + snapshots.Count +
            "|RelativeOffsetsPreserved=True|RotateOffsets=" + explicitFacing +
            "|Append=" + rightAppend);

        snapshots.Clear();
    }

    private bool TryGetGroundPoint(Vector3 screenPosition, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 2500f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            if (collider.GetComponentInParent<PrototypeCompanyTacticalEntity09L2>() != null)
                continue;
            if (collider.GetComponentInParent<Regiment>() != null)
                continue;

            point = hits[i].point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float enter))
            return false;

        point = ray.GetPoint(enter);
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return true;
    }
}
