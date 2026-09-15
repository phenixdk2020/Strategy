using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29l
// Full visual Square reform layer.
// F29 already had Square combat state and a target outline, but the normal line renderer
// continued pulling displayed soldiers back toward Line every frame. F29L becomes the
// final visual writer while Square is active: displayed men visibly leave Line/Column,
// interpolate to a hollow two-rank square over the actual reform time, and the square
// footprint/collider scales with surviving company strength.
[DefaultExecutionOrder(38000)]
public sealed class PrototypeSquareVisual09F29L : MonoBehaviour
{
    private sealed class Transition
    {
        public float StartedAt;
        public float Duration;
        public int LastCount;
        public int LastStrength;
        public bool CompletionLogged;
        public readonly List<Vector3> StartPositions = new List<Vector3>();
    }

    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly Dictionary<Regiment, Transition> transitions = new Dictionary<Regiment, Transition>();
    private readonly HashSet<Regiment> active = new HashSet<Regiment>();

    private FieldInfo squareDictionaryField;
    private FieldInfo visualStatesField;
    private PrototypeInfantrySquare09F29 squareManager;
    private PrototypeBattleVisuals09F5 visualManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareVisual09F29L>() == null)
            new GameObject("PrototypeSquareVisual_v000009f29l").AddComponent<PrototypeSquareVisual09F29L>();
    }

    private void Awake()
    {
        squareDictionaryField = typeof(PrototypeInfantrySquare09F29).GetField("square", AnyInstance);
        visualStatesField = typeof(PrototypeBattleVisuals09F5).GetField("states", AnyInstance);

        Debug.Log(
            "SQUARE-VISUAL-09F29L|Installed=True|PhysicalVisualReform=True|HollowSquare=True|" +
            "TwoRanks=True|StrengthScaled=True|ReformTimeSynchronized=True|ColliderScaled=True");
    }

    private void Update()
    {
        ResolveManagers();
        if (squareManager == null || visualManager == null || squareDictionaryField == null || visualStatesField == null)
            return;

        IDictionary squareStates = squareDictionaryField.GetValue(squareManager) as IDictionary;
        IDictionary visualStates = visualStatesField.GetValue(visualManager) as IDictionary;
        if (squareStates == null || visualStates == null)
            return;

        active.Clear();

        foreach (DictionaryEntry entry in squareStates)
        {
            Regiment unit = entry.Key as Regiment;
            object squareState = entry.Value;
            if (unit == null || squareState == null || unit.CurrentStrength <= 0 || !visualStates.Contains(unit))
                continue;

            object visualState = visualStates[unit];
            if (visualState == null)
                continue;

            FieldInfo positionsField = visualState.GetType().GetField("LocalPositions", AnyInstance);
            IList positions = positionsField != null ? positionsField.GetValue(visualState) as IList : null;
            if (positions == null || positions.Count == 0)
                continue;

            FieldInfo readyAtField = squareState.GetType().GetField("ReadyAt", AnyInstance);
            FieldInfo readyField = squareState.GetType().GetField("Ready", AnyInstance);
            float readyAt = readyAtField != null && readyAtField.GetValue(squareState) is float
                ? (float)readyAtField.GetValue(squareState)
                : Time.time + 1f;
            bool ready = readyField != null && readyField.GetValue(squareState) is bool && (bool)readyField.GetValue(squareState);

            active.Add(unit);

            if (!transitions.TryGetValue(unit, out Transition transition))
            {
                transition = CreateTransition(positions, readyAt, ready, unit.CurrentStrength);
                transitions[unit] = transition;
                Debug.Log("SQUARE-VISUAL-09F29L|Unit=" + unit.RegimentName +
                          "|State=VISUAL_REFORM_START|Men=" + unit.CurrentStrength +
                          "|Duration=" + transition.Duration.ToString("0.0"));
            }
            else if (transition.LastCount != positions.Count ||
                     (ready && transition.LastStrength != unit.CurrentStrength))
            {
                ResetTransition(transition, positions, ready ? 0.90f : Mathf.Max(0.45f, readyAt - Time.time), unit.CurrentStrength);
            }

            ApplyTransition(unit, positions, transition);
            ScaleCollider(unit);
        }

        CleanupTransitions();
    }

    private void ResolveManagers()
    {
        if (squareManager == null)
            squareManager = PrototypeInfantrySquare09F29.Instance;
        if (visualManager == null)
            visualManager = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F5>();
    }

    private static Transition CreateTransition(IList positions, float readyAt, bool ready, int strength)
    {
        Transition transition = new Transition();
        float duration = ready ? 0.35f : Mathf.Max(0.80f, readyAt - Time.time);
        ResetTransition(transition, positions, duration, strength);
        return transition;
    }

    private static void ResetTransition(Transition transition, IList positions, float duration, int strength)
    {
        transition.StartedAt = Time.time;
        transition.Duration = Mathf.Max(0.25f, duration);
        transition.LastCount = positions != null ? positions.Count : 0;
        transition.LastStrength = strength;
        transition.CompletionLogged = false;
        transition.StartPositions.Clear();

        if (positions == null)
            return;
        for (int i = 0; i < positions.Count; i++)
            transition.StartPositions.Add(positions[i] is Vector3 ? (Vector3)positions[i] : Vector3.zero);
    }

    private static void ApplyTransition(Regiment unit, IList positions, Transition transition)
    {
        if (unit == null || positions == null || transition == null)
            return;

        float raw = Mathf.Clamp01((Time.time - transition.StartedAt) / Mathf.Max(0.01f, transition.Duration));
        float smooth = raw * raw * (3f - 2f * raw);
        float halfSide = GetSquareHalfSide(unit.CurrentStrength);

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 start = i < transition.StartPositions.Count
                ? transition.StartPositions[i]
                : (positions[i] is Vector3 ? (Vector3)positions[i] : Vector3.zero);
            Vector3 target = SquarePosition(i, positions.Count, halfSide);
            positions[i] = Vector3.Lerp(start, target, smooth);
        }

        if (!transition.CompletionLogged && raw >= 0.995f)
        {
            transition.CompletionLogged = true;
            Debug.Log("SQUARE-VISUAL-09F29L|Unit=" + unit.RegimentName +
                      "|State=VISUAL_SQUARE_COMPLETE|Men=" + unit.CurrentStrength +
                      "|Side=" + (halfSide * 2f).ToString("0.0") + "m");
        }
    }

    private static float GetSquareHalfSide(int strength)
    {
        int perSidePerRank = Mathf.CeilToInt(Mathf.Max(1, strength) / 8f);
        float sideLength = Mathf.Max(7.5f, (perSidePerRank - 1) * 0.75f);
        return Mathf.Clamp(sideLength * 0.5f, 4.0f, 10.5f);
    }

    private static Vector3 SquarePosition(int index, int total, float halfSide)
    {
        int rank = index & 1;
        int perimeterIndex = index / 2;
        int perimeterCount = Mathf.Max(4, Mathf.CeilToInt(total / 2f));
        float u = (perimeterIndex / (float)perimeterCount) * 4f;
        int side = Mathf.Clamp(Mathf.FloorToInt(u), 0, 3);
        float t = u - side;
        float h = Mathf.Max(1.5f, halfSide - rank * 0.90f);

        switch (side)
        {
            case 0: return new Vector3(Mathf.Lerp(-h, h, t), 0f, h);
            case 1: return new Vector3(h, 0f, Mathf.Lerp(h, -h, t));
            case 2: return new Vector3(Mathf.Lerp(h, -h, t), 0f, -h);
            default: return new Vector3(-h, 0f, Mathf.Lerp(-h, h, t));
        }
    }

    private static void ScaleCollider(Regiment unit)
    {
        if (unit == null)
            return;
        BoxCollider box = unit.GetComponent<BoxCollider>();
        if (box == null)
            return;

        float halfSide = GetSquareHalfSide(unit.CurrentStrength);
        float side = halfSide * 2f + 1.8f;
        box.size = new Vector3(side, 2.2f, side);
    }

    private void CleanupTransitions()
    {
        if (transitions.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, Transition> pair in transitions)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;
        for (int i = 0; i < stale.Count; i++)
            transitions.Remove(stale[i]);
    }
}
