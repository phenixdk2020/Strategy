#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyRouteTypes.h"
#include "StrategyRoutePlannerComponent.generated.h"

class AStrategyRiverBarrier;

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyRoutePlannerComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyRoutePlannerComponent();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    bool bUseNavigationSystem = true;

    UFUNCTION(BlueprintCallable, Category="Strategy|Navigation")
    TArray<FVector> BuildRoute(
        const FVector& StartLocation,
        const FVector& EndLocation) const;

    UFUNCTION(BlueprintCallable, Category="Strategy|Navigation")
    FStrategyRoutePlan BuildRoutePlan(
        const FVector& StartLocation,
        const FVector& EndLocation) const;

private:
    AStrategyRiverBarrier* FindRelevantRiverBarrier(
        const FVector& StartLocation,
        const FVector& EndLocation) const;

    void AppendNavSegment(
        const FVector& SegmentStart,
        const FVector& SegmentEnd,
        TArray<FVector>& InOutPoints) const;
};
