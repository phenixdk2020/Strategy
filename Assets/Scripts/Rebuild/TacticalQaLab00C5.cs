using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c5 - QA/test harness only.
    // It deliberately does not own tactical translation, pathfinding, combat or AI.
    [DefaultExecutionOrder(720)]
    public sealed class TacticalQaLab00C5 : MonoBehaviour
    {
        public static TacticalQaLab00C5 Instance { get; private set; }

        private static readonly int[] BenchmarkRatios = { 1, 2, 5, 10, 20 };

        private bool panelVisible;
        private bool autoDrill;
        private int autoStage;
        private float nextAutoAction;

        private bool benchmarkRunning;
        private int benchmarkStage;
        private int benchmarkOriginalRatio = 1;
        private float benchmarkStageElapsed;
        private float benchmarkAccumulatedDt;
        private int benchmarkFrames;
        private readonly List<string> benchmarkResults = new List<string>();

        private string transient = string.Empty;
        private float transientUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalQaLab00C5>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C5_QA_LAB");
            root.AddComponent<TacticalQaLab00C5>();
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
            Debug.Log(
                "REBUILD-QALAB-00C5|Installed=True|F8=Panel|F7=AutoDrill|F6=RatioBenchmark|Y=FacingOverlay|" +
                "MovementWrites=False|NavigationWrites=False|CombatWrites=False|AIWrites=False");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                panelVisible = !panelVisible;
                ShowTransient("QA Lab: " + (panelVisible ? "ON" : "OFF"));
            }

            if (Input.GetKeyDown(KeyCode.F7))
                ToggleAutoDrill();

            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (benchmarkRunning)
                    StopBenchmark(true);
                else
                    StartBenchmark();
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled =
                    !TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled;
                ShowTransient("Facing arrows: " +
                    (TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled ? "ON" : "OFF"));
            }

            if (autoDrill)
                UpdateAutoDrill();

            if (benchmarkRunning)
                UpdateBenchmark();
        }

        private void ToggleAutoDrill()
        {
            autoDrill = !autoDrill;
            autoStage = 0;
            nextAutoAction = Time.unscaledTime + 0.35f;
            ShowTransient("Auto drill: " + (autoDrill ? "ON" : "OFF"));
            Debug.Log("REBUILD-QALAB-00C5|AutoDrill=" + autoDrill);
        }

        private void UpdateAutoDrill()
        {
            if (Time.unscaledTime < nextAutoAction)
                return;

            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            if (selection == null || selection.Selected == null || selection.Selected.Count == 0)
            {
                nextAutoAction = Time.unscaledTime + 0.5f;
                return;
            }

            if (AnySelectedBusy(selection))
            {
                nextAutoAction = Time.unscaledTime + 0.20f;
                return;
            }

            int affected = 0;
            switch (autoStage)
            {
                case 0:
                    affected = ToggleSelectedFormation(selection, 2.2f);
                    nextAutoAction = Time.unscaledTime + 2.55f;
                    break;
                case 1:
                    affected = TurnSelected(selection, 45f, 1.30f);
                    nextAutoAction = Time.unscaledTime + 1.60f;
                    break;
                case 2:
                    affected = TurnSelected(selection, -45f, 1.30f);
                    nextAutoAction = Time.unscaledTime + 1.60f;
                    break;
                default:
                    affected = ToggleSelectedFormation(selection, 2.2f);
                    nextAutoAction = Time.unscaledTime + 2.70f;
                    break;
            }

            Debug.Log(
                "REBUILD-QALAB-00C5|AutoDrillStage=" + autoStage +
                "|Affected=" + affected + "|Translation=False");

            autoStage = (autoStage + 1) % 4;
        }

        private static bool AnySelectedBusy(TacticalCompanySelection00B selection)
        {
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company != null && (company.IsReforming || company.IsTurning))
                    return true;
            }
            return false;
        }

        private static int ToggleSelectedFormation(TacticalCompanySelection00B selection, float seconds)
        {
            int changed = 0;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark || company.IsReforming)
                    continue;

                RebuildFormation next = company.Formation == RebuildFormation.Line
                    ? RebuildFormation.Column
                    : RebuildFormation.Line;
                if (company.BeginFormationDrill(next, seconds))
                    changed++;
            }
            return changed;
        }

        private static int TurnSelected(TacticalCompanySelection00B selection, float degrees, float seconds)
        {
            int changed = 0;
            for (int i = 0; i < selection.Selected.Count; i++)
            {
                TacticalCompanyEntity00B company = selection.Selected[i];
                if (company == null || company.Nation != RebuildNation.Denmark || company.IsTurning)
                    continue;

                if (company.BeginTurnDrill(degrees, seconds))
                    changed++;
            }
            return changed;
        }

        private void StartBenchmark()
        {
            TacticalRebuildSettings00C4 settings = TacticalRebuildSettings00C4.Instance;
            if (settings == null)
            {
                ShowTransient("Benchmark: settings ikke klar");
                return;
            }

            benchmarkOriginalRatio = TacticalRebuildSettings00C4.SoldierVisualDenominator;
            benchmarkStage = 0;
            benchmarkResults.Clear();
            benchmarkRunning = true;
            BeginBenchmarkStage();
            ShowTransient("Ratio benchmark startet");
        }

        private void BeginBenchmarkStage()
        {
            if (benchmarkStage >= BenchmarkRatios.Length)
            {
                StopBenchmark(false);
                return;
            }

            TacticalRebuildSettings00C4.Instance.SetSoldierRatio(BenchmarkRatios[benchmarkStage]);
            benchmarkStageElapsed = 0f;
            benchmarkAccumulatedDt = 0f;
            benchmarkFrames = 0;

            Debug.Log(
                "REBUILD-BENCH-00C5|StageStart=True|Ratio=1:" + BenchmarkRatios[benchmarkStage]);
        }

        private void UpdateBenchmark()
        {
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            benchmarkStageElapsed += dt;

            // First 0.75 s is warm-up after ratio change; do not include in average.
            if (benchmarkStageElapsed > 0.75f)
            {
                benchmarkAccumulatedDt += dt;
                benchmarkFrames++;
            }

            if (benchmarkStageElapsed < 3.75f)
                return;

            float avgDt = benchmarkFrames > 0 ? benchmarkAccumulatedDt / benchmarkFrames : 0f;
            float fps = avgDt > 0f ? 1f / avgDt : 0f;
            float ms = avgDt * 1000f;
            int ratio = BenchmarkRatios[benchmarkStage];
            string result = "1:" + ratio + " = " + fps.ToString("0.0") + " FPS / " + ms.ToString("0.00") + " ms";
            benchmarkResults.Add(result);

            Debug.Log(
                "REBUILD-BENCH-00C5|StageComplete=True|Ratio=1:" + ratio +
                "|AverageFPS=" + fps.ToString("0.0") +
                "|AverageMS=" + ms.ToString("0.00") +
                "|Frames=" + benchmarkFrames);

            benchmarkStage++;
            BeginBenchmarkStage();
        }

        private void StopBenchmark(bool cancelled)
        {
            if (TacticalRebuildSettings00C4.Instance != null)
                TacticalRebuildSettings00C4.Instance.SetSoldierRatio(benchmarkOriginalRatio);

            benchmarkRunning = false;

            if (cancelled)
            {
                ShowTransient("Ratio benchmark afbrudt");
                Debug.Log("REBUILD-BENCH-00C5|Cancelled=True");
                return;
            }

            ShowTransient("Ratio benchmark færdig - se F8/Console");
            Debug.Log("REBUILD-BENCH-00C5|Complete=True|Results=" + string.Join(" ; ", benchmarkResults));
        }

        private void ShowTransient(string message)
        {
            transient = message;
            transientUntil = Time.unscaledTime + 2.4f;
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(transient) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -997;
                GUI.Box(new Rect(Screen.width * 0.5f - 175f, 70f, 350f, 28f), transient);
            }

            if (!panelVisible)
                return;

            GUI.depth = -992;
            const float width = 470f;
            const float height = 330f;
            Rect box = new Rect(12f, Mathf.Max(230f, Screen.height - height - 12f), width, height);
            GUI.Box(box, string.Empty);

            GUI.Label(new Rect(box.x + 12f, box.y + 8f, width - 24f, 22f),
                "C5 QA LAB — presentation/drill stress test");

            TacticalCompanySelection00B selection = TacticalCompanySelection00B.Instance;
            int selected = selection != null && selection.Selected != null ? selection.Selected.Count : 0;
            GUI.Label(new Rect(box.x + 12f, box.y + 34f, width - 24f, 20f),
                "Selected: " + selected +
                " | Visual ratio: 1:" + TacticalRebuildSettings00C4.SoldierVisualDenominator +
                " | Facing arrows: " + (TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled ? "ON" : "OFF"));

            if (selected > 0)
            {
                TacticalCompanyEntity00B company = selection.Selected[0];
                int reps = TacticalRebuildSettings00C4.GetVisibleRepresentativeCount(company.PresentStrength);
                GUI.Label(new Rect(box.x + 12f, box.y + 58f, width - 24f, 20f),
                    "First: " + company.UnitId + " | " + company.DisplayName);
                GUI.Label(new Rect(box.x + 12f, box.y + 80f, width - 24f, 20f),
                    "Strength " + company.PresentStrength + " | Visible reps " + reps +
                    " | Formation " + company.Formation + " | Facing " + company.transform.eulerAngles.y.ToString("0.0") + "°");
                GUI.Label(new Rect(box.x + 12f, box.y + 102f, width - 24f, 20f),
                    "Footprint " + company.FootprintWidth.ToString("0.0") + " x " + company.FootprintDepth.ToString("0.0") +
                    " | Reform " + (company.IsReforming ? (company.ReformProgress * 100f).ToString("0") + "%" : "idle") +
                    " | Turn " + (company.IsTurning ? "active" : "idle"));
            }
            else
            {
                GUI.Label(new Rect(box.x + 12f, box.y + 58f, width - 24f, 42f),
                    "Vælg et eller flere danske Companies for at teste auto drill og inspector-data.");
            }

            Rect autoButton = new Rect(box.x + 12f, box.y + 136f, 138f, 28f);
            if (GUI.Button(autoButton, autoDrill ? "Stop Auto Drill [F7]" : "Start Auto Drill [F7]"))
                ToggleAutoDrill();

            Rect benchButton = new Rect(box.x + 158f, box.y + 136f, 160f, 28f);
            if (GUI.Button(benchButton, benchmarkRunning ? "Stop Benchmark [F6]" : "Ratio Benchmark [F6]"))
            {
                if (benchmarkRunning) StopBenchmark(true); else StartBenchmark();
            }

            Rect arrowButton = new Rect(box.x + 326f, box.y + 136f, 130f, 28f);
            if (GUI.Button(arrowButton, "Facing Arrows [Y]"))
                TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled = !TacticalCompanyQaOverlay00C5.DirectionOverlayEnabled;

            GUI.Label(new Rect(box.x + 12f, box.y + 174f, width - 24f, 20f),
                "Auto Drill: " + (autoDrill ? "RUNNING" : "OFF") +
                " | Benchmark: " + (benchmarkRunning ? "RUNNING 1:" + BenchmarkRatios[Mathf.Min(benchmarkStage, BenchmarkRatios.Length - 1)] : "OFF"));

            GUI.Label(new Rect(box.x + 12f, box.y + 198f, width - 24f, 20f),
                "Benchmark sequence: 1:1 -> 1:2 -> 1:5 -> 1:10 -> 1:20 (3 s measured each)");

            int y = 222;
            if (benchmarkResults.Count == 0)
            {
                GUI.Label(new Rect(box.x + 12f, box.y + y, width - 24f, 20f),
                    "Ingen benchmark-resultater endnu.");
            }
            else
            {
                for (int i = 0; i < benchmarkResults.Count && i < 4; i++)
                {
                    GUI.Label(new Rect(box.x + 12f, box.y + y + i * 20f, width - 24f, 20f), benchmarkResults[i]);
                }
            }

            GUI.Label(new Rect(box.x + 12f, box.y + 302f, width - 24f, 20f),
                "F8 panel | F7 auto drill | F6 benchmark | Y facing arrows | ingen Gate D movement");
        }
    }
}
