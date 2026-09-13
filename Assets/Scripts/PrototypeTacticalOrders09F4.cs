using System.Collections.Generic;
using UnityEngine;

public enum PrototypeTacticalOrderMode09F4
{
    None,
    DefendHere,
    Attack,
    CaptureHere
}

// v00.00.09f4 explicit tactical order layer.
// Order selection is made from the F9 companion panel, then the player left-clicks
// a ground position (Defend/Capture) or an enemy regiment (Attack).
[DefaultExecutionOrder(120)]
public sealed class PrototypeTacticalOrders09F4 : MonoBehaviour
{
    private sealed class GroundOrder
    {
        public PrototypeTacticalOrderMode09F4 Mode;
        public Vector3 Point;
        public bool Arrived;
    }

    public static PrototypeTacticalOrders09F4 Instance { get; private set; }
    public PrototypeTacticalOrderMode09F4 PendingMode { get; private set; }
    public string StatusText { get; private set; } = string.Empty;

    private readonly Dictionary<Regiment, GroundOrder> groundOrders =
        new Dictionary<Regiment, GroundOrder>();

    private Camera cam;
    private bool commanderSuppressed;
    private bool boxSelectionSuppressed;
    private bool restoreOnMouseRelease;
    private GUIStyle orderStyle;
    private float statusUntil;

    private const float ArrivalDistance = 4.5f;
    private const float DeployDistance = 14f;
    private const float MarchColumnDistance = 24f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeTacticalOrders09F4>() != null)
            return;

        GameObject root = new GameObject("PrototypeTacticalOrders_v000009f4");
        root.AddComponent<PrototypeTacticalOrders09F4>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("ORDER-09F4|Installed=True|Orders=DefendHere,Attack,CaptureHere");
    }

    private void OnDestroy()
    {
        RestorePointerHandlers();
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (PendingMode != PrototypeTacticalOrderMode09F4.None)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPendingOrder("Ordre annulleret");
            }
            else if (Input.GetMouseButtonDown(0) && !IsPointerOverExistingUI())
            {
                SuppressPointerHandlers();
                restoreOnMouseRelease = true;
                TryResolvePendingOrder();
            }
        }

        if (restoreOnMouseRelease && Input.GetMouseButtonUp(0))
        {
            restoreOnMouseRelease = false;
            RestorePointerHandlers();
        }

        UpdateGroundOrders();
    }

    public void BeginOrder(PrototypeTacticalOrderMode09F4 mode)
    {
        if (mode == PrototypeTacticalOrderMode09F4.None)
            return;

        if (GetSelectedDanish().Count == 0)
        {
            SetStatus("Vælg mindst ét dansk regiment først", 3f);
            return;
        }

        PendingMode = mode;

        switch (mode)
        {
            case PrototypeTacticalOrderMode09F4.DefendHere:
                SetStatus("FORSVAR HER: venstreklik på positionen", 30f);
                break;
            case PrototypeTacticalOrderMode09F4.Attack:
                SetStatus("ANGRIB: venstreklik på fjendtligt regiment", 30f);
                break;
            case PrototypeTacticalOrderMode09F4.CaptureHere:
                SetStatus("EROBR HER: venstreklik på positionen", 30f);
                break;
        }

        Debug.Log("ORDER-09F4|Pending=" + mode + "|Selected=" + GetSelectedDanish().Count);
    }

    public void CancelPendingOrder(string reason)
    {
        PendingMode = PrototypeTacticalOrderMode09F4.None;
        SetStatus(reason, 2.5f);
        RestorePointerHandlers();
    }

    private void TryResolvePendingOrder()
    {
        if (cam == null)
        {
            SetStatus("Kamera ikke klar", 2.5f);
            return;
        }

        List<Regiment> selected = GetSelectedDanish();
        if (selected.Count == 0)
        {
            CancelPendingOrder("Ingen danske enheder valgt");
            return;
        }

        if (PendingMode == PrototypeTacticalOrderMode09F4.Attack)
        {
            Regiment target = GetEnemyUnderMouse();
            if (target == null)
            {
                SetStatus("ANGRIB: klik direkte på et fjendtligt regiment", 3f);
                return;
            }

            foreach (Regiment regiment in selected)
                IssueAttackOrder(regiment, target);

            CreateOrderMarker(target.transform.position, PrototypeTacticalOrderMode09F4.Attack);
            Debug.Log(
                "ORDER-09F4|Type=ATTACK|Target=" + target.RegimentName +
                "|Units=" + selected.Count);

            PendingMode = PrototypeTacticalOrderMode09F4.None;
            SetStatus("Angreb beordret mod " + target.RegimentName, 3f);
            return;
        }

        if (!TryGetGroundPoint(out Vector3 point))
        {
            SetStatus("Klik på slagmarkens terræn", 3f);
            return;
        }

        Vector3 line = selected.Count > 1
            ? Vector3.Cross(Vector3.up, GetAverageFacing(selected)).normalized
            : Vector3.right;
        if (line.sqrMagnitude < 0.01f)
            line = Vector3.right;

        float centerIndex = (selected.Count - 1) * 0.5f;
        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            Vector3 destination = point + line * ((i - centerIndex) * 12f);
            destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;
            IssueGroundOrder(regiment, PendingMode, destination);
        }

        CreateOrderMarker(point, PendingMode);
        Debug.Log(
            "ORDER-09F4|Type=" + PendingMode +
            "|Point=(" + point.x.ToString("0.0") + "," + point.z.ToString("0.0") + ")" +
            "|Units=" + selected.Count);

        string completedLabel = PendingMode == PrototypeTacticalOrderMode09F4.DefendHere
            ? "Forsvarsposition beordret"
            : "Erobring beordret";

        PendingMode = PrototypeTacticalOrderMode09F4.None;
        SetStatus(completedLabel, 3f);
    }

    private static void IssueAttackOrder(Regiment regiment, Regiment target)
    {
        if (regiment == null || target == null)
            return;

        PrototypeTacticalOrders09F4 manager = Instance;
        if (manager != null)
            manager.groundOrders.Remove(regiment);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null)
        {
            controller.SetDoctrine(OfficerAIDoctrine.Offensive);
            controller.SetAIEnabled(true);
            controller.SetAttackMission(target);
        }
        else
        {
            regiment.OrderAttack(target);
        }
    }

    private void IssueGroundOrder(
        Regiment regiment,
        PrototypeTacticalOrderMode09F4 mode,
        Vector3 destination)
    {
        if (regiment == null)
            return;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled)
            controller.SetAIEnabled(false);

        float distance = PlanarDistance(regiment.transform.position, destination);
        if (distance > MarchColumnDistance)
            regiment.SetFormation(RegimentFormation.Column);

        regiment.OrderMove(destination);
        groundOrders[regiment] = new GroundOrder
        {
            Mode = mode,
            Point = destination,
            Arrived = false
        };
    }

    private void UpdateGroundOrders()
    {
        if (groundOrders.Count == 0)
            return;

        List<Regiment> completed = null;

        foreach (KeyValuePair<Regiment, GroundOrder> pair in groundOrders)
        {
            Regiment regiment = pair.Key;
            GroundOrder order = pair.Value;

            if (regiment == null || regiment.IsRouted)
            {
                if (completed == null)
                    completed = new List<Regiment>();
                completed.Add(regiment);
                continue;
            }

            float distance = PlanarDistance(regiment.transform.position, order.Point);

            if (distance > DeployDistance)
            {
                if (distance > MarchColumnDistance && regiment.Formation != RegimentFormation.Column)
                    regiment.SetFormation(RegimentFormation.Column);

                // Reassert the strategic intent every frame. The v09f3 river layer
                // may temporarily rewrite the steering destination to bridge entry/exit.
                // It runs later and therefore still owns the actual bridge crossing.
                regiment.OrderMove(order.Point);
                continue;
            }

            if (regiment.Formation != RegimentFormation.Line)
                regiment.SetFormation(RegimentFormation.Line);

            if (distance > ArrivalDistance)
            {
                regiment.OrderMove(order.Point);
                continue;
            }

            regiment.OrderHold();
            order.Arrived = true;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
            {
                controller.SetDoctrine(OfficerAIDoctrine.Defensive);
                controller.SetAIEnabled(true);
            }

            string state = order.Mode == PrototypeTacticalOrderMode09F4.DefendHere
                ? "DEFENDING"
                : "CAPTURED";

            Debug.Log(
                "ORDER-09F4|Unit=" + regiment.RegimentName +
                "|State=" + state +
                "|Point=(" + order.Point.x.ToString("0.0") + "," + order.Point.z.ToString("0.0") + ")");

            if (completed == null)
                completed = new List<Regiment>();
            completed.Add(regiment);
        }

        if (completed == null)
            return;

        for (int i = 0; i < completed.Count; i++)
            groundOrders.Remove(completed[i]);
    }

    private Regiment GetEnemyUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1600f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Regiment regiment = hit.collider.GetComponentInParent<Regiment>();
            if (regiment != null && regiment.Team == BattleTeam.Prussia && !regiment.IsRouted)
                return regiment;
        }

        return null;
    }

    private bool TryGetGroundPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1600f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;

            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        return false;
    }

    private static List<Regiment> GetSelectedDanish()
    {
        List<Regiment> selected = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return selected;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null ||
                regiment.Team != BattleTeam.Denmark ||
                !regiment.IsSelected ||
                regiment.IsRouted)
            {
                continue;
            }
            selected.Add(regiment);
        }

        return selected;
    }

    private static Vector3 GetAverageFacing(List<Regiment> regiments)
    {
        Vector3 sum = Vector3.zero;
        foreach (Regiment regiment in regiments)
        {
            if (regiment == null)
                continue;
            Vector3 forward = regiment.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
                sum += forward.normalized;
        }

        if (sum.sqrMagnitude < 0.01f)
            return Vector3.forward;
        return sum.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool IsPointerOverExistingUI()
    {
        BattleManager battle = BattleManager.Instance;
        return battle != null && battle.IsPointerOverSimulationControls(Input.mousePosition);
    }

    private void SuppressPointerHandlers()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && commander.enabled)
        {
            commander.enabled = false;
            commanderSuppressed = true;
        }

        PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
        if (box != null && box.enabled)
        {
            box.enabled = false;
            boxSelectionSuppressed = true;
        }
    }

    private void RestorePointerHandlers()
    {
        if (commanderSuppressed)
        {
            PlayerCommander commander = PlayerCommander.Instance;
            if (commander != null)
                commander.enabled = true;
            commanderSuppressed = false;
        }

        if (boxSelectionSuppressed)
        {
            PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
            if (box != null)
                box.enabled = true;
            boxSelectionSuppressed = false;
        }
    }

    private void CreateOrderMarker(Vector3 point, PrototypeTacticalOrderMode09F4 mode)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.08f;

        GameObject root = new GameObject("OrderMarker09F4_" + mode);
        root.transform.position = point;

        Color color = mode == PrototypeTacticalOrderMode09F4.DefendHere
            ? new Color(0.20f, 0.50f, 0.95f)
            : mode == PrototypeTacticalOrderMode09F4.CaptureHere
                ? new Color(0.18f, 0.72f, 0.30f)
                : new Color(0.88f, 0.18f, 0.12f);

        Material material = PrototypeBootstrap.CreateSharedMaterial(color, "OrderMarker09F4_" + mode);

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        disc.transform.localScale = new Vector3(2.1f, 0.025f, 2.1f);
        disc.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(disc.GetComponent<Collider>());

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        pole.transform.localScale = new Vector3(0.035f, 1.10f, 0.035f);
        pole.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(pole.GetComponent<Collider>());

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.transform.SetParent(root.transform, false);
        flag.transform.localPosition = new Vector3(0.62f, 1.85f, 0f);
        flag.transform.localScale = new Vector3(1.20f, 0.55f, 0.035f);
        flag.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(flag.GetComponent<Collider>());

        Destroy(root, mode == PrototypeTacticalOrderMode09F4.CaptureHere ? 60f : 20f);
    }

    private void SetStatus(string text, float seconds)
    {
        StatusText = text ?? string.Empty;
        statusUntil = Time.unscaledTime + Mathf.Max(0.2f, seconds);
    }

    private void EnsureStyle()
    {
        if (orderStyle != null)
            return;

        orderStyle = new GUIStyle(GUI.skin.box);
        orderStyle.fontSize = 12;
        orderStyle.fontStyle = FontStyle.Bold;
        orderStyle.alignment = TextAnchor.MiddleCenter;
        orderStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(StatusText) || Time.unscaledTime > statusUntil)
            return;

        EnsureStyle();
        float width = Mathf.Min(520f, Screen.width - 40f);
        GUI.Box(
            new Rect((Screen.width - width) * 0.5f, 40f, width, 34f),
            StatusText + (PendingMode != PrototypeTacticalOrderMode09F4.None ? "   [ESC = annuller]" : string.Empty),
            orderStyle);
    }
}
