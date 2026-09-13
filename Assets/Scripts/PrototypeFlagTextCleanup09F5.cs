using UnityEngine;

// v00.00.09f5: flags remain visible, but no TextMesh labels are allowed to float over formations.
[DefaultExecutionOrder(30000)]
public sealed class PrototypeFlagTextCleanup09F5 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFlagTextCleanup09F5>() != null)
            return;

        GameObject root = new GameObject("PrototypeFlagTextCleanup_v000009f5");
        root.AddComponent<PrototypeFlagTextCleanup09F5>();
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

            TextMesh[] texts = regiment.GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.activeSelf)
                    texts[i].gameObject.SetActive(false);
            }
        }
    }
}
