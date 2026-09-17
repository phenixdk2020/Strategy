using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30a
// Shared cavalry runtime/selection manager.
// F30 originally exposed a temporary F10 CAVALRY TEST panel. F30A removes that parallel
// control path: cavalry is selected in the world/OOB and controlled from the normal
// bottom command HUD / right-click battlefield flow like infantry company-scale units.
[DefaultExecutionOrder(41000)]
public sealed class PrototypeCavalryManager09F30 : MonoBehaviour
{
    public static PrototypeCavalryManager09F30 Instance { get; private set; }

    private const float HudHeight = 90f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private PrototypeCavalryUnit09F30 gardehusar;
    private PrototypeCavalryUnit09F30 dragon;
    private PrototypeCavalryUnit09F30 selected;
    private Camera cam;
    private bool installed;
    private bool pendingCharge;
    private bool orderConsumedThisFrame;
    private float installRetry;

    private FieldInfo playerSelectedField;
    private FieldInfo selectedBattalionField;
    private MethodInfo setRegimentalSelectedMethod;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle activeStyle;
    private GUIStyle neutralStyle;

    public bool Installed => installed;
    public PrototypeCavalryUnit09F30 Gardehusar => gardehusar;
    public PrototypeCavalryUnit09F30 Dragon => dragon;
    public PrototypeCavalryUnit09F30 SelectedUnit => selected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryManager09F30>() == null)
            new GameObject("PrototypeCavalryManager_v000009f30a")
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
        playerSelectedField = typeof(PlayerCommander).GetField("selected", PrivateInstance);
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod("SetSelected", PrivateInstance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        orderConsumedThisFrame = false;

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

        // Selection from the normal infantry/HQ OOB is authoritative. If the user
        // changes command level there, cavalry relinquishes its selection on the next
        // update instead of leaving two bottom HUDs active.
        if (selected != null && HasOtherCommandSelection())
            ClearSelection();

        HandlePendingChargePick();
        HandleWorldSelection();
        HandleRightClickOrder();
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

        // Do not auto-select cavalry. It now behaves like other tactical units: the player
        // selects it deliberately on the battlefield or in OOB.
        ClearSelection();

        Debug.Log(
            "CAVALRY-09F30A|Installed=True|Units=2|Gardehusar=120|Dragon=140|" +
            "SharedCore=True|WorldSelection=True|OOBSelection=True|BottomHud=True|" +
            "RightClickMove=True|RightClickEnemyCharge=True|F10Panel=False");
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

    public void SelectUnit(PrototypeCavalryUnit09F30 unit)
    {
        if (unit != gardehusar && unit != dragon)
            return;

        ClearInfantryAndHqSelection();

        if (gardehusar != null)
            gardehusar.SetSelected(unit == gardehusar);
        if (dragon != null)
            dragon.SetSelected(unit == dragon);

        selected = unit;
        pendingCharge = false;

        Debug.Log("CAVALRY-09F30A|Select=True|Unit=" + unit.UnitName + "|CameraMoved=False");
    }

    public void ClearSelection()
    {
        if (gardehusar != null)
            gardehusar.SetSelected(false);
        if (dragon != null)
            dragon.SetSelected(false);
        selected = null;
        pendingCharge = false;
    }

    private void HandleWorldSelection()
    {
        if (orderConsumedThisFrame || cam == null || !Input.GetMouseButtonDown(0))
            return;
        if (pendingCharge)
            return;
        if (IsPointerOverUi(Input.mousePosition))
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        PrototypeCavalryUnit09F30 clicked = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f) && hit.collider != null)
            clicked = hit.collider.GetComponentInParent<PrototypeCavalryUnit09F30>();

        if (clicked != null)
        {
            SelectUnit(clicked);
            return;
        }

        // Clicking a different battlefield entity/empty ground relinquishes cavalry
        // selection exactly as company selection does.
        if (selected != null)
            ClearSelection();
    }

    private void HandlePendingChargePick()
    {
        if (!pendingCharge || selected == null || cam == null)
            return;
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUi(Input.mousePosition))
            return;

        // Consume this world click even when it was not a valid enemy. Otherwise the
        // same mouse-down would immediately fall through to normal selection and clear
        // the cavalry unit whose CHARGE target is being picked.
        orderConsumedThisFrame = true;

        Regiment target = RaycastRegiment(Input.mousePosition);
        if (target != null && target.Team == BattleTeam.Prussia &&
            selected.Mode == PrototypeCavalryMode09F30.Mounted)
        {
            selected.OrderCharge(target);
            pendingCharge = false;
            Debug.Log("CAVALRY-09F30A|Order=CHARGE|Unit=" + selected.UnitName + "|Target=" + target.RegimentName);
        }
    }

    private void HandleRightClickOrder()
    {
        if (selected == null || cam == null || pendingCharge)
            return;
        if (!Input.GetMouseButtonDown(1) || IsPointerOverUi(Input.mousePosition))
            return;

        Regiment target = RaycastRegiment(Input.mousePosition);
        if (target != null && target.Team == BattleTeam.Prussia)
        {
            if (selected.Mode == PrototypeCavalryMode09F30.Mounted)
            {
                selected.OrderCharge(target);
                Debug.Log("CAVALRY-09F30A|RightClick=ENEMY|Order=CHARGE|Unit=" + selected.UnitName +
                          "|Target=" + target.RegimentName);
            }
            else
            {
                Debug.Log("CAVALRY-09F30A|RightClick=ENEMY|Order=None|Reason=DismountedFireNotImplemented|Unit=" +
                          selected.UnitName);
            }
            return;
        }

        if (TryGetGround(Input.mousePosition, out Vector3 point))
        {
            selected.OrderMove(point);
            Debug.Log("CAVALRY-09F30A|RightClick=GROUND|Order=MOVE|Unit=" + selected.UnitName +
                      "|Goal=" + point.x.ToString("0.0") + "," + point.z.ToString("0.0"));
        }
    }

    private bool HasOtherCommandSelection()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Selected)
            return true;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && selectedBattalionField != null)
        {
            object value = selectedBattalionField.GetValue(hierarchy);
            if (value is int && (int)value >= 0)
                return true;
        }

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> list = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (list != null && list.Count > 0)
                return true;
        }

        return false;
    }

    private void ClearInfantryAndHqSelection()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Selected && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { false });

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> list = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        list[i].SetSelected(false);
                list.Clear();
            }
        }
    }

    private bool TryGetGround(Vector3 mouse, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mouse);
        if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
        {
            // Do not use a unit collider as a movement endpoint when a ground click was intended.
            if (hit.collider != null)
            {
                PrototypeCavalryUnit09F30 cavalryHit = hit.collider.GetComponentInParent<PrototypeCavalryUnit09F30>();
                Regiment regimentHit = hit.collider.GetComponentInParent<Regiment>();
                if (cavalryHit != null || regimentHit != null)
                {
                    Plane fallback = new Plane(Vector3.up, Vector3.zero);
                    if (fallback.Raycast(ray, out float d))
                    {
                        point = ray.GetPoint(d);
                        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
                        return true;
                    }
                }
            }

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

    private bool IsPointerOverUi(Vector3 mouse)
    {
        if (mouse.y <= HudHeight)
            return true;
        if (PrototypeOobNavigator09F29Q.IsPointerOverPanel(mouse))
            return true;
        if (PrototypeCavalryOob09F30A.IsPointerOverPanel(mouse))
            return true;
        return BattleManager.Instance != null &&
               BattleManager.Instance.IsPointerOverSimulationControls(mouse);
    }

    private void OnGUI()
    {
        if (!installed || selected == null)
            return;

        EnsureStyles();
        GUI.depth = -7000;

        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        string title = selected.UnitName + " | " +
                       (selected.Kind == PrototypeCavalryKind09F30.Gardehusar ? "GARDEHUSAR" : "DRAGON") +
                       " | KAVALERIKOMMANDO";
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f), title, headerStyle);

        float infoWidth = Mathf.Clamp(panel.width * 0.34f, 340f, 470f);
        float commandX = infoWidth + 12f;
        float commandWidth = panel.width - commandX - 8f;
        float y = panel.y + 22f;

        GUI.Label(new Rect(8f, y, infoWidth - 12f, 11f), "ENHEDSINFO", sectionStyle);

        int losses = Mathf.Max(0, selected.InitialStrength - selected.CurrentStrength);
        GUI.Label(new Rect(10f, y + 12f, infoWidth - 16f, 14f),
            "Mænd " + selected.CurrentStrength + "/" + selected.InitialStrength +
            " | Tab " + losses +
            " | Moral " + selected.Morale.ToString("0") +
            " | Coh " + selected.Cohesion.ToString("0"), valueStyle);
        GUI.Label(new Rect(10f, y + 26f, infoWidth - 16f, 14f),
            selected.GetStatusLabel() + " | Sabel / Karabin / Pistol", valueStyle);
        GUI.Label(new Rect(10f, y + 40f, infoWidth - 16f, 14f),
            "Højreklik jord = flyt | Højreklik fjende = " +
            (selected.Mode == PrototypeCavalryMode09F30.Mounted ? "charge" : "angreb afventer dismounted fire"), valueStyle);

        GUI.Label(new Rect(commandX, y, commandWidth, 11f), "ORDRER / FORMATION", sectionStyle);

        const float gap = 5f;
        float buttonW = (commandWidth - gap * 5f) / 6f;
        float row1 = y + 13f;
        float row2 = y + 39f;

        if (GUI.Button(new Rect(commandX, row1, buttonW, 22f),
                pendingCharge ? "VÆLG MÅL" : "CHARGE",
                pendingCharge ? activeStyle : neutralStyle))
        {
            if (selected.Mode == PrototypeCavalryMode09F30.Mounted)
                pendingCharge = !pendingCharge;
        }

        if (GUI.Button(new Rect(commandX + (buttonW + gap), row1, buttonW, 22f),
                "STOP / HOLD", selected.Action == PrototypeCavalryAction09F30.Hold ? activeStyle : neutralStyle))
        {
            selected.OrderHold();
            pendingCharge = false;
        }

        if (GUI.Button(new Rect(commandX + (buttonW + gap) * 2f, row1, buttonW, 22f),
                "LINJE", selected.Formation == PrototypeCavalryFormation09F30.Line ? activeStyle : neutralStyle))
            selected.SetFormation(PrototypeCavalryFormation09F30.Line);

        if (GUI.Button(new Rect(commandX + (buttonW + gap) * 3f, row1, buttonW, 22f),
                "KOLONNE", selected.Formation == PrototypeCavalryFormation09F30.Column ? activeStyle : neutralStyle))
            selected.SetFormation(PrototypeCavalryFormation09F30.Column);

        if (selected.Kind == PrototypeCavalryKind09F30.Dragon)
        {
            string modeLabel = selected.Mode == PrototypeCavalryMode09F30.Mounted ? "SID AF" : "STIG OP";
            if (GUI.Button(new Rect(commandX + (buttonW + gap) * 4f, row1, buttonW, 22f), modeLabel, neutralStyle))
            {
                pendingCharge = false;
                if (selected.Mode == PrototypeCavalryMode09F30.Mounted)
                    selected.Dismount();
                else
                    selected.Remount();
            }
        }

        GUI.Label(new Rect(commandX, row2 + 2f, commandWidth, 18f),
            "AI: endnu ikke implementeret i F30 | Mounted firearms/dismounted fire kommer i næste cavalry-pass",
            labelStyle);

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown ||
             current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag ||
             current.type == EventType.ScrollWheel))
        {
            current.Use();
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(10);
        sectionStyle = PrototypeUiTheme09F15.Section(8);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        valueStyle = PrototypeUiTheme09F15.Label(8);
        activeStyle = PrototypeUiTheme09F15.AccentBox(8);
        neutralStyle = PrototypeUiTheme09F15.Button(8);
    }
}
