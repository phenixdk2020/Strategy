using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09m1 - long-march column for independent companies.
// A company going far switches to Column, then deploys Line a short
// distance before the destination or as soon as it takes / enters fire.
// F/C still override the policy for a short lock window.
[DefaultExecutionOrder(11240)]
public sealed class PrototypeKampCompanyMarch09M1 : MonoBehaviour
{
    public static PrototypeKampCompanyMarch09M1 Instance { get; private set; }

    public enum MarchStatus
    {
        Hold,
        ColumnMarch,
        Deploying,
        Contact,
        Manual
    }

    private sealed class State
    {
        public bool AutoColumn;
        public float ManualLockUntil;
        public MarchStatus Status;
        public string Reason = "INIT";
    }

    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, State> states =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, State>();

    private FieldInfo waypointsField;
    private FieldInfo waypointPointField;
    private bool announced;

    private const float LongMarchMetres = 18f;
    private const float DeployBeforeDestinationMetres = 14f;
    private const float ContactBufferMetres = 8f;
    private const float ManualLockSeconds = 14f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampCompanyMarch09M1>() != null)
            return;

        GameObject root = new GameObject("PrototypeKampCompanyMarch_v000009m1");
        root.AddComponent<PrototypeKampCompanyMarch09M1>();
    }

    private void Awake()
    {
        Instance = this;
        waypointsField = typeof(PrototypeCompanyTacticalEntity09L2).GetField(
            "waypoints",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public MarchStatus GetStatus(PrototypeCompanyTacticalEntity09L2 company)
    {
        return company != null && states.TryGetValue(company, out State state)
            ? state.Status
            : MarchStatus.Hold;
    }

    public string GetStatusLabel(PrototypeCompanyTacticalEntity09L2 company)
    {
        switch (GetStatus(company))
        {
            case MarchStatus.ColumnMarch:
                return "KOLONNE";
            case MarchStatus.Deploying:
                return "DEPLOYERER";
            case MarchStatus.Contact:
                return "KONTAKT";
            case MarchStatus.Manual:
                return "MANUEL";
            default:
                return company != null && company.IsMoving ? "MARSCH" : "HOLD";
        }
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "KAMP-MARCH-09M1|Installed=True|Policy=LongMarchColumn|" +
                "DeployBeforeDestination=" + DeployBeforeDestinationMetres +
                "|ContactDeploysLine=True|ManualLock=F/C");
        }

        NoteManualFormationLocks(control);

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || company.CurrentStrength <= 0)
                continue;
            ApplyPolicy(company, companies);
        }
    }

    private void NoteManualFormationLocks(PrototypeCompanyTacticalControl09L2 control)
    {
        if (!control.HasCompanySelection)
            return;
        if (!Input.GetKeyDown(KeyCode.F) && !Input.GetKeyDown(KeyCode.C))
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        for (int i = 0; i < selected.Count; i++)
        {
            State state = GetState(selected[i]);
            state.ManualLockUntil = Time.unscaledTime + ManualLockSeconds;
            state.AutoColumn = false;
            state.Status = MarchStatus.Manual;
            state.Reason = Input.GetKeyDown(KeyCode.C) ? "PLAYER_COLUMN" : "PLAYER_LINE";
        }
    }

    private void ApplyPolicy(
        PrototypeCompanyTacticalEntity09L2 company,
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies)
    {
        State state = GetState(company);
        if (Time.unscaledTime < state.ManualLockUntil)
        {
            state.Status = MarchStatus.Manual;
            return;
        }

        float remaining = GetRemainingPathDistance(company);
        float enemyDistance = GetNearestEnemyDistance(company, companies);
        Regiment parent = company.ParentRegiment;
        float contactRange = parent.EffectiveRange + ContactBufferMetres;
        bool underFire = parent.HasHitFeedback || enemyDistance <= contactRange;

        bool longMarch = remaining >= LongMarchMetres;
        bool nearDestination = remaining > 0.55f && remaining <= DeployBeforeDestinationMetres;

        if (underFire)
        {
            DeployLine(company, state, MarchStatus.Contact, "ENEMY_FIRE_OR_RANGE");
            return;
        }

        if (!company.IsMoving || remaining <= 0.55f)
        {
            if (state.AutoColumn)
                DeployLine(company, state, MarchStatus.Hold, "ARRIVED");
            else
                SetStatus(state, MarchStatus.Hold, "HOLD");
            return;
        }

        if (nearDestination)
        {
            DeployLine(company, state, MarchStatus.Deploying, "NEAR_DESTINATION");
            return;
        }

        if (longMarch)
        {
            if (company.Formation != RegimentFormation.Column)
                company.SetFormation(RegimentFormation.Column);
            state.AutoColumn = true;
            SetStatus(state, MarchStatus.ColumnMarch, "LONG_MARCH");
            return;
        }

        SetStatus(state, company.Formation == RegimentFormation.Column ? MarchStatus.ColumnMarch : MarchStatus.Hold, "SHORT_MOVE");
    }

    private static void DeployLine(
        PrototypeCompanyTacticalEntity09L2 company,
        State state,
        MarchStatus status,
        string reason)
    {
        if (company.Formation != RegimentFormation.Line)
            company.SetFormation(RegimentFormation.Line);
        state.AutoColumn = false;
        SetStatus(state, status, reason);
    }

    private static void SetStatus(State state, MarchStatus status, string reason)
    {
        if (state.Status == status && state.Reason == reason)
            return;
        state.Status = status;
        state.Reason = reason;
    }

    private State GetState(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (!states.TryGetValue(company, out State state))
        {
            state = new State();
            states[company] = state;
        }
        return state;
    }

    private float GetRemainingPathDistance(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (waypointsField == null || company == null || !company.IsMoving)
            return 0f;

        IList waypoints = waypointsField.GetValue(company) as IList;
        if (waypoints == null || waypoints.Count == 0)
            return 0f;

        float total = 0f;
        Vector3 previous = company.transform.position;
        for (int i = 0; i < waypoints.Count; i++)
        {
            object waypoint = waypoints[i];
            if (waypoint == null)
                continue;

            if (waypointPointField == null)
                waypointPointField = waypoint.GetType().GetField("WorldPoint");
            if (waypointPointField == null)
                continue;

            Vector3 point = (Vector3)waypointPointField.GetValue(waypoint);
            previous.y = 0f;
            point.y = 0f;
            total += Vector3.Distance(previous, point);
            previous = point;
        }

        return total;
    }

    private static float GetNearestEnemyDistance(
        PrototypeCompanyTacticalEntity09L2 company,
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies)
    {
        float best = float.PositiveInfinity;
        BattleTeam team = company.ParentRegiment.Team;
        Vector3 here = company.transform.position;

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 candidate = companies[i];
            if (candidate == null || candidate == company || candidate.CurrentStrength <= 0)
                continue;
            if (candidate.ParentRegiment == null || candidate.ParentRegiment.Team == team)
                continue;
            if (candidate.ParentRegiment.IsRouted)
                continue;

            Vector3 to = candidate.transform.position - here;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < best)
                best = dist;
        }

        return best;
    }
}
