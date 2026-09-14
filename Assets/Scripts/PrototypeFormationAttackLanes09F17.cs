using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f21: authority-safe manual formation attack lanes.
//
// Legacy 09f17 grouped every Danish company that happened to share the same forcedTarget.
// That allowed one manually detached company to be mixed with Major/AI-controlled companies;
// the layer then called OrderMove() and silently replaced the player's direct attack.
//
// 09f21 rule:
// - Major/AI ON companies are never part of this manual planner.
// - A single manual company is never touched by this layer.
// - Only 2+ CURRENTLY SELECTED, AI OFF companies sharing the same target get manual lanes.
// - The target is temporarily suspended only while they move to their separate lane slots;
//   once a slot is reached (or the enemy is already inside useful fire range), OrderAttack()
//   is restored so the units continue fighting rather than stopping after the manoeuvre.
[DefaultExecutionOrder(450)]
public sealed class PrototypeFormationAttackLanes09F17 : MonoBehaviour
{
    private sealed class ManualLaneAssignment
    {
        public Regiment Target;
        public Vector3 Goal;
        public bool Engaged;
        public float NextAssert;
    }

    private FieldInfo forcedTargetField;
    private readonly Dictionary<Regiment, ManualLaneAssignment> assignments =
        new Dictionary<Regiment, ManualLaneAssignment>();

    private const float CompanySpacing = 60f;   // ~48 m frontage + ~12 m interval.
    private const float MinStandoff = 28f;
    private const float MaxStandoff = 88f;
    private const float LaneArrival = 5.0f;
    private const float ReassertInterval = 0.45f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFormationAttackLanes09F17>() != null)
            return;

        GameObject root = new GameObject("PrototypeFormationAttackLanes_v000009f21");
        root.AddComponent<PrototypeFormationAttackLanes09F17>();
    }

    private void Awake()
    {
        forcedTargetField = typeof(Regiment).GetField(
            "forcedTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (forcedTargetField == null)
        {
            Debug.LogError("ATTACK-LANES-09F21|Installed=False|Reason=forcedTarget_not_found");
            enabled = false;
            return;
        }

        Debug.Log("ATTACK-LANES-09F21|Installed=True|ManualOnly=True|MajorAIExcluded=True|Spacing=60m");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        // Any new direct player order cancels old lane ownership first. PlayerCommander
        // runs before this script (300 vs 450), so a fresh multi-company attack can then
        // be detected again later in this same frame.
        if (IsNewDirectPlayerOrderInput())
            assignments.Clear();

        UpdateExistingAssignments();
        DetectNewManualAttackGroups(battle);
    }

    private void UpdateExistingAssignments()
    {
        if (assignments.Count == 0)
            return;

        List<Regiment> remove = null;

        foreach (KeyValuePair<Regiment, ManualLaneAssignment> pair in assignments)
        {
            Regiment regiment = pair.Key;
            ManualLaneAssignment assignment = pair.Value;

            if (!IsStillManual(regiment) ||
                assignment == null ||
                assignment.Target == null ||
                assignment.Target.IsRouted ||
                assignment.Target.CurrentStrength <= 0)
            {
                if (remove == null) remove = new List<Regiment>();
                remove.Add(regiment);
                continue;
            }

            if (assignment.Engaged)
            {
                // Once engaged, Regiment owns the direct target chase/fire logic.
                // Do not re-plan a lane and do not clear forcedTarget again.
                continue;
            }

            float laneDistance = PlanarDistance(regiment.transform.position, assignment.Goal);
            float targetDistance = PlanarDistance(regiment.transform.position, assignment.Target.transform.position);
            float usefulFireRange = Mathf.Max(4f, regiment.GetFireTriggerRange() * 0.96f);

            if (laneDistance <= LaneArrival || targetDistance <= usefulFireRange)
            {
                regiment.SetFormation(RegimentFormation.Line);
                regiment.OrderAttack(assignment.Target);
                assignment.Engaged = true;

                Debug.Log("ATTACK-LANES-09F21|Unit=" + regiment.RegimentName +
                          "|Phase=ENGAGE|Target=" + assignment.Target.RegimentName +
                          "|LaneDistance=" + laneDistance.ToString("0.0") +
                          "|TargetDistance=" + targetDistance.ToString("0.0") +
                          "|ManualAttackRestored=True");
                continue;
            }

            if (Time.time >= assignment.NextAssert)
            {
                assignment.NextAssert = Time.time + ReassertInterval;
                regiment.SetFormation(RegimentFormation.Line);
                regiment.OrderMove(assignment.Goal);
            }
        }

        if (remove != null)
            foreach (Regiment regiment in remove)
                assignments.Remove(regiment);
    }

    private void DetectNewManualAttackGroups(BattleManager battle)
    {
        Dictionary<Regiment, List<Regiment>> groups = new Dictionary<Regiment, List<Regiment>>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (!IsEligibleForNewManualLane(regiment) || assignments.ContainsKey(regiment))
                continue;

            Regiment target = forcedTargetField.GetValue(regiment) as Regiment;
            if (target == null || target.IsRouted || target.CurrentStrength <= 0 || target.Team == regiment.Team)
                continue;

            if (!groups.TryGetValue(target, out List<Regiment> attackers))
            {
                attackers = new List<Regiment>();
                groups[target] = attackers;
            }

            attackers.Add(regiment);
        }

        foreach (KeyValuePair<Regiment, List<Regiment>> pair in groups)
        {
            // CRITICAL 09f21 rule: a single manual company is left completely alone.
            if (pair.Value.Count < 2)
                continue;

            CreateManualFormationAttack(pair.Key, pair.Value);
        }
    }

    private bool IsEligibleForNewManualLane(Regiment regiment)
    {
        if (regiment == null || regiment.Team != BattleTeam.Denmark || regiment.IsRouted)
            return false;

        // A lane is only created for the direct player selection that issued the attack.
        // This prevents stale/manual units elsewhere on the map from being grouped by accident.
        if (!regiment.IsSelected)
            return false;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        return controller == null || !controller.AIEnabled;
    }

    private bool IsStillManual(Regiment regiment)
    {
        if (regiment == null || regiment.Team != BattleTeam.Denmark || regiment.IsRouted)
            return false;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        return controller == null || !controller.AIEnabled;
    }

    private void CreateManualFormationAttack(Regiment target, List<Regiment> attackers)
    {
        if (target == null || attackers == null || attackers.Count < 2)
            return;

        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in attackers)
        {
            if (regiment == null) continue;
            center += regiment.transform.position;
            count++;
        }
        if (count < 2)
            return;
        center /= count;

        Vector3 facing = target.transform.position - center;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector3.forward;
        facing.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, facing).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        attackers.Sort((a, b) =>
        {
            float ap = Vector3.Dot(a.transform.position, lateral);
            float bp = Vector3.Dot(b.transform.position, lateral);
            return ap.CompareTo(bp);
        });

        // Respect the shortest selected fire policy so all companies can use one line.
        float standoff = MaxStandoff;
        bool foundRange = false;
        foreach (Regiment regiment in attackers)
        {
            if (regiment == null) continue;
            float range = regiment.GetFireTriggerRange();
            if (range <= 0.1f)
                range = regiment.EffectiveRange * 0.70f;
            float desired = Mathf.Clamp(range * 0.88f, MinStandoff, MaxStandoff);
            if (!foundRange || desired < standoff)
                standoff = desired;
            foundRange = true;
        }

        Vector3 lineCenter = target.transform.position - facing * standoff;
        float centerIndex = (attackers.Count - 1) * 0.5f;

        for (int i = 0; i < attackers.Count; i++)
        {
            Regiment regiment = attackers[i];
            if (regiment == null || regiment.IsRouted)
                continue;

            Vector3 goal = lineCenter + lateral * ((i - centerIndex) * CompanySpacing);
            goal.y = PrototypeBootstrap.SampleGroundHeight(goal.x, goal.z) + 0.10f;

            ManualLaneAssignment assignment = new ManualLaneAssignment
            {
                Target = target,
                Goal = goal,
                Engaged = false,
                NextAssert = 0f
            };
            assignments[regiment] = assignment;

            float targetDistance = PlanarDistance(regiment.transform.position, target.transform.position);
            float usefulFireRange = Mathf.Max(4f, regiment.GetFireTriggerRange() * 0.96f);

            regiment.SetFormation(RegimentFormation.Line);
            if (targetDistance <= usefulFireRange)
            {
                regiment.OrderAttack(target);
                assignment.Engaged = true;
            }
            else
            {
                // Temporarily suspend forcedTarget while moving to this company's own lane.
                // It is restored with OrderAttack() once the lane is reached.
                regiment.OrderMove(goal);
                assignment.NextAssert = Time.time + ReassertInterval;
            }
        }

        Debug.Log(
            "ATTACK-LANES-09F21|Target=" + target.RegimentName +
            "|ManualCompanies=" + attackers.Count +
            "|Spacing=" + CompanySpacing.ToString("0") +
            "m|Standoff=" + standoff.ToString("0.0") +
            "m|MajorAIExcluded=True|SingleManualUntouched=True");
    }

    private static bool IsNewDirectPlayerOrderInput()
    {
        return Input.GetMouseButtonDown(1) ||
               Input.GetKeyDown(KeyCode.H) ||
               Input.GetKeyDown(KeyCode.F) ||
               Input.GetKeyDown(KeyCode.C) ||
               Input.GetKeyDown(KeyCode.Z) ||
               Input.GetKeyDown(KeyCode.X);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
