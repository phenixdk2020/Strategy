using UnityEngine;

// v00.00.09f30
// TEST-only controller for the first playable Gardehusar/Dragon cavalry core.
[DefaultExecutionOrder(41000)]
public sealed class PrototypeCavalryManager09F30 : MonoBehaviour
{
    public static PrototypeCavalryManager09F30 Instance { get; private set; }

    private const float PanelX = 310f;
    private const float PanelY = 39f;
    private const float PanelWidth = 340f;
    private const float HeaderHeight = 25f;

    private PrototypeCavalryUnit09F30 gardehusar;
    private PrototypeCavalryUnit09F30 dragon;
    private PrototypeCavalryUnit09F30 selected;
    private Camera cam;
    private bool installed;
    private bool open;
    private bool pendingMove;
    private bool pendingCharge;
    private float installRetry;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeStyle;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryManager09F30>() == null)
            new GameObject("PrototypeCavalryManager_v000009f30")
                .AddComponent<PrototypeCavalryManager09F30>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        open = false;
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

        if (!installed)
        {
            if (Time.unscaledTime >= installRetry)
            {
                installRetry = Time.unscaledTime + 0.5f;
                TryInstall();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.F10))
            open = !open;

        HandlePendingWorldOrder();
        HandleWorldSelection();
    }

    private void TryInstall()
    {
        BattleManager battle = BattleManager.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (battle == null || hierarchy == null || !hierarchy.Installed)
            return;

        gardehusar = Spawn(
            "GARDEHUSAR ESKADRON",
            PrototypeCavalryKind09F30.Gardehusar,
            120,
            new Vector3(-560f, 0f, -330f));

        dragon = Spawn(
            "DRAGON ESKADRON",
            PrototypeCavalryKind09F30.Dragon,
            140,
            new Vector3(-560f, 0f, 330f));

        installed = gardehusar != null && dragon != null;
        if (!installed)
            return;

        Select(gardehusar);
        Debug.Log(
            "CAVALRY-09F30|Installed=True|Units=2|Gardehusar=120|Dragon=140|" +
            "SharedCore=True|MountedLineColumn=True|Charge=True|DragonDismount=True|" +
            "BridgeOnlyCrossing=True|Panel=F10");
    }

    private PrototypeCavalryUnit09F30 Spawn(
        string label,
        PrototypeCavalryKind09F30 kind,
        int strength,
        Vector3 position)
    {
        GameObject root = new GameObject(label);
        PrototypeCavalryUnit09F30 unit = root.AddComponent<PrototypeCavalryUnit09F30>();
        unit.Initialize(label, kind, strength, position);
        return unit;
    }

    private void HandlePendingWorldOrder()
    {
        if (selected == null || cam == null)
            return;
        if (!Input.GetMouseButtonDown(0) || IsPointerOverPanel(Input.mousePosition))
            return;

        if (pendingCharge)
        {
            Regiment target = RaycastRegiment(Input.mousePosition);
            if (target != null && target.Team == BattleTeam.Prussia)
            {
                selected.OrderCharge(target);
                pendingCharge = false;
                pendingMove = false;
            }
            return;
        }

        if (pendingMove)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
            {
                selected.OrderMove(point);
                pendingMove = false;
                pendingCharge = false;
            }
        }
    }

    private void HandleWorldSelection()
    {
        if (pendingMove || pendingCharge || cam == null)
            return;
        if (!Input.GetMouseButtonDown(0) || IsPointerOverPanel(Input.mousePosition))
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 5000f))
            return;

        PrototypeCavalryUnit09F30 unit = hit.collider != null
            ? hit.collider.GetComponentInParent<PrototypeCavalryUnit09F30>()
            : null;
        if (unit != null)
            Select(unit);
    }

    private void Select(PrototypeCavalryUnit09F30 unit)
    {
        if (gardehusar != null)
            gardehusar.SetSelected(unit == gardehusar);
        if (dragon != null)
            dragon.SetSelected(unit == dragon);
        selected = unit;
        pendingMove = false;
        pendingCharge = false;
    }

    private bool TryGetGround(Vector3 mouse, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mouse);
        if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
        {
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return true;
    }

    private Regiment RaycastRegiment(Vector3 mouse)
    {
        if (cam == null)
            return null;

        Ray ray = cam.ScreenPointToRay(mouse);
        if (!Physics.Raycast(ray, out RaycastHit hit, 5000f))
            return null;
        return hit.collider != null ? hit.collider.GetComponentInParent<Regiment>() : null;
    }

    private void OnGUI()
    {
        if (!installed)
            return;

        EnsureStyles();
        GUI.depth = -125500;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);

        Rect header = new Rect(panel.x, panel.y, panel.width, HeaderHeight);
        GUI.Box(header, "CAVALRY TEST [F30]", headerStyle);
        if (GUI.Button(new Rect(header.xMax - 29f, header.y + 2f, 24f, 21f), open ? "−" : "+", buttonStyle))
            open = !open;

        if (!open)
            return;

        float y = header.yMax + 4f;
        DrawUnitRow(gardehusar, new Rect(panel.x + 5f, y, panel.width - 10f, 23f));
        y += 26f;
        DrawUnitRow(dragon, new Rect(panel.x + 5f, y, panel.width - 10f, 23f));
        y += 28f;

        if (selected == null)
            return;

        GUI.Label(
            new Rect(panel.x + 7f, y, panel.width - 14f, 18f),
            selected.UnitName + " | " + selected.GetStatusLabel() +
            " | M" + selected.Morale.ToString("0") +
            " C" + selected.Cohesion.ToString("0"),
            labelStyle);
        y += 21f;

        float gap = 4f;
        float w = (panel.width - 10f - gap * 2f) / 3f;
        if (GUI.Button(new Rect(panel.x + 5f, y, w, 22f), pendingMove ? "VÆLG PUNKT" : "FLYT", pendingMove ? activeStyle : buttonStyle))
        {
            pendingMove = true;
            pendingCharge = false;
        }
        if (GUI.Button(new Rect(panel.x + 5f + w + gap, y, w, 22f), pendingCharge ? "VÆLG MÅL" : "CHARGE", pendingCharge ? activeStyle : buttonStyle))
        {
            pendingCharge = selected.Mode == PrototypeCavalryMode09F30.Mounted;
            pendingMove = false;
        }
        if (GUI.Button(new Rect(panel.x + 5f + (w + gap) * 2f, y, w, 22f), "STOP", buttonStyle))
        {
            selected.OrderHold();
            pendingMove = false;
            pendingCharge = false;
        }
        y += 26f;

        if (GUI.Button(new Rect(panel.x + 5f, y, w, 22f), "LINJE",
                selected.Formation == PrototypeCavalryFormation09F30.Line ? activeStyle : buttonStyle))
            selected.SetFormation(PrototypeCavalryFormation09F30.Line);

        if (GUI.Button(new Rect(panel.x + 5f + w + gap, y, w, 22f), "KOLONNE",
                selected.Formation == PrototypeCavalryFormation09F30.Column ? activeStyle : buttonStyle))
            selected.SetFormation(PrototypeCavalryFormation09F30.Column);

        if (selected.Kind == PrototypeCavalryKind09F30.Dragon)
        {
            string modeLabel = selected.Mode == PrototypeCavalryMode09F30.Mounted ? "SID AF" : "STIG OP";
            if (GUI.Button(new Rect(panel.x + 5f + (w + gap) * 2f, y, w, 22f), modeLabel, buttonStyle))
            {
                if (selected.Mode == PrototypeCavalryMode09F30.Mounted)
                    selected.Dismount();
                else
                    selected.Remount();
            }
        }

        y += 27f;
        GUI.Label(
            new Rect(panel.x + 7f, y, panel.width - 14f, 17f),
            pendingCharge ? "CHARGE: klik et preussisk infanterikompagni" :
            pendingMove ? "FLYT: klik på terrænet" :
            "F10 skjuler/viser panelet. World-click på cavalry vælger enheden.",
            labelStyle);
    }

    private void DrawUnitRow(PrototypeCavalryUnit09F30 unit, Rect rect)
    {
        if (unit == null)
            return;

        string text = unit.UnitName + "   " + unit.CurrentStrength + "/" + unit.InitialStrength +
                      "   " + unit.GetStatusLabel();
        if (GUI.Button(rect, text, selected == unit ? activeStyle : buttonStyle))
            Select(unit);
    }

    private Rect GetPanelRect()
    {
        float height = open ? 164f : HeaderHeight;
        return new Rect(PanelX, PanelY, PanelWidth, height);
    }

    private bool IsPointerOverPanel(Vector3 mouse)
    {
        Vector2 gui = new Vector2(mouse.x, Screen.height - mouse.y);
        return GetPanelRect().Contains(gui);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(10);
        buttonStyle = PrototypeUiTheme09F15.Button(8);
        activeStyle = PrototypeUiTheme09F15.AccentBox(8);
        labelStyle = PrototypeUiTheme09F15.Label(8);
    }
}
