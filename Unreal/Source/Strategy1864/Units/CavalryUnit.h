#pragma once

#include "CoreMinimal.h"
#include "StrategyUnit.h"
#include "CavalryUnit.generated.h"

class USkeletalMeshComponent;

UCLASS(Blueprintable)
class STRATEGY1864_API ACavalryUnit : public AStrategyUnit
{
    GENERATED_BODY()

public:
    ACavalryUnit();

protected:
    virtual void BeginPlay() override;

public:
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Cavalry")
    TObjectPtr<USkeletalMeshComponent> HorseMesh;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Cavalry")
    TObjectPtr<USkeletalMeshComponent> RiderMesh;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Cavalry")
    FName RiderSocketName = TEXT("RiderSocket");
};
