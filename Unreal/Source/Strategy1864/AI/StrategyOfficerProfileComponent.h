#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyOfficerProfileComponent.generated.h"

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyOfficerProfileComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyOfficerProfileComponent();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Officer", meta=(ClampMin="0.0", ClampMax="100.0"))
    float Leadership = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Officer", meta=(ClampMin="0.0", ClampMax="100.0"))
    float Initiative = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Officer", meta=(ClampMin="0.0", ClampMax="100.0"))
    float StaffQuality = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Officer", meta=(ClampMin="0.0", ClampMax="100.0"))
    float Aggression = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Officer", meta=(ClampMin="0.0", ClampMax="100.0"))
    float Caution = 50.0f;

    UFUNCTION(BlueprintPure, Category="Strategy|Officer")
    float GetCommandEfficiency() const;
};
