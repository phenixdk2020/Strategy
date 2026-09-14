using System.Collections.Generic;
using UnityEngine;

public enum PrototypeWithdrawalRange09F10
{
    Medium,
    Long,
    OutOfRange
}

// v00.00.09f10 controlled fighting withdrawal, river geometry aligned in 09f15.
// The company remains in Line and facing the threat, alternates stationary covering
// fire with short backward steps, and stops at the selected distance band.
[DefaultExecutionOrder(3200)]
public sealed class PrototypeFightingWithdrawal09F10 : MonoBehaviour
{
    private enum WithdrawalPhase
    {
        FirePause,
        Backstep
    }

    private sealed class WithdrawalState
    {
        public Regiment Unit;
        public Regiment Threat;
        public PrototypeWithdrawalRange09F10 Mode;
        public RegimentFirePolicy RestoreFirePolicy;
        public WithdrawalPhase Phase;
        public float PhaseUntil;
        public Vector3 StepEnd;
        public float TargetDistance;
        public float LastProgressDistance;
        public float LastProgressAt;
    }

    public static PrototypeFightingWithdrawal09F10 Instance { get; private set; }

    private readonly Dictionary<Regiment, WithdrawalState> active =
        new Dictionary<Regiment, WithdrawalState>();

    private const float BackstepDistance = 10.0f;
    private const float BackwardSpeed = 1.70f;
    private const float OutOfRangeMargin = 15.0f;
    private const float PositionArrival = 0.35f;
    private const float FacingTurnSpeed = 16.8f;

    // 09f15 river geometry. Fighting withdrawal is a local tactical order;
    // it does not automatically reverse-march through a bridge. If a backward step
    // would enter open water, the order halts instead of walking into the stream.
    private const float RiverHalfWidth = 2.75f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 9.0f;
    private const float BridgeHalfWidthZ = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeFightingWithdrawal09F10>() != null)
            return;

        GameObject root = new GameObject("PrototypeFightingWithdrawal_v000009f10");
        root.AddComponent<PrototypeFightingWithdrawal09F10>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log(
            "WITHDRAW-09F15|Installed=True|Step=" + BackstepDistance.ToString("0") +
            "m|Speed=" + BackwardSpeed.ToString("0.00") +
            "mps|Modes=MEDIUM,LONG,OUT|LineOnly=True|FireThenMove=True|RiverWidth=5.5m");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BeginForSelected(PrototypeWithdrawalRange09F10 mode)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int started = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null ||
                regiment.Team != BattleTeam.Denmark ||
                !regiment.IsSelected ||
                regiment.IsRouted)
            {
                continue;
            }

            Regiment threat = FindNearestEnemy(regiment, battle);
            if (threat == null)
                continue;

            StartWithdrawal(regiment, threat, mode);
            started++;
        }

        Debug.Log("WITHDRAW-09F15|SelectedStarted=" + started + "|Mode=" + mode);
    }

    public bool IsWithdrawing(Regiment regiment)
    {
        return regiment != null && active.ContainsKey(regiment);
    }

    private void StartWithdrawal(
        Regiment regiment,
        Regiment threat,
        PrototypeWithdrawalRange09F10 mode)
    {
        Cancel(regiment, false, "REPLACED");

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
        {
            controller.SetAIEnabled(false);
        }

        regiment.OrderHold();
        regiment.SetFormation(RegimentFormation.Line);

        float targetDistance = GetTargetDistance(regiment, mode);
        WithdrawalState state = new WithdrawalState
        {
            Unit = regiment,
            Threat = threat,
            Mode = mode,
            RestoreFirePolicy = regiment.FirePolicy,
            Phase = WithdrawalPhase.FirePause,
            PhaseUntil = Time.time + GetFirePauseSeconds(regiment),
            TargetDistance = targetDistance,
            LastProgressDistance = PlanarDistance(regiment.transform.position, threat.transform.position),
            LastProgressAt = Time.time
        };

        active[regiment] = state;
        regiment.SetFirePolicy(RegimentFirePolicy.LongRange);

        Debug.Log(
            "WITHDRAW-09F15|Unit=" + regiment.RegimentName +
            "|Start=True|Mode=" + mode +
            "|Current=" + state.LastProgressDistance.ToString("0.0") +
            "m|Target=" + targetDistance.ToString("0.0") +
            "m|AIOverride=True");
    }

    private void Update()
    {
        if (active.Count == 0)
            return;

        List<Regiment> complete = null;

        foreach (KeyValuePair<Regiment, WithdrawalState> pair in active)
        {
            Regiment regiment = pair.Key;
            WithdrawalState state = pair.Value;

            if (regiment == null || state == null || regiment.IsRouted)
            {
                AddComplete(ref complete, regiment);
                continue;
            }

            if (state.Threat == null || state.Threat.IsRouted || state.Threat.CurrentStrength <= 0)
                state.Threat = FindNearestEnemy(regiment, BattleManager.Instance);

            if (state.Threat == null)
            {
                Finish(state, "NO_VALID_THREAT");
                AddComplete(ref complete, regiment);
                continue;
            }

            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
            FaceThreat(regiment, state.Threat);

            float distance = PlanarDistance(regiment.transform.position, state.Threat.transform.position);
            if (distance >= state.TargetDistance - 0.25f)
            {
                Finish(state, "TARGET_DISTANCE_REACHED");
                AddComplete(ref complete, regiment);
                continue;
            }

            if (state.Phase == WithdrawalPhase.FirePause)
            {
                regiment.SetFirePolicy(RegimentFirePolicy.LongRange);

                if (Time.time < state.PhaseUntil)
                    continue;

                if (!BeginBackstep(state, distance))
                {
                    Finish(state, "BACKSTEP_BLOCKED_BY_RIVER");
                    AddComplete(ref complete, regiment);
                }
                continue;
            }

            if (UpdateBackstep(state))
                AddComplete(ref complete, regiment);
        }

        if (complete == null)
            return;

        for (int i = 0; i < complete.Count; i++)
            if (complete[i] != null)
                active.Remove(complete[i]);
    }

    private static void AddComplete(ref List<Regiment> complete, Regiment regiment)
    {
        if (complete == null)
            complete = new List<Regiment>();
        complete.Add(regiment);
    }

    private bool BeginBackstep(WithdrawalState state, float currentDistance)
    {
        Regiment regiment = state.Unit;
        Regiment threat = state.Threat;

        Vector3 away = regiment.transform.position - threat.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -regiment.transform.forward;
        away.Normalize();

        float remaining = Mathf.Max(0f, state.TargetDistance - currentDistance);
        float step = Mathf.Min(BackstepDistance, remaining + 0.20f);
        if (step < 0.5f)
            step = Mathf.Min(BackstepDistance, remaining);

        Vector3 end = regiment.transform.position + away * step;
        end.y = PrototypeBootstrap.SampleGroundHeight(end.x, end.z) + 0.10f;

        if (SegmentTouchesOpenWater(regiment.transform.position, end))
        {
            Debug.LogWarning(
                "WITHDRAW-09F15|Unit=" + regiment.RegimentName +
                "|Blocked=True|Reason=RIVER|AutomaticBridgeReverse=False");
            return false;
        }

        state.StepEnd = end;
        state.Phase = WithdrawalPhase.Backstep;
        state.LastProgressDistance = PlanarDistance(regiment.transform.position, end);
        state.LastProgressAt = Time.time;
        regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);

        Debug.Log(
            "WITHDRAW-09F15|Unit=" + regiment.RegimentName +
            "|Phase=BACKSTEP|Step=" + step.ToString("0.0") + "m");
        return true;
    }

    private bool UpdateBackstep(WithdrawalState state)
    {
        Regiment regiment = state.Unit;
        regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);

        Vector3 here = regiment.transform.position;
        Vector3 delta = state.StepEnd - here;
        delta.y = 0f;
        float remaining = delta.magnitude;

        if (remaining <= PositionArrival)
        {
            CompleteBackstep(state);
            return false;
        }

        Vector3 step = delta.normalized * BackwardSpeed * Time.deltaTime;
        if (step.magnitude > remaining)
            step = delta;

        Vector3 next = here + step;
        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
        regiment.transform.position = next;

        float newRemaining = PlanarDistance(next, state.StepEnd);
        if (newRemaining + 0.05f < state.LastProgressDistance)
        {
            state.LastProgressDistance = newRemaining;
            state.LastProgressAt = Time.time;
        }
        else if (Time.time - state.LastProgressAt > 3.0f)
        {
            Finish(state, "NO_RETREAT_PROGRESS");
            return true;
        }

        return false;
    }

    private void CompleteBackstep(WithdrawalState state)
    {
        state.Unit.transform.position = state.StepEnd;
        state.Unit.OrderHold();
        state.Unit.SetFirePolicy(RegimentFirePolicy.LongRange);
        state.Phase = WithdrawalPhase.FirePause;
        state.PhaseUntil = Time.time + GetFirePauseSeconds(state.Unit);

        Debug.Log(
            "WITHDRAW-09F15|Unit=" + state.Unit.RegimentName +
            "|Phase=FIRE_PAUSE|Distance=" +
            PlanarDistance(state.Unit.transform.position, state.Threat.transform.position).ToString("0.0") + "m");
    }

    private void Finish(WithdrawalState state, string reason)
    {
        if (state == null || state.Unit == null)
            return;

        state.Unit.OrderHold();
        state.Unit.SetFormation(RegimentFormation.Line);
        state.Unit.SetFirePolicy(state.RestoreFirePolicy);

        Debug.Log(
            "WITHDRAW-09F15|Unit=" + state.Unit.RegimentName +
            "|Complete=True|Reason=" + reason +
            "|RestoredFire=" + state.RestoreFirePolicy +
            "|OfficerAI=OFF_UNTIL_REENABLED");
    }

    private void Cancel(Regiment regiment, bool restoreFire, string reason)
    {
        if (regiment == null || !active.TryGetValue(regiment, out WithdrawalState state))
            return;

        if (restoreFire && state != null)
            regiment.SetFirePolicy(state.RestoreFirePolicy);

        active.Remove(regiment);
        Debug.Log("WITHDRAW-09F15|Unit=" + regiment.RegimentName + "|Cancelled=True|Reason=" + reason);
    }

    private static float GetTargetDistance(Regiment regiment, PrototypeWithdrawalRange09F10 mode)
    {
        switch (mode)
        {
            case PrototypeWithdrawalRange09F10.Medium:
                return regiment.EffectiveRange;

            case PrototypeWithdrawalRange09F10.Long:
                return regiment.EffectiveRange +
                       (regiment.MaximumRange - regiment.EffectiveRange) * 0.65f;

            default:
                return regiment.MaximumRange + OutOfRangeMargin;
        }
    }

    private static float GetFirePauseSeconds(Regiment regiment)
    {
        return Mathf.Max(2.0f, regiment.CurrentReloadSeconds + 0.65f);
    }

    private static void FaceThreat(Regiment regiment, Regiment threat)
    {
        Vector3 toward = threat.transform.position - regiment.transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude < 0.01f)
            return;

        Quaternion desired = Quaternion.LookRotation(toward.normalized, Vector3.up);
        regiment.transform.rotation = Quaternion.RotateTowards(
            regiment.transform.rotation,
            desired,
            FacingTurnSpeed * Time.deltaTime);
    }

    private static Regiment FindNearestEnemy(Regiment regiment, BattleManager battle)
    {
        if (regiment == null || battle == null || battle.Regiments == null)
            return null;

        Regiment nearest = null;
        float best = float.PositiveInfinity;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null ||
                candidate == regiment ||
                candidate.Team == regiment.Team ||
                candidate.IsRouted ||
                candidate.CurrentStrength <= 0)
            {
                continue;
            }

            float distance = PlanarDistance(regiment.transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private static bool SegmentTouchesOpenWater(Vector3 a, Vector3 b)
    {
        const int samples = 24;
        for (int i = 0; i <= samples; i++)
        {
            Vector3 point = Vector3.Lerp(a, b, i / (float)samples);
            if (IsInOpenWater(point))
                return true;
        }
        return false;
    }

    private static bool IsInOpenWater(Vector3 point)
    {
        float bridgeX = StreamCenterX(BridgeZ);
        bool bridgeZone =
            Mathf.Abs(point.z - BridgeZ) <= BridgeHalfWidthZ &&
            Mathf.Abs(point.x - bridgeX) <= BridgeHalfLengthX;

        if (bridgeZone)
            return false;

        return Mathf.Abs(point.x - StreamCenterX(point.z)) < RiverHalfWidth;
    }

    private static float StreamCenterX(float z)
    {
        return PrototypeBootstrap.StreamCenterX(z);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
