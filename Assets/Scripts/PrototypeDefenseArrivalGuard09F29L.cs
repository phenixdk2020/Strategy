using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29l
// Defensive slot arrival hysteresis.
// F29E's precise 0.85 m correction could repeatedly restart movement after a company
// had already settled in its visible defensive footprint. F29L latches a defending
// company once it is genuinely inside the slot and does not release it unless it
// drifts materially away. Under-fire reaction keeps tactical authority while latched.
[DefaultExecutionOrder(33000)]
public sealed class PrototypeDefenseArrivalGuard09F29L : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float ArrivalLatchDistance = 0.50f;
    private const float ArrivalReleaseDistance = 1.25f;

    private readonly Dictionary<Regiment, Vector3> latchedGoals = new Dictionary<Regiment, Vector3>();
    private readonly HashSet<Regiment> present = new HashSet<Regiment>();

    private FieldInfo battalionsField;
    private FieldInfo visualsField;
    private FieldInfo preciseArrivalOwnedField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeDefenseArrivalGuard09F29L>() == null)
            new GameObject("PrototypeDefenseArrivalGuard_v000009f29l")
                .AddComponent<PrototypeDefenseArrivalGuard09F29L>();
    }

    private void Awake()
    {
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);
        visualsField = typeof(PrototypeRegimentHierarchy09F27).GetField("visuals", AnyInstance);
        preciseArrivalOwnedField = typeof(PrototypeRegimentalCommandHardening09F29E)
            .GetField("preciseArrivalOwned", AnyInstance);

        Debug.Log(
            "DEF-ARRIVAL-09F30V|Installed=True|SingleArrivalAuthority=True|Latch=" + ArrivalLatchDistance.ToString("0.00") +
            "m|Release=" + ArrivalReleaseDistance.ToString("0.00") +
            "m|PreciseArrivalPingPongSuppressed=True");
    }

    private void Update()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return;

        present.Clear();

        for (int b = 0; b < battalions.Count; b++)
        {
            object battalion = battalions[b];
            if (battalion == null)
                continue;

            FieldInfo missionsField = battalion.GetType().GetField("Missions", AnyInstance);
            IDictionary missions = missionsField != null ? missionsField.GetValue(battalion) as IDictionary : null;
            if (missions == null)
                continue;

            foreach (DictionaryEntry entry in missions)
            {
                Regiment unit = entry.Key as Regiment;
                object mission = entry.Value;
                if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                    continue;

                System.Type missionType = mission.GetType();
                FieldInfo orderField = missionType.GetField("Order", AnyInstance);
                FieldInfo goalField = missionType.GetField("Goal", AnyInstance);
                FieldInfo arrivedField = missionType.GetField("Arrived", AnyInstance);
                if (orderField == null || goalField == null || arrivedField == null)
                    continue;

                MajorOrder09F18 order = (MajorOrder09F18)orderField.GetValue(mission);
                if (order != MajorOrder09F18.DefendHere)
                    continue;

                Vector3 goal = (Vector3)goalField.GetValue(mission);
                present.Add(unit);

                if (latchedGoals.TryGetValue(unit, out Vector3 previousGoal) &&
                    PlanarDistance(previousGoal, goal) > 0.75f)
                {
                    latchedGoals.Remove(unit);
                }

                float distance = PlanarDistance(unit.transform.position, goal);
                bool reacting = PrototypeUnderFireReaction09F26.IsReacting(unit);
                bool latched = latchedGoals.ContainsKey(unit);

                if (latched && !reacting && distance > ArrivalReleaseDistance)
                {
                    latchedGoals.Remove(unit);
                    arrivedField.SetValue(mission, false);
                    Debug.Log("DEF-ARRIVAL-09F30V|Unit=" + unit.RegimentName +
                              "|Latched=False|Reason=Drift|Distance=" + distance.ToString("0.00"));
                    continue;
                }

                if (!latched && !reacting && distance <= ArrivalLatchDistance)
                {
                    latchedGoals[unit] = goal;
                    latched = true;
                    Debug.Log("DEF-ARRIVAL-09F29L|Unit=" + unit.RegimentName +
                              "|Latched=True|Distance=" + distance.ToString("0.00") +
                              "|Goal=" + goal.x.ToString("0.0") + "," + goal.z.ToString("0.0"));
                }

                if (!latched)
                    continue;

                arrivedField.SetValue(mission, true);
                SuppressF29EPreciseCorrection(unit);
                SetVisualArrived(unit, false);

                if (!reacting)
                {
                    unit.OrderHold();
                    OfficerAIController controller = unit.GetComponent<OfficerAIController>();
                    if (controller != null && controller.AIEnabled)
                        controller.enabled = true;
                }
            }
        }

        CleanupStale();
    }

    private void LateUpdate()
    {
        // During Update we temporarily clear the visual Arrived flag so F29E cannot
        // restart its 0.85 m correction. Restore it here so the player still sees the
        // destination as completed during rendering.
        foreach (KeyValuePair<Regiment, Vector3> pair in latchedGoals)
        {
            Regiment unit = pair.Key;
            if (unit != null && present.Contains(unit))
                SetVisualArrived(unit, true);
        }
    }

    private void SuppressF29EPreciseCorrection(Regiment unit)
    {
        PrototypeRegimentalCommandHardening09F29E hardening =
            UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalCommandHardening09F29E>();
        if (hardening == null || preciseArrivalOwnedField == null)
            return;

        HashSet<Regiment> owned = preciseArrivalOwnedField.GetValue(hardening) as HashSet<Regiment>;
        if (owned != null)
            owned.Remove(unit);
    }

    private void SetVisualArrived(Regiment unit, bool value)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || visualsField == null || unit == null)
            return;

        IDictionary visuals = visualsField.GetValue(hierarchy) as IDictionary;
        if (visuals == null || !visuals.Contains(unit))
            return;

        object visual = visuals[unit];
        if (visual == null)
            return;

        FieldInfo arrivedField = visual.GetType().GetField("Arrived", AnyInstance);
        if (arrivedField != null)
            arrivedField.SetValue(visual, value);
    }

    private void CleanupStale()
    {
        if (latchedGoals.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, Vector3> pair in latchedGoals)
        {
            Regiment unit = pair.Key;
            if (unit != null && present.Contains(unit))
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(unit);
        }

        if (stale == null)
            return;
        for (int i = 0; i < stale.Count; i++)
            latchedGoals.Remove(stale[i]);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
