#include "StrategyHUD.h"

#include "StrategyPlayerController.h"
#include "../Units/StrategyUnit.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Orders/StrategyOrderTypes.h"

void AStrategyHUD::BeginSelectionBox(const FVector2D& ScreenPoint)
{
    bSelectionBoxActive = true;
    SelectionStart = ScreenPoint;
    SelectionEnd = ScreenPoint;
}

void AStrategyHUD::UpdateSelectionBox(const FVector2D& ScreenPoint)
{
    SelectionEnd = ScreenPoint;
}

void AStrategyHUD::EndSelectionBox()
{
    bSelectionBoxActive = false;
}

void AStrategyHUD::DrawHUD()
{
    Super::DrawHUD();

    if (bDrawQABuildMarker)
    {
        DrawText(
            BuildMarker,
            FLinearColor::White,
            18.0f,
            18.0f,
            nullptr,
            1.0f,
            false);
    }

    if (bDrawSelectedUnitQA)
    {
        if (const AStrategyPlayerController* StrategyPC =
            Cast<AStrategyPlayerController>(GetOwningPlayerController()))
        {
            const TArray<AStrategyUnit*> SelectedUnits =
                StrategyPC->GetSelectedUnits();

            if (SelectedUnits.Num() > 0 && IsValid(SelectedUnits[0]))
            {
                const AStrategyUnit* Unit = SelectedUnits[0];
                FString OrderText = TEXT("NONE");
                FString ExecText = TEXT("Idle");

                if (Unit->OrderComponent)
                {
                    const FStrategyOrder Order =
                        Unit->OrderComponent->GetCurrentOrder();

                    OrderText =
                        StaticEnum<EStrategyOrderType>()->GetNameStringByValue(
                            static_cast<int64>(Order.Type));

                    ExecText =
                        StaticEnum<EStrategyOrderExecutionState>()->GetNameStringByValue(
                            static_cast<int64>(
                                Unit->OrderComponent->GetExecutionState()));
                }

                const FString QA =
                    FString::Printf(
                        TEXT("%s | %s\nSTR %d/%d | MOR %.0f | COH %.0f | AI %s\nORDER %s | EXEC %s"),
                        *Unit->DisplayName.ToString(),
                        *Unit->StableUnitId.ToString(),
                        Unit->CurrentStrength,
                        Unit->InitialStrength,
                        Unit->Morale,
                        Unit->Cohesion,
                        Unit->bOfficerAIEnabled ? TEXT("ON") : TEXT("OFF"),
                        *OrderText,
                        *ExecText);

                DrawText(
                    QA,
                    FLinearColor::White,
                    18.0f,
                    45.0f,
                    nullptr,
                    0.9f,
                    false);
            }
        }
    }

    if (!bSelectionBoxActive)
    {
        return;
    }

    const float Left = FMath::Min(SelectionStart.X, SelectionEnd.X);
    const float Top = FMath::Min(SelectionStart.Y, SelectionEnd.Y);
    const float Right = FMath::Max(SelectionStart.X, SelectionEnd.X);
    const float Bottom = FMath::Max(SelectionStart.Y, SelectionEnd.Y);
    const float Width = Right - Left;
    const float Height = Bottom - Top;

    DrawRect(SelectionFillColor, Left, Top, Width, Height);
    DrawLine(Left, Top, Right, Top, SelectionBorderColor, 1.5f);
    DrawLine(Right, Top, Right, Bottom, SelectionBorderColor, 1.5f);
    DrawLine(Right, Bottom, Left, Bottom, SelectionBorderColor, 1.5f);
    DrawLine(Left, Bottom, Left, Top, SelectionBorderColor, 1.5f);
}
