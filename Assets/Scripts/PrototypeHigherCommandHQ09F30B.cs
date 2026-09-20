using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum PrototypeHigherCommandLevel09F30B
{
    None,
    Brigade,
    Division
}

public sealed class PrototypeHigherCommandMarker09F30B : MonoBehaviour
{
    public PrototypeHigherCommandLevel09F30B Level;
}

// v00.00.09f30b
// Physical Brigade/Division HQ and the first higher-command mission/attachment layer.
// Higher HQ owns command intent only. Existing Regiment -> Battalion -> Company systems
// remain the physical mission decomposition/movement authority.
[DefaultExecutionOrder(820)]
public sealed class PrototypeHigherCommandHQ09F30B : MonoBehaviour
{
    public static PrototypeHigherCommandHQ09F30B Instance { get; private set; }

    public bool Installed { get; private set; }
    public GameObject DivisionHqRoot { get; private set; }
    public GameObject BrigadeHqRoot { get; private set; }
    public PrototypeHigherCommandLevel09F30B SelectedLevel { get; private set; }
    public bool BrigadeAIEnabled { get; private set; }
    public bool DivisionAIEnabled { get; private set; }
    public OfficerAIDoctrine BrigadeDoctrine { get; private set; } = OfficerAIDoctrine.Balanced;
    public OfficerAIDoctrine DivisionDoctrine { get; private set; } = OfficerAIDoctrine.Balanced;

    public const string DivisionId = "1. DIVISION";
    public const string BrigadeId = "1. BRIGADE";
    public const string RegimentId = "1. REGIMENT";

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float HudHeight = 96f;

    // F30P: higher HQ positions are relative to the committed battle-facing.
    // World-axis offsets are intentionally avoided.
    private const float BrigadeFollowDistance = 120f;
    private const float BrigadeLateralOffset = 65f;
    private const float DivisionFollowDistance = 145f;
    private const float DivisionLateralOffset = -75f;
    private const float BrigadeMoveSpeed = 6.2f;
    private const float DivisionMoveSpeed = 5.9f;

    // QA command-reach bands. These are visual/command-delay bands, not weapon range.
    private const float BrigadeCommandInner = 1350f;
    private const float BrigadeCommandOuter = 1850f;
    private const float DivisionCommandInner = 2100f;
    private const float DivisionCommandOuter = 2850f;

    // F30S defensive cavalry reserve geometry.
    private const float DefendCavalryRearDepth = 150f;
    private const float DefendCavalryOutwardOffset = 45f;

    private const int LinkSamples = 28;

    private PrototypeRegimentalHQ09F28 regimental;
    private PrototypeRegimentHierarchy09F27 hierarchy;
    private PrototypeCavalryManager09F30 cavalry;
    private Camera cam;

    private MethodInfo setRegimentalSelectedMethod;
    private FieldInfo playerSelectedField;

    private MajorOrder09F18 pendingOrder = MajorOrder09F18.None;
    private PrototypeHigherCommandLevel09F30B pendingLevel = PrototypeHigherCommandLevel09F30B.None;
    private MajorOrder09F18 brigadeMission = MajorOrder09F18.None;
    private MajorOrder09F18 divisionMission = MajorOrder09F18.None;
    private Vector3 brigadeObjective;
    private Vector3 divisionObjective;

    private bool temporaryAttackAttachmentsActive;
    private bool gardeTemporarilyAttached;
    private bool dragonTemporarilyAttached;
    private string gardeReturnParent = string.Empty;
    private string dragonReturnParent = string.Empty;

    private LineRenderer divisionBrigadeLink;
    private LineRenderer brigadeRegimentLink;
    private LineRenderer cavalryLinkA;
    private LineRenderer cavalryLinkB;
    private LineRenderer divisionSelection;
    private LineRenderer brigadeSelection;
    private LineRenderer divisionCommandInner;
    private LineRenderer divisionCommandOuter;
    private LineRenderer brigadeCommandInner;
    private LineRenderer brigadeCommandOuter;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;
    private GUIStyle activeBlueStyle;
    private GUIStyle activeGreenStyle;
    private GUIStyle dangerStyle;
    private Texture2D activeBlueTexture;
    private Texture2D activeGreenTexture;
    private Texture2D dangerTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandHQ09F30B>() == null)
            new GameObject("PrototypeHigherCommandHQ_v000009f30b")
                .AddComponent<PrototypeHigherCommandHQ09F30B>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28)
            .GetMethod("SetSelected", PrivateInstance);
        playerSelectedField = typeof(PlayerCommander)
            .GetField("selected", PrivateInstance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (!Installed)
        {
            TryInstall();
            return;
        }

        UpdateHqFollow();
        UpdateCommandLinks();
        UpdateTemporaryAttackAttachments();
        HandlePendingOrderInput();
        HandleWorldSelection();
    }

    private void TryInstall()
    {
        regimental = PrototypeRegimentalHQ09F28.Instance;
        hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        cavalry = PrototypeCavalryManager09F30.Instance;

        if (regimental == null || !regimental.Installed || regimental.HqRoot == null ||
            hierarchy == null || !hierarchy.Installed || cavalry == null || !cavalry.Installed)
            return;

        Vector3 regimentPos = regimental.HqRoot.transform.position;
        Vector3 rear = -Flat(regimental.HqRoot.transform.forward);
        if (rear.sqrMagnitude < 0.01f)
            rear = Vector3.left;
        rear.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, -rear).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.forward;

        BrigadeHqRoot = CreateHigherHq(
            "DK_Brigade_HQ_Brigadechef", "X HQ\n1. BRIGADE", PrototypeHigherCommandLevel09F30B.Brigade,
            Ground(regimentPos + rear * BrigadeFollowDistance + right * BrigadeLateralOffset),
            new Color(0.07f, 0.17f, 0.31f));

        DivisionHqRoot = CreateHigherHq(
            "DK_Division_HQ_Divisionschef", "XX HQ\n1. DIVISION", PrototypeHigherCommandLevel09F30B.Division,
            Ground(BrigadeHqRoot.transform.position + rear * DivisionFollowDistance + right * DivisionLateralOffset),
            new Color(0.09f, 0.20f, 0.36f));

        ConfigureCommandParents();
        CreateLinks();
        Installed = BrigadeHqRoot != null && DivisionHqRoot != null;

        Debug.Log(
            "HQ-09F30P|Installed=" + Installed +
            "|Chain=DIVISION>BRIGADE>REGIMENT>BATTALION>COMPANY|" +
            "CavalryParent=BRIGADE|HigherCommandZones=True|" +
            "BrigadeReach=" + BrigadeCommandInner + "/" + BrigadeCommandOuter +
            "|DivisionReach=" + DivisionCommandInner + "/" + DivisionCommandOuter +
            "|RelativeFollow=True|HigherAI=False");
    }

    private void ConfigureCommandParents()
    {
        PrototypeCommandAttachment09F30B regAttach = regimental.HqRoot.GetComponent<PrototypeCommandAttachment09F30B>();
        if (regAttach == null)
            regAttach = regimental.HqRoot.AddComponent<PrototypeCommandAttachment09F30B>();
        regAttach.Configure(BrigadeId, BrigadeId, PrototypeAttachmentType09F30B.Organic);

        ConfigureCavalry(cavalry.Gardehusar);
        ConfigureCavalry(cavalry.Dragon);
    }

    private static void ConfigureCavalry(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return;
        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment == null)
            attachment = unit.gameObject.AddComponent<PrototypeCommandAttachment09F30B>();
        attachment.Configure(DivisionId + " / KAVALERI", BrigadeId, PrototypeAttachmentType09F30B.Attached);
    }

    public void SelectLevel(PrototypeHigherCommandLevel09F30B level, bool focusBehind = false)
    {
        if (!Installed || level == PrototypeHigherCommandLevel09F30B.None)
            return;

        ClearLowerSelection();
        SelectedLevel = level;
        pendingOrder = MajorOrder09F18.None;
        pendingLevel = PrototypeHigherCommandLevel09F30B.None;

        if (focusBehind)
        {
            Transform target = level == PrototypeHigherCommandLevel09F30B.Division
                ? DivisionHqRoot.transform
                : BrigadeHqRoot.transform;
            FocusBehindExternal(target);
        }

        Debug.Log("HQ-09F30B|Select=" + level + "|CameraMoved=" + focusBehind);
    }

    public bool GetAIEnabled(PrototypeHigherCommandLevel09F30B level)
    {
        return level == PrototypeHigherCommandLevel09F30B.Division
            ? DivisionAIEnabled
            : level == PrototypeHigherCommandLevel09F30B.Brigade && BrigadeAIEnabled;
    }

    public OfficerAIDoctrine GetDoctrine(PrototypeHigherCommandLevel09F30B level)
    {
        return level == PrototypeHigherCommandLevel09F30B.Division
            ? DivisionDoctrine
            : BrigadeDoctrine;
    }

    public void ToggleAI(PrototypeHigherCommandLevel09F30B level)
    {
        bool enabled;
        if (level == PrototypeHigherCommandLevel09F30B.Division)
        {
            enabled = !DivisionAIEnabled;
            DivisionAIEnabled = enabled;

            // Division delegation owns the single Brigade in this prototype.
            BrigadeAIEnabled = enabled;
            CascadeSubordinateAI(enabled, "DIVISION_CASCADE");
        }
        else if (level == PrototypeHigherCommandLevel09F30B.Brigade)
        {
            enabled = !BrigadeAIEnabled;
            BrigadeAIEnabled = enabled;
            CascadeSubordinateAI(enabled, "BRIGADE_CASCADE");
        }
        else
        {
            return;
        }

        Debug.Log("HQ-AI-09F30M|Level=" + level +
                  "|AI=" + (GetAIEnabled(level) ? "ON" : "OFF") +
                  "|BrigadeAI=" + (BrigadeAIEnabled ? "ON" : "OFF") +
                  "|Doctrine=" + GetDoctrine(level) +
                  "|Cascade=True");
    }

    private void CascadeSubordinateAI(bool enabled, string reason)
    {
        if (regimental != null)
            regimental.SetAIEnabled(enabled, reason);

        if (hierarchy != null)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
                hierarchy.SetBattalionAIEnabled(i, enabled, reason);
        }

        PrototypeCavalryOfficerAI09F30C cavAi = PrototypeCavalryOfficerAI09F30C.Instance;
        if (cavAi != null && cavalry != null)
        {
            if (cavalry.Gardehusar != null)
                cavAi.SetAIEnabled(cavalry.Gardehusar, enabled, reason);
            if (cavalry.Dragon != null)
                cavAi.SetAIEnabled(cavalry.Dragon, enabled, reason);
        }
    }

    public void SetDoctrine(PrototypeHigherCommandLevel09F30B level, OfficerAIDoctrine doctrine)
    {
        if (level == PrototypeHigherCommandLevel09F30B.Division)
            DivisionDoctrine = doctrine;
        else if (level == PrototypeHigherCommandLevel09F30B.Brigade)
            BrigadeDoctrine = doctrine;

        Debug.Log("HQ-AI-09F30K|Level=" + level + "|Doctrine=" + doctrine);
    }

    public void ClearSelectionOnly()
    {
        SelectedLevel = PrototypeHigherCommandLevel09F30B.None;
        pendingOrder = MajorOrder09F18.None;
        pendingLevel = PrototypeHigherCommandLevel09F30B.None;
    }

    public void FocusBehindExternal(Transform target)
    {
        if (target == null)
            return;
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        Vector3 forward = Flat(target.forward);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 p = target.position - forward * 58f;
        p.y = PrototypeBootstrap.SampleGroundHeight(target.position.x, target.position.z) + 39f;
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 10f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 10f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, 10f, 600f);
        cam.transform.position = p;
        cam.transform.rotation = Quaternion.Euler(34f, target.eulerAngles.y, 0f);
    }

    public string GetCavalryCommandParent(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return "—";
        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        return attachment != null ? attachment.CurrentCommandParent : BrigadeId;
    }

    public void ToggleCavalryAttachment(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return;

        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment == null)
        {
            ConfigureCavalry(unit);
            attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        }
        if (attachment == null)
            return;

        bool toRegiment = attachment.CurrentCommandParent != RegimentId;
        attachment.SetCurrentCommandParent(
            toRegiment ? RegimentId : BrigadeId,
            PrototypeAttachmentType09F30B.Attached);

        Debug.Log("HQ-09F30B|Attachment=" + unit.UnitName + "|CurrentCommandParent=" + attachment.CurrentCommandParent);
    }

    private void ClearLowerSelection()
    {
        if (regimental != null && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        if (hierarchy != null)
            hierarchy.ClearMajorSelection();

        if (cavalry != null)
            cavalry.ClearSelection();

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
            {
                for (int i = 0; i < selected.Count; i++)
                    if (selected[i] != null)
                        selected[i].SetSelected(false);
                selected.Clear();
            }
        }
    }

    private void HandlePendingOrderInput()
    {
        if (pendingOrder == MajorOrder09F18.None || pendingLevel == PrototypeHigherCommandLevel09F30B.None || cam == null)
            return;
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUi(Input.mousePosition))
            return;

        if (!TryGetGround(Input.mousePosition, out Vector3 point))
            return;

        CommitHigherOrder(pendingLevel, pendingOrder, point);
        pendingOrder = MajorOrder09F18.None;
        pendingLevel = PrototypeHigherCommandLevel09F30B.None;
        Input.ResetInputAxes();
    }

    public void IssueHigherOrder(
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order,
        Vector3 point,
        Vector3? explicitFacing = null)
    {
        if (!Installed || level == PrototypeHigherCommandLevel09F30B.None)
            return;

        pendingOrder = MajorOrder09F18.None;
        pendingLevel = PrototypeHigherCommandLevel09F30B.None;
        CommitHigherOrder(level, order, point, explicitFacing);
    }

    private void CommitHigherOrder(
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order,
        Vector3 point,
        Vector3? explicitFacing = null)
    {
        if (regimental == null || !regimental.Installed)
            return;

        if (temporaryAttackAttachmentsActive && order != MajorOrder09F18.AttackHere)
            ReleaseTemporaryAttackAttachments("MISSION_CHANGED");

        if (level == PrototypeHigherCommandLevel09F30B.Division)
        {
            divisionMission = order;
            divisionObjective = point;
            // Single-brigade F30B prototype: Division intent is delegated to Brigade,
            // which delegates to the existing Regimental HQ execution pipeline.
            brigadeMission = order;
            brigadeObjective = point;
        }
        else
        {
            brigadeMission = order;
            brigadeObjective = point;
        }

        OfficerAIDoctrine doctrine = GetDoctrine(level);
        regimental.SetDoctrine(doctrine);
        for (int i = 0; hierarchy != null && i < hierarchy.BattalionCount; i++)
            hierarchy.SetBattalionDoctrine(i, doctrine);

        bool autonomous = GetAIEnabled(level);
        regimental.IssueRegimentalOrder(order, point, autonomous, explicitFacing);

        if (order == MajorOrder09F18.AttackHere)
            BeginTemporaryAttackAttachments(level);

        IssueAttachedCavalryMission(level, order, point, autonomous, explicitFacing);

        Vector3 committedFacing = explicitFacing.HasValue
            ? Flat(explicitFacing.Value)
            : regimental.CurrentMissionFacing;

        Debug.Log("HQ-09F30S|MissionCommitted=True|Level=" + level + "|Order=" + order +
                  "|DelegatedTo=REGIMENT+CAVALRY|Objective=" +
                  point.x.ToString("0.0") + "," + point.z.ToString("0.0") +
                  "|ExplicitFacing=" + explicitFacing.HasValue +
                  "|Facing=" + committedFacing.x.ToString("0.00") + "," +
                  committedFacing.z.ToString("0.00"));
    }

    private void BeginTemporaryAttackAttachments(PrototypeHigherCommandLevel09F30B level)
    {
        if (cavalry == null || hierarchy == null || hierarchy.BattalionCount < 2)
            return;

        PrototypeCavalryUnit09F30 garde = cavalry.Gardehusar;
        PrototypeCavalryUnit09F30 dragon = cavalry.Dragon;
        if (garde == null && dragon == null)
            return;

        if (!temporaryAttackAttachmentsActive)
        {
            gardeReturnParent = garde != null ? GetCavalryCommandParent(garde) : string.Empty;
            dragonReturnParent = dragon != null ? GetCavalryCommandParent(dragon) : string.Empty;
        }

        gardeTemporarilyAttached = false;
        dragonTemporarilyAttached = false;

        Vector3 a = hierarchy.GetBattalionCenter(0);
        Vector3 b = hierarchy.GetBattalionCenter(1);

        if (garde != null && dragon != null &&
            IsCavalrySubordinateToLevel(garde, level) &&
            IsCavalrySubordinateToLevel(dragon, level))
        {
            float normal = PlanarDistance(garde.transform.position, a) +
                           PlanarDistance(dragon.transform.position, b);
            float swapped = PlanarDistance(garde.transform.position, b) +
                            PlanarDistance(dragon.transform.position, a);

            if (normal <= swapped)
            {
                gardeTemporarilyAttached = SetTemporaryCavalryParent(
                    garde, PrototypeCavalryCommandControl09F30C.MajorAId);
                dragonTemporarilyAttached = SetTemporaryCavalryParent(
                    dragon, PrototypeCavalryCommandControl09F30C.MajorBId);
            }
            else
            {
                gardeTemporarilyAttached = SetTemporaryCavalryParent(
                    garde, PrototypeCavalryCommandControl09F30C.MajorBId);
                dragonTemporarilyAttached = SetTemporaryCavalryParent(
                    dragon, PrototypeCavalryCommandControl09F30C.MajorAId);
            }
        }
        else
        {
            if (garde != null && IsCavalrySubordinateToLevel(garde, level))
                gardeTemporarilyAttached = SetTemporaryCavalryParent(
                    garde,
                    PlanarDistance(garde.transform.position, a) <= PlanarDistance(garde.transform.position, b)
                        ? PrototypeCavalryCommandControl09F30C.MajorAId
                        : PrototypeCavalryCommandControl09F30C.MajorBId);

            if (dragon != null && IsCavalrySubordinateToLevel(dragon, level))
                dragonTemporarilyAttached = SetTemporaryCavalryParent(
                    dragon,
                    PlanarDistance(dragon.transform.position, a) <= PlanarDistance(dragon.transform.position, b)
                        ? PrototypeCavalryCommandControl09F30C.MajorAId
                        : PrototypeCavalryCommandControl09F30C.MajorBId);
        }

        temporaryAttackAttachmentsActive =
            gardeTemporarilyAttached || dragonTemporarilyAttached;

        if (!temporaryAttackAttachmentsActive)
        {
            gardeReturnParent = string.Empty;
            dragonReturnParent = string.Empty;
        }

        Debug.Log("HQ-CAV-ATTACH-09F30M|AttackTask=" +
                  (temporaryAttackAttachmentsActive ? "True" : "False") +
                  "|GardeAttached=" + gardeTemporarilyAttached +
                  "|DragonAttached=" + dragonTemporarilyAttached +
                  "|GardeParent=" + (garde != null ? GetCavalryCommandParent(garde) : "—") +
                  "|DragonParent=" + (dragon != null ? GetCavalryCommandParent(dragon) : "—") +
                  "|ReturnGarde=" + gardeReturnParent + "|ReturnDragon=" + dragonReturnParent);
    }

    private static bool SetTemporaryCavalryParent(PrototypeCavalryUnit09F30 unit, string parent)
    {
        if (unit == null)
            return false;

        PrototypeCommandAttachment09F30B attachment =
            unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment == null)
            return false;

        attachment.SetCurrentCommandParent(parent, PrototypeAttachmentType09F30B.Attached);
        return true;
    }

    private void UpdateTemporaryAttackAttachments()
    {
        if (!temporaryAttackAttachmentsActive || regimental == null)
            return;

        if (regimental.HasActiveMissionExecutors(MajorOrder09F18.AttackHere))
            return;

        if ((gardeTemporarilyAttached &&
             IsCavalryInCommittedCharge(cavalry != null ? cavalry.Gardehusar : null)) ||
            (dragonTemporarilyAttached &&
             IsCavalryInCommittedCharge(cavalry != null ? cavalry.Dragon : null)))
            return;

        ReleaseTemporaryAttackAttachments("ATTACK_COMPLETE");
    }

    private static bool IsCavalryInCommittedCharge(PrototypeCavalryUnit09F30 unit)
    {
        return unit != null && unit.Action == PrototypeCavalryAction09F30.Charge;
    }

    private void ReleaseTemporaryAttackAttachments(string reason)
    {
        if (!temporaryAttackAttachmentsActive || cavalry == null)
            return;

        if (gardeTemporarilyAttached)
            ReleaseCavalryToReserve(cavalry.Gardehusar, gardeReturnParent, -1);
        if (dragonTemporarilyAttached)
            ReleaseCavalryToReserve(cavalry.Dragon, dragonReturnParent, 1);

        temporaryAttackAttachmentsActive = false;
        gardeTemporarilyAttached = false;
        dragonTemporarilyAttached = false;
        gardeReturnParent = string.Empty;
        dragonReturnParent = string.Empty;

        Debug.Log("HQ-CAV-ATTACH-09F30M|AttackTask=False|Reason=" + reason +
                  "|Result=RETURN_TO_PARENT_RESERVE");
    }

    private void ReleaseCavalryToReserve(
        PrototypeCavalryUnit09F30 unit,
        string returnParent,
        int side)
    {
        if (unit == null)
            return;

        if (string.IsNullOrEmpty(returnParent))
            returnParent = BrigadeId;

        PrototypeCommandAttachment09F30B attachment =
            unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment != null)
            attachment.SetCurrentCommandParent(returnParent, PrototypeAttachmentType09F30B.Reserve);

        Transform parentHq = ResolveCommandParentTransform(returnParent);
        Vector3 center = parentHq != null
            ? parentHq.position
            : (BrigadeHqRoot != null ? BrigadeHqRoot.transform.position : unit.transform.position);

        Vector3 forward = parentHq != null ? Flat(parentHq.forward) : Vector3.forward;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 reserveGoal = Ground(center - forward * 55f + right * (side * 45f));
        PrototypeCavalryOfficerAI09F30C cavAi = PrototypeCavalryOfficerAI09F30C.Instance;
        if (cavAi != null)
            cavAi.SetHigherMission(
                unit,
                MajorOrder09F18.AssembleHere,
                reserveGoal,
                forward,
                false);
        else
            unit.OrderMove(reserveGoal, forward, true);
    }

    private Transform ResolveCommandParentTransform(string parent)
    {
        if (parent == DivisionId && DivisionHqRoot != null)
            return DivisionHqRoot.transform;
        if (parent == BrigadeId && BrigadeHqRoot != null)
            return BrigadeHqRoot.transform;
        if (parent == RegimentId && regimental != null && regimental.HqRoot != null)
            return regimental.HqRoot.transform;
        if (parent == PrototypeCavalryCommandControl09F30C.MajorAId &&
            hierarchy != null && hierarchy.GetMajorHq(0) != null)
            return hierarchy.GetMajorHq(0).transform;
        if (parent == PrototypeCavalryCommandControl09F30C.MajorBId &&
            hierarchy != null && hierarchy.GetMajorHq(1) != null)
            return hierarchy.GetMajorHq(1).transform;
        return BrigadeHqRoot != null ? BrigadeHqRoot.transform : null;
    }

    private void IssueAttachedCavalryMission(
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order,
        Vector3 objective,
        bool autonomous,
        Vector3? explicitFacing = null)
    {
        if (cavalry == null)
            return;

        Vector3 origin = level == PrototypeHigherCommandLevel09F30B.Division && DivisionHqRoot != null
            ? DivisionHqRoot.transform.position
            : BrigadeHqRoot != null ? BrigadeHqRoot.transform.position : objective - Vector3.forward;

        Vector3 forward = explicitFacing.HasValue
            ? Flat(explicitFacing.Value)
            : Flat(objective - origin);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(regimental != null && regimental.HqRoot != null
                ? regimental.HqRoot.transform.forward
                : Vector3.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        IssueCavalrySupportToUnit(cavalry.Gardehusar, level, order, objective, forward, right, -1, autonomous);
        IssueCavalrySupportToUnit(cavalry.Dragon, level, order, objective, forward, right, 1, autonomous);
    }

    private void IssueCavalrySupportToUnit(
        PrototypeCavalryUnit09F30 unit,
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order,
        Vector3 objective,
        Vector3 forward,
        Vector3 right,
        int side,
        bool autonomous)
    {
        if (unit == null || !IsCavalrySubordinateToLevel(unit, level))
            return;

        Vector3 goal;
        switch (order)
        {
            case MajorOrder09F18.DefendHere:
            {
                // F30S: defend CAV is anchored behind the battalion it supports,
                // not around the Division objective. This prevents one flank unit
                // from ending up visually in front of the infantry line.
                int battalionIndex = side < 0 ? 0 : 1;
                Vector3 battalionAnchor =
                    hierarchy != null && hierarchy.BattalionCount > battalionIndex
                        ? hierarchy.GetBattalionLastOrderPoint(battalionIndex)
                        : objective;

                goal = battalionAnchor
                    - forward * DefendCavalryRearDepth
                    + right * (side * DefendCavalryOutwardOffset);
                break;
            }
            case MajorOrder09F18.AttackHere:
                goal = objective - forward * 85f + right * (side * 145f);
                break;
            case MajorOrder09F18.WithdrawHere:
                goal = objective - forward * 75f + right * (side * 70f);
                break;
            case MajorOrder09F18.AssembleHere:
                goal = objective - forward * 70f + right * (side * 55f);
                break;
            case MajorOrder09F18.HoldPosition:
                goal = unit.transform.position;
                break;
            default:
                goal = objective - forward * 95f + right * (side * 85f);
                break;
        }

        goal = Ground(goal);
        PrototypeCavalryOfficerAI09F30C cavAi = PrototypeCavalryOfficerAI09F30C.Instance;
        if (cavAi != null)
            cavAi.SetHigherMission(unit, order, goal, forward, autonomous);
        else if (order == MajorOrder09F18.HoldPosition)
            unit.OrderHold();
        else
            unit.OrderMove(goal, forward, true);
    }

    private bool IsCavalrySubordinateToLevel(
        PrototypeCavalryUnit09F30 unit,
        PrototypeHigherCommandLevel09F30B level)
    {
        PrototypeCommandAttachment09F30B attachment = unit != null
            ? unit.GetComponent<PrototypeCommandAttachment09F30B>()
            : null;
        if (attachment == null)
            return false;

        string parent = attachment.CurrentCommandParent;
        if (level == PrototypeHigherCommandLevel09F30B.Division)
            return parent == DivisionId || parent == BrigadeId || parent == RegimentId ||
                   parent == PrototypeCavalryCommandControl09F30C.MajorAId ||
                   parent == PrototypeCavalryCommandControl09F30C.MajorBId;

        return parent == BrigadeId || parent == RegimentId ||
               parent == PrototypeCavalryCommandControl09F30C.MajorAId ||
               parent == PrototypeCavalryCommandControl09F30C.MajorBId;
    }

    public bool HasHigherOrderActive(
        PrototypeHigherCommandLevel09F30B level,
        MajorOrder09F18 order)
    {
        MajorOrder09F18 committed = level == PrototypeHigherCommandLevel09F30B.Division
            ? divisionMission : brigadeMission;
        if (committed != order)
            return false;

        if (regimental != null &&
            regimental.CurrentMissionOrder == order &&
            regimental.IsMissionActive(order))
            return true;

        // F30S: higher-HQ button state is execution-based. A cavalry mission may
        // remain logically assigned after arrival, but that must not keep the HUD
        // blue. Only actual CAV movement/reform/charge counts as active execution.
        if (cavalry != null)
        {
            if (IsCavalryExecuting(cavalry.Gardehusar, level))
                return true;
            if (IsCavalryExecuting(cavalry.Dragon, level))
                return true;
        }

        if (IsHigherHqStillMoving(level))
            return true;

        return false;
    }

    private bool IsHigherHqStillMoving(PrototypeHigherCommandLevel09F30B level)
    {
        if (regimental == null || regimental.HqRoot == null ||
            BrigadeHqRoot == null || DivisionHqRoot == null)
            return false;

        Vector3 forward = Flat(regimental.CurrentMissionFacing);
        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(regimental.HqRoot.transform.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.right;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.forward;

        Vector3 brigadeDesired = Ground(
            regimental.HqRoot.transform.position
            - forward * BrigadeFollowDistance
            + right * BrigadeLateralOffset);

        bool brigadeMoving =
            PlanarDistance(BrigadeHqRoot.transform.position, brigadeDesired) > 12f;

        if (level == PrototypeHigherCommandLevel09F30B.Brigade)
            return brigadeMoving;

        Vector3 divisionDesired = Ground(
            BrigadeHqRoot.transform.position
            - forward * DivisionFollowDistance
            + right * DivisionLateralOffset);

        bool divisionMoving =
            PlanarDistance(DivisionHqRoot.transform.position, divisionDesired) > 12f;

        return brigadeMoving || divisionMoving;
    }

    private bool IsCavalryExecuting(
        PrototypeCavalryUnit09F30 unit,
        PrototypeHigherCommandLevel09F30B level)
    {
        if (unit == null || !IsCavalrySubordinateToLevel(unit, level))
            return false;

        return unit.HasDestination ||
               unit.IsReforming ||
               unit.Action == PrototypeCavalryAction09F30.Move ||
               unit.Action == PrototypeCavalryAction09F30.Charge;
    }

    private void HandleWorldSelection()
    {
        PrototypeOfficerFacingOrder09F29G facingOrder =
            PrototypeOfficerFacingOrder09F29G.Instance;

        // F30S: placing/dragging an order objective must not be interpreted as
        // clicking empty ground and clearing the currently selected HQ.
        if (facingOrder != null && facingOrder.HasPendingOrder)
            return;

        if (cam == null || pendingOrder != MajorOrder09F18.None || !Input.GetMouseButtonDown(0))
            return;
        if (IsPointerOverUi(Input.mousePosition))
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        PrototypeHigherCommandMarker09F30B marker = null;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
                continue;
            marker = hits[i].collider.GetComponentInParent<PrototypeHigherCommandMarker09F30B>();
            if (marker != null)
                break;
        }

        if (marker != null)
        {
            SelectLevel(marker.Level, false);
            return;
        }

        if (SelectedLevel != PrototypeHigherCommandLevel09F30B.None)
            ClearSelectionOnly();
    }

    private void UpdateHqFollow()
    {
        if (regimental == null || regimental.HqRoot == null || BrigadeHqRoot == null || DivisionHqRoot == null)
            return;

        Vector3 regimentForward = Flat(regimental.CurrentMissionFacing);
        if (regimentForward.sqrMagnitude < 0.01f)
            regimentForward = Flat(regimental.HqRoot.transform.forward);
        if (regimentForward.sqrMagnitude < 0.01f)
            regimentForward = Vector3.right;
        regimentForward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, regimentForward).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.forward;

        Vector3 brigadeDesired = Ground(
            regimental.HqRoot.transform.position
            - regimentForward * BrigadeFollowDistance
            + right * BrigadeLateralOffset);
        MoveHqToward(
            BrigadeHqRoot.transform,
            brigadeDesired,
            BrigadeMoveSpeed,
            regimentForward);

        Vector3 divisionDesired = Ground(
            BrigadeHqRoot.transform.position
            - regimentForward * DivisionFollowDistance
            + right * DivisionLateralOffset);
        MoveHqToward(
            DivisionHqRoot.transform,
            divisionDesired,
            DivisionMoveSpeed,
            regimentForward);
    }

    private static void MoveHqToward(Transform hq, Vector3 desired, float speed, Vector3 facing)
    {
        if (hq == null)
            return;
        Vector3 delta = desired - hq.position;
        delta.y = 0f;
        if (delta.magnitude > 12f)
        {
            Vector3 step = delta.normalized * speed * Time.deltaTime;
            if (step.magnitude > delta.magnitude)
                step = delta;
            hq.position = Ground(hq.position + step);
        }

        Vector3 flatFacing = Flat(facing);
        if (flatFacing.sqrMagnitude > 0.01f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(flatFacing.normalized, Vector3.up);
            hq.rotation = Quaternion.Slerp(hq.rotation, desiredRot, 2.5f * Time.deltaTime);
        }
    }

    private void CreateLinks()
    {
        Material highLink = PrototypeBootstrap.CreateSharedMaterial(new Color(0.48f, 0.78f, 0.98f, 0.92f), "F30B_HigherLink");
        Material supportLink = PrototypeBootstrap.CreateSharedMaterial(new Color(0.78f, 0.66f, 0.28f, 0.90f), "F30B_SupportLink");
        Material select = PrototypeBootstrap.CreateSharedMaterial(new Color(1f, 0.80f, 0.18f, 0.98f), "F30B_HqSelect");
        Material inner = PrototypeBootstrap.CreateSharedMaterial(new Color(0.35f, 0.68f, 0.88f, 0.45f), "F30P_HigherCommandInner");
        Material outer = PrototypeBootstrap.CreateSharedMaterial(new Color(0.82f, 0.52f, 0.18f, 0.40f), "F30P_HigherCommandOuter");

        divisionBrigadeLink = CreateLine("F30B_DivisionBrigadeLink", highLink, 0.20f);
        brigadeRegimentLink = CreateLine("F30B_BrigadeRegimentLink", highLink, 0.20f);
        cavalryLinkA = CreateLine("F30B_CavalryLink_Gardehusar", supportLink, 0.17f);
        cavalryLinkB = CreateLine("F30B_CavalryLink_Dragon", supportLink, 0.17f);
        divisionSelection = CreateLine("F30B_DivisionSelection", select, 0.26f);
        brigadeSelection = CreateLine("F30B_BrigadeSelection", select, 0.26f);

        divisionCommandInner = CreateLine("F30P_DivisionCommandInner", inner, 0.12f);
        divisionCommandOuter = CreateLine("F30P_DivisionCommandOuter", outer, 0.12f);
        brigadeCommandInner = CreateLine("F30P_BrigadeCommandInner", inner, 0.12f);
        brigadeCommandOuter = CreateLine("F30P_BrigadeCommandOuter", outer, 0.12f);
    }

    private void UpdateCommandLinks()
    {
        if (DivisionHqRoot == null || BrigadeHqRoot == null || regimental == null || regimental.HqRoot == null)
            return;

        bool divisionSelected = SelectedLevel == PrototypeHigherCommandLevel09F30B.Division;
        bool brigadeSelected = SelectedLevel == PrototypeHigherCommandLevel09F30B.Brigade;
        bool regimentSelected = regimental.Selected;
        bool cavalryASelected = cavalry != null && cavalry.Gardehusar != null && cavalry.Gardehusar.IsSelected;
        bool cavalryBSelected = cavalry != null && cavalry.Dragon != null && cavalry.Dragon.IsSelected;

        SetTerrainLink(divisionBrigadeLink, DivisionHqRoot.transform.position, BrigadeHqRoot.transform.position,
            divisionSelected || brigadeSelected);
        SetTerrainLink(brigadeRegimentLink, BrigadeHqRoot.transform.position, regimental.HqRoot.transform.position,
            divisionSelected || brigadeSelected || regimentSelected);

        UpdateCavalryLink(cavalryLinkA, cavalry != null ? cavalry.Gardehusar : null,
            brigadeSelected || cavalryASelected || divisionSelected);
        UpdateCavalryLink(cavalryLinkB, cavalry != null ? cavalry.Dragon : null,
            brigadeSelected || cavalryBSelected || divisionSelected);

        SetCircle(divisionSelection, DivisionHqRoot.transform.position, 10.5f, divisionSelected);
        SetCircle(brigadeSelection, BrigadeHqRoot.transform.position, 9.5f, brigadeSelected);

        SetCircle(
            divisionCommandInner,
            DivisionHqRoot.transform.position,
            DivisionCommandInner,
            divisionSelected);
        SetCircle(
            divisionCommandOuter,
            DivisionHqRoot.transform.position,
            DivisionCommandOuter,
            divisionSelected);
        SetCircle(
            brigadeCommandInner,
            BrigadeHqRoot.transform.position,
            BrigadeCommandInner,
            brigadeSelected);
        SetCircle(
            brigadeCommandOuter,
            BrigadeHqRoot.transform.position,
            BrigadeCommandOuter,
            brigadeSelected);
    }

    private void UpdateCavalryLink(LineRenderer line, PrototypeCavalryUnit09F30 unit, bool requested)
    {
        if (line == null || unit == null)
        {
            if (line != null) line.enabled = false;
            return;
        }

        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        bool regimentParent = attachment != null && attachment.CurrentCommandParent == RegimentId;
        Vector3 from = regimentParent && regimental != null && regimental.HqRoot != null
            ? regimental.HqRoot.transform.position
            : BrigadeHqRoot.transform.position;
        SetTerrainLink(line, from, unit.transform.position, requested);
    }

    private static LineRenderer CreateLine(string name, Material material, float width)
    {
        GameObject root = new GameObject(name);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.positionCount = 0;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.enabled = false;
        return line;
    }

    private static void SetTerrainLink(LineRenderer line, Vector3 a, Vector3 b, bool enabled)
    {
        if (line == null)
            return;
        line.enabled = enabled;
        if (!enabled)
            return;

        line.positionCount = LinkSamples + 1;
        for (int i = 0; i <= LinkSamples; i++)
        {
            float t = i / (float)LinkSamples;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 1.18f;
            line.SetPosition(i, p);
        }
    }

    private static void SetCircle(LineRenderer line, Vector3 center, float radius, bool enabled)
    {
        if (line == null)
            return;
        line.enabled = enabled;
        if (!enabled)
            return;

        const int segments = 56;
        line.loop = true;
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.38f;
            line.SetPosition(i, p);
        }
    }

    private GameObject CreateHigherHq(string name, string echelon, PrototypeHigherCommandLevel09F30B level,
        Vector3 position, Color officerColor)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Ground(position);
        root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        PrototypeHigherCommandMarker09F30B marker = root.AddComponent<PrototypeHigherCommandMarker09F30B>();
        marker.Level = level;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 2.0f, 0f);
        collider.size = new Vector3(17f, 5.5f, 13f);

        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.25f, 0.15f, 0.08f), name + "_Horse");
        Material officer = PrototypeBootstrap.CreateSharedMaterial(officerColor, name + "_Officer");
        Material leather = PrototypeBootstrap.CreateSharedMaterial(new Color(0.09f, 0.055f, 0.03f), name + "_Leather");
        Material brass = PrototypeBootstrap.CreateSharedMaterial(new Color(0.78f, 0.65f, 0.23f), name + "_Brass");

        CreateMountedStaff(root.transform, new Vector3(0f, 0f, 1.2f), true, horse, officer, leather, brass);
        CreateMountedStaff(root.transform, new Vector3(-3.6f, 0f, -1.8f), false, horse, officer, leather, brass);
        CreateMountedStaff(root.transform, new Vector3(0f, 0f, -2.5f), false, horse, officer, leather, brass);
        CreateMountedStaff(root.transform, new Vector3(3.6f, 0f, -1.8f), false, horse, officer, leather, brass);

        GameObject labelRoot = new GameObject("HQ_Label_" + echelon.Replace(" ", "_"));
        labelRoot.transform.SetParent(root.transform, false);
        labelRoot.transform.localPosition = new Vector3(0f, 5.2f, 0f);
        TextMesh text = labelRoot.AddComponent<TextMesh>();
        text.text = echelon;
        text.fontSize = 42;
        text.characterSize = 0.16f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.92f, 0.86f, 0.58f, 1f);
        labelRoot.AddComponent<PrototypeHigherCommandBillboard09F30B>();

        return root;
    }

    private static void CreateMountedStaff(Transform parent, Vector3 localPosition, bool commander,
        Material horse, Material officer, Material leather, Material brass)
    {
        GameObject mount = new GameObject(commander ? "HigherHQ_Commander" : "HigherHQ_Staff");
        mount.transform.SetParent(parent, false);
        mount.transform.localPosition = localPosition;

        CreatePart(mount.transform, PrimitiveType.Cube, "HorseBody", new Vector3(0f, 1.05f, 0f),
            new Vector3(0.72f, 0.68f, 1.70f), Quaternion.identity, horse);
        CreatePart(mount.transform, PrimitiveType.Cube, "HorseNeck", new Vector3(0f, 1.52f, 0.72f),
            new Vector3(0.42f, 0.92f, 0.42f), Quaternion.Euler(-22f, 0f, 0f), horse);
        CreatePart(mount.transform, PrimitiveType.Cube, "HorseHead", new Vector3(0f, 1.92f, 1.10f),
            new Vector3(0.42f, 0.46f, 0.68f), Quaternion.identity, horse);
        CreatePart(mount.transform, PrimitiveType.Cube, "Saddle", new Vector3(0f, 1.46f, -0.05f),
            new Vector3(0.78f, 0.18f, 0.78f), Quaternion.identity, leather);
        CreatePart(mount.transform, PrimitiveType.Capsule, "OfficerBody", new Vector3(0f, 2.20f, -0.05f),
            new Vector3(0.36f, 0.50f, 0.36f), Quaternion.identity, officer);
        CreatePart(mount.transform, PrimitiveType.Sphere, "OfficerHead", new Vector3(0f, 2.94f, -0.02f),
            new Vector3(0.29f, 0.33f, 0.29f), Quaternion.identity, leather);
        if (commander)
            CreatePart(mount.transform, PrimitiveType.Cube, "OfficerSash", new Vector3(0.18f, 2.30f, -0.24f),
                new Vector3(0.08f, 0.72f, 0.06f), Quaternion.Euler(0f, 0f, 24f), brass);
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, string name,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        return part;
    }

    private bool TryGetGround(Vector3 mouse, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mouse);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
                continue;
            if (hits[i].collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hits[i].collider.GetComponentInParent<PrototypeCavalryUnit09F30>() != null)
                continue;
            if (hits[i].collider.GetComponentInParent<PrototypeHigherCommandMarker09F30B>() != null)
                continue;
            if (hits[i].collider.gameObject.name == "Battlefield Ground")
            {
                point = Ground(hits[i].point);
                return true;
            }
        }

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
            return false;
        point = ray.GetPoint(distance);
        point = Ground(point);
        return true;
    }

    private static bool IsPointerOverUi(Vector3 mouse)
    {
        if (mouse.y <= HudHeight)
            return true;
        if (PrototypeHigherCommandOob09F30B.IsPointerOverPanel(mouse))
            return true;
        return BattleManager.Instance != null && BattleManager.Instance.IsPointerOverSimulationControls(mouse);
    }

    private void OnGUI()
    {
        if (!Installed || SelectedLevel == PrototypeHigherCommandLevel09F30B.None)
            return;

        // F30O: the authoritative F29G unified HUD now draws Regiment, Brigade and
        // Division through one renderer. Keep this legacy panel only as fallback.
        if (PrototypeUnifiedCommandHud09F29G.Instance != null)
            return;

        EnsureStyles();

        // F30N: Higher HQ uses the Regimental HQ HUD as the literal visual template:
        // same panel, header, AI/doctrine placement, two-line info area and one-row
        // six-button mission geometry. Only the text/data source changes.
        GUI.depth = -7600;
        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, string.Empty, panelStyle);

        bool division = SelectedLevel == PrototypeHigherCommandLevel09F30B.Division;
        string title = division
            ? "1. DIVISION | DIVISIONSCHEF | HØJERE KOMMANDO"
            : "1. BRIGADE | BRIGADECHEF | HØJERE KOMMANDO";

        float pad = 7f;
        float headerY = panel.y + 3f;
        GUI.Box(new Rect(pad, headerY, panel.width - pad * 2f, 19f), title, headerStyle);

        bool aiOn = GetAIEnabled(SelectedLevel);
        OfficerAIDoctrine doctrine = GetDoctrine(SelectedLevel);

        // Exact copy of Regimental HUD top-right AI/doctrine placement.
        float aiX = panel.xMax - 274f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 70f, 17f), aiOn ? "AI ON" : "AI OFF",
                aiOn ? accentStyle : buttonStyle))
            ToggleAI(SelectedLevel);
        aiX += 73f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF", buttonStyle))
            SetDoctrine(SelectedLevel, OfficerAIDoctrine.Defensive);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL", buttonStyle))
            SetDoctrine(SelectedLevel, OfficerAIDoctrine.Balanced);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF", buttonStyle))
            SetDoctrine(SelectedLevel, OfficerAIDoctrine.Offensive);

        // Exact copy of Regimental HUD lower-row geometry.
        float infoWidth = Mathf.Clamp(panel.width * 0.33f, 390f, 560f);
        float rowY = panel.y + 27f;

        MajorOrder09F18 missionOrder = division ? divisionMission : brigadeMission;
        string mission = missionOrder == MajorOrder09F18.None
            ? "Ingen aktiv ordre"
            : HigherOrderLabel(missionOrder);
        int strength = AggregateHigherStrength(SelectedLevel);

        string regAi = regimental != null && regimental.AIEnabled ? "ON" : "OFF";
        string batA = hierarchy != null && hierarchy.GetBattalionAIEnabled(0) ? "ON" : "OFF";
        string batB = hierarchy != null && hierarchy.GetBattalionAIEnabled(1) ? "ON" : "OFF";
        PrototypeCavalryOfficerAI09F30C cavAi = PrototypeCavalryOfficerAI09F30C.Instance;
        string gardeAi = cavAi != null && cavalry != null && cavalry.Gardehusar != null &&
                         cavAi.IsAIEnabled(cavalry.Gardehusar) ? "ON" : "OFF";
        string dragonAi = cavAi != null && cavalry != null && cavalry.Dragon != null &&
                          cavAi.IsAIEnabled(cavalry.Dragon) ? "ON" : "OFF";

        GUI.Label(new Rect(pad + 3f, rowY, infoWidth - 8f, 15f),
            mission + " | Mænd " + strength, labelStyle);
        GUI.Label(new Rect(pad + 3f, rowY + 16f, infoWidth - 8f, 15f),
            "REG " + regAi + " | MAJ A " + batA + " | MAJ B " + batB +
            " | GARDE " + gardeAi + " | DRAGON " + dragonAi, mutedStyle);

        float commandX = infoWidth + 8f;
        float commandWidth = panel.width - commandX - 8f;
        const float gap = 4f;
        float buttonWidth = (commandWidth - gap * 5f) / 6f;
        const float commandHeight = 27f;

        DrawOrderButton(new Rect(commandX, rowY, buttonWidth, commandHeight),
            "ANGRIB HER", MajorOrder09F18.AttackHere);
        DrawOrderButton(new Rect(commandX + (buttonWidth + gap), rowY, buttonWidth, commandHeight),
            "FORSVAR HER", MajorOrder09F18.DefendHere);
        DrawOrderButton(new Rect(commandX + (buttonWidth + gap) * 2f, rowY, buttonWidth, commandHeight),
            "RYK FREM", MajorOrder09F18.AdvanceHere);
        DrawOrderButton(new Rect(commandX + (buttonWidth + gap) * 3f, rowY, buttonWidth, commandHeight),
            "TILBAGETRÆK", MajorOrder09F18.WithdrawHere);
        DrawOrderButton(new Rect(commandX + (buttonWidth + gap) * 4f, rowY, buttonWidth, commandHeight),
            "SAML", MajorOrder09F18.AssembleHere);

        bool holdActive = HasHigherOrderActive(SelectedLevel, MajorOrder09F18.HoldPosition);
        string holdLabel = holdActive ? "[HOLD]" : "HOLD";
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 5f, rowY, buttonWidth, commandHeight),
                holdLabel, buttonStyle))
        {
            Vector3 center = regimental != null && regimental.HqRoot != null
                ? regimental.HqRoot.transform.position
                : Vector3.zero;
            CommitHigherOrder(SelectedLevel, MajorOrder09F18.HoldPosition, center);
            pendingOrder = MajorOrder09F18.None;
            pendingLevel = PrototypeHigherCommandLevel09F30B.None;
        }

        if (pendingOrder != MajorOrder09F18.None && pendingLevel == SelectedLevel)
            GUI.Label(new Rect(commandX, rowY + 31f, commandWidth, 15f),
                "Klik på slagmarken: " + HigherOrderLabel(pendingOrder), mutedStyle);

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel))
            current.Use();
    }

    private static string HigherOrderLabel(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB HER";
            case MajorOrder09F18.DefendHere: return "FORSVAR HER";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AssembleHere: return "SAML";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            default: return order.ToString().ToUpperInvariant();
        }
    }

    private int AggregateHigherStrength(PrototypeHigherCommandLevel09F30B level)
    {
        int total = 0;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int b = 0; b < hierarchy.BattalionCount; b++)
            {
                IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
                if (companies == null)
                    continue;
                for (int i = 0; i < companies.Count; i++)
                    if (companies[i] != null)
                        total += companies[i].CurrentStrength;
            }
        }

        if (cavalry != null)
        {
            if (cavalry.Gardehusar != null && IsCavalrySubordinateToLevel(cavalry.Gardehusar, level))
                total += cavalry.Gardehusar.CurrentStrength;
            if (cavalry.Dragon != null && IsCavalrySubordinateToLevel(cavalry.Dragon, level))
                total += cavalry.Dragon.CurrentStrength;
        }
        return total;
    }

    private void DrawOrderButton(Rect rect, string label, MajorOrder09F18 order)
    {
        bool active = pendingOrder == order && pendingLevel == SelectedLevel;
        active = active || HasHigherOrderActive(SelectedLevel, order);

        // Keep the exact Regimental button palette. Active state is indicated in
        // the caption instead of introducing a different higher-HQ button colour.
        string caption = active ? "[" + label + "]" : label;
        if (GUI.Button(rect, caption, buttonStyle))
        {
            pendingOrder = order;
            pendingLevel = SelectedLevel;
            Debug.Log("HQ-09F30N|OrderPending=True|Level=" + SelectedLevel + "|Order=" + order);
        }
    }

    private void DrawAttachmentButton(PrototypeCavalryUnit09F30 unit, Rect rect)
    {
        if (unit == null)
            return;
        string parent = GetCavalryCommandParent(unit);
        string shortParent = parent == RegimentId ? "REGIMENT" : "BRIGADE";
        if (GUI.Button(rect, unit.UnitName + " → " + shortParent, buttonStyle))
            ToggleCavalryAttachment(unit);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        // F30N: literal Regimental HQ palette/typography.
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        sectionStyle = PrototypeUiTheme09F15.Section(8);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);

        // Retained for binary/source compatibility with older F30M paths, but the
        // higher-HQ HUD no longer uses a separate colour language.
        activeBlueStyle = new GUIStyle(buttonStyle);
        activeGreenStyle = new GUIStyle(accentStyle);
        dangerStyle = new GUIStyle(buttonStyle);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private static Vector3 Ground(Vector3 p)
    {
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 18f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 18f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.10f;
        return p;
    }
}

// Keeps world-space HQ text facing the camera without owning HQ movement.
[DefaultExecutionOrder(42500)]
public sealed class PrototypeHigherCommandBillboard09F30B : MonoBehaviour
{
    private Camera cam;
    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 toward = transform.position - cam.transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
    }
}
