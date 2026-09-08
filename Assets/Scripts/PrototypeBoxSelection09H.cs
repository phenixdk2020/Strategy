using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09h RTS box selection.
// LMB click remains owned by PlayerCommander. LMB drag on the battlefield draws a
// marquee and, on release, selects all Danish regiment centres inside the rectangle.
// Shift adds; Ctrl toggles. This layer changes selection only - never movement/orders.
[DefaultExecutionOrder(325)]
public sealed class PrototypeBoxSelection09H : MonoBehaviour
{
    private const float DragThresholdPixels = 9f;

    private readonly HashSet<Regiment> selectionAtMouseDown = new HashSet<Regiment>();

    private FieldInfo selectedField;
    private List<Regiment> selectedList;
    private Camera cam;
    private bool mouseDownTracked;
    private bool dragging;
    private bool shiftMode;
    private bool ctrlMode;
    private Vector2 dragStartScreen;
    private Vector2 dragCurrentScreen;
    private GUIStyle countStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBoxSelection09H>() != null)
            return;

        GameObject root = new GameObject("PrototypeBoxSelection_v000009h");
        root.AddComponent<PrototypeBoxSelection09H>();
    }

    private void Start()
    {
        cam = Camera.main;
        ResolveSelectionList();
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (selectedList == null)
            ResolveSelectionList();

        if (Input.GetMouseButtonDown(0))
            BeginPotentialDrag();

        if (mouseDownTracked && Input.GetMouseButton(0))
        {
            dragCurrentScreen = Input.mousePosition;
            if (!dragging &&
                Vector2.Distance(dragCurrentScreen, dragStartScreen) >= DragThresholdPixels)
            {
                dragging = true;
            }
        }

        if (mouseDownTracked && Input.GetMouseButtonUp(0))
            CompletePotentialDrag();
    }

    private void ResolveSelectionList()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null)
            return;

        if (selectedField == null)
        {
            selectedField = typeof(PlayerCommander).GetField(
                "selected",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (selectedField == null)
        {
            Debug.LogError("SELECT-09H|Installed=False|Reason=PlayerCommander.selected_not_found");
            enabled = false;
            return;
        }

        selectedList = selectedField.GetValue(commander) as List<Regiment>;
        if (selectedList != null)
        {
            Debug.Log(
                "SELECT-09H|Installed=True|Mode=BoxSelect|ThresholdPx=" +
                DragThresholdPixels.ToString("0") +
                "|OwnTeam=Denmark|MovementWrites=False");
        }
    }

    private void BeginPotentialDrag()
    {
        if (cam == null || selectedList == null)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.IsPointerOverSimulationControls(Input.mousePosition))
            return;

        mouseDownTracked = true;
        dragging = false;
        dragStartScreen = Input.mousePosition;
        dragCurrentScreen = dragStartScreen;

        shiftMode =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        ctrlMode =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        selectionAtMouseDown.Clear();
        for (int i = 0; i < selectedList.Count; i++)
        {
            Regiment regiment = selectedList[i];
            if (regiment != null)
                selectionAtMouseDown.Add(regiment);
        }
    }

    private void CompletePotentialDrag()
    {
        dragCurrentScreen = Input.mousePosition;

        if (dragging)
            ApplyBoxSelection();

        mouseDownTracked = false;
        dragging = false;
        selectionAtMouseDown.Clear();
    }

    private void ApplyBoxSelection()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || cam == null || selectedList == null)
            return;

        Rect screenRect = GetScreenRectBottomLeft(dragStartScreen, dragCurrentScreen);
        List<Regiment> inside = new List<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark || regiment.IsRouted)
                continue;

            Vector3 screen = cam.WorldToScreenPoint(regiment.transform.position);
            if (screen.z <= 0f)
                continue;

            if (screenRect.Contains(new Vector2(screen.x, screen.y), true))
                inside.Add(regiment);
        }

        // Restore the exact state from mouse-down first. PlayerCommander's ordinary
        // click handler may have selected/cleared/toggled something in that same frame.
        RestoreMouseDownSelection();

        if (!shiftMode && !ctrlMode)
            ClearSelection();

        for (int i = 0; i < inside.Count; i++)
        {
            Regiment regiment = inside[i];

            if (ctrlMode)
            {
                if (selectedList.Contains(regiment))
                    RemoveSelection(regiment);
                else
                    AddSelection(regiment);
            }
            else
            {
                AddSelection(regiment);
            }
        }

        string mode = ctrlMode ? "Toggle" : shiftMode ? "Add" : "Replace";
        Debug.Log(
            "SELECT-09H|BoxComplete=True|Mode=" + mode +
            "|Inside=" + inside.Count +
            "|Selected=" + selectedList.Count);
    }

    private void RestoreMouseDownSelection()
    {
        ClearSelection();
        foreach (Regiment regiment in selectionAtMouseDown)
            if (regiment != null)
                AddSelection(regiment);
    }

    private void ClearSelection()
    {
        for (int i = selectedList.Count - 1; i >= 0; i--)
        {
            Regiment regiment = selectedList[i];
            if (regiment != null)
                regiment.SetSelected(false);
        }
        selectedList.Clear();
    }

    private void AddSelection(Regiment regiment)
    {
        if (regiment == null || regiment.Team != BattleTeam.Denmark)
            return;

        if (!selectedList.Contains(regiment))
            selectedList.Add(regiment);
        regiment.SetSelected(true);
    }

    private void RemoveSelection(Regiment regiment)
    {
        if (regiment == null)
            return;

        selectedList.Remove(regiment);
        regiment.SetSelected(false);
    }

    private static Rect GetScreenRectBottomLeft(Vector2 a, Vector2 b)
    {
        float xMin = Mathf.Min(a.x, b.x);
        float xMax = Mathf.Max(a.x, b.x);
        float yMin = Mathf.Min(a.y, b.y);
        float yMax = Mathf.Max(a.y, b.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Rect GetGuiRectTopLeft(Vector2 a, Vector2 b)
    {
        Vector2 ga = new Vector2(a.x, Screen.height - a.y);
        Vector2 gb = new Vector2(b.x, Screen.height - b.y);
        float xMin = Mathf.Min(ga.x, gb.x);
        float xMax = Mathf.Max(ga.x, gb.x);
        float yMin = Mathf.Min(ga.y, gb.y);
        float yMax = Mathf.Max(ga.y, gb.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void EnsureGuiStyle()
    {
        if (countStyle != null)
            return;

        countStyle = new GUIStyle(GUI.skin.label);
        countStyle.fontSize = 10;
        countStyle.fontStyle = FontStyle.Bold;
        countStyle.normal.textColor = new Color(1f, 0.92f, 0.46f, 0.95f);
        countStyle.alignment = TextAnchor.UpperLeft;
    }

    private void OnGUI()
    {
        if (!dragging)
            return;

        EnsureGuiStyle();
        Rect rect = GetGuiRectTopLeft(dragStartScreen, dragCurrentScreen);

        Color oldColor = GUI.color;
        GUI.color = new Color(0.34f, 0.72f, 1f, 0.13f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = new Color(0.60f, 0.84f, 1f, 0.88f);
        const float border = 1.5f;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - border, rect.y, border, rect.height), Texture2D.whiteTexture);
        GUI.color = oldColor;

        string mode = ctrlMode ? "TOGGLE" : shiftMode ? "ADD" : "SELECT";
        GUI.Label(new Rect(rect.x + 5f, rect.y + 4f, 90f, 18f), mode, countStyle);
    }
}
