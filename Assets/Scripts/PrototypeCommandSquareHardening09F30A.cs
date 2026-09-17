using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f30a
// Hardening pass for command-selection persistence and Square presentation/transition safety.
// - OOB-selected company/Major/regimental HQ remains selected through bottom-HUD actions
//   and point-order commit frames without moving the camera.
// - legacy Line/mission formation footprints are forced off for the full Square state,
//   including FORMING.
// - F29V Square sector lines are terrain-conformed with extra clearance so short pieces
//   do not disappear into uneven ground.
// - Square enter/exit synchronises F7's timer-based volley detector so formation changes
//   can never create fake full-front smoke or a fake FIRE pose.

[DefaultExecutionOrder(-45000)]
public sealed class PrototypeOobCommandSelectionGuard09F30A : MonoBehaviour
{
    private enum SelectionKind
    {
        None,
        Companies,
        Major,
        Regiment
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float BottomHudHeight = 112f;
    private const float DirectHudGraceSeconds = 0.55f;
    private const float CommitGraceSeconds = 0.40f;

    private FieldInfo playerSelectedField;
    private FieldInfo selectedBattalionField;
    private MethodInfo selectMajorMethod;
    private MethodInfo setRegimentalSelectedMethod;

    private readonly List<Regiment> cachedCompanies = new List<Regiment>();
    private SelectionKind cachedKind;
    private int cachedMajor = -1;
    private bool wasPending;
    private float preserveUntil = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeOobCommandSelectionGuard09F30A>() == null)
            new GameObject("PrototypeOobCommandSelectionGuard_v000009f30a")
                .AddComponent<PrototypeOobCommandSelectionGuard09F30A>();
    }

    private void Awake()
    {
        playerSelectedField = typeof(PlayerCommander).GetField("selected", PrivateInstance);
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        selectMajorMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SelectMajor", PrivateInstance);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod("SetSelected", PrivateInstance);

        Debug.Log("OOB-SELECTION-09F30A|Installed=True|HudPersistence=True|PointOrderPersistence=True|CameraMoved=False");
    }

    private void Update()
    {
        PrototypeOfficerFacingOrder09F29G facing = PrototypeOfficerFacingOrder09F29G.Instance;
        bool pending = facing != null && facing.HasPendingOrder;

        // A bottom-HUD mouse-down happens before IMGUI handles the button later in the
        // frame. Capture the live OOB/battlefield selection now, before any button path
        // can clear hierarchy/player selection.
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.y <= BottomHudHeight)
        {
            CaptureLiveSelection(false);
            preserveUntil = Mathf.Max(preserveUntil, Time.unscaledTime + DirectHudGraceSeconds);
        }

        if (pending && !wasPending)
        {
            CaptureLiveSelection(false);
            preserveUntil = Mathf.Max(preserveUntil, Time.unscaledTime + DirectHudGraceSeconds);
        }

        if (wasPending && !pending)
            preserveUntil = Mathf.Max(preserveUntil, Time.unscaledTime + CommitGraceSeconds);

        if (pending || Time.unscaledTime < preserveUntil)
        {
            RestoreCachedSelectionIfNeeded();
        }
        else
        {
            // Normal user selection is authoritative whenever no HUD/order transaction
            // is in progress. This is what lets a later battlefield/OOB click replace
            // the cache rather than being fought by the persistence guard.
            CaptureLiveSelection(true);
        }

        wasPending = pending;
    }

    private void CaptureLiveSelection(bool clearWhenNone)
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.Selected)
        {
            cachedKind = SelectionKind.Regiment;
            cachedMajor = -1;
            cachedCompanies.Clear();
            return;
        }

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int major = ReadSelectedMajor(hierarchy);
        if (major >= 0)
        {
            cachedKind = SelectionKind.Major;
            cachedMajor = major;
            cachedCompanies.Clear();
            return;
        }

        List<Regiment> selected = ReadPlayerSelection();
        if (selected != null && selected.Count > 0)
        {
            cachedKind = SelectionKind.Companies;
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
            cachedKind = SelectionKind.None;
            cachedMajor = -1;
            cachedCompanies.Clear();
        }
    }

    private void RestoreCachedSelectionIfNeeded()
    {
        switch (cachedKind)
        {
            case SelectionKind.Regiment:
                RestoreRegiment();
                break;
            case SelectionKind.Major:
                RestoreMajor();
                break;
            case SelectionKind.Companies:
                RestoreCompanies();
                break;
        }
    }

    private void RestoreRegiment()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed || regimental.Selected || setRegimentalSelectedMethod == null)
            return;

        ClearCompanyAndMajorSelection();
        setRegimentalSelectedMethod.Invoke(regimental, new object[] { true });
        Debug.Log("OOB-SELECTION-09F30A|Restore=REGIMENT|CameraMoved=False");
    }

    private void RestoreMajor()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || cachedMajor < 0 ||
            ReadSelectedMajor(hierarchy) == cachedMajor || selectMajorMethod == null)
            return;

        ClearCompanySelection();
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Selected && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        selectMajorMethod.Invoke(hierarchy, new object[] { cachedMajor });
        Debug.Log("OOB-SELECTION-09F30A|Restore=MAJOR|Battalion=" + (cachedMajor + 1) + "|CameraMoved=False");
    }

    private void RestoreCompanies()
    {
        if (cachedCompanies.Count == 0)
            return;

        List<Regiment> selected = ReadPlayerSelection();
        if (selected == null)
            return;

        bool matches = selected.Count == cachedCompanies.Count;
        if (matches)
        {
            for (int i = 0; i < cachedCompanies.Count; i++)
            {
                if (!selected.Contains(cachedCompanies[i]))
                {
                    matches = false;
                    break;
                }
            }
        }
        if (matches)
            return;

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Selected && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();

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

        Debug.Log("OOB-SELECTION-09F30A|Restore=COMPANY|Count=" + selected.Count + "|CameraMoved=False");
    }

    private void ClearCompanyAndMajorSelection()
    {
        ClearCompanySelection();
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();
    }

    private void ClearCompanySelection()
    {
        List<Regiment> selected = ReadPlayerSelection();
        if (selected == null)
            return;
        for (int i = 0; i < selected.Count; i++)
            if (selected[i] != null)
                selected[i].SetSelected(false);
        selected.Clear();
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

[DefaultExecutionOrder(100000)]
public sealed class PrototypeSquarePresentationGuard09F30A : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float SquareLineGroundClearance = 0.70f;

    private readonly Dictionary<Regiment, bool> wasSquare = new Dictionary<Regiment, bool>();
    private FieldInfo hierarchyVisualsField;
    private FieldInfo f7StatesField;
    private FieldInfo nextFireTimeField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSquarePresentationGuard09F30A>() == null)
            new GameObject("PrototypeSquarePresentationGuard_v000009f30a")
                .AddComponent<PrototypeSquarePresentationGuard09F30A>();
    }

    private void Awake()
    {
        hierarchyVisualsField = typeof(PrototypeRegimentHierarchy09F27).GetField("visuals", AnyInstance);
        f7StatesField = typeof(PrototypeBattleVisuals09F7).GetField("states", AnyInstance);
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log("SQUARE-HARDEN-09F30A|Installed=True|LegacyFootprint=OFF_DURING_FORMING_AND_READY|TerrainClearance=" +
                  SquareLineGroundClearance.ToString("0.00") + "|FormationSmoke=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();
        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null)
                continue;

            active.Add(unit);
            bool inSquare = PrototypeInfantrySquare09F29.IsInSquare(unit);
            bool previous = wasSquare.TryGetValue(unit, out bool old) && old;

            if (inSquare)
            {
                SuppressFormationFootprints(unit);
                SyncF7VolleyDetector(unit, true);
            }
            else if (previous)
            {
                // F29V restores the global reload timer on Square exit. F7 normally
                // interprets any timer increase as a real volley; synchronise its
                // remembered timer after that restore so Square->Line is smoke-free.
                SyncF7VolleyDetector(unit, true);
                Debug.Log("SQUARE-HARDEN-09F30A|Unit=" + unit.RegimentName + "|Transition=SQUARE_TO_LINE|FakeSmokeSuppressed=True");
            }

            wasSquare[unit] = inSquare;
        }

        List<Regiment> stale = null;
        foreach (Regiment unit in wasSquare.Keys)
        {
            if (unit != null && active.Contains(unit))
                continue;
            if (stale == null) stale = new List<Regiment>();
            stale.Add(unit);
        }
        if (stale != null)
            for (int i = 0; i < stale.Count; i++)
                wasSquare.Remove(stale[i]);
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

            // Last-writer protection: old F5/F27 visuals are allowed to update normally
            // for Line/Column, but never remain visible while Square owns formation geometry.
            SuppressFormationFootprints(unit);
            LiftSquareSectorLines(unit);
        }
    }

    private void SuppressFormationFootprints(Regiment unit)
    {
        if (unit == null)
            return;

        Transform selection = unit.transform.Find("SelectionFootprint09F5");
        DisableLine(selection);

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || hierarchyVisualsField == null)
            return;

        IDictionary visuals = hierarchyVisualsField.GetValue(hierarchy) as IDictionary;
        if (visuals == null || !visuals.Contains(unit))
            return;

        object visual = visuals[unit];
        if (visual == null)
            return;

        FieldInfo footprintField = visual.GetType().GetField("Footprint", AnyInstance);
        LineRenderer footprint = footprintField != null ? footprintField.GetValue(visual) as LineRenderer : null;
        if (footprint != null)
            footprint.enabled = false;
    }

    private static void DisableLine(Transform transform)
    {
        if (transform == null)
            return;
        LineRenderer line = transform.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = false;
    }

    private static void LiftSquareSectorLines(Regiment unit)
    {
        GameObject root = GameObject.Find("SquareSector09F29V_" + unit.RegimentName);
        if (root == null)
            return;

        LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int l = 0; l < lines.Length; l++)
        {
            LineRenderer line = lines[l];
            if (line == null || !line.enabled || !line.useWorldSpace)
                continue;

            line.numCapVertices = Mathf.Max(2, line.numCapVertices);
            line.numCornerVertices = Mathf.Max(2, line.numCornerVertices);
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                float terrainY = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + SquareLineGroundClearance;
                if (p.y < terrainY)
                {
                    p.y = terrainY;
                    line.SetPosition(i, p);
                }
            }
        }
    }

    private void SyncF7VolleyDetector(Regiment unit, bool stopLegacySmoke)
    {
        if (unit == null || f7StatesField == null)
            return;

        PrototypeBattleVisuals09F7 visuals = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (visuals == null)
            return;

        IDictionary states = f7StatesField.GetValue(visuals) as IDictionary;
        if (states == null || !states.Contains(unit))
            return;

        object state = states[unit];
        if (state == null)
            return;

        float timer = Time.time;
        if (nextFireTimeField != null)
        {
            object value = nextFireTimeField.GetValue(unit);
            if (value is float)
                timer = (float)value;
        }

        FieldInfo lastTimer = state.GetType().GetField("LastNextFireTime", AnyInstance);
        if (lastTimer != null)
            lastTimer.SetValue(state, timer);

        FieldInfo volleyStarted = state.GetType().GetField("VolleyStartedAt", AnyInstance);
        if (volleyStarted != null)
            volleyStarted.SetValue(state, -100f);

        if (!stopLegacySmoke)
            return;

        FieldInfo smokeField = state.GetType().GetField("Smoke", AnyInstance);
        ParticleSystem smoke = smokeField != null ? smokeField.GetValue(state) as ParticleSystem : null;
        if (smoke != null && smoke.IsAlive(true))
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
