using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09h4 manual PlayerCommander route policy/recovery.
// Applies ONLY while Officer AI is disabled. Manual routes march in Column until
// the final deployment zone. If a route makes no useful progress for 3.5 seconds,
// one controlled bypass waypoint is inserted into the existing PlayerCommander route.
// Navigation V3 remains the steering owner toward each route waypoint.
[DefaultExecutionOrder(1250)]
public sealed class PrototypeManualRouteRecovery09H4 : MonoBehaviour
{
    private sealed class RouteProgress
    {
        public object RouteObject;
        public int CurrentIndex = -1;
        public Vector3 CurrentTarget;
        public Vector3 LastPosition;
        public float LastProgressAt;
        public float CooldownUntil;
        public int RecoveryCount;
        public bool Initialized;
        public bool MarchColumn;
    }

    private readonly Dictionary<Regiment, RouteProgress> progress =
        new Dictionary<Regiment, RouteProgress>();

    private FieldInfo routesField;
    private float nextSample;

    private const float SampleInterval = 0.25f;
    private const float DeployDistance = 14f;
    private const float ProgressDistance = 0.45f;
    private const float StallSeconds = 3.5f;
    private const float RecoveryCooldown = 4.0f;
    private const float RouteArrivalGuard = 5.0f;
    private const int MaxRecoveryWaypoints = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeManualRouteRecovery09H4>() != null)
            return;

        GameObject root = new GameObject("PrototypeManualRouteRecovery_v000009h4");
        root.AddComponent<PrototypeManualRouteRecovery09H4>();
    }

    private void Awake()
    {
        routesField = typeof(PlayerCommander).GetField(
            "routes",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (routesField == null)
        {
            Debug.LogWarning("MANUAL-ROUTE-09H4|Installed=False|Reason=PlayerCommander.routes_not_found");
            enabled = false;
            return;
        }

        Debug.Log(
            "MANUAL-ROUTE-09H4|Installed=True|AI=OFF-only|March=Column|DeployDistance=14|" +
            "StallRecovery=3.5s|V3SteeringOwner=True");
    }

    private void Update()
    {
        if (Time.time < nextSample)
            return;

        nextSample = Time.time + SampleInterval;

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null)
            return;

        IDictionary routes = routesField.GetValue(commander) as IDictionary;
        if (routes == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (DictionaryEntry entry in routes)
        {
            Regiment regiment = entry.Key as Regiment;
            object route = entry.Value;
            if (regiment == null || route == null || regiment.IsRouted)
                continue;

            OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
            if (ai != null && ai.AIEnabled)
                continue;

            active.Add(regiment);
            ProcessManualRoute(regiment, route);
        }

        CleanupInactive(active);
    }

    private void ProcessManualRoute(Regiment regiment, object route)
    {
        FieldInfo waypointsField = route.GetType().GetField(
            "Waypoints",
            BindingFlags.Instance | BindingFlags.Public);
        FieldInfo currentIndexField = route.GetType().GetField(
            "CurrentIndex",
            BindingFlags.Instance | BindingFlags.Public);
        FieldInfo finalFormationField = route.GetType().GetField(
            "FinalFormation",
            BindingFlags.Instance | BindingFlags.Public);

        if (waypointsField == null || currentIndexField == null)
            return;

        IList waypoints = waypointsField.GetValue(route) as IList;
        int currentIndex = (int)currentIndexField.GetValue(route);
        if (waypoints == null || currentIndex < 0 || currentIndex >= waypoints.Count)
            return;

        object waypointObject = waypoints[currentIndex];
        if (!(waypointObject is Vector3))
            return;

        Vector3 target = (Vector3)waypointObject;
        Vector3 position = regiment.transform.position;
        float distance = PlanarDistance(position, target);

        RouteProgress state;
        if (!progress.TryGetValue(regiment, out state))
        {
            state = new RouteProgress();
            progress[regiment] = state;
        }

        bool routeChanged =
            !ReferenceEquals(state.RouteObject, route) ||
            state.CurrentIndex != currentIndex ||
            !state.Initialized ||
            PlanarDistance(state.CurrentTarget, target) > 1.0f;

        if (routeChanged)
        {
            state.RouteObject = route;
            state.CurrentIndex = currentIndex;
            state.CurrentTarget = target;
            state.LastPosition = position;
            state.LastProgressAt = Time.time;
            state.Initialized = true;
        }

        ApplyManualFormation(regiment, route, finalFormationField, distance, state);

        if (distance <= RouteArrivalGuard)
        {
            state.LastPosition = position;
            state.LastProgressAt = Time.time;
            return;
        }

        float moved = PlanarDistance(position, state.LastPosition);
        if (moved >= ProgressDistance)
        {
            state.LastPosition = position;
            state.LastProgressAt = Time.time;
            return;
        }

        if (Time.time < state.CooldownUntil ||
            Time.time - state.LastProgressAt < StallSeconds)
        {
            return;
        }

        if (state.RecoveryCount >= MaxRecoveryWaypoints)
        {
            state.LastProgressAt = Time.time;
            state.CooldownUntil = Time.time + RecoveryCooldown;
            Debug.LogWarning(string.Format(
                "MANUAL-ROUTE-09H4|Unit={0}|Recovery=False|Reason=RecoveryLimit|Target=({1:0.0},{2:0.0})|Distance={3:0.0}",
                regiment.RegimentName,
                target.x,
                target.z,
                distance));
            return;
        }

        Vector3 bypass;
        float score;
        if (!TryChooseBypass(position, target, out bypass, out score))
        {
            state.LastProgressAt = Time.time;
            state.CooldownUntil = Time.time + RecoveryCooldown;
            regiment.SetFormation(RegimentFormation.Column);
            regiment.OrderMove(target);

            Debug.LogWarning(string.Format(
                "MANUAL-ROUTE-09H4|Unit={0}|Recovery=False|Reason=NoSafeBypass|ReissueTarget=True|Target=({1:0.0},{2:0.0})",
                regiment.RegimentName,
                target.x,
                target.z));
            return;
        }

        waypoints.Insert(currentIndex, bypass);
        regiment.SetFormation(RegimentFormation.Column);
        regiment.OrderMove(bypass);

        state.CurrentTarget = bypass;
        state.LastPosition = position;
        state.LastProgressAt = Time.time;
        state.CooldownUntil = Time.time + RecoveryCooldown;
        state.RecoveryCount++;
        state.MarchColumn = true;

        Debug.LogWarning(string.Format(
            "MANUAL-ROUTE-09H4|Unit={0}|Recovery=True|Reason=NoProgress|InsertedBypass=({1:0.0},{2:0.0})|OriginalTarget=({3:0.0},{4:0.0})|Score={5:0.0}|RecoveryCount={6}",
            regiment.RegimentName,
            bypass.x,
            bypass.z,
            target.x,
            target.z,
            score,
            state.RecoveryCount));
    }

    private static void ApplyManualFormation(
        Regiment regiment,
        object route,
        FieldInfo finalFormationField,
        float distance,
        RouteProgress state)
    {
        if (distance > DeployDistance)
        {
            if (regiment.Formation != RegimentFormation.Column)
                regiment.SetFormation(RegimentFormation.Column);

            if (!state.MarchColumn)
            {
                state.MarchColumn = true;
                Debug.Log(string.Format(
                    "MANUAL-FORMATION-09H4|Unit={0}|MarchColumn=True|Distance={1:0.0}",
                    regiment.RegimentName,
                    distance));
            }
            return;
        }

        RegimentFormation finalFormation = RegimentFormation.Line;
        if (finalFormationField != null)
        {
            object value = finalFormationField.GetValue(route);
            if (value is RegimentFormation)
                finalFormation = (RegimentFormation)value;
        }

        if (regiment.Formation != finalFormation)
            regiment.SetFormation(finalFormation);

        if (state.MarchColumn)
        {
            state.MarchColumn = false;
            Debug.Log(string.Format(
                "MANUAL-FORMATION-09H4|Unit={0}|MarchColumn=False|Deploy={1}|Distance={2:0.0}",
                regiment.RegimentName,
                finalFormation,
                distance));
        }
    }

    private static bool TryChooseBypass(
        Vector3 current,
        Vector3 target,
        out Vector3 best,
        out float bestScore)
    {
        best = current;
        bestScore = float.PositiveInfinity;

        Vector3 direction = target - current;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return false;
        direction.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        float[] forwardSteps = { 6f, 10f, 16f };
        float[] lateralSteps = { 10f, -10f, 16f, -16f, 24f, -24f };

        bool found = false;
        for (int f = 0; f < forwardSteps.Length; f++)
        {
            for (int l = 0; l < lateralSteps.Length; l++)
            {
                Vector3 requested =
                    current +
                    direction * forwardSteps[f] +
                    side * lateralSteps[l];

                Vector3 safe;
                if (!PrototypeBattlefieldNavigationV3.TryFindNearestValidDestination(
                        requested,
                        RegimentFormation.Column,
                        out safe))
                {
                    continue;
                }

                float firstLeg = PlanarDistance(current, safe);
                if (firstLeg < 4f)
                    continue;

                float firstPenalty = PrototypeBattlefieldNavigationV3.EstimatePathPenalty(
                    current,
                    safe,
                    RegimentFormation.Column);

                // Do not invent a bypass that itself requires a new river crossing.
                if (firstPenalty >= 450f)
                    continue;

                float secondPenalty = PrototypeBattlefieldNavigationV3.EstimatePathPenalty(
                    safe,
                    target,
                    RegimentFormation.Column);

                float score =
                    firstLeg +
                    PlanarDistance(safe, target) +
                    firstPenalty * 0.30f +
                    secondPenalty * 0.08f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = safe;
                found = true;
            }
        }

        if (!found)
            return false;

        best.y = PrototypeBootstrap.SampleGroundHeight(best.x, best.z) + 0.10f;
        return true;
    }

    private void CleanupInactive(HashSet<Regiment> active)
    {
        if (progress.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, RouteProgress> pair in progress)
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
            progress.Remove(remove[i]);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
