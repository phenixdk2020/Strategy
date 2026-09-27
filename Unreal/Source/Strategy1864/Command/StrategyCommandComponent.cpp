#include "StrategyCommandComponent.h"

#include "../Units/StrategyUnit.h"

UStrategyCommandComponent::UStrategyCommandComponent()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UStrategyCommandComponent::SetOrganicParent(AStrategyUnit* NewParent)
{
    AStrategyUnit* OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (!OwnerUnit || NewParent == OwnerUnit || OrganicParent == NewParent)
    {
        return;
    }

    if (OrganicParent && OrganicParent->CommandComponent)
    {
        OrganicParent->CommandComponent->RemoveOrganicSubordinate(OwnerUnit);
    }

    OrganicParent = NewParent;

    if (OrganicParent && OrganicParent->CommandComponent)
    {
        OrganicParent->CommandComponent->AddOrganicSubordinate(OwnerUnit);
    }

    if (!CurrentCommandParent)
    {
        SetCurrentCommandParent(OrganicParent);
    }
}

void UStrategyCommandComponent::SetCurrentCommandParent(AStrategyUnit* NewParent)
{
    AStrategyUnit* OwnerUnit = Cast<AStrategyUnit>(GetOwner());
    if (!OwnerUnit || NewParent == OwnerUnit || CurrentCommandParent == NewParent)
    {
        return;
    }

    AStrategyUnit* OldParent = CurrentCommandParent;
    RemoveFromParentCurrentList(OldParent, OwnerUnit);

    CurrentCommandParent = NewParent;
    AddToParentCurrentList(CurrentCommandParent, OwnerUnit);

    OnCurrentCommandParentChanged.Broadcast(OldParent, CurrentCommandParent);
}

void UStrategyCommandComponent::RestoreOrganicCommandParent()
{
    SetCurrentCommandParent(OrganicParent);
}

void UStrategyCommandComponent::AddOrganicSubordinate(AStrategyUnit* Unit)
{
    if (IsValid(Unit) && Unit != GetOwner())
    {
        OrganicSubordinates.AddUnique(Unit);
    }
}

void UStrategyCommandComponent::RemoveOrganicSubordinate(AStrategyUnit* Unit)
{
    OrganicSubordinates.Remove(Unit);
}

void UStrategyCommandComponent::AddCurrentSubordinate(AStrategyUnit* Unit)
{
    if (IsValid(Unit) && Unit != GetOwner())
    {
        CurrentSubordinates.AddUnique(Unit);
    }
}

void UStrategyCommandComponent::RemoveCurrentSubordinate(AStrategyUnit* Unit)
{
    CurrentSubordinates.Remove(Unit);
}

void UStrategyCommandComponent::RemoveFromParentCurrentList(AStrategyUnit* Parent, AStrategyUnit* Child)
{
    if (IsValid(Parent) && Parent->CommandComponent)
    {
        Parent->CommandComponent->RemoveCurrentSubordinate(Child);
    }
}

void UStrategyCommandComponent::AddToParentCurrentList(AStrategyUnit* Parent, AStrategyUnit* Child)
{
    if (IsValid(Parent) && Parent->CommandComponent)
    {
        Parent->CommandComponent->AddCurrentSubordinate(Child);
    }
}
