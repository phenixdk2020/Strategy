using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29x
// Final hardening for the F29V square + OOB interaction:
// - suppresses legacy Regiment auto-fire while a company is in Square without changing
//   the player's persistent fire policy,
// - removes the F29V nextFireTime parking side effect before legacy telemetry/visuals can
//   mistake it for a volley,
// - keeps real square-sector volleys visible to the normal smoke / hit-feedback pipeline,
// - hides the old Line selection footprint while Square keeps its own SquareOutline,
// - preserves OOB command selection through bottom-HUD clicks and officer order placement.
internal static class PrototypeSquareGateState09F29X
{
    internal sealed class UnitState
    {
        public RegimentFirePolicy DesiredPolicy;
        public float PreF29VTimer;
        public float PreF29VLastShotAt = -999f;
    }

    internal static readonly Dictionary<Regiment, UnitState> Units =
        new Dictionary<Regiment, UnitState>();

    internal static readonly BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo NextFireTimeField =
        typeof(Regiment).GetField("nextFireTime", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo f29vRuntimeField;

    internal static UnitState GetOrCreate(Regiment unit)
    {
        if (unit == null)
            return null;

        UnitState state;
        if (!Units.TryGetValue(unit, out state))
        {
            state = new UnitState
            {
                DesiredPolicy = unit.FirePolicy
            };
            Units.Add(unit, state);
        }
        return state;
    }

    internal static float ReadTimer(Regiment unit)
    {
        if (unit == null || NextFireTimeField == null)
            return Time.time;
        object value = NextFireTimeField.GetValue(unit);
        return value is float ? (float)value : Time.time;
    }

    internal static void WriteTimer(Regiment unit, float value)
    {
        if (unit != null && NextFireTimeField != null)
            NextFireTimeField.SetValue(unit, value);
    }

    private static object GetF29VState(Regiment unit)
    {
        PrototypeSquareCorrections09F29V owner =
            UnityEngine.Object.FindAnyObjectByType<PrototypeSquareCorrections09F29V>();
        if (owner == null || unit == null)
            return null;

        if (f29vRuntimeField == null)
            f29vRuntimeField = typeof(PrototypeSquareCorrections09F29V).GetField("runtime", AnyInstance);
        if (f29vRuntimeField == null)
            return null;

        IDictionary dictionary = f29vRuntimeField.GetValue(owner) as IDictionary;
        if (dictionary == null || !dictionary.Contains(unit))
            return null;
        return dictionary[unit];
    }

    internal static float ReadF29VLastShotAt(Regiment unit)
    {
        object state = GetF29VState(unit);
        if (state == null)
            return -999f;

        FieldInfo field = state.GetType().GetField("LastShotAt", AnyInstance);
        if (field == null)
            return -999f;
        object value = field.GetValue(state);
        return value is float ? (float)value : -999f;
    }

    internal static float ReadBestFaceReload(Regiment unit)
    {
        object state = GetF29VState(unit);
        if (state == null)
            return -1f;

        FieldInfo field = state.GetType().GetField("FaceNextFire", AnyInstance);
        float[] clocks = field != null ? field.GetValue(state) as float[] : null;
        if (clocks == null)
            return -1f;

        float best = -1f;
        float ceiling = Time.time + 120f;
        for (int i = 0; i < clocks.Length; i++)
        {
            float value = clocks[i];
            if (float.IsNaN(value) || float.IsInfinity(value))
                continue;
            if (value <= Time.time || value > ceiling)
                continue;
            if (value > best)
                best = value;
        }
        return best;
    }

    internal static void RemoveDeadEntries()
    {
        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, UnitState> pair in Units)
        {
            if (pair.Key != null)
                continue;
            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }
        if (remove == null)
            return;
        for (int i = 0; i < remove.Count; i++)
            Units.Remove(remove[i]);
    }
}

[DefaultExecutionOrder(-100)]
public sealed class PrototypeSquareLegacyFireGate09F29X : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquareLegacyFireGate09F29X>() != null)
            return;

        GameObject root = new GameObject("PrototypeSquareOobHardening_v000009f29x");
        root.AddComponent<PrototypeSquareLegacyFireGate09F29X>();
        root.AddComponent<PrototypeOobSelectionPersistence09F29X>();
        root.AddComponent<PrototypeSquarePreSectorRestore09F29X>();
        root.AddComponent<PrototypeSquarePostSectorRestore09F29X>();
    }

    private void Awake()
    {
        Debug.Log(
            "SQUARE-OOB-09F29X|Installed=True|LegacyAutoFireGate=True|" +
            "FalseVolleyParking=False|LineSelectionFootprintInSquare=False|" +
            "OOBSelectionPersistence=True");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> inSquare = new HashSet<Regiment>();
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            inSquare.Add(unit);
            PrototypeSquareGateState09F29X.UnitState state =
                PrototypeSquareGateState09F29X.GetOrCreate(unit);
            if (state == null)
                continue;

            // At the end of the previous frame the real policy is restored, so this
            // also captures a policy change made by the player while already in Square.
            state.DesiredPolicy = unit.FirePolicy;
            unit.SetFirePolicy(RegimentFirePolicy.HoldFire);
        }

        List<Regiment> exited = null;
        foreach (KeyValuePair<Regiment, PrototypeSquareGateState09F29X.UnitState> pair
                 in PrototypeSquareGateState09F29X.Units)
        {
            Regiment unit = pair.Key;
            if (unit == null || inSquare.Contains(unit))
                continue;

            if (unit != null)
                unit.SetFirePolicy(pair.Value.DesiredPolicy);
            if (exited == null)
                exited = new List<Regiment>();
            exited.Add(unit);
        }

        if (exited != null)
            for (int i = 0; i < exited.Count; i++)
                PrototypeSquareGateState09F29X.Units.Remove(exited[i]);

        PrototypeSquareGateState09F29X.RemoveDeadEntries();
    }
}

// Runs immediately before the authoritative F29V sector owner (39950).
[DefaultExecutionOrder(39900)]
public sealed class PrototypeSquarePreSectorRestore09F29X : MonoBehaviour
{
    private void Update()
    {
        foreach (KeyValuePair<Regiment, PrototypeSquareGateState09F29X.UnitState> pair
                 in PrototypeSquareGateState09F29X.Units)
        {
            Regiment unit = pair.Key;
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            PrototypeSquareGateState09F29X.UnitState state = pair.Value;
            unit.SetFirePolicy(state.DesiredPolicy);
            state.PreF29VTimer = PrototypeSquareGateState09F29X.ReadTimer(unit);
            state.PreF29VLastShotAt = PrototypeSquareGateState09F29X.ReadF29VLastShotAt(unit);
        }
    }
}

// Runs immediately after F29V. It removes F29V's +3600 second parking sentinel in the
// same frame, before the next frame's combat telemetry and F7 smoke detector can see it.
[DefaultExecutionOrder(40100)]
public sealed class PrototypeSquarePostSectorRestore09F29X : MonoBehaviour
{
    private void Update()
    {
        foreach (KeyValuePair<Regiment, PrototypeSquareGateState09F29X.UnitState> pair
                 in PrototypeSquareGateState09F29X.Units)
        {
            Regiment unit = pair.Key;
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            PrototypeSquareGateState09F29X.UnitState state = pair.Value;
            float postShot = PrototypeSquareGateState09F29X.ReadF29VLastShotAt(unit);
            bool realVolley = postShot > state.PreF29VLastShotAt + 0.0001f &&
                              postShot >= Time.time - 0.15f;

            if (realVolley)
            {
                float reload = PrototypeSquareGateState09F29X.ReadBestFaceReload(unit);
                if (reload <= Time.time)
                    reload = Time.time + Mathf.Max(0.25f, unit.CurrentReloadSeconds);
                PrototypeSquareGateState09F29X.WriteTimer(unit, reload);
            }
            else
            {
                PrototypeSquareGateState09F29X.WriteTimer(unit, state.PreF29VTimer);
            }
        }
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || !PrototypeInfantrySquare09F29.IsInSquare(unit))
                continue;

            // This is the old yellow Line/Column selection rectangle. It is not the
            // Square formation outline and must not be shown while Square owns geometry.
            Transform oldFootprint = unit.transform.Find("SelectionFootprint09F5");
            if (oldFootprint != null)
            {
                LineRenderer line = oldFootprint.GetComponent<LineRenderer>();
                if (line != null)
                    line.enabled = false;
            }
        }
    }
}

[DefaultExecutionOrder(1000)]
public sealed class PrototypeOobSelectionPersistence09F29X : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float BottomHudHeight = 100f;
    private const float CommitGraceSeconds = 0.30f;

    private FieldInfo playerSelectedField;
    private FieldInfo selectedBattalionField;
    private MethodInfo selectMajorMethod;
    private MethodInfo setRegimentalSelectedMethod;

    private readonly List<Regiment> cachedCompanies = new List<Regiment>();
    private int cachedMajor = -1;
    private bool cachedRegimental;
    private bool wasPending;
    private float preserveUntil = -1f;

    private void Awake()
    {
        playerSelectedField = typeof(PlayerCommander).GetField("selected", PrivateInstance);
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        selectMajorMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SelectMajor", PrivateInstance);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod("SetSelected", PrivateInstance);
    }

    private void Update()
    {
        PrototypeOfficerFacingOrder09F29G facing = PrototypeOfficerFacingOrder09F29G.Instance;
        bool pending = facing != null && facing.HasPendingOrder;

        if (pending && !wasPending)
            CaptureLiveSelection(false);

        if (wasPending && !pending)
            preserveUntil = Time.unscaledTime + CommitGraceSeconds;

        bool bottomHudClick = Input.GetMouseButtonDown(0) && Input.mousePosition.y <= BottomHudHeight;
        bool preserve = pending || Time.unscaledTime < preserveUntil || bottomHudClick;

        if (preserve)
            RestoreCachedSelectionIfNeeded();

        wasPending = pending;
    }

    private void LateUpdate()
    {
        bool pending = PrototypeOfficerFacingOrder09F29G.Instance != null &&
                       PrototypeOfficerFacingOrder09F29G.Instance.HasPendingOrder;
        if (pending || Time.unscaledTime < preserveUntil)
            return;

        CaptureLiveSelection(true);
    }

    private void CaptureLiveSelection(bool clearWhenNone)
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.Selected)
        {
            cachedRegimental = true;
            cachedMajor = -1;
            cachedCompanies.Clear();
            return;
        }

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int major = ReadSelectedMajor(hierarchy);
        if (major >= 0)
        {
            cachedRegimental = false;
            cachedMajor = major;
            cachedCompanies.Clear();
            return;
        }

        List<Regiment> selected = ReadPlayerSelection();
        if (selected != null && selected.Count > 0)
        {
            cachedRegimental = false;
            cachedMajor = -1;
            cachedCompanies.Clear();
            for (int i = 0; i < selected.Count; i++)
            {
                Regiment unit = selected[i];
                if (unit != null && unit.Team == BattleTeam.Denmark)
                    cachedCompanies.Add(unit);
            }
            return;
        }

        if (clearWhenNone)
        {
            cachedRegimental = false;
            cachedMajor = -1;
            cachedCompanies.Clear();
        }
    }

    private void RestoreCachedSelectionIfNeeded()
    {
        if (cachedRegimental)
        {
            PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
            if (regimental != null && regimental.Installed && !regimental.Selected &&
                setRegimentalSelectedMethod != null)
            {
                setRegimentalSelectedMethod.Invoke(regimental, new object[] { true });
                Debug.Log("OOB-SELECTION-09F29X|Restore=REGIMENT|CameraMoved=False");
            }
            return;
        }

        if (cachedMajor >= 0)
        {
            PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
            if (hierarchy != null && hierarchy.Installed &&
                ReadSelectedMajor(hierarchy) != cachedMajor && selectMajorMethod != null)
            {
                selectMajorMethod.Invoke(hierarchy, new object[] { cachedMajor });
                Debug.Log("OOB-SELECTION-09F29X|Restore=MAJOR|Battalion=" + (cachedMajor + 1) + "|CameraMoved=False");
            }
            return;
        }

        if (cachedCompanies.Count == 0)
            return;

        List<Regiment> selected = ReadPlayerSelection();
        bool missing = selected == null || selected.Count != cachedCompanies.Count;
        if (!missing && selected != null)
        {
            for (int i = 0; i < cachedCompanies.Count; i++)
                if (!selected.Contains(cachedCompanies[i]))
                {
                    missing = true;
                    break;
                }
        }
        if (!missing || selected == null)
            return;

        PrototypeRegimentalHQ09F28 regimentalHq = PrototypeRegimentalHQ09F28.Instance;
        if (regimentalHq != null && regimentalHq.Selected && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimentalHq, new object[] { false });

        PrototypeRegimentHierarchy09F27 hierarchyHq = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchyHq != null)
            hierarchyHq.ClearMajorSelection();

        for (int i = 0; i < selected.Count; i++)
            if (selected[i] != null)
                selected[i].SetSelected(false);
        selected.Clear();

        for (int i = 0; i < cachedCompanies.Count; i++)
        {
            Regiment unit = cachedCompanies[i];
            if (unit == null || unit.Team != BattleTeam.Denmark)
                continue;
            selected.Add(unit);
            unit.SetSelected(true);
        }

        Debug.Log("OOB-SELECTION-09F29X|Restore=COMPANY|Count=" + selected.Count + "|CameraMoved=False");
    }

    private int ReadSelectedMajor(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private List<Regiment> ReadPlayerSelection()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null || playerSelectedField == null)
            return null;
        return playerSelectedField.GetValue(commander) as List<Regiment>;
    }
}
