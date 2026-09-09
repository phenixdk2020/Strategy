using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09a TEST hotfix.
// Keeps Navigation V4 as the route planner, but adds formation-footprint avoidance
// and deterministic recovery so broad LINE formations do not snag on trees that the
// centre-point A* route technically clears.
[DefaultExecutionOrder(1750)]
public sealed class PrototypeNavigation09AHotfix : MonoBehaviour
{
    private sealed class Obstacle
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
    }

    private sealed class UnitState
    {
        public bool Initialized;
        public bool OwnsColumn;
        public RegimentFormation PreviousFormation;
        public float ClearTimer;
        public Vector3 LastPosition;
        public float StuckTimer;
    }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, UnitState> states =
        new Dictionary<Regiment, UnitState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool installed;

    // Formation footprint clearances are intentionally larger than the centre-point
    // route clearance in V4. This is the missing layer that caused edge soldiers to
    // touch a tree even when the regiment centre had a legal route.
    private const float LineFootprintClearance = 10.8f;
    private const float ColumnFootprintClearance = 5.2f;
    private const float LookAheadDistance = 22f;
    private const float ClearToRestoreSeconds = 0.75f;
    private const float StuckSeconds = 0.60f;
    private const float AvoidanceSpeed = 7.5f;

    private const float BattlefieldHalfWidth = 176f;
    private const float BattlefieldHalfDepth = 116f;
    private const float RiverHalfWidth = 2.10f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 8.0f;
    private const float BridgeHalfWidthZ = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigation09AHotfix>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigation09AHotfix_v000009a");
        root.AddComponent<PrototypeNavigation09AHotfix>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("NAV-09A|Init=Failed|Reason=Regiment movement fields not found");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!installed)
            Install();

        if (!installed || BattleManager.Instance == null)
            return;

        foreach (Regiment regiment in BattleManager.Instance.Regiments)
        {
            if (regiment == null)
                continue;

            if (!states.TryGetValue(regiment, out UnitState state))
            {
                state = new UnitState
                {
                    LastPosition = regiment.transform.position
                };
                states[regiment] = state;
            }

            HandleRegiment(regiment, state);
        }
    }

    private void Install()
    {
        if (BattleManager.Instance == null)
            return;

        RefreshObstacles();
        installed = true;

        Debug.Log(string.Format(
            "NAV-09A|Installed=True|Obstacles={0}|LineFootprint={1:0.0}|ColumnFootprint={2:0.0}|Purpose=AntiTreeSnag",
            obstacles.Count,
            LineFootprintClearance,
            ColumnFootprintClearance));
    }

    private void RefreshObstacles()
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
                    AddObstacle("Tree", item.position, 2.8f);
                    break;
                case "Farmhouse":
                    AddObstacle("Farmhouse", item.position, 6.5f);
                    break;
                case "Barn":
                    AddObstacle("Barn", item.position, 4.5f);
                    break;
                case "FencePost":
                    AddObstacle("FencePost", item.position, 0.60f);
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

    private void HandleRegiment(Regiment regiment, UnitState state)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        if (!state.Initialized)
        {
            state.Initialized = true;
            ResolveInitialFormationOverlap(regiment, state, current);
            current = regiment.transform.position;
            current.y = 0f;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        Vector3 steeringTarget = current;
        if (hasDestination)
        {
            steeringTarget = (Vector3)destinationField.GetValue(regiment);
            steeringTarget.y = 0f;
        }

        Obstacle lineOverlap = FindClosestOverlap(current, LineFootprintClearance);
        Obstacle columnOverlap = FindClosestOverlap(current, ColumnFootprintClearance);

        if (regiment.Formation == RegimentFormation.Line && lineOverlap != null)
            ForceOwnColumn(regiment, state, "CurrentFootprintTouchesObstacle", lineOverlap);

        if (columnOverlap != null)
        {
            ApplyAvoidanceStep(regiment, columnOverlap, steeringTarget, hasDestination, true);
        }
        else if (hasDestination)
        {
            Vector3 lookTarget = LimitLookAhead(current, steeringTarget);

            Obstacle lineThreat = FindFirstThreat(current, lookTarget, LineFootprintClearance);
            if (regiment.Formation == RegimentFormation.Line && lineThreat != null)
                ForceOwnColumn(regiment, state, "LinePathThreat", lineThreat);

            Obstacle compactThreat = FindFirstThreat(current, lookTarget, ColumnFootprintClearance);
            if (compactThreat != null)
                ApplyAvoidanceStep(regiment, compactThreat, steeringTarget, true, false);
        }

        UpdateStuckRecovery(regiment, state, hasDestination, steeringTarget);
        UpdateFormationRestore(regiment, state, hasDestination, steeringTarget);
    }

    private void ResolveInitialFormationOverlap(
        Regiment regiment,
        UnitState state,
        Vector3 current)
    {
        float initialClearance = regiment.Formation == RegimentFormation.Line
            ? LineFootprintClearance
            : ColumnFootprintClearance;

        Obstacle obstacle = FindClosestOverlap(current, initialClearance);
        if (obstacle == null)
            return;

        Vector3 safe;
        if (!TryFindSafePointAroundObstacle(
            current,
            obstacle,
            initialClearance + 0.8f,
            current,
            false,
            out safe))
        {
            return;
        }

        safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
        regiment.transform.position = safe;
        state.LastPosition = safe;

        Debug.Log(string.Format(
            "NAV-09A|Unit={0}|SpawnAdjusted=True|Obstacle={1}|Safe=({2:0.0},{3:0.0})",
            regiment.RegimentName,
            obstacle.Name,
            safe.x,
            safe.z));
    }

    private void ForceOwnColumn(
        Regiment regiment,
        UnitState state,
        string reason,
        Obstacle obstacle)
    {
        if (!state.OwnsColumn)
        {
            state.PreviousFormation = regiment.Formation;
            state.OwnsColumn = true;
            state.ClearTimer = 0f;
        }

        if (regiment.Formation != RegimentFormation.Column)
            regiment.SetFormation(RegimentFormation.Column);

        Debug.Log(string.Format(
            "NAV-09A|Unit={0}|Formation=COLUMN|Reason={1}|Obstacle={2}",
            regiment.RegimentName,
            reason,
            obstacle != null ? obstacle.Name : "Unknown"));
    }

    private void ApplyAvoidanceStep(
        Regiment regiment,
        Obstacle obstacle,
        Vector3 steeringTarget,
        bool hasDestination,
        bool strong)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        Vector3 toTarget = steeringTarget - current;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
            toTarget = regiment.transform.forward;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
            toTarget = Vector3.forward;
        toTarget.Normalize();

        Vector3 away = current - obstacle.Center;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.Cross(Vector3.up, toTarget);
        away.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, toTarget).normalized;
        float sideSign = Mathf.Sign(Vector3.Dot(away, side));
        if (Mathf.Abs(sideSign) < 0.01f)
            sideSign = StableSide(regiment.RegimentName);
        side *= sideSign;

        float required = obstacle.Radius + ColumnFootprintClearance;
        float distance = PlanarDistance(current, obstacle.Center);
        float penetration = Mathf.Max(0f, required - distance);

        float outwardWeight = strong ? 1.65f : 1.10f;
        float sideWeight = strong ? 0.55f : 1.05f;
        Vector3 avoidance = (away * outwardWeight + side * sideWeight).normalized;

        float speed = AvoidanceSpeed * (strong ? 1.35f : 1f);
        float extra = Mathf.Min(2.0f, penetration * 0.45f);
        Vector3 next = current + avoidance * (speed + extra) * Time.deltaTime;

        next.x = Mathf.Clamp(next.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        next.z = Mathf.Clamp(next.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
        regiment.transform.position = next;

        if (strong)
        {
            Debug.Log(string.Format(
                "NAV-09A|Unit={0}|Avoidance=PushOut|Obstacle={1}|Distance={2:0.0}|Required={3:0.0}",
                regiment.RegimentName,
                obstacle.Name,
                distance,
                required));
        }
    }

    private void UpdateStuckRecovery(
        Regiment regiment,
        UnitState state,
        bool hasDestination,
        Vector3 steeringTarget)
    {
        Vector3 current = regiment.transform.position;
        float moved = PlanarDistance(current, state.LastPosition);
        state.LastPosition = current;

        if (!hasDestination || Time.deltaTime <= 0f)
        {
            state.StuckTimer = 0f;
            return;
        }

        if (moved < 0.02f && PlanarDistance(current, steeringTarget) > 3.0f)
            state.StuckTimer += Time.deltaTime;
        else
            state.StuckTimer = 0f;

        if (state.StuckTimer < StuckSeconds)
            return;

        state.StuckTimer = 0f;
        Obstacle nearest = FindNearestObstacle(current);
        if (nearest == null)
            return;

        float distance = PlanarDistance(current, nearest.Center);
        if (distance > nearest.Radius + LineFootprintClearance + 5f)
            return;

        Vector3 safe;
        if (!TryFindSafePointAroundObstacle(
            current,
            nearest,
            ColumnFootprintClearance + 1.2f,
            steeringTarget,
            true,
            out safe))
        {
            return;
        }

        if (!state.OwnsColumn && regiment.Formation == RegimentFormation.Line)
            ForceOwnColumn(regiment, state, "StuckRecovery", nearest);

        safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;
        regiment.transform.position = safe;
        state.LastPosition = safe;

        Debug.Log(string.Format(
            "NAV-09A|Unit={0}|EmergencyRelocate=True|Obstacle={1}|Safe=({2:0.0},{3:0.0})|Reason=NoProgressNearObstacle",
            regiment.RegimentName,
            nearest.Name,
            safe.x,
            safe.z));
    }

    private void UpdateFormationRestore(
        Regiment regiment,
        UnitState state,
        bool hasDestination,
        Vector3 steeringTarget)
    {
        if (!state.OwnsColumn)
            return;

        Vector3 current = regiment.transform.position;
        current.y = 0f;

        bool lineClearHere = FindClosestOverlap(current, LineFootprintClearance) == null;
        bool lineClearAhead = true;

        if (hasDestination)
        {
            Vector3 lookTarget = LimitLookAhead(current, steeringTarget);
            lineClearAhead = FindFirstThreat(current, lookTarget, LineFootprintClearance) == null;
        }

        if (lineClearHere && lineClearAhead)
            state.ClearTimer += Time.deltaTime;
        else
            state.ClearTimer = 0f;

        if (state.ClearTimer < ClearToRestoreSeconds)
            return;

        if (regiment != null && !regiment.IsRouted)
            regiment.SetFormation(state.PreviousFormation);

        state.OwnsColumn = false;
        state.ClearTimer = 0f;

        Debug.Log("NAV-09A|Unit=" + regiment.RegimentName + "|FormationRestore=" + state.PreviousFormation);
    }

    private Vector3 LimitLookAhead(Vector3 current, Vector3 target)
    {
        Vector3 delta = target - current;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (distance <= LookAheadDistance || distance < 0.01f)
            return target;

        return current + delta.normalized * LookAheadDistance;
    }

    private Obstacle FindClosestOverlap(Vector3 point, float clearance)
    {
        Obstacle best = null;
        float bestPenetration = 0f;

        foreach (Obstacle obstacle in obstacles)
        {
            float required = obstacle.Radius + clearance;
            float distance = PlanarDistance(point, obstacle.Center);
            float penetration = required - distance;
            if (penetration <= 0f)
                continue;

            if (best == null || penetration > bestPenetration)
            {
                best = obstacle;
                bestPenetration = penetration;
            }
        }

        return best;
    }

    private Obstacle FindFirstThreat(Vector3 a, Vector3 b, float clearance)
    {
        Obstacle best = null;
        float bestAlong = float.PositiveInfinity;

        foreach (Obstacle obstacle in obstacles)
        {
            float along;
            float distance = DistancePointToSegment(obstacle.Center, a, b, out along);
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

    private Obstacle FindNearestObstacle(Vector3 point)
    {
        Obstacle best = null;
        float bestDistance = float.PositiveInfinity;

        foreach (Obstacle obstacle in obstacles)
        {
            float distance = PlanarDistance(point, obstacle.Center) - obstacle.Radius;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = obstacle;
        }

        return best;
    }

    private bool TryFindSafePointAroundObstacle(
        Vector3 origin,
        Obstacle obstacle,
        float clearance,
        Vector3 preferredTarget,
        bool biasToTarget,
        out Vector3 safe)
    {
        safe = origin;
        float bestCost = float.PositiveInfinity;
        bool found = false;

        float baseRadius = obstacle.Radius + clearance;
        for (float extra = 0.8f; extra <= 14f; extra += 1.6f)
        {
            float radius = baseRadius + extra;

            for (int i = 0; i < 64; i++)
            {
                float angle = i / 64f * Mathf.PI * 2f;
                Vector3 candidate = obstacle.Center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);

                if (!IsPointSafe(candidate, clearance))
                    continue;

                float cost = PlanarDistance(origin, candidate);
                if (biasToTarget)
                    cost += PlanarDistance(candidate, preferredTarget) * 0.12f;

                if (cost >= bestCost)
                    continue;

                bestCost = cost;
                safe = candidate;
                found = true;
            }

            if (found)
                return true;
        }

        return false;
    }

    private bool IsPointSafe(Vector3 point, float clearance)
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

    private static float StableSide(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 1f;

        int sum = 0;
        for (int i = 0; i < value.Length; i++)
            sum += value[i];

        return (sum & 1) == 0 ? 1f : -1f;
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
