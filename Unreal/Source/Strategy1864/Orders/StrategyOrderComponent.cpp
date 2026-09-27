#include "StrategyOrderComponent.h"

UStrategyOrderComponent::UStrategyOrderComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

bool UStrategyOrderComponent::SetOrder(const FStrategyOrder& NewOrder)
{
    if (!NewOrder.IsValidOrder() || !CanReplaceCurrentOrder(NewOrder))
    {
        return false;
    }

    if (CurrentOrder.IsValidOrder() && IsPhysicallyExecuting())
    {
        SetExecutionState(EStrategyOrderExecutionState::Superseded);
    }

    CurrentOrder = NewOrder;
    CurrentOrder.OrderSerial = NextOrderSerial++;
    SetExecutionState(EStrategyOrderExecutionState::Pending);
    OnOrderChanged.Broadcast(CurrentOrder);
    return true;
}

void UStrategyOrderComponent::ClearOrder()
{
    CurrentOrder = FStrategyOrder();
    SetExecutionState(EStrategyOrderExecutionState::Idle);
    OnOrderChanged.Broadcast(CurrentOrder);
}

void UStrategyOrderComponent::BeginExecution()
{
    if (!CurrentOrder.IsValidOrder())
    {
        return;
    }

    SetExecutionState(EStrategyOrderExecutionState::Executing);
}

void UStrategyOrderComponent::CompleteExecution()
{
    if (!CurrentOrder.IsValidOrder())
    {
        SetExecutionState(EStrategyOrderExecutionState::Idle);
        return;
    }

    SetExecutionState(EStrategyOrderExecutionState::Completed);
}

void UStrategyOrderComponent::FailExecution()
{
    if (!CurrentOrder.IsValidOrder())
    {
        return;
    }

    SetExecutionState(EStrategyOrderExecutionState::Failed);
}

void UStrategyOrderComponent::MarkPendingTarget()
{
    SetExecutionState(EStrategyOrderExecutionState::PendingTarget);
}

bool UStrategyOrderComponent::IsPhysicallyExecuting() const
{
    return ExecutionState == EStrategyOrderExecutionState::PendingTarget ||
           ExecutionState == EStrategyOrderExecutionState::Pending ||
           ExecutionState == EStrategyOrderExecutionState::Executing;
}

bool UStrategyOrderComponent::HasStandingIntent() const
{
    return CurrentOrder.IsValidOrder() && CurrentOrder.IsStandingIntent();
}

bool UStrategyOrderComponent::CanReplaceCurrentOrder(const FStrategyOrder& NewOrder) const
{
    if (!CurrentOrder.IsValidOrder())
    {
        return true;
    }

    return static_cast<uint8>(NewOrder.Authority) >= static_cast<uint8>(CurrentOrder.Authority);
}

void UStrategyOrderComponent::SetExecutionState(EStrategyOrderExecutionState NewState)
{
    if (ExecutionState == NewState)
    {
        return;
    }

    const EStrategyOrderExecutionState OldState = ExecutionState;
    ExecutionState = NewState;
    OnExecutionStateChanged.Broadcast(OldState, ExecutionState);
}
