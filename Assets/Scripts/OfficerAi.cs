using UnityEngine;

public enum OfficerMission
{
    Hold,
    DefendArea,
    AdvanceAttack
}

public enum AiDifficulty
{
    Easy,
    Normal,
    Hard
}

public struct OfficerProfile
{
    public string DisplayName;
    public float TacticalSkill;
    public float Initiative;
    public float Aggressiveness;

    public OfficerProfile(string displayName, float tacticalSkill, float initiative, float aggressiveness)
    {
        DisplayName = displayName;
        TacticalSkill = Mathf.Clamp(tacticalSkill, 0f, 100f);
        Initiative = Mathf.Clamp(initiative, 0f, 100f);
        Aggressiveness = Mathf.Clamp(aggressiveness, 0f, 100f);
    }

    public float Caution => 100f - Aggressiveness;
}

public struct OfficerDecision
{
    public string Task;
    public string ReasonCode;
    public Regiment Target;
    public Vector3? MoveTo;
    public bool HoldPosition;
    public float Score;
    public float ThinkDelay;
}

/// <summary>
/// Shared officer decision core for player-delegated and enemy regiments.
/// Difficulty only changes delay, noise and coordination — never combat stats.
/// </summary>
public static class OfficerDecisionCore
{
    public static OfficerProfile ProfileFor(string regimentName)
    {
        // Temporary QA profiles. Not historical judgements.
        switch (regimentName)
        {
            case "1. Regiment":
                return new OfficerProfile("Maj. Petersen", 74f, 42f, 28f);
            case "5. Regiment":
                return new OfficerProfile("Kapt. Holm", 46f, 78f, 84f);
            case "8th Regiment":
                return new OfficerProfile("Maj. von Stein", 81f, 54f, 40f);
            case "18th Regiment":
                return new OfficerProfile("Hptm. Krueger", 36f, 71f, 88f);
            default:
                return new OfficerProfile("Acting officer", 50f, 50f, 50f);
        }
    }

    public static OfficerMission DefaultMission(BattleTeam team, bool hasAdvanceWaypoint)
    {
        if (team == BattleTeam.Denmark)
            return OfficerMission.Hold;
        return hasAdvanceWaypoint ? OfficerMission.AdvanceAttack : OfficerMission.AdvanceAttack;
    }

    public static float NextThinkDelay(OfficerProfile officer, AiDifficulty difficulty)
    {
        float initiativeDelay = Mathf.Lerp(1.35f, 0.35f, officer.Initiative / 100f);
        float difficultyDelay;
        switch (difficulty)
        {
            case AiDifficulty.Easy:
                difficultyDelay = Random.Range(0.55f, 1.25f);
                break;
            case AiDifficulty.Hard:
                difficultyDelay = Random.Range(0f, 0.18f);
                break;
            default:
                difficultyDelay = Random.Range(0.08f, 0.28f);
                break;
        }

        return Mathf.Clamp(initiativeDelay + difficultyDelay, 0.28f, 2.4f);
    }

    public static OfficerDecision Decide(
        Regiment self,
        OfficerProfile officer,
        OfficerMission mission,
        Vector3 defendAnchor,
        Vector3? advancePoint,
        AiDifficulty difficulty)
    {
        Regiment threat = PerceiveThreat(self, officer, difficulty);
        float morale = self.Morale;
        float cohesion = self.Cohesion;
        float distance = threat != null
            ? Vector3.Distance(self.transform.position, threat.transform.position)
            : float.MaxValue;

        if (morale <= 28f || cohesion <= 28f)
        {
            Vector3 withdraw = self.transform.position +
                (self.Team == BattleTeam.Denmark ? Vector3.left : Vector3.right) * 18f;
            return new OfficerDecision
            {
                Task = "Local withdrawal",
                ReasonCode = "Withdrawal: morale critical",
                Target = null,
                MoveTo = withdraw,
                HoldPosition = false,
                Score = 90f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        if (mission == OfficerMission.Hold || mission == OfficerMission.DefendArea)
            return DecideHold(self, officer, threat, distance, defendAnchor, difficulty);

        return DecideAdvanceAttack(self, officer, threat, distance, advancePoint, difficulty);
    }

    private static OfficerDecision DecideHold(
        Regiment self,
        OfficerProfile officer,
        Regiment threat,
        float distance,
        Vector3 defendAnchor,
        AiDifficulty difficulty)
    {
        float anchorDistance = Vector3.Distance(self.transform.position, defendAnchor);
        float holdRange = Mathf.Lerp(self.MaximumRange * 0.92f, self.EffectiveRange * 0.84f, officer.Aggressiveness / 100f);

        if (anchorDistance > 7.5f && (threat == null || distance > self.EffectiveRange * 0.7f || officer.Caution > 55f))
        {
            return new OfficerDecision
            {
                Task = "Return to assigned ground",
                ReasonCode = "Holding assigned ground",
                Target = null,
                MoveTo = defendAnchor,
                HoldPosition = false,
                Score = 70f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        if (threat != null && distance <= holdRange)
        {
            bool closeIn = officer.Aggressiveness > 70f && distance > self.EffectiveRange * 0.78f && Random.value < officer.Aggressiveness / 160f;
            if (closeIn)
            {
                return new OfficerDecision
                {
                    Task = "Local counter-move",
                    ReasonCode = "Threat to front — engaging",
                    Target = threat,
                    MoveTo = threat.transform.position,
                    HoldPosition = false,
                    Score = 65f,
                    ThinkDelay = NextThinkDelay(officer, difficulty)
                };
            }

            return new OfficerDecision
            {
                Task = "Hold fire line",
                ReasonCode = officer.Caution >= 55f
                    ? "Officer cautious — holding fire line"
                    : "Threat to front — engaging",
                Target = threat,
                MoveTo = null,
                HoldPosition = true,
                Score = 80f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        if (threat != null && officer.Aggressiveness >= 75f && distance < self.MaximumRange * 1.35f)
        {
            return new OfficerDecision
            {
                Task = "Close to effective range",
                ReasonCode = "Closing to effective range",
                Target = threat,
                MoveTo = threat.transform.position,
                HoldPosition = false,
                Score = 55f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        return new OfficerDecision
        {
            Task = "Hold position",
            ReasonCode = "Holding assigned ground",
            Target = null,
            MoveTo = null,
            HoldPosition = true,
            Score = 40f,
            ThinkDelay = NextThinkDelay(officer, difficulty)
        };
    }

    private static OfficerDecision DecideAdvanceAttack(
        Regiment self,
        OfficerProfile officer,
        Regiment threat,
        float distance,
        Vector3? advancePoint,
        AiDifficulty difficulty)
    {
        if (advancePoint.HasValue && Vector3.Distance(self.transform.position, advancePoint.Value) > 5f)
        {
            bool threatBlocks = threat != null && distance < self.EffectiveRange * 0.95f;
            if (!threatBlocks)
            {
                return new OfficerDecision
                {
                    Task = "Advance to line",
                    ReasonCode = "Advancing to contact",
                    Target = null,
                    MoveTo = advancePoint.Value,
                    HoldPosition = false,
                    Score = 75f,
                    ThinkDelay = NextThinkDelay(officer, difficulty)
                };
            }
        }

        if (threat == null)
        {
            return new OfficerDecision
            {
                Task = "Await contact",
                ReasonCode = "Waiting for opportunity",
                Target = null,
                MoveTo = null,
                HoldPosition = true,
                Score = 20f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        float closeThreshold = Mathf.Lerp(self.MaximumRange * 0.90f, self.EffectiveRange * 0.72f, officer.Aggressiveness / 100f);
        if (distance > closeThreshold)
        {
            return new OfficerDecision
            {
                Task = "Close to effective range",
                ReasonCode = "Closing to effective range",
                Target = threat,
                MoveTo = threat.transform.position,
                HoldPosition = false,
                Score = 70f,
                ThinkDelay = NextThinkDelay(officer, difficulty)
            };
        }

        return new OfficerDecision
        {
            Task = "Engage target",
            ReasonCode = "Threat to front — engaging",
            Target = threat,
            MoveTo = null,
            HoldPosition = true,
            Score = 85f,
            ThinkDelay = NextThinkDelay(officer, difficulty)
        };
    }

    private static Regiment PerceiveThreat(Regiment self, OfficerProfile officer, AiDifficulty difficulty)
    {
        if (BattleManager.Instance == null)
            return null;

        Regiment best = null;
        float bestScore = float.MinValue;
        float noise;
        switch (difficulty)
        {
            case AiDifficulty.Easy:
                noise = Mathf.Lerp(18f, 8f, officer.TacticalSkill / 100f);
                break;
            case AiDifficulty.Hard:
                noise = Mathf.Lerp(6f, 0.5f, officer.TacticalSkill / 100f);
                break;
            default:
                noise = Mathf.Lerp(10f, 2f, officer.TacticalSkill / 100f);
                break;
        }

        foreach (Regiment candidate in BattleManager.Instance.Regiments)
        {
            if (candidate == null || candidate == self || candidate.Team == self.Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(self.transform.position, candidate.transform.position);
            float score = 220f - distance;

            if (difficulty == AiDifficulty.Hard)
            {
                Regiment neighborThreat = NeighborUnderPressure(self, candidate);
                if (neighborThreat)
                    score += 18f;
            }

            if (difficulty == AiDifficulty.Easy && officer.TacticalSkill < 50f && Random.value < 0.28f)
                score -= Random.Range(10f, 40f);

            score += Random.Range(-noise, noise);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static bool NeighborUnderPressure(Regiment self, Regiment candidate)
    {
        foreach (Regiment ally in BattleManager.Instance.Regiments)
        {
            if (ally == null || ally == self || ally.Team != self.Team || ally.IsRouted)
                continue;
            if (Vector3.Distance(ally.transform.position, candidate.transform.position) < ally.EffectiveRange * 0.9f && ally.Morale < 70f)
                return true;
        }

        return false;
    }

    public static void LogTelemetry(Regiment unit, OfficerProfile officer, OfficerMission mission, OfficerDecision decision, AiDifficulty difficulty)
    {
        string targetName = decision.Target != null ? decision.Target.RegimentName : "none";
        Debug.Log(
            "[AI] " + unit.RegimentName +
            " | " + officer.DisplayName +
            " | " + mission +
            " | " + difficulty +
            " | task=" + decision.Task +
            " | reason=" + decision.ReasonCode +
            " | target=" + targetName +
            " | delay=" + decision.ThinkDelay.ToString("0.00"));
    }
}
