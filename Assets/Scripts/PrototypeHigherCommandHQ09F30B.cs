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

    public const string DivisionId = "1. DIVISION";
    public const string BrigadeId = "1. BRIGADE";
    public const string RegimentId = "1. REGIMENT";

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float HudHeight = 90f;
    private const float BrigadeFollowDistance = 125f;
    private const float DivisionFollowDistance = 190f;
    private const float BrigadeMoveSpeed = 6.0f;
    private const float DivisionMoveSpeed = 5.2f;
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

    private LineRenderer divisionBrigadeLink;
    private LineRenderer brigadeRegimentLink;
    private LineRenderer cavalryLinkA;
    private LineRenderer cavalryLinkB;
    private LineRenderer divisionSelection;
    private LineRenderer brigadeSelection;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeBlueStyle;
    private Texture2D activeBlueTexture;

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

        BrigadeHqRoot = CreateHigherHq(
            "DK_Brigade_HQ_Brigadechef", "X HQ", PrototypeHigherCommandLevel09F30B.Brigade,
            Ground(regimentPos + rear * BrigadeFollowDistance + Vector3.forward * 75f),
            new Color(0.07f, 0.17f, 0.31f));

        DivisionHqRoot = CreateHigherHq(
            "DK_Division_HQ_Divisionschef", "XX HQ", PrototypeHigherCommandLevel09F30B.Division,
            Ground(BrigadeHqRoot.transform.position + rear * DivisionFollowDistance - Vector3.forward * 95f),
            new Color(0.09f, 0.20f, 0.36f));

        ConfigureCommandParents();
        CreateLinks();
        Installed = BrigadeHqRoot != null && DivisionHqRoot != null;

        Debug.Log(
            "HQ-09F30B|Installed=" + Installed +
            "|Chain=DIVISION>BRIGADE>REGIMENT>BATTALION>COMPANY|" +
            "CavalryParent=BRIGADE|ArtilleryReadyAttachmentModel=True|HigherAI=False");
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

    private void CommitHigherOrder(PrototypeHigherCommandLevel09F30B level, MajorOrder09F18 order, Vector3 point)
    {
        if (regimental == null || !regimental.Installed)
            return;

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

        regimental.IssueRegimentalOrder(order, point, false);
        Debug.Log("HQ-09F30B|MissionCommitted=True|Level=" + level + "|Order=" + order +
                  "|DelegatedTo=1.REGIMENT|Objective=" + point.x.ToString("0.0") + "," + point.z.ToString("0.0"));
    }

    private void HandleWorldSelection()
    {
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

        Vector3 regimentForward = Flat(regimental.HqRoot.transform.forward);
        if (regimentForward.sqrMagnitude < 0.01f)
            regimentForward = Vector3.right;
        regimentForward.Normalize();

        Vector3 brigadeDesired = Ground(
            regimental.HqRoot.transform.position - regimentForward * BrigadeFollowDistance + Vector3.forward * 75f);
        MoveHqToward(BrigadeHqRoot.transform, brigadeDesired, BrigadeMoveSpeed, regimentForward);

        Vector3 brigadeForward = Flat(BrigadeHqRoot.transform.forward);
        if (brigadeForward.sqrMagnitude < 0.01f)
            brigadeForward = regimentForward;
        brigadeForward.Normalize();

        Vector3 divisionDesired = Ground(
            BrigadeHqRoot.transform.position - brigadeForward * DivisionFollowDistance - Vector3.forward * 95f);
        MoveHqToward(DivisionHqRoot.transform, divisionDesired, DivisionMoveSpeed, brigadeForward);
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

        divisionBrigadeLink = CreateLine("F30B_DivisionBrigadeLink", highLink, 0.20f);
        brigadeRegimentLink = CreateLine("F30B_BrigadeRegimentLink", highLink, 0.20f);
        cavalryLinkA = CreateLine("F30B_CavalryLink_Gardehusar", supportLink, 0.17f);
        cavalryLinkB = CreateLine("F30B_CavalryLink_Dragon", supportLink, 0.17f);
        divisionSelection = CreateLine("F30B_DivisionSelection", select, 0.26f);
        brigadeSelection = CreateLine("F30B_BrigadeSelection", select, 0.26f);
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

        EnsureStyles();
        GUI.depth = -7600;
        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        bool division = SelectedLevel == PrototypeHigherCommandLevel09F30B.Division;
        string title = division
            ? "1. DIVISION | DIVISIONSCHEF | HØJERE KOMMANDO"
            : "1. BRIGADE | BRIGADECHEF | HØJERE KOMMANDO";
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f), title, headerStyle);

        float infoWidth = Mathf.Clamp(panel.width * 0.30f, 320f, 470f);
        float commandX = infoWidth + 12f;
        float commandWidth = Mathf.Max(620f, panel.width - commandX - 8f);
        float y = panel.y + 22f;

        GUI.Label(new Rect(8f, y, infoWidth - 14f, 11f), "KOMMANDO / UNDERENHEDER", sectionStyle);
        string mission = division ? divisionMission.ToString().ToUpperInvariant() : brigadeMission.ToString().ToUpperInvariant();
        if (mission == MajorOrder09F18.None.ToString().ToUpperInvariant()) mission = "INGEN AKTIV ORDRE";
        GUI.Label(new Rect(10f, y + 13f, infoWidth - 18f, 14f), "Mission: " + mission, labelStyle);
        GUI.Label(new Rect(10f, y + 28f, infoWidth - 18f, 14f),
            division ? "Under: 1. Brigade | Assets fordeles via Brigade" : "Under: 1. Regiment | Gardehusar | Dragoner",
            labelStyle);
        GUI.Label(new Rect(10f, y + 43f, infoWidth - 18f, 14f),
            "Higher Officer AI: ikke aktiv endnu | Ordre delegeres ned gennem HQ-kæden", labelStyle);

        GUI.Label(new Rect(commandX, y, commandWidth, 11f), "MISSIONSORDRER", sectionStyle);
        float gap = 4f;
        float orderW = (commandWidth * 0.67f - gap * 2f) / 3f;
        float row1 = y + 13f;
        float row2 = y + 38f;

        DrawOrderButton(new Rect(commandX, row1, orderW, 21f), "ANGRIB HER", MajorOrder09F18.AttackHere);
        DrawOrderButton(new Rect(commandX + orderW + gap, row1, orderW, 21f), "FORSVAR HER", MajorOrder09F18.DefendHere);
        DrawOrderButton(new Rect(commandX + (orderW + gap) * 2f, row1, orderW, 21f), "RYK FREM", MajorOrder09F18.AdvanceHere);
        DrawOrderButton(new Rect(commandX, row2, orderW, 21f), "TILBAGETRÆK", MajorOrder09F18.WithdrawHere);
        DrawOrderButton(new Rect(commandX + orderW + gap, row2, orderW, 21f), "SAML", MajorOrder09F18.AssembleHere);

        if (GUI.Button(new Rect(commandX + (orderW + gap) * 2f, row2, orderW, 21f), "STOP / HOLD", buttonStyle))
        {
            Vector3 center = regimental != null && regimental.HqRoot != null ? regimental.HqRoot.transform.position : Vector3.zero;
            CommitHigherOrder(SelectedLevel, MajorOrder09F18.HoldPosition, center);
            pendingOrder = MajorOrder09F18.None;
            pendingLevel = PrototypeHigherCommandLevel09F30B.None;
        }

        if (!division && cavalry != null)
        {
            float attachX = commandX + commandWidth * 0.69f;
            float attachW = commandWidth * 0.31f - 5f;
            GUI.Label(new Rect(attachX, y, attachW, 11f), "ATTACHMENT", sectionStyle);
            DrawAttachmentButton(cavalry.Gardehusar, new Rect(attachX, row1, attachW, 21f));
            DrawAttachmentButton(cavalry.Dragon, new Rect(attachX, row2, attachW, 21f));
        }

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel))
            current.Use();
    }

    private void DrawOrderButton(Rect rect, string label, MajorOrder09F18 order)
    {
        bool active = pendingOrder == order && pendingLevel == SelectedLevel;
        MajorOrder09F18 committed = SelectedLevel == PrototypeHigherCommandLevel09F30B.Division ? divisionMission : brigadeMission;
        active = active || committed == order;
        if (GUI.Button(rect, label, active ? activeBlueStyle : buttonStyle))
        {
            pendingOrder = order;
            pendingLevel = SelectedLevel;
            Debug.Log("HQ-09F30B|OrderPending=True|Level=" + SelectedLevel + "|Order=" + order);
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
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(10);
        sectionStyle = PrototypeUiTheme09F15.Section(8);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        buttonStyle = PrototypeUiTheme09F15.Button(8);

        activeBlueTexture = MakeTexture(new Color(0.18f, 0.42f, 0.72f, 1f));
        activeBlueStyle = new GUIStyle(buttonStyle);
        activeBlueStyle.normal.background = activeBlueTexture;
        activeBlueStyle.hover.background = activeBlueTexture;
        activeBlueStyle.active.background = activeBlueTexture;
        activeBlueStyle.normal.textColor = Color.white;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
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
