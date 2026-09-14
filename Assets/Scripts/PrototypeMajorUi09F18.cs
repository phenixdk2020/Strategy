using UnityEngine;

// v00.00.09f18 selection/input retained.
// v00.00.09f22: full-width low-profile bottom HUD shared visually with company HUD.
[DefaultExecutionOrder(440)]
public sealed class PrototypeMajorUi09F18 : MonoBehaviour
{
    public static PrototypeMajorUi09F18 Instance { get; private set; }

    private Camera cam;
    private MajorOrder09F18 pendingOrder = MajorOrder09F18.None;
    private bool mouseTracked;
    private bool dragging;
    private Vector2 dragStart;
    private Vector2 dragCurrent;
    private LineRenderer selectionRing;
    private LineRenderer targetRing;
    private Material selectionMaterial;
    private Material targetMaterial;
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;
    private GUIStyle hoverStyle;
    private const float DragThreshold = 9f;
    private const float HudHeight = 90f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorUi09F18>() == null)
            new GameObject("PrototypeMajorUi_v000009f18").AddComponent<PrototypeMajorUi09F18>();
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

        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed || cam == null)
            return;

        EnsureWorldVisuals();
        UpdateSelectionRing(major);
        UpdateTargetRing(major);
        HandleWorldInput(major);
    }

    private void HandleWorldInput(PrototypeMajorBattalion09F18 major)
    {
        bool overPanel = IsPointerOverControls(Input.mousePosition);

        if (major.Selected && pendingOrder != MajorOrder09F18.None && Input.GetMouseButtonDown(0) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
            {
                major.IssueOrder(pendingOrder, point);
                pendingOrder = MajorOrder09F18.None;
                targetRing.enabled = false;
            }
            mouseTracked = false;
            dragging = false;
            return;
        }

        if (major.Selected && pendingOrder == MajorOrder09F18.None && Input.GetMouseButtonDown(1) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
                major.MoveMajor(point);
        }

        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            mouseTracked = true;
            dragging = false;
            dragStart = Input.mousePosition;
            dragCurrent = dragStart;
        }

        if (mouseTracked && Input.GetMouseButton(0))
        {
            dragCurrent = Input.mousePosition;
            if (!dragging && Vector2.Distance(dragStart, dragCurrent) >= DragThreshold)
                dragging = true;
        }

        if (!mouseTracked || !Input.GetMouseButtonUp(0))
            return;

        dragCurrent = Input.mousePosition;
        if (dragging)
        {
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(dragStart.x, dragCurrent.x),
                Mathf.Min(dragStart.y, dragCurrent.y),
                Mathf.Max(dragStart.x, dragCurrent.x),
                Mathf.Max(dragStart.y, dragCurrent.y));

            Vector3 screen = cam.WorldToScreenPoint(major.HqRoot.transform.position);
            if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y), true))
                major.SetSelected(true);
        }
        else
        {
            if (RayHitsHQ(major, Input.mousePosition))
                major.SetSelected(true);
            else if (major.Selected && pendingOrder == MajorOrder09F18.None)
                major.SetSelected(false);
        }

        mouseTracked = false;
        dragging = false;
    }

    private bool RayHitsHQ(PrototypeMajorBattalion09F18 major, Vector3 mousePosition)
    {
        if (major.HqRoot == null || cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            Transform t = hit.collider.transform;
            if (t == major.HqRoot.transform || t.IsChildOf(major.HqRoot.transform))
                return true;
        }
        return false;
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = default;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.1f;
            return true;
        }
        return false;
    }

    private void EnsureWorldVisuals()
    {
        if (selectionRing != null && targetRing != null)
            return;

        selectionMaterial = CreateMaterial(new Color(1f, 0.80f, 0.18f, 0.96f), "HQ18Selection");
        targetMaterial = CreateMaterial(new Color(0.92f, 0.72f, 0.18f, 0.96f), "HQ18Target");
        selectionRing = CreateRing("MajorSelectionRing09F18", 0.25f, selectionMaterial);
        targetRing = CreateRing("MajorTargetRing09F18", 0.22f, targetMaterial);
    }

    private LineRenderer CreateRing(string name, float width, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = width;
        line.positionCount = 48;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private void UpdateSelectionRing(PrototypeMajorBattalion09F18 major)
    {
        selectionRing.enabled = major.Selected;
        if (major.Selected && major.HqRoot != null)
            DrawCircle(selectionRing, major.HqRoot.transform.position, 6.5f, 0.30f);
    }

    private void UpdateTargetRing(PrototypeMajorBattalion09F18 major)
    {
        if (!major.Selected || pendingOrder == MajorOrder09F18.None)
        {
            targetRing.enabled = false;
            return;
        }

        if (TryGetGround(Input.mousePosition, out Vector3 point))
        {
            targetMaterial.color = ColorFor(pendingOrder);
            DrawCircle(targetRing, point, pendingOrder == MajorOrder09F18.AssembleHere ? 12f : 22f, 0.42f);
            targetRing.enabled = true;
        }
    }

    private static void DrawCircle(LineRenderer line, Vector3 center, float radius, float yOffset)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static Color ColorFor(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder09F18.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder09F18.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Selected)
            return false;
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return PanelRect().Contains(gui);
    }

    private Rect PanelRect()
    {
        return new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
        hoverStyle = PrototypeUiTheme09F15.Panel(9);
        hoverStyle.alignment = TextAnchor.UpperLeft;
    }

    private void OnGUI()
    {
        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed)
            return;

        EnsureStyles();

        if (!major.Selected && cam != null && RayHitsHQ(major, Input.mousePosition))
        {
            Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Rect hover = new Rect(
                Mathf.Clamp(p.x + 15f, 8f, Screen.width - 270f),
                Mathf.Clamp(p.y - 20f, 38f, Screen.height - 88f),
                262f,
                78f);
            GUI.Box(hover,
                "MAJOR | BATALJONS HQ\n" +
                "AI: " + (major.AIEnabled ? "ON" : "OFF") + " | " + major.Doctrine + " | 3 heste | 4 kompagnier\n" +
                major.LastOrderText + "\n" + major.LastDecisionText,
                hoverStyle);
        }

        if (!major.Selected)
            return;

        GUI.depth = -870;
        Rect panel = PanelRect();
        GUI.Box(panel, string.Empty, panelStyle);

        float pad = 7f;
        float headerY = panel.y + 3f;
        GUI.Box(new Rect(pad, headerY, panel.width - pad * 2f, 19f),
            "BATALJONS HQ | MAJOR | 4 KOMPAGNIER", headerStyle);

        float aiX = panel.xMax - 274f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 70f, 17f), major.AIEnabled ? "AI ON" : "AI OFF", major.AIEnabled ? accentStyle : buttonStyle))
            major.ToggleAI();
        aiX += 73f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), major.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF", buttonStyle)) major.SetDoctrine(OfficerAIDoctrine.Defensive);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), major.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL", buttonStyle)) major.SetDoctrine(OfficerAIDoctrine.Balanced);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f), major.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF", buttonStyle)) major.SetDoctrine(OfficerAIDoctrine.Offensive);

        float infoWidth = Mathf.Clamp(panel.width * 0.31f, 360f, 520f);
        float rowY = panel.y + 27f;
        GUI.Label(new Rect(pad + 3f, rowY, infoWidth - 8f, 15f), major.LastOrderText, labelStyle);
        GUI.Label(new Rect(pad + 3f, rowY + 16f, infoWidth - 8f, 15f), major.LastDecisionText, mutedStyle);

        float commandX = infoWidth + 8f;
        float commandWidth = panel.width - commandX - 8f;
        const float gap = 4f;
        float buttonWidth = (commandWidth - gap * 5f) / 6f;
        const float commandHeight = 27f;

        if (GUI.Button(new Rect(commandX, rowY, buttonWidth, commandHeight), "ANGRIB HER", buttonStyle)) pendingOrder = MajorOrder09F18.AttackHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap), rowY, buttonWidth, commandHeight), "FORSVAR HER", buttonStyle)) pendingOrder = MajorOrder09F18.DefendHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, rowY, buttonWidth, commandHeight), "TILBAGETRÆK", buttonStyle)) pendingOrder = MajorOrder09F18.WithdrawHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 3f, rowY, buttonWidth, commandHeight), "RYK FREM", buttonStyle)) pendingOrder = MajorOrder09F18.AdvanceHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 4f, rowY, buttonWidth, commandHeight), "HOLD", buttonStyle)) major.IssueOrder(MajorOrder09F18.HoldPosition, major.HqRoot.transform.position);
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 5f, rowY, buttonWidth, commandHeight), "SAML HER", buttonStyle)) pendingOrder = MajorOrder09F18.AssembleHere;

        float companyY = panel.y + 60f;
        float companyWidth = (panel.width - pad * 2f - 9f) / 4f;
        for (int i = 0; i < major.Companies.Count && i < 4; i++)
        {
            Regiment regiment = major.Companies[i];
            if (regiment == null)
                continue;
            int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
            string text = PrototypeMajorBattalion09F18.Name(regiment) + " " + regiment.CurrentStrength + "/" + regiment.InitialStrength +
                          " | tab " + losses + " | " + major.Role(regiment);
            GUI.Label(new Rect(pad + i * (companyWidth + 3f), companyY, companyWidth, 18f), text, labelStyle);
        }
    }
}
