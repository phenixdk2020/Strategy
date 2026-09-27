#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "StrategyUnit.generated.h"

class USceneComponent;
class UStrategyOrderComponent;

UENUM(BlueprintType)
enum class EStrategyEchelon : uint8
{
    Company,
    Battalion,
    Regiment,
    Brigade,
    Division,
    Cavalry,
    Headquarters
};

UCLASS(Abstract, Blueprintable)
class STRATEGY1864_API AStrategyUnit : public APawn
{
    GENERATED_BODY()

public:
    AStrategyUnit();

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<USceneComponent> SceneRoot;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyOrderComponent> OrderComponent;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    EStrategyEchelon Echelon = EStrategyEchelon::Company;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    int32 CurrentStrength = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Selection")
    bool bSelected = false;

    UFUNCTION(BlueprintCallable, Category="Strategy|Selection")
    virtual void SetSelected(bool bNewSelected);
};
