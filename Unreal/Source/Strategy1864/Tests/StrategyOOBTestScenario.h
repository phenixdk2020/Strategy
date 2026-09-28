#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "StrategyOOBTestScenario.generated.h"

class AStrategyCompanyUnit;
class AStrategyHQUnit;
class AStrategyUnit;
class AStrategyRiverBarrier;
class ACavalryUnit;
class AStrategyNavigationObstacle;

UCLASS(Blueprintable)
class STRATEGY1864_API AStrategyOOBTestScenario : public AActor
{
    GENERATED_BODY()

public:
    AStrategyOOBTestScenario();

protected:
    virtual void BeginPlay() override;

public:
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bBuildOnBeginPlay = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    FVector Origin = FVector::ZeroVector;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    float CompanySpacing = 1400.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bSpawnEnemyQAUnits = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bSpawnRiverQA = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bSpawnCavalryQA = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bSpawnObstacleQA = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    int32 QARandomSeed = 1864;

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    void BuildTestOOB();

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    void ClearSpawnedUnits();

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    void ResetScenario();

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    bool ValidateStableIdsAndHierarchy(TArray<FString>& OutErrors) const;

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    bool RunRegressionChecklist(TArray<FString>& OutFailures) const;

    UFUNCTION(BlueprintPure, Category="Strategy|Test")
    TArray<AStrategyUnit*> GetSpawnedUnits() const;

private:
    AStrategyHQUnit* SpawnHQ(
        const FName StableId,
        const FString& Name,
        uint8 HQLevelValue,
        const FVector& Location,
        AStrategyUnit* OrganicParent);

    AStrategyCompanyUnit* SpawnCompany(
        const FName StableId,
        const FString& Name,
        int32 CompanyNumber,
        const FVector& Location,
        AStrategyUnit* OrganicParent,
        uint8 SideValue);

    ACavalryUnit* SpawnCavalry(
        const FName StableId,
        const FString& Name,
        const FVector& Location,
        AStrategyUnit* OrganicParent);

    UPROPERTY()
    TArray<TObjectPtr<AStrategyUnit>> SpawnedUnitObjects;

    UPROPERTY()
    TObjectPtr<AStrategyRiverBarrier> SpawnedRiverBarrier;

    UPROPERTY()
    TObjectPtr<AStrategyNavigationObstacle> SpawnedNavigationObstacle;

};
