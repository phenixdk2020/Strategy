#include "StrategyGameMode.h"

#include "StrategyCameraPawn.h"
#include "StrategyHUD.h"
#include "StrategyPlayerController.h"
#include "../Tests/StrategyOOBTestScenario.h"
#include "Engine/World.h"

AStrategyGameMode::AStrategyGameMode()
{
    DefaultPawnClass = AStrategyCameraPawn::StaticClass();
    PlayerControllerClass = AStrategyPlayerController::StaticClass();
    HUDClass = AStrategyHUD::StaticClass();
    OOBTestScenarioClass = AStrategyOOBTestScenario::StaticClass();
}

void AStrategyGameMode::BeginPlay()
{
    Super::BeginPlay();

    if (!bSpawnOOBTestScenario || !OOBTestScenarioClass || !GetWorld())
    {
        return;
    }

    GetWorld()->SpawnActor<AStrategyOOBTestScenario>(
        OOBTestScenarioClass,
        FVector::ZeroVector,
        FRotator::ZeroRotator);
}
