#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "StrategyPlayerController.generated.h"

class AStrategyHUD;
class AStrategyUnit;

UCLASS()
class STRATEGY1864_API AStrategyPlayerController : public APlayerController
{
    GENERATED_BODY()

public:
    AStrategyPlayerController();

    virtual void SetupInputComponent() override;
    virtual void PlayerTick(float DeltaTime) override;

    UFUNCTION(BlueprintCallable, Category="Strategy|Selection")
    void ClearSelection();

    UFUNCTION(BlueprintPure, Category="Strategy|Selection")
    TArray<AStrategyUnit*> GetSelectedUnits() const;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Selection")
    float BoxSelectionThresholdPixels = 6.0f;

private:
    void SelectionPressed();
    void SelectionReleased();
    void SelectSingleUnderCursor();
    void SelectUnitsInScreenRectangle(const FVector2D& Start, const FVector2D& End);
    void ApplySelection(AStrategyUnit* Unit, bool bAdd, bool bRemove);
    void ReadModifierState(bool& bOutAdd, bool& bOutRemove) const;
    AStrategyHUD* GetStrategyHUD() const;

    bool bSelectionInputDown = false;
    FVector2D SelectionStart = FVector2D::ZeroVector;
    FVector2D SelectionCurrent = FVector2D::ZeroVector;

    UPROPERTY()
    TArray<TObjectPtr<AStrategyUnit>> SelectedUnitObjects;
};
