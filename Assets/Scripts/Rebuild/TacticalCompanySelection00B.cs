using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // Gate B selection owner retained through Gate D1.
    // Selection still owns only selection; D1 order/movement is handled by TacticalCompanyOrder00D1 + Company state.
    public sealed class TacticalCompanySelection00B : MonoBehaviour
    {
        public static TacticalCompanySelection00B Instance { get; private set; }
        public IReadOnlyList<TacticalCompanyEntity00B> Selected => selected;

        private readonly List<TacticalCompanyEntity00B> allCompanies = new List<TacticalCompanyEntity00B>();
        private readonly List<TacticalCompanyEntity00B> selected = new List<TacticalCompanyEntity00B>();

        private Camera cam;
        private bool leftTracking;
        private bool leftDragging;
        private Vector2 leftStart;
        private Vector2 leftCurrent;
        private bool shiftAtDown;
        private bool ctrlAtDown;
        private string transientMessage = string.Empty;
        private float transientUntil;

        private const float DragThresholdPixels = 8f;

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
            RefreshCompanies();
            Debug.Log(
                "REBUILD-SELECT-00D1|Installed=True|Owner=Single|" +
                "LMB=Select|Shift=Add|Ctrl=Toggle|Drag=Box|RMBOwnedByGateD1=True");
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

            if (Input.GetKeyDown(KeyCode.Escape))
                ClearSelection();

            if (Input.GetMouseButtonDown(0))
            {
                leftTracking = true;
                leftDragging = false;
                leftStart = Input.mousePosition;
                leftCurrent = leftStart;
                shiftAtDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                ctrlAtDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            }

            if (leftTracking && Input.GetMouseButton(0))
            {
                leftCurrent = Input.mousePosition;
                if (!leftDragging && Vector2.Distance(leftStart, leftCurrent) >= DragThresholdPixels)
                    leftDragging = true;
            }

            if (leftTracking && Input.GetMouseButtonUp(0))
            {
                leftCurrent = Input.mousePosition;
                if (leftDragging)
                    CompleteBoxSelection();
                else
                    CompletePointSelection();

                leftTracking = false;
                leftDragging = false;
            }

            // RMB is intentionally not consumed here in Gate D1.
            // TacticalCompanyOrder00D1 owns destination/facing ghost and order confirmation.
        }

        public void RefreshCompanies()
        {
            allCompanies.Clear();
            TacticalCompanyEntity00B[] found = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    allCompanies.Add(found[i]);
            }
        }

        private void CompletePointSelection()
        {
            TacticalCompanyEntity00B hit = GetCompanyUnderMouse();

            if (hit == null || hit.Nation != RebuildNation.Denmark)
            {
                if (!shiftAtDown && !ctrlAtDown)
                    ClearSelection();
                return;
            }

            if (ctrlAtDown)
            {
                if (selected.Contains(hit))
                    Remove(hit);
                else
                    Add(hit);
            }
            else if (shiftAtDown)
            {
                Add(hit);
            }
            else
            {
                ClearSelection();
                Add(hit);
            }

            Debug.Log(
                "REBUILD-SELECT-00D1|Point=True|UnitID=" + hit.UnitId +
                "|SelectedCount=" + selected.Count +
                "|Mode=" + (ctrlAtDown ? "Toggle" : shiftAtDown ? "Add" : "Replace"));
        }

        private void CompleteBoxSelection()
        {
            Rect rect = ScreenRect(leftStart, leftCurrent);
            if (!shiftAtDown && !ctrlAtDown)
                ClearSelection();

            int inside = 0;
            for (int i = 0; i < allCompanies.Count; i++)
            {
                TacticalCompanyEntity00B company = allCompanies[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;

                Vector3 screen = cam.WorldToScreenPoint(company.transform.position);
                if (screen.z <= 0f)
                    continue;

                if (!rect.Contains(new Vector2(screen.x, screen.y), true))
                    continue;

                inside++;
                if (ctrlAtDown && selected.Contains(company))
                    Remove(company);
                else
                    Add(company);
            }

            Debug.Log(
                "REBUILD-SELECT-00D1|Box=True|Inside=" + inside +
                "|SelectedCount=" + selected.Count +
                "|Mode=" + (ctrlAtDown ? "Toggle" : shiftAtDown ? "Add" : "Replace"));
        }

        private TacticalCompanyEntity00B GetCompanyUnderMouse()
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 2500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                TacticalCompanyEntity00B company = hits[i].collider.GetComponentInParent<TacticalCompanyEntity00B>();
                if (company != null)
                    return company;
            }

            return null;
        }

        private void Add(TacticalCompanyEntity00B company)
        {
            if (company == null || company.Nation != RebuildNation.Denmark)
                return;
            if (!selected.Contains(company))
                selected.Add(company);
            company.SetSelected(true);
        }

        private void Remove(TacticalCompanyEntity00B company)
        {
            if (company == null)
                return;
            selected.Remove(company);
            company.SetSelected(false);
        }

        private void ClearSelection()
        {
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (selected[i] != null)
                    selected[i].SetSelected(false);
            }
            selected.Clear();
        }

        private static Rect ScreenRect(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float xMax = Mathf.Max(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void OnGUI()
        {
            if (leftTracking && leftDragging)
                DrawSelectionBox(leftStart, leftCurrent);

            if (!string.IsNullOrEmpty(transientMessage) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -970;
                GUI.Box(new Rect(12f, 118f, 620f, 30f), transientMessage);
            }
        }

        private static void DrawSelectionBox(Vector2 start, Vector2 current)
        {
            Rect screen = ScreenRect(start, current);
            Rect gui = new Rect(screen.xMin, Screen.height - screen.yMax, screen.width, screen.height);

            Color previous = GUI.color;
            GUI.color = new Color(0.9f, 0.75f, 0.2f, 0.22f);
            GUI.DrawTexture(gui, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            GUI.DrawTexture(new Rect(gui.x, gui.y, gui.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.x, gui.yMax - 2f, gui.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.x, gui.y, 2f, gui.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.xMax - 2f, gui.y, 2f, gui.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
