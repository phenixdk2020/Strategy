using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29k + v00.00.09f30b null-safety
// Charge input hardening without replacing the proven F25 charge/melee engine.
// F30B: reflection bindings are revalidated before capture so a transient missing
// FieldInfo/MethodInfo cannot spam CaptureLegacyTargetPick NullReferenceException.
[DefaultExecutionOrder(-900)]
public sealed class PrototypeChargeTargeting09F29K : MonoBehaviour
{
    public static PrototypeChargeTargeting09F29K Instance { get; private set; }
    public bool IsArmed => armed;

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float BottomHudHeight = 100f;

    private PrototypeInfantryCharge09F25 charge;
    private Camera cam;

    private FieldInfo pendingTargetPickField;
    private FieldInfo pendingUnitsField;
    private MethodInfo issueChargeMethod;

    private readonly List<Regiment> armedUnits = new List<Regiment>();
    private bool armed;
    private bool reflectionWarningLogged;

    private Texture2D greenTexture;
    private GUIStyle armedButtonStyle;
    private GUIStyle hintStyle;
    private GUIStyle targetStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeChargeTargeting09F29K>() == null)
            new GameObject("PrototypeChargeTargeting_v000009f29k")
                .AddComponent<PrototypeChargeTargeting09F29K>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!EnsureReflectionBindings())
        {
            Debug.LogError("CHARGE-09F29K|Installed=False|Reason=F25ReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log("CHARGE-09F29K|Installed=True|Arm=CHARGE_OR_V|Confirm=RIGHT_CLICK_ENEMY|" +
                  "LeftClickConfirm=False|MoveOrderSuppression=True|ArmedButtonGreen=True|F30BNullGuard=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (charge == null)
            charge = PrototypeInfantryCharge09F25.Instance;
        if (cam == null)
            cam = Camera.main;
        if (charge == null || cam == null)
            return;

        if (!EnsureReflectionBindings())
        {
            if (!reflectionWarningLogged)
            {
                reflectionWarningLogged = true;
                Debug.LogWarning("CHARGE-09F30B|CaptureSkipped=True|Reason=F25ReflectionUnavailable|LegacyEnginePreserved=True");
            }
            return;
        }

        CaptureLegacyTargetPick();

        if (!armed)
            return;

        RemoveInvalidArmedUnits();
        if (armedUnits.Count == 0)
        {
            CancelArmed("NO_VALID_UNITS");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelArmed("ESC");
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Regiment target = GetEnemyUnderMouse();
            if (target != null)
            {
                if (issueChargeMethod != null)
                {
                    issueChargeMethod.Invoke(charge, new object[] { new List<Regiment>(armedUnits), target });
                    Debug.Log("CHARGE-09F29K|Confirmed=True|Target=" + target.RegimentName +
                              "|Companies=" + armedUnits.Count + "|Input=RIGHT_CLICK");
                    ClearArmed();
                }
            }
            else
            {
                Debug.Log("CHARGE-09F29K|Confirmed=False|Reason=RIGHT_CLICK_NO_ENEMY|StillArmed=True");
            }

            Input.ResetInputAxes();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !PointerOverBottomHud())
            CancelArmed("LEFT_CLICK_SELECTION");
    }

    private bool EnsureReflectionBindings()
    {
        if (pendingTargetPickField == null)
            pendingTargetPickField = typeof(PrototypeInfantryCharge09F25)
                .GetField("pendingTargetPick", PrivateInstance);
        if (pendingUnitsField == null)
            pendingUnitsField = typeof(PrototypeInfantryCharge09F25)
                .GetField("pendingUnits", PrivateInstance);
        if (issueChargeMethod == null)
            issueChargeMethod = typeof(PrototypeInfantryCharge09F25)
                .GetMethod("IssueCharge", PrivateInstance);

        return pendingTargetPickField != null && pendingUnitsField != null && issueChargeMethod != null;
    }

    private void CaptureLegacyTargetPick()
    {
        if (charge == null || !EnsureReflectionBindings())
            return;

        object pendingValue = pendingTargetPickField.GetValue(charge);
        if (!(pendingValue is bool) || !(bool)pendingValue)
            return;

        List<Regiment> legacyUnits = pendingUnitsField.GetValue(charge) as List<Regiment>;
        pendingTargetPickField.SetValue(charge, false);

        if (armed)
        {
            if (legacyUnits != null)
                legacyUnits.Clear();
            CancelArmed("TOGGLE_OFF");
            return;
        }

        armedUnits.Clear();
        if (legacyUnits != null)
        {
            for (int i = 0; i < legacyUnits.Count; i++)
            {
                Regiment unit = legacyUnits[i];
                if (unit != null && unit.Team == BattleTeam.Denmark && !unit.IsRouted && unit.CurrentStrength > 0)
                    armedUnits.Add(unit);
            }
            legacyUnits.Clear();
        }

        if (armedUnits.Count == 0)
            return;

        armed = true;
        Debug.Log("CHARGE-09F29K|Armed=True|Companies=" + armedUnits.Count +
                  "|Instruction=RIGHT_CLICK_ENEMY|FireBeginsAfterConfirm=False");
    }

    private void RemoveInvalidArmedUnits()
    {
        for (int i = armedUnits.Count - 1; i >= 0; i--)
        {
            Regiment unit = armedUnits[i];
            if (unit == null || unit.IsRouted || unit.CurrentStrength <= 0)
                armedUnits.RemoveAt(i);
        }
    }

    private void CancelArmed(string reason)
    {
        if (!armed && armedUnits.Count == 0)
            return;

        Debug.Log("CHARGE-09F29K|Armed=False|Cancelled=True|Reason=" + reason);
        ClearArmed();
    }

    private void ClearArmed()
    {
        armed = false;
        armedUnits.Clear();
    }

    private Regiment GetEnemyUnderMouse()
    {
        if (cam == null)
            return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            Regiment regiment = collider != null ? collider.GetComponentInParent<Regiment>() : null;
            if (regiment != null && regiment.Team == BattleTeam.Prussia &&
                !regiment.IsRouted && regiment.CurrentStrength > 0)
                return regiment;
        }
        return null;
    }

    private static bool PointerOverBottomHud()
    {
        return Input.mousePosition.y <= BottomHudHeight;
    }

    private void OnGUI()
    {
        if (!armed)
            return;

        EnsureStyles();
        GUI.depth = -130000;

        float width = Screen.width;
        float infoW = Mathf.Clamp(width * 0.20f, 220f, 300f);
        float aiW = Mathf.Clamp(width * 0.17f, 195f, 250f);
        float fireW = Mathf.Clamp(width * 0.19f, 215f, 275f);
        float infoX = 6f;
        float aiX = infoX + infoW + 5f;
        float fireX = aiX + aiW + 5f;
        float cmdX = fireX + fireW + 5f;
        float cmdW = width - cmdX - 6f;
        const float gap = 3f;
        float actionW = (cmdW - gap * 5f) / 6f;
        float y = Screen.height - 96f + 22f;
        Rect chargeRect = new Rect(cmdX + (actionW + gap) * 4f, y + 11f, actionW, 20f);
        GUI.Box(chargeRect, "CHARGE", armedButtonStyle);

        Rect hint = new Rect(Mathf.Max(6f, cmdX), y + 34f, Mathf.Min(360f, cmdW), 16f);
        GUI.Label(hint, "CHARGE ARMED — HØJREKLIK PÅ FJENDTLIG ENHED • ESC = ANNULLER", hintStyle);

        Regiment hover = GetEnemyUnderMouse();
        if (hover != null && cam != null)
        {
            Vector3 screen = cam.WorldToScreenPoint(hover.transform.position + Vector3.up * 4f);
            if (screen.z > 0f)
            {
                Rect targetRect = new Rect(screen.x - 58f, Screen.height - screen.y - 24f, 116f, 20f);
                GUI.Box(targetRect, "CHARGE MÅL", targetStyle);
            }
        }
    }

    private void EnsureStyles()
    {
        if (armedButtonStyle != null)
            return;

        greenTexture = MakeTexture(new Color(0.16f, 0.43f, 0.19f, 1f), "CHARGE29K_GREEN");

        armedButtonStyle = new GUIStyle(GUI.skin.box);
        armedButtonStyle.normal.background = greenTexture;
        armedButtonStyle.normal.textColor = Color.white;
        armedButtonStyle.alignment = TextAnchor.MiddleCenter;
        armedButtonStyle.fontSize = 9;
        armedButtonStyle.fontStyle = FontStyle.Bold;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.normal.textColor = new Color(0.90f, 0.96f, 0.82f, 1f);
        hintStyle.fontSize = 8;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleLeft;

        targetStyle = new GUIStyle(GUI.skin.box);
        targetStyle.normal.textColor = new Color(1f, 0.88f, 0.75f, 1f);
        targetStyle.fontSize = 9;
        targetStyle.fontStyle = FontStyle.Bold;
        targetStyle.alignment = TextAnchor.MiddleCenter;
    }

    private static Texture2D MakeTexture(Color color, string name)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }
}
