#include "StrategyParentFormationPlannerComponent.h"

#include "../Command/StrategyCommandComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Units/StrategyCompanyUnit.h"
#include "../Units/StrategyUnit.h"

UStrategyParentFormationPlannerComponent::UStrategyParentFormationPlannerComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

TArray<FStrategyFormationSlot> UStrategyParentFormationPlannerComponent::GenerateCompanyLineSlots(
    const FVector& ObjectiveCenter,
    float FacingYaw,
    int32 CompanyCount) const
{
    TArray<FStrategyFormationSlot> Result;
    if (CompanyCount <= 0)
    {
        return Result;
    }

    Result.Reserve(CompanyCount);

    const FRotator FacingRotation(0.0f, FacingYaw, 0.0f);
    const FVector Right = FRotationMatrix(FacingRotation).GetScaledAxis(EAxis::Y);
    const float EffectiveSpacing = FMath::Max(
        CompanyNominalSpacingCm,
        CompanyMinimumReservedSpacingCm);

    for (int32 Index = 0; Index < CompanyCount; ++Index)
    {
        const float CenteredIndex =
            static_cast<float>(Index) - (CompanyCount - 1) * 0.5f;

        FStrategyFormationSlot Slot;
        Slot.WorldLocation = ObjectiveCenter + Right * CenteredIndex * EffectiveSpacing;
        Slot.FacingYaw = FacingYaw;
        Slot.SlotIndex = Index;
        Result.Add(Slot);
    }

    return Result;
}

bool UStrategyParentFormationPlannerComponent::IssueCompanySlots(
    const FVector& ObjectiveCenter,
    float FacingYaw,
    bool bDefensiveMission)
{
    AStrategyUnit* OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (!OwnerUnit)
    {
        return false;
    }

    TArray<AStrategyUnit*> Companies = GetCommandedCompanies();
    if (Companies.Num() == 0)
    {
        return false;
    }

    Companies.Sort([](const AStrategyUnit& A, const AStrategyUnit& B)
    {
        const AStrategyCompanyUnit* CompanyA = Cast<AStrategyCompanyUnit>(&A);
        const AStrategyCompanyUnit* CompanyB = Cast<AStrategyCompanyUnit>(&B);
        const int32 NumberA = CompanyA ? CompanyA->CompanyNumber : MAX_int32;
        const int32 NumberB = CompanyB ? CompanyB->CompanyNumber : MAX_int32;
        return NumberA < NumberB;
    });

    const TArray<FStrategyFormationSlot> Slots =
        GenerateCompanyLineSlots(ObjectiveCenter, FacingYaw, Companies.Num());

    bool bIssuedAny = false;

    for (int32 Index = 0; Index < Companies.Num() && Index < Slots.Num(); ++Index)
    {
        AStrategyUnit* Company = Companies[Index];
        if (!IsValid(Company) || !Company->OrderComponent)
        {
            continue;
        }

        FStrategyOrder ChildOrder;
        ChildOrder.Type =
            bDefensiveMission
            ? EStrategyOrderType::DefendHere
            : EStrategyOrderType::AttackHere;
        ChildOrder.TargetLocation = Slots[Index].WorldLocation;
        ChildOrder.FacingYaw = Slots[Index].FacingYaw;
        ChildOrder.bHasFacing = true;
        ChildOrder.Authority = EStrategyOrderAuthority::OfficerAI;

        if (Company->OrderComponent->SetOrder(ChildOrder))
        {
            bIssuedAny = true;
        }
    }

    return bIssuedAny;
}

TArray<AStrategyUnit*> UStrategyParentFormationPlannerComponent::GetCommandedCompanies() const
{
    TArray<AStrategyUnit*> Result;

    const AStrategyUnit* OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (!OwnerUnit || !OwnerUnit->CommandComponent)
    {
        return Result;
    }

    for (AStrategyUnit* Subordinate : OwnerUnit->CommandComponent->CurrentSubordinates)
    {
        if (IsValid(Subordinate) && Subordinate->Echelon == EStrategyEchelon::Company)
        {
            Result.Add(Subordinate);
        }
    }

    return Result;
}
