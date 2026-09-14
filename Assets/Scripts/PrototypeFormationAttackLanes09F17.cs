using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f17: formation-level attack lanes.
// When 2+ Danish companies are ordered to attack the same enemy, Regiment.OrderAttack
// would normally make every unit chase the exact same target transform and overlap.
// This layer converts that shared target into a common firing line with one lane per company.
[DefaultExecutionOrder(450)]
public sealed class PrototypeFormationAttackLanes09F17 : MonoBehaviour
{
    private FieldInfo forcedTargetField;

    private const float CompanySpacing = 60f;   // ~48 m frontage + ~12 m interval.
    private const float MinStandoff = 28f;
    private const float MaxStandoff = 88f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFormationAttackLanes09F17>() != null)
            return;

        GameObject root = new GameObject("PrototypeFormationAttackLanes_v000009f17");
        root.AddComponent<PrototypeFormationAttackLanes09F17>();
    }

    private void Awake()
    {
        forcedTargetField = typeof(Regiment).GetField(
            "forcedTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (forcedTargetField == null)
        {
            Debug.LogError("ATTACK-LANES-09F17|Installed=False|Reason=forcedTarget_not_found");
            enabled = false;
            return;
        }

        Debug.Log("ATTACK-LANES-09F17|Installed=True|Spacing=60m|SideBySide=True");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Dictionary<Regiment, List<Regiment>> groups = new Dictionary<Regiment, List<Regiment>>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || regiment.IsRouted)
                continue;

            Regiment target = forcedTargetField.GetValue(regiment) as Regiment;
            if (target == null || target.IsRouted || target.Team == regiment.Team)
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
            if (pair.Value.Count < 2)
                continue;

            ConvertToFormationAttack(pair.Key, pair.Value);
        }
    }

    private void ConvertToFormationAttack(Regiment target, List<Regiment> attackers)
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

        // Respect the shortest selected fire policy so all companies can remain on one line.
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

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            Vector3 goal = lineCenter + lateral * ((i - centerIndex) * CompanySpacing);
            goal.y = PrototypeBootstrap.SampleGroundHeight(goal.x, goal.z) + 0.10f;

            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderMove(goal); // Clears shared forcedTarget and prevents convergence.
        }

        Debug.Log(
            "ATTACK-LANES-09F17|Target=" + target.RegimentName +
            "|Companies=" + attackers.Count +
            "|Spacing=" + CompanySpacing.ToString("0") +
            "m|Standoff=" + standoff.ToString("0.0") +
            "m|SharedTargetConvergence=False");
    }
}
