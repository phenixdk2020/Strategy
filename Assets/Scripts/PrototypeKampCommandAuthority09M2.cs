using System.Collections.Generic;
using UnityEngine;

public enum PrototypeKampCommandAuthorityLevel09M2
{
    Manual,
    RegimentOfficer,
    BrigadeOfficer
}

// v00.00.09m2 - bind manual / regiment-officer / brigade-officer to detached companies.
[DefaultExecutionOrder(800)]
public sealed class PrototypeKampCommandAuthority09M2 : MonoBehaviour
{
    public static PrototypeKampCommandAuthority09M2 Instance { get; private set; }

    private readonly Dictionary<Regiment, float> nextIssue = new Dictionary<Regiment, float>();
    private readonly Dictionary<Regiment, bool> playerOverride = new Dictionary<Regiment, bool>();
    private readonly Dictionary<Regiment, bool> localOfficer = new Dictionary<Regiment, bool>();
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampCommandAuthority09M2>() != null)
            return;
        GameObject root = new GameObject("PrototypeKampCommandAuthority_v000009m2");
        root.AddComponent<PrototypeKampCommandAuthority09M2>();
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

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        CaptureManualOverride(control);
        SyncPivotsAndIssue(control);

        if (!announced)
        {
            announced = true;
            Debug.Log("KAMP-AUTH-09M2|Installed=True|Levels=Manual|RegimentOfficer|BrigadeOfficer|CompanyDetachment=Preserved");
        }
    }

    public PrototypeKampCommandAuthorityLevel09M2 GetLevel(Regiment regiment)
    {
        if (regiment == null)
            return PrototypeKampCommandAuthorityLevel09M2.Manual;
        if (IsPlayerOverride(regiment))
            return PrototypeKampCommandAuthorityLevel09M2.Manual;
        if (IsLocalOfficer(regiment))
            return PrototypeKampCommandAuthorityLevel09M2.RegimentOfficer;
        if (IsBrigadeCommanding(regiment))
            return PrototypeKampCommandAuthorityLevel09M2.BrigadeOfficer;
        OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
        if (officer != null && officer.AIEnabled)
            return PrototypeKampCommandAuthorityLevel09M2.RegimentOfficer;
        return PrototypeKampCommandAuthorityLevel09M2.Manual;
    }

    public string GetLevelLabel(Regiment regiment)
    {
        switch (GetLevel(regiment))
        {
            case PrototypeKampCommandAuthorityLevel09M2.BrigadeOfficer:
                return "BRIGADE";
            case PrototypeKampCommandAuthorityLevel09M2.RegimentOfficer:
                return "REG-AI";
            default:
                return "MANUEL";
        }
    }

    public void SetManual(Regiment regiment)
    {
        if (regiment == null)
            return;
        playerOverride[regiment] = true;
        localOfficer[regiment] = false;
        OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
        if (officer != null && officer.AIEnabled)
            officer.SetAIEnabled(false);
    }

    public void SetRegimentOfficer(Regiment regiment)
    {
        if (regiment == null)
            return;
        playerOverride[regiment] = false;
        localOfficer[regiment] = true;
        OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
        if (officer != null && !officer.AIEnabled)
            officer.SetAIEnabled(true);
    }

    public void SetBrigadeOfficer(Regiment regiment)
    {
        PrototypeBrigadeCommand09L brigade = PrototypeBrigadeCommand09L.Instance;
        if (brigade == null)
        {
            SetRegimentOfficer(regiment);
            return;
        }
        if (regiment != null)
        {
            playerOverride[regiment] = false;
            localOfficer[regiment] = false;
        }
        if (regiment == null || regiment.Team == BattleTeam.Denmark)
            brigade.SetDanishAIEnabled(true);
    }

    public static bool ShouldKeepOfficerAI(Regiment regiment)
    {
        if (regiment == null)
            return false;
        PrototypeKampCommandAuthority09M2 auth = Instance;
        if (auth == null)
            return false;
        if (auth.IsPlayerOverride(regiment))
            return false;
        OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
        return officer != null && officer.AIEnabled;
    }

    public void ClearDanishBrigadeOverrides(Regiment regiment)
    {
        if (regiment == null)
            return;
        playerOverride[regiment] = false;
        localOfficer[regiment] = false;
    }

    public bool BrigadeMayCommand(Regiment regiment)
    {
        return regiment != null && !IsPlayerOverride(regiment) && !IsLocalOfficer(regiment);
    }

    private void CaptureManualOverride(PrototypeCompanyTacticalControl09L2 control)
    {
        bool overUi = BattleManager.Instance != null &&
                      BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);
        if (overUi || !control.HasCompanySelection || !Input.GetMouseButtonDown(1))
            return;
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment parent = selected[i] != null ? selected[i].ParentRegiment : null;
            if (parent == null || parent.Team != BattleTeam.Denmark)
                continue;
            SetManual(parent);
        }
    }

    private void SyncPivotsAndIssue(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<Regiment> parents = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || company.CurrentStrength <= 0)
                continue;
            parents.Add(company.ParentRegiment);
        }

        foreach (Regiment regiment in parents)
        {
            Vector3 centroid;
            Vector3 facing;
            if (!TryGetFormationPose(control, regiment, out centroid, out facing))
                continue;
            SnapEmptyPivot(regiment, centroid, facing);
            OfficerAIController officer = regiment.GetComponent<OfficerAIController>();
            if (officer == null || !officer.AIEnabled || IsPlayerOverride(regiment))
                continue;
            float due;
            if (nextIssue.TryGetValue(regiment, out due) && Time.time < due)
                continue;
            nextIssue[regiment] = Time.time + 0.70f;
            IssueFromOfficer(control, regiment, officer, centroid, facing);
        }
    }

    private void IssueFromOfficer(PrototypeCompanyTacticalControl09L2 control, Regiment regiment, OfficerAIController officer, Vector3 centroid, Vector3 currentFacing)
    {
        Vector3 intent;
        Vector3 facing;
        bool hold;
        ResolveIntent(regiment, officer, centroid, currentFacing, out intent, out facing, out hold);
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        if (hold)
        {
            for (int i = 0; i < companies.Count; i++)
            {
                PrototypeCompanyTacticalEntity09L2 company = companies[i];
                if (company == null || company.ParentRegiment != regiment)
                    continue;
                company.Hold();
                if (facing.sqrMagnitude > 0.01f)
                    company.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
            return;
        }
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment != regiment || company.CurrentStrength <= 0)
                continue;
            Vector3 world = intent + Quaternion.LookRotation(facing, Vector3.up) * company.DefaultLocalPosition;
            world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.10f;
            company.IssueMove(world, facing, true, false);
        }
    }

    private static void ResolveIntent(Regiment regiment, OfficerAIController officer, Vector3 centroid, Vector3 currentFacing, out Vector3 intent, out Vector3 facing, out bool hold)
    {
        intent = centroid;
        facing = currentFacing;
        hold = false;
        Regiment target = officer.MissionTarget;
        if (officer.Mission == OfficerAIMission.AttackNearest || (officer.Mission == OfficerAIMission.AttackTarget && (target == null || target.IsRouted)))
            target = FindNearestEnemy(regiment, 999f);
        switch (officer.Mission)
        {
            case OfficerAIMission.Hold:
                hold = true;
                return;
            case OfficerAIMission.DefendArea:
                if (target == null)
                {
                    hold = true;
                    intent = officer.DefendAnchor;
                    return;
                }
                if (Horizontal(centroid, target.transform.position) > regiment.MaximumRange + 6f)
                {
                    hold = true;
                    intent = officer.DefendAnchor;
                    FaceToward(target.transform.position, centroid, ref facing);
                    return;
                }
                CloseOrHold(regiment, centroid, target.transform.position, out intent, out facing, out hold);
                return;
            case OfficerAIMission.MoveToPoint:
                intent = officer.MissionPoint;
                FaceToward(intent, centroid, ref facing);
                if (Horizontal(centroid, intent) <= 4.5f)
                    hold = true;
                return;
            default:
                if (target == null)
                {
                    hold = true;
                    return;
                }
                CloseOrHold(regiment, centroid, target.transform.position, out intent, out facing, out hold);
                return;
        }
    }

    private static void CloseOrHold(Regiment regiment, Vector3 centroid, Vector3 targetPoint, out Vector3 intent, out Vector3 facing, out bool hold)
    {
        Vector3 to = targetPoint - centroid;
        to.y = 0f;
        facing = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
        float distance = to.magnitude;
        float preferred = Mathf.Clamp(regiment.GetFireTriggerRange() > 1f ? regiment.GetFireTriggerRange() * 0.92f : regiment.EffectiveRange * 0.84f, 8f, regiment.MaximumRange);
        if (distance <= preferred + 1.5f)
        {
            intent = centroid;
            hold = true;
            return;
        }
        intent = centroid + facing * Mathf.Max(2f, distance - preferred);
        intent.y = PrototypeBootstrap.SampleGroundHeight(intent.x, intent.z) + 0.10f;
        hold = false;
    }

    private static void FaceToward(Vector3 target, Vector3 from, ref Vector3 facing)
    {
        Vector3 to = target - from;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            facing = to.normalized;
    }

    private static void SnapEmptyPivot(Regiment regiment, Vector3 centroid, Vector3 facing)
    {
        Vector3 pos = centroid;
        pos.y = PrototypeBootstrap.SampleGroundHeight(pos.x, pos.z) + 0.10f;
        regiment.transform.position = pos;
        if (facing.sqrMagnitude > 0.01f)
            regiment.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        regiment.OrderHold();
    }

    private static bool TryGetFormationPose(PrototypeCompanyTacticalControl09L2 control, Regiment regiment, out Vector3 centroid, out Vector3 facing)
    {
        centroid = Vector3.zero;
        facing = Vector3.zero;
        int count = 0;
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment != regiment || company.CurrentStrength <= 0)
                continue;
            centroid += company.transform.position;
            Vector3 f = company.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f)
                facing += f.normalized;
            count++;
        }
        if (count <= 0)
            return false;
        centroid /= count;
        centroid.y = PrototypeBootstrap.SampleGroundHeight(centroid.x, centroid.z) + 0.10f;
        if (facing.sqrMagnitude < 0.01f)
            facing = regiment.transform.forward;
        facing.y = 0f;
        facing.Normalize();
        return true;
    }

    private static Regiment FindNearestEnemy(Regiment self, float maxDistance)
    {
        BattleManager manager = BattleManager.Instance;
        if (manager == null || self == null)
            return null;
        Regiment nearest = null;
        float best = maxDistance;
        IReadOnlyList<Regiment> list = manager.Regiments;
        for (int i = 0; i < list.Count; i++)
        {
            Regiment candidate = list[i];
            if (candidate == null || candidate == self || candidate.Team == self.Team || candidate.IsRouted)
                continue;
            float distance = Horizontal(self.transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private bool IsPlayerOverride(Regiment regiment)
    {
        bool value;
        return regiment != null && playerOverride.TryGetValue(regiment, out value) && value;
    }

    private bool IsLocalOfficer(Regiment regiment)
    {
        bool value;
        return regiment != null && localOfficer.TryGetValue(regiment, out value) && value;
    }

    private static bool IsBrigadeCommanding(Regiment regiment)
    {
        PrototypeBrigadeCommand09L brigade = PrototypeBrigadeCommand09L.Instance;
        if (brigade == null || regiment == null || !brigade.Contains(regiment))
            return false;
        return regiment.Team == BattleTeam.Denmark ? brigade.DanishAIEnabled : brigade.PrussianAIEnabled;
    }

    private static float Horizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
