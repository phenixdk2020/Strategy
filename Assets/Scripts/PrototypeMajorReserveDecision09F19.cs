using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f19
// Tactical reserve discretion overlay for the 09f18 battalion command layer.
// Main change: a 4-company battalion may still keep one company in reserve against
// a single known enemy if the remaining three companies are judged sufficient.
// A flank is a separate decision and is never mandatory.
[DefaultExecutionOrder(430)]
public sealed class PrototypeMajorReserveDecision09F19 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private FieldInfo missionsField;
    private FieldInfo reserveField;
    private FieldInfo lastDecisionBackingField;
    private MethodInfo safeCompanyMethod;

    private float nextReview;
    private string lastAppliedKey = string.Empty;

    private const float ReviewInterval = 0.75f;
    private const float KnownRange = 700f;
    private const float CompanySpacing = 60f;
    private const float ReserveDepth = 78f;
    private const float FlankOffset = 112f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorReserveDecision09F19>() == null)
            new GameObject("PrototypeMajorReserveDecision_v000009f19").AddComponent<PrototypeMajorReserveDecision09F19>();
    }

    private void Update()
    {
        if (Time.time < nextReview)
            return;
        nextReview = Time.time + ReviewInterval;

        if (!ResolveMajor())
            return;

        ReviewFourVersusOne();
    }

    private bool ResolveMajor()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return false;

        if (missionsField == null)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type type = typeof(PrototypeMajorBattalion09F18);
            missionsField = type.GetField("missions", flags);
            reserveField = type.GetField("reserve", flags);
            safeCompanyMethod = type.GetMethod("SafeCompany", flags);
            lastDecisionBackingField = type.GetField("<LastDecisionText>k__BackingField", flags);
        }

        return missionsField != null && reserveField != null && safeCompanyMethod != null;
    }

    private void ReviewFourVersusOne()
    {
        string orderText = major.LastOrderText ?? string.Empty;
        bool attack = orderText.StartsWith("ANGRIB");
        bool defend = orderText.StartsWith("FORSVAR");
        if (!attack && !defend)
        {
            lastAppliedKey = string.Empty;
            return;
        }

        List<Regiment> active = ActiveCompanies();
        if (active.Count != 4)
            return;

        List<Regiment> known = KnownEnemies(active);
        if (known.Count != 1)
            return; // 09f18 already handles the multi-threat case.

        if (reserveField.GetValue(major) as Regiment != null)
            return;

        float averageStrength = AverageStrength(active);
        float averageCohesion = AverageCohesion(active);
        int ownStrength = TotalStrength(active);
        Regiment enemy = known[0];
        float superiority = ownStrength / (float)Mathf.Max(1, enemy.CurrentStrength);

        // The Major may keep a reserve if the remaining three companies should still
        // comfortably outmatch the known threat. This deliberately makes reserve a
        // judgement, not a function of enemy unit count alone.
        bool reserveJustified =
            averageStrength >= 0.68f &&
            averageCohesion >= 54f &&
            superiority >= 2.65f;

        if (!reserveJustified)
        {
            SetDecision("Reserve: ingen | 4 mod 1, men styrke/cohesion vurderes for lav");
            return;
        }

        IDictionary missions = missionsField.GetValue(major) as IDictionary;
        if (missions == null || missions.Count < 4)
            return;

        Regiment reserve = BestReserve(active);
        if (reserve == null || !missions.Contains(reserve))
            return;

        string key = orderText + "|" + enemy.GetInstanceID() + "|" + reserve.GetInstanceID();
        if (lastAppliedKey == key)
            return;

        Vector3 center = MissionCenter(missions, active);
        Vector3 forward = MissionFacing(missions, active, enemy.transform.position - center);
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;

        bool flank = ShouldFlank(active, reserve, enemy, center, forward, lateral, attack);
        int flankSign = ChooseFlankSign(reserve, center, forward, lateral);

        List<Regiment> front = new List<Regiment>(active);
        front.Remove(reserve);

        float middle = (front.Count - 1) * 0.5f;
        for (int i = 0; i < front.Count; i++)
        {
            Regiment regiment = front[i];
            object mission = missions[regiment];
            if (mission == null)
                continue;

            Vector3 desired = center + lateral * ((i - middle) * CompanySpacing);
            Vector3 safe = SafeCompany(desired, forward);
            SetMission(mission, safe, forward, false, false);
            Reassert(regiment, mission, safe);
        }

        object reserveMission = missions[reserve];
        Vector3 reserveGoal = flank
            ? center - forward * 22f + lateral * (flankSign * FlankOffset)
            : center - forward * ReserveDepth;
        reserveGoal = SafeCompany(reserveGoal, forward);
        SetMission(reserveMission, reserveGoal, forward, true, flank);
        Reassert(reserve, reserveMission, reserveGoal);

        reserveField.SetValue(major, reserve);
        lastAppliedKey = key;

        string decision = "Reserve: " + PrototypeMajorBattalion09F18.Name(reserve) +
                          " | 4 mod 1 | styrkeoverlegenhed x" + superiority.ToString("0.0") +
                          (flank ? " | FLANKE vurderet fordelagtig" : " | holdes bag front");
        SetDecision(decision);

        Debug.Log("HQ-RESERVE-09F19|Scenario=4v1|Reserve=" + reserve.RegimentName +
                  "|Superiority=" + superiority.ToString("0.00") +
                  "|AvgStrength=" + averageStrength.ToString("0.00") +
                  "|AvgCohesion=" + averageCohesion.ToString("0") +
                  "|Flank=" + flank +
                  "|Rule=ReserveIsDiscretionary");
    }

    private List<Regiment> ActiveCompanies()
    {
        List<Regiment> result = new List<Regiment>();
        foreach (Regiment r in major.Companies)
            if (r != null && !r.IsRouted && r.CurrentStrength > 0)
                result.Add(r);
        return result;
    }

    private List<Regiment> KnownEnemies(List<Regiment> active)
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment enemy in battle.Regiments)
        {
            if (enemy == null || enemy.Team != BattleTeam.Prussia || enemy.IsRouted || enemy.CurrentStrength <= 0)
                continue;

            bool known = major.HqRoot != null && Dist(major.HqRoot.transform.position, enemy.transform.position) <= KnownRange;
            if (!known)
            {
                foreach (Regiment own in active)
                {
                    if (Dist(own.transform.position, enemy.transform.position) <= KnownRange)
                    {
                        known = true;
                        break;
                    }
                }
            }

            if (known)
                result.Add(enemy);
        }
        return result;
    }

    private bool ShouldFlank(
        List<Regiment> active,
        Regiment reserve,
        Regiment enemy,
        Vector3 center,
        Vector3 forward,
        Vector3 lateral,
        bool attack)
    {
        float avgStrength = AverageStrength(active);
        float avgCohesion = AverageCohesion(active);
        float reserveStrength = reserve.CurrentStrength / (float)Mathf.Max(1, reserve.InitialStrength);
        float enemyDistance = Dist(center, enemy.transform.position);

        if (avgStrength < 0.76f || avgCohesion < 62f || reserveStrength < 0.75f)
            return false;

        // A flank is easier to justify during an attack. During defence it is treated
        // as a local counter-flank and requires the enemy to be closer.
        float maxDistance = attack ? 285f : 190f;
        if (enemyDistance > maxDistance)
            return false;

        Vector3 leftGoal = SafeCompany(center - forward * 22f - lateral * FlankOffset, forward);
        Vector3 rightGoal = SafeCompany(center - forward * 22f + lateral * FlankOffset, forward);

        float leftTravel = Dist(reserve.transform.position, leftGoal);
        float rightTravel = Dist(reserve.transform.position, rightGoal);
        float bestTravel = Mathf.Min(leftTravel, rightTravel);

        // Do not send the reserve on a huge detour simply because numerical superiority exists.
        return bestTravel <= 245f;
    }

    private int ChooseFlankSign(Regiment reserve, Vector3 center, Vector3 forward, Vector3 lateral)
    {
        Vector3 left = SafeCompany(center - forward * 22f - lateral * FlankOffset, forward);
        Vector3 right = SafeCompany(center - forward * 22f + lateral * FlankOffset, forward);
        return Dist(reserve.transform.position, left) <= Dist(reserve.transform.position, right) ? -1 : 1;
    }

    private Regiment BestReserve(List<Regiment> active)
    {
        Regiment best = null;
        float bestScore = float.NegativeInfinity;
        foreach (Regiment r in active)
        {
            float strength = r.CurrentStrength / (float)Mathf.Max(1, r.InitialStrength);
            float score = strength * 70f + r.Cohesion * 0.30f;
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }
        return best;
    }

    private Vector3 MissionCenter(IDictionary missions, List<Regiment> active)
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment r in active)
        {
            object mission = missions[r];
            if (mission == null)
                continue;
            FieldInfo goalField = mission.GetType().GetField("Goal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (goalField == null)
                continue;
            total += (Vector3)goalField.GetValue(mission);
            count++;
        }
        return count > 0 ? total / count : CompanyCenter(active);
    }

    private Vector3 MissionFacing(IDictionary missions, List<Regiment> active, Vector3 fallback)
    {
        foreach (Regiment r in active)
        {
            object mission = missions[r];
            if (mission == null)
                continue;
            FieldInfo facingField = mission.GetType().GetField("Facing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (facingField == null)
                continue;
            Vector3 facing = (Vector3)facingField.GetValue(mission);
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f)
                return facing.normalized;
        }
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward;
    }

    private void SetMission(object mission, Vector3 goal, Vector3 facing, bool reserveRole, bool flank)
    {
        System.Type type = mission.GetType();
        SetField(type, mission, "Goal", goal);
        SetField(type, mission, "Facing", facing);
        SetField(type, mission, "Reserve", reserveRole);
        SetField(type, mission, "Flank", flank);
        SetField(type, mission, "Arrived", false);
        SetField(type, mission, "NextAssert", 0f);
    }

    private static void SetField(System.Type type, object target, string name, object value)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(target, value);
    }

    private void Reassert(Regiment regiment, object mission, Vector3 goal)
    {
        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null)
        {
            if (!controller.AIEnabled)
                controller.SetAIEnabled(true);
            controller.SetMoveMission(goal);
        }
        else
        {
            regiment.OrderMove(goal);
        }

        FieldInfo orderField = mission.GetType().GetField("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MajorOrder09F18 order = orderField != null ? (MajorOrder09F18)orderField.GetValue(mission) : MajorOrder09F18.None;
        FieldInfo facingField = mission.GetType().GetField("Facing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Vector3 facing = facingField != null ? (Vector3)facingField.GetValue(mission) : regiment.transform.forward;
        FieldInfo reserveRoleField = mission.GetType().GetField("Reserve", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bool reserveRole = reserveRoleField != null && (bool)reserveRoleField.GetValue(mission);
        FieldInfo flankField = mission.GetType().GetField("Flank", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bool flank = flankField != null && (bool)flankField.GetValue(mission);

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
            visuals.SetMission(regiment, goal, facing, order, reserveRole, flank);
    }

    private Vector3 SafeCompany(Vector3 point, Vector3 facing)
    {
        object result = safeCompanyMethod.Invoke(major, new object[] { point, facing });
        return result is Vector3 ? (Vector3)result : point;
    }

    private void SetDecision(string text)
    {
        if (lastDecisionBackingField != null)
            lastDecisionBackingField.SetValue(major, text);
    }

    private static float AverageStrength(List<Regiment> list)
    {
        float total = 0f;
        foreach (Regiment r in list)
            total += r.CurrentStrength / (float)Mathf.Max(1, r.InitialStrength);
        return list.Count > 0 ? total / list.Count : 0f;
    }

    private static float AverageCohesion(List<Regiment> list)
    {
        float total = 0f;
        foreach (Regiment r in list)
            total += r.Cohesion;
        return list.Count > 0 ? total / list.Count : 0f;
    }

    private static int TotalStrength(List<Regiment> list)
    {
        int total = 0;
        foreach (Regiment r in list)
            total += Mathf.Max(0, r.CurrentStrength);
        return total;
    }

    private static Vector3 CompanyCenter(List<Regiment> list)
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment r in list)
        {
            if (r == null)
                continue;
            total += r.transform.position;
            count++;
        }
        return count > 0 ? total / count : Vector3.zero;
    }

    private static float Dist(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
