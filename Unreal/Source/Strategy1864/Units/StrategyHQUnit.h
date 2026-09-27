#pragma once

#include "CoreMinimal.h"
#include "StrategyUnit.h"
#include "StrategyHQUnit.generated.h"

UENUM(BlueprintType)
enum class EStrategyHQLevel : uint8
{
    Battalion,
    Regiment,
    Brigade,
    Division
};

UCLASS(Blueprintable)
class STRATEGY1864_API AStrategyHQUnit : public AStrategyUnit
{
    GENERATED_BODY()

public:
    AStrategyHQUnit();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|HQ")
    EStrategyHQLevel HQLevel = EStrategyHQLevel::Battalion;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|HQ")
    float CommandInnerRadius = 32000.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|HQ")
    float CommandOuterRadius = 45000.0f;

    UFUNCTION(BlueprintCallable, Category="Strategy|HQ")
    void ApplyHQLevelDefaults();
};
