#include "StrategyPlayerController.h"

#include "StrategyHUD.h"
#include "StrategyCameraPawn.h"
#include "StrategyGameMode.h"
#include "EngineUtils.h"
#include "InputCoreTypes.h"
#include "Kismet/GameplayStatics.h"
#include "../Units/StrategyUnit.h"
#include "../Orders/StrategyOrderComponent.h"

AStrategyPlayerController::AStrategyPlayerController()
{
    PrimaryActorTick.bCanEverTick = true;
    bShowMouseCursor = true;
    bEnableClickEvents = true;
    bEnableMouseOverEvents = true;
    DefaultMouseCursor = EMouseCursor::Default;
}

void AStrategyPlayerController::SetupInputComponent()
{
    Super::SetupInputComponent();

    check(InputComponent);
    InputComponent->BindAction(TEXT("Select"), IE_Pressed, this, &AStrategyPlayerController::SelectionPressed);
    InputComponent->BindAction(TEXT("Select"), IE_Released, this, &AStrategyPlayerController::SelectionReleased);

    FInputActionBinding& PauseBinding =
        InputComponent->BindAction(
            TEXT("PauseSimulation"),
            IE_Pressed,
            this,
            &AStrategyPlayerController::TogglePauseSimulation);
    PauseBinding.bExecuteWhenPaused = true;

    FInputActionBinding& Speed1Binding =
        InputComponent->BindAction(TEXT("Time1x"), IE_Pressed, this, &AStrategyPlayerController::SetSpeed1x);
    Speed1Binding.bExecuteWhenPaused = true;

    FInputActionBinding& Speed2Binding =
        InputComponent->BindAction(TEXT("Time2x"), IE_Pressed, this, &AStrategyPlayerController::SetSpeed2x);
    Speed2Binding.bExecuteWhenPaused = true;

    FInputActionBinding& Speed3Binding =
        InputComponent->BindAction(TEXT("Time3x"), IE_Pressed, this, &AStrategyPlayerController::SetSpeed3x);
    Speed3Binding.bExecuteWhenPaused = true;

    FInputActionBinding& ResetBinding =
        InputComponent->BindAction(TEXT("ResetQAScenario"), IE_Pressed, this, &AStrategyPlayerController::ResetQAScenario);
    ResetBinding.bExecuteWhenPaused = true;
}

void AStrategyPlayerController::PlayerTick(float DeltaTime)
{
    Super::PlayerTick(DeltaTime);

    if (!bSelectionInputDown)
    {
        return;
    }

    float MouseX = 0.0f;
    float MouseY = 0.0f;
    if (GetMousePosition(MouseX, MouseY))
    {
        SelectionCurrent = FVector2D(MouseX, MouseY);
        if (AStrategyHUD* HUD = GetStrategyHUD())
        {
            HUD->UpdateSelectionBox(SelectionCurrent);
        }
    }
}

void AStrategyPlayerController::SelectionPressed()
{
    if (bOrderPlacementPending)
    {
        FVector GroundPoint;
        if (ResolveGroundPointUnderCursor(GroundPoint))
        {
            PendingOrderTarget = GroundPoint;
            bOrderFacingDragActive = true;
        }
        return;
    }

    float MouseX = 0.0f;
    float MouseY = 0.0f;
    if (!GetMousePosition(MouseX, MouseY))
    {
        return;
    }

    bSelectionInputDown = true;
    SelectionStart = FVector2D(MouseX, MouseY);
    SelectionCurrent = SelectionStart;

    if (AStrategyHUD* HUD = GetStrategyHUD())
    {
        HUD->BeginSelectionBox(SelectionStart);
    }
}

void AStrategyPlayerController::SelectionReleased()
{
    if (bOrderPlacementPending && bOrderFacingDragActive)
    {
        FVector FacingPoint;
        const bool bHasFacingPoint = ResolveGroundPointUnderCursor(FacingPoint);

        const FVector FlatFacingDelta(
            FacingPoint.X - PendingOrderTarget.X,
            FacingPoint.Y - PendingOrderTarget.Y,
            0.0f);

        const bool bHasFacing =
            bHasFacingPoint &&
            FlatFacingDelta.SizeSquared() >= FMath::Square(100.0f);

        const float FacingYaw =
            bHasFacing
            ? FlatFacingDelta.Rotation().Yaw
            : 0.0f;

        const EStrategyOrderType OrderType = PendingOrderType;
        const FVector Target = PendingOrderTarget;

        CancelOrderPlacement();

        IssueOrderToSelection(
            OrderType,
            Target,
            FacingYaw,
            bHasFacing);

        return;
    }

    if (!bSelectionInputDown)
    {
        return;
    }

    bSelectionInputDown = false;

    if (AStrategyHUD* HUD = GetStrategyHUD())
    {
        HUD->EndSelectionBox();
    }

    const float DragDistance = FVector2D::Distance(SelectionStart, SelectionCurrent);
    if (DragDistance >= BoxSelectionThresholdPixels)
    {
        SelectUnitsInScreenRectangle(SelectionStart, SelectionCurrent);
    }
    else
    {
        SelectSingleUnderCursor();
    }
}

void AStrategyPlayerController::ReadModifierState(bool& bOutAdd, bool& bOutRemove) const
{
    bOutAdd =
        IsInputKeyDown(EKeys::LeftShift) ||
        IsInputKeyDown(EKeys::RightShift);

    bOutRemove =
        IsInputKeyDown(EKeys::LeftControl) ||
        IsInputKeyDown(EKeys::RightControl);
}

void AStrategyPlayerController::SelectSingleUnderCursor()
{
    bool bAdd = false;
    bool bRemove = false;
    ReadModifierState(bAdd, bRemove);

    FHitResult Hit;
    AStrategyUnit* HitUnit = nullptr;
    if (GetHitResultUnderCursor(ECC_Visibility, true, Hit))
    {
        HitUnit = Cast<AStrategyUnit>(Hit.GetActor());
    }

    if (!bAdd && !bRemove)
    {
        ClearSelection();
    }

    if (HitUnit && HitUnit->bPlayerControllable)
    {
        ApplySelection(HitUnit, bAdd, bRemove);
    }
}

void AStrategyPlayerController::SelectUnitsInScreenRectangle(const FVector2D& Start, const FVector2D& End)
{
    bool bAdd = false;
    bool bRemove = false;
    ReadModifierState(bAdd, bRemove);

    if (!bAdd && !bRemove)
    {
        ClearSelection();
    }

    const float Left = FMath::Min(Start.X, End.X);
    const float Top = FMath::Min(Start.Y, End.Y);
    const float Right = FMath::Max(Start.X, End.X);
    const float Bottom = FMath::Max(Start.Y, End.Y);

    UWorld* World = GetWorld();
    if (!World)
    {
        return;
    }

    for (TActorIterator<AStrategyUnit> It(World); It; ++It)
    {
        AStrategyUnit* Unit = *It;
        if (!IsValid(Unit) || !Unit->bPlayerControllable)
        {
            continue;
        }

        FVector2D ScreenPoint;
        if (!ProjectWorldLocationToScreen(Unit->GetActorLocation(), ScreenPoint, false))
        {
            continue;
        }

        const bool bInside =
            ScreenPoint.X >= Left &&
            ScreenPoint.X <= Right &&
            ScreenPoint.Y >= Top &&
            ScreenPoint.Y <= Bottom;

        if (bInside)
        {
            ApplySelection(Unit, bAdd, bRemove);
        }
    }
}

void AStrategyPlayerController::ApplySelection(AStrategyUnit* Unit, bool bAdd, bool bRemove)
{
    if (!IsValid(Unit))
    {
        return;
    }

    if (bRemove)
    {
        if (SelectedUnitObjects.Remove(Unit) > 0)
        {
            Unit->SetSelected(false);
        }
        return;
    }

    if (!SelectedUnitObjects.Contains(Unit))
    {
        SelectedUnitObjects.Add(Unit);
        Unit->SetSelected(true);
    }
}

void AStrategyPlayerController::ClearSelection()
{
    for (AStrategyUnit* Unit : SelectedUnitObjects)
    {
        if (IsValid(Unit))
        {
            Unit->SetSelected(false);
        }
    }

    SelectedUnitObjects.Reset();
}

TArray<AStrategyUnit*> AStrategyPlayerController::GetSelectedUnits() const
{
    TArray<AStrategyUnit*> Result;
    Result.Reserve(SelectedUnitObjects.Num());

    for (AStrategyUnit* Unit : SelectedUnitObjects)
    {
        if (IsValid(Unit))
        {
            Result.Add(Unit);
        }
    }

    return Result;
}

AStrategyHUD* AStrategyPlayerController::GetStrategyHUD() const
{
    return Cast<AStrategyHUD>(GetHUD());
}


void AStrategyPlayerController::BeginOrderPlacement(EStrategyOrderType OrderType)
{
    if (OrderType == EStrategyOrderType::None || SelectedUnitObjects.Num() == 0)
    {
        return;
    }

    if (OrderType == EStrategyOrderType::Hold)
    {
        IssueHoldToSelection();
        return;
    }

    PendingOrderType = OrderType;
    bOrderPlacementPending = true;
}

void AStrategyPlayerController::CancelOrderPlacement()
{
    PendingOrderType = EStrategyOrderType::None;
    PendingOrderTarget = FVector::ZeroVector;
    bOrderFacingDragActive = false;
    bOrderPlacementPending = false;
}

bool AStrategyPlayerController::CommitPendingOrderUnderCursor()
{
    if (!bOrderPlacementPending || PendingOrderType == EStrategyOrderType::None)
    {
        return false;
    }

    FVector GroundPoint;
    if (!ResolveGroundPointUnderCursor(GroundPoint))
    {
        return false;
    }

    const EStrategyOrderType OrderType = PendingOrderType;
    CancelOrderPlacement();

    return IssueOrderToSelection(
        OrderType,
        GroundPoint,
        0.0f,
        false);
}

bool AStrategyPlayerController::IssueOrderToSelection(
    EStrategyOrderType OrderType,
    const FVector& TargetLocation,
    float FacingYaw,
    bool bHasFacing)
{
    if (OrderType == EStrategyOrderType::None)
    {
        return false;
    }

    bool bIssuedAny = false;

    for (AStrategyUnit* Unit : SelectedUnitObjects)
    {
        if (!IsValid(Unit) || !Unit->OrderComponent)
        {
            continue;
        }

        FStrategyOrder Order;
        Order.Type = OrderType;
        Order.TargetLocation = TargetLocation;
        Order.FacingYaw = FacingYaw;
        Order.bHasFacing = bHasFacing;
        Order.Authority = EStrategyOrderAuthority::DirectPlayer;

        if (Unit->OrderComponent->SetOrder(Order))
        {
            bIssuedAny = true;
        }
    }

    return bIssuedAny;
}

void AStrategyPlayerController::IssueHoldToSelection()
{
    IssueOrderToSelection(
        EStrategyOrderType::Hold,
        FVector::ZeroVector,
        0.0f,
        false);

    CancelOrderPlacement();
}

bool AStrategyPlayerController::ResolveGroundPointUnderCursor(FVector& OutWorldPoint) const
{
    FHitResult Hit;
    if (!GetHitResultUnderCursor(ECC_Visibility, true, Hit))
    {
        return false;
    }

    OutWorldPoint = Hit.ImpactPoint;
    return true;
}


void AStrategyPlayerController::SelectUnitFromOOB(
    AStrategyUnit* Unit,
    bool bFocusCamera)
{
    if (!IsValid(Unit) || !Unit->bPlayerControllable)
    {
        return;
    }

    ClearSelection();
    SelectedUnitObjects.Add(Unit);
    Unit->SetSelected(true);

    if (bFocusCamera)
    {
        if (AStrategyCameraPawn* CameraPawn =
            Cast<AStrategyCameraPawn>(GetPawn()))
        {
            CameraPawn->FocusOnWorldLocation(Unit->GetActorLocation());
        }
    }
}


void AStrategyPlayerController::TogglePauseSimulation()
{
    const bool bNewPaused = !UGameplayStatics::IsGamePaused(this);
    UGameplayStatics::SetGamePaused(this, bNewPaused);
}

void AStrategyPlayerController::SetSimulationSpeed(float NewSpeed)
{
    SimulationSpeed = FMath::Clamp(NewSpeed, 1.0f, 3.0f);
    UGameplayStatics::SetGlobalTimeDilation(this, SimulationSpeed);

    if (UGameplayStatics::IsGamePaused(this))
    {
        UGameplayStatics::SetGamePaused(this, false);
    }
}

void AStrategyPlayerController::SetSpeed1x()
{
    SetSimulationSpeed(1.0f);
}

void AStrategyPlayerController::SetSpeed2x()
{
    SetSimulationSpeed(2.0f);
}

void AStrategyPlayerController::SetSpeed3x()
{
    SetSimulationSpeed(3.0f);
}


void AStrategyPlayerController::ResetQAScenario()
{
    ClearSelection();

    if (AStrategyGameMode* StrategyGameMode =
        GetWorld() ? GetWorld()->GetAuthGameMode<AStrategyGameMode>() : nullptr)
    {
        StrategyGameMode->ResetQAScenario();
    }
}
