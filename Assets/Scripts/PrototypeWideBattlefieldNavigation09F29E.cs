using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29e
// Wide-battlefield movement guard for the 5760 x 3840 m F29B map.
// The older V4 A* layer was still compiled around the original +/-176 x +/-116 m
// prototype grid and could clamp/unstick valid companies back into that tiny area.
// F29E disables those obsolete bounded navigation writers and uses dynamic battlefield
// limits, persistent final goals and light-weight obstacle detours. The existing F15
// bridge-only phase machine remains the sole river-crossing authority.
[DefaultExecutionOrder(1450)]
public sealed class PrototypeWideBattlefieldNavigation09F29E : MonoBehaviour
{
    private sealed class Obstacle
    {
        public Vector3 Center;
        public float Radius;
        public string Name;
    }

    private sealed class NavigationState
    {
        public bool HasGoal;
        public Vector3 FinalGoal;
        public Vector3 LastWrittenTarget;
        public bool HasDetour;
        public Vector3 Detour;
        public float NextPlan;
        public Vector3 LastPosition;
        public float LastProgressAt;
        public bool FormationStored;
        public RegimentFormation FormationBeforeDetour;
    }

    public static PrototypeWideBattlefieldNavigation09F29E Instance { get; private set; }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, NavigationState> states =
        new Dictionary<Regiment, NavigationState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool legacyDisabled;
    private bool obstaclesLoaded;

    private const float GoalChangeTolerance = 1.25f;
    private const float DetourArrival = 2.5f;
    private const float TravelClearance = 3.8f;
    private const float ReplanInterval = 0.28f;
    private const float StuckSeconds = 1.2f;
    private const float EdgeMargin = 18f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeWideBattlefieldNavigation09F29E>() == null)
            new GameObject("PrototypeWideBattlefieldNavigation_v000009f29e")
                .AddComponent<PrototypeWideBattlefieldNavigation09F29E>();
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
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("NAV-WIDE-09F29E|Installed=False|Reason=RegimentMovementReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "NAV-WIDE-09F29E|Installed=True|Bounds=" +
            PrototypeBootstrap.BattlefieldWidth.ToString("0") + "x" +
            PrototypeBootstrap.BattlefieldDepth.ToString("0") +
            "|LegacySmallGridDisabled=True|RiverAuthority=PrototypeRiverBridgeOnly09F3");
    }

    private void OnDestroy()
    {
        RestoreAllFormations();
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        DisableObsoleteNavigation();
        EnsureObstacles();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null)
                continue;

            active.Add(unit);
            UpdateUnit(unit);
        }

        Cleanup(active);
    }

    private void DisableObsoleteNavigation()
    {
        if (legacyDisabled)
            return;

        PrototypeBattlefieldNavigationV4 v4 =
            UnityEngine.Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV4>();
        if (v4 != null)
            v4.enabled = false;

        PrototypeBattlefieldNavigationV3 v3 =
            UnityEngine.Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>();
        if (v3 != null)
            v3.enabled = false;

        PrototypeBattlefieldNavigationManager v1 =
            UnityEngine.Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (v1 != null)
            v1.enabled = false;

        PrototypeNavigationRecoveryManager recovery =
            UnityEngine.Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (recovery != null)
            recovery.enabled = false;

        legacyDisabled = true;
        Debug.Log("NAV-WIDE-09F29E|ObsoleteNavigationDisabled=True|V4=True|V3=True|V1=True|Recovery=True");
    }

    private void EnsureObstacles()
    {
        if (obstaclesLoaded || BattleManager.Instance == null)
            return;

        obstacles.Clear();
        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>();
        foreach (Transform item in all)
        {
            if (item == null)
                continue;

            switch (item.name)
            {
                case "Tree": AddObstacle(item.name, item.position, 2.6f); break;
                case "Farmhouse": AddObstacle(item.name, item.position, 6.5f); break;
                case "Barn": AddObstacle(item.name, item.position, 4.5f); break;
                case "FencePost": AddObstacle(item.name, item.position, 0.55f); break;
            }
        }

        obstaclesLoaded = true;
        Debug.Log("NAV-WIDE-09F29E|ObstaclesLoaded=" + obstacles.Count);
    }

    private void AddObstacle(string name, Vector3 center, float radius)
    {
        center.y = 0f;
        obstacles.Add(new Obstacle { Name = name, Center = center, Radius = radius });
    }

    private void UpdateUnit(Regiment unit)
    {
        if (unit.IsRouted)
        {
            if (states.TryGetValue(unit, out NavigationState routed))
                RestoreFormation(unit, routed);
            states.Remove(unit);
            return;
        }

        if (!states.TryGetValue(unit, out NavigationState state))
        {
            state = new NavigationState
            {
                LastPosition = unit.transform.position,
                LastProgressAt = Time.time
            };
            states[unit] = state;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(unit);
        if (!hasDestination)
        {
            state.HasGoal = false;
            state.HasDetour = false;
            RestoreFormation(unit, state);
            state.LastPosition = unit.transform.position;
            state.LastProgressAt = Time.time;
            return;
        }

        // The dedicated river phase machine runs later (execution order 5000) and
        // owns bridge staging/entry/cross/exit steering. Never reinterpret its temporary
        // steering targets as new final goals.
        if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(unit))
        {
            state.LastPosition = unit.transform.position;
            state.LastProgressAt = Time.time;
            return;
        }

        Vector3 raw = (Vector3)destinationField.GetValue(unit);
        raw.y = 0f;

        bool rawIsOurSteering = state.HasGoal &&
            PlanarDistance(raw, state.LastWrittenTarget) <= GoalChangeTolerance;
        bool rawIsFinalGoal = state.HasGoal &&
            PlanarDistance(raw, state.FinalGoal) <= GoalChangeTolerance;

        if (!state.HasGoal || (!rawIsOurSteering && !rawIsFinalGoal))
        {
            state.FinalGoal = ClampToBattlefield(raw);
            state.HasGoal = true;
            state.HasDetour = false;
            state.NextPlan = 0f;
            state.LastProgressAt = Time.time;
            state.LastPosition = unit.transform.position;
        }

        float moved = PlanarDistance(unit.transform.position, state.LastPosition);
        state.LastPosition = unit.transform.position;
        if (moved > 0.035f)
            state.LastProgressAt = Time.time;

        bool stuck = Time.time - state.LastProgressAt >= StuckSeconds;
        bool detourReached = state.HasDetour &&
            PlanarDistance(unit.transform.position, state.Detour) <= DetourArrival;

        if (!stuck && !detourReached && Time.time < state.NextPlan)
            return;

        state.NextPlan = Time.time + ReplanInterval;
        if (stuck)
        {
            state.LastProgressAt = Time.time;
            state.HasDetour = false;
        }

        PlanSteering(unit, state, stuck ? "STUCK_REPLAN" : detourReached ? "DETOUR_REACHED" : "PLAN");
    }

    private void PlanSteering(Regiment unit, NavigationState state, string reason)
    {
        Vector3 current = unit.transform.position;
        current.y = 0f;
        Vector3 goal = ClampToBattlefield(state.FinalGoal);

        Obstacle blocker = FindFirstBlockingObstacle(current, goal, TravelClearance);
        if (blocker == null)
        {
            state.HasDetour = false;
            RestoreFormation(unit, state);
            WriteTarget(unit, state, goal);
            return;
        }

        Vector3 direct = Flat(goal - current);
        Vector3 side = Vector3.Cross(Vector3.up, direct).normalized;
        if (side.sqrMagnitude < 0.01f)
            side = Vector3.right;

        float baseRadius = blocker.Radius + TravelClearance + 5.0f;
        Vector3 best = current;
        float bestCost = float.PositiveInfinity;
        bool found = false;

        for (int ring = 0; ring < 5; ring++)
        {
            float radius = baseRadius + ring * 4f;
            Vector3[] candidates =
            {
                blocker.Center + side * radius + direct * 3f,
                blocker.Center - side * radius + direct * 3f,
                blocker.Center + side * radius - direct * 3f,
                blocker.Center - side * radius - direct * 3f
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3 candidate = ClampToBattlefield(candidates[i]);
                if (IsPointBlocked(candidate, TravelClearance))
                    continue;
                if (FindFirstBlockingObstacle(current, candidate, TravelClearance) != null)
                    continue;

                float cost = PlanarDistance(current, candidate) +
                             PlanarDistance(candidate, goal) * 0.92f;
                if (cost >= bestCost)
                    continue;

                bestCost = cost;
                best = candidate;
                found = true;
            }

            if (found)
                break;
        }

        if (!found)
        {
            // Never clamp back to the obsolete small map. Leave the original final goal
            // intact and let the hard river/final-endpoint safety layers handle it.
            state.HasDetour = false;
            WriteTarget(unit, state, goal);
            Debug.LogWarning("NAV-WIDE-09F29E|Unit=" + unit.RegimentName +
                             "|Detour=False|Blocker=" + blocker.Name +
                             "|Reason=NoLocalObstacleDetour");
            return;
        }

        if (!state.FormationStored)
        {
            state.FormationBeforeDetour = unit.Formation;
            state.FormationStored = true;
        }
        if (unit.Formation != RegimentFormation.Column)
            unit.SetFormation(RegimentFormation.Column);

        state.HasDetour = true;
        state.Detour = best;
        WriteTarget(unit, state, best);

        Debug.Log("NAV-WIDE-09F29E|Unit=" + unit.RegimentName +
                  "|Detour=True|Blocker=" + blocker.Name +
                  "|Reason=" + reason +
                  "|Waypoint=" + best.x.ToString("0.0") + "," + best.z.ToString("0.0") +
                  "|Final=" + goal.x.ToString("0.0") + "," + goal.z.ToString("0.0"));
    }

    private void WriteTarget(Regiment unit, NavigationState state, Vector3 target)
    {
        target = ClampToBattlefield(target);
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        destinationField.SetValue(unit, target);
        hasDestinationField.SetValue(unit, true);
        state.LastWrittenTarget = target;
    }

    private Obstacle FindFirstBlockingObstacle(Vector3 a, Vector3 b, float clearance)
    {
        Obstacle best = null;
        float bestAlong = float.PositiveInfinity;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle obstacle = obstacles[i];
            float along;
            float distance = DistancePointToSegment(obstacle.Center, a, b, out along);
            if (distance >= obstacle.Radius + clearance || along <= 0.01f)
                continue;
            if (along >= bestAlong)
                continue;
            bestAlong = along;
            best = obstacle;
        }

        return best;
    }

    private bool IsPointBlocked(Vector3 point, float clearance)
    {
        if (Mathf.Abs(point.x) > PrototypeBootstrap.BattlefieldHalfWidth - EdgeMargin ||
            Mathf.Abs(point.z) > PrototypeBootstrap.BattlefieldHalfDepth - EdgeMargin)
            return true;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle obstacle = obstacles[i];
            if (PlanarDistance(point, obstacle.Center) < obstacle.Radius + clearance)
                return true;
        }
        return false;
    }

    private static Vector3 ClampToBattlefield(Vector3 point)
    {
        float xLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfWidth - EdgeMargin);
        float zLimit = Mathf.Max(30f, PrototypeBootstrap.BattlefieldHalfDepth - EdgeMargin);
        point.x = Mathf.Clamp(point.x, -xLimit, xLimit);
        point.z = Mathf.Clamp(point.z, -zLimit, zLimit);
        point.y = 0f;
        return point;
    }

    private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b, out float along01)
    {
        point.y = 0f;
        a.y = 0f;
        b.y = 0f;
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 0.001f)
        {
            along01 = 0f;
            return Vector3.Distance(point, a);
        }
        along01 = Mathf.Clamp01(Vector3.Dot(point - a, ab) / denom);
        Vector3 closest = a + ab * along01;
        return Vector3.Distance(point, closest);
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void RestoreFormation(Regiment unit, NavigationState state)
    {
        if (unit == null || state == null || !state.FormationStored)
            return;
        if (!unit.IsRouted)
            unit.SetFormation(state.FormationBeforeDetour);
        state.FormationStored = false;
    }

    private void RestoreAllFormations()
    {
        foreach (KeyValuePair<Regiment, NavigationState> pair in states)
            RestoreFormation(pair.Key, pair.Value);
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        if (states.Count == 0)
            return;
        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, NavigationState> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;
            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }
        if (remove == null)
            return;
        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }
}
