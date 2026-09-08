using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tactical QA guard for v00.00.09/v00.00.13 integration.
// COLUMN is allowed while a regiment is manoeuvring. Once movement has actually
// ended, the prototype's route contract expects the regiment to deploy back to
// LINE. This guard covers both Officer AI and direct/manual control so a unit
// cannot remain visually stuck in marching column after arrival.
[DefaultExecutionOrder(1800)]
public sealed class PrototypeAIFormationDeploymentGuard : MonoBehaviour
{
    private sealed class FormationState
    {
        public bool Initialized;
        public Vector3 LastPosition;
        public float StationarySeconds;
        public bool MovedSinceLastDeployment;
    }

    private readonly Dictionary<Regiment, FormationState> states =
        new Dictionary<Regiment, FormationState>();

    private static readonly FieldInfo HasDestinationField =
        typeof(Regiment).GetField("hasDestination", BindingFlags.Instance | BindingFlags.NonPublic);

    private const float MovementEpsilon = 0.025f;
    private const float DeployDelaySeconds = 0.65f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "PrototypeBattle", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<PrototypeAIFormationDeploymentGuard>() != null)
            return;

        GameObject root = new GameObject("PrototypeAIFormationDeploymentGuard_v009");
        root.AddComponent<PrototypeAIFormationDeploymentGuard>();
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
            {
                if (regiment != null)
                    states.Remove(regiment);
                continue;
            }

            if (!states.TryGetValue(regiment, out FormationState state))
            {
                state = new FormationState();
                states[regiment] = state;
            }

            Vector3 current = regiment.transform.position;
            current.y = 0f;

            if (!state.Initialized)
            {
                state.Initialized = true;
                state.LastPosition = current;
                state.StationarySeconds = 0f;
                state.MovedSinceLastDeployment = false;
                continue;
            }

            float moved = Vector3.Distance(current, state.LastPosition);
            state.LastPosition = current;

            if (moved > MovementEpsilon)
            {
                state.StationarySeconds = 0f;
                state.MovedSinceLastDeployment = true;
            }
            else
            {
                state.StationarySeconds += Time.deltaTime;
            }

            if (regiment.Formation != RegimentFormation.Column)
                continue;

            if (state.StationarySeconds < DeployDelaySeconds)
                continue;

            OfficerAIController officerAI = regiment.GetComponent<OfficerAIController>();
            bool aiEnabled = officerAI != null && officerAI.AIEnabled;

            bool shouldDeploy = aiEnabled
                ? ShouldDeployAIToLine(officerAI)
                : ShouldDeployManualToLine(regiment, state);

            if (!shouldDeploy)
                continue;

            regiment.SetFormation(RegimentFormation.Line);
            state.StationarySeconds = 0f;
            state.MovedSinceLastDeployment = false;

            Debug.Log(string.Format(
                "FORMATION-GUARD|Unit={0}|ColumnToLine=True|Control={1}|Mission={2}|Task={3}|Reason=MovementEnded",
                regiment.RegimentName,
                aiEnabled ? "AI" : "MANUAL",
                officerAI != null ? officerAI.Mission.ToString() : "N/A",
                officerAI != null ? officerAI.CurrentTask : "N/A"));
        }
    }

    private static bool ShouldDeployManualToLine(Regiment regiment, FormationState state)
    {
        if (regiment == null || state == null || !state.MovedSinceLastDeployment)
            return false;

        // Do not override COLUMN while the regiment still has an active destination.
        // This preserves temporary obstacle/bridge column movement. Once the route
        // has ended, the player route contract's final formation is LINE.
        return !HasActiveDestination(regiment);
    }

    private static bool ShouldDeployAIToLine(OfficerAIController officerAI)
    {
        if (officerAI == null)
            return false;

        if (officerAI.Mission == OfficerAIMission.Hold ||
            officerAI.Mission == OfficerAIMission.DefendArea)
        {
            return true;
        }

        string task = officerAI.CurrentTask ?? string.Empty;
        return task.StartsWith("ASSESS", StringComparison.OrdinalIgnoreCase) ||
               task.StartsWith("DEFEND", StringComparison.OrdinalIgnoreCase) ||
               task.StartsWith("HOLD", StringComparison.OrdinalIgnoreCase) ||
               task.StartsWith("ENGAGE", StringComparison.OrdinalIgnoreCase) ||
               task.StartsWith("STABILISE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasActiveDestination(Regiment regiment)
    {
        if (regiment == null || HasDestinationField == null)
            return false;

        object value = HasDestinationField.GetValue(regiment);
        return value is bool active && active;
    }
}
