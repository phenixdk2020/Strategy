using System;
using System.Collections.Generic;
using UnityEngine;

public enum CampaignNation
{
    Denmark,
    Prussia,
    Austria,
    SwedenNorway,
    RussianEmpire,
    GermanConfederation
}

public enum CampaignMapRegion
{
    Denmark,
    Sweden,
    Norway,
    Finland,
    Germany
}

public enum CampaignTerrainType
{
    Plains,
    Rolling,
    Forest,
    Urban,
    Fortified,
    RiverCrossing,
    Coastal,
    Mountain
}

[Serializable]
public sealed class CampaignNodeState
{
    public string Id;
    public string Name;
    public CampaignMapRegion Region;
    public double Latitude;
    public double Longitude;
    public Vector2 MapPosition;
    public CampaignNation Controller;
    public CampaignTerrainType Terrain;
    public bool HasDepot;
    public bool HasBridge;
    public bool HasPort;
    public bool HasRail;
    public readonly List<string> Links = new List<string>();
}

[Serializable]
public sealed class CampaignRegimentState
{
    public string Id;
    public string Name;
    public CampaignNation Nation;
    public int Strength;
    public int AmmunitionRoundsPerMan;
    public float Morale;
    public float Cohesion;
    public float Experience;
    public string OfficerProfileKey;
    public bool Routed;
    public bool Destroyed;

    public CampaignRegimentState Clone()
    {
        return new CampaignRegimentState
        {
            Id = Id,
            Name = Name,
            Nation = Nation,
            Strength = Strength,
            AmmunitionRoundsPerMan = AmmunitionRoundsPerMan,
            Morale = Morale,
            Cohesion = Cohesion,
            Experience = Experience,
            OfficerProfileKey = OfficerProfileKey,
            Routed = Routed,
            Destroyed = Destroyed
        };
    }
}

[Serializable]
public sealed class CampaignFormationState
{
    public string Id;
    public string Name;
    public CampaignNation Nation;
    public string CurrentNodeId;
    public readonly List<CampaignRegimentState> Regiments = new List<CampaignRegimentState>();
    public readonly List<string> Route = new List<string>();
    public int RouteIndex;
    public float LegProgressHours;
    public float LegTravelHours;
    public string LegFromNodeId;
    public string LegToNodeId;
    public bool IsMoving;
    public bool IsEngaged;
    public bool IsDestroyed;

    public int TotalStrength
    {
        get
        {
            int total = 0;
            foreach (CampaignRegimentState regiment in Regiments)
            {
                if (regiment != null && !regiment.Destroyed)
                    total += Mathf.Max(0, regiment.Strength);
            }
            return total;
        }
    }

    public int AverageAmmo
    {
        get
        {
            int total = 0;
            int count = 0;
            foreach (CampaignRegimentState regiment in Regiments)
            {
                if (regiment == null || regiment.Destroyed)
                    continue;
                total += Mathf.Max(0, regiment.AmmunitionRoundsPerMan);
                count++;
            }
            return count > 0 ? Mathf.RoundToInt(total / (float)count) : 0;
        }
    }
}

public sealed class CampaignBattleContext
{
    public string BattleId;
    public string NodeId;
    public string LocationName;
    public string AttackerFormationId;
    public string DefenderFormationId;
    public CampaignNation AttackerNation;
    public CampaignNation DefenderNation;
    public DateTime CampaignDateTime;
    public readonly List<CampaignRegimentState> AttackerRegiments = new List<CampaignRegimentState>();
    public readonly List<CampaignRegimentState> DefenderRegiments = new List<CampaignRegimentState>();
}

public sealed class CampaignBattleResult
{
    public string BattleId;
    public string NodeId;
    public CampaignNation Winner;
    public readonly List<CampaignRegimentState> RegimentResults = new List<CampaignRegimentState>();
}
