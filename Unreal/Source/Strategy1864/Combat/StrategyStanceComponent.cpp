#include "StrategyStanceComponent.h"

#include "../Units/StrategyUnit.h"
#include "../Movement/StrategyMovementExecutorComponent.h"

UStrategyStanceComponent::UStrategyStanceComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

bool UStrategyStanceComponent::SetStance(EStrategyStance NewStance)
{
    AStrategyUnit* Unit = Cast<AStrategyUnit>(GetOwner());
    if (!Unit ||
        Unit->Echelon != EStrategyEchelon::Company ||
        Unit->UnitState == EStrategyUnitState::Routed ||
        Unit->UnitState == EStrategyUnitState::Destroyed)
    {
        return false;
    }

    Stance = NewStance;
    return true;
}

float UStrategyStanceComponent::GetMovementMultiplier() const
{
    return Stance == EStrategyStance::Prone
        ? FMath::Max(0.05f, ProneMovementMultiplier)
        : 1.0f;
}

float UStrategyStanceComponent::GetReloadMultiplier() const
{
    return Stance == EStrategyStance::Prone
        ? FMath::Max(0.10f, ProneReloadMultiplier)
        : 1.0f;
}

float UStrategyStanceComponent::GetIncomingHitMultiplier() const
{
    return Stance == EStrategyStance::Prone
        ? FMath::Clamp(ProneIncomingHitMultiplier, 0.05f, 1.0f)
        : 1.0f;
}
