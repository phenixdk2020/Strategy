#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "../UI/StrategySemanticZoomTypes.h"
#include "StrategyUnit.generated.h"

class USceneComponent;
class USphereComponent;
class UTextRenderComponent;
class UStrategyOrderComponent;
class UStrategyCommandComponent;
class UStrategyMovementExecutorComponent;
class UStrategyFormationComponent;
class UStrategyFormationPolicyComponent;
class UStrategyFormationTransitionComponent;
class UStrategyParentExecutionComponent;
class UStrategyRoutePlannerComponent;
class UStrategyVisibilityComponent;
class UStrategyFireControlComponent;
class UStrategyCombatComponent;
class UStrategyOfficerAIComponent;
class UStrategyThreatReactionComponent;
class UStrategyOOBStatusComponent;
class UStrategySemanticZoomComponent;
class UStrategyWorldDebugComponent;
class UStrategyPresentationSnapshotComponent;
class UStrategyLocalDeconflictionComponent;
class UStrategyMissionAnchorComponent;
class UStrategyOfficerProfileComponent;
class UStrategyCommandDelayComponent;
class UStrategyConditionComponent;

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(
    FStrategyCasualtyVisualEvent,
    int32,
    AppliedLoss,
    FVector,
    WorldLocation);

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

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyFormationComponent> FormationComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyFormationPolicyComponent> FormationPolicy;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyFormationTransitionComponent> FormationTransition;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyParentExecutionComponent> ParentExecution;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyRoutePlannerComponent> RoutePlanner;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyVisibilityComponent> VisibilityComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyFireControlComponent> FireControlComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyCombatComponent> CombatComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyOfficerAIComponent> OfficerAIComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyThreatReactionComponent> ThreatReactionComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyOOBStatusComponent> OOBStatusComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategySemanticZoomComponent> SemanticZoomComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyWorldDebugComponent> WorldDebugComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyPresentationSnapshotComponent> PresentationSnapshotComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyLocalDeconflictionComponent> LocalDeconflictionComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyMissionAnchorComponent> MissionAnchorComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyOfficerProfileComponent> OfficerProfileComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyCommandDelayComponent> CommandDelayComponent;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Components")
    TObjectPtr<UStrategyConditionComponent> ConditionComponent;

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

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float MaximumFireRangeCm = 10000.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float Morale = 100.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float Cohesion = 100.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float Fatigue = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Combat")
    float Experience = 0.0f;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Unit")
    EStrategyUnitState UnitState = EStrategyUnitState::Ready;

    UPROPERTY(BlueprintAssignable, Category="Strategy|Presentation")
    FStrategyCasualtyVisualEvent OnCasualtyVisualEvent;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|AI")
    bool bOfficerAIEnabled = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Selection")
    bool bPlayerControllable = true;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Selection")
    bool bSelected = false;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Semantic Zoom")
    EStrategySemanticZoomState SemanticZoomState = EStrategySemanticZoomState::Close;

    UFUNCTION(BlueprintCallable, Category="Strategy|Selection")
    virtual void SetSelected(bool bNewSelected);

    UFUNCTION(BlueprintCallable, Category="Strategy|Unit")
    void SetUnitState(EStrategyUnitState NewState);

    UFUNCTION(BlueprintCallable, Category="Strategy|Combat")
    int32 ApplyStrengthLoss(int32 RequestedLoss);

    UFUNCTION(BlueprintCallable, Category="Strategy|Identity")
    void RefreshDebugLabel();

    UFUNCTION(BlueprintCallable, Category="Strategy|Semantic Zoom")
    void SetSemanticZoomState(EStrategySemanticZoomState NewState);

    UFUNCTION(BlueprintPure, Category="Strategy|Presentation")
    FString GetNATOEchelonSymbol() const;

    UFUNCTION(BlueprintPure, Category="Strategy|Unit")
    bool IsCombatEffective() const;

    UFUNCTION(BlueprintImplementableEvent, Category="Strategy|Selection")
    void OnSelectionChanged(bool bNewSelected);

    UFUNCTION(BlueprintImplementableEvent, Category="Strategy|Unit")
    void OnUnitStateChanged(EStrategyUnitState NewState);

    UFUNCTION(BlueprintImplementableEvent, Category="Strategy|Semantic Zoom")
    void OnSemanticZoomChanged(EStrategySemanticZoomState NewState);
};
