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
    private const float HoverInfoWidth = 300f;
    private const float HoverInfoHeight = 122f;

    private readonly List<Regiment> regiments = new List<Regiment>();
    private float battleMinutes = 10f * 60f + 20f;
    private float speed = 1f;
    private bool paused;
    private string resultMessage = string.Empty;

    private GUIStyle hoverStyle;
    private GUIStyle hitStyle;
    private GUIStyle topStyle;
    private GUIStyle timeStyle;
    private GUIStyle buttonStyle;

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
        // Future hook: chronicle, prisoners and after-action reporting.
    }

    public bool IsPointerOverSimulationControls(Vector3 mousePosition)
    {
        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        if (GetTimeControlRect().Contains(guiPoint))
            return true;

        PrototypeMajorHQ09F15 majorHQ = PrototypeMajorHQ09F15.Instance;
        if (majorHQ != null && majorHQ.IsPointerOverControls(mousePosition))
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
        float x = screen.x + 18f;
        if (x + HoverInfoWidth > Screen.width - 12f)
            x = screen.x - HoverInfoWidth - 18f;

        x = Mathf.Clamp(x, 12f, Mathf.Max(12f, Screen.width - HoverInfoWidth - 12f));
        float y = Mathf.Clamp(anchorY - HoverInfoHeight * 0.55f, 42f, Mathf.Max(42f, Screen.height - HoverInfoHeight - 78f));
        return new Rect(x, y, HoverInfoWidth, HoverInfoHeight);
    }

    private void EvaluateBattleResult()
    {
        if (!string.IsNullOrEmpty(resultMessage) || regiments.Count < 2)
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
            resultMessage = "DANSK SEJR - fjenden er slået tilbage";
            SetPaused(true);
        }
        else if (!danesActive)
        {
            resultMessage = "DANSK NEDERLAG - kompagnierne er slået ud";
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

    private static Rect GetTimeControlRect()
    {
        const float width = 318f;
        const float height = 28f;
        return new Rect(Mathf.Max(8f, Screen.width - width - 8f), 8f, width, height);
    }

    private void EnsureStyles()
    {
        if (hoverStyle != null)
            return;

        hoverStyle = new GUIStyle(GUI.skin.box);
        hoverStyle.fontSize = 10;
        hoverStyle.alignment = TextAnchor.UpperLeft;
        hoverStyle.padding = new RectOffset(8, 8, 6, 6);
        hoverStyle.normal.textColor = new Color(1f, 1f, 1f, 0.96f);
        hoverStyle.wordWrap = true;

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
        timeStyle.fontSize = 9;
        timeStyle.fontStyle = FontStyle.Bold;
        timeStyle.alignment = TextAnchor.MiddleCenter;
        timeStyle.normal.textColor = Color.white;
        timeStyle.padding = new RectOffset(4, 4, 1, 1);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 9;
        buttonStyle.padding = new RectOffset(2, 2, 1, 1);
    }

    private void DrawTimeControls(string clock)
    {
        Rect panel = GetTimeControlRect();
        GUI.Box(panel, string.Empty);

        float x = panel.x + 3f;
        float y = panel.y + 3f;
        const float h = 22f;
        const float gap = 2f;
        const float clockWidth = 136f;
        const float buttonWidth = 27f;

        string state = paused
            ? "PAUSE"
            : (Mathf.Approximately(speed, 0.5f) ? "x0.5" : "x" + speed.ToString("0"));

        GUI.Box(new Rect(x, y, clockWidth, h), clock + " | " + state, timeStyle);
        x += clockWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), paused ? ">" : "II", buttonStyle))
            SetPaused(!paused);
        x += buttonWidth + gap;

        if (GUI.Button(new Rect(x, y, buttonWidth, h), ".5", buttonStyle))
            SetSpeed(0.5f);
        x += buttonWidth + gap;
        if (GUI.Button(new Rect(x, y, buttonWidth, h), "1", buttonStyle))
            SetSpeed(1f);
        x += buttonWidth + gap;
        if (GUI.Button(new Rect(x, y, buttonWidth, h), "2", buttonStyle))
            SetSpeed(2f);
        x += buttonWidth + gap;
        if (GUI.Button(new Rect(x, y, buttonWidth, h), "5", buttonStyle))
            SetSpeed(5f);
        x += buttonWidth + gap;
        if (GUI.Button(new Rect(x, y, buttonWidth, h), "20", buttonStyle))
            SetSpeed(20f);
    }

    private static string GetDisplayName(Regiment regiment)
    {
        if (regiment == null)
            return string.Empty;

        if (regiment.Team == BattleTeam.Denmark && regiment.RegimentName == "1. Regiment")
            return "1. Kompagni - Sjællandske Livregiment";

        if (regiment.Team == BattleTeam.Denmark && regiment.RegimentName == "5. Regiment")
            return "2. Kompagni - Sjællandske Livregiment";

        if (regiment.Team == BattleTeam.Prussia && regiment.RegimentName == "8th Regiment")
            return "8. Kompagni - Preussen";

        return regiment.RegimentName;
    }

    private static string GetOrderLabel(Regiment regiment)
    {
        if (regiment == null)
            return "-";

        OfficerAIController ai = regiment.GetComponent<OfficerAIController>();
        if (ai == null)
            return "Manuel";

        if (!ai.AIEnabled)
            return "Manuel";

        return ai.CurrentTask.ToString();
    }

    private void OnGUI()
    {
        EnsureStyles();

        int hours = Mathf.FloorToInt(battleMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(battleMinutes) % 60;
        DrawTimeControls($"{hours:00}:{minutes:00}");

        Camera mainCamera = Camera.main;
        Regiment hoveredRegiment = GetHoveredRegiment(mainCamera);

        foreach (Regiment regiment in regiments)
        {
            if (regiment == null || mainCamera == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(regiment.transform.position + Vector3.up * 2.2f);
            if (screen.z <= 0f)
                continue;

            if (regiment == hoveredRegiment)
            {
                int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
                int ammo = PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment);
                int startAmmo = PrototypeCombatStatusManager.GetStartingAmmunitionRoundsPerMan(regiment);
                string team = regiment.Team == BattleTeam.Denmark ? "Danmark" : "Preussen";
                string routed = regiment.IsRouted ? "  |  ROUTED" : string.Empty;

                string label =
                    GetDisplayName(regiment) + routed + "\n" +
                    $"Side: {team}   Styrke: {regiment.CurrentStrength}/{regiment.InitialStrength}   Faldne: {losses}\n" +
                    $"Formation: {regiment.Formation}   Ordre: {GetOrderLabel(regiment)}\n" +
                    $"Moral: {regiment.Morale:0}   Cohesion: {regiment.Cohesion:0}   Ammo: {ammo}/{startAmmo}\n" +
                    $"{regiment.WeaponShortName}   Effektiv: {regiment.EffectiveRange:0} m   Lang: {regiment.MaximumRange:0} m";

                GUI.Box(GetHoverInfoRect(screen), label, hoverStyle);
            }

            if (PrototypeCombatStatusManager.TryGetVolleyFeedback(regiment, out int volleyHits))
            {
                float hitY = Mathf.Max(42f, Screen.height - screen.y - 42f);
                GUI.Box(new Rect(screen.x - 55f, hitY, 110f, 24f), $"Ramte {volleyHits}", hitStyle);
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
