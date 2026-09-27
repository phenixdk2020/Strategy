#include "StrategyUnit.h"
#include "Components/SceneComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Command/StrategyCommandComponent.h"

AStrategyUnit::AStrategyUnit()
{
    PrimaryActorTick.bCanEverTick = false;

    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
    SetRootComponent(SceneRoot);

    OrderComponent = CreateDefaultSubobject<UStrategyOrderComponent>(TEXT("OrderComponent"));
    CommandComponent = CreateDefaultSubobject<UStrategyCommandComponent>(TEXT("CommandComponent"));
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
