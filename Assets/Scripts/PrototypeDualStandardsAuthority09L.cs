using UnityEngine;

// v00.00.09l - authoritative flag-layer switchboard.
// Runs after 09g/09i standard systems so only the new National + Regimental pair remains visible.
[DefaultExecutionOrder(13000)]
public sealed class PrototypeDualStandardsAuthority09L : MonoBehaviour
{
    private float nextSweep;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeDualStandardsAuthority09L>() != null)
            return;

        GameObject root = new GameObject("PrototypeDualStandardsAuthority_v000009l");
        root.AddComponent<PrototypeDualStandardsAuthority09L>();
    }

    private void Update()
    {
        PrototypeRegimentalStandards old09g = Object.FindAnyObjectByType<PrototypeRegimentalStandards>();
        if (old09g != null && old09g.enabled)
            old09g.enabled = false;

        PrototypeRegimentalStandards09I old09i = Object.FindAnyObjectByType<PrototypeRegimentalStandards09I>();
        if (old09i != null && old09i.enabled)
            old09i.enabled = false;

        if (Time.unscaledTime < nextSweep)
            return;

        nextSweep = Time.unscaledTime + 0.50f;
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        int hiddenRoots = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            for (int i = 0; i < regiment.transform.childCount; i++)
            {
                Transform child = regiment.transform.GetChild(i);
                if (child == null || child.name == "DualStandards09L")
                    continue;

                bool legacy =
                    child.name.StartsWith("RegimentalStandard_") ||
                    child.name.StartsWith("RegimentalStandard09I_");

                if (!legacy)
                    continue;

                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                    renderers[r].enabled = false;
                hiddenRoots++;
            }
        }

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "STANDARD-AUTH-09L|DualStandardsAuthoritative=True|09GDisabled=" + (old09g != null) +
                "|09IDisabled=" + (old09i != null) +
                "|LegacyRootsHidden=" + hiddenRoots);
        }
    }
}
