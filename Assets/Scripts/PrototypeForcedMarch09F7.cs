using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f7 movement-mode layer.
// Normal movement walks, Forced March moves faster with extra cohesion cost,
// and routed/panicked units run faster while fleeing.
[DefaultExecutionOrder(1850)]
public sealed class PrototypeForcedMarch09F7 : MonoBehaviour
{
    public static PrototypeForcedMarch09F7 Instance { get; private set; }

    private readonly HashSet<Regiment> forcedMarch = new HashSet<Regiment>();

    private FieldInfo moveSpeedField;
    private FieldInfo hasDestinationField;
    private FieldInfo cohesionField;

    private const float DenmarkWalkSpeed = 3.20f;
    private const float PrussiaWalkSpeed = 3.35f;
    private const float DenmarkForcedSpeed = 4.25f;
    private const float PrussiaForcedSpeed = 4.45f;
    private const float DenmarkRoutRunSpeed = 5.20f;
    private const float PrussiaRoutRunSpeed = 5.40f;
    private const float ForcedMarchExtraCohesionCostPerSecond = 0.22f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeForcedMarch09F7>() != null)
            return;

        GameObject root = new GameObject("PrototypeForcedMarch_v000009f7");
        root.AddComponent<PrototypeForcedMarch09F7>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        moveSpeedField = typeof(Regiment).GetField("moveSpeed", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        cohesionField = typeof(Regiment).GetField("<Cohesion>k__BackingField", flags);

        if (moveSpeedField == null || hasDestinationField == null || cohesionField == null)
        {
            Debug.LogError("MOVE-09F7|Installed=False|Reason=RegimentMovementFieldsMissing");
            enabled = false;
            return;
        }

        Debug.Log(
            "MOVE-09F7|Installed=True|WalkDK=" + DenmarkWalkSpeed.ToString("0.00") +
            "|ForcedDK=" + DenmarkForcedSpeed.ToString("0.00") +
            "|RoutRunDK=" + DenmarkRoutRunSpeed.ToString("0.00") +
            "|ForcedExtraCohesionCost=" + ForcedMarchExtraCohesionCostPerSecond.ToString("0.00") + "/s|Toggle=G");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            ToggleSelectedDanish();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            bool moving = HasDestination(regiment);

            if (regiment.IsRouted)
            {
                forcedMarch.Remove(regiment);
                moveSpeedField.SetValue(regiment, GetRoutRunSpeed(regiment.Team));
                continue;
            }

            if (forcedMarch.Contains(regiment))
            {
                moveSpeedField.SetValue(regiment, GetForcedSpeed(regiment.Team));

                if (moving)
                {
                    float cohesion = (float)cohesionField.GetValue(regiment);
                    cohesion = Mathf.Max(20f, cohesion - ForcedMarchExtraCohesionCostPerSecond * Time.deltaTime);
                    cohesionField.SetValue(regiment, cohesion);
                }
            }
            else
            {
                moveSpeedField.SetValue(regiment, GetWalkSpeed(regiment.Team));
            }
        }

        Cleanup(active);
    }

    public bool HasDestination(Regiment regiment)
    {
        if (regiment == null || hasDestinationField == null)
            return false;
        return (bool)hasDestinationField.GetValue(regiment);
    }

    public static bool IsMoving(Regiment regiment)
    {
        return Instance != null && Instance.HasDestination(regiment);
    }

    public static bool IsForcedMarch(Regiment regiment)
    {
        return Instance != null && regiment != null && Instance.forcedMarch.Contains(regiment) && !regiment.IsRouted;
    }

    public void Toggle(Regiment regiment)
    {
        if (regiment == null || regiment.IsRouted)
            return;

        if (forcedMarch.Contains(regiment))
        {
            forcedMarch.Remove(regiment);
            Debug.Log("MOVE-09F7|Unit=" + regiment.RegimentName + "|ForcedMarch=False");
        }
        else
        {
            forcedMarch.Add(regiment);
            Debug.Log("MOVE-09F7|Unit=" + regiment.RegimentName + "|ForcedMarch=True");
        }
    }

    private void ToggleSelectedDanish()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        Regiment first = null;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected && !regiment.IsRouted)
            {
                first = regiment;
                break;
            }
        }

        if (first == null)
            return;

        bool enable = !forcedMarch.Contains(first);
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected || regiment.IsRouted)
                continue;

            if (enable)
                forcedMarch.Add(regiment);
            else
                forcedMarch.Remove(regiment);

            Debug.Log("MOVE-09F7|Unit=" + regiment.RegimentName + "|ForcedMarch=" + enable);
        }
    }

    private void OnGUI()
    {
        Regiment selected = GetFirstSelectedDanish();
        if (selected == null)
            return;

        GUI.depth = -2200;
        bool active = forcedMarch.Contains(selected) && !selected.IsRouted;
        string label = active ? "[TVANG G]" : "TVANG G";
        Rect rect = new Rect(475f, Screen.height - 31f, 88f, 27f);

        if (GUI.Button(rect, label))
            ToggleSelectedDanish();
    }

    private Regiment GetFirstSelectedDanish()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                return regiment;

        return null;
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        List<Regiment> remove = null;
        foreach (Regiment regiment in forcedMarch)
        {
            if (regiment != null && active.Contains(regiment))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(regiment);
        }

        if (remove == null)
            return;
        for (int i = 0; i < remove.Count; i++)
            forcedMarch.Remove(remove[i]);
    }

    private static float GetWalkSpeed(BattleTeam team)
    {
        return team == BattleTeam.Denmark ? DenmarkWalkSpeed : PrussiaWalkSpeed;
    }

    private static float GetForcedSpeed(BattleTeam team)
    {
        return team == BattleTeam.Denmark ? DenmarkForcedSpeed : PrussiaForcedSpeed;
    }

    private static float GetRoutRunSpeed(BattleTeam team)
    {
        return team == BattleTeam.Denmark ? DenmarkRoutRunSpeed : PrussiaRoutRunSpeed;
    }
}
