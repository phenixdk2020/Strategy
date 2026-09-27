#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "StrategyUnit.generated.h"

class USceneComponent;
class USphereComponent;
class UTextRenderComponent;
class UStrategyOrderComponent;
class UStrategyCommandComponent;
class UStrategyMovementExecutorComponent;

UENUM(BlueprintType)
enum class EStrategyEchelon : uint8
{
    Company,
    Battalion,
    Regiment,
    Brigade,
    Division,
    Cavalry,
    Headquarters
};

UENUM(BlueprintType)
enum class EStrategySide : uint8
{
    Neutral,
    Denmark,
    Prussia,
    Austria,
    Allied,
    Enemy
};

UENUM(BlueprintType)
enum class EStrategyUnitState : uint8
{
    Ready,
    Moving,
    Reforming,
    Engaged,
    UnderFire,
    Routed,
    Destroyed
};

UCLASS(Abstract, Blueprintable)
class STRATEGY1864_API AStrategyUnit : public APawn
{
    GENERATED_BODY()

public:
    AStrategyUnit();

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<USceneComponent> SceneRoot;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<USphereComponent> SelectionCollider;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UTextRenderComponent> DebugLabel;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyOrderComponent> OrderComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyCommandComponent> CommandComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyMovementExecutorComponent> MovementExecutor;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Identity")
    FName StableUnitId = NAME_None;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Identity")
    FText DisplayName;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    EStrategySide Side = EStrategySide::Neutral;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    EStrategyEchelon Echelon = EStrategyEchelon::Company;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    int32 InitialStrength = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Unit")
    int32 CurrentStrength = 0;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Unit")
    EStrategyUnitState UnitState = EStrategyUnitState::Ready;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|AI")
    bool bOfficerAIEnabled = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Selection")
    bool bPlayerControllable = true;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Selection")
    bool bSelected = false;

    UFUNCTION(BlueprintCallable, Category="Strategy|Selection")
    virtual void SetSelected(bool bNewSelected);

    UFUNCTION(BlueprintCallable, Category="Strategy|Unit")
    void SetUnitState(EStrategyUnitState NewState);

    UFUNCTION(BlueprintCallable, Category="Strategy|Identity")
    void RefreshDebugLabel();

    UFUNCTION(BlueprintPure, Category="Strategy|Unit")
    bool IsCombatEffective() const;

    UFUNCTION(BlueprintImplementableEvent, Category="Strategy|Selection")
    void OnSelectionChanged(bool bNewSelected);

    UFUNCTION(BlueprintImplementableEvent, Category="Strategy|Unit")
    void OnUnitStateChanged(EStrategyUnitState NewState);
};
