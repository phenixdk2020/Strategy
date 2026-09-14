using UnityEngine;

public enum TacticalWithdrawalMode
{
    None,
    FightingWithdrawal,
    BreakContact,
    RallyMarch
}

/// <summary>
/// Campaign3 v00.00.10n5 tactical withdrawal controller.
/// Keeps controlled withdrawal, emergency break-contact and normal rally movement
/// separate from the existing uncontrolled Regiment rout state.
/// </summary>
[DefaultExecutionOrder(200)]
public sealed class PrototypeWithdrawalControllerV010N5 : MonoBehaviour
{
    public TacticalWithdrawalMode Mode { get; private set; } = TacticalWithdrawalMode.None;
    public bool HasRallyPoint { get; private set; }
    public Vector3 RallyPoint { get; private set; }

    public string StatusLabel
    {
        get
        {
            switch (Mode)
            {
                case TacticalWithdrawalMode.FightingWithdrawal: return "KÆMPENDE TILBAGETRÆKNING";
                case TacticalWithdrawalMode.BreakContact: return "BRYD KONTAKT";
                case TacticalWithdrawalMode.RallyMarch: return "MOD SAMLEPUNKT";
                default: return "INGEN";
            }
        }
    }

    private Regiment regiment;
    private OfficerAIController officerAI;
    private Regiment threat;
    private bool restoreOfficerAI;
    private bool savedFirePolicyValid;
    private RegimentFirePolicy savedFirePolicy;
    private bool movingPhase;
    private float phaseTimer;
    private float clearTimer;
    private Vector3 breakDirection;

    private const float FightingStepDistance = 8f;
    private const float FightingMoveSeconds = 1.35f;
    private const float FightingFireMinimumSeconds = 1.15f;
    private const float ContactRangeMargin = 3f;
    private const float ClearConfirmationSeconds = 1.0f;
    private const float RallyArrivalDistance = 3.25f;
    private const float BreakContactStepDistance = 20f;
    private const float BreakContactExtraSpeed = 1.85f;

    private void Awake()
    {
        regiment = GetComponent<Regiment>();
        officerAI = GetComponent<OfficerAIController>();
        if (regiment == null)
            enabled = false;
    }

    public void BeginWithdrawal(Vector3 rallyPoint)
    {
        if (regiment == null || regiment.IsRouted)
            return;

        RallyPoint = rallyPoint;
        RallyPoint.y = PrototypeBootstrap.SampleGroundHeight(RallyPoint.x, RallyPoint.z) + 0.10f;
        HasRallyPoint = true;
        CaptureOfficerControl();

        threat = FindNearestEnemy();
        clearTimer = 0f;

        if (IsCombatContact(threat))
            StartFightingWithdrawal();
        else
            StartRallyMarch();

        Debug.Log(
            "WITHDRAW-10N5|Unit=" + regiment.RegimentName +
            "|Order=WithdrawToHere|Mode=" + Mode +
            "|Rally=" + RallyPoint.ToString("F1"));
    }

    public void BeginBreakContact()
    {
        if (regiment == null || regiment.IsRouted)
            return;

        CaptureOfficerControl();
        SaveFirePolicy();
        regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
        threat = FindNearestEnemy();
        clearTimer = 0f;
        Mode = TacticalWithdrawalMode.BreakContact;

        if (threat == null)
        {
            if (HasRallyPoint)
                StartRallyMarch();
            else
                CompleteOrder(true);
            return;
        }

        UpdateBreakDirection();
        regiment.SetFormation(RegimentFormation.Column);
        regiment.OrderMove(regiment.transform.position + breakDirection * BreakContactStepDistance);

        Debug.Log(
            "WITHDRAW-10N5|Unit=" + regiment.RegimentName +
            "|Order=BreakContact|HasRally=" + HasRallyPoint);
    }

    public void CancelWithdrawal()
    {
        if (regiment == null)
            return;

        Mode = TacticalWithdrawalMode.None;
        HasRallyPoint = false;
        RestoreFirePolicy();
        regiment.OrderHold();
        RestoreOfficerControl();
    }

    private void Update()
    {
        if (regiment == null || regiment.IsRouted)
        {
            if (Mode != TacticalWithdrawalMode.None)
                FinishForRout();
            return;
        }

        switch (Mode)
        {
            case TacticalWithdrawalMode.FightingWithdrawal:
                UpdateFightingWithdrawal();
                break;
            case TacticalWithdrawalMode.BreakContact:
                UpdateBreakContact();
                break;
            case TacticalWithdrawalMode.RallyMarch:
                UpdateRallyMarch();
                break;
        }
    }

    private void LateUpdate()
    {
        if (regiment == null || regiment.IsRouted)
            return;

        if (Mode == TacticalWithdrawalMode.FightingWithdrawal && threat != null && !threat.IsRouted)
        {
            FaceThreat(threat, 10f);
            return;
        }

        if (Mode == TacticalWithdrawalMode.BreakContact && breakDirection.sqrMagnitude > 0.01f)
        {
            // Emergency disengagement is deliberately faster and less orderly than
            // standard movement. Regiment still performs its normal movement; this
            // additional displacement represents men running rather than marching.
            Vector3 next = regiment.transform.position + breakDirection.normalized * BreakContactExtraSpeed * Time.deltaTime;
            next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
            regiment.transform.position = next;
            regiment.transform.rotation = Quaternion.Slerp(
                regiment.transform.rotation,
                Quaternion.LookRotation(breakDirection.normalized, Vector3.up),
                9f * Time.deltaTime);
        }
    }

    private void StartFightingWithdrawal()
    {
        Mode = TacticalWithdrawalMode.FightingWithdrawal;
        regiment.SetFormation(RegimentFormation.Line);
        movingPhase = true;
        phaseTimer = 0f;
        BeginFightingMovePhase();
    }

    private void UpdateFightingWithdrawal()
    {
        if (threat == null || threat.IsRouted)
            threat = FindNearestEnemy();

        if (!IsCombatContact(threat))
        {
            clearTimer += Time.deltaTime;
            regiment.OrderHold();
            if (clearTimer >= ClearConfirmationSeconds)
                StartRallyMarch();
            return;
        }

        clearTimer = 0f;
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
            return;

        if (movingPhase)
            BeginFightingFirePhase();
        else
            BeginFightingMovePhase();
    }

    private void BeginFightingMovePhase()
    {
        movingPhase = true;
        phaseTimer = FightingMoveSeconds;

        if (threat == null)
            threat = FindNearestEnemy();

        Vector3 retreatDirection = GetControlledRetreatDirection();
        Vector3 step = regiment.transform.position + retreatDirection * FightingStepDistance;
        step.y = PrototypeBootstrap.SampleGroundHeight(step.x, step.z) + 0.10f;
        regiment.OrderMove(step);
    }

    private void BeginFightingFirePhase()
    {
        movingPhase = false;
        regiment.OrderHold();
        if (threat != null)
            FaceThreat(threat, 20f);

        phaseTimer = Mathf.Max(FightingFireMinimumSeconds, regiment.CurrentReloadSeconds * 0.48f);
    }

    private Vector3 GetControlledRetreatDirection()
    {
        Vector3 away = threat != null
            ? regiment.transform.position - threat.transform.position
            : -regiment.transform.forward;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -regiment.transform.forward;
        away.Normalize();

        Vector3 towardRally = HasRallyPoint ? RallyPoint - regiment.transform.position : away;
        towardRally.y = 0f;
        if (towardRally.sqrMagnitude > 0.01f)
            towardRally.Normalize();
        else
            towardRally = away;

        Vector3 combined = away * 0.82f + towardRally * 0.18f;
        if (combined.sqrMagnitude < 0.01f)
            combined = away;
        return combined.normalized;
    }

    private void UpdateBreakContact()
    {
        if (threat == null || threat.IsRouted)
            threat = FindNearestEnemy();

        if (threat == null)
        {
            clearTimer += Time.deltaTime;
        }
        else
        {
            float distance = Vector3.Distance(regiment.transform.position, threat.transform.position);
            float safeDistance = Mathf.Max(regiment.MaximumRange, threat.MaximumRange) * 1.22f + ContactRangeMargin;
            clearTimer = distance > safeDistance ? clearTimer + Time.deltaTime : 0f;
        }

        if (clearTimer >= ClearConfirmationSeconds)
        {
            RestoreFirePolicy();
            if (HasRallyPoint)
                StartRallyMarch();
            else
                CompleteOrder(true);
            return;
        }

        UpdateBreakDirection();
        if (breakDirection.sqrMagnitude < 0.01f)
            return;

        regiment.SetFormation(RegimentFormation.Column);
        Vector3 target = regiment.transform.position + breakDirection * BreakContactStepDistance;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        regiment.OrderMove(target);
    }

    private void UpdateBreakDirection()
    {
        if (threat == null)
        {
            breakDirection = -regiment.transform.forward;
            breakDirection.y = 0f;
            if (breakDirection.sqrMagnitude < 0.01f)
                breakDirection = Vector3.back;
            breakDirection.Normalize();
            return;
        }

        breakDirection = regiment.transform.position - threat.transform.position;
        breakDirection.y = 0f;
        if (breakDirection.sqrMagnitude < 0.01f)
            breakDirection = -regiment.transform.forward;
        breakDirection.Normalize();
    }

    private void StartRallyMarch()
    {
        RestoreFirePolicy();

        if (!HasRallyPoint)
        {
            CompleteOrder(true);
            return;
        }

        Mode = TacticalWithdrawalMode.RallyMarch;
        regiment.SetFormation(RegimentFormation.Column);
        regiment.OrderMove(RallyPoint);
    }

    private void UpdateRallyMarch()
    {
        Vector3 delta = RallyPoint - regiment.transform.position;
        delta.y = 0f;

        if (delta.sqrMagnitude <= RallyArrivalDistance * RallyArrivalDistance)
        {
            CompleteOrder(true);
            return;
        }

        regiment.OrderMove(RallyPoint);
    }

    private bool IsCombatContact(Regiment enemy)
    {
        if (enemy == null || enemy.IsRouted)
            return false;

        float distance = Vector3.Distance(regiment.transform.position, enemy.transform.position);
        float contactRange = Mathf.Max(regiment.MaximumRange, enemy.MaximumRange) * 1.08f + ContactRangeMargin;
        return distance <= contactRange;
    }

    private Regiment FindNearestEnemy()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = float.MaxValue;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == regiment.Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(regiment.transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void FaceThreat(Regiment enemy, float turnRate)
    {
        if (enemy == null)
            return;

        Vector3 toEnemy = enemy.transform.position - regiment.transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.01f)
            return;

        regiment.transform.rotation = Quaternion.Slerp(
            regiment.transform.rotation,
            Quaternion.LookRotation(toEnemy.normalized, Vector3.up),
            turnRate * Time.deltaTime);
    }

    private void CaptureOfficerControl()
    {
        if (Mode != TacticalWithdrawalMode.None)
            return;

        officerAI = officerAI != null ? officerAI : GetComponent<OfficerAIController>();
        if (officerAI != null && officerAI.AIEnabled)
        {
            restoreOfficerAI = true;
            officerAI.SetAIEnabled(false);
        }
    }

    private void RestoreOfficerControl()
    {
        if (!restoreOfficerAI)
            return;

        restoreOfficerAI = false;
        officerAI = officerAI != null ? officerAI : GetComponent<OfficerAIController>();
        if (officerAI != null && !regiment.IsRouted)
            officerAI.SetAIEnabled(true);
    }

    private void SaveFirePolicy()
    {
        if (savedFirePolicyValid)
            return;
        savedFirePolicy = regiment.FirePolicy;
        savedFirePolicyValid = true;
    }

    private void RestoreFirePolicy()
    {
        if (!savedFirePolicyValid)
            return;
        regiment.SetFirePolicy(savedFirePolicy);
        savedFirePolicyValid = false;
    }

    private void CompleteOrder(bool hold)
    {
        if (hold)
            regiment.OrderHold();

        Mode = TacticalWithdrawalMode.None;
        clearTimer = 0f;
        phaseTimer = 0f;
        movingPhase = false;
        threat = null;
        breakDirection = Vector3.zero;
        RestoreFirePolicy();
        RestoreOfficerControl();

        Debug.Log(
            "WITHDRAW-10N5|Unit=" + regiment.RegimentName +
            "|Complete=True|Rally=" + HasRallyPoint);
    }

    private void FinishForRout()
    {
        Mode = TacticalWithdrawalMode.None;
        RestoreFirePolicy();
        restoreOfficerAI = false;
        threat = null;
        breakDirection = Vector3.zero;
    }
}
