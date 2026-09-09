using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(900)]
public sealed class PrototypeBattlefieldNavigationManager : MonoBehaviour
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
        public bool BridgeRouting;
        public RegimentFormation FormationBeforeBridge;
        public bool FormationStored;
        public Vector3 LastAdjustedGoal;
    }

    public static PrototypeBattlefieldNavigationManager Instance { get; private set; }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, NavigationState> states =
        new Dictionary<Regiment, NavigationState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private FieldInfo playerRoutesField;
    private bool installed;

    private const float RiverHalfWidth = 2.10f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 8.0f;
    private const float BridgeHalfWidthZ = 4.0f;
    private const float BridgeApproachOffset = 11.0f;
    private const float BattlefieldHalfWidth = 176f;
    private const float BattlefieldHalfDepth = 116f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattlefieldNavigationManager_v009");
        root.AddComponent<PrototypeBattlefieldNavigationManager>();
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
            Debug.LogError("NAV-DIAG|Init=Failed|Reason=Regiment movement fields not found");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!installed)
            TryInstall();

        if (!installed || destinationField == null || hasDestinationField == null)
            return;

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

    private void TryInstall()
    {
        if (BattleManager.Instance == null)
            return;

        RefreshStaticObstacles();
        CreateBridgeVisual();
        installed = true;

        Debug.Log(string.Format(
            "NAV-DIAG|Installed=True|Obstacles={0}|River=Blocked|FixedBridgeZ={1:0.0}|Pontoon=RequiresFutureEngineerAttachment",
            obstacles.Count,
            BridgeZ));
    }

    private void RefreshStaticObstacles()
    {
        obstacles.Clear();

        Transform[] all = Object.FindObjectsByType<Transform>();
        foreach (Transform item in all)
        {
            if (item == null)
                continue;

            switch (item.name)
            {
                case "Tree":
                    AddObstacle(item.name, item.position, 2.4f);
                    break;
                case "Farmhouse":
                    AddObstacle(item.name, item.position, 5.5f);
                    break;
                case "Barn":
                    AddObstacle(item.name, item.position, 3.8f);
                    break;
                case "FencePost":
                    AddObstacle(item.name, item.position, 0.55f);
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

    private void CreateBridgeVisual()
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
            PrototypeBootstrap.CreateSharedMaterial(new Color(0.38f, 0.28f, 0.16f), "BridgeDeck");

        Collider collider = bridge.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void ApplyNavigation(Regiment regiment)
    {
        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);

        if (!states.TryGetValue(regiment, out NavigationState state))
        {
            state = new NavigationState();
            states[regiment] = state;
        }

        if (!hasDestination)
        {
            RestoreFormationAfterBridge(regiment, state);
            state.HasGoal = false;
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

        bool externalGoal =
            !state.HasGoal ||
            PlanarDistance(rawDestination, state.LastSteeringTarget) > 0.80f;

        if (externalGoal)
        {
            Vector3 safeGoal;
            if (!TryFindNearestValidDestination(rawDestination, regiment.Formation, out safeGoal))
            {
                safeGoal = regiment.transform.position;
                safeGoal.y = 0f;
            }

            if (PlanarDistance(rawDestination, safeGoal) > 0.50f &&
                PlanarDistance(safeGoal, state.LastAdjustedGoal) > 0.50f)
            {
                Debug.Log(string.Format(
                    "NAV-DIAG|Unit={0}|GoalAdjusted=True|Requested=({1:0.0},{2:0.0})|Safe=({3:0.0},{4:0.0})|Reason=BlockedDestination",
                    regiment.RegimentName,
                    rawDestination.x,
                    rawDestination.z,
                    safeGoal.x,
                    safeGoal.z));
                state.LastAdjustedGoal = safeGoal;
            }

            state.Goal = safeGoal;
            state.HasGoal = true;
        }

        Vector3 here = regiment.transform.position;
        here.y = 0f;

        bool needsBridge = SegmentCrossesRiver(here, state.Goal, GetClearance(regiment.Formation));
        if (needsBridge)
        {
            if (!state.BridgeRouting)
            {
                state.BridgeRouting = true;
                state.FormationBeforeBridge = regiment.Formation;
                state.FormationStored = true;
                Debug.Log("NAV-DIAG|Unit=" + regiment.RegimentName + "|RiverCrossing=BridgeRoute");
            }

            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);
        }
        else
        {
            RestoreFormationAfterBridge(regiment, state);
        }

        Vector3 steering = GetSteeringTarget(here, state.Goal, regiment.Formation);
        steering.y = PrototypeBootstrap.SampleGroundHeight(steering.x, steering.z) + 0.10f;

        destinationField.SetValue(regiment, steering);
        state.LastSteeringTarget = steering;
    }

    private void RestoreFormationAfterBridge(Regiment regiment, NavigationState state)
    {
        if (!state.BridgeRouting)
            return;

        state.BridgeRouting = false;

        if (state.FormationStored && regiment != null && !regiment.IsRouted)
            regiment.SetFormation(state.FormationBeforeBridge);

        state.FormationStored = false;
    }

    private Vector3 GetSteeringTarget(Vector3 current, Vector3 goal, RegimentFormation formation)
    {
        float clearance = GetClearance(formation);

        if (SegmentCrossesRiver(current, goal, clearance))
            return GetBridgeSteeringTarget(current, goal);

        Obstacle blocker = FindFirstBlockingObstacle(current, goal, clearance);
        if (blocker == null)
            return goal;

        Vector3 direct = goal - current;
        direct.y = 0f;
        if (direct.sqrMagnitude < 0.01f)
            return current;
        direct.Normalize();

        Vector3 radial = current - blocker.Center;
        radial.y = 0f;
        if (radial.sqrMagnitude < 0.01f)
            radial = Vector3.Cross(Vector3.up, direct);
        radial.Normalize();

        Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized;
        float detourRadius = blocker.Radius + clearance + 2.5f;

        Vector3 left = blocker.Center + tangent * detourRadius;
        Vector3 right = blocker.Center - tangent * detourRadius;
        left.y = 0f;
        right.y = 0f;

        bool leftValid = IsPointNavigable(left, clearance);
        bool rightValid = IsPointNavigable(right, clearance);

        if (leftValid && rightValid)
        {
            float leftCost = PlanarDistance(current, left) + PlanarDistance(left, goal);
            float rightCost = PlanarDistance(current, right) + PlanarDistance(right, goal);
            return leftCost <= rightCost ? left : right;
        }

        if (leftValid)
            return left;
        if (rightValid)
            return right;

        return current;
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

    public static bool TryFindNearestValidDestination(
        Vector3 requested,
        RegimentFormation formation,
        out Vector3 valid)
    {
        requested.x = Mathf.Clamp(requested.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        requested.z = Mathf.Clamp(requested.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        requested.y = 0f;

        PrototypeBattlefieldNavigationManager manager = Instance;
        if (manager == null)
        {
            valid = requested;
            return true;
        }

        float clearance = GetClearance(formation);
        if (manager.IsPointNavigable(requested, clearance))
        {
            valid = requested;
            return true;
        }

        for (float radius = 4f; radius <= 34f; radius += 4f)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2f;
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

    public static bool IsBlockedDestination(Vector3 point, RegimentFormation formation)
    {
        if (Instance == null)
            return false;

        return !Instance.IsPointNavigable(point, GetClearance(formation));
    }

    private bool IsPointNavigable(Vector3 point, float clearance)
    {
        if (Mathf.Abs(point.x) > BattlefieldHalfWidth || Mathf.Abs(point.z) > BattlefieldHalfDepth)
            return false;

        if (IsRiverWater(point, clearance) && !IsBridgeZone(point))
            return false;

        foreach (Obstacle obstacle in obstacles)
        {
            if (PlanarDistance(point, obstacle.Center) < obstacle.Radius + clearance)
                return false;
        }

        return true;
    }

    private Obstacle FindFirstBlockingObstacle(Vector3 a, Vector3 b, float clearance)
    {
        Obstacle best = null;
        float bestAlong = float.PositiveInfinity;

        foreach (Obstacle obstacle in obstacles)
        {
            float distance = DistancePointToSegment(obstacle.Center, a, b, out float along01);
            if (distance >= obstacle.Radius + clearance)
                continue;

            if (along01 < bestAlong)
            {
                bestAlong = along01;
                best = obstacle;
            }
        }

        return best;
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

    private static float GetClearance(RegimentFormation formation)
    {
        return formation == RegimentFormation.Line ? 6.5f : 3.2f;
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

    private static bool SegmentCrossesRiver(Vector3 a, Vector3 b, float clearance)
    {
        const int samples = 32;
        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(a, b, t);
            if (IsRiverWater(p, clearance) && !IsBridgeZone(p))
                return true;
        }

        return false;
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
                if (!TryFindNearestValidDestination(requested, RegimentFormation.Line, out safe))
                    continue;

                if (PlanarDistance(requested, safe) <= 0.50f)
                    continue;

                safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
                waypoints[i] = safe;

                Debug.Log(string.Format(
                    "NAV-DIAG|Unit={0}|RouteWaypointAdjusted=True|Index={1}|Reason=ObstacleOrWater",
                    regiment.RegimentName,
                    i));
            }
        }
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
