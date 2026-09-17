using System.Collections;
using System.Reflection;
using UnityEngine;

// v00.00.09f30a
// Persistent regimental-order state overlay.
// Pending and active regiment orders are shown in blue until no subordinate mission
// remains under that order. This is visual state only; command authority remains F28/F27.
[DefaultExecutionOrder(110000)]
public sealed class PrototypeRegimentalOrderState09F30A : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float HudHeight = 90f;

    private FieldInfo pendingOrderField;
    private FieldInfo currentMissionField;
    private FieldInfo battalionsField;

    private GUIStyle blueStyle;
    private Texture2D blueTexture;
    private Texture2D blueHoverTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentalOrderState09F30A>() == null)
            new GameObject("PrototypeRegimentalOrderState_v000009f30a")
                .AddComponent<PrototypeRegimentalOrderState09F30A>();
    }

    private void Awake()
    {
        pendingOrderField = typeof(PrototypeRegimentalHQ09F28).GetField("pendingTargetOrder", AnyInstance);
        currentMissionField = typeof(PrototypeRegimentalHQ09F28).GetField("currentMission", AnyInstance);
        battalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);

        Debug.Log("REG-ORDER-STATE-09F30A|Installed=True|Pending=BLUE|Active=BLUE|EndsWhenNoExecutors=True");
    }

    private void OnGUI()
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental == null || !regimental.Installed || !regimental.Selected)
            return;

        BuildStyle();
        MajorOrder09F18 highlighted = GetHighlightedOrder(regimental);
        if (highlighted == MajorOrder09F18.None)
            return;

        // F29F draws the real clickable controls at GUI.depth -100000.
        // This component only paints the persistent blue state above them;
        // GUI.Box does not own the button action.
        GUI.depth = -100100;

        float width = Screen.width;
        float infoWidth = Mathf.Clamp(width * 0.24f, 280f, 350f);
        float battalionWidth = Mathf.Clamp(width * 0.31f, 370f, 500f);
        float commandX = infoWidth + 8f;
        float battalionX = width - battalionWidth - 6f;
        float commandWidth = Mathf.Max(400f, battalionX - commandX - 7f);
        float y = Screen.height - HudHeight + 21f;
        const float gap = 4f;
        float orderW = (commandWidth - gap * 2f) / 3f;
        float row1 = y + 11f;
        float row2 = y + 36f;

        Rect rect;
        string label;
        switch (highlighted)
        {
            case MajorOrder09F18.AttackHere:
                rect = new Rect(commandX, row1, orderW, 21f);
                label = "ANGRIB HER";
                break;
            case MajorOrder09F18.DefendHere:
                rect = new Rect(commandX + orderW + gap, row1, orderW, 21f);
                label = "FORSVAR HER";
                break;
            case MajorOrder09F18.AdvanceHere:
                rect = new Rect(commandX + (orderW + gap) * 2f, row1, orderW, 21f);
                label = "RYK FREM";
                break;
            case MajorOrder09F18.WithdrawHere:
                rect = new Rect(commandX, row2, orderW, 21f);
                label = "TILBAGETRÆK";
                break;
            case MajorOrder09F18.AssembleHere:
                rect = new Rect(commandX + orderW + gap, row2, orderW, 21f);
                label = "SAML";
                break;
            case MajorOrder09F18.HoldPosition:
                rect = new Rect(commandX + (orderW + gap) * 2f, row2, orderW, 21f);
                label = "STOP / HOLD";
                break;
            default:
                return;
        }

        GUI.Box(rect, label, blueStyle);
    }

    private MajorOrder09F18 GetHighlightedOrder(PrototypeRegimentalHQ09F28 regimental)
    {
        MajorOrder09F18 pending = ReadPending(regimental);
        if (pending != MajorOrder09F18.None)
            return pending;

        MajorOrder09F18 current = ReadCurrentMissionOrder(regimental);
        if (current == MajorOrder09F18.None)
            return MajorOrder09F18.None;

        return HasActiveExecutors(current) ? current : MajorOrder09F18.None;
    }

    private MajorOrder09F18 ReadPending(PrototypeRegimentalHQ09F28 regimental)
    {
        if (pendingOrderField == null || regimental == null)
            return MajorOrder09F18.None;
        object value = pendingOrderField.GetValue(regimental);
        return value is MajorOrder09F18 ? (MajorOrder09F18)value : MajorOrder09F18.None;
    }

    private MajorOrder09F18 ReadCurrentMissionOrder(PrototypeRegimentalHQ09F28 regimental)
    {
        if (currentMissionField == null || regimental == null)
            return MajorOrder09F18.None;

        object mission = currentMissionField.GetValue(regimental);
        if (mission == null)
            return MajorOrder09F18.None;

        FieldInfo orderField = mission.GetType().GetField("Order", AnyInstance);
        if (orderField == null)
            return MajorOrder09F18.None;

        object value = orderField.GetValue(mission);
        return value is MajorOrder09F18 ? (MajorOrder09F18)value : MajorOrder09F18.None;
    }

    private bool HasActiveExecutors(MajorOrder09F18 order)
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || battalionsField == null)
            return false;

        IList battalions = battalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return false;

        for (int b = 0; b < battalions.Count; b++)
        {
            object battalion = battalions[b];
            if (battalion == null)
                continue;

            System.Type type = battalion.GetType();

            if (order == MajorOrder09F18.HoldPosition)
            {
                FieldInfo hasLastField = type.GetField("HasLastOrder", AnyInstance);
                FieldInfo lastOrderField = type.GetField("LastOrder", AnyInstance);
                bool hasLast = hasLastField != null && hasLastField.GetValue(battalion) is bool &&
                               (bool)hasLastField.GetValue(battalion);
                MajorOrder09F18 last = MajorOrder09F18.None;
                if (lastOrderField != null)
                {
                    object value = lastOrderField.GetValue(battalion);
                    if (value is MajorOrder09F18)
                        last = (MajorOrder09F18)value;
                }

                if (hasLast && last == MajorOrder09F18.HoldPosition && BattalionHasLivingCompany(hierarchy, b))
                    return true;
                continue;
            }

            FieldInfo missionsField = type.GetField("Missions", AnyInstance);
            IDictionary missions = missionsField != null ? missionsField.GetValue(battalion) as IDictionary : null;
            if (missions == null)
                continue;

            foreach (DictionaryEntry entry in missions)
            {
                Regiment unit = entry.Key as Regiment;
                object mission = entry.Value;
                if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                    continue;

                FieldInfo orderField = mission.GetType().GetField("Order", AnyInstance);
                if (orderField == null)
                    continue;
                object value = orderField.GetValue(mission);
                if (value is MajorOrder09F18 && (MajorOrder09F18)value == order)
                    return true;
            }
        }

        return false;
    }

    private static bool BattalionHasLivingCompany(PrototypeRegimentHierarchy09F27 hierarchy, int battalionIndex)
    {
        var companies = hierarchy.GetCompanies(battalionIndex);
        if (companies == null)
            return false;

        for (int i = 0; i < companies.Count; i++)
        {
            Regiment unit = companies[i];
            if (unit != null && !unit.IsRouted && unit.CurrentStrength > 0)
                return true;
        }
        return false;
    }

    private void BuildStyle()
    {
        if (blueStyle != null)
            return;

        blueTexture = MakeTexture(new Color(0.10f, 0.30f, 0.62f, 1f), "HUD30A_BLUE");
        blueHoverTexture = MakeTexture(new Color(0.14f, 0.40f, 0.80f, 1f), "HUD30A_BLUE_HOVER");

        blueStyle = new GUIStyle(GUI.skin.box);
        blueStyle.normal.background = blueTexture;
        blueStyle.hover.background = blueHoverTexture;
        blueStyle.active.background = blueHoverTexture;
        blueStyle.normal.textColor = Color.white;
        blueStyle.hover.textColor = Color.white;
        blueStyle.active.textColor = Color.white;
        blueStyle.fontSize = 8;
        blueStyle.fontStyle = FontStyle.Bold;
        blueStyle.alignment = TextAnchor.MiddleCenter;
        blueStyle.padding = new RectOffset(2, 2, 1, 1);
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
