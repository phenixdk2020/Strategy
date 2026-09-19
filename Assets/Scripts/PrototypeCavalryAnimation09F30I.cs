using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30i
// Lightweight procedural cavalry animation: visible mount/dismount transitions and
// a simple riding gait. Simulation authority remains in PrototypeCavalryUnit09F30.
[DefaultExecutionOrder(43400)]
public sealed class PrototypeCavalryAnimation09F30I : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DismountDuration = 2.25f;
    private const float RemountDuration = 4.50f;

    private sealed class TransitionState
    {
        public PrototypeCavalryUnit09F30 Unit;
        public bool Dismount;
        public float Started;
        public List<GameObject> Riders = new List<GameObject>();
        public List<Vector3> RiderScales = new List<Vector3>();
        public List<Transform> Feet = new List<Transform>();
        public List<Vector3> FootScales = new List<Vector3>();
        public List<Vector3> FootStartPositions = new List<Vector3>();
        public List<Vector3> FootRemountPositions = new List<Vector3>();
        public Transform FootRoot;
    }

    private static PrototypeCavalryAnimation09F30I instance;
    private readonly Dictionary<PrototypeCavalryUnit09F30, TransitionState> transitions =
        new Dictionary<PrototypeCavalryUnit09F30, TransitionState>();
    private readonly Dictionary<PrototypeCavalryUnit09F30, List<Transform>> mountedCache =
        new Dictionary<PrototypeCavalryUnit09F30, List<Transform>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        GetOrCreate();
    }

    private static PrototypeCavalryAnimation09F30I GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryAnimation09F30I>();
        if (instance == null)
            instance = new GameObject("PrototypeCavalryAnimation_v000009f30i")
                .AddComponent<PrototypeCavalryAnimation09F30I>();
        return instance;
    }

    public static bool IsTransitioning(PrototypeCavalryUnit09F30 unit)
    {
        return unit != null && GetOrCreate().transitions.ContainsKey(unit);
    }

    public static void BeginDismount(PrototypeCavalryUnit09F30 unit)
    {
        GetOrCreate().Begin(unit, true);
    }

    public static void BeginRemount(PrototypeCavalryUnit09F30 unit)
    {
        GetOrCreate().Begin(unit, false);
    }

    private void Begin(PrototypeCavalryUnit09F30 unit, bool dismount)
    {
        if (unit == null || transitions.ContainsKey(unit))
            return;

        TransitionState state = Capture(unit, dismount);
        if (state == null)
            return;

        state.Started = Time.time;
        transitions[unit] = state;

        for (int i = 0; i < state.Riders.Count; i++)
        {
            if (state.Riders[i] == null) continue;
            state.Riders[i].SetActive(true);
            if (!dismount)
                state.Riders[i].transform.localScale = state.RiderScales[i] * 0.08f;
        }

        if (state.FootRoot != null)
            state.FootRoot.gameObject.SetActive(true);

        for (int i = 0; i < state.Feet.Count; i++)
        {
            if (state.Feet[i] == null) continue;
            if (dismount)
                state.Feet[i].localScale = state.FootScales[i] * 0.08f;
        }
    }

    private static TransitionState Capture(PrototypeCavalryUnit09F30 unit, bool dismount)
    {
        FieldInfo ridersField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRiders", PrivateInstance);
        FieldInfo feetField = typeof(PrototypeCavalryUnit09F30).GetField("footFigures", PrivateInstance);
        FieldInfo footRootField = typeof(PrototypeCavalryUnit09F30).GetField("footRoot", PrivateInstance);

        List<GameObject> riders = ridersField != null ? ridersField.GetValue(unit) as List<GameObject> : null;
        List<Transform> feet = feetField != null ? feetField.GetValue(unit) as List<Transform> : null;
        Transform footRoot = footRootField != null ? footRootField.GetValue(unit) as Transform : null;
        if (riders == null || feet == null)
            return null;

        TransitionState state = new TransitionState
        {
            Unit = unit,
            Dismount = dismount,
            FootRoot = footRoot
        };

        for (int i = 0; i < riders.Count; i++)
        {
            GameObject rider = riders[i];
            if (rider == null) continue;
            state.Riders.Add(rider);
            state.RiderScales.Add(rider.transform.localScale);
        }

        for (int i = 0; i < feet.Count; i++)
        {
            Transform foot = feet[i];
            if (foot == null) continue;
            state.Feet.Add(foot);
            state.FootScales.Add(foot.localScale);
            state.FootStartPositions.Add(foot.localPosition);

            int slot = state.Feet.Count - 1;
            int columns = 8;
            int row = slot / columns;
            int col = slot % columns;
            state.FootRemountPositions.Add(new Vector3(
                (col - (columns - 1) * 0.5f) * 0.74f,
                0f,
                -2.0f - row * 0.78f));
        }

        return state;
    }

    private void Update()
    {
        UpdateTransitions();
        UpdateGait();
    }

    private void UpdateTransitions()
    {
        if (transitions.Count == 0)
            return;

        List<PrototypeCavalryUnit09F30> completed = null;
        foreach (KeyValuePair<PrototypeCavalryUnit09F30, TransitionState> pair in transitions)
        {
            TransitionState s = pair.Value;
            if (s == null || s.Unit == null)
            {
                if (completed == null) completed = new List<PrototypeCavalryUnit09F30>();
                completed.Add(pair.Key);
                continue;
            }

            float duration = s.Dismount ? DismountDuration : RemountDuration;
            float p = Mathf.Clamp01((Time.time - s.Started) / duration);
            float smooth = p * p * (3f - 2f * p);

            for (int i = 0; i < s.Riders.Count; i++)
            {
                if (s.Riders[i] == null) continue;
                float scale = s.Dismount ? Mathf.Lerp(1f, 0.08f, smooth) : Mathf.Lerp(0.08f, 1f, smooth);
                s.Riders[i].transform.localScale = s.RiderScales[i] * scale;
                s.Riders[i].transform.localRotation =
                    Quaternion.Euler(0f, 0f, (s.Dismount ? 1f : -1f) * 18f * Mathf.Sin(p * Mathf.PI));
            }

            for (int i = 0; i < s.Feet.Count; i++)
            {
                if (s.Feet[i] == null) continue;
                float scale = s.Dismount ? Mathf.Lerp(0.08f, 1f, smooth) : Mathf.Lerp(1f, 0.08f, smooth);
                s.Feet[i].localScale = s.FootScales[i] * scale;

                if (!s.Dismount)
                    s.Feet[i].localPosition = Vector3.Lerp(
                        s.FootStartPositions[i], s.FootRemountPositions[i], smooth);
            }

            if (p < 1f)
                continue;

            for (int i = 0; i < s.Riders.Count; i++)
            {
                if (s.Riders[i] == null) continue;
                s.Riders[i].transform.localScale = s.RiderScales[i];
                s.Riders[i].transform.localRotation = Quaternion.identity;
                s.Riders[i].SetActive(!s.Dismount);
            }

            for (int i = 0; i < s.Feet.Count; i++)
                if (s.Feet[i] != null)
                    s.Feet[i].localScale = s.FootScales[i];

            if (!s.Dismount && s.FootRoot != null)
                s.FootRoot.gameObject.SetActive(false);

            if (completed == null) completed = new List<PrototypeCavalryUnit09F30>();
            completed.Add(pair.Key);

            Debug.Log("CAV-ANIM-09F30I|Unit=" + s.Unit.UnitName +
                      "|Transition=" + (s.Dismount ? "DISMOUNT" : "REMOUNT") + "|Complete=True");
        }

        if (completed != null)
            for (int i = 0; i < completed.Count; i++)
                transitions.Remove(completed[i]);
    }

    private void UpdateGait()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed)
            return;

        ApplyGait(cavalry.Gardehusar);
        ApplyGait(cavalry.Dragon);
    }

    private void ApplyGait(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || unit.Mode != PrototypeCavalryMode09F30.Mounted)
            return;

        List<Transform> figures;
        if (!mountedCache.TryGetValue(unit, out figures) || figures == null)
        {
            FieldInfo field = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
            figures = field != null ? field.GetValue(unit) as List<Transform> : null;
            mountedCache[unit] = figures;
        }
        if (figures == null)
            return;

        bool moving = unit.Action != PrototypeCavalryAction09F30.Hold || unit.HasDestination;
        float amp = unit.Action == PrototypeCavalryAction09F30.Charge ? 5.0f : 2.6f;
        float freq = unit.Action == PrototypeCavalryAction09F30.Charge ? 10.5f : 6.5f;

        for (int i = 0; i < figures.Count; i++)
        {
            Transform f = figures[i];
            if (f == null) continue;
            if (!moving)
            {
                f.localRotation = Quaternion.RotateTowards(f.localRotation, Quaternion.identity, 120f * Time.deltaTime);
                continue;
            }

            float phase = Time.time * freq + i * 0.73f;
            float pitch = Mathf.Sin(phase) * amp;
            float roll = Mathf.Sin(phase * 0.5f) * amp * 0.32f;
            f.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
