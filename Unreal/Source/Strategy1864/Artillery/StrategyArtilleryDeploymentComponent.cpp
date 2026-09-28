#include "StrategyArtilleryDeploymentComponent.h"

#include "StrategyArtilleryBatteryUnit.h"
#include "../Movement/StrategyMovementExecutorComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "Engine/World.h"

UStrategyArtilleryDeploymentComponent::UStrategyArtilleryDeploymentComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
}

void UStrategyArtilleryDeploymentComponent::BeginPlay()
{
    Super::BeginPlay();
    OwnerBattery = Cast<AStrategyArtilleryBatteryUnit>(GetOwner());
}

void UStrategyArtilleryDeploymentComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!OwnerBattery)
    {
        return;
    }

    if (IsTransitioning())
    {
        TransitionRemainingSeconds =
            FMath::Max(0.0f, TransitionRemainingSeconds - DeltaTime);

        if (TransitionRemainingSeconds <= 0.0f)
        {
            CompleteTransition();
        }

        return;
    }

    if (MobilityState == EStrategyArtilleryMobilityState::Manhandling &&
        OwnerBattery->MovementExecutor &&
        OwnerBattery->MovementExecutor->HasMovementGoal())
    {
        OwnerBattery->Fatigue =
            FMath::Clamp(
                OwnerBattery->Fatigue +
                ManhandlingFatiguePerSecond * DeltaTime,
                0.0f,
                100.0f);
    }

    if (MobilityState == EStrategyArtilleryMobilityState::Manhandling &&
        OwnerBattery->MovementExecutor &&
        !OwnerBattery->MovementExecutor->HasMovementGoal())
    {
        MobilityState = EStrategyArtilleryMobilityState::Deployed;

        if (PreviousMoveSpeedCmPerSecond > 0.0f)
        {
            OwnerBattery->MovementExecutor->MoveSpeedCmPerSecond =
                PreviousMoveSpeedCmPerSecond;
        }

        PreviousMoveSpeedCmPerSecond = 0.0f;

        if (OwnerBattery->UnitState != EStrategyUnitState::Routed &&
            OwnerBattery->UnitState != EStrategyUnitState::Destroyed)
        {
            OwnerBattery->SetUnitState(EStrategyUnitState::Ready);
        }
    }
}

bool UStrategyArtilleryDeploymentComponent::RequestDeploy()
{
    if (!OwnerBattery ||
        MobilityState != EStrategyArtilleryMobilityState::Limbered ||
        OwnerBattery->OwnershipState !=
            EStrategyArtilleryOwnershipState::Operational ||
        !CanDeployAtCurrentLocation() ||
        (OwnerBattery->MovementExecutor &&
         OwnerBattery->MovementExecutor->HasMovementGoal()))
    {
        return false;
    }

    MobilityState = EStrategyArtilleryMobilityState::Deploying;
    TransitionRemainingSeconds = FMath::Max(0.1f, UnlimberSeconds);
    OwnerBattery->SetUnitState(EStrategyUnitState::Reforming);
    return true;
}

bool UStrategyArtilleryDeploymentComponent::RequestLimber()
{
    if (!OwnerBattery ||
        MobilityState != EStrategyArtilleryMobilityState::Deployed ||
        OwnerBattery->OwnershipState !=
            EStrategyArtilleryOwnershipState::Operational ||
        OwnerBattery->HorseStrength <= 0 ||
        OwnerBattery->DriverStrength <= 0)
    {
        return false;
    }

    MobilityState = EStrategyArtilleryMobilityState::Limbering;
    TransitionRemainingSeconds = FMath::Max(0.1f, LimberSeconds);
    OwnerBattery->SetUnitState(EStrategyUnitState::Reforming);
    return true;
}

bool UStrategyArtilleryDeploymentComponent::RequestManhandle(
    const FVector& TargetLocation,
    float FacingYaw,
    EStrategyOrderAuthority Authority)
{
    if (!OwnerBattery ||
        MobilityState != EStrategyArtilleryMobilityState::Deployed ||
        OwnerBattery->OwnershipState !=
            EStrategyArtilleryOwnershipState::Operational ||
        OwnerBattery->CrewStrength <= 0 ||
        !OwnerBattery->MovementExecutor ||
        !OwnerBattery->OrderComponent)
    {
        return false;
    }

    const float DistanceCm =
        FVector::Dist2D(
            OwnerBattery->GetActorLocation(),
            TargetLocation);

    if (DistanceCm > MaximumManhandleDistanceCm)
    {
        return false;
    }

    PreviousMoveSpeedCmPerSecond =
        OwnerBattery->MovementExecutor->MoveSpeedCmPerSecond;

    OwnerBattery->MovementExecutor->MoveSpeedCmPerSecond =
        ManhandleSpeedCmPerSecond;

    MobilityState = EStrategyArtilleryMobilityState::Manhandling;

    FStrategyOrder Order;
    Order.Type = EStrategyOrderType::Move;
    Order.TargetLocation = TargetLocation;
    Order.FacingYaw = FacingYaw;
    Order.bHasFacing = true;
    Order.Authority = Authority;

    if (!OwnerBattery->OrderComponent->SetOrder(Order))
    {
        MobilityState = EStrategyArtilleryMobilityState::Deployed;
        OwnerBattery->MovementExecutor->MoveSpeedCmPerSecond =
            PreviousMoveSpeedCmPerSecond;
        PreviousMoveSpeedCmPerSecond = 0.0f;
        return false;
    }

    return true;
}

void UStrategyArtilleryDeploymentComponent::CompleteTransition()
{
    TransitionRemainingSeconds = 0.0f;

    if (MobilityState == EStrategyArtilleryMobilityState::Deploying)
    {
        MobilityState = EStrategyArtilleryMobilityState::Deployed;
    }
    else if (MobilityState == EStrategyArtilleryMobilityState::Limbering)
    {
        MobilityState = EStrategyArtilleryMobilityState::Limbered;
    }

    if (OwnerBattery &&
        OwnerBattery->UnitState != EStrategyUnitState::Routed &&
        OwnerBattery->UnitState != EStrategyUnitState::Destroyed)
    {
        OwnerBattery->SetUnitState(EStrategyUnitState::Ready);
    }
}


float UStrategyArtilleryDeploymentComponent::GetCurrentGroundSlopeDegrees() const
{
    if (!OwnerBattery || !GetWorld())
    {
        return 90.0f;
    }

    const FVector Center = OwnerBattery->GetActorLocation();
    const float R = FMath::Max(100.0f, DeployTerrainSampleRadiusCm);

    const FVector Samples[4] =
    {
        Center + FVector(R, 0.0f, 1500.0f),
        Center + FVector(-R, 0.0f, 1500.0f),
        Center + FVector(0.0f, R, 1500.0f),
        Center + FVector(0.0f, -R, 1500.0f)
    };

    FVector Ground[4];

    FCollisionQueryParams Params(
        SCENE_QUERY_STAT(StrategyArtilleryDeploySlope),
        false);
    Params.AddIgnoredActor(OwnerBattery);

    for (int32 Index = 0; Index < 4; ++Index)
    {
        FHitResult Hit;

        const bool bHit =
            GetWorld()->LineTraceSingleByChannel(
                Hit,
                Samples[Index],
                Samples[Index] - FVector(0.0f, 0.0f, 4000.0f),
                ECC_Visibility,
                Params);

        if (!bHit)
        {
            return 90.0f;
        }

        Ground[Index] = Hit.ImpactPoint;
    }

    const FVector XSpan = Ground[0] - Ground[1];
    const FVector YSpan = Ground[2] - Ground[3];

    FVector Normal =
        FVector::CrossProduct(XSpan, YSpan).GetSafeNormal();

    if (Normal.Z < 0.0f)
    {
        Normal *= -1.0f;
    }

    const float UpDot =
        FMath::Clamp(
            FVector::DotProduct(Normal, FVector::UpVector),
            -1.0f,
            1.0f);

    return FMath::RadiansToDegrees(FMath::Acos(UpDot));
}

bool UStrategyArtilleryDeploymentComponent::CanDeployAtCurrentLocation() const
{
    return GetCurrentGroundSlopeDegrees() <=
        FMath::Max(0.0f, MaximumDeploySlopeDegrees);
}
