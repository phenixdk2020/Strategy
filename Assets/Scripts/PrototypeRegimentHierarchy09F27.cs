using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f27
// Generic two-battalion command layer.
// Replaces the legacy single-Major runtime controller with one shared controller model
// for both Danish battalions. Each Major owns four company-scale infantry units and
// uses the same UI, mission planner, authority rules and dynamic HQ movement.
[DefaultExecutionOrder(720)]
public sealed class PrototypeRegimentHierarchy09F27 : MonoBehaviour
{
    private sealed class CompanyMission
    {
        public MajorOrder09F18 Order;
        public Vector3 Goal;
        public Vector3 Facing;
        public bool Reserve;
        public bool Flank;
        public bool Arrived;
        public float NextAssert;
    }

    private sealed class MissionVisual
    {
        public GameObject Root;
        public LineRenderer Path;
        public LineRenderer Footprint;
        public Vector3 Goal;
        public Vector3 Facing;
        public MajorOrder09F18 Order;
        public bool Arrived;
        public int BattalionIndex;
    }

    private sealed class Battalion
    {
        public string Label;
        public string MajorLabel;
        public readonly List<Regiment> Companies = new List<Regiment>();
        public readonly Dictionary<Regiment, CompanyMission> Missions = new Dictionary<Regiment, CompanyMission>();
        public readonly HashSet<Regiment> ManualDetached = new HashSet<Regiment>();
        public GameObject HqRoot;
        public bool Selected;
        public bool AIEnabled;
        public bool AwaitHigherMission;
        public OfficerAIDoctrine Doctrine = OfficerAIDoctrine.Balanced;
        public MajorOrder09F18 LastOrder = MajorOrder09F18.None;
        public Vector3 LastOrderPoint;
        public bool HasLastOrder;
        public string LastOrderText = "Ingen bataljonsordre";
        public string LastDecisionText = "Reserve: ikke vurderet";
        public bool HasHqGoal;
        public Vector3 HqGoal;
        public float NextThink;
    }

    public static PrototypeRegimentHierarchy09F27 Instance { get; private set; }
    public bool Installed { get; private set; }
    public int BattalionCount => battalions.Count;
    public bool AnyMajorSelected => selectedBattalion >= 0;

    private readonly List<Battalion> battalions = new List<Battalion>();
    private readonly Dictionary<Regiment, MissionVisual> visuals = new Dictionary<Regiment, MissionVisual>();
    private readonly Dictionary<int, List<LineRenderer>> commandLinks = new Dictionary<int, List<LineRenderer>>();

    private Camera cam;
    private int selectedBattalion = -1;
    private MajorOrder09F18 pendingOrder = MajorOrder09F18.None;
    private bool mouseTracked;
    private bool dragging;
    private Vector2 dragStart;
    private Vector2 dragCurrent;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle mutedStyle;
    private GUIStyle buttonStyle;
    private GUIStyle accentStyle;
    private GUIStyle hoverStyle;

    private Material selectionMaterial;
    private Material linkMaterial;
    private Material commandZoneInnerMaterial;
    private Material commandZoneOuterMaterial;
    private readonly List<LineRenderer> selectionRings = new List<LineRenderer>();
    private readonly List<LineRenderer> commandInnerRings = new List<LineRenderer>();
    private readonly List<LineRenderer> commandOuterRings = new List<LineRenderer>();

    private MethodInfo clearPlayerRouteMethod;
    private FieldInfo playerSelectedField;
    private FieldInfo regimentHasDestinationField;

    private const float HudHeight = 90f;
    private const float DragThreshold = 9f;
    private const float CompanySpacing = 60f;
    private const float ReserveDepth = 86f;
    private const float FlankOffset = 126f;
    private const float ExactArrival = 4.5f;
    private const float HqSpeed = 6.2f;
    private const float HqPreferredBehind = 155f;
    private const float HqRelocateThreshold = 230f;
    private const float CommandInner = 320f;
    private const float CommandOuter = 450f;

    private static readonly string[] BattalionACompanyNames =
    {
        "1. Regiment", "5. Regiment", "2. Regiment", "3. Regiment"
    };

    private static readonly string[] BattalionBCompanyNames =
    {
        "5. Kompagni", "6. Kompagni", "7. Kompagni", "8. Kompagni"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeRegimentHierarchy09F27>() == null)
            new GameObject("PrototypeRegimentHierarchy_v000009f27").AddComponent<PrototypeRegimentHierarchy09F27>();
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
        clearPlayerRouteMethod = typeof(PlayerCommander).GetMethod("ClearRoute", flags);
        playerSelectedField = typeof(PlayerCommander).GetField("selected", flags);
        regimentHasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        if (!Installed)
        {
            TryInstall();
            return;
        }

        for (int i = 0; i < battalions.Count; i++)
        {
            UpdateAuthority(i);
            UpdateMissions(i);
            UpdateDynamicHq(i);
            UpdateBattalionAI(i);
        }

        UpdateWorldVisuals();
        HandleWorldInput();
    }

    private void TryInstall()
    {
        PrototypeMajorBattalion09F18 legacy = PrototypeMajorBattalion09F18.Instance;
        if (legacy == null || !legacy.Installed)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (string name in BattalionACompanyNames)
            if (FindRegiment(name) == null)
                return;

        SpawnSecondBattalionCompanies();
        PositionEightCompanies();
        DisableLegacyMajorSystems(legacy);

        Battalion first = new Battalion
        {
            Label = "1. BATALJON",
            MajorLabel = "MAJOR A",
            HqRoot = CreateMajorHq("DK_Battalion_HQ_Major_A", new Vector3(-405f, 0f, -120f))
        };
        foreach (string name in BattalionACompanyNames)
            first.Companies.Add(FindRegiment(name));

        Battalion second = new Battalion
        {
            Label = "2. BATALJON",
            MajorLabel = "MAJOR B",
            HqRoot = CreateMajorHq("DK_Battalion_HQ_Major_B", new Vector3(-405f, 0f, 120f))
        };
        foreach (string name in BattalionBCompanyNames)
            second.Companies.Add(FindRegiment(name));

        battalions.Add(first);
        battalions.Add(second);

        EnsureNewCompaniesHaveOfficerAI(second);
        EnsureWorldVisualInfrastructure();

        Installed = true;

        Debug.Log(
            "HQ-09F27|Installed=True|Battalions=2|Majors=2|Companies=8|" +
            "SharedController=True|SharedHUD=True|MajorA_AI=OFF|MajorB_AI=OFF");
    }

    private void SpawnSecondBattalionCompanies()
    {
        Vector3[] starts =
        {
            new Vector3(-270f, 0f, 30f),
            new Vector3(-270f, 0f, 90f),
            new Vector3(-270f, 0f, 150f),
            new Vector3(-270f, 0f, 210f)
        };

        for (int i = 0; i < BattalionBCompanyNames.Length; i++)
        {
            if (FindRegiment(BattalionBCompanyNames[i]) != null)
                continue;

            Vector3 p = Ground(starts[i]);
            GameObject root = new GameObject(BattalionBCompanyNames[i]);
            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Regiment regiment = root.AddComponent<Regiment>();
            regiment.Initialize(BattalionBCompanyNames[i], BattleTeam.Denmark, 190, false, p);

            OfficerAIController controller = root.AddComponent<OfficerAIController>();
            controller.Configure(false, null);

            Debug.Log("HQ-09F27|SpawnCompany=True|Unit=" + BattalionBCompanyNames[i] +
                      "|Battalion=2|Strength=190");
        }
    }

    private void EnsureNewCompaniesHaveOfficerAI(Battalion battalion)
    {
        foreach (Regiment regiment in battalion.Companies)
        {
            if (regiment == null)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null)
            {
                controller = regiment.gameObject.AddComponent<OfficerAIController>();
                controller.Configure(false, null);
            }
        }
    }

    private void PositionEightCompanies()
    {
        Vector3[] a =
        {
            new Vector3(-270f, 0f, -210f),
            new Vector3(-270f, 0f, -150f),
            new Vector3(-270f, 0f, -90f),
            new Vector3(-270f, 0f, -30f)
        };

        Vector3[] b =
        {
            new Vector3(-270f, 0f, 30f),
            new Vector3(-270f, 0f, 90f),
            new Vector3(-270f, 0f, 150f),
            new Vector3(-270f, 0f, 210f)
        };

        for (int i = 0; i < 4; i++)
        {
            SetPose(FindRegiment(BattalionACompanyNames[i]), a[i]);
            SetPose(FindRegiment(BattalionBCompanyNames[i]), b[i]);
        }
    }

    private static void SetPose(Regiment regiment, Vector3 position)
    {
        if (regiment == null)
            return;

        regiment.transform.position = Ground(position);
        regiment.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        regiment.SetFormation(RegimentFormation.Line);
        regiment.OrderHold();
    }

    private void DisableLegacyMajorSystems(PrototypeMajorBattalion09F18 legacy)
    {
        if (legacy.HqRoot != null)
            legacy.HqRoot.SetActive(false);
        legacy.enabled = false;

        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorUi09F18>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorMissionCommitment09F24>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorCommandZone09F25>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorFormationPlanner09F22>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorSlotDeconfliction09F23>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorIntentAuthority09F20>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorReserveDecision09F19>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorOrderVisuals09F18>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorSelectionHandoff09F20>());
        DisableComponent(UnityEngine.Object.FindAnyObjectByType<PrototypeMajorPointerIsolation09F18>());

        PrototypeMajorHQ09F15 oldHq = PrototypeMajorHQ09F15.Instance;
        if (oldHq != null)
            oldHq.enabled = false;

        PrototypeMajorBattalion09F17 old17 = PrototypeMajorBattalion09F17.Instance;
        if (old17 != null)
            old17.enabled = false;
    }

    private static void DisableComponent(MonoBehaviour behaviour)
    {
        if (behaviour != null)
            behaviour.enabled = false;
    }

    private void UpdateAuthority(int battalionIndex)
    {
        Battalion battalion = battalions[battalionIndex];
        List<Regiment> detach = null;

        foreach (KeyValuePair<Regiment, CompanyMission> pair in battalion.Missions)
        {
            Regiment regiment = pair.Key;
            if (regiment == null)
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
            {
                if (detach == null)
                    detach = new List<Regiment>();
                detach.Add(regiment);
            }
        }

        if (detach == null)
            return;

        foreach (Regiment regiment in detach)
        {
            battalion.Missions.Remove(regiment);
            battalion.ManualDetached.Add(regiment);
            ClearMissionVisual(regiment);
            Debug.Log("AI-AUTHORITY-09F27|Unit=" + regiment.RegimentName +
                      "|Battalion=" + (battalionIndex + 1) +
                      "|Authority=PLAYER_OR_LOCAL_CAPTAIN|ParentMissionReleased=True");
        }
    }

    private void UpdateMissions(int battalionIndex)
    {
        Battalion battalion = battalions[battalionIndex];

        // F30O: a higher-HQ AI cascade is an armed/waiting state. Even stale
        // mission records from an earlier order must not move companies until a
        // fresh parent mission is committed.
        if (battalion.AwaitHigherMission)
            return;

        if (battalion.Missions.Count == 0)
            return;

        List<Regiment> completedDestroyed = null;

        foreach (KeyValuePair<Regiment, CompanyMission> pair in battalion.Missions)
        {
            Regiment regiment = pair.Key;
            CompanyMission mission = pair.Value;

            if (regiment == null || regiment.IsRouted || regiment.CurrentStrength <= 0)
            {
                if (completedDestroyed == null)
                    completedDestroyed = new List<Regiment>();
                completedDestroyed.Add(regiment);
                continue;
            }

            if (PrototypeUnderFireReaction09F26.IsReacting(regiment))
                continue;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null && !controller.AIEnabled)
                continue;

            float distance = PlanarDistance(regiment.transform.position, mission.Goal);

            if (distance > ExactArrival)
            {
                mission.Arrived = false;

                if (controller != null)
                    controller.enabled = false;

                if (!HasDestination(regiment) || Time.time >= mission.NextAssert)
                {
                    mission.NextAssert = Time.time + 1.1f;
                    regiment.SetFormation(RegimentFormation.Line);
                    regiment.OrderMove(mission.Goal);
                }
            }
            else
            {
                if (!mission.Arrived)
                {
                    mission.Arrived = true;
                    regiment.OrderHold();
                    FaceDirection(regiment, mission.Facing, true);

                    if (controller != null)
                    {
                        controller.enabled = true;
                        if (mission.Order == MajorOrder09F18.AttackHere)
                        {
                            controller.SetDoctrine(OfficerAIDoctrine.Offensive);
                            Regiment target = FindNearestEnemy(regiment.transform.position, 220f);
                            if (target != null)
                                controller.SetAttackMission(target);
                            else
                                controller.SetHoldMission();
                        }
                        else
                        {
                            controller.SetHoldMission();
                        }
                    }

                    MarkMissionVisualArrived(regiment);
                    Debug.Log("HQ-09F27|Arrived=True|Battalion=" + (battalionIndex + 1) +
                              "|Unit=" + regiment.RegimentName + "|Order=" + mission.Order);
                }
                else
                {
                    FaceDirection(regiment, mission.Facing, false);
                }
            }
        }

        if (completedDestroyed != null)
        {
            foreach (Regiment regiment in completedDestroyed)
            {
                battalion.Missions.Remove(regiment);
                ClearMissionVisual(regiment);
            }
        }
    }

    private void UpdateDynamicHq(int battalionIndex)
    {
        Battalion battalion = battalions[battalionIndex];
        if (battalion.HqRoot == null)
            return;

        if (battalion.HasHqGoal)
        {
            MoveHqToward(battalion, battalion.HqGoal);
            return;
        }

        if (!battalion.AIEnabled || battalion.AwaitHigherMission ||
            !battalion.HasLastOrder || battalion.LastOrder == MajorOrder09F18.HoldPosition)
            return;

        Vector3 center = GetBattalionCenter(battalionIndex);
        Vector3 forward = battalion.LastOrderPoint - center;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battalion.HqRoot.transform.forward;
        forward.Normalize();

        Vector3 desired = Ground(center - forward * HqPreferredBehind);
        if (PlanarDistance(battalion.HqRoot.transform.position, desired) < HqRelocateThreshold)
            return;

        if (!IsSafeHqRelocation(battalion.HqRoot.transform.position, desired))
            return;

        battalion.HqGoal = desired;
        battalion.HasHqGoal = true;

        Debug.Log("HQ-ZONE-09F27|Battalion=" + (battalionIndex + 1) +
                  "|MajorAdvance=True|Goal=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0"));
    }

    private void MoveHqToward(Battalion battalion, Vector3 goal)
    {
        Vector3 current = battalion.HqRoot.transform.position;
        Vector3 delta = goal - current;
        delta.y = 0f;

        if (delta.magnitude <= 2.5f)
        {
            battalion.HqRoot.transform.position = Ground(goal);
            battalion.HasHqGoal = false;
            return;
        }

        Vector3 step = delta.normalized * HqSpeed * Time.deltaTime;
        if (step.magnitude > delta.magnitude)
            step = delta;

        Vector3 next = Ground(current + step);
        battalion.HqRoot.transform.position = next;

        Quaternion desired = Quaternion.LookRotation(delta.normalized, Vector3.up);
        battalion.HqRoot.transform.rotation = Quaternion.Slerp(battalion.HqRoot.transform.rotation, desired, 3f * Time.deltaTime);
    }

    private void UpdateBattalionAI(int battalionIndex)
    {
        Battalion battalion = battalions[battalionIndex];
        if (!battalion.AIEnabled || Time.time < battalion.NextThink)
            return;

        battalion.NextThink = Time.time + 6f;

        // Higher-HQ cascade only arms this Major/Battalion. No automatic default
        // mission may be created before a parent order is actually committed.
        if (battalion.AwaitHigherMission)
            return;

        if (!battalion.HasLastOrder)
            IssueBattalionOrder(battalionIndex, MajorOrder09F18.DefendHere, GetBattalionCenter(battalionIndex), true);
    }

    public void IssueBattalionOrderFromRegiment(
        int battalionIndex,
        MajorOrder09F18 order,
        Vector3 point,
        OfficerAIDoctrine doctrine,
        Vector3? explicitFacing = null)
    {
        if (!ValidBattalion(battalionIndex))
            return;

        Battalion battalion = battalions[battalionIndex];
        battalion.AIEnabled = true;
        battalion.AwaitHigherMission = false;
        battalion.Doctrine = doctrine;
        IssueBattalionOrder(battalionIndex, order, point, true, explicitFacing);
    }

    public void IssueBattalionOrder(
        int battalionIndex,
        MajorOrder09F18 order,
        Vector3 point,
        bool fromHigherCommand = false,
        Vector3? explicitFacing = null)
    {
        if (!ValidBattalion(battalionIndex))
            return;

        Battalion battalion = battalions[battalionIndex];

        if (order == MajorOrder09F18.HoldPosition)
        {
            battalion.Missions.Clear();
            foreach (Regiment regiment in battalion.Companies)
            {
                if (regiment == null || regiment.IsRouted)
                    continue;

                ClearPlayerRoute(regiment);
                battalion.ManualDetached.Remove(regiment);

                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null)
                {
                    if (!controller.AIEnabled)
                        controller.SetAIEnabled(true);
                    controller.enabled = true;
                    controller.SetHoldMission();
                }
                else
                {
                    regiment.OrderHold();
                }

                ClearMissionVisual(regiment);
            }

            battalion.LastOrder = order;
            battalion.LastOrderPoint = GetBattalionCenter(battalionIndex);
            battalion.HasLastOrder = true;
            battalion.LastOrderText = "HOLD | bataljonen fastholder position";
            battalion.LastDecisionText = "Reserve: eksisterende placering";
            return;
        }

        List<Regiment> active = new List<Regiment>();
        foreach (Regiment regiment in battalion.Companies)
            if (regiment != null && !regiment.IsRouted && regiment.CurrentStrength > 0)
                active.Add(regiment);

        if (active.Count == 0)
            return;

        List<Regiment> known = KnownEnemies(point, 700f);
        Regiment threat = NearestEnemyToPoint(point, known);

        Vector3 forward = Vector3.zero;

        if (explicitFacing.HasValue)
            forward = Flat(explicitFacing.Value);

        if (forward.sqrMagnitude < 0.01f)
            forward = threat != null
                ? Flat(threat.transform.position - point)
                : Flat(point - battalion.HqRoot.transform.position);

        if (order == MajorOrder09F18.WithdrawHere &&
            !explicitFacing.HasValue &&
            threat != null)
        {
            forward = Flat(threat.transform.position - point);
        }

        if (forward.sqrMagnitude < 0.01f)
            forward = Flat(battalion.HqRoot.transform.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = Ground(point);

        bool useReserve = battalion.AIEnabled && active.Count >= 4 &&
            (order == MajorOrder09F18.AttackHere || order == MajorOrder09F18.DefendHere) && known.Count <= active.Count;

        Regiment reserve = useReserve ? BestReserve(active) : null;
        bool flank = reserve != null && order == MajorOrder09F18.AttackHere &&
            battalion.Doctrine != OfficerAIDoctrine.Defensive && known.Count <= 2;
        int flankSign = battalionIndex == 0 ? -1 : 1;

        battalion.Missions.Clear();
        battalion.ManualDetached.Clear();

        List<Regiment> front = new List<Regiment>(active);
        if (reserve != null)
            front.Remove(reserve);

        List<Vector3> desiredSlots = new List<Vector3>();
        List<Vector3> reservedSlots = new List<Vector3>();
        float middle = (front.Count - 1) * 0.5f;

        for (int i = 0; i < front.Count; i++)
        {
            Vector3 desired = center + lateral * ((i - middle) * CompanySpacing);
            Vector3 legal = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
                desired, forward, lateral, reservedSlots, out bool moved, out bool depthFallback);
            desiredSlots.Add(legal);
            reservedSlots.Add(legal);

            if (moved)
            {
                Debug.Log("HQ-SLOT-09F27|Battalion=" + (battalionIndex + 1) +
                          "|Moved=True|DepthFallback=" + depthFallback +
                          "|Slot=" + legal.x.ToString("0.0") + "," + legal.z.ToString("0.0"));
            }
        }

        Dictionary<Regiment, Vector3> assignment = AssignMinimumDistance(front, desiredSlots);
        foreach (KeyValuePair<Regiment, Vector3> pair in assignment)
            AssignMission(battalionIndex, pair.Key, NewMission(order, pair.Value, forward, false, false));

        if (reserve != null)
        {
            Vector3 desiredReserve = flank
                ? center - forward * 24f + lateral * (flankSign * FlankOffset)
                : center - forward * ReserveDepth;

            Vector3 legalReserve = PrototypeFormationSlotSafety09F23.ResolveOnFormationLine(
                desiredReserve, forward, lateral, reservedSlots, out bool moved, out bool depthFallback);

            AssignMission(battalionIndex, reserve, NewMission(order, legalReserve, forward, true, flank));
        }

        battalion.LastOrder = order;
        battalion.LastOrderPoint = point;
        battalion.HasLastOrder = true;
        battalion.LastOrderText = Label(order) + " | kendte fjender: " + known.Count +
            (fromHigherCommand ? " | FRA REGIMENT" : string.Empty);
        battalion.LastDecisionText = reserve == null
            ? "Reserve: ingen - hele bataljonen bruges"
            : "Reserve: " + reserve.RegimentName + (flank ? " | flanke" : " | bag front");

        Debug.Log("HQ-ORDER-09F30P|Battalion=" + (battalionIndex + 1) +
                  "|Major=" + battalion.MajorLabel + "|Order=" + order +
                  "|FromRegiment=" + fromHigherCommand +
                  "|ExplicitFacing=" + explicitFacing.HasValue +
                  "|Facing=" + forward.x.ToString("0.00") + "," + forward.z.ToString("0.00") +
                  "|Front=" + front.Count +
                  "|Reserve=" + (reserve != null ? reserve.RegimentName : "NONE") +
                  "|Flank=" + flank + "|CompanyAI=ON|SharedController=True");
    }

    private static CompanyMission NewMission(MajorOrder09F18 order, Vector3 goal, Vector3 facing, bool reserve, bool flank)
    {
        return new CompanyMission
        {
            Order = order,
            Goal = Ground(goal),
            Facing = Flat(facing),
            Reserve = reserve,
            Flank = flank,
            Arrived = false,
            NextAssert = 0f
        };
    }

    private void AssignMission(int battalionIndex, Regiment regiment, CompanyMission mission)
    {
        Battalion battalion = battalions[battalionIndex];
        ClearPlayerRoute(regiment);
        battalion.ManualDetached.Remove(regiment);

        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        if (controller == null)
        {
            controller = regiment.gameObject.AddComponent<OfficerAIController>();
            controller.Configure(false, null);
        }

        if (!controller.AIEnabled)
            controller.SetAIEnabled(true);

        switch (mission.Order)
        {
            case MajorOrder09F18.AttackHere:
                controller.SetDoctrine(OfficerAIDoctrine.Offensive);
                regiment.SetFirePolicy(RegimentFirePolicy.LongRange);
                break;
            case MajorOrder09F18.DefendHere:
                controller.SetDoctrine(OfficerAIDoctrine.Defensive);
                regiment.SetFirePolicy(RegimentFirePolicy.MediumRange);
                break;
            default:
                controller.SetDoctrine(OfficerAIDoctrine.Balanced);
                break;
        }

        controller.enabled = false;
        regiment.OrderHold();
        regiment.OrderMove(mission.Goal);
        battalion.Missions[regiment] = mission;
        SetMissionVisual(regiment, mission, battalionIndex);
    }

    public void ToggleBattalionAI(int battalionIndex)
    {
        if (!ValidBattalion(battalionIndex))
            return;
        SetBattalionAIEnabled(battalionIndex, !battalions[battalionIndex].AIEnabled, "HUD_TOGGLE");
    }

    public void SetBattalionAIEnabled(int battalionIndex, bool enabled, string reason = "HIGHER_COMMAND")
    {
        if (!ValidBattalion(battalionIndex))
            return;

        Battalion battalion = battalions[battalionIndex];
        battalion.AIEnabled = enabled;

        bool higherCascade =
            !string.IsNullOrEmpty(reason) &&
            reason.EndsWith("_CASCADE", System.StringComparison.Ordinal);

        battalion.AwaitHigherMission = enabled && higherCascade;
        if (!enabled)
            battalion.AwaitHigherMission = false;

        battalion.NextThink = Time.time + 0.2f;

        foreach (Regiment regiment in battalion.Companies)
        {
            if (regiment == null)
                continue;
            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller != null)
            {
                controller.SetAIEnabled(enabled);
                if (enabled && battalion.AwaitHigherMission)
                    controller.SetHoldMission();
            }
        }

        Debug.Log("AI-AUTHORITY-09F30O|Unit=" + battalion.MajorLabel +
                  "|Battalion=" + (battalionIndex + 1) +
                  "|AI=" + (enabled ? "ON" : "OFF") +
                  "|AwaitHigherMission=" + battalion.AwaitHigherMission +
                  "|Reason=" + reason);
    }

    public bool HasActiveMissionExecutors(MajorOrder09F18 order)
    {
        for (int b = 0; b < battalions.Count; b++)
            if (IsBattalionOrderActive(b, order))
                return true;

        return false;
    }

    public bool IsBattalionOrderActive(int battalionIndex, MajorOrder09F18 order)
    {
        if (!ValidBattalion(battalionIndex))
            return false;

        Battalion battalion = battalions[battalionIndex];
        if (battalion == null || !battalion.HasLastOrder || battalion.LastOrder != order)
            return false;

        // Standing missions remain active after arrival until explicitly replaced.
        if (order == MajorOrder09F18.HoldPosition ||
            order == MajorOrder09F18.DefendHere)
        {
            for (int i = 0; i < battalion.Companies.Count; i++)
            {
                Regiment unit = battalion.Companies[i];
                if (unit != null && !unit.IsRouted && unit.CurrentStrength > 0)
                    return true;
            }
            return false;
        }

        foreach (KeyValuePair<Regiment, CompanyMission> pair in battalion.Missions)
        {
            Regiment unit = pair.Key;
            CompanyMission mission = pair.Value;
            if (unit == null || mission == null || unit.IsRouted || unit.CurrentStrength <= 0)
                continue;
            if (mission.Order != order)
                continue;

            if (!mission.Arrived)
                return true;

            // Attack stays active while companies are still in local combat even
            // after they have reached their planned attack slots.
            if (order == MajorOrder09F18.AttackHere && IsCompanyStillEngaged(unit))
                return true;
        }

        return false;
    }

    private static bool IsCompanyStillEngaged(Regiment unit)
    {
        if (unit == null || unit.IsRouted || unit.CurrentStrength <= 0)
            return false;

        if (PrototypeAttackContact09F29G.GetLocalContactTarget(unit) != null)
            return true;

        if (PrototypeUnderFireReaction09F26.IsReacting(unit) || unit.HasHitFeedback)
            return true;

        return false;
    }

    public void SetBattalionDoctrine(int battalionIndex, OfficerAIDoctrine doctrine)
    {
        if (ValidBattalion(battalionIndex))
            battalions[battalionIndex].Doctrine = doctrine;
    }

    public void MoveMajor(int battalionIndex, Vector3 point)
    {
        if (!ValidBattalion(battalionIndex))
            return;

        Battalion battalion = battalions[battalionIndex];
        battalion.AIEnabled = false;
        Vector3 goal = Ground(point);

        if (!IsSafeHqRelocation(battalion.HqRoot.transform.position, goal))
        {
            Debug.LogWarning("HQ-09F27|MoveMajor=False|Battalion=" + (battalionIndex + 1) + "|Reason=BlockedOrRiver");
            return;
        }

        battalion.HqGoal = goal;
        battalion.HasHqGoal = true;
        Debug.Log("AI-AUTHORITY-09F27|Unit=" + battalion.MajorLabel + "|Authority=PLAYER|AI=OFF|MoveHQ=True");
    }

    public Vector3 GetBattalionCenter(int battalionIndex)
    {
        if (!ValidBattalion(battalionIndex))
            return Vector3.zero;

        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in battalions[battalionIndex].Companies)
        {
            if (regiment == null || regiment.IsRouted)
                continue;
            total += regiment.transform.position;
            count++;
        }

        return count > 0 ? total / count : battalions[battalionIndex].HqRoot.transform.position;
    }

    public GameObject GetMajorHq(int battalionIndex)
    {
        return ValidBattalion(battalionIndex) ? battalions[battalionIndex].HqRoot : null;
    }

    public IReadOnlyList<Regiment> GetCompanies(int battalionIndex)
    {
        return ValidBattalion(battalionIndex) ? battalions[battalionIndex].Companies : null;
    }

    public bool GetBattalionAIEnabled(int battalionIndex)
    {
        return ValidBattalion(battalionIndex) && battalions[battalionIndex].AIEnabled;
    }

    public OfficerAIDoctrine GetBattalionDoctrine(int battalionIndex)
    {
        return ValidBattalion(battalionIndex) ? battalions[battalionIndex].Doctrine : OfficerAIDoctrine.Balanced;
    }

    public void ClearMajorSelection()
    {
        selectedBattalion = -1;
        foreach (Battalion battalion in battalions)
            battalion.Selected = false;
        pendingOrder = MajorOrder09F18.None;
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (selectedBattalion < 0)
            return false;
        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight).Contains(gui);
    }

    private void HandleWorldInput()
    {
        if (cam == null)
            return;

        bool overPanel = IsPointerOverControls(Input.mousePosition);

        if (selectedBattalion >= 0 && pendingOrder != MajorOrder09F18.None && Input.GetMouseButtonDown(0) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
            {
                IssueBattalionOrder(selectedBattalion, pendingOrder, point, false);
                pendingOrder = MajorOrder09F18.None;
            }
            mouseTracked = false;
            dragging = false;
            return;
        }

        if (selectedBattalion >= 0 && pendingOrder == MajorOrder09F18.None && Input.GetMouseButtonDown(1) && !overPanel)
        {
            if (TryGetGround(Input.mousePosition, out Vector3 point))
                MoveMajor(selectedBattalion, point);
        }

        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            mouseTracked = true;
            dragging = false;
            dragStart = Input.mousePosition;
            dragCurrent = dragStart;
        }

        if (mouseTracked && Input.GetMouseButton(0))
        {
            dragCurrent = Input.mousePosition;
            if (!dragging && Vector2.Distance(dragStart, dragCurrent) >= DragThreshold)
                dragging = true;
        }

        if (!mouseTracked || !Input.GetMouseButtonUp(0))
            return;

        dragCurrent = Input.mousePosition;
        int hitBattalion = -1;

        if (dragging)
        {
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(dragStart.x, dragCurrent.x), Mathf.Min(dragStart.y, dragCurrent.y),
                Mathf.Max(dragStart.x, dragCurrent.x), Mathf.Max(dragStart.y, dragCurrent.y));

            for (int i = 0; i < battalions.Count; i++)
            {
                Vector3 screen = cam.WorldToScreenPoint(battalions[i].HqRoot.transform.position);
                if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y), true))
                {
                    hitBattalion = i;
                    break;
                }
            }
        }
        else
        {
            hitBattalion = RayHitMajor(Input.mousePosition);
        }

        if (hitBattalion >= 0)
            SelectMajor(hitBattalion);
        else if (selectedBattalion >= 0 && pendingOrder == MajorOrder09F18.None)
            ClearMajorSelection();

        mouseTracked = false;
        dragging = false;
    }

    private void SelectMajor(int battalionIndex)
    {
        if (!ValidBattalion(battalionIndex))
            return;

        ClearCompanySelection();
        selectedBattalion = battalionIndex;
        for (int i = 0; i < battalions.Count; i++)
            battalions[i].Selected = i == battalionIndex;
    }

    private void ClearCompanySelection()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
            {
                foreach (Regiment regiment in selected)
                    if (regiment != null)
                        regiment.SetSelected(false);
                selected.Clear();
            }
        }
    }

    private int RayHitMajor(Vector3 mousePosition)
    {
        if (cam == null)
            return -1;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            for (int i = 0; i < battalions.Count; i++)
            {
                GameObject hq = battalions[i].HqRoot;
                if (hq == null)
                    continue;

                Transform t = hit.collider.transform;
                if (t == hq.transform || t.IsChildOf(hq.transform))
                    return i;
            }
        }

        return -1;
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;

            point = Ground(hit.point);
            return true;
        }

        return false;
    }

    private void EnsureWorldVisualInfrastructure()
    {
        selectionMaterial = CreateUnlit(new Color(1f, 0.80f, 0.18f, 0.95f), "HQ27Selection");
        linkMaterial = CreateUnlit(new Color(0.82f, 0.69f, 0.29f, 0.88f), "HQ27CommandLink");
        commandZoneInnerMaterial = CreateUnlit(new Color(0.45f, 0.75f, 0.35f, 0.50f), "HQ27CommandInner");
        commandZoneOuterMaterial = CreateUnlit(new Color(0.86f, 0.62f, 0.20f, 0.42f), "HQ27CommandOuter");

        for (int i = 0; i < battalions.Count; i++)
        {
            selectionRings.Add(CreateRing("HQ27Selection_" + i, 0.25f, selectionMaterial));
            commandInnerRings.Add(CreateRing("HQ27CommandInner_" + i, 0.11f, commandZoneInnerMaterial));
            commandOuterRings.Add(CreateRing("HQ27CommandOuter_" + i, 0.11f, commandZoneOuterMaterial));

            List<LineRenderer> lines = new List<LineRenderer>();
            foreach (Regiment regiment in battalions[i].Companies)
            {
                GameObject root = new GameObject("HQ27Link_" + i + "_" + regiment.RegimentName);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthMultiplier = 0.14f;
                line.sharedMaterial = linkMaterial;
                line.enabled = false;
                lines.Add(line);
            }
            commandLinks[i] = lines;
        }
    }

    private void UpdateWorldVisuals()
    {
        for (int i = 0; i < battalions.Count; i++)
        {
            Battalion battalion = battalions[i];
            bool selected = i == selectedBattalion;

            LineRenderer selection = selectionRings[i];
            selection.enabled = selected;
            if (selected)
                DrawCircle(selection, battalion.HqRoot.transform.position, 7f, 0.35f);

            LineRenderer inner = commandInnerRings[i];
            LineRenderer outer = commandOuterRings[i];
            inner.enabled = selected;
            outer.enabled = selected;
            if (selected)
            {
                DrawCircle(inner, battalion.HqRoot.transform.position, CommandInner, 0.22f);
                DrawCircle(outer, battalion.HqRoot.transform.position, CommandOuter, 0.24f);
            }

            List<LineRenderer> links = commandLinks[i];
            for (int c = 0; c < links.Count && c < battalion.Companies.Count; c++)
            {
                LineRenderer line = links[c];
                Regiment regiment = battalion.Companies[c];
                line.enabled = selected && regiment != null;
                if (!line.enabled)
                    continue;

                line.SetPosition(0, GroundWithOffset(battalion.HqRoot.transform.position, 0.45f));
                line.SetPosition(1, GroundWithOffset(regiment.transform.position, 0.45f));
            }
        }

        foreach (KeyValuePair<Regiment, MissionVisual> pair in visuals)
        {
            Regiment regiment = pair.Key;
            MissionVisual visual = pair.Value;
            if (regiment == null || visual == null)
                continue;

            bool show = visual.BattalionIndex == selectedBattalion || regiment.IsSelected;
            visual.Footprint.enabled = show;
            visual.Path.enabled = show && !visual.Arrived;

            if (!show)
                continue;

            DrawFootprint(visual.Footprint, visual.Goal, visual.Facing);
            if (!visual.Arrived)
            {
                visual.Path.positionCount = 2;
                visual.Path.SetPosition(0, GroundWithOffset(regiment.transform.position, 0.46f));
                visual.Path.SetPosition(1, GroundWithOffset(visual.Goal, 0.46f));
            }
        }
    }

    private void SetMissionVisual(Regiment regiment, CompanyMission mission, int battalionIndex)
    {
        if (!visuals.TryGetValue(regiment, out MissionVisual visual))
        {
            GameObject root = new GameObject("HQ27Mission_" + regiment.RegimentName);
            visual = new MissionVisual
            {
                Root = root,
                Path = CreateLine(root.transform, "Path", 0.12f, ColorFor(mission.Order)),
                Footprint = CreateLine(root.transform, "Footprint", 0.23f, ColorFor(mission.Order))
            };
            visuals[regiment] = visual;
        }

        visual.Goal = mission.Goal;
        visual.Facing = mission.Facing;
        visual.Order = mission.Order;
        visual.Arrived = false;
        visual.BattalionIndex = battalionIndex;
        SetLineColor(visual.Path, ColorFor(mission.Order));
        SetLineColor(visual.Footprint, ColorFor(mission.Order));
    }

    private void MarkMissionVisualArrived(Regiment regiment)
    {
        if (regiment != null && visuals.TryGetValue(regiment, out MissionVisual visual))
            visual.Arrived = true;
    }

    private void ClearMissionVisual(Regiment regiment)
    {
        if (regiment == null || !visuals.TryGetValue(regiment, out MissionVisual visual))
            return;
        if (visual.Root != null)
            Destroy(visual.Root);
        visuals.Remove(regiment);
    }

    private static LineRenderer CreateLine(Transform parent, string name, float width, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.sharedMaterial = CreateUnlit(color, "HQ27_" + name);
        return line;
    }

    private LineRenderer CreateRing(string name, float width, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = width;
        line.positionCount = 64;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static void DrawCircle(LineRenderer line, Vector3 center, float radius, float yOffset)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static void DrawFootprint(LineRenderer line, Vector3 center, Vector3 facing)
    {
        facing = Flat(facing);
        Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
        Vector3[] points =
        {
            center - right * 24f - facing * 3.2f,
            center + right * 24f - facing * 3.2f,
            center + right * 24f + facing * 3.2f,
            center - right * 24f + facing * 3.2f,
            center - right * 24f - facing * 3.2f
        };

        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            line.SetPosition(i, GroundWithOffset(points[i], 0.48f));
    }

    private GameObject CreateMajorHq(string name, Vector3 position)
    {
        GameObject hq = new GameObject(name);
        hq.transform.position = Ground(position);
        hq.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        BoxCollider box = hq.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.8f, 0f);
        box.size = new Vector3(12f, 4.5f, 10f);

        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.30f, 0.18f, 0.10f), name + "_Horse");
        Material officer = PrototypeBootstrap.CreateSharedMaterial(new Color(0.08f, 0.16f, 0.28f), name + "_Officer");
        Material leather = PrototypeBootstrap.CreateSharedMaterial(new Color(0.12f, 0.075f, 0.045f), name + "_Leather");
        Material brass = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.60f, 0.24f), name + "_Brass");

        CreateHorse(hq.transform, new Vector3(0f, 0f, 0.6f), 0f, true, horse, officer, leather, brass);
        CreateHorse(hq.transform, new Vector3(-3f, 0f, -1.8f), 8f, false, horse, officer, leather, brass);
        CreateHorse(hq.transform, new Vector3(3f, 0f, -1.8f), -8f, false, horse, officer, leather, brass);
        return hq;
    }

    private static void CreateHorse(Transform parent, Vector3 localPosition, float yaw, bool rider,
        Material horse, Material officer, Material leather, Material brass)
    {
        GameObject root = new GameObject(rider ? "MajorHorse" : "HQHorse");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreatePart(root.transform, PrimitiveType.Cube, "HorseBody", new Vector3(0f, 1.05f, 0f), new Vector3(0.72f, 0.68f, 1.70f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseNeck", new Vector3(0f, 1.52f, 0.72f), new Vector3(0.42f, 0.92f, 0.42f), Quaternion.Euler(-22f, 0f, 0f), horse);
        CreatePart(root.transform, PrimitiveType.Cube, "HorseHead", new Vector3(0f, 1.92f, 1.10f), new Vector3(0.42f, 0.46f, 0.68f), Quaternion.identity, horse);
        CreatePart(root.transform, PrimitiveType.Cube, "Saddle", new Vector3(0f, 1.46f, -0.05f), new Vector3(0.78f, 0.18f, 0.78f), Quaternion.identity, leather);

        if (!rider)
            return;

        CreatePart(root.transform, PrimitiveType.Capsule, "MajorBody", new Vector3(0f, 2.20f, -0.05f), new Vector3(0.34f, 0.48f, 0.34f), Quaternion.identity, officer);
        CreatePart(root.transform, PrimitiveType.Sphere, "MajorHead", new Vector3(0f, 2.92f, -0.02f), new Vector3(0.28f, 0.32f, 0.28f), Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cylinder, "MajorCap", new Vector3(0f, 3.18f, -0.02f), new Vector3(0.30f, 0.08f, 0.30f), Quaternion.identity, leather);
        CreatePart(root.transform, PrimitiveType.Cube, "MajorSash", new Vector3(0.18f, 2.30f, -0.24f), new Vector3(0.08f, 0.68f, 0.06f), Quaternion.Euler(0f, 0f, 24f), brass);
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, string name,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        return part;
    }

    private void ClearPlayerRoute(Regiment regiment)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && clearPlayerRouteMethod != null && regiment != null)
            clearPlayerRouteMethod.Invoke(commander, new object[] { regiment });
    }

    private bool HasDestination(Regiment regiment)
    {
        if (regiment == null || regimentHasDestinationField == null)
            return false;
        object value = regimentHasDestinationField.GetValue(regiment);
        return value is bool && (bool)value;
    }

    private static Dictionary<Regiment, Vector3> AssignMinimumDistance(List<Regiment> units, List<Vector3> slots)
    {
        Dictionary<Regiment, Vector3> result = new Dictionary<Regiment, Vector3>();
        if (units == null || slots == null || units.Count == 0 || units.Count != slots.Count)
            return result;

        int count = units.Count;
        int[] current = new int[count];
        bool[] used = new bool[count];
        int[] best = new int[count];
        float bestCost = float.MaxValue;
        AssignRecursive(units, slots, 0, current, used, ref bestCost, best);

        for (int i = 0; i < count; i++)
            result[units[i]] = slots[best[i]];
        return result;
    }

    private static void AssignRecursive(List<Regiment> units, List<Vector3> slots, int depth,
        int[] current, bool[] used, ref float bestCost, int[] best)
    {
        if (depth >= units.Count)
        {
            float cost = 0f;
            for (int i = 0; i < units.Count; i++)
                cost += PlanarDistance(units[i].transform.position, slots[current[i]]);
            if (cost < bestCost)
            {
                bestCost = cost;
                for (int i = 0; i < current.Length; i++)
                    best[i] = current[i];
            }
            return;
        }

        for (int slot = 0; slot < slots.Count; slot++)
        {
            if (used[slot])
                continue;
            used[slot] = true;
            current[depth] = slot;
            AssignRecursive(units, slots, depth + 1, current, used, ref bestCost, best);
            used[slot] = false;
        }
    }

    private List<Regiment> KnownEnemies(Vector3 point, float range)
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.Team == BattleTeam.Denmark || regiment.IsRouted || regiment.CurrentStrength <= 0)
                continue;
            if (PlanarDistance(point, regiment.transform.position) <= range)
                result.Add(regiment);
        }
        return result;
    }

    private static Regiment NearestEnemyToPoint(Vector3 point, List<Regiment> enemies)
    {
        Regiment nearest = null;
        float best = float.MaxValue;
        foreach (Regiment regiment in enemies)
        {
            float distance = PlanarDistance(point, regiment.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = regiment;
            }
        }
        return nearest;
    }

    private Regiment FindNearestEnemy(Vector3 point, float range)
    {
        return NearestEnemyToPoint(point, KnownEnemies(point, range));
    }

    private static Regiment BestReserve(List<Regiment> units)
    {
        Regiment best = null;
        float bestScore = float.MinValue;
        foreach (Regiment regiment in units)
        {
            if (regiment == null)
                continue;
            float strength = regiment.InitialStrength > 0 ? regiment.CurrentStrength / (float)regiment.InitialStrength : 0f;
            float score = strength * 60f + regiment.Cohesion * 0.25f + regiment.Morale * 0.15f;
            if (score > bestScore)
            {
                bestScore = score;
                best = regiment;
            }
        }
        return best;
    }

    private static void FaceDirection(Regiment regiment, Vector3 direction, bool immediate)
    {
        if (regiment == null)
            return;
        direction = Flat(direction);
        Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
        regiment.transform.rotation = immediate
            ? desired
            : Quaternion.Slerp(regiment.transform.rotation, desired, 2.5f * Time.deltaTime);
    }

    private static bool IsSafeHqRelocation(Vector3 current, Vector3 goal)
    {
        if (!PrototypeFormationSlotSafety09F23.IsLegalFormationEndpoint(goal, goal - current))
            return false;
        int currentSide = BankSide(current);
        int goalSide = BankSide(goal);
        return !(currentSide != 0 && goalSide != 0 && currentSide != goalSide);
    }

    private static int BankSide(Vector3 point)
    {
        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        float delta = point.x - riverX;
        if (Mathf.Abs(delta) < 4.0f)
            return 0;
        return delta < 0f ? -1 : 1;
    }

    private bool ValidBattalion(int index)
    {
        return index >= 0 && index < battalions.Count;
    }

    private Regiment FindRegiment(string name)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;
        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.RegimentName == name)
                return regiment;
        return null;
    }

    private static Material CreateUnlit(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private static void SetLineColor(LineRenderer line, Color color)
    {
        if (line != null && line.sharedMaterial != null)
            line.sharedMaterial.color = color;
    }

    private static Color ColorFor(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder09F18.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder09F18.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    private static string Label(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return "ANGRIB HER";
            case MajorOrder09F18.DefendHere: return "FORSVAR HER";
            case MajorOrder09F18.WithdrawHere: return "TILBAGETRÆK";
            case MajorOrder09F18.AdvanceHere: return "RYK FREM";
            case MajorOrder09F18.AssembleHere: return "SAML";
            case MajorOrder09F18.HoldPosition: return "HOLD";
            default: return "INGEN";
        }
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static Vector3 GroundWithOffset(Vector3 point, float offset)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + offset;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = PrototypeUiTheme09F15.Panel(9);
        headerStyle = PrototypeUiTheme09F15.Header(11);
        labelStyle = PrototypeUiTheme09F15.Label(8);
        mutedStyle = PrototypeUiTheme09F15.MutedLabel(8);
        buttonStyle = PrototypeUiTheme09F15.Button(9);
        accentStyle = PrototypeUiTheme09F15.AccentBox(9);
        hoverStyle = PrototypeUiTheme09F15.Panel(9);
        hoverStyle.alignment = TextAnchor.UpperLeft;
    }

    private void OnGUI()
    {
        if (!Installed)
            return;

        EnsureStyles();

        if (cam != null && selectedBattalion < 0)
        {
            int hoverIndex = RayHitMajor(Input.mousePosition);
            if (hoverIndex >= 0)
            {
                Battalion hoverBattalion = battalions[hoverIndex];
                Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                Rect hover = new Rect(
                    Mathf.Clamp(p.x + 15f, 8f, Screen.width - 285f),
                    Mathf.Clamp(p.y - 20f, 38f, Screen.height - 92f), 277f, 82f);
                GUI.Box(hover,
                    hoverBattalion.MajorLabel + " | " + hoverBattalion.Label + "\n" +
                    "AI: " + (hoverBattalion.AIEnabled ? "ON" : "OFF") + " | " + hoverBattalion.Doctrine +
                    " | 3 heste | 4 kompagnier\n" + hoverBattalion.LastOrderText + "\n" + hoverBattalion.LastDecisionText,
                    hoverStyle);
            }
        }

        if (selectedBattalion < 0)
            return;

        Battalion battalion = battalions[selectedBattalion];
        Rect panel = new Rect(0f, Screen.height - HudHeight, Screen.width, HudHeight);
        GUI.depth = -890;
        GUI.Box(panel, string.Empty, panelStyle);

        float pad = 7f;
        float headerY = panel.y + 3f;
        GUI.Box(new Rect(pad, headerY, panel.width - pad * 2f, 19f),
            battalion.Label + " | " + battalion.MajorLabel + " | 4 KOMPAGNIER", headerStyle);

        float aiX = panel.xMax - 274f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 70f, 17f), battalion.AIEnabled ? "AI ON" : "AI OFF",
                battalion.AIEnabled ? accentStyle : buttonStyle))
            ToggleBattalionAI(selectedBattalion);

        aiX += 73f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                battalion.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF", buttonStyle))
            SetBattalionDoctrine(selectedBattalion, OfficerAIDoctrine.Defensive);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                battalion.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL", buttonStyle))
            SetBattalionDoctrine(selectedBattalion, OfficerAIDoctrine.Balanced);
        aiX += 65f;
        if (GUI.Button(new Rect(aiX, headerY + 1f, 62f, 17f),
                battalion.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF", buttonStyle))
            SetBattalionDoctrine(selectedBattalion, OfficerAIDoctrine.Offensive);

        float infoWidth = Mathf.Clamp(panel.width * 0.31f, 360f, 520f);
        float rowY = panel.y + 27f;
        GUI.Label(new Rect(pad + 3f, rowY, infoWidth - 8f, 15f), battalion.LastOrderText, labelStyle);
        GUI.Label(new Rect(pad + 3f, rowY + 16f, infoWidth - 8f, 15f), battalion.LastDecisionText, mutedStyle);

        float commandX = infoWidth + 8f;
        float commandWidth = panel.width - commandX - 8f;
        const float gap = 4f;
        float buttonWidth = (commandWidth - gap * 5f) / 6f;
        const float commandHeight = 27f;

        if (GUI.Button(new Rect(commandX, rowY, buttonWidth, commandHeight), "ANGRIB HER", buttonStyle)) pendingOrder = MajorOrder09F18.AttackHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap), rowY, buttonWidth, commandHeight), "FORSVAR HER", buttonStyle)) pendingOrder = MajorOrder09F18.DefendHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 2f, rowY, buttonWidth, commandHeight), "RYK FREM", buttonStyle)) pendingOrder = MajorOrder09F18.AdvanceHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 3f, rowY, buttonWidth, commandHeight), "TILBAGETRÆK", buttonStyle)) pendingOrder = MajorOrder09F18.WithdrawHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 4f, rowY, buttonWidth, commandHeight), "SAML", buttonStyle)) pendingOrder = MajorOrder09F18.AssembleHere;
        if (GUI.Button(new Rect(commandX + (buttonWidth + gap) * 5f, rowY, buttonWidth, commandHeight), "HOLD", buttonStyle))
        {
            pendingOrder = MajorOrder09F18.None;
            IssueBattalionOrder(selectedBattalion, MajorOrder09F18.HoldPosition, GetBattalionCenter(selectedBattalion), false);
        }

        if (pendingOrder != MajorOrder09F18.None)
            GUI.Label(new Rect(commandX, rowY + 31f, commandWidth, 15f), "Klik på slagmarken: " + Label(pendingOrder), mutedStyle);
    }
}
