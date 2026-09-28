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
         NewOrder.Type == EStrategyOrderType::DefendHere ||
         NewOrder.Type == EStrategyOrderType::Advance ||
         NewOrder.Type == EStrategyOrderType::Withdraw ||
         NewOrder.Type == EStrategyOrderType::Assemble);

    if (bCommandParentMission)
    {
        StopMovement();
        OwnerUnit->SetUnitState(EStrategyUnitState::Ready);
        OwnerUnit->OrderComponent->BeginExecution();
        return;
    }

    if (IsMovementOrder(NewOrder.Type))
    {
        if (!TryRetargetDuringBridge(NewOrder))
        {
            BeginMovementForOrder(NewOrder);
        }
    }
}

void UStrategyMovementExecutorComponent::BeginMovementForOrder(const FStrategyOrder& Order)
{
    MovementGoal = Order.TargetLocation;
    ActiveRoutePlan = FStrategyRoutePlan();
    RoutePoints.Reset();
    RoutePointIndex = 0;
    bCavalryDefileActive = false;
    bTurningToGoalFacing = false;

    if (OwnerUnit && OwnerUnit->RoutePlanner)
    {
        ActiveRoutePlan = OwnerUnit->RoutePlanner->BuildRoutePlan(
            OwnerUnit->GetActorLocation(),
            MovementGoal);

        if (!ActiveRoutePlan.bValid)
        {
            UE_LOG(
                LogTemp,
                Warning,
                TEXT("PROJECT1864-MOVE: route rejected for %s: %s"),
                *OwnerUnit->StableUnitId.ToString(),
                *ActiveRoutePlan.FailureReason);

            OwnerUnit->SetUnitState(EStrategyUnitState::Ready);
            OwnerUnit->OrderComponent->FailExecution();
            bHasMovementGoal = false;
            SetComponentTickEnabled(false);
            return;
        }

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

    if (PauseRemainingSeconds > 0.0f)
    {
        PauseRemainingSeconds = FMath::Max(0.0f, PauseRemainingSeconds - DeltaTime);

        if (PauseRemainingSeconds <= 0.0f &&
            OwnerUnit->UnitState == EStrategyUnitState::UnderFire)
        {
            OwnerUnit->SetUnitState(EStrategyUnitState::Moving);
        }

        return;
    }

    const FVector CurrentLocation = OwnerUnit->GetActorLocation();
    UpdateBridgeFormationState(CurrentLocation);

    if (bTurningToGoalFacing && bApplyGoalFacing)
    {
        const FRotator CurrentRotation = OwnerUnit->GetActorRotation();
        FRotator DesiredRotation = CurrentRotation;
        DesiredRotation.Yaw = GoalFacingYaw;

        const float RemainingYaw =
            FMath::Abs(
                FMath::FindDeltaAngleDegrees(
                    CurrentRotation.Yaw,
                    GoalFacingYaw));

        if (RemainingYaw <= FinalFacingToleranceDegrees)
        {
            OwnerUnit->SetActorRotation(DesiredRotation);
            bTurningToGoalFacing = false;
            FinishMovement();
            return;
        }

        OwnerUnit->SetActorRotation(
            FMath::RInterpConstantTo(
                CurrentRotation,
                DesiredRotation,
                DeltaTime,
                TurnSpeedDegreesPerSecond));
        return;
    }

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

        OwnerUnit->SetActorLocation(
            FVector(MovementGoal.X, MovementGoal.Y, CurrentLocation.Z));

        if (bApplyGoalFacing)
        {
            const float RemainingYaw =
                FMath::Abs(
                    FMath::FindDeltaAngleDegrees(
                        OwnerUnit->GetActorRotation().Yaw,
                        GoalFacingYaw));

            if (RemainingYaw > FinalFacingToleranceDegrees)
            {
                bTurningToGoalFacing = true;
                OwnerUnit->SetUnitState(EStrategyUnitState::Reforming);
                return;
            }
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
    bTurningToGoalFacing = false;
    PauseRemainingSeconds = 0.0f;
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
        case EStrategyOrderType::Charge:
            return true;

        default:
            return false;
    }
}


void UStrategyMovementExecutorComponent::PauseMovementForSeconds(float DurationSeconds)
{
    if (!bHasMovementGoal || DurationSeconds <= 0.0f || !OwnerUnit)
    {
        return;
    }

    PauseRemainingSeconds = FMath::Max(PauseRemainingSeconds, DurationSeconds);
    OwnerUnit->SetUnitState(EStrategyUnitState::UnderFire);

    // Deliberately do not change OrderComponent execution state or route.
    // The authoritative parent mission remains active and movement resumes.
    SetComponentTickEnabled(true);
}


bool UStrategyMovementExecutorComponent::TryRetargetDuringBridge(
    const FStrategyOrder& Order)
{
    if (!OwnerUnit ||
        !OwnerUnit->RoutePlanner ||
        !bHasMovementGoal ||
        !ActiveRoutePlan.bUsesBridge ||
        ActiveRoutePlan.BridgeExitPointIndex == INDEX_NONE ||
        RoutePointIndex > ActiveRoutePlan.BridgeExitPointIndex ||
        !RoutePoints.IsValidIndex(ActiveRoutePlan.BridgeExitPointIndex))
    {
        return false;
    }

    const int32 OriginalRoutePointIndex = RoutePointIndex;
    const int32 OriginalBridgeExitIndex = ActiveRoutePlan.BridgeExitPointIndex;

    const FVector PreservedBridgeExit =
        RoutePoints[OriginalBridgeExitIndex];

    TArray<FVector> PreservedPoints;
    const int32 KeepThroughIndex =
        FMath::Clamp(
            OriginalBridgeExitIndex,
            OriginalRoutePointIndex,
            RoutePoints.Num() - 1);

    for (int32 Index = RoutePointIndex; Index <= KeepThroughIndex; ++Index)
    {
        PreservedPoints.Add(RoutePoints[Index]);
    }

    const FStrategyRoutePlan TailPlan =
        OwnerUnit->RoutePlanner->BuildRoutePlan(
            PreservedBridgeExit,
            Order.TargetLocation);

    if (!TailPlan.bValid)
    {
        return false;
    }

    for (const FVector& Point : TailPlan.Points)
    {
        PreservedPoints.Add(Point);
    }

    RoutePoints = PreservedPoints;
    RoutePointIndex = 0;

    ActiveRoutePlan.BridgeEnterPointIndex =
        FMath::Max(
            0,
            ActiveRoutePlan.BridgeEnterPointIndex - OriginalRoutePointIndex);

    ActiveRoutePlan.BridgeExitPointIndex =
        FMath::Max(
            0,
            OriginalBridgeExitIndex - OriginalRoutePointIndex);
    ActiveRoutePlan.bUsesBridge = true;
    ActiveRoutePlan.bValid = true;

    MovementGoal = Order.TargetLocation;
    GoalFacingYaw = Order.FacingYaw;
    bApplyGoalFacing = Order.bHasFacing;
    ExecutingOrderSerial = Order.OrderSerial;
    bTurningToGoalFacing = false;

    OwnerUnit->OrderComponent->BeginExecution();
    SetComponentTickEnabled(true);

    UE_LOG(
        LogTemp,
        Display,
        TEXT("PROJECT1864-MOVE: %s retargeted while preserving active bridge transaction."),
        *OwnerUnit->StableUnitId.ToString());

    return true;
}
