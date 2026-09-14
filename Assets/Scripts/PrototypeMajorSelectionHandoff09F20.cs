using UnityEngine;

// v00.00.09f20
// Ensures that company click/box selection can always take selection authority away from the Major.
// Runs after PlayerCommander, box selection and Major UI have processed the frame.
[DefaultExecutionOrder(520)]
public sealed class PrototypeMajorSelectionHandoff09F20 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorSelectionHandoff09F20>() == null)
            new GameObject("PrototypeMajorSelectionHandoff_v000009f20").AddComponent<PrototypeMajorSelectionHandoff09F20>();
    }

    private void Update()
    {
        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed || !major.Selected)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        // If normal unit selection succeeded this frame, Major selection must yield immediately.
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark)
                continue;

            if (!regiment.IsSelected)
                continue;

            major.SetSelected(false);
            Debug.Log("SELECTION-09F20|Authority=COMPANY|MajorDeselected=True|Unit=" + regiment.RegimentName);
            return;
        }
    }
}
