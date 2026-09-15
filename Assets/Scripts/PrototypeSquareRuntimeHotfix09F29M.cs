using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29m
// Square visual hotfix for the ACTIVE company renderer.
// Root cause of F29L QA failure: F29/F29L both reflected PrototypeBattleVisuals09F5,
// but PrototypeBattleVisuals09F7 disables F5 at runtime and owns the visible soldiers.
// F29M writes the active F7 LocalPositions after F7's normal Line/Column update, hides
// the normal line/column selection footprint while Square is active, and reapplies the
// strength-scaled Square collider after PrototypeUnitFootprint09F5 LateUpdate.
[DefaultExecutionOrder(39000)]
public sealed class PrototypeSquareRuntimeHotfix09F29M : MonoBehaviour
{
    private sealed class Transition
    {
        public float StartedAt;
        public float Duration;
        public int LastCount;
        public int LastStrength;
        public readonly List<Vector3> StartPositions = new List<Vector3>();
    }

    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly Dictionary<Regiment, Transition> transitions = new Dictionary<Regiment, Transition>();
    private readonly HashSet<Regiment> active = new HashSet<Regiment>();

    private PrototypeBattleVisuals09F7 rendererF7;
    private PrototypeInfantrySquare09F29 squareManager;
    private FieldInfo rendererStatesField;
    private FieldInfo squareStatesField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareRuntimeHotfix09F29M>() == null)
            new GameObject("PrototypeSquareRuntimeHotfix_v000009f29m")
                .AddComponent<PrototypeSquareRuntimeHotfix09F29M>();
    }

    private void Awake()
    {
        rendererStatesField = typeof(PrototypeBattleVisuals09F7).GetField("states", AnyInstance);
        squareStatesField = typeof(PrototypeInfantrySquare09F29).GetField("square", AnyInstance);

        PrototypeSquareVisual09F29L old =
            UnityEngine.Object.FindAnyObjectByType<PrototypeSquareVisual09F29L>();
        if (old != null)
            old.enabled = false;

        Debug.Log(
            "SQUARE-VISUAL-09F29M|Installed=True|Renderer=PrototypeBattleVisuals09F7|" +
            "LegacyF29LWriter=False|PhysicalReform=True|NormalFootprintHidden=True|StrengthScaled=True");
    }

    private void Update()
    {
        Resolve();
        if (rendererF7 == null || squareManager == null ||
            rendererStatesField == null || squareStatesField == null)
            return;

        IDictionary rendererStates = rendererStatesField.GetValue(rendererF7) as IDictionary;
        IDictionary squareStates = squareStatesField.GetValue(squareManager) as IDictionary;
        if (rendererStates == null || squareStates == null)
            return;

        active.Clear();

        foreach (DictionaryEntry entry in squareStates)
        {
            Regiment unit = entry.Key as Regiment;
            object squareState = entry.Value;
            if (unit == null || squareState == null || unit.CurrentStrength <= 0 || !rendererStates.Contains(unit))
                continue;

            object renderState = rendererStates[unit];
            if (renderState == null)
                continue;

            FieldInfo positionsField = renderState.GetType().GetField("LocalPositions", AnyInstance);
            IList positions = positionsField != null ? positionsField.GetValue(renderState) as IList : null;
            if (positions == null || positions.Count == 0)
                continue;

            active.Add(unit);

            float readyAt = ReadFloat(squareState, "ReadyAt", Time.time + 1f);
            bool ready = ReadBool(squareState, "Ready", false);

            if (!transitions.TryGetValue(unit, out Transition transition))
            {
                transition = new Transition();
                ResetTransition(transition, positions,
                    ready ? 0.30f : Mathf.Max(0.75f, readyAt - Time.time),
                    unit.CurrentStrength);
                transitions[unit] = transition;

                Debug.Log("SQUARE-VISUAL-09F29M|Unit=" + unit.RegimentName +
                          "|State=FORMING_VISUALLY|VisibleMen=" + positions.Count +
                          "|Strength=" + unit.CurrentStrength +
                          "|Duration=" + transition.Duration.ToString("0.0"));
            }
            else if (transition.LastCount != positions.Count || transition.LastStrength != unit.CurrentStrength)
            {
                ResetTransition(transition, positions,
                    ready ? 0.65f : Mathf.Max(0.45f, readyAt - Time.time),
                    unit.CurrentStrength);
            }

            ApplySquarePositions(unit, positions, transition);
        }

        CleanupTransitions();
    }

    private void LateUpdate()
    {
        // PrototypeUnitFootprint09F5 runs at 26000 and redraws the normal rectangular
        // Line/Column outline and collider. F29M runs later and replaces those visuals
        // for every company that is actually in Square state.
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            HideNormalSelectionFootprint(unit);
            ApplySquareCollider(unit);
        }
    }

    private void Resolve()
    {
        if (rendererF7 == null)
            rendererF7 = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (squareManager == null)
            squareManager = PrototypeInfantrySquare09F29.Instance;
    }

    private static void ResetTransition(Transition transition, IList positions, float duration, int strength)
    {
        transition.StartedAt = Time.time;
        transition.Duration = Mathf.Max(0.20f, duration);
        transition.LastCount = positions != null ? positions.Count : 0;
        transition.LastStrength = strength;
        transition.StartPositions.Clear();

        if (positions == null)
            return;

        for (int i = 0; i < positions.Count; i++)
            transition.StartPositions.Add(positions[i] is Vector3 ? (Vector3)positions[i] : Vector3.zero);
    }

    private static void ApplySquarePositions(Regiment unit, IList positions, Transition transition)
    {
        float raw = Mathf.Clamp01((Time.time - transition.StartedAt) /
                                  Mathf.Max(0.01f, transition.Duration));
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
    }

    private static float GetSquareHalfSide(int strength)
    {
        // Two ranks on four sides = roughly 8 men per one perimeter position step.
        // This mirrors the F29 square sizing rule, but now drives the visible F7 men.
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

    private static void HideNormalSelectionFootprint(Regiment unit)
    {
        Transform footprint = unit.transform.Find("SelectionFootprint09F5");
        if (footprint == null)
            return;

        LineRenderer line = footprint.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = false;
    }

    private static void ApplySquareCollider(Regiment unit)
    {
        BoxCollider box = unit.GetComponent<BoxCollider>();
        if (box == null)
            return;

        float halfSide = GetSquareHalfSide(unit.CurrentStrength);
        float side = halfSide * 2f + 1.8f;
        box.center = new Vector3(0f, 1.0f, 0f);
        box.size = new Vector3(side, 2.4f, side);
    }

    private static float ReadFloat(object instance, string name, float fallback)
    {
        if (instance == null)
            return fallback;
        FieldInfo field = instance.GetType().GetField(name, AnyInstance);
        object value = field != null ? field.GetValue(instance) : null;
        return value is float ? (float)value : fallback;
    }

    private static bool ReadBool(object instance, string name, bool fallback)
    {
        if (instance == null)
            return fallback;
        FieldInfo field = instance.GetType().GetField(name, AnyInstance);
        object value = field != null ? field.GetValue(instance) : null;
        return value is bool ? (bool)value : fallback;
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
