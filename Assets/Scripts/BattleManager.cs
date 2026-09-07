using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public IReadOnlyList<Regiment> Regiments => regiments;
    public bool IsPaused => paused;
    public int SimulationSpeed => paused ? 0 : speed;
    public float BattleMinutes => battleMinutes;

    private const float GameMinutesPerSimulationSecond = 2.2f;

    private readonly List<Regiment> regiments = new List<Regiment>();
    private float battleMinutes = 10f * 60f + 20f;
    private int speed = 1;
    private bool paused;
    private string resultMessage = string.Empty;
    private GUIStyle unitStyle;
    private GUIStyle hitStyle;
    private GUIStyle topStyle;
    private GUIStyle helpStyle;
    private GUIStyle timeStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BattleManager detected; the duplicate component has been disabled.");
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSpeed(1);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSpeed(2);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSpeed(5);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetSpeed(20);

        // Time.deltaTime is scaled by Time.timeScale. Therefore the battle clock
        // advances in lockstep with the selected simulation speed and stops
        // completely while paused.
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
        // Hook for future chronicle, prisoners, wounded and command reports.
    }

    public bool IsPointerOverSimulationControls(Vector3 mousePosition)
    {
        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return GetTimeControlRect().Contains(guiPoint);
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
            resultMessage = "DANSK SEJR - de preussiske regimenter er slået tilbage";
            SetPaused(true);
        }
        else if (!danesActive)
        {
            resultMessage = "DANSK NEDERLAG - de danske regimenter har forladt slagmarken";
            SetPaused(true);
        }
    }

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : speed;
    }

    private void SetSpeed(int newSpeed)
    {
        // A battle result is terminal. Mouse-based time controls must not be able to
        // resume the simulation even for a single frame after victory/defeat.
        if (!string.IsNullOrEmpty(resultMessage))
        {
            SetPaused(true);
            return;
        }

        if (newSpeed != 1 && newSpeed != 2 && newSpeed != 5 && newSpeed != 20)
            newSpeed = 1;

        speed = newSpeed;
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
        const float width = 515f;
        return new Rect(Mathf.Max(10f, Screen.width - width - 10f), 10f, width, 34f);
    }

    private void EnsureStyles()
    {
        if (unitStyle != null)
            return;

        unitStyle = new GUIStyle(GUI.skin.box);
        unitStyle.fontSize = 11;
        unitStyle.alignment = TextAnchor.MiddleCenter;
        unitStyle.normal.textColor = Color.white;

        hitStyle = new GUIStyle(GUI.skin.box);
        hitStyle.fontSize = 16;
        hitStyle.fontStyle = FontStyle.Bold;
        hitStyle.alignment = TextAnchor.MiddleCenter;
        hitStyle.normal.textColor = new Color(1f, 0.88f, 0.30f);

        topStyle = new GUIStyle(GUI.skin.box);
        topStyle.fontSize = 14;
        topStyle.fontStyle = FontStyle.Bold;
        topStyle.alignment = TextAnchor.MiddleCenter;
        topStyle.normal.textColor = Color.white;

        helpStyle = new GUIStyle(GUI.skin.box);
        helpStyle.fontSize = 12;
        helpStyle.alignment = TextAnchor.UpperLeft;
        helpStyle.wordWrap = true;
        helpStyle.normal.textColor = Color.white;

        timeStyle = new GUIStyle(GUI.skin.box);
        timeStyle.fontSize = 12;
        timeStyle.fontStyle = FontStyle.Bold;
        timeStyle.alignment = TextAnchor.MiddleCenter;
        timeStyle.normal.textColor = Color.white;
    }

    private void DrawTimeControls(string clock)
    {
        Rect panel = GetTimeControlRect();
        GUI.Box(panel, string.Empty);

        float x = panel.x + 4f;
        float y = panel.y + 4f;
        const float h = 26f;

        if (GUI.Button(new Rect(x, y, 48f, h), speed == 1 && !paused ? "[PLAY]" : "PLAY"))
            SetSpeed(1);
        x += 52f;

        if (GUI.Button(new Rect(x, y, 58f, h), paused ? "[PAUSE]" : "PAUSE"))
            SetPaused(true);
        x += 62f;

        if (GUI.Button(new Rect(x, y, 42f, h), speed == 2 && !paused ? "[x2]" : "x2"))
            SetSpeed(2);
        x += 46f;

        if (GUI.Button(new Rect(x, y, 42f, h), speed == 5 && !paused ? "[x5]" : "x5"))
            SetSpeed(5);
        x += 46f;

        if (GUI.Button(new Rect(x, y, 48f, h), speed == 20 && !paused ? "[x20]" : "x20"))
            SetSpeed(20);
        x += 52f;

        string state = paused ? "PAUSED" : "x" + speed;
        GUI.Box(new Rect(x, y, panel.xMax - x - 4f, h), clock + "  [" + state + "]", timeStyle);
    }

    private void OnGUI()
    {
        EnsureStyles();

        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;
        string clock = $"1 Feb 1864  {hours:00}:{minutes:00}";

        Rect timePanel = GetTimeControlRect();
        float titleWidth = Mathf.Max(200f, timePanel.x - 20f);
        GUI.Box(new Rect(10, 10, titleWidth, 34), "PROJECT 1864 - P0A v00.00.09 OFFICER AI TEST", topStyle);
        DrawTimeControls(clock);

        GUI.Box(new Rect(10, 80, 285, 84),
            "DANMARK\nHold højderyggen og gården\nSlå de to preussiske regimenter tilbage", helpStyle);
        GUI.Box(new Rect(Screen.width - 295, 80, 285, 84),
            "PREUSSEN\nTag højderyggen\nBryd den danske stilling / flankér vejen", helpStyle);

        Camera mainCamera = Camera.main;
        foreach (Regiment regiment in regiments)
        {
            if (regiment == null || mainCamera == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(regiment.transform.position + Vector3.up * 3f);
            if (screen.z <= 0f)
                continue;

            float x = screen.x - 125f;
            float y = Screen.height - screen.y - 45f;
            string team = regiment.Team == BattleTeam.Denmark ? "DK" : "PR";
            string routed = regiment.IsRouted ? "  ROUTED" : string.Empty;

            OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
            string aiLine = "AI controller installing";
            string taskLine = string.Empty;
            if (ai != null && ai.Officer != null)
            {
                aiLine = string.Format(
                    "{0} | {1} | T{2:0} Init{3:0} Comp{4:0}",
                    ai.AIEnabled ? "AI ON" : "AI OFF",
                    ai.Officer.OfficerName,
                    ai.Officer.TacticalSkill,
                    ai.Officer.Initiative,
                    ai.Officer.Composure);
                taskLine = ai.CurrentTask + " | " + ai.ReasonCode;
            }

            string label =
                $"{regiment.RegimentName} ({team})  {regiment.CurrentStrength}\n" +
                $"{regiment.WeaponShortName} | Exp {regiment.Experience:0} | Reload {regiment.CurrentReloadSeconds:0.0}s\n" +
                $"Morale {regiment.Morale:0}  Coh {regiment.Cohesion:0}{routed}\n" +
                aiLine + "\n" + taskLine;

            GUI.Box(new Rect(x, y, 250f, 94f), label, unitStyle);

            if (regiment.HasHitFeedback)
                GUI.Box(new Rect(screen.x - 62f, y - 30f, 124f, 26f), $"Ramte {regiment.LastVolleyHits}", hitStyle);
        }

        GUI.Box(new Rect(10, Screen.height - 116, 430, 106),
            "STYRING\nKlik = vælg | Shift+klik = flere | Højreklik = flyt/angrib\nF = line | C = column | H = hold | T = range | A = AI UNIT ON/OFF\nWASD = kamera | Q/E = roter | hjul = zoom | Space = pause/resume\n1 = Play | 2 = x2 | 3 = x5 | 4 = x20 | R = restart", helpStyle);

        if (!string.IsNullOrEmpty(resultMessage))
        {
            Rect resultRect = new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 45f, 540f, 90f);
            GUI.Box(resultRect, resultMessage + "\nTryk R for at spille igen", topStyle);
        }
    }
}
