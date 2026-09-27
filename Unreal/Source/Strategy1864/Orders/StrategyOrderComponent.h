#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyOrderTypes.h"
#include "StrategyOrderComponent.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FStrategyOrderChanged, const FStrategyOrder&, NewOrder);

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyOrderComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyOrderComponent();

    UPROPERTY(BlueprintAssignable, Category="Strategy|Orders")
    FStrategyOrderChanged OnOrderChanged;

    UFUNCTION(BlueprintCallable, Category="Strategy|Orders")
    void SetOrder(const FStrategyOrder& NewOrder);

    UFUNCTION(BlueprintCallable, Category="Strategy|Orders")
    void ClearOrder();

    UFUNCTION(BlueprintPure, Category="Strategy|Orders")
    const FStrategyOrder& GetCurrentOrder() const { return CurrentOrder; }

private:
    UPROPERTY(VisibleInstanceOnly, Category="Strategy|Orders")
    FStrategyOrder CurrentOrder;
};
