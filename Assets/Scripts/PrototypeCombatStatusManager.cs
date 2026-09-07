using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class PrototypeCombatStatusManager : MonoBehaviour
{
    private sealed class CombatState
    {
        public int AmmunitionRoundsPerMan;
        public int StartingAmmunitionRoundsPerMan;
        public float LastObservedNextFireTime;
        public int LastVolleyHits;
        public float FeedbackUntil;
        public int VolleySequence;
    }

    public static PrototypeCombatStatusManager Instance { get; private set; }

    private readonly Dictionary<Regiment, CombatState> states = new Dictionary<Regiment, CombatState>();
    private FieldInfo nextFireTimeField;
    private FieldInfo forcedTargetField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCombatStatusManager>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeCombatStatusManager_v009");
        managerObject.AddComponent<PrototypeCombatStatusManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);

        if (nextFireTimeField == null)
            Debug.LogError("PrototypeCombatStatusManager: Regiment.nextFireTime was not found.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || nextFireTimeField == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            if (!states.TryGetValue(regiment, out CombatState state))
            {
                int startingAmmo = PrototypeCombatTuningManager.GetStartingAmmoRoundsPerMan();
                state = new CombatState
                {
                    AmmunitionRoundsPerMan = startingAmmo,
                    StartingAmmunitionRoundsPerMan = startingAmmo,
                    LastObservedNextFireTime = ReadNextFireTime(regiment)
                };
                states[regiment] = state;
            }

            float currentNextFireTime = ReadNextFireTime(regiment);

            bool volleyDetected =
                state.AmmunitionRoundsPerMan > 0 &&
                !float.IsInfinity(currentNextFireTime) &&
                currentNextFireTime > state.LastObservedNextFireTime + 0.05f &&
                currentNextFireTime > Time.time + 0.05f;

            if (volleyDetected)
            {
                int ammoBefore = state.AmmunitionRoundsPerMan;
                state.AmmunitionRoundsPerMan = Mathf.Max(0, state.AmmunitionRoundsPerMan - 1);
                state.VolleySequence++;

                Regiment target = GetLikelyTarget(regiment);
                state.LastVolleyHits = target != null ? target.LastVolleyHits : 0;
                state.FeedbackUntil = Time.unscaledTime + 2.35f;

                WriteVolleyDiagnostic(
                    regiment,
                    target,
                    state.LastVolleyHits,
                    state.VolleySequence,
                    ammoBefore,
                    state.AmmunitionRoundsPerMan);
            }

            state.LastObservedNextFireTime = currentNextFireTime;

            if (state.AmmunitionRoundsPerMan <= 0)
                nextFireTimeField.SetValue(regiment, float.PositiveInfinity);
        }
    }

    private float ReadNextFireTime(Regiment regiment)
    {
        object value = nextFireTimeField.GetValue(regiment);
        return value is float number ? number : 0f;
    }

    private Regiment GetLikelyTarget(Regiment shooter)
    {
        if (forcedTargetField != null)
        {
            Regiment forced = forcedTargetField.GetValue(shooter) as Regiment;
            if (forced != null && !forced.IsRouted && forced.Team != shooter.Team)
                return forced;
        }

        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return null;

        Regiment nearest = null;
        float best = float.PositiveInfinity;
        float triggerRange = shooter.GetFireTriggerRange();

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == shooter.Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(shooter.transform.position, candidate.transform.position);
            if (distance > triggerRange || distance >= best || !shooter.IsTargetInFireArc(candidate))
                continue;

            best = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static void WriteVolleyDiagnostic(
        Regiment shooter,
        Regiment target,
        int hits,
        int sequence,
        int ammoBefore,
        int ammoAfter)
    {
        if (shooter == null)
            return;

        float distance = target != null
            ? Vector3.Distance(shooter.transform.position, target.transform.position)
            : -1f;

        string band = target != null
            ? PrototypeCombatTuningManager.GetRangeBandLabel(shooter, distance)
            : "NO_TARGET";

        float rangeMultiplier = target != null
            ? PrototypeCombatTuningManager.GetConfiguredRangeMultiplier(shooter, distance)
            : 0f;

        float qualityMultiplier = PrototypeCombatTuningManager.GetConfiguredQualityMultiplier(shooter);
        int firingMen = PrototypeCombatTuningManager.GetConfiguredFiringMen(shooter);
        float expectedHits = target != null
            ? PrototypeCombatTuningManager.GetExpectedHitsPreview(shooter, distance)
            : 0f;

        BattleManager battle = BattleManager.Instance;
        float battleMinutes = battle != null ? battle.BattleMinutes : 0f;
        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;

        string targetName = target != null ? target.RegimentName : "None";
        int targetStrength = target != null ? target.CurrentStrength : -1;

        Debug.Log(string.Format(
            "VOLLEY-LOG|Time={0:00}:{1:00}|Seq={2}|Shooter={3}|Target={4}|Distance={5:0.0}|Band={6}|Policy={7}|Strength={8}|FiringMen={9}|Morale={10:0.0}|Cohesion={11:0.0}|QualityX={12:0.000}|BaseHit={13:0.00}%|RangeX={14:0.000}|Expected={15:0.00}|Hits={16}|TargetStrength={17}|Ammo={18}->{19}",
            hours,
            minutes,
            sequence,
            shooter.RegimentName,
            targetName,
            distance,
            band,
            shooter.GetFirePolicyLabel(),
            shooter.CurrentStrength,
            firingMen,
            shooter.Morale,
            shooter.Cohesion,
            qualityMultiplier,
            PrototypeCombatTuningManager.BaseHitChancePercent,
            rangeMultiplier,
            expectedHits,
            hits,
            targetStrength,
            ammoBefore,
            ammoAfter));
    }

    public static int GetAmmunitionRoundsPerMan(Regiment regiment)
    {
        if (Instance == null || regiment == null)
            return PrototypeCombatTuningManager.GetStartingAmmoRoundsPerMan();

        return Instance.states.TryGetValue(regiment, out CombatState state)
            ? state.AmmunitionRoundsPerMan
            : PrototypeCombatTuningManager.GetStartingAmmoRoundsPerMan();
    }

    public static int GetStartingAmmunitionRoundsPerMan(Regiment regiment)
    {
        if (Instance == null || regiment == null)
            return PrototypeCombatTuningManager.GetStartingAmmoRoundsPerMan();

        return Instance.states.TryGetValue(regiment, out CombatState state)
            ? state.StartingAmmunitionRoundsPerMan
            : PrototypeCombatTuningManager.GetStartingAmmoRoundsPerMan();
    }

    public static bool TryGetVolleyFeedback(Regiment regiment, out int hits)
    {
        hits = 0;

        if (Instance == null || regiment == null)
            return false;

        if (!Instance.states.TryGetValue(regiment, out CombatState state))
            return false;

        if (Time.unscaledTime >= state.FeedbackUntil)
            return false;

        hits = state.LastVolleyHits;
        return true;
    }
}
