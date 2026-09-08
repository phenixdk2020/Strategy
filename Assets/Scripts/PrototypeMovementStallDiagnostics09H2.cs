using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09h2 passive movement diagnostics.
// Reports sustained no-progress while a Regiment still has an active destination.
// This component is read-only: it never writes destination, formation, AI or combat.
[DefaultExecutionOrder(1500)]
public sealed class PrototypeMovementStallDiagnostics09H2 : MonoBehaviour
{
    private sealed class StallState
    {
        public Vector3 LastPosition;
        public float StationarySince;
        public bool Reported;
        public bool Initialized;
    }

    private readonly Dictionary<Regiment, StallState> states =
        new Dictionary<Regiment, StallState>();

    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private float nextSample;

    private const float SampleInterval = 0.50f;
    private const float ProgressDistance = 0.30f;
    private const float ReportAfterSeconds = 3.50f;
    private const float IgnoreGoalDistance = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMovementStallDiagnostics09H2>() != null)
            return;

        GameObject root = new GameObject("PrototypeMovementStallDiagnostics_v000009h2");
        root.AddComponent<PrototypeMovementStallDiagnostics09H2>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);

        if (destinationField == null || hasDestinationField == null)
        {
            Debug.LogWarning("STALL-09H2|Installed=False|Reason=RegimentMovementFieldsNotFound");
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < nextSample)
            return;

        nextSample = Time.time + SampleInterval;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            SampleRegiment(regiment);
        }
    }

    private void SampleRegiment(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out StallState state))
        {
            state = new StallState();
            states[regiment] = state;
        }

        bool hasDestination = (bool)hasDestinationField.GetValue(regiment);
        Vector3 position = regiment.transform.position;

        if (!state.Initialized)
        {
            state.Initialized = true;
            state.LastPosition = position;
            state.StationarySince = Time.time;
            return;
        }

        if (!hasDestination || regiment.IsRouted)
        {
            ResetState(state, position);
            return;
        }

        Vector3 destination = (Vector3)destinationField.GetValue(regiment);
        float goalDistance = PlanarDistance(position, destination);
        float moved = PlanarDistance(position, state.LastPosition);

        if (goalDistance <= IgnoreGoalDistance)
        {
            ResetState(state, position);
            return;
        }

        if (moved >= ProgressDistance)
        {
            state.LastPosition = position;
            state.StationarySince = Time.time;
            state.Reported = false;
            return;
        }

        if (state.StationarySince <= 0f)
            state.StationarySince = Time.time;

        if (!state.Reported && Time.time - state.StationarySince >= ReportAfterSeconds)
        {
            state.Reported = true;
            WriteReport(regiment, position, destination, goalDistance);
        }
    }

    private static void ResetState(StallState state, Vector3 position)
    {
        state.LastPosition = position;
        state.StationarySince = Time.time;
        state.Reported = false;
    }

    private static void WriteReport(
        Regiment regiment,
        Vector3 position,
        Vector3 destination,
        float distance)
    {
        OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
        string mission = ai != null ? ai.Mission.ToString() : "None";
        string task = ai != null ? ai.CurrentTask : "None";

        string nearbyName;
        float nearbyDistance;
        FindNearestHardObstacle(position, out nearbyName, out nearbyDistance);

        Debug.LogWarning(string.Format(
            "STALL-09H2|Unit={0}|Team={1}|Pos=({2:0.0},{3:0.0})|Dest=({4:0.0},{5:0.0})|Dist={6:0.0}|Formation={7}|AI={8}|Mission={9}|Task={10}|NearestHardObstacle={11}|ObstacleDist={12:0.0}|ReadOnly=True",
            regiment.RegimentName,
            regiment.Team,
            position.x,
            position.z,
            destination.x,
            destination.z,
            distance,
            regiment.Formation,
            ai != null && ai.AIEnabled,
            mission,
            task,
            nearbyName,
            nearbyDistance));
    }

    private static void FindNearestHardObstacle(
        Vector3 position,
        out string obstacleName,
        out float obstacleDistance)
    {
        obstacleName = "None";
        obstacleDistance = float.PositiveInfinity;

        // Unity 6.6 preferred overload: no deprecated FindObjectsSortMode parameter.
        Transform[] all = Object.FindObjectsByType<Transform>();
        foreach (Transform item in all)
        {
            if (item == null)
                continue;

            if (item.name != "Farmhouse" &&
                item.name != "Barn" &&
                item.name != "FencePost")
            {
                continue;
            }

            float distance = PlanarDistance(position, item.position);
            if (distance >= obstacleDistance)
                continue;

            obstacleDistance = distance;
            obstacleName = item.name;
        }

        if (float.IsInfinity(obstacleDistance))
            obstacleDistance = -1f;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
