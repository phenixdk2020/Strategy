using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(31000)]
public sealed class CampaignMapSearchHoverV011 : MonoBehaviour
{
    private string searchText = string.Empty;
    private string searchResult = "Find location/formation";
    private CampaignFormationState hoverFormation;
    private GUIStyle panelStyle;
    private GUIStyle smallStyle;
    private bool showSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignMapSearchHoverV011>() != null)
            return;

        GameObject root = new GameObject("CampaignMapSearchHover_v0011");
        root.AddComponent<CampaignMapSearchHoverV011>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            showSearch = !showSearch;

        UpdateHoverFormation();
    }

    private void UpdateHoverFormation()
    {
        hoverFormation = null;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 2200f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            CampaignFormationView view = hit.collider.GetComponentInParent<CampaignFormationView>();
            if (view == null)
                continue;

            hoverFormation = CampaignSession.GetFormation(view.FormationId);
            return;
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            padding = new RectOffset(8, 8, 6, 6)
        };
        panelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 10
        };
        smallStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (showSearch)
            DrawSearchPanel();
        DrawFormationHover();
    }

    private void DrawSearchPanel()
    {
        Rect box = new Rect(16f, 260f, 250f, 92f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.Label(new Rect(box.x + 8f, box.y + 6f, 234f, 18f), "FIND PÅ CAMPAIGN-KORT  [F3 hide]", smallStyle);

        searchText = GUI.TextField(new Rect(box.x + 8f, box.y + 28f, 168f, 24f), searchText, 40);
        if (GUI.Button(new Rect(box.x + 181f, box.y + 28f, 61f, 24f), "FIND"))
            ExecuteSearch();

        GUI.Label(new Rect(box.x + 8f, box.y + 57f, 234f, 25f), searchResult, smallStyle);
    }

    private void ExecuteSearch()
    {
        string query = searchText == null ? string.Empty : searchText.Trim();
        if (query.Length == 0)
        {
            searchResult = "Skriv navn eller ID";
            return;
        }

        CampaignNodeState bestNode = FindNode(query);
        if (bestNode != null)
        {
            FocusMapPosition(bestNode.MapPosition);
            searchResult = "Node: " + bestNode.Name + " [" + bestNode.Id + "]";
            Debug.Log(string.Format("CAMPAIGN-SEARCH|Query={0}|Type=Node|Result={1}", query, bestNode.Id));
            return;
        }

        CampaignFormationState bestFormation = FindFormation(query);
        if (bestFormation != null)
        {
            CampaignNodeState node = CampaignSession.GetNode(bestFormation.CurrentNodeId);
            if (node != null)
                FocusMapPosition(node.MapPosition);

            searchResult = "Formation: " + bestFormation.Name;
            Debug.Log(string.Format("CAMPAIGN-SEARCH|Query={0}|Type=Formation|Result={1}", query, bestFormation.Id));
            return;
        }

        searchResult = "Ingen match: " + query;
        Debug.Log(string.Format("CAMPAIGN-SEARCH|Query={0}|Result=None", query));
    }

    private static CampaignNodeState FindNode(string query)
    {
        CampaignNodeState firstPartial = null;
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            if (string.Equals(node.Id, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(node.Name, query, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            if (firstPartial == null &&
                ((node.Id != null && node.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                 (node.Name != null && node.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                firstPartial = node;
            }
        }
        return firstPartial;
    }

    private static CampaignFormationState FindFormation(string query)
    {
        CampaignFormationState firstPartial = null;
        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState formation = pair.Value;
            if (formation == null)
                continue;

            if (string.Equals(formation.Id, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(formation.Name, query, StringComparison.OrdinalIgnoreCase))
            {
                return formation;
            }

            if (firstPartial == null &&
                ((formation.Id != null && formation.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                 (formation.Name != null && formation.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                firstPartial = formation;
            }
        }
        return firstPartial;
    }

    private static void FocusMapPosition(Vector2 mapPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 current = cam.transform.position;
        cam.transform.position = new Vector3(
            mapPosition.x,
            current.y,
            mapPosition.y - Mathf.Max(20f, current.y * 0.25f));
    }

    private void DrawFormationHover()
    {
        if (hoverFormation == null)
            return;

        string destination = hoverFormation.IsMoving && hoverFormation.Route.Count > 0
            ? hoverFormation.Route[hoverFormation.Route.Count - 1]
            : "-";

        string state = hoverFormation.IsDestroyed ? "Destroyed" :
            hoverFormation.IsEngaged ? "Engaged" :
            hoverFormation.IsMoving ? "Moving" : "Idle";

        string text = string.Format(
            "{0}\n{1} | {2}\nStrength: {3} | Regiments: {4}\nState: {5} | Destination: {6}",
            hoverFormation.Name,
            hoverFormation.Nation,
            hoverFormation.Id,
            hoverFormation.TotalStrength,
            hoverFormation.Regiments.Count,
            state,
            destination);

        Vector2 mouse = Event.current.mousePosition;
        float x = Mathf.Clamp(mouse.x + 18f, 0f, Screen.width - 282f);
        float y = Mathf.Clamp(mouse.y + 18f, 0f, Screen.height - 92f);
        GUI.Box(new Rect(x, y, 275f, 84f), text, panelStyle);
    }
}
