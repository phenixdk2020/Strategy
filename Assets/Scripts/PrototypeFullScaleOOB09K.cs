using System;
using System.Collections.Generic;
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

[DefaultExecutionOrder(-10500)]
public sealed class PrototypeFullScaleOOBManager09K : MonoBehaviour
{
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeFullScaleOOBManager09K>() != null)
            return;

        GameObject root = new GameObject("PrototypeFullScaleOOBManager_v000009k");
        root.AddComponent<PrototypeFullScaleOOBManager09K>();
    }

    private void Update()
    {
        if (installed)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 4)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            PrototypeRegimentOOB09K oob = regiment.GetComponent<PrototypeRegimentOOB09K>();
            if (oob == null)
                oob = regiment.gameObject.AddComponent<PrototypeRegimentOOB09K>();
            oob.Build(regiment);
        }

        installed = true;
        Debug.Log("OOB-09K|Installed=True|Scale=FullStrength|CommandLevel=Regiment|InternalLevels=Battalion+Company");
    }
}
