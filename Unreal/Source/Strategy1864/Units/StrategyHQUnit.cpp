#include "StrategyHQUnit.h"

AStrategyHQUnit::AStrategyHQUnit()
{
    Echelon = EStrategyEchelon::Headquarters;
    ApplyHQLevelDefaults();
}

void AStrategyHQUnit::ApplyHQLevelDefaults()
{
    switch (HQLevel)
    {
        case EStrategyHQLevel::Battalion:
            Echelon = EStrategyEchelon::Battalion;
            CommandInnerRadius = 32000.0f;
            CommandOuterRadius = 45000.0f;
            break;

        case EStrategyHQLevel::Regiment:
            Echelon = EStrategyEchelon::Regiment;
            CommandInnerRadius = 80000.0f;
            CommandOuterRadius = 110000.0f;
            break;

        case EStrategyHQLevel::Brigade:
            Echelon = EStrategyEchelon::Brigade;
            CommandInnerRadius = 135000.0f;
            CommandOuterRadius = 185000.0f;
            break;

        case EStrategyHQLevel::Division:
            Echelon = EStrategyEchelon::Division;
            CommandInnerRadius = 210000.0f;
            CommandOuterRadius = 285000.0f;
            break;

        default:
            break;
    }
}
