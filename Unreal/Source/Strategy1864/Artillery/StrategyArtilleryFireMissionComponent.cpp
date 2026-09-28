#include "StrategyArtilleryFireMissionComponent.h"

#include "StrategyArtilleryBatteryUnit.h"
#include "StrategyArtilleryDeploymentComponent.h"
#include "StrategyArtilleryAmmunitionComponent.h"
#include "StrategyArtilleryTraverseComponent.h"
#include "StrategyArtilleryDamageComponent.h"
#include "../Combat/StrategyContactComponent.h"
#include "../Combat/StrategyVisibilityComponent.h"
#include "../Combat/StrategyCombatComponent.h"
#include "../Combat/StrategyDirectionalCoverComponent.h"
#include "../Combat/StrategyStanceComponent.h"
#include "../Combat/StrategySkirmisherComponent.h"
#include "../Combat/StrategySmokeField.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Units/StrategyUnit.h"
#include "EngineUtils.h"
#include "Engine/World.h"

UStrategyArtilleryFireMissionComponent::UStrategyArtilleryFireMissionComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
}

void UStrategyArtilleryFireMissionComponent::BeginPlay()
{
    Super::BeginPlay();

    OwnerBattery = Cast<AStrategyArtilleryBatteryUnit>(GetOwner());

    const int32 Seed =
        OwnerBattery
        ? static_cast<int32>(GetTypeHash(OwnerBattery->StableUnitId)) ^ 0xA1864
        : GetUniqueID();

    RandomStream.Initialize(Seed);
}

void UStrategyArtilleryFireMissionComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!OwnerBattery)
    {
        return;
    }

    if (ReloadRemainingSeconds > 0.0f)
    {
        ReloadRemainingSeconds =
            FMath::Max(0.0f, ReloadRemainingSeconds - DeltaTime);
    }

    EvaluationAccumulator += DeltaTime;
    if (EvaluationAccumulator < EvaluationIntervalSeconds)
    {
        return;
    }

    EvaluationAccumulator = 0.0f;

    if (FireMode == EStrategyArtilleryFireMode::HoldFire ||
        ReloadRemainingSeconds > 0.0f ||
        !OwnerBattery->CanFireBattery())
    {
        return;
    }

    AStrategyUnit* Target =
        FireMode == EStrategyArtilleryFireMode::ManualTarget
        ? ManualTarget.Get()
        : FindBestAutoTarget();

    if (!IsValid(Target) || !Target->IsCombatEffective())
    {
        if (FireMode == EStrategyArtilleryFireMode::ManualTarget)
        {
            CompleteManualMission(!IsValid(Target));
        }
        return;
    }

    if (OwnerBattery->ArtilleryTraverseComponent &&
        !OwnerBattery->ArtilleryTraverseComponent
            ->IsLocationInsideTraverseArc(Target->GetActorLocation()))
    {
        OwnerBattery->ArtilleryTraverseComponent
            ->RequestTraverseToward(Target->GetActorLocation());
        return;
    }

    if (CanEngageTarget(Target))
    {
        FireAt(Target);
    }
}

bool UStrategyArtilleryFireMissionComponent::SetManualTarget(
    AStrategyUnit* Target,
    EStrategyOrderAuthority Authority)
{
    if (!OwnerBattery ||
        !OwnerBattery->OrderComponent ||
        !IsValid(Target) ||
        Target == OwnerBattery ||
        Target->Side == EStrategySide::Neutral ||
        Target->Side == OwnerBattery->Side)
    {
        return false;
    }

    ManualTarget = Target;
    FireMode = EStrategyArtilleryFireMode::ManualTarget;
    PreviousNonHoldMode = FireMode;

    FStrategyOrder Order;
    Order.Type = EStrategyOrderType::ArtilleryFireMission;
    Order.TargetLocation = Target->GetActorLocation();
    Order.FacingYaw =
        (Target->GetActorLocation() - OwnerBattery->GetActorLocation())
        .Rotation().Yaw;
    Order.bHasFacing = true;
    Order.Authority = Authority;

    if (!OwnerBattery->OrderComponent->SetOrder(Order))
    {
        ManualTarget = nullptr;
        return false;
    }

    OwnerBattery->OrderComponent->BeginExecution();
    return true;
}

void UStrategyArtilleryFireMissionComponent::SetAutoTargetEnabled(
    bool bEnabled)
{
    if (!OwnerBattery)
    {
        return;
    }

    if (bEnabled)
    {
        FireMode = EStrategyArtilleryFireMode::AutoTarget;
        PreviousNonHoldMode = FireMode;
        ManualTarget = nullptr;
    }
    else
    {
        FireMode = EStrategyArtilleryFireMode::ManualTarget;
        PreviousNonHoldMode = FireMode;
    }
}

void UStrategyArtilleryFireMissionComponent::SetHoldFire(bool bHold)
{
    if (bHold)
    {
        if (FireMode != EStrategyArtilleryFireMode::HoldFire)
        {
            PreviousNonHoldMode = FireMode;
        }

        FireMode = EStrategyArtilleryFireMode::HoldFire;

        if (OwnerBattery &&
            OwnerBattery->OrderComponent &&
            OwnerBattery->OrderComponent->GetCurrentOrder().Type ==
                EStrategyOrderType::ArtilleryFireMission)
        {
            OwnerBattery->OrderComponent->CompleteExecution();
        }
    }
    else if (FireMode == EStrategyArtilleryFireMode::HoldFire)
    {
        FireMode = PreviousNonHoldMode;

        if (OwnerBattery &&
            OwnerBattery->OrderComponent &&
            OwnerBattery->OrderComponent->GetCurrentOrder().Type ==
                EStrategyOrderType::ArtilleryFireMission)
        {
            OwnerBattery->OrderComponent->BeginExecution();
        }
    }
}

bool UStrategyArtilleryFireMissionComponent::SelectAmmo(
    EStrategyArtilleryAmmoType AmmoType)
{
    return OwnerBattery &&
        OwnerBattery->ArtilleryAmmunitionComponent &&
        OwnerBattery->ArtilleryAmmunitionComponent->SelectAmmo(AmmoType);
}

float UStrategyArtilleryFireMissionComponent::GetMinimumRangeCm(
    EStrategyArtilleryAmmoType AmmoType) const
{
    if (!OwnerBattery)
    {
        return 0.0f;
    }

    switch (AmmoType)
    {
        case EStrategyArtilleryAmmoType::Canister:
            return 0.0f;
        case EStrategyArtilleryAmmoType::Shrapnel:
            return OwnerBattery->GunProfile.MinimumRangeCm * 1.10f;
        case EStrategyArtilleryAmmoType::Shell:
            return OwnerBattery->GunProfile.MinimumRangeCm * 1.25f;
        case EStrategyArtilleryAmmoType::RoundShot:
        default:
            return OwnerBattery->GunProfile.MinimumRangeCm;
    }
}

float UStrategyArtilleryFireMissionComponent::GetMaximumRangeCm(
    EStrategyArtilleryAmmoType AmmoType) const
{
    if (!OwnerBattery)
    {
        return 0.0f;
    }

    const float MaxRange = OwnerBattery->GunProfile.MaximumRangeCm;

    switch (AmmoType)
    {
        case EStrategyArtilleryAmmoType::Canister:
            return FMath::Min(40000.0f, MaxRange * 0.22f);
        case EStrategyArtilleryAmmoType::Shrapnel:
            return MaxRange * 0.75f;
        case EStrategyArtilleryAmmoType::Shell:
            return MaxRange * 0.95f;
        case EStrategyArtilleryAmmoType::RoundShot:
        default:
            return MaxRange;
    }
}

bool UStrategyArtilleryFireMissionComponent::CanEngageTarget(
    const AStrategyUnit* Target) const
{
    if (!OwnerBattery ||
        !OwnerBattery->CanFireBattery() ||
        FireMode == EStrategyArtilleryFireMode::HoldFire ||
        !IsValid(Target) ||
        !Target->IsCombatEffective() ||
        !OwnerBattery->ArtilleryAmmunitionComponent ||
        OwnerBattery->ArtilleryAmmunitionComponent->GetSelectedRounds() <= 0 ||
        !OwnerBattery->ArtilleryTraverseComponent ||
        !OwnerBattery->ArtilleryTraverseComponent
            ->IsLocationInsideTraverseArc(Target->GetActorLocation()) ||
        !CanObserveTarget(Target))
    {
        return false;
    }

    const EStrategyArtilleryAmmoType AmmoType =
        OwnerBattery->ArtilleryAmmunitionComponent->SelectedAmmo;

    const float DistanceCm =
        FVector::Dist2D(
            OwnerBattery->GetActorLocation(),
            Target->GetActorLocation());

    return DistanceCm >= GetMinimumRangeCm(AmmoType) &&
           DistanceCm <= GetMaximumRangeCm(AmmoType);
}

bool UStrategyArtilleryFireMissionComponent::CanObserveTarget(
    const AStrategyUnit* Target) const
{
    if (!OwnerBattery ||
        !IsValid(Target) ||
        !OwnerBattery->ContactComponent ||
        !OwnerBattery->VisibilityComponent)
    {
        return false;
    }

    return OwnerBattery->ContactComponent->HasCurrentContact(Target) &&
        OwnerBattery->VisibilityComponent->HasLineOfSightTo(Target);
}

AStrategyUnit* UStrategyArtilleryFireMissionComponent::FindBestAutoTarget() const
{
    if (!OwnerBattery || !GetWorld())
    {
        return nullptr;
    }

    AStrategyUnit* Best = nullptr;
    float BestScore = -TNumericLimits<float>::Max();

    for (TActorIterator<AStrategyUnit> It(GetWorld()); It; ++It)
    {
        AStrategyUnit* Candidate = *It;

        if (!IsValid(Candidate) ||
            Candidate == OwnerBattery ||
            Candidate->Side == EStrategySide::Neutral ||
            Candidate->Side == OwnerBattery->Side ||
            !Candidate->IsCombatEffective() ||
            !CanObserveTarget(Candidate))
        {
            continue;
        }

        if (!OwnerBattery->ArtilleryAmmunitionComponent)
        {
            continue;
        }

        const EStrategyArtilleryAmmoType AmmoType =
            OwnerBattery->ArtilleryAmmunitionComponent->SelectedAmmo;

        const float Distance =
            FVector::Dist2D(
                OwnerBattery->GetActorLocation(),
                Candidate->GetActorLocation());

        if (Distance < GetMinimumRangeCm(AmmoType) ||
            Distance > GetMaximumRangeCm(AmmoType))
        {
            continue;
        }

        const float Score = CalculateTargetScore(Candidate);
        if (Score > BestScore)
        {
            BestScore = Score;
            Best = Candidate;
        }
    }

    return Best;
}

bool UStrategyArtilleryFireMissionComponent::FireAt(
    AStrategyUnit* Target)
{
    if (!CanEngageTarget(Target) ||
        !OwnerBattery->ArtilleryAmmunitionComponent)
    {
        return false;
    }

    const int32 OperationalGuns =
        OwnerBattery->GetOperationalGunCount();

    const int32 GunsFiring =
        FMath::Min(
            OperationalGuns,
            OwnerBattery->ArtilleryAmmunitionComponent->GetSelectedRounds());

    if (GunsFiring <= 0)
    {
        return false;
    }

    const int32 Consumed =
        OwnerBattery->ArtilleryAmmunitionComponent
            ->ConsumeSelectedRounds(GunsFiring);

    if (Consumed <= 0)
    {
        return false;
    }

    const float DistanceCm =
        FVector::Dist2D(
            OwnerBattery->GetActorLocation(),
            Target->GetActorLocation());

    const int32 Casualties =
        ResolveCasualties(Target, Consumed, DistanceCm);

    const EStrategyArtilleryAmmoType AmmoType =
        OwnerBattery->ArtilleryAmmunitionComponent->SelectedAmmo;

    if (AStrategyArtilleryBatteryUnit* TargetBattery =
        Cast<AStrategyArtilleryBatteryUnit>(Target))
    {
        if (TargetBattery->ArtilleryDamageComponent && Casualties > 0)
        {
            const bool bExplosive =
                AmmoType == EStrategyArtilleryAmmoType::Shell ||
                AmmoType == EStrategyArtilleryAmmoType::Shrapnel;

            TargetBattery->ArtilleryDamageComponent
                ->ApplyIncomingHits(Casualties, bExplosive);
        }
    }
    else if (Casualties > 0)
    {
        Target->ApplyStrengthLoss(Casualties);
    }

    if (Target->CombatComponent)
    {
        Target->CombatComponent->NotifyIncomingVolley(Casualties);
    }

    const float CrewRatio =
        OwnerBattery->GunCount > 0
        ? static_cast<float>(OwnerBattery->GetOperationalGunCount()) /
          static_cast<float>(OwnerBattery->GunCount)
        : 0.0f;

    const float ExperienceFactor =
        FMath::Lerp(
            1.12f,
            0.88f,
            FMath::Clamp(OwnerBattery->Experience / 100.0f, 0.0f, 1.0f));

    const float FatigueFactor =
        FMath::Lerp(
            1.0f,
            1.25f,
            FMath::Clamp(OwnerBattery->Fatigue / 100.0f, 0.0f, 1.0f));

    const float CrewFactor =
        FMath::Lerp(1.35f, 1.0f, FMath::Clamp(CrewRatio, 0.0f, 1.0f));

    ReloadRemainingSeconds =
        OwnerBattery->GunProfile.ReloadSeconds *
        ExperienceFactor *
        FatigueFactor *
        CrewFactor;

    OnArtilleryShotResolved.Broadcast(
        Target,
        AmmoType,
        Consumed,
        Casualties);

    if (GetWorld())
    {
        FVector Direction =
            Target->GetActorLocation() - OwnerBattery->GetActorLocation();
        Direction.Z = 0.0f;
        Direction = Direction.GetSafeNormal();

        if (AStrategySmokeField* Smoke =
            GetWorld()->SpawnActor<AStrategySmokeField>(
                AStrategySmokeField::StaticClass(),
                OwnerBattery->GetActorLocation() + Direction * 350.0f,
                Direction.Rotation()))
        {
            Smoke->InitialDensity =
                FMath::Clamp(0.32f + Consumed * 0.05f, 0.32f, 0.70f);
            Smoke->RadiusCm =
                FMath::Clamp(1100.0f + Consumed * 140.0f, 1100.0f, 2400.0f);
        }
    }

    return true;
}

int32 UStrategyArtilleryFireMissionComponent::ResolveCasualties(
    AStrategyUnit* Target,
    int32 GunsFired,
    float DistanceCm)
{
    if (!OwnerBattery ||
        !OwnerBattery->ArtilleryAmmunitionComponent ||
        !IsValid(Target))
    {
        return 0;
    }

    const EStrategyArtilleryAmmoType AmmoType =
        OwnerBattery->ArtilleryAmmunitionComponent->SelectedAmmo;

    const float MaxRange =
        FMath::Max(1.0f, GetMaximumRangeCm(AmmoType));

    const float RangeFraction =
        FMath::Clamp(DistanceCm / MaxRange, 0.0f, 1.0f);

    float HitChance =
        GetBaseGunHitChance(AmmoType) *
        FMath::Lerp(1.0f, 0.45f, RangeFraction);

    if (Target->StanceComponent)
    {
        HitChance *= Target->StanceComponent->GetIncomingHitMultiplier();
    }

    if (Target->DirectionalCoverComponent)
    {
        HitChance *=
            Target->DirectionalCoverComponent->CalculateIncomingHitMultiplier(
                OwnerBattery->GetActorLocation());
    }

    if (Target->SkirmisherComponent)
    {
        HitChance *=
            Target->SkirmisherComponent->GetIncomingHitMultiplier();
    }

    if (OwnerBattery->VisibilityComponent)
    {
        HitChance *=
            OwnerBattery->VisibilityComponent->GetSmokeTransmissionTo(Target);
    }

    HitChance = FMath::Clamp(HitChance, 0.01f, 0.95f);

    const FIntPoint CasualtyRange = GetCasualtyRange(AmmoType);
    int32 Casualties = 0;

    for (int32 GunIndex = 0; GunIndex < GunsFired; ++GunIndex)
    {
        if (RandomStream.FRand() <= HitChance)
        {
            Casualties +=
                RandomStream.RandRange(
                    CasualtyRange.X,
                    CasualtyRange.Y);
        }
    }

    return Casualties;
}

float UStrategyArtilleryFireMissionComponent::CalculateTargetScore(
    const AStrategyUnit* Target) const
{
    if (!OwnerBattery || !IsValid(Target))
    {
        return -TNumericLimits<float>::Max();
    }

    const float Distance =
        FVector::Dist2D(
            OwnerBattery->GetActorLocation(),
            Target->GetActorLocation());

    float Score =
        static_cast<float>(Target->CurrentStrength) * 0.35f -
        Distance / 1000.0f;

    if (Target->Echelon == EStrategyEchelon::Artillery)
    {
        Score += 260.0f;
    }
    else if (Target->Echelon == EStrategyEchelon::Company)
    {
        Score += 100.0f;
    }
    else if (Target->Echelon == EStrategyEchelon::Cavalry)
    {
        Score += 80.0f;
    }

    if (Target->UnitState == EStrategyUnitState::Routed)
    {
        Score -= 300.0f;
    }

    return Score;
}

float UStrategyArtilleryFireMissionComponent::GetBaseGunHitChance(
    EStrategyArtilleryAmmoType AmmoType) const
{
    switch (AmmoType)
    {
        case EStrategyArtilleryAmmoType::Canister:
            return 0.82f;
        case EStrategyArtilleryAmmoType::Shrapnel:
            return 0.58f;
        case EStrategyArtilleryAmmoType::Shell:
            return 0.48f;
        case EStrategyArtilleryAmmoType::RoundShot:
        default:
            return 0.55f;
    }
}

FIntPoint UStrategyArtilleryFireMissionComponent::GetCasualtyRange(
    EStrategyArtilleryAmmoType AmmoType) const
{
    switch (AmmoType)
    {
        case EStrategyArtilleryAmmoType::Canister:
            return FIntPoint(4, 8);
        case EStrategyArtilleryAmmoType::Shrapnel:
            return FIntPoint(2, 5);
        case EStrategyArtilleryAmmoType::Shell:
            return FIntPoint(1, 4);
        case EStrategyArtilleryAmmoType::RoundShot:
        default:
            return FIntPoint(1, 3);
    }
}

void UStrategyArtilleryFireMissionComponent::CompleteManualMission(
    bool bFailed)
{
    ManualTarget = nullptr;

    if (!OwnerBattery || !OwnerBattery->OrderComponent)
    {
        return;
    }

    if (bFailed)
    {
        OwnerBattery->OrderComponent->FailExecution();
    }
    else
    {
        OwnerBattery->OrderComponent->CompleteExecution();
    }
}
