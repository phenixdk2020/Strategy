#include "CavalryUnit.h"
#include "Components/SkeletalMeshComponent.h"
#include "../Formations/StrategyFormationComponent.h"
#include "../Movement/StrategyMovementExecutorComponent.h"
#include "../Combat/StrategyCavalryChargeComponent.h"
#include "StrategyDragoonComponent.h"

ACavalryUnit::ACavalryUnit()
{
    Echelon = EStrategyEchelon::Cavalry;

    if (FormationComponent)
    {
        FormationComponent->RankCount = 4;
        FormationComponent->ColumnWidth = 4;
        FormationComponent->CurrentFormation = EStrategyFormationType::CavalryLine;
    }

    if (MovementExecutor)
    {
        MovementExecutor->MoveSpeedCmPerSecond = 900.0f;
    }

    ChargeComponent =
        CreateDefaultSubobject<UStrategyCavalryChargeComponent>(TEXT("ChargeComponent"));

    DragoonComponent =
        CreateDefaultSubobject<UStrategyDragoonComponent>(TEXT("DragoonComponent"));

    HorseMesh = CreateDefaultSubobject<USkeletalMeshComponent>(TEXT("HorseMesh"));
    HorseMesh->SetupAttachment(SceneRoot);

    RiderMesh = CreateDefaultSubobject<USkeletalMeshComponent>(TEXT("RiderMesh"));
    RiderMesh->SetupAttachment(HorseMesh, RiderSocketName);
}

void ACavalryUnit::BeginPlay()
{
    Super::BeginPlay();

    if (HorseMesh && RiderMesh && HorseMesh->DoesSocketExist(RiderSocketName))
    {
        RiderMesh->AttachToComponent(
            HorseMesh,
            FAttachmentTransformRules::SnapToTargetNotIncludingScale,
            RiderSocketName);
    }
}


void ACavalryUnit::SetDefileMode(bool bEnable)
{
    if (!FormationComponent)
    {
        return;
    }

    if (bEnable)
    {
        if (FormationComponent->CurrentFormation != EStrategyFormationType::DefileColumn)
        {
            PreDefileFormation = FormationComponent->CurrentFormation;
            FormationComponent->SetFormation(EStrategyFormationType::DefileColumn);
        }
        return;
    }

    if (FormationComponent->CurrentFormation == EStrategyFormationType::DefileColumn)
    {
        FormationComponent->SetFormation(PreDefileFormation);
    }
}
