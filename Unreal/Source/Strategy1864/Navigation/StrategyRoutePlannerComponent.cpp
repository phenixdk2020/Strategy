#include "StrategyRoutePlannerComponent.h"

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
    TArray<FVector> Result;

    if (!bUseNavigationSystem || !GetWorld())
    {
        Result.Add(EndLocation);
        return Result;
    }

    UNavigationPath* Path = UNavigationSystemV1::FindPathToLocationSynchronously(
        GetWorld(),
        StartLocation,
        EndLocation,
        GetOwner());

    if (!Path || !Path->IsValid() || Path->PathPoints.Num() < 2)
    {
        Result.Add(EndLocation);
        return Result;
    }

    Result.Reserve(Path->PathPoints.Num() - 1);

    for (int32 Index = 1; Index < Path->PathPoints.Num(); ++Index)
    {
        Result.Add(Path->PathPoints[Index]);
    }

    if (Result.Num() == 0)
    {
        Result.Add(EndLocation);
    }

    return Result;
}
