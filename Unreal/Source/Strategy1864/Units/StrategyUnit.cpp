#include "Units/StrategyUnit.h"
#include "Components/SceneComponent.h"
#include "Orders/StrategyOrderComponent.h"

AStrategyUnit::AStrategyUnit()
{
    PrimaryActorTick.bCanEverTick = false;

    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
    SetRootComponent(SceneRoot);

    OrderComponent = CreateDefaultSubobject<UStrategyOrderComponent>(TEXT("OrderComponent"));
}

void AStrategyUnit::SetSelected(bool bNewSelected)
{
    bSelected = bNewSelected;
}
