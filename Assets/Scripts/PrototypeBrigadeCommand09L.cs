using System.Collections.Generic;
using UnityEngine;

public enum PrototypeBrigadeMission09L
{
    Hold,
    DefendArea,
    AttackCaptureArea
}

public enum PrototypeBrigadeRegimentRole09L
{
    Reserve,
    Support,
    Engaged,
    Manoeuvre,
    Withdraw
}

// v00.00.09l TEST - first real brigade command layer.
// The brigade commander receives an objective and decides whether the second regiment
// remains in reserve or is committed. Denmark is player-controlled by default (F11 toggles
// Brigade AI); Prussian QA brigade starts autonomous. This layer issues missions only
// through OfficerAIController and never writes Regiment destination directly.
[DefaultExecutionOrder(-8500)]
public sealed class PrototypeBrigadeCommand09L : MonoBehaviour
{
    private sealed class BrigadeState
    {
        public string Name;
        public BattleTeam Team;
        public Regiment Primary;
        public Regiment Reserve;
        public GameObject HqRoot;
        public PrototypeBrigadeMission09L Mission;
        public Vector3 Objective;
        public float ObjectiveRadius;
        public float RequestedReserveFraction;
        public bool AIEnabled;
        public bool ReserveCommitted;
        public float NextThink;
        public float LastPrimaryDistance;
        public float LastProgressTime;
        public readonly Dictionary<Regiment, PrototypeBrigadeRegimentRole09L> Roles =
            new Dictionary<Regiment, PrototypeBrigadeRegimentRole09L>();
    }

    public static PrototypeBrigadeCommand09L Instance { get; private set; }

    private BrigadeState danish;
    private BrigadeState prussian;
    private bool installed;
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBrigadeCommand09L>() != null)
            return;

        GameObject root = new GameObject("PrototypeBrigadeCommand_v000009l");
        root.AddComponent<PrototypeBrigadeCommand09L>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
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
        if (!installed)
        {
            TryInstall();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            danish.AIEnabled = !danish.AIEnabled;
            if (!danish.AIEnabled)
                ReleaseDanishToManualControl();
            else
                ApplyMission(danish, true);

            Debug.Log("BRIGADE-09L|Brigade=7. Brigade|AI=" + danish.AIEnabled + "|Hotkey=F11");
        }

        EvaluateBrigade(danish);
        EvaluateBrigade(prussian);
    }

    private void TryInstall()
    {
        Regiment dk1 = PrototypeFullScaleOOBManager09K.FindRegiment("1. Regiment");
        Regiment dk11Technical = PrototypeFullScaleOOBManager09K.FindRegiment("5. Regiment");
        Regiment pr8 = PrototypeFullScaleOOBManager09K.FindRegiment("8th Regiment");
        Regiment pr18 = PrototypeFullScaleOOBManager09K.FindRegiment("18th Regiment");

        if (!Ready(dk1) || !Ready(dk11Technical) || !Ready(pr8) || !Ready(pr18))
            return;

        danish = CreateBrigade(
            "7. Brigade",
            BattleTeam.Denmark,
            dk1,
            dk11Technical,
            new Vector3(-150f, 0f, 0f),
            false,
            PrototypeBrigadeMission09L.DefendArea,
            new Vector3(-72f, 0f, 0f),
            68f,
            0.45f);

        prussian = CreateBrigade(
            "Prussian Brigade (QA)",
            BattleTeam.Prussia,
            pr8,
            pr18,
            new Vector3(150f, 0f, 0f),
            true,
            PrototypeBrigadeMission09L.AttackCaptureArea,
            new Vector3(-62f, 0f, 0f),
            55f,
            0.35f);

        installed = true;
        ApplyMission(prussian, true);

        Debug.Log(
            "BRIGADE-09L|Installed=True|Danish=7.Brigade(1+11)|DanishAI=False|" +
            "Prussian=QA(8+18)|PrussianAI=True|ReserveCommitAI=True|HQ=6RidersPerBrigade");
    }

    private static bool Ready(Regiment regiment)
    {
        return regiment != null &&
               regiment.GetComponent<PrototypeRegimentOOB09K>() != null &&
               regiment.GetComponent<OfficerAIController>() != null;
    }

    private BrigadeState CreateBrigade(
        string brigadeName,
        BattleTeam team,
        Regiment primary,
        Regiment reserve,
        Vector3 hqPosition,
        bool aiEnabled,
        PrototypeBrigadeMission09L mission,
        Vector3 objective,
        float objectiveRadius,
        float reserveFraction)
    {
        BrigadeState state = new BrigadeState
        {
            Name = brigadeName,
            Team = team,
            Primary = primary,
            Reserve = reserve,
            AIEnabled = aiEnabled,
            Mission = mission,
            Objective = objective,
            ObjectiveRadius = objectiveRadius,
            RequestedReserveFraction = reserveFraction,
            NextThink = Time.time + 0.50f,
            LastProgressTime = Time.time
        };

        state.Roles[primary] = PrototypeBrigadeRegimentRole09L.Engaged;
        state.Roles[reserve] = PrototypeBrigadeRegimentRole09L.Reserve;
        state.LastPrimaryDistance = HorizontalDistance(primary.transform.position, objective);
        state.HqRoot = BuildBrigadeHQ(state, hqPosition);

        Debug.Log(
            "BRIGADE-09L|Create=" + brigadeName +
            "|Primary=" + GetDisplayName(primary) +
            "|Reserve=" + GetDisplayName(reserve) +
            "|Mission=" + mission +
            "|ReserveRequested=" + (reserveFraction * 100f).ToString("0") + "%" +
            "|AI=" + aiEnabled);

        return state;
    }

    private void EvaluateBrigade(BrigadeState brigade)
    {
        if (brigade == null || !brigade.AIEnabled || Time.time < brigade.NextThink)
            return;

        brigade.NextThink = Time.time + 1.25f;

        if (brigade.Primary == null || brigade.Reserve == null)
            return;

        if (!brigade.ReserveCommitted)
        {
            float distance = HorizontalDistance(brigade.Primary.transform.position, brigade.Objective);
            bool madeProgress = distance < brigade.LastPrimaryDistance - 4f;
            if (madeProgress)
            {
                brigade.LastPrimaryDistance = distance;
                brigade.LastProgressTime = Time.time;
            }

            float strengthFraction = brigade.Primary.InitialStrength > 0
                ? brigade.Primary.CurrentStrength / (float)brigade.Primary.InitialStrength
                : 0f;

            bool primaryInTrouble =
                brigade.Primary.IsRouted ||
                strengthFraction < 0.72f ||
                brigade.Primary.Morale < 52f ||
                brigade.Primary.Cohesion < 46f;

            bool stalledAttack =
                brigade.Mission == PrototypeBrigadeMission09L.AttackCaptureArea &&
                Time.time - brigade.LastProgressTime > 12f &&
                distance > brigade.ObjectiveRadius;

            if (primaryInTrouble || stalledAttack)
            {
                CommitReserve(
                    brigade,
                    primaryInTrouble ? "PrimaryUnderPressure" : "AttackNoProgress");
            }
        }
        else
        {
            float reserveDistance = HorizontalDistance(brigade.Reserve.transform.position, brigade.Objective);
            if (reserveDistance <= brigade.ObjectiveRadius * 1.20f)
                SetRole(brigade, brigade.Reserve, PrototypeBrigadeRegimentRole09L.Engaged, "ReserveReachedObjectiveSpace");
        }
    }

    private void ApplyMission(BrigadeState brigade, bool resetReserve)
    {
        if (brigade == null || !brigade.AIEnabled)
            return;

        if (resetReserve)
        {
            brigade.ReserveCommitted = false;
            SetRole(brigade, brigade.Primary, PrototypeBrigadeRegimentRole09L.Engaged, "InitialCommitment");
            SetRole(brigade, brigade.Reserve, PrototypeBrigadeRegimentRole09L.Reserve, "HeldBackByBrigadeCommander");
        }

        OfficerAIController primaryAI = GetOfficer(brigade.Primary);
        OfficerAIController reserveAI = GetOfficer(brigade.Reserve);
        if (primaryAI == null || reserveAI == null)
            return;

        primaryAI.SetAIEnabled(true);

        switch (brigade.Mission)
        {
            case PrototypeBrigadeMission09L.AttackCaptureArea:
                primaryAI.SetDoctrine(OfficerAIDoctrine.Offensive);
                primaryAI.SetMoveMission(brigade.Objective + new Vector3(0f, 0f, -10f));
                break;
            case PrototypeBrigadeMission09L.DefendArea:
                primaryAI.SetDoctrine(OfficerAIDoctrine.Defensive);
                primaryAI.SetMoveMission(brigade.Objective + new Vector3(0f, 0f, -18f));
                break;
            default:
                primaryAI.SetHoldMission();
                break;
        }

        if (!brigade.ReserveCommitted)
        {
            reserveAI.SetAIEnabled(false);
            brigade.Reserve.OrderHold();
        }
    }

    private void CommitReserve(BrigadeState brigade, string reason)
    {
        if (brigade == null || brigade.ReserveCommitted || brigade.Reserve == null)
            return;

        brigade.ReserveCommitted = true;
        SetRole(brigade, brigade.Reserve, PrototypeBrigadeRegimentRole09L.Support, reason);

        OfficerAIController reserveAI = GetOfficer(brigade.Reserve);
        if (reserveAI != null)
        {
            reserveAI.SetAIEnabled(true);
            reserveAI.SetDoctrine(
                brigade.Mission == PrototypeBrigadeMission09L.AttackCaptureArea
                    ? OfficerAIDoctrine.Offensive
                    : OfficerAIDoctrine.Balanced);

            Vector3 offset = brigade.Team == BattleTeam.Denmark
                ? new Vector3(-12f, 0f, 18f)
                : new Vector3(12f, 0f, 18f);
            reserveAI.SetMoveMission(brigade.Objective + offset);
        }

        Debug.Log(
            "BRIGADE-09L|Brigade=" + brigade.Name +
            "|CommitReserve=True|Unit=" + GetDisplayName(brigade.Reserve) +
            "|Reason=" + reason +
            "|Mission=" + brigade.Mission);
    }

    private static OfficerAIController GetOfficer(Regiment regiment)
    {
        return regiment != null ? regiment.GetComponent<OfficerAIController>() : null;
    }

    private void ReleaseDanishToManualControl()
    {
        if (danish == null)
            return;

        OfficerAIController a = GetOfficer(danish.Primary);
        OfficerAIController b = GetOfficer(danish.Reserve);
        if (a != null) a.SetAIEnabled(false);
        if (b != null) b.SetAIEnabled(false);

        SetRole(danish, danish.Primary, PrototypeBrigadeRegimentRole09L.Manoeuvre, "PlayerManualControl");
        SetRole(danish, danish.Reserve, PrototypeBrigadeRegimentRole09L.Manoeuvre, "PlayerManualControl");
    }

    private static void SetRole(
        BrigadeState brigade,
        Regiment regiment,
        PrototypeBrigadeRegimentRole09L role,
        string reason)
    {
        if (brigade == null || regiment == null)
            return;

        PrototypeBrigadeRegimentRole09L oldRole;
        bool had = brigade.Roles.TryGetValue(regiment, out oldRole);
        brigade.Roles[regiment] = role;

        if (!had || oldRole != role)
        {
            Debug.Log(
                "BRIGADE-09L|Brigade=" + brigade.Name +
                "|Unit=" + GetDisplayName(regiment) +
                "|Role=" + role +
                "|Reason=" + reason);
        }
    }

    private static string GetDisplayName(Regiment regiment)
    {
        if (regiment == null)
            return "None";

        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        return oob != null ? oob.DisplayName : regiment.RegimentName;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static GameObject BuildBrigadeHQ(BrigadeState brigade, Vector3 position)
    {
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject root = new GameObject("BrigadeHQ09L_" + brigade.Name);
        root.transform.position = position;
        root.transform.rotation = brigade.Team == BattleTeam.Denmark
            ? Quaternion.Euler(0f, 90f, 0f)
            : Quaternion.Euler(0f, -90f, 0f);

        Color coatColor = brigade.Team == BattleTeam.Denmark
            ? new Color(0.10f, 0.20f, 0.36f)
            : new Color(0.08f, 0.12f, 0.22f);
        Material coat = PrototypeBootstrap.CreateSharedMaterial(coatColor, "BrigadeHQ09L_Coat_" + brigade.Name);
        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.24f, 0.14f, 0.075f), "BrigadeHQ09L_Horse");
        Material skin = PrototypeBootstrap.CreateSharedMaterial(new Color(0.72f, 0.56f, 0.43f), "BrigadeHQ09L_Skin");
        Material dark = PrototypeBootstrap.CreateSharedMaterial(new Color(0.06f, 0.06f, 0.07f), "BrigadeHQ09L_Dark");

        string[] roles =
        {
            "BrigadeCommander",
            "Adjutant",
            "StaffOfficer1",
            "StaffOfficer2",
            "Courier1",
            "Courier2"
        };

        for (int i = 0; i < roles.Length; i++)
        {
            GameObject rider = new GameObject(roles[i]);
            rider.transform.SetParent(root.transform, false);
            int row = i / 3;
            int file = i % 3;
            rider.transform.localPosition = new Vector3((file - 1) * 2.2f, 0f, -row * 2.5f);

            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseBody", new Vector3(0f, 0.75f, 0f), new Vector3(0.48f, 0.42f, 0.88f), Quaternion.Euler(90f, 0f, 0f), horse);
            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseNeck", new Vector3(0f, 1.14f, 0.54f), new Vector3(0.23f, 0.40f, 0.23f), Quaternion.Euler(-20f, 0f, 0f), horse);
            CreatePart(rider.transform, PrimitiveType.Sphere, "HorseHead", new Vector3(0f, 1.46f, 0.70f), new Vector3(0.25f, 0.21f, 0.32f), Quaternion.identity, horse);
            CreatePart(rider.transform, PrimitiveType.Capsule, "RiderBody", new Vector3(0f, 1.82f, 0f), new Vector3(0.28f, 0.46f, 0.24f), Quaternion.identity, coat);
            CreatePart(rider.transform, PrimitiveType.Sphere, "RiderHead", new Vector3(0f, 2.34f, 0f), Vector3.one * 0.18f, Quaternion.identity, skin);
            CreatePart(rider.transform, PrimitiveType.Cylinder, "Headgear", new Vector3(0f, 2.50f, 0f), new Vector3(0.20f, 0.08f, 0.20f), Quaternion.identity, dark);

            if (i == 0)
                CreatePart(rider.transform, PrimitiveType.Cube, "CommanderSword", new Vector3(-0.34f, 1.65f, 0.10f), new Vector3(0.035f, 0.58f, 0.035f), Quaternion.Euler(0f, 0f, -18f), dark);
        }

        AddHQLabel(root.transform, brigade.Name);
        return root;
    }

    private static void AddHQLabel(Transform parent, string text)
    {
        GameObject label = new GameObject("HQLabel");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, 3.3f, 0f);
        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 42;
        tm.characterSize = 0.10f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
    }

    private static void CreatePart(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
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
            Object.Destroy(collider);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 10;
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.padding = new RectOffset(8, 8, 6, 6);
        panelStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!installed)
            return;

        EnsureStyles();
        float width = 310f;
        Rect panel = new Rect(Screen.width - width - 12f, 108f, width, 108f);
        GUI.Box(panel, string.Empty, panelStyle);

        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 6f, panel.width - 16f, panel.height - 12f));
        GUILayout.Label("BRIGADE COMMAND 09L", labelStyle);
        GUILayout.Label(DescribeBrigade(danish) + "   [F11 toggles DK Brigade AI]", labelStyle);
        GUILayout.Label(DescribeBrigade(prussian), labelStyle);
        GUILayout.EndArea();
    }

    private static string DescribeBrigade(BrigadeState b)
    {
        if (b == null)
            return "-";

        string reserveState = b.ReserveCommitted ? "COMMITTED" : "RESERVE";
        return b.Name + " | AI " + (b.AIEnabled ? "ON" : "OFF") + " | " + b.Mission + " | " + reserveState;
    }
}
