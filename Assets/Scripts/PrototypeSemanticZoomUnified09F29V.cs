using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29v
// One authoritative semantic-zoom presentation for the tactical prototype.
// HQ counters appear from 72 m. All company NATO counters appear continuously from
// 175 m upward. 3D units remain visible together with NATO symbols until 315 m;
// at 315 m the 3D meshes are suppressed while NATO counters remain visible.
[DefaultExecutionOrder(40200)]
public sealed class PrototypeSemanticZoomUnified09F29V : MonoBehaviour
{
    private const float HqStart = 72f;
    private const float CompanyNatoStart = 175f;
    private const float StrategicStart = 315f;
    private const float BottomGuard = 104f;

    private static readonly Color Friendly = new Color(0.22f, 0.58f, 0.92f, 1f);
    private static readonly Color Enemy = new Color(0.86f, 0.24f, 0.20f, 1f);
    private static readonly Color Selected = new Color(1.00f, 0.80f, 0.18f, 1f);
    private static readonly Color Routed = new Color(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color Fill = new Color(0.025f, 0.032f, 0.030f, 0.96f);

    private Camera cam;
    private bool legacyDisabled;
    private bool strategicSuppressed;
    private bool f7WasEnabled;
    private FieldInfo selectedBattalionField;

    private GUIStyle counterStyle;
    private GUIStyle symbolStyle;
    private GUIStyle echelonStyle;
    private GUIStyle nameStyle;
    private GUIStyle viewStyle;
    private Texture2D fillTexture;
    private Texture2D whiteTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSemanticZoomUnified09F29V>() == null)
            new GameObject("PrototypeSemanticZoomUnified_v000009f29v").AddComponent<PrototypeSemanticZoomUnified09F29V>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", BindingFlags.Instance | BindingFlags.NonPublic);
        Debug.Log("SEMANTIC-ZOOM-09F29V|Installed=True|HQ=72m|CompanyNATO=175m+|3DAndNATO=175-314m|Strategic3DOff=315m|NATOContinuous=True");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        DisableLegacyLayers();
        if (cam == null)
            return;

        bool strategic = cam.transform.position.y >= StrategicStart;
        if (strategic)
            ApplyStrategicSuppression();
        else if (strategicSuppressed)
            RestoreStrategicMeshes();
    }

    private void DisableLegacyLayers()
    {
        if (legacyDisabled)
            return;

        PrototypeSemanticZoom09F29D d = UnityEngine.Object.FindAnyObjectByType<PrototypeSemanticZoom09F29D>();
        PrototypeSemanticZoomPolish09F29Q q = UnityEngine.Object.FindAnyObjectByType<PrototypeSemanticZoomPolish09F29Q>();
        bool found = false;
        if (d != null) { d.enabled = false; found = true; }
        if (q != null) { q.enabled = false; found = true; }
        if (found)
        {
            legacyDisabled = true;
            Debug.Log("SEMANTIC-ZOOM-09F29V|LegacyD=False|LegacyQ=False");
        }
    }

    private void OnGUI()
    {
        if (cam == null)
            return;

        EnsureStyles();
        GUI.depth = -5200;
        float height = cam.transform.position.y;

        if (height >= CompanyNatoStart)
            DrawCompanyCounters();
        if (height >= HqStart)
            DrawHqCounters();
        if (height >= 95f)
            DrawViewIndicator(height);
    }

    private void DrawCompanyCounters()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment unit in battle.Regiments)
        {
            if (unit == null || unit.CurrentStrength <= 0)
                continue;
            if (!TryProject(unit.transform.position + Vector3.up * 2.5f, out Vector2 anchor))
                continue;

            Rect frame = new Rect(anchor.x - 25f, anchor.y - 43f, 50f, 30f);
            Color color = unit.IsRouted ? Routed : unit.IsSelected ? Selected : unit.Team == BattleTeam.Denmark ? Friendly : Enemy;
            GUI.Box(frame, GUIContent.none, counterStyle);
            DrawBorder(frame, color, unit.IsSelected ? 3f : 2f);
            GUI.Label(new Rect(frame.x, frame.y - 13f, frame.width, 11f), "I", echelonStyle);
            GUI.Label(new Rect(frame.x, frame.y + 1f, frame.width, 20f), "X", symbolStyle);

            string name = unit.Team == BattleTeam.Denmark ? PrototypeUnitNames09F29C.Get(unit) : EnemyName(unit);
            GUI.Label(new Rect(frame.center.x - 67f, frame.yMax, 134f, 13f), name, nameStyle);
        }
    }

    private void DrawHqCounters()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int selectedMajor = GetSelectedMajor(hierarchy);
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null || !TryProject(hq.transform.position + Vector3.up * 4f, out Vector2 anchor))
                    continue;
                DrawHqCounter(anchor, "II", "MAJOR " + (i == 0 ? "A" : "B"), selectedMajor == i, false);
            }
        }

        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && regiment.Installed && regiment.HqRoot != null &&
            TryProject(regiment.HqRoot.transform.position + Vector3.up * 4.5f, out Vector2 regAnchor))
        {
            DrawHqCounter(regAnchor, "III", "OBERSTLØJTNANT", regiment.Selected, true);
        }
    }

    private void DrawHqCounter(Vector2 anchor, string echelon, string label, bool selected, bool regimental)
    {
        float width = regimental ? 78f : 64f;
        float height = regimental ? 40f : 35f;
        Rect frame = new Rect(anchor.x - width * 0.5f, anchor.y - height - 28f, width, height);
        if (!IsUseful(frame))
            return;

        GUI.Box(frame, GUIContent.none, counterStyle);
        DrawBorder(frame, selected ? Selected : Friendly, selected ? 3f : 2f);
        GUI.Label(new Rect(frame.x, frame.y - 15f, frame.width, 13f), echelon, echelonStyle);
        GUI.Label(new Rect(frame.x, frame.y + 2f, frame.width, frame.height - 4f), "HQ", symbolStyle);
        float nameWidth = regimental ? 180f : 130f;
        GUI.Label(new Rect(frame.center.x - nameWidth * 0.5f, frame.yMax, nameWidth, 13f), label, nameStyle);
    }

    private void DrawViewIndicator(float height)
    {
        string level = height >= StrategicStart ? "STRATEGIC" : height >= CompanyNatoStart ? "OPERATIONAL" : "MEDIUM";
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 242f), 39f, 234f, 22f);
        GUI.Box(rect, "TACTICAL VIEW  " + level + "  |  " + height.ToString("0") + "m", viewStyle);
    }

    private void ApplyStrategicSuppression()
    {
        PrototypeBattleVisuals09F7 f7 = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (!strategicSuppressed)
        {
            f7WasEnabled = f7 != null && f7.enabled;
            strategicSuppressed = true;
            Debug.Log("SEMANTIC-ZOOM-09F29V|Level=STRATEGIC|Height=" + cam.transform.position.y.ToString("0") + "|3DMeshes=False|NATO=True");
        }
        if (f7 != null)
            f7.enabled = false;
        SetAllMeshes(false);
    }

    private void RestoreStrategicMeshes()
    {
        strategicSuppressed = false;
        SetAllMeshes(true);
        PrototypeBattleVisuals09F7 f7 = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (f7 != null && f7WasEnabled)
            f7.enabled = true;
        Debug.Log("SEMANTIC-ZOOM-09F29V|Level=OPERATIONAL_OR_CLOSER|3DMeshes=True|NATO=" + (cam != null && cam.transform.position.y >= CompanyNatoStart));
    }

    private static void SetAllMeshes(bool visible)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
            foreach (Regiment unit in battle.Regiments)
                if (unit != null) SetMeshVisibility(unit.gameObject, visible);

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
            for (int i = 0; i < hierarchy.BattalionCount; i++)
                if (hierarchy.GetMajorHq(i) != null) SetMeshVisibility(hierarchy.GetMajorHq(i), visible);

        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        if (regiment != null && regiment.Installed && regiment.HqRoot != null)
            SetMeshVisibility(regiment.HqRoot, visible);
    }

    private static void SetMeshVisibility(GameObject root, bool visible)
    {
        if (root == null) return;
        MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++) if (meshes[i] != null) meshes[i].enabled = visible;
        ParticleSystemRenderer[] particles = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particles.Length; i++) if (particles[i] != null) particles[i].enabled = visible;
    }

    private int GetSelectedMajor(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null) return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private bool TryProject(Vector3 world, out Vector2 gui)
    {
        gui = Vector2.zero;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0f) return false;
        gui = new Vector2(screen.x, Screen.height - screen.y);
        return gui.x >= -150f && gui.x <= Screen.width + 150f && gui.y >= -100f && gui.y <= Screen.height - BottomGuard + 90f;
    }

    private static bool IsUseful(Rect rect)
    {
        return rect.xMax >= -20f && rect.xMin <= Screen.width + 20f && rect.yMax >= -20f && rect.yMin <= Screen.height - BottomGuard + 20f;
    }

    private static string EnemyName(Regiment unit)
    {
        switch (unit.RegimentName)
        {
            case "8th Regiment": return "PR. 1. KOMPAGNI";
            case "18th Regiment": return "PR. 2. KOMPAGNI";
            default: return unit.RegimentName.ToUpperInvariant();
        }
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        DrawSolid(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawSolid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawSolid(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawSolid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawSolid(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture != null ? whiteTexture : Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void EnsureStyles()
    {
        if (counterStyle != null) return;
        fillTexture = MakeTexture(Fill);
        whiteTexture = MakeTexture(Color.white);
        counterStyle = new GUIStyle(GUI.skin.box); counterStyle.normal.background = fillTexture;
        symbolStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 18 };
        symbolStyle.normal.textColor = Color.white;
        echelonStyle = new GUIStyle(symbolStyle) { fontSize = 10 };
        nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 9 };
        nameStyle.normal.textColor = Color.white;
        viewStyle = new GUIStyle(GUI.skin.box) { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        viewStyle.normal.textColor = new Color(0.92f, 0.84f, 0.52f, 1f);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color); texture.Apply(false, true); return texture;
    }

    private void OnDisable()
    {
        if (strategicSuppressed)
            RestoreStrategicMeshes();
    }
}