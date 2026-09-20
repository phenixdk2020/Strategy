using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

// v00.00.09f29h
// One visual language for Oberstløjtnant, Major and Kaptajn/company command.
// Supersedes the F29F regimental overlay and F29C Major/Company overlay during runtime.
// Legacy F27/F28 components stay enabled as command/state engines, but their old bottom
// panels are fully covered and their mouse events are consumed by this authoritative HUD.
// F29H compile hotfix: explicitly aliases Object to UnityEngine.Object so System.Object
// cannot collide with UnityEngine.Object in FindAnyObjectByType calls.
[DefaultExecutionOrder(-40000)]
public sealed class PrototypeUnifiedCommandHud09F29G : MonoBehaviour
{
    public static PrototypeUnifiedCommandHud09F29G Instance { get; private set; }

    private const float HudHeight = 96f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private FieldInfo selectedBattalionField;
    private FieldInfo playerSelectedField;
    private MethodInfo clearPlayerRouteMethod;
    private MethodInfo enterSquareMethod;
    private MethodInfo leaveSquareMethod;
    private MethodInfo beginChargePickMethod;
    private MethodInfo finishChargeMethod;
    private MethodInfo cancelWithdrawalMethod;
    private FieldInfo withdrawalActiveField;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle valueStyle;
    private GUIStyle greenButtonStyle;
    private GUIStyle redButtonStyle;
    private GUIStyle blueButtonStyle;
    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D greenTexture;
    private Texture2D greenHoverTexture;
    private Texture2D redTexture;
    private Texture2D redHoverTexture;
    private Texture2D blueTexture;
    private Texture2D blueHoverTexture;
    private Texture2D topBorderTexture;
    private bool logged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeUnifiedCommandHud09F29G>() == null)
            new GameObject("PrototypeUnifiedCommandHud_v000009f29g")
                .AddComponent<PrototypeUnifiedCommandHud09F29G>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        playerSelectedField = typeof(PlayerCommander).GetField("selected", PrivateInstance);
        clearPlayerRouteMethod = typeof(PlayerCommander).GetMethod("ClearRoute", PrivateInstance);
        enterSquareMethod = typeof(PrototypeInfantrySquare09F29).GetMethod("EnterSquare", PrivateInstance);
        leaveSquareMethod = typeof(PrototypeInfantrySquare09F29).GetMethod("LeaveSquare", PrivateInstance);
        beginChargePickMethod = typeof(PrototypeInfantryCharge09F25).GetMethod("BeginTargetPickFromSelection", PrivateInstance);
        finishChargeMethod = typeof(PrototypeInfantryCharge09F25).GetMethod("FinishCharge", PrivateInstance);
        cancelWithdrawalMethod = typeof(PrototypeFightingWithdrawal09F10).GetMethod("Cancel", PrivateInstance);
        withdrawalActiveField = typeof(PrototypeFightingWithdrawal09F10).GetField("active", PrivateInstance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        DisableSupersededHudComponents();
        ApplyVisibleCompanyNames();

        if (!logged)
        {
            logged = true;
            Debug.Log("HUD-09F30Q|Installed=True|Levels=DIVISION,BRIGADE,REGIMENT,BATTALION,COMPANY|SingleRenderer=True|FacingDragOrders=True|ActiveOrderBlue=True|BlackTopEdge=True");
        }
    }

    private static void DisableSupersededHudComponents()
    {
        PrototypeCommandHud09F29C majorCompany = Object.FindAnyObjectByType<PrototypeCommandHud09F29C>();
        if (majorCompany != null && majorCompany.enabled)
            majorCompany.enabled = false;

        PrototypeRegimentalHud09F29F regimental = Object.FindAnyObjectByType<PrototypeRegimentalHud09F29F>();
        if (regimental != null && regimental.enabled)
            regimental.enabled = false;

        PrototypeRegimentalHud09F29E oldRegimental = Object.FindAnyObjectByType<PrototypeRegimentalHud09F29E>();
        if (oldRegimental != null && oldRegimental.enabled)
            oldRegimental.enabled = false;
    }

    private void OnGUI()
    {
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        int selectedMajor = GetSelectedBattalionIndex();
        List<Regiment> selectedCompanies = GetSelectedCompanies();

        bool higherSelected =
            higher != null &&
            higher.Installed &&
            higher.SelectedLevel != PrototypeHigherCommandLevel09F30B.None;
        bool regimentSelected = regiment != null && regiment.Installed && regiment.Selected;

        if (!higherSelected && !regimentSelected &&
            selectedMajor < 0 && selectedCompanies.Count == 0)
            return;

        BuildStyles();
        GUI.depth = -120000;
        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        // F30O: explicit black top edge. This removes the inherited olive/green
        // line visible above the authoritative command HUD.
        if (topBorderTexture != null)
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), topBorderTexture);

        if (higherSelected)
            DrawHigherHud(panel, higher);
        else if (regimentSelected)
            DrawRegimentalHud(panel, regiment);
        else if (selectedMajor >= 0)
            DrawMajorHud(panel, selectedMajor);
        else
            DrawCompanyHud(panel, selectedCompanies);

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel))
        {
            current.Use();
        }
    }

    private void DrawHigherHud(
        Rect panel,
        PrototypeHigherCommandHQ09F30B higher)
    {
        PrototypeRegimentHierarchy09F27 hierarchy =
            PrototypeRegimentHierarchy09F27.Instance;
        PrototypeCavalryManager09F30 cavalry =
            PrototypeCavalryManager09F30.Instance;

        List<Regiment> companies = GetAllCompanies(hierarchy);
        AggregateStats stats = CalculateStats(companies);

        bool division =
            higher.SelectedLevel == PrototypeHigherCommandLevel09F30B.Division;
        string title = division
            ? "1. DIVISION | DIVISIONSCHEF | HØJERE KOMMANDO | F30S"
            : "1. BRIGADE | BRIGADECHEF | HØJERE KOMMANDO | F30S";

        GUI.Box(
            new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            title,
            headerStyle);

        // Literal F29G Regimental geometry.
        float width = panel.width;
        float infoW = Mathf.Clamp(width * 0.24f, 285f, 355f);
        float subW = Mathf.Clamp(width * 0.31f, 380f, 510f);
        float commandX = infoW + 8f;
        float subX = width - subW - 6f;
        float commandW = Mathf.Max(390f, subX - commandX - 7f);
        float y = panel.y + 22f;

        PrototypeHigherCommandLevel09F30B level = higher.SelectedLevel;
        DrawInfoAiBlock(
            new Rect(7f, y, infoW - 10f, 66f),
            stats,
            higher.GetAIEnabled(level),
            higher.GetDoctrine(level),
            () => higher.ToggleAI(level),
            d => higher.SetDoctrine(level, d));

        DrawSection(
            new Rect(commandX, y, commandW, 10f),
            division
                ? "DIVISIONSORDRER — KLIK = POSITION, TRÆK = FACING"
                : "BRIGADEORDRER — KLIK = POSITION, TRÆK = FACING");

        PrototypeOfficerFacingOrder09F29G facingInput =
            PrototypeOfficerFacingOrder09F29G.Instance;

        DrawOfficerOrderGrid(
            commandX,
            y + 11f,
            commandW,
            o => BeginHigherOrder(level, o),
            o => higher.HasHigherOrderActive(level, o),
            o => facingInput != null && facingInput.IsPendingHigher(level, o));

        DrawSection(
            new Rect(subX, y, subW - 4f, 10f),
            "UNDERLAGTE — STATUS / AI / ATTACHMENT");

        string regAi =
            PrototypeRegimentalHQ09F28.Instance != null &&
            PrototypeRegimentalHQ09F28.Instance.AIEnabled
                ? "AI ON"
                : "AI OFF";

        string majorA =
            hierarchy != null && hierarchy.Installed &&
            hierarchy.GetBattalionAIEnabled(0)
                ? "AI ON"
                : "AI OFF";
        string majorB =
            hierarchy != null && hierarchy.Installed &&
            hierarchy.GetBattalionAIEnabled(1)
                ? "AI ON"
                : "AI OFF";

        GUI.Label(
            new Rect(subX + 2f, y + 12f, subW - 7f, 14f),
            (division
                ? "1. BRIGADE " + (higher.BrigadeAIEnabled ? "AI ON" : "AI OFF") + " | "
                : string.Empty) +
            "1. REGIMENT " + regAi,
            valueStyle);

        GUI.Label(
            new Rect(subX + 2f, y + 28f, subW - 7f, 14f),
            "MAJOR A " + majorA + " | MAJOR B " + majorB,
            valueStyle);

        if (cavalry != null && cavalry.Installed)
        {
            PrototypeCavalryOfficerAI09F30C cavAi =
                PrototypeCavalryOfficerAI09F30C.Instance;

            string garde =
                cavalry.Gardehusar != null
                    ? cavalry.Gardehusar.UnitName + " " +
                      (cavAi != null && cavAi.IsAIEnabled(cavalry.Gardehusar)
                          ? "AI ON"
                          : "AI OFF") +
                      " | " + higher.GetCavalryCommandParent(cavalry.Gardehusar)
                    : "GARDEHUSAR —";

            string dragon =
                cavalry.Dragon != null
                    ? cavalry.Dragon.UnitName + " " +
                      (cavAi != null && cavAi.IsAIEnabled(cavalry.Dragon)
                          ? "AI ON"
                          : "AI OFF") +
                      " | " + higher.GetCavalryCommandParent(cavalry.Dragon)
                    : "DRAGON —";

            GUI.Label(
                new Rect(subX + 2f, y + 44f, subW - 7f, 14f),
                garde + " | " + dragon,
                valueStyle);
        }
    }

    private void DrawRegimentalHud(Rect panel, PrototypeRegimentalHQ09F28 regiment)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        List<Regiment> companies = GetAllCompanies(hierarchy);
        AggregateStats stats = CalculateStats(companies);

        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            "1. REGIMENT | OBERSTLØJTNANT | REGIMENTSKOMMANDO | F29G", headerStyle);

        float width = panel.width;
        float infoW = Mathf.Clamp(width * 0.24f, 285f, 355f);
        float subW = Mathf.Clamp(width * 0.31f, 380f, 510f);
        float commandX = infoW + 8f;
        float subX = width - subW - 6f;
        float commandW = Mathf.Max(390f, subX - commandX - 7f);
        float y = panel.y + 22f;

        DrawInfoAiBlock(new Rect(7f, y, infoW - 10f, 66f), stats,
            regiment.AIEnabled, regiment.Doctrine,
            () => regiment.ToggleAI(), d => regiment.SetDoctrine(d));

        DrawSection(new Rect(commandX, y, commandW, 10f), "REGIMENTSORDRER — KLIK = POSITION, TRÆK = FACING");
        PrototypeOfficerFacingOrder09F29G facingInput =
            PrototypeOfficerFacingOrder09F29G.Instance;

        DrawOfficerOrderGrid(commandX, y + 11f, commandW,
            o => BeginRegimentalOrder(o),
            o => regiment.IsMissionActive(o),
            o => facingInput != null && facingInput.IsPendingRegimental(o));

        DrawSection(new Rect(subX, y, subW - 4f, 10f), "BATALJONER UNDER OBERSTLØJTNANT — MÆND / TAB / MORAL / AMMO");
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount && i < 2; i++)
            {
                AggregateStats b = CalculateStats(hierarchy.GetCompanies(i));
                string row = (i + 1) + ". BATALJON | MAJOR " + (i == 0 ? "A" : "B") +
                             "  " + b.Current + "/" + b.Initial + "  T" + b.Losses +
                             "  M" + b.Morale.ToString("0") + "  A" + b.Ammo.ToString("0") +
                             "  " + (hierarchy.GetBattalionAIEnabled(i) ? "AI ON" : "AI OFF") +
                             "  " + ShortDoctrine(hierarchy.GetBattalionDoctrine(i));
                GUI.Label(new Rect(subX + 2f, y + 12f + i * 20f, subW - 7f, 16f), row, valueStyle);
            }
        }
    }

    private void DrawMajorHud(Rect panel, int battalionIndex)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionIndex < 0 || battalionIndex >= hierarchy.BattalionCount)
            return;

        IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
        AggregateStats stats = CalculateStats(companies);
        string major = battalionIndex == 0 ? "MAJOR A" : "MAJOR B";
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            (battalionIndex + 1) + ". BATALJON | " + major + " | BATALJONSKOMMANDO | F29G", headerStyle);

        float width = panel.width;
        float infoW = Mathf.Clamp(width * 0.24f, 285f, 355f);
        float subW = Mathf.Clamp(width * 0.34f, 410f, 550f);
        float commandX = infoW + 8f;
        float subX = width - subW - 6f;
        float commandW = Mathf.Max(390f, subX - commandX - 7f);
        float y = panel.y + 22f;

        DrawInfoAiBlock(new Rect(7f, y, infoW - 10f, 66f), stats,
            hierarchy.GetBattalionAIEnabled(battalionIndex),
            hierarchy.GetBattalionDoctrine(battalionIndex),
            () => hierarchy.ToggleBattalionAI(battalionIndex),
            d => hierarchy.SetBattalionDoctrine(battalionIndex, d));

        DrawSection(new Rect(commandX, y, commandW, 10f), "BATALJONSORDRER — KLIK = POSITION, TRÆK = FACING");
        PrototypeOfficerFacingOrder09F29G facingInput =
            PrototypeOfficerFacingOrder09F29G.Instance;

        DrawOfficerOrderGrid(commandX, y + 11f, commandW,
            o => BeginBattalionOrder(battalionIndex, o),
            o => hierarchy.IsBattalionOrderActive(battalionIndex, o),
            o => facingInput != null && facingInput.IsPendingBattalion(battalionIndex, o));

        DrawSection(new Rect(subX, y, subW - 4f, 10f), "KOMPAGNIER UNDER MAJOR — MÆND / TAB / MORAL / AMMO");
        if (companies != null)
        {
            for (int i = 0; i < companies.Count && i < 4; i++)
            {
                Regiment unit = companies[i];
                if (unit == null) continue;
                int loss = Mathf.Max(0, unit.InitialStrength - unit.CurrentStrength);
                int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(unit);
                GUI.Label(new Rect(subX + 2f, y + 11f + i * 14f, subW - 7f, 13f),
                    PrototypeUnitNames09F29C.Get(unit) + "  " + unit.CurrentStrength + "/" + unit.InitialStrength +
                    "  T" + loss + "  M" + unit.Morale.ToString("0") + "  A" + ammo,
                    valueStyle);
            }
        }
    }

    private void DrawCompanyHud(Rect panel, List<Regiment> selected)
    {
        if (selected == null || selected.Count == 0)
            return;

        Regiment first = selected[0];
        OfficerAIController controller = first.GetComponent<OfficerAIController>();
        AggregateStats stats = CalculateStats(selected);
        bool allAi = All(selected, r => { OfficerAIController c = r.GetComponent<OfficerAIController>(); return c != null && c.AIEnabled; });
        bool allOff = All(selected, r => { OfficerAIController c = r.GetComponent<OfficerAIController>(); return c == null || !c.AIEnabled; });
        OfficerAIDoctrine doctrine = controller != null ? controller.Doctrine : OfficerAIDoctrine.Balanced;
        bool sameDoctrine = controller != null && All(selected, r => { OfficerAIController c = r.GetComponent<OfficerAIController>(); return c != null && c.Doctrine == doctrine; });
        bool sameFire = All(selected, r => r.FirePolicy == first.FirePolicy);
        bool allForced = All(selected, r => PrototypeForcedMarch09F7.IsForcedMarch(r));
        bool anyCharge = Any(selected, r => PrototypeInfantryCharge09F25.Instance != null && PrototypeInfantryCharge09F25.Instance.IsCharging(r));
        bool allSquare = All(selected, r => PrototypeInfantrySquare09F29.IsInSquare(r));
        bool allLine = !allSquare && All(selected, r => r.Formation == RegimentFormation.Line);
        bool allColumn = !allSquare && All(selected, r => r.Formation == RegimentFormation.Column);
        PrototypeWithdrawalRange09F10? withdraw = GetCommonWithdrawalMode(selected);

        string title = selected.Count == 1
            ? PrototypeUnitNames09F29C.Get(first) + " | KAPTAJN"
            : selected.Count + " KOMPAGNIER VALGT";
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            title + " | KOMPAGNIKOMMANDO | F29G", headerStyle);

        float width = panel.width;
        float infoW = Mathf.Clamp(width * 0.20f, 220f, 300f);
        float aiW = Mathf.Clamp(width * 0.17f, 195f, 250f);
        float fireW = Mathf.Clamp(width * 0.19f, 215f, 275f);
        float infoX = 6f;
        float aiX = infoX + infoW + 5f;
        float fireX = aiX + aiW + 5f;
        float cmdX = fireX + fireW + 5f;
        float cmdW = width - cmdX - 6f;
        float y = panel.y + 22f;

        DrawSection(new Rect(infoX, y, infoW, 10f), "ENHEDSINFO");
        GUI.Label(new Rect(infoX + 2f, y + 11f, infoW - 4f, 13f),
            "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses + " | Moral " + stats.Morale.ToString("0"), valueStyle);
        GUI.Label(new Rect(infoX + 2f, y + 24f, infoW - 4f, 13f),
            "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") + " | " + FormationLabel(first), valueStyle);
        GUI.Label(new Rect(infoX + 2f, y + 37f, infoW - 4f, 13f),
            first.WeaponShortName + " | Fire " + first.GetFirePolicyLabel(), valueStyle);
        if (selected.Count == 1 && controller != null)
            GUI.Label(new Rect(infoX + 2f, y + 50f, infoW - 4f, 13f), controller.CurrentTask, valueStyle);

        DrawSection(new Rect(aiX, y, aiW, 10f), "AI / DOKTRIN");
        float gap = 3f;
        float aiBW = (aiW - gap * 3f) / 4f;
        string aiLabel = allAi ? "AI ON" : (allOff ? "AI OFF" : "AI MIX");
        if (GUI.Button(new Rect(aiX, y + 11f, aiBW, 20f), aiLabel, State(allAi))) SetSelectedAi(selected, !allAi);
        if (GUI.Button(new Rect(aiX + (aiBW + gap), y + 11f, aiBW, 20f), "DEF", State(sameDoctrine && doctrine == OfficerAIDoctrine.Defensive))) SetSelectedDoctrine(selected, OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(aiX + (aiBW + gap) * 2f, y + 11f, aiBW, 20f), "BAL", State(sameDoctrine && doctrine == OfficerAIDoctrine.Balanced))) SetSelectedDoctrine(selected, OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(aiX + (aiBW + gap) * 3f, y + 11f, aiBW, 20f), "OFF", State(sameDoctrine && doctrine == OfficerAIDoctrine.Offensive))) SetSelectedDoctrine(selected, OfficerAIDoctrine.Offensive);
        GUI.Label(new Rect(aiX, y + 36f, aiW, 13f), allForced ? "Tvangsmarch: ON" : "Tvangsmarch: OFF", valueStyle);
        GUI.Label(new Rect(aiX, y + 50f, aiW, 13f), anyCharge ? "Status: CHARGE" : (withdraw.HasValue ? "Status: TILBAGETRÆK" : "Status: klar"), valueStyle);

        DrawSection(new Rect(fireX, y, fireW, 10f), "SKYDNING");
        float fireBW = (fireW - gap * 3f) / 4f;
        DrawFireButton(new Rect(fireX, y + 11f, fireBW, 20f), "HOLD", RegimentFirePolicy.HoldFire, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + fireBW + gap, y + 11f, fireBW, 20f), "CLOSE", RegimentFirePolicy.CloseRange, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + (fireBW + gap) * 2f, y + 11f, fireBW, 20f), "MED", RegimentFirePolicy.MediumRange, sameFire, first.FirePolicy, selected);
        DrawFireButton(new Rect(fireX + (fireBW + gap) * 3f, y + 11f, fireBW, 20f), "LONG", RegimentFirePolicy.LongRange, sameFire, first.FirePolicy, selected);
        GUI.Label(new Rect(fireX, y + 36f, fireW, 13f),
            "C " + first.CloseRange.ToString("0") + "m | M " + first.EffectiveRange.ToString("0") + "m | L " + first.MaximumRange.ToString("0") + "m", valueStyle);

        DrawSection(new Rect(cmdX, y, cmdW, 10f), "ORDRER / BEVÆGELSE");
        float actionW = (cmdW - gap * 5f) / 6f;
        if (GUI.Button(new Rect(cmdX, y + 11f, actionW, 20f), "→ MED", State(withdraw == PrototypeWithdrawalRange09F10.Medium))) BeginWithdrawal(PrototypeWithdrawalRange09F10.Medium);
        if (GUI.Button(new Rect(cmdX + (actionW + gap), y + 11f, actionW, 20f), "→ LONG", State(withdraw == PrototypeWithdrawalRange09F10.Long))) BeginWithdrawal(PrototypeWithdrawalRange09F10.Long);
        if (GUI.Button(new Rect(cmdX + (actionW + gap) * 2f, y + 11f, actionW, 20f), "→ UD", State(withdraw == PrototypeWithdrawalRange09F10.OutOfRange))) BeginWithdrawal(PrototypeWithdrawalRange09F10.OutOfRange);
        if (GUI.Button(new Rect(cmdX + (actionW + gap) * 3f, y + 11f, actionW, 20f), "TVANG", State(allForced))) SetForcedMarch(selected, !allForced);
        if (GUI.Button(new Rect(cmdX + (actionW + gap) * 4f, y + 11f, actionW, 20f), "CHARGE", State(anyCharge))) BeginChargePick();
        if (GUI.Button(new Rect(cmdX + (actionW + gap) * 5f, y + 11f, actionW, 20f), "STOP", redButtonStyle)) StopSelected(selected);

        DrawSection(new Rect(cmdX, y + 36f, cmdW, 10f), "FORMATION");
        float formW = (cmdW - gap * 2f) / 3f;
        if (GUI.Button(new Rect(cmdX, y + 47f, formW, 20f), "LINJE", State(allLine))) SetFormation(selected, RegimentFormation.Line, false);
        if (GUI.Button(new Rect(cmdX + formW + gap, y + 47f, formW, 20f), "KOLONNE", State(allColumn))) SetFormation(selected, RegimentFormation.Column, false);
        if (GUI.Button(new Rect(cmdX + (formW + gap) * 2f, y + 47f, formW, 20f), "SQUARE", State(allSquare))) SetFormation(selected, RegimentFormation.Line, true);
    }

    private void DrawInfoAiBlock(Rect r, AggregateStats stats, bool ai, OfficerAIDoctrine doctrine, Action toggleAi, Action<OfficerAIDoctrine> setDoctrine)
    {
        DrawSection(new Rect(r.x, r.y, r.width, 10f), "ENHEDSINFO / AI");
        GUI.Label(new Rect(r.x + 2f, r.y + 10f, r.width - 4f, 13f),
            "Mænd " + stats.Current + "/" + stats.Initial + " | Tab " + stats.Losses + " | Moral " + stats.Morale.ToString("0"), valueStyle);
        GUI.Label(new Rect(r.x + 2f, r.y + 23f, r.width - 4f, 13f),
            "Coh " + stats.Cohesion.ToString("0") + " | Ammo " + stats.Ammo.ToString("0") + " r/m", valueStyle);

        float gap = 3f;
        float bw = (r.width - gap * 3f) / 4f;
        float by = r.y + 40f;
        if (GUI.Button(new Rect(r.x, by, bw, 20f), ai ? "AI ON" : "AI OFF", State(ai))) toggleAi();
        if (GUI.Button(new Rect(r.x + bw + gap, by, bw, 20f), "DEF", State(doctrine == OfficerAIDoctrine.Defensive))) setDoctrine(OfficerAIDoctrine.Defensive);
        if (GUI.Button(new Rect(r.x + (bw + gap) * 2f, by, bw, 20f), "BAL", State(doctrine == OfficerAIDoctrine.Balanced))) setDoctrine(OfficerAIDoctrine.Balanced);
        if (GUI.Button(new Rect(r.x + (bw + gap) * 3f, by, bw, 20f), "OFF", State(doctrine == OfficerAIDoctrine.Offensive))) setDoctrine(OfficerAIDoctrine.Offensive);
    }

    private void DrawOfficerOrderGrid(
        float x,
        float y,
        float width,
        Action<MajorOrder09F18> command,
        Func<MajorOrder09F18, bool> isActive,
        Func<MajorOrder09F18, bool> isPending)
    {
        const float gap = 4f;
        float w = (width - gap * 2f) / 3f;

        DrawOfficerOrderButton(new Rect(x, y, w, 22f),
            "ANGRIB HER", MajorOrder09F18.AttackHere, command, isActive, isPending);
        DrawOfficerOrderButton(new Rect(x + w + gap, y, w, 22f),
            "FORSVAR HER", MajorOrder09F18.DefendHere, command, isActive, isPending);
        DrawOfficerOrderButton(new Rect(x + (w + gap) * 2f, y, w, 22f),
            "RYK FREM", MajorOrder09F18.AdvanceHere, command, isActive, isPending);
        DrawOfficerOrderButton(new Rect(x, y + 27f, w, 22f),
            "TILBAGETRÆK", MajorOrder09F18.WithdrawHere, command, isActive, isPending);
        DrawOfficerOrderButton(new Rect(x + w + gap, y + 27f, w, 22f),
            "SAML", MajorOrder09F18.AssembleHere, command, isActive, isPending);
        DrawOfficerOrderButton(new Rect(x + (w + gap) * 2f, y + 27f, w, 22f),
            "STOP / HOLD", MajorOrder09F18.HoldPosition, command, isActive, isPending);
    }

    private void DrawOfficerOrderButton(
        Rect rect,
        string label,
        MajorOrder09F18 order,
        Action<MajorOrder09F18> command,
        Func<MajorOrder09F18, bool> isActive,
        Func<MajorOrder09F18, bool> isPending)
    {
        bool active = isActive != null && isActive(order);
        bool pending = isPending != null && isPending(order);
        GUIStyle style = active || pending ? blueButtonStyle : redButtonStyle;

        if (GUI.Button(rect, label, style))
            command(order);
    }

    private static void BeginHigherOrder(
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order)
    {
        if (PrototypeOfficerFacingOrder09F29G.Instance != null)
            PrototypeOfficerFacingOrder09F29G.Instance.BeginHigherOrder(level, order);
    }

    private static void BeginRegimentalOrder(MajorOrder09F18 order)
    {
        if (PrototypeOfficerFacingOrder09F29G.Instance != null)
            PrototypeOfficerFacingOrder09F29G.Instance.BeginRegimentalOrder(order);
    }

    private static void BeginBattalionOrder(int battalion, MajorOrder09F18 order)
    {
        if (PrototypeOfficerFacingOrder09F29G.Instance != null)
            PrototypeOfficerFacingOrder09F29G.Instance.BeginBattalionOrder(battalion, order);
    }

    private void DrawFireButton(Rect rect, string label, RegimentFirePolicy policy, bool same, RegimentFirePolicy current, List<Regiment> selected)
    {
        if (!GUI.Button(rect, label, State(same && current == policy)))
            return;
        for (int i = 0; i < selected.Count; i++)
            PrototypeAttackContact09F29G.SetPreferredFirePolicy(selected[i], policy);
    }

    private static void SetSelectedAi(List<Regiment> selected, bool value)
    {
        foreach (Regiment unit in selected)
        {
            OfficerAIController c = unit != null ? unit.GetComponent<OfficerAIController>() : null;
            if (c != null && c.AIEnabled != value) c.SetAIEnabled(value);
        }
    }

    private static void SetSelectedDoctrine(List<Regiment> selected, OfficerAIDoctrine doctrine)
    {
        foreach (Regiment unit in selected)
        {
            OfficerAIController c = unit != null ? unit.GetComponent<OfficerAIController>() : null;
            if (c != null) c.SetDoctrine(doctrine);
        }
    }

    private static void BeginWithdrawal(PrototypeWithdrawalRange09F10 mode)
    {
        if (PrototypeFightingWithdrawal09F10.Instance != null)
            PrototypeFightingWithdrawal09F10.Instance.BeginForSelected(mode);
    }

    private void SetForcedMarch(List<Regiment> selected, bool enable)
    {
        PrototypeForcedMarch09F7 manager = PrototypeForcedMarch09F7.Instance;
        if (manager == null) return;
        foreach (Regiment unit in selected)
            if (unit != null && PrototypeForcedMarch09F7.IsForcedMarch(unit) != enable)
                manager.Toggle(unit);
    }

    private void BeginChargePick()
    {
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && beginChargePickMethod != null)
            beginChargePickMethod.Invoke(charge, null);
    }

    private void StopSelected(List<Regiment> selected)
    {
        foreach (Regiment unit in selected)
        {
            if (unit == null || unit.IsRouted) continue;
            OfficerAIController c = unit.GetComponent<OfficerAIController>();
            if (c != null && c.AIEnabled) c.SetAIEnabled(false);
            ClearRoute(unit);
            CancelWithdrawal(unit, true, "HUD29G_STOP");
            FinishCharge(unit, "HUD29G_STOP");
            if (PrototypeForcedMarch09F7.Instance != null && PrototypeForcedMarch09F7.IsForcedMarch(unit))
                PrototypeForcedMarch09F7.Instance.Toggle(unit);
            unit.OrderHold();
        }
    }

    private void SetFormation(List<Regiment> selected, RegimentFormation formation, bool square)
    {
        PrototypeInfantrySquare09F29 squareManager = PrototypeInfantrySquare09F29.Instance;
        foreach (Regiment unit in selected)
        {
            if (unit == null || unit.IsRouted) continue;
            OfficerAIController c = unit.GetComponent<OfficerAIController>();
            if (c != null && c.AIEnabled) c.SetAIEnabled(false);

            if (square)
            {
                if (!PrototypeInfantrySquare09F29.IsInSquare(unit) && squareManager != null && enterSquareMethod != null)
                {
                    Vector3 hint = unit.transform.position + unit.transform.forward * 80f;
                    enterSquareMethod.Invoke(squareManager, new object[] { unit, false, hint });
                }
                continue;
            }

            if (PrototypeInfantrySquare09F29.IsInSquare(unit) && squareManager != null && leaveSquareMethod != null)
                leaveSquareMethod.Invoke(squareManager, new object[] { unit, "HUD29G_FORMATION", true });
            unit.SetFormation(formation);
        }
    }

    private void FinishCharge(Regiment unit, string reason)
    {
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && finishChargeMethod != null && charge.IsCharging(unit))
            finishChargeMethod.Invoke(charge, new object[] { unit, reason });
    }

    private void CancelWithdrawal(Regiment unit, bool restore, string reason)
    {
        PrototypeFightingWithdrawal09F10 w = PrototypeFightingWithdrawal09F10.Instance;
        if (w != null && cancelWithdrawalMethod != null && w.IsWithdrawing(unit))
            cancelWithdrawalMethod.Invoke(w, new object[] { unit, restore, reason });
    }

    private void ClearRoute(Regiment unit)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && clearPlayerRouteMethod != null && unit != null)
            clearPlayerRouteMethod.Invoke(commander, new object[] { unit });
    }

    private PrototypeWithdrawalRange09F10? GetCommonWithdrawalMode(List<Regiment> selected)
    {
        PrototypeFightingWithdrawal09F10 w = PrototypeFightingWithdrawal09F10.Instance;
        if (w == null || withdrawalActiveField == null || selected.Count == 0) return null;
        IDictionary active = withdrawalActiveField.GetValue(w) as IDictionary;
        if (active == null) return null;

        PrototypeWithdrawalRange09F10? common = null;
        foreach (Regiment unit in selected)
        {
            if (unit == null || !active.Contains(unit)) return null;
            object state = active[unit];
            FieldInfo f = state != null ? state.GetType().GetField("Mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null;
            if (f == null || !(f.GetValue(state) is PrototypeWithdrawalRange09F10)) return null;
            PrototypeWithdrawalRange09F10 mode = (PrototypeWithdrawalRange09F10)f.GetValue(state);
            if (!common.HasValue) common = mode;
            else if (common.Value != mode) return null;
        }
        return common;
    }

    private int GetSelectedBattalionIndex()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || selectedBattalionField == null) return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static List<Regiment> GetSelectedCompanies()
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return result;
        foreach (Regiment unit in battle.Regiments)
            if (unit != null && unit.Team == BattleTeam.Denmark && unit.IsSelected && !unit.IsRouted)
                result.Add(unit);
        return result;
    }

    private static List<Regiment> GetAllCompanies(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        List<Regiment> result = new List<Regiment>();
        if (hierarchy == null || !hierarchy.Installed) return result;
        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            IReadOnlyList<Regiment> list = hierarchy.GetCompanies(b);
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++) if (list[i] != null) result.Add(list[i]);
        }
        return result;
    }

    private void ApplyVisibleCompanyNames()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed) return;
        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            IReadOnlyList<Regiment> list = hierarchy.GetCompanies(b);
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                Regiment unit = list[i];
                if (unit == null) continue;
                string visible = PrototypeUnitNames09F29C.Get(unit);
                if (!string.IsNullOrEmpty(visible)) unit.gameObject.name = visible;
            }
        }
    }

    private struct AggregateStats
    {
        public int Initial, Current, Losses;
        public float Morale, Cohesion, Ammo;
    }

    private static AggregateStats CalculateStats(IReadOnlyList<Regiment> units)
    {
        AggregateStats r = new AggregateStats();
        if (units == null) return r;
        int count = 0;
        foreach (Regiment unit in units)
        {
            if (unit == null) continue;
            r.Initial += unit.InitialStrength;
            r.Current += unit.CurrentStrength;
            r.Morale += unit.Morale;
            r.Cohesion += unit.Cohesion;
            r.Ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(unit);
            count++;
        }
        r.Losses = Mathf.Max(0, r.Initial - r.Current);
        if (count > 0)
        {
            r.Morale /= count;
            r.Cohesion /= count;
            r.Ammo /= count;
        }
        return r;
    }

    private static AggregateStats CalculateStats(List<Regiment> units)
    {
        return CalculateStats((IReadOnlyList<Regiment>)units);
    }

    private static bool All(List<Regiment> units, Func<Regiment, bool> p)
    {
        if (units == null || units.Count == 0) return false;
        foreach (Regiment u in units) if (u == null || !p(u)) return false;
        return true;
    }

    private static bool Any(List<Regiment> units, Func<Regiment, bool> p)
    {
        if (units == null) return false;
        foreach (Regiment u in units) if (u != null && p(u)) return true;
        return false;
    }

    private static string FormationLabel(Regiment unit)
    {
        if (unit == null) return "?";
        if (PrototypeInfantrySquare09F29.IsInSquare(unit)) return PrototypeInfantrySquare09F29.IsSquareReady(unit) ? "SQUARE" : "SQUARE...";
        return unit.Formation == RegimentFormation.Column ? "KOLONNE" : "LINJE";
    }

    private static string ShortDoctrine(OfficerAIDoctrine d)
    {
        return d == OfficerAIDoctrine.Defensive ? "DEF" : d == OfficerAIDoctrine.Offensive ? "OFF" : "BAL";
    }

    private GUIStyle State(bool active) => active ? greenButtonStyle : redButtonStyle;
    private void DrawSection(Rect r, string text) => GUI.Label(r, text, sectionStyle);

    private void BuildStyles()
    {
        if (panelStyle != null) return;
        panelTexture = MakeTexture(new Color(0.055f, 0.065f, 0.055f, 0.995f), "HUD29G_PANEL");
        headerTexture = MakeTexture(new Color(0.13f, 0.16f, 0.11f, 1f), "HUD29G_HEADER");
        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "HUD29G_GREEN");
        greenHoverTexture = MakeTexture(new Color(0.22f, 0.56f, 0.25f, 1f), "HUD29G_GREEN_HOVER");
        redTexture = MakeTexture(new Color(0.43f, 0.14f, 0.12f, 1f), "HUD29G_RED");
        redHoverTexture = MakeTexture(new Color(0.57f, 0.19f, 0.16f, 1f), "HUD29G_RED_HOVER");
        blueTexture = MakeTexture(new Color(0.10f, 0.29f, 0.54f, 1f), "HUD30Q_ORDER_BLUE");
        blueHoverTexture = MakeTexture(new Color(0.14f, 0.39f, 0.70f, 1f), "HUD30Q_ORDER_BLUE_HOVER");
        topBorderTexture = MakeTexture(Color.black, "HUD30O_TOP_BLACK");

        panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture } };
        headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = headerTexture;
        headerStyle.normal.textColor = new Color(0.96f, 0.94f, 0.84f);
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.padding = new RectOffset(7, 5, 1, 1);
        headerStyle.border = new RectOffset(0, 0, 0, 0);

        sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.normal.textColor = new Color(0.77f, 0.67f, 0.35f);
        sectionStyle.fontSize = 8;
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.alignment = TextAnchor.MiddleLeft;

        valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.normal.textColor = new Color(0.94f, 0.93f, 0.85f);
        valueStyle.fontSize = 8;
        valueStyle.alignment = TextAnchor.MiddleLeft;
        valueStyle.clipping = TextClipping.Clip;

        greenButtonStyle = MakeButtonStyle(greenTexture, greenHoverTexture);
        redButtonStyle = MakeButtonStyle(redTexture, redHoverTexture);
        blueButtonStyle = MakeButtonStyle(blueTexture, blueHoverTexture);
    }

    private static GUIStyle MakeButtonStyle(Texture2D normal, Texture2D hover)
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.normal.background = normal;
        s.hover.background = hover;
        s.active.background = hover;
        s.focused.background = hover;
        s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
        s.fontSize = 8;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;
        s.padding = new RectOffset(2, 2, 1, 1);
        return s;
    }

    private static Texture2D MakeTexture(Color c, string name)
    {
        Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.name = name;
        t.hideFlags = HideFlags.HideAndDontSave;
        t.SetPixel(0, 0, c);
        t.Apply(false, true);
        return t;
    }
}
