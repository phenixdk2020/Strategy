#include "StrategyFormationComponent.h"

UStrategyFormationComponent::UStrategyFormationComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UStrategyFormationComponent::SetFormation(EStrategyFormationType NewFormation)
{
    if (CurrentFormation == NewFormation)
    {
        return;
    }

    const EStrategyFormationType OldFormation = CurrentFormation;
    CurrentFormation = NewFormation;
    OnFormationChanged.Broadcast(OldFormation, CurrentFormation);
}

TArray<FStrategyFormationSlot> UStrategyFormationComponent::GenerateSoldierSlots(
    const FVector& FormationCenter,
    float FacingYaw,
    int32 Strength) const
{
    TArray<FStrategyFormationSlot> Result;
    if (Strength <= 0)
    {
        return Result;
    }

    Result.Reserve(Strength);

    const FRotator FacingRotation(0.0f, FacingYaw, 0.0f);
    const FVector Forward = FacingRotation.Vector();
    const FVector Right = FRotationMatrix(FacingRotation).GetScaledAxis(EAxis::Y);

    if (CurrentFormation == EStrategyFormationType::MarchColumn)
    {
        const int32 EffectiveColumnWidth = FMath::Max(1, ColumnWidth);
        const int32 RowCount = FMath::CeilToInt(static_cast<float>(Strength) / EffectiveColumnWidth);

        for (int32 Index = 0; Index < Strength; ++Index)
        {
            const int32 Column = Index % EffectiveColumnWidth;
            const int32 Row = Index / EffectiveColumnWidth;

            const float Lateral =
                (static_cast<float>(Column) - (EffectiveColumnWidth - 1) * 0.5f) * SoldierLateralSpacingCm;
            const float Longitudinal =
                -static_cast<float>(Row) * SoldierRankSpacingCm;

            FStrategyFormationSlot Slot;
            Slot.WorldLocation = FormationCenter + Right * Lateral + Forward * Longitudinal;
            Slot.FacingYaw = FacingYaw;
            Slot.SlotIndex = Index;
            Result.Add(Slot);
        }

        return Result;
    }

    const int32 EffectiveRanks = FMath::Max(1, RankCount);
    const int32 Files = FMath::CeilToInt(static_cast<float>(Strength) / EffectiveRanks);

    for (int32 Index = 0; Index < Strength; ++Index)
    {
        const int32 Rank = Index % EffectiveRanks;
        const int32 File = Index / EffectiveRanks;

        const float Lateral =
            (static_cast<float>(File) - (Files - 1) * 0.5f) * SoldierLateralSpacingCm;
        const float Longitudinal =
            -static_cast<float>(Rank) * SoldierRankSpacingCm;

        FStrategyFormationSlot Slot;
        Slot.WorldLocation = FormationCenter + Right * Lateral + Forward * Longitudinal;
        Slot.FacingYaw = FacingYaw;
        Slot.SlotIndex = Index;
        Result.Add(Slot);
    }

    return Result;
}

float UStrategyFormationComponent::EstimateFrontageCm(int32 Strength) const
{
    if (Strength <= 0)
    {
        return 0.0f;
    }

    if (CurrentFormation == EStrategyFormationType::MarchColumn)
    {
        return FMath::Max(1, ColumnWidth) * SoldierLateralSpacingCm;
    }

    const int32 EffectiveRanks = FMath::Max(1, RankCount);
    const int32 Files = FMath::CeilToInt(static_cast<float>(Strength) / EffectiveRanks);
    return FMath::Max(0, Files - 1) * SoldierLateralSpacingCm;
}
