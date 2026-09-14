using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f24
// Major mission commitment/authority guard.
//
// Fixes two observed problems:
// 1) a new Major order could leave a company physically executing an older destination;
// 2) a company could declare itself "arrived" early simply because an enemy entered
//    fire range, even though the displayed Major destination box was still several
//    metres away.
//
// While a Major-owned company is travelling to its assigned slot, local Officer AI
// decision-making is temporarily paused. AIEnabled itself remains ON, so UI/authority
// semantics are unchanged. Terrain/bridge routers remain free to replace the raw
// strategic destination with legal steering waypoints.
[DefaultExecutionOrder(650)]
public sealed class PrototypeMajorMissionCommitment09F24 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private FieldInfo missionsField;
    private FieldInfo lastOrderField;
    private FieldInfo lastOrderPointField;
    private MethodInfo clearPlayerRouteMethod;

    private FieldInfo regimentHasDestinationField;
    private FieldInfo regimentForcedTargetField;

    private readonly HashSet<OfficerAIController> pausedControllers = new HashSet<OfficerAIController>();
    private string lastOrderSignature = string.Empty;

    private const float ExactArrival = 4.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMajorMissionCommitment09F24>() == null)
            new GameObject("PrototypeMajorMissionCommitment_v000009f24").AddComponent<PrototypeMajorMissionCommitment09F24>();
    }

    private void Update()
    {
        if (!Resolve())
        {
            RestoreAllPausedControllers();
            return;
        }

        IDictionary missions = missionsField.GetValue(major) as IDictionary;
        if (missions == null || missions.Count == 0)
        {
            RestoreAllPausedControllers();
            lastOrderSignature = string.Empty;
            return;
        }

        MajorOrder09F18 order = (MajorOrder09F18)lastOrderField.GetValue(major);
        Vector3 orderPoint = (Vector3)lastOrderPointField.GetValue(major);
        string signature = BuildSignature(order, orderPoint);
        bool newMajorOrder = signature != lastOrderSignature;

        HashSet<OfficerAIController> activeThisFrame = new HashSet<OfficerAIController>();

        foreach (DictionaryEntry entry in missions)
        {
            Regiment regiment = entry.Key as Regiment;
            object mission = entry.Value;
            if (regiment == null || mission == null || regiment.IsRouted)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
                continue; // manual company authority always wins.

            Vector3 goal = ReadVector(mission, "Goal", regiment.transform.position);
            Vector3 facing = ReadVector(mission, "Facing", regiment.transform.forward);
            MajorOrder09F18 missionOrder = ReadOrder(mission);
            float distance = PlanarDistance(regiment.transform.position, goal);

            if (newMajorOrder)
            {
                HardReplaceOldExecution(regiment, controller, mission, goal, facing, missionOrder);
            }

            if (distance > ExactArrival)
            {
                // Never allow fire-range proximity to masquerade as arrival at the
                // displayed formation box. The destination box is the promised slot.
                WriteBool(mission, "Arrived", false);
                WriteFloat(mission, "NextAssert", 0f);

                if (controller != null)
                {
                    activeThisFrame.Add(controller);
                    PauseController(controller);
                }

                // Major.UpdateMissions can issue OrderHold() because of its old
                // canStopToFight rule. If that removed the active destination, restore
                // the strategic goal. Do not overwrite an existing destination here:
                // it may be a legal bridge/building steering waypoint.
                if (!HasDestination(regiment))
                    regiment.OrderMove(goal);

                PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
                if (visuals != null)
                {
                    visuals.SetMission(
                        regiment,
                        goal,
                        facing,
                        missionOrder,
                        ReadBool(mission, "Reserve"),
                        ReadBool(mission, "Flank"));
                }
            }
            else if (controller != null && pausedControllers.Contains(controller))
            {
                controller.enabled = true;
                pausedControllers.Remove(controller);
            }
        }

        RestoreControllersNotUsed(activeThisFrame);

        if (newMajorOrder)
        {
            Debug.Log("HQ-COMMIT-09F24|NewOrder=True|Order=" + order +
                      "|Objective=" + orderPoint.x.ToString("0.0") + "," + orderPoint.z.ToString("0.0") +
                      "|OldExecutionCleared=True|ExactDestinationCommitment=True");
            lastOrderSignature = signature;
        }
    }

    private bool Resolve()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return false;

        if (missionsField != null)
            return true;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type majorType = typeof(PrototypeMajorBattalion09F18);
        missionsField = majorType.GetField("missions", flags);
        lastOrderField = majorType.GetField("lastOrder", flags);
        lastOrderPointField = majorType.GetField("lastOrderPoint", flags);

        clearPlayerRouteMethod = majorType.GetMethod("ClearPlayerRoute", flags);

        Type regimentType = typeof(Regiment);
        regimentHasDestinationField = regimentType.GetField("hasDestination", flags);
        regimentForcedTargetField = regimentType.GetField("forcedTarget", flags);

        bool ok = missionsField != null && lastOrderField != null && lastOrderPointField != null;
        if (!ok)
            Debug.LogError("HQ-COMMIT-09F24|Installed=False|Reason=ReflectionMissing");
        return ok;
    }

    private void HardReplaceOldExecution(
        Regiment regiment,
        OfficerAIController controller,
        object mission,
        Vector3 goal,
        Vector3 facing,
        MajorOrder09F18 order)
    {
        // Remove a stale direct-player route/ghost if one still exists.
        if (clearPlayerRouteMethod != null)
            clearPlayerRouteMethod.Invoke(major, new object[] { regiment });

        // Remove any old direct attack target. The new Major mission is authoritative.
        if (regimentForcedTargetField != null)
            regimentForcedTargetField.SetValue(regiment, null);

        WriteBool(mission, "Arrived", false);
        WriteFloat(mission, "NextAssert", 0f);

        // Stop the old execution state first, then write the new strategic destination.
        regiment.OrderHold();
        regiment.OrderMove(goal);

        if (controller != null)
        {
            activeControllerReset(controller, order);
            PauseController(controller);
        }

        PrototypeMajorOrderVisuals09F18 visuals = PrototypeMajorOrderVisuals09F18.Instance;
        if (visuals != null)
        {
            visuals.SetMission(
                regiment,
                goal,
                facing,
                order,
                ReadBool(mission, "Reserve"),
                ReadBool(mission, "Flank"));
        }
    }

    private static void activeControllerReset(OfficerAIController controller, MajorOrder09F18 order)
    {
        if (controller == null)
            return;

        // Keep the same unified AI state, but remove the previous local mission before
        // the new Major destination is executed.
        controller.SetHoldMission();

        if (order == MajorOrder09F18.AttackHere)
            controller.SetDoctrine(OfficerAIDoctrine.Offensive);
        else if (order == MajorOrder09F18.DefendHere)
            controller.SetDoctrine(OfficerAIDoctrine.Defensive);
        else
            controller.SetDoctrine(OfficerAIDoctrine.Balanced);
    }

    private void PauseController(OfficerAIController controller)
    {
        if (controller == null)
            return;

        if (!pausedControllers.Contains(controller))
            pausedControllers.Add(controller);
        controller.enabled = false;
    }

    private void RestoreControllersNotUsed(HashSet<OfficerAIController> active)
    {
        List<OfficerAIController> restore = null;
        foreach (OfficerAIController controller in pausedControllers)
        {
            if (controller == null || active.Contains(controller))
                continue;
            if (restore == null)
                restore = new List<OfficerAIController>();
            restore.Add(controller);
        }

        if (restore == null)
            return;

        foreach (OfficerAIController controller in restore)
        {
            if (controller != null)
                controller.enabled = true;
            pausedControllers.Remove(controller);
        }
    }

    private void RestoreAllPausedControllers()
    {
        foreach (OfficerAIController controller in pausedControllers)
            if (controller != null)
                controller.enabled = true;
        pausedControllers.Clear();
    }

    private bool HasDestination(Regiment regiment)
    {
        if (regiment == null)
            return false;
        if (regimentHasDestinationField == null)
            return false;
        object value = regimentHasDestinationField.GetValue(regiment);
        return value is bool && (bool)value;
    }

    private static string BuildSignature(MajorOrder09F18 order, Vector3 point)
    {
        return order + "|" + Mathf.RoundToInt(point.x * 4f) + ":" + Mathf.RoundToInt(point.z * 4f);
    }

    private static MajorOrder09F18 ReadOrder(object mission)
    {
        FieldInfo field = MissionField(mission, "Order");
        return field != null ? (MajorOrder09F18)field.GetValue(mission) : MajorOrder09F18.None;
    }

    private static bool ReadBool(object mission, string name)
    {
        FieldInfo field = MissionField(mission, name);
        return field != null && (bool)field.GetValue(mission);
    }

    private static Vector3 ReadVector(object mission, string name, Vector3 fallback)
    {
        FieldInfo field = MissionField(mission, name);
        return field != null ? (Vector3)field.GetValue(mission) : fallback;
    }

    private static void WriteBool(object mission, string name, bool value)
    {
        FieldInfo field = MissionField(mission, name);
        if (field != null)
            field.SetValue(mission, value);
    }

    private static void WriteFloat(object mission, string name, float value)
    {
        FieldInfo field = MissionField(mission, name);
        if (field != null)
            field.SetValue(mission, value);
    }

    private static FieldInfo MissionField(object mission, string name)
    {
        return mission == null
            ? null
            : mission.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDisable()
    {
        RestoreAllPausedControllers();
    }

    private void OnDestroy()
    {
        RestoreAllPausedControllers();
    }
}
