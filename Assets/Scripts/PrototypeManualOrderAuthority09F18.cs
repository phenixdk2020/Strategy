using UnityEngine;

// v00.00.09f18: one AI authority rule for direct company control.
// A direct player company order turns that company's OfficerAIController OFF before
// PlayerCommander consumes the input. A later Major order explicitly turns it ON again.
[DefaultExecutionOrder(250)]
public sealed class PrototypeManualOrderAuthority09F18 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeManualOrderAuthority09F18>() != null)
            return;

        GameObject root = new GameObject("PrototypeManualOrderAuthority_v000009f18");
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

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
            if (major != null)
                major.ReleaseCompanyToManual(regiment);

            released++;
        }

        if (released > 0)
        {
            Debug.Log("AI-AUTHORITY-09F18|Source=PLAYER_COMPANY_ORDER|Companies=" + released +
                      "|AI=OFF|MajorMissionDetached=True");
        }
    }
}
