#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "StrategyCameraPawn.generated.h"

class UCameraComponent;
class UFloatingPawnMovement;
class USceneComponent;
class USpringArmComponent;

UCLASS(Blueprintable)
class STRATEGY1864_API AStrategyCameraPawn : public APawn
{
    GENERATED_BODY()

public:
    AStrategyCameraPawn();

    virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Camera")
    TObjectPtr<USceneComponent> SceneRoot;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Camera")
    TObjectPtr<USpringArmComponent> SpringArm;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Camera")
    TObjectPtr<UCameraComponent> Camera;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Strategy|Camera")
    TObjectPtr<UFloatingPawnMovement> MovementComponent;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Camera")
    float MinZoom = 800.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Camera")
    float MaxZoom = 60000.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Camera")
    float ZoomStep = 2500.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Strategy|Camera")
    float RotationSpeedDegrees = 70.0f;

    UFUNCTION(BlueprintCallable, Category="Strategy|Camera")
    void FocusOnWorldLocation(const FVector& WorldLocation);

private:
    void MoveForward(float Value);
    void MoveRight(float Value);
    void ZoomCamera(float Value);
    void RotateCamera(float Value);
};
