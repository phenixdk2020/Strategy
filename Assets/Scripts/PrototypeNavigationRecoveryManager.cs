using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(1100)]
public sealed class PrototypeNavigationRecoveryManager : MonoBehaviour
{
    private sealed class Obstacle
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
    }

    public static PrototypeNavigationRecoveryManager Instance { get; private set; }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly Dictionary<Regiment, Vector3> lastSafePositions =
        new Dictionary<Regiment, Vector3>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool installed;

    private const float BattlefieldHalfWidth = 176f;
    private const float BattlefieldHalfDepth = 116f;
    private const float RiverHalfWidth = 2.10f;
    private const float BridgeZ = 22.0f;
    private const float BridgeHalfLengthX = 8.0f;
    private const float BridgeHalfWidthZ = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationRecoveryManager_v009");
        root.AddComponent<PrototypeNavigationRecoveryManager>();
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

        if (!installed || BattleManager.Instance == null)
            return;

        foreach (Regiment regiment in BattleManager.Instance.Regiments)
        {
            if (regiment == null)
                continue;

            RecoverIfInsideBlockedTerrain(regiment);
            ReinforceCurrentSteering(regiment);
        }
    }

    private void TryInstall()
    {
        if (BattleManager.Instance == null || PrototypeBattlefieldNavigationManager.Instance == null)
            return;

        RefreshObstacles();
        installed = true;

        Debug.Log(string.Format(
            "NAV-RECOVERY|Installed=True|Obstacles={0}|Purpose=SpawnUnstick+PathSafety+AttackSlotQueries",
            obstacles.Count));
    }

    private void RefreshObstacles()
    {
        obstacles.Clear();

        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>();
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

    private void RecoverIfInsideBlockedTerrain(Regiment regiment)
    {
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        bool blocked = PrototypeBattlefieldNavigationManager.IsBlockedDestination(
            current,
            regiment.Formation);

        if (!blocked)
        {
            lastSafePositions[regiment] = current;
            return;
        }

        Vector3 safe;
        bool haveSafe =
            lastSafePositions.TryGetValue(regiment, out safe) &&
            !PrototypeBattlefieldNavigationManager.IsBlockedDestination(safe, regiment.Formation);

        if (!haveSafe)
        {
            haveSafe = PrototypeBattlefieldNavigationManager.TryFindNearestValidDestination(
                current,
                regiment.Formation,
                out safe);
        }

        if (!haveSafe)
            return;

        safe.x = Mathf.Clamp(safe.x, -BattlefieldHalfWidth, BattlefieldHalfWidth);
        safe.z = Mathf.Clamp(safe.z, -BattlefieldHalfDepth, BattlefieldHalfDepth);
        safe.y = PrototypeBootstrap.SampleGroundHeight(safe.x, safe.z) + 0.10f;

        if (PlanarDistance(regiment.transform.position, safe) < 0.20f)
            return;

        regiment.transform.position = safe;
        lastSafePositions[regiment] = safe;

        Debug.Log(string.Format(
            "NAV-RECOVERY|Unit={0}|EmergencyUnstick=True|Safe=({1:0.0},{2:0.0})|Reason=FormationInsideBlockedTerrain",
            regiment.RegimentName,
            safe.x,
            safe.z));
    }

    private void ReinforceCurrentSteering(Regiment regiment)
    {
        if (destinationField == null || hasDestinationField == null)
            return;

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
            return;

        Vector3 current = regiment.transform.position;
        Vector3 target = (Vector3)destinationField.GetValue(regiment);
        current.y = 0f;
        target.y = 0f;

        float clearance = GetClearance(regiment.Formation);
        Obstacle blocker = FindFirstBlockingObstacle(current, target, clearance);
        if (blocker == null)
            return;

        Vector3 detour;
        if (!TryFindBestDetour(current, target, blocker, clearance, out detour))
            return;

        detour.y = PrototypeBootstrap.SampleGroundHeight(detour.x, detour.z) + 0.10f;
        destinationField.SetValue(regiment, detour);
    }

    private bool TryFindBestDetour(
        Vector3 current,
        Vector3 goal,
        Obstacle blocker,
        float clearance,
        out Vector3 detour)
    {
        float radius = blocker.Radius + clearance + 2.5f;
        float bestCost = float.PositiveInfinity;
        Vector3 best = current;
        bool found = false;

        for (int i = 0; i < 24; i++)
        {
            float angle = i / 24f * Mathf.PI * 2f;
            Vector3 candidate = blocker.Center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            if (!IsPointNavigableForRecovery(candidate, clearance))
                continue;

            float cost =
                PlanarDistance(current, candidate) +
                PlanarDistance(candidate, goal);

            cost += EstimatePathPenaltyInternal(current, candidate, clearance) * 0.45f;
            cost += EstimatePathPenaltyInternal(candidate, goal, clearance) * 0.20f;

            if (cost >= bestCost)
                continue;

            bestCost = cost;
            best = candidate;
            found = true;
        }

        detour = best;
        return found;
    }

    public static float EstimatePathPenalty(
        Vector3 start,
        Vector3 end,
        RegimentFormation formation)
    {
        if (Instance == null)
            return 0f;

        return Instance.EstimatePathPenaltyInternal(
            start,
            end,
            GetClearance(formation));
    }

    private float EstimatePathPenaltyInternal(Vector3 start, Vector3 end, float clearance)
    {
        float penalty = 0f;

        foreach (Obstacle obstacle in obstacles)
        {
            float along;
            float distance = DistancePointToSegment(obstacle.Center, start, end, out along);
            float required = obstacle.Radius + clearance;

            if (distance >= required)
                continue;

            float depth = required - distance;
            float weight =
                obstacle.Name == "Farmhouse" || obstacle.Name == "Barn"
                    ? 190f
                    : obstacle.Name == "Tree"
                        ? 85f
                        : 35f;

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

    private Obstacle FindFirstBlockingObstacle(Vector3 a, Vector3 b, float clearance)
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

    private bool IsPointNavigableForRecovery(Vector3 point, float clearance)
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

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
