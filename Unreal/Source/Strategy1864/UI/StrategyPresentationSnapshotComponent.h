#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "StrategyPresentationSnapshotComponent.generated.h"

USTRUCT(BlueprintType)
struct FStrategyUnitPresentationSnapshot
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly)
    FName StableUnitId = NAME_None;

    UPROPERTY(BlueprintReadOnly)
    FString DisplayName;

    UPROPERTY(BlueprintReadOnly)
    FString NATOEchelon;

    UPROPERTY(BlueprintReadOnly)
    FString Echelon;

    UPROPERTY(BlueprintReadOnly)
    FString Side;

    UPROPERTY(BlueprintReadOnly)
    int32 InitialStrength = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 CurrentStrength = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 Losses = 0;

    UPROPERTY(BlueprintReadOnly)
    float Morale = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    float Cohesion = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    float Fatigue = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    FString UnitState;

    UPROPERTY(BlueprintReadOnly)
    FString AIState;

    UPROPERTY(BlueprintReadOnly)
    FString OrderType;

    UPROPERTY(BlueprintReadOnly)
    FString ExecutionState;

    UPROPERTY(BlueprintReadOnly)
    FString Formation;

    UPROPERTY(BlueprintReadOnly)
    FName OrganicParentId = NAME_None;

    UPROPERTY(BlueprintReadOnly)
    FName CurrentCommandParentId = NAME_None;

    UPROPERTY(BlueprintReadOnly)
    bool bTemporarilyAttached = false;

    UPROPERTY(BlueprintReadOnly)
    int32 AmmunitionRounds = 0;

    UPROPERTY(BlueprintReadOnly)
    FString CavalryRole;

    UPROPERTY(BlueprintReadOnly)
    FString MountedState;

    UPROPERTY(BlueprintReadOnly)
    int32 ArtilleryGunCount = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 ArtilleryOperationalGuns = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 ArtilleryCrew = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 ArtilleryDrivers = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 ArtilleryHorses = 0;

    UPROPERTY(BlueprintReadOnly)
    FString ArtilleryMobilityState;

    UPROPERTY(BlueprintReadOnly)
    FString ArtilleryFireMode;

    UPROPERTY(BlueprintReadOnly)
    FString ArtilleryAmmoType;

    UPROPERTY(BlueprintReadOnly)
    FString ArtilleryOwnershipState;

    UPROPERTY(BlueprintReadOnly)
    int32 SupplySmallArmsRounds = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 SupplyArtilleryRounds = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 SupplyDrivers = 0;

    UPROPERTY(BlueprintReadOnly)
    int32 SupplyHorses = 0;

    UPROPERTY(BlueprintReadOnly)
    float SupplyWagonCondition = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    FString SupplyOwnershipState;

    UPROPERTY(BlueprintReadOnly)
    float TerrainGroundZ = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    float TerrainLocalSlopeDegrees = 0.0f;

    UPROPERTY(BlueprintReadOnly)
    bool bTerrainNearCrest = false;
};

UCLASS(ClassGroup=(Strategy1864), meta=(BlueprintSpawnableComponent))
class STRATEGY1864_API UStrategyPresentationSnapshotComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UStrategyPresentationSnapshotComponent();

    UFUNCTION(BlueprintPure, Category="Strategy|Presentation")
    FStrategyUnitPresentationSnapshot BuildSnapshot() const;
};
