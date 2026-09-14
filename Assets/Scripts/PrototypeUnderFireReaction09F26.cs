using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f26
// Under-fire reaction layer for AI-controlled companies.
// A moving AI ON company that receives confirmed hostile fire from a valid enemy
// inside its own Long range suspends movement, deploys Line, faces the threat and
// returns fire. The parent mission is never deleted; it resumes after the local
// reaction ends. AI OFF remains literal player authority.
[DefaultExecutionOrder(900)]
public sealed class PrototypeUnderFireReaction09F26 : MonoBehaviour
{
    private sealed class ReactionState
    {
        public Regiment Unit;
        public Regiment Attacker;
        public RegimentFirePolicy PreviousFirePolicy;
        public bool ControllerWasEnabled;
        public bool TemporaryLong;
        public float StartedAt;
        public float LastConfirmedFireAt;
    }

    private sealed class DeferredState
    {
        public Regiment Attacker;
        public float ExpiresAt;
    }

    public static PrototypeUnderFireReaction09F26 Instance { get; private set; }

    private readonly Dictionary<Regiment, ReactionState> reactions =
        new Dictionary<Regiment, ReactionState>();
    private readonly Dictionary<Regiment, DeferredState> deferred =
        new Dictionary<Regiment, DeferredState>();
    private readonly Dictionary<Regiment, float> previousUnderFire =
        new Dictionary<Regiment, float>();

    private FieldInfo underFireTimerField;
    private FieldInfo hasDestinationField;
    private GUIStyle statusStyle;

    private const float VolleyJumpThreshold = 0.55f;
    private const float FreshUnderFireThreshold = 6.20f;
    private const float MinimumReactionSeconds = 4.0f;
    private const float QuietReleaseSeconds = 7.25f;
    private const float DeferredBridgeSeconds = 20.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeUnderFireReaction09F26>() == null)
            new GameObject("PrototypeUnderFireReaction_v000009f26").AddComponent<PrototypeUnderFireReaction09F26>();
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
        underFireTimerField = typeof(Regiment).GetField("underFireTimer", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (underFireTimerField == null || hasDestinationField == null)
        {
            Debug.LogError("UNDER-FIRE-09F26|Installed=False|Reason=RegimentReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "UNDER-FIRE-09F26|Installed=True|AIOnly=True|MissionSuspended=True|" +
            "BridgePassageDeferred=True|QuietRelease=" + QuietReleaseSeconds.ToString("0.00") + "s");
    }

    private void OnDestroy()
    {
        RestoreAll();
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            float currentUnderFire = ReadUnderFire(regiment);
            float previous = previousUnderFire.TryGetValue(regiment, out float p) ? p : 0f;
            bool volleyEvent = IsFreshVolleyEvent(currentUnderFire, previous);
            previousUnderFire[regiment] = currentUnderFire;

            if (reactions.TryGetValue(regiment, out ReactionState reaction))
            {
                UpdateReaction(reaction, volleyEvent);
                continue;
            }

            TryStartDeferred(regiment);
            if (reactions.ContainsKey(regiment))
                continue;

            if (!volleyEvent)
                continue;

            TryStartReaction(regiment, false);
        }

        Cleanup(active);
    }

    public static bool IsReacting(Regiment regiment)
    {
        return Instance != null && regiment != null && Instance.reactions.ContainsKey(regiment);
    }

    private bool IsFreshVolleyEvent(float current, float previous)
    {
        if (current < FreshUnderFireThreshold)
            return false;

        if (previous <= 0.01f)
            return true;

        return current - previous >= VolleyJumpThreshold;
    }

    private void TryStartReaction(Regiment regiment, bool fromDeferred)
    {
        if (regiment == null || regiment.IsRouted || regiment.CurrentStrength <= 0)
            return;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
        {
            if (!fromDeferred)
            {
                Debug.Log("UNDER-FIRE-09F26|Unit=" + regiment.RegimentName +
                          "|Reaction=False|Reason=AI_OFF_PLAYER_AUTHORITY");
            }
            return;
        }

        if (!HasDestination(regiment))
            return; // B-282 is specifically a movement-under-fire reaction.

        Regiment attacker = FindNearestEnemyInsideLong(regiment);
        if (attacker == null)
            return;

        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(regiment))
        {
            deferred[regiment] = new DeferredState
            {
                Attacker = attacker,
                ExpiresAt = Time.time + DeferredBridgeSeconds
            };

            Debug.Log("UNDER-FIRE-09F26|Unit=" + regiment.RegimentName +
                      "|Reaction=DEFERRED|Reason=ACTIVE_BRIDGE_PASSAGE|Attacker=" + attacker.RegimentName +
                      "|MissionPreserved=True");
            return;
        }

        StartReaction(regiment, attacker, controller, fromDeferred);
    }

    private void TryStartDeferred(Regiment regiment)
    {
        if (!deferred.TryGetValue(regiment, out DeferredState state) || state == null)
            return;

        if (Time.time > state.ExpiresAt || regiment.IsRouted)
        {
            deferred.Remove(regiment);
            return;
        }

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
        {
            deferred.Remove(regiment);
            return;
        }

        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(regiment))
            return;

        Regiment attacker = state.Attacker;
        if (attacker == null || attacker.IsRouted || attacker.Team == regiment.Team ||
            PlanarDistance(regiment.transform.position, attacker.transform.position) > regiment.MaximumRange)
        {
            attacker = FindNearestEnemyInsideLong(regiment);
        }

        if (attacker == null || !HasDestination(regiment))
        {
            deferred.Remove(regiment);
            return;
        }

        deferred.Remove(regiment);
        StartReaction(regiment, attacker, controller, true);
    }

    private void StartReaction(
        Regiment regiment,
        Regiment attacker,
        OfficerAIController controller,
        bool fromDeferred)
    {
        RegimentFirePolicy previousPolicy = regiment.FirePolicy;
        bool controllerWasEnabled = controller.enabled;

        ReactionState state = new ReactionState
        {
            Unit = regiment,
            Attacker = attacker,
            PreviousFirePolicy = previousPolicy,
            ControllerWasEnabled = controllerWasEnabled,
            TemporaryLong = false,
            StartedAt = Time.time,
            LastConfirmedFireAt = Time.time
        };

        reactions[regiment] = state;

        if (controllerWasEnabled)
            controller.enabled = false;

        regiment.SetFormation(RegimentFormation.Line);
        regiment.OrderHold();
        ApplyReturnFire(state);

        Debug.Log("UNDER-FIRE-09F26|Start=True|Unit=" + regiment.RegimentName +
                  "|Attacker=" + attacker.RegimentName +
                  "|Distance=" + PlanarDistance(regiment.transform.position, attacker.transform.position).ToString("0.0") +
                  "|AI=ON|MissionSuspended=True|MissionDeleted=False|FromBridgeDeferred=" + fromDeferred +
                  "|PreviousFire=" + previousPolicy);
    }

    private void UpdateReaction(ReactionState state, bool volleyEvent)
    {
        Regiment unit = state.Unit;
        if (unit == null)
            return;

        OfficerAIController controller = unit.GetComponent<OfficerAIController>();

        if (unit.IsRouted || unit.CurrentStrength <= 0)
        {
            FinishReaction(unit, "UNIT_ROUTED_OR_DESTROYED");
            return;
        }

        if (controller == null || !controller.AIEnabled)
        {
            FinishReaction(unit, "AI_OFF_PLAYER_OVERRIDE");
            return;
        }

        if (volleyEvent)
        {
            Regiment newAttacker = FindNearestEnemyInsideLong(unit);
            if (newAttacker != null)
                state.Attacker = newAttacker;
            state.LastConfirmedFireAt = Time.time;
        }

        if (state.Attacker == null || state.Attacker.IsRouted || state.Attacker.CurrentStrength <= 0)
        {
            FinishReaction(unit, "ATTACKER_BROKEN");
            return;
        }

        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(unit))
        {
            // A later emergency bridge route may take priority. Do not let the local
            // reaction freeze a formation in the bridge throat.
            FinishReaction(unit, "BRIDGE_PASSAGE_PRIORITY");
            return;
        }

        float activeFor = Time.time - state.StartedAt;
        float quietFor = Time.time - state.LastConfirmedFireAt;
        if (activeFor >= MinimumReactionSeconds && quietFor >= QuietReleaseSeconds)
        {
            FinishReaction(unit, "FIRE_QUIET_MISSION_RESUMES");
            return;
        }

        unit.SetFormation(RegimentFormation.Line);
        unit.OrderHold();
        ApplyReturnFire(state);
    }

    private void ApplyReturnFire(ReactionState state)
    {
        Regiment unit = state.Unit;
        Regiment attacker = state.Attacker;
        if (unit == null || attacker == null)
            return;

        float distance = PlanarDistance(unit.transform.position, attacker.transform.position);
        TurnToward(unit, attacker);

        if (state.PreviousFirePolicy == RegimentFirePolicy.HoldFire)
        {
            unit.SetFirePolicy(RegimentFirePolicy.HoldFire);
            unit.OrderHold();
            return;
        }

        if (distance <= unit.MaximumRange)
        {
            if (distance > unit.GetFireTriggerRange() && unit.FirePolicy != RegimentFirePolicy.LongRange)
            {
                unit.SetFirePolicy(RegimentFirePolicy.LongRange);
                state.TemporaryLong = true;
            }

            unit.OrderAttack(attacker);
        }
        else
        {
            unit.OrderHold();
        }
    }

    private void FinishReaction(Regiment regiment, string reason)
    {
        if (regiment == null || !reactions.TryGetValue(regiment, out ReactionState state))
            return;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();

        if (!regiment.IsRouted)
        {
            regiment.OrderHold();
            regiment.SetFirePolicy(state.PreviousFirePolicy);
        }

        if (controller != null && controller.AIEnabled && state.ControllerWasEnabled)
            controller.enabled = true;

        reactions.Remove(regiment);

        Debug.Log("UNDER-FIRE-09F26|End=True|Unit=" + regiment.RegimentName +
                  "|Reason=" + reason +
                  "|MissionPreserved=True|FireRestored=" + state.PreviousFirePolicy +
                  "|TemporaryLong=" + state.TemporaryLong);
    }

    private Regiment FindNearestEnemyInsideLong(Regiment regiment)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || regiment == null)
            return null;

        Regiment nearest = null;
        float best = regiment.MaximumRange;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == regiment.Team || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;

            float distance = PlanarDistance(regiment.transform.position, candidate.transform.position);
            if (distance <= best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private float ReadUnderFire(Regiment regiment)
    {
        object value = underFireTimerField.GetValue(regiment);
        return value is float ? (float)value : 0f;
    }

    private bool HasDestination(Regiment regiment)
    {
        object value = hasDestinationField.GetValue(regiment);
        return value is bool && (bool)value;
    }

    private static void TurnToward(Regiment unit, Regiment attacker)
    {
        Vector3 direction = attacker.transform.position - unit.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        unit.transform.rotation = Quaternion.Slerp(
            unit.transform.rotation,
            desired,
            6.0f * Time.deltaTime);
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, ReactionState> pair in reactions)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;
            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove != null)
            foreach (Regiment regiment in remove)
                FinishReaction(regiment, "UNIT_REMOVED");

        List<Regiment> deferredRemove = null;
        foreach (KeyValuePair<Regiment, DeferredState> pair in deferred)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;
            if (deferredRemove == null)
                deferredRemove = new List<Regiment>();
            deferredRemove.Add(pair.Key);
        }
        if (deferredRemove != null)
            foreach (Regiment regiment in deferredRemove)
                deferred.Remove(regiment);

        List<Regiment> timerRemove = null;
        foreach (Regiment regiment in previousUnderFire.Keys)
        {
            if (regiment != null && active.Contains(regiment))
                continue;
            if (timerRemove == null)
                timerRemove = new List<Regiment>();
            timerRemove.Add(regiment);
        }
        if (timerRemove != null)
            foreach (Regiment regiment in timerRemove)
                previousUnderFire.Remove(regiment);
    }

    private void RestoreAll()
    {
        List<Regiment> units = new List<Regiment>(reactions.Keys);
        foreach (Regiment regiment in units)
            FinishReaction(regiment, "SYSTEM_DISABLED");
        deferred.Clear();
        previousUnderFire.Clear();
    }

    private void EnsureStyle()
    {
        if (statusStyle != null)
            return;
        statusStyle = PrototypeUiTheme09F15.Header(10);
        statusStyle.alignment = TextAnchor.MiddleRight;
    }

    private void OnGUI()
    {
        Regiment selected = FirstSelectedReactingCompany();
        if (selected == null || !reactions.TryGetValue(selected, out ReactionState state))
            return;

        EnsureStyle();
        GUI.depth = -3200;
        string fireText = state.PreviousFirePolicy == RegimentFirePolicy.HoldFire
            ? "HOLD FIRE"
            : "RETURNING FIRE";

        GUI.Box(
            new Rect(Screen.width - 430f, 40f, 420f, 26f),
            "UNDER FIRE | " + fireText + " | " +
            (state.Attacker != null ? state.Attacker.RegimentName : "UNKNOWN"),
            statusStyle);
    }

    private Regiment FirstSelectedReactingCompany()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.IsSelected && reactions.ContainsKey(regiment))
                return regiment;

        return null;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
