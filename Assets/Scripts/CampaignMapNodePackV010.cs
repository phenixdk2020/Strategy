using System;
using System.Reflection;
using UnityEngine;

// v00.00.10 campaign-map node expansion.
// Adds 18 additional strategic nodes to the existing 32-node CampaignSession
// network, giving the campaign map exactly 50 points without changing tactical AI.
//
// This is intentionally isolated on the campaign-map work branch. It uses the
// existing CampaignSession AddNodeGeo/Link helpers so projection, controller,
// terrain and route data remain consistent with the current campaign prototype.
public static class CampaignMapNodePackV010
{
    private const int ExpectedBaseNodeCount = 32;
    private const int AddedNodeCount = 18;
    private const int ExpectedTotalNodeCount = 50;

    private static MethodInfo addNodeGeoMethod;
    private static MethodInfo linkMethod;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallBeforeSceneLoad()
    {
        CampaignSession.EnsureInitialized();

        // Guard against duplicate installation if domain reload settings cause
        // runtime initialization to execute more than once.
        if (CampaignSession.GetNode("HJORRING") != null)
        {
            Debug.Log(string.Format(
                "CAMPAIGN-NODES|Pack=v010|AlreadyInstalled=True|Nodes={0}",
                CampaignSession.Nodes.Count));
            return;
        }

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        addNodeGeoMethod = typeof(CampaignSession).GetMethod("AddNodeGeo", flags);
        linkMethod = typeof(CampaignSession).GetMethod("Link", flags);

        if (addNodeGeoMethod == null || linkMethod == null)
        {
            Debug.LogError("CAMPAIGN-NODES|Pack=v010|Installed=False|Reason=CampaignSession helpers not found");
            return;
        }

        int baseCount = CampaignSession.Nodes.Count;
        if (baseCount != ExpectedBaseNodeCount)
        {
            Debug.LogWarning(string.Format(
                "CAMPAIGN-NODES|Pack=v010|BaseNodeCount={0}|Expected={1}|Action=Continue",
                baseCount,
                ExpectedBaseNodeCount));
        }

        AddDenmarkNodes();
        AddSwedenNodes();
        AddNorwayNodes();
        AddFinlandNodes();
        AddGermanyNodes();
        AddStrategicLinks();

        int total = CampaignSession.Nodes.Count;
        Debug.Log(string.Format(
            "CAMPAIGN-NODES|Pack=v010|Installed=True|Added={0}|Nodes={1}|Target={2}",
            total - baseCount,
            total,
            ExpectedTotalNodeCount));

        if (total != ExpectedTotalNodeCount)
        {
            Debug.LogWarning(string.Format(
                "CAMPAIGN-NODES|Pack=v010|Validation=False|Nodes={0}|Expected={1}|NominalAdded={2}",
                total,
                ExpectedTotalNodeCount,
                AddedNodeCount));
        }
    }

    private static void AddDenmarkNodes()
    {
        AddNode("HJORRING", "Hjorring", CampaignMapRegion.Denmark, 57.4642, 9.9823,
            CampaignNation.Denmark, CampaignTerrainType.Rolling, false, false, false, false);

        AddNode("RANDERS", "Randers", CampaignMapRegion.Denmark, 56.4607, 10.0364,
            CampaignNation.Denmark, CampaignTerrainType.Urban, true, true, true, true);

        AddNode("VIBORG", "Viborg", CampaignMapRegion.Denmark, 56.4520, 9.3963,
            CampaignNation.Denmark, CampaignTerrainType.Rolling, true, false, false, true);

        AddNode("HORSENS", "Horsens", CampaignMapRegion.Denmark, 55.8607, 9.8503,
            CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, false);

        AddNode("VEJLE", "Vejle", CampaignMapRegion.Denmark, 55.7113, 9.5364,
            CampaignNation.Denmark, CampaignTerrainType.Rolling, true, false, true, false);

        AddNode("ODENSE", "Odense", CampaignMapRegion.Denmark, 55.4038, 10.4024,
            CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, false);

        AddNode("NYBORG", "Nyborg", CampaignMapRegion.Denmark, 55.3127, 10.7896,
            CampaignNation.Denmark, CampaignTerrainType.Coastal, true, false, true, false);

        AddNode("KORSOR", "Korsor", CampaignMapRegion.Denmark, 55.3299, 11.1386,
            CampaignNation.Denmark, CampaignTerrainType.Coastal, true, false, true, true);

        AddNode("ROSKILDE", "Roskilde", CampaignMapRegion.Denmark, 55.6415, 12.0803,
            CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, false, true);
    }

    private static void AddSwedenNodes()
    {
        AddNode("HELSINGBORG", "Helsingborg", CampaignMapRegion.Sweden, 56.0465, 12.6945,
            CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, false);

        AddNode("HALMSTAD", "Halmstad", CampaignMapRegion.Sweden, 56.6745, 12.8578,
            CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, false);

        AddNode("JONKOPING", "Jonkoping", CampaignMapRegion.Sweden, 57.7826, 14.1618,
            CampaignNation.SwedenNorway, CampaignTerrainType.Rolling, true, false, false, true);
    }

    private static void AddNorwayNodes()
    {
        // Halden was known as Fredrikshald during the 1864 period.
        AddNode("FREDRIKSHALD", "Fredrikshald", CampaignMapRegion.Norway, 59.1226, 11.3871,
            CampaignNation.SwedenNorway, CampaignTerrainType.Fortified, true, false, true, false);

        AddNode("DRAMMEN", "Drammen", CampaignMapRegion.Norway, 59.7439, 10.2045,
            CampaignNation.SwedenNorway, CampaignTerrainType.Rolling, true, true, true, false);
    }

    private static void AddFinlandNodes()
    {
        // Swedish-language period names are used to match Helsingfors/Abo/Tammerfors/Vasa.
        AddNode("BJORNEBORG", "Bjorneborg", CampaignMapRegion.Finland, 61.4851, 21.7974,
            CampaignNation.RussianEmpire, CampaignTerrainType.Coastal, true, false, true, false);

        AddNode("TAVASTEHUS", "Tavastehus", CampaignMapRegion.Finland, 60.9959, 24.4643,
            CampaignNation.RussianEmpire, CampaignTerrainType.Forest, true, false, false, true);
    }

    private static void AddGermanyNodes()
    {
        AddNode("RENDSBURG", "Rendsburg", CampaignMapRegion.Germany, 54.3060, 9.6631,
            CampaignNation.GermanConfederation, CampaignTerrainType.Fortified, true, true, false, true);

        AddNode("ALTONA", "Altona", CampaignMapRegion.Germany, 53.5500, 9.9330,
            CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, true, true);
    }

    private static void AddStrategicLinks()
    {
        // Denmark: denser north-south Jutland corridor plus the Great Belt chain.
        Link("HJORRING", "AALBORG");
        Link("AALBORG", "RANDERS");
        Link("RANDERS", "AARHUS");
        Link("RANDERS", "VIBORG");
        Link("VIBORG", "AARHUS");
        Link("AARHUS", "HORSENS");
        Link("HORSENS", "VEJLE");
        Link("VEJLE", "FREDERICIA");
        Link("VEJLE", "KOLDING");
        Link("FREDERICIA", "ODENSE");
        Link("ODENSE", "NYBORG");
        Link("NYBORG", "KORSOR");
        Link("KORSOR", "ROSKILDE");
        Link("ROSKILDE", "CPH");

        // Sweden: west-coast axis and an inland branch through Jonkoping.
        Link("MALMO", "HELSINGBORG");
        Link("HELSINGBORG", "HALMSTAD");
        Link("HALMSTAD", "GOTEBORG");
        Link("MALMO", "JONKOPING");
        Link("JONKOPING", "GOTEBORG");
        Link("JONKOPING", "STOCKHOLM");

        // Norway: southeastern defensive/access network around Christiania.
        Link("FREDRIKSHALD", "CHRISTIANIA");
        Link("FREDRIKSHALD", "GOTEBORG");
        Link("DRAMMEN", "CHRISTIANIA");
        Link("DRAMMEN", "KRISTIANSAND");

        // Finland: coastal and first-generation inland transport corridors.
        Link("ABO", "BJORNEBORG");
        Link("BJORNEBORG", "VASA");
        Link("ABO", "TAVASTEHUS");
        Link("TAVASTEHUS", "TAMMERFORS");
        Link("TAVASTEHUS", "HELSINGFORS");

        // Schleswig-Holstein / Hamburg approach network.
        Link("SCHLESWIG", "RENDSBURG");
        Link("RENDSBURG", "KIEL");
        Link("RENDSBURG", "ALTONA");
        Link("ALTONA", "HAMBURG");
    }

    private static void AddNode(
        string id,
        string name,
        CampaignMapRegion region,
        double latitude,
        double longitude,
        CampaignNation controller,
        CampaignTerrainType terrain,
        bool hasDepot,
        bool hasBridge,
        bool hasPort,
        bool hasRail)
    {
        addNodeGeoMethod.Invoke(null, new object[]
        {
            id,
            name,
            region,
            latitude,
            longitude,
            controller,
            terrain,
            hasDepot,
            hasBridge,
            hasPort,
            hasRail
        });
    }

    private static void Link(string a, string b)
    {
        linkMethod.Invoke(null, new object[] { a, b });
    }
}
