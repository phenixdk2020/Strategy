using UnityEngine;

// v00.00.09h TEST uniform designer.
// F10 opens a compact per-regiment colour editor. Colours are presentation-only and
// can be saved locally through PlayerPrefs for QA/customisation testing.
[DefaultExecutionOrder(31000)]
public sealed class PrototypeUniformDesigner09H : MonoBehaviour
{
    public static PrototypeUniformDesigner09H Instance { get; private set; }

    private static readonly string[] PartLabels =
    {
        "Frakke", "Bukser", "Hovedbekl.", "Trim", "Remme", "Udstyr",
        "Fane 1", "Fane 2", "Bånd"
    };

    private bool open;
    private Rect windowRect = new Rect(18f, 112f, 330f, 470f);
    private Regiment selected;
    private PrototypeUniformProfile09H working;
    private int selectedPart;
    private Vector2 scroll;
    private GUIStyle titleStyle;
    private GUIStyle smallStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeUniformDesigner09H>() != null)
            return;

        GameObject root = new GameObject("PrototypeUniformDesigner_v000009h");
        root.AddComponent<PrototypeUniformDesigner09H>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
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
        if (Input.GetKeyDown(KeyCode.F10))
        {
            open = !open;
            RefreshSelection(true);
        }

        if (open)
            RefreshSelection(false);
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (!open)
            return false;

        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return windowRect.Contains(guiPoint);
    }

    private void RefreshSelection(bool force)
    {
        Regiment current = FindSingleSelectedRegiment();
        if (!force && current == selected)
            return;

        selected = current;
        PrototypeSoldierVisualPass09H visual = PrototypeSoldierVisualPass09H.Instance;
        working = selected != null && visual != null
            ? visual.GetProfileCopy(selected)
            : null;
    }

    private static Regiment FindSingleSelectedRegiment()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        Regiment found = null;
        int count = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !regiment.IsSelected)
                continue;

            found = regiment;
            count++;
            if (count > 1)
                return null;
        }

        return count == 1 ? found : null;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 12;
        titleStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 9;
        smallStyle.wordWrap = true;
        smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.86f);
    }

    private void OnGUI()
    {
        if (!open)
            return;

        EnsureStyles();
        GUI.depth = -2500;
        windowRect = GUI.Window(91964, windowRect, DrawWindow, "UNIFORM DESIGNER 09H");
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 96f, Mathf.Max(96f, Screen.height - 44f));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        if (selected == null || working == null)
        {
            GUILayout.Label("Vælg præcis ét regiment for at redigere uniformen.", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                "F10 lukker vinduet. Multi-select kan stadig bruges normalt, men farver redigeres én enhed ad gangen.",
                smallStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("LUK"))
                open = false;
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
            return;
        }

        string team = selected.Team == BattleTeam.Denmark ? "Danmark" : "Preussen";
        GUILayout.Label(selected.RegimentName + " | " + team, titleStyle);
        GUILayout.Label("Farver ændrer kun grafik - aldrig stats, movement eller combat.", smallStyle);
        GUILayout.Space(5f);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(300f));

        GUILayout.Label("Uniform-/fanedel", titleStyle);
        int newPart = GUILayout.SelectionGrid(selectedPart, PartLabels, 3, GUILayout.Height(82f));
        if (newPart != selectedPart)
            selectedPart = newPart;

        GUILayout.Space(8f);
        Color color = GetSelectedColor(working, selectedPart);

        Color oldGuiColor = GUI.color;
        GUI.color = color;
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(30f));
        GUI.color = oldGuiColor;

        float r = DrawChannel("R", color.r);
        float g = DrawChannel("G", color.g);
        float b = DrawChannel("B", color.b);

        Color edited = new Color(r, g, b, 1f);
        if (edited != color)
        {
            SetSelectedColor(working, selectedPart, edited);
            ApplyWorking();
        }

        GUILayout.Label(
            string.Format("RGB {0:0} / {1:0} / {2:0}", edited.r * 255f, edited.g * 255f, edited.b * 255f),
            smallStyle);

        GUILayout.EndScrollView();
        GUILayout.Space(5f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("GEM"))
        {
            ApplyWorking();
            PrototypeSoldierVisualPass09H.Instance.SaveProfile(selected);
        }
        if (GUILayout.Button("INDLÆS"))
        {
            if (PrototypeSoldierVisualPass09H.Instance.LoadSavedProfile(selected))
                working = PrototypeSoldierVisualPass09H.Instance.GetProfileCopy(selected);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("REGIMENT STANDARD"))
        {
            PrototypeSoldierVisualPass09H.Instance.ResetRegimentDefault(selected);
            working = PrototypeSoldierVisualPass09H.Instance.GetProfileCopy(selected);
        }
        if (GUILayout.Button("FACTION STANDARD"))
        {
            PrototypeSoldierVisualPass09H.Instance.ResetFactionDefault(selected);
            working = PrototypeSoldierVisualPass09H.Instance.GetProfileCopy(selected);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(3f);
        if (GUILayout.Button("LUK [F10]"))
            open = false;

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private static float DrawChannel(string label, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(18f));
        float result = GUILayout.HorizontalSlider(value, 0f, 1f);
        GUILayout.Label(Mathf.RoundToInt(result * 255f).ToString(), GUILayout.Width(32f));
        GUILayout.EndHorizontal();
        return result;
    }

    private void ApplyWorking()
    {
        PrototypeSoldierVisualPass09H visual = PrototypeSoldierVisualPass09H.Instance;
        if (selected != null && working != null && visual != null)
            visual.ApplyProfile(selected, working);
    }

    private static Color GetSelectedColor(PrototypeUniformProfile09H profile, int index)
    {
        switch (index)
        {
            case 0: return profile.CoatColor;
            case 1: return profile.TrouserColor;
            case 2: return profile.HeadgearColor;
            case 3: return profile.TrimColor;
            case 4: return profile.StrapColor;
            case 5: return profile.EquipmentColor;
            case 6: return profile.FlagPrimaryColor;
            case 7: return profile.FlagSecondaryColor;
            default: return profile.RibbonColor;
        }
    }

    private static void SetSelectedColor(PrototypeUniformProfile09H profile, int index, Color color)
    {
        switch (index)
        {
            case 0: profile.CoatColor = color; break;
            case 1: profile.TrouserColor = color; break;
            case 2: profile.HeadgearColor = color; break;
            case 3: profile.TrimColor = color; break;
            case 4: profile.StrapColor = color; break;
            case 5: profile.EquipmentColor = color; break;
            case 6: profile.FlagPrimaryColor = color; break;
            case 7: profile.FlagSecondaryColor = color; break;
            default: profile.RibbonColor = color; break;
        }
    }
}
