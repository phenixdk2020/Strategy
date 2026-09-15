using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29k
// Charge input hardening without replacing the proven F25 charge/melee engine.
// - Existing CHARGE/V activation is converted into an ARMED state.
// - ARMED charge is confirmed with RIGHT CLICK on an enemy formation.
// - LEFT CLICK never confirms a charge; clicking elsewhere cancels target-pick normally.
// - A valid/invalid right click is consumed before PlayerCommander can turn it into a move order.
// - Unified HUD CHARGE button is visibly green while target-pick is armed.
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

        pendingTargetPickField = typeof(PrototypeInfantryCharge09F25)
            .GetField("pendingTargetPick", PrivateInstance);
        pendingUnitsField = typeof(PrototypeInfantryCharge09F25)
            .GetField("pendingUnits", PrivateInstance);
        issueChargeMethod = typeof(PrototypeInfantryCharge09F25)
            .GetMethod("IssueCharge", PrivateInstance);

        if (pendingTargetPickField == null || pendingUnitsField == null || issueChargeMethod == null)
        {
            Debug.LogError("CHARGE-09F29K|Installed=False|Reason=F25ReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log("CHARGE-09F29K|Installed=True|Arm=CHARGE_OR_V|Confirm=RIGHT_CLICK_ENEMY|" +
                  "LeftClickConfirm=False|MoveOrderSuppression=True|ArmedButtonGreen=True");
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

        // The F25 engine still owns CHARGE mechanics. F29K only steals its legacy
        // left-click target-pick state and replaces that input transaction with an
        // explicit armed/right-click workflow.
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
                issueChargeMethod.Invoke(charge, new object[] { new List<Regiment>(armedUnits), target });
                Debug.Log("CHARGE-09F29K|Confirmed=True|Target=" + target.RegimentName +
                          "|Companies=" + armedUnits.Count + "|Input=RIGHT_CLICK");
                ClearArmed();
            }
            else
            {
                Debug.Log("CHARGE-09F29K|Confirmed=False|Reason=RIGHT_CLICK_NO_ENEMY|StillArmed=True");
            }

            // PlayerCommander runs later (execution order 300). Prevent the same RMB
            // edge from also creating a normal move/attack order.
            Input.ResetInputAxes();
            return;
        }

        // A normal left-click on the battlefield means the player changed their mind
        // or wants a different selection. It must never confirm charge.
        if (Input.GetMouseButtonDown(0) && !PointerOverBottomHud())
            CancelArmed("LEFT_CLICK_SELECTION");
    }

    private void CaptureLegacyTargetPick()
    {
        if (charge == null)
            return;

        object pendingValue = pendingTargetPickField.GetValue(charge);
        if (!(pendingValue is bool) || !(bool)pendingValue)
            return;

        List<Regiment> legacyUnits = pendingUnitsField.GetValue(charge) as List<Regiment>;

        // Immediately disarm the F25 left-click picker so it cannot consume LMB.
        pendingTargetPickField.SetValue(charge, false);

        if (armed)
        {
            // Clicking CHARGE (or V) again while armed is a deliberate toggle-off.
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
            {
                return regiment;
            }
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

        // Draw over the existing unified CHARGE button. The original button remains the
        // click target; this layer only makes the ARMED state visually explicit.
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
