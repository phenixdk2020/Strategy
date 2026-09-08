using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tactical QA guard for v00.00.09/v00.00.13 integration.
// Officer AI is allowed to use COLUMN while manoeuvring, but a regiment must not
// remain stuck in COLUMN after the manoeuvre has ended and the officer is
// assessing, defending, holding or engaging.
[DefaultExecutionOrder(1800)]
public sealed class PrototypeAIFormationDeploymentGuard : MonoBehaviour
{
    private sealed class FormationState
    {
        public bool Initialized;
        public Vector3 LastPosition;
        public float StationarySeconds;
    }

    private readonly Dictionary<Regiment, FormationState> states =
        new Dictionary<Regiment, FormationState>();

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
            if (regiment == null)
                continue;

            OfficerAIController officerAI = regiment.GetComponent<OfficerAIController>();
            if (officerAI == null || !officerAI.AIEnabled || regiment.IsRouted)
            {
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
                continue;
            }

            float moved = Vector3.Distance(current, state.LastPosition);
            state.LastPosition = current;

            if (moved > MovementEpsilon)
                state.StationarySeconds = 0f;
            else
                state.StationarySeconds += Time.deltaTime;

            if (regiment.Formation != RegimentFormation.Column)
                continue;

            if (state.StationarySeconds < DeployDelaySeconds)
                continue;

            if (!ShouldDeployToLine(officerAI))
                continue;

            regiment.SetFormation(RegimentFormation.Line);
            state.StationarySeconds = 0f;

            Debug.Log(string.Format(
                "FORMATION-GUARD|Unit={0}|ColumnToLine=True|Mission={1}|Task={2}|Reason=ManoeuvreEnded",
                regiment.RegimentName,
                officerAI.Mission,
                officerAI.CurrentTask));
        }
    }

    private static bool ShouldDeployToLine(OfficerAIController officerAI)
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
}
