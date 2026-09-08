using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CampaignStrategicLinkType
{
    Road,
    Rail,
    SeaFerry
}

[DefaultExecutionOrder(30000)]
public sealed class CampaignMapUsabilityV011 : MonoBehaviour
{
    private const int ExpectedNodeCount = 50;
    private const float QaMarchSpeedKmPerHour = 4.0f;

    private static readonly HashSet<string> seaLinks = new HashSet<string>(StringComparer.Ordinal)
    {
        "CPH|MALMO",
        "CPH|KIEL",
        "FREDERICIA|ODENSE",
        "KORSOR|NYBORG",
        "ABO|STOCKHOLM",
        "HELSINGFORS|STOCKHOLM"
    };

    private readonly Dictionary<CampaignNation, Material> controlMaterials =
        new Dictionary<CampaignNation, Material>();

    private readonly Dictionary<string, GameObject> controlIndicators =
        new Dictionary<string, GameObject>(StringComparer.Ordinal);

    private CampaignNodeState selectedNode;
    private CampaignFormationState selectedFormation;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle diagStyle;
    private int brokenLinkCount;
    private int invalidNodeCount;
    private int uniqueLinkCount;
    private int roadLinkCount;
    private int railLinkCount;
    private int seaLinkCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignMapUsabilityV011>() != null)
            return;

        GameObject root = new GameObject("CampaignMapUsability_v0011");
        root.AddComponent<CampaignMapUsabilityV011>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        BuildControlMaterials();
        CreatePoliticalControlIndicators();
        ValidateNetwork();
    }

    private void Update()
    {
        HandleSelection();
        HandleCameraShortcuts();
    }

    private void LateUpdate()
    {
        ApplyFormationStackOffsets();
        RefreshPoliticalControlIndicators();
    }

    private void HandleSelection()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 2200f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            CampaignFormationView formationView = hit.collider.GetComponentInParent<CampaignFormationView>();
            if (formationView != null)
            {
                selectedFormation = CampaignSession.GetFormation(formationView.FormationId);
                selectedNode = selectedFormation != null
                    ? CampaignSession.GetNode(selectedFormation.CurrentNodeId)
                    : null;
                LogSelection();
                return;
            }

            CampaignNodeView nodeView = hit.collider.GetComponentInParent<CampaignNodeView>();
            if (nodeView != null)
            {
                selectedNode = CampaignSession.GetNode(nodeView.NodeId);
                selectedFormation = null;
                LogSelection();
                return;
            }
        }
    }

    private void HandleCameraShortcuts()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        if (Input.GetKeyDown(KeyCode.Home))
        {
            cam.transform.position = new Vector3(-25f, 410f, -145f);
            cam.transform.rotation = Quaternion.Euler(56f, 7f, 0f);
            Debug.Log("CAMPAIGN-CAMERA|Action=Home|Result=Reset");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector2 mapPosition;
            string targetId;

            if (selectedFormation != null)
            {
                CampaignNodeState node = CampaignSession.GetNode(selectedFormation.CurrentNodeId);
                if (node == null)
                    return;
                mapPosition = node.MapPosition;
                targetId = selectedFormation.Id;
            }
            else if (selectedNode != null)
            {
                mapPosition = selectedNode.MapPosition;
                targetId = selectedNode.Id;
            }
            else
            {
                return;
            }

            Vector3 current = cam.transform.position;
            cam.transform.position = new Vector3(mapPosition.x, current.y, mapPosition.y - Mathf.Max(20f, current.y * 0.25f));
            Debug.Log(string.Format("CAMPAIGN-CAMERA|Action=Focus|Target={0}", targetId));
        }
    }

    private void ValidateNetwork()
    {
        brokenLinkCount = 0;
        invalidNodeCount = 0;
        uniqueLinkCount = 0;
        roadLinkCount = 0;
        railLinkCount = 0;
        seaLinkCount = 0;

        HashSet<string> seenEdges = new HashSet<string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || string.IsNullOrEmpty(node.Id) || node.Id != pair.Key ||
                node.Latitude < -90.0 || node.Latitude > 90.0 ||
                node.Longitude < -180.0 || node.Longitude > 180.0)
            {
                invalidNodeCount++;
                continue;
            }

            foreach (string linkedId in node.Links)
            {
                CampaignNodeState linked = CampaignSession.GetNode(linkedId);
                if (linked == null)
                {
                    brokenLinkCount++;
                    Debug.LogError(string.Format(
                        "CAMPAIGN-NETWORK|BrokenLink=True|From={0}|To={1}",
                        node.Id,
                        linkedId));
                    continue;
                }

                string edgeKey = MakeEdgeKey(node.Id, linked.Id);
                if (!seenEdges.Add(edgeKey))
                    continue;

                uniqueLinkCount++;
                switch (GetLinkType(node, linked))
                {
                    case CampaignStrategicLinkType.SeaFerry:
                        seaLinkCount++;
                        break;
                    case CampaignStrategicLinkType.Rail:
                        railLinkCount++;
                        break;
                    default:
                        roadLinkCount++;
                        break;
                }
            }
        }

        bool valid = CampaignSession.Nodes.Count == ExpectedNodeCount &&
                     brokenLinkCount == 0 &&
                     invalidNodeCount == 0;

        Debug.Log(string.Format(
            "CAMPAIGN-V011|NetworkValid={0}|Nodes={1}|ExpectedNodes={2}|InvalidNodes={3}|BrokenLinks={4}|UniqueLinks={5}|Road={6}|Rail={7}|SeaFerry={8}",
            valid,
            CampaignSession.Nodes.Count,
            ExpectedNodeCount,
            invalidNodeCount,
            brokenLinkCount,
            uniqueLinkCount,
            roadLinkCount,
            railLinkCount,
            seaLinkCount));
    }

    private void BuildControlMaterials()
    {
        controlMaterials[CampaignNation.Denmark] = CreateMaterial(new Color(0.72f, 0.18f, 0.18f), "Control_Denmark");
        controlMaterials[CampaignNation.Prussia] = CreateMaterial(new Color(0.12f, 0.12f, 0.14f), "Control_Prussia");
        controlMaterials[CampaignNation.Austria] = CreateMaterial(new Color(0.78f, 0.78f, 0.76f), "Control_Austria");
        controlMaterials[CampaignNation.SwedenNorway] = CreateMaterial(new Color(0.18f, 0.40f, 0.72f), "Control_SwedenNorway");
        controlMaterials[CampaignNation.RussianEmpire] = CreateMaterial(new Color(0.18f, 0.50f, 0.30f), "Control_RussianEmpire");
        controlMaterials[CampaignNation.GermanConfederation] = CreateMaterial(new Color(0.55f, 0.44f, 0.22f), "Control_GermanConfederation");
    }

    private void CreatePoliticalControlIndicators()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null)
                continue;

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "CampaignControl_" + node.Id;
            indicator.transform.position = new Vector3(node.MapPosition.x, 2.65f, node.MapPosition.y);
            indicator.transform.localScale = new Vector3(1.15f, 0.15f, 1.15f);

            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null && controlMaterials.TryGetValue(node.Controller, out Material material))
                renderer.sharedMaterial = material;

            controlIndicators[node.Id] = indicator;
        }
    }

    private void RefreshPoliticalControlIndicators()
    {
        foreach (KeyValuePair<string, GameObject> pair in controlIndicators)
        {
            CampaignNodeState node = CampaignSession.GetNode(pair.Key);
            GameObject indicator = pair.Value;
            if (node == null || indicator == null)
                continue;

            indicator.transform.position = new Vector3(node.MapPosition.x, 2.65f, node.MapPosition.y);
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null && controlMaterials.TryGetValue(node.Controller, out Material material))
                renderer.sharedMaterial = material;
        }
    }

    private void ApplyFormationStackOffsets()
    {
        Dictionary<string, List<CampaignFormationState>> byNode =
            new Dictionary<string, List<CampaignFormationState>>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, CampaignFormationState> pair in CampaignSession.Formations)
        {
            CampaignFormationState formation = pair.Value;
            if (formation == null || formation.IsDestroyed || formation.IsMoving || string.IsNullOrEmpty(formation.CurrentNodeId))
                continue;

            if (!byNode.TryGetValue(formation.CurrentNodeId, out List<CampaignFormationState> list))
            {
                list = new List<CampaignFormationState>();
                byNode[formation.CurrentNodeId] = list;
            }
            list.Add(formation);
        }

        CampaignFormationView[] views = UnityEngine.Object.FindObjectsByType<CampaignFormationView>();
        Dictionary<string, CampaignFormationView> viewById =
            new Dictionary<string, CampaignFormationView>(StringComparer.Ordinal);
        foreach (CampaignFormationView view in views)
        {
            if (view != null && !string.IsNullOrEmpty(view.FormationId))
                viewById[view.FormationId] = view;
        }

        foreach (KeyValuePair<string, List<CampaignFormationState>> pair in byNode)
        {
            List<CampaignFormationState> list = pair.Value;
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            if (list.Count <= 1)
                continue;

            float center = (list.Count - 1) * 0.5f;
            for (int i = 0; i < list.Count; i++)
            {
                CampaignFormationState formation = list[i];
                if (!viewById.TryGetValue(formation.Id, out CampaignFormationView view) || view == null)
                    continue;

                float offset = (i - center) * 6.5f;
                view.transform.position += new Vector3(offset, i * 0.35f, 0f);
            }
        }
    }

    private void LogSelection()
    {
        Debug.Log(string.Format(
            "CAMPAIGN-SELECT|Node={0}|Formation={1}",
            selectedNode != null ? selectedNode.Id : "-",
            selectedFormation != null ? selectedFormation.Id : "-"));
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 11,
            padding = new RectOffset(10, 10, 8, 8),
            wordWrap = true
        };
        panelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        titleStyle.normal.textColor = Color.white;

        diagStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(7, 7, 5, 5)
        };
        diagStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawNodeInformation();
        DrawFormationEta();
        DrawDiagnostics();
        DrawControlLegend();
    }

    private void DrawNodeInformation()
    {
        if (selectedNode == null)
            return;

        Rect box = new Rect(Screen.width - 326f, 72f, 310f, 268f);
        GUI.Box(box, string.Empty, panelStyle);
        GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, 24f),
            selectedNode.Name + "  [" + selectedNode.Id + "]", titleStyle);

        string flags = string.Format(
            "Region: {0}\nController: {1}\nTerrain: {2}\nDepot: {3} | Port: {4}\nRail: {5} | Bridge: {6}\nLat/Lon: {7:0.0000}, {8:0.0000}",
            selectedNode.Region,
            selectedNode.Controller,
            selectedNode.Terrain,
            YesNo(selectedNode.HasDepot),
            YesNo(selectedNode.HasPort),
            YesNo(selectedNode.HasRail),
            YesNo(selectedNode.HasBridge),
            selectedNode.Latitude,
            selectedNode.Longitude);

        GUI.Label(new Rect(box.x + 10f, box.y + 36f, box.width - 20f, 112f), flags);

        string links = "Direkte links:";
        int shown = 0;
        foreach (string linkedId in selectedNode.Links)
        {
            CampaignNodeState linked = CampaignSession.GetNode(linkedId);
            if (linked == null)
                continue;
            links += "\n• " + linked.Name + "  [" + GetLinkType(selectedNode, linked) + "]";
            shown++;
            if (shown >= 7)
            {
                if (selectedNode.Links.Count > shown)
                    links += "\n• +" + (selectedNode.Links.Count - shown) + " flere";
                break;
            }
        }

        GUI.Label(new Rect(box.x + 10f, box.y + 150f, box.width - 20f, 108f), links);
    }

    private void DrawFormationEta()
    {
        if (selectedFormation == null)
            return;

        float etaHours = CalculateRemainingEtaHours(selectedFormation);
        string destination = selectedFormation.Route.Count > 0
            ? selectedFormation.Route[selectedFormation.Route.Count - 1]
            : "-";

        Rect box = new Rect(Screen.width - 326f, 350f, 310f, 102f);
        string text = string.Format(
            "FORMATION\n{0} [{1}]\nDestination: {2}\nRemaining ETA: {3}\nF = fokusér kamera | Home = reset view",
            selectedFormation.Name,
            selectedFormation.Id,
            destination,
            FormatHours(etaHours));
        GUI.Box(box, text, panelStyle);
    }

    private void DrawDiagnostics()
    {
        Rect box = new Rect(Screen.width - 286f, Screen.height - 88f, 270f, 72f);
        string text = string.Format(
            "V011 DIAG | Nodes {0}/{1} | Invalid {2} | Broken {3}\nLinks {4}: Road {5} | Rail {6} | Sea {7}\nSelected: {8} / {9}",
            CampaignSession.Nodes.Count,
            ExpectedNodeCount,
            invalidNodeCount,
            brokenLinkCount,
            uniqueLinkCount,
            roadLinkCount,
            railLinkCount,
            seaLinkCount,
            selectedNode != null ? selectedNode.Id : "-",
            selectedFormation != null ? selectedFormation.Id : "-");
        GUI.Box(box, text, diagStyle);
    }

    private void DrawControlLegend()
    {
        Rect box = new Rect(16f, Screen.height - 96f, 230f, 80f);
        GUI.Box(box,
            "POLITICAL CONTROL\nRed: Denmark | Black: Prussia\nBlue: Sweden-Norway | Green: Russia\nBrown: German Confederation",
            diagStyle);
    }

    private static float CalculateRemainingEtaHours(CampaignFormationState formation)
    {
        if (formation == null || formation.Route == null || formation.Route.Count < 2)
            return 0f;

        float total = 0f;
        int edgeStartIndex = Mathf.Clamp(formation.RouteIndex, 0, formation.Route.Count - 1);

        if (!string.IsNullOrEmpty(formation.LegToNodeId) && formation.LegTravelHours > 0f)
        {
            total += Mathf.Max(0f, formation.LegTravelHours - formation.LegProgressHours);
            edgeStartIndex = Mathf.Min(formation.RouteIndex + 1, formation.Route.Count - 1);
        }

        for (int i = edgeStartIndex; i < formation.Route.Count - 1; i++)
        {
            CampaignNodeState from = CampaignSession.GetNode(formation.Route[i]);
            CampaignNodeState to = CampaignSession.GetNode(formation.Route[i + 1]);
            if (from == null || to == null)
                continue;

            float distanceKm = CampaignGeoProjection.ApproximateDistanceKm(
                from.Latitude,
                from.Longitude,
                to.Latitude,
                to.Longitude);
            total += Mathf.Max(0.5f, distanceKm / QaMarchSpeedKmPerHour * GetTerrainTravelFactor(to.Terrain));
        }

        return total;
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

    public static CampaignStrategicLinkType GetLinkType(CampaignNodeState a, CampaignNodeState b)
    {
        if (a == null || b == null)
            return CampaignStrategicLinkType.Road;

        if (seaLinks.Contains(MakeEdgeKey(a.Id, b.Id)))
            return CampaignStrategicLinkType.SeaFerry;

        if (a.HasRail && b.HasRail)
            return CampaignStrategicLinkType.Rail;

        return CampaignStrategicLinkType.Road;
    }

    private static string MakeEdgeKey(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
    }

    private static string YesNo(bool value)
    {
        return value ? "Ja" : "Nej";
    }

    private static string FormatHours(float hours)
    {
        if (hours <= 0.01f)
            return "-";

        int wholeHours = Mathf.FloorToInt(hours);
        int minutes = Mathf.RoundToInt((hours - wholeHours) * 60f);
        if (minutes >= 60)
        {
            wholeHours++;
            minutes -= 60;
        }
        return wholeHours + "t " + minutes.ToString("00") + "m";
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }
}
