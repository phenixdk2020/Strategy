using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f25
// First dynamic battalion-HQ positioning + command-zone model.
// HQ follows an advancing battalion in bounds. Distance penalties affect command
// reaction/coordination only; they never modify weapon accuracy or musket range.
[DefaultExecutionOrder(720)]
public sealed class PrototypeMajorCommandZone09F25 : MonoBehaviour
{
    public enum CommandBand
    {
        InCommand,
        Extended,
        OutOfCommand
    }

    public static PrototypeMajorCommandZone09F25 Instance { get; private set; }

    private PrototypeMajorBattalion09F18 major;
    private FieldInfo lastOrderField;
    private FieldInfo lastOrderPointField;
    private FieldInfo hqGoalField;
    private FieldInfo hasHqGoalField;
    private MethodInfo safeHqMethod;
    private FieldInfo thinkTimerField;

    private LineRenderer inCommandRing;
    private LineRenderer extendedRing;
    private Material inCommandMaterial;
    private Material extendedMaterial;
    private GUIStyle statusStyle;

    private float nextPositionReview;
    private float nextPenaltyReview;
    private string lastBandTelemetry = string.Empty;

    private const float PreferredAttackStandoff = 165f;
    private const float RelocationTrigger = 80f;
    private const float CompanyAdvanceTrigger = 235f;
    private const float InCommandRange = 320f;
    private const float ExtendedRange = 450f;
    private const float ExtendedReactionDelay = 0.15f;
    private const float OutReactionDelay = 0.40f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMajorCommandZone09F25>() == null)
            new GameObject("PrototypeMajorCommandZone_v000009f25").AddComponent<PrototypeMajorCommandZone09F25>();
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
        if (!Resolve())
            return;

        if (Time.time >= nextPositionReview)
        {
            nextPositionReview = Time.time + 2.5f;
            ReviewHqPosition();
        }

        if (Time.time >= nextPenaltyReview)
        {
            nextPenaltyReview = Time.time + 1.0f;
            ApplyCommandDelay();
        }

        UpdateRings();
    }

    public CommandBand GetBand(Regiment regiment)
    {
        if (major == null || major.HqRoot == null || regiment == null)
            return CommandBand.OutOfCommand;

        float distance = PlanarDistance(regiment.transform.position, major.HqRoot.transform.position);
        if (distance <= InCommandRange)
            return CommandBand.InCommand;
        if (distance <= ExtendedRange)
            return CommandBand.Extended;
        return CommandBand.OutOfCommand;
    }

    public string GetBandLabel(Regiment regiment)
    {
        switch (GetBand(regiment))
        {
            case CommandBand.InCommand: return "IN COMMAND";
            case CommandBand.Extended: return "EXTENDED";
            default: return "OUT OF COMMAND";
        }
    }

    private bool Resolve()
    {
        if (major == null)
            major = PrototypeMajorBattalion09F18.Instance;
        if (major == null || !major.Installed || major.HqRoot == null)
            return false;

        if (lastOrderField != null)
            return true;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        System.Type majorType = typeof(PrototypeMajorBattalion09F18);
        lastOrderField = majorType.GetField("lastOrder", flags);
        lastOrderPointField = majorType.GetField("lastOrderPoint", flags);
        hqGoalField = majorType.GetField("hqGoal", flags);
        hasHqGoalField = majorType.GetField("hasHqGoal", flags);
        safeHqMethod = majorType.GetMethod("SafeHq", flags);
        thinkTimerField = typeof(OfficerAIController).GetField("thinkTimer", flags);

        bool ok = lastOrderField != null && lastOrderPointField != null &&
                  hqGoalField != null && hasHqGoalField != null && safeHqMethod != null &&
                  thinkTimerField != null;

        if (!ok)
            Debug.LogError("HQ-ZONE-09F25|Installed=False|Reason=ReflectionMissing");
        else
            EnsureRings();

        return ok;
    }

    private void ReviewHqPosition()
    {
        // Direct/manual Major movement deliberately switches Major AI OFF. Automatic
        // relocation therefore only acts while the Major is actually delegated/AI ON.
        if (!major.AIEnabled)
            return;

        MajorOrder09F18 order = (MajorOrder09F18)lastOrderField.GetValue(major);
        if (order != MajorOrder09F18.AttackHere &&
            order != MajorOrder09F18.AdvanceHere &&
            order != MajorOrder09F18.DefendHere)
            return;

        Vector3 battalionCenter = GetBattalionCenter();
        Vector3 hq = major.HqRoot.transform.position;
        float farthest = FarthestCompanyDistance(hq);
        if (farthest < CompanyAdvanceTrigger)
            return;

        Vector3 objective = (Vector3)lastOrderPointField.GetValue(major);
        Vector3 forward = objective - battalionCenter;
        forward.y = 0f;

        if (forward.sqrMagnitude < 25f)
        {
            forward = battalionCenter - hq;
            forward.y = 0f;
        }
        if (forward.sqrMagnitude < 0.01f)
            forward = major.HqRoot.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float standoff = order == MajorOrder09F18.DefendHere ? 145f : PreferredAttackStandoff;
        Vector3 desired = battalionCenter - forward * standoff;
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3[] candidates =
        {
            desired,
            desired + lateral * 55f,
            desired - lateral * 55f,
            desired + lateral * 105f,
            desired - lateral * 105f,
            desired - forward * 45f
        };

        Vector3 chosen = hq;
        bool found = false;
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 safe = (Vector3)safeHqMethod.Invoke(major, new object[] { candidates[i] });
            if (PlanarDistance(hq, safe) < RelocationTrigger)
                continue;

            // HQ movement is currently straight-line. Test only HARD blockers here.
            // Trees/fences remain pass-through in the active terrain design and must
            // therefore not freeze HQ advance merely because old recovery code scores them.
            if (!HqPathClear(hq, safe))
                continue;

            chosen = safe;
            found = true;
            break;
        }

        if (!found)
        {
            Debug.Log("HQ-ZONE-09F25|Relocate=False|Reason=NoClearHQPath|FarthestCompany=" + farthest.ToString("0"));
            return;
        }

        hqGoalField.SetValue(major, chosen);
        hasHqGoalField.SetValue(major, true);

        Debug.Log("HQ-ZONE-09F25|Relocate=True|Order=" + order +
                  "|HQFrom=" + hq.x.ToString("0") + "," + hq.z.ToString("0") +
                  "|HQTo=" + chosen.x.ToString("0") + "," + chosen.z.ToString("0") +
                  "|BattalionCenter=" + battalionCenter.x.ToString("0") + "," + battalionCenter.z.ToString("0") +
                  "|FarthestCompany=" + farthest.ToString("0") +
                  "|MajorAI=ON");
    }

    private static bool HqPathClear(Vector3 start, Vector3 end)
    {
        float distance = PlanarDistance(start, end);
        int samples = Mathf.Clamp(Mathf.CeilToInt(distance / 4f), 2, 128);

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(start, end, t);

            if (Mathf.Abs(p.x) > PrototypeBootstrap.BattlefieldHalfWidth - 8f ||
                Mathf.Abs(p.z) > PrototypeBootstrap.BattlefieldHalfDepth - 8f)
                return false;

            float riverX = PrototypeBootstrap.StreamCenterX(p.z);
            bool inBridgeZone = Mathf.Abs(p.z - 22f) <= 8f;
            if (Mathf.Abs(p.x - riverX) <= 4.7f && !inBridgeZone)
                return false;

            Vector3 probe = new Vector3(
                p.x,
                PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 1.0f,
                p.z);
            Collider[] hits = Physics.OverlapSphere(probe, 1.8f);
            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;
                string name = hit.gameObject.name;
                string root = hit.transform.root != null ? hit.transform.root.name : string.Empty;
                if (name.Contains("Farmhouse") || name.Contains("Barn") ||
                    root.Contains("Farmhouse") || root.Contains("Barn"))
                    return false;
            }
        }

        return true;
    }

    private void ApplyCommandDelay()
    {
        if (thinkTimerField == null || major.HqRoot == null)
            return;

        int inCommand = 0;
        int extended = 0;
        int outOfCommand = 0;

        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null || regiment.IsRouted)
                continue;

            CommandBand band = GetBand(regiment);
            if (band == CommandBand.InCommand) inCommand++;
            else if (band == CommandBand.Extended) extended++;
            else outOfCommand++;

            OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
            if (controller == null || !controller.AIEnabled || !controller.enabled)
                continue;

            float penalty = band == CommandBand.Extended
                ? ExtendedReactionDelay
                : band == CommandBand.OutOfCommand ? OutReactionDelay : 0f;

            if (penalty <= 0f)
                continue;

            float timer = (float)thinkTimerField.GetValue(controller);
            timer = Mathf.Min(2.5f, Mathf.Max(0f, timer) + penalty);
            thinkTimerField.SetValue(controller, timer);
        }

        string telemetry = inCommand + "|" + extended + "|" + outOfCommand;
        if (telemetry != lastBandTelemetry)
        {
            lastBandTelemetry = telemetry;
            Debug.Log("HQ-ZONE-09F25|InCommand=" + inCommand +
                      "|Extended=" + extended +
                      "|OutOfCommand=" + outOfCommand +
                      "|Penalty=ReactionDelayOnly|AccuracyPenalty=False");
        }
    }

    private Vector3 GetBattalionCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null || regiment.IsRouted || regiment.CurrentStrength <= 0)
                continue;
            sum += regiment.transform.position;
            count++;
        }
        return count > 0 ? sum / count : major.HqRoot.transform.position;
    }

    private float FarthestCompanyDistance(Vector3 hq)
    {
        float farthest = 0f;
        foreach (Regiment regiment in major.Companies)
        {
            if (regiment == null || regiment.IsRouted || regiment.CurrentStrength <= 0)
                continue;
            farthest = Mathf.Max(farthest, PlanarDistance(hq, regiment.transform.position));
        }
        return farthest;
    }

    private void EnsureRings()
    {
        if (inCommandRing != null && extendedRing != null)
            return;

        inCommandMaterial = CreateMaterial(new Color(0.35f, 0.70f, 0.36f, 0.62f), "HQ25InCommand");
        extendedMaterial = CreateMaterial(new Color(0.86f, 0.67f, 0.20f, 0.58f), "HQ25Extended");

        inCommandRing = CreateRing("MajorCommandZone_IN", inCommandMaterial, 0.34f);
        extendedRing = CreateRing("MajorCommandZone_EXTENDED", extendedMaterial, 0.26f);
        inCommandRing.enabled = false;
        extendedRing.enabled = false;
    }

    private LineRenderer CreateRing(string name, Material material, float width)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 96;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        return line;
    }

    private void UpdateRings()
    {
        EnsureRings();
        bool show = major != null && major.Selected && major.HqRoot != null;
        inCommandRing.enabled = show;
        extendedRing.enabled = show;
        if (!show)
            return;

        DrawRing(inCommandRing, major.HqRoot.transform.position, InCommandRange);
        DrawRing(extendedRing, major.HqRoot.transform.position, ExtendedRange);
    }

    private static void DrawRing(LineRenderer line, Vector3 center, float radius)
    {
        if (line == null)
            return;

        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.40f;
            line.SetPosition(i, p);
        }
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }

    private void EnsureStyle()
    {
        if (statusStyle != null)
            return;
        statusStyle = PrototypeUiTheme09F15.Label(10, true);
        statusStyle.alignment = TextAnchor.MiddleRight;
    }

    private void OnGUI()
    {
        if (major == null || !major.Installed || major.Selected)
            return;

        Regiment selected = FirstSelectedCompany();
        if (selected == null || !ContainsCompany(selected))
            return;

        EnsureStyle();
        float distance = PlanarDistance(selected.transform.position, major.HqRoot.transform.position);
        GUI.depth = -3050;
        GUI.Label(
            new Rect(Screen.width - 390f, Screen.height - 61f, 380f, 27f),
            "HQ: " + GetBandLabel(selected) + "  |  " + distance.ToString("0") + " m",
            statusStyle);
    }

    private bool ContainsCompany(Regiment regiment)
    {
        foreach (Regiment company in major.Companies)
            if (company == regiment)
                return true;
        return false;
    }

    private static Regiment FirstSelectedCompany()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;
        foreach (Regiment regiment in battle.Regiments)
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected)
                return regiment;
        return null;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
