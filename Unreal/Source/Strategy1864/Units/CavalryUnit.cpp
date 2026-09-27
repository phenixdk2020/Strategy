#include "CavalryUnit.h"
#include "Components/SkeletalMeshComponent.h"

ACavalryUnit::ACavalryUnit()
{
    Echelon = EStrategyEchelon::Cavalry;

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
