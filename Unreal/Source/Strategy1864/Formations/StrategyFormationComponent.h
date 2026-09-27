#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyFormationTypes.h"
#include "StrategyFormationComponent.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(
    FStrategyFormationChanged,
    EStrategyFormationType,
    OldFormation,
    EStrategyFormationType,
    NewFormation);

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyFormationComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyFormationComponent();

    UPROPERTY(BlueprintAssignable, Category="Strategy|Formation")
    FStrategyFormationChanged OnFormationChanged;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Formation")
    EStrategyFormationType CurrentFormation = EStrategyFormationType::Line;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Formation")
    int32 RankCount = 3;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Formation")
    float SoldierLateralSpacingCm = 75.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Formation")
    float SoldierRankSpacingCm = 90.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Formation")
    int32 ColumnWidth = 4;

    UFUNCTION(BlueprintCallable, Category="Strategy|Formation")
    void SetFormation(EStrategyFormationType NewFormation);

    UFUNCTION(BlueprintPure, Category="Strategy|Formation")
    TArray<FStrategyFormationSlot> GenerateSoldierSlots(
        const FVector& FormationCenter,
        float FacingYaw,
        int32 Strength) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Formation")
    float EstimateFrontageCm(int32 Strength) const;
};
