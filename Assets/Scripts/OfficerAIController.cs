using UnityEngine;

public enum OfficerAIMission
{
    DefendArea,
    Hold,
    MoveToPoint,
    AttackTarget,
    AttackNearest
}

public enum OfficerAIDoctrine
{
    Defensive,
    Balanced,
    Offensive
}

public sealed class OfficerAIController : MonoBehaviour
{
    public bool AIEnabled { get; private set; }
    public OfficerProfile Officer { get; private set; }
    public OfficerAIMission Mission { get; private set; } = OfficerAIMission.Hold;
    public OfficerAIDoctrine Doctrine { get; private set; } = OfficerAIDoctrine.Defensive;
    public float OrderAggressiveness { get; private set; } = 50f;
    public string CurrentTask { get; private set; } = "MANUAL";
    public string ReasonCode { get; private set; } = "Direct player control";
    public Vector3 MissionPoint => missionPoint;
    public Vector3 DefendAnchor => defendAnchor;
    public Regiment MissionTarget => missionTarget;

    private Regiment regiment;
    private float thinkTimer;
    private Vector3 defendAnchor;
    private Vector3 missionPoint;
    private Regiment missionTarget;
    private bool returnToAttackNearestAfterMove;
    private string lastTelemetryKey = string.Empty;

    public void Configure(bool enabledAtStart, Vector3? initialWaypoint)
    {
        regiment = GetComponent<Regiment>();
        if (regiment == null)
        {
            enabled = false;
            Debug.LogError("OfficerAIController requires Regiment on the same GameObject.");
            return;
        }

        Officer = OfficerProfile.CreatePrototype(regiment.RegimentName);
        defendAnchor = regiment.transform.position;
        AIEnabled = enabledAtStart;

        if (regiment.Team == BattleTeam.Prussia)
        {
            Doctrine = OfficerAIDoctrine.Offensive;
            OrderAggressiveness = 65f;
            regiment.SetFirePolicy(RegimentFirePolicy.MediumRange);
        }
        else
        {
            Doctrine = OfficerAIDoctrine.Defensive;
            OrderAggressiveness = 35f;
            regiment.SetFirePolicy(RegimentFirePolicy.MediumRange);
        }

        if (enabledAtStart)
        {
            if (initialWaypoint.HasValue)
            {
                Mission = OfficerAIMission.MoveToPoint;
                missionPoint = initialWaypoint.Value;
                returnToAttackNearestAfterMove = true;
                SetStatus("MANOEUVRE", "Attacker flank/approach waypoint");
            }
            else
            {
                Mission = regiment.Team == BattleTeam.Prussia
                    ? OfficerAIMission.AttackNearest
                    : OfficerAIMission.DefendArea;
                SetStatus(
                    regiment.Team == BattleTeam.Prussia ? "ASSESS ATTACK" : "DEFEND",
                    regiment.Team == BattleTeam.Prussia
                        ? "Attacker assessing route and engagement distance"
                        : "Defender taking local control");
            }

            thinkTimer = 0.05f;
        }
        else
        {
            SetStatus("MANUAL", "Direct player control");
        }
    }

    private void Update()
    {
        if (!AIEnabled || regiment == null || regiment.IsRouted)
            return;

        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
            return;

        MakeDecision();
        ScheduleNextDecision();
    }

    public void ToggleAI()
    {
        SetAIEnabled(!AIEnabled);
    }

    public void SetAIEnabled(bool enabledValue)
    {
        if (regiment == null || regiment.IsRouted)
            return;

        AIEnabled = enabledValue;
        thinkTimer = 0f;
        missionTarget = null;
        returnToAttackNearestAfterMove = false;

        if (AIEnabled)
        {
            defendAnchor = regiment.transform.position;

            if (Doctrine == OfficerAIDoctrine.Offensive)
            {
                Mission = OfficerAIMission.AttackNearest;
                SetStatus("ASSESS ATTACK", "AI UNIT enabled with offensive doctrine");
            }
            else
            {
                Mission = OfficerAIMission.DefendArea;
                regiment.OrderHold();
                SetStatus("DEFEND", "AI UNIT enabled - defend current area");
            }
        }
        else
        {
            Mission = OfficerAIMission.Hold;
            SetStatus("MANUAL", "AI UNIT disabled");
        }
    }

    public void SetDoctrine(OfficerAIDoctrine value)
    {
        Doctrine = value;

        if (!AIEnabled || regiment == null || regiment.IsRouted)
            return;

        thinkTimer = 0f;

        if (Mission == OfficerAIMission.AttackTarget || Mission == OfficerAIMission.MoveToPoint)
        {
            SetStatus(CurrentTask, "Doctrine changed to " + Doctrine + " - explicit mission retained");
            return;
        }

        defendAnchor = regiment.transform.position;

        if (Doctrine == OfficerAIDoctrine.Offensive)
        {
            Mission = OfficerAIMission.AttackNearest;
            SetStatus("ASSESS ATTACK", "Offensive doctrine ordered");
        }
        else
        {
            Mission = OfficerAIMission.DefendArea;
            SetStatus("DEFEND", Doctrine + " doctrine ordered around current position");
        }
    }

    public void SetOrderAggressiveness(float value)
    {
        OrderAggressiveness = Mathf.Clamp(value, 0f, 100f);
        thinkTimer = 0f;

        if (AIEnabled)
            SetStatus(CurrentTask, "Commander aggression intent updated");
    }

    public float GetEffectiveAggressiveness()
    {
        if (Officer == null)
            return OrderAggressiveness;

        float blended = Officer.Aggressiveness * 0.70f + OrderAggressiveness * 0.30f;

        switch (Doctrine)
        {
            case OfficerAIDoctrine.Defensive:
                blended -= 12f;
                break;
            case OfficerAIDoctrine.Offensive:
                blended += 12f;
                break;
        }

        return Mathf.Clamp(blended, 0f, 100f);
    }

    public void SetMoveMission(Vector3 worldPoint)
    {
        if (!AIEnabled)
        {
            regiment.OrderMove(worldPoint);
            return;
        }

        Mission = OfficerAIMission.MoveToPoint;
        missionPoint = worldPoint;
        returnToAttackNearestAfterMove = false;
        thinkTimer = 0f;
        SetStatus("MOVE", "Player mission point");
    }

    public void SetAttackMission(Regiment target)
    {
        if (!AIEnabled)
        {
            regiment.OrderAttack(target);
            return;
        }

        if (target == null || target.Team == regiment.Team)
            return;

        Mission = OfficerAIMission.AttackTarget;
        missionTarget = target;
        thinkTimer = 0f;
        SetStatus("ATTACK", "Player designated target");
    }

    public void SetHoldMission()
    {
        if (!AIEnabled)
        {
            regiment.OrderHold();
            return;
        }

        Mission = OfficerAIMission.Hold;
        missionTarget = null;
        regiment.OrderHold();
        thinkTimer = 0f;
        SetStatus("HOLD", "Player hold mission");
    }

    private void MakeDecision()
    {
        float stress = GetStress01();

        if (Mission == OfficerAIMission.Hold)
        {
            regiment.OrderHold();
            SetStatus("HOLD", "Mission requires position to be held");
            return;
        }

        Regiment nearestEnemy = FindNearestEnemy(999f);

        if (Mission == OfficerAIMission.MoveToPoint)
        {
            float pointDistance = Vector3.Distance(regiment.transform.position, missionPoint);
            if (pointDistance <= 3f)
            {
                defendAnchor = regiment.transform.position;

                if (returnToAttackNearestAfterMove)
                {
                    Mission = OfficerAIMission.AttackNearest;
                    returnToAttackNearestAfterMove = false;
                    SetStatus("ASSESS ATTACK", "Manoeuvre point reached - reassessing attack");
                }
                else
                {
                    Mission = Doctrine == OfficerAIDoctrine.Offensive
                        ? OfficerAIMission.AttackNearest
                        : OfficerAIMission.DefendArea;
                    SetStatus(
                        Doctrine == OfficerAIDoctrine.Offensive ? "ASSESS ATTACK" : "DEFEND",
                        "Mission point reached");
                }

                return;
            }

            bool immediateThreat = nearestEnemy != null &&
                                   Vector3.Distance(regiment.transform.position, nearestEnemy.transform.position) <= regiment.MaximumRange;

            float independencePressure = GetEffectiveAggressiveness() - Officer.Discipline;

            if (!immediateThreat || independencePressure < 12f)
            {
                if (Officer.TacticalSkill >= 55f && pointDistance > regiment.EffectiveRange * 1.25f)
                    regiment.SetFormation(RegimentFormation.Column);

                regiment.OrderMove(missionPoint);
                SetStatus(
                    "MANOEUVRE",
                    immediateThreat
                        ? "Discipline keeps unit on assigned movement"
                        : "Moving to assigned mission point");
                return;
            }

            EngageEnemy(nearestEnemy, stress, "Aggressive local reaction during movement");
            return;
        }

        if (Mission == OfficerAIMission.DefendArea)
        {
            if (nearestEnemy == null)
            {
                regiment.OrderHold();
                SetStatus("DEFEND", "No enemy currently available");
                return;
            }

            float enemyDistance = Vector3.Distance(regiment.transform.position, nearestEnemy.transform.position);
            float distanceFromAnchor = Vector3.Distance(regiment.transform.position, defendAnchor);
            float aggression01 = GetEffectiveAggressiveness() / 100f;

            float doctrineAdvance;
            switch (Doctrine)
            {
                case OfficerAIDoctrine.Offensive:
                    doctrineAdvance = Mathf.Lerp(10f, 28f, aggression01);
                    break;
                case OfficerAIDoctrine.Balanced:
                    doctrineAdvance = Mathf.Lerp(5f, 18f, aggression01);
                    break;
                default:
                    doctrineAdvance = Mathf.Lerp(2f, 11f, aggression01);
                    break;
            }

            if (enemyDistance > regiment.MaximumRange && distanceFromAnchor >= doctrineAdvance)
            {
                regiment.SetFormation(RegimentFormation.Line);
                regiment.OrderHold();
                SetStatus("DEFEND", "Holding assigned defensive area");
                return;
            }

            EngageEnemy(nearestEnemy, stress, "Threat inside local defensive decision space");
            return;
        }

        Regiment target = Mission == OfficerAIMission.AttackTarget ? missionTarget : nearestEnemy;
        if (target == null || target.IsRouted)
        {
            missionTarget = null;
            if (Mission == OfficerAIMission.AttackTarget)
                Mission = OfficerAIMission.AttackNearest;

            target = FindNearestEnemy(999f);
            if (target == null)
            {
                regiment.OrderHold();
                SetStatus("ASSESS", "No valid enemy target");
                return;
            }
        }

        EngageEnemy(
            target,
            stress,
            Mission == OfficerAIMission.AttackTarget
                ? "Executing designated attack"
                : "Attacker assessing nearest enemy");
    }

    private void EngageEnemy(Regiment target, float stress, string baseReason)
    {
        if (target == null)
            return;

        float stability = (
            Officer.Leadership * 0.35f +
            Officer.Inspiration * 0.25f +
            Officer.Composure * 0.40f) / 100f;
        float stabiliseThreshold = Mathf.Lerp(46f, 24f, stability);
        float effectiveAggressiveness = GetEffectiveAggressiveness();

        if (regiment.Morale < stabiliseThreshold && effectiveAggressiveness < 72f)
        {
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            SetStatus("STABILISE", "Morale pressure - officer pauses advance");
            return;
        }

        float distance = Vector3.Distance(regiment.transform.position, target.transform.position);
        float aggression01 = effectiveAggressiveness / 100f;
        float tactical01 = Officer.TacticalSkill / 100f;

        float preferredRangeFactor = Mathf.Lerp(0.98f, 0.68f, aggression01);
        preferredRangeFactor *= Mathf.Lerp(0.96f, 1.04f, tactical01);

        float noise = GetDecisionNoise(stress);
        preferredRangeFactor *= 1f + Random.Range(-noise, noise);

        float preferredRange = Mathf.Clamp(
            regiment.EffectiveRange * preferredRangeFactor,
            regiment.EffectiveRange * 0.58f,
            regiment.EffectiveRange * 1.02f);

        float fireTriggerRange = regiment.GetFireTriggerRange();
        if (regiment.FirePolicy == RegimentFirePolicy.HoldFire)
        {
            preferredRange = Mathf.Min(preferredRange, regiment.CloseRange * 0.90f);
        }
        else
        {
            preferredRange = Mathf.Min(preferredRange, fireTriggerRange * 0.96f);
        }

        if (distance > preferredRange + 1.0f)
        {
            if (Officer.TacticalSkill >= 58f && distance > regiment.EffectiveRange * 1.35f)
                regiment.SetFormation(RegimentFormation.Column);
            else
                regiment.SetFormation(RegimentFormation.Line);

            Vector3 toward = target.transform.position - regiment.transform.position;
            toward.y = 0f;

            if (toward.sqrMagnitude > 0.01f)
            {
                float closeDistance = Mathf.Max(0f, distance - preferredRange);
                Vector3 closePoint = regiment.transform.position + toward.normalized * closeDistance;
                regiment.OrderMove(closePoint);
            }

            SetStatus(
                "MANOEUVRE/ATTACK",
                baseReason + " - closing to officer/order preferred range");
        }
        else
        {
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            regiment.OrderAttack(target);

            string fireConstraint = regiment.FirePolicy == RegimentFirePolicy.HoldFire
                ? " - HOLD FIRE"
                : " - fire policy " + regiment.GetFirePolicyLabel();

            SetStatus("ENGAGE", baseReason + " - preferred engagement range reached" + fireConstraint);
        }
    }

    private void ScheduleNextDecision()
    {
        if (Officer == null)
        {
            thinkTimer = 1f;
            return;
        }

        float commandQuality = (Officer.Initiative * 0.62f + Officer.StaffCommandSkill * 0.38f) / 100f;
        float baseDelay = Mathf.Lerp(1.55f, 0.52f, commandQuality);
        float stress = GetStress01();
        float composure01 = Officer.Composure / 100f;
        float stressPenalty = 1f + stress * Mathf.Lerp(0.95f, 0.08f, composure01);

        BattleManager manager = BattleManager.Instance;
        float difficultyMultiplier = manager != null
            ? manager.GetAIReactionMultiplier(regiment.Team)
            : 1f;

        thinkTimer = baseDelay * stressPenalty * difficultyMultiplier * Random.Range(0.92f, 1.08f);
    }

    private float GetDecisionNoise(float stress)
    {
        BattleManager manager = BattleManager.Instance;
        float difficultyNoise = manager != null
            ? manager.GetAIDecisionNoise(regiment.Team)
            : 0.10f;

        float experienceFactor = Mathf.Lerp(1.0f, 0.55f, Officer.Experience / 100f);
        float composureFactor = Mathf.Lerp(1.0f, 0.58f, Officer.Composure / 100f);
        float stressFactor = 1f + stress * composureFactor;

        return Mathf.Clamp(difficultyNoise * experienceFactor * stressFactor, 0.01f, 0.30f);
    }

    private float GetStress01()
    {
        float moraleStress = 1f - regiment.Morale / 100f;
        float cohesionStress = 1f - regiment.Cohesion / 100f;

        return Mathf.Clamp01(moraleStress * 0.58f + cohesionStress * 0.42f);
    }

    private Regiment FindNearestEnemy(float maxDistance)
    {
        BattleManager manager = BattleManager.Instance;
        if (manager == null)
            return null;

        Regiment nearest = null;
        float best = maxDistance;

        foreach (Regiment candidate in manager.Regiments)
        {
            if (candidate == null || candidate == regiment || candidate.Team == regiment.Team || candidate.IsRouted)
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

    private void SetStatus(string task, string reason)
    {
        CurrentTask = task;
        ReasonCode = reason;

        string telemetryKey = task + "|" + reason;
        if (telemetryKey == lastTelemetryKey || regiment == null)
            return;

        lastTelemetryKey = telemetryKey;
        string difficulty = OfficerAIPrototypeManager.CurrentDifficulty.ToString();

        Debug.Log(string.Format(
            "AI-DIAG|Unit={0}|Team={1}|Officer={2}|AI={3}|Difficulty={4}|Doctrine={5}|OrderAgg={6:0}|EffectiveAgg={7:0}|Fire={8}|Mission={9}|Task={10}|Reason={11}",
            regiment.RegimentName,
            regiment.Team,
            Officer != null ? Officer.OfficerName : "None",
            AIEnabled,
            difficulty,
            Doctrine,
            OrderAggressiveness,
            GetEffectiveAggressiveness(),
            regiment.GetFirePolicyLabel(),
            Mission,
            task,
            reason));
    }
}
