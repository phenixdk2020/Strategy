#include "StrategyOfficerProfileComponent.h"

UStrategyOfficerProfileComponent::UStrategyOfficerProfileComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

float UStrategyOfficerProfileComponent::GetCommandEfficiency() const
{
    return FMath::Clamp(
        (Leadership * 0.35f + Initiative * 0.30f + StaffQuality * 0.35f) / 100.0f,
        0.10f,
        1.0f);
}
