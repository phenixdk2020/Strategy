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

        // QA geography for the first vertical slice. Positions are deliberately
        // schematic; historical coordinate/OOB validation belongs to the research gate.
        AddNode("KOLDING", "Kolding", new Vector2(-48f, 40f), CampaignNation.Denmark, CampaignTerrainType.Urban, true, false);
        AddNode("HADERSLEV", "Haderslev", new Vector2(-35f, 23f), CampaignNation.Denmark, CampaignTerrainType.Rolling, true, false);
        AddNode("AABENRAA", "Aabenraa", new Vector2(-20f, 8f), CampaignNation.Denmark, CampaignTerrainType.Rolling, false, false);
        AddNode("DYBBOEL", "Dybbol", new Vector2(7f, 15f), CampaignNation.Denmark, CampaignTerrainType.Fortified, true, false);
        AddNode("SONDERBORG", "Sonderborg", new Vector2(25f, 16f), CampaignNation.Denmark, CampaignTerrainType.Urban, true, true);
        AddNode("FLENSBURG", "Flensburg", new Vector2(-10f, -12f), CampaignNation.Prussia, CampaignTerrainType.Urban, true, false);
        AddNode("SCHLESWIG", "Schleswig", new Vector2(-1f, -31f), CampaignNation.Prussia, CampaignTerrainType.Rolling, true, false);
        AddNode("RENDSBURG", "Rendsburg", new Vector2(12f, -48f), CampaignNation.Prussia, CampaignTerrainType.RiverCrossing, true, true);

        Link("KOLDING", "HADERSLEV");
        Link("HADERSLEV", "AABENRAA");
        Link("AABENRAA", "FLENSBURG");
        Link("AABENRAA", "DYBBOEL");
        Link("DYBBOEL", "SONDERBORG");
        Link("FLENSBURG", "SCHLESWIG");
        Link("SCHLESWIG", "RENDSBURG");
        Link("FLENSBURG", "DYBBOEL");

        CampaignFormationState danishNorth = AddFormation(
            "DK-1DIV",
            "1. Division",
            CampaignNation.Denmark,
            "HADERSLEV");
        danishNorth.Regiments.Add(MakeRegiment("DK-1REG", "1. Regiment", CampaignNation.Denmark, 620, 60, 100f, 100f, 55f, "QA Officer A"));
        danishNorth.Regiments.Add(MakeRegiment("DK-2REG", "2. Regiment", CampaignNation.Denmark, 600, 60, 100f, 100f, 50f, "QA Officer E"));

        CampaignFormationState danishEast = AddFormation(
            "DK-2DIV",
            "2. Division",
            CampaignNation.Denmark,
            "DYBBOEL");
        danishEast.Regiments.Add(MakeRegiment("DK-5REG", "5. Regiment", CampaignNation.Denmark, 585, 60, 100f, 100f, 42f, "QA Officer B"));
        danishEast.Regiments.Add(MakeRegiment("DK-6REG", "6. Regiment", CampaignNation.Denmark, 570, 60, 100f, 100f, 50f, "QA Officer F"));

        CampaignFormationState prussianNorth = AddFormation(
            "PR-I",
            "I. Angrebskolonne",
            CampaignNation.Prussia,
            "FLENSBURG");
        prussianNorth.Regiments.Add(MakeRegiment("PR-8REG", "8th Regiment", CampaignNation.Prussia, 610, 60, 100f, 100f, 65f, "QA Officer C"));
        prussianNorth.Regiments.Add(MakeRegiment("PR-12REG", "12th Regiment", CampaignNation.Prussia, 590, 60, 100f, 100f, 50f, "QA Officer G"));

        CampaignFormationState prussianSouth = AddFormation(
            "PR-II",
            "II. Angrebskolonne",
            CampaignNation.Prussia,
            "SCHLESWIG");
        prussianSouth.Regiments.Add(MakeRegiment("PR-18REG", "18th Regiment", CampaignNation.Prussia, 560, 60, 100f, 100f, 50f, "QA Officer D"));
        prussianSouth.Regiments.Add(MakeRegiment("PR-24REG", "24th Regiment", CampaignNation.Prussia, 575, 60, 100f, 100f, 50f, "QA Officer H"));

        Initialized = true;
        Debug.Log("CAMPAIGN-DIAG|SessionInitialized=True|Nodes=8|Formations=4|Date=1864-02-01 08:00|Geography=QA_SCHEMATIC");
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

    private static void AddNode(
        string id,
        string name,
        Vector2 mapPosition,
        CampaignNation controller,
        CampaignTerrainType terrain,
        bool hasDepot,
        bool hasBridge)
    {
        nodes[id] = new CampaignNodeState
        {
            Id = id,
            Name = name,
            MapPosition = mapPosition,
            Controller = controller,
            Terrain = terrain,
            HasDepot = hasDepot,
            HasBridge = hasBridge
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
