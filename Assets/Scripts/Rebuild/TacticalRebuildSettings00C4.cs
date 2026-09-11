using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00c4 - Presentation/test settings only.
    // Soldier ratio changes rendered representatives, never OOB/manpower/tactical state.
    [DefaultExecutionOrder(610)]
    public sealed class TacticalRebuildSettings00C4 : MonoBehaviour
    {
        public static TacticalRebuildSettings00C4 Instance { get; private set; }
        public static int SoldierVisualDenominator { get; private set; } = 1;
        public static bool DebugViewEnabled { get; private set; } = true;
        public static bool HoverInfoEnabled { get; private set; } = true;
        public static bool SettingsVisible { get; private set; }

        public static int Revision { get; private set; }

        private string transient = string.Empty;
        private float transientUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalRebuildSettings00C4>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00C4_SETTINGS");
            root.AddComponent<TacticalRebuildSettings00C4>();
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
                "REBUILD-SETTINGS-00C4|Installed=True|SoldierRatio=1:" + SoldierVisualDenominator +
                "|RatioAffectsSimulation=False|F10=Settings|H=Hover|V=Debug");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                SettingsVisible = !SettingsVisible;
                ShowTransient("Settings: " + (SettingsVisible ? "ON" : "OFF"));
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                HoverInfoEnabled = !HoverInfoEnabled;
                ShowTransient("Hover info: " + (HoverInfoEnabled ? "ON" : "OFF"));
                Debug.Log("REBUILD-SETTINGS-00C4|HoverInfo=" + HoverInfoEnabled);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                DebugViewEnabled = !DebugViewEnabled;
                ShowTransient("Debug view: " + (DebugViewEnabled ? "ON" : "OFF"));
                Debug.Log("REBUILD-SETTINGS-00C4|DebugView=" + DebugViewEnabled);
            }
        }

        public static int GetVisibleRepresentativeCount(int actualStrength)
        {
            if (actualStrength <= 0)
                return 0;
            return Mathf.Max(1, Mathf.CeilToInt(actualStrength / (float)Mathf.Max(1, SoldierVisualDenominator)));
        }

        public void SetSoldierRatio(int denominator)
        {
            int normalized = NormalizeRatio(denominator);
            if (normalized == SoldierVisualDenominator)
                return;

            SoldierVisualDenominator = normalized;
            Revision++;
            ShowTransient("Soldater: 1:" + SoldierVisualDenominator + " (kun grafik)");

            Debug.Log(
                "REBUILD-SETTINGS-00C4|Action=SoldierRatioChanged|Ratio=1:" + SoldierVisualDenominator +
                "|SimulationStrengthUnchanged=True|Revision=" + Revision);
        }

        private static int NormalizeRatio(int denominator)
        {
            int[] allowed = { 1, 2, 3, 4, 5, 10, 20 };
            int best = allowed[0];
            int bestDistance = Mathf.Abs(denominator - best);
            for (int i = 1; i < allowed.Length; i++)
            {
                int distance = Mathf.Abs(denominator - allowed[i]);
                if (distance < bestDistance)
                {
                    best = allowed[i];
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void ShowTransient(string message)
        {
            transient = message;
            transientUntil = Time.unscaledTime + 2.2f;
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(transient) && Time.unscaledTime < transientUntil)
            {
                GUI.depth = -995;
                GUI.Box(new Rect(Screen.width * 0.5f - 145f, 38f, 290f, 28f), transient);
            }

            if (!SettingsVisible)
                return;

            GUI.depth = -990;
            const float width = 360f;
            const float height = 202f;
            Rect box = new Rect(Mathf.Max(8f, Screen.width - width - 12f), 96f, width, height);
            GUI.Box(box, string.Empty);
            GUI.Label(new Rect(box.x + 12f, box.y + 8f, 330f, 22f), "REBUILD SETTINGS — visuel soldat-skala");
            GUI.Label(new Rect(box.x + 12f, box.y + 34f, 330f, 20f),
                "Aktuel: 1:" + SoldierVisualDenominator + "  |  OOB/manpower ændres IKKE");

            int[] ratios = { 1, 2, 3, 4, 5, 10, 20 };
            for (int i = 0; i < ratios.Length; i++)
            {
                int ratio = ratios[i];
                int col = i % 4;
                int row = i / 4;
                Rect button = new Rect(box.x + 12f + col * 82f, box.y + 64f + row * 34f, 74f, 27f);
                GUI.enabled = ratio != SoldierVisualDenominator;
                if (GUI.Button(button, "1:" + ratio))
                    SetSoldierRatio(ratio);
            }
            GUI.enabled = true;

            GUI.Label(new Rect(box.x + 12f, box.y + 136f, 334f, 20f),
                "Eksempel 190 mand: 1:1=190 | 1:2=95 | 1:5=38 | 1:10=19");
            GUI.Label(new Rect(box.x + 12f, box.y + 158f, 334f, 20f),
                "F10 settings | H hover | V debug");
            GUI.Label(new Rect(box.x + 12f, box.y + 179f, 334f, 18f),
                "Reducerede figurer fordeles over hele den rigtige formation.");
        }
    }
}
