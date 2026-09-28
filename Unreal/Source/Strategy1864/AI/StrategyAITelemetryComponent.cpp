#include "StrategyAITelemetryComponent.h"

#include "../Units/StrategyUnit.h"
#include "Engine/World.h"

UStrategyAITelemetryComponent::UStrategyAITelemetryComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UStrategyAITelemetryComponent::SetDecision(
    const FString& Task,
    const FString& Reason)
{
    CurrentTask = Task;
    ReasonCode = Reason;
    LastDecisionWorldSeconds =
        GetWorld() ? GetWorld()->GetTimeSeconds() : 0.0f;

    const AStrategyUnit* Unit = Cast<AStrategyUnit>(GetOwner());

    UE_LOG(
        LogTemp,
        Display,
        TEXT("AI-DIAG|unit=%s|side=%s|AI=%s|task=%s|reason=%s"),
        Unit ? *Unit->StableUnitId.ToString() : TEXT("<none>"),
        Unit
            ? *StaticEnum<EStrategySide>()->GetNameStringByValue(
                static_cast<int64>(Unit->Side))
            : TEXT("<none>"),
        Unit && Unit->bOfficerAIEnabled ? TEXT("ON") : TEXT("OFF"),
        *CurrentTask,
        *ReasonCode);
}
