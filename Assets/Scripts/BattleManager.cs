using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public IReadOnlyList<Regiment> Regiments => regiments;

    private readonly List<Regiment> regiments = new List<Regiment>();
    private float battleMinutes = 10f * 60f + 20f;
    private int speed = 1;
    private bool paused;
    private string resultMessage = string.Empty;
    private GUIStyle unitStyle;
    private GUIStyle hitStyle;
    private GUIStyle topStyle;
    private GUIStyle helpStyle;

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

        // A concluded battle is a terminal paused state. Only restart is accepted.
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
            SetSpeed(3);

        if (!paused)
            battleMinutes += Time.deltaTime * 2.2f;

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
        speed = Mathf.Clamp(newSpeed, 1, 3);
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

    private void EnsureStyles()
    {
        if (unitStyle != null)
            return;

        unitStyle = new GUIStyle(GUI.skin.box);
        unitStyle.fontSize = 12;
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
    }

    private void OnGUI()
    {
        EnsureStyles();

        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;
        string clock = $"1 February 1864  {hours:00}:{minutes:00}";
        string state = paused ? "PAUSE" : speed + "x";
        GUI.Box(new Rect(10, 10, Screen.width - 20, 34), $"PROJECT 1864 - P0A Battle Prototype v00.00.08     {clock}     [{state}]", topStyle);

        GUI.Box(new Rect(10, 52, 285, 84),
            "DANMARK\nHold højderyggen og gården\nSlå de to preussiske regimenter tilbage", helpStyle);
        GUI.Box(new Rect(Screen.width - 295, 52, 285, 84),
            "PREUSSEN\nTag højderyggen\nBryd den danske stilling / flankér vejen", helpStyle);

        Camera mainCamera = Camera.main;
        foreach (Regiment regiment in regiments)
        {
            if (regiment == null || mainCamera == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(regiment.transform.position + Vector3.up * 3f);
            if (screen.z <= 0f)
                continue;

            float x = screen.x - 100f;
            float y = Screen.height - screen.y - 31f;
            string team = regiment.Team == BattleTeam.Denmark ? "DK" : "PR";
            string routed = regiment.IsRouted ? "  ROUTED" : string.Empty;
            string label =
                $"{regiment.RegimentName} ({team})  {regiment.CurrentStrength}\n" +
                $"{regiment.WeaponShortName} | Exp {regiment.Experience:0} | Reload {regiment.CurrentReloadSeconds:0.0}s\n" +
                $"Morale {regiment.Morale:0}  Coh {regiment.Cohesion:0}{routed}";

            GUI.Box(new Rect(x, y, 200f, 62f), label, unitStyle);

            if (regiment.HasHitFeedback)
                GUI.Box(new Rect(screen.x - 62f, y - 30f, 124f, 26f), $"Ramte {regiment.LastVolleyHits}", hitStyle);
        }

        GUI.Box(new Rect(10, Screen.height - 116, 390, 106),
            "STYRING\nKlik = vælg | Shift+klik = flere | Højreklik = flyt/angrib\nF = line | C = column | H = hold | T = range\nWASD = kamera | Q/E = roter | hjul = zoom | Space = pause | 1/2/3 = speed | R = restart", helpStyle);

        if (!string.IsNullOrEmpty(resultMessage))
        {
            Rect resultRect = new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 45f, 540f, 90f);
            GUI.Box(resultRect, resultMessage + "\nTryk R for at spille igen", topStyle);
        }
    }
}
