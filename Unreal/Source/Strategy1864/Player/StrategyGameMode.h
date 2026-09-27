#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "StrategyGameMode.generated.h"

class AStrategyOOBTestScenario;

UCLASS()
class STRATEGY1864_API AStrategyGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    AStrategyGameMode();

protected:
    virtual void BeginPlay() override;

public:
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    bool bSpawnOOBTestScenario = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Test")
    TSubclassOf<AStrategyOOBTestScenario> OOBTestScenarioClass;
};
