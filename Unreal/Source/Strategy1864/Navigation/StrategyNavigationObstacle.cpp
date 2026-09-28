#include "StrategyNavigationObstacle.h"

AStrategyNavigationObstacle::AStrategyNavigationObstacle()
{
    PrimaryActorTick.bCanEverTick = false;
}

FBox AStrategyNavigationObstacle::GetExpandedBounds() const
{
    const FVector Extent =
        HalfExtentCm + FVector(ClearanceCm, ClearanceCm, 0.0f);

    return FBox(
        GetActorLocation() - Extent,
        GetActorLocation() + Extent);
}

bool AStrategyNavigationObstacle::IntersectsSegment2D(
    const FVector& Start,
    const FVector& End) const
{
    const FBox Bounds = GetExpandedBounds();

    FVector FlatStart = Start;
    FVector FlatEnd = End;
    FlatStart.Z = GetActorLocation().Z;
    FlatEnd.Z = GetActorLocation().Z;

    return FMath::LineBoxIntersection(
        Bounds,
        FlatStart,
        FlatEnd,
        FlatEnd - FlatStart);
}

FVector AStrategyNavigationObstacle::BuildDetourPoint(
    const FVector& Start,
    const FVector& End) const
{
    FVector Direction = End - Start;
    Direction.Z = 0.0f;
    Direction = Direction.GetSafeNormal();

    if (Direction.IsNearlyZero())
    {
        Direction = FVector::ForwardVector;
    }

    const FVector Right(-Direction.Y, Direction.X, 0.0f);
    const FVector Center = GetActorLocation();

    const FVector CandidateA =
        Center + Right * (HalfExtentCm.Size2D() + ClearanceCm);
    const FVector CandidateB =
        Center - Right * (HalfExtentCm.Size2D() + ClearanceCm);

    const float CostA =
        FVector::DistSquared2D(Start, CandidateA) +
        FVector::DistSquared2D(CandidateA, End);

    const float CostB =
        FVector::DistSquared2D(Start, CandidateB) +
        FVector::DistSquared2D(CandidateB, End);

    return CostA <= CostB ? CandidateA : CandidateB;
}
