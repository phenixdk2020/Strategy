using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f3 visual formation/facing motion.
// 09f8 tuning: stationary regiment facing speed reduced 30% from 24 to 16.8 deg/s.
// Regiment.cs remains the combat/movement authority. This layer only replaces
// instantaneous-looking formation/facing changes with visible representative-man
// movement and a bounded physical turn of the regiment root while stationary.
[DefaultExecutionOrder(20000)]
public sealed class PrototypeFormationMotion09F3 : MonoBehaviour
{
    private sealed class MotionState
    {
        public RegimentFormation LastFormation;
        public readonly List<Transform> Soldiers = new List<Transform>();
        public readonly List<Vector3> VisualPositions = new List<Vector3>();
        public bool FormationChanging;
        public Quaternion VisualRotation;
        public Quaternion TargetRotation;
        public bool Turning;
        public bool Initialized;
    }

    private readonly Dictionary<Regiment, MotionState> states =
        new Dictionary<Regiment, MotionState>();

    private FieldInfo hasDestinationField;

    private const float FormationMoveSpeed = 2.15f;
    private const float SoldierTurnSpeed = 150f;
    private const float RegimentTurnSpeed = 16.8f;
    private const float FormationArrival = 0.035f;
    private const float TurnArrivalDegrees = 0.20f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFormationMotion09F3>() != null)
            return;

        GameObject root = new GameObject("PrototypeFormationMotion_v000009f3");
        root.AddComponent<PrototypeFormationMotion09F3>();
    }

    private void Awake()
    {
        hasDestinationField = typeof(Regiment).GetField(
            "hasDestination",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "FORMATION-MOTION-09F3|Installed=True|" +
            "FormationMoveSpeed=" + FormationMoveSpeed.ToString("0.00") +
            "|TurnSpeedDeg=" + RegimentTurnSpeed.ToString("0.0") +
            "|TeleportRotation=False");
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);

            if (!states.TryGetValue(regiment, out MotionState state))
            {
                state = new MotionState();
                states[regiment] = state;
                InitializeState(regiment, state);
            }

            if (!state.Initialized)
                InitializeState(regiment, state);

            UpdateFormationMotion(regiment, state);
            UpdateFacingMotion(regiment, state);
        }

        Cleanup(active);
    }

    private static void InitializeState(Regiment regiment, MotionState state)
    {
        state.Soldiers.Clear();
        state.VisualPositions.Clear();

        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("Soldier_"))
                continue;

            state.Soldiers.Add(child);
            state.VisualPositions.Add(child.localPosition);
        }

        state.LastFormation = regiment.Formation;
        state.VisualRotation = regiment.transform.rotation;
        state.TargetRotation = state.VisualRotation;
        state.FormationChanging = false;
        state.Turning = false;
        state.Initialized = true;
    }

    private void UpdateFormationMotion(Regiment regiment, MotionState state)
    {
        if (state.LastFormation != regiment.Formation)
        {
            EnsureSoldierCache(regiment, state);

            for (int i = 0; i < state.Soldiers.Count; i++)
                state.VisualPositions[i] = state.Soldiers[i] != null
                    ? state.Soldiers[i].localPosition
                    : Vector3.zero;

            state.LastFormation = regiment.Formation;
            state.FormationChanging = true;

            Debug.Log(
                "FORMATION-MOTION-09F3|Unit=" + regiment.RegimentName +
                "|Target=" + regiment.Formation +
                "|Animated=True");
        }

        if (!state.FormationChanging)
            return;

        EnsureSoldierCache(regiment, state);
        bool complete = true;
        int total = state.Soldiers.Count;

        for (int i = 0; i < total; i++)
        {
            Transform soldier = state.Soldiers[i];
            if (soldier == null)
                continue;

            Vector3 target = GetFormationPosition(regiment.Formation, i, total);
            Vector3 current = state.VisualPositions[i];
            Vector3 next = Vector3.MoveTowards(
                current,
                target,
                FormationMoveSpeed * Time.deltaTime);

            state.VisualPositions[i] = next;
            soldier.localPosition = next;
            soldier.localRotation = Quaternion.RotateTowards(
                soldier.localRotation,
                Quaternion.identity,
                SoldierTurnSpeed * Time.deltaTime);

            if ((next - target).sqrMagnitude > FormationArrival * FormationArrival)
                complete = false;
        }

        if (!complete)
            return;

        state.FormationChanging = false;
        Debug.Log(
            "FORMATION-MOTION-09F3|Unit=" + regiment.RegimentName +
            "|FormationComplete=" + regiment.Formation);
    }

    private void UpdateFacingMotion(Regiment regiment, MotionState state)
    {
        bool moving = IsMoving(regiment);
        Quaternion observed = regiment.transform.rotation;

        // Movement steering remains owned by Regiment/AI. We only smooth facing
        // orders while the regiment is stationary or has just finished a route.
        if (moving || regiment.IsRouted)
        {
            state.VisualRotation = observed;
            state.TargetRotation = observed;
            state.Turning = false;
            return;
        }

        if (!state.Turning)
        {
            float requestedDelta = Quaternion.Angle(state.VisualRotation, observed);
            if (requestedDelta <= TurnArrivalDegrees)
            {
                state.VisualRotation = observed;
                return;
            }

            state.TargetRotation = observed;
            regiment.transform.rotation = state.VisualRotation;
            state.Turning = true;

            Debug.Log(
                "FORMATION-TURN-09F3|Unit=" + regiment.RegimentName +
                "|Angle=" + requestedDelta.ToString("0.0") +
                "|Animated=True");
        }
        else
        {
            // A combat/command system can update the requested target while the
            // visual turn is in progress. Capture that target, then restore the
            // current visual orientation before applying the bounded turn step.
            if (Quaternion.Angle(observed, state.VisualRotation) > 0.40f)
                state.TargetRotation = observed;
        }

        state.VisualRotation = Quaternion.RotateTowards(
            state.VisualRotation,
            state.TargetRotation,
            RegimentTurnSpeed * Time.deltaTime);

        regiment.transform.rotation = state.VisualRotation;

        if (Quaternion.Angle(state.VisualRotation, state.TargetRotation) > TurnArrivalDegrees)
            return;

        state.VisualRotation = state.TargetRotation;
        regiment.transform.rotation = state.TargetRotation;
        state.Turning = false;

        Debug.Log(
            "FORMATION-TURN-09F3|Unit=" + regiment.RegimentName +
            "|Complete=True");
    }

    private bool IsMoving(Regiment regiment)
    {
        if (hasDestinationField == null || regiment == null)
            return false;

        object value = hasDestinationField.GetValue(regiment);
        return value is bool && (bool)value;
    }

    private static Vector3 GetFormationPosition(
        RegimentFormation formation,
        int index,
        int total)
    {
        if (formation == RegimentFormation.Line)
        {
            const int ranks = 3;
            int columns = Mathf.CeilToInt(total / (float)ranks);
            int rank = index / columns;
            int columnIndex = index % columns;
            float x = (columnIndex - (columns - 1) * 0.5f) * 0.75f;
            float z = (rank - 1f) * -0.90f;
            return new Vector3(x, 0f, z);
        }

        const int columnCount = 4;
        int columnRank = index / columnCount;
        int columnIndexInRank = index % columnCount;
        return new Vector3(
            (columnIndexInRank - 1.5f) * 0.78f,
            0f,
            -columnRank * 0.78f);
    }

    private static void EnsureSoldierCache(Regiment regiment, MotionState state)
    {
        int expected = 0;
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child != null && child.name.StartsWith("Soldier_"))
                expected++;
        }

        if (expected == state.Soldiers.Count)
            return;

        state.Soldiers.Clear();
        state.VisualPositions.Clear();

        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("Soldier_"))
                continue;

            state.Soldiers.Add(child);
            state.VisualPositions.Add(child.localPosition);
        }
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, MotionState> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }
}
