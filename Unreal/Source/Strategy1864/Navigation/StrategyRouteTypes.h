#pragma once

#include "CoreMinimal.h"
#include "StrategyRouteTypes.generated.h"

USTRUCT(BlueprintType)
struct FStrategyRoutePlan
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    TArray<FVector> Points;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    bool bUsesBridge = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    int32 BridgeEnterPointIndex = INDEX_NONE;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    int32 BridgeExitPointIndex = INDEX_NONE;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Navigation")
    bool bSameBankDetour = false;
};
