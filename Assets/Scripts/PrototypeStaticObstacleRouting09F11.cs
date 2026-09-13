using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f11 lightweight static-obstacle routing, corrected by v00.00.09f12.
// This deliberately does NOT re-enable the old V1/V3/V4 obstacle-navigation stack.
// It owns only explicitly approved hard blockers (Farmhouse + Barn) and inserts
// safe corner waypoints when the current route crosses one.
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

    private static PrototypeStaticObstacleRouting09F11 instance;

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
        instance = this;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogError("OBSTACLE-09F12|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static bool RequiresDetour(Regiment regiment, Vector3 goal)
    {
        if (instance == null || !instance.enabled || regiment == null)
            return false;

        if (!instance.scanned)
            instance.ScanApprovedBlockers();

        Vector3 start = regiment.transform.position;
        start.y = 0f;
        goal.y = 0f;
        return instance.FindFirstBlockingObstacle(start, goal, regiment.Formation) != null;
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
            "OBSTACLE-09F12|Installed=True|ApprovedBlockers=Farmhouse,Barn|" +
            "Found=" + obstacles.Count +
            "|RepeatedFinalGoalDoesNotReset=True|ClusterMerge=True|" +
            "Trees=False|Fences=False|RiverOwnedSeparately=True");
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

        bool rawIsFinalGoal =
            state.HasGoal &&
            PlanarDistance(rawDestination, state.FinalGoal) <= 1.25f;

        bool isNewExternalGoal =
            !state.HasGoal || (!rawIsOwnSteering && !rawIsFinalGoal);

        if (isNewExternalGoal)
        {
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
                    "OBSTACLE-09F12|Unit=" + regiment.RegimentName +
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

    private void BuildDetour(
        Vector3 start,
        Vector3 goal,
        RegimentFormation formation,
        Obstacle obstacle,
        Queue<Vector3> output)
    {
        float clearance = formation == RegimentFormation.Line
            ? LineHalfWidth
            : ColumnHalfWidth;

        Rect cluster = ExpandedRect(obstacle.Bounds, clearance + CornerPad);

        // If approved hard blockers overlap after formation clearance is added,
        // route around them as one compound obstacle. This is important for the
        // Farmhouse + Barn pair when the 48 m Line frontage is used.
        bool expanded;
        int guard = 0;
        do
        {
            expanded = false;
            guard++;

            foreach (Obstacle other in obstacles)
            {
                Rect otherRect = ExpandedRect(other.Bounds, clearance + CornerPad);
                if (!RectsOverlap(cluster, otherRect))
                    continue;

                Rect merged = UnionRect(cluster, otherRect);
                if (!ApproximatelySameRect(cluster, merged))
                {
                    cluster = merged;
                    expanded = true;
                }
            }
        }
        while (expanded && guard < 8);

        Vector3 tl = GroundPoint(cluster.xMin, cluster.yMax);
        Vector3 tr = GroundPoint(cluster.xMax, cluster.yMax);
        Vector3 bl = GroundPoint(cluster.xMin, cluster.yMin);
        Vector3 br = GroundPoint(cluster.xMax, cluster.yMin);

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

    private static Rect UnionRect(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static bool RectsOverlap(Rect a, Rect b)
    {
        return a.xMin <= b.xMax && a.xMax >= b.xMin &&
               a.yMin <= b.yMax && a.yMax >= b.yMin;
    }

    private static bool ApproximatelySameRect(Rect a, Rect b)
    {
        return Mathf.Abs(a.xMin - b.xMin) < 0.01f &&
               Mathf.Abs(a.xMax - b.xMax) < 0.01f &&
               Mathf.Abs(a.yMin - b.yMin) < 0.01f &&
               Mathf.Abs(a.yMax - b.yMax) < 0.01f;
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
