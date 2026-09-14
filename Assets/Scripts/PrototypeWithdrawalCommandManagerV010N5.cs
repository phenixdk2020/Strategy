using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign3 v00.00.10n5 player-facing withdrawal command layer.
/// TILB. HERTIL arms a map-click rally point for the currently selected Danish units.
/// BRYD KONTAKT immediately orders selected units to disengage as fast as possible.
/// </summary>
[DefaultExecutionOrder(-9000)]
public sealed class PrototypeWithdrawalCommandManagerV010N5 : MonoBehaviour
{
    public static PrototypeWithdrawalCommandManagerV010N5 Instance { get; private set; }

    private readonly List<Regiment> armedTargets = new List<Regiment>();
    private bool awaitingRallyPoint;
    private GUIStyle panelStyle;
    private GUIStyle statusStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeWithdrawalCommandManagerV010N5>() != null)
            return;

        GameObject go = new GameObject("PrototypeWithdrawalCommandManager_v00.00.10n5");
        go.AddComponent<PrototypeWithdrawalCommandManagerV010N5>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
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
        EnsureControllersInstalled();

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        Rect withdrawButton = GetWithdrawButtonRect();
        Rect breakButton = GetBreakButtonRect();

        if (withdrawButton.Contains(guiPoint))
        {
            ArmWithdrawalToHere();
            return;
        }

        if (breakButton.Contains(guiPoint))
        {
            IssueBreakContact();
            return;
        }

        if (!awaitingRallyPoint)
            return;

        if (GetPanelRect().Contains(guiPoint))
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.IsPointerOverSimulationControls(Input.mousePosition))
            return;

        if (!TryGetGroundPoint(Input.mousePosition, out Vector3 rallyPoint))
            return;

        int issued = 0;
        for (int i = armedTargets.Count - 1; i >= 0; i--)
        {
            Regiment regiment = armedTargets[i];
            if (regiment == null || regiment.IsRouted)
                continue;

            PrototypeWithdrawalControllerV010N5 controller = GetOrAddController(regiment);
            controller.BeginWithdrawal(rallyPoint);
            issued++;
        }

        awaitingRallyPoint = false;
        armedTargets.Clear();

        Debug.Log(
            "WITHDRAW-10N5|Command=WithdrawToHere|Issued=" + issued +
            "|Rally=" + rallyPoint.ToString("F1"));
    }

    private void ArmWithdrawalToHere()
    {
        List<Regiment> selected = GetSelectedDanishRegiments();
        if (selected.Count == 0)
            return;

        armedTargets.Clear();
        armedTargets.AddRange(selected);
        awaitingRallyPoint = true;

        Debug.Log("WITHDRAW-10N5|Command=WithdrawToHere|Armed=" + armedTargets.Count);
    }

    private void IssueBreakContact()
    {
        List<Regiment> selected = GetSelectedDanishRegiments();
        if (selected.Count == 0)
            return;

        awaitingRallyPoint = false;
        armedTargets.Clear();

        int issued = 0;
        foreach (Regiment regiment in selected)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            PrototypeWithdrawalControllerV010N5 controller = GetOrAddController(regiment);
            controller.BeginBreakContact();
            issued++;
        }

        Debug.Log("WITHDRAW-10N5|Command=BreakContact|Issued=" + issued);
    }

    private void EnsureControllersInstalled()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.GetComponent<PrototypeWithdrawalControllerV010N5>() == null)
                regiment.gameObject.AddComponent<PrototypeWithdrawalControllerV010N5>();
        }
    }

    private static PrototypeWithdrawalControllerV010N5 GetOrAddController(Regiment regiment)
    {
        PrototypeWithdrawalControllerV010N5 controller = regiment.GetComponent<PrototypeWithdrawalControllerV010N5>();
        if (controller == null)
            controller = regiment.gameObject.AddComponent<PrototypeWithdrawalControllerV010N5>();
        return controller;
    }

    private static List<Regiment> GetSelectedDanishRegiments()
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null &&
                regiment.Team == BattleTeam.Denmark &&
                regiment.IsSelected &&
                !regiment.IsRouted)
            {
                result.Add(regiment);
            }
        }

        return result;
    }

    private static int GetActiveWithdrawalCount()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return 0;

        int count = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team != BattleTeam.Denmark)
                continue;

            PrototypeWithdrawalControllerV010N5 controller = regiment.GetComponent<PrototypeWithdrawalControllerV010N5>();
            if (controller != null && controller.Mode != TacticalWithdrawalMode.None)
                count++;
        }
        return count;
    }

    private bool TryGetGroundPoint(Vector3 screenPoint, out Vector3 point)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            point = default;
            return false;
        }

        Ray ray = cam.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1600f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;

            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        point = default;
        return false;
    }

    private static Rect GetPanelRect()
    {
        float width = Mathf.Clamp(Screen.width - 160f, 430f, 680f);
        const float height = 36f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 142f;
        return new Rect(x, y, width, height);
    }

    private static Rect GetWithdrawButtonRect()
    {
        Rect panel = GetPanelRect();
        return new Rect(panel.x + 5f, panel.y + 5f, 158f, 26f);
    }

    private static Rect GetBreakButtonRect()
    {
        Rect panel = GetPanelRect();
        return new Rect(panel.x + 168f, panel.y + 5f, 142f, 26f);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 9;
        panelStyle.normal.textColor = Color.white;

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft
        };
        statusStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        int selectedCount = GetSelectedDanishRegiments().Count;
        int activeCount = GetActiveWithdrawalCount();
        if (selectedCount == 0 && activeCount == 0 && !awaitingRallyPoint)
            return;

        EnsureStyles();
        Rect panel = GetPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);

        Rect withdrawRect = GetWithdrawButtonRect();
        Rect breakRect = GetBreakButtonRect();
        GUI.Box(
            withdrawRect,
            awaitingRallyPoint ? "[KLIK SAMLEPUNKT]" : "TILBAGETRÆK HERTIL",
            GUI.skin.button);
        GUI.Box(breakRect, "BRYD KONTAKT", GUI.skin.button);

        string status;
        if (awaitingRallyPoint)
            status = "Klik på terrænet: " + armedTargets.Count + " enheder får fælles samlepunkt";
        else if (selectedCount > 0)
            status = selectedCount + " valgt(e) | aktive tilbagetrækninger: " + activeCount;
        else
            status = "Aktive tilbagetrækninger: " + activeCount;

        GUI.Label(
            new Rect(panel.x + 318f, panel.y + 5f, panel.width - 323f, 26f),
            status,
            statusStyle);
    }
}
