using System;
using UnityEngine;

[Serializable]
public sealed class PrototypeUniformProfile09H
{
    public Color CoatColor;
    public Color TrouserColor;
    public Color HeadgearColor;
    public Color TrimColor;
    public Color StrapColor;
    public Color EquipmentColor;
    public Color SkinColor;
    public Color FlagPrimaryColor;
    public Color FlagSecondaryColor;
    public Color RibbonColor;

    public PrototypeUniformProfile09H Clone()
    {
        return new PrototypeUniformProfile09H
        {
            CoatColor = CoatColor,
            TrouserColor = TrouserColor,
            HeadgearColor = HeadgearColor,
            TrimColor = TrimColor,
            StrapColor = StrapColor,
            EquipmentColor = EquipmentColor,
            SkinColor = SkinColor,
            FlagPrimaryColor = FlagPrimaryColor,
            FlagSecondaryColor = FlagSecondaryColor,
            RibbonColor = RibbonColor
        };
    }

    public static PrototypeUniformProfile09H CreateFactionDefault(BattleTeam team)
    {
        if (team == BattleTeam.Denmark)
        {
            return new PrototypeUniformProfile09H
            {
                CoatColor = new Color(0.12f, 0.22f, 0.38f),
                TrouserColor = new Color(0.55f, 0.57f, 0.60f),
                HeadgearColor = new Color(0.08f, 0.12f, 0.19f),
                TrimColor = new Color(0.68f, 0.08f, 0.09f),
                StrapColor = new Color(0.86f, 0.83f, 0.75f),
                EquipmentColor = new Color(0.12f, 0.08f, 0.05f),
                SkinColor = new Color(0.72f, 0.56f, 0.43f),
                FlagPrimaryColor = new Color(0.75f, 0.055f, 0.075f),
                FlagSecondaryColor = new Color(0.95f, 0.95f, 0.91f),
                RibbonColor = new Color(0.88f, 0.72f, 0.16f)
            };
        }

        return new PrototypeUniformProfile09H
        {
            CoatColor = new Color(0.10f, 0.14f, 0.24f),
            TrouserColor = new Color(0.42f, 0.44f, 0.47f),
            HeadgearColor = new Color(0.055f, 0.055f, 0.065f),
            TrimColor = new Color(0.88f, 0.88f, 0.84f),
            StrapColor = new Color(0.76f, 0.75f, 0.70f),
            EquipmentColor = new Color(0.08f, 0.065f, 0.05f),
            SkinColor = new Color(0.72f, 0.56f, 0.43f),
            FlagPrimaryColor = new Color(0.91f, 0.89f, 0.81f),
            FlagSecondaryColor = new Color(0.065f, 0.065f, 0.075f),
            RibbonColor = new Color(0.42f, 0.56f, 0.80f)
        };
    }

    // Compatibility helpers used by the 09i visual pass. Keep faction colour
    // definitions centralized in CreateFactionDefault so defaults cannot drift.
    public static PrototypeUniformProfile09H CreateDenmarkDefault()
    {
        return CreateFactionDefault(BattleTeam.Denmark);
    }

    public static PrototypeUniformProfile09H CreatePrussiaDefault()
    {
        return CreateFactionDefault(BattleTeam.Prussia);
    }

    public static PrototypeUniformProfile09H CreateRegimentDefault(Regiment regiment)
    {
        PrototypeUniformProfile09H profile = CreateFactionDefault(regiment.Team);
        string name = regiment.RegimentName ?? string.Empty;

        if (name == "1. Regiment")
        {
            profile.TrimColor = new Color(0.74f, 0.09f, 0.10f);
            profile.RibbonColor = new Color(0.94f, 0.73f, 0.14f);
        }
        else if (name == "2. Regiment")
        {
            profile.TrimColor = new Color(0.83f, 0.50f, 0.12f);
            profile.RibbonColor = new Color(0.82f, 0.55f, 0.12f);
        }
        else if (name == "5. Regiment")
        {
            profile.CoatColor = new Color(0.10f, 0.18f, 0.33f);
            profile.TrimColor = new Color(0.74f, 0.62f, 0.13f);
            profile.RibbonColor = new Color(0.78f, 0.66f, 0.18f);
        }
        else if (name == "6. Regiment")
        {
            profile.TrimColor = new Color(0.42f, 0.56f, 0.72f);
            profile.RibbonColor = new Color(0.48f, 0.62f, 0.80f);
        }
        else if (name == "8th Regiment")
        {
            profile.TrimColor = new Color(0.88f, 0.88f, 0.84f);
            profile.RibbonColor = new Color(0.72f, 0.70f, 0.18f);
        }
        else if (name == "12th Regiment")
        {
            profile.CoatColor = new Color(0.09f, 0.13f, 0.23f);
            profile.TrimColor = new Color(0.72f, 0.16f, 0.13f);
            profile.RibbonColor = new Color(0.72f, 0.20f, 0.17f);
        }
        else if (name == "18th Regiment")
        {
            profile.CoatColor = new Color(0.075f, 0.11f, 0.20f);
            profile.TrimColor = new Color(0.76f, 0.76f, 0.72f);
            profile.RibbonColor = new Color(0.39f, 0.53f, 0.78f);
        }
        else if (name == "24th Regiment")
        {
            profile.TrimColor = new Color(0.86f, 0.58f, 0.14f);
            profile.RibbonColor = new Color(0.88f, 0.62f, 0.16f);
        }

        return profile;
    }
}
