#include "StrategyUnit.h"
#include "Components/SceneComponent.h"
#include "Components/SphereComponent.h"
#include "Components/TextRenderComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Command/StrategyCommandComponent.h"
#include "../Movement/StrategyMovementExecutorComponent.h"
#include "../Formations/StrategyFormationComponent.h"
#include "../Formations/StrategyFormationPolicyComponent.h"
#include "../Orders/StrategyParentExecutionComponent.h"
#include "../Navigation/StrategyRoutePlannerComponent.h"
#include "../Combat/StrategyVisibilityComponent.h"
#include "../Combat/StrategyFireControlComponent.h"
#include "../Combat/StrategyCombatComponent.h"
#include "../AI/StrategyOfficerAIComponent.h"
#include "../Combat/StrategyThreatReactionComponent.h"

AStrategyUnit::AStrategyUnit()
{
    PrimaryActorTick.bCanEverTick = false;

    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
    SetRootComponent(SceneRoot);

    SelectionCollider = CreateDefaultSubobject<USphereComponent>(TEXT("SelectionCollider"));
    SelectionCollider->SetupAttachment(SceneRoot);
    SelectionCollider->InitSphereRadius(180.0f);
    SelectionCollider->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    SelectionCollider->SetCollisionResponseToAllChannels(ECR_Ignore);
    SelectionCollider->SetCollisionResponseToChannel(ECC_Visibility, ECR_Block);

    DebugLabel = CreateDefaultSubobject<UTextRenderComponent>(TEXT("DebugLabel"));
    DebugLabel->SetupAttachment(SceneRoot);
    DebugLabel->SetRelativeLocation(FVector(0.0f, 0.0f, 220.0f));
    DebugLabel->SetHorizontalAlignment(EHorizTextAligment::EHTA_Center);
    DebugLabel->SetWorldSize(80.0f);
    DebugLabel->SetText(FText::FromString(TEXT("StrategyUnit")));

    OrderComponent = CreateDefaultSubobject<UStrategyOrderComponent>(TEXT("OrderComponent"));
    CommandComponent = CreateDefaultSubobject<UStrategyCommandComponent>(TEXT("CommandComponent"));
    MovementExecutor = CreateDefaultSubobject<UStrategyMovementExecutorComponent>(TEXT("MovementExecutor"));
    FormationComponent = CreateDefaultSubobject<UStrategyFormationComponent>(TEXT("FormationComponent"));
    FormationPolicy = CreateDefaultSubobject<UStrategyFormationPolicyComponent>(TEXT("FormationPolicy"));
    ParentExecution = CreateDefaultSubobject<UStrategyParentExecutionComponent>(TEXT("ParentExecution"));
    RoutePlanner = CreateDefaultSubobject<UStrategyRoutePlannerComponent>(TEXT("RoutePlanner"));
    VisibilityComponent = CreateDefaultSubobject<UStrategyVisibilityComponent>(TEXT("VisibilityComponent"));
    FireControlComponent = CreateDefaultSubobject<UStrategyFireControlComponent>(TEXT("FireControlComponent"));
    CombatComponent = CreateDefaultSubobject<UStrategyCombatComponent>(TEXT("CombatComponent"));
    OfficerAIComponent = CreateDefaultSubobject<UStrategyOfficerAIComponent>(TEXT("OfficerAIComponent"));
    ThreatReactionComponent = CreateDefaultSubobject<UStrategyThreatReactionComponent>(TEXT("ThreatReactionComponent"));
}

void AStrategyUnit::SetSelected(bool bNewSelected)
{
    if (bSelected == bNewSelected)
    {
        return;
    }

    bSelected = bNewSelected;
    OnSelectionChanged(bSelected);
}

void AStrategyUnit::RefreshDebugLabel()
{
    if (!DebugLabel)
    {
        return;
    }

    const FString NameText = DisplayName.IsEmpty() ? StableUnitId.ToString() : DisplayName.ToString();
    const FString StrengthText = FString::Printf(TEXT("%d/%d"), CurrentStrength, InitialStrength);
    DebugLabel->SetText(FText::FromString(FString::Printf(TEXT("%s\n%s"), *NameText, *StrengthText)));
}

int32 AStrategyUnit::ApplyStrengthLoss(int32 RequestedLoss)
{
    if (RequestedLoss <= 0 || CurrentStrength <= 0)
    {
        return 0;
    }

    const int32 AppliedLoss = FMath::Min(RequestedLoss, CurrentStrength);
    CurrentStrength -= AppliedLoss;

    if (CurrentStrength <= 0)
    {
        CurrentStrength = 0;
        SetUnitState(EStrategyUnitState::Destroyed);
    }

    RefreshDebugLabel();
    return AppliedLoss;
}

void AStrategyUnit::SetUnitState(EStrategyUnitState NewState)
{
    if (UnitState == NewState)
    {
        return;
    }

    UnitState = NewState;
    OnUnitStateChanged(UnitState);
}

bool AStrategyUnit::IsCombatEffective() const
{
    return CurrentStrength > 0 &&
        UnitState != EStrategyUnitState::Routed &&
        UnitState != EStrategyUnitState::Destroyed;
}
