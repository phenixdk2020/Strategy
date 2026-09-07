using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CampaignMapController : MonoBehaviour
{
    private const float BaseHoursPerRealSecond = 0.35f;
    private const float QaMarchSpeedKmPerHour = 4.0f;

    private readonly Dictionary<string, CampaignFormationView> formationViews =
        new Dictionary<string, CampaignFormationView>(StringComparer.Ordinal);

    private CampaignFormationState selectedFormation;
    private CampaignFormationState contactAttacker;
    private CampaignFormationState contactDefender;
    private CampaignNodeState contactNode;

    private LineRenderer routeGhost;
    private bool paused;
    private float speed = 1f;
    private bool enemyOrdersSeeded;

    private GUIStyle topStyle;
    private GUIStyle panelStyle;
    private GUIStyle smallStyle;
    private GUIStyle nodeLabelStyle;

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        RefreshFormationViews();
        CreateRouteGhost();
    }

    private void Update()
    {
        HandleInput();

        if (!enemyOrdersSeeded)
            SeedQaEnemyOrders();

        if (paused || contactNode != null)
        {
            UpdateFormationTokenPositions();
            return;
        }

        float elapsedHours = Time.unscaledDeltaTime * BaseHoursPerRealSecond * speed;
        CampaignSession.AdvanceHours(elapsedHours);
        AdvanceFormations(elapsedHours);
        UpdateFormationTokenPositions();
        RefreshRouteGhost();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSpeed(5f);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSpeed(20f);

        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();
        if (Input.GetMouseButtonDown(1))
            HandleRightClick();
    }

    private void HandleLeftClick()
    {
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

            selectedFormation = CampaignSession.GetFormation(view.FormationId);
            RefreshRouteGhost();
            return;
        }
    }

    private void HandleRightClick()
    {
        if (selectedFormation == null ||
            selectedFormation.IsDestroyed ||
            selectedFormation.IsEngaged ||
            selectedFormation.Nation != CampaignNation.Denmark)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 2200f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            CampaignNodeView nodeView = hit.collider.GetComponentInParent<CampaignNodeView>();
            if (nodeView == null)
                continue;

            CampaignNodeState destination = CampaignSession.GetNode(nodeView.NodeId);
            if (destination == null)
                return;

            IssueMoveOrder(selectedFormation, destination.Id);
            return;
        }
    }

    private void IssueMoveOrder(CampaignFormationState formation, string destinationNodeId)
    {
        if (formation == null || string.IsNullOrEmpty(destinationNodeId))
            return;

        List<string> route = FindShortestRoute(formation.CurrentNodeId, destinationNodeId);
        if (route == null || route.Count < 2)
        {
            Debug.Log(string.Format(
                "CAMPAIGN-MOVE|Unit={0}|Order=False|From={1}|To={2}|Reason=NoRoute",
                formation.Name,
                formation.CurrentNodeId,
                destinationNodeId));
            return;
        }

        SetFormationRoute(formation, route);
        Debug.Log(string.Format(
            "CAMPAIGN-MOVE|Unit={0}|Order=True|From={1}|To={2}|Legs={3}",
            formation.Name,
            route[0],
            route[route.Count - 1],
            route.Count - 1));

        RefreshRouteGhost();
    }

    private static void SetFormationRoute(CampaignFormationState formation, List<string> route)
    {
        formation.Route.Clear();
        formation.Route.AddRange(route);
        formation.RouteIndex = 0;
        formation.LegProgressHours = 0f;
        formation.LegTravelHours = 0f;
        formation.LegFromNodeId = null;
        formation.LegToNodeId = null;
        formation.IsMoving = route.Count > 1;
    }

    private void AdvanceFormations(float elapsedHours)
    {
        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState formation = pair.Value;
            if (formation == null || formation.IsDestroyed || formation.IsEngaged || !formation.IsMoving)
                continue;

            float remainingHours = elapsedHours;
            int guard = 0;

            while (remainingHours > 0f && formation.IsMoving && guard++ < 12)
            {
                if (string.IsNullOrEmpty(formation.LegToNodeId))
                {
                    if (!BeginNextLeg(formation))
                        break;
                }

                float need = Mathf.Max(0f, formation.LegTravelHours - formation.LegProgressHours);
                float used = Mathf.Min(remainingHours, need);
                formation.LegProgressHours += used;
                remainingHours -= used;

                if (formation.LegProgressHours + 0.0001f < formation.LegTravelHours)
                    break;

                CompleteLeg(formation);
                if (contactNode != null)
                    break;
            }

            if (contactNode != null)
                break;
        }
    }

    private static bool BeginNextLeg(CampaignFormationState formation)
    {
        if (formation.Route == null || formation.Route.Count < 2)
        {
            formation.IsMoving = false;
            return false;
        }

        int nextIndex = formation.RouteIndex + 1;
        if (nextIndex >= formation.Route.Count)
        {
            formation.IsMoving = false;
            return false;
        }

        CampaignNodeState from = CampaignSession.GetNode(formation.Route[formation.RouteIndex]);
        CampaignNodeState to = CampaignSession.GetNode(formation.Route[nextIndex]);
        if (from == null || to == null)
        {
            formation.IsMoving = false;
            return false;
        }

        formation.LegFromNodeId = from.Id;
        formation.LegToNodeId = to.Id;
        formation.LegProgressHours = 0f;

        float distanceKm = CampaignGeoProjection.ApproximateDistanceKm(
            from.Latitude,
            from.Longitude,
            to.Latitude,
            to.Longitude);

        float terrainFactor = GetTerrainTravelFactor(to.Terrain);
        formation.LegTravelHours = Mathf.Max(0.5f, distanceKm / QaMarchSpeedKmPerHour * terrainFactor);
        return true;
    }

    private void CompleteLeg(CampaignFormationState formation)
    {
        string arrivedNodeId = formation.LegToNodeId;
        formation.CurrentNodeId = arrivedNodeId;
        formation.RouteIndex++;
        formation.LegProgressHours = 0f;
        formation.LegTravelHours = 0f;
        formation.LegFromNodeId = null;
        formation.LegToNodeId = null;

        if (formation.RouteIndex >= formation.Route.Count - 1)
            formation.IsMoving = false;

        Debug.Log(string.Format(
            "CAMPAIGN-MOVE|Unit={0}|Arrived={1}|Time={2:yyyy-MM-dd HH:mm}",
            formation.Name,
            arrivedNodeId,
            CampaignSession.CurrentDateTime));

        CheckForContact(formation);
    }

    private void CheckForContact(CampaignFormationState arriving)
    {
        if (arriving == null || arriving.IsDestroyed)
            return;

        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState other = pair.Value;
            if (other == null || other == arriving || other.IsDestroyed || other.IsEngaged)
                continue;
            if (other.CurrentNodeId != arriving.CurrentNodeId)
                continue;
            if (!AreHostile(arriving.Nation, other.Nation))
                continue;

            contactAttacker = arriving;
            contactDefender = other;
            contactNode = CampaignSession.GetNode(arriving.CurrentNodeId);
            paused = true;

            Debug.Log(string.Format(
                "CAMPAIGN-CONTACT|Location={0}|Attacker={1}|Defender={2}|AtkMen={3}|DefMen={4}",
                contactNode != null ? contactNode.Name : arriving.CurrentNodeId,
                arriving.Name,
                other.Name,
                arriving.TotalStrength,
                other.TotalStrength));
            return;
        }
    }

    private static bool AreHostile(CampaignNation a, CampaignNation b)
    {
        return (a == CampaignNation.Denmark && b == CampaignNation.Prussia) ||
               (a == CampaignNation.Prussia && b == CampaignNation.Denmark);
    }

    private static float GetTerrainTravelFactor(CampaignTerrainType terrain)
    {
        switch (terrain)
        {
            case CampaignTerrainType.Forest:
                return 1.25f;
            case CampaignTerrainType.Mountain:
                return 1.55f;
            case CampaignTerrainType.Fortified:
            case CampaignTerrainType.Urban:
                return 1.08f;
            default:
                return 1f;
        }
    }

    private void SeedQaEnemyOrders()
    {
        enemyOrdersSeeded = true;

        CampaignFormationState first = CampaignSession.GetFormation("PR-I");
        CampaignFormationState second = CampaignSession.GetFormation("PR-II");

        if (first != null)
        {
            List<string> route = FindShortestRoute(first.CurrentNodeId, "HADERSLEV");
            if (route != null && route.Count > 1)
                SetFormationRoute(first, route);
        }

        if (second != null)
        {
            List<string> route = FindShortestRoute(second.CurrentNodeId, "DYBBOEL");
            if (route != null && route.Count > 1)
                SetFormationRoute(second, route);
        }
    }

    private static List<string> FindShortestRoute(string startId, string goalId)
    {
        if (string.IsNullOrEmpty(startId) || string.IsNullOrEmpty(goalId))
            return null;
        if (startId == goalId)
            return new List<string> { startId };

        Dictionary<string, float> distance = new Dictionary<string, float>(StringComparer.Ordinal);
        Dictionary<string, string> previous = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> unvisited = new HashSet<string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            distance[pair.Key] = float.PositiveInfinity;
            unvisited.Add(pair.Key);
        }

        if (!distance.ContainsKey(startId) || !distance.ContainsKey(goalId))
            return null;

        distance[startId] = 0f;

        while (unvisited.Count > 0)
        {
            string current = null;
            float best = float.PositiveInfinity;
            foreach (string candidate in unvisited)
            {
                float d = distance[candidate];
                if (d < best)
                {
                    best = d;
                    current = candidate;
                }
            }

            if (current == null || float.IsInfinity(best))
                break;
            if (current == goalId)
                break;

            unvisited.Remove(current);
            CampaignNodeState node = CampaignSession.GetNode(current);
            if (node == null)
                continue;

            foreach (string neighbourId in node.Links)
            {
                if (!unvisited.Contains(neighbourId))
                    continue;

                CampaignNodeState neighbour = CampaignSession.GetNode(neighbourId);
                if (neighbour == null)
                    continue;

                float edge = CampaignGeoProjection.ApproximateDistanceKm(
                    node.Latitude,
                    node.Longitude,
                    neighbour.Latitude,
                    neighbour.Longitude);
                float alt = best + edge * GetTerrainTravelFactor(neighbour.Terrain);
                if (alt >= distance[neighbourId])
                    continue;

                distance[neighbourId] = alt;
                previous[neighbourId] = current;
            }
        }

        if (!previous.ContainsKey(goalId))
            return null;

        List<string> result = new List<string>();
        string cursor = goalId;
        result.Add(cursor);

        while (cursor != startId)
        {
            if (!previous.TryGetValue(cursor, out cursor))
                return null;
            result.Add(cursor);
        }

        result.Reverse();
        return result;
    }

    private void RefreshFormationViews()
    {
        formationViews.Clear();
        CampaignFormationView[] views = Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in views)
        {
            if (view != null && !string.IsNullOrEmpty(view.FormationId))
                formationViews[view.FormationId] = view;
        }
    }

    private void UpdateFormationTokenPositions()
    {
        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState formation = pair.Value;
            if (formation == null || !formationViews.TryGetValue(formation.Id, out CampaignFormationView view) || view == null)
                continue;

            Vector3 targetPosition;
            if (!string.IsNullOrEmpty(formation.LegFromNodeId) &&
                !string.IsNullOrEmpty(formation.LegToNodeId) &&
                formation.LegTravelHours > 0f)
            {
                CampaignNodeState from = CampaignSession.GetNode(formation.LegFromNodeId);
                CampaignNodeState to = CampaignSession.GetNode(formation.LegToNodeId);
                if (from == null || to == null)
                    continue;

                float t = Mathf.Clamp01(formation.LegProgressHours / formation.LegTravelHours);
                targetPosition = Vector3.Lerp(
                    new Vector3(from.MapPosition.x, 4f, from.MapPosition.y),
                    new Vector3(to.MapPosition.x, 4f, to.MapPosition.y),
                    t);
            }
            else
            {
                CampaignNodeState node = CampaignSession.GetNode(formation.CurrentNodeId);
                if (node == null)
                    continue;
                targetPosition = new Vector3(node.MapPosition.x, 4f, node.MapPosition.y);
            }

            view.transform.position = targetPosition;
        }
    }

    private void CreateRouteGhost()
    {
        GameObject root = new GameObject("CampaignRouteGhost");
        routeGhost = root.AddComponent<LineRenderer>();
        routeGhost.useWorldSpace = true;
        routeGhost.widthMultiplier = 1.5f;
        routeGhost.sharedMaterial = CreateUiMaterial(new Color(0.95f, 0.82f, 0.20f), "CampaignRouteGhostMaterial");
        routeGhost.enabled = false;
    }

    private void RefreshRouteGhost()
    {
        if (routeGhost == null)
            return;

        if (selectedFormation == null || selectedFormation.Route.Count < 2)
        {
            routeGhost.enabled = false;
            return;
        }

        int startIndex = Mathf.Clamp(selectedFormation.RouteIndex, 0, selectedFormation.Route.Count - 1);
        int count = selectedFormation.Route.Count - startIndex;
        routeGhost.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            CampaignNodeState node = CampaignSession.GetNode(selectedFormation.Route[startIndex + i]);
            if (node == null)
                continue;
            routeGhost.SetPosition(i, new Vector3(node.MapPosition.x, 2.2f, node.MapPosition.y));
        }

        routeGhost.enabled = true;
    }

    private void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        paused = false;
    }

    private void EnsureStyles()
    {
        if (topStyle != null)
            return;

        topStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        topStyle.normal.textColor = Color.white;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 11,
            padding = new RectOffset(9, 9, 7, 7)
        };
        panelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 10
        };
        smallStyle.normal.textColor = Color.white;

        nodeLabelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9
        };
        nodeLabelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();

        Rect top = new Rect(Screen.width * 0.5f - 360f, 18f, 720f, 34f);
        GUI.Box(top, string.Empty);

        string state = paused ? "PAUSE" : "x" + speed.ToString("0");
        GUI.Box(new Rect(top.x + 4f, top.y + 4f, 255f, 26f),
            CampaignSession.CurrentDateTime.ToString("d MMM yyyy HH:mm") + " | " + state,
            topStyle);

        if (GUI.Button(new Rect(top.x + 264f, top.y + 4f, 100f, 26f), paused ? "FORTSÆT" : "PAUSE"))
            paused = !paused;
        if (GUI.Button(new Rect(top.x + 369f, top.y + 4f, 80f, 26f), speed == 1f && !paused ? "[1]" : "1"))
            SetSpeed(1f);
        if (GUI.Button(new Rect(top.x + 454f, top.y + 4f, 80f, 26f), speed == 5f && !paused ? "[5]" : "5"))
            SetSpeed(5f);
        if (GUI.Button(new Rect(top.x + 539f, top.y + 4f, 80f, 26f), speed == 20f && !paused ? "[20]" : "20"))
            SetSpeed(20f);
        if (GUI.Button(new Rect(top.x + 624f, top.y + 4f, 92f, 26f), "RESET"))
        {
            CampaignSession.ResetCampaign();
            SceneManager.LoadScene("CampaignMap");
        }

        DrawNodeLabels();
        DrawSelectionPanel();
        DrawContactPanel();
    }

    private void DrawNodeLabels()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            Vector3 world = new Vector3(node.MapPosition.x, 2.7f, node.MapPosition.y);
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 55f || guiY > Screen.height)
                continue;

            string flags = string.Empty;
            if (node.HasPort) flags += " ⚓";
            if (node.HasRail) flags += " R";
            if (node.Terrain == CampaignTerrainType.Fortified) flags += " F";

            GUI.Box(new Rect(screen.x - 46f, guiY - 12f, 92f, 20f), node.Name + flags, nodeLabelStyle);
        }
    }

    private void DrawSelectionPanel()
    {
        if (selectedFormation == null)
            return;

        CampaignNodeState node = CampaignSession.GetNode(selectedFormation.CurrentNodeId);
        string location = node != null ? node.Name : selectedFormation.CurrentNodeId;
        string route = selectedFormation.IsMoving && selectedFormation.Route.Count > 0
            ? selectedFormation.Route[selectedFormation.Route.Count - 1]
            : "-";

        string text = string.Format(
            "{0}\nNation: {1}\nPosition: {2}\nStyrke: {3}\nAmmo: {4} runder/mand\nOrdre: {5}\n\nVenstreklik = vælg\nHøjreklik node = marchordre\nWASD = pan | Q/E = drej | hjul = zoom",
            selectedFormation.Name,
            selectedFormation.Nation,
            location,
            selectedFormation.TotalStrength,
            selectedFormation.AverageAmmo,
            route);

        GUI.Box(new Rect(16f, 72f, 250f, 176f), text, panelStyle);
    }

    private void DrawContactPanel()
    {
        if (contactNode == null || contactAttacker == null || contactDefender == null)
            return;

        Rect box = new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 100f, 480f, 200f);
        GUI.Box(box, string.Empty);
        GUI.Label(new Rect(box.x + 18f, box.y + 14f, box.width - 36f, 28f),
            "FJENDTLIG KONTAKT - " + contactNode.Name, topStyle);

        GUI.Label(new Rect(box.x + 25f, box.y + 52f, box.width - 50f, 78f),
            string.Format(
                "Angriber: {0} ({1} mand)\nForsvarer: {2} ({3} mand)\nTid: {4:dd MMM yyyy HH:mm}",
                contactAttacker.Name,
                contactAttacker.TotalStrength,
                contactDefender.Name,
                contactDefender.TotalStrength,
                CampaignSession.CurrentDateTime),
            smallStyle);

        if (GUI.Button(new Rect(box.x + 80f, box.y + 143f, 320f, 38f), "KÆMP TAKTISK"))
        {
            CampaignSession.PrepareBattle(contactAttacker, contactDefender, contactNode);
            SceneManager.LoadScene("PrototypeBattle");
        }
    }

    private static Material CreateUiMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }
}
