#include "StrategyUnit.h"
#include "Components/SceneComponent.h"
#include "Components/SphereComponent.h"
#include "Components/TextRenderComponent.h"
#include "../Orders/StrategyOrderComponent.h"
#include "../Command/StrategyCommandComponent.h"
#include "../Movement/StrategyMovementExecutorComponent.h"
#include "../Formations/StrategyFormationComponent.h"
#include "../Formations/StrategyFormationPolicyComponent.h"
#include "../Formations/StrategyFormationTransitionComponent.h"
#include "../Orders/StrategyParentExecutionComponent.h"
#include "../Navigation/StrategyRoutePlannerComponent.h"
#include "../Combat/StrategyVisibilityComponent.h"
#include "../Combat/StrategyFireControlComponent.h"
#include "../Combat/StrategyCombatComponent.h"
#include "../AI/StrategyOfficerAIComponent.h"
#include "../Combat/StrategyThreatReactionComponent.h"
#include "../Command/StrategyOOBStatusComponent.h"
#include "../UI/StrategySemanticZoomComponent.h"
#include "../UI/StrategyWorldDebugComponent.h"
#include "../UI/StrategyPresentationSnapshotComponent.h"
#include "../Movement/StrategyLocalDeconflictionComponent.h"
#include "Components/MeshComponent.h"

AStrategyUnit::AStrategyUnit()
{
    PrimaryActorTick.bCanEverTick = false;

    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
    SetRootComponent(SceneRoot);

    SelectionCollider = CreateDefaultSubobject<USphereComponent>(TEXT("SelectionCollider"));
    SelectionCollider->SetupAttachment(SceneRoot);
    SelectionCollider->InitSphereRadius(180.0f);
    SelectionCollider->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    SelectionCollider->SetCollisionResponseToAllChannels(ECR_Ignore);
    SelectionCollider->SetCollisionResponseToChannel(ECC_Visibility, ECR_Block);

    DebugLabel = CreateDefaultSubobject<UTextRenderComponent>(TEXT("DebugLabel"));
    DebugLabel->SetupAttachment(SceneRoot);
    DebugLabel->SetRelativeLocation(FVector(0.0f, 0.0f, 220.0f));
    DebugLabel->SetHorizontalAlignment(EHorizTextAligment::EHTA_Center);
    DebugLabel->SetWorldSize(80.0f);
    DebugLabel->SetText(FText::FromString(TEXT("StrategyUnit")));

    OrderComponent = CreateDefaultSubobject<UStrategyOrderComponent>(TEXT("OrderComponent"));
    CommandComponent = CreateDefaultSubobject<UStrategyCommandComponent>(TEXT("CommandComponent"));
    MovementExecutor = CreateDefaultSubobject<UStrategyMovementExecutorComponent>(TEXT("MovementExecutor"));
    FormationComponent = CreateDefaultSubobject<UStrategyFormationComponent>(TEXT("FormationComponent"));
    FormationPolicy = CreateDefaultSubobject<UStrategyFormationPolicyComponent>(TEXT("FormationPolicy"));
    FormationTransition =
        CreateDefaultSubobject<UStrategyFormationTransitionComponent>(TEXT("FormationTransition"));
    ParentExecution = CreateDefaultSubobject<UStrategyParentExecutionComponent>(TEXT("ParentExecution"));
    RoutePlanner = CreateDefaultSubobject<UStrategyRoutePlannerComponent>(TEXT("RoutePlanner"));
    VisibilityComponent = CreateDefaultSubobject<UStrategyVisibilityComponent>(TEXT("VisibilityComponent"));
    FireControlComponent = CreateDefaultSubobject<UStrategyFireControlComponent>(TEXT("FireControlComponent"));
    CombatComponent = CreateDefaultSubobject<UStrategyCombatComponent>(TEXT("CombatComponent"));
    OfficerAIComponent = CreateDefaultSubobject<UStrategyOfficerAIComponent>(TEXT("OfficerAIComponent"));
    ThreatReactionComponent = CreateDefaultSubobject<UStrategyThreatReactionComponent>(TEXT("ThreatReactionComponent"));
    OOBStatusComponent = CreateDefaultSubobject<UStrategyOOBStatusComponent>(TEXT("OOBStatusComponent"));
    SemanticZoomComponent = CreateDefaultSubobject<UStrategySemanticZoomComponent>(TEXT("SemanticZoomComponent"));
    WorldDebugComponent = CreateDefaultSubobject<UStrategyWorldDebugComponent>(TEXT("WorldDebugComponent"));
    PresentationSnapshotComponent =
        CreateDefaultSubobject<UStrategyPresentationSnapshotComponent>(TEXT("PresentationSnapshotComponent"));
    LocalDeconflictionComponent =
        CreateDefaultSubobject<UStrategyLocalDeconflictionComponent>(TEXT("LocalDeconflictionComponent"));
}

void AStrategyUnit::SetSelected(bool bNewSelected)
{
    if (bSelected == bNewSelected)
    {
        return;
    }

    bSelected = bNewSelected;
    OnSelectionChanged(bSelected);
}

void AStrategyUnit::RefreshDebugLabel()
{
    if (!DebugLabel)
    {
        return;
    }

    const FString NameText =
        DisplayName.IsEmpty()
        ? StableUnitId.ToString()
        : DisplayName.ToString();

    const FString StrengthText =
        FString::Printf(TEXT("%d/%d"), CurrentStrength, InitialStrength);

    DebugLabel->SetText(
        FText::FromString(
            FString::Printf(
                TEXT("[%s] %s\n%s"),
                *GetNATOEchelonSymbol(),
                *NameText,
                *StrengthText)));
}

int32 AStrategyUnit::ApplyStrengthLoss(int32 RequestedLoss)
{
    if (RequestedLoss <= 0 || CurrentStrength <= 0)
    {
        return 0;
    }

    const int32 AppliedLoss = FMath::Min(RequestedLoss, CurrentStrength);
    CurrentStrength -= AppliedLoss;

    if (CurrentStrength <= 0)
    {
        CurrentStrength = 0;
        SetUnitState(EStrategyUnitState::Destroyed);
    }

    RefreshDebugLabel();

    OnCasualtyVisualEvent.Broadcast(
        AppliedLoss,
        GetActorLocation());

    return AppliedLoss;
}

void AStrategyUnit::SetUnitState(EStrategyUnitState NewState)
{
    if (UnitState == NewState)
    {
        return;
    }

    UnitState = NewState;
    OnUnitStateChanged(UnitState);
}

bool AStrategyUnit::IsCombatEffective() const
{
    return CurrentStrength > 0 &&
        UnitState != EStrategyUnitState::Routed &&
        UnitState != EStrategyUnitState::Destroyed;
}


void AStrategyUnit::SetSemanticZoomState(EStrategySemanticZoomState NewState)
{
    const bool bChanged = SemanticZoomState != NewState;
    SemanticZoomState = NewState;

    const bool bStrategic =
        SemanticZoomState == EStrategySemanticZoomState::Strategic ||
        SemanticZoomState == EStrategySemanticZoomState::VeryFar;

    TArray<UMeshComponent*> MeshComponents;
    GetComponents<UMeshComponent>(MeshComponents);

    for (UMeshComponent* Mesh : MeshComponents)
    {
        if (IsValid(Mesh))
        {
            Mesh->SetVisibility(!bStrategic, true);
        }
    }

    if (DebugLabel)
    {
        const bool bHQ =
            Echelon == EStrategyEchelon::Battalion ||
            Echelon == EStrategyEchelon::Regiment ||
            Echelon == EStrategyEchelon::Brigade ||
            Echelon == EStrategyEchelon::Division;

        const bool bShowLabel =
            bSelected ||
            (SemanticZoomState == EStrategySemanticZoomState::Medium && bHQ) ||
            SemanticZoomState == EStrategySemanticZoomState::Operational ||
            bStrategic;

        DebugLabel->SetVisibility(bShowLabel);
    }

    if (bChanged)
    {
        OnSemanticZoomChanged(SemanticZoomState);
    }
}

FString AStrategyUnit::GetNATOEchelonSymbol() const
{
    switch (Echelon)
    {
        case EStrategyEchelon::Company:
            return TEXT("I");

        case EStrategyEchelon::Battalion:
            return TEXT("II");

        case EStrategyEchelon::Regiment:
            return TEXT("III");

        case EStrategyEchelon::Brigade:
            return TEXT("X");

        case EStrategyEchelon::Division:
            return TEXT("XX");

        case EStrategyEchelon::Cavalry:
            return TEXT("CAV");

        case EStrategyEchelon::Headquarters:
        default:
            return TEXT("HQ");
    }
}
