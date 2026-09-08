using System;
using System.Collections.Generic;
using UnityEngine;

public static class CampaignSession
{
    private static readonly Dictionary<string, CampaignNodeState> nodes =
        new Dictionary<string, CampaignNodeState>(StringComparer.Ordinal);

    private static readonly Dictionary<string, CampaignFormationState> formations =
        new Dictionary<string, CampaignFormationState>(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, CampaignNodeState> Nodes => nodes;
    public static IReadOnlyDictionary<string, CampaignFormationState> Formations => formations;

    public static DateTime CurrentDateTime { get; private set; }
    public static CampaignBattleContext PendingBattleContext { get; private set; }
    public static CampaignBattleResult LastBattleResult { get; private set; }
    public static bool Initialized { get; private set; }
    public static bool IsCampaignBattleActive => PendingBattleContext != null;

    public static void EnsureInitialized()
    {
        if (Initialized)
            return;

        nodes.Clear();
        formations.Clear();
        PendingBattleContext = null;
        LastBattleResult = null;
        CurrentDateTime = new DateTime(1864, 2, 1, 8, 0, 0);

        BuildGeographicNodeNetwork();
        BuildInitialQaFormations();

        Initialized = true;
        Debug.Log(string.Format(
            "CAMPAIGN-DIAG|SessionInitialized=True|Nodes={0}|Formations={1}|Date=1864-02-01 08:00|Coverage=Denmark+Sweden+Norway+Finland+Germany|Projection=LatLon",
            nodes.Count,
            formations.Count));
    }

    private static void BuildGeographicNodeNetwork()
    {
        // Geography is based on real latitude/longitude positions. The first MVP
        // deliberately uses a sparse strategic network; coastlines, DEM relief,
        // roads, rail and rivers are separate map layers built on the same projection.

        // DENMARK
        AddNodeGeo("CPH", "Kobenhavn", CampaignMapRegion.Denmark, 55.6761, 12.5683, CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("AALBORG", "Aalborg", CampaignMapRegion.Denmark, 57.0488, 9.9217, CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("AARHUS", "Aarhus", CampaignMapRegion.Denmark, 56.1629, 10.2039, CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("FREDERICIA", "Fredericia", CampaignMapRegion.Denmark, 55.5657, 9.7526, CampaignNation.Denmark, CampaignTerrainType.Fortified, true, true, true, true);
        AddNodeGeo("KOLDING", "Kolding", CampaignMapRegion.Denmark, 55.4904, 9.4722, CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, false, true);
        AddNodeGeo("HADERSLEV", "Haderslev", CampaignMapRegion.Denmark, 55.2494, 9.4877, CampaignNation.Denmark, CampaignTerrainType.Rolling, true, false, true, false);
        AddNodeGeo("DYBBOEL", "Dybbol", CampaignMapRegion.Denmark, 54.9108, 9.7368, CampaignNation.Denmark, CampaignTerrainType.Fortified, true, false, false, false);
        AddNodeGeo("SONDERBORG", "Sonderborg", CampaignMapRegion.Denmark, 54.9093, 9.7920, CampaignNation.Denmark, CampaignTerrainType.Urban, true, true, true, false);

        // SWEDEN
        AddNodeGeo("MALMO", "Malmo", CampaignMapRegion.Sweden, 55.6050, 13.0038, CampaignNation.SwedenNorway, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("GOTEBORG", "Goteborg", CampaignMapRegion.Sweden, 57.7089, 11.9746, CampaignNation.SwedenNorway, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("KARLSKRONA", "Karlskrona", CampaignMapRegion.Sweden, 56.1612, 15.5869, CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, true);
        AddNodeGeo("STOCKHOLM", "Stockholm", CampaignMapRegion.Sweden, 59.3293, 18.0686, CampaignNation.SwedenNorway, CampaignTerrainType.Urban, true, false, true, true);

        // NORWAY - Christiania is the contemporary 1864 city name for Oslo.
        AddNodeGeo("CHRISTIANIA", "Christiania", CampaignMapRegion.Norway, 59.9139, 10.7522, CampaignNation.SwedenNorway, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("KRISTIANSAND", "Kristiansand", CampaignMapRegion.Norway, 58.1467, 7.9956, CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, false);
        AddNodeGeo("BERGEN", "Bergen", CampaignMapRegion.Norway, 60.3913, 5.3221, CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, false);
        AddNodeGeo("TRONDHEIM", "Trondheim", CampaignMapRegion.Norway, 63.4305, 10.3951, CampaignNation.SwedenNorway, CampaignTerrainType.Coastal, true, false, true, false);

        // FINLAND - in 1864 Finland is part of the Russian Empire.
        AddNodeGeo("HELSINGFORS", "Helsingfors", CampaignMapRegion.Finland, 60.1699, 24.9384, CampaignNation.RussianEmpire, CampaignTerrainType.Urban, true, false, true, false);
        AddNodeGeo("ABO", "Abo", CampaignMapRegion.Finland, 60.4518, 22.2666, CampaignNation.RussianEmpire, CampaignTerrainType.Coastal, true, false, true, false);
        AddNodeGeo("TAMMERFORS", "Tammerfors", CampaignMapRegion.Finland, 61.4978, 23.7610, CampaignNation.RussianEmpire, CampaignTerrainType.Forest, true, false, false, false);
        AddNodeGeo("VASA", "Vasa", CampaignMapRegion.Finland, 63.0951, 21.6165, CampaignNation.RussianEmpire, CampaignTerrainType.Coastal, true, false, true, false);

        // GERMANY is the geographic map region. Political control remains 1864-aware:
        // Prussia is explicit, while non-Prussian German locations use GermanConfederation.
        AddNodeGeo("FLENSBURG", "Flensburg", CampaignMapRegion.Germany, 54.7937, 9.4469, CampaignNation.Denmark, CampaignTerrainType.Urban, true, false, true, false);
        AddNodeGeo("SCHLESWIG", "Schleswig", CampaignMapRegion.Germany, 54.5216, 9.5586, CampaignNation.Denmark, CampaignTerrainType.Rolling, true, false, false, false);
        AddNodeGeo("KIEL", "Kiel", CampaignMapRegion.Germany, 54.3233, 10.1228, CampaignNation.GermanConfederation, CampaignTerrainType.Coastal, true, false, true, true);
        AddNodeGeo("LUBECK", "Lubeck", CampaignMapRegion.Germany, 53.8655, 10.6866, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, true, true);
        AddNodeGeo("HAMBURG", "Hamburg", CampaignMapRegion.Germany, 53.5511, 9.9937, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, true, true, true);
        AddNodeGeo("ROSTOCK", "Rostock", CampaignMapRegion.Germany, 54.0924, 12.0991, CampaignNation.GermanConfederation, CampaignTerrainType.Coastal, true, false, true, true);
        AddNodeGeo("HANNOVER", "Hannover", CampaignMapRegion.Germany, 52.3759, 9.7320, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, false, true);
        AddNodeGeo("BERLIN", "Berlin", CampaignMapRegion.Germany, 52.5200, 13.4050, CampaignNation.Prussia, CampaignTerrainType.Urban, true, false, false, true);
        AddNodeGeo("DRESDEN", "Dresden", CampaignMapRegion.Germany, 51.0504, 13.7373, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, false, true);
        AddNodeGeo("FRANKFURT", "Frankfurt", CampaignMapRegion.Germany, 50.1109, 8.6821, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, false, true);
        AddNodeGeo("COLOGNE", "Koln", CampaignMapRegion.Germany, 50.9375, 6.9603, CampaignNation.Prussia, CampaignTerrainType.Urban, true, true, false, true);
        AddNodeGeo("MUNICH", "Munchen", CampaignMapRegion.Germany, 48.1351, 11.5820, CampaignNation.GermanConfederation, CampaignTerrainType.Urban, true, false, false, true);

        // Strategic land/rail corridor links. They are not a claim that every route
        // had identical military capacity in 1864; capacity/speed is a later data layer.
        Link("AALBORG", "AARHUS");
        Link("AARHUS", "FREDERICIA");
        Link("FREDERICIA", "KOLDING");
        Link("KOLDING", "HADERSLEV");
        Link("HADERSLEV", "FLENSBURG");
        Link("FLENSBURG", "SCHLESWIG");
        Link("SCHLESWIG", "KIEL");
        Link("HADERSLEV", "DYBBOEL");
        Link("DYBBOEL", "SONDERBORG");
        Link("FREDERICIA", "CPH");

        Link("MALMO", "GOTEBORG");
        Link("MALMO", "KARLSKRONA");
        Link("KARLSKRONA", "STOCKHOLM");
        Link("GOTEBORG", "STOCKHOLM");

        Link("KRISTIANSAND", "CHRISTIANIA");
        Link("CHRISTIANIA", "BERGEN");
        Link("CHRISTIANIA", "TRONDHEIM");

        Link("ABO", "HELSINGFORS");
        Link("ABO", "TAMMERFORS");
        Link("TAMMERFORS", "VASA");
        Link("TAMMERFORS", "HELSINGFORS");

        Link("KIEL", "HAMBURG");
        Link("KIEL", "LUBECK");
        Link("LUBECK", "ROSTOCK");
        Link("ROSTOCK", "BERLIN");
        Link("HAMBURG", "HANNOVER");
        Link("HANNOVER", "BERLIN");
        Link("HANNOVER", "FRANKFURT");
        Link("FRANKFURT", "COLOGNE");
        Link("FRANKFURT", "MUNICH");
        Link("BERLIN", "DRESDEN");
        Link("DRESDEN", "MUNICH");

        // Sea/strategic transfer links are represented as links now; transport type,
        // naval control and embarkation cost are added before they become unrestricted.
        Link("CPH", "MALMO");
        Link("GOTEBORG", "CHRISTIANIA");
        Link("STOCKHOLM", "ABO");
        Link("STOCKHOLM", "HELSINGFORS");
        Link("KIEL", "CPH");
    }

    private static void BuildInitialQaFormations()
    {
        CampaignFormationState danishJutland = AddFormation(
            "DK-1DIV",
            "1. Division",
            CampaignNation.Denmark,
            "HADERSLEV");
        danishJutland.Regiments.Add(MakeRegiment("DK-1REG", "1. Regiment", CampaignNation.Denmark, 620, 60, 100f, 100f, 55f, "QA Officer A"));
        danishJutland.Regiments.Add(MakeRegiment("DK-2REG", "2. Regiment", CampaignNation.Denmark, 600, 60, 100f, 100f, 50f, "QA Officer E"));

        CampaignFormationState danishDybbol = AddFormation(
            "DK-2DIV",
            "2. Division",
            CampaignNation.Denmark,
            "DYBBOEL");
        danishDybbol.Regiments.Add(MakeRegiment("DK-5REG", "5. Regiment", CampaignNation.Denmark, 585, 60, 100f, 100f, 42f, "QA Officer B"));
        danishDybbol.Regiments.Add(MakeRegiment("DK-6REG", "6. Regiment", CampaignNation.Denmark, 570, 60, 100f, 100f, 50f, "QA Officer F"));

        CampaignFormationState prussianNorth = AddFormation(
            "PR-I",
            "I. Angrebskolonne",
            CampaignNation.Prussia,
            "KIEL");
        prussianNorth.Regiments.Add(MakeRegiment("PR-8REG", "8th Regiment", CampaignNation.Prussia, 610, 60, 100f, 100f, 65f, "QA Officer C"));
        prussianNorth.Regiments.Add(MakeRegiment("PR-12REG", "12th Regiment", CampaignNation.Prussia, 590, 60, 100f, 100f, 50f, "QA Officer G"));

        CampaignFormationState prussianReserve = AddFormation(
            "PR-II",
            "II. Angrebskolonne",
            CampaignNation.Prussia,
            "HAMBURG");
        prussianReserve.Regiments.Add(MakeRegiment("PR-18REG", "18th Regiment", CampaignNation.Prussia, 560, 60, 100f, 100f, 50f, "QA Officer D"));
        prussianReserve.Regiments.Add(MakeRegiment("PR-24REG", "24th Regiment", CampaignNation.Prussia, 575, 60, 100f, 100f, 50f, "QA Officer H"));
    }

    public static void ResetCampaign()
    {
        Initialized = false;
        EnsureInitialized();
    }

    public static void AdvanceHours(float hours)
    {
        EnsureInitialized();
        if (hours <= 0f)
            return;
        CurrentDateTime = CurrentDateTime.AddHours(hours);
    }

    public static CampaignNodeState GetNode(string id)
    {
        EnsureInitialized();
        return id != null && nodes.TryGetValue(id, out CampaignNodeState node) ? node : null;
    }

    public static CampaignFormationState GetFormation(string id)
    {
        EnsureInitialized();
        return id != null && formations.TryGetValue(id, out CampaignFormationState formation) ? formation : null;
    }

    public static CampaignBattleContext PrepareBattle(
        CampaignFormationState attacker,
        CampaignFormationState defender,
        CampaignNodeState location)
    {
        if (attacker == null || defender == null || location == null)
            return null;

        CampaignBattleContext context = new CampaignBattleContext
        {
            BattleId = "BATTLE-" + CurrentDateTime.ToString("yyyyMMdd-HHmmss") + "-" + attacker.Id + "-" + defender.Id,
            NodeId = location.Id,
            LocationName = location.Name,
            AttackerFormationId = attacker.Id,
            DefenderFormationId = defender.Id,
            AttackerNation = attacker.Nation,
            DefenderNation = defender.Nation,
            CampaignDateTime = CurrentDateTime
        };

        foreach (CampaignRegimentState regiment in attacker.Regiments)
        {
            if (regiment != null && !regiment.Destroyed && regiment.Strength > 0)
                context.AttackerRegiments.Add(regiment.Clone());
        }

        foreach (CampaignRegimentState regiment in defender.Regiments)
        {
            if (regiment != null && !regiment.Destroyed && regiment.Strength > 0)
                context.DefenderRegiments.Add(regiment.Clone());
        }

        attacker.IsEngaged = true;
        defender.IsEngaged = true;
        attacker.IsMoving = false;
        defender.IsMoving = false;
        PendingBattleContext = context;

        Debug.Log(string.Format(
            "CAMPAIGN-BATTLE|Prepared=True|Id={0}|Location={1}|Attacker={2}|Defender={3}|AtkMen={4}|DefMen={5}",
            context.BattleId,
            context.LocationName,
            attacker.Name,
            defender.Name,
            attacker.TotalStrength,
            defender.TotalStrength));

        return context;
    }

    public static CampaignBattleResult CompleteBattleFromTactical(
        IReadOnlyList<Regiment> tacticalRegiments,
        CampaignNation winner)
    {
        CampaignBattleContext context = PendingBattleContext;
        if (context == null)
            return null;

        CampaignBattleResult result = new CampaignBattleResult
        {
            BattleId = context.BattleId,
            NodeId = context.NodeId,
            Winner = winner
        };

        if (tacticalRegiments != null)
        {
            foreach (Regiment regiment in tacticalRegiments)
            {
                if (regiment == null)
                    continue;

                CampaignRegimentState state = FindContextRegiment(context, regiment.RegimentName);
                if (state == null)
                    continue;

                CampaignRegimentState updated = state.Clone();
                updated.Strength = Mathf.Max(0, regiment.CurrentStrength);
                updated.Morale = Mathf.Clamp(regiment.Morale, 0f, 100f);
                updated.Cohesion = Mathf.Clamp(regiment.Cohesion, 0f, 100f);
                updated.Experience = Mathf.Clamp(regiment.Experience, 0f, 100f);
                updated.AmmunitionRoundsPerMan = Mathf.Max(0, PrototypeCombatStatusManager.GetAmmunitionRoundsPerMan(regiment));
                updated.Routed = regiment.IsRouted;
                updated.Destroyed = updated.Strength <= 0;
                result.RegimentResults.Add(updated);
            }
        }

        ApplyBattleResult(result);
        return result;
    }

    private static void ApplyBattleResult(CampaignBattleResult result)
    {
        CampaignBattleContext context = PendingBattleContext;
        if (context == null || result == null)
            return;

        CampaignFormationState attacker = GetFormation(context.AttackerFormationId);
        CampaignFormationState defender = GetFormation(context.DefenderFormationId);

        ApplyRegimentResults(attacker, result.RegimentResults);
        ApplyRegimentResults(defender, result.RegimentResults);

        if (attacker != null)
        {
            attacker.IsEngaged = false;
            attacker.IsDestroyed = attacker.TotalStrength <= 0;
            ClearRoute(attacker);
        }

        if (defender != null)
        {
            defender.IsEngaged = false;
            defender.IsDestroyed = defender.TotalStrength <= 0;
            ClearRoute(defender);
        }

        CampaignNodeState location = GetNode(result.NodeId);
        if (location != null)
            location.Controller = result.Winner;

        LastBattleResult = result;
        PendingBattleContext = null;

        Debug.Log(string.Format(
            "CAMPAIGN-BATTLE|Completed=True|Id={0}|Winner={1}|Location={2}",
            result.BattleId,
            result.Winner,
            result.NodeId));
    }

    private static void ApplyRegimentResults(
        CampaignFormationState formation,
        List<CampaignRegimentState> results)
    {
        if (formation == null || results == null)
            return;

        foreach (CampaignRegimentState regiment in formation.Regiments)
        {
            if (regiment == null)
                continue;

            CampaignRegimentState result = results.Find(r => r != null && r.Id == regiment.Id);
            if (result == null)
                result = results.Find(r => r != null && r.Name == regiment.Name && r.Nation == regiment.Nation);
            if (result == null)
                continue;

            regiment.Strength = result.Strength;
            regiment.AmmunitionRoundsPerMan = result.AmmunitionRoundsPerMan;
            regiment.Morale = result.Morale;
            regiment.Cohesion = result.Cohesion;
            regiment.Experience = result.Experience;
            regiment.Routed = result.Routed;
            regiment.Destroyed = result.Destroyed;
        }
    }

    private static CampaignRegimentState FindContextRegiment(
        CampaignBattleContext context,
        string regimentName)
    {
        if (context == null || string.IsNullOrEmpty(regimentName))
            return null;

        CampaignRegimentState found = context.AttackerRegiments.Find(r => r != null && r.Name == regimentName);
        if (found != null)
            return found;
        return context.DefenderRegiments.Find(r => r != null && r.Name == regimentName);
    }

    private static void ClearRoute(CampaignFormationState formation)
    {
        formation.Route.Clear();
        formation.RouteIndex = 0;
        formation.LegProgressHours = 0f;
        formation.LegTravelHours = 0f;
        formation.LegFromNodeId = null;
        formation.LegToNodeId = null;
        formation.IsMoving = false;
    }

    private static CampaignFormationState AddFormation(
        string id,
        string name,
        CampaignNation nation,
        string currentNodeId)
    {
        CampaignFormationState formation = new CampaignFormationState
        {
            Id = id,
            Name = name,
            Nation = nation,
            CurrentNodeId = currentNodeId
        };
        formations[id] = formation;
        return formation;
    }

    private static CampaignRegimentState MakeRegiment(
        string id,
        string name,
        CampaignNation nation,
        int strength,
        int ammo,
        float morale,
        float cohesion,
        float experience,
        string officerProfileKey)
    {
        return new CampaignRegimentState
        {
            Id = id,
            Name = name,
            Nation = nation,
            Strength = strength,
            AmmunitionRoundsPerMan = ammo,
            Morale = morale,
            Cohesion = cohesion,
            Experience = experience,
            OfficerProfileKey = officerProfileKey
        };
    }

    private static void AddNodeGeo(
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
        nodes[id] = new CampaignNodeState
        {
            Id = id,
            Name = name,
            Region = region,
            Latitude = latitude,
            Longitude = longitude,
            MapPosition = CampaignGeoProjection.Project(latitude, longitude),
            Controller = controller,
            Terrain = terrain,
            HasDepot = hasDepot,
            HasBridge = hasBridge,
            HasPort = hasPort,
            HasRail = hasRail
        };
    }

    private static void Link(string a, string b)
    {
        if (!nodes.TryGetValue(a, out CampaignNodeState nodeA) ||
            !nodes.TryGetValue(b, out CampaignNodeState nodeB))
        {
            return;
        }

        if (!nodeA.Links.Contains(b))
            nodeA.Links.Add(b);
        if (!nodeB.Links.Contains(a))
            nodeB.Links.Add(a);
    }
}
