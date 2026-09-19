using System.Collections.Generic;
using UnityEngine;

public enum PrototypeCavalryKind09F30
{
    Gardehusar,
    Dragon
}

public enum PrototypeCavalryFormation09F30
{
    Line,
    Column
}

public enum PrototypeCavalryMode09F30
{
    Mounted,
    Dismounted
}

public enum PrototypeCavalryAction09F30
{
    Hold,
    Move,
    Charge,
    Falter
}

// v00.00.09f30
// First shared mounted-cavalry core. Values are prototype QA values, not final historical stats.
// Gardehusar and Dragon share movement/formation/charge infrastructure; Dragon additionally
// supports dismount/remount with horses physically left at the dismount point.
public sealed class PrototypeCavalryUnit09F30 : MonoBehaviour
{
    private enum BridgePhase
    {
        Direct,
        NearBank,
        FarBank,
        ExitBank
    }

    public string UnitName { get; private set; }
    public PrototypeCavalryKind09F30 Kind { get; private set; }
    public PrototypeCavalryFormation09F30 Formation { get; private set; } = PrototypeCavalryFormation09F30.Line;
    public PrototypeCavalryMode09F30 Mode { get; private set; } = PrototypeCavalryMode09F30.Mounted;
    public PrototypeCavalryAction09F30 Action { get; private set; } = PrototypeCavalryAction09F30.Hold;
    public int InitialStrength { get; private set; }
    public int CurrentStrength { get; private set; }
    public float Morale { get; private set; } = 100f;
    public float Cohesion { get; private set; } = 100f;
    public bool IsSelected { get; private set; }
    public bool HasCarbine => true;
    public bool HasPistol => true;
    public bool HasSabre => true;
    public Regiment ChargeTarget => chargeTarget;
    public bool IsBridgeRouteActive => bridgePhase != BridgePhase.Direct;
    public bool IsReforming { get; private set; }
    public float FormationReadyFraction { get; private set; } = 1f;
    public string BridgePhaseLabel => bridgePhase == BridgePhase.Direct ? "DIRECT" : bridgePhase.ToString().ToUpperInvariant();
    public bool HasDestination => hasDestination;
    public Vector3 FinalDestination => finalDestination;
    public bool AutoMarchColumnActive => autoMarchColumnActive;
    public bool ManualFormationOverride => manualFormationOverride;
    public PrototypeCavalryFormation09F30 PlannedDestinationFormation =>
        bridgePhase != BridgePhase.Direct ? formationBeforeBridge : Formation;
    public Vector3 CurrentSteeringTarget => hasDestination ? ResolveSteeringTarget() : transform.position;

    public Vector2 GetCurrentFootprintSize()
    {
        return CalculateFootprint(Mode, Formation, bridgePhase != BridgePhase.Direct);
    }

    public Vector2 GetDestinationFootprintSize()
    {
        return CalculateFootprint(Mode, PlannedDestinationFormation, false);
    }

    public Vector3 GetCurrentFootprintCenterWorld()
    {
        Vector2 size = GetCurrentFootprintSize();
        float localZ = CalculateFootprintCenterOffsetZ(Mode, Formation, bridgePhase != BridgePhase.Direct, size);
        return transform.TransformPoint(new Vector3(0f, 0f, localZ));
    }

    public Vector3 GetDestinationFootprintCenterWorld(Vector3 forward)
    {
        Vector2 size = GetDestinationFootprintSize();
        float localZ = CalculateFootprintCenterOffsetZ(Mode, PlannedDestinationFormation, false, size);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = transform.forward;
        forward.Normalize();
        return finalDestination + forward * localZ;
    }

    public Vector3 GetDismountedCombatCenterWorld()
    {
        return transform.TransformPoint(new Vector3(0f, 0f, 18f));
    }

    public int GetDismountedCombatStrength()
    {
        return Mathf.Max(1, CurrentStrength - Mathf.CeilToInt(CurrentStrength * 0.25f));
    }

    public int GetHorseHolderStrength()
    {
        return Mathf.Max(1, CurrentStrength - GetDismountedCombatStrength());
    }

    private readonly List<Transform> mountedFigures = new List<Transform>();
    private readonly List<GameObject> mountedRiders = new List<GameObject>();
    private readonly List<Transform> footFigures = new List<Transform>();

    private Transform mountedRoot;
    private Transform footRoot;
    private GameObject selectionMarker;
    private BoxCollider unitCollider;
    private Material horseMaterial;
    private Material uniformMaterial;
    private Material equipmentMaterial;

    private Vector3 finalDestination;
    private bool hasDestination;
    private Regiment chargeTarget;
    private bool chargeResolved;

    private BridgePhase bridgePhase = BridgePhase.Direct;
    private int bridgeStartSide;
    private PrototypeCavalryFormation09F30 formationBeforeBridge;

    private const float BridgeZ = 22f;
    private const float BridgeBankOffset = 13.5f;
    private const float ContactDistance = 7.5f;
    private const float MountedReformSpeed = 8.0f;
    private const float FootReformSpeed = 5.0f;
    private const float FormationReadyTolerance = 0.55f;
    private const float RemountDistance = 18f;
    private const float AutoMarchColumnEnterDistance = 140f;
    private const float AutoMarchLineDistance = 90f;

    private bool autoMarchColumnActive;
    private bool manualFormationOverride;

    public void Initialize(
        string unitName,
        PrototypeCavalryKind09F30 kind,
        int strength,
        Vector3 worldPosition)
    {
        UnitName = unitName;
        Kind = kind;
        InitialStrength = Mathf.Max(20, strength);
        CurrentStrength = InitialStrength;
        transform.position = Ground(worldPosition);
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        horseMaterial = PrototypeBootstrap.CreateSharedMaterial(
            kind == PrototypeCavalryKind09F30.Gardehusar
                ? new Color(0.22f, 0.12f, 0.055f)
                : new Color(0.28f, 0.18f, 0.09f),
            "F30_Horse_" + kind);
        uniformMaterial = PrototypeBootstrap.CreateSharedMaterial(
            kind == PrototypeCavalryKind09F30.Gardehusar
                ? new Color(0.08f, 0.14f, 0.28f)
                : new Color(0.13f, 0.20f, 0.30f),
            "F30_Uniform_" + kind);
        equipmentMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.05f, 0.045f, 0.035f),
            "F30_Equipment");

        CreateVisuals();
        RefreshFormationInstant();

        Debug.Log(
            "CAVALRY-09F30|Spawn=True|Unit=" + UnitName +
            "|Kind=" + Kind +
            "|Strength=" + CurrentStrength +
            "|Mounted=True|Sabre=True|Carbine=True|Pistol=True");
    }

    private void OnDestroy()
    {
        if (mountedRoot != null && mountedRoot.parent == null)
            Destroy(mountedRoot.gameObject);
    }

    private void Update()
    {
        if (Action == PrototypeCavalryAction09F30.Charge && chargeTarget != null)
        {
            if (chargeTarget.IsRouted || chargeTarget.CurrentStrength <= 0)
            {
                OrderHold();
            }
            else
            {
                finalDestination = chargeTarget.transform.position;
                if (PlanarDistance(transform.position, chargeTarget.transform.position) <= ContactDistance)
                    ResolveChargeContact();
            }
        }

        UpdateMountedMoveFormationPolicy();
        UpdateMovement();
        UpdateVisualFormation();

        if (Action == PrototypeCavalryAction09F30.Hold)
        {
            Cohesion = Mathf.Min(100f, Cohesion + 1.1f * Time.deltaTime);
            Morale = Mathf.Min(100f, Morale + 0.35f * Time.deltaTime);
        }
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;
        if (selectionMarker != null)
            selectionMarker.SetActive(false);
    }

    public void SnapVisualFormationForInitialization()
    {
        RefreshFormationInstant();
        FormationReadyFraction = 1f;
        IsReforming = false;
        ResizeCollider();
    }

    public void SetFormationManual(PrototypeCavalryFormation09F30 formation)
    {
        manualFormationOverride = true;
        autoMarchColumnActive = false;
        SetFormation(formation);

        Debug.Log("CAVALRY-09F30K|Unit=" + UnitName +
                  "|ManualFormationOverride=True|Formation=" + formation);
    }

    public void ClearManualFormationOverride()
    {
        manualFormationOverride = false;
    }

    public void SetFormation(PrototypeCavalryFormation09F30 formation)
    {
        if (bridgePhase != BridgePhase.Direct)
        {
            formationBeforeBridge = formation;
            return;
        }

        if (Formation == formation)
            return;

        Formation = formation;
        IsReforming = true;
        FormationReadyFraction = 0f;
        ResizeCollider();

        Debug.Log("CAVALRY-09F30H|Unit=" + UnitName + "|Formation=" + Formation +
                  "|Reforming=True|Ranks=" + (Formation == PrototypeCavalryFormation09F30.Line ? "4" : "4-abreast"));
    }

    public void OrderMove(Vector3 worldPoint)
    {
        chargeTarget = null;
        chargeResolved = false;
        Action = PrototypeCavalryAction09F30.Move;

        Vector3 groundedGoal = Ground(worldPoint);
        if (!manualFormationOverride)
            ApplyMountedMoveFormationPolicy(groundedGoal, true);

        BeginRoute(groundedGoal);
    }

    public void OrderCharge(Regiment target)
    {
        if (Mode != PrototypeCavalryMode09F30.Mounted || target == null || target.Team != BattleTeam.Prussia)
            return;

        manualFormationOverride = false;
        autoMarchColumnActive = false;
        SetFormation(PrototypeCavalryFormation09F30.Line);

        chargeTarget = target;
        chargeResolved = false;
        Action = PrototypeCavalryAction09F30.Charge;
        BeginRoute(target.transform.position);

        Debug.Log(
            "CAVALRY-09F30|Unit=" + UnitName +
            "|Action=CHARGE|Target=" + target.RegimentName);
    }

    public void OrderHold()
    {
        hasDestination = false;
        chargeTarget = null;
        chargeResolved = false;
        bridgePhase = BridgePhase.Direct;
        autoMarchColumnActive = false;
        manualFormationOverride = false;
        Action = PrototypeCavalryAction09F30.Hold;
    }

    public bool Dismount()
    {
        if (Kind != PrototypeCavalryKind09F30.Dragon || Mode != PrototypeCavalryMode09F30.Mounted ||
            PrototypeCavalryAnimation09F30I.IsTransitioning(this))
            return false;

        OrderHold();
        Mode = PrototypeCavalryMode09F30.Dismounted;

        if (mountedRoot != null)
            mountedRoot.SetParent(null, true);

        if (footRoot != null)
            footRoot.gameObject.SetActive(true);

        PrototypeCavalryAnimation09F30I.BeginDismount(this);
        ResizeCollider();
        Debug.Log("CAVALRY-09F30I|Unit=" + UnitName + "|Action=DISMOUNT|Animated=True|HorseHolders=True");
        return true;
    }

    public bool Remount()
    {
        if (Kind != PrototypeCavalryKind09F30.Dragon || Mode != PrototypeCavalryMode09F30.Dismounted ||
            mountedRoot == null || PrototypeCavalryAnimation09F30I.IsTransitioning(this))
            return false;

        if (PlanarDistance(transform.position, mountedRoot.position) > RemountDistance)
        {
            Debug.LogWarning(
                "CAVALRY-09F30|Unit=" + UnitName +
                "|Action=REMOUNT|Success=False|Reason=TooFarFromHorses|Distance=" +
                PlanarDistance(transform.position, mountedRoot.position).ToString("0.0"));
            return false;
        }

        OrderHold();
        transform.position = Ground(mountedRoot.position);
        transform.rotation = mountedRoot.rotation;
        mountedRoot.SetParent(transform, true);
        mountedRoot.localPosition = Vector3.zero;
        mountedRoot.localRotation = Quaternion.identity;

        Mode = PrototypeCavalryMode09F30.Mounted;
        PrototypeCavalryAnimation09F30I.BeginRemount(this);
        ResizeCollider();
        Debug.Log("CAVALRY-09F30I|Unit=" + UnitName + "|Action=REMOUNT|Success=True|Animated=True");
        return true;
    }

    public string GetStatusLabel()
    {
        string mode = Mode == PrototypeCavalryMode09F30.Mounted ? "MOUNTED" : "AFSIDDET";
        return mode + " | " + Formation.ToString().ToUpperInvariant() + " | " + Action.ToString().ToUpperInvariant();
    }

    private void BeginRoute(Vector3 goal)
    {
        finalDestination = Ground(goal);
        hasDestination = true;

        // Preserve an active bridge transaction. F30C Officer AI can reissue a maneuver
        // goal while the unit is crossing; resetting the phase here stranded 1:1 cavalry
        // on/near the bridge.
        if (bridgePhase != BridgePhase.Direct)
            return;

        int startSide = BankSide(transform.position);
        int goalSide = BankSide(finalDestination);
        if (startSide != 0 && goalSide != 0 && startSide != goalSide)
        {
            bridgeStartSide = startSide;
            bridgePhase = BridgePhase.NearBank;
            formationBeforeBridge = Formation;
            Formation = PrototypeCavalryFormation09F30.Column;
            IsReforming = true;
            FormationReadyFraction = 0f;
            ResizeCollider();

            Debug.Log(
                "CAVALRY-09F30|Unit=" + UnitName +
                "|BridgeRoute=True|StartSide=" + bridgeStartSide +
                "|ColumnForced=True");
        }
        else
        {
            bridgePhase = BridgePhase.Direct;
        }
    }

    private void UpdateMountedMoveFormationPolicy()
    {
        if (Mode != PrototypeCavalryMode09F30.Mounted ||
            Action != PrototypeCavalryAction09F30.Move ||
            !hasDestination ||
            bridgePhase != BridgePhase.Direct ||
            manualFormationOverride)
        {
            return;
        }

        ApplyMountedMoveFormationPolicy(finalDestination, false);
    }

    private void ApplyMountedMoveFormationPolicy(Vector3 goal, bool newOrder)
    {
        if (Mode != PrototypeCavalryMode09F30.Mounted || manualFormationOverride)
            return;

        float remaining = PlanarDistance(transform.position, goal);
        bool enemyInsideLong = HasEnemyInsideLongRange();

        if (enemyInsideLong || remaining <= AutoMarchLineDistance)
        {
            if (autoMarchColumnActive || (newOrder && Formation == PrototypeCavalryFormation09F30.Column))
            {
                autoMarchColumnActive = false;
                SetFormation(PrototypeCavalryFormation09F30.Line);
                Debug.Log("CAVALRY-09F30K|Unit=" + UnitName +
                          "|AutoFormation=LINE|Reason=" +
                          (enemyInsideLong ? "ENEMY_INSIDE_LONG" : "NEAR_DESTINATION") +
                          "|Remaining=" + remaining.ToString("0"));
            }
            return;
        }

        if (!autoMarchColumnActive && remaining >= AutoMarchColumnEnterDistance)
        {
            autoMarchColumnActive = true;
            SetFormation(PrototypeCavalryFormation09F30.Column);
            Debug.Log("CAVALRY-09F30K|Unit=" + UnitName +
                      "|AutoFormation=COLUMN|Reason=LONG_MARCH|Remaining=" +
                      remaining.ToString("0"));
        }
    }

    private bool HasEnemyInsideLongRange()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return false;

        float longRange = PrototypeRangeTuning09F8.MaximumRangeMetres;
        for (int i = 0; i < battle.Regiments.Count; i++)
        {
            Regiment enemy = battle.Regiments[i];
            if (enemy == null ||
                enemy.Team != BattleTeam.Prussia ||
                enemy.IsRouted ||
                enemy.CurrentStrength <= 0)
            {
                continue;
            }

            if (PlanarDistance(transform.position, enemy.transform.position) <= longRange)
                return true;
        }

        return false;
    }

    private void UpdateMovement()
    {
        if (!hasDestination)
            return;

        Vector3 steering = ResolveSteeringTarget();
        Vector3 here = transform.position;
        Vector3 delta = steering - here;
        delta.y = 0f;

        if (delta.magnitude <= 1.2f)
        {
            if (AdvanceBridgePhase())
                return;

            hasDestination = false;
            if (Action == PrototypeCavalryAction09F30.Move || Action == PrototypeCavalryAction09F30.Falter)
                Action = PrototypeCavalryAction09F30.Hold;
            return;
        }

        float speed = GetMoveSpeed();
        Vector3 step = delta.normalized * speed * Mathf.Lerp(0.72f, 1f, Cohesion / 100f) * Time.deltaTime;
        if (step.magnitude > delta.magnitude)
            step = delta;

        Vector3 next = Ground(here + step);
        transform.position = next;

        Quaternion desired = Quaternion.LookRotation(delta.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 5.2f * Time.deltaTime);

        float fatigue = Mode == PrototypeCavalryMode09F30.Mounted ? 0.10f : 0.15f;
        if (Action == PrototypeCavalryAction09F30.Charge)
            fatigue = 0.42f;
        Cohesion = Mathf.Max(28f, Cohesion - fatigue * Time.deltaTime);
    }

    private Vector3 ResolveSteeringTarget()
    {
        if (bridgePhase == BridgePhase.Direct)
            return finalDestination;

        Vector3 bridge = new Vector3(
            PrototypeBootstrap.StreamCenterX(BridgeZ),
            0f,
            BridgeZ);
        bridge = Ground(bridge);

        if (bridgePhase == BridgePhase.NearBank)
            return Ground(bridge + Vector3.right * (bridgeStartSide * BridgeBankOffset));

        if (bridgePhase == BridgePhase.FarBank)
            return Ground(bridge - Vector3.right * (bridgeStartSide * BridgeBankOffset));

        // Do not reform immediately at the far bridge lip. A 1:1 two-abreast cavalry
        // column is long; move the head far enough onto the bank for the whole column
        // to clear the bridge before restoring the normal four-rank line.
        float exit = BridgeExitClearance();
        return Ground(bridge - Vector3.right * (bridgeStartSide * exit));
    }

    private bool AdvanceBridgePhase()
    {
        if (bridgePhase == BridgePhase.NearBank)
        {
            bridgePhase = BridgePhase.FarBank;
            return true;
        }

        if (bridgePhase == BridgePhase.FarBank)
        {
            bridgePhase = BridgePhase.ExitBank;
            return true;
        }

        if (bridgePhase == BridgePhase.ExitBank)
        {
            bridgePhase = BridgePhase.Direct;
            Formation = formationBeforeBridge;
            IsReforming = true;
            FormationReadyFraction = 0f;
            ResizeCollider();
            Debug.Log("CAVALRY-09F30H|Unit=" + UnitName +
                      "|BridgeRoute=False|CrossingComplete=True|Reform=" + Formation);
            return true;
        }

        return false;
    }

    private float GetMoveSpeed()
    {
        if (Mode == PrototypeCavalryMode09F30.Dismounted)
            return 3.15f;

        if (bridgePhase != BridgePhase.Direct)
            return 5.6f;

        if (Action == PrototypeCavalryAction09F30.Charge)
            return IsReforming ? 4.4f : 13.2f;

        return Formation == PrototypeCavalryFormation09F30.Column ? 9.4f : 8.2f;
    }

    private void ResolveChargeContact()
    {
        if (chargeResolved || chargeTarget == null)
            return;
        chargeResolved = true;

        Regiment target = chargeTarget;
        bool inSquare = PrototypeInfantrySquare09F29.IsInSquare(target);
        bool squareReady = inSquare && PrototypeInfantrySquare09F29.IsSquareReady(target);

        if (squareReady)
        {
            Morale = Mathf.Max(0f, Morale - 12f);
            Cohesion = Mathf.Max(0f, Cohesion - 18f);

            Vector3 away = transform.position - target.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -target.transform.forward;
            finalDestination = Ground(transform.position + away.normalized * 38f);
            hasDestination = true;
            chargeTarget = null;
            Action = PrototypeCavalryAction09F30.Falter;

            Debug.Log(
                "CAVALRY-CHARGE-09F30|Unit=" + UnitName +
                "|Target=" + target.RegimentName +
                "|Contact=SQUARE_READY|Result=FALTER|Hits=0");
            return;
        }

        string aspect = GetChargeAspect(target);
        float rate;
        float shock;
        switch (aspect)
        {
            case "REAR":
                rate = 0.065f;
                shock = 15f;
                break;
            case "FLANK":
                rate = 0.045f;
                shock = 10f;
                break;
            default:
                rate = 0.025f;
                shock = 6f;
                break;
        }

        if (inSquare && !squareReady)
        {
            rate *= 1.25f;
            shock *= 1.25f;
        }

        float strengthFactor = Mathf.Lerp(0.55f, 1f, CurrentStrength / (float)Mathf.Max(1, InitialStrength));
        int hits = Mathf.Clamp(
            Mathf.RoundToInt(CurrentStrength * rate * strengthFactor * Random.Range(0.82f, 1.18f)),
            1,
            18);

        target.ReceiveVolley(hits, shock, null);
        Cohesion = Mathf.Max(25f, Cohesion - (aspect == "FRONT" ? 9f : 5f));
        chargeTarget = null;
        hasDestination = false;
        Action = PrototypeCavalryAction09F30.Hold;

        Debug.Log(
            "CAVALRY-CHARGE-09F30|Unit=" + UnitName +
            "|Target=" + target.RegimentName +
            "|Aspect=" + aspect +
            "|TargetSquare=" + inSquare +
            "|SquareReady=" + squareReady +
            "|Hits=" + hits +
            "|Shock=" + shock.ToString("0.0"));
    }

    private string GetChargeAspect(Regiment target)
    {
        Vector3 fromTargetToCavalry = transform.position - target.transform.position;
        fromTargetToCavalry.y = 0f;
        if (fromTargetToCavalry.sqrMagnitude < 0.01f)
            return "FRONT";

        Vector3 facing = target.transform.forward;
        facing.y = 0f;
        float dot = Vector3.Dot(facing.normalized, fromTargetToCavalry.normalized);
        if (dot >= 0.55f)
            return "FRONT";
        if (dot <= -0.55f)
            return "REAR";
        return "FLANK";
    }

    private void CreateVisuals()
    {
        mountedRoot = new GameObject("MountedVisual09F30").transform;
        mountedRoot.SetParent(transform, false);

        footRoot = new GameObject("DismountedVisual09F30").transform;
        footRoot.SetParent(transform, false);
        footRoot.gameObject.SetActive(false);

        int visualCount = Mathf.Clamp(Mathf.CeilToInt(InitialStrength / 10f), 8, 18);
        for (int i = 0; i < visualCount; i++)
        {
            CreateMountedFigure(i);
            CreateFootFigure(i);
        }

        unitCollider = gameObject.AddComponent<BoxCollider>();
        unitCollider.center = new Vector3(0f, 1.2f, 0f);

        selectionMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectionMarker.name = "CavalrySelection09F30";
        selectionMarker.transform.SetParent(transform, false);
        selectionMarker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        selectionMarker.transform.localScale = new Vector3(5.5f, 0.025f, 5.5f);
        selectionMarker.GetComponent<Renderer>().sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.95f, 0.78f, 0.18f),
            "F30_CavalrySelection");
        Collider markerCollider = selectionMarker.GetComponent<Collider>();
        if (markerCollider != null)
            Destroy(markerCollider);
        selectionMarker.SetActive(false);

        ResizeCollider();
    }

    private void CreateMountedFigure(int index)
    {
        GameObject root = new GameObject("Mounted_" + (index + 1));
        root.transform.SetParent(mountedRoot, false);

        GameObject horseBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horseBody.transform.SetParent(root.transform, false);
        horseBody.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        horseBody.transform.localScale = new Vector3(0.55f, 0.62f, 1.55f);
        horseBody.GetComponent<Renderer>().sharedMaterial = horseMaterial;
        Destroy(horseBody.GetComponent<Collider>());

        GameObject horseHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horseHead.transform.SetParent(root.transform, false);
        horseHead.transform.localPosition = new Vector3(0f, 1.13f, 0.95f);
        horseHead.transform.localScale = new Vector3(0.40f, 0.52f, 0.52f);
        horseHead.GetComponent<Renderer>().sharedMaterial = horseMaterial;
        Destroy(horseHead.GetComponent<Collider>());

        GameObject rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rider.name = "Rider";
        rider.transform.SetParent(root.transform, false);
        rider.transform.localPosition = new Vector3(0f, 1.72f, -0.10f);
        rider.transform.localScale = new Vector3(0.32f, 0.48f, 0.32f);
        rider.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(rider.GetComponent<Collider>());
        mountedRiders.Add(rider);

        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weapon.transform.SetParent(rider.transform, false);
        weapon.transform.localPosition = new Vector3(0.42f, 0.0f, 0.15f);
        weapon.transform.localScale = new Vector3(0.06f, 0.06f, 1.15f);
        weapon.GetComponent<Renderer>().sharedMaterial = equipmentMaterial;
        Destroy(weapon.GetComponent<Collider>());

        mountedFigures.Add(root.transform);
    }

    private void CreateFootFigure(int index)
    {
        GameObject root = new GameObject("Dismounted_" + (index + 1));
        root.transform.SetParent(footRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        body.transform.localScale = new Vector3(0.29f, 0.48f, 0.29f);
        body.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(body.GetComponent<Collider>());

        GameObject carbine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carbine.transform.SetParent(root.transform, false);
        carbine.transform.localPosition = new Vector3(0.22f, 0.78f, 0.18f);
        carbine.transform.localScale = new Vector3(0.06f, 0.06f, 0.70f);
        carbine.GetComponent<Renderer>().sharedMaterial = equipmentMaterial;
        Destroy(carbine.GetComponent<Collider>());

        footFigures.Add(root.transform);
    }

    private void UpdateVisualFormation()
    {
        int ready = 0;
        int total = 0;

        if (Mode == PrototypeCavalryMode09F30.Mounted)
        {
            total = mountedFigures.Count;
            for (int i = 0; i < mountedFigures.Count; i++)
            {
                if (mountedFigures[i] == null)
                    continue;

                Vector3 target = MountedPosition(i, mountedFigures.Count);
                mountedFigures[i].localPosition = Vector3.MoveTowards(
                    mountedFigures[i].localPosition, target, MountedReformSpeed * Time.deltaTime);
                mountedFigures[i].localRotation = Quaternion.RotateTowards(
                    mountedFigures[i].localRotation, Quaternion.identity, 120f * Time.deltaTime);

                if (Vector3.Distance(mountedFigures[i].localPosition, target) <= FormationReadyTolerance)
                    ready++;
            }
        }
        else
        {
            total = footFigures.Count;
            for (int i = 0; i < footFigures.Count; i++)
            {
                if (footFigures[i] == null)
                    continue;

                Vector3 target = FootPosition(i, footFigures.Count);
                footFigures[i].localPosition = Vector3.MoveTowards(
                    footFigures[i].localPosition, target, FootReformSpeed * Time.deltaTime);
                footFigures[i].localRotation = Quaternion.RotateTowards(
                    footFigures[i].localRotation, Quaternion.identity, 120f * Time.deltaTime);

                if (Vector3.Distance(footFigures[i].localPosition, target) <= FormationReadyTolerance)
                    ready++;
            }
        }

        FormationReadyFraction = total > 0 ? Mathf.Clamp01(ready / (float)total) : 1f;
        IsReforming = FormationReadyFraction < 0.90f;
    }

    private void RefreshFormationInstant()
    {
        for (int i = 0; i < mountedFigures.Count; i++)
            mountedFigures[i].localPosition = MountedPosition(i, mountedFigures.Count);
        for (int i = 0; i < footFigures.Count; i++)
            footFigures[i].localPosition = FootPosition(i, footFigures.Count);
    }

    private Vector3 MountedPosition(int index, int total)
    {
        // Bridge/defile column: two abreast. This is intentionally independent of the
        // normal COLUMN formation so a bridge crossing can remain narrow without changing
        // the player's normal march doctrine.
        if (bridgePhase != BridgePhase.Direct)
        {
            int bridgeColumns = 2;
            int rank = index / bridgeColumns;
            int col = index % bridgeColumns;
            return new Vector3((col - 0.5f) * 1.55f, 0f, -rank * 2.15f);
        }

        // Normal march/manoeuvre column: four abreast.
        if (Formation == PrototypeCavalryFormation09F30.Column)
        {
            int columns = 4;
            int rank = index / columns;
            int col = index % columns;
            return new Vector3((col - 1.5f) * 1.55f, 0f, -rank * 2.15f);
        }

        // Project decision F30H: normal cavalry line and charge line are both four ranks.
        int ranks = 4;
        int columnsLine = Mathf.CeilToInt(total / (float)ranks);
        int lineRank = index / columnsLine;
        int lineCol = index % columnsLine;
        return new Vector3(
            (lineCol - (columnsLine - 1) * 0.5f) * 1.70f,
            0f,
            -(lineRank - (ranks - 1) * 0.5f) * 1.75f);
    }

    private Vector3 FootPosition(int index, int total)
    {
        // F30J Dragon dismount doctrine:
        // every fourth man remains with the horses as a prototype horse-holder;
        // the remaining ~75% move forward and form a two-rank firing line.
        if (Kind == PrototypeCavalryKind09F30.Dragon && Mode == PrototypeCavalryMode09F30.Dismounted)
        {
            bool horseHolder = (index % 4) == 0;
            if (horseHolder)
            {
                int holderIndex = index / 4;
                int holderColumns = 8;
                int holderRank = holderIndex / holderColumns;
                int holderCol = holderIndex % holderColumns;
                return new Vector3(
                    (holderCol - (holderColumns - 1) * 0.5f) * 0.82f,
                    0f,
                    -5.0f - holderRank * 0.92f);
            }

            int combatIndex = index - (index / 4) - 1;
            if (combatIndex < 0) combatIndex = 0;
            int combatCount = Mathf.Max(1, total - Mathf.CeilToInt(total / 4f));
            int combatRanks = 2;
            int combatColumns = Mathf.CeilToInt(combatCount / (float)combatRanks);
            int lineRank = combatIndex / combatColumns;
            int lineCol = combatIndex % combatColumns;
            return new Vector3(
                (lineCol - (combatColumns - 1) * 0.5f) * 0.78f,
                0f,
                18f - lineRank * 0.92f);
        }

        if (Formation == PrototypeCavalryFormation09F30.Column)
        {
            int columns = 4;
            int rank = index / columns;
            int col = index % columns;
            return new Vector3((col - 1.5f) * 0.78f, 0f, -rank * 0.85f);
        }

        int ranks = 2;
        int columnsLine = Mathf.CeilToInt(total / (float)ranks);
        int normalRank = index / columnsLine;
        int normalCol = index % columnsLine;
        return new Vector3((normalCol - (columnsLine - 1) * 0.5f) * 0.78f, 0f, -normalRank * 0.90f);
    }

    private Vector2 CalculateFootprint(
        PrototypeCavalryMode09F30 mode,
        PrototypeCavalryFormation09F30 formation,
        bool bridge)
    {
        int men = Mathf.Max(1, CurrentStrength);

        if (mode == PrototypeCavalryMode09F30.Dismounted)
        {
            int combat = Mathf.Max(1, men - Mathf.CeilToInt(men * 0.25f));
            int combatColumns = Mathf.CeilToInt(combat / 2f);
            float width = Mathf.Max(18f, combatColumns * 0.78f + 3f);
            // Includes horse-holder line around z=-5 and combat line around z=18.
            return new Vector2(width, 28f);
        }

        if (bridge)
        {
            int rows = Mathf.CeilToInt(men / 2f);
            return new Vector2(5.2f, Mathf.Max(24f, rows * 2.15f + 3f));
        }

        if (formation == PrototypeCavalryFormation09F30.Line)
        {
            int columns = Mathf.CeilToInt(men / 4f);
            return new Vector2(Mathf.Max(22f, columns * 1.70f + 3f), 10f);
        }

        int columnRows = Mathf.CeilToInt(men / 4f);
        return new Vector2(8.2f, Mathf.Max(24f, columnRows * 2.15f + 3f));
    }

    private float CalculateFootprintCenterOffsetZ(
        PrototypeCavalryMode09F30 mode,
        PrototypeCavalryFormation09F30 formation,
        bool bridge,
        Vector2 size)
    {
        if (mode == PrototypeCavalryMode09F30.Dismounted)
            return 6.5f;

        // Four-rank Line is centered around the cavalry root.
        if (!bridge && formation == PrototypeCavalryFormation09F30.Line)
            return 0f;

        // Column/bridge slots start at the root/front and extend backwards (negative local Z).
        // The old box used the root as its mathematical center, which put only half of
        // the actual column inside the selection/ghost footprint.
        return -Mathf.Max(0f, size.y - 3f) * 0.5f;
    }

    private void ResizeCollider()
    {
        if (unitCollider == null)
            return;

        if (Mode == PrototypeCavalryMode09F30.Dismounted)
        {
            Vector2 fp = CalculateFootprint(Mode, Formation, false);
            unitCollider.size = new Vector3(fp.x, 2.4f, fp.y);
            unitCollider.center = new Vector3(0f, 1.2f, 6.5f);
            return;
        }

        unitCollider.center = new Vector3(0f, 1.2f, 0f);

        int men = Mathf.Max(1, CurrentStrength);
        if (bridgePhase != BridgePhase.Direct)
        {
            int rows = Mathf.CeilToInt(men / 2f);
            unitCollider.size = new Vector3(5.2f, 3.1f, Mathf.Max(24f, rows * 2.15f + 3f));
        }
        else if (Formation == PrototypeCavalryFormation09F30.Line)
        {
            int columns = Mathf.CeilToInt(men / 4f);
            unitCollider.size = new Vector3(Mathf.Max(22f, columns * 1.70f + 3f), 3.1f, 10f);
        }
        else
        {
            int rows = Mathf.CeilToInt(men / 4f);
            unitCollider.size = new Vector3(8.2f, 3.1f, Mathf.Max(24f, rows * 2.15f + 3f));
        }

        Vector2 mountedSize = new Vector2(unitCollider.size.x, unitCollider.size.z);
        float centerZ = CalculateFootprintCenterOffsetZ(
            Mode, Formation, bridgePhase != BridgePhase.Direct, mountedSize);
        unitCollider.center = new Vector3(0f, 1.2f, centerZ);
    }

    private float BridgeExitClearance()
    {
        int rows = Mathf.CeilToInt(Mathf.Max(1, CurrentStrength) / 2f);
        return Mathf.Max(42f, rows * 2.15f + 18f);
    }

    private static int BankSide(Vector3 point)
    {
        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        float delta = point.x - riverX;
        if (Mathf.Abs(delta) < 5f)
            return 0;
        return delta < 0f ? -1 : 1;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
