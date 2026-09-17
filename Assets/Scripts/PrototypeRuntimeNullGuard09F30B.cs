using System.Reflection;
using UnityEngine;

// v00.00.09f30b
// Runtime hardening for the repeated NullReference errors observed in the F30A Unity log.
// This guard does not take movement/combat ownership. It only repairs transient singleton/
// reflection references before the normal legacy systems execute.
[DefaultExecutionOrder(-32000)]
public sealed class PrototypeRuntimeNullGuard09F30B : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    private FieldInfo battleManagerInstanceBackingField;
    private FieldInfo targetingPendingTargetPickSlot;
    private FieldInfo targetingPendingUnitsSlot;
    private FieldInfo targetingIssueChargeSlot;

    private bool loggedBattleRepair;
    private bool loggedChargeRepair;
    private bool loggedChargeDisable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRuntimeNullGuard09F30B>() == null)
            new GameObject("PrototypeRuntimeNullGuard_v000009f30b")
                .AddComponent<PrototypeRuntimeNullGuard09F30B>();
    }

    private void Awake()
    {
        battleManagerInstanceBackingField = typeof(BattleManager)
            .GetField("<Instance>k__BackingField", PrivateStatic);

        System.Type targetingType = typeof(PrototypeChargeTargeting09F29K);
        targetingPendingTargetPickSlot = targetingType.GetField("pendingTargetPickField", PrivateInstance);
        targetingPendingUnitsSlot = targetingType.GetField("pendingUnitsField", PrivateInstance);
        targetingIssueChargeSlot = targetingType.GetField("issueChargeMethod", PrivateInstance);
    }

    private void Update()
    {
        RepairBattleManagerReference();
        RepairChargeTargetingReflection();
    }

    private void RepairBattleManagerReference()
    {
        if (BattleManager.Instance != null)
            return;

        BattleManager manager = UnityEngine.Object.FindAnyObjectByType<BattleManager>();
        if (manager == null || battleManagerInstanceBackingField == null)
            return;

        battleManagerInstanceBackingField.SetValue(null, manager);
        if (!loggedBattleRepair)
        {
            loggedBattleRepair = true;
            Debug.Log("RUNTIME-GUARD-09F30B|BattleManagerInstance=Repaired|Prevents=Regiment.FindNearestEnemyInFireArc_NullRef");
        }
    }

    private void RepairChargeTargetingReflection()
    {
        PrototypeChargeTargeting09F29K targeting = UnityEngine.Object.FindAnyObjectByType<PrototypeChargeTargeting09F29K>();
        if (targeting == null || !targeting.enabled)
            return;

        if (targetingPendingTargetPickSlot == null || targetingPendingUnitsSlot == null || targetingIssueChargeSlot == null)
        {
            DisableBrokenTargeting(targeting, "F29K_PRIVATE_SLOTS_MISSING");
            return;
        }

        FieldInfo pendingTargetPick = targetingPendingTargetPickSlot.GetValue(targeting) as FieldInfo;
        FieldInfo pendingUnits = targetingPendingUnitsSlot.GetValue(targeting) as FieldInfo;
        MethodInfo issueCharge = targetingIssueChargeSlot.GetValue(targeting) as MethodInfo;

        bool changed = false;
        if (pendingTargetPick == null)
        {
            pendingTargetPick = typeof(PrototypeInfantryCharge09F25).GetField("pendingTargetPick", PrivateInstance);
            if (pendingTargetPick != null)
            {
                targetingPendingTargetPickSlot.SetValue(targeting, pendingTargetPick);
                changed = true;
            }
        }

        if (pendingUnits == null)
        {
            pendingUnits = typeof(PrototypeInfantryCharge09F25).GetField("pendingUnits", PrivateInstance);
            if (pendingUnits != null)
            {
                targetingPendingUnitsSlot.SetValue(targeting, pendingUnits);
                changed = true;
            }
        }

        if (issueCharge == null)
        {
            issueCharge = typeof(PrototypeInfantryCharge09F25).GetMethod("IssueCharge", PrivateInstance);
            if (issueCharge != null)
            {
                targetingIssueChargeSlot.SetValue(targeting, issueCharge);
                changed = true;
            }
        }

        if (pendingTargetPick == null || pendingUnits == null || issueCharge == null)
        {
            DisableBrokenTargeting(targeting, "F25_REFLECTION_TARGET_MISSING");
            return;
        }

        if (changed && !loggedChargeRepair)
        {
            loggedChargeRepair = true;
            Debug.Log("RUNTIME-GUARD-09F30B|ChargeTargetingReflection=Repaired|Prevents=CaptureLegacyTargetPick_NullRef");
        }
    }

    private void DisableBrokenTargeting(PrototypeChargeTargeting09F29K targeting, string reason)
    {
        if (targeting == null)
            return;

        targeting.enabled = false;
        if (!loggedChargeDisable)
        {
            loggedChargeDisable = true;
            Debug.LogWarning("RUNTIME-GUARD-09F30B|ChargeTargeting09F29K=Disabled|Reason=" + reason +
                             "|LegacyF25ChargeEngineRemains=True");
        }
    }
}
