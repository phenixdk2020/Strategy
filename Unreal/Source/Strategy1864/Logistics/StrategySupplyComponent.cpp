#include "StrategySupplyComponent.h"

#include "../Combat/StrategyCombatComponent.h"
#include "../Units/StrategyUnit.h"
#include "../Artillery/StrategyArtilleryBatteryUnit.h"
#include "../Artillery/StrategyArtilleryAmmunitionComponent.h"
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
        float Fraction = 1.0f;
        bool bHasAmmoStore = false;

        if (const AStrategyArtilleryBatteryUnit* Battery =
            Cast<AStrategyArtilleryBatteryUnit>(OwnerUnit))
        {
            if (Battery->ArtilleryAmmunitionComponent &&
                Battery->ArtilleryAmmunitionComponent->MaximumTotalRounds > 0)
            {
                Fraction =
                    static_cast<float>(
                        Battery->ArtilleryAmmunitionComponent->GetTotalRounds()) /
                    static_cast<float>(
                        Battery->ArtilleryAmmunitionComponent->MaximumTotalRounds);
                bHasAmmoStore = true;
            }
        }
        else if (OwnerUnit->CombatComponent &&
                 OwnerUnit->CombatComponent->MaxAmmunitionRounds > 0)
        {
            Fraction =
                static_cast<float>(OwnerUnit->CombatComponent->AmmunitionRounds) /
                static_cast<float>(OwnerUnit->CombatComponent->MaxAmmunitionRounds);
            bHasAmmoStore = true;
        }

        if (bHasAmmoStore)
        {
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

        float AmmoFraction = 1.0f;

        if (const AStrategyArtilleryBatteryUnit* Battery =
            Cast<AStrategyArtilleryBatteryUnit>(Candidate))
        {
            if (!Battery->ArtilleryAmmunitionComponent)
            {
                continue;
            }

            const int32 MaxAmmo =
                FMath::Max(
                    1,
                    Battery->ArtilleryAmmunitionComponent->MaximumTotalRounds);

            AmmoFraction =
                static_cast<float>(
                    Battery->ArtilleryAmmunitionComponent->GetTotalRounds()) /
                static_cast<float>(MaxAmmo);
        }
        else
        {
            if (!Candidate->CombatComponent ||
                Candidate->CombatComponent->MaxAmmunitionRounds <= 0)
            {
                continue;
            }

            const int32 MaxAmmo =
                FMath::Max(
                    1,
                    Candidate->CombatComponent->MaxAmmunitionRounds);

            AmmoFraction =
                static_cast<float>(Candidate->CombatComponent->AmmunitionRounds) /
                static_cast<float>(MaxAmmo);
        }

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
        StoredAmmunitionRounds <= 0)
    {
        return;
    }

    if (AStrategyArtilleryBatteryUnit* Battery =
        Cast<AStrategyArtilleryBatteryUnit>(Receiver))
    {
        if (!Battery->ArtilleryAmmunitionComponent)
        {
            return;
        }

        const int32 Missing =
            Battery->ArtilleryAmmunitionComponent->GetMissingRounds();

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

        const int32 Added =
            Battery->ArtilleryAmmunitionComponent->AddCompatibleMixedRounds(
                Transfer);

        StoredAmmunitionRounds -= Added;

        if (Battery->ArtilleryAmmunitionComponent->GetMissingRounds() <= 0)
        {
            Receiver->SupplyComponent->bRequestingResupply = false;
        }

        return;
    }

    if (!Receiver->CombatComponent)
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
