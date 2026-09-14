using UnityEngine;

// v00.00.09f23 authority correction.
// A direct player company order MUST detach the selected company from the Major and
// switch Officer AI OFF before PlayerCommander consumes the same input frame.
//
// Important: the old v00.00.09f18 execution order was +250, which meant the default
// PlayerCommander Update could consume RMB/key input first. That allowed a supposed
// manual order to be interpreted while Officer AI/Major authority was still active.
// f23 runs this layer well before normal input/order processing.
[DefaultExecutionOrder(-1200)]
public sealed class PrototypeManualOrderAuthority09F18 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeManualOrderAuthority09F18>() != null)
            return;

        GameObject root = new GameObject("PrototypeManualOrderAuthority_v000009f23");
        root.AddComponent<PrototypeManualOrderAuthority09F18>();
    }

    private void Update()
    {
        bool directOrder = Input.GetMouseButtonDown(1) ||
                           Input.GetKeyDown(KeyCode.H) ||
                           Input.GetKeyDown(KeyCode.F) ||
                           Input.GetKeyDown(KeyCode.C) ||
                           Input.GetKeyDown(KeyCode.Z) ||
                           Input.GetKeyDown(KeyCode.X);

        if (!directOrder)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        if (battle.IsPointerOverSimulationControls(Input.mousePosition))
            return;

        int released = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected)
                continue;

            // Release Major authority first. ReleaseCompanyToManual removes the live
            // Major mission, clears the HQ mission visual and switches Officer AI OFF.
            PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
            if (major != null)
                major.ReleaseCompanyToManual(regiment);

            // Keep this explicit as a safety net if the Major layer is not installed yet.
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            released++;
        }

        if (released > 0)
        {
            Debug.Log("AI-AUTHORITY-09F23|Source=PLAYER_COMPANY_ORDER|Companies=" + released +
                      "|AI=OFF|MajorMissionDetached=True|BeforePlayerCommander=True");
        }
    }
}
