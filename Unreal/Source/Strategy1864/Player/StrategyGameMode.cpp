#include "StrategyGameMode.h"

#include "StrategyCameraPawn.h"
#include "StrategyHUD.h"
#include "StrategyPlayerController.h"

AStrategyGameMode::AStrategyGameMode()
{
    DefaultPawnClass = AStrategyCameraPawn::StaticClass();
    PlayerControllerClass = AStrategyPlayerController::StaticClass();
    HUDClass = AStrategyHUD::StaticClass();
}
