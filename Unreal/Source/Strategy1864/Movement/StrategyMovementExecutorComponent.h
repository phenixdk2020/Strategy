#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "../Orders/StrategyOrderTypes.h"
#include "../Navigation/StrategyRouteTypes.h"
#include "StrategyMovementExecutorComponent.generated.h"

class AStrategyUnit;

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyMovementExecutorComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyMovementExecutorComponent();

protected:
    virtual void BeginPlay() override;

public:
    virtual void TickComponent(
        float DeltaTime,
        ELevelTick TickType,
        FActorComponentTickFunction* ThisTickFunction) override;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Movement")
    float MoveSpeedCmPerSecond = 600.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Movement")
    float ArrivalToleranceCm = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Movement")
    float TurnSpeedDegreesPerSecond = 120.0f;

    UFUNCTION(BlueprintCallable, Category="Strategy|Movement")
    void StopMovement();

    UFUNCTION(BlueprintPure, Category="Strategy|Movement")
    bool HasMovementGoal() const { return bHasMovementGoal; }

    UFUNCTION(BlueprintPure, Category="Strategy|Movement")
    FVector GetMovementGoal() const { return MovementGoal; }

private:
    UFUNCTION()
    void HandleOrderChanged(const FStrategyOrder& NewOrder);

    void BeginMovementForOrder(const FStrategyOrder& Order);
    void FinishMovement();
    bool IsMovementOrder(EStrategyOrderType Type) const;

    UPROPERTY()
    TObjectPtr<AStrategyUnit> OwnerUnit;

    FVector MovementGoal = FVector::ZeroVector;
    FStrategyRoutePlan ActiveRoutePlan;
    TArray<FVector> RoutePoints;
    int32 RoutePointIndex = 0;
    float GoalFacingYaw = 0.0f;
    bool bHasMovementGoal = false;
    bool bApplyGoalFacing = false;
    int32 ExecutingOrderSerial = 0;
    bool bCavalryDefileActive = false;

    void UpdateBridgeFormationState(const FVector& CurrentLocation);
};
