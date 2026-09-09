using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09l TEST - company/battalion OOB plus historical identity metadata.
// Regiment remains the tactical movement/combat owner. Battalion/company state is the
// persistent allocation layer for Brigade/Regimental Officer AI and later OOB Designer.
public sealed class PrototypeRegimentOOB09K : MonoBehaviour
{
    [Serializable]
    public sealed class CompanyState
    {
        public string Name;
        public int InitialStrength;
        public int CurrentStrength;
    }

    [Serializable]
    public sealed class BattalionState
    {
        public string Name;
        public List<CompanyState> Companies = new List<CompanyState>();

        public int InitialStrength
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Companies.Count; i++)
                    total += Companies[i].InitialStrength;
                return total;
            }
        }
    }

    public Regiment Regiment { get; private set; }
    public IReadOnlyList<BattalionState> Battalions => battalions;
    public int RegimentalStaffStrength { get; private set; }
    public int CompanyCount { get; private set; }

    public string OfficialName1864 { get; private set; }
    public string TraditionalName { get; private set; }
    public string HistoricalLineage { get; private set; }
    public string RegimentalRomanNumeral { get; private set; }
    public int AuthorizedStrength { get; private set; }
    public int PresentStrength => Regiment != null ? Regiment.CurrentStrength : 0;
    public string DisplayName => string.IsNullOrEmpty(OfficialName1864) ? (Regiment != null ? Regiment.RegimentName : name) : OfficialName1864;

    private readonly List<BattalionState> battalions = new List<BattalionState>();

    public void Build(Regiment regiment)
    {
        Regiment = regiment;
        battalions.Clear();

        if (regiment == null)
            return;

        ApplyHistoricalIdentity(regiment.RegimentName);
        AuthorizedStrength = regiment.InitialStrength;

        int battalionCount = regiment.Team == BattleTeam.Denmark ? 2 : 3;
        const int companiesPerBattalion = 4;
        CompanyCount = battalionCount * companiesPerBattalion;

        RegimentalStaffStrength = Mathf.Clamp(25, 0, Mathf.Max(0, regiment.InitialStrength - CompanyCount));
        int companyManpower = Mathf.Max(0, regiment.InitialStrength - RegimentalStaffStrength);
        int baseCompany = companyManpower / CompanyCount;
        int remainder = companyManpower % CompanyCount;
        int companyNumber = 1;

        for (int b = 0; b < battalionCount; b++)
        {
            BattalionState battalion = new BattalionState
            {
                Name = regiment.Team == BattleTeam.Prussia && b == 2
                    ? "Füsilier-Bataillon"
                    : Roman(b + 1) + " Bataljon"
            };

            for (int c = 0; c < companiesPerBattalion; c++)
            {
                int strength = baseCompany + (remainder > 0 ? 1 : 0);
                if (remainder > 0)
                    remainder--;

                battalion.Companies.Add(new CompanyState
                {
                    Name = companyNumber + ". Kompagni",
                    InitialStrength = strength,
                    CurrentStrength = strength
                });
                companyNumber++;
            }

            battalions.Add(battalion);
        }

        LogIdentity();
    }

    public void OverrideHistoricalIdentity(
        string officialName1864,
        string traditionalName,
        string lineage,
        string romanNumeral)
    {
        OfficialName1864 = officialName1864;
        TraditionalName = traditionalName;
        HistoricalLineage = lineage;
        RegimentalRomanNumeral = romanNumeral;
        LogIdentity();
    }

    private void LogIdentity()
    {
        if (Regiment == null)
            return;

        Debug.Log(
            "OOB-09L|Technical=" + Regiment.RegimentName +
            "|Official=" + OfficialName1864 +
            "|Tradition=" + (string.IsNullOrEmpty(TraditionalName) ? "None" : TraditionalName) +
            "|Strength=" + Regiment.InitialStrength +
            "|Battalions=" + battalions.Count +
            "|Companies=" + CompanyCount +
            "|Staff=" + RegimentalStaffStrength +
            "|CompanyData=True");
    }

    private void ApplyHistoricalIdentity(string regimentName)
    {
        OfficialName1864 = regimentName;
        TraditionalName = string.Empty;
        HistoricalLineage = string.Empty;
        RegimentalRomanNumeral = string.Empty;

        switch (regimentName)
        {
            case "1. Regiment":
                OfficialName1864 = "1. Infanteri-Regiment";
                TraditionalName = "Danske Livregiment";
                HistoricalLineage = "Danske Livregiment til Fods -> 1. Livregiment til Fods";
                RegimentalRomanNumeral = "I";
                break;
            case "5. Regiment":
                OfficialName1864 = "5. Infanteri-Regiment";
                TraditionalName = "Sjællandske Livregiment";
                HistoricalLineage = "Sjællandske Regiment -> Kronprinsens Regiment -> Kongens Regiment";
                RegimentalRomanNumeral = "V";
                break;
            case "8th Regiment":
                OfficialName1864 = "8th Regiment (Prussia QA)";
                RegimentalRomanNumeral = "8";
                break;
            case "18th Regiment":
                OfficialName1864 = "18th Regiment (Prussia QA)";
                RegimentalRomanNumeral = "18";
                break;
        }
    }

    private static string Roman(int value)
    {
        switch (value)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            default: return value.ToString();
        }
    }
}

[DefaultExecutionOrder(-12000)]
public sealed class PrototypeFullScaleOOBManager09K : MonoBehaviour
{
    private bool installed;
    private FieldInfo initialStrengthField;
    private FieldInfo currentStrengthField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFullScaleOOBManager09K>() != null)
            return;

        GameObject root = new GameObject("PrototypeFullScaleOOBManager_v000009l");
        root.AddComponent<PrototypeFullScaleOOBManager09K>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        initialStrengthField = typeof(Regiment).GetField("<InitialStrength>k__BackingField", flags);
        currentStrengthField = typeof(Regiment).GetField("<CurrentStrength>k__BackingField", flags);

        if (initialStrengthField == null || currentStrengthField == null)
        {
            Debug.LogError("OOB-09L|Installed=False|Reason=RegimentStrengthBackingFieldsNotFound");
            enabled = false;
        }
    }

    private void Update()
    {
        if (installed)
            return;

        PrototypeExpandedOOBManager expanded = Object.FindAnyObjectByType<PrototypeExpandedOOBManager>();
        if (expanded != null && expanded.enabled)
            expanded.enabled = false;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 4)
            return;

        // The technical 09k slot remains named 5. Regiment so the stable 1:1 renderer
        // keeps working. OOB identity overrides it to historical 11. Infanteri-Regiment.
        ApplyStrength("1. Regiment", 1540);
        ApplyStrength("5. Regiment", 1584);
        ApplyStrength("8th Regiment", 2460);
        ApplyStrength("18th Regiment", 2440);

        SetPose("1. Regiment", new Vector3(-122f, 0f, -68f), Quaternion.Euler(0f, 90f, 0f));
        SetPose("5. Regiment", new Vector3(-122f, 0f, 68f), Quaternion.Euler(0f, 90f, 0f));
        SetPose("8th Regiment", new Vector3(122f, 0f, -68f), Quaternion.Euler(0f, -90f, 0f));
        SetPose("18th Regiment", new Vector3(122f, 0f, 68f), Quaternion.Euler(0f, -90f, 0f));

        int configured = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !IsPilotRegiment(regiment.RegimentName))
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob == null)
                oob = regiment.gameObject.AddComponent<PrototypeRegimentOOB09K>();
            oob.Build(regiment);

            if (regiment.RegimentName == "5. Regiment")
            {
                oob.OverrideHistoricalIdentity(
                    "11. Infanteri-Regiment",
                    "Falstersk/Jysk tradition",
                    "Falsterske Regiment -> Aalborgske Infanteriregiment -> 3. Jyske Infanteriregiment",
                    "XI");
            }

            if (regiment.GetComponent<PrototypeOfficerObjective09K>() == null)
                regiment.gameObject.AddComponent<PrototypeOfficerObjective09K>();

            configured++;
        }

        installed = configured == 4;
        if (installed)
        {
            Debug.Log(
                "OOB-09L|Installed=True|Scenario=HistoricalDanish7BrigadeVsPrussianQABrigade|" +
                "Denmark=1.Infanteri-Regiment(1540)+11.Infanteri-Regiment(1584)|" +
                "Prussia=8th(2460)+18th(2440)|InfantryTotal=8024|" +
                "InternalLevels=Battalion+Company|StableTechnicalKeys=True");
        }
    }

    private void ApplyStrength(string unitName, int strength)
    {
        Regiment regiment = FindRegiment(unitName);
        if (regiment == null)
            return;

        initialStrengthField.SetValue(regiment, strength);
        currentStrengthField.SetValue(regiment, strength);
        Debug.Log("OOB-09L|TechnicalUnit=" + unitName + "|FullStrengthApplied=" + strength);
    }

    private static void SetPose(string unitName, Vector3 position, Quaternion rotation)
    {
        Regiment regiment = FindRegiment(unitName);
        if (regiment == null)
            return;

        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        regiment.transform.position = position;
        regiment.transform.rotation = rotation;
        regiment.SetFormation(RegimentFormation.Line);
    }

    private static bool IsPilotRegiment(string name)
    {
        return name == "1. Regiment" ||
               name == "5. Regiment" ||
               name == "8th Regiment" ||
               name == "18th Regiment";
    }

    public static Regiment FindRegiment(string unitName)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.RegimentName == unitName)
                return regiment;
        }

        return null;
    }
}
