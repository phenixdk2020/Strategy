#include "StrategyFieldworksComponent.h"

#include "../Navigation/StrategyNavigationObstacle.h"
#include "../Movement/StrategyMovementExecutorComponent.h"
#include "../Formations/StrategyFormationComponent.h"
#include "../Units/StrategyUnit.h"
#include "Engine/World.h"

UStrategyFieldworksComponent::UStrategyFieldworksComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
    PrimaryComponentTick.bStartWithTickEnabled = false;
}

void UStrategyFieldworksComponent::BeginPlay()
{
    Super::BeginPlay();
    OwnerUnit = Cast<AStrategyUnit>(GetOwner());
}

bool UStrategyFieldworksComponent::BeginHastyFieldworks()
{
    if (!OwnerUnit ||
        OwnerUnit->Echelon != EStrategyEchelon::Company ||
        OwnerUnit->UnitState == EStrategyUnitState::Routed ||
        OwnerUnit->UnitState == EStrategyUnitState::Destroyed ||
        (OwnerUnit->MovementExecutor &&
         OwnerUnit->MovementExecutor->HasMovementGoal()))
    {
        return false;
    }

    if (HasCompletedFieldworks())
    {
        return true;
    }

    const float Multiplier =
        bEngineerUnit
        ? FMath::Clamp(EngineerBuildTimeMultiplier, 0.10f, 1.0f)
        : 1.0f;

    BuildRemainingSeconds =
        FMath::Max(0.5f, BuildSeconds * Multiplier);

    bBuilding = true;
    SetComponentTickEnabled(true);
    return true;
}

void UStrategyFieldworksComponent::CancelFieldworks()
{
    bBuilding = false;
    BuildRemainingSeconds = 0.0f;
    SetComponentTickEnabled(false);
}

bool UStrategyFieldworksComponent::HasCompletedFieldworks() const
{
    return IsValid(FieldworksObstacle);
}

void UStrategyFieldworksComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!bBuilding || !OwnerUnit)
    {
        SetComponentTickEnabled(false);
        return;
    }

    if ((OwnerUnit->MovementExecutor &&
         OwnerUnit->MovementExecutor->HasMovementGoal()) ||
        OwnerUnit->UnitState == EStrategyUnitState::Routed ||
        OwnerUnit->UnitState == EStrategyUnitState::Destroyed)
    {
        CancelFieldworks();
        return;
    }

    BuildRemainingSeconds =
        FMath::Max(0.0f, BuildRemainingSeconds - DeltaTime);

    if (BuildRemainingSeconds <= 0.0f)
    {
        CompleteFieldworks();
    }
}

void UStrategyFieldworksComponent::CompleteFieldworks()
{
    if (!OwnerUnit || !GetWorld())
    {
        CancelFieldworks();
        return;
    }

    FieldworksObstacle =
        GetWorld()->SpawnActor<AStrategyNavigationObstacle>(
            AStrategyNavigationObstacle::StaticClass(),
            OwnerUnit->GetActorLocation(),
            OwnerUnit->GetActorRotation());

    if (FieldworksObstacle)
    {
        FieldworksObstacle->ObstacleType = EStrategyObstacleType::Fieldworks;

        const float Frontage =
            OwnerUnit->FormationComponent
            ? OwnerUnit->FormationComponent->EstimateFrontageCm(
                FMath::Max(1, OwnerUnit->CurrentStrength))
            : 4800.0f;

        FieldworksObstacle->HalfExtentCm =
            FVector(250.0f, FMath::Max(800.0f, Frontage * 0.5f), 100.0f);
        FieldworksObstacle->ClearanceCm = 150.0f;
    }

    bBuilding = false;
    BuildRemainingSeconds = 0.0f;
    SetComponentTickEnabled(false);
}
