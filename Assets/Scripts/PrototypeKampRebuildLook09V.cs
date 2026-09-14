using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(20100)]
public sealed class PrototypeKampRebuildLook09V : MonoBehaviour
{
    public static bool HideLegacyBottomBar = true;
    public static PrototypeKampRebuildLook09V Instance { get; private set; }

    private Texture2D panelTex;
    private Texture2D goldTex;
    private Texture2D portraitTex;
    private Texture2D barBackTex;
    private Texture2D barGreenTex;
    private Texture2D barAmberTex;
    private Texture2D defendTex;
    private Texture2D attackTex;
    private Texture2D captureTex;
    private Texture2D buttonTex;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle tinyButton;
    private bool showFlags = true;
    private bool showMarking = true;
    private LineRenderer selectionBox;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampRebuildLook09V>() != null)
            return;
        new GameObject("PrototypeKampRebuildLook_v000009f19L2").AddComponent<PrototypeKampRebuildLook09V>();
    }

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        Debug.Log("LOOK-09V|Dock=Bottom|Scale=0.60|ReplaceLegacyHUD=True");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        ApplyFlagVisibility();
        UpdateSelectionMarking();
    }

    public static bool ContainsPointer(Vector3 mousePosition)
    {
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return UnitCardRect().Contains(gui) || CommandRect().Contains(gui);
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
            selectionBox.widthMultiplier = 0.16f;
            selectionBox.shadowCastingMode = ShadowCastingMode.Off;
            selectionBox.positionCount = 4;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                selectionBox.sharedMaterial = new Material(shader) { color = new Color(1f, 0.82f, 0.18f, 0.95f) };
        }

        Regiment selected = GetSelectedDanish();
        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        bool majorOn = major != null && major.Selected;
        selectionBox.enabled = showMarking && selected != null && !majorOn;
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
        buttonTex = Solid(new Color(0.16f, 0.17f, 0.14f, 1f));
        portraitTex = MakePortrait();

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTex;
        panelStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 11;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 9;
        labelStyle.normal.textColor = new Color(0.93f, 0.90f, 0.78f);
        labelStyle.alignment = TextAnchor.MiddleLeft;

        mutedStyle = new GUIStyle(labelStyle);
        mutedStyle.fontSize = 8;
        mutedStyle.normal.textColor = new Color(0.72f, 0.70f, 0.60f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 9;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = new Color(0.95f, 0.93f, 0.82f);
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.normal.background = buttonTex;
        buttonStyle.padding = new RectOffset(2, 2, 1, 1);

        tinyButton = new GUIStyle(buttonStyle);
        tinyButton.fontSize = 8;
    }

    private static float DockWidth()
    {
        return Mathf.Clamp(Screen.width * 0.60f, 520f, 760f);
    }

    private static float DockHeight()
    {
        return 90f;
    }

    private static Rect DockRect()
    {
        float w = DockWidth();
        float h = DockHeight();
        return new Rect((Screen.width - w) * 0.5f, Screen.height - h, w, h);
    }

    public static Rect UnitCardRect()
    {
        Rect dock = DockRect();
        return new Rect(dock.x, dock.y, dock.width * 0.36f, dock.height);
    }

    public static Rect CommandRect()
    {
        Rect dock = DockRect();
        float left = dock.width * 0.36f + 4f;
        return new Rect(dock.x + left, dock.y, dock.width - left, dock.height);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -920;
        DrawCompass();
        DrawMinimap();
        DrawUnitCard();
        DrawCommandPanel();
        BlockPointerOnHud();
    }

    private void DrawCompass()
    {
        Rect box = new Rect(12f, 12f, 46f, 46f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f), goldTex);
        GUI.Label(new Rect(box.x + 16f, box.y + 2f, 16f, 12f), "N", mutedStyle);
        GUI.Label(new Rect(box.x + 16f, box.yMax - 14f, 16f, 12f), "S", mutedStyle);
        GUI.Label(new Rect(box.x + 4f, box.y + 16f, 16f, 12f), "W", mutedStyle);
        GUI.Label(new Rect(box.xMax - 14f, box.y + 16f, 16f, 12f), "E", mutedStyle);
    }

    private void DrawMinimap()
    {
        Rect box = new Rect(Screen.width - 122f, 10f, 110f, 72f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f), goldTex);
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return;
        Rect inner = new Rect(box.x + 6f, box.y + 6f, box.width - 12f, box.height - 12f);
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null) continue;
            float nx = Mathf.InverseLerp(-1440f, 1440f, regiment.transform.position.x);
            float nz = Mathf.InverseLerp(-960f, 960f, regiment.transform.position.z);
            float px = inner.x + nx * inner.width;
            float py = inner.yMax - nz * inner.height;
            Texture2D color = regiment.Team == BattleTeam.Denmark ? (regiment.IsSelected ? goldTex : defendTex) : attackTex;
            GUI.DrawTexture(new Rect(px - 3f, py - 2f, 6f, 4f), color);
        }
    }

    private void DrawUnitCard()
    {
        Rect panel = UnitCardRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), goldTex);
        GUI.DrawTexture(new Rect(panel.x + 6f, panel.y + 8f, 42f, 54f), portraitTex);

        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        bool majorOn = major != null && major.Selected;
        Regiment selected = GetSelectedDanish();

        string name = majorOn ? "Bataljons HQ" : selected != null ? selected.RegimentName : "Ingen enhed";
        string kind = majorOn ? "Major" : selected != null ? "Infanterikompagni" : "—";
        int cur = 0;
        int init = 0;
        float morale = 0f;
        float cohesion = 0f;
        if (majorOn && major.Companies != null)
        {
            foreach (Regiment company in major.Companies)
            {
                if (company == null) continue;
                cur += company.CurrentStrength;
                init += company.InitialStrength;
                morale += company.Morale;
                cohesion += company.Cohesion;
            }
            int n = Mathf.Max(1, major.Companies.Count);
            morale /= n;
            cohesion /= n;
        }
        else if (selected != null)
        {
            cur = selected.CurrentStrength;
            init = selected.InitialStrength;
            morale = selected.Morale;
            cohesion = selected.Cohesion;
        }

        GUI.Label(new Rect(panel.x + 54f, panel.y + 4f, panel.width - 60f, 16f), name, titleStyle);
        GUI.Label(new Rect(panel.x + 54f, panel.y + 18f, panel.width - 60f, 14f), kind, mutedStyle);
        GUI.Label(new Rect(panel.x + 54f, panel.y + 32f, panel.width - 60f, 14f), cur + "/" + init + "  tab " + Mathf.Max(0, init - cur), labelStyle);
        DrawBar(new Rect(panel.x + 54f, panel.y + 52f, panel.width - 64f, 7f), "Moral", morale / 100f, barGreenTex);
        DrawBar(new Rect(panel.x + 54f, panel.y + 68f, panel.width - 64f, 7f), "Kohæsion", cohesion / 100f, barAmberTex);
    }

    private void DrawBar(Rect rect, string label, float value, Texture2D fill)
    {
        GUI.Label(new Rect(rect.x, rect.y - 10f, 80f, 10f), label, mutedStyle);
        GUI.DrawTexture(rect, barBackTex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), fill);
    }

    private void DrawCommandPanel()
    {
        Rect panel = CommandRect();
        GUI.Box(panel, string.Empty, panelStyle);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), goldTex);

        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major != null && major.Selected)
            DrawMajorCommands(panel, major);
        else
            DrawCompanyCommands(panel);
    }

    private void DrawMajorCommands(Rect panel, PrototypeMajorBattalion09F18 major)
    {
        GUI.Label(new Rect(panel.x + 6f, panel.y + 4f, 160f, 14f), "Major", titleStyle);
        float x = panel.x + 6f;
        float y = panel.y + 20f;
        if (Tiny(ref x, y, 48f, major.AIEnabled ? "AI ON" : "AI OFF")) major.ToggleAI();
        if (Tiny(ref x, y, 36f, major.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF")) major.SetDoctrine(OfficerAIDoctrine.Defensive);
        if (Tiny(ref x, y, 36f, major.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL")) major.SetDoctrine(OfficerAIDoctrine.Balanced);
        if (Tiny(ref x, y, 36f, major.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF")) major.SetDoctrine(OfficerAIDoctrine.Offensive);

        PrototypeMajorUi09F18 ui = PrototypeMajorUi09F18.Instance;
        x = panel.x + 6f;
        y = panel.y + 42f;
        float bw = (panel.width - 18f) / 3f;
        if (GUI.Button(new Rect(x, y, bw - 3f, 20f), "ANGRIB HER", tinyButton) && ui != null) ui.QueueOrder(MajorOrder09F18.AttackHere);
        if (GUI.Button(new Rect(x + bw, y, bw - 3f, 20f), "FORSVAR HER", tinyButton) && ui != null) ui.QueueOrder(MajorOrder09F18.DefendHere);
        if (GUI.Button(new Rect(x + bw * 2f, y, bw - 3f, 20f), "TILBAGETRÆK", tinyButton) && ui != null) ui.QueueOrder(MajorOrder09F18.WithdrawHere);
        y += 22f;
        if (GUI.Button(new Rect(x, y, bw - 3f, 20f), "RYK FREM", tinyButton) && ui != null) ui.QueueOrder(MajorOrder09F18.AdvanceHere);
        if (GUI.Button(new Rect(x + bw, y, bw - 3f, 20f), "HOLD", tinyButton) && major.HqRoot != null) major.IssueOrder(MajorOrder09F18.HoldPosition, major.HqRoot.transform.position);
        if (GUI.Button(new Rect(x + bw * 2f, y, bw - 3f, 20f), "SAML HER", tinyButton) && ui != null) ui.QueueOrder(MajorOrder09F18.AssembleHere);
    }

    private void DrawCompanyCommands(Rect panel)
    {
        Regiment selected = GetSelectedDanish();
        OfficerAIController controller = selected != null ? selected.GetComponent<OfficerAIController>() : null;
        GUI.Label(new Rect(panel.x + 6f, panel.y + 4f, 180f, 14f), "Kompagni", titleStyle);

        float x = panel.x + 6f;
        float y = panel.y + 20f;
        if (controller != null)
        {
            if (Tiny(ref x, y, 48f, controller.AIEnabled ? "AI ON" : "AI OFF"))
                ForEachSelected((r, c) => c.SetAIEnabled(!controller.AIEnabled));
            if (Tiny(ref x, y, 36f, controller.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF"))
                ForEachSelected((r, c) => c.SetDoctrine(OfficerAIDoctrine.Defensive));
            if (Tiny(ref x, y, 36f, controller.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL"))
                ForEachSelected((r, c) => c.SetDoctrine(OfficerAIDoctrine.Balanced));
            if (Tiny(ref x, y, 36f, controller.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF"))
                ForEachSelected((r, c) => c.SetDoctrine(OfficerAIDoctrine.Offensive));
        }

        x = panel.x + 6f;
        y = panel.y + 42f;
        if (selected != null)
        {
            if (Tiny(ref x, y, 44f, Mark(selected.FirePolicy == RegimentFirePolicy.HoldFire, "HOLD")))
                ForEachSelected((r, c) => r.SetFirePolicy(RegimentFirePolicy.HoldFire));
            if (Tiny(ref x, y, 44f, Mark(selected.FirePolicy == RegimentFirePolicy.CloseRange, "CLOSE")))
                ForEachSelected((r, c) => r.SetFirePolicy(RegimentFirePolicy.CloseRange));
            if (Tiny(ref x, y, 40f, Mark(selected.FirePolicy == RegimentFirePolicy.MediumRange, "MED")))
                ForEachSelected((r, c) => r.SetFirePolicy(RegimentFirePolicy.MediumRange));
            if (Tiny(ref x, y, 44f, Mark(selected.FirePolicy == RegimentFirePolicy.LongRange, "LONG")))
                ForEachSelected((r, c) => r.SetFirePolicy(RegimentFirePolicy.LongRange));
        }

        x = panel.x + 6f;
        y = panel.y + 64f;
        if (Tiny(ref x, y, 52f, "<15" ) && PlayerCommander.Instance != null) PlayerCommander.Instance.RotateSelectedFacing(-15f);
        if (Tiny(ref x, y, 52f, "15>" ) && PlayerCommander.Instance != null) PlayerCommander.Instance.RotateSelectedFacing(15f);
        if (Tiny(ref x, y, 70f, "FORSVAR")) BeginOrder(PrototypeTacticalOrderMode09F4.DefendHere);
        if (Tiny(ref x, y, 60f, "ANGRIB")) BeginOrder(PrototypeTacticalOrderMode09F4.Attack);
        if (Tiny(ref x, y, 60f, "EROBR")) BeginOrder(PrototypeTacticalOrderMode09F4.CaptureHere);
        showFlags = GUI.Toggle(new Rect(panel.xMax - 148f, panel.y + 6f, 70f, 16f), showFlags, "Faner");
        showMarking = GUI.Toggle(new Rect(panel.xMax - 74f, panel.y + 6f, 68f, 16f), showMarking, "Markér");
    }

    private bool Tiny(ref float x, float y, float w, string label)
    {
        bool hit = GUI.Button(new Rect(x, y, w, 18f), label, tinyButton);
        x += w + 3f;
        return hit;
    }

    private static string Mark(bool on, string label)
    {
        return on ? "[" + label + "]" : label;
    }

    private static void BeginOrder(PrototypeTacticalOrderMode09F4 mode)
    {
        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders != null) orders.BeginOrder(mode);
    }

    private static void ForEachSelected(System.Action<Regiment, OfficerAIController> action)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null) return;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || !regiment.IsSelected) continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null) action(regiment, controller);
        }
    }

    private void BlockPointerOnHud()
    {
        bool over = ContainsPointer(Input.mousePosition);
        PlayerCommander commander = PlayerCommander.Instance;
        if (over)
        {
            if (commander != null && commander.enabled) commander.enabled = false;
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
