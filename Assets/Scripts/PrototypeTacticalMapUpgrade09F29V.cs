using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29v
// Replaces the moving unit-bounds minimap with a fixed whole-battlefield overview.
// The map itself no longer pans/scales as units move; instead a camera viewport polygon
// moves across the map. HQs are explicit II / III markers. Interaction follows OOB:
// single-click friendly = select only, double-click = select + camera behind,
// empty/enemy click = move camera to that map location.
[DefaultExecutionOrder(40100)]
public sealed class PrototypeTacticalMapUpgrade09F29V : MonoBehaviour
{
    private enum EntityType { None, Company, Major, RegimentHq }

    private struct Hit
    {
        public EntityType Type;
        public Regiment Company;
        public int MajorIndex;
        public Transform Target;
        public string Key;
        public float Distance;
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float PanelWidth = 270f;
    private const float PanelHeight = 188f;
    private const float BottomGuard = 104f;
    private const float DoubleClickSeconds = 0.34f;

    private Camera cam;
    private bool mapVisible = true;
    private bool oldUiDisabled;
    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

    private FieldInfo playerSelectedField;
    private MethodInfo selectMajorMethod;
    private MethodInfo setRegimentalSelectedMethod;
    private FieldInfo selectedBattalionField;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hqStyle;
    private Texture2D panelTexture;
    private Texture2D whiteTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalMapUpgrade09F29V>() == null)
            new GameObject("PrototypeTacticalMapUpgrade_v000009f29v").AddComponent<PrototypeTacticalMapUpgrade09F29V>();
    }

    private void Awake()
    {
        playerSelectedField = typeof(PlayerCommander).GetField("selected", PrivateInstance);
        selectMajorMethod = typeof(PrototypeRegimentHierarchy09F27).GetMethod("SelectMajor", PrivateInstance);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28).GetMethod("SetSelected", PrivateInstance);
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        Debug.Log("TACTICAL-MAP-09F29V|Installed=True|Bounds=WHOLE_BATTLEFIELD|CameraViewport=True|HQMarkers=II-III|SingleClick=SelectOnly|DoubleClick=Behind");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        DisableOldMapLayers();

        if (Input.GetKeyDown(KeyCode.M))
            mapVisible = !mapVisible;

        if (cam == null || !mapVisible || !Input.GetMouseButtonDown(0))
            return;

        Rect map = GetMapRect();
        Vector2 gui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (!map.Contains(gui))
            return;

        Hit hit = FindFriendly(gui, map);
        if (hit.Type != EntityType.None)
        {
            bool dbl = RegisterClick(hit.Key);
            Select(hit);
            if (dbl && hit.Target != null)
                FocusBehind(hit.Target);
            Input.ResetInputAxes();
            Debug.Log("TACTICAL-MAP-09F29V|Action=" + (dbl ? "SELECT_AND_BEHIND" : "SELECT_ONLY") + "|Entity=" + hit.Key);
            return;
        }

        lastClickKey = string.Empty;
        lastClickAt = -10f;
        Vector3 world = MapToWorld(gui, map);
        FocusWorld(world);
        Input.ResetInputAxes();
        Debug.Log("TACTICAL-MAP-09F29V|Action=FOCUS_MAP|X=" + world.x.ToString("0") + "|Z=" + world.z.ToString("0"));
    }

    private void DisableOldMapLayers()
    {
        if (oldUiDisabled)
            return;

        PrototypeCameraNavigation09F29P oldNav = UnityEngine.Object.FindAnyObjectByType<PrototypeCameraNavigation09F29P>();
        PrototypeTacticalMapInteraction09F29T oldInteraction = UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalMapInteraction09F29T>();
        PrototypeCropFieldMapOverlay09F29R oldCrop = UnityEngine.Object.FindAnyObjectByType<PrototypeCropFieldMapOverlay09F29R>();

        bool found = false;
        if (oldNav != null) { oldNav.enabled = false; found = true; }
        if (oldInteraction != null) { oldInteraction.enabled = false; found = true; }
        if (oldCrop != null) { oldCrop.enabled = false; found = true; }
        if (found)
        {
            oldUiDisabled = true;
            Debug.Log("TACTICAL-MAP-09F29V|LegacyMapLayers=Disabled");
        }
    }

    private void OnGUI()
    {
        if (!mapVisible || cam == null)
            return;

        EnsureStyles();
        GUI.depth = -71000;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 18f), "TAKTISK KORT / KAMERA  [M]", titleStyle);

        Rect map = GetMapRect();
        DrawMap(map);

        float by = panel.y + 143f;
        if (GUI.Button(new Rect(panel.x + 8f, by, 32f, 27f), "<", buttonStyle)) RotateCamera(-45f);
        if (GUI.Button(new Rect(panel.x + 43f, by, 32f, 27f), "N", buttonStyle)) FaceNorth();
        if (GUI.Button(new Rect(panel.x + 78f, by, 32f, 27f), ">", buttonStyle)) RotateCamera(45f);
        if (GUI.Button(new Rect(panel.x + 115f, by, 65f, 27f), "FOCUS", buttonStyle)) FocusSelected(false);
        if (GUI.Button(new Rect(panel.x + 184f, by, 77f, 27f), "BAG", buttonStyle)) FocusSelected(true);

        GUI.Label(new Rect(panel.x + 8f, panel.y + 171f, panel.width - 16f, 14f),
            "Fast slagmark • hvid ramme = kamera • II/III = HQ", smallStyle);
    }

    private void DrawMap(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.18f, 0.25f, 0.15f, 0.97f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;

        DrawCropFields(rect);
        DrawRoad(rect);
        DrawRiver(rect);
        DrawUnits(rect);
        DrawHeadquarters(rect);
        DrawCameraViewport(rect);

        DrawBorder(rect, new Color(0.85f, 0.82f, 0.64f, 0.85f), 1f);
    }

    private static void DrawRoad(Rect rect)
    {
        Vector2 prev = Vector2.zero;
        bool has = false;
        for (int i = 0; i <= 96; i++)
        {
            float x = Mathf.Lerp(-PrototypeBootstrap.BattlefieldHalfWidth, PrototypeBootstrap.BattlefieldHalfWidth, i / 96f);
            float z = 22f + Mathf.Sin(x * 0.014f) * 8.2f;
            Vector2 p = WorldToMap(rect, new Vector3(x, 0f, z));
            if (has) DrawLine(prev, p, new Color(0.66f, 0.52f, 0.31f, 0.90f), 2f);
            prev = p; has = true;
        }
    }

    private static void DrawRiver(Rect rect)
    {
        Vector2 prev = Vector2.zero;
        bool has = false;
        for (int i = 0; i <= 96; i++)
        {
            float z = Mathf.Lerp(-PrototypeBootstrap.BattlefieldHalfDepth, PrototypeBootstrap.BattlefieldHalfDepth, i / 96f);
            float x = PrototypeBootstrap.StreamCenterX(z);
            Vector2 p = WorldToMap(rect, new Vector3(x, 0f, z));
            if (has) DrawLine(prev, p, new Color(0.20f, 0.58f, 0.82f, 0.95f), 3f);
            prev = p; has = true;
        }
    }

    private static void DrawCropFields(Rect rect)
    {
        IReadOnlyList<PrototypeCropFieldTerrain09F29R.FieldDescriptor> fields = PrototypeCropFieldTerrain09F29R.Fields;
        if (fields == null)
            return;

        foreach (PrototypeCropFieldTerrain09F29R.FieldDescriptor field in fields)
        {
            Quaternion rot = Quaternion.Euler(0f, field.Yaw, 0f);
            Vector3 center = new Vector3(field.Center.x, 0f, field.Center.y);
            Vector3[] local =
            {
                new Vector3(-field.Width * 0.5f, 0f, -field.Depth * 0.5f),
                new Vector3( field.Width * 0.5f, 0f, -field.Depth * 0.5f),
                new Vector3( field.Width * 0.5f, 0f,  field.Depth * 0.5f),
                new Vector3(-field.Width * 0.5f, 0f,  field.Depth * 0.5f)
            };
            Vector2 a = WorldToMap(rect, center + rot * local[0]);
            Vector2 b = WorldToMap(rect, center + rot * local[1]);
            Vector2 c = WorldToMap(rect, center + rot * local[2]);
            Vector2 d = WorldToMap(rect, center + rot * local[3]);
            Color color = new Color(0.86f, 0.72f, 0.15f, 0.88f);
            DrawLine(a, b, color, 1.2f); DrawLine(b, c, color, 1.2f);
            DrawLine(c, d, color, 1.2f); DrawLine(d, a, color, 1.2f);
        }
    }

    private static void DrawUnits(Rect rect)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.CurrentStrength <= 0)
                continue;
            Vector2 p = WorldToMap(rect, unit.transform.position);
            float size = unit.IsSelected ? 8f : 5f;
            Color color = unit.IsSelected ? new Color(1f, 0.83f, 0.12f, 1f) :
                unit.Team == BattleTeam.Denmark ? new Color(0.22f, 0.58f, 0.95f, 1f) : new Color(0.88f, 0.20f, 0.16f, 1f);
            DrawDot(p, size, color);
        }
    }

    private void DrawHeadquarters(Rect rect)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int selectedMajor = GetSelectedMajor(hierarchy);
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null) continue;
                DrawHqMarker(WorldToMap(rect, hq.transform.position), "II", selectedMajor == i);
            }
        }

        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && regiment.Installed && regiment.HqRoot != null)
            DrawHqMarker(WorldToMap(rect, regiment.HqRoot.transform.position), "III", regiment.Selected);
    }

    private void DrawHqMarker(Vector2 p, string echelon, bool selected)
    {
        Rect r = new Rect(p.x - 9f, p.y - 7f, 18f, 14f);
        Color old = GUI.color;
        GUI.color = selected ? new Color(0.96f, 0.78f, 0.12f, 1f) : new Color(0.25f, 0.88f, 0.95f, 1f);
        GUI.DrawTexture(r, whiteTexture);
        GUI.color = old;
        GUI.Label(r, echelon, hqStyle);
    }

    private void DrawCameraViewport(Rect rect)
    {
        Vector3[] world = new Vector3[4];
        bool ok = TryViewportGround(new Vector2(0f, 0f), out world[0]) &&
                  TryViewportGround(new Vector2(1f, 0f), out world[1]) &&
                  TryViewportGround(new Vector2(1f, 1f), out world[2]) &&
                  TryViewportGround(new Vector2(0f, 1f), out world[3]);

        if (ok)
        {
            Vector2 a = WorldToMap(rect, world[0]);
            Vector2 b = WorldToMap(rect, world[1]);
            Vector2 c = WorldToMap(rect, world[2]);
            Vector2 d = WorldToMap(rect, world[3]);
            DrawLine(a, b, Color.white, 1.4f); DrawLine(b, c, Color.white, 1.4f);
            DrawLine(c, d, Color.white, 1.4f); DrawLine(d, a, Color.white, 1.4f);
        }

        Vector2 center = WorldToMap(rect, cam.transform.position);
        DrawDot(center, 4f, Color.white);
    }

    private bool TryViewportGround(Vector2 viewport, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float enter) || enter < 0f)
            return false;
        point = ray.GetPoint(enter);
        point.x = Mathf.Clamp(point.x, -PrototypeBootstrap.BattlefieldHalfWidth, PrototypeBootstrap.BattlefieldHalfWidth);
        point.z = Mathf.Clamp(point.z, -PrototypeBootstrap.BattlefieldHalfDepth, PrototypeBootstrap.BattlefieldHalfDepth);
        return true;
    }

    private Hit FindFriendly(Vector2 click, Rect rect)
    {
        Hit best = new Hit { Type = EntityType.None, MajorIndex = -1, Distance = float.PositiveInfinity };
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit == null || unit.Team != BattleTeam.Denmark || unit.IsRouted || unit.CurrentStrength <= 0)
                    continue;
                float d = Vector2.Distance(click, WorldToMap(rect, unit.transform.position));
                if (d <= 10f && d < best.Distance)
                    best = new Hit { Type = EntityType.Company, Company = unit, Target = unit.transform, Key = "COMP:" + unit.RegimentName, Distance = d, MajorIndex = -1 };
            }
        }

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null) continue;
                float d = Vector2.Distance(click, WorldToMap(rect, hq.transform.position));
                if (d <= 12f && d < best.Distance)
                    best = new Hit { Type = EntityType.Major, MajorIndex = i, Target = hq.transform, Key = "MAJOR:" + i, Distance = d };
            }
        }

        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && regiment.Installed && regiment.HqRoot != null)
        {
            float d = Vector2.Distance(click, WorldToMap(rect, regiment.HqRoot.transform.position));
            if (d <= 13f && d < best.Distance)
                best = new Hit { Type = EntityType.RegimentHq, Target = regiment.HqRoot.transform, Key = "REGHQ", Distance = d, MajorIndex = -1 };
        }
        return best;
    }

    private void Select(Hit hit)
    {
        switch (hit.Type)
        {
            case EntityType.Company: SelectCompany(hit.Company); break;
            case EntityType.Major: SelectMajor(hit.MajorIndex); break;
            case EntityType.RegimentHq: SelectRegiment(); break;
        }
    }

    private void SelectCompany(Regiment unit)
    {
        if (unit == null) return;
        SetRegimentSelected(false);
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null) hierarchy.ClearMajorSelection();
        ClearCompanies();
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> list = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (list != null) { list.Add(unit); unit.SetSelected(true); }
        }
    }

    private void SelectMajor(int index)
    {
        SetRegimentSelected(false);
        ClearCompanies();
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed && selectMajorMethod != null)
            selectMajorMethod.Invoke(hierarchy, new object[] { index });
    }

    private void SelectRegiment()
    {
        ClearCompanies();
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null) hierarchy.ClearMajorSelection();
        SetRegimentSelected(true);
    }

    private void ClearCompanies()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
            foreach (Regiment unit in battle.Regiments)
                if (unit != null && unit.Team == BattleTeam.Denmark && unit.IsSelected) unit.SetSelected(false);
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> list = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (list != null) list.Clear();
        }
    }

    private void SetRegimentSelected(bool value)
    {
        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regiment, new object[] { value });
    }

    private bool RegisterClick(string key)
    {
        float now = Time.unscaledTime;
        bool dbl = key == lastClickKey && now - lastClickAt <= DoubleClickSeconds;
        lastClickKey = key; lastClickAt = now;
        return dbl;
    }

    private void FocusSelected(bool behind)
    {
        Transform target = ResolveSelected();
        if (target == null) return;
        if (behind) FocusBehind(target); else FocusWorld(target.position);
    }

    private Transform ResolveSelected()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
            foreach (Regiment unit in battle.Regiments)
                if (unit != null && unit.IsSelected && !unit.IsRouted) return unit.transform;

        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && regiment.Selected && regiment.HqRoot != null) return regiment.HqRoot.transform;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int index = GetSelectedMajor(hierarchy);
        if (hierarchy != null && index >= 0 && index < hierarchy.BattalionCount)
        {
            GameObject hq = hierarchy.GetMajorHq(index);
            if (hq != null) return hq.transform;
        }
        return null;
    }

    private int GetSelectedMajor(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null) return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private void FocusBehind(Transform target)
    {
        if (target == null || cam == null) return;
        Vector3 forward = target.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 p = target.position - forward * 48f;
        p.y = PrototypeBootstrap.SampleGroundHeight(target.position.x, target.position.z) + 34f;
        cam.transform.position = ClampCamera(p);
        cam.transform.rotation = Quaternion.Euler(33f, target.eulerAngles.y, 0f);
    }

    private void FocusWorld(Vector3 point)
    {
        if (cam == null) return;
        Vector3 flat = cam.transform.forward; flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        flat.Normalize();
        float height = Mathf.Clamp(cam.transform.position.y, 18f, 600f);
        Vector3 p = point - flat * Mathf.Clamp(height * 0.72f, 22f, 260f);
        p.y = height;
        cam.transform.position = ClampCamera(p);
    }

    private void RotateCamera(float degrees)
    {
        if (cam == null) return;
        Transform selected = ResolveSelected();
        Vector3 pivot = selected != null ? selected.position : cam.transform.position + Flat(cam.transform.forward) * 100f;
        cam.transform.RotateAround(pivot, Vector3.up, degrees);
        cam.transform.position = ClampCamera(cam.transform.position);
    }

    private void FaceNorth()
    {
        if (cam == null) return;
        float pitch = cam.transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        cam.transform.rotation = Quaternion.Euler(Mathf.Clamp(pitch, 20f, 70f), 0f, 0f);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
    }

    private static Vector3 ClampCamera(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, -PrototypeBootstrap.BattlefieldHalfWidth + 8f, PrototypeBootstrap.BattlefieldHalfWidth - 8f);
        p.z = Mathf.Clamp(p.z, -PrototypeBootstrap.BattlefieldHalfDepth + 8f, PrototypeBootstrap.BattlefieldHalfDepth - 8f);
        p.y = Mathf.Clamp(p.y, 8f, 600f);
        return p;
    }

    private static Vector2 WorldToMap(Rect rect, Vector3 world)
    {
        float tx = Mathf.InverseLerp(-PrototypeBootstrap.BattlefieldHalfWidth, PrototypeBootstrap.BattlefieldHalfWidth, world.x);
        float tz = Mathf.InverseLerp(-PrototypeBootstrap.BattlefieldHalfDepth, PrototypeBootstrap.BattlefieldHalfDepth, world.z);
        return new Vector2(Mathf.Lerp(rect.x, rect.xMax, tx), Mathf.Lerp(rect.yMax, rect.y, tz));
    }

    private static Vector3 MapToWorld(Vector2 gui, Rect rect)
    {
        float tx = Mathf.InverseLerp(rect.x, rect.xMax, gui.x);
        float tz = Mathf.InverseLerp(rect.yMax, rect.y, gui.y);
        float x = Mathf.Lerp(-PrototypeBootstrap.BattlefieldHalfWidth, PrototypeBootstrap.BattlefieldHalfWidth, tx);
        float z = Mathf.Lerp(-PrototypeBootstrap.BattlefieldHalfDepth, PrototypeBootstrap.BattlefieldHalfDepth, tz);
        return new Vector3(x, PrototypeBootstrap.SampleGroundHeight(x, z), z);
    }

    private static Rect GetPanelRect()
    {
        float x = Mathf.Max(8f, Screen.width - PanelWidth - 10f);
        float y = Mathf.Max(66f, Screen.height - BottomGuard - PanelHeight - 8f);
        return new Rect(x, y, PanelWidth, PanelHeight);
    }

    private static Rect GetMapRect()
    {
        Rect p = GetPanelRect();
        return new Rect(p.x + 8f, p.y + 25f, p.width - 16f, 112f);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelTexture = MakeTexture(new Color(0.03f, 0.04f, 0.032f, 0.96f));
        whiteTexture = MakeTexture(Color.white);
        panelStyle = new GUIStyle(GUI.skin.box); panelStyle.normal.background = panelTexture;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = new Color(0.88f, 0.82f, 0.55f, 1f);
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 8, alignment = TextAnchor.MiddleCenter };
        smallStyle.normal.textColor = new Color(0.68f, 0.70f, 0.62f, 1f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 9, fontStyle = FontStyle.Bold };
        hqStyle = new GUIStyle(GUI.skin.label) { fontSize = 7, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        hqStyle.normal.textColor = Color.black;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.hideFlags = HideFlags.HideAndDontSave;
        t.SetPixel(0, 0, color); t.Apply(false, true); return t;
    }

    private static void DrawDot(Vector2 p, float size, Color color)
    {
        Color old = GUI.color; GUI.color = color;
        GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static void DrawBorder(Rect rect, Color color, float width)
    {
        DrawSolid(new Rect(rect.x, rect.y, rect.width, width), color);
        DrawSolid(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
        DrawSolid(new Rect(rect.x, rect.y, width, rect.height), color);
        DrawSolid(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
    }

    private static void DrawSolid(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
    }

    private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 d = b - a;
        float length = d.magnitude;
        if (length < 0.01f) return;
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.color = color;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }
}