#include "StrategyGameMode.h"

#include "StrategyCameraPawn.h"
#include "StrategyHUD.h"
#include "StrategyPlayerController.h"
#include "../Tests/StrategyOOBTestScenario.h"
#include "../Tests/StrategyScenarioStateComponent.h"
#include "Engine/World.h"

AStrategyGameMode::AStrategyGameMode()
{
    DefaultPawnClass = AStrategyCameraPawn::StaticClass();
    PlayerControllerClass = AStrategyPlayerController::StaticClass();
    HUDClass = AStrategyHUD::StaticClass();
    OOBTestScenarioClass = AStrategyOOBTestScenario::StaticClass();
    ScenarioStateComponent =
        CreateDefaultSubobject<UStrategyScenarioStateComponent>(TEXT("ScenarioStateComponent"));
}

void AStrategyGameMode::BeginPlay()
{
    Super::BeginPlay();

    if (!bSpawnOOBTestScenario || !OOBTestScenarioClass || !GetWorld())
    {
        return;
    }

    SpawnedQAScenario = GetWorld()->SpawnActor<AStrategyOOBTestScenario>(
        OOBTestScenarioClass,
        FVector::ZeroVector,
        FRotator::ZeroRotator);
}


void AStrategyGameMode::ResetQAScenario()
{
    if (ScenarioStateComponent)
    {
        ScenarioStateComponent->ResetOutcome();
    }

    if (IsValid(SpawnedQAScenario))
    {
        SpawnedQAScenario->ResetScenario();
    }
}
