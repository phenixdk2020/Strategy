using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f12 attack-route authority.
// Direct ATTACK orders normally let Regiment continuously rewrite destination to the
// target position. That bypasses bridge/building steering. This layer temporarily owns
// physical movement while terrain detours are required, then restores the original
// forced target once a clear legal approach exists again.
[DefaultExecutionOrder(-5000)]
public sealed class PrototypeAttackRouteAuthority09F12 : MonoBehaviour
{
    private sealed class AttackRouteState
    {
        public Regiment Target;
        public bool Active;
        public string LastOwner = string.Empty;
    }

    private readonly Dictionary<Regiment, AttackRouteState> states =
        new Dictionary<Regiment, AttackRouteState>();

    private FieldInfo forcedTargetField;
    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeAttackRouteAuthority09F12>() != null)
            return;

        GameObject root = new GameObject("PrototypeAttackRouteAuthority_v000009f12");
        root.AddComponent<PrototypeAttackRouteAuthority09F12>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (forcedTargetField == null || destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("ATTACK-ROUTE-09F12|Installed=False|Reason=RegimentFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "ATTACK-ROUTE-09F12|Installed=True|Priority=BeforeRegimentMovement|" +
            "Terrain=RiverThenBuildings|ForcedTargetSuspension=True");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        List<Regiment> stale = null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted || regiment.CurrentStrength <= 0)
                continue;

            states.TryGetValue(regiment, out AttackRouteState active);
            Regiment forced = forcedTargetField.GetValue(regiment) as Regiment;

            // A normal clear ATTACK should remain completely untouched. Only create
            // route ownership when a terrain detour is actually required.
            if (active == null || !active.Active)
            {
                if (forced == null || forced.IsRouted || forced.CurrentStrength <= 0)
                    continue;

                Vector3 initialTargetPosition = forced.transform.position;
                initialTargetPosition.y = 0f;

                bool initialRiverDetour = PrototypeRiverBridgeOnly09F3.TryGetAttackSteering(
                    regiment,
                    initialTargetPosition,
                    out Vector3 initialRiverSteering);

                bool initialBuildingDetour = !initialRiverDetour &&
                    PrototypeStaticObstacleRouting09F11.RequiresDetour(regiment, initialTargetPosition);

                if (!initialRiverDetour && !initialBuildingDetour)
                    continue;

                active = GetOrCreate(regiment);
                active.Target = forced;
                active.Active = true;
                active.LastOwner = string.Empty;

                ApplyRouteOwnership(
                    regiment,
                    active,
                    initialRiverDetour,
                    initialRiverSteering,
                    initialBuildingDetour,
                    initialTargetPosition);
                continue;
            }

            // If another explicit attack target was assigned while this terrain route
            // was active, preserve the new target as the strategic mission intent.
            if (forced != null && forced != active.Target && !forced.IsRouted && forced.CurrentStrength > 0)
                active.Target = forced;

            if (active.Target == null || active.Target.IsRouted || active.Target.CurrentStrength <= 0)
            {
                RestoreAndClear(regiment, active, null, "TARGET_INVALID");
                AddStale(ref stale, regiment);
                continue;
            }

            Vector3 targetPosition = active.Target.transform.position;
            targetPosition.y = 0f;

            bool riverDetour = PrototypeRiverBridgeOnly09F3.TryGetAttackSteering(
                regiment,
                targetPosition,
                out Vector3 riverSteering);

            bool buildingDetour = !riverDetour &&
                PrototypeStaticObstacleRouting09F11.RequiresDetour(regiment, targetPosition);

            if (!riverDetour && !buildingDetour)
            {
                RestoreAndClear(regiment, active, active.Target, "CLEAR_APPROACH");
                AddStale(ref stale, regiment);
                continue;
            }

            ApplyRouteOwnership(
                regiment,
                active,
                riverDetour,
                riverSteering,
                buildingDetour,
                targetPosition);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
            states.Remove(stale[i]);
    }

    private void ApplyRouteOwnership(
        Regiment regiment,
        AttackRouteState state,
        bool riverDetour,
        Vector3 riverSteering,
        bool buildingDetour,
        Vector3 targetPosition)
    {
        forcedTargetField.SetValue(regiment, null);

        Vector3 steering = riverDetour ? riverSteering : targetPosition;
        steering.y = PrototypeBootstrap.SampleGroundHeight(steering.x, steering.z) + 0.10f;
        destinationField.SetValue(regiment, steering);
        hasDestinationField.SetValue(regiment, true);

        string owner = riverDetour ? "RIVER" : buildingDetour ? "BUILDING" : "NONE";
        if (state.LastOwner == owner)
            return;

        state.LastOwner = owner;
        Debug.Log(
            "ATTACK-ROUTE-09F12|Unit=" + regiment.RegimentName +
            "|Target=" + state.Target.RegimentName +
            "|Owner=" + owner +
            "|DirectAttackSuspended=True");
    }

    private AttackRouteState GetOrCreate(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out AttackRouteState state) || state == null)
        {
            state = new AttackRouteState();
            states[regiment] = state;
        }

        return state;
    }

    private void RestoreAndClear(
        Regiment regiment,
        AttackRouteState state,
        Regiment target,
        string reason)
    {
        forcedTargetField.SetValue(regiment, target);
        hasDestinationField.SetValue(regiment, false);
        state.Active = false;

        Debug.Log(
            "ATTACK-ROUTE-09F12|Unit=" + regiment.RegimentName +
            "|RouteReleased=True|Reason=" + reason +
            "|ForcedTargetRestored=" + (target != null));
    }

    private static void AddStale(ref List<Regiment> stale, Regiment regiment)
    {
        if (stale == null)
            stale = new List<Regiment>();
        stale.Add(regiment);
    }
}
