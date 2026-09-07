using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class PrototypeCombatStatusManager : MonoBehaviour
{
    private sealed class CombatState
    {
        public int AmmunitionRoundsPerMan = 60;
        public float LastObservedNextFireTime;
        public int LastVolleyHits;
        public float FeedbackUntil;
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
                state = new CombatState();
                state.LastObservedNextFireTime = ReadNextFireTime(regiment);
                states[regiment] = state;
            }

            float currentNextFireTime = ReadNextFireTime(regiment);

            // FireVolley moves nextFireTime from its previous value to a future timestamp.
            // Observing that transition lets this TEST layer count one carried round per man
            // per volley without changing the v00.00.08 combat kernel yet.
            bool volleyDetected =
                state.AmmunitionRoundsPerMan > 0 &&
                !float.IsInfinity(currentNextFireTime) &&
                currentNextFireTime > state.LastObservedNextFireTime + 0.05f &&
                currentNextFireTime > Time.time + 0.05f;

            if (volleyDetected)
            {
                state.AmmunitionRoundsPerMan = Mathf.Max(0, state.AmmunitionRoundsPerMan - 1);

                Regiment target = GetLikelyTarget(regiment);
                state.LastVolleyHits = target != null ? target.LastVolleyHits : 0;
                state.FeedbackUntil = Time.unscaledTime + 1.75f;
            }

            state.LastObservedNextFireTime = currentNextFireTime;

            if (state.AmmunitionRoundsPerMan <= 0)
            {
                // No magical ammunition regeneration. A later supply system can replace this
                // TEST-layer lock with actual carried/resupplied ammunition inventories.
                nextFireTimeField.SetValue(regiment, float.PositiveInfinity);
            }
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

    public static int GetAmmunitionRoundsPerMan(Regiment regiment)
    {
        if (Instance == null || regiment == null)
            return 60;

        return Instance.states.TryGetValue(regiment, out CombatState state)
            ? state.AmmunitionRoundsPerMan
            : 60;
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
