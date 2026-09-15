using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29c
// Unified compact bottom HUD for Major/Battalion and Company/Kaptajn control.
// Draws over the legacy 90 px command strips and consumes the IMGUI event afterwards,
// so old buttons remain present as logic dependencies but cannot double-fire underneath.
// Internal prototype ids such as "1. Regiment" are kept for compatibility; visible
// Danish company names are normalized to 1.-8. Kompagni.
[DefaultExecutionOrder(-30000)]
public sealed class PrototypeCommandHud09F29C : MonoBehaviour
{
    public static PrototypeCommandHud09F29C Instance { get; private set; }

    private const float HudHeight = 90f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private FieldInfo selectedBattalionField;
    private FieldInfo pendingMajorOrderField;
    private MethodInfo clearPlayerRouteMethod;
    private MethodInfo enterSquareMethod;
    private MethodInfo leaveSquareMethod;
    private MethodInfo beginChargePickMethod;
    private MethodInfo finishChargeMethod;
    private MethodInfo cancelWithdrawalMethod;
    private FieldInfo withdrawalActiveField;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle sectionStyle;
    private GUIStyle greenButtonStyle;
    private GUIStyle redButtonStyle;
    private GUIStyle neutralButtonStyle;
    private GUIStyle smallValueStyle;

    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D greenTexture;
    private Texture2D greenHoverTexture;
    private Texture2D redTexture;
    private Texture2D redHoverTexture;
    private Texture2D neutralTexture;
    private Texture2D neutralHoverTexture;

    private bool namesApplied;
    private bool installLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCommandHud09F29C>() == null)
            new GameObject("PrototypeCommandHud_v000009f29c")
                .AddComponent<PrototypeCommandHud09F29C>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", PrivateInstance);
        pendingMajorOrderField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("pendingOrder", PrivateInstance);
        clearPlayerRouteMethod = typeof(PlayerCommander)
            .GetMethod("ClearRoute", PrivateInstance);
        enterSquareMethod = typeof(PrototypeInfantrySquare09F29)
            .GetMethod("EnterSquare", PrivateInstance);
        leaveSquareMethod = typeof(PrototypeInfantrySquare09F29)
            .GetMethod("LeaveSquare", PrivateInstance);
        beginChargePickMethod = typeof(PrototypeInfantryCharge09F25)
            .GetMethod("BeginTargetPickFromSelection", PrivateInstance);
        finishChargeMethod = typeof(PrototypeInfantryCharge09F25)
            .GetMethod("FinishCharge", PrivateInstance);
        cancelWithdrawalMethod = typeof(PrototypeFightingWithdrawal09F10)
            .GetMethod("Cancel", PrivateInstance);
        withdrawalActiveField = typeof(PrototypeFightingWithdrawal09F10)
            .GetField("active", PrivateInstance);

        BuildStyles();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        ApplyVisibleCompanyNames();

        if (!installLogged)
        {
            installLogged = true;
            Debug.Log(
                "HUD-09F29C|Installed=True|Height=90|UnifiedMajorCompany=True|" +
                "StateColours=GreenActiveRedInactive|SquareButton=True|StopButton=True|" +
                "VisibleCompanyNames=1-8.Kompagni");
        }
    }

    private void OnGUI()
    {
        int selectedMajor = GetSelectedBattalionIndex();
        List<Regiment> selectedCompanies = GetSelectedDanishCompanies();

        if (selectedMajor < 0 && selectedCompanies.Count == 0)
            return;

        BuildStyles();
        GUI.depth = -6000;

        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        if (selectedMajor >= 0)
            DrawMajorHud(panel, selectedMajor);
        else
            DrawCompanyHud(panel, selectedCompanies);

        // Our HUD uses exactly the already-isolated legacy bottom-command area.
        // Consume GUI mouse events after our buttons have processed them so the old
        // overlapping IMGUI controls cannot also execute the same click.
        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition))
        {
            if (current.type == EventType.MouseDown ||
                current.type == EventType.MouseUp ||
                current.type == EventType.MouseDrag ||
                current.type == EventType.ScrollWheel)
            {
                current.Use();
            }
        }
    }

    private void DrawMajorHud(Rect panel, int battalionIndex)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionIndex < 0 || battalionIndex >= hierarchy.BattalionCount)
            return;

        IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
        bool aiOn = hierarchy.GetBattalionAIEnabled(battalionIndex);
        OfficerAIDoctrine doctrine = hierarchy.GetBattalionDoctrine(battalionIndex);
        MajorOrder09F18 pending = GetPendingMajorOrder();

        float width = panel.width;
        float infoWidth = Mathf.Clamp(width * 0.235f, 250f, 330f);
        float companyWidth = Mathf.Clamp(width * 0.34f, 390f, 540f);
        float commandX = infoWidth + 8f;
        float companyX = width - companyWidth - 6f;
        float commandWidth = Mathf.Max(360f, companyX - commandX - 7f);

        string majorName = battalionIndex == 0 ? "MAJOR A" : "MAJOR B";
        string battalionName = (battalionIndex + 1) + ". BATALJON";

        GUI.Box(new Rect(5f, panel.y + 3f, width - 10f, 17f),
            battalionName + " | " + majorName + " | BATALJONSKOMMANDO", headerStyle);

        float y = panel.y + 21f;
        DrawSectionLabel(new Rect(7f, y, infoWidth - 12f, 10f), "ENHEDSINFO / AI");

        AggregateStats stats = CalculateStats(companies);
        GUI.Label(new Rect(9f, y + 10f, infoWidth - 14f, 14f),
            "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses +
            " | Moral " + stats.Morale.ToString("0"), smallValueStyle);
        GUI.Label(new Rect(9f, y + 23f, infoWidth - 14f, 14f),
            "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") + " r/m", smallValueStyle);

        float stateY = y + 39f;
        float gap = 3f;
        float stateW = (infoWidth - 15f - gap * 3f) / 4f;
        if (GUI.Button(new Rect(8f, stateY, stateW, 19f), aiOn ? "AI ON" : "AI OFF", StateStyle(aiOn)))
            hierarchy.ToggleBattalionAI(battalionIndex);
        if (GUI.Button(new Rect(8f + (stateW + gap), stateY, stateW, 19f), "DEF",
                StateStyle(doctrine == OfficerAIDoctrine.Defensive)))
            hierarchy.SetBattalionDoctrine(battalionIndex, OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(8f + (stateW + gap) * 2f, stateY, stateW, 19f), "BAL",
                StateStyle(doctrine == OfficerAIDoctrine.Balanced)))
            hierarchy.SetBattalionDoctrine(battalionIndex, OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(8f + (stateW + gap) * 3f, stateY, stateW, 19f), "OFF",
                StateStyle(doctrine == OfficerAIDoctrine.Offensive)))
            hierarchy.SetBattalionDoctrine(battalionIndex, OfficerAIDoctrine.Offensive);

        DrawSectionLabel(new Rect(commandX, y, commandWidth, 10f), "BATALJONSORDRER");
        float orderY1 = y + 11f;
        float orderY2 = y + 36f;
        float orderGap = 4f;
        float orderW = (commandWidth - orderGap * 2f) / 3f;

        DrawMajorOrderButton(new Rect(commandX, orderY1, orderW, 21f), "ANGRIB HER", MajorOrder09F18.AttackHere, pending, battalionIndex);
        DrawMajorOrderButton(new Rect(commandX + orderW + orderGap, orderY1, orderW, 21f), "FORSVAR HER", MajorOrder09F18.DefendHere, pending, battalionIndex);
        DrawMajorOrderButton(new Rect(commandX + (orderW + orderGap) * 2f, orderY1, orderW, 21f), "RYK FREM", MajorOrder09F18.AdvanceHere, pending, battalionIndex);
        DrawMajorOrderButton(new Rect(commandX, orderY2, orderW, 21f), "TILBAGETRÆK", MajorOrder09F18.WithdrawHere, pending, battalionIndex);
        DrawMajorOrderButton(new Rect(commandX + orderW + orderGap, orderY2, orderW, 21f), "SAML", MajorOrder09F18.AssembleHere, pending, battalionIndex);

        if (GUI.Button(new Rect(commandX + (orderW + orderGap) * 2f, orderY2, orderW, 21f),
                "STOP / HOLD", pending == MajorOrder09F18.HoldPosition ? greenButtonStyle : redButtonStyle))
        {
            SetPendingMajorOrder(MajorOrder09F18.None);
            hierarchy.IssueBattalionOrder(
                battalionIndex,
                MajorOrder09F18.HoldPosition,
                hierarchy.GetBattalionCenter(battalionIndex),
                false);
        }

        DrawSectionLabel(new Rect(companyX, y, companyWidth - 4f, 10f), "KOMPAGNIER UNDER MAJOR — MÆND / TAB / MORAL / AMMO");
        if (companies != null)
        {
            for (int i = 0; i < companies.Count && i < 4; i++)
            {
                Regiment regiment = companies[i];
                if (regiment == null)
                    continue;

                int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
                int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
                string status = PrototypeUnitNames09F29C.Get(regiment) +
                    "  " + regiment.CurrentStrength + "/" + regiment.InitialStrength +
                    "  T" + losses +
                    "  M" + regiment.Morale.ToString("0") +
                    "  A" + ammo;
                GUI.Label(new Rect(companyX + 2f, y + 11f + i * 13f, companyWidth - 7f, 13f), status, smallValueStyle);
            }
        }
    }

    private void DrawMajorOrderButton(
        Rect rect,
        string label,
        MajorOrder09F18 order,
        MajorOrder09F18 pending,
        int battalionIndex)
    {
        if (GUI.Button(rect, label, StateStyle(pending == order)))
        {
            PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
            if (hierarchy == null || !hierarchy.Installed)
                return;

            // Keep the selected Major unchanged and let the existing F27 world-input
            // handler consume the next battlefield click using its normal route/slot logic.
            SetPendingMajorOrder(order);
            Debug.Log("HUD-09F29C|MajorOrderPending=True|Battalion=" + (battalionIndex + 1) + "|Order=" + order);
        }
    }

    private void DrawCompanyHud(Rect panel, List<Regiment> selected)
    {
        if (selected == null || selected.Count == 0)
            return;

        Regiment first = selected[0];
        OfficerAIController firstController = first.GetComponent<OfficerAIController>();

        bool allAiOn = All(selected, r =>
        {
            OfficerAIController c = r.GetComponent<OfficerAIController>();
            return c != null && c.AIEnabled;
        });
        bool allAiOff = All(selected, r =>
        {
            OfficerAIController c = r.GetComponent<OfficerAIController>();
            return c == null || !c.AIEnabled;
        });

        bool sameDoctrine = firstController != null && All(selected, r =>
        {
            OfficerAIController c = r.GetComponent<OfficerAIController>();
            return c != null && c.Doctrine == firstController.Doctrine;
        });

        bool sameFire = All(selected, r => r.FirePolicy == first.FirePolicy);
        bool allForced = All(selected, r => PrototypeForcedMarch09F7.IsForcedMarch(r));
        bool anyCharging = Any(selected, r =>
            PrototypeInfantryCharge09F25.Instance != null && PrototypeInfantryCharge09F25.Instance.IsCharging(r));
        bool allSquare = All(selected, r => PrototypeInfantrySquare09F29.IsInSquare(r));
        bool allLine = !allSquare && All(selected, r => r.Formation == RegimentFormation.Line && !PrototypeInfantrySquare09F29.IsInSquare(r));
        bool allColumn = !allSquare && All(selected, r => r.Formation == RegimentFormation.Column && !PrototypeInfantrySquare09F29.IsInSquare(r));
        PrototypeWithdrawalRange09F10? withdrawalMode = GetCommonWithdrawalMode(selected);

        float width = panel.width;
        float infoWidth = Mathf.Clamp(width * 0.22f, 235f, 305f);
        float aiWidth = Mathf.Clamp(width * 0.19f, 210f, 270f);
        float fireWidth = Mathf.Clamp(width * 0.20f, 220f, 285f);
        float infoX = 6f;
        float aiX = infoX + infoWidth + 5f;
        float fireX = aiX + aiWidth + 5f;
        float commandX = fireX + fireWidth + 5f;
        float commandWidth = width - commandX - 6f;

        string title = selected.Count == 1
            ? PrototypeUnitNames09F29C.Get(first) + " | KAPTAJN"
            : selected.Count + " KOMPAGNIER VALGT";
        GUI.Box(new Rect(5f, panel.y + 3f, width - 10f, 17f), title + " | KOMPAGNIKOMMANDO", headerStyle);

        float y = panel.y + 21f;
        DrawSectionLabel(new Rect(infoX + 1f, y, infoWidth - 3f, 10f), "ENHEDSINFO");
        AggregateStats stats = CalculateStats(selected);
        GUI.Label(new Rect(infoX + 3f, y + 10f, infoWidth - 6f, 13f),
            "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses +
            " | Moral " + stats.Morale.ToString("0"), smallValueStyle);
        GUI.Label(new Rect(infoX + 3f, y + 23f, infoWidth - 6f, 13f),
            "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") +
            " | " + GetFormationLabel(first), smallValueStyle);
        GUI.Label(new Rect(infoX + 3f, y + 36f, infoWidth - 6f, 13f),
            first.WeaponShortName + " | Fire " + first.GetFirePolicyLabel(), smallValueStyle);
        if (selected.Count == 1 && firstController != null)
            GUI.Label(new Rect(infoX + 3f, y + 49f, infoWidth - 6f, 13f),
                firstController.CurrentTask + " | " + firstController.ReasonCode, smallValueStyle);

        DrawSectionLabel(new Rect(aiX, y, aiWidth, 10f), "AI / DOKTRIN");
        float stateY = y + 11f;
        const float gap = 3f;
        float aiButtonW = (aiWidth - gap * 3f) / 4f;
        string aiLabel = allAiOn ? "AI ON" : (allAiOff ? "AI OFF" : "AI MIX");
        if (GUI.Button(new Rect(aiX, stateY, aiButtonW, 20f), aiLabel, StateStyle(allAiOn)))
            SetSelectedAi(selected, !allAiOn);

        OfficerAIDoctrine doctrine = firstController != null ? firstController.Doctrine : OfficerAIDoctrine.Balanced;
        if (GUI.Button(new Rect(aiX + (aiButtonW + gap), stateY, aiButtonW, 20f), "DEF",
                StateStyle(sameDoctrine && doctrine == OfficerAIDoctrine.Defensive)))
            SetSelectedDoctrine(selected, OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(aiX + (aiButtonW + gap) * 2f, stateY, aiButtonW, 20f), "BAL",
                StateStyle(sameDoctrine && doctrine == OfficerAIDoctrine.Balanced)))
            SetSelectedDoctrine(selected, OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(aiX + (aiButtonW + gap) * 3f, stateY, aiButtonW, 20f), "OFF",
                StateStyle(sameDoctrine && doctrine == OfficerAIDoctrine.Offensive)))
            SetSelectedDoctrine(selected, OfficerAIDoctrine.Offensive);

        GUI.Label(new Rect(aiX, y + 36f, 63f, 12f), "Aggression", sectionStyle);
        float currentAgg = firstController != null ? firstController.OrderAggressiveness : 50f;
        float slider = GUI.HorizontalSlider(new Rect(aiX + 64f, y + 40f, aiWidth - 68f, 10f), currentAgg, 0f, 100f);
        if (firstController != null && Mathf.Abs(slider - currentAgg) >= 0.5f)
            SetSelectedAggression(selected, slider);
        GUI.Label(new Rect(aiX, y + 51f, aiWidth, 12f), "AGG " + currentAgg.ToString("0"), smallValueStyle);

        DrawSectionLabel(new Rect(fireX, y, fireWidth, 10f), "SKYDNING");
        float fireY = y + 11f;
        float fireButtonW = (fireWidth - gap * 3f) / 4f;
        DrawFireButton(new Rect(fireX, fireY, fireButtonW, 20f), "HOLD", RegimentFirePolicy.HoldFire, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + fireButtonW + gap, fireY, fireButtonW, 20f), "CLOSE", RegimentFirePolicy.CloseRange, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + (fireButtonW + gap) * 2f, fireY, fireButtonW, 20f), "MED", RegimentFirePolicy.MediumRange, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + (fireButtonW + gap) * 3f, fireY, fireButtonW, 20f), "LONG", RegimentFirePolicy.LongRange, sameFire, first.FirePolicy, selected);
        GUI.Label(new Rect(fireX, y + 36f, fireWidth, 13f),
            "CLOSE " + first.CloseRange.ToString("0") + "m | MED " + first.EffectiveRange.ToString("0") +
            "m | LONG " + first.MaximumRange.ToString("0") + "m", smallValueStyle);
        GUI.Label(new Rect(fireX, y + 50f, fireWidth, 13f),
            anyCharging ? "Status: CHARGE" : (withdrawalMode.HasValue ? "Status: TILBAGETRÆK" : "Status: klar"),
            smallValueStyle);

        DrawSectionLabel(new Rect(commandX, y, commandWidth, 10f), "ORDRER / BEVÆGELSE");
        float actionY = y + 11f;
        float actionGap = 3f;
        float actionW = (commandWidth - actionGap * 5f) / 6f;

        if (GUI.Button(new Rect(commandX, actionY, actionW, 20f), "→ MED",
                StateStyle(withdrawalMode == PrototypeWithdrawalRange09F10.Medium)))
            BeginWithdrawal(PrototypeWithdrawalRange09F10.Medium);
        if (GUI.Button(new Rect(commandX + (actionW + actionGap), actionY, actionW, 20f), "→ LONG",
                StateStyle(withdrawalMode == PrototypeWithdrawalRange09F10.Long)))
            BeginWithdrawal(PrototypeWithdrawalRange09F10.Long);
        if (GUI.Button(new Rect(commandX + (actionW + actionGap) * 2f, actionY, actionW, 20f), "→ UD",
                StateStyle(withdrawalMode == PrototypeWithdrawalRange09F10.OutOfRange)))
            BeginWithdrawal(PrototypeWithdrawalRange09F10.OutOfRange);
        if (GUI.Button(new Rect(commandX + (actionW + actionGap) * 3f, actionY, actionW, 20f), "TVANG",
                StateStyle(allForced)))
            SetForcedMarch(selected, !allForced);
        if (GUI.Button(new Rect(commandX + (actionW + actionGap) * 4f, actionY, actionW, 20f), "CHARGE",
                StateStyle(anyCharging)))
            BeginChargePick();
        if (GUI.Button(new Rect(commandX + (actionW + actionGap) * 5f, actionY, actionW, 20f), "STOP", redButtonStyle))
            StopSelected(selected);

        DrawSectionLabel(new Rect(commandX, y + 35f, commandWidth, 10f), "FORMATION");
        float formationY = y + 46f;
        float formationW = (commandWidth - actionGap * 2f) / 3f;
        if (GUI.Button(new Rect(commandX, formationY, formationW, 20f), "LINJE",
                StateStyle(allLine)))
            SetSelectedFormation(selected, RegimentFormation.Line, false);
        if (GUI.Button(new Rect(commandX + formationW + actionGap, formationY, formationW, 20f), "KOLONNE",
                StateStyle(allColumn)))
            SetSelectedFormation(selected, RegimentFormation.Column, false);
        if (GUI.Button(new Rect(commandX + (formationW + actionGap) * 2f, formationY, formationW, 20f), "SQUARE",
                StateStyle(allSquare)))
            SetSelectedFormation(selected, RegimentFormation.Line, true);
    }

    private void DrawFireButton(
        Rect rect,
        string label,
        RegimentFirePolicy policy,
        bool same,
        RegimentFirePolicy current,
        List<Regiment> selected)
    {
        if (GUI.Button(rect, label, StateStyle(same && current == policy)))
        {
            for (int i = 0; i < selected.Count; i++)
                selected[i].SetFirePolicy(policy);
        }
    }

    private void SetSelectedAi(List<Regiment> selected, bool enabledValue)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled != enabledValue)
                controller.SetAIEnabled(enabledValue);
        }
    }

    private void SetSelectedDoctrine(List<Regiment> selected, OfficerAIDoctrine doctrine)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            OfficerAIController controller = selected[i].GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetDoctrine(doctrine);
        }
    }

    private void SetSelectedAggression(List<Regiment> selected, float value)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            OfficerAIController controller = selected[i].GetComponent<OfficerAIController>();
            if (controller != null)
                controller.SetOrderAggressiveness(value);
        }
    }

    private static void BeginWithdrawal(PrototypeWithdrawalRange09F10 mode)
    {
        if (PrototypeFightingWithdrawal09F10.Instance != null)
            PrototypeFightingWithdrawal09F10.Instance.BeginForSelected(mode);
    }

    private void BeginChargePick()
    {
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && beginChargePickMethod != null)
            beginChargePickMethod.Invoke(charge, null);
    }

    private void SetForcedMarch(List<Regiment> selected, bool enable)
    {
        PrototypeForcedMarch09F7 manager = PrototypeForcedMarch09F7.Instance;
        if (manager == null)
            return;

        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (PrototypeForcedMarch09F7.IsForcedMarch(regiment) != enable)
                manager.Toggle(regiment);
        }
    }

    private void StopSelected(List<Regiment> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (regiment == null || regiment.IsRouted)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            ClearPlayerRoute(regiment);
            CancelWithdrawal(regiment, true, "HUD_STOP");
            FinishCharge(regiment, "HUD_STOP");

            if (PrototypeForcedMarch09F7.Instance != null && PrototypeForcedMarch09F7.IsForcedMarch(regiment))
                PrototypeForcedMarch09F7.Instance.Toggle(regiment);

            regiment.OrderHold();
        }

        Debug.Log("HUD-09F29C|Command=STOP|Companies=" + selected.Count + "|AI=OFF|RoutesCleared=True");
    }

    private void SetSelectedFormation(List<Regiment> selected, RegimentFormation formation, bool square)
    {
        PrototypeInfantrySquare09F29 squareManager = PrototypeInfantrySquare09F29.Instance;

        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (regiment == null || regiment.IsRouted)
                continue;

            // Formation buttons are direct company orders and therefore detach from
            // higher command just like the existing F/C/Q hotkeys.
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            if (square)
            {
                if (!PrototypeInfantrySquare09F29.IsInSquare(regiment) && squareManager != null && enterSquareMethod != null)
                {
                    Vector3 threatHint = regiment.transform.position + regiment.transform.forward * 80f;
                    enterSquareMethod.Invoke(squareManager, new object[] { regiment, false, threatHint });
                }
                continue;
            }

            if (PrototypeInfantrySquare09F29.IsInSquare(regiment) && squareManager != null && leaveSquareMethod != null)
                leaveSquareMethod.Invoke(squareManager, new object[] { regiment, "HUD_FORMATION", true });

            regiment.SetFormation(formation);
        }

        Debug.Log("HUD-09F29C|Command=FORMATION|Square=" + square + "|Formation=" + formation + "|Companies=" + selected.Count);
    }

    private void FinishCharge(Regiment regiment, string reason)
    {
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge == null || finishChargeMethod == null || !charge.IsCharging(regiment))
            return;
        finishChargeMethod.Invoke(charge, new object[] { regiment, reason });
    }

    private void CancelWithdrawal(Regiment regiment, bool restoreFire, string reason)
    {
        PrototypeFightingWithdrawal09F10 withdrawal = PrototypeFightingWithdrawal09F10.Instance;
        if (withdrawal == null || cancelWithdrawalMethod == null || !withdrawal.IsWithdrawing(regiment))
            return;
        cancelWithdrawalMethod.Invoke(withdrawal, new object[] { regiment, restoreFire, reason });
    }

    private void ClearPlayerRoute(Regiment regiment)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && clearPlayerRouteMethod != null && regiment != null)
            clearPlayerRouteMethod.Invoke(commander, new object[] { regiment });
    }

    private PrototypeWithdrawalRange09F10? GetCommonWithdrawalMode(List<Regiment> selected)
    {
        PrototypeFightingWithdrawal09F10 withdrawal = PrototypeFightingWithdrawal09F10.Instance;
        if (withdrawal == null || withdrawalActiveField == null || selected.Count == 0)
            return null;

        IDictionary active = withdrawalActiveField.GetValue(withdrawal) as IDictionary;
        if (active == null)
            return null;

        PrototypeWithdrawalRange09F10? common = null;
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (!active.Contains(regiment))
                return null;

            object state = active[regiment];
            if (state == null)
                return null;

            FieldInfo modeField = state.GetType().GetField("Mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (modeField == null)
                return null;

            object value = modeField.GetValue(state);
            if (!(value is PrototypeWithdrawalRange09F10))
                return null;

            PrototypeWithdrawalRange09F10 mode = (PrototypeWithdrawalRange09F10)value;
            if (!common.HasValue)
                common = mode;
            else if (common.Value != mode)
                return null;
        }

        return common;
    }

    private int GetSelectedBattalionIndex()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || selectedBattalionField == null)
            return -1;

        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private MajorOrder09F18 GetPendingMajorOrder()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || pendingMajorOrderField == null)
            return MajorOrder09F18.None;

        object value = pendingMajorOrderField.GetValue(hierarchy);
        return value is MajorOrder09F18 ? (MajorOrder09F18)value : MajorOrder09F18.None;
    }

    private void SetPendingMajorOrder(MajorOrder09F18 order)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && pendingMajorOrderField != null)
            pendingMajorOrderField.SetValue(hierarchy, order);
    }

    private static List<Regiment> GetSelectedDanishCompanies()
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected && !regiment.IsRouted)
                result.Add(regiment);
        }
        return result;
    }

    private void ApplyVisibleCompanyNames()
    {
        if (namesApplied)
            return;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed)
            return;

        int renamed = 0;
        for (int battalion = 0; battalion < hierarchy.BattalionCount; battalion++)
        {
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalion);
            if (companies == null)
                continue;

            for (int i = 0; i < companies.Count; i++)
            {
                Regiment regiment = companies[i];
                if (regiment == null)
                    continue;

                string visible = PrototypeUnitNames09F29C.Get(regiment);
                if (!string.IsNullOrEmpty(visible) && regiment.gameObject.name != visible)
                {
                    regiment.gameObject.name = visible;
                    renamed++;
                }
            }
        }

        namesApplied = true;
        Debug.Log("HUD-09F29C|VisibleCompanyRename=True|Renamed=" + renamed + "|InternalIdsPreserved=True");
    }

    private static string GetFormationLabel(Regiment regiment)
    {
        if (regiment == null)
            return "?";
        if (PrototypeInfantrySquare09F29.IsInSquare(regiment))
            return PrototypeInfantrySquare09F29.IsSquareReady(regiment) ? "SQUARE" : "SQUARE...";
        return regiment.Formation == RegimentFormation.Column ? "KOLONNE" : "LINJE";
    }

    private static bool All(List<Regiment> units, Func<Regiment, bool> predicate)
    {
        if (units == null || units.Count == 0)
            return false;
        for (int i = 0; i < units.Count; i++)
            if (units[i] == null || !predicate(units[i]))
                return false;
        return true;
    }

    private static bool Any(List<Regiment> units, Func<Regiment, bool> predicate)
    {
        if (units == null)
            return false;
        for (int i = 0; i < units.Count; i++)
            if (units[i] != null && predicate(units[i]))
                return true;
        return false;
    }

    private struct AggregateStats
    {
        public int Initial;
        public int Current;
        public int Losses;
        public float Morale;
        public float Cohesion;
        public float Ammo;
    }

    private static AggregateStats CalculateStats(IReadOnlyList<Regiment> units)
    {
        AggregateStats result = new AggregateStats();
        if (units == null)
            return result;

        int count = 0;
        float morale = 0f;
        float cohesion = 0f;
        float ammo = 0f;

        for (int i = 0; i < units.Count; i++)
        {
            Regiment regiment = units[i];
            if (regiment == null)
                continue;

            result.Initial += regiment.InitialStrength;
            result.Current += regiment.CurrentStrength;
            morale += regiment.Morale;
            cohesion += regiment.Cohesion;
            ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
            count++;
        }

        result.Losses = Mathf.Max(0, result.Initial - result.Current);
        if (count > 0)
        {
            result.Morale = morale / count;
            result.Cohesion = cohesion / count;
            result.Ammo = ammo / count;
        }
        return result;
    }

    private static AggregateStats CalculateStats(List<Regiment> units)
    {
        return CalculateStats((IReadOnlyList<Regiment>)units);
    }

    private GUIStyle StateStyle(bool active)
    {
        return active ? greenButtonStyle : redButtonStyle;
    }

    private void DrawSectionLabel(Rect rect, string text)
    {
        GUI.Label(rect, text, sectionStyle);
    }

    private void BuildStyles()
    {
        if (panelStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.055f, 0.065f, 0.055f, 0.995f), "HUD29C_PANEL");
        headerTexture = MakeTexture(new Color(0.13f, 0.16f, 0.11f, 1f), "HUD29C_HEADER");
        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "HUD29C_GREEN");
        greenHoverTexture = MakeTexture(new Color(0.22f, 0.56f, 0.25f, 1f), "HUD29C_GREEN_HOVER");
        redTexture = MakeTexture(new Color(0.43f, 0.14f, 0.12f, 1f), "HUD29C_RED");
        redHoverTexture = MakeTexture(new Color(0.57f, 0.19f, 0.16f, 1f), "HUD29C_RED_HOVER");
        neutralTexture = MakeTexture(new Color(0.20f, 0.22f, 0.17f, 1f), "HUD29C_NEUTRAL");
        neutralHoverTexture = MakeTexture(new Color(0.30f, 0.33f, 0.24f, 1f), "HUD29C_NEUTRAL_HOVER");

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.border = new RectOffset(1, 1, 1, 1);
        panelStyle.padding = new RectOffset(4, 4, 3, 3);

        headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = headerTexture;
        headerStyle.normal.textColor = new Color(0.96f, 0.94f, 0.84f);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.padding = new RectOffset(7, 5, 1, 1);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = new Color(0.94f, 0.93f, 0.85f);
        labelStyle.fontSize = 9;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        sectionStyle = new GUIStyle(labelStyle);
        sectionStyle.normal.textColor = new Color(0.77f, 0.67f, 0.35f);
        sectionStyle.fontSize = 8;
        sectionStyle.fontStyle = FontStyle.Bold;

        smallValueStyle = new GUIStyle(labelStyle);
        smallValueStyle.fontSize = 8;
        smallValueStyle.clipping = TextClipping.Clip;

        greenButtonStyle = MakeButtonStyle(greenTexture, greenHoverTexture);
        redButtonStyle = MakeButtonStyle(redTexture, redHoverTexture);
        neutralButtonStyle = MakeButtonStyle(neutralTexture, neutralHoverTexture);
    }

    private static GUIStyle MakeButtonStyle(Texture2D normal, Texture2D hover)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = hover;
        style.focused.background = hover;
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.fontSize = 8;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(2, 2, 1, 1);
        return style;
    }

    private static Texture2D MakeTexture(Color color, string name)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}

public static class PrototypeUnitNames09F29C
{
    // Compatibility mapping: old internal prototype ids are deliberately NOT changed,
    // because multiple older systems still use those strings for lookup/QA. Everything
    // visible to the player is normalized to company naming instead.
    public static string Get(Regiment regiment)
    {
        if (regiment == null)
            return "KOMPAGNI";

        switch (regiment.RegimentName)
        {
            case "1. Regiment": return "1. KOMPAGNI";
            case "5. Regiment": return "2. KOMPAGNI";
            case "2. Regiment": return "3. KOMPAGNI";
            case "3. Regiment": return "4. KOMPAGNI";
            case "5. Kompagni": return "5. KOMPAGNI";
            case "6. Kompagni": return "6. KOMPAGNI";
            case "7. Kompagni": return "7. KOMPAGNI";
            case "8. Kompagni": return "8. KOMPAGNI";
            default: return regiment.RegimentName.ToUpperInvariant();
        }
    }
}
