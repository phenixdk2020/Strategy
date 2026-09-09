using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09i1 TEST - targeted V3 hard-obstacle detour continuation fix.
//
// Root cause confirmed from QA telemetry:
// a regiment can reach the first V3 detour point while the same Farmhouse/Barn still
// blocks the route. V3 then finishes that leg and can immediately choose the same
// near-identical detour point again, producing an endless no-progress loop.
//
// This compatibility layer runs immediately after V3. It does NOT become a second
// steering owner. It only replaces a reached/degenerate V3 detour point with the next
// safe continuation point around the SAME active hard obstacle and writes that point
// back into V3's existing NavigationState. V3 remains authoritative next frame.
[DefaultExecutionOrder(1450)]
public sealed class PrototypeNavigationV3DetourContinuation09I1 : MonoBehaviour
{
    private sealed class ContinuationState
    {
        public Vector3 ObstacleCenter;
        public int Count;
        public float CooldownUntil;
        public float LastFailureLogAt;
    }

    private readonly Dictionary<Regiment, ContinuationState> continuation =
        new Dictionary<Regiment, ContinuationState>();

    private FieldInfo statesField;
    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private bool reflectionReady;
    private bool announced;

    private const float ColumnClearance = 4.4f;
    private const float TriggerDistance = 2.85f;
    private const float MinimumLegDistance = 5.0f;
    private const float ContinuationCooldown = 0.20f;
    private const int MaxContinuationsPerObstacle = 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigationV3DetourContinuation09I1>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationV3DetourContinuation_v000009i1");
        root.AddComponent<PrototypeNavigationV3DetourContinuation09I1>();
    }

    private void Awake()
    {
        const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        statesField = typeof(PrototypeBattlefieldNavigationV3).GetField("states", privateInstance);
        destinationField = typeof(Regiment).GetField("destination", privateInstance);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", privateInstance);

        reflectionReady =
            statesField != null &&
            destinationField != null &&
            hasDestinationField != null;

        if (!reflectionReady)
        {
            Debug.LogError(
                "NAV-V3-09I1|Installed=False|Reason=RequiredPrivateFieldsNotFound");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!reflectionReady)
            return;

        PrototypeBattlefieldNavigationV3 v3 = PrototypeBattlefieldNavigationV3.Instance;
        BattleManager battle = BattleManager.Instance;
        if (v3 == null || battle == null || battle.Regiments == null)
            return;

        IDictionary states = statesField.GetValue(v3) as IDictionary;
        if (states == null)
            return;

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "NAV-V3-09I1|Installed=True|Fix=ReachedDetourContinuation|" +
                "HardObstacles=Farmhouse+Barn|V3SteeringOwner=True");
        }

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            object navState = states[regiment];
            if (navState == null)
            {
                continuation.Remove(regiment);
                continue;
            }

            ProcessRegiment(regiment, navState);
        }

        CleanupDestroyed();
    }

    private void ProcessRegiment(Regiment regiment, object navState)
    {
        System.Type stateType = navState.GetType();
        FieldInfo detouringField = stateType.GetField("Detouring", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo obstacleField = stateType.GetField("ActiveObstacle", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo detourPointField = stateType.GetField("DetourPoint", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo goalField = stateType.GetField("Goal", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo lastSteeringField = stateType.GetField("LastSteeringTarget", BindingFlags.Instance | BindingFlags.Public);

        if (detouringField == null || obstacleField == null ||
            detourPointField == null || goalField == null)
        {
            return;
        }

        bool detouring = (bool)detouringField.GetValue(navState);
        object obstacle = obstacleField.GetValue(navState);
        if (!detouring || obstacle == null)
        {
            continuation.Remove(regiment);
            return;
        }

        System.Type obstacleType = obstacle.GetType();
        FieldInfo nameField = obstacleType.GetField("Name", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo centerField = obstacleType.GetField("Center", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo radiusField = obstacleType.GetField("Radius", BindingFlags.Instance | BindingFlags.Public);
        if (nameField == null || centerField == null || radiusField == null)
            return;

        string obstacleName = nameField.GetValue(obstacle) as string;
        if (obstacleName != "Farmhouse" && obstacleName != "Barn")
            return;

        Vector3 current = regiment.transform.position;
        current.y = 0f;
        Vector3 detourPoint = (Vector3)detourPointField.GetValue(navState);
        detourPoint.y = 0f;
        Vector3 goal = (Vector3)goalField.GetValue(navState);
        goal.y = 0f;

        if (PlanarDistance(current, detourPoint) > TriggerDistance ||
            PlanarDistance(current, goal) <= 5f)
        {
            return;
        }

        Vector3 obstacleCenter = (Vector3)centerField.GetValue(obstacle);
        obstacleCenter.y = 0f;
        float obstacleRadius = (float)radiusField.GetValue(obstacle);

        ContinuationState watch;
        if (!continuation.TryGetValue(regiment, out watch))
        {
            watch = new ContinuationState
            {
                ObstacleCenter = obstacleCenter,
                Count = 0,
                CooldownUntil = 0f
            };
            continuation[regiment] = watch;
        }

        if (PlanarDistance(watch.ObstacleCenter, obstacleCenter) > 0.5f)
        {
            watch.ObstacleCenter = obstacleCenter;
            watch.Count = 0;
            watch.CooldownUntil = 0f;
        }

        if (Time.time < watch.CooldownUntil)
            return;

        if (watch.Count >= MaxContinuationsPerObstacle)
        {
            if (Time.time - watch.LastFailureLogAt >= 2f)
            {
                watch.LastFailureLogAt = Time.time;
                Debug.LogWarning(string.Format(
                    "NAV-V3-09I1|Unit={0}|Continuation=False|Obstacle={1}|" +
                    "Reason=ContinuationLimit|Count={2}|Goal=({3:0.0},{4:0.0})",
                    regiment.RegimentName,
                    obstacleName,
                    watch.Count,
                    goal.x,
                    goal.z));
            }
            return;
        }

        Vector3 next;
        float score;
        if (!TryChooseContinuation(
                current,
                goal,
                obstacleCenter,
                obstacleRadius,
                out next,
                out score))
        {
            watch.CooldownUntil = Time.time + 0.75f;
            if (Time.time - watch.LastFailureLogAt >= 1.5f)
            {
                watch.LastFailureLogAt = Time.time;
                Debug.LogWarning(string.Format(
                    "NAV-V3-09I1|Unit={0}|Continuation=False|Obstacle={1}|" +
                    "Reason=NoSafeContinuation|At=({2:0.0},{3:0.0})|Goal=({4:0.0},{5:0.0})",
                    regiment.RegimentName,
                    obstacleName,
                    current.x,
                    current.z,
                    goal.x,
                    goal.z));
            }
            return;
        }

        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
        detourPointField.SetValue(navState, next);
        detouringField.SetValue(navState, true);
        if (lastSteeringField != null)
            lastSteeringField.SetValue(navState, next);

        // V3 already owns this destination field. Writing the replacement target here
        // simply makes the corrected V3 state effective for the next Regiment movement
        // tick; PlayerCommander may still publish the original route goal before V3
        // runs again, exactly as in the existing architecture.
        destinationField.SetValue(regiment, next);
        hasDestinationField.SetValue(regiment, true);

        watch.Count++;
        watch.CooldownUntil = Time.time + ContinuationCooldown;

        Debug.LogWarning(string.Format(
            "NAV-V3-09I1|Unit={0}|Continuation=True|Obstacle={1}|" +
            "FromVia=({2:0.0},{3:0.0})|NextVia=({4:0.0},{5:0.0})|" +
            "Goal=({6:0.0},{7:0.0})|Score={8:0.0}|Count={9}",
            regiment.RegimentName,
            obstacleName,
            detourPoint.x,
            detourPoint.z,
            next.x,
            next.z,
            goal.x,
            goal.z,
            score,
            watch.Count));
    }

    private static bool TryChooseContinuation(
        Vector3 current,
        Vector3 goal,
        Vector3 obstacleCenter,
        float obstacleRadius,
        out Vector3 best,
        out float bestScore)
    {
        best = current;
        bestScore = float.PositiveInfinity;

        float required = obstacleRadius + ColumnClearance;
        float[] ringRadii =
        {
            required + 4.5f,
            required + 7.5f,
            required + 11.0f
        };

        float currentGoalPenalty =
            PrototypeBattlefieldNavigationV3.EstimatePathPenalty(
                current,
                goal,
                RegimentFormation.Column);

        bool found = false;
        for (int r = 0; r < ringRadii.Length; r++)
        {
            float ring = ringRadii[r];
            for (int i = 0; i < 64; i++)
            {
                float angle = i / 64f * Mathf.PI * 2f;
                Vector3 requested = obstacleCenter + new Vector3(
                    Mathf.Cos(angle) * ring,
                    0f,
                    Mathf.Sin(angle) * ring);

                Vector3 candidate;
                if (!PrototypeBattlefieldNavigationV3.TryFindNearestValidDestination(
                        requested,
                        RegimentFormation.Column,
                        out candidate))
                {
                    continue;
                }

                candidate.y = 0f;

                // A large snap means this ring point was not genuinely usable.
                if (PlanarDistance(requested, candidate) > 0.85f)
                    continue;

                float firstLeg = PlanarDistance(current, candidate);
                if (firstLeg < MinimumLegDistance)
                    continue;

                // Do not choose a chord that cuts back through the same hard obstacle.
                float clearance = DistancePointToSegment(
                    obstacleCenter,
                    current,
                    candidate);
                if (clearance < required - 0.10f)
                    continue;

                float firstPenalty =
                    PrototypeBattlefieldNavigationV3.EstimatePathPenalty(
                        current,
                        candidate,
                        RegimentFormation.Column);

                // 500+ is the V3 river-crossing penalty. A local building bypass may
                // never invent a new water crossing.
                if (firstPenalty >= 450f)
                    continue;

                float secondPenalty =
                    PrototypeBattlefieldNavigationV3.EstimatePathPenalty(
                        candidate,
                        goal,
                        RegimentFormation.Column);

                float goalDistanceBefore = PlanarDistance(current, goal);
                float goalDistanceAfter = PlanarDistance(candidate, goal);
                float backward = Mathf.Max(0f, goalDistanceAfter - goalDistanceBefore);

                float score =
                    firstLeg +
                    goalDistanceAfter +
                    firstPenalty * 0.35f +
                    secondPenalty * 0.10f +
                    backward * 1.8f;

                // Strongly prefer a continuation from which the remaining path has
                // less obstacle penalty than from the stuck point.
                if (secondPenalty + 1f < currentGoalPenalty)
                    score -= 24f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = candidate;
                found = true;
            }

            // Prefer the smallest ring that provides a clean/credible continuation.
            if (found && bestScore < 500f)
                break;
        }

        return found;
    }

    private static float DistancePointToSegment(
        Vector3 point,
        Vector3 a,
        Vector3 b)
    {
        point.y = 0f;
        a.y = 0f;
        b.y = 0f;

        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 0.001f)
            return PlanarDistance(point, a);

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / denom);
        Vector3 closest = a + ab * t;
        return PlanarDistance(point, closest);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void CleanupDestroyed()
    {
        if (continuation.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, ContinuationState> pair in continuation)
        {
            if (pair.Key != null)
                continue;

            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
            continuation.Remove(stale[i]);
    }
}
