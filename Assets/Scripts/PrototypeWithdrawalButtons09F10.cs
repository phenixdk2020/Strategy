using UnityEngine;

// v00.00.09f10 companion controls drawn inside the existing bottom command bar.
// The base 09f2 bar already owns pointer isolation for the entire 64 px bottom panel.
[DefaultExecutionOrder(3250)]
public sealed class PrototypeWithdrawalButtons09F10 : MonoBehaviour
{
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeWithdrawalButtons09F10>() != null)
            return;

        GameObject root = new GameObject("PrototypeWithdrawalButtons_v000009f10");
        root.AddComponent<PrototypeWithdrawalButtons09F10>();
    }

    private void EnsureStyle()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 9;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (PrototypeFightingWithdrawal09F10.Instance == null || GetSelectedDanishCount() == 0)
            return;

        EnsureStyle();
        GUI.depth = -200;

        // Existing second row starts at x=5 with aggression (210 px) and then four
        // 65 px fire buttons. Start immediately after those controls.
        float x = 5f + 210f + (65f * 4f) + 8f;
        float y = Screen.height - 64f + 33f;
        const float h = 27f;

        GUI.Label(new Rect(x, y, 46f, h), "TRÆK", labelStyle);
        x += 48f;

        if (GUI.Button(new Rect(x, y, 78f, h), "→ MED"))
            PrototypeFightingWithdrawal09F10.Instance.BeginForSelected(PrototypeWithdrawalRange09F10.Medium);
        x += 81f;

        if (GUI.Button(new Rect(x, y, 78f, h), "→ LONG"))
            PrototypeFightingWithdrawal09F10.Instance.BeginForSelected(PrototypeWithdrawalRange09F10.Long);
        x += 81f;

        if (GUI.Button(new Rect(x, y, 78f, h), "→ UD"))
            PrototypeFightingWithdrawal09F10.Instance.BeginForSelected(PrototypeWithdrawalRange09F10.OutOfRange);
    }

    private static int GetSelectedDanishCount()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return 0;

        int count = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null &&
                regiment.Team == BattleTeam.Denmark &&
                regiment.IsSelected &&
                !regiment.IsRouted)
            {
                count++;
            }
        }
        return count;
    }
}
