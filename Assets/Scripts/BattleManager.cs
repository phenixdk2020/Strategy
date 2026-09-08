using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public IReadOnlyList<Regiment> Regiments => regiments;
    public bool IsPaused => paused;
    public float SimulationSpeed => paused ? 0f : speed;
    public float BattleMinutes => battleMinutes;

    private const float GameMinutesPerSimulationSecond = 2.2f;
    private const float HoverInfoWidth = 200f;
    private const float HoverInfoHeight = 34f;
    private const float SelectedInfoHeight = 68f;

    private readonly List<Regiment> regiments = new List<Regiment>();
    private float battleMinutes = 10f * 60f + 20f;
    private float speed = 1f;
    private bool paused;
    private string resultMessage = string.Empty;

    private GUIStyle selectedUnitStyle;
    private GUIStyle hoverStyle;
    private GUIStyle hitStyle;
    private GUIStyle topStyle;
    private GUIStyle timeStyle;
    private GUIStyle miniStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BattleManager detected; duplicate disabled.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartBattle();
            return;
        }

        if (!string.IsNullOrEmpty(resultMessage))
        {
            if (!paused)
                SetPaused(true);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            SetPaused(!paused);
        if (Input.GetKeyDown(KeyCode.Alpha0))
            SetSpeed(0.5f);
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSpeed(2f);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSpeed(5f);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetSpeed(20f);

        if (!paused)
            battleMinutes += Time.deltaTime * GameMinutesPerSimulationSecond;

        EvaluateBattleResult();
    }

    public void Register(Regiment regiment)
    {
        if (regiment != null && !regiments.Contains(regiment))
            regiments.Add(regiment);
    }

    public void NotifyRout(Regiment regiment)
    {
        // Hook for future chronicle / prisoner / casualty reporting.
    }

    public bool IsPointerOverSimulationControls(Vector3 mousePosition)
    {
        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);

        if (GetTimeControlRect().Contains(guiPoint))
            return true;

        OfficerAIPrototypeManager aiManager = OfficerAIPrototypeManager.Instance;
        if (aiManager != null && aiManager.IsPointerOverControls(mousePosition))
            return true;

        PrototypeCombatTuningManager tuning = PrototypeCombatTuningManager.Instance;
        return tuning != null && tuning.IsPointerOverControls(mousePosition);
    }

    private Regiment GetHoveredRegiment(Camera mainCamera)
    {
        if (mainCamera == null || IsPointerOverSimulationControls(Input.mousePosition))
            return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1600f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Regiment regiment = hit.collider.GetComponentInParent<Regiment>();
            if (regiment != null)
                return regiment;
        }

        return null;
    }

    private static Rect GetHoverInfoRect(Vector3 screen)
    {
        float anchorY = Screen.height - screen.y;
        float x = screen.x + 16f;

        if (x + HoverInfoWidth > Screen.width - 12f)
            x = screen.x - HoverInfoWidth - 16f;

        x = Mathf.Clamp(x, 12f, Mathf.Max(12f, Screen.width - HoverInfoWidth - 12f));
        float y = Mathf.Clamp(anchorY - HoverInfoHeight - 12f, 102f, Mathf.Max(102f, Screen.height - HoverInfoHeight - 110f));

        return new Rect(x, y, HoverInfoWidth, HoverInfoHeight);
    }

    private static Rect GetSelectedInfoRect()
    {
        float width = Mathf.Min(430f, Mathf.Max(280f, Screen.width - 36f));
        return new Rect(18f, 104f, width, SelectedInfoHeight);
    }

    private void EvaluateBattleResult()
    {
        if (!string.IsNullOrEmpty(resultMessage) || regiments.Count < 4)
            return;

        bool danesActive = false;
        bool prussiansActive = false;

        foreach (Regiment r in regiments)
        {
            if (r == null || r.IsRouted || r.CurrentStrength <= 0)
                continue;

            if (r.Team == BattleTeam.Denmark)
                danesActive = true;
            else
                prussiansActive = true;
        }

        if (!prussiansActive)
        {
            resultMessage = "DANSK SEJR - forsvarerne har slået det preussiske angreb tilbage";
            SetPaused(true);
        }
        else if (!danesActive)
        {
            resultMessage = "DANSK NEDERLAG - den danske forsvarsstilling er brudt";
            SetPaused(true);
        }
    }

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : speed;
    }

    private void SetSpeed(float newSpeed)
    {
        if (!string.IsNullOrEmpty(resultMessage))
        {
            SetPaused(true);
            return;
        }

        bool valid =
            Mathf.Approximately(newSpeed, 0.5f) ||
            Mathf.Approximately(newSpeed, 1f) ||
            Mathf.Approximately(newSpeed, 2f) ||
            Mathf.Approximately(newSpeed, 5f) ||
            Mathf.Approximately(newSpeed, 20f);

        speed = valid ? newSpeed : 1f;
        paused = false;
        Time.timeScale = speed;
    }

    private void RestartBattle()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();

        if (!string.IsNullOrEmpty(scene.name))
            SceneManager.LoadScene(scene.name);
    }

    private Rect GetTimeControlRect()
    {
        float width = Mathf.Min(650f, Mathf.Max(390f, Screen.width - 120f));
        return new Rect((Screen.width - width) * 0.5f, 40f, width, 30f);
    }

    private void EnsureStyles()
    {
        if (selectedUnitStyle != null)
            return;

        selectedUnitStyle = new GUIStyle(GUI.skin.box);
        selectedUnitStyle.fontSize = 9;
        selectedUnitStyle.alignment = TextAnchor.MiddleLeft;
        selectedUnitStyle.padding = new RectOffset(8, 8, 4, 4);
        selectedUnitStyle.normal.textColor = Color.white;
        selectedUnitStyle.wordWrap = true;

        hoverStyle = new GUIStyle(GUI.skin.box);
        hoverStyle.fontSize = 9;
        hoverStyle.alignment = TextAnchor.MiddleLeft;
        hoverStyle.padding = new RectOffset(6, 6, 2, 2);
        hoverStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

        hitStyle = new GUIStyle(GUI.skin.box);
        hitStyle.fontSize = 14;
        hitStyle.fontStyle = FontStyle.Bold;
        hitStyle.alignment = TextAnchor.MiddleCenter;
        hitStyle.normal.textColor = new Color(1f, 0.88f, 0.30f);

        topStyle = new GUIStyle(GUI.skin.box);
        topStyle.fontSize = 12;
        topStyle.fontStyle = FontStyle.Bold;
        topStyle.alignment = TextAnchor.MiddleCenter;
        topStyle.normal.textColor = Color.white;

        timeStyle = new GUIStyle(GUI.skin.box);
        timeStyle.fontSize = 10;
        timeStyle.fontStyle = FontStyle.Bold;
        timeStyle.alignment = TextAnchor.MiddleCenter;
        timeStyle.normal.textColor = Color.white;

        miniStyle = new GUIStyle(GUI.skin.box);
        miniStyle.fontSize = 9;
        miniStyle.alignment = TextAnchor.MiddleCenter;
        miniStyle.normal.textColor = Color.white;
    }

    private void DrawTimeControls(string clock)
    {
        Rect panel = GetTimeControlRect();
        GUI.Box(panel, string.Empty);

        float x = panel.x + 3f;
        float y = panel.y + 3f;
        const float h = 24f;
        const float gap = 3f;

        float clockWidth = Mathf.Clamp(panel.width * 0.36f, 135f, 220f);
        string state = paused
            ? "PAUSE"
            : (Mathf.Approximately(speed, 0.5f) ? "x0.5" : "x" + speed.ToString("0"));

        GUI.Box(
            new Rect(x, y, clockWidth, h),
            clock + " | " + state,
            timeStyle);
        x += clockWidth + gap;

        float buttonArea = panel.xMax - x - 3f;
        float buttonWidth = Mathf.Max(38f, (buttonArea - gap * 5f) / 6f);

        if (GUI.Button(new Rect(x, y, buttonWidth, h), paused ? "FORTSÆT" : "PAUSE"))
            SetPaused(!paused);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), Mathf.Approximately(speed, 0.5f) && !paused ? "[0.5]" : "0.5"))
            SetSpeed(0.5f);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), Mathf.Approximately(speed, 1f) && !paused ? "[1]" : "1"))
            SetSpeed(1f);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), Mathf.Approximately(speed, 2f) && !paused ? "[2]" : "2"))
            SetSpeed(2f);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), Mathf.Approximately(speed, 5f) && !paused ? "[5]" : "5"))
            SetSpeed(5f);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), Mathf.Approximately(speed, 20f) && !paused ? "[20]" : "20"))
            SetSpeed(20f);
    }

    private void DrawSelectedUnitPanel(
        Regiment firstSelected,
        int selectedCount,
        int totalStrength,
        int totalInitial,
        float moraleSum,
        float cohesionSum)
    {
        if (firstSelected == null || selectedCount <= 0)
            return;

        string label;

        if (selectedCount > 1)
        {
            float avgMorale = moraleSum / selectedCount;
            float avgCohesion = cohesionSum / selectedCount;
            label =
                $"VALGT x{selectedCount} | Styrke {totalStrength}/{totalInitial} | Mor {avgMorale:0} | Coh {avgCohesion:0}\n" +
                "Fælles ordrer, doctrine, aggression og fire policy styres i command-bar'en nederst.";
        }
        else
        {
            string team = firstSelected.Team == BattleTeam.Denmark ? "DK" : "PR";
            int losses = Mathf.Max(0, firstSelected.InitialStrength - firstSelected.CurrentStrength);
            int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(firstSelected);
            int startAmmo = PrototypeCombatStatusManager.GetStartingAmmunitionRoundsPerMan(firstSelected);
            string routed = firstSelected.IsRouted ? " | ROUTED" : string.Empty;

            OfficerAIController ai = firstSelected.GetComponent<OfficerAIController>();
            string aiLine = "AI installerer";
            if (ai != null && ai.Officer != null)
            {
                aiLine = string.Format(
                    "AI {0} | {1} | {2} | Agg {3:0} | {4}",
                    ai.AIEnabled ? "ON" : "OFF",
                    ai.Officer.OfficerName,
                    ai.Doctrine,
                    ai.OrderAggressiveness,
                    ai.CurrentTask);
            }

            label =
                $"VALGT {firstSelected.RegimentName} ({team}) | {firstSelected.CurrentStrength}/{firstSelected.InitialStrength} | Tab {losses} | Ammo {ammo}/{startAmmo}{routed}\n" +
                $"{firstSelected.WeaponShortName} | {firstSelected.Formation} | Ild {firstSelected.GetFirePolicyLabel()} | Mor {firstSelected.Morale:0} | Coh {firstSelected.Cohesion:0} | Exp {firstSelected.Experience:0}\n" +
                aiLine;
        }

        GUI.Box(GetSelectedInfoRect(), label, selectedUnitStyle);
    }

    private void OnGUI()
    {
        EnsureStyles();

        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;
        string clock = $"1 Feb 1864 {hours:00}:{minutes:00}";

        DrawTimeControls(clock);

        float helperWidth = Mathf.Min(650f, Mathf.Max(320f, Screen.width - 160f));
        GUI.Box(
            new Rect((Screen.width - helperWidth) * 0.5f, 74f, helperWidth, 22f),
            "Ctrl/Shift multi | RMB: mål + træk = facing | Alt+RMB = waypoint | F/C formation | Z/X drej | F9 kamp-setup",
            miniStyle);

        Camera mainCamera = Camera.main;
        Regiment hoveredRegiment = GetHoveredRegiment(mainCamera);

        Regiment firstSelected = null;
        int selectedCount = 0;
        int totalStrength = 0;
        int totalInitial = 0;
        float moraleSum = 0f;
        float cohesionSum = 0f;

        foreach (Regiment regiment in regiments)
        {
            if (regiment == null)
                continue;

            if (regiment.IsSelected)
            {
                if (firstSelected == null)
                    firstSelected = regiment;

                selectedCount++;
                totalStrength += regiment.CurrentStrength;
                totalInitial += regiment.InitialStrength;
                moraleSum += regiment.Morale;
                cohesionSum += regiment.Cohesion;
            }

            if (mainCamera == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(regiment.transform.position + Vector3.up * 3f);
            if (screen.z <= 0f)
                continue;

            if (regiment == hoveredRegiment && !regiment.IsSelected)
            {
                string team = regiment.Team == BattleTeam.Denmark ? "DK" : "PR";
                string routed = regiment.IsRouted ? " | ROUTED" : string.Empty;
                string hoverLabel =
                    $"{regiment.RegimentName} ({team}) | {regiment.CurrentStrength}/{regiment.InitialStrength}{routed}\n" +
                    $"Mor {regiment.Morale:0} | Coh {regiment.Cohesion:0} | {regiment.Formation}";

                GUI.Box(GetHoverInfoRect(screen), hoverLabel, hoverStyle);
            }

            if (PrototypeCombatStatusManager.TryGetVolleyFeedback(regiment, out int volleyHits))
            {
                float hitY = Mathf.Max(102f, Screen.height - screen.y - 46f);

                GUI.Box(
                    new Rect(screen.x - 55f, hitY, 110f, 24f),
                    $"Ramte {volleyHits}",
                    hitStyle);
            }
        }

        DrawSelectedUnitPanel(
            firstSelected,
            selectedCount,
            totalStrength,
            totalInitial,
            moraleSum,
            cohesionSum);

        if (!string.IsNullOrEmpty(resultMessage))
        {
            Rect resultRect = new Rect(
                Screen.width * 0.5f - 270f,
                Screen.height * 0.5f - 45f,
                540f,
                90f);

            GUI.Box(resultRect, resultMessage + "\nTryk R for at spille igen", topStyle);
        }
    }
}
