using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f25
// A company that was detached from the Major by a direct player order must NOT
// resurrect its previous Major assignment merely because the player toggles AI ON.
// AI ON in this state means: local Captain AI may take over locally. Only a NEW
// Major order is allowed to reclaim the company into battalion command.
[DefaultExecutionOrder(-100)]
public sealed class PrototypeLocalCaptainAuthority09F25 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private FieldInfo manualDetachedField;
    private FieldInfo lastAssignmentsField;
    private FieldInfo missionsField;
    private FieldInfo playerRoutesField;

    private readonly HashSet<Regiment> localCaptainAuthority = new HashSet<Regiment>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeLocalCaptainAuthority09F25>() == null)
            new GameObject("PrototypeLocalCaptainAuthority_v000009f25").AddComponent<PrototypeLocalCaptainAuthority09F25>();
    }

    private void Update()
    {
        if (!Resolve())
            return;

        HashSet<Regiment> detached = manualDetachedField.GetValue(major) as HashSet<Regiment>;
        IDictionary savedAssignments = lastAssignmentsField.GetValue(major) as IDictionary;
        IDictionary majorMissions = missionsField.GetValue(major) as IDictionary;
        IDictionary playerRoutes = GetPlayerRoutes();

        if (detached == null || savedAssignments == null || majorMissions == null)
            return;

        // This runs before PrototypeMajorBattalion09F18.Update(). If a manually
        // detached company has just been switched AI ON, invalidate the OLD saved
        // battalion assignment before SyncManualAiState() can reapply it.
        foreach (Regiment regiment in new List<Regiment>(detached))
        {
            if (regiment == null)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled)
                continue;

            if (savedAssignments.Contains(regiment))
                savedAssignments.Remove(regiment);

            if (localCaptainAuthority.Add(regiment))
            {
                Debug.Log("AI-AUTHORITY-09F25|Unit=" + regiment.RegimentName +
                          "|AI=ON|Authority=LOCAL_CAPTAIN|OldMajorAssignmentDiscarded=True");
            }
        }

        List<Regiment> remove = null;
        foreach (Regiment regiment in localCaptainAuthority)
        {
            if (regiment == null)
            {
                AddRemove(ref remove, regiment);
                continue;
            }

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null)
            {
                AddRemove(ref remove, regiment);
                continue;
            }

            // A NEW Major order creates a fresh live mission. That is the only parent
            // event which may reclaim a company from local Captain authority.
            if (majorMissions.Contains(regiment))
            {
                if (!controller.enabled)
                    controller.enabled = true;
                AddRemove(ref remove, regiment);
                Debug.Log("AI-AUTHORITY-09F25|Unit=" + regiment.RegimentName +
                          "|Authority=MAJOR|Reason=NewMajorMission");
                continue;
            }

            if (!controller.AIEnabled)
            {
                if (!controller.enabled)
                    controller.enabled = true;
                AddRemove(ref remove, regiment);
                continue;
            }

            bool hasManualRoute = playerRoutes != null && playerRoutes.Contains(regiment);

            // PlayerCommander owns an existing manual route until it reaches its final
            // waypoint. Keep AIEnabled=true for UI/state semantics, but pause the local
            // OfficerAI Update so it cannot replace the route with Defend/AttackNearest.
            if (hasManualRoute)
            {
                if (controller.enabled)
                    controller.enabled = false;
            }
            else
            {
                // Manual route is complete. Local Captain AI can now assess from the
                // company's current position without reviving a stale Major objective.
                if (!controller.enabled)
                {
                    controller.enabled = true;
                    Debug.Log("AI-AUTHORITY-09F25|Unit=" + regiment.RegimentName +
                              "|Authority=LOCAL_CAPTAIN|ManualRouteComplete=True|LocalAIReleased=True");
                }
            }
        }

        if (remove != null)
            foreach (Regiment regiment in remove)
                localCaptainAuthority.Remove(regiment);
    }

    private bool Resolve()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return false;

        if (manualDetachedField != null)
            return true;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        System.Type majorType = typeof(PrototypeMajorBattalion09F18);
        manualDetachedField = majorType.GetField("manualDetached", flags);
        lastAssignmentsField = majorType.GetField("lastAssignments", flags);
        missionsField = majorType.GetField("missions", flags);
        playerRoutesField = typeof(PlayerCommander).GetField("routes", flags);

        bool ok = manualDetachedField != null &&
                  lastAssignmentsField != null &&
                  missionsField != null &&
                  playerRoutesField != null;

        if (!ok)
            Debug.LogError("AI-AUTHORITY-09F25|Installed=False|Reason=ReflectionMissing");

        return ok;
    }

    private IDictionary GetPlayerRoutes()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        return commander != null && playerRoutesField != null
            ? playerRoutesField.GetValue(commander) as IDictionary
            : null;
    }

    private static void AddRemove(ref List<Regiment> list, Regiment regiment)
    {
        if (list == null)
            list = new List<Regiment>();
        list.Add(regiment);
    }

    private void OnDisable()
    {
        RestoreControllers();
    }

    private void OnDestroy()
    {
        RestoreControllers();
    }

    private void RestoreControllers()
    {
        foreach (Regiment regiment in localCaptainAuthority)
        {
            if (regiment == null)
                continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
                controller.enabled = true;
        }
        localCaptainAuthority.Clear();
    }
}
