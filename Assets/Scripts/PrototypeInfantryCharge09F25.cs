using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f25 core, hardened by v00.00.09f29n.
// Explicit infantry CHARGE state. This deliberately does NOT use Regiment.OrderAttack(),
// because normal attack stops at musket range. Charge owns the approach until deep
// physical contact, then the existing PrototypeMeleeCombatManager owns melee resolution.
// F29N guarantees HOLD FIRE + LINE throughout charge and removes the old bridge-column
// exception so a charging company never reforms into march column.
[DefaultExecutionOrder(-1050)]
public sealed class PrototypeInfantryCharge09F25 : MonoBehaviour
{
    private sealed class ChargeState
    {
        public Regiment Unit;
        public Regiment Target;
        public RegimentFirePolicy PreviousFirePolicy;
        public float NextSteer;
        public bool ContactLogged;
        public float ContactStartedAt;
    }

    public static PrototypeInfantryCharge09F25 Instance { get; private set; }

    private readonly Dictionary<Regiment, ChargeState> charges = new Dictionary<Regiment, ChargeState>();
    private readonly List<Regiment> pendingUnits = new List<Regiment>();

    private Camera cam;
    private bool pendingTargetPick;
    private FieldInfo moveSpeedField;
    private FieldInfo cohesionField;
    private FieldInfo forcedTargetField;
    private MethodInfo clearPlayerRouteMethod;

    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;

    // F29N: old 7.2 m root-to-root stop left a visually large no-man's-land between
    // two company formations. 2.2 m puts both visual formations deeply into bayonet
    // contact while still giving the melee resolver a stable stop threshold.
    private const float ContactDistance = 2.2f;
    private const float SteerInterval = 0.28f;
    private const float DenmarkChargeSpeed = 4.80f;
    private const float PrussiaChargeSpeed = 5.00f;
    private const float ExtraCohesionCostPerSecond = 0.30f;
    private const float ChargeMomentumSeconds = 2.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeInfantryCharge09F25>() == null)
            new GameObject("PrototypeInfantryCharge_v000009f25").AddComponent<PrototypeInfantryCharge09F25>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        moveSpeedField = typeof(Regiment).GetField("moveSpeed", flags);
        cohesionField = typeof(Regiment).GetField("<Cohesion>k__BackingField", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);
        clearPlayerRouteMethod = typeof(PlayerCommander).GetMethod("ClearRoute", flags);

        if (moveSpeedField == null || cohesionField == null || forcedTargetField == null)
        {
            Debug.LogError("CHARGE-09F25|Installed=False|Reason=RegimentReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log("CHARGE-09F29N|Installed=True|Contact=" + ContactDistance.ToString("0.0") +
                  "m|SpeedDK=" + DenmarkChargeSpeed.ToString("0.00") +
                  "|SpeedPR=" + PrussiaChargeSpeed.ToString("0.00") +
                  "|Momentum=" + ChargeMomentumSeconds.ToString("0.0") +
                  "s|Formation=LINE_LOCKED|Fire=HOLD_LOCKED|BridgeColumn=False");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (Input.GetKeyDown(KeyCode.Escape) && pendingTargetPick)
        {
            pendingTargetPick = false;
            pendingUnits.Clear();
        }

        if (Input.GetKeyDown(KeyCode.V) && !pendingTargetPick)
            BeginTargetPickFromSelection();

        if (pendingTargetPick && Input.GetMouseButtonDown(0) && !PointerOverControls())
        {
            Regiment target = GetEnemyUnderMouse();
            if (target != null)
            {
                IssueCharge(pendingUnits, target);
                pendingTargetPick = false;
                pendingUnits.Clear();
            }
        }

        UpdateCharges();
    }

    private void LateUpdate()
    {
        if (charges.Count == 0)
            return;

        foreach (KeyValuePair<Regiment, ChargeState> pair in charges)
        {
            Regiment unit = pair.Key;
            ChargeState state = pair.Value;
            if (unit == null || state == null || unit.IsRouted || state.Target == null || state.Target.IsRouted)
                continue;

            // Final authority pass: no other tactical helper may turn a charge into
            // march column or re-enable musket fire while the charge state exists.
            if (unit.Formation != RegimentFormation.Line)
                unit.SetFormation(RegimentFormation.Line);
            if (unit.FirePolicy != RegimentFirePolicy.HoldFire)
                unit.SetFirePolicy(RegimentFirePolicy.HoldFire);

            float distance = PlanarDistance(unit.transform.position, state.Target.transform.position);
            if (distance <= ContactDistance)
                continue;

            moveSpeedField.SetValue(unit, unit.Team == BattleTeam.Denmark ? DenmarkChargeSpeed : PrussiaChargeSpeed);

            float cohesion = (float)cohesionField.GetValue(unit);
            cohesion = Mathf.Max(20f, cohesion - ExtraCohesionCostPerSecond * Time.deltaTime);
            cohesionField.SetValue(unit, cohesion);
        }
    }

    public bool IsCharging(Regiment regiment)
    {
        return regiment != null && charges.ContainsKey(regiment);
    }

    // Presentation layers use this to hide musket range cones for both sides once a
    // charge has become physical melee. The attacker remains IsCharging throughout.
    public bool IsChargeMeleeParticipant(Regiment regiment)
    {
        if (regiment == null)
            return false;

        foreach (KeyValuePair<Regiment, ChargeState> pair in charges)
        {
            ChargeState state = pair.Value;
            if (state == null || !state.ContactLogged)
                continue;

            if (state.Unit == regiment || state.Target == regiment)
                return true;
        }

        return false;
    }

    public bool HasChargeMomentum(Regiment regiment)
    {
        if (regiment == null || !charges.TryGetValue(regiment, out ChargeState state))
            return false;
        return state.ContactLogged && Time.time - state.ContactStartedAt <= ChargeMomentumSeconds;
    }

    private void BeginTargetPickFromSelection()
    {
        pendingUnits.Clear();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected || regiment.IsRouted)
                continue;
            pendingUnits.Add(regiment);
        }

        if (pendingUnits.Count == 0)
            return;

        pendingTargetPick = true;
        Debug.Log("CHARGE-09F25|TargetPick=True|Companies=" + pendingUnits.Count);
    }

    private void IssueCharge(List<Regiment> units, Regiment target)
    {
        if (target == null || target.IsRouted)
            return;

        for (int i = 0; i < units.Count; i++)
        {
            Regiment unit = units[i];
            if (unit == null || unit.IsRouted || unit.Team == target.Team)
                continue;

            if (charges.ContainsKey(unit))
                FinishCharge(unit, "REPLACED");

            PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
            if (major != null && major.Installed)
                major.ReleaseCompanyToManual(unit);

            OfficerAIController controller = unit.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            ClearPlayerRoute(unit);
            forcedTargetField.SetValue(unit, null);

            RegimentFirePolicy previous = unit.FirePolicy;
            unit.SetFirePolicy(RegimentFirePolicy.HoldFire);
            unit.SetFormation(RegimentFormation.Line);
            unit.OrderHold();

            ChargeState state = new ChargeState
            {
                Unit = unit,
                Target = target,
                PreviousFirePolicy = previous,
                NextSteer = 0f,
                ContactLogged = false,
                ContactStartedAt = -999f
            };
            charges[unit] = state;

            Debug.Log("CHARGE-09F29N|Start=True|Unit=" + unit.RegimentName +
                      "|Target=" + target.RegimentName +
                      "|Distance=" + PlanarDistance(unit.transform.position, target.transform.position).ToString("0.0") +
                      "|AI=OFF|Fire=HOLD|Formation=LINE");
        }
    }

    private void UpdateCharges()
    {
        if (charges.Count == 0)
            return;

        List<Regiment> finish = null;
        List<string> reasons = null;

        foreach (KeyValuePair<Regiment, ChargeState> pair in new List<KeyValuePair<Regiment, ChargeState>>(charges))
        {
            Regiment unit = pair.Key;
            ChargeState state = pair.Value;

            if (unit == null || unit.IsRouted)
            {
                AddFinish(ref finish, ref reasons, unit, "ATTACKER_ROUTED");
                continue;
            }
            if (state == null || state.Target == null || state.Target.IsRouted || state.Target.CurrentStrength <= 0)
            {
                AddFinish(ref finish, ref reasons, unit, "TARGET_BROKEN");
                continue;
            }

            // Hard charge invariants. Keep these in Update as well as LateUpdate so
            // Regiment.Update cannot fire a volley during the frame between helpers.
            if (unit.FirePolicy != RegimentFirePolicy.HoldFire)
                unit.SetFirePolicy(RegimentFirePolicy.HoldFire);
            if (unit.Formation != RegimentFormation.Line)
                unit.SetFormation(RegimentFormation.Line);

            float distance = PlanarDistance(unit.transform.position, state.Target.transform.position);
            if (distance <= ContactDistance)
            {
                unit.OrderHold();
                if (!state.ContactLogged)
                {
                    state.ContactLogged = true;
                    state.ContactStartedAt = Time.time;
                    Debug.Log("CHARGE-09F29N|Contact=True|Unit=" + unit.RegimentName +
                              "|Target=" + state.Target.RegimentName +
                              "|Distance=" + distance.ToString("0.0") +
                              "|DeepContact=True|Fire=HOLD|Formation=LINE|MomentumWindow=" +
                              ChargeMomentumSeconds.ToString("0.0") +
                              "s|MeleeAuthority=PrototypeMeleeCombatManager");
                }
                continue;
            }

            state.ContactLogged = false;
            state.ContactStartedAt = -999f;
            if (Time.time < state.NextSteer)
                continue;
            state.NextSteer = Time.time + SteerInterval;

            Vector3 targetPoint = state.Target.transform.position;
            if (PrototypeRiverBridgeOnly09F3.TryGetAttackSteering(unit, targetPoint, out Vector3 bridgeSteering))
            {
                // F29N: bridge steering may alter the path, but never charge formation.
                unit.SetFormation(RegimentFormation.Line);
                unit.OrderMove(bridgeSteering);
                continue;
            }

            unit.SetFormation(RegimentFormation.Line);
            unit.OrderMove(targetPoint);
        }

        if (finish == null)
            return;

        for (int i = 0; i < finish.Count; i++)
            FinishCharge(finish[i], reasons[i]);
    }

    private void FinishCharge(Regiment unit, string reason)
    {
        if (unit == null || !charges.TryGetValue(unit, out ChargeState state))
            return;

        if (!unit.IsRouted)
        {
            unit.OrderHold();
            unit.SetFormation(RegimentFormation.Line);
            unit.SetFirePolicy(state.PreviousFirePolicy);
        }

        charges.Remove(unit);
        Debug.Log("CHARGE-09F29N|End=True|Unit=" + unit.RegimentName + "|Reason=" + reason +
                  "|FireRestored=" + state.PreviousFirePolicy + "|Formation=LINE");
    }

    private void ClearPlayerRoute(Regiment regiment)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && clearPlayerRouteMethod != null)
            clearPlayerRouteMethod.Invoke(commander, new object[] { regiment });
    }

    private Regiment GetEnemyUnderMouse()
    {
        if (cam == null)
            return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Regiment regiment = hit.collider != null ? hit.collider.GetComponentInParent<Regiment>() : null;
            if (regiment != null && regiment.Team == BattleTeam.Prussia && !regiment.IsRouted)
                return regiment;
        }
        return null;
    }

    private bool PointerOverControls()
    {
        return BattleManager.Instance != null && BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);
    }

    private void EnsureStyles()
    {
        if (buttonStyle != null)
            return;
        buttonStyle = PrototypeUiTheme09F15.Button(10);
        hintStyle = PrototypeUiTheme09F15.Label(10);
        hintStyle.alignment = TextAnchor.MiddleRight;
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -3100;

        if (pendingTargetPick)
        {
            GUI.Label(new Rect(Screen.width - 360f, Screen.height - 61f, 350f, 27f),
                "CHARGE: klik på fjendtlig enhed  |  ESC annullerer", hintStyle);
            return;
        }

        if (!HasSelectedDanishCompany())
            return;

        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major != null && major.Selected)
            return;

        Rect rect = new Rect(Screen.width - 142f, Screen.height - 31f, 132f, 27f);
        if (GUI.Button(rect, "CHARGE [V]", buttonStyle))
            BeginTargetPickFromSelection();
    }

    private static bool HasSelectedDanishCompany()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return false;
        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected && !regiment.IsRouted)
                return true;
        return false;
    }

    private static void AddFinish(ref List<Regiment> units, ref List<string> reasons, Regiment unit, string reason)
    {
        if (units == null)
        {
            units = new List<Regiment>();
            reasons = new List<string>();
        }
        units.Add(unit);
        reasons.Add(reason);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
