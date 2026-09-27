#include "StrategyVisibilityComponent.h"

#include "../Units/StrategyUnit.h"
#include "Engine/World.h"

UStrategyVisibilityComponent::UStrategyVisibilityComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

bool UStrategyVisibilityComponent::HasLineOfSightTo(const AStrategyUnit* Target) const
{
    const AActor* OwnerActor = GetOwner();
    if (!OwnerActor || !IsValid(Target) || !GetWorld())
    {
        return false;
    }

    const FVector Start =
        OwnerActor->GetActorLocation() + FVector(0.0f, 0.0f, EyeHeightCm);
    const FVector End =
        Target->GetActorLocation() + FVector(0.0f, 0.0f, TargetHeightCm);

    FCollisionQueryParams QueryParams(SCENE_QUERY_STAT(StrategyLOS), true);
    QueryParams.AddIgnoredActor(OwnerActor);

    FHitResult Hit;
    const bool bBlocked = GetWorld()->LineTraceSingleByChannel(
        Hit,
        Start,
        End,
        ECC_Visibility,
        QueryParams);

    if (!bBlocked)
    {
        return true;
    }

    return Hit.GetActor() == Target;
}
