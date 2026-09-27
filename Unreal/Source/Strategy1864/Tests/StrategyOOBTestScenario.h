#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "StrategyOOBTestScenario.generated.h"

class AStrategyCompanyUnit;
class AStrategyHQUnit;
class AStrategyUnit;

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

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    void BuildTestOOB();

    UFUNCTION(BlueprintCallable, Category="Strategy|Test")
    void ClearSpawnedUnits();

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
        AStrategyUnit* OrganicParent);

    UPROPERTY()
    TArray<TObjectPtr<AStrategyUnit>> SpawnedUnitObjects;

};
