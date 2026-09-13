using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f11 lightweight static-obstacle routing.
// This deliberately does NOT re-enable the old V1/V3/V4 obstacle-navigation stack.
// Instead it owns only explicitly approved hard blockers (currently Farmhouse + Barn)
// and inserts a short sequence of safe corner waypoints when the current route crosses one.
[DefaultExecutionOrder(-200)]
public sealed class PrototypeStaticObstacleRouting09F11 : MonoBehaviour
{
    private sealed class Obstacle
    {
        public string Name;
        public Bounds Bounds;
    }

    private sealed class RouteState
    {
        public bool HasGoal;
        public Vector3 FinalGoal;
        public Vector3 LastSteeringTarget;
        public readonly Queue<Vector3> Waypoints = new Queue<Vector3>();
        public string ObstacleName = string.Empty;
    }

    private readonly Dictionary<Regiment, RouteState> states = new Dictionary<Regiment, RouteState>();
    private readonly List<Obstacle> obstacles = new List<Obstacle>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool scanned;

    private const float WaypointArrival = 1.35f;
    private const float CornerPad = 1.25f;
    private const float ColumnHalfWidth = 3.4f;
    private const float LineHalfWidth = 25.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeStaticObstacleRouting09F11>() != null)
            return;

        GameObject root = new GameObject("PrototypeStaticObstacleRouting_v000009f11");
        root.AddComponent<PrototypeStaticObstacleRouting09F11>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("OBSTACLE-09F11|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!scanned)
            ScanApprovedBlockers();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            // River/bridge routing has higher priority than building avoidance.
            if (PrototypeRiverBridgeOnly09F3.IsBridgeRouteActive(regiment))
                continue;

            ApplyRouting(regiment);
        }
    }

    private void ScanApprovedBlockers()
    {
        obstacles.Clear();

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string objectName = renderer.gameObject.name;
            if (objectName != "Farmhouse" && objectName != "Barn")
                continue;

            obstacles.Add(new Obstacle
            {
                Name = objectName,
                Bounds = renderer.bounds
            });
        }

        scanned = true;
        Debug.Log(
            "OBSTACLE-09F11|Installed=True|ApprovedBlockers=Farmhouse,Barn|" +
            "Found=" + obstacles.Count +
            "|Trees=False|Fences=False|Road=False|RiverOwnedSeparately=True");
    }

    private void ApplyRouting(Regiment regiment)
    {
        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        if (!hasDestination)
        {
            states.Remove(regiment);
            return;
        }

        Vector3 rawDestination = (Vector3)destinationField.GetValue(regiment);
        rawDestination.y = 0f;
        Vector3 current = regiment.transform.position;
        current.y = 0f;

        if (!states.TryGetValue(regiment, out RouteState state))
        {
            state = new RouteState();
            states[regiment] = state;
        }

        bool rawIsOwnSteering =
            state.HasGoal &&
            PlanarDistance(rawDestination, state.LastSteeringTarget) <= 1.25f;

        if (!state.HasGoal || !rawIsOwnSteering)
        {
            // A new external order replaces any previous obstacle detour.
            state.HasGoal = true;
            state.FinalGoal = rawDestination;
            state.Waypoints.Clear();
            state.ObstacleName = string.Empty;
        }

        while (state.Waypoints.Count > 0 &&
               PlanarDistance(current, state.Waypoints.Peek()) <= WaypointArrival)
        {
            state.Waypoints.Dequeue();
        }

        if (state.Waypoints.Count == 0)
        {
            Obstacle hit = FindFirstBlockingObstacle(current, state.FinalGoal, regiment.Formation);
            if (hit != null)
            {
                BuildDetour(current, state.FinalGoal, regiment.Formation, hit, state.Waypoints);
                state.ObstacleName = hit.Name;

                Debug.Log(
                    "OBSTACLE-09F11|Unit=" + regiment.RegimentName +
                    "|BlockedBy=" + hit.Name +
                    "|Formation=" + regiment.Formation +
                    "|Waypoints=" + state.Waypoints.Count +
                    "|AutoDetour=True");
            }
        }

        Vector3 steering = state.Waypoints.Count > 0
            ? state.Waypoints.Peek()
            : state.FinalGoal;

        steering.y = PrototypeBootstrap.SampleGroundHeight(steering.x, steering.z) + 0.10f;
        destinationField.SetValue(regiment, steering);
        hasDestinationField.SetValue(regiment, true);
        state.LastSteeringTarget = steering;
    }

    private Obstacle FindFirstBlockingObstacle(
        Vector3 start,
        Vector3 goal,
        RegimentFormation formation)
    {
        Obstacle best = null;
        float bestDistance = float.PositiveInfinity;
        float clearance = formation == RegimentFormation.Line
            ? LineHalfWidth
            : ColumnHalfWidth;

        foreach (Obstacle obstacle in obstacles)
        {
            Rect rect = ExpandedRect(obstacle.Bounds, clearance);
            if (!SegmentIntersectsRect(start, goal, rect))
                continue;

            Vector3 centre = obstacle.Bounds.center;
            centre.y = 0f;
            float distance = PlanarDistance(start, centre);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = obstacle;
            }
        }

        return best;
    }

    private static void BuildDetour(
        Vector3 start,
        Vector3 goal,
        RegimentFormation formation,
        Obstacle obstacle,
        Queue<Vector3> output)
    {
        float clearance = formation == RegimentFormation.Line
            ? LineHalfWidth
            : ColumnHalfWidth;

        Rect r = ExpandedRect(obstacle.Bounds, clearance + CornerPad);

        Vector3 tl = GroundPoint(r.xMin, r.yMax);
        Vector3 tr = GroundPoint(r.xMax, r.yMax);
        Vector3 bl = GroundPoint(r.xMin, r.yMin);
        Vector3 br = GroundPoint(r.xMax, r.yMin);

        Vector3[][] candidates =
        {
            new[] { tl, tr },
            new[] { tr, tl },
            new[] { bl, br },
            new[] { br, bl },
            new[] { tl, bl },
            new[] { bl, tl },
            new[] { tr, br },
            new[] { br, tr }
        };

        float bestCost = float.PositiveInfinity;
        Vector3[] best = null;

        foreach (Vector3[] pair in candidates)
        {
            float cost =
                PlanarDistance(start, pair[0]) +
                PlanarDistance(pair[0], pair[1]) +
                PlanarDistance(pair[1], goal);

            if (cost < bestCost)
            {
                bestCost = cost;
                best = pair;
            }
        }

        if (best == null)
            return;

        output.Enqueue(best[0]);
        output.Enqueue(best[1]);
    }

    private static Rect ExpandedRect(Bounds bounds, float clearance)
    {
        return Rect.MinMaxRect(
            bounds.min.x - clearance,
            bounds.min.z - clearance,
            bounds.max.x + clearance,
            bounds.max.z + clearance);
    }

    private static bool SegmentIntersectsRect(Vector3 a, Vector3 b, Rect rect)
    {
        if (rect.Contains(new Vector2(a.x, a.z)) || rect.Contains(new Vector2(b.x, b.z)))
            return true;

        Vector2 p1 = new Vector2(a.x, a.z);
        Vector2 p2 = new Vector2(b.x, b.z);
        Vector2 tl = new Vector2(rect.xMin, rect.yMax);
        Vector2 tr = new Vector2(rect.xMax, rect.yMax);
        Vector2 bl = new Vector2(rect.xMin, rect.yMin);
        Vector2 br = new Vector2(rect.xMax, rect.yMin);

        return SegmentsIntersect(p1, p2, tl, tr) ||
               SegmentsIntersect(p1, p2, tr, br) ||
               SegmentsIntersect(p1, p2, br, bl) ||
               SegmentsIntersect(p1, p2, bl, tl);
    }

    private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float d1 = Cross(b - a, c - a);
        float d2 = Cross(b - a, d - a);
        float d3 = Cross(d - c, a - c);
        float d4 = Cross(d - c, b - c);

        return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) &&
               ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static Vector3 GroundPoint(float x, float z)
    {
        return new Vector3(
            x,
            PrototypeBootstrap.SampleGroundHeight(x, z) + 0.10f,
            z);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
