using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29q
// Earlier semantic-zoom transitions layered on top of the proven F29D counter renderer.
// F29D remains authoritative from its original thresholds upward; Q fills the earlier
// discoverability gaps and advances the 3D->2D strategic hand-off to 315 m.
[DefaultExecutionOrder(37200)]
public sealed class PrototypeSemanticZoomPolish09F29Q : MonoBehaviour
{
    private const float HqStart = 55f;
    private const float MediumStart = 95f;
    private const float OperationalStart = 175f;
    private const float F29DOperationalStart = 235f;
    private const float StrategicStart = 315f;
    private const float F29DStrategicStart = 390f;
    private const float BottomGuard = 104f;

    private static readonly Color Friendly = new Color(0.22f, 0.58f, 0.92f, 1f);
    private static readonly Color Enemy = new Color(0.86f, 0.24f, 0.20f, 1f);
    private static readonly Color Selected = new Color(1.00f, 0.80f, 0.18f, 1f);
    private static readonly Color Routed = new Color(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color Fill = new Color(0.025f, 0.032f, 0.030f, 0.96f);

    private Camera cam;
    private bool qStrategicSuppressed;
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
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSemanticZoomPolish09F29Q>() == null)
            new GameObject("PrototypeSemanticZoomPolish_v000009f29q")
                .AddComponent<PrototypeSemanticZoomPolish09F29Q>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField(
            "selectedBattalion", BindingFlags.Instance | BindingFlags.NonPublic);

        Debug.Log(
            "SEMANTIC-ZOOM-09F29Q|Installed=True|HQ=" + HqStart.ToString("0") +
            "m|Medium=" + MediumStart.ToString("0") +
            "m|Operational=" + OperationalStart.ToString("0") +
            "m|Strategic=" + StrategicStart.ToString("0") +
            "m|NATO=I-II-III|HQPriority=True");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        bool strategic = cam.transform.position.y >= StrategicStart;
        if (strategic)
            ApplyEarlyStrategicSuppression();
        else if (qStrategicSuppressed)
            RestoreEarlyStrategicSuppression();
    }

    private void OnDisable()
    {
        if (qStrategicSuppressed)
            RestoreEarlyStrategicSuppression();
    }

    private void OnGUI()
    {
        if (cam == null)
            return;

        EnsureStyles();
        GUI.depth = -5100;
        float height = cam.transform.position.y;

        // F29D begins HQ counters at 72 m. Fill the close-to-medium gap so HQs become
        // readable earlier, without duplicating the established F29D presentation.
        if (height >= HqStart && height < 72f)
        {
            DrawEarlyHqCounters();
            DrawSelectedCompanyCounter();
        }

        // F29D begins all-company operational counters at 235 m. Q advances that handoff.
        if (height >= OperationalStart && height < F29DOperationalStart)
            DrawAllCompanyCounters();

        // F29D already draws Major counters above 72 m, but it does not give a selected
        // Major a gold frame. Add only the selection halo, not a duplicate HQ counter.
        if (height >= 72f)
            DrawSelectedMajorHalo();

        if (height >= MediumStart)
            DrawQViewIndicator(height);
    }

    private void DrawEarlyHqCounters()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int selectedMajor = GetSelectedBattalion(hierarchy);
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null)
                    continue;
                DrawHqCounter(
                    hq.transform,
                    "II",
                    "MAJOR " + (i == 0 ? "A" : "B"),
                    selectedMajor == i,
                    () => PrototypeOobNavigator09F29Q.Instance?.SelectMajorAndFocus(i, false));
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
        {
            DrawHqCounter(
                regimental.HqRoot.transform,
                "III",
                "OBERSTLØJTNANT",
                regimental.Selected,
                () => PrototypeOobNavigator09F29Q.Instance?.SelectRegimentalAndFocus(false));
        }
    }

    private void DrawHqCounter(Transform target, string echelon, string label, bool selected, System.Action click)
    {
        if (target == null || !TryProject(target.position + Vector3.up * 4f, out Vector2 anchor))
            return;

        Rect frame = new Rect(anchor.x - 35f, anchor.y - 49f, 70f, 36f);
        GUI.Box(frame, GUIContent.none, counterStyle);
        DrawBorder(frame, selected ? Selected : Friendly, selected ? 3f : 2f);
        GUI.Label(new Rect(frame.x, frame.y - 14f, frame.width, 12f), echelon, echelonStyle);
        GUI.Label(new Rect(frame.x, frame.y + 1f, frame.width, 23f), "HQ", symbolStyle);
        GUI.Label(new Rect(frame.center.x - 75f, frame.yMax, 150f, 13f), label, nameStyle);

        if (GUI.Button(frame, GUIContent.none, GUIStyle.none) && click != null)
            click();
    }

    private void DrawSelectedCompanyCounter()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.IsSelected && regiment.CurrentStrength > 0)
                DrawCompanyCounter(regiment);
        }
    }

    private void DrawAllCompanyCounters()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.CurrentStrength > 0)
                DrawCompanyCounter(regiment);
        }
    }

    private void DrawCompanyCounter(Regiment regiment)
    {
        if (!TryProject(regiment.transform.position + Vector3.up * 2.5f, out Vector2 anchor))
            return;

        Rect frame = new Rect(anchor.x - 25f, anchor.y - 43f, 50f, 30f);
        Color color = regiment.IsRouted
            ? Routed
            : regiment.IsSelected
                ? Selected
                : regiment.Team == BattleTeam.Denmark ? Friendly : Enemy;

        GUI.Box(frame, GUIContent.none, counterStyle);
        DrawBorder(frame, color, regiment.IsSelected ? 3f : 2f);
        GUI.Label(new Rect(frame.x, frame.y - 13f, frame.width, 11f), "I", echelonStyle);
        GUI.Label(new Rect(frame.x, frame.y + 1f, frame.width, 20f), "X", symbolStyle);

        string display = regiment.Team == BattleTeam.Denmark
            ? PrototypeUnitNames09F29C.Get(regiment)
            : EnemyDisplayName(regiment);
        GUI.Label(new Rect(frame.center.x - 67f, frame.yMax, 134f, 13f), display, nameStyle);

        if (GUI.Button(frame, GUIContent.none, GUIStyle.none) &&
            regiment.Team == BattleTeam.Denmark && PrototypeOobNavigator09F29Q.Instance != null)
        {
            PrototypeOobNavigator09F29Q.Instance.SelectCompanyAndFocus(regiment, false);
        }
    }

    private void DrawSelectedMajorHalo()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        int index = GetSelectedBattalion(hierarchy);
        if (index < 0 || hierarchy == null || !hierarchy.Installed)
            return;

        GameObject hq = hierarchy.GetMajorHq(index);
        if (hq == null || !TryProject(hq.transform.position + Vector3.up * 4f, out Vector2 anchor))
            return;

        Rect halo = new Rect(anchor.x - 38f, anchor.y - 54f, 76f, 43f);
        DrawBorder(halo, Selected, 3f);
    }

    private void DrawQViewIndicator(float height)
    {
        string level;
        if (height >= StrategicStart)
            level = "STRATEGIC";
        else if (height >= OperationalStart)
            level = "OPERATIONAL";
        else
            level = "MEDIUM";

        string text = "TACTICAL VIEW  " + level + "  |  Q  |  " + height.ToString("0") + "m";
        Rect rect = new Rect(Mathf.Max(8f, Screen.width - 250f), 39f, 242f, 22f);
        GUI.Box(rect, text, viewStyle);
    }

    private void ApplyEarlyStrategicSuppression()
    {
        PrototypeBattleVisuals09F7 f7 = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (!qStrategicSuppressed)
        {
            f7WasEnabled = f7 != null && f7.enabled;
            qStrategicSuppressed = true;
            Debug.Log("SEMANTIC-ZOOM-09F29Q|Level=STRATEGIC|Meshes=False|F7=False");
        }
        if (f7 != null)
            f7.enabled = false;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
            foreach (Regiment regiment in battle.Regiments)
                if (regiment != null)
                    SetMeshVisibility(regiment.gameObject, false);

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
            for (int i = 0; i < hierarchy.BattalionCount; i++)
                if (hierarchy.GetMajorHq(i) != null)
                    SetMeshVisibility(hierarchy.GetMajorHq(i), false);

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
            SetMeshVisibility(regimental.HqRoot, false);
    }

    private void RestoreEarlyStrategicSuppression()
    {
        qStrategicSuppressed = false;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
            foreach (Regiment regiment in battle.Regiments)
                if (regiment != null)
                    SetMeshVisibility(regiment.gameObject, true);

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
            for (int i = 0; i < hierarchy.BattalionCount; i++)
                if (hierarchy.GetMajorHq(i) != null)
                    SetMeshVisibility(hierarchy.GetMajorHq(i), true);

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
            SetMeshVisibility(regimental.HqRoot, true);

        PrototypeBattleVisuals09F7 f7 = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F7>();
        if (f7 != null && f7WasEnabled)
            f7.enabled = true;

        Debug.Log("SEMANTIC-ZOOM-09F29Q|Level=OPERATIONAL_OR_CLOSER|Meshes=True|F7=" + f7WasEnabled);
    }

    private static void SetMeshVisibility(GameObject root, bool visible)
    {
        if (root == null)
            return;
        MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++)
            if (meshes[i] != null)
                meshes[i].enabled = visible;
        ParticleSystemRenderer[] particles = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particles.Length; i++)
            if (particles[i] != null)
                particles[i].enabled = visible;
    }

    private int GetSelectedBattalion(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private bool TryProject(Vector3 world, out Vector2 guiPoint)
    {
        guiPoint = Vector2.zero;
        if (cam == null)
            return false;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0f)
            return false;
        guiPoint = new Vector2(screen.x, Screen.height - screen.y);
        return guiPoint.x >= -150f && guiPoint.x <= Screen.width + 150f &&
               guiPoint.y >= -100f && guiPoint.y <= Screen.height - BottomGuard + 90f;
    }

    private static string EnemyDisplayName(Regiment regiment)
    {
        switch (regiment.RegimentName)
        {
            case "8th Regiment": return "PR. 1. KOMPAGNI";
            case "18th Regiment": return "PR. 2. KOMPAGNI";
            default: return regiment.RegimentName.ToUpperInvariant();
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
        if (counterStyle != null)
            return;

        fillTexture = MakeTexture(Fill);
        whiteTexture = MakeTexture(Color.white);

        counterStyle = new GUIStyle(GUI.skin.box);
        counterStyle.normal.background = fillTexture;
        counterStyle.padding = new RectOffset(0, 0, 0, 0);

        symbolStyle = new GUIStyle(GUI.skin.label);
        symbolStyle.alignment = TextAnchor.MiddleCenter;
        symbolStyle.fontStyle = FontStyle.Bold;
        symbolStyle.fontSize = 18;
        symbolStyle.normal.textColor = Color.white;

        echelonStyle = new GUIStyle(symbolStyle);
        echelonStyle.fontSize = 10;

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.alignment = TextAnchor.MiddleCenter;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.fontSize = 9;
        nameStyle.normal.textColor = Color.white;

        viewStyle = new GUIStyle(GUI.skin.box);
        viewStyle.normal.background = fillTexture;
        viewStyle.normal.textColor = new Color(0.94f, 0.91f, 0.77f, 1f);
        viewStyle.alignment = TextAnchor.MiddleCenter;
        viewStyle.fontSize = 9;
        viewStyle.fontStyle = FontStyle.Bold;
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
