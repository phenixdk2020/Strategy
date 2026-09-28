#include "StrategyAutonomousBattleAIComponent.h"

#include "../Combat/StrategyContactComponent.h"
#include "../Combat/StrategyFireControlComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Units/StrategyUnit.h"
#include "EngineUtils.h"

UStrategyAutonomousBattleAIComponent::UStrategyAutonomousBattleAIComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
}

void UStrategyAutonomousBattleAIComponent::BeginPlay()
{
    Super::BeginPlay();
    OwnerUnit = Cast<AStrategyUnit>(GetOwner());
}

void UStrategyAutonomousBattleAIComponent::TickComponent(
    float DeltaTime,
    ELevelTick TickType,
    FActorComponentTickFunction* ThisTickFunction)
{
    Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

    if (!IsAutonomousEnemy() ||
        !OwnerUnit->bOfficerAIEnabled ||
        !OwnerUnit->OrderComponent ||
        !OwnerUnit->FireControlComponent ||
        OwnerUnit->UnitState == EStrategyUnitState::Routed ||
        OwnerUnit->UnitState == EStrategyUnitState::Destroyed)
    {
        return;
    }

    EvaluationAccumulator += DeltaTime;
    if (EvaluationAccumulator < EvaluationIntervalSeconds)
    {
        return;
    }

    EvaluationAccumulator = 0.0f;

    if (OwnerUnit->OrderComponent->IsPhysicallyExecuting())
    {
        return;
    }

    AStrategyUnit* Enemy = FindCurrentVisibleEnemy();
    if (!IsValid(Enemy))
    {
        return;
    }

    const float RangeCm =
        FMath::Max(
            1000.0f,
            OwnerUnit->FireControlComponent->GetActiveRangeCm());

    const float DesiredDistance =
        RangeCm * FMath::Clamp(DesiredRangeFraction, 0.25f, 0.95f);

    const float CurrentDistance =
        FVector::Dist2D(
            OwnerUnit->GetActorLocation(),
            Enemy->GetActorLocation());

    if (CurrentDistance <= DesiredDistance)
    {
        return;
    }

    FVector FromEnemy =
        OwnerUnit->GetActorLocation() - Enemy->GetActorLocation();
    FromEnemy.Z = 0.0f;
    FromEnemy = FromEnemy.GetSafeNormal();

    if (FromEnemy.IsNearlyZero())
    {
        FromEnemy = -OwnerUnit->GetActorForwardVector().GetSafeNormal2D();
    }

    const FVector Goal =
        Enemy->GetActorLocation() + FromEnemy * DesiredDistance;

    FStrategyOrder Order;
    Order.Type = EStrategyOrderType::Advance;
    Order.TargetLocation = Goal;
    Order.FacingYaw =
        (Enemy->GetActorLocation() - Goal).Rotation().Yaw;
    Order.bHasFacing = true;
    Order.Authority = EStrategyOrderAuthority::OfficerAI;

    OwnerUnit->OrderComponent->SetOrder(Order);
}

AStrategyUnit* UStrategyAutonomousBattleAIComponent::FindCurrentVisibleEnemy() const
{
    if (!OwnerUnit || !OwnerUnit->ContactComponent || !GetWorld())
    {
        return nullptr;
    }

    AStrategyUnit* Best = nullptr;
    float BestDistance = TNumericLimits<float>::Max();

    for (TActorIterator<AStrategyUnit> It(GetWorld()); It; ++It)
    {
        AStrategyUnit* Candidate = *It;

        if (!IsValid(Candidate) ||
            Candidate == OwnerUnit ||
            Candidate->Side == EStrategySide::Neutral ||
            Candidate->Side == OwnerUnit->Side ||
            !Candidate->IsCombatEffective() ||
            !OwnerUnit->ContactComponent->HasCurrentContact(Candidate))
        {
            continue;
        }

        const float Distance =
            FVector::Dist2D(
                OwnerUnit->GetActorLocation(),
                Candidate->GetActorLocation());

        if (Distance < BestDistance)
        {
            BestDistance = Distance;
            Best = Candidate;
        }
    }

    return Best;
}

bool UStrategyAutonomousBattleAIComponent::IsAutonomousEnemy() const
{
    if (!bEnableForNonPlayerSides || !OwnerUnit)
    {
        return false;
    }

    const bool bEnemySide =
        OwnerUnit->Side == EStrategySide::Prussia ||
        OwnerUnit->Side == EStrategySide::Austria ||
        OwnerUnit->Side == EStrategySide::Enemy;

    return bEnemySide &&
        OwnerUnit->Echelon == EStrategyEchelon::Company;
}
