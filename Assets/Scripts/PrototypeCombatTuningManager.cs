using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public sealed class PrototypeCombatTuningManager : MonoBehaviour
{
    public static PrototypeCombatTuningManager Instance { get; private set; }

    public static float BaseHitChancePercent { get; private set; } = 1.40f;
    public static float CloseRangeMultiplier { get; private set; } = 1.75f;
    public static float MediumRangeMultiplier { get; private set; } = 1.00f;
    public static float LongRangeMultiplier { get; private set; } = 0.30f;
    public static float FiringFractionPercent { get; private set; } = 58f;
    public static int StartingAmmoRoundsPerMan { get; private set; } = 60;
    public static int CasualtiesPerBody { get; private set; } = 8;

    private FieldInfo baseAccuracyField;
    private bool showPanel;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCombatTuningManager>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeCombatTuningManager_v009");
        managerObject.AddComponent<PrototypeCombatTuningManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseAccuracyField = typeof(Regiment).GetField(
            "baseAccuracy",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (baseAccuracyField == null)
            Debug.LogError("COMBAT-TUNE: Regiment.baseAccuracy blev ikke fundet.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            TogglePanel();

        ApplyCombatTuning();
    }

    public void TogglePanel()
    {
        showPanel = !showPanel;
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (!showPanel)
            return false;

        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return GetPanelRect().Contains(guiPoint);
    }

    public static int GetStartingAmmoRoundsPerMan()
    {
        return Mathf.Clamp(StartingAmmoRoundsPerMan, 1, 300);
    }

    public static int GetCasualtiesPerBody()
    {
        return Mathf.Clamp(CasualtiesPerBody, 1, 100);
    }

    private void ApplyCombatTuning()
    {
        if (baseAccuracyField == null)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            Regiment target = FindNearestReferenceTarget(regiment, battle);
            float rangeMultiplier = MediumRangeMultiplier;

            if (target != null)
            {
                float distance = Vector3.Distance(regiment.transform.position, target.transform.position);
                rangeMultiplier = GetRangeTuningMultiplier(regiment, distance);
            }

            // The original kernel internally assumes 58% of the regiment fires in a volley.
            // Scaling baseAccuracy by requested/58 makes the TEST slider act like a firing-fraction control
            // without replacing the authoritative combat kernel in this gate.
            float firingFractionScale = Mathf.Clamp(FiringFractionPercent, 1f, 100f) / 58f;
            float weaponFactor = regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle
                ? 0.013f / 0.014f
                : 1f;

            float tunedAccuracy =
                Mathf.Max(0.00001f, BaseHitChancePercent / 100f) *
                weaponFactor *
                Mathf.Max(0.01f, rangeMultiplier) *
                firingFractionScale;

            baseAccuracyField.SetValue(regiment, tunedAccuracy);
        }
    }

    private static Regiment FindNearestReferenceTarget(Regiment shooter, BattleManager battle)
    {
        Regiment nearest = null;
        float best = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate.Team == shooter.Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(shooter.transform.position, candidate.transform.position);
            if (distance > shooter.MaximumRange || distance >= best || !shooter.IsTargetInFireArc(candidate))
                continue;

            best = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static float GetRangeTuningMultiplier(Regiment regiment, float distance)
    {
        if (distance <= regiment.CloseRange)
            return CloseRangeMultiplier;

        if (distance <= regiment.EffectiveRange)
        {
            float t = Mathf.InverseLerp(regiment.CloseRange, regiment.EffectiveRange, distance);
            return Mathf.Lerp(CloseRangeMultiplier, MediumRangeMultiplier, t);
        }

        float longT = Mathf.InverseLerp(regiment.EffectiveRange, regiment.MaximumRange, distance);
        return Mathf.Lerp(MediumRangeMultiplier, LongRangeMultiplier, longT);
    }

    private Rect GetPanelRect()
    {
        float width = Mathf.Min(430f, Mathf.Max(330f, Screen.width - 80f));
        float height = 286f;
        return new Rect((Screen.width - width) * 0.5f, 78f, width, height);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 10;
        panelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 13;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!showPanel)
            return;

        EnsureStyles();
        Rect panel = GetPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);

        float x = panel.x + 10f;
        float y = panel.y + 7f;
        float w = panel.width - 20f;

        GUI.Label(new Rect(x, y, w - 72f, 22f), "KAMP-SETUP / TEST-TUNING  [F9]", titleStyle);
        if (GUI.Button(new Rect(panel.xMax - 66f, y, 56f, 22f), "LUK"))
            showPanel = false;
        y += 28f;

        BaseHitChancePercent = DrawFloatSlider(
            x, ref y, w,
            "Grundtræf pr. skytte",
            BaseHitChancePercent,
            0.05f, 3.00f,
            BaseHitChancePercent.ToString("0.00") + "%");

        CloseRangeMultiplier = DrawFloatSlider(
            x, ref y, w,
            "Close range faktor",
            CloseRangeMultiplier,
            0.50f, 3.00f,
            "x" + CloseRangeMultiplier.ToString("0.00"));

        MediumRangeMultiplier = DrawFloatSlider(
            x, ref y, w,
            "Medium range faktor",
            MediumRangeMultiplier,
            0.20f, 2.00f,
            "x" + MediumRangeMultiplier.ToString("0.00"));

        LongRangeMultiplier = DrawFloatSlider(
            x, ref y, w,
            "Long range faktor",
            LongRangeMultiplier,
            0.02f, 1.00f,
            "x" + LongRangeMultiplier.ToString("0.00"));

        FiringFractionPercent = DrawFloatSlider(
            x, ref y, w,
            "Andel mænd der skyder",
            FiringFractionPercent,
            20f, 100f,
            FiringFractionPercent.ToString("0") + "%");

        float ammo = StartingAmmoRoundsPerMan;
        ammo = DrawFloatSlider(
            x, ref y, w,
            "Start ammunition / mand",
            ammo,
            10f, 120f,
            StartingAmmoRoundsPerMan + " runder");
        StartingAmmoRoundsPerMan = Mathf.RoundToInt(ammo);

        float casualtyRatio = CasualtiesPerBody;
        casualtyRatio = DrawFloatSlider(
            x, ref y, w,
            "Tab pr. synligt lig",
            casualtyRatio,
            2f, 20f,
            CasualtiesPerBody.ToString());
        CasualtiesPerBody = Mathf.RoundToInt(casualtyRatio);

        y += 2f;
        GUI.Label(
            new Rect(x, y, w - 108f, 20f),
            "Accuracy/faktorer virker straks. Start-ammo gælder næste restart [R].",
            labelStyle);

        if (GUI.Button(new Rect(panel.xMax - 102f, y, 92f, 22f), "STANDARD"))
            ResetDefaults();
    }

    private float DrawFloatSlider(
        float x,
        ref float y,
        float width,
        string label,
        float value,
        float min,
        float max,
        string valueText)
    {
        const float labelWidth = 174f;
        const float valueWidth = 66f;
        GUI.Label(new Rect(x, y, labelWidth, 22f), label, labelStyle);
        GUI.Label(new Rect(x + width - valueWidth, y, valueWidth, 22f), valueText, labelStyle);

        float sliderX = x + labelWidth;
        float sliderWidth = Mathf.Max(70f, width - labelWidth - valueWidth - 6f);
        value = GUI.HorizontalSlider(new Rect(sliderX, y + 6f, sliderWidth, 16f), value, min, max);
        y += 29f;
        return value;
    }

    private static void ResetDefaults()
    {
        BaseHitChancePercent = 1.40f;
        CloseRangeMultiplier = 1.75f;
        MediumRangeMultiplier = 1.00f;
        LongRangeMultiplier = 0.30f;
        FiringFractionPercent = 58f;
        StartingAmmoRoundsPerMan = 60;
        CasualtiesPerBody = 8;
    }
}
