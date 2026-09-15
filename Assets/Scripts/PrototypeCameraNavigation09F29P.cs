using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29p
// Tactical overview/navigation UI.
// Provides a compact battlefield map, quick camera rotation and fast focus on selected
// company/HQ. Shift+F places the camera behind the selected formation at tactical zoom.
[DefaultExecutionOrder(39000)]
public sealed class PrototypeCameraNavigation09F29P : MonoBehaviour
{
    private struct MapBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
    }

    private Camera cam;
    private bool mapVisible = true;
    private GUIStyle panelStyle;
    private GUIStyle smallStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private FieldInfo selectedBattalionField;

    private const float PanelWidth = 270f;
    private const float PanelHeight = 188f;
    private const float BottomGuard = 104f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCameraNavigation09F29P>() == null)
            new GameObject("PrototypeCameraNavigation_v000009f29p")
                .AddComponent<PrototypeCameraNavigation09F29P>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "CAMERA-NAV-09F29P|Installed=True|Map=True|M=ToggleMap|F=FocusSelected|" +
            "ShiftF=BehindSelected|F1=MajorA|F2=MajorB|F3=Oberstlojtnant|N=North");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        if (Input.GetKeyDown(KeyCode.M))
            mapVisible = !mapVisible;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                FocusBehindSelected();
            else
                FocusSelected();
        }

        if (Input.GetKeyDown(KeyCode.N))
            FaceNorth();

        if (Input.GetKeyDown(KeyCode.F1))
            FocusMajor(0);
        if (Input.GetKeyDown(KeyCode.F2))
            FocusMajor(1);
        if (Input.GetKeyDown(KeyCode.F3))
            FocusRegimental();
    }

    private void OnGUI()
    {
        if (!mapVisible || cam == null)
            return;

        EnsureStyles();
        GUI.depth = -70000;

        float x = Mathf.Max(8f, Screen.width - PanelWidth - 10f);
        float y = Mathf.Max(66f, Screen.height - BottomGuard - PanelHeight - 8f);
        Rect panel = new Rect(x, y, PanelWidth, PanelHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 18f),
            "TAKTISK KORT / KAMERA  [M]", titleStyle);

        Rect map = new Rect(panel.x + 8f, panel.y + 25f, panel.width - 16f, 112f);
        DrawMap(map);

        float by = panel.y + 143f;
        if (GUI.Button(new Rect(panel.x + 8f, by, 32f, 27f), "<", buttonStyle))
            RotateCamera(-45f);
        if (GUI.Button(new Rect(panel.x + 43f, by, 32f, 27f), "N", buttonStyle))
            FaceNorth();
        if (GUI.Button(new Rect(panel.x + 78f, by, 32f, 27f), ">", buttonStyle))
            RotateCamera(45f);
        if (GUI.Button(new Rect(panel.x + 115f, by, 65f, 27f), "FOCUS", buttonStyle))
            FocusSelected();
        if (GUI.Button(new Rect(panel.x + 184f, by, 77f, 27f), "BAG", buttonStyle))
            FocusBehindSelected();

        GUI.Label(new Rect(panel.x + 8f, panel.y + 171f, panel.width - 16f, 14f),
            "F=valgt  Shift+F=bag valgt  F1/F2=Major  F3=Oberstløjtnant", smallStyle);
    }

    private void DrawMap(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.18f, 0.25f, 0.15f, 0.97f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;

        MapBounds bounds = CalculateMapBounds();

        DrawRoad(rect, bounds);
        DrawRiver(rect, bounds);
        DrawUnits(rect, bounds);
        DrawHeadquarters(rect, bounds);
        DrawCameraDirection(rect, bounds);

        GUI.color = new Color(0.85f, 0.82f, 0.64f, 0.85f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static MapBounds CalculateMapBounds()
    {
        BattleManager battle = BattleManager.Instance;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit == null || unit.CurrentStrength <= 0)
                    continue;
                Vector3 p = unit.transform.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
        }

        if (float.IsInfinity(minX))
        {
            minX = -450f; maxX = 450f;
            minZ = -300f; maxZ = 300f;
        }

        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float width = Mathf.Clamp(Mathf.Max(900f, maxX - minX + 360f), 900f, PrototypeBootstrap.BattlefieldWidth);
        float depth = Mathf.Clamp(Mathf.Max(620f, maxZ - minZ + 300f), 620f, PrototypeBootstrap.BattlefieldDepth);

        centerX = Mathf.Clamp(centerX,
            -PrototypeBootstrap.BattlefieldHalfWidth + width * 0.5f,
            PrototypeBootstrap.BattlefieldHalfWidth - width * 0.5f);
        centerZ = Mathf.Clamp(centerZ,
            -PrototypeBootstrap.BattlefieldHalfDepth + depth * 0.5f,
            PrototypeBootstrap.BattlefieldHalfDepth - depth * 0.5f);

        return new MapBounds
        {
            MinX = centerX - width * 0.5f,
            MaxX = centerX + width * 0.5f,
            MinZ = centerZ - depth * 0.5f,
            MaxZ = centerZ + depth * 0.5f
        };
    }

    private static void DrawRoad(Rect rect, MapBounds bounds)
    {
        Vector2 previous = Vector2.zero;
        bool hasPrevious = false;
        for (int i = 0; i <= 48; i++)
        {
            float t = i / 48f;
            float x = Mathf.Lerp(bounds.MinX, bounds.MaxX, t);
            float z = 22f + Mathf.Sin(x * 0.014f) * 8.2f;
            Vector2 p = WorldToMap(rect, bounds, new Vector3(x, 0f, z));
            if (hasPrevious)
                DrawLine(previous, p, new Color(0.66f, 0.52f, 0.31f, 0.90f), 2f);
            previous = p;
            hasPrevious = true;
        }
    }

    private static void DrawRiver(Rect rect, MapBounds bounds)
    {
        Vector2 previous = Vector2.zero;
        bool hasPrevious = false;
        for (int i = 0; i <= 48; i++)
        {
            float t = i / 48f;
            float z = Mathf.Lerp(bounds.MinZ, bounds.MaxZ, t);
            float x = PrototypeBootstrap.StreamCenterX(z);
            Vector2 p = WorldToMap(rect, bounds, new Vector3(x, 0f, z));
            if (hasPrevious)
                DrawLine(previous, p, new Color(0.20f, 0.58f, 0.82f, 0.95f), 3f);
            previous = p;
            hasPrevious = true;
        }
    }

    private static void DrawUnits(Rect rect, MapBounds bounds)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.CurrentStrength <= 0)
                continue;

            Vector2 p = WorldToMap(rect, bounds, unit.transform.position);
            float size = unit.IsSelected ? 8f : 5f;
            Color color = unit.IsSelected
                ? new Color(1f, 0.83f, 0.12f, 1f)
                : unit.Team == BattleTeam.Denmark
                    ? new Color(0.22f, 0.58f, 0.95f, 1f)
                    : new Color(0.88f, 0.20f, 0.16f, 1f);
            DrawDot(p, size, color);
        }
    }

    private void DrawHeadquarters(Rect rect, MapBounds bounds)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null)
                    continue;
                Vector2 p = WorldToMap(rect, bounds, hq.transform.position);
                DrawDot(p, 6f, new Color(0.28f, 0.90f, 0.95f, 1f));
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
        {
            Vector2 p = WorldToMap(rect, bounds, regimental.HqRoot.transform.position);
            DrawDot(p, 7f, new Color(1f, 0.72f, 0.15f, 1f));
        }
    }

    private void DrawCameraDirection(Rect rect, MapBounds bounds)
    {
        if (cam == null)
            return;
        Vector3 flat = cam.transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return;
        flat.Normalize();

        Vector2 a = WorldToMap(rect, bounds, cam.transform.position);
        Vector2 b = WorldToMap(rect, bounds, cam.transform.position + flat * 90f);
        DrawLine(a, b, Color.white, 1.5f);
        DrawDot(a, 4f, Color.white);
    }

    private void FocusSelected()
    {
        Transform target = ResolveSelectedTransform();
        if (target != null)
            FocusWorld(target.position);
    }

    private void FocusBehindSelected()
    {
        Transform target = ResolveSelectedTransform();
        if (target == null || cam == null)
            return;

        Vector3 forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 point = target.position;
        float ground = PrototypeBootstrap.SampleGroundHeight(point.x, point.z);
        Vector3 position = point - forward * 48f;
        position.y = ground + 34f;
        cam.transform.position = ClampCamera(position);
        cam.transform.rotation = Quaternion.Euler(33f, target.eulerAngles.y, 0f);
    }

    private void FocusWorld(Vector3 point)
    {
        if (cam == null)
            return;

        Vector3 flat = cam.transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            flat = Vector3.forward;
        flat.Normalize();

        float height = Mathf.Clamp(cam.transform.position.y, 18f, 600f);
        Vector3 position = point - flat * Mathf.Clamp(height * 0.72f, 22f, 260f);
        position.y = height;
        cam.transform.position = ClampCamera(position);
    }

    private void RotateCamera(float degrees)
    {
        if (cam == null)
            return;
        Transform selected = ResolveSelectedTransform();
        Vector3 pivot;
        if (selected != null)
            pivot = selected.position;
        else
        {
            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
            pivot = cam.transform.position + flat.normalized * 100f;
        }
        cam.transform.RotateAround(pivot, Vector3.up, degrees);
        cam.transform.position = ClampCamera(cam.transform.position);
    }

    private void FaceNorth()
    {
        if (cam == null)
            return;
        Vector3 euler = cam.transform.eulerAngles;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        pitch = Mathf.Clamp(pitch, 20f, 70f);
        cam.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FocusMajor(int index)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed)
            return;
        GameObject hq = hierarchy.GetMajorHq(index);
        if (hq != null)
            FocusWorld(hq.transform.position);
    }

    private void FocusRegimental()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
            FocusWorld(regimental.HqRoot.transform.position);
    }

    private Transform ResolveSelectedTransform()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
                if (unit != null && unit.IsSelected && !unit.IsRouted)
                    return unit.transform;
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Selected && regimental.HqRoot != null)
            return regimental.HqRoot.transform;

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed && selectedBattalionField != null)
        {
            object value = selectedBattalionField.GetValue(hierarchy);
            if (value is int)
            {
                int index = (int)value;
                if (index >= 0 && index < hierarchy.BattalionCount)
                {
                    GameObject hq = hierarchy.GetMajorHq(index);
                    if (hq != null)
                        return hq.transform;
                }
            }
        }

        return null;
    }

    private static Vector3 ClampCamera(Vector3 p)
    {
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 10f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 10f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, 9.5f, 600f);
        return p;
    }

    private static Vector2 WorldToMap(Rect rect, MapBounds bounds, Vector3 world)
    {
        float tx = Mathf.InverseLerp(bounds.MinX, bounds.MaxX, world.x);
        float tz = Mathf.InverseLerp(bounds.MinZ, bounds.MaxZ, world.z);
        return new Vector2(rect.x + tx * rect.width, rect.yMax - tz * rect.height);
    }

    private static void DrawDot(Vector2 p, float size, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.1f)
            return;

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.color = color;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(new Color(0.035f, 0.045f, 0.035f, 0.95f));

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 10;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.95f, 0.90f, 0.70f, 1f);

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 8;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.normal.textColor = new Color(0.78f, 0.80f, 0.72f, 1f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 9;
        buttonStyle.fontStyle = FontStyle.Bold;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}
