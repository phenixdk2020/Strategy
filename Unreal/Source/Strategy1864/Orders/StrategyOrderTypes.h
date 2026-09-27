#pragma once

#include "CoreMinimal.h"
#include "StrategyOrderTypes.generated.h"

UENUM(BlueprintType)
enum class EStrategyOrderType : uint8
{
    None        UMETA(DisplayName = "None"),
    Move        UMETA(DisplayName = "Move"),
    AttackHere  UMETA(DisplayName = "Angrib her"),
    DefendHere  UMETA(DisplayName = "Forsvar her"),
    Hold        UMETA(DisplayName = "Hold"),
    ScoutHere   UMETA(DisplayName = "Spejd her"),
    Charge      UMETA(DisplayName = "Charge")
};

USTRUCT(BlueprintType)
struct FStrategyOrder
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    EStrategyOrderType Type = EStrategyOrderType::None;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector TargetLocation = FVector::ZeroVector;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    float FacingYaw = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    bool bHasFacing = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    bool bPlayerAuthority = false;

    bool IsActive() const
    {
        return Type != EStrategyOrderType::None;
    }
};
