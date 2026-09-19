using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29
// Infantry anti-cavalry foundation: manual/AI square state, visible square footprint,
// multi-side range presentation and a future-facing mounted-threat API for F30 cavalry.
// Square is deliberately implemented as a tactical state layered over the existing
// Line/Column kernel so F29 does not destabilise the mature movement/route authority.
[DefaultExecutionOrder(36050)]
public sealed class PrototypeInfantrySquare09F29 : MonoBehaviour
{
    private sealed class SquareState
    {
        public Regiment Unit;
        public bool Automatic;
        public bool Ready;
        public float ReadyAt;
        public float LastThreatAt;
        public Vector3 LastThreatPosition;
        public RegimentFirePolicy PreviousFirePolicy;
        public float OriginalAccuracy;
        public bool AccuracyCaptured;
        public Vector3 OriginalColliderSize;
        public bool ColliderCaptured;
        public bool ControllerWasEnabled;
        public GameObject VisualRoot;
        public LineRenderer Outline;
        public LineRenderer CloseRing;
        public LineRenderer MediumRing;
        public LineRenderer LongRing;
    }

    public static PrototypeInfantrySquare09F29 Instance { get; private set; }

    private readonly Dictionary<Regiment, SquareState> square =
        new Dictionary<Regiment, SquareState>();

    private FieldInfo hasDestinationField;
    private FieldInfo baseAccuracyField;
    private FieldInfo visualStatesField;
    private UnityEngine.Object visualManager;

    private Material outlineMaterial;
    private Material closeMaterial;
    private Material mediumMaterial;
    private Material longMaterial;
    private GUIStyle statusStyle;

    private const float SquareVisualSpeed = 8.0f;
    private const float SquareAccuracyFactor = 0.42f;
    private const float ThreatHoldSeconds = 7.5f;
    private const float AutoSquareThreshold = 62f;
    private const float MaxThreatDistance = 220f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeInfantrySquare09F29>() == null)
            new GameObject("PrototypeInfantrySquare_v000009f29")
                .AddComponent<PrototypeInfantrySquare09F29>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        baseAccuracyField = typeof(Regiment).GetField("baseAccuracy", flags);

        BuildMaterials();

        Debug.Log(
            "SQUARE-09F29|Installed=True|ManualKey=Q|ReformLine=F|" +
            "AutoThreatAPI=True|AutoThreshold=" + AutoSquareThreshold.ToString("0") +
            "|SquareFireFactor=" + SquareAccuracyFactor.ToString("0.00") +
            "|KernelFormationLayered=True");
    }

    private void OnDestroy()
    {
        RestoreAll();
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void Update()
    {
        HandlePlayerInput();

        if (square.Count == 0)
            return;

        // Iterate a snapshot because leaving square is allowed during this pass.
        List<KeyValuePair<Regiment, SquareState>> snapshot =
            new List<KeyValuePair<Regiment, SquareState>>(square);

        foreach (KeyValuePair<Regiment, SquareState> pair in snapshot)
        {
            Regiment regiment = pair.Key;
            SquareState state = pair.Value;

            if (regiment == null)
            {
                square.Remove(regiment);
                continue;
            }

            if (regiment.IsRouted || regiment.CurrentStrength <= 0)
            {
                LeaveSquare(regiment, "UNIT_REMOVED", false);
                continue;
            }

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();

            // Manual move after a player-created square means the player wants to
            // leave square. A new parent mission can likewise reclaim an AI-enabled
            // manually squared company by producing a fresh destination.
            if (!state.Automatic && HasDestination(regiment))
            {
                LeaveSquare(regiment, "NEW_MOVEMENT_ORDER", true);
                continue;
            }

            if (state.Automatic && Time.time - state.LastThreatAt > ThreatHoldSeconds)
            {
                LeaveSquare(regiment, "THREAT_CLEARED", false);
                continue;
            }

            // Square owns local physical movement while active. Parent mission remains
            // stored in Major/Regimental layers and can resume after auto-square clears.
            regiment.OrderHold();

            if (controller != null && controller.AIEnabled)
                controller.enabled = false;

            if (!state.Ready)
            {
                regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
                if (Time.time >= state.ReadyAt)
                {
                    state.Ready = true;
                    ApplySquareFireFactor(state);
                    regiment.SetFirePolicy(state.PreviousFirePolicy);
                    Debug.Log("SQUARE-09F29|Unit=" + regiment.RegimentName +
                              "|State=READY|Automatic=" + state.Automatic +
                              "|Fire=" + state.PreviousFirePolicy);
                }
            }
            else
            {
                // First F29 fire model: square can answer a threat on any side, but
                // only one face is treated as the active firing face at a time.
                // The reduced accuracy factor approximates side-limited firing men
                // instead of giving a 360-degree full-company volley.
                Regiment target = FindNearestEnemy(regiment, regiment.GetFireTriggerRange());
                if (target != null)
                    TurnSquareTowardThreat(regiment, target.transform.position);
            }

            ForceSquareCollider(state);
            ForceSquareVisual(state);
        }
    }

    private void LateUpdate()
    {
        if (square.Count == 0)
            return;

        foreach (SquareState state in square.Values)
        {
            if (state == null || state.Unit == null)
                continue;

            ForceSquareVisual(state);
            UpdateWorldVisual(state);
            SuppressLegacyForwardFans(state.Unit);
        }
    }

    private void HandlePlayerInput()
    {
        if (BattleManager.Instance == null || BattleManager.Instance.Regiments == null)
            return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Q) && shift)
        {
            // QA hook until F30 adds a real mounted unit. Requires AI ON.
            foreach (Regiment regiment in SelectedDanishCompanies())
            {
                Vector3 source = regiment.transform.position + regiment.transform.forward * 70f;
                ReportMountedThreat(regiment, source, 70f, 8.5f, 1.0f, true);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            List<Regiment> selected = SelectedDanishCompanies();
            bool anySquare = false;
            foreach (Regiment regiment in selected)
                if (IsInSquare(regiment))
                    anySquare = true;

            foreach (Regiment regiment in selected)
            {
                if (anySquare)
                    LeaveSquare(regiment, "MANUAL_REFORM_LINE", false);
                else
                    EnterSquare(regiment, false, regiment.transform.position + regiment.transform.forward * 80f);
            }
        }

        bool linePressed = Input.GetKeyDown(KeyCode.F);
        bool columnPressed = Input.GetKeyDown(KeyCode.C);
        if (linePressed || columnPressed)
        {
            foreach (Regiment regiment in SelectedDanishCompanies())
                if (IsInSquare(regiment))
                    LeaveSquare(regiment, columnPressed ? "MANUAL_COLUMN" : "MANUAL_LINE", true);
        }
    }

    public static bool IsInSquare(Regiment regiment)
    {
        return Instance != null && regiment != null && Instance.square.ContainsKey(regiment);
    }

    public static bool IsSquareReady(Regiment regiment)
    {
        return Instance != null && regiment != null &&
               Instance.square.TryGetValue(regiment, out SquareState state) &&
               state.Ready;
    }

    public static float GetMountedChargeDefenseMultiplier(Regiment regiment)
    {
        if (!IsInSquare(regiment))
            return 1f;
        return IsSquareReady(regiment) ? 0.34f : 0.72f;
    }

    // F30 mounted units call this continuously while presenting a credible threat.
    // relativeStrength is 1.0 for roughly equal strength, closingSpeed is m/s.
    public static void ReportMountedThreat(
        Regiment infantry,
        Vector3 sourcePosition,
        float distance,
        float closingSpeed,
        float relativeStrength,
        bool charging)
    {
        if (Instance == null || infantry == null || infantry.IsRouted)
            return;

        Instance.HandleMountedThreat(
            infantry,
            sourcePosition,
            distance,
            closingSpeed,
            relativeStrength,
            charging);
    }

    private void HandleMountedThreat(
        Regiment infantry,
        Vector3 sourcePosition,
        float distance,
        float closingSpeed,
        float relativeStrength,
        bool charging)
    {
        // F30N: mounted-threat assessment is team-neutral. The original
        // Denmark-only guard existed before real opposing cavalry interaction.
        if (distance > MaxThreatDistance)
            return;

        float proximity = 1f - Mathf.Clamp01(distance / MaxThreatDistance);
        float score =
            (charging ? 38f : 10f) +
            proximity * 34f +
            Mathf.Clamp(closingSpeed, 0f, 12f) * 2.1f +
            Mathf.Clamp(relativeStrength, 0.25f, 2f) * 9f;

        if (square.TryGetValue(infantry, out SquareState existing))
        {
            existing.LastThreatAt = Time.time;
            existing.LastThreatPosition = sourcePosition;
            return;
        }

        OfficerAIController controller = infantry.GetComponent<OfficerAIController>();
        if (controller == null || !controller.AIEnabled)
            return;

        if (score < AutoSquareThreshold)
        {
            Debug.Log("SQUARE-THREAT-09F29|Unit=" + infantry.RegimentName +
                      "|Score=" + score.ToString("0") +
                      "|Distance=" + distance.ToString("0") +
                      "|Charging=" + charging + "|Action=NO_SQUARE");
            return;
        }

        EnterSquare(infantry, true, sourcePosition);
        Debug.Log("SQUARE-THREAT-09F29|Unit=" + infantry.RegimentName +
                  "|Score=" + score.ToString("0") +
                  "|Distance=" + distance.ToString("0") +
                  "|Closing=" + closingSpeed.ToString("0.0") +
                  "|Charging=" + charging + "|Action=FORM_SQUARE");
    }

    private void EnterSquare(Regiment regiment, bool automatic, Vector3 threatPosition)
    {
        if (regiment == null || regiment.IsRouted || square.ContainsKey(regiment))
            return;

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (!automatic && controller != null)
            controller.SetAIEnabled(false);

        float reformSeconds = Mathf.Lerp(8.4f, 5.2f, Mathf.Clamp01(regiment.Cohesion / 100f));
        SquareState state = new SquareState
        {
            Unit = regiment,
            Automatic = automatic,
            Ready = false,
            ReadyAt = Time.time + reformSeconds,
            LastThreatAt = Time.time,
            LastThreatPosition = threatPosition,
            PreviousFirePolicy = regiment.FirePolicy,
            ControllerWasEnabled = controller != null && controller.enabled
        };

        CaptureAccuracy(state);
        CaptureCollider(state);
        CreateWorldVisual(state);
        square[regiment] = state;

        regiment.OrderHold();
        regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
        if (controller != null && controller.AIEnabled)
            controller.enabled = false;

        Debug.Log("SQUARE-09F29|Unit=" + regiment.RegimentName +
                  "|State=FORMING|Automatic=" + automatic +
                  "|ReadyIn=" + reformSeconds.ToString("0.0") + "s" +
                  "|Cohesion=" + regiment.Cohesion.ToString("0") +
                  "|ParentMissionPreserved=" + automatic);
    }

    private void LeaveSquare(Regiment regiment, string reason, bool preserveMovement)
    {
        if (regiment == null || !square.TryGetValue(regiment, out SquareState state))
            return;

        RestoreAccuracy(state);
        RestoreCollider(state);

        if (!preserveMovement)
            regiment.OrderHold();
        regiment.SetFirePolicy(state.PreviousFirePolicy);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller != null && controller.AIEnabled && state.ControllerWasEnabled)
            controller.enabled = true;

        if (state.VisualRoot != null)
            Destroy(state.VisualRoot);

        square.Remove(regiment);
        RestoreLegacyFans(regiment);

        Debug.Log("SQUARE-09F29|Unit=" + regiment.RegimentName +
                  "|State=REFORM_LINE|Reason=" + reason +
                  "|PreserveMovement=" + preserveMovement +
                  "|PreviousFire=" + state.PreviousFirePolicy);
    }

    private void CaptureAccuracy(SquareState state)
    {
        if (baseAccuracyField == null || state.Unit == null)
            return;
        object value = baseAccuracyField.GetValue(state.Unit);
        if (!(value is float))
            return;
        state.OriginalAccuracy = (float)value;
        state.AccuracyCaptured = true;
    }

    private void ApplySquareFireFactor(SquareState state)
    {
        if (!state.AccuracyCaptured || baseAccuracyField == null || state.Unit == null)
            return;
        baseAccuracyField.SetValue(state.Unit, state.OriginalAccuracy * SquareAccuracyFactor);
    }

    private void RestoreAccuracy(SquareState state)
    {
        if (state != null && state.AccuracyCaptured && baseAccuracyField != null && state.Unit != null)
            baseAccuracyField.SetValue(state.Unit, state.OriginalAccuracy);
    }

    private static void CaptureCollider(SquareState state)
    {
        BoxCollider box = state.Unit != null ? state.Unit.GetComponent<BoxCollider>() : null;
        if (box == null)
            return;
        state.OriginalColliderSize = box.size;
        state.ColliderCaptured = true;
    }

    private static void ForceSquareCollider(SquareState state)
    {
        BoxCollider box = state.Unit != null ? state.Unit.GetComponent<BoxCollider>() : null;
        if (box != null)
            box.size = new Vector3(20.5f, 2.2f, 20.5f);
    }

    private static void RestoreCollider(SquareState state)
    {
        if (!state.ColliderCaptured || state.Unit == null)
            return;
        BoxCollider box = state.Unit.GetComponent<BoxCollider>();
        if (box != null)
            box.size = state.OriginalColliderSize;
    }

    private bool HasDestination(Regiment regiment)
    {
        if (hasDestinationField == null || regiment == null)
            return false;
        object value = hasDestinationField.GetValue(regiment);
        return value is bool && (bool)value;
    }

    private void ResolveVisualReflection()
    {
        if (visualManager != null && visualStatesField != null)
            return;

        PrototypeBattleVisuals09F5 manager =
            UnityEngine.Object.FindAnyObjectByType<PrototypeBattleVisuals09F5>();
        if (manager == null)
            return;

        visualManager = manager;
        visualStatesField = typeof(PrototypeBattleVisuals09F5).GetField(
            "states", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void ForceSquareVisual(SquareState state)
    {
        if (state == null || state.Unit == null)
            return;

        ResolveVisualReflection();
        if (visualManager == null || visualStatesField == null)
            return;

        IDictionary states = visualStatesField.GetValue(visualManager) as IDictionary;
        if (states == null || !states.Contains(state.Unit))
            return;

        object visualState = states[state.Unit];
        if (visualState == null)
            return;

        FieldInfo positionsField = visualState.GetType().GetField(
            "LocalPositions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (positionsField == null)
            return;

        IList positions = positionsField.GetValue(visualState) as IList;
        if (positions == null || positions.Count == 0)
            return;

        float halfSide = GetSquareHalfSide(state.Unit);
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 current = positions[i] is Vector3 ? (Vector3)positions[i] : Vector3.zero;
            Vector3 target = SquarePosition(i, positions.Count, halfSide);
            positions[i] = Vector3.MoveTowards(
                current,
                target,
                SquareVisualSpeed * Time.deltaTime);
        }
    }

    private static float GetSquareHalfSide(Regiment regiment)
    {
        int perSidePerRank = Mathf.CeilToInt(Mathf.Max(1, regiment.CurrentStrength) / 8f);
        float sideLength = Mathf.Max(7.5f, (perSidePerRank - 1) * 0.75f);
        return Mathf.Clamp(sideLength * 0.5f, 4.0f, 10.5f);
    }

    private static Vector3 SquarePosition(int index, int total, float halfSide)
    {
        int rank = index & 1;
        int perimeterIndex = index / 2;
        int perimeterCount = Mathf.Max(4, Mathf.CeilToInt(total / 2f));
        float u = (perimeterIndex / (float)perimeterCount) * 4f;
        int side = Mathf.Clamp(Mathf.FloorToInt(u), 0, 3);
        float t = u - side;
        float h = Mathf.Max(1.5f, halfSide - rank * 0.90f);

        switch (side)
        {
            case 0: return new Vector3(Mathf.Lerp(-h, h, t), 0f, h);
            case 1: return new Vector3(h, 0f, Mathf.Lerp(h, -h, t));
            case 2: return new Vector3(Mathf.Lerp(h, -h, t), 0f, -h);
            default: return new Vector3(-h, 0f, Mathf.Lerp(-h, h, t));
        }
    }

    private static Regiment FindNearestEnemy(Regiment unit, float maxDistance)
    {
        if (unit == null || maxDistance <= 0f || BattleManager.Instance == null)
            return null;

        Regiment best = null;
        float bestDistance = maxDistance;
        foreach (Regiment candidate in BattleManager.Instance.Regiments)
        {
            if (candidate == null || candidate.Team == unit.Team || candidate.IsRouted || candidate.CurrentStrength <= 0)
                continue;
            float d = PlanarDistance(unit.transform.position, candidate.transform.position);
            if (d <= bestDistance)
            {
                bestDistance = d;
                best = candidate;
            }
        }
        return best;
    }

    private static void TurnSquareTowardThreat(Regiment regiment, Vector3 point)
    {
        Vector3 direction = point - regiment.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;
        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        regiment.transform.rotation = Quaternion.RotateTowards(
            regiment.transform.rotation,
            desired,
            120f * Time.deltaTime);
    }

    private List<Regiment> SelectedDanishCompanies()
    {
        List<Regiment> result = new List<Regiment>();
        if (BattleManager.Instance == null || BattleManager.Instance.Regiments == null)
            return result;
        foreach (Regiment regiment in BattleManager.Instance.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                result.Add(regiment);
        return result;
    }

    private void BuildMaterials()
    {
        outlineMaterial = MakeMaterial(new Color(0.98f, 0.84f, 0.20f, 0.96f), "Square29Outline");
        closeMaterial = MakeMaterial(new Color(1.00f, 0.92f, 0.10f, 0.92f), "Square29Close");
        mediumMaterial = MakeMaterial(new Color(1.00f, 0.58f, 0.05f, 0.90f), "Square29Medium");
        longMaterial = MakeMaterial(new Color(1.00f, 0.16f, 0.04f, 0.88f), "Square29Long");
    }

    private static Material MakeMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private void CreateWorldVisual(SquareState state)
    {
        GameObject root = new GameObject("Square29_" + state.Unit.RegimentName);
        root.transform.SetParent(transform, false);
        state.VisualRoot = root;
        state.Outline = MakeLine(root.transform, "SquareOutline", 0.20f, outlineMaterial, true);
        state.CloseRing = MakeLine(root.transform, "SquareClose", 0.11f, closeMaterial, true);
        state.MediumRing = MakeLine(root.transform, "SquareMedium", 0.14f, mediumMaterial, true);
        state.LongRing = MakeLine(root.transform, "SquareLong", 0.18f, longMaterial, true);
    }

    private static LineRenderer MakeLine(Transform parent, string name, float width, Material material, bool loop)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static void UpdateWorldVisual(SquareState state)
    {
        Regiment regiment = state.Unit;
        if (regiment == null)
            return;

        bool selected = regiment.IsSelected;
        float half = GetSquareHalfSide(regiment) + 0.65f;
        DrawWorldSquare(state.Outline, regiment.transform.position, regiment.transform.rotation, half, selected);

        bool rangeVisible = selected && regiment.ShowRange && state.Ready;
        DrawWorldCircle(state.CloseRing, regiment.transform.position, regiment.CloseRange + half, rangeVisible);
        DrawWorldCircle(state.MediumRing, regiment.transform.position, regiment.EffectiveRange + half, rangeVisible);
        DrawWorldCircle(state.LongRing, regiment.transform.position, regiment.MaximumRange + half, rangeVisible);
    }

    private static void DrawWorldSquare(LineRenderer line, Vector3 center, Quaternion rotation, float half, bool visible)
    {
        if (line == null)
            return;
        line.enabled = visible;
        if (!visible)
            return;

        Vector3[] local =
        {
            new Vector3(-half, 0f, -half),
            new Vector3(half, 0f, -half),
            new Vector3(half, 0f, half),
            new Vector3(-half, 0f, half)
        };
        line.positionCount = 4;
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = center + rotation * local[i];
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.44f;
            line.SetPosition(i, p);
        }
    }

    private static void DrawWorldCircle(LineRenderer line, Vector3 center, float radius, bool visible)
    {
        if (line == null)
            return;
        line.enabled = visible;
        if (!visible)
            return;

        const int segments = 64;
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.42f;
            line.SetPosition(i, p);
        }
    }

    private static void SuppressLegacyForwardFans(Regiment regiment)
    {
        if (regiment == null)
            return;
        SetChildLine(regiment.transform.Find("CloseRangeFan"), false);
        SetChildLine(regiment.transform.Find("MediumRangeFan"), false);
        SetChildLine(regiment.transform.Find("LongRangeFan"), false);
    }

    private static void RestoreLegacyFans(Regiment regiment)
    {
        if (regiment != null)
            regiment.RefreshRangeVisibility();
    }

    private static void SetChildLine(Transform child, bool value)
    {
        if (child == null)
            return;
        LineRenderer line = child.GetComponent<LineRenderer>();
        if (line != null)
            line.enabled = value;
    }

    private void EnsureStatusStyle()
    {
        if (statusStyle != null)
            return;
        statusStyle = PrototypeUiTheme09F15.AccentBox(9);
        statusStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void OnGUI()
    {
        EnsureStatusStyle();
        Regiment selectedSquare = null;
        SquareState selectedState = null;
        foreach (KeyValuePair<Regiment, SquareState> pair in square)
        {
            if (pair.Key != null && pair.Key.IsSelected)
            {
                selectedSquare = pair.Key;
                selectedState = pair.Value;
                break;
            }
        }

        if (selectedSquare == null || selectedState == null)
            return;

        GUI.depth = -890;
        string text = selectedState.Ready
            ? "KARRÉ KLAR | Q = REFORM LINE"
            : "DANNER KARRÉ | " + Mathf.Max(0f, selectedState.ReadyAt - Time.time).ToString("0.0") + " s";
        GUI.Box(new Rect(8f, Screen.height - 119f, 250f, 24f), text, statusStyle);
    }

    private void RestoreAll()
    {
        List<Regiment> units = new List<Regiment>(square.Keys);
        foreach (Regiment regiment in units)
            if (regiment != null)
                LeaveSquare(regiment, "SYSTEM_DISABLED", false);
        square.Clear();
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
