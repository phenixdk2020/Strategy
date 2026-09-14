using UnityEngine;
using UnityEngine.Rendering;

// Presentation-only 1864 look pack for strategi/kampe_rebuild.
// Does not own movement, fire, AI or selection rules.
[DefaultExecutionOrder(20100)]
public sealed class PrototypeKampRebuildLook09V : MonoBehaviour
{
    public static bool HideLegacyBottomBar = true;

    private Texture2D panelTex;
    private Texture2D goldTex;
    private Texture2D portraitTex;
    private Texture2D barBackTex;
    private Texture2D barGreenTex;
    private Texture2D barAmberTex;
    private Texture2D defendTex;
    private Texture2D attackTex;
    private Texture2D captureTex;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private bool showFlags = true;
    private bool showMarking = true;
    private LineRenderer selectionBox;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampRebuildLook09V>() != null)
            return;
        new GameObject("PrototypeKampRebuildLook_v000009f19L").AddComponent<PrototypeKampRebuildLook09V>();
    }

    private void Awake()
    {
        cam = Camera.main;
        Debug.Log("LOOK-09V|Installed=True|Scope=PresentationOnly");
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        ApplyFlagVisibility();
        UpdateSelectionMarking();
    }

    private void ApplyFlagVisibility()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null) continue;
            Transform flagsRoot = regiment.transform.Find("Flags09F4_" + regiment.RegimentName);
            if (flagsRoot != null && flagsRoot.gameObject.activeSelf != showFlags)
                flagsRoot.gameObject.SetActive(showFlags);
        }
    }

    private void UpdateSelectionMarking()
    {
        if (selectionBox == null)
        {
            GameObject go = new GameObject("Look09V_Selection");
            go.transform.SetParent(transform, false);
            selectionBox = go.AddComponent<LineRenderer>();
            selectionBox.useWorldSpace = true;
            selectionBox.loop = true;
            selectionBox.widthMultiplier = 0.18f;
            selectionBox.shadowCastingMode = ShadowCastingMode.Off;
            selectionBox.positionCount = 4;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                selectionBox.sharedMaterial = new Material(shader) { color = new Color(1f, 0.82f, 0.18f, 0.95f) };
        }

        Regiment selected = GetSelectedDanish();
        selectionBox.enabled = showMarking && selected != null;
        if (!selectionBox.enabled) return;

        float halfW = 9.5f;
        float halfD = 4.2f;
        Vector3 c = selected.transform.position;
        Vector3 f = selected.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        Vector3[] corners =
        {
            c - r * halfW - f * halfD,
            c - r * halfW + f * halfD,
            c + r * halfW + f * halfD,
            c + r * halfW - f * halfD
        };
        for (int i = 0; i < 4; i++)
        {
            corners[i].y = PrototypeBootstrap.SampleGroundHeight(corners[i].x, corners[i].z) + 0.14f;
            selectionBox.SetPosition(i, corners[i]);
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelTex = Solid(new Color(0.07f, 0.08f, 0.07f, 0.94f));
        goldTex = Solid(new Color(0.72f, 0.58f, 0.28f, 1f));
        barBackTex = Solid(new Color(0.16f, 0.17f, 0.14f, 1f));
        barGreenTex = Solid(new Color(0.28f, 0.72f, 0.28f, 1f));
        barAmberTex = Solid(new Color(0.82f, 0.68f, 0.18f, 1f));
        defendTex = Solid(new Color(0.18f, 0.32f, 0.38f, 1f));
        attackTex = Solid(new Color(0.48f, 0.16f, 0.16f, 1f));
        captureTex = Solid(new Color(0.16f, 0.32f, 0.20f, 1f));
        portraitTex = MakePortrait();

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTex;
        panelStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 13;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 11;
        labelStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);
        labelStyle.alignment = TextAnchor.MiddleLeft;

        mutedStyle = new GUIStyle(labelStyle);
        mutedStyle.fontSize = 10;
        mutedStyle.normal.textColor = new Color(0.72f, 0.70f, 0.60f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = new Color(0.95f, 0.93f, 0.82f);
        buttonStyle.hover.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -900;
        DrawCompass();
        DrawMinimap();
        DrawUnitCard();
        DrawKampPanel();
        BlockPointerOnHud();
    }

    private void DrawCompass()
    {
        Rect box = new Rect(18f, 18f, 74f, 74f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f), goldTex);
        GUI.Label(new Rect(box.x + 28f, box.y + 4f, 20f, 16f), "N", titleStyle);
        GUI.Label(new Rect(box.x + 28f, box.yMax - 20f, 20f, 16f), "S", mutedStyle);
        GUI.Label(new Rect(box.x + 6f, box.y + 28f, 20f, 16f), "W", mutedStyle);
        GUI.Label(new Rect(box.xMax - 20f, box.y + 28f, 20f, 16f), "E", mutedStyle);
    }

    private void DrawMinimap()
    {
        Rect box = new Rect(Screen.width - 188f, 16f, 172f, 118f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f), goldTex);
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return;
        const float worldW = 2880f;
        const float worldD = 1920f;
        Rect inner = new Rect(box.x + 8f, box.y + 8f, box.width - 16f, box.height - 16f);
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null) continue;
            float nx = Mathf.InverseLerp(-worldW * 0.5f, worldW * 0.5f, regiment.transform.position.x);
            float nz = Mathf.InverseLerp(-worldD * 0.5f, worldD * 0.5f, regiment.transform.position.z);
            float px = inner.x + nx * inner.width;
            float py = inner.yMax - nz * inner.height;
            Texture2D color = regiment.Team == BattleTeam.Denmark ? (regiment.IsSelected ? goldTex : defendTex) : attackTex;
            GUI.DrawTexture(new Rect(px - 4f, py - 3f, 8f, 6f), color);
        }
    }

    private static Rect UnitCardRect()
    {
        return new Rect(16f, Screen.height - 168f, 360f, 150f);
    }

    private static Rect KampPanelRect()
    {
        float x = 384f;
        float w = Mathf.Min(620f, Screen.width - x - 16f);
        return new Rect(x, Screen.height - 168f, w, 150f);
    }

    private void DrawUnitCard()
    {
        Rect panel = UnitCardRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), goldTex);
        GUI.DrawTexture(new Rect(panel.x + 10f, panel.y + 14f, 78f, 96f), portraitTex);

        Regiment selected = GetSelectedDanish();
        string name = selected != null ? selected.RegimentName : "Ingen enhed valgt";
        string kind = selected != null ? "Infanterikompagni" : "—";
        int cur = selected != null ? selected.CurrentStrength : 0;
        int init = selected != null ? selected.InitialStrength : 0;
        int dead = Mathf.Max(0, init - cur);
        float ammo = 1f;
        float morale = selected != null ? Mathf.Clamp01(selected.Morale / 100f) : 0f;
        float fatigue = selected != null ? Mathf.Clamp01(1f - selected.Cohesion / 100f) : 0f;

        GUI.Label(new Rect(panel.x + 98f, panel.y + 10f, 250f, 22f), name, titleStyle);
        GUI.Label(new Rect(panel.x + 98f, panel.y + 30f, 250f, 18f), kind, mutedStyle);
        GUI.Label(new Rect(panel.x + 98f, panel.y + 50f, 250f, 18f),
            cur + " / " + init + "     tab " + dead + "     ammo " + Mathf.RoundToInt(ammo * 100f) + " %", labelStyle);

        DrawBar(new Rect(panel.x + 98f, panel.y + 78f, 246f, 12f), "Moral", morale, barGreenTex);
        DrawBar(new Rect(panel.x + 98f, panel.y + 98f, 246f, 12f), "Ammunition", ammo, barGreenTex);
        DrawBar(new Rect(panel.x + 98f, panel.y + 118f, 246f, 12f), "Træthed", fatigue, barAmberTex);
    }

    private void DrawBar(Rect rect, string label, float value, Texture2D fill)
    {
        GUI.Label(new Rect(rect.x, rect.y - 12f, 120f, 14f), label, mutedStyle);
        GUI.DrawTexture(rect, barBackTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), fill);
    }

    private void DrawKampPanel()
    {
        Rect panel = KampPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), goldTex);
        GUIStyle centered = new GUIStyle(titleStyle);
        centered.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width, 22f), "Kamp (F9)", centered);

        GUI.Label(new Rect(panel.x + 16f, panel.y + 36f, 130f, 22f), "Vis soldater (skala)", mutedStyle);
        if (GUI.Button(new Rect(panel.x + 150f, panel.y + 34f, 70f, 22f), "1 : " + PrototypeBattleVisuals09F4.SoldierDisplayRatio, buttonStyle))
            PrototypeBattleVisuals09F4.CycleSoldierDisplayRatio();

        GUI.Label(new Rect(panel.x + 16f, panel.y + 62f, 130f, 22f), "Vis faldne (skala)", mutedStyle);
        if (GUI.Button(new Rect(panel.x + 150f, panel.y + 60f, 70f, 22f), "1 : " + PrototypeBattleVisuals09F4.CasualtyDisplayRatio, buttonStyle))
            PrototypeBattleVisuals09F4.CycleCasualtyDisplayRatio();

        showFlags = GUI.Toggle(new Rect(panel.x + 16f, panel.y + 90f, 140f, 20f), showFlags, " Vis faner");
        showMarking = GUI.Toggle(new Rect(panel.x + 16f, panel.y + 112f, 180f, 20f), showMarking, " Vis enhedsmarkering");

        float bx = panel.xMax - 214f;
        float by = panel.y + 36f;
        buttonStyle.normal.background = defendTex;
        if (GUI.Button(new Rect(bx, by, 198f, 28f), "Forsvar her", buttonStyle))
            BeginOrder(PrototypeTacticalOrderMode09F4.DefendHere);
        buttonStyle.normal.background = attackTex;
        if (GUI.Button(new Rect(bx, by + 34f, 198f, 28f), "Angrib", buttonStyle))
            BeginOrder(PrototypeTacticalOrderMode09F4.Attack);
        buttonStyle.normal.background = captureTex;
        if (GUI.Button(new Rect(bx, by + 68f, 198f, 28f), "Erobr her", buttonStyle))
            BeginOrder(PrototypeTacticalOrderMode09F4.CaptureHere);
        buttonStyle.normal.background = panelTex;
    }

    private static void BeginOrder(PrototypeTacticalOrderMode09F4 mode)
    {
        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders != null)
            orders.BeginOrder(mode);
    }

    private void BlockPointerOnHud()
    {
        Vector2 gui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        bool over = UnitCardRect().Contains(gui) || KampPanelRect().Contains(gui);
        PlayerCommander commander = PlayerCommander.Instance;
        if (over)
        {
            if (commander != null && commander.enabled)
                commander.enabled = false;
        }
        else if (commander != null && !commander.enabled)
            commander.enabled = true;
    }

    private static Regiment GetSelectedDanish()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return null;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                return regiment;
        }
        return null;
    }

    private static Texture2D Solid(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D MakePortrait()
    {
        Texture2D texture = new Texture2D(64, 80, TextureFormat.RGBA32, false);
        Color coat = new Color(0.12f, 0.18f, 0.32f);
        Color skin = new Color(0.76f, 0.58f, 0.46f);
        Color shako = new Color(0.08f, 0.08f, 0.09f);
        Color red = new Color(0.62f, 0.10f, 0.12f);
        for (int y = 0; y < 80; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color c = coat;
                if (y > 52) c = shako;
                else if (y > 34 && x > 16 && x < 48) c = skin;
                if (y > 18 && y < 28) c = red;
                texture.SetPixel(x, y, c);
            }
        }
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
