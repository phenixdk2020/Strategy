using System.Reflection;
using UnityEngine;

// v00.00.09f30a
// Cavalry rows integrated visually beneath the existing OOB navigator.
// Single click selects only; double click selects and moves camera behind the unit,
// matching the existing company/HQ OOB interaction rule.
[DefaultExecutionOrder(39410)]
public sealed class PrototypeCavalryOob09F30A : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DoubleClickSeconds = 0.34f;
    private const float HeaderHeight = 19f;
    private const float RowHeight = 22f;

    private static PrototypeCavalryOob09F30A instance;

    private MethodInfo getOobPanelRectMethod;
    private FieldInfo oobOpenField;
    private Camera cam;
    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle rowStyle;
    private GUIStyle echelonStyle;
    private GUIStyle nameStyle;
    private GUIStyle rightStyle;
    private Texture2D panelTexture;
    private Texture2D selectedTexture;
    private Texture2D rowTexture;
    private Texture2D hoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryOob09F30A>() == null)
            new GameObject("PrototypeCavalryOob_v000009f30a")
                .AddComponent<PrototypeCavalryOob09F30A>();
    }

    private void Awake()
    {
        instance = this;
        getOobPanelRectMethod = typeof(PrototypeOobNavigator09F29Q).GetMethod("GetPanelRect", PrivateInstance);
        oobOpenField = typeof(PrototypeOobNavigator09F29Q).GetField("open", PrivateInstance);
        Debug.Log("CAVALRY-OOB-09F30A|Installed=True|SingleClick=SelectOnly|DoubleClick=Behind|SeparateTestPanel=False");
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
    }

    public static bool IsPointerOverPanel(Vector3 mousePosition)
    {
        if (instance == null || !instance.ShouldDraw())
            return false;

        Rect rect = instance.GetPanelRect();
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return rect.Contains(gui);
    }

    private bool ShouldDraw()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeOobNavigator09F29Q oob = PrototypeOobNavigator09F29Q.Instance;
        if (cavalry == null || !cavalry.Installed || oob == null)
            return false;

        if (oobOpenField == null)
            return true;

        object value = oobOpenField.GetValue(oob);
        return !(value is bool) || (bool)value;
    }

    private Rect GetPanelRect()
    {
        PrototypeOobNavigator09F29Q oob = PrototypeOobNavigator09F29Q.Instance;
        if (oob != null && getOobPanelRectMethod != null)
        {
            object raw = getOobPanelRectMethod.Invoke(oob, null);
            if (raw is Rect)
            {
                Rect baseRect = (Rect)raw;
                return new Rect(baseRect.x, baseRect.yMax + 2f, baseRect.width,
                    HeaderHeight + RowHeight * 2f + 5f);
            }
        }

        return new Rect(8f, 300f, 292f, HeaderHeight + RowHeight * 2f + 5f);
    }

    private void OnGUI()
    {
        if (!ShouldDraw())
            return;

        EnsureStyles();
        GUI.depth = -125050;

        PrototypeCavalryManager09F30 manager = PrototypeCavalryManager09F30.Instance;
        if (manager == null)
            return;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 7f, panel.y + 1f, panel.width - 14f, HeaderHeight - 2f),
            "KAVALERI", headerStyle);

        float y = panel.y + HeaderHeight;
        DrawUnitRow(manager.Gardehusar, new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), "CAV_GARDE");
        y += RowHeight;
        DrawUnitRow(manager.Dragon, new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), "CAV_DRAGON");
    }

    private void DrawUnitRow(PrototypeCavalryUnit09F30 unit, Rect rect, string clickKey)
    {
        if (unit == null)
            return;

        bool selected = unit.IsSelected;
        Color old = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, selected ? selectedTexture : rowTexture);
        GUI.color = old;

        if (GUI.Button(rect, GUIContent.none, rowStyle))
        {
            PrototypeCavalryManager09F30 manager = PrototypeCavalryManager09F30.Instance;
            if (manager != null)
            {
                bool dbl = RegisterClick(clickKey);
                manager.SelectUnit(unit);
                if (dbl)
                    FocusBehind(unit.transform);
                Input.ResetInputAxes();
            }
        }

        GUI.Label(new Rect(rect.x + 4f, rect.y, 24f, rect.height), "I", echelonStyle);
        GUI.Label(new Rect(rect.x + 31f, rect.y, rect.width - 112f, rect.height), unit.UnitName, nameStyle);
        GUI.Label(new Rect(rect.xMax - 78f, rect.y, 73f, rect.height),
            unit.CurrentStrength + "  " + ShortStatus(unit), rightStyle);
    }

    private bool RegisterClick(string key)
    {
        float now = Time.unscaledTime;
        bool dbl = key == lastClickKey && now - lastClickAt <= DoubleClickSeconds;
        lastClickKey = key;
        lastClickAt = now;
        return dbl;
    }

    private void FocusBehind(Transform target)
    {
        if (target == null)
            return;
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        Vector3 forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 p = target.position - forward * 48f;
        p.y = PrototypeBootstrap.SampleGroundHeight(target.position.x, target.position.z) + 34f;

        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 10f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 10f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, 9.5f, 600f);

        cam.transform.position = p;
        cam.transform.rotation = Quaternion.Euler(33f, target.eulerAngles.y, 0f);
    }

    private static string ShortStatus(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return "—";
        if (unit.Action == PrototypeCavalryAction09F30.Charge)
            return "CHARGE";
        if (unit.Action == PrototypeCavalryAction09F30.Move)
            return "→";
        if (unit.Action == PrototypeCavalryAction09F30.Falter)
            return "FALTER";
        return unit.Mode == PrototypeCavalryMode09F30.Mounted ? "HOLD" : "AFSIDDET";
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.030f, 0.040f, 0.032f, 0.96f));
        selectedTexture = MakeTexture(new Color(0.44f, 0.34f, 0.075f, 0.98f));
        rowTexture = MakeTexture(new Color(0.065f, 0.080f, 0.058f, 0.96f));
        hoverTexture = MakeTexture(new Color(0.22f, 0.28f, 0.14f, 0.38f));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.padding = new RectOffset(0, 0, 0, 0);

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 9;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.normal.textColor = new Color(0.95f, 0.82f, 0.35f, 1f);

        rowStyle = new GUIStyle(GUI.skin.button);
        rowStyle.normal.background = null;
        rowStyle.hover.background = hoverTexture;
        rowStyle.active.background = hoverTexture;
        rowStyle.border = new RectOffset(0, 0, 0, 0);

        echelonStyle = new GUIStyle(GUI.skin.label);
        echelonStyle.fontSize = 9;
        echelonStyle.fontStyle = FontStyle.Bold;
        echelonStyle.alignment = TextAnchor.MiddleCenter;
        echelonStyle.normal.textColor = new Color(0.38f, 0.70f, 1.00f, 1f);

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 9;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.normal.textColor = new Color(0.94f, 0.94f, 0.87f, 1f);

        rightStyle = new GUIStyle(GUI.skin.label);
        rightStyle.fontSize = 8;
        rightStyle.alignment = TextAnchor.MiddleRight;
        rightStyle.normal.textColor = new Color(0.67f, 0.70f, 0.62f, 1f);
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
