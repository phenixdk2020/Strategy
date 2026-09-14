using UnityEngine;

// Lightweight IMGUI theme used by the v00.00.09f15 HQ panel.
// It intentionally stays procedural so the prototype has no external UI asset dependency.
public static class PrototypeUiTheme09F15
{
    private static Texture2D panelTexture;
    private static Texture2D panelHeaderTexture;
    private static Texture2D buttonTexture;
    private static Texture2D buttonHoverTexture;
    private static Texture2D buttonActiveTexture;
    private static Texture2D accentTexture;

    public static readonly Color Text = new Color(0.94f, 0.92f, 0.82f);
    public static readonly Color MutedText = new Color(0.72f, 0.72f, 0.66f);
    public static readonly Color Accent = new Color(0.78f, 0.65f, 0.29f);
    public static readonly Color Attack = new Color(0.72f, 0.25f, 0.20f);
    public static readonly Color Defend = new Color(0.22f, 0.42f, 0.62f);
    public static readonly Color Withdraw = new Color(0.68f, 0.42f, 0.16f);
    public static readonly Color Move = new Color(0.36f, 0.52f, 0.31f);

    public static GUIStyle Panel(int fontSize = 10)
    {
        EnsureTextures();
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = panelTexture;
        style.normal.textColor = Text;
        style.fontSize = fontSize;
        style.padding = new RectOffset(12, 12, 9, 9);
        style.border = new RectOffset(2, 2, 2, 2);
        return style;
    }

    public static GUIStyle Header(int fontSize = 13)
    {
        EnsureTextures();
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = panelHeaderTexture;
        style.normal.textColor = Text;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleLeft;
        style.padding = new RectOffset(10, 8, 3, 3);
        return style;
    }

    public static GUIStyle Label(int fontSize = 10, bool bold = false)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Text;
        style.fontSize = fontSize;
        style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.alignment = TextAnchor.MiddleLeft;
        return style;
    }

    public static GUIStyle MutedLabel(int fontSize = 9)
    {
        GUIStyle style = Label(fontSize, false);
        style.normal.textColor = MutedText;
        return style;
    }

    public static GUIStyle Button(int fontSize = 10)
    {
        EnsureTextures();
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = buttonTexture;
        style.hover.background = buttonHoverTexture;
        style.active.background = buttonActiveTexture;
        style.focused.background = buttonHoverTexture;
        style.normal.textColor = Text;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(6, 6, 3, 3);
        return style;
    }

    public static GUIStyle AccentBox(int fontSize = 9)
    {
        EnsureTextures();
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = accentTexture;
        style.normal.textColor = new Color(0.12f, 0.11f, 0.08f);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(6, 6, 2, 2);
        return style;
    }

    private static void EnsureTextures()
    {
        if (panelTexture != null)
            return;

        panelTexture = MakeTexture(new Color(0.075f, 0.085f, 0.072f, 0.96f));
        panelHeaderTexture = MakeTexture(new Color(0.16f, 0.18f, 0.13f, 0.98f));
        buttonTexture = MakeTexture(new Color(0.17f, 0.19f, 0.15f, 0.98f));
        buttonHoverTexture = MakeTexture(new Color(0.26f, 0.29f, 0.20f, 1f));
        buttonActiveTexture = MakeTexture(new Color(0.39f, 0.33f, 0.16f, 1f));
        accentTexture = MakeTexture(new Color(0.76f, 0.64f, 0.30f, 1f));
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "UI09F15_" + ColorUtility.ToHtmlStringRGBA(color);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}
