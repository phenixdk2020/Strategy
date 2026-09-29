#include "StrategyFireDrillComponent.h"
#include "../Units/StrategyUnit.h"

UStrategyFireDrillComponent::UStrategyFireDrillComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

bool UStrategyFireDrillComponent::SetDrillMode(EStrategyFireDrillMode NewMode)
{
    const AStrategyUnit* Unit = Cast<AStrategyUnit>(GetOwner());
    if (!Unit ||
        Unit->UnitState == EStrategyUnitState::Routed ||
        Unit->UnitState == EStrategyUnitState::Destroyed)
    {
        return false;
    }

    DrillMode = NewMode;
    return true;
}

void UStrategyFireDrillComponent::SetLoadingMethod(
    EStrategyLoadingMethod NewMethod)
{
    LoadingMethod = NewMethod;
}

float UStrategyFireDrillComponent::GetEligibleFiringFraction(
    EStrategyStance Stance) const
{
    float Fraction = 1.0f;

    switch (DrillMode)
    {
        case EStrategyFireDrillMode::FrontRank:
            Fraction = 0.50f;
            break;
        case EStrategyFireDrillMode::AlternatingSections:
            Fraction = 0.55f;
            break;
        case EStrategyFireDrillMode::KneelingFrontRank:
            Fraction = Stance == EStrategyStance::Kneeling ? 0.95f : 0.80f;
            break;
        case EStrategyFireDrillMode::Independent:
            Fraction = 0.90f;
            break;
        case EStrategyFireDrillMode::Volley:
        default:
            Fraction = 1.0f;
            break;
    }

    if (Stance == EStrategyStance::Prone &&
        LoadingMethod == EStrategyLoadingMethod::MuzzleLoader)
    {
        Fraction *= 0.65f;
    }

    return FMath::Clamp(Fraction, 0.10f, 1.0f);
}

float UStrategyFireDrillComponent::GetReloadMultiplier(
    EStrategyStance Stance) const
{
    const float Training =
        FMath::Clamp(DrillTraining / 100.0f, 0.0f, 1.0f);

    float Multiplier = FMath::Lerp(1.15f, 0.90f, Training);

    if (DrillMode == EStrategyFireDrillMode::Independent)
    {
        Multiplier *= 0.92f;
    }
    else if (DrillMode == EStrategyFireDrillMode::AlternatingSections)
    {
        Multiplier *= 0.96f;
    }

    if (Stance == EStrategyStance::Prone)
    {
        Multiplier *=
            LoadingMethod == EStrategyLoadingMethod::BreechLoader
            ? 1.02f
            : 1.45f;
    }
    else if (Stance == EStrategyStance::Kneeling)
    {
        Multiplier *=
            LoadingMethod == EStrategyLoadingMethod::BreechLoader
            ? 0.98f
            : 1.08f;
    }

    return FMath::Clamp(Multiplier, 0.65f, 1.75f);
}

float UStrategyFireDrillComponent::GetVolleyCoordinationMultiplier() const
{
    const float Discipline = FMath::Clamp(FireDiscipline / 100.0f, 0.0f, 1.0f);
    return FMath::Lerp(0.75f, 1.08f, Discipline);
}

float UStrategyFireDrillComponent::GetIndependentFireCadenceMultiplier() const
{
    const float Training = FMath::Clamp(DrillTraining / 100.0f, 0.0f, 1.0f);
    return DrillMode == EStrategyFireDrillMode::Independent
        ? FMath::Lerp(0.95f, 1.20f, Training)
        : 1.0f;
}

bool UStrategyFireDrillComponent::SupportsProneReload() const
{
    return LoadingMethod == EStrategyLoadingMethod::BreechLoader ||
           DrillTraining >= 70.0f;
}

bool UStrategyFireDrillComponent::SupportsKneelingFrontRank() const
{
    return DrillTraining >= 35.0f;
}

bool UStrategyFireDrillComponent::IsAlternatingFire() const
{
    return DrillMode == EStrategyFireDrillMode::AlternatingSections;
}

float UStrategyFireDrillComponent::GetDoctrineReadiness() const
{
    return FMath::Clamp((DrillTraining + FireDiscipline) / 200.0f, 0.0f, 1.0f);
}
