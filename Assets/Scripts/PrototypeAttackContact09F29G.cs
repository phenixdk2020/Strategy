using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29g
// Contact override for companies executing a Major AttackHere mission.
// A company no longer blindly marches through/around an enemy just because its assigned
// formation slot has not been reached. Once a valid enemy enters the company's selected
// fire-policy range (CLOSE/MED/LONG), the Captain temporarily takes local authority,
// drops the march leg and attacks. The parent mission is retained and can resume if the
// contact disappears. F29E's competing coordinated-slot attack writer is disabled.
[DefaultExecutionOrder(1700)]
public sealed class PrototypeAttackContact09F29G : MonoBehaviour
{
    private sealed class ContactState
    {
        public int BattalionIndex;
        public object Battalion;
        public object ParentMission;
        public Regiment Unit;
        public Regiment Target;
    }

    public static PrototypeAttackContact09F29G Instance { get; private set; }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly Dictionary<Regiment, ContactState> contacts = new Dictionary<Regiment, ContactState>();
    private readonly Dictionary<Regiment, RegimentFirePolicy> preferredFire = new Dictionary<Regiment, RegimentFirePolicy>();

    private FieldInfo battalionsField;
    private MethodInfo clearMissionVisualMethod;
    private MethodInfo setMissionVisualMethod;
    private float nextScan;
    private bool logged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeAttackContact09F29G>() == null)
            new GameObject("PrototypeAttackContact_v000009f29g")
                .AddComponent<PrototypeAttackContact09F29G>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", PrivateInstance);
        clearMissionVisualMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("ClearMissionVisual", PrivateInstance);
        setMissionVisualMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SetMissionVisual", PrivateInstance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void SetPreferredFirePolicy(Regiment unit, RegimentFirePolicy policy)
    {
        if (unit == null)
            return;
        unit.SetFirePolicy(policy);
        if (Instance != null)
            Instance.preferredFire[unit] = policy;
    }

    private void Update()
    {
        DisableCompetingCoordinator();
        CaptureInitialPolicies();
        MaintainContacts();

        if (Time.time < nextScan)
            return;
        nextScan = Time.time + 0.12f;
        ScanParentAttackMissions();

        if (!logged)
        {
            logged = true;
            Debug.Log("ATTACK-CONTACT-09F29G|Installed=True|Trigger=CompanyFirePolicyRange|ParentMissionRetained=True|F29ECoordinatorDisabled=True");
        }
    }

    private static void DisableCompetingCoordinator()
    {
        PrototypeCoordinatedAttack09F29E old = Object.FindAnyObjectByType<PrototypeCoordinatedAttack09F29E>();
        if (old != null && old.enabled)
            old.enabled = false;
    }

    private void CaptureInitialPolicies()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.Team != BattleTeam.Denmark || preferredFire.ContainsKey(unit))
                continue;
            preferredFire[unit] = unit.FirePolicy;
        }
    }

    private void MaintainContacts()
    {
        if (contacts.Count == 0)
            return;

        List<Regiment> release = null;
        foreach (KeyValuePair<Regiment, ContactState> pair in new List<KeyValuePair<Regiment, ContactState>>(contacts))
        {
            ContactState state = pair.Value;
            Regiment unit = pair.Key;
            if (state == null || !ValidUnit(unit))
            {
                Add(ref release, unit);
                continue;
            }

            IDictionary missions = GetMissions(state.Battalion);
            if (missions == null)
            {
                Add(ref release, unit);
                continue;
            }

            // A new higher command has explicitly reinserted this company into the
            // parent's mission set. That new order wins immediately.
            if (missions.Contains(unit))
            {
                Add(ref release, unit);
                continue;
            }

            if (!ValidEnemy(unit, state.Target))
            {
                RestoreParentMission(state, missions);
                Add(ref release, unit);
                continue;
            }

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller == null)
                continue;

            if (!controller.AIEnabled)
                controller.SetAIEnabled(true);
            controller.enabled = true;

            if (controller.Mission != OfficerAIMission.AttackTarget)
                controller.SetAttackMission(state.Target);

            // Immediate physical stop/attack avoids one extra march frame before the
            // OfficerAI think cycle runs. OrderAttack stops at the selected fire range.
            unit.SetFormation(RegimentFormation.Line);
            unit.OrderAttack(state.Target);
        }

        if (release != null)
            for (int i = 0; i < release.Count; i++)
                contacts.Remove(release[i]);
    }

    private void ScanParentAttackMissions()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        for (int b = 0; b < battalions.Count; b++)
        {
            object battalion = battalions[b];
            IDictionary missions = GetMissions(battalion);
            if (missions == null || missions.Count == 0)
                continue;

            List<Regiment> candidates = new List<Regiment>();
            foreach (DictionaryEntry entry in missions)
            {
                Regiment unit = entry.Key as Regiment;
                object mission = entry.Value;
                if (!ValidUnit(unit) || mission == null || contacts.ContainsKey(unit))
                    continue;

                FieldInfo orderField = mission.GetType().GetField("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (orderField == null || !(orderField.GetValue(mission) is MajorOrder09F18))
                    continue;
                MajorOrder09F18 order = (MajorOrder09F18)orderField.GetValue(mission);
                if (order != MajorOrder09F18.AttackHere)
                    continue;

                if (preferredFire.TryGetValue(unit, out RegimentFirePolicy preferred) && unit.FirePolicy != preferred)
                    unit.SetFirePolicy(preferred);

                if (unit.FirePolicy == RegimentFirePolicy.HoldFire)
                    continue;

                Regiment contact = FindNearestEnemyWithinPolicy(unit);
                if (contact == null)
                    continue;

                candidates.Add(unit);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                Regiment unit = candidates[i];
                if (unit == null || !missions.Contains(unit))
                    continue;

                object mission = missions[unit];
                Regiment contact = FindNearestEnemyWithinPolicy(unit);
                if (contact == null)
                    continue;

                missions.Remove(unit);
                if (clearMissionVisualMethod != null)
                    clearMissionVisualMethod.Invoke(hierarchy, new object[] { unit });

                OfficerAIController controller = unit.GetComponent<OfficerAIController>();
                if (controller == null)
                {
                    controller = unit.gameObject.AddComponent<OfficerAIController>();
                    controller.Configure(true, null);
                }
                else
                {
                    if (!controller.AIEnabled)
                        controller.SetAIEnabled(true);
                    controller.enabled = true;
                }

                controller.SetDoctrine(OfficerAIDoctrine.Offensive);
                controller.SetAttackMission(contact);
                unit.SetFormation(RegimentFormation.Line);
                unit.OrderAttack(contact);

                contacts[unit] = new ContactState
                {
                    BattalionIndex = b,
                    Battalion = battalion,
                    ParentMission = mission,
                    Unit = unit,
                    Target = contact
                };

                Debug.Log("ATTACK-CONTACT-09F29G|Unit=" + unit.RegimentName +
                          "|Contact=" + contact.RegimentName +
                          "|FirePolicy=" + unit.GetFirePolicyLabel() +
                          "|TriggerRange=" + unit.GetFireTriggerRange().ToString("0.0") +
                          "|Action=DROP_MARCH_AND_ENGAGE");
            }
        }
    }

    private void RestoreParentMission(ContactState state, IDictionary missions)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (state == null || state.Unit == null || state.ParentMission == null || missions == null || hierarchy == null)
            return;

        if (!missions.Contains(state.Unit))
            missions.Add(state.Unit, state.ParentMission);

        OfficerAIController controller = state.Unit.GetComponent<OfficerAIController>();
        if (controller != null)
            controller.enabled = false;

        if (setMissionVisualMethod != null)
            setMissionVisualMethod.Invoke(hierarchy, new object[] { state.Unit, state.ParentMission, state.BattalionIndex });

        Debug.Log("ATTACK-CONTACT-09F29G|Unit=" + state.Unit.RegimentName +
                  "|ContactEnded=True|Action=RESUME_PARENT_ATTACK_MISSION");
    }

    private static IDictionary GetMissions(object battalion)
    {
        if (battalion == null)
            return null;
        FieldInfo field = battalion.GetType().GetField("Missions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null ? field.GetValue(battalion) as IDictionary : null;
    }

    private static Regiment FindNearestEnemyWithinPolicy(Regiment unit)
    {
        if (!ValidUnit(unit) || unit.FirePolicy == RegimentFirePolicy.HoldFire)
            return null;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        float limit = unit.GetFireTriggerRange() + 0.75f;
        float best = limit;
        Regiment nearest = null;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (!ValidEnemy(unit, candidate))
                continue;
            float distance = PlanarDistance(unit.transform.position, candidate.transform.position);
            if (distance <= best)
            {
                best = distance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private static bool ValidUnit(Regiment unit)
    {
        return unit != null && !unit.IsRouted && unit.CurrentStrength > 0;
    }

    private static bool ValidEnemy(Regiment unit, Regiment target)
    {
        return ValidUnit(unit) && target != null && target != unit && target.Team != unit.Team &&
               !target.IsRouted && target.CurrentStrength > 0;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void Add(ref List<Regiment> list, Regiment unit)
    {
        if (list == null)
            list = new List<Regiment>();
        list.Add(unit);
    }
}
