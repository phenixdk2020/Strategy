using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09 TEST navigation replacement.
// V4 replaces the tangent/one-obstacle steering with a persistent A* route over
// the tactical battlefield. The important invariant is that the regiment keeps
// its ORIGINAL player/AI goal while this layer owns only the short-term steering
// point. Static obstacles and the river are therefore solved as a route problem
// rather than as repeated local reactions when a formation is already touching a
// tree or building.
[DefaultExecutionOrder(1600)]
public sealed class PrototypeBattlefieldNavigationV4 : MonoBehaviour
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
        public readonly List<Vector3> Path = new List<Vector3>();
        public int PathIndex;
        public Vector3 LastPosition;
        public float StuckTimer;
        public bool FormationStored;
        public RegimentFormation FormationBeforePath;
        public bool ForcedColumn;
        public float LastPlanTime;
    }

    public static PrototypeBattlefieldNavigationV4 Instance { get; private set; }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, NavigationState> states =
        new Dictionary<Regiment, NavigationState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool installed;
    private bool olderLayersDisabled;

    private const float BattlefieldHalfWidth = 176f;
    private const float BattlefieldHalfDepth = 116f;

    private const float RiverHalfWidth = 2.10f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 8.0f;
    private const float BridgeHalfWidthZ = 4.0f;

    // Movement is planned for a compact marching footprint. The final destination
    // is still validated for LINE, so a regiment may march through a gap in COLUMN
    // but cannot finish deployed on top of a tree/building.
    private const float TravelClearance = 3.8f;
    private const float LineDestinationClearance = 9.0f;

    private const float CellSize = 4.0f;
    private const int GridWidth = 89;   // -176 .. +176 inclusive
    private const int GridHeight = 59;  // -116 .. +116 inclusive
    private const float PathPointArrival = 2.2f;
    private const float ReplanStuckSeconds = 0.85f;
    private const float GoalChangeTolerance = 0.80f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV4>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattlefieldNavigationV4_v009");
        root.AddComponent<PrototypeBattlefieldNavigationV4>();
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
            Debug.LogError("NAV-V4|Init=Failed|Reason=Regiment movement fields not found");
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

        DisableOlderActiveNavigation();

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
        installed = true;

        Debug.Log(string.Format(
            "NAV-V4|Installed=True|Mode=AStar|Grid={0}x{1}|Cell={2:0.0}|Obstacles={3}|River=BridgeOnly",
            GridWidth,
            GridHeight,
            CellSize,
            obstacles.Count));
    }

    private void DisableOlderActiveNavigation()
    {
        if (olderLayersDisabled)
            return;

        PrototypeBattlefieldNavigationV3 v3 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>();
        if (v3 != null)
            v3.enabled = false;

        PrototypeBattlefieldNavigationManager v1 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (v1 != null)
            v1.enabled = false;

        PrototypeNavigationRecoveryManager recovery =
            Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (recovery != null)
            recovery.enabled = false;

        olderLayersDisabled = true;
        Debug.Log("NAV-V4|OlderActiveNavigationDisabled=True");
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
                    // Trunk/near-crown tactical footprint. TravelClearance is added
                    // separately, so the regiment centre stays well outside it.
                    AddObstacle("Tree", item.position, 2.6f);
                    break;
                case "Farmhouse":
                    AddObstacle("Farmhouse", item.position, 6.5f);
                    break;
                case "Barn":
                    AddObstacle("Barn", item.position, 4.5f);
                    break;
                case "FencePost":
                    AddObstacle("FencePost", item.position, 0.55f);
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

    private void ApplyNavigation(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out NavigationState state))
        {
            state = new NavigationState
            {
                LastPosition = regiment.transform.position
            };
            states[regiment] = state;
        }

        RecoverIfInsideBlockedSpace(regiment, state);

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            state.HasGoal = false;
            state.Path.Clear();
            state.PathIndex = 0;
            state.StuckTimer = 0f;
            RestoreFormation(regiment, state);
            state.LastPosition = regiment.transform.position;
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;

        bool rawIsOurSteering =
            state.HasGoal &&
            PlanarDistance(rawDestination, state.LastSteeringTarget) <= GoalChangeTolerance;

        bool rawIsKnownGoal =
            state.HasGoal &&
            PlanarDistance(rawDestination, state.Goal) <= GoalChangeTolerance;

        bool newExternalGoal =
            !state.HasGoal ||
            (!rawIsOurSteering && !rawIsKnownGoal);

        if (newExternalGoal)
        {
            Vector3 safeGoal;
            if (!TryFindNearestValidDestination(
                rawDestination,
                LineDestinationClearance,
                out safeGoal))
            {
                safeGoal = rawDestination;
            }

            state.Goal = safeGoal;
            state.HasGoal = true;
            state.StuckTimer = 0f;

            PlanPath(regiment, state, "NewGoal");
        }
        else if (state.Path.Count == 0)
        {
            PlanPath(regiment, state, "MissingPath");
        }

        UpdateStuckDetection(regiment, state);

        if (state.Path.Count == 0)
        {
            // A failed route must never silently collapse into "stand still in the
            // tree". Try one explicit escape point and plan again from there.
            if (TryEmergencyEscape(regiment, state, state.Goal))
                PlanPath(regiment, state, "EmergencyEscape");

            if (state.Path.Count == 0)
            {
                Debug.Log(string.Format(
                    "NAV-V4|Unit={0}|Path=False|Goal=({1:0.0},{2:0.0})|Reason=NoRoute",
                    regiment.RegimentName,
                    state.Goal.x,
                    state.Goal.z));
                return;
            }
        }

        AdvancePathIndex(regiment, state);

        if (state.PathIndex < 0 || state.PathIndex >= state.Path.Count)
        {
            SetSteering(regiment, state, state.Goal);
            return;
        }

        bool hasIntermediatePath = state.Path.Count > 1;
        if (hasIntermediatePath)
            ForceColumn(regiment, state);

        Vector3 steering = state.Path[state.PathIndex];
        SetSteering(regiment, state, steering);
    }

    private void UpdateStuckDetection(Regiment regiment, NavigationState state)
    {
        Vector3 current = regiment.transform.position;
        float moved = PlanarDistance(current, state.LastPosition);
        state.LastPosition = current;

        if (Time.deltaTime <= 0f)
            return;

        if (moved < 0.025f &&
            state.HasGoal &&
            PlanarDistance(current, state.Goal) > 3.5f)
        {
            state.StuckTimer += Time.deltaTime;
        }
        else
        {
            state.StuckTimer = 0f;
        }

        if (state.StuckTimer < ReplanStuckSeconds)
            return;

        state.StuckTimer = 0f;
        Debug.Log("NAV-V4|Unit=" + regiment.RegimentName + "|Stuck=True|Action=Replan");

        RecoverIfInsideBlockedSpace(regiment, state);
        PlanPath(regiment, state, "StuckReplan");
    }

    private void PlanPath(Regiment regiment, NavigationState state, string reason)
    {
        state.Path.Clear();
        state.PathIndex = 0;
        state.LastPlanTime = Time.time;

        Vector3 start = regiment.transform.position;
        start.y = 0f;
        Vector3 goal = state.Goal;
        goal.y = 0f;

        if (SegmentWalkable(start, goal, TravelClearance))
        {
            state.Path.Add(goal);
            RestoreFormation(regiment, state);

            Debug.Log(string.Format(
                "NAV-V4|Unit={0}|Path=True|Type=Direct|Points=1|Reason={1}",
                regiment.RegimentName,
                reason));
            return;
        }

        List<Vector3> rawPath;
        if (!TryBuildAStarPath(start, goal, out rawPath))
        {
            Debug.Log(string.Format(
                "NAV-V4|Unit={0}|Path=False|Type=AStar|Reason={1}",
                regiment.RegimentName,
                reason));
            return;
        }

        List<Vector3> simplified = SimplifyPath(start, rawPath, goal);
        for (int i = 0; i < simplified.Count; i++)
            state.Path.Add(simplified[i]);

        if (state.Path.Count == 0)
            state.Path.Add(goal);

        ForceColumn(regiment, state);

        Debug.Log(string.Format(
            "NAV-V4|Unit={0}|Path=True|Type=AStar|RawPoints={1}|Points={2}|Reason={3}",
            regiment.RegimentName,
            rawPath.Count,
            state.Path.Count,
            reason));
    }

    private void AdvancePathIndex(Regiment regiment, NavigationState state)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        while (state.PathIndex < state.Path.Count)
        {
            Vector3 point = state.Path[state.PathIndex];
            point.y = 0f;

            if (PlanarDistance(current, point) > PathPointArrival)
                break;

            state.PathIndex++;
        }

        // If the remaining route has become line-clear after the last obstacle,
        // collapse it to the original goal. This prevents unnecessary zig-zagging.
        if (state.PathIndex < state.Path.Count - 1 &&
            SegmentWalkable(current, state.Goal, TravelClearance))
        {
            state.Path.Clear();
            state.Path.Add(state.Goal);
            state.PathIndex = 0;
        }

        if (state.PathIndex >= state.Path.Count)
        {
            state.Path.Clear();
            state.Path.Add(state.Goal);
            state.PathIndex = 0;
        }
    }

    private bool TryBuildAStarPath(
        Vector3 start,
        Vector3 goal,
        out List<Vector3> path)
    {
        path = new List<Vector3>();

        int startIndex;
        int goalIndex;
        if (!TryFindNearestGridNode(start, true, out startIndex) ||
            !TryFindNearestGridNode(goal, false, out goalIndex))
        {
            return false;
        }

        int nodeCount = GridWidth * GridHeight;
        float[] gScore = new float[nodeCount];
        float[] fScore = new float[nodeCount];
        int[] cameFrom = new int[nodeCount];
        bool[] closed = new bool[nodeCount];
        bool[] inOpen = new bool[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            gScore[i] = float.PositiveInfinity;
            fScore[i] = float.PositiveInfinity;
            cameFrom[i] = -1;
        }

        List<int> open = new List<int>();
        gScore[startIndex] = 0f;
        fScore[startIndex] = Heuristic(startIndex, goalIndex);
        open.Add(startIndex);
        inOpen[startIndex] = true;

        int iterations = 0;
        int maxIterations = nodeCount * 8;

        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int bestOpenPosition = 0;
            int current = open[0];
            float bestF = fScore[current];

            for (int i = 1; i < open.Count; i++)
            {
                int candidate = open[i];
                if (fScore[candidate] < bestF)
                {
                    bestF = fScore[candidate];
                    current = candidate;
                    bestOpenPosition = i;
                }
            }

            open.RemoveAt(bestOpenPosition);
            inOpen[current] = false;

            if (current == goalIndex)
            {
                ReconstructPath(cameFrom, current, path);
                return path.Count > 0;
            }

            closed[current] = true;

            int cx = current % GridWidth;
            int cz = current / GridWidth;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    int nx = cx + dx;
                    int nz = cz + dz;
                    if (nx < 0 || nx >= GridWidth || nz < 0 || nz >= GridHeight)
                        continue;

                    int neighbor = nz * GridWidth + nx;
                    if (closed[neighbor] || !GridNodeWalkable(neighbor))
                        continue;

                    // Do not cut diagonally through the corner of an obstacle.
                    if (dx != 0 && dz != 0)
                    {
                        int orthogonalA = cz * GridWidth + nx;
                        int orthogonalB = nz * GridWidth + cx;
                        if (!GridNodeWalkable(orthogonalA) || !GridNodeWalkable(orthogonalB))
                            continue;
                    }

                    float stepCost = (dx != 0 && dz != 0) ? 1.41421356f : 1f;
                    float tentative = gScore[current] + stepCost;

                    if (tentative >= gScore[neighbor])
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goalIndex);

                    if (!inOpen[neighbor])
                    {
                        open.Add(neighbor);
                        inOpen[neighbor] = true;
                    }
                }
            }
        }

        return false;
    }

    private void ReconstructPath(
        int[] cameFrom,
        int current,
        List<Vector3> path)
    {
        List<Vector3> reverse = new List<Vector3>();
        reverse.Add(GridPosition(current));

        while (cameFrom[current] >= 0)
        {
            current = cameFrom[current];
            reverse.Add(GridPosition(current));
        }

        reverse.Reverse();

        // The first grid point is only an anchor near the regiment; steering to it
        // can cause a needless step backwards, so skip it when possible.
        int start = reverse.Count > 1 ? 1 : 0;
        for (int i = start; i < reverse.Count; i++)
            path.Add(reverse[i]);
    }

    private List<Vector3> SimplifyPath(
        Vector3 start,
        List<Vector3> rawPath,
        Vector3 exactGoal)
    {
        List<Vector3> all = new List<Vector3>();
        all.Add(start);
        for (int i = 0; i < rawPath.Count; i++)
            all.Add(rawPath[i]);
        all.Add(exactGoal);

        List<Vector3> result = new List<Vector3>();
        int anchor = 0;

        while (anchor < all.Count - 1)
        {
            int farthest = anchor + 1;

            for (int candidate = all.Count - 1; candidate > anchor; candidate--)
            {
                if (!SegmentWalkable(all[anchor], all[candidate], TravelClearance))
                    continue;

                farthest = candidate;
                break;
            }

            result.Add(all[farthest]);
            anchor = farthest;
        }

        return result;
    }

    private bool TryFindNearestGridNode(
        Vector3 point,
        bool requireReachableFromExactPoint,
        out int index)
    {
        index = -1;
        float best = float.PositiveInfinity;

        int centerX = Mathf.Clamp(
            Mathf.RoundToInt((point.x + BattlefieldHalfWidth) / CellSize),
            0,
            GridWidth - 1);

        int centerZ = Mathf.Clamp(
            Mathf.RoundToInt((point.z + BattlefieldHalfDepth) / CellSize),
            0,
            GridHeight - 1);

        for (int radius = 0; radius <= 8; radius++)
        {
            bool foundAtRadius = false;

            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (z < 0 || z >= GridHeight)
                    continue;

                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= GridWidth)
                        continue;

                    if (radius > 0 &&
                        x > centerX - radius && x < centerX + radius &&
                        z > centerZ - radius && z < centerZ + radius)
                    {
                        continue;
                    }

                    int candidate = z * GridWidth + x;
                    if (!GridNodeWalkable(candidate))
                        continue;

                    Vector3 world = GridPosition(candidate);
                    if (requireReachableFromExactPoint &&
                        !SegmentWalkable(point, world, TravelClearance))
                    {
                        continue;
                    }

                    float distance = PlanarDistance(point, world);
                    if (distance >= best)
                        continue;

                    best = distance;
                    index = candidate;
                    foundAtRadius = true;
                }
            }

            if (foundAtRadius)
                return true;
        }

        return index >= 0;
    }

    private bool GridNodeWalkable(int index)
    {
        return IsPointWalkable(GridPosition(index), TravelClearance);
    }

    private static Vector3 GridPosition(int index)
    {
        int x = index % GridWidth;
        int z = index / GridWidth;

        return new Vector3(
            -BattlefieldHalfWidth + x * CellSize,
            0f,
            -BattlefieldHalfDepth + z * CellSize);
    }

    private static float Heuristic(int a, int b)
    {
        int ax = a % GridWidth;
        int az = a / GridWidth;
        int bx = b % GridWidth;
        int bz = b / GridWidth;

        float dx = ax - bx;
        float dz = az - bz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private bool SegmentWalkable(Vector3 a, Vector3 b, float clearance)
    {
        a.y = 0f;
        b.y = 0f;

        if (!IsPointWalkable(a, clearance) || !IsPointWalkable(b, clearance))
            return false;

        foreach (Obstacle obstacle in obstacles)
        {
            float distance = DistancePointToSegment(obstacle.Center, a, b);
            if (distance < obstacle.Radius + clearance)
                return false;
        }

        float length = PlanarDistance(a, b);
        int samples = Mathf.Max(2, Mathf.CeilToInt(length / 1.6f));

        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(a, b, t);

            if (IsRiverWater(p, clearance) && !IsBridgeZone(p))
                return false;
        }

        return true;
    }

    private bool IsPointWalkable(Vector3 point, float clearance)
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

    private void RecoverIfInsideBlockedSpace(Regiment regiment, NavigationState state)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        if (IsPointWalkable(current, TravelClearance))
            return;

        Vector3 safe;
        if (!TryFindNearestValidDestination(current, TravelClearance, out safe))
            return;

        if (PlanarDistance(current, safe) <= 0.10f)
            return;

        safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
        regiment.transform.position = safe;
        state.LastPosition = safe;
        state.Path.Clear();
        state.PathIndex = 0;

        Debug.Log(string.Format(
            "NAV-V4|Unit={0}|EmergencyUnstick=True|Safe=({1:0.0},{2:0.0})|Reason=InsideBlockedSpace",
            regiment.RegimentName,
            safe.x,
            safe.z));
    }

    private bool TryEmergencyEscape(
        Regiment regiment,
        NavigationState state,
        Vector3 goal)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        Vector3 best = current;
        float bestCost = float.PositiveInfinity;
        bool found = false;

        for (float radius = 2f; radius <= 18f; radius += 2f)
        {
            for (int i = 0; i < 48; i++)
            {
                float angle = i / 48f * Mathf.PI * 2f;
                Vector3 candidate = current + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

                if (!IsPointWalkable(candidate, TravelClearance))
                    continue;

                // For emergency recovery we allow the exact current point to be
                // invalid, but the candidate itself must be safe. Bias toward the
                // original destination so recovery does not send the unit backwards
                // unless that is the only escape.
                float cost = radius + PlanarDistance(candidate, goal) * 0.08f;
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
            return false;

        best.y = PrototypeBootstrap.SampleGroundHeight(best.x, best.z) + 0.10f;
        regiment.transform.position = best;
        state.LastPosition = best;

        Debug.Log(string.Format(
            "NAV-V4|Unit={0}|EmergencyEscape=True|Safe=({1:0.0},{2:0.0})",
            regiment.RegimentName,
            best.x,
            best.z));

        return true;
    }

    private bool TryFindNearestValidDestination(
        Vector3 requested,
        float clearance,
        out Vector3 valid)
    {
        requested.x = Mathf.Clamp(requested.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        requested.z = Mathf.Clamp(requested.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        requested.y = 0f;

        if (IsPointWalkable(requested, clearance))
        {
            valid = requested;
            return true;
        }

        for (float radius = 1.5f; radius <= 44f; radius += 1.5f)
        {
            Vector3 best = requested;
            float bestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < 48; i++)
            {
                float angle = i / 48f * Mathf.PI * 2f;
                Vector3 candidate = requested + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

                candidate.x = Mathf.Clamp(candidate.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
                candidate.z = Mathf.Clamp(candidate.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);

                if (!IsPointWalkable(candidate, clearance))
                    continue;

                float distance = PlanarDistance(requested, candidate);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = candidate;
                found = true;
            }

            if (found)
            {
                valid = best;
                return true;
            }
        }

        valid = requested;
        return false;
    }

    private void ForceColumn(Regiment regiment, NavigationState state)
    {
        if (!state.FormationStored)
        {
            state.FormationBeforePath = regiment.Formation;
            state.FormationStored = true;
        }

        if (regiment.Formation != RegimentFormation.Column)
        {
            regiment.SetFormation(RegimentFormation.Column);
            state.ForcedColumn = true;
        }
    }

    private void RestoreFormation(Regiment regiment, NavigationState state)
    {
        if (!state.FormationStored)
            return;

        if (regiment != null && !regiment.IsRouted)
            regiment.SetFormation(state.FormationBeforePath);

        state.FormationStored = false;
        state.ForcedColumn = false;
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

    private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        point.y = 0f;
        a.y = 0f;
        b.y = 0f;

        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 0.001f)
            return Vector3.Distance(point, a);

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / denom);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(point, closest);
    }

    private static float StreamCenterX(float z)
    {
        return Mathf.Sin(z * 0.065f) * 4.8f;
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

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}