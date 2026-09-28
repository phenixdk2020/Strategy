#include "StrategyVisibilityComponent.h"

#include "../Units/StrategyUnit.h"
#include "StrategySmokeField.h"
#include "EngineUtils.h"
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

    const bool bTerrainLOS =
        !bBlocked || Hit.GetActor() == Target;

    if (!bTerrainLOS)
    {
        return false;
    }

    return GetSmokeTransmissionTo(Target) >=
        MinimumSmokeTransmissionForLOS;
}


float UStrategyVisibilityComponent::GetSmokeTransmissionTo(
    const AStrategyUnit* Target) const
{
    const AActor* OwnerActor = GetOwner();
    if (!OwnerActor || !IsValid(Target) || !GetWorld())
    {
        return 0.0f;
    }

    const FVector Start =
        OwnerActor->GetActorLocation() + FVector(0.0f, 0.0f, EyeHeightCm);
    const FVector End =
        Target->GetActorLocation() + FVector(0.0f, 0.0f, TargetHeightCm);

    float Transmission = 1.0f;

    for (TActorIterator<AStrategySmokeField> It(GetWorld()); It; ++It)
    {
        const AStrategySmokeField* Smoke = *It;
        if (!IsValid(Smoke) || !Smoke->IntersectsSightSegment(Start, End))
        {
            continue;
        }

        Transmission *=
            1.0f - FMath::Clamp(Smoke->GetCurrentDensity(), 0.0f, 0.95f);
    }

    return FMath::Clamp(Transmission, 0.0f, 1.0f);
}
