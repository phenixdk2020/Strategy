using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09k TEST - company/battalion OOB attached to each full-scale regiment.
// This is persistent tactical structure data for the prototype. Regiment remains the
// command/movement owner; battalions/companies become the future officer allocation layer.
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

    private readonly List<BattalionState> battalions = new List<BattalionState>();

    public void Build(Regiment regiment)
    {
        Regiment = regiment;
        battalions.Clear();

        if (regiment == null)
            return;

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

        Debug.Log(
            "OOB-09K|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Strength=" + regiment.InitialStrength +
            "|Battalions=" + battalionCount +
            "|Companies=" + CompanyCount +
            "|Staff=" + RegimentalStaffStrength +
            "|CompanyData=True");
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

        GameObject root = new GameObject("PrototypeFullScaleOOBManager_v000009k");
        root.AddComponent<PrototypeFullScaleOOBManager09K>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        initialStrengthField = typeof(Regiment).GetField("<InitialStrength>k__BackingField", flags);
        currentStrengthField = typeof(Regiment).GetField("<CurrentStrength>k__BackingField", flags);

        if (initialStrengthField == null || currentStrengthField == null)
        {
            Debug.LogError("OOB-09K|Installed=False|Reason=RegimentStrengthBackingFieldsNotFound");
            enabled = false;
        }
    }

    private void Update()
    {
        if (installed)
            return;

        // 09k returns the tactical scenario to the original four regiments. The old
        // expanded OOB manager would otherwise add four more 570-600 man QA regiments
        // before the full-scale pilot can establish the new baseline.
        PrototypeExpandedOOBManager expanded = Object.FindAnyObjectByType<PrototypeExpandedOOBManager>();
        if (expanded != null && expanded.enabled)
            expanded.enabled = false;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 4)
            return;

        ApplyStrength("1. Regiment", 1620);
        ApplyStrength("5. Regiment", 1587);
        ApplyStrength("8th Regiment", 2460);
        ApplyStrength("18th Regiment", 2440);

        int configured = 0;
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || !IsPilotRegiment(regiment.RegimentName))
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob == null)
                oob = regiment.gameObject.AddComponent<PrototypeRegimentOOB09K>();
            oob.Build(regiment);
            configured++;
        }

        installed = configured == 4;
        if (installed)
        {
            Debug.Log(
                "OOB-09K|Installed=True|Scenario=FourFullScaleRegiments|" +
                "Denmark=2|Prussia=2|ApproxInfantry=8107|" +
                "CommandLevel=Regiment|InternalLevels=Battalion+Company");
        }
    }

    private void ApplyStrength(string unitName, int strength)
    {
        Regiment regiment = FindRegiment(unitName);
        if (regiment == null)
            return;

        initialStrengthField.SetValue(regiment, strength);
        currentStrengthField.SetValue(regiment, strength);

        Debug.Log("OOB-09K|Unit=" + unitName + "|FullStrengthApplied=" + strength);
    }

    private static bool IsPilotRegiment(string name)
    {
        return name == "1. Regiment" ||
               name == "5. Regiment" ||
               name == "8th Regiment" ||
               name == "18th Regiment";
    }

    private static Regiment FindRegiment(string unitName)
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
