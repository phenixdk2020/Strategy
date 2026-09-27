#include "StrategyOrderComponent.h"

UStrategyOrderComponent::UStrategyOrderComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UStrategyOrderComponent::SetOrder(const FStrategyOrder& NewOrder)
{
    CurrentOrder = NewOrder;
    OnOrderChanged.Broadcast(CurrentOrder);
}

void UStrategyOrderComponent::ClearOrder()
{
    CurrentOrder = FStrategyOrder();
    OnOrderChanged.Broadcast(CurrentOrder);
}
