#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyArtilleryTypes.h"
#include "../Orders/StrategyOrderTypes.h"
#include "StrategyArtilleryFireMissionComponent.generated.h"

class AStrategyArtilleryBatteryUnit;
class AStrategyUnit;

DECLARE_DYNAMIC_MULTICAST_DELEGATE_FourParams(
    FStrategyArtilleryShotResolved,
    AStrategyUnit*,
    Target,
    EStrategyArtilleryAmmoType,
    AmmoType,
    int32,
    GunsFired,
    int32,
    Casualties);

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyArtilleryFireMissionComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyArtilleryFireMissionComponent();

protected:
    virtual void BeginPlay() override;

public:
    virtual void TickComponent(
        float DeltaTime,
        ELevelTick TickType,
        FActorComponentTickFunction* ThisTickFunction) override;

    UPROPERTY(BlueprintAssignable, Category="Strategy|Artillery")
    FStrategyArtilleryShotResolved OnArtilleryShotResolved;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Artillery")
    EStrategyArtilleryFireMode FireMode =
        EStrategyArtilleryFireMode::ManualTarget;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Artillery")
    TObjectPtr<AStrategyUnit> ManualTarget;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Artillery")
    float ReloadRemainingSeconds = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Artillery")
    float EvaluationIntervalSeconds = 0.20f;

    UFUNCTION(BlueprintCallable, Category="Strategy|Artillery")
    bool SetManualTarget(
        AStrategyUnit* Target,
        EStrategyOrderAuthority Authority);

    UFUNCTION(BlueprintCallable, Category="Strategy|Artillery")
    void SetAutoTargetEnabled(bool bEnabled);

    UFUNCTION(BlueprintCallable, Category="Strategy|Artillery")
    void SetHoldFire(bool bHold);

    UFUNCTION(BlueprintCallable, Category="Strategy|Artillery")
    bool SelectAmmo(EStrategyArtilleryAmmoType AmmoType);

    UFUNCTION(BlueprintPure, Category="Strategy|Artillery")
    float GetMinimumRangeCm(
        EStrategyArtilleryAmmoType AmmoType) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Artillery")
    float GetMaximumRangeCm(
        EStrategyArtilleryAmmoType AmmoType) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Artillery")
    bool CanEngageTarget(const AStrategyUnit* Target) const;

private:
    bool CanObserveTarget(const AStrategyUnit* Target) const;
    AStrategyUnit* FindBestAutoTarget() const;
    bool FireAt(AStrategyUnit* Target);
    int32 ResolveCasualties(
        AStrategyUnit* Target,
        int32 GunsFired,
        float DistanceCm);
    float CalculateTargetScore(const AStrategyUnit* Target) const;
    float GetBaseGunHitChance(
        EStrategyArtilleryAmmoType AmmoType) const;
    FIntPoint GetCasualtyRange(
        EStrategyArtilleryAmmoType AmmoType) const;
    void CompleteManualMission(bool bFailed);

    UPROPERTY()
    TObjectPtr<AStrategyArtilleryBatteryUnit> OwnerBattery;

    EStrategyArtilleryFireMode PreviousNonHoldMode =
        EStrategyArtilleryFireMode::ManualTarget;

    float EvaluationAccumulator = 0.0f;
    FRandomStream RandomStream;
};
