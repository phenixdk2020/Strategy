using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09 TEST navigation replacement.
// The earlier navigation/recovery layers could oscillate between a direct goal and
// a one-frame detour when a formation touched a tree. V3 keeps the ORIGINAL goal,
// keeps one chosen detour until it has actually been cleared, and temporarily
// narrows LINE formations to COLUMN while passing discrete obstacles.
[DefaultExecutionOrder(1400)]
public sealed class PrototypeBattlefieldNavigationV3 : MonoBehaviour
{
    private sealed class Obstacle
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
    }

    private sealed class NavigationState
    {
        public bool HasGoal;
        public Vector3 Goal;
        public Vector3 LastSteeringTarget;
        public Vector3 LastAdjustedGoal;

        public bool Detouring;
        public Obstacle ActiveObstacle;
        public Vector3 DetourPoint;

        public bool NarrowedForObstacle;
        public RegimentFormation FormationBeforeObstacle;

        public bool BridgeRouting;
        public bool BridgeFormationStored;
        public RegimentFormation FormationBeforeBridge;
    }

    public static PrototypeBattlefieldNavigationV3 Instance { get; private set; }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, NavigationState> states =
        new Dictionary<Regiment, NavigationState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private FieldInfo playerRoutesField;

    private bool installed;
    private bool legacyDisabled;

    private const float BattlefieldHalfWidth = 176f;
    private const float BattlefieldHalfDepth = 116f;

    private const float RiverHalfWidth = 2.10f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 8.0f;
    private const float BridgeHalfWidthZ = 4.0f;
    private const float BridgeApproachOffset = 12.0f;

    // Conservative formation-centre clearances. LINE uses the visual frontage,
    // not merely the regiment transform point, so a soldier at the edge does not
    // clip a trunk while the centre technically remains clear.
    private const float LineClearance = 9.0f;
    private const float ColumnClearance = 4.4f;
    private const float DetourExtraClearance = 3.0f;
    private const float DetourArrivalDistance = 2.4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattlefieldNavigationV3_v009");
        root.AddComponent<PrototypeBattlefieldNavigationV3>();
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
        playerRoutesField = typeof(PlayerCommander).GetField("routes", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("NAV-V3|Init=Failed|Reason=Regiment movement fields not found");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!installed)
            Install();

        if (!installed)
            return;

        // Let the previous layers complete one install pass so their static query
        // APIs remain usable by the attack-frontage planner, then stop their active
        // steering. V3 is the only movement steering layer after this point.
        DisableLegacySteering();
        SanitizePlayerRoutes();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            ApplyNavigation(regiment);
        }
    }

    private void Install()
    {
        if (BattleManager.Instance == null)
            return;

        RefreshObstacles();
        EnsureBridgeVisual();
        installed = true;

        Debug.Log(string.Format(
            "NAV-V3|Installed=True|Obstacles={0}|LineClearance={1:0.0}|ColumnClearance={2:0.0}|PersistentDetours=True",
            obstacles.Count,
            LineClearance,
            ColumnClearance));
    }

    private void DisableLegacySteering()
    {
        if (legacyDisabled)
            return;

        PrototypeBattlefieldNavigationManager oldNavigation =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (oldNavigation != null)
            oldNavigation.enabled = false;

        PrototypeNavigationRecoveryManager oldRecovery =
            Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (oldRecovery != null)
            oldRecovery.enabled = false;

        legacyDisabled = true;
        Debug.Log("NAV-V3|LegacySteeringDisabled=True");
    }

    private void RefreshObstacles()
    {
        obstacles.Clear();

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform item in all)
        {
            if (item == null)
                continue;

            switch (item.name)
            {
                case "Tree":
                    // Tree crowns can be wider than the old 2.4 radius. Use a
                    // conservative tactical footprint so the line begins turning
                    // before the visual soldiers touch the trunk/crown.
                    AddObstacle("Tree", item.position, 3.4f);
                    break;
                case "Farmhouse":
                    AddObstacle("Farmhouse", item.position, 6.5f);
                    break;
                case "Barn":
                    AddObstacle("Barn", item.position, 4.5f);
                    break;
                case "FencePost":
                    AddObstacle("FencePost", item.position, 0.65f);
                    break;
            }
        }
    }

    private void AddObstacle(string name, Vector3 center, float radius)
    {
        center.y = 0f;
        obstacles.Add(new Obstacle
        {
            Name = name,
            Center = center,
            Radius = radius
        });
    }

    private void EnsureBridgeVisual()
    {
        if (GameObject.Find("PrototypeBridge_v009") != null)
            return;

        float x = StreamCenterX(BridgeZ);
        float y = PrototypeBootstrap.SampleGroundHeight(x, BridgeZ) + 0.24f;

        GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "PrototypeBridge_v009";
        bridge.transform.position = new Vector3(x, y, BridgeZ);
        bridge.transform.localScale = new Vector3(16f, 0.28f, 6.2f);
        bridge.GetComponent<Renderer>().sharedMaterial =
            PrototypeBootstrap.CreateSharedMaterial(
                new Color(0.38f, 0.28f, 0.16f),
                "BridgeDeck");

        Collider collider = bridge.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void ApplyNavigation(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out NavigationState state))
        {
            state = new NavigationState();
            states[regiment] = state;
        }

        RecoverIfInsideObstacle(regiment, state);

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            FinishObstacleDetour(regiment, state, true);
            FinishBridgeRouting(regiment, state, true);
            state.HasGoal = false;
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

        bool externalGoal =
            !state.HasGoal ||
            PlanarDistance(rawDestination, state.LastSteeringTarget) > 0.85f;

        if (externalGoal)
        {
            Vector3 safeGoal;
            if (!TryFindNearestValidDestination(rawDestination, RegimentFormation.Line, out safeGoal))
            {
                safeGoal = regiment.transform.position;
                safeGoal.y = 0f;
            }

            if (PlanarDistance(rawDestination, safeGoal) > 0.50f &&
                PlanarDistance(safeGoal, state.LastAdjustedGoal) > 0.50f)
            {
                Debug.Log(string.Format(
                    "NAV-V3|Unit={0}|GoalAdjusted=True|Requested=({1:0.0},{2:0.0})|Safe=({3:0.0},{4:0.0})|Reason=BlockedDestination",
                    regiment.RegimentName,
                    rawDestination.x,
                    rawDestination.z,
                    safeGoal.x,
                    safeGoal.z));
                state.LastAdjustedGoal = safeGoal;
            }

            // A materially new order invalidates the previous obstacle detour.
            if (state.HasGoal && PlanarDistance(state.Goal, safeGoal) > 1.5f)
                FinishObstacleDetour(regiment, state, false);

            state.Goal = safeGoal;
            state.HasGoal = true;
        }

        Vector3 current = regiment.transform.position;
        current.y = 0f;

        Vector3 navigationGoal = state.Goal;

        bool needsBridge = SegmentCrossesRiver(
            current,
            state.Goal,
            ColumnClearance);

        if (needsBridge)
        {
            BeginBridgeRouting(regiment, state);
            navigationGoal = GetBridgeSteeringTarget(current, state.Goal);
        }
        else
        {
            FinishBridgeRouting(regiment, state, false);
        }

        // If an existing detour is still active, keep it. This is the key difference
        // from the old one-frame tangent steering which could flip sides every frame.
        if (state.Detouring)
        {
            bool reachedDetour =
                PlanarDistance(current, state.DetourPoint) <= DetourArrivalDistance;

            bool fullLinePathClear =
                !PathBlockedByObstacle(current, navigationGoal, LineClearance, null);

            bool clearOfActiveObstacle =
                state.ActiveObstacle == null ||
                PlanarDistance(current, state.ActiveObstacle.Center) >=
                    state.ActiveObstacle.Radius + LineClearance + 1.0f;

            if (reachedDetour || (fullLinePathClear && clearOfActiveObstacle))
            {
                FinishObstacleDetour(regiment, state, false);
            }
            else
            {
                SetSteering(regiment, state, state.DetourPoint);
                return;
            }
        }

        float planningClearance =
            regiment.Formation == RegimentFormation.Line
                ? LineClearance
                : ColumnClearance;

        Obstacle blocker = FindFirstBlockingObstacle(
            current,
            navigationGoal,
            planningClearance);

        if (blocker != null)
        {
            BeginObstacleDetour(regiment, state, blocker, current, navigationGoal);

            if (state.Detouring)
            {
                SetSteering(regiment, state, state.DetourPoint);
                return;
            }
        }

        // No discrete obstacle in the way. If the direct path is otherwise legal,
        // restore line after a previous obstacle pass and continue toward the real goal.
        FinishObstacleDetour(regiment, state, false);
        SetSteering(regiment, state, navigationGoal);
    }

    private void BeginObstacleDetour(
        Regiment regiment,
        NavigationState state,
        Obstacle blocker,
        Vector3 current,
        Vector3 goal)
    {
        if (regiment.Formation == RegimentFormation.Line && !state.NarrowedForObstacle)
        {
            state.FormationBeforeObstacle = regiment.Formation;
            state.NarrowedForObstacle = true;
            regiment.SetFormation(RegimentFormation.Column);
        }

        Vector3 detour;
        if (!TryChoosePersistentDetour(current, goal, blocker, out detour))
        {
            Debug.Log(string.Format(
                "NAV-V3|Unit={0}|Detour=False|Obstacle={1}|Reason=NoSafeCandidate",
                regiment.RegimentName,
                blocker.Name));
            return;
        }

        state.Detouring = true;
        state.ActiveObstacle = blocker;
        state.DetourPoint = detour;

        Debug.Log(string.Format(
            "NAV-V3|Unit={0}|Detour=True|Obstacle={1}|Center=({2:0.0},{3:0.0})|Via=({4:0.0},{5:0.0})|Formation=COLUMN",
            regiment.RegimentName,
            blocker.Name,
            blocker.Center.x,
            blocker.Center.z,
            detour.x,
            detour.z));
    }

    private void FinishObstacleDetour(
        Regiment regiment,
        NavigationState state,
        bool forceRestore)
    {
        if (state.Detouring)
        {
            state.Detouring = false;
            state.ActiveObstacle = null;
            state.DetourPoint = Vector3.zero;
        }

        if (!state.NarrowedForObstacle)
            return;

        if (!forceRestore && state.BridgeRouting)
            return;

        if (regiment != null && !regiment.IsRouted)
            regiment.SetFormation(state.FormationBeforeObstacle);

        state.NarrowedForObstacle = false;
    }

    private bool TryChoosePersistentDetour(
        Vector3 current,
        Vector3 goal,
        Obstacle blocker,
        out Vector3 detour)
    {
        float[] extraRadii = { DetourExtraClearance, 6f, 9f };
        float bestCost = float.PositiveInfinity;
        Vector3 best = current;
        bool found = false;

        foreach (float extra in extraRadii)
        {
            float radius = blocker.Radius + ColumnClearance + extra;

            for (int i = 0; i < 64; i++)
            {
                float angle = i / 64f * Mathf.PI * 2f;
                Vector3 candidate = blocker.Center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

                candidate.x = Mathf.Clamp(candidate.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
                candidate.z = Mathf.Clamp(candidate.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);

                if (!IsPointNavigable(candidate, ColumnClearance))
                    continue;

                // The path INTO the detour must already clear the blocker; otherwise
                // we would still drive the formation into the tree before turning.
                float blockerDistance = DistancePointToSegment(
                    blocker.Center,
                    current,
                    candidate,
                    out float along);

                if (blockerDistance < blocker.Radius + ColumnClearance - 0.15f)
                    continue;

                if (PathBlockedByObstacle(current, candidate, ColumnClearance, blocker))
                    continue;

                if (SegmentCrossesRiver(current, candidate, ColumnClearance) &&
                    !IsBridgeZone(candidate))
                {
                    continue;
                }

                float cost =
                    PlanarDistance(current, candidate) +
                    PlanarDistance(candidate, goal);

                // Prefer candidates from which the final goal is visible, but do
                // not require it; tree clusters may need more than one detour leg.
                if (PathBlockedByObstacle(candidate, goal, ColumnClearance, blocker))
                    cost += 45f;

                if (SegmentCrossesRiver(candidate, goal, ColumnClearance))
                    cost += 80f;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = candidate;
                    found = true;
                }
            }

            if (found)
                break;
        }

        detour = best;
        return found;
    }

    private void BeginBridgeRouting(Regiment regiment, NavigationState state)
    {
        if (!state.BridgeRouting)
        {
            state.BridgeRouting = true;

            if (!state.NarrowedForObstacle)
            {
                state.FormationBeforeBridge = regiment.Formation;
                state.BridgeFormationStored = true;
            }

            Debug.Log("NAV-V3|Unit=" + regiment.RegimentName + "|RiverCrossing=BridgeRoute");
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);
    }

    private void FinishBridgeRouting(
        Regiment regiment,
        NavigationState state,
        bool forceRestore)
    {
        if (!state.BridgeRouting)
            return;

        state.BridgeRouting = false;

        if (state.NarrowedForObstacle && !forceRestore)
            return;

        if (state.BridgeFormationStored && regiment != null && !regiment.IsRouted)
            regiment.SetFormation(state.FormationBeforeBridge);

        state.BridgeFormationStored = false;
    }

    private void SetSteering(
        Regiment regiment,
        NavigationState state,
        Vector3 target)
    {
        target.x = Mathf.Clamp(target.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        target.z = Mathf.Clamp(target.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;

        destinationField.SetValue(regiment, target);
        hasDestinationField.SetValue(regiment, true);
        state.LastSteeringTarget = target;
    }

    private void RecoverIfInsideObstacle(Regiment regiment, NavigationState state)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        // Use column clearance for emergency recovery. A LINE may visually touch an
        // obstacle while its centre is still safe; that situation is handled by
        // proactive narrowing/detour rather than teleporting the regiment.
        if (IsPointNavigable(current, ColumnClearance))
            return;

        Vector3 safe;
        if (!TryFindNearestValidDestination(current, RegimentFormation.Column, out safe))
            return;

        if (PlanarDistance(current, safe) <= 0.15f)
            return;

        if (regiment.Formation == RegimentFormation.Line && !state.NarrowedForObstacle)
        {
            state.FormationBeforeObstacle = regiment.Formation;
            state.NarrowedForObstacle = true;
            regiment.SetFormation(RegimentFormation.Column);
        }

        safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
        regiment.transform.position = safe;

        Debug.Log(string.Format(
            "NAV-V3|Unit={0}|EmergencyUnstick=True|Safe=({1:0.0},{2:0.0})",
            regiment.RegimentName,
            safe.x,
            safe.z));
    }

    private bool PathBlockedByObstacle(
        Vector3 start,
        Vector3 end,
        float clearance,
        Obstacle ignore)
    {
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle == ignore)
                continue;

            float distance = DistancePointToSegment(
                obstacle.Center,
                start,
                end,
                out float along);

            if (distance < obstacle.Radius + clearance)
                return true;
        }

        return false;
    }

    private Obstacle FindFirstBlockingObstacle(
        Vector3 start,
        Vector3 end,
        float clearance)
    {
        Obstacle best = null;
        float bestAlong = float.PositiveInfinity;

        foreach (Obstacle obstacle in obstacles)
        {
            float distance = DistancePointToSegment(
                obstacle.Center,
                start,
                end,
                out float along);

            if (distance >= obstacle.Radius + clearance)
                continue;

            if (along < bestAlong)
            {
                bestAlong = along;
                best = obstacle;
            }
        }

        return best;
    }

    public static bool TryFindNearestValidDestination(
        Vector3 requested,
        RegimentFormation formation,
        out Vector3 valid)
    {
        requested.x = Mathf.Clamp(requested.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        requested.z = Mathf.Clamp(requested.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        requested.y = 0f;

        PrototypeBattlefieldNavigationV3 manager = Instance;
        if (manager == null)
        {
            valid = requested;
            return true;
        }

        float clearance = formation == RegimentFormation.Line
            ? LineClearance
            : ColumnClearance;

        if (manager.IsPointNavigable(requested, clearance))
        {
            valid = requested;
            return true;
        }

        for (float radius = 3f; radius <= 42f; radius += 3f)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                Vector3 candidate = requested + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

                candidate.x = Mathf.Clamp(candidate.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
                candidate.z = Mathf.Clamp(candidate.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);

                if (manager.IsPointNavigable(candidate, clearance))
                {
                    valid = candidate;
                    return true;
                }
            }
        }

        valid = requested;
        return false;
    }

    public static bool IsBlockedDestination(
        Vector3 point,
        RegimentFormation formation)
    {
        if (Instance == null)
            return false;

        float clearance = formation == RegimentFormation.Line
            ? LineClearance
            : ColumnClearance;

        return !Instance.IsPointNavigable(point, clearance);
    }

    public static float EstimatePathPenalty(
        Vector3 start,
        Vector3 end,
        RegimentFormation formation)
    {
        if (Instance == null)
            return 0f;

        float clearance = formation == RegimentFormation.Line
            ? LineClearance
            : ColumnClearance;

        float penalty = 0f;
        foreach (Obstacle obstacle in Instance.obstacles)
        {
            float distance = DistancePointToSegment(
                obstacle.Center,
                start,
                end,
                out float along);

            float required = obstacle.Radius + clearance;
            if (distance >= required)
                continue;

            float depth = required - distance;
            float weight =
                obstacle.Name == "Farmhouse" || obstacle.Name == "Barn"
                    ? 200f
                    : obstacle.Name == "Tree"
                        ? 100f
                        : 40f;

            penalty += weight + depth * 12f;
        }

        if (SegmentCrossesRiver(start, end, clearance))
            penalty += 500f;

        return penalty;
    }

    public static bool HasObstacleBetween(
        Vector3 start,
        Vector3 end,
        RegimentFormation formation)
    {
        return EstimatePathPenalty(start, end, formation) > 0.1f;
    }

    private bool IsPointNavigable(Vector3 point, float clearance)
    {
        if (Mathf.Abs(point.x) > BattlefieldHalfWidth ||
            Mathf.Abs(point.z) > BattlefieldHalfDepth)
        {
            return false;
        }

        if (IsRiverWater(point, clearance) && !IsBridgeZone(point))
            return false;

        foreach (Obstacle obstacle in obstacles)
        {
            if (PlanarDistance(point, obstacle.Center) < obstacle.Radius + clearance)
                return false;
        }

        return true;
    }

    private Vector3 GetBridgeSteeringTarget(Vector3 current, Vector3 goal)
    {
        float bridgeX = StreamCenterX(BridgeZ);
        float currentSide = BankSide(current);
        float goalSide = BankSide(goal);

        if (Mathf.Abs(current.z - BridgeZ) <= BridgeHalfWidthZ + 1f &&
            Mathf.Abs(current.x - bridgeX) <= BridgeHalfLengthX + 2f)
        {
            float exitSide = Mathf.Abs(goalSide) > 0.1f ? goalSide : -currentSide;
            return new Vector3(
                bridgeX + exitSide * BridgeApproachOffset,
                0f,
                BridgeZ);
        }

        if (Mathf.Abs(currentSide) < 0.1f)
            currentSide = goalSide >= 0f ? -1f : 1f;

        return new Vector3(
            bridgeX + currentSide * BridgeApproachOffset,
            0f,
            BridgeZ);
    }

    private void SanitizePlayerRoutes()
    {
        if (playerRoutesField == null || PlayerCommander.Instance == null)
            return;

        IDictionary routes = playerRoutesField.GetValue(PlayerCommander.Instance) as IDictionary;
        if (routes == null)
            return;

        foreach (DictionaryEntry entry in routes)
        {
            Regiment regiment = entry.Key as Regiment;
            object route = entry.Value;
            if (regiment == null || route == null)
                continue;

            FieldInfo waypointsField = route.GetType().GetField(
                "Waypoints",
                BindingFlags.Instance | BindingFlags.Public);

            if (waypointsField == null)
                continue;

            IList waypoints = waypointsField.GetValue(route) as IList;
            if (waypoints == null)
                continue;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (!(waypoints[i] is Vector3 requested))
                    continue;

                Vector3 safe;
                if (!TryFindNearestValidDestination(
                    requested,
                    RegimentFormation.Line,
                    out safe))
                {
                    continue;
                }

                if (PlanarDistance(requested, safe) <= 0.50f)
                    continue;

                safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
                waypoints[i] = safe;

                Debug.Log(string.Format(
                    "NAV-V3|Unit={0}|RouteWaypointAdjusted=True|Index={1}|Reason=ObstacleOrWater",
                    regiment.RegimentName,
                    i));
            }
        }
    }

    private static float DistancePointToSegment(
        Vector3 point,
        Vector3 a,
        Vector3 b,
        out float along01)
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

    private static float StreamCenterX(float z)
    {
        return Mathf.Sin(z * 0.065f) * 4.8f;
    }

    private static float BankSide(Vector3 point)
    {
        return Mathf.Sign(point.x - StreamCenterX(point.z));
    }

    private static bool IsBridgeZone(Vector3 point)
    {
        float bridgeX = StreamCenterX(BridgeZ);
        return Mathf.Abs(point.z - BridgeZ) <= BridgeHalfWidthZ &&
               Mathf.Abs(point.x - bridgeX) <= BridgeHalfLengthX;
    }

    private static bool IsRiverWater(Vector3 point, float clearance)
    {
        float extra = Mathf.Min(1.5f, clearance * 0.18f);
        return Mathf.Abs(point.x - StreamCenterX(point.z)) <= RiverHalfWidth + extra;
    }

    private static bool SegmentCrossesRiver(
        Vector3 a,
        Vector3 b,
        float clearance)
    {
        const int samples = 40;

        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector3 point = Vector3.Lerp(a, b, t);

            if (IsRiverWater(point, clearance) && !IsBridgeZone(point))
                return true;
        }

        return false;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
