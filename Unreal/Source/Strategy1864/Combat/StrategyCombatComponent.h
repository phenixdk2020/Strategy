#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyCombatComponent.generated.h"

class AStrategyUnit;

DECLARE_DYNAMIC_MULTICAST_DELEGATE_ThreeParams(
    FStrategyVolleyResolved,
    AStrategyUnit*,
    Target,
    int32,
    Shots,
    int32,
    Hits);

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyCombatComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyCombatComponent();

protected:
    virtual void BeginPlay() override;

public:
    virtual void TickComponent(
        float DeltaTime,
        ELevelTick TickType,
        FActorComponentTickFunction* ThisTickFunction) override;

    UPROPERTY(BlueprintAssignable, Category="Strategy|Combat")
    FStrategyVolleyResolved OnVolleyResolved;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    int32 AmmunitionRounds = 1900;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float ReloadSeconds = 18.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float BaseHitChance = 0.035f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    int32 MaxShotsPerVolley = 190;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Combat")
    float ReloadRemainingSeconds = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float UnderFireDurationSeconds = 2.5f;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Combat")
    float UnderFireRemainingSeconds = 0.0f;

    UFUNCTION(BlueprintCallable, Category="Strategy|Combat")
    bool TryFireAt(AStrategyUnit* Target);

    UFUNCTION(BlueprintCallable, Category="Strategy|Combat")
    void NotifyIncomingVolley(int32 Hits);

    UFUNCTION(BlueprintPure, Category="Strategy|Combat")
    bool IsReloading() const { return ReloadRemainingSeconds > 0.0f; }

private:
    AStrategyUnit* FindBestTarget() const;
    int32 ResolveHits(int32 ShotCount, float DistanceCm);

    UPROPERTY()
    TObjectPtr<AStrategyUnit> OwnerUnit;

    FRandomStream RandomStream;
};
