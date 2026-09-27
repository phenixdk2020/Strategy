#include "StrategyMovementExecutorComponent.h"

#include "../Orders/StrategyOrderComponent.h"
#include "../Command/StrategyCommandComponent.h"
#include "../Units/StrategyUnit.h"
#include "../Navigation/StrategyRoutePlannerComponent.h"
#include "../Units/CavalryUnit.h"

UStrategyMovementExecutorComponent::UStrategyMovementExecutorComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
    PrimaryComponentTick.bStartWithTickEnabled = false;
}

void UStrategyMovementExecutorComponent::BeginPlay()
{
    Super::BeginPlay();

    OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (!OwnerUnit || !OwnerUnit->OrderComponent)
    {
        SetComponentTickEnabled(false);
        return;
    }

    OwnerUnit->OrderComponent->OnOrderChanged.AddDynamic(
        this,
        &UStrategyMovementExecutorComponent::HandleOrderChanged);
}

void UStrategyMovementExecutorComponent::HandleOrderChanged(const FStrategyOrder& NewOrder)
{
    if (!OwnerUnit || !OwnerUnit->OrderComponent)
    {
        return;
    }

    if (!NewOrder.IsValidOrder())
    {
        StopMovement();
        OwnerUnit->SetUnitState(EStrategyUnitState::Ready);
        return;
    }

    if (NewOrder.Type == EStrategyOrderType::Hold)
    {
        StopMovement();
        OwnerUnit->SetUnitState(EStrategyUnitState::Ready);
        OwnerUnit->OrderComponent->CompleteExecution();
        return;
    }

    const bool bCommandParentMission =
        OwnerUnit->CommandComponent &&
        OwnerUnit->CommandComponent->CurrentSubordinates.Num() > 0 &&
        (NewOrder.Type == EStrategyOrderType::AttackHere ||
         NewOrder.Type == EStrategyOrderType::DefendHere);

    if (bCommandParentMission)
    {
        StopMovement();
        OwnerUnit->SetUnitState(EStrategyUnitState::Ready);
        OwnerUnit->OrderComponent->BeginExecution();
        return;
    }

    if (IsMovementOrder(NewOrder.Type))
    {
        BeginMovementForOrder(NewOrder);
    }
}

void UStrategyMovementExecutorComponent::BeginMovementForOrder(const FStrategyOrder& Order)
{
    MovementGoal = Order.TargetLocation;
    ActiveRoutePlan = FStrategyRoutePlan();
    RoutePoints.Reset();
    RoutePointIndex = 0;
    bCavalryDefileActive = false;

    if (OwnerUnit && OwnerUnit->RoutePlanner)
    {
        ActiveRoutePlan = OwnerUnit->RoutePlanner->BuildRoutePlan(
            OwnerUnit->GetActorLocation(),
            MovementGoal);
        RoutePoints = ActiveRoutePlan.Points;
    }

    if (RoutePoints.Num() == 0)
    {
        RoutePoints.Add(MovementGoal);
    }

    GoalFacingYaw = Order.FacingYaw;
    bApplyGoalFacing = Order.bHasFacing;
    ExecutingOrderSerial = Order.OrderSerial;
    bHasMovementGoal = true;

    if (OwnerUnit)
    {
        OwnerUnit->SetUnitState(EStrategyUnitState::Moving);
    }

    if (OwnerUnit && OwnerUnit->OrderComponent)
    {
        OwnerUnit->OrderComponent->BeginExecution();
    }

    SetComponentTickEnabled(true);
}

void UStrategyMovementExecutorComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!bHasMovementGoal || !OwnerUnit || !OwnerUnit->OrderComponent)
    {
        SetComponentTickEnabled(false);
        return;
    }

    const FStrategyOrder CurrentOrder = OwnerUnit->OrderComponent->GetCurrentOrder();
    if (CurrentOrder.OrderSerial != ExecutingOrderSerial || !IsMovementOrder(CurrentOrder.Type))
    {
        StopMovement();
        return;
    }

    const FVector CurrentLocation = OwnerUnit->GetActorLocation();
    UpdateBridgeFormationState(CurrentLocation);

    if (!RoutePoints.IsValidIndex(RoutePointIndex))
    {
        FinishMovement();
        return;
    }

    const FVector ActiveWaypoint = RoutePoints[RoutePointIndex];
    const FVector WaypointDelta = ActiveWaypoint - CurrentLocation;
    const FVector FlatDelta(WaypointDelta.X, WaypointDelta.Y, 0.0f);

    const bool bFinalWaypoint = RoutePointIndex == RoutePoints.Num() - 1;
    const float WaypointToleranceCm =
        bFinalWaypoint
        ? ArrivalToleranceCm
        : FMath::Max(ArrivalToleranceCm, 100.0f);

    if (FlatDelta.Size() <= WaypointToleranceCm)
    {
        if (!bFinalWaypoint)
        {
            ++RoutePointIndex;
            return;
        }

        OwnerUnit->SetActorLocation(FVector(MovementGoal.X, MovementGoal.Y, CurrentLocation.Z));

        if (bApplyGoalFacing)
        {
            FRotator Rotation = OwnerUnit->GetActorRotation();
            Rotation.Yaw = GoalFacingYaw;
            OwnerUnit->SetActorRotation(Rotation);
        }

        FinishMovement();
        return;
    }

    const FVector Direction = FlatDelta.GetSafeNormal();
    const float Step = MoveSpeedCmPerSecond * DeltaTime;
    const FVector NewLocation =
        CurrentLocation + Direction * FMath::Min(Step, FlatDelta.Size());

    OwnerUnit->SetActorLocation(NewLocation);

    if (!Direction.IsNearlyZero())
    {
        const FRotator DesiredRotation = Direction.Rotation();
        const FRotator NewRotation = FMath::RInterpConstantTo(
            OwnerUnit->GetActorRotation(),
            DesiredRotation,
            DeltaTime,
            TurnSpeedDegreesPerSecond);

        OwnerUnit->SetActorRotation(NewRotation);
    }
}

void UStrategyMovementExecutorComponent::FinishMovement()
{
    bHasMovementGoal = false;
    SetComponentTickEnabled(false);

    if (!OwnerUnit)
    {
        return;
    }

    OwnerUnit->SetUnitState(EStrategyUnitState::Ready);

    if (OwnerUnit->OrderComponent)
    {
        OwnerUnit->OrderComponent->CompleteExecution();
    }
}

void UStrategyMovementExecutorComponent::StopMovement()
{
    if (bCavalryDefileActive)
    {
        if (ACavalryUnit* Cavalry = Cast<ACavalryUnit>(OwnerUnit))
        {
            Cavalry->SetDefileMode(false);
        }
    }

    bCavalryDefileActive = false;
    bHasMovementGoal = false;
    ExecutingOrderSerial = 0;
    ActiveRoutePlan = FStrategyRoutePlan();
    RoutePoints.Reset();
    RoutePointIndex = 0;
    SetComponentTickEnabled(false);
}

void UStrategyMovementExecutorComponent::UpdateBridgeFormationState(const FVector& CurrentLocation)
{
    ACavalryUnit* Cavalry = Cast<ACavalryUnit>(OwnerUnit);
    if (!Cavalry || !ActiveRoutePlan.bUsesBridge)
    {
        return;
    }

    const bool bAtOrInsideBridgeTransaction =
        ActiveRoutePlan.BridgeEnterPointIndex != INDEX_NONE &&
        ActiveRoutePlan.BridgeExitPointIndex != INDEX_NONE &&
        RoutePointIndex >= ActiveRoutePlan.BridgeEnterPointIndex &&
        RoutePointIndex <= ActiveRoutePlan.BridgeExitPointIndex;

    bool bNearBridgeApproach = false;

    if (RoutePoints.IsValidIndex(ActiveRoutePlan.BridgeEnterPointIndex) &&
        RoutePointIndex <= ActiveRoutePlan.BridgeEnterPointIndex)
    {
        const float DistanceToApproach = FVector::Dist2D(
            CurrentLocation,
            RoutePoints[ActiveRoutePlan.BridgeEnterPointIndex]);

        bNearBridgeApproach = DistanceToApproach <= 3600.0f;
    }

    const bool bShouldUseDefile =
        bAtOrInsideBridgeTransaction || bNearBridgeApproach;

    if (bShouldUseDefile != bCavalryDefileActive)
    {
        Cavalry->SetDefileMode(bShouldUseDefile);
        bCavalryDefileActive = bShouldUseDefile;
    }
}

bool UStrategyMovementExecutorComponent::IsMovementOrder(EStrategyOrderType Type) const
{
    switch (Type)
    {
        case EStrategyOrderType::Move:
        case EStrategyOrderType::AttackHere:
        case EStrategyOrderType::DefendHere:
        case EStrategyOrderType::Advance:
        case EStrategyOrderType::Withdraw:
        case EStrategyOrderType::Assemble:
        case EStrategyOrderType::ScoutHere:
            return true;

        default:
            return false;
    }
}
