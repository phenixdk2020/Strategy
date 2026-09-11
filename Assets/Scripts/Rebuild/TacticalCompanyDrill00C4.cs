using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c4 - Animated drill controls only.
    // Formation/facing changes are animated in place. No translation/navigation/combat/AI.
    [DefaultExecutionOrder(660)]
    public sealed class TacticalCompanyDrill00C4 : MonoBehaviour
    {
        private const float FacingStepDegrees = 15f;
        private const float FormationTransitionSeconds = 2.2f;
        private const float TurnTransitionSeconds = 0.75f;

        private string transientMessage = string.Empty;
        private float transientUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            TacticalCompanyDrill00C3 old = UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C3>();
            if (old != null)
                old.enabled = false;

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanyDrill00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C4_ANIMATED_DRILL");
            root.AddComponent<TacticalCompanyDrill00C4>();
        }

        private void Start()
        {
            Debug.Log(
                "REBUILD-DRILL-00C4|Installed=True|AnimatedFormation=True|AnimatedFacing=True|" +
                "FormationToggle=F|RotateLeft=Z|RotateRight=X|" +
                "FormationSeconds=" + FormationTransitionSeconds.ToString("0.00") +
                "|TurnSeconds=" + TurnTransitionSeconds.ToString("0.00") +
                "|TranslationWrites=False|CombatWrites=False|AIWrites=False");
        }

        private void Update()
        {
            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            if (selection == null || selection.Selected == null || selection.Selected.Count == 0)
                return;

            if (Input.GetKeyDown(KeyCode.F))
                ToggleFormation(selection);

            if (Input.GetKeyDown(KeyCode.Z))
                RotateSelected(selection, -FacingStepDegrees);

            if (Input.GetKeyDown(KeyCode.X))
                RotateSelected(selection, FacingStepDegrees);
        }

        private void ToggleFormation(TacticalCompanySelection00B selection)
        {
            int changed = 0;
            int busy = 0;

            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;

                if (company.IsReforming)
                {
                    busy++;
                    continue;
                }

                RebuildFormation next = company.Formation == RebuildFormation.Line
                    ? RebuildFormation.Column
                    : RebuildFormation.Line;

                if (company.BeginFormationDrill(next, FormationTransitionSeconds))
                    changed++;
            }

            transientMessage = changed > 0
                ? "Reformering startet: " + changed + " Company(s)"
                : busy > 0 ? "Company er allerede ved at reformere" : "Ingen Company ændret";
            transientUntil = Time.unscaledTime + 2.0f;

            Debug.Log(
                "REBUILD-DRILL-00C4|Action=AnimatedFormationToggle|Changed=" + changed +
                "|Busy=" + busy + "|Duration=" + FormationTransitionSeconds.ToString("0.00"));
        }

        private void RotateSelected(TacticalCompanySelection00B selection, float degrees)
        {
            int changed = 0;
            int busy = 0;

            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark)
                    continue;

                if (company.IsTurning)
                {
                    busy++;
                    continue;
                }

                if (company.BeginTurnDrill(degrees, TurnTransitionSeconds))
                    changed++;
            }

            transientMessage = changed > 0
                ? "Drejning " + (degrees < 0f ? "venstre" : "højre") + ": " + changed + " Company(s)"
                : busy > 0 ? "Company er allerede ved at dreje" : "Ingen Company ændret";
            transientUntil = Time.unscaledTime + 1.6f;

            Debug.Log(
                "REBUILD-DRILL-00C4|Action=AnimatedTurn|Degrees=" + degrees.ToString("0") +
                "|Changed=" + changed + "|Busy=" + busy +
                "|Duration=" + TurnTransitionSeconds.ToString("0.00") + "|Translation=False");
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(transientMessage) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -975;
                GUI.Box(new Rect(12f, 118f, 500f, 28f), transientMessage);
            }

            if (!TacticalRebuildSettings00C4.DebugViewEnabled)
                return;

            GUI.depth = -960;
            Rect box = new Rect(12f, 154f, 585f, 65f);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 10f, box.y + 7f, box.width - 20f, 20f),
                "C4 DRILL | F animated Line/Column (2.2s) | Z/X animated facing +/-15° (0.75s)");
            GUI.Label(new Rect(box.x + 10f, box.y + 29f, box.width - 20f, 20f),
                "F10 settings | H hover | V debug | Movement/navigation/combat/AI remain OFF");
        }
    }
}
