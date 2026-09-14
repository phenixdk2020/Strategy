using System.Collections.Generic;
using UnityEngine;

// v00.00.09f15 first physical battalion-HQ prototype.
// The Major is deliberately NOT autonomous AI in this build. The player selects the
// HQ and gives six battalion-level orders to two directly subordinate companies.
[DefaultExecutionOrder(250)]
public sealed class PrototypeMajorHQ09F15 : MonoBehaviour
{
    private enum MajorOrder
    {
        None,
        AttackHere,
        DefendHere,
        WithdrawHere,
        AdvanceHere,
        HoldPosition,
        AssembleHere
    }

    private sealed class UnitMission
    {
        public MajorOrder Order;
        public Vector3 Goal;
        public Vector3 FinalFacing;
        public bool AttackCommitted;
    }

    public static PrototypeMajorHQ09F15 Instance { get; private set; }

    private readonly List<Regiment> subordinates = new List<Regiment>();
    private readonly Dictionary<Regiment, UnitMission> missions = new Dictionary<Regiment, UnitMission>();
    private readonly Dictionary<Regiment, LineRenderer> commandLinks = new Dictionary<Regiment, LineRenderer>();

    private GameObject hqRoot;
    private BoxCollider selectionCollider;
    private LineRenderer selectionRing;
    private LineRenderer targetPreview;
    private bool selected;
    private MajorOrder pendingOrder = MajorOrder.None;
    private string lastOrderText = "Ingen bataljonsordre";
    private Camera cam;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;

    private Material horseMaterial;
    private Material leatherMaterial;
    private Material officerMaterial;
    private Material brassMaterial;
    private Material flagRedMaterial;
    private Material flagWhiteMaterial;
    private Material linkMaterial;
    private Material selectionMaterial;
    private Material targetMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMajorHQ09F15>() != null)
            return;

        GameObject root = new GameObject("PrototypeMajorHQ_v000009f15");
        root.AddComponent<PrototypeMajorHQ09F15>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

        if (hqRoot == null)
        {
            TryCreateHQ();
            return;
        }

        RefreshSubordinates();
        UpdateCommandLinks();
        UpdateMissionStates();
        UpdateTargetPreview();

        if (cam == null || !Input.GetMouseButtonDown(0) || IsPointerOverControls(Input.mousePosition))
            return;

        if (selected && pendingOrder != MajorOrder.None)
        {
            if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
            {
                IssueTargetedOrder(pendingOrder, point);
                pendingOrder = MajorOrder.None;
                SetTargetPreviewVisible(false);
            }
            return;
        }

        if (RayHitsHQ(Input.mousePosition))
        {
            SetSelected(true);
            return;
        }

        if (selected)
            SetSelected(false);
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (!selected)
            return false;

        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return GetPanelRect().Contains(guiPoint) ||
               (pendingOrder != MajorOrder.None && GetTargetHintRect().Contains(guiPoint));
    }

    private void TryCreateHQ()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 3)
            return;

        Regiment first = FindRegiment("1. Regiment");
        Regiment second = FindRegiment("5. Regiment");
        if (first == null || second == null)
            return;

        CreateMaterials();

        hqRoot = new GameObject("DK_Battalion_HQ_Major");
        Vector3 position = new Vector3(-315f, 0f, 0f);
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z);
        hqRoot.transform.position = position;
        hqRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        selectionCollider = hqRoot.AddComponent<BoxCollider>();
        selectionCollider.center = new Vector3(0f, 1.5f, 0f);
        selectionCollider.size = new Vector3(11f, 3.2f, 9f);

        CreateHorse(new Vector3(0f, 0f, 0.6f), 0f, true);
        CreateHorse(new Vector3(-3.0f, 0f, -1.8f), 8f, false);
        CreateHorse(new Vector3(3.0f, 0f, -1.8f), -8f, false);
        CreateHQFlag();
        CreateSelectionRing();
        CreateTargetPreview();

        RefreshSubordinates();
        CreateCommandLinks();

        Debug.Log(
            "HQ-09F15|Created=True|Rank=Major|Level=Battalion|AIEnabled=False|" +
            "Horses=3|Subordinates=2|Orders=Attack,Defend,Withdraw,Advance,Hold,Assemble");
    }

    private void CreateMaterials()
    {
        horseMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.30f, 0.18f, 0.10f), "HQHorse09F15");
        leatherMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.12f, 0.075f, 0.045f), "HQLeather09F15");
        officerMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.08f, 0.16f, 0.28f), "HQOfficer09F15");
        brassMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.60f, 0.24f), "HQBrass09F15");
        flagRedMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.68f, 0.06f, 0.08f), "HQFlagRed09F15");
        flagWhiteMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.94f, 0.91f, 0.82f), "HQFlagWhite09F15");
        linkMaterial = CreateUnlitMaterial(new Color(0.82f, 0.69f, 0.29f, 0.88f), "HQCommandLink09F15");
        selectionMaterial = CreateUnlitMaterial(new Color(1f, 0.80f, 0.18f, 0.95f), "HQSelection09F15");
        targetMaterial = CreateUnlitMaterial(new Color(0.92f, 0.72f, 0.18f, 0.95f), "HQTarget09F15");
    }

    private static Material CreateUnlitMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private void CreateHorse(Vector3 localPosition, float yaw, bool majorRider)
    {
        GameObject horse = new GameObject(majorRider ? "MajorHorse" : "HQHorse");
        horse.transform.SetParent(hqRoot.transform, false);
        horse.transform.localPosition = localPosition;
        horse.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "HorseBody",
            new Vector3(0f, 1.05f, 0f), new Vector3(0.72f, 0.68f, 1.70f), Quaternion.identity, horseMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "HorseNeck",
            new Vector3(0f, 1.52f, 0.72f), new Vector3(0.42f, 0.92f, 0.42f), Quaternion.Euler(-22f, 0f, 0f), horseMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "HorseHead",
            new Vector3(0f, 1.92f, 1.10f), new Vector3(0.42f, 0.46f, 0.68f), Quaternion.Euler(6f, 0f, 0f), horseMaterial);

        float[] legX = { -0.28f, 0.28f, -0.28f, 0.28f };
        float[] legZ = { -0.58f, -0.58f, 0.58f, 0.58f };
        for (int i = 0; i < 4; i++)
        {
            CreatePrimitivePart(horse.transform, PrimitiveType.Cylinder, "HorseLeg",
                new Vector3(legX[i], 0.47f, legZ[i]), new Vector3(0.11f, 0.47f, 0.11f), Quaternion.identity, horseMaterial);
        }

        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "Saddle",
            new Vector3(0f, 1.46f, -0.05f), new Vector3(0.78f, 0.18f, 0.78f), Quaternion.identity, leatherMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "Tail",
            new Vector3(0f, 1.05f, -1.02f), new Vector3(0.16f, 0.85f, 0.16f), Quaternion.Euler(28f, 0f, 0f), leatherMaterial);

        if (!majorRider)
            return;

        CreatePrimitivePart(horse.transform, PrimitiveType.Capsule, "MajorBody",
            new Vector3(0f, 2.20f, -0.05f), new Vector3(0.34f, 0.48f, 0.34f), Quaternion.identity, officerMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Sphere, "MajorHead",
            new Vector3(0f, 2.92f, -0.02f), new Vector3(0.28f, 0.32f, 0.28f), Quaternion.identity, flagWhiteMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cylinder, "MajorCap",
            new Vector3(0f, 3.18f, -0.02f), new Vector3(0.30f, 0.08f, 0.30f), Quaternion.identity, leatherMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "MajorSash",
            new Vector3(0.18f, 2.30f, -0.24f), new Vector3(0.08f, 0.68f, 0.06f), Quaternion.Euler(0f, 0f, 24f), brassMaterial);
        CreatePrimitivePart(horse.transform, PrimitiveType.Cube, "MajorSword",
            new Vector3(-0.42f, 1.95f, -0.05f), new Vector3(0.05f, 0.75f, 0.05f), Quaternion.Euler(0f, 0f, -8f), brassMaterial);
    }

    private GameObject CreatePrimitivePart(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
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

    private void CreateHQFlag()
    {
        GameObject flagRoot = new GameObject("MajorHQFlag");
        flagRoot.transform.SetParent(hqRoot.transform, false);
        flagRoot.transform.localPosition = new Vector3(-4.2f, 0f, 2.1f);

        CreatePrimitivePart(flagRoot.transform, PrimitiveType.Cylinder, "FlagPole",
            new Vector3(0f, 2.3f, 0f), new Vector3(0.055f, 2.3f, 0.055f), Quaternion.identity, leatherMaterial);
        CreatePrimitivePart(flagRoot.transform, PrimitiveType.Cube, "DannebrogField",
            new Vector3(0.75f, 4.05f, 0f), new Vector3(1.55f, 0.82f, 0.06f), Quaternion.identity, flagRedMaterial);
        CreatePrimitivePart(flagRoot.transform, PrimitiveType.Cube, "DannebrogVertical",
            new Vector3(0.45f, 4.05f, -0.04f), new Vector3(0.18f, 0.84f, 0.035f), Quaternion.identity, flagWhiteMaterial);
        CreatePrimitivePart(flagRoot.transform, PrimitiveType.Cube, "DannebrogHorizontal",
            new Vector3(0.75f, 4.05f, -0.04f), new Vector3(1.56f, 0.16f, 0.035f), Quaternion.identity, flagWhiteMaterial);
    }

    private void CreateSelectionRing()
    {
        GameObject ringObject = new GameObject("MajorSelectionRing");
        ringObject.transform.SetParent(hqRoot.transform, false);
        selectionRing = ringObject.AddComponent<LineRenderer>();
        selectionRing.useWorldSpace = true;
        selectionRing.loop = true;
        selectionRing.widthMultiplier = 0.22f;
        selectionRing.positionCount = 48;
        selectionRing.sharedMaterial = selectionMaterial;
        selectionRing.enabled = false;
        UpdateCircle(selectionRing, hqRoot.transform.position, 6.2f, 0.30f);
    }

    private void CreateTargetPreview()
    {
        GameObject targetObject = new GameObject("MajorOrderTargetPreview");
        targetObject.transform.SetParent(transform, false);
        targetPreview = targetObject.AddComponent<LineRenderer>();
        targetPreview.useWorldSpace = true;
        targetPreview.loop = true;
        targetPreview.widthMultiplier = 0.24f;
        targetPreview.positionCount = 48;
        targetPreview.sharedMaterial = targetMaterial;
        targetPreview.enabled = false;
    }

    private void RefreshSubordinates()
    {
        Regiment first = FindRegiment("1. Regiment");
        Regiment second = FindRegiment("5. Regiment");

        bool changed = subordinates.Count != 2 ||
                       (subordinates.Count > 0 && subordinates[0] != first) ||
                       (subordinates.Count > 1 && subordinates[1] != second);

        if (!changed)
            return;

        subordinates.Clear();
        if (first != null)
            subordinates.Add(first);
        if (second != null)
            subordinates.Add(second);

        CreateCommandLinks();
    }

    private Regiment FindRegiment(string name)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.RegimentName == name)
                return regiment;

        return null;
    }

    private void CreateCommandLinks()
    {
        foreach (KeyValuePair<Regiment, LineRenderer> pair in commandLinks)
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        commandLinks.Clear();

        foreach (Regiment regiment in subordinates)
        {
            if (regiment == null)
                continue;

            GameObject linkObject = new GameObject("MajorCommandLink_" + regiment.RegimentName);
            linkObject.transform.SetParent(transform, false);
            LineRenderer line = linkObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.widthMultiplier = 0.17f;
            line.positionCount = 20;
            line.numCapVertices = 2;
            line.sharedMaterial = linkMaterial;
            line.enabled = selected;
            commandLinks[regiment] = line;
        }
    }

    private void UpdateCommandLinks()
    {
        if (hqRoot == null)
            return;

        foreach (KeyValuePair<Regiment, LineRenderer> pair in commandLinks)
        {
            Regiment regiment = pair.Key;
            LineRenderer line = pair.Value;
            if (line == null)
                continue;

            line.enabled = selected && regiment != null;
            if (!line.enabled)
                continue;

            Vector3 a = hqRoot.transform.position;
            Vector3 b = regiment.transform.position;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
                line.SetPosition(i, p);
            }
        }
    }

    private void SetSelected(bool value)
    {
        selected = value;
        pendingOrder = MajorOrder.None;
        if (selectionRing != null)
        {
            selectionRing.enabled = selected;
            UpdateCircle(selectionRing, hqRoot.transform.position, 6.2f, 0.30f);
        }
        SetTargetPreviewVisible(false);
    }

    private bool RayHitsHQ(Vector3 mousePosition)
    {
        if (cam == null || hqRoot == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider != null ? hit.collider.transform : null;
            if (t == null)
                continue;
            if (t == hqRoot.transform || t.IsChildOf(hqRoot.transform))
                return true;
        }

        return false;
    }

    private bool TryGetGroundPoint(Vector3 mousePosition, out Vector3 point)
    {
        point = default;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hqRoot != null && (hit.collider.transform == hqRoot.transform || hit.collider.transform.IsChildOf(hqRoot.transform)))
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;

            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        return false;
    }

    private void IssueTargetedOrder(MajorOrder order, Vector3 point)
    {
        if (subordinates.Count == 0)
            return;

        Vector3 forward = point - hqRoot.transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = hqRoot.transform.forward;
        forward.Normalize();

        Vector3 lineDirection = Vector3.Cross(Vector3.up, forward).normalized;
        float spacing = order == MajorOrder.AssembleHere ? 16f : 42f;
        float centerIndex = (subordinates.Count - 1) * 0.5f;

        for (int i = 0; i < subordinates.Count; i++)
        {
            Regiment regiment = subordinates[i];
            if (regiment == null || regiment.IsRouted)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);

            Vector3 goal = point + lineDirection * ((i - centerIndex) * spacing);
            goal.y = PrototypeBootstrap.SampleGroundHeight(goal.x, goal.z) + 0.10f;

            UnitMission mission = new UnitMission
            {
                Order = order,
                Goal = goal,
                FinalFacing = forward,
                AttackCommitted = false
            };
            missions[regiment] = mission;

            switch (order)
            {
                case MajorOrder.AttackHere:
                    regiment.SetFirePolicy(RegimentFirePolicy.LongRange);
                    regiment.OrderMove(goal);
                    break;
                case MajorOrder.DefendHere:
                    regiment.OrderMove(goal);
                    break;
                case MajorOrder.WithdrawHere:
                    regiment.SetFormation(RegimentFormation.Line);
                    regiment.OrderMove(goal);
                    break;
                case MajorOrder.AdvanceHere:
                    regiment.OrderMove(goal);
                    break;
                case MajorOrder.AssembleHere:
                    regiment.OrderMove(goal);
                    break;
            }
        }

        lastOrderText = GetOrderLabel(order) + " | mål " + point.x.ToString("0") + ", " + point.z.ToString("0");
        Debug.Log("HQ-ORDER-09F15|Major=True|Order=" + order + "|X=" + point.x.ToString("0.0") + "|Z=" + point.z.ToString("0.0") + "|Recipients=" + subordinates.Count);
    }

    private void IssueHoldOrder()
    {
        pendingOrder = MajorOrder.None;
        missions.Clear();

        foreach (Regiment regiment in subordinates)
        {
            if (regiment == null || regiment.IsRouted)
                continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && controller.AIEnabled)
                controller.SetAIEnabled(false);
            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();
        }

        lastOrderText = "HOLD POSITION";
        Debug.Log("HQ-ORDER-09F15|Major=True|Order=HoldPosition|Recipients=" + subordinates.Count);
    }

    private void UpdateMissionStates()
    {
        if (missions.Count == 0)
            return;

        List<Regiment> completed = null;
        foreach (KeyValuePair<Regiment, UnitMission> pair in missions)
        {
            Regiment regiment = pair.Key;
            UnitMission mission = pair.Value;
            if (regiment == null || regiment.IsRouted)
            {
                AddCompleted(ref completed, regiment);
                continue;
            }

            if (mission.Order == MajorOrder.AttackHere && !mission.AttackCommitted)
            {
                Regiment enemy = FindNearestEnemy(regiment, 135f);
                if (enemy != null)
                {
                    regiment.OrderAttack(enemy);
                    mission.AttackCommitted = true;
                    continue;
                }
            }

            if (mission.AttackCommitted)
                continue;

            if (PlanarDistance(regiment.transform.position, mission.Goal) > 5.5f)
                continue;

            regiment.SetFormation(RegimentFormation.Line);
            regiment.OrderHold();

            Regiment nearest = FindNearestEnemy(regiment, 600f);
            Vector3 facing = nearest != null
                ? nearest.transform.position - regiment.transform.position
                : mission.FinalFacing;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f)
                regiment.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            AddCompleted(ref completed, regiment);
        }

        if (completed == null)
            return;
        foreach (Regiment regiment in completed)
            if (regiment != null)
                missions.Remove(regiment);
    }

    private static void AddCompleted(ref List<Regiment> completed, Regiment regiment)
    {
        if (completed == null)
            completed = new List<Regiment>();
        completed.Add(regiment);
    }

    private Regiment FindNearestEnemy(Regiment from, float maxDistance)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || from == null)
            return null;

        Regiment nearest = null;
        float best = maxDistance;
        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == from.Team || candidate.IsRouted)
                continue;
            float distance = PlanarDistance(from.transform.position, candidate.transform.position);
            if (distance >= best)
                continue;
            best = distance;
            nearest = candidate;
        }
        return nearest;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void UpdateTargetPreview()
    {
        if (!selected || pendingOrder == MajorOrder.None || targetPreview == null || cam == null)
        {
            SetTargetPreviewVisible(false);
            return;
        }

        if (!TryGetGroundPoint(Input.mousePosition, out Vector3 point))
        {
            SetTargetPreviewVisible(false);
            return;
        }

        Color color = GetOrderColor(pendingOrder);
        if (targetMaterial != null)
            targetMaterial.color = color;
        UpdateCircle(targetPreview, point, pendingOrder == MajorOrder.AssembleHere ? 12f : 22f, 0.42f);
        targetPreview.enabled = true;
    }

    private static void UpdateCircle(LineRenderer line, Vector3 center, float radius, float heightOffset)
    {
        if (line == null)
            return;

        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + heightOffset;
            line.SetPosition(i, p);
        }
    }

    private void SetTargetPreviewVisible(bool value)
    {
        if (targetPreview != null)
            targetPreview.enabled = value;
    }

    private static Color GetOrderColor(MajorOrder order)
    {
        switch (order)
        {
            case MajorOrder.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    private static string GetOrderLabel(MajorOrder order)
    {
        switch (order)
        {
            case MajorOrder.AttackHere: return "ANGRIB HER";
            case MajorOrder.DefendHere: return "FORSVAR HER";
            case MajorOrder.WithdrawHere: return "TILBAGETRÆK HERTIL";
            case MajorOrder.AdvanceHere: return "RYK FREM HERTIL";
            case MajorOrder.HoldPosition: return "HOLD POSITION";
            case MajorOrder.AssembleHere: return "SAML HER";
            default: return "-";
        }
    }

    private Rect GetPanelRect()
    {
        float width = Mathf.Min(940f, Mathf.Max(720f, Screen.width - 90f));
        const float height = 174f;
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 34f, width, height);
    }

    private Rect GetTargetHintRect()
    {
        const float width = 430f;
        const float height = 30f;
        return new Rect((Screen.width - width) * 0.5f, 46f, width, height);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = PrototypeUiTheme09F15.Panel(10);
        headerStyle = PrototypeUiTheme09F15.Header(13);
        labelStyle = PrototypeUiTheme09F15.Label(10);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(9);
        buttonStyle = PrototypeUiTheme09F15.Button(10);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
    }

    private void OnGUI()
    {
        if (!selected || hqRoot == null)
            return;

        EnsureStyles();
        GUI.depth = -850;

        Rect panel = GetPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.Box(new Rect(panel.x + 8f, panel.y + 7f, panel.width - 16f, 27f), "BATALJONS HQ  |  MAJOR", headerStyle);
        GUI.Box(new Rect(panel.xMax - 182f, panel.y + 10f, 164f, 20f), "HQ AI: OFF  |  3 HESTE", accentStyle);

        float infoX = panel.x + 14f;
        float infoY = panel.y + 42f;
        float infoWidth = Mathf.Min(330f, panel.width * 0.36f);

        GUI.Label(new Rect(infoX, infoY, infoWidth, 20f), "UNDER KOMMANDO", PrototypeUiTheme09F15.Label(10, true));
        infoY += 22f;
        for (int i = 0; i < subordinates.Count; i++)
        {
            Regiment regiment = subordinates[i];
            if (regiment == null)
                continue;

            int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
            string mission = missions.TryGetValue(regiment, out UnitMission active)
                ? GetOrderLabel(active.Order)
                : "HOLD / lokal ordre";
            string line =
                (i + 1) + ". Kompagni   " + regiment.CurrentStrength + "/" + regiment.InitialStrength +
                "   tab " + losses + "   |   " + mission;
            GUI.Label(new Rect(infoX, infoY, infoWidth, 19f), line, labelStyle);
            infoY += 20f;
        }

        GUI.Label(new Rect(infoX, panel.yMax - 43f, infoWidth, 18f), "Aktuel: " + lastOrderText, mutedStyle);
        GUI.Label(new Rect(infoX, panel.yMax - 25f, infoWidth, 18f), "Gule linjer = direkte underordnede enheder", mutedStyle);

        float commandX = panel.x + infoWidth + 26f;
        float commandY = panel.y + 44f;
        float commandWidth = panel.xMax - commandX - 14f;
        const float gap = 7f;
        float buttonWidth = (commandWidth - gap * 2f) / 3f;
        const float buttonHeight = 44f;

        if (GUI.Button(new Rect(commandX, commandY, buttonWidth, buttonHeight), "ANGRIB HER", buttonStyle))
            pendingOrder = MajorOrder.AttackHere;
        if (GUI.Button(new Rect(commandX + buttonWidth + gap, commandY, buttonWidth, buttonHeight), "FORSVAR HER", buttonStyle))
            pendingOrder = MajorOrder.DefendHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, commandY, buttonWidth, buttonHeight), "TILBAGETRÆK HERTIL", buttonStyle))
            pendingOrder = MajorOrder.WithdrawHere;

        commandY += buttonHeight + gap;
        if (GUI.Button(new Rect(commandX, commandY, buttonWidth, buttonHeight), "RYK FREM HERTIL", buttonStyle))
            pendingOrder = MajorOrder.AdvanceHere;
        if (GUI.Button(new Rect(commandX + buttonWidth + gap, commandY, buttonWidth, buttonHeight), "HOLD POSITION", buttonStyle))
        {
            pendingOrder = MajorOrder.None;
            IssueHoldOrder();
        }
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, commandY, buttonWidth, buttonHeight), "SAML HER", buttonStyle))
            pendingOrder = MajorOrder.AssembleHere;

        if (pendingOrder != MajorOrder.None)
        {
            Rect hint = GetTargetHintRect();
            GUI.Box(hint, "MAJOR: " + GetOrderLabel(pendingOrder) + "  —  klik på terrænet for at vælge mål", headerStyle);
        }
    }
}
