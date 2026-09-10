using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09m1 - Strategy-Kamp command QA.
[DefaultExecutionOrder(12050)]
public sealed class PrototypeKampCommandQa09M1 : MonoBehaviour
{
    private sealed class HqState
    {
        public Regiment Regiment;
        public Transform Root;
        public LineRenderer CloseFan;
        public LineRenderer MediumFan;
        public LineRenderer LongFan;
    }

    private readonly Dictionary<Regiment, HqState> hqs = new Dictionary<Regiment, HqState>();
    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, LineRenderer> groundedOutlines =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, LineRenderer>();

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private bool announced;
    private bool hidLegacyMarkers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampCommandQa09M1>() != null)
            return;
        GameObject root = new GameObject("PrototypeKampCommandQa_v000009m1");
        root.AddComponent<PrototypeKampCommandQa09M1>();
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        SuppressConflictingLayers();
        GroundSelectionBoxes(control);
        UpdateRegimentalHqs(control);
        UpdateRangeFans();

        if (!announced)
        {
            announced = true;
            Debug.Log("KAMP-CMD-09M1|Installed=True|SelectionBox=TerrainFollow|RangeCone=Selected|HQ=3MountedOfficers|Menu=CompanySelection");
        }
    }

    private void SuppressConflictingLayers()
    {
        PrototypeCompanyGuidons09L4 guidons = Object.FindAnyObjectByType<PrototypeCompanyGuidons09L4>();
        if (guidons != null && guidons.enabled)
            guidons.enabled = false;

        if (!hidLegacyMarkers)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tr = transforms[i];
                if (tr == null)
                    continue;
                if (tr.name == "CompanyGuidons09L4" || tr.name.StartsWith("DualStandards09L") || tr.name.StartsWith("RegimentalHQ09K"))
                    tr.gameObject.SetActive(false);
            }
            hidLegacyMarkers = true;
        }

        PrototypeCompanyCombatAuthority09L4 combatAuth = Object.FindAnyObjectByType<PrototypeCompanyCombatAuthority09L4>();
        if (combatAuth != null && combatAuth.enabled)
            combatAuth.enabled = false;

        PrototypeFullScaleRenderer09K scale = Object.FindAnyObjectByType<PrototypeFullScaleRenderer09K>();
        if (scale != null && scale.enabled)
            scale.enabled = false;

        PrototypeDualStandards09L standards = Object.FindAnyObjectByType<PrototypeDualStandards09L>();
        if (standards != null && standards.enabled)
            standards.enabled = false;
    }

    private void GroundSelectionBoxes(PrototypeCompanyTacticalControl09L2 control)
    {
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null)
                continue;

            LineRenderer outline = GetOrCaptureOutline(company);
            if (outline == null)
                continue;

            outline.useWorldSpace = true;
            outline.loop = true;
            outline.positionCount = 4;
            outline.enabled = company.IsSelected;
            if (!company.IsSelected)
                continue;

            float halfW = company.GetFootprintWidth() * 0.5f + 0.7f;
            float halfD = company.GetFootprintDepth() * 0.5f + 0.7f;
            Vector3[] local =
            {
                new Vector3(-halfW, 0f, -halfD),
                new Vector3(-halfW, 0f,  halfD),
                new Vector3( halfW, 0f,  halfD),
                new Vector3( halfW, 0f, -halfD)
            };

            for (int c = 0; c < 4; c++)
            {
                Vector3 world = company.transform.TransformPoint(local[c]);
                world.y = PrototypeBootstrap.SampleGroundHeight(world.x, world.z) + 0.12f;
                outline.SetPosition(c, world);
            }
        }
    }

    private LineRenderer GetOrCaptureOutline(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (groundedOutlines.TryGetValue(company, out LineRenderer existing) && existing != null)
            return existing;

        Transform child = company.transform.Find("CompanySelectionOutline09L2");
        LineRenderer outline = child != null ? child.GetComponent<LineRenderer>() : company.GetComponentInChildren<LineRenderer>(true);
        if (outline != null)
            groundedOutlines[company] = outline;
        return outline;
    }

    private void UpdateRegimentalHqs(PrototypeCompanyTacticalControl09L2 control)
    {
        HashSet<Regiment> live = new HashSet<Regiment>();
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null || company.CurrentStrength <= 0)
                continue;
            Regiment regiment = company.ParentRegiment;
            if (!live.Add(regiment))
                continue;
            HqState state = GetOrBuildHq(regiment);
            PlaceHqBehindCompanies(state, companies);
        }
    }

    private HqState GetOrBuildHq(Regiment regiment)
    {
        if (hqs.TryGetValue(regiment, out HqState state) && state.Root != null)
            return state;

        GameObject root = new GameObject("KampRegimentalHQ09M1_" + regiment.RegimentName);
        BuildMountedOfficers(root.transform, regiment);
        state = new HqState
        {
            Regiment = regiment,
            Root = root.transform,
            CloseFan = CreateFan(root.transform, "CloseFan", regiment.CloseRange, 0.10f, new Color(0.95f, 0.78f, 0.18f)),
            MediumFan = CreateFan(root.transform, "MediumFan", regiment.EffectiveRange, 0.13f, new Color(0.95f, 0.64f, 0.12f)),
            LongFan = CreateFan(root.transform, "LongFan", regiment.MaximumRange, 0.18f, new Color(0.95f, 0.42f, 0.10f))
        };
        hqs[regiment] = state;
        return state;
    }

    private static void BuildMountedOfficers(Transform root, Regiment regiment)
    {
        PrototypeUniformProfile09H profile = PrototypeUniformProfile09H.CreateRegimentDefault(regiment);
        Material coat = PrototypeBootstrap.CreateSharedMaterial(profile.CoatColor, "09M1_HQCoat_" + regiment.RegimentName);
        Material horse = PrototypeBootstrap.CreateSharedMaterial(new Color(0.23f, 0.13f, 0.07f), "09M1_HQHorse_" + regiment.RegimentName);
        Material skin = PrototypeBootstrap.CreateSharedMaterial(profile.SkinColor, "09M1_HQSkin_" + regiment.RegimentName);
        Material dark = PrototypeBootstrap.CreateSharedMaterial(profile.HeadgearColor, "09M1_HQDark_" + regiment.RegimentName);
        Material pole = PrototypeBootstrap.CreateSharedMaterial(new Color(0.27f, 0.17f, 0.07f), "09M1_HQPole_" + regiment.RegimentName);
        Material brass = PrototypeBootstrap.CreateSharedMaterial(new Color(0.75f, 0.58f, 0.20f), "09M1_HQBrass_" + regiment.RegimentName);
        string[] roles = { "Commander", "NationalStandardBearer", "RegimentalStandardBearer" };
        float[] xs = { 0f, -1.8f, 1.8f };
        for (int i = 0; i < 3; i++)
        {
            GameObject rider = new GameObject(roles[i]);
            rider.transform.SetParent(root, false);
            rider.transform.localPosition = new Vector3(xs[i], 0f, 0f);
            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseBody", new Vector3(0f, 0.75f, 0f), new Vector3(0.48f, 0.42f, 0.88f), Quaternion.Euler(90f, 0f, 0f), horse);
            CreatePart(rider.transform, PrimitiveType.Capsule, "HorseNeck", new Vector3(0f, 1.14f, 0.54f), new Vector3(0.23f, 0.40f, 0.23f), Quaternion.Euler(-20f, 0f, 0f), horse);
            CreatePart(rider.transform, PrimitiveType.Sphere, "HorseHead", new Vector3(0f, 1.46f, 0.70f), new Vector3(0.25f, 0.21f, 0.32f), Quaternion.identity, horse);
            CreatePart(rider.transform, PrimitiveType.Capsule, "RiderBody", new Vector3(0f, 1.82f, 0f), new Vector3(0.28f, 0.46f, 0.24f), Quaternion.identity, coat);
            CreatePart(rider.transform, PrimitiveType.Sphere, "RiderHead", new Vector3(0f, 2.34f, 0f), Vector3.one * 0.18f, Quaternion.identity, skin);
            CreatePart(rider.transform, PrimitiveType.Cylinder, "Headgear", new Vector3(0f, 2.50f, 0f), new Vector3(0.20f, 0.08f, 0.20f), Quaternion.identity, dark);
            if (i == 1) BuildCarriedFlag(rider.transform, regiment, true, pole, brass);
            if (i == 2) BuildCarriedFlag(rider.transform, regiment, false, pole, brass);
        }
    }

    private static void BuildCarriedFlag(Transform rider, Regiment regiment, bool national, Material pole, Material brass)
    {
        GameObject flag = new GameObject(national ? "NationalFlag" : "RegimentalFlag");
        flag.transform.SetParent(rider, false);
        flag.transform.localPosition = new Vector3(0.22f, 0f, -0.12f);
        CreatePart(flag.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 2.35f, 0f), new Vector3(0.04f, 2.15f, 0.04f), Quaternion.identity, pole);
        CreatePart(flag.transform, PrimitiveType.Cube, "Finial", new Vector3(0f, 4.55f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Quaternion.Euler(0f, 45f, 45f), brass);
        Color field = regiment.Team == BattleTeam.Denmark ? new Color(0.75f, 0.045f, 0.065f) : (national ? new Color(0.92f, 0.92f, 0.89f) : new Color(0.88f, 0.86f, 0.78f));
        Color device = regiment.Team == BattleTeam.Denmark ? new Color(0.96f, 0.96f, 0.93f) : new Color(0.055f, 0.055f, 0.065f);
        Material fieldMat = PrototypeBootstrap.CreateSharedMaterial(field, "09M1_FlagField_" + regiment.RegimentName + (national ? "_N" : "_R"));
        Material deviceMat = PrototypeBootstrap.CreateSharedMaterial(device, "09M1_FlagDevice_" + regiment.RegimentName + (national ? "_N" : "_R"));
        CreatePart(flag.transform, PrimitiveType.Cube, "Cloth", new Vector3(1.05f, 3.55f, 0f), new Vector3(2.05f, 1.15f, 0.04f), Quaternion.identity, fieldMat);
        CreatePart(flag.transform, PrimitiveType.Cube, "CrossV", new Vector3(national ? 0.62f : 1.05f, 3.55f, -0.028f), new Vector3(0.16f, 1.16f, 0.016f), Quaternion.identity, deviceMat);
        CreatePart(flag.transform, PrimitiveType.Cube, "CrossH", new Vector3(1.05f, 3.55f, -0.028f), new Vector3(2.06f, 0.15f, 0.016f), Quaternion.identity, deviceMat);
    }

    private static void PlaceHqBehindCompanies(HqState state, IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies)
    {
        Regiment regiment = state.Regiment;
        Vector3 centroid = Vector3.zero;
        Vector3 forward = Vector3.zero;
        int count = 0;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment != regiment || company.CurrentStrength <= 0)
                continue;
            centroid += company.transform.position;
            Vector3 f = company.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f)
                forward += f.normalized;
            count++;
        }
        if (count <= 0)
            return;
        centroid /= count;
        if (forward.sqrMagnitude < 0.01f)
            forward = regiment.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 pos = centroid - forward * 14f;
        pos.y = PrototypeBootstrap.SampleGroundHeight(pos.x, pos.z) + 0.10f;
        state.Root.position = pos;
        state.Root.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void UpdateRangeFans()
    {
        HashSet<Regiment> selectedParents = GetSelectedParents();
        foreach (KeyValuePair<Regiment, HqState> pair in hqs)
        {
            HqState state = pair.Value;
            if (state == null || state.Root == null)
                continue;
            bool show = selectedParents.Contains(state.Regiment) && state.Regiment.Team == BattleTeam.Denmark;
            RebuildFan(state.CloseFan, state.Regiment.CloseRange, show);
            RebuildFan(state.MediumFan, state.Regiment.EffectiveRange, show);
            RebuildFan(state.LongFan, state.Regiment.MaximumRange, show);
        }
    }

    private HashSet<Regiment> GetSelectedParents()
    {
        HashSet<Regiment> parents = new HashSet<Regiment>();
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control != null)
        {
            IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] != null && selected[i].ParentRegiment != null)
                    parents.Add(selected[i].ParentRegiment);
            }
        }
        return parents;
    }

    private static LineRenderer CreateFan(Transform parent, string name, float range, float width, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = width;
        line.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(color, name + "Mat");
        line.enabled = false;
        RebuildFan(line, range, false);
        return line;
    }

    private static void RebuildFan(LineRenderer line, float range, bool enabled)
    {
        if (line == null)
            return;
        const int arcSegments = 28;
        const float muzzleHalfWidth = 6.5f;
        const float muzzleZ = 1.10f;
        const float halfAngle = 60f;
        line.positionCount = arcSegments + 3;
        line.SetPosition(0, new Vector3(-muzzleHalfWidth, 0.16f, muzzleZ));
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;
            line.SetPosition(i + 1, new Vector3(Mathf.Sin(angle) * range, 0.16f, Mathf.Cos(angle) * range));
        }
        line.SetPosition(line.positionCount - 1, new Vector3(muzzleHalfWidth, 0.16f, muzzleZ));
        line.enabled = enabled;
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
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
        return part;
    }

    private Regiment GetMenuRegiment()
    {
        foreach (Regiment regiment in GetSelectedParents())
        {
            if (regiment != null && regiment.Team == BattleTeam.Denmark)
                return regiment;
        }
        return null;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.fontSize = 10;
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.normal.textColor = Color.white;
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = Color.white;
    }

    private Rect GetPanelRect()
    {
        const float height = 62f;
        float width = Mathf.Max(420f, Screen.width - 100f);
        return new Rect(50f, Screen.height - height - 18f, width, height);
    }

    public bool IsPointerOverControls(Vector3 mousePosition)
    {
        if (GetMenuRegiment() == null)
            return false;
        Vector2 guiPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return GetPanelRect().Contains(guiPoint);
    }

    private void OnGUI()
    {
        Regiment regiment = GetMenuRegiment();
        if (regiment == null)
            return;
        EnsureStyles();
        OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
        Rect panel = GetPanelRect();
        GUI.Box(panel, string.Empty, panelStyle);
        float x = panel.x + 6f;
        float y = panel.y + 6f;
        const float h = 24f;
        const float gap = 4f;
        PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
        string title = oob != null ? oob.DisplayName : regiment.RegimentName;
        GUI.Label(new Rect(x, y, 220f, h), title + "  " + regiment.CurrentStrength + "/" + regiment.InitialStrength, labelStyle);
        x += 224f;
        float buttonW = Mathf.Max(52f, (panel.xMax - x - 8f - gap * 6f) / 7f);
        bool aiOn = controller != null && controller.AIEnabled;
        if (GUI.Button(new Rect(x, y, buttonW, h), aiOn ? "[AI ON]" : "AI OFF") && controller != null)
            controller.SetAIEnabled(!aiOn);
        x += buttonW + gap;
        if (controller != null)
        {
            if (GUI.Button(new Rect(x, y, buttonW, h), controller.Doctrine == OfficerAIDoctrine.Defensive ? "[DEF]" : "DEF"))
                controller.SetDoctrine(OfficerAIDoctrine.Defensive);
            x += buttonW + gap;
            if (GUI.Button(new Rect(x, y, buttonW, h), controller.Doctrine == OfficerAIDoctrine.Balanced ? "[BAL]" : "BAL"))
                controller.SetDoctrine(OfficerAIDoctrine.Balanced);
            x += buttonW + gap;
            if (GUI.Button(new Rect(x, y, buttonW, h), controller.Doctrine == OfficerAIDoctrine.Offensive ? "[OFF]" : "OFF"))
                controller.SetDoctrine(OfficerAIDoctrine.Offensive);
            x += buttonW + gap;
        }
        y += h + 4f;
        x = panel.x + 6f;
        GUI.Label(new Rect(x, y, 90f, h), "Ild " + regiment.GetFirePolicyLabel(), labelStyle);
        x += 94f;
        buttonW = Mathf.Max(58f, (panel.xMax - x - 8f - gap * 3f) / 4f);
        if (GUI.Button(new Rect(x, y, buttonW, h), regiment.FirePolicy == RegimentFirePolicy.HoldFire ? "[HOLD]" : "HOLD"))
            regiment.SetFirePolicy(RegimentFirePolicy.HoldFire);
        x += buttonW + gap;
        if (GUI.Button(new Rect(x, y, buttonW, h), regiment.FirePolicy == RegimentFirePolicy.CloseRange ? "[KORT]" : "KORT"))
            regiment.SetFirePolicy(RegimentFirePolicy.CloseRange);
        x += buttonW + gap;
        if (GUI.Button(new Rect(x, y, buttonW, h), regiment.FirePolicy == RegimentFirePolicy.MediumRange ? "[MELLEM]" : "MELLEM"))
            regiment.SetFirePolicy(RegimentFirePolicy.MediumRange);
        x += buttonW + gap;
        if (GUI.Button(new Rect(x, y, buttonW, h), regiment.FirePolicy == RegimentFirePolicy.LongRange ? "[LANG]" : "LANG"))
            regiment.SetFirePolicy(RegimentFirePolicy.LongRange);
    }
}
