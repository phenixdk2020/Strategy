using System.Collections.Generic;
using UnityEngine;

// v00.00.09i2 TEST - tactical visual readability supplement.
// Presentation-only. Makes the 09i procedural regimental standard readable from the
// normal tactical camera and reports clearly if the 09i visual layers failed to install.
[DefaultExecutionOrder(11950)]
public sealed class PrototypeVisualReadability09I2 : MonoBehaviour
{
    private sealed class FlagState
    {
        public Transform Root;
        public Vector3 BaseScale;
        public float AppliedScale = -1f;
        public bool MissingLogged;
    }

    private readonly Dictionary<Regiment, FlagState> flags =
        new Dictionary<Regiment, FlagState>();

    private Camera cam;
    private float nextRefresh;
    private bool announced;

    private const float NearDistance = 70f;
    private const float FarDistance = 300f;
    private const float NearScale = 1.18f;
    private const float FarScale = 1.52f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeVisualReadability09I2>() != null)
            return;

        GameObject root = new GameObject("PrototypeVisualReadability_v000009i2");
        root.AddComponent<PrototypeVisualReadability09I2>();
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
            return;
        nextRefresh = Time.unscaledTime + 0.25f;

        if (cam == null)
            cam = Camera.main;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "VIS-09I2|Installed=True|SoldierMediumZoomReadability=True|" +
                "FlagDynamicScale=True|SimulationWrites=False");
        }

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            FlagState state;
            if (!flags.TryGetValue(regiment, out state))
            {
                state = new FlagState();
                flags[regiment] = state;
            }

            if (state.Root == null)
            {
                Transform candidate = regiment.transform.Find(
                    "RegimentalStandard09I_" + regiment.RegimentName);
                if (candidate == null)
                {
                    if (!state.MissingLogged && Time.unscaledTime > 2f)
                    {
                        state.MissingLogged = true;
                        Debug.LogWarning(
                            "VIS-09I2|Unit=" + regiment.RegimentName +
                            "|Standard09I=False|Reason=ProceduralStandardNotFound");
                    }
                    continue;
                }

                state.Root = candidate;
                state.BaseScale = candidate.localScale;
                if (state.BaseScale.sqrMagnitude < 0.01f)
                    state.BaseScale = Vector3.one;

                Debug.Log(
                    "VIS-09I2|Unit=" + regiment.RegimentName +
                    "|Standard09I=True|ReadabilityScale=True");
            }

            if (cam == null)
                continue;

            float distance = Vector3.Distance(cam.transform.position, regiment.transform.position);
            float t = Mathf.InverseLerp(NearDistance, FarDistance, distance);
            float scale = Mathf.Lerp(NearScale, FarScale, t);
            if (Mathf.Abs(scale - state.AppliedScale) < 0.02f)
                continue;

            state.AppliedScale = scale;
            state.Root.localScale = state.BaseScale * scale;
        }

        CleanupDestroyed();
    }

    private void CleanupDestroyed()
    {
        if (flags.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, FlagState> pair in flags)
        {
            if (pair.Key != null)
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
            flags.Remove(stale[i]);
    }
}
