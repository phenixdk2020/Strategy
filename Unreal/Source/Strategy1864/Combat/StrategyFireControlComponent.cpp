#include "StrategyFireControlComponent.h"

#include "StrategyVisibilityComponent.h"
#include "../Units/StrategyUnit.h"

UStrategyFireControlComponent::UStrategyFireControlComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UStrategyFireControlComponent::BeginPlay()
{
    Super::BeginPlay();

    OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (OwnerUnit)
    {
        OwnerUnit->MaximumFireRangeCm = LongRangeCm;
    }
}

void UStrategyFireControlComponent::SetFirePolicy(EStrategyFirePolicy NewPolicy)
{
    FirePolicy = NewPolicy;
}

float UStrategyFireControlComponent::GetActiveRangeCm() const
{
    switch (FirePolicy)
    {
        case EStrategyFirePolicy::Close:
            return CloseRangeCm;

        case EStrategyFirePolicy::Medium:
            return MediumRangeCm;

        case EStrategyFirePolicy::Long:
            return LongRangeCm;

        case EStrategyFirePolicy::Hold:
        default:
            return 0.0f;
    }
}

bool UStrategyFireControlComponent::IsInsideFireCone(const AStrategyUnit* Target) const
{
    const AStrategyUnit* Unit = OwnerUnit ? OwnerUnit.Get() : Cast<AStrategyUnit>(GetOwner());
    if (!Unit || !IsValid(Target))
    {
        return false;
    }

    FVector ToTarget = Target->GetActorLocation() - Unit->GetActorLocation();
    ToTarget.Z = 0.0f;

    if (ToTarget.IsNearlyZero())
    {
        return true;
    }

    const FVector Forward = Unit->GetActorForwardVector().GetSafeNormal2D();
    const FVector Direction = ToTarget.GetSafeNormal();

    const float Dot = FMath::Clamp(
        FVector::DotProduct(Forward, Direction),
        -1.0f,
        1.0f);

    const float AngleDegrees = FMath::RadiansToDegrees(FMath::Acos(Dot));
    return AngleDegrees <= FireConeHalfAngleDegrees;
}

bool UStrategyFireControlComponent::CanEngageTarget(const AStrategyUnit* Target) const
{
    const AStrategyUnit* Unit = OwnerUnit ? OwnerUnit.Get() : Cast<AStrategyUnit>(GetOwner());
    if (!Unit ||
        !IsValid(Target) ||
        !Unit->IsCombatEffective() ||
        !Target->IsCombatEffective() ||
        Target->Side == EStrategySide::Neutral ||
        Target->Side == Unit->Side ||
        FirePolicy == EStrategyFirePolicy::Hold)
    {
        return false;
    }

    const float DistanceCm = FVector::Dist2D(
        Unit->GetActorLocation(),
        Target->GetActorLocation());

    if (DistanceCm > GetActiveRangeCm() || !IsInsideFireCone(Target))
    {
        return false;
    }

    return Unit->VisibilityComponent &&
        Unit->VisibilityComponent->HasLineOfSightTo(Target);
}
