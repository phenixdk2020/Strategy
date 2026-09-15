using System.Collections.Generic;
using UnityEngine;

// v00.00.09f29d
// Semantic tactical zoom layer. Close zoom keeps the physical 3D battle readable;
// medium/operational zoom progressively adds screen-space NATO-style counters;
// strategic zoom hides company/HQ mesh renderers and keeps command/range LineRenderers,
// colliders and simulation state alive under a pure 2D tactical-symbol presentation.
[DefaultExecutionOrder(37100)]
public sealed class PrototypeSemanticZoom09F29D : MonoBehaviour
{
    private enum ZoomLevel
    {
        Close,
        Medium,
        Operational,
        Strategic
    }

    public static PrototypeSemanticZoom09F29D Instance { get; private set; }

    private const float HqMarkerStartHeight = 72f;
    private const float MediumStartHeight = 135f;
    private const float OperationalStartHeight = 235f;
    private const float StrategicStartHeight = 390f;
    private const float VeryFarHeight = 525f;
    private const float RenderRefreshSeconds = 0.80f;
    private const float BottomHudGuard = 96f;

    private static readonly Color Friendly = new Color(0.22f, 0.58f, 0.92f, 1f);
    private static readonly Color Enemy = new Color(0.86f, 0.24f, 0.20f, 1f);
    private static readonly Color Selected = new Color(1.00f, 0.80f, 0.18f, 1f);
    private static readonly Color Routed = new Color(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color CounterFill = new Color(0.035f, 0.045f, 0.040f, 0.90f);
    private static readonly Color CounterFillStrong = new Color(0.025f, 0.032f, 0.030f, 0.97f);
    private static readonly Color BarBack = new Color(0.10f, 0.11f, 0.10f, 0.96f);

    private Camera cam;
    private ZoomLevel currentLevel = ZoomLevel.Close;
    private bool levelInitialized;
    private bool meshesSuppressed;
    private float nextRenderRefresh;

    private Texture2D fillTexture;
    private Texture2D strongFillTexture;
    private Texture2D whiteTexture;
    private Texture2D barBackTexture;

    private GUIStyle counterStyle;
    private GUIStyle strongCounterStyle;
    private GUIStyle symbolStyle;
    private GUIStyle echelonStyle;
    private GUIStyle nameStyle;
    private GUIStyle statStyle;
    private GUIStyle viewStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSemanticZoom09F29D>() == null)
            new GameObject("PrototypeSemanticZoom_v000009f29d")
                .AddComponent<PrototypeSemanticZoom09F29D>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildStyles();
        Debug.Log(
            "SEMANTIC-ZOOM-09F29D|Installed=True|HQMarkerStart=" + HqMarkerStartHeight.ToString("0") +
            "|Medium=" + MediumStartHeight.ToString("0") +
            "|Operational=" + OperationalStartHeight.ToString("0") +
            "|Strategic=" + StrategicStartHeight.ToString("0") +
            "|Strategic3DMeshes=False|NATOCompany=I|NATOBattalion=II|NATORegiment=III");
    }

    private void OnDestroy()
    {
        RestoreMeshes();
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        RestoreMeshes();
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        ZoomLevel next = DetermineLevel(cam.transform.position.y);
        if (!levelInitialized || next != currentLevel)
        {
            ZoomLevel previous = currentLevel;
            currentLevel = next;
            levelInitialized = true;
            ApplyStrategicRendering(currentLevel == ZoomLevel.Strategic, true);
            Debug.Log(
                "SEMANTIC-ZOOM-09F29D|Level=" + currentLevel +
                "|Previous=" + previous +
                "|CameraHeight=" + cam.transform.position.y.ToString("0.0") +
                "|MeshesSuppressed=" + (currentLevel == ZoomLevel.Strategic));
        }

        // Newly spawned flags/companies/HQ pieces must inherit the strategic state.
        if (Time.unscaledTime >= nextRenderRefresh)
        {
            nextRenderRefresh = Time.unscaledTime + RenderRefreshSeconds;
            ApplyStrategicRendering(currentLevel == ZoomLevel.Strategic, false);
        }
    }

    private static ZoomLevel DetermineLevel(float height)
    {
        if (height >= StrategicStartHeight)
            return ZoomLevel.Strategic;
        if (height >= OperationalStartHeight)
            return ZoomLevel.Operational;
        if (height >= MediumStartHeight)
            return ZoomLevel.Medium;
        return ZoomLevel.Close;
    }

    private void OnGUI()
    {
        if (cam == null)
            return;

        BuildStyles();
        GUI.depth = -4700;

        float height = cam.transform.position.y;
        bool showHq = height >= HqMarkerStartHeight;
        bool showCompanies = currentLevel == ZoomLevel.Operational || currentLevel == ZoomLevel.Strategic;
        bool showSelectedCompanies = height >= HqMarkerStartHeight;

        if (showCompanies || showSelectedCompanies)
            DrawCompanyCounters(showCompanies);

        if (showHq)
            DrawHqCounters();

        if (currentLevel != ZoomLevel.Close)
            DrawViewIndicator(height);
    }

    private void DrawCompanyCounters(bool showAll)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.CurrentStrength <= 0)
                continue;

            bool selected = regiment.IsSelected;
            if (!showAll && !selected)
                continue;

            // At medium zoom, only selected companies get a counter. At operational
            // and strategic zoom, both sides are represented by NATO-style counters.
            DrawCompanyCounter(regiment, selected);
        }
    }

    private void DrawCompanyCounter(Regiment regiment, bool selected)
    {
        Vector3 world = regiment.transform.position + Vector3.up * 2.6f;
        if (!TryProject(world, out Vector2 anchor))
            return;

        float t = currentLevel == ZoomLevel.Strategic
            ? Mathf.InverseLerp(StrategicStartHeight, 600f, cam.transform.position.y)
            : 0f;

        float width = Mathf.Lerp(48f, 58f, t);
        float height = Mathf.Lerp(29f, 34f, t);
        Rect frame = AnchoredRect(anchor, width, height, 15f);
        if (!IsUsefulRect(frame))
            return;

        Color affiliation = regiment.IsRouted
            ? Routed
            : selected
                ? Selected
                : (regiment.Team == BattleTeam.Denmark ? Friendly : Enemy);

        GUI.Box(frame, GUIContent.none,
            currentLevel == ZoomLevel.Strategic ? strongCounterStyle : counterStyle);
        DrawBorder(frame, affiliation, selected ? 3f : 2f);

        Rect echelon = new Rect(frame.x, frame.y - 13f, frame.width, 12f);
        GUI.Label(echelon, "I", echelonStyle);

        Rect symbol = new Rect(frame.x + 3f, frame.y + 1f, frame.width - 6f, frame.height - 8f);
        GUI.Label(symbol, "X", symbolStyle);

        DrawStrengthBar(regiment, new Rect(frame.x + 4f, frame.yMax - 5f, frame.width - 8f, 3f), affiliation);
        DrawFacingArrow(regiment.transform.position, regiment.transform.forward, frame, affiliation);

        string display = DisplayCompanyName(regiment);
        float labelWidth = currentLevel == ZoomLevel.Strategic ? 154f : 136f;
        Rect nameRect = new Rect(frame.center.x - labelWidth * 0.5f, frame.yMax + 1f, labelWidth, 13f);
        GUI.Label(nameRect, display, nameStyle);

        if (currentLevel == ZoomLevel.Operational || currentLevel == ZoomLevel.Strategic)
        {
            int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
            string status = regiment.CurrentStrength + "/" + regiment.InitialStrength +
                            "  M" + regiment.Morale.ToString("0") +
                            (regiment.Team == BattleTeam.Denmark ? "  A" + ammo : string.Empty);
            if (regiment.IsRouted)
                status = "ROUT | " + status;

            Rect statRect = new Rect(frame.center.x - 82f, frame.yMax + 13f, 164f, 12f);
            GUI.Label(statRect, status, statStyle);
        }
    }

    private void DrawHqCounters()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null)
                    continue;

                IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(i);
                DrawHqCounter(
                    hq.transform.position,
                    "II",
                    (i + 1) + ". BATALJON | MAJOR " + (i == 0 ? "A" : "B"),
                    Aggregate(companies),
                    62f,
                    35f,
                    Friendly,
                    false);
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
        {
            DrawHqCounter(
                regimental.HqRoot.transform.position,
                "III",
                "1. REGIMENT | OBERSTLØJTNANT",
                AggregateRegiment(hierarchy),
                78f,
                40f,
                regimental.Selected ? Selected : Friendly,
                true);
        }
    }

    private void DrawHqCounter(
        Vector3 worldPosition,
        string echelon,
        string label,
        AggregateStats stats,
        float baseWidth,
        float baseHeight,
        Color frameColor,
        bool regimental)
    {
        Vector3 elevated = worldPosition + Vector3.up * (regimental ? 4.5f : 3.8f);
        if (!TryProject(elevated, out Vector2 anchor))
            return;

        float strategicScale = currentLevel == ZoomLevel.Strategic
            ? Mathf.Lerp(1.08f, 1.22f, Mathf.InverseLerp(StrategicStartHeight, 600f, cam.transform.position.y))
            : 1f;

        Rect frame = AnchoredRect(anchor, baseWidth * strategicScale, baseHeight * strategicScale, regimental ? 34f : 29f);
        if (!IsUsefulRect(frame))
            return;

        GUI.Box(frame, GUIContent.none, strongCounterStyle);
        DrawBorder(frame, frameColor, regimental ? 3f : 2f);

        GUI.Label(new Rect(frame.x, frame.y - 16f, frame.width, 14f), echelon, echelonStyle);
        GUI.Label(new Rect(frame.x + 2f, frame.y + 2f, frame.width - 4f, frame.height - 4f), "HQ", symbolStyle);

        DrawHqBeacon(worldPosition, frame, frameColor);

        float nameWidth = regimental ? 230f : 190f;
        GUI.Label(new Rect(frame.center.x - nameWidth * 0.5f, frame.yMax + 1f, nameWidth, 13f), label, nameStyle);

        if (stats.Count > 0 && (currentLevel == ZoomLevel.Operational || currentLevel == ZoomLevel.Strategic))
        {
            string status = stats.Current + "/" + stats.Initial +
                            "  M" + stats.Morale.ToString("0") +
                            "  A" + stats.Ammo.ToString("0");
            float statWidth = regimental ? 210f : 178f;
            GUI.Label(new Rect(frame.center.x - statWidth * 0.5f, frame.yMax + 14f, statWidth, 12f), status, statStyle);
        }
    }

    private void DrawHqBeacon(Vector3 worldPosition, Rect frame, Color color)
    {
        Vector3 groundScreen = cam.WorldToScreenPoint(worldPosition + Vector3.up * 0.2f);
        if (groundScreen.z <= 0f)
            return;

        Vector2 ground = new Vector2(groundScreen.x, Screen.height - groundScreen.y);
        Vector2 start = new Vector2(frame.center.x, frame.yMax);
        if (ground.y <= start.y + 2f)
            return;

        DrawLine(start, ground, color, 2f);
        DrawSolid(new Rect(ground.x - 3f, ground.y - 3f, 6f, 6f), color);
    }

    private void DrawFacingArrow(Vector3 worldPosition, Vector3 forward, Rect frame, Color color)
    {
        Vector3 a3 = cam.WorldToScreenPoint(worldPosition);
        Vector3 b3 = cam.WorldToScreenPoint(worldPosition + Flat(forward) * 22f);
        if (a3.z <= 0f || b3.z <= 0f)
            return;

        Vector2 direction = new Vector2(b3.x - a3.x, -(b3.y - a3.y));
        if (direction.sqrMagnitude < 0.1f)
            return;
        direction.Normalize();

        Vector2 start = frame.center + direction * (Mathf.Max(frame.width, frame.height) * 0.42f);
        Vector2 end = start + direction * 13f;
        DrawLine(start, end, color, 2f);

        Vector2 side = new Vector2(-direction.y, direction.x);
        DrawLine(end, end - direction * 5f + side * 3f, color, 2f);
        DrawLine(end, end - direction * 5f - side * 3f, color, 2f);
    }

    private void DrawStrengthBar(Regiment regiment, Rect rect, Color color)
    {
        DrawSolid(rect, BarBack);
        float fraction = regiment.InitialStrength > 0
            ? Mathf.Clamp01(regiment.CurrentStrength / (float)regiment.InitialStrength)
            : 0f;
        Rect fill = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
        DrawSolid(fill, color);
    }

    private void DrawViewIndicator(float height)
    {
        string text = "TACTICAL VIEW  " + currentLevel.ToString().ToUpperInvariant() +
                      "  |  ZOOM " + height.ToString("0") + "m";
        float width = 238f;
        GUI.Box(new Rect(Screen.width - width - 8f, 8f, width, 23f), text, viewStyle);
    }

    private void ApplyStrategicRendering(bool suppress, bool force)
    {
        if (!force && meshesSuppressed == suppress && !suppress)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment regiment in battle.Regiments)
            {
                if (regiment != null)
                    SetMeshVisibility(regiment.gameObject, !suppress);
            }
        }

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq != null)
                    SetMeshVisibility(hq, !suppress);
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
            SetMeshVisibility(regimental.HqRoot, !suppress);

        meshesSuppressed = suppress;
    }

    private void RestoreMeshes()
    {
        ApplyStrategicRendering(false, true);
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

    private bool TryProject(Vector3 world, out Vector2 guiPoint)
    {
        guiPoint = Vector2.zero;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0f)
            return false;

        guiPoint = new Vector2(screen.x, Screen.height - screen.y);
        return guiPoint.x >= -140f && guiPoint.x <= Screen.width + 140f &&
               guiPoint.y >= -90f && guiPoint.y <= Screen.height - BottomHudGuard + 90f;
    }

    private static Rect AnchoredRect(Vector2 anchor, float width, float height, float lift)
    {
        float y = anchor.y - lift - height;
        y = Mathf.Min(y, Screen.height - BottomHudGuard - height - 2f);
        return new Rect(anchor.x - width * 0.5f, y, width, height);
    }

    private static bool IsUsefulRect(Rect rect)
    {
        return rect.xMax >= -20f && rect.xMin <= Screen.width + 20f &&
               rect.yMax >= -20f && rect.yMin <= Screen.height - BottomHudGuard + 20f;
    }

    private static string DisplayCompanyName(Regiment regiment)
    {
        if (regiment == null)
            return "KOMPAGNI";

        if (regiment.Team == BattleTeam.Denmark)
            return PrototypeUnitNames09F29C.Get(regiment);

        // Current enemy entities are company-scale QA formations despite legacy
        // internal regiment identifiers. Keep the internal ids untouched for compatibility.
        switch (regiment.RegimentName)
        {
            case "8th Regiment": return "PR. 1. KOMPAGNI";
            case "18th Regiment": return "PR. 2. KOMPAGNI";
            default: return regiment.RegimentName.ToUpperInvariant();
        }
    }

    private struct AggregateStats
    {
        public int Count;
        public int Initial;
        public int Current;
        public float Morale;
        public float Ammo;
    }

    private static AggregateStats Aggregate(IReadOnlyList<Regiment> companies)
    {
        AggregateStats result = new AggregateStats();
        if (companies == null)
            return result;

        float morale = 0f;
        float ammo = 0f;
        for (int i = 0; i < companies.Count; i++)
        {
            Regiment regiment = companies[i];
            if (regiment == null)
                continue;

            result.Count++;
            result.Initial += regiment.InitialStrength;
            result.Current += regiment.CurrentStrength;
            morale += regiment.Morale;
            ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
        }

        if (result.Count > 0)
        {
            result.Morale = morale / result.Count;
            result.Ammo = ammo / result.Count;
        }
        return result;
    }

    private static AggregateStats AggregateRegiment(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        AggregateStats result = new AggregateStats();
        if (hierarchy == null || !hierarchy.Installed)
            return result;

        float morale = 0f;
        float ammo = 0f;
        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null)
                continue;

            for (int i = 0; i < companies.Count; i++)
            {
                Regiment regiment = companies[i];
                if (regiment == null)
                    continue;

                result.Count++;
                result.Initial += regiment.InitialStrength;
                result.Current += regiment.CurrentStrength;
                morale += regiment.Morale;
                ammo += PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
            }
        }

        if (result.Count > 0)
        {
            result.Morale = morale / result.Count;
            result.Ammo = ammo / result.Count;
        }
        return result;
    }

    private void BuildStyles()
    {
        if (counterStyle != null)
            return;

        fillTexture = MakeTexture(CounterFill, "SEMZOOM_COUNTER");
        strongFillTexture = MakeTexture(CounterFillStrong, "SEMZOOM_COUNTER_STRONG");
        whiteTexture = MakeTexture(Color.white, "SEMZOOM_WHITE");
        barBackTexture = MakeTexture(BarBack, "SEMZOOM_BARBACK");

        counterStyle = new GUIStyle(GUI.skin.box);
        counterStyle.normal.background = fillTexture;
        counterStyle.padding = new RectOffset(0, 0, 0, 0);
        counterStyle.border = new RectOffset(1, 1, 1, 1);

        strongCounterStyle = new GUIStyle(counterStyle);
        strongCounterStyle.normal.background = strongFillTexture;

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

        statStyle = new GUIStyle(nameStyle);
        statStyle.fontStyle = FontStyle.Normal;
        statStyle.fontSize = 8;
        statStyle.normal.textColor = new Color(0.88f, 0.88f, 0.82f, 1f);

        viewStyle = new GUIStyle(GUI.skin.box);
        viewStyle.normal.background = strongFillTexture;
        viewStyle.normal.textColor = new Color(0.92f, 0.90f, 0.80f, 1f);
        viewStyle.alignment = TextAnchor.MiddleCenter;
        viewStyle.fontSize = 9;
        viewStyle.fontStyle = FontStyle.Bold;
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
        if (rect.width <= 0f || rect.height <= 0f)
            return;
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture != null ? whiteTexture : Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
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
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width),
            whiteTexture != null ? whiteTexture : Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
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
