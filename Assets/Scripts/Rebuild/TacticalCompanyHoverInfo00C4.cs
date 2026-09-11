using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c4 - reliable mouse-over Company info.
    // H toggle is owned by TacticalRebuildSettings00C4 and always gives visible feedback.
    [DefaultExecutionOrder(710)]
    public sealed class TacticalCompanyHoverInfo00C4 : MonoBehaviour
    {
        private Camera cam;
        private TacticalCompanyEntity00B hovered;
        private RebuildUnitRecord hoveredBattalion;
        private RebuildUnitRecord hoveredRegiment;
        private float nextRefresh;

        private const float RefreshInterval = 0.04f;
        private const float MaxRayDistance = 2500f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyHoverInfo00C2 old = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyHoverInfo00C2>();
            if (old != null)
                old.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyHoverInfo00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C4_HOVER_INFO");
            root.AddComponent<TacticalCompanyHoverInfo00C4>();
        }

        private void Start()
        {
            cam = Camera.main;
            Debug.Log(
                "REBUILD-HOVER-00C4|Installed=True|Toggle=H|FeedbackToast=True|" +
                "Mode=MouseOver|WritesSelection=False|WritesMovement=False|WritesCombat=False");
        }

        private void Update()
        {
            if (!TacticalRebuildSettings00C4.HoverInfoEnabled)
            {
                ClearHover();
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
            ClearHover();

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

        private void ClearHover()
        {
            hovered = null;
            hoveredBattalion = null;
            hoveredRegiment = null;
        }

        private void OnGUI()
        {
            if (!TacticalRebuildSettings00C4.HoverInfoEnabled || hovered == null)
                return;

            string nation = hovered.Nation == RebuildNation.Denmark ? "Danmark" : "Preussen";
            string regiment = hoveredRegiment != null ? hoveredRegiment.OfficialName1864 : "<Regiment ukendt>";
            string battalion = hoveredBattalion != null ? hoveredBattalion.OfficialName1864 : "<Bataljon ukendt>";
            string drillState = hovered.IsReforming
                ? "Reformer " + hovered.ReformFromFormation + " -> " + hovered.ReformTargetFormation +
                  " (" + Mathf.RoundToInt(hovered.ReformProgress * 100f) + "%)"
                : hovered.IsTurning ? "Drejer" : "Klar";

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            const float width = 370f;
            const float height = 174f;

            float x = Mathf.Clamp(mouse.x + 18f, 8f, Mathf.Max(8f, Screen.width - width - 8f));
            float y = Mathf.Clamp(mouse.y + 18f, 8f, Mathf.Max(8f, Screen.height - height - 8f));
            Rect box = new Rect(x, y, width, height);

            GUI.depth = -985;
            Color previous = GUI.color;
            GUI.color = new Color(0.045f, 0.050f, 0.058f, 0.95f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Box(box, string.Empty);

            GUIStyle title = new GUIStyle(GUI.skin.label);
            title.fontStyle = FontStyle.Bold;
            title.fontSize = 13;
            title.normal.textColor = Color.white;

            GUIStyle body = new GUIStyle(GUI.skin.label);
            body.fontSize = 11;
            body.normal.textColor = new Color(0.92f, 0.93f, 0.94f);

            GUI.Label(new Rect(x + 12f, y + 8f, width - 24f, 22f), regiment, title);
            GUI.Label(new Rect(x + 12f, y + 31f, width - 24f, 20f), battalion + " | " + hovered.DisplayName, body);
            GUI.Label(new Rect(x + 12f, y + 53f, width - 24f, 20f), "Nation: " + nation, body);
            GUI.Label(new Rect(x + 12f, y + 75f, width - 24f, 20f),
                "Styrke: " + hovered.PresentStrength + " / " + hovered.AuthorizedStrength +
                " | Formation: " + hovered.Formation, body);
            GUI.Label(new Rect(x + 12f, y + 97f, width - 24f, 20f),
                "Grafik: 1:" + TacticalRebuildSettings00C4.SoldierVisualDenominator +
                " | Synlige repr.: " + TacticalRebuildSettings00C4.GetVisibleRepresentativeCount(hovered.PresentStrength), body);
            GUI.Label(new Rect(x + 12f, y + 119f, width - 24f, 20f), "Drill: " + drillState, body);
            GUI.Label(new Rect(x + 12f, y + 141f, width - 24f, 20f), "UnitID: " + hovered.UnitId, body);
        }
    }
}
