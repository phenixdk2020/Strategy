using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29g, contact authority hardened by v00.00.09f29o.
// Contact override for companies executing a Major AttackHere mission.
// A company no longer blindly marches through/around an enemy just because its assigned
// formation slot has not been reached. Once a valid enemy enters the company's selected
// fire-policy range (CLOSE/MED/LONG), the Captain temporarily takes local authority,
// drops the march leg, deploys Line, turns and attacks. Facing arc is deliberately NOT
// part of contact acquisition: a company must recognise an enemy entering from a flank.
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
    private const float ContactReleaseBuffer = 18f;
    private const float ContactTurnDegreesPerSecond = 32f;

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
            new GameObject("PrototypeAttackContact_v000009f29o")
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

    public static bool IsLocalContact(Regiment unit)
    {
        return Instance != null && unit != null && Instance.contacts.ContainsKey(unit);
    }

    public static Regiment GetLocalContactTarget(Regiment unit)
    {
        if (Instance == null || unit == null || !Instance.contacts.TryGetValue(unit, out ContactState state))
            return null;
        return state != null ? state.Target : null;
    }

    private void Update()
    {
        DisableCompetingCoordinator();
        CaptureInitialPolicies();
        MaintainContacts();

        if (Time.time < nextScan)
            return;
        nextScan = Time.time + 0.10f;
        ScanParentAttackMissions();

        if (!logged)
        {
            logged = true;
            Debug.Log(
                "ATTACK-CONTACT-09F29O|Installed=True|Trigger=CompanyFirePolicyRange|" +
                "ArcRequired=False|TurnAndEngage=True|ParentMissionRetained=True|" +
                "NavigationYield=True|F29ECoordinatorDisabled=True");
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
        foreach (KeyValuePair<Regiment, ContactState> pair in
                 new List<KeyValuePair<Regiment, ContactState>>(contacts))
        {
            ContactState state = pair.Value;
            Regiment unit = pair.Key;
            if (state == null || !ValidUnit(unit))
            {
                Add(ref release, unit);
                continue;
            }

            // Explicit player tactical states supersede this automatic contact layer.
            PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
            if ((charge != null && charge.IsCharging(unit)) || PrototypeInfantrySquare09F29.IsInSquare(unit))
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

            RegimentFirePolicy policy = preferredFire.TryGetValue(unit, out RegimentFirePolicy preferred)
                ? preferred
                : unit.FirePolicy;
            if (unit.FirePolicy != policy)
                unit.SetFirePolicy(policy);

            float trigger = GetPolicyRange(unit, policy);
            float distance = PlanarDistance(unit.transform.position, state.Target.transform.position);

            // Once local contact has been acquired, keep it sticky through a modest
            // buffer so target motion does not make the Captain alternate between
            // combat and parent-march every few frames.
            if (policy == RegimentFirePolicy.HoldFire || distance > trigger + ContactReleaseBuffer)
            {
                RestoreParentMission(state, missions);
                Add(ref release, unit);
                continue;
            }

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
            if (controller.Mission != OfficerAIMission.AttackTarget)
                controller.SetAttackMission(state.Target);

            // Local contact owns formation and target. OrderAttack stops movement as soon
            // as the chosen policy range has been reached. FaceContact then guarantees
            // that a flank contact rotates toward the enemy instead of continuing past it.
            if (unit.Formation != RegimentFormation.Line)
                unit.SetFormation(RegimentFormation.Line);
            unit.OrderAttack(state.Target);
            if (distance <= trigger + 1.0f)
                FaceContact(unit, state.Target);
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

                // Terrain and explicit tactical states outrank automatic contact capture.
                if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(unit) ||
                    PrototypeInfantrySquare09F29.IsInSquare(unit))
                    continue;
                PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
                if (charge != null && charge.IsCharging(unit))
                    continue;

                FieldInfo orderField = mission.GetType().GetField(
                    "Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                FaceContact(unit, contact);

                contacts[unit] = new ContactState
                {
                    BattalionIndex = b,
                    Battalion = battalion,
                    ParentMission = mission,
                    Unit = unit,
                    Target = contact
                };

                float bearing = ContactBearingDegrees(unit, contact);
                Debug.Log("ATTACK-CONTACT-09F29O|Unit=" + unit.RegimentName +
                          "|Contact=" + contact.RegimentName +
                          "|FirePolicy=" + unit.GetFirePolicyLabel() +
                          "|TriggerRange=" + unit.GetFireTriggerRange().ToString("0.0") +
                          "|Distance=" + PlanarDistance(unit.transform.position, contact.transform.position).ToString("0.0") +
                          "|BearingDeg=" + bearing.ToString("0") +
                          "|ArcRequired=False|Action=STOP_TURN_ENGAGE");
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
            setMissionVisualMethod.Invoke(
                hierarchy,
                new object[] { state.Unit, state.ParentMission, state.BattalionIndex });

        Debug.Log("ATTACK-CONTACT-09F29O|Unit=" + state.Unit.RegimentName +
                  "|ContactEnded=True|Action=RESUME_PARENT_ATTACK_MISSION");
    }

    private static IDictionary GetMissions(object battalion)
    {
        if (battalion == null)
            return null;
        FieldInfo field = battalion.GetType().GetField(
            "Missions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

    private static float GetPolicyRange(Regiment unit, RegimentFirePolicy policy)
    {
        if (unit == null)
            return 0f;
        switch (policy)
        {
            case RegimentFirePolicy.CloseRange: return unit.CloseRange;
            case RegimentFirePolicy.MediumRange: return unit.EffectiveRange;
            case RegimentFirePolicy.LongRange: return unit.MaximumRange;
            default: return 0f;
        }
    }

    private static void FaceContact(Regiment unit, Regiment target)
    {
        if (unit == null || target == null)
            return;

        Vector3 direction = target.transform.position - unit.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        unit.transform.rotation = Quaternion.RotateTowards(
            unit.transform.rotation,
            desired,
            ContactTurnDegreesPerSecond * Time.deltaTime);
    }

    private static float ContactBearingDegrees(Regiment unit, Regiment target)
    {
        if (unit == null || target == null)
            return 0f;
        Vector3 forward = unit.transform.forward;
        forward.y = 0f;
        Vector3 toTarget = target.transform.position - unit.transform.position;
        toTarget.y = 0f;
        if (forward.sqrMagnitude < 0.01f || toTarget.sqrMagnitude < 0.01f)
            return 0f;
        return Vector3.SignedAngle(forward.normalized, toTarget.normalized, Vector3.up);
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
