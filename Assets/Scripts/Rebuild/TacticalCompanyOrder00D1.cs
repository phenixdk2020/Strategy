using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00d1 - first real Company order preview + movement order owner.
    // RMB places the destination centre; dragging defines final facing.
    // F while the ghost is active switches target formation before confirmation.
    [DefaultExecutionOrder(640)]
    public sealed class TacticalCompanyOrder00D1 : MonoBehaviour
    {
        public static TacticalCompanyOrder00D1 Instance { get; private set; }
        public static bool GhostActive => Instance != null && Instance.ghostActive;

        private const float MarchSpeed = 1.35f;
        private const float TurnSpeed = 55f;
        private const float MinimumReformSeconds = 2.2f;
        private const float FacingDragThreshold = 0.55f;
        private const float MaxRayDistance = 3000f;

        private readonly List<TacticalCompanyEntity00B> companies = new List<TacticalCompanyEntity00B>();
        private readonly List<Vector3> offsets = new List<Vector3>();
        private readonly List<Quaternion> relativeRotations = new List<Quaternion>();
        private readonly List<LineRenderer> ghostRects = new List<LineRenderer>();
        private readonly List<LineRenderer> ghostArrows = new List<LineRenderer>();

        private Camera cam;
        private bool ghostActive;
        private Vector3 anchor;
        private Vector3 selectionCentre;
        private Quaternion referenceRotation = Quaternion.identity;
        private Quaternion targetRotation = Quaternion.identity;
        private RebuildFormation targetFormation = RebuildFormation.Line;
        private Material ghostMaterial;
        private Material arrowMaterial;

        private string transient = string.Empty;
        private float transientUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyDrill00C4 oldDrill = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C4>();
            if (oldDrill != null)
                oldDrill.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyOrder00D1>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00D1_GHOST_ORDER");
            root.AddComponent<TacticalCompanyOrder00D1>();
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

        private void Start()
        {
            cam = Camera.main;
            ghostMaterial = CreateLineMaterial(new Color(0.30f, 0.95f, 0.55f, 0.95f), "00D1_Ghost");
            arrowMaterial = CreateLineMaterial(new Color(0.95f, 0.90f, 0.25f, 0.98f), "00D1_Facing");

            Debug.Log(
                "REBUILD-ORDER-00D1|Installed=True|RMB=GhostCentre+DragFacing+ReleaseConfirm|" +
                "FWhileGhost=LineColumn|MoveSpeed=" + MarchSpeed.ToString("0.00") +
                "|TurnSpeed=" + TurnSpeed.ToString("0") +
                "|OneCompanyMovementOwner=True|ObstacleNavigation=False");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            if (ghostActive)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelGhost("Order annulleret");
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    targetFormation = targetFormation == RebuildFormation.Line
                        ? RebuildFormation.Column
                        : RebuildFormation.Line;
                    UpdateGhostVisuals();
                    ShowTransient("Ghost formation: " + targetFormation);
                    Debug.Log("REBUILD-ORDER-00D1|GhostFormation=" + targetFormation);
                }

                if (Input.GetMouseButton(1))
                {
                    if (TryGetGroundPoint(out Vector3 current))
                    {
                        Vector3 forward = current - anchor;
                        forward.y = 0f;
                        if (forward.magnitude >= FacingDragThreshold)
                            targetRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                    }
                    UpdateGhostVisuals();
                }

                if (Input.GetMouseButtonUp(1))
                    CommitGhostOrder();

                return;
            }

            if (Input.GetMouseButtonDown(1))
                BeginGhostFromSelection();
        }

        private void BeginGhostFromSelection()
        {
            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            if (selection == null || selection.Selected == null || selection.Selected.Count == 0)
                return;

            if (!TryGetGroundPoint(out Vector3 point))
                return;

            companies.Clear();
            offsets.Clear();
            relativeRotations.Clear();

            selectionCentre = Vector3.zero;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;
                companies.Add(company);
                selectionCentre += company.transform.position;
            }

            if (companies.Count == 0)
                return;

            selectionCentre /= companies.Count;
            referenceRotation = companies[0].transform.rotation;
            targetRotation = referenceRotation;
            targetFormation = companies[0].Formation;
            anchor = point;
            anchor.y = selectionCentre.y;

            Quaternion invRef = Quaternion.Inverse(referenceRotation);
            for (int i = 0; i < companies.Count; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                offsets.Add(invRef * (company.transform.position - selectionCentre));
                relativeRotations.Add(invRef * company.transform.rotation);
            }

            EnsureGhostCount(companies.Count);
            ghostActive = true;
            SetGhostVisibility(true);
            UpdateGhostVisuals();

            ShowTransient("Ghost aktiv: hold RMB og træk for facing | F Line/Column | slip RMB = ordre");
            Debug.Log(
                "REBUILD-ORDER-00D1|GhostStart=True|Selected=" + companies.Count +
                "|Target=" + anchor.ToString("F2") +
                "|Formation=" + targetFormation);
        }

        private void CommitGhostOrder()
        {
            if (!ghostActive)
                return;

            int issued = 0;
            Quaternion groupDelta = targetRotation;

            for (int i = 0; i < companies.Count; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                Vector3 targetPos = anchor + groupDelta * offsets[i];
                targetPos.y = company.transform.position.y;
                Quaternion targetRot = groupDelta * relativeRotations[i];

                if (company.BeginMoveOrder(
                    targetPos,
                    targetRot,
                    targetFormation,
                    MarchSpeed,
                    TurnSpeed,
                    MinimumReformSeconds))
                {
                    issued++;
                }
            }

            Debug.Log(
                "REBUILD-ORDER-00D1|Commit=True|Issued=" + issued +
                "|Formation=" + targetFormation +
                "|Facing=" + targetRotation.eulerAngles.y.ToString("0.0"));

            ShowTransient("Ordre givet til " + issued + " Company(s) — mændene går til ghost-positionen");
            EndGhost();
        }

        private void CancelGhost(string message)
        {
            Debug.Log("REBUILD-ORDER-00D1|GhostCancel=True");
            ShowTransient(message);
            EndGhost();
        }

        private void EndGhost()
        {
            ghostActive = false;
            SetGhostVisibility(false);
            companies.Clear();
            offsets.Clear();
            relativeRotations.Clear();
        }

        private void UpdateGhostVisuals()
        {
            if (!ghostActive)
                return;

            for (int i = 0; i < companies.Count; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                Vector3 centre = anchor + targetRotation * offsets[i];
                centre.y = company.transform.position.y + 0.24f;
                Quaternion rotation = targetRotation * relativeRotations[i];

                company.GetFootprintDimensions(targetFormation, out float width, out float depth);
                DrawGhostRect(ghostRects[i], centre, rotation, width, depth);
                DrawFacingArrow(ghostArrows[i], centre, rotation, Mathf.Max(3.0f, depth * 0.65f + 2.0f));
            }
        }

        private static void DrawGhostRect(LineRenderer line, Vector3 centre, Quaternion rotation, float width, float depth)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            Vector3[] local =
            {
                new Vector3(-hw, 0f, -hd),
                new Vector3(-hw, 0f, hd),
                new Vector3(hw, 0f, hd),
                new Vector3(hw, 0f, -hd)
            };

            for (int p = 0; p < 4; p++)
                line.SetPosition(p, centre + rotation * local[p]);
        }

        private static void DrawFacingArrow(LineRenderer line, Vector3 centre, Quaternion rotation, float length)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 tip = centre + forward * length;
            Vector3 wingBase = tip - forward * 0.85f;

            line.SetPosition(0, centre);
            line.SetPosition(1, tip);
            line.SetPosition(2, wingBase + right * 0.45f);
            line.SetPosition(3, tip);
            line.SetPosition(4, wingBase - right * 0.45f);
        }

        private void EnsureGhostCount(int count)
        {
            while (ghostRects.Count < count)
            {
                GameObject rectGo = new GameObject("00D1_GhostRect_" + ghostRects.Count);
                rectGo.transform.SetParent(transform, false);
                LineRenderer rect = rectGo.AddComponent<LineRenderer>();
                rect.useWorldSpace = true;
                rect.loop = true;
                rect.positionCount = 4;
                rect.startWidth = 0.16f;
                rect.endWidth = 0.16f;
                rect.material = ghostMaterial;
                ghostRects.Add(rect);

                GameObject arrowGo = new GameObject("00D1_GhostFacing_" + ghostArrows.Count);
                arrowGo.transform.SetParent(transform, false);
                LineRenderer arrow = arrowGo.AddComponent<LineRenderer>();
                arrow.useWorldSpace = true;
                arrow.loop = false;
                arrow.positionCount = 5;
                arrow.startWidth = 0.13f;
                arrow.endWidth = 0.13f;
                arrow.material = arrowMaterial;
                ghostArrows.Add(arrow);
            }
        }

        private void SetGhostVisibility(bool visible)
        {
            for (int i = 0; i < ghostRects.Count; i++)
            {
                bool active = visible && i < companies.Count;
                ghostRects[i].enabled = active;
                ghostArrows[i].enabled = active;
            }
        }

        private bool TryGetGroundPoint(out Vector3 point)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxRayDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                    continue;
                if (collider.GetComponentInParent<TacticalCompanyEntity00B>() != null)
                    continue;

                point = hits[i].point;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void ShowTransient(string message)
        {
            transient = message;
            transientUntil = Time.unscaledTime + 3.0f;
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(transient) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -998;
                GUI.Box(new Rect(Screen.width * 0.5f - 285f, 38f, 570f, 28f), transient);
            }

            if (!TacticalRebuildSettings00C4.DebugViewEnabled)
                return;

            GUI.depth = -962;
            Rect box = new Rect(12f, 222f, 650f, 66f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "D1 ORDER | RMB destination | hold+drag = final facing | F while ghost = Line/Column | release RMB = GO");
            GUI.Label(new Rect(box.x + 10f, box.y + 30f, box.width - 20f, 20f),
                ghostActive
                    ? "Ghost ACTIVE | Target formation: " + targetFormation + " | soldiers will march/reform to preview"
                    : "Ghost idle | Obstacle/bridge navigation comes after this direct-movement gate");
        }

        private static Material CreateLineMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            return material;
        }
    }
}
