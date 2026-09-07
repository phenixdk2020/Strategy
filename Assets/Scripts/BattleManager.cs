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
    private const float UnitInfoWidth = 330f;
    private const float UnitInfoHeight = 104f;
    private const float BottomUiReserve = 76f;

    private readonly List<Regiment> regiments = new List<Regiment>();
    private float battleMinutes = 10f * 60f + 20f;
    private float speed = 1f;
    private bool paused;
    private string resultMessage = string.Empty;

    private GUIStyle unitStyle;
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
        return aiManager != null && aiManager.IsPointerOverControls(mousePosition);
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

    private static Rect GetUnitInfoRect(Vector3 screen)
    {
        float anchorY = Screen.height - screen.y;
        float x = screen.x + 26f;

        if (x + UnitInfoWidth > Screen.width - 8f)
            x = screen.x - UnitInfoWidth - 26f;

        x = Mathf.Clamp(x, 6f, Mathf.Max(6f, Screen.width - UnitInfoWidth - 6f));

        float maxY = Mathf.Max(42f, Screen.height - BottomUiReserve - UnitInfoHeight - 4f);
        float y = Mathf.Clamp(anchorY - UnitInfoHeight - 30f, 42f, maxY);

        return new Rect(x, y, UnitInfoWidth, UnitInfoHeight);
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
        float width = Mathf.Min(690f, Mathf.Max(420f, Screen.width - 12f));
        return new Rect((Screen.width - width) * 0.5f, 6f, width, 32f);
    }

    private void EnsureStyles()
    {
        if (unitStyle != null)
            return;

        unitStyle = new GUIStyle(GUI.skin.box);
        unitStyle.fontSize = 10;
        unitStyle.alignment = TextAnchor.MiddleLeft;
        unitStyle.padding = new RectOffset(8, 8, 5, 5);
        unitStyle.normal.textColor = Color.white;

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
        miniStyle.alignment = TextAnchor.MiddleLeft;
        miniStyle.normal.textColor = Color.white;
    }

    private void DrawTimeControls(string clock)
    {
        Rect panel = GetTimeControlRect();
        GUI.Box(panel, string.Empty);

        float x = panel.x + 3f;
        float y = panel.y + 3f;
        const float h = 26f;
        const float gap = 3f;

        if (GUI.Button(new Rect(x, y, 55f, h), paused ? "[PAUSE]" : "PAUSE"))
            SetPaused(true);
        x += 55f + gap;

        if (GUI.Button(new Rect(x, y, 46f, h), Mathf.Approximately(speed, 0.5f) && !paused ? "[0.5]" : "0.5"))
            SetSpeed(0.5f);
        x += 46f + gap;

        if (GUI.Button(new Rect(x, y, 42f, h), Mathf.Approximately(speed, 1f) && !paused ? "[1]" : "1"))
            SetSpeed(1f);
        x += 42f + gap;

        if (GUI.Button(new Rect(x, y, 42f, h), Mathf.Approximately(speed, 2f) && !paused ? "[2]" : "2"))
            SetSpeed(2f);
        x += 42f + gap;

        if (GUI.Button(new Rect(x, y, 42f, h), Mathf.Approximately(speed, 5f) && !paused ? "[5]" : "5"))
            SetSpeed(5f);
        x += 42f + gap;

        if (GUI.Button(new Rect(x, y, 48f, h), Mathf.Approximately(speed, 20f) && !paused ? "[20]" : "20"))
            SetSpeed(20f);
        x += 48f + gap;

        string state = paused
            ? "PAUSED"
            : (Mathf.Approximately(speed, 0.5f) ? "x0.5" : "x" + speed.ToString("0"));

        float clockWidth = Mathf.Max(120f, panel.xMax - x - 3f);
        GUI.Box(
            new Rect(x, y, clockWidth, h),
            clock + " | " + state + " | AI " + OfficerAIPrototypeManager.CurrentDifficulty,
            timeStyle);
    }

    private void OnGUI()
    {
        EnsureStyles();

        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;
        string clock = $"1 Feb 1864 {hours:00}:{minutes:00}";

        DrawTimeControls(clock);

        GUI.Box(
            new Rect(6f, 42f, Mathf.Min(465f, Screen.width - 12f), 24f),
            "TEST: DK forsvarer | PR angriber | Ctrl/Shift multi | højre-drag linje | Z/X drej 15° | T range",
            miniStyle);

        Camera mainCamera = Camera.main;
        Regiment hoveredRegiment = GetHoveredRegiment(mainCamera);

        foreach (Regiment regiment in regiments)
        {
            if (regiment == null || mainCamera == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(regiment.transform.position + Vector3.up * 3f);
            if (screen.z <= 0f)
                continue;

            bool showInfo = regiment.IsSelected || regiment == hoveredRegiment;
            Rect infoRect = default;

            if (showInfo)
            {
                infoRect = GetUnitInfoRect(screen);
                string team = regiment.Team == BattleTeam.Denmark ? "DK" : "PR";
                string context = regiment.IsSelected ? "VALGT" : "MOUSE OVER";
                string routed = regiment.IsRouted ? " | ROUTED" : string.Empty;
                int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
                int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);

                OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
                string aiLine = "AI controller installerer";
                string taskLine = string.Empty;

                if (ai != null && ai.Officer != null)
                {
                    aiLine = string.Format(
                        "AI {0} | {1} | {2} | Agg {3:0}",
                        ai.AIEnabled ? "ON" : "OFF",
                        ai.Officer.OfficerName,
                        ai.Doctrine,
                        ai.OrderAggressiveness);

                    taskLine = "Task " + ai.CurrentTask;
                }

                string label =
                    $"{context} - {regiment.RegimentName} ({team}) | {regiment.CurrentStrength}/{regiment.InitialStrength} | Tab {losses} | Ammo {ammo}/60\n" +
                    $"{regiment.WeaponShortName} | Exp {regiment.Experience:0} | Reload {regiment.CurrentReloadSeconds:0.0}s | {regiment.Formation}\n" +
                    $"Fire {regiment.GetFirePolicyLabel()} | Arc {regiment.FireArcHalfAngle * 2f:0}° | C {regiment.CloseRange:0} M {regiment.EffectiveRange:0} L {regiment.MaximumRange:0}\n" +
                    $"Morale {regiment.Morale:0} | Coh {regiment.Cohesion:0}{routed}\n" +
                    aiLine + " | " + taskLine;

                GUI.Box(infoRect, label, unitStyle);
            }

            if (PrototypeCombatStatusManager.TryGetVolleyFeedback(regiment, out int volleyHits))
            {
                float hitY = showInfo
                    ? Mathf.Max(70f, infoRect.y - 28f)
                    : Mathf.Max(70f, Screen.height - screen.y - 52f);

                GUI.Box(
                    new Rect(screen.x - 55f, hitY, 110f, 24f),
                    $"Ramte {volleyHits}",
                    hitStyle);
            }
        }

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
