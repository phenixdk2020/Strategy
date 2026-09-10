using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c3 - Drill-only controls.
    // This component may change only Company formation/facing for selected Companies.
    // It does not own translation, routes, navigation, combat, AI or OOB identity.
    [DefaultExecutionOrder(650)]
    public sealed class TacticalCompanyDrill00C3 : MonoBehaviour
    {
        public static bool DebugViewEnabled { get; private set; } = true;
        public static bool HoverInfoEnabled { get; private set; } = true;

        private const float FacingStepDegrees = 15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C3>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C3_DRILL");
            root.AddComponent<TacticalCompanyDrill00C3>();
        }

        private void Start()
        {
            Debug.Log(
                "REBUILD-DRILL-00C3|Installed=True|FormationToggle=F|RotateLeft=Z|RotateRight=X|" +
                "DebugView=V|HoverToggle=H|TranslationWrites=False|CombatWrites=False|AIWrites=False");
        }

        private void Update()
        {
            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;

            if (Input.GetKeyDown(KeyCode.V))
            {
                DebugViewEnabled = !DebugViewEnabled;
                Debug.Log("REBUILD-DRILL-00C3|DebugView=" + DebugViewEnabled);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                HoverInfoEnabled = !HoverInfoEnabled;
                Debug.Log("REBUILD-DRILL-00C3|HoverInfo=" + HoverInfoEnabled);
            }

            if (selection == null || selection.Selected == null || selection.Selected.Count == 0)
                return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                int changed = 0;
                for (int i = 0; i < selection.Selected.Count; i++)
                {
                    TacticalCompanyEntity00B company = selection.Selected[i];
                    if (company == null || company.Nation != RebuildNation.Denmark)
                        continue;

                    RebuildFormation next = company.Formation == RebuildFormation.Line
                        ? RebuildFormation.Column
                        : RebuildFormation.Line;
                    company.SetFormationForDrill(next);
                    changed++;
                }

                Debug.Log("REBUILD-DRILL-00C3|Action=FormationToggle|Changed=" + changed);
            }

            if (Input.GetKeyDown(KeyCode.Z))
                RotateSelected(selection, -FacingStepDegrees);

            if (Input.GetKeyDown(KeyCode.X))
                RotateSelected(selection, FacingStepDegrees);
        }

        private static void RotateSelected(TacticalCompanySelection00B selection, float degrees)
        {
            int changed = 0;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;

                Vector3 before = company.transform.position;
                company.transform.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * company.transform.rotation;
                company.transform.position = before;
                changed++;
            }

            Debug.Log(
                "REBUILD-DRILL-00C3|Action=RotateInPlace|Degrees=" + degrees.ToString("0") +
                "|Changed=" + changed + "|Translation=False");
        }

        private void OnGUI()
        {
            if (!DebugViewEnabled)
                return;

            GUI.depth = -960;
            Rect box = new Rect(12f, 154f, 520f, 65f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "C3 DRILL | F Line/Column | Z/X facing +/-15° | V Clean/Debug | H Hover");
            GUI.Label(new Rect(box.x + 10f, box.y + 29f, box.width - 20f, 20f),
                "Movement/navigation/combat/AI remain OFF. RMB is still deferred to Gate D.");
        }
    }
}
