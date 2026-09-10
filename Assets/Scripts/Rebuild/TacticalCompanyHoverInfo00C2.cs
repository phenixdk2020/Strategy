using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c3 - Mouse-over Company information retained from c2.
    // UI/presentation only. No selection, movement, combat or OOB writes.
    [DefaultExecutionOrder(700)]
    public sealed class TacticalCompanyHoverInfo00C2 : MonoBehaviour
    {
        private Camera cam;
        private TacticalCompanyEntity00B hovered;
        private RebuildUnitRecord hoveredBattalion;
        private RebuildUnitRecord hoveredRegiment;
        private float nextRefresh;

        private const float RefreshInterval = 0.05f;
        private const float MaxRayDistance = 2500f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyHoverInfo00C2>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C3_HOVER_INFO");
            root.AddComponent<TacticalCompanyHoverInfo00C2>();
        }

        private void Start()
        {
            cam = Camera.main;
            Debug.Log(
                "REBUILD-HOVER-00C3|Installed=True|Mode=MouseOver|Toggle=H|" +
                "WritesSelection=False|WritesMovement=False|WritesCombat=False");
        }

        private void Update()
        {
            if (!TacticalCompanyDrill00C3.HoverInfoEnabled)
            {
                hovered = null;
                hoveredBattalion = null;
                hoveredRegiment = null;
                return;
            }

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            if (Time.unscaledTime < nextRefresh)
                return;

            nextRefresh = Time.unscaledTime + RefreshInterval;
            RefreshHoverTarget();
        }

        private void RefreshHoverTarget()
        {
            hovered = null;
            hoveredBattalion = null;
            hoveredRegiment = null;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxRayDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                    continue;

                TacticalCompanyEntity00B company = collider.GetComponentInParent<TacticalCompanyEntity00B>();
                if (company == null)
                    continue;

                hovered = company;
                break;
            }

            if (hovered == null)
                return;

            RebuildOOBRegistry00B registry = RebuildOOBRegistry00B.Instance;
            if (registry == null)
                return;

            hoveredBattalion = registry.Get(hovered.ParentUnitId);
            if (hoveredBattalion != null && !string.IsNullOrEmpty(hoveredBattalion.ParentUnitId))
                hoveredRegiment = registry.Get(hoveredBattalion.ParentUnitId);
        }

        private void OnGUI()
        {
            if (!TacticalCompanyDrill00C3.HoverInfoEnabled || hovered == null)
                return;

            string nation = hovered.Nation == RebuildNation.Denmark ? "Danmark" : "Preussen";
            string regiment = hoveredRegiment != null ? hoveredRegiment.OfficialName1864 : "<Regiment ukendt>";
            string battalion = hoveredBattalion != null ? hoveredBattalion.OfficialName1864 : "<Bataljon ukendt>";

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            const float width = 355f;
            const float height = 150f;

            float x = Mathf.Clamp(mouse.x + 18f, 8f, Mathf.Max(8f, Screen.width - width - 8f));
            float y = Mathf.Clamp(mouse.y + 18f, 8f, Mathf.Max(8f, Screen.height - height - 8f));
            Rect box = new Rect(x, y, width, height);

            GUI.depth = -980;

            Color previous = GUI.color;
            GUI.color = new Color(0.055f, 0.060f, 0.068f, 0.94f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Box(box, string.Empty);

            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                normal = { textColor = Color.white }
            };

            GUIStyle body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.92f, 0.93f, 0.94f) }
            };

            GUI.Label(new Rect(x + 12f, y + 8f, width - 24f, 22f), regiment, title);
            GUI.Label(new Rect(x + 12f, y + 31f, width - 24f, 20f), battalion + " | " + hovered.DisplayName, body);
            GUI.Label(new Rect(x + 12f, y + 54f, width - 24f, 20f), "Nation: " + nation, body);
            GUI.Label(new Rect(x + 12f, y + 75f, width - 24f, 20f),
                "Styrke: " + hovered.PresentStrength + " / " + hovered.AuthorizedStrength + " | Formation: " + hovered.Formation,
                body);
            GUI.Label(new Rect(x + 12f, y + 96f, width - 24f, 20f), "UnitID: " + hovered.UnitId, body);
            GUI.Label(new Rect(x + 12f, y + 117f, width - 24f, 20f),
                hovered.IsSelected ? "Status: Selected" : "Status: Mouse-over",
                body);
        }
    }
}
