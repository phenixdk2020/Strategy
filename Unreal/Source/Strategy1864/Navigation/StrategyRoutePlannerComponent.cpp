#include "StrategyRoutePlannerComponent.h"

#include "StrategyRiverBarrier.h"
#include "EngineUtils.h"
#include "NavigationPath.h"
#include "NavigationSystem.h"

UStrategyRoutePlannerComponent::UStrategyRoutePlannerComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

TArray<FVector> UStrategyRoutePlannerComponent::BuildRoute(
    const FVector& StartLocation,
    const FVector& EndLocation) const
{
    return BuildRoutePlan(StartLocation, EndLocation).Points;
}

FStrategyRoutePlan UStrategyRoutePlannerComponent::BuildRoutePlan(
    const FVector& StartLocation,
    const FVector& EndLocation) const
{
    FStrategyRoutePlan Plan;

    AStrategyRiverBarrier* River = FindRelevantRiverBarrier(
        StartLocation,
        EndLocation);

    if (!River)
    {
        AppendNavSegment(StartLocation, EndLocation, Plan.Points);
        return Plan;
    }

    if (River->IsInsideRiver(StartLocation))
    {
        Plan.bValid = false;
        Plan.FailureReason = TEXT("Start location is inside hard-blocked water.");
        return Plan;
    }

    if (River->IsInsideRiver(EndLocation))
    {
        Plan.bValid = false;
        Plan.FailureReason = TEXT("Destination is inside hard-blocked water.");
        return Plan;
    }

    if (River->RequiresBankChange(StartLocation, EndLocation))
    {
        const int32 StartSide = River->GetBankSide(StartLocation);
        const int32 EndSide = River->GetBankSide(EndLocation);

        const FVector NearApproach = River->GetBridgeApproachForSide(StartSide);
        const FVector FarApproach = River->GetBridgeApproachForSide(EndSide);
        const FVector FarExit = River->GetBridgeExitForSide(EndSide);

        AppendNavSegment(StartLocation, NearApproach, Plan.Points);
        Plan.BridgeEnterPointIndex = Plan.Points.Num() - 1;

        Plan.Points.Add(FarApproach);
        Plan.Points.Add(FarExit);
        Plan.BridgeExitPointIndex = Plan.Points.Num() - 1;
        Plan.bUsesBridge = true;

        AppendNavSegment(FarExit, EndLocation, Plan.Points);
        return Plan;
    }

    if (River->SameBankChordIntersectsRiver(StartLocation, EndLocation))
    {
        const int32 Side = River->GetBankSide(StartLocation);
        const FVector BankWaypoint = River->GetBridgeExitForSide(Side);

        AppendNavSegment(StartLocation, BankWaypoint, Plan.Points);
        AppendNavSegment(BankWaypoint, EndLocation, Plan.Points);

        Plan.bSameBankDetour = true;
        return Plan;
    }

    AppendNavSegment(StartLocation, EndLocation, Plan.Points);
    return Plan;
}

AStrategyRiverBarrier* UStrategyRoutePlannerComponent::FindRelevantRiverBarrier(
    const FVector& StartLocation,
    const FVector& EndLocation) const
{
    if (!GetWorld())
    {
        return nullptr;
    }

    AStrategyRiverBarrier* BestBarrier = nullptr;
    float BestDistanceSq = TNumericLimits<float>::Max();
    const FVector Midpoint = (StartLocation + EndLocation) * 0.5f;

    for (TActorIterator<AStrategyRiverBarrier> It(GetWorld()); It; ++It)
    {
        AStrategyRiverBarrier* Barrier = *It;
        if (!IsValid(Barrier))
        {
            continue;
        }

        if (!Barrier->IsInsideRiver(StartLocation) &&
            !Barrier->IsInsideRiver(EndLocation) &&
            !Barrier->RequiresBankChange(StartLocation, EndLocation) &&
            !Barrier->SameBankChordIntersectsRiver(StartLocation, EndLocation))
        {
            continue;
        }

        const float DistanceSq = FVector::DistSquared2D(
            Midpoint,
            Barrier->GetActorLocation());

        if (DistanceSq < BestDistanceSq)
        {
            BestDistanceSq = DistanceSq;
            BestBarrier = Barrier;
        }
    }

    return BestBarrier;
}

void UStrategyRoutePlannerComponent::AppendNavSegment(
    const FVector& SegmentStart,
    const FVector& SegmentEnd,
    TArray<FVector>& InOutPoints) const
{
    if (!bUseNavigationSystem || !GetWorld())
    {
        InOutPoints.Add(SegmentEnd);
        return;
    }

    UNavigationPath* Path = UNavigationSystemV1::FindPathToLocationSynchronously(
        GetWorld(),
        SegmentStart,
        SegmentEnd,
        GetOwner());

    if (!Path || !Path->IsValid() || Path->PathPoints.Num() < 2)
    {
        InOutPoints.Add(SegmentEnd);
        return;
    }

    for (int32 Index = 1; Index < Path->PathPoints.Num(); ++Index)
    {
        InOutPoints.Add(Path->PathPoints[Index]);
    }
}
