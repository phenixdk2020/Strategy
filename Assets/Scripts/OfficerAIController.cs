using UnityEngine;

public enum OfficerAIMission
{
    DefendArea,
    Hold,
    MoveToPoint,
    AttackTarget,
    AttackNearest
}

public sealed class OfficerAIController : MonoBehaviour
{
    public bool AIEnabled { get; private set; }
    public OfficerProfile Officer { get; private set; }
    public OfficerAIMission Mission { get; private set; } = OfficerAIMission.Hold;
    public string CurrentTask { get; private set; } = "MANUAL";
    public string ReasonCode { get; private set; } = "Direct player control";

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

        if (enabledAtStart)
        {
            if (initialWaypoint.HasValue)
            {
                Mission = OfficerAIMission.MoveToPoint;
                missionPoint = initialWaypoint.Value;
                returnToAttackNearestAfterMove = true;
                SetStatus("ADVANCE", "Initial mission waypoint");
            }
            else
            {
                Mission = regiment.Team == BattleTeam.Prussia
                    ? OfficerAIMission.AttackNearest
                    : OfficerAIMission.DefendArea;
                SetStatus("ASSESS", "Officer taking local control");
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
            Mission = OfficerAIMission.DefendArea;
            regiment.OrderHold();
            SetStatus("ASSESS", "AI UNIT enabled - defend current area");
        }
        else
        {
            Mission = OfficerAIMission.Hold;
            SetStatus("MANUAL", "AI UNIT disabled");
        }
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
                    SetStatus("ASSESS", "Waypoint reached - seeking enemy");
                }
                else
                {
                    Mission = OfficerAIMission.DefendArea;
                    SetStatus("DEFEND", "Mission point reached");
                }
                return;
            }

            bool immediateThreat = nearestEnemy != null &&
                                   Vector3.Distance(regiment.transform.position, nearestEnemy.transform.position) <= regiment.MaximumRange;
            float independencePressure = Officer.Aggressiveness - Officer.Discipline;

            if (!immediateThreat || independencePressure < 12f)
            {
                if (Officer.TacticalSkill >= 55f && pointDistance > regiment.EffectiveRange * 1.25f)
                    regiment.SetFormation(RegimentFormation.Column);

                regiment.OrderMove(missionPoint);
                SetStatus("ADVANCE", immediateThreat
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
            float permittedAdvance = Mathf.Lerp(2f, 14f, Officer.Aggressiveness / 100f);

            if (enemyDistance > regiment.MaximumRange && distanceFromAnchor >= permittedAdvance)
            {
                regiment.OrderHold();
                SetStatus("DEFEND", "Holding assigned local area");
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

        EngageEnemy(target, stress,
            Mission == OfficerAIMission.AttackTarget ? "Executing designated attack" : "Seeking nearest enemy");
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

        if (regiment.Morale < stabiliseThreshold && Officer.Aggressiveness < 72f)
        {
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            SetStatus("STABILISE", "Morale pressure - officer pauses advance");
            return;
        }

        float distance = Vector3.Distance(regiment.transform.position, target.transform.position);
        float aggression01 = Officer.Aggressiveness / 100f;
        float tactical01 = Officer.TacticalSkill / 100f;
        float preferredRangeFactor = Mathf.Lerp(0.96f, 0.72f, aggression01);
        preferredRangeFactor *= Mathf.Lerp(0.96f, 1.04f, tactical01);

        float noise = GetDecisionNoise(stress);
        preferredRangeFactor *= 1f + Random.Range(-noise, noise);
        float preferredRange = Mathf.Clamp(
            regiment.EffectiveRange * preferredRangeFactor,
            regiment.EffectiveRange * 0.62f,
            regiment.EffectiveRange * 1.02f);

        if (distance > preferredRange)
        {
            if (Officer.TacticalSkill >= 58f && distance > regiment.EffectiveRange * 1.35f)
                regiment.SetFormation(RegimentFormation.Column);
            else
                regiment.SetFormation(RegimentFormation.Line);

            regiment.OrderAttack(target);
            SetStatus("ADVANCE/ATTACK", baseReason + " - closing to preferred range");
        }
        else
        {
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            regiment.OrderAttack(target);
            SetStatus("ENGAGE", baseReason + " - preferred engagement range reached");
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
            "AI-DIAG|Unit={0}|Team={1}|Officer={2}|AI={3}|Difficulty={4}|Mission={5}|Task={6}|Reason={7}",
            regiment.RegimentName,
            regiment.Team,
            Officer != null ? Officer.OfficerName : "None",
            AIEnabled,
            difficulty,
            Mission,
            task,
            reason));
    }
}
