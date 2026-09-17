using UnityEngine;

// v00.00.09f30c
// Player-facing cavalry command ownership. CurrentCommandParent can be moved live through
// Division -> Brigade -> Regiment -> Major A -> Major B while OrganicParent stays unchanged.
// Also exposes explicit cavalry AI ON/OFF so the player can take over at any time.
[DefaultExecutionOrder(41900)]
public sealed class PrototypeCavalryCommandControl09F30C : MonoBehaviour
{
    public const string DivisionId = "1. DIVISION";
    public const string BrigadeId = "1. BRIGADE";
    public const string RegimentId = "1. REGIMENT";
    public const string MajorAId = "MAJOR A / 1. BATALJON";
    public const string MajorBId = "MAJOR B / 2. BATALJON";

    private const float HudHeight = 90f;

    private GUIStyle buttonStyle;
    private GUIStyle activeStyle;
    private GUIStyle labelStyle;
    private Texture2D activeTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryCommandControl09F30C>() == null)
            new GameObject("PrototypeCavalryCommandControl_v000009f30c")
                .AddComponent<PrototypeCavalryCommandControl09F30C>();
    }

    public static string GetParent(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null)
            return BrigadeId;
        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        return attachment != null && !string.IsNullOrEmpty(attachment.CurrentCommandParent)
            ? attachment.CurrentCommandParent
            : BrigadeId;
    }

    public static string GetParentShort(PrototypeCavalryUnit09F30 unit)
    {
        string parent = GetParent(unit);
        if (parent == DivisionId) return "DIV";
        if (parent == RegimentId) return "REG";
        if (parent == MajorAId) return "MAJ A";
        if (parent == MajorBId) return "MAJ B";
        return "BRIG";
    }

    public static void SetParent(PrototypeCavalryUnit09F30 unit, string parent)
    {
        if (unit == null || string.IsNullOrEmpty(parent))
            return;

        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        if (attachment == null)
        {
            attachment = unit.gameObject.AddComponent<PrototypeCommandAttachment09F30B>();
            attachment.Configure(DivisionId + " / KAVALERI", parent, PrototypeAttachmentType09F30B.Attached);
        }
        else
        {
            attachment.SetCurrentCommandParent(parent, PrototypeAttachmentType09F30B.Attached);
        }

        Debug.Log("CAV-ATTACH-09F30C|Unit=" + unit.UnitName + "|CurrentCommandParent=" + parent +
                  "|OrganicParent=" + attachment.OrganicParent);
    }

    public static void CycleParent(PrototypeCavalryUnit09F30 unit)
    {
        string parent = GetParent(unit);
        if (parent == DivisionId) SetParent(unit, BrigadeId);
        else if (parent == BrigadeId) SetParent(unit, RegimentId);
        else if (parent == RegimentId) SetParent(unit, MajorAId);
        else if (parent == MajorAId) SetParent(unit, MajorBId);
        else SetParent(unit, DivisionId);
    }

    private void OnGUI()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        PrototypeCavalryUnit09F30 selected = cavalry != null ? cavalry.SelectedUnit : null;
        if (selected == null)
            return;

        EnsureStyles();
        GUI.depth = -7900;

        float y = Screen.height - HudHeight + 61f;
        float x = Mathf.Max(660f, Screen.width - 455f);
        float available = Screen.width - x - 8f;
        if (available < 330f)
        {
            x = Mathf.Max(8f, Screen.width - 338f);
            available = Screen.width - x - 8f;
        }

        PrototypeCavalryOfficerAI09F30C ai = PrototypeCavalryOfficerAI09F30C.Instance;
        bool aiOn = ai != null && ai.IsAIEnabled(selected);
        string aiLabel = aiOn ? "AI ON" : "AI OFF / MANUEL";
        Rect aiRect = new Rect(x, y, Mathf.Min(112f, available * 0.31f), 22f);
        if (GUI.Button(aiRect, aiLabel, aiOn ? activeStyle : buttonStyle) && ai != null)
            ai.ToggleAI(selected);

        float parentX = aiRect.xMax + 5f;
        Rect parentRect = new Rect(parentX, y, Mathf.Min(155f, available * 0.42f), 22f);
        if (GUI.Button(parentRect, "TILKNYT: " + GetParentShort(selected), buttonStyle))
            CycleParent(selected);

        float statusX = parentRect.xMax + 6f;
        float statusW = Mathf.Max(40f, Screen.width - statusX - 8f);
        string status = ai != null
            ? ai.GetPhase(selected) + " | mål: " + ai.GetTargetName(selected)
            : "AI initialiseres";
        GUI.Label(new Rect(statusX, y, statusW, 22f), status, labelStyle);
    }

    private void EnsureStyles()
    {
        if (buttonStyle != null)
            return;

        buttonStyle = PrototypeUiTheme09F15.Button(8);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        activeTexture = MakeTexture(new Color(0.18f, 0.42f, 0.72f, 1f));
        activeStyle = new GUIStyle(buttonStyle);
        activeStyle.normal.background = activeTexture;
        activeStyle.hover.background = activeTexture;
        activeStyle.active.background = activeTexture;
        activeStyle.normal.textColor = Color.white;
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
