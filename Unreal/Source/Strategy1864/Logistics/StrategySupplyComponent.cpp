#include "StrategySupplyComponent.h"

#include "../Combat/StrategyCombatComponent.h"
#include "../Units/StrategyUnit.h"
#include "EngineUtils.h"

UStrategySupplyComponent::UStrategySupplyComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
}

void UStrategySupplyComponent::BeginPlay()
{
    Super::BeginPlay();
    OwnerUnit = Cast<AStrategyUnit>(GetOwner());
}

void UStrategySupplyComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!OwnerUnit)
    {
        return;
    }

    if (!bActsAsSupplySource)
    {
        if (OwnerUnit->CombatComponent &&
            OwnerUnit->CombatComponent->MaxAmmunitionRounds > 0)
        {
            const float Fraction =
                static_cast<float>(OwnerUnit->CombatComponent->AmmunitionRounds) /
                static_cast<float>(OwnerUnit->CombatComponent->MaxAmmunitionRounds);

            if (Fraction <= AutomaticRequestThresholdFraction)
            {
                bRequestingResupply = true;
            }
            else if (Fraction >= 0.90f)
            {
                bRequestingResupply = false;
            }
        }

        return;
    }

    if (StoredAmmunitionRounds <= 0)
    {
        return;
    }

    if (AStrategyUnit* Receiver = FindBestReceiver())
    {
        TransferTo(Receiver, DeltaTime);
    }
}

int32 UStrategySupplyComponent::AddStoredAmmunition(int32 Rounds)
{
    if (Rounds <= 0)
    {
        return 0;
    }

    const int32 Before = StoredAmmunitionRounds;
    StoredAmmunitionRounds =
        FMath::Clamp(
            StoredAmmunitionRounds + Rounds,
            0,
            FMath::Max(1, MaxStoredAmmunitionRounds));

    return StoredAmmunitionRounds - Before;
}

AStrategyUnit* UStrategySupplyComponent::FindBestReceiver() const
{
    if (!OwnerUnit || !GetWorld())
    {
        return nullptr;
    }

    AStrategyUnit* Best = nullptr;
    float BestAmmoFraction = 2.0f;

    for (TActorIterator<AStrategyUnit> It(GetWorld()); It; ++It)
    {
        AStrategyUnit* Candidate = *It;

        if (!IsValid(Candidate) ||
            Candidate == OwnerUnit ||
            Candidate->Side != OwnerUnit->Side ||
            !Candidate->SupplyComponent ||
            !Candidate->SupplyComponent->bRequestingResupply ||
            !Candidate->CombatComponent ||
            Candidate->UnitState == EStrategyUnitState::Destroyed)
        {
            continue;
        }

        if (FVector::Dist2D(
                OwnerUnit->GetActorLocation(),
                Candidate->GetActorLocation()) > ResupplyRadiusCm)
        {
            continue;
        }

        const int32 MaxAmmo =
            FMath::Max(1, Candidate->CombatComponent->MaxAmmunitionRounds);

        const float AmmoFraction =
            static_cast<float>(Candidate->CombatComponent->AmmunitionRounds) /
            static_cast<float>(MaxAmmo);

        if (AmmoFraction < BestAmmoFraction)
        {
            BestAmmoFraction = AmmoFraction;
            Best = Candidate;
        }
    }

    return Best;
}

void UStrategySupplyComponent::TransferTo(
    AStrategyUnit* Receiver,
    float DeltaTime)
{
    if (!IsValid(Receiver) ||
        !Receiver->CombatComponent ||
        StoredAmmunitionRounds <= 0)
    {
        return;
    }

    const int32 Missing =
        FMath::Max(
            0,
            Receiver->CombatComponent->MaxAmmunitionRounds -
            Receiver->CombatComponent->AmmunitionRounds);

    if (Missing <= 0)
    {
        if (Receiver->SupplyComponent)
        {
            Receiver->SupplyComponent->bRequestingResupply = false;
        }
        return;
    }

    const int32 Transfer =
        FMath::Clamp(
            FMath::CeilToInt(
                FMath::Max(1.0f, TransferRoundsPerSecond) * DeltaTime),
            1,
            FMath::Min(Missing, StoredAmmunitionRounds));

    Receiver->CombatComponent->ResupplyAmmunition(Transfer);
    StoredAmmunitionRounds -= Transfer;

    if (Receiver->CombatComponent->AmmunitionRounds >=
        Receiver->CombatComponent->MaxAmmunitionRounds)
    {
        if (Receiver->SupplyComponent)
        {
            Receiver->SupplyComponent->bRequestingResupply = false;
        }
    }
}
