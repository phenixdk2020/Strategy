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
    private GUIStyle redStyle;
    private Texture2D panelTexture;
    private Texture2D headerTexture;
    private Texture2D greenTexture;
    private Texture2D greenHoverTexture;
    private Texture2D redTexture;
    private Texture2D redHoverTexture;
    private Texture2D neutralTexture;
    private Texture2D neutralHoverTexture;

    public bool Installed => installed;
    public PrototypeCavalryUnit09F30 Gardehusar => gardehusar;
    public PrototypeCavalryUnit09F30 Dragon => dragon;
    public PrototypeCavalryUnit09F30 SelectedUnit => selected;
    public bool PendingCharge => pendingCharge;

    public void BeginChargePick()
    {
        if (selected != null && selected.Mode == PrototypeCavalryMode09F30.Mounted)
            pendingCharge = true;
    }

    public void CancelChargePick()
    {
        pendingCharge = false;
    }

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
            new Vector3(-520f, 0f, -245f));

        dragon = Spawn(
            "DRAGON ESKADRON",
            PrototypeCavalryKind09F30.Dragon,
            140,
            new Vector3(-520f, 0f, 245f));

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
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (higher != null && higher.SelectedLevel != PrototypeHigherCommandLevel09F30B.None)
            return true;

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
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        if (higher != null)
            higher.ClearSelectionOnly();

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
        if (PrototypeHigherCommandOob09F30D.IsPointerOverPanel(mouse))
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

        string kind = selected.Kind == PrototypeCavalryKind09F30.Gardehusar ? "GARDEHUSAR" : "DRAGON";
        GUI.Box(new Rect(5f, panel.y + 3f, panel.width - 10f, 17f),
            selected.UnitName + " | " + kind + " | KAVALERIKOMMANDO", headerStyle);

        float width = panel.width;
        float infoWidth = Mathf.Clamp(width * 0.22f, 235f, 305f);
        float aiWidth = Mathf.Clamp(width * 0.19f, 210f, 270f);
        float weaponWidth = Mathf.Clamp(width * 0.20f, 220f, 285f);
        float infoX = 6f;
        float aiX = infoX + infoWidth + 5f;
        float weaponX = aiX + aiWidth + 5f;
        float commandX = weaponX + weaponWidth + 5f;
        float commandWidth = width - commandX - 6f;
        float y = panel.y + 21f;

        GUI.Label(new Rect(infoX + 1f, y, infoWidth - 3f, 10f), "ENHEDSINFO", sectionStyle);
        int losses = Mathf.Max(0, selected.InitialStrength - selected.CurrentStrength);
        GUI.Label(new Rect(infoX + 3f, y + 10f, infoWidth - 6f, 13f),
            "Mænd " + selected.CurrentStrength + "/" + selected.InitialStrength +
            " | Tab " + losses + " | Moral " + selected.Morale.ToString("0"), valueStyle);
        GUI.Label(new Rect(infoX + 3f, y + 23f, infoWidth - 6f, 13f),
            "Coh " + selected.Cohesion.ToString("0") + " | " + selected.GetStatusLabel(), valueStyle);
        GUI.Label(new Rect(infoX + 3f, y + 36f, infoWidth - 6f, 13f),
            selected.IsBridgeRouteActive
                ? "BRO " + selected.BridgePhaseLabel + " | 2-ABREAST"
                : (selected.IsReforming
                    ? "REFORMER " + Mathf.RoundToInt(selected.FormationReadyFraction * 100f) + "%"
                    : "FORMATION KLAR"), valueStyle);
        GUI.Label(new Rect(infoX + 3f, y + 49f, infoWidth - 6f, 13f),
            "Højreklik jord = flyt | fjende = charge", valueStyle);

        PrototypeCavalryOfficerAI09F30C aiController = PrototypeCavalryOfficerAI09F30C.Instance;
        bool aiOn = aiController != null && aiController.IsAIEnabled(selected);
        string phase = aiController != null ? aiController.GetPhase(selected) : "—";
        string target = aiController != null ? aiController.GetTargetName(selected) : "—";
        string parent = PrototypeCavalryCommandControl09F30C.GetParent(selected);

        GUI.Label(new Rect(aiX, y, aiWidth, 10f), "AI / DOKTRIN", sectionStyle);
        if (GUI.Button(new Rect(aiX, y + 11f, aiWidth * 0.45f, 20f),
                aiOn ? "AI ON" : "AI OFF / MANUEL", aiOn ? activeStyle : redStyle))
        {
            if (aiController != null)
                aiController.SetAIEnabled(selected, !aiOn, "HUD_TOGGLE_F30I");
        }
        GUI.Label(new Rect(aiX, y + 35f, aiWidth, 12f), "Plan: FLANK / REAR → CHARGE", valueStyle);
        GUI.Label(new Rect(aiX, y + 47f, aiWidth, 12f), "Status: " + phase, valueStyle);
        GUI.Label(new Rect(aiX, y + 59f, aiWidth, 12f), "Mål: " + target + " | " + parent, valueStyle);

        GUI.Label(new Rect(weaponX, y, weaponWidth, 10f), "VÅBEN / TILSTAND", sectionStyle);
        GUI.Label(new Rect(weaponX, y + 11f, weaponWidth, 13f),
            "Sabel | Karabin | Pistol", valueStyle);
        GUI.Label(new Rect(weaponX, y + 24f, weaponWidth, 13f),
            selected.Mode == PrototypeCavalryMode09F30.Mounted ? "MOUNTED" : "AFSIDDET", valueStyle);
        GUI.Label(new Rect(weaponX, y + 37f, weaponWidth, 13f),
            selected.Formation == PrototypeCavalryFormation09F30.Line
                ? "4-GELED LINJE"
                : (selected.IsBridgeRouteActive ? "BROKOLONNE 2" : "MARCHKOLONNE 4"), valueStyle);
        GUI.Label(new Rect(weaponX, y + 50f, weaponWidth, 13f),
            "Mounted fire: ikke implementeret endnu", valueStyle);

        GUI.Label(new Rect(commandX, y, commandWidth, 10f), "ORDRER / BEVÆGELSE", sectionStyle);
        const float gap = 3f;
        float actionY = y + 11f;
        float actionW = (commandWidth - gap * 2f) / 3f;

        if (GUI.Button(new Rect(commandX, actionY, actionW, 20f),
                pendingCharge ? "VÆLG MÅL" : "CHARGE", pendingCharge ? activeStyle : redStyle))
        {
            if (selected.Mode == PrototypeCavalryMode09F30.Mounted)
            {
                SetSelectedManual("HUD_CHARGE");
                pendingCharge = !pendingCharge;
            }
        }
        if (GUI.Button(new Rect(commandX + actionW + gap, actionY, actionW, 20f),
                "STOP / HOLD", selected.Action == PrototypeCavalryAction09F30.Hold ? activeStyle : redStyle))
        {
            SetSelectedManual("HUD_STOP_HOLD");
            selected.OrderHold();
            pendingCharge = false;
        }

        string modeLabel = selected.Kind == PrototypeCavalryKind09F30.Dragon
            ? (selected.Mode == PrototypeCavalryMode09F30.Mounted ? "SID AF" : "STIG OP")
            : "MOUNTED";
        if (GUI.Button(new Rect(commandX + (actionW + gap) * 2f, actionY, actionW, 20f),
                modeLabel, selected.Kind == PrototypeCavalryKind09F30.Dragon ? neutralStyle : activeStyle))
        {
            if (selected.Kind == PrototypeCavalryKind09F30.Dragon)
            {
                SetSelectedManual("HUD_MOUNT_MODE");
                pendingCharge = false;
                if (selected.Mode == PrototypeCavalryMode09F30.Mounted) selected.Dismount();
                else selected.Remount();
            }
        }

        GUI.Label(new Rect(commandX, y + 35f, commandWidth, 10f), "FORMATION", sectionStyle);
        float formationY = y + 46f;
        float formationW = (commandWidth - gap) / 2f;
        if (GUI.Button(new Rect(commandX, formationY, formationW, 20f), "4-GELED LINJE",
                selected.Formation == PrototypeCavalryFormation09F30.Line && !selected.IsBridgeRouteActive ? activeStyle : redStyle))
        {
            SetSelectedManual("HUD_FORMATION_LINE");
            selected.SetFormation(PrototypeCavalryFormation09F30.Line);
        }
        if (GUI.Button(new Rect(commandX + formationW + gap, formationY, formationW, 20f), "MARCHKOLONNE",
                selected.Formation == PrototypeCavalryFormation09F30.Column && !selected.IsBridgeRouteActive ? activeStyle : redStyle))
        {
            SetSelectedManual("HUD_FORMATION_COLUMN");
            selected.SetFormation(PrototypeCavalryFormation09F30.Column);
        }

        Event current = Event.current;
        if (current != null && panel.Contains(current.mousePosition) &&
            (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
             current.type == EventType.MouseDrag || current.type == EventType.ScrollWheel))
            current.Use();
    }

    private void SetSelectedManual(string reason)
    {
        PrototypeCavalryOfficerAI09F30C ai = PrototypeCavalryOfficerAI09F30C.Instance;
        if (selected != null && ai != null && ai.IsAIEnabled(selected))
            ai.SetAIEnabled(selected, false, reason);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.055f, 0.065f, 0.055f, 0.995f), "CAVHUD30H_PANEL");
        headerTexture = MakeTexture(new Color(0.13f, 0.16f, 0.11f, 1f), "CAVHUD30H_HEADER");
        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "CAVHUD30H_GREEN");
        greenHoverTexture = MakeTexture(new Color(0.22f, 0.56f, 0.25f, 1f), "CAVHUD30H_GREEN_HOVER");
        redTexture = MakeTexture(new Color(0.43f, 0.14f, 0.12f, 1f), "CAVHUD30H_RED");
        redHoverTexture = MakeTexture(new Color(0.57f, 0.19f, 0.16f, 1f), "CAVHUD30H_RED_HOVER");
        neutralTexture = MakeTexture(new Color(0.20f, 0.22f, 0.17f, 1f), "CAVHUD30H_NEUTRAL");
        neutralHoverTexture = MakeTexture(new Color(0.30f, 0.33f, 0.24f, 1f), "CAVHUD30H_NEUTRAL_HOVER");

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

        valueStyle = new GUIStyle(labelStyle);
        valueStyle.fontSize = 8;
        valueStyle.clipping = TextClipping.Clip;

        activeStyle = MakeButtonStyle(greenTexture, greenHoverTexture);
        redStyle = MakeButtonStyle(redTexture, redHoverTexture);
        neutralStyle = MakeButtonStyle(neutralTexture, neutralHoverTexture);
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
