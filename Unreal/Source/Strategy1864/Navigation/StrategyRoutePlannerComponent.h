#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyRoutePlannerComponent.generated.h"

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
};
