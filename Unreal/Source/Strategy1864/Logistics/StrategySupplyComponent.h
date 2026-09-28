#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategySupplyComponent.generated.h"

class AStrategyUnit;

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategySupplyComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategySupplyComponent();

protected:
    virtual void BeginPlay() override;

public:
    virtual void TickComponent(
        float DeltaTime,
        ELevelTick TickType,
        FActorComponentTickFunction* ThisTickFunction) override;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply")
    bool bActsAsSupplySource = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply")
    int32 StoredAmmunitionRounds = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply")
    int32 MaxStoredAmmunitionRounds = 20000;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply")
    float ResupplyRadiusCm = 15000.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply")
    float TransferRoundsPerSecond = 120.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Supply", meta=(ClampMin="0.0", ClampMax="1.0"))
    float AutomaticRequestThresholdFraction = 0.25f;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Supply")
    bool bRequestingResupply = false;

    UFUNCTION(BlueprintCallable, Category="Strategy|Supply")
    void RequestResupply()
    {
        bRequestingResupply = true;
    }

    UFUNCTION(BlueprintCallable, Category="Strategy|Supply")
    void CancelResupplyRequest()
    {
        bRequestingResupply = false;
    }

    UFUNCTION(BlueprintCallable, Category="Strategy|Supply")
    int32 AddStoredAmmunition(int32 Rounds);

private:
    AStrategyUnit* FindBestReceiver() const;
    void TransferTo(AStrategyUnit* Receiver, float DeltaTime);

    UPROPERTY()
    TObjectPtr<AStrategyUnit> OwnerUnit;
};
