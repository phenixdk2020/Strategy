#include "StrategyHQUnit.h"

#include "../Formations/StrategyParentFormationPlannerComponent.h"
#include "../Orders/StrategyOrderComponent.h"

AStrategyHQUnit::AStrategyHQUnit()
{
    Echelon = EStrategyEchelon::Headquarters;
    FormationPlanner = CreateDefaultSubobject<UStrategyParentFormationPlannerComponent>(TEXT("FormationPlanner"));
    ApplyHQLevelDefaults();
}

void AStrategyHQUnit::BeginPlay()
{
    Super::BeginPlay();

    if (OrderComponent)
    {
        OrderComponent->OnOrderChanged.AddDynamic(
            this,
            &AStrategyHQUnit::HandleHQOrderChanged);
    }
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

void AStrategyHQUnit::HandleHQOrderChanged(const FStrategyOrder& NewOrder)
{
    if (!FormationPlanner || HQLevel != EStrategyHQLevel::Battalion)
    {
        return;
    }

    if (NewOrder.Type != EStrategyOrderType::AttackHere &&
        NewOrder.Type != EStrategyOrderType::DefendHere)
    {
        return;
    }

    const float FacingYaw =
        NewOrder.bHasFacing
        ? NewOrder.FacingYaw
        : GetActorRotation().Yaw;

    FormationPlanner->IssueCompanySlots(
        NewOrder.TargetLocation,
        FacingYaw,
        NewOrder.Type == EStrategyOrderType::DefendHere,
        NewOrder.Authority);
}
