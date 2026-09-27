#include "StrategyCombatComponent.h"

#include "StrategyFireControlComponent.h"
#include "../Units/StrategyUnit.h"
#include "EngineUtils.h"

UStrategyCombatComponent::UStrategyCombatComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
}

void UStrategyCombatComponent::BeginPlay()
{
    Super::BeginPlay();

    OwnerUnit = Cast<AStrategyUnit>(GetOwner());

    const int32 Seed =
        OwnerUnit
        ? static_cast<int32>(GetTypeHash(OwnerUnit->StableUnitId))
        : GetUniqueID();

    RandomStream.Initialize(Seed);
}

void UStrategyCombatComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!OwnerUnit || !OwnerUnit->IsCombatEffective())
    {
        return;
    }

    if (ReloadRemainingSeconds > 0.0f)
    {
        ReloadRemainingSeconds = FMath::Max(
            0.0f,
            ReloadRemainingSeconds - DeltaTime);
        return;
    }

    if (AmmunitionRounds <= 0)
    {
        return;
    }

    AStrategyUnit* Target = FindBestTarget();
    if (Target)
    {
        TryFireAt(Target);
    }
}

bool UStrategyCombatComponent::TryFireAt(AStrategyUnit* Target)
{
    if (!OwnerUnit ||
        !OwnerUnit->FireControlComponent ||
        !OwnerUnit->FireControlComponent->CanEngageTarget(Target) ||
        ReloadRemainingSeconds > 0.0f ||
        AmmunitionRounds <= 0)
    {
        return false;
    }

    const int32 ShotCount = FMath::Min3(
        FMath::Max(0, OwnerUnit->CurrentStrength),
        MaxShotsPerVolley,
        AmmunitionRounds);

    if (ShotCount <= 0)
    {
        return false;
    }

    AmmunitionRounds -= ShotCount;

    const float DistanceCm = FVector::Dist2D(
        OwnerUnit->GetActorLocation(),
        Target->GetActorLocation());

    const int32 Hits = ResolveHits(ShotCount, DistanceCm);

    if (Hits > 0)
    {
        Target->ApplyStrengthLoss(Hits);
    }

    ReloadRemainingSeconds = ReloadSeconds;
    OnVolleyResolved.Broadcast(Target, ShotCount, Hits);

    return true;
}

AStrategyUnit* UStrategyCombatComponent::FindBestTarget() const
{
    if (!OwnerUnit || !OwnerUnit->FireControlComponent || !GetWorld())
    {
        return nullptr;
    }

    AStrategyUnit* BestTarget = nullptr;
    float BestDistanceCm = TNumericLimits<float>::Max();

    for (TActorIterator<AStrategyUnit> It(GetWorld()); It; ++It)
    {
        AStrategyUnit* Candidate = *It;

        if (!IsValid(Candidate) ||
            Candidate == OwnerUnit ||
            !OwnerUnit->FireControlComponent->CanEngageTarget(Candidate))
        {
            continue;
        }

        const float DistanceCm = FVector::Dist2D(
            OwnerUnit->GetActorLocation(),
            Candidate->GetActorLocation());

        if (DistanceCm < BestDistanceCm)
        {
            BestDistanceCm = DistanceCm;
            BestTarget = Candidate;
        }
    }

    return BestTarget;
}

int32 UStrategyCombatComponent::ResolveHits(int32 ShotCount, float DistanceCm)
{
    if (!OwnerUnit || !OwnerUnit->FireControlComponent || ShotCount <= 0)
    {
        return 0;
    }

    const float ActiveRangeCm =
        FMath::Max(1.0f, OwnerUnit->FireControlComponent->GetActiveRangeCm());

    const float RangeFactor =
        FMath::Clamp(1.0f - (DistanceCm / ActiveRangeCm) * 0.55f, 0.25f, 1.0f);

    const float HitChance =
        FMath::Clamp(BaseHitChance * RangeFactor, 0.0f, 1.0f);

    int32 Hits = 0;

    for (int32 Index = 0; Index < ShotCount; ++Index)
    {
        if (RandomStream.FRand() < HitChance)
        {
            ++Hits;
        }
    }

    return Hits;
}
