#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyStanceComponent.h"
#include "StrategyFireDrillComponent.generated.h"

UENUM(BlueprintType)
enum class EStrategyLoadingMethod : uint8
{
    MuzzleLoader,
    BreechLoader
};

UENUM(BlueprintType)
enum class EStrategyFireDrillMode : uint8
{
    FrontRank,
    Volley,
    Independent,
    AlternatingSections,
    KneelingFrontRank
};

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyFireDrillComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyFireDrillComponent();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Fire Drill")
    EStrategyLoadingMethod LoadingMethod = EStrategyLoadingMethod::MuzzleLoader;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Fire Drill")
    EStrategyFireDrillMode DrillMode = EStrategyFireDrillMode::Volley;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Fire Drill", meta=(ClampMin="0.0", ClampMax="100.0"))
    float DrillTraining = 50.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Fire Drill", meta=(ClampMin="0.0", ClampMax="100.0"))
    float FireDiscipline = 50.0f;

    UFUNCTION(BlueprintCallable, Category="Strategy|Fire Drill")
    bool SetDrillMode(EStrategyFireDrillMode NewMode);

    UFUNCTION(BlueprintCallable, Category="Strategy|Fire Drill")
    void SetLoadingMethod(EStrategyLoadingMethod NewMethod);

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    float GetEligibleFiringFraction(EStrategyStance Stance) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    float GetReloadMultiplier(EStrategyStance Stance) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    float GetVolleyCoordinationMultiplier() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    float GetIndependentFireCadenceMultiplier() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    bool SupportsProneReload() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    bool SupportsKneelingFrontRank() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    bool IsAlternatingFire() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Fire Drill")
    float GetDoctrineReadiness() const;
};
