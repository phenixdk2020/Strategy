using System.Reflection;
using UnityEngine;

// v00.00.09f20
// A player-issued Major mission is commander intent. Major AI may interpret the mission,
// but it must not silently replace FORSVAR HER/ANGRIB HER/etc. with a new autonomous mission.
// Doctrine changes immediately re-plan the existing player intent at the same objective.
[DefaultExecutionOrder(560)]
public sealed class PrototypeMajorIntentAuthority09F20 : MonoBehaviour
{
    private PrototypeMajorBattalion09F18 major;
    private PrototypeMajorUi09F18 ui;

    private FieldInfo pendingOrderField;
    private FieldInfo lastOrderField;
    private FieldInfo lastOrderPointField;
    private FieldInfo nextMajorThinkField;

    private MajorOrder09F18 previousPending = MajorOrder09F18.None;
    private MajorOrder09F18 previousObservedOrder = MajorOrder09F18.None;
    private MajorOrder09F18 explicitOrder = MajorOrder09F18.None;
    private Vector3 explicitPoint;
    private bool hasExplicitIntent;

    private bool doctrineInitialized;
    private OfficerAIDoctrine previousDoctrine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorIntentAuthority09F20>() == null)
            new GameObject("PrototypeMajorIntentAuthority_v000009f20").AddComponent<PrototypeMajorIntentAuthority09F20>();
    }

    private void Update()
    {
        if (!Resolve())
            return;

        MajorOrder09F18 pending = (MajorOrder09F18)pendingOrderField.GetValue(ui);
        MajorOrder09F18 observedOrder = (MajorOrder09F18)lastOrderField.GetValue(major);
        Vector3 observedPoint = (Vector3)lastOrderPointField.GetValue(major);

        // Targeted Major buttons go Pending -> None when the player clicks the battlefield.
        if (previousPending != MajorOrder09F18.None &&
            pending == MajorOrder09F18.None &&
            observedOrder == previousPending)
        {
            CaptureExplicitIntent(observedOrder, observedPoint, "TARGET_ORDER");
        }

        // HOLD POSITION has no target-picking phase, so capture it from the order transition.
        if (observedOrder == MajorOrder09F18.HoldPosition &&
            previousObservedOrder != MajorOrder09F18.HoldPosition &&
            major.Selected)
        {
            CaptureExplicitIntent(observedOrder, observedPoint, "HOLD_ORDER");
        }

        if (!doctrineInitialized)
        {
            previousDoctrine = major.Doctrine;
            doctrineInitialized = true;
        }
        else if (major.Doctrine != previousDoctrine)
        {
            OfficerAIDoctrine oldDoctrine = previousDoctrine;
            previousDoctrine = major.Doctrine;

            if (hasExplicitIntent && major.AIEnabled)
            {
                // Re-issue the same commander's mission so the new doctrine affects execution,
                // reserve choice and subordinate behavior without changing the objective.
                major.IssueOrder(explicitOrder, explicitPoint, false);
                SuppressAutonomousReplacement();

                Debug.Log("MAJOR-INTENT-09F20|DoctrineChanged=True|From=" + oldDoctrine +
                          "|To=" + major.Doctrine +
                          "|MissionRetained=" + explicitOrder +
                          "|Objective=" + explicitPoint.x.ToString("0") + "," + explicitPoint.z.ToString("0"));
            }
        }

        if (hasExplicitIntent && major.AIEnabled)
            SuppressAutonomousReplacement();

        previousPending = pending;
        previousObservedOrder = observedOrder;
    }

    private bool Resolve()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (ui == null)
            ui = PrototypeMajorUi09F18.Instance;

        if (major == null || ui == null || !major.Installed)
            return false;

        if (pendingOrderField == null)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            pendingOrderField = typeof(PrototypeMajorUi09F18).GetField("pendingOrder", flags);
            lastOrderField = typeof(PrototypeMajorBattalion09F18).GetField("lastOrder", flags);
            lastOrderPointField = typeof(PrototypeMajorBattalion09F18).GetField("lastOrderPoint", flags);
            nextMajorThinkField = typeof(PrototypeMajorBattalion09F18).GetField("nextMajorThink", flags);
        }

        return pendingOrderField != null &&
               lastOrderField != null &&
               lastOrderPointField != null &&
               nextMajorThinkField != null;
    }

    private void CaptureExplicitIntent(MajorOrder09F18 order, Vector3 point, string source)
    {
        explicitOrder = order;
        explicitPoint = point;
        hasExplicitIntent = order != MajorOrder09F18.None;

        if (hasExplicitIntent && major.AIEnabled)
            SuppressAutonomousReplacement();

        Debug.Log("MAJOR-INTENT-09F20|Captured=True|Source=" + source +
                  "|Order=" + order +
                  "|Objective=" + point.x.ToString("0") + "," + point.z.ToString("0") +
                  "|MajorAI=" + (major.AIEnabled ? "ON" : "OFF"));
    }

    private void SuppressAutonomousReplacement()
    {
        // Reserve/flank reviews continue to run. Only the higher-level ThinkMajor mission
        // replacement is held while a player-issued mission is active.
        nextMajorThinkField.SetValue(major, Time.time + 3600f);
    }
}
