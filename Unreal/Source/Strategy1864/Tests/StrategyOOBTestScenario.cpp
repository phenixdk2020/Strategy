#include "StrategyOOBTestScenario.h"

#include "../Command/StrategyCommandComponent.h"
#include "../Units/StrategyCompanyUnit.h"
#include "../Units/StrategyHQUnit.h"
#include "../Units/StrategyUnit.h"
#include "../Units/CavalryUnit.h"
#include "../Units/StrategyDragoonComponent.h"
#include "../Navigation/StrategyRiverBarrier.h"
#include "../Combat/StrategyCombatComponent.h"
#include "../Formations/StrategyFormationComponent.h"
#include "../Navigation/StrategyNavigationObstacle.h"
#include "../AI/StrategyOfficerProfileComponent.h"
#include "../AI/StrategyCommandDelayComponent.h"
#include "../Combat/StrategyConditionComponent.h"
#include "../Combat/StrategyContactComponent.h"
#include "../AI/StrategyReconComponent.h"
#include "../AI/StrategyAutonomousBattleAIComponent.h"
#include "../AI/StrategyRoutRecoveryComponent.h"
#include "../AI/StrategyCavalryScreenAIComponent.h"
#include "../Combat/StrategyFireControlComponent.h"
#include "../Combat/StrategyFireDisciplineComponent.h"
#include "../Combat/StrategyStanceComponent.h"
#include "../Combat/StrategyDirectionalCoverComponent.h"
#include "../Combat/StrategyFieldworksComponent.h"
#include "../Combat/StrategySkirmisherComponent.h"
#include "../Logistics/StrategySupplyComponent.h"
#include "../AI/StrategyDoctrineComponent.h"
#include "../AI/StrategyAutonomyComponent.h"
#include "../AI/StrategyAIDifficultyComponent.h"
#include "../AI/StrategyAITelemetryComponent.h"
#include "../AI/StrategyMissionConstraintsComponent.h"
#include "../Artillery/StrategyArtilleryBatteryUnit.h"
#include "../Artillery/StrategyArtilleryDeploymentComponent.h"
#include "../Artillery/StrategyArtilleryAmmunitionComponent.h"
#include "../Artillery/StrategyArtilleryFireMissionComponent.h"
#include "../Artillery/StrategyArtilleryDamageComponent.h"
#include "../Artillery/StrategyArtilleryCaptureComponent.h"
#include "../Artillery/StrategyArtilleryTraverseComponent.h"
#include "../Artillery/StrategyArtilleryRepairComponent.h"
#include "../Logistics/StrategySupplyWagonUnit.h"
#include "../Logistics/StrategySupplyCargoComponent.h"
#include "../Logistics/StrategySupplyCaptureComponent.h"
#include "../Terrain/StrategyTerrainFeature.h"
#include "../Terrain/StrategyTerrainQueryLibrary.h"
#include "../Terrain/StrategyTerrainAwarenessComponent.h"
#include "../Artillery/StrategyArtilleryPositioningComponent.h"
#include "Engine/World.h"

AStrategyOOBTestScenario::AStrategyOOBTestScenario()
{
    PrimaryActorTick.bCanEverTick = false;
}

void AStrategyOOBTestScenario::BeginPlay()
{
    Super::BeginPlay();

    if (bBuildOnBeginPlay)
    {
        BuildTestOOB();
    }
}

void AStrategyOOBTestScenario::BuildTestOOB()
{
    ClearSpawnedUnits();

    if (bSpawnTerrainQA)
    {
        SpawnTerrainFeature(
            EStrategyTerrainFeatureType::Hill,
            Origin + FVector(1800.0f, 7200.0f, 0.0f),
            FVector2D(5200.0f, 4300.0f),
            750.0f,
            0.0f);

        SpawnTerrainFeature(
            EStrategyTerrainFeatureType::Ridge,
            Origin + FVector(13000.0f, 1500.0f, 0.0f),
            FVector2D(2200.0f, 3200.0f),
            1800.0f,
            0.0f);

        SpawnTerrainFeature(
            EStrategyTerrainFeatureType::Depression,
            Origin + FVector(17500.0f, -9000.0f, 0.0f),
            FVector2D(3500.0f, 2600.0f),
            550.0f,
            15.0f);
    }

    AStrategyHQUnit* Division = SpawnHQ(
        TEXT("DK-DIV-1"),
        TEXT("1. Division"),
        static_cast<uint8>(EStrategyHQLevel::Division),
        Origin + FVector(0.0f, 0.0f, 0.0f),
        nullptr);

    if (Division && Division->SupplyComponent)
    {
        Division->SupplyComponent->bActsAsSupplySource =
            !bSpawnSupplyWagonQA;

        Division->SupplyComponent->StoredAmmunitionRounds =
            bSpawnSupplyWagonQA ? 0 : 50000;

        Division->SupplyComponent->MaxStoredAmmunitionRounds =
            bSpawnSupplyWagonQA ? 0 : 50000;

        Division->SupplyComponent->ResupplyRadiusCm = 30000.0f;
    }

    AStrategyHQUnit* Brigade = SpawnHQ(
        TEXT("DK-BDE-1"),
        TEXT("1. Brigade"),
        static_cast<uint8>(EStrategyHQLevel::Brigade),
        Origin + FVector(1800.0f, 0.0f, 0.0f),
        Division);

    AStrategyHQUnit* Regiment = SpawnHQ(
        TEXT("DK-REG-1"),
        TEXT("1. Regiment"),
        static_cast<uint8>(EStrategyHQLevel::Regiment),
        Origin + FVector(3600.0f, 0.0f, 0.0f),
        Brigade);

    AStrategyHQUnit* MajorA = SpawnHQ(
        TEXT("DK-REG-1-MAJ-A"),
        TEXT("Major A"),
        static_cast<uint8>(EStrategyHQLevel::Battalion),
        Origin + FVector(5400.0f, -2200.0f, 0.0f),
        Regiment);

    AStrategyHQUnit* MajorB = SpawnHQ(
        TEXT("DK-REG-1-MAJ-B"),
        TEXT("Major B"),
        static_cast<uint8>(EStrategyHQLevel::Battalion),
        Origin + FVector(5400.0f, 2200.0f, 0.0f),
        Regiment);

    if (bSpawnCavalryQA)
    {
        SpawnCavalry(
            TEXT("DK-CAV-1"),
            TEXT("Gardehusar QA"),
            Origin + FVector(2500.0f, -5000.0f, 0.0f),
            Division);

        if (ACavalryUnit* Dragoon = SpawnCavalry(
            TEXT("DK-CAV-2"),
            TEXT("Dragon QA"),
            Origin + FVector(2500.0f, 5000.0f, 0.0f),
            Division))
        {
            if (Dragoon->DragoonComponent)
            {
                Dragoon->DragoonComponent->Role =
                    EStrategyCavalryRole::Dragoon;
            }
        }
    }

    if (bSpawnArtilleryQA)
    {
        SpawnArtilleryBattery(
            TEXT("DK-ART-BAT-1"),
            TEXT("Artilleribatteri QA"),
            Origin + FVector(1500.0f, 7800.0f, 0.0f),
            Division);
    }

    if (bSpawnSupplyWagonQA)
    {
        SpawnSupplyWagon(
            TEXT("DK-SUP-WAGON-1"),
            TEXT("Ammunitionsvogn QA"),
            Origin + FVector(1500.0f, 5600.0f, 0.0f),
            Division);
    }

    for (int32 Index = 0; Index < 4; ++Index)
    {
        const int32 CompanyNumber = Index + 1;
        SpawnCompany(
            FName(*FString::Printf(TEXT("DK-REG-1-A-C%d"), CompanyNumber)),
            FString::Printf(TEXT("%d. Kompagni"), CompanyNumber),
            CompanyNumber,
            Origin + FVector(7600.0f, -4300.0f + Index * CompanySpacing, 0.0f),
            MajorA,
            static_cast<uint8>(EStrategySide::Denmark));
    }

    for (int32 Index = 0; Index < 4; ++Index)
    {
        const int32 CompanyNumber = Index + 5;
        SpawnCompany(
            FName(*FString::Printf(TEXT("DK-REG-1-B-C%d"), CompanyNumber)),
            FString::Printf(TEXT("%d. Kompagni"), CompanyNumber),
            CompanyNumber,
            Origin + FVector(9800.0f, -4300.0f + Index * CompanySpacing, 0.0f),
            MajorB,
            static_cast<uint8>(EStrategySide::Denmark));
    }

    if (bSpawnRiverQA && GetWorld())
    {
        SpawnedRiverBarrier = GetWorld()->SpawnActor<AStrategyRiverBarrier>(
            AStrategyRiverBarrier::StaticClass(),
            Origin + FVector(15000.0f, 0.0f, 0.0f),
            FRotator::ZeroRotator);

        if (SpawnedRiverBarrier)
        {
            SpawnedRiverBarrier->RiverAxisDirection = FVector(0.0f, 1.0f, 0.0f);
            SpawnedRiverBarrier->RiverHalfWidthCm = 1200.0f;
            SpawnedRiverBarrier->BankAApproachOffset = FVector(-1800.0f, 0.0f, 0.0f);
            SpawnedRiverBarrier->BankBApproachOffset = FVector(1800.0f, 0.0f, 0.0f);
            SpawnedRiverBarrier->ExitClearanceCm = 3600.0f;
        }
    }

    if (bSpawnObstacleQA && GetWorld())
    {
        SpawnedNavigationObstacle =
            GetWorld()->SpawnActor<AStrategyNavigationObstacle>(
                AStrategyNavigationObstacle::StaticClass(),
                Origin + FVector(9000.0f, -12000.0f, 0.0f),
                FRotator::ZeroRotator);

        if (SpawnedNavigationObstacle)
        {
            SpawnedNavigationObstacle->ObstacleType =
                EStrategyObstacleType::Fence;
            SpawnedNavigationObstacle->HalfExtentCm =
                FVector(1800.0f, 250.0f, 150.0f);
            SpawnedNavigationObstacle->ClearanceCm = 700.0f;
        }
    }

    if (bSpawnEnemyQAUnits)
    {
        SpawnCompany(
            TEXT("PR-QA-C1"),
            TEXT("PR. 1. KOMPAGNI"),
            1,
            Origin + FVector(22000.0f, -3500.0f, 0.0f),
            nullptr,
            static_cast<uint8>(EStrategySide::Prussia));

        SpawnCompany(
            TEXT("PR-QA-C2"),
            TEXT("PR. 2. KOMPAGNI"),
            2,
            Origin + FVector(22000.0f, 3500.0f, 0.0f),
            nullptr,
            static_cast<uint8>(EStrategySide::Prussia));
    }

    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (IsValid(Unit) && Unit->CombatComponent)
        {
            const int32 UnitSeed =
                QARandomSeed ^
                static_cast<int32>(GetTypeHash(Unit->StableUnitId));

            Unit->CombatComponent->SetDeterministicRandomSeed(UnitSeed);
        }

        if (AStrategyArtilleryBatteryUnit* Battery =
            Cast<AStrategyArtilleryBatteryUnit>(Unit))
        {
            const int32 ArtillerySeed =
                QARandomSeed ^
                static_cast<int32>(GetTypeHash(Battery->StableUnitId)) ^
                0x7719;

            if (Battery->ArtilleryDamageComponent)
            {
                Battery->ArtilleryDamageComponent
                    ->SetDeterministicRandomSeed(ArtillerySeed);
            }
        }
    }

    TArray<FString> ValidationErrors;
    const bool bHierarchyValid = ValidateStableIdsAndHierarchy(ValidationErrors);

    UE_LOG(
        LogTemp,
        bHierarchyValid ? Display : Error,
        TEXT("PROJECT1864-QA: StableId/Hierarchy validation %s (%d errors)"),
        bHierarchyValid ? TEXT("PASS") : TEXT("FAIL"),
        ValidationErrors.Num());

    for (const FString& Error : ValidationErrors)
    {
        UE_LOG(LogTemp, Error, TEXT("PROJECT1864-QA: %s"), *Error);
    }

    UE_LOG(LogTemp, Display, TEXT("PROJECT1864-OOB: spawned %d units"), SpawnedUnitObjects.Num());

    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (!IsValid(Unit))
        {
            continue;
        }

        const FString ParentName =
            Unit->CommandComponent && Unit->CommandComponent->CurrentCommandParent
            ? Unit->CommandComponent->CurrentCommandParent->DisplayName.ToString()
            : TEXT("<ROOT>");

        UE_LOG(
            LogTemp,
            Display,
            TEXT("PROJECT1864-OOB: %s [%s] CurrentParent=%s OrganicSubordinates=%d CurrentSubordinates=%d"),
            *Unit->DisplayName.ToString(),
            *Unit->StableUnitId.ToString(),
            *ParentName,
            Unit->CommandComponent ? Unit->CommandComponent->OrganicSubordinates.Num() : 0,
            Unit->CommandComponent ? Unit->CommandComponent->CurrentSubordinates.Num() : 0);
    }

    TArray<FString> RegressionFailures;
    const bool bRegressionPass = RunRegressionChecklist(RegressionFailures);

    UE_LOG(
        LogTemp,
        bRegressionPass ? Display : Error,
        TEXT("PROJECT1864-QA: regression checklist %s (%d failures)"),
        bRegressionPass ? TEXT("PASS") : TEXT("FAIL"),
        RegressionFailures.Num());

    for (const FString& Failure : RegressionFailures)
    {
        UE_LOG(LogTemp, Error, TEXT("PROJECT1864-QA: %s"), *Failure);
    }
}

void AStrategyOOBTestScenario::ClearSpawnedUnits()
{
    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (IsValid(Unit))
        {
            Unit->Destroy();
        }
    }

    SpawnedUnitObjects.Reset();

    if (IsValid(SpawnedRiverBarrier))
    {
        SpawnedRiverBarrier->Destroy();
    }

    SpawnedRiverBarrier = nullptr;

    if (IsValid(SpawnedNavigationObstacle))
    {
        SpawnedNavigationObstacle->Destroy();
    }

    SpawnedNavigationObstacle = nullptr;

    for (AStrategyTerrainFeature* Feature : SpawnedTerrainFeatures)
    {
        if (IsValid(Feature))
        {
            Feature->Destroy();
        }
    }

    SpawnedTerrainFeatures.Reset();
}

AStrategyHQUnit* AStrategyOOBTestScenario::SpawnHQ(
    const FName StableId,
    const FString& Name,
    uint8 HQLevelValue,
    const FVector& Location,
    AStrategyUnit* OrganicParent)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    const FVector SpawnLocation =
        UStrategyTerrainQueryLibrary::ProjectPointToTerrain(
            this,
            Location);

    AStrategyHQUnit* HQ = World->SpawnActor<AStrategyHQUnit>(
        AStrategyHQUnit::StaticClass(),
        SpawnLocation,
        FRotator::ZeroRotator);

    if (!HQ)
    {
        return nullptr;
    }

    HQ->StableUnitId = StableId;
    HQ->DisplayName = FText::FromString(Name);
    HQ->HQLevel = static_cast<EStrategyHQLevel>(HQLevelValue);
    HQ->ApplyHQLevelDefaults();
    HQ->InitialStrength = 1;
    HQ->CurrentStrength = 1;
    HQ->Side = EStrategySide::Denmark;
    HQ->RefreshDebugLabel();

    if (HQ->CommandComponent)
    {
        HQ->CommandComponent->SetOrganicParent(OrganicParent);
    }

    SpawnedUnitObjects.Add(HQ);
    return HQ;
}

AStrategyCompanyUnit* AStrategyOOBTestScenario::SpawnCompany(
    const FName StableId,
    const FString& Name,
    int32 CompanyNumber,
    const FVector& Location,
    AStrategyUnit* OrganicParent,
    uint8 SideValue)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    const FVector SpawnLocation =
        UStrategyTerrainQueryLibrary::ProjectPointToTerrain(
            this,
            Location);

    AStrategyCompanyUnit* Company = World->SpawnActor<AStrategyCompanyUnit>(
        AStrategyCompanyUnit::StaticClass(),
        SpawnLocation,
        FRotator::ZeroRotator);

    if (!Company)
    {
        return nullptr;
    }

    Company->StableUnitId = StableId;
    Company->DisplayName = FText::FromString(Name);
    Company->CompanyNumber = CompanyNumber;
    Company->InitialStrength = 190;
    Company->CurrentStrength = 190;
    Company->Side = static_cast<EStrategySide>(SideValue);
    Company->bPlayerControllable = Company->Side == EStrategySide::Denmark;
    Company->RefreshDebugLabel();

    if (Company->CommandComponent)
    {
        Company->CommandComponent->SetOrganicParent(OrganicParent);
    }

    const bool bPrussian =
        Company->Side == EStrategySide::Prussia;

    if (Company->FireControlComponent)
    {
        Company->FireControlComponent->SetFirePolicy(
            EStrategyFirePolicy::Medium);
    }

    if (Company->FireDisciplineComponent)
    {
        Company->FireDisciplineComponent->Discipline =
            EStrategyFireDiscipline::Volley;
        Company->FireDisciplineComponent->bConserveAmmunition = false;
    }

    if (Company->DoctrineComponent)
    {
        Company->DoctrineComponent->Doctrine =
            bPrussian
            ? EStrategyDoctrine::Offensive
            : EStrategyDoctrine::Defensive;

        Company->DoctrineComponent->CommanderOrderAggression =
            bPrussian ? 65.0f : 35.0f;
    }

    if (Company->AutonomyComponent)
    {
        Company->AutonomyComponent->Autonomy =
            bPrussian
            ? EStrategyAutonomyLevel::Independent
            : EStrategyAutonomyLevel::Normal;
    }

    if (Company->MissionConstraintsComponent)
    {
        Company->MissionConstraintsComponent->bDoNotPursue = true;
        Company->MissionConstraintsComponent->bConserveAmmunition = false;
    }

    if (Company->OfficerProfileComponent)
    {
        if (bPrussian && CompanyNumber == 1)
        {
            Company->OfficerProfileComponent->Leadership = 62.0f;
            Company->OfficerProfileComponent->Inspiration = 55.0f;
            Company->OfficerProfileComponent->TacticalSkill = 68.0f;
            Company->OfficerProfileComponent->Initiative = 65.0f;
            Company->OfficerProfileComponent->StaffQuality = 60.0f;
            Company->OfficerProfileComponent->Aggression = 72.0f;
            Company->OfficerProfileComponent->Caution = 28.0f;
            Company->OfficerProfileComponent->Discipline = 65.0f;
            Company->OfficerProfileComponent->Composure = 60.0f;
            Company->OfficerProfileComponent->Experience = 55.0f;
        }
        else if (bPrussian)
        {
            Company->OfficerProfileComponent->Leadership = 48.0f;
            Company->OfficerProfileComponent->Inspiration = 44.0f;
            Company->OfficerProfileComponent->TacticalSkill = 52.0f;
            Company->OfficerProfileComponent->Initiative = 45.0f;
            Company->OfficerProfileComponent->StaffQuality = 50.0f;
            Company->OfficerProfileComponent->Aggression = 58.0f;
            Company->OfficerProfileComponent->Caution = 42.0f;
            Company->OfficerProfileComponent->Discipline = 54.0f;
            Company->OfficerProfileComponent->Composure = 46.0f;
            Company->OfficerProfileComponent->Experience = 45.0f;
        }
        else
        {
            const float Variant =
                static_cast<float>(CompanyNumber % 4) * 3.0f;

            Company->OfficerProfileComponent->Leadership = 55.0f + Variant;
            Company->OfficerProfileComponent->Inspiration = 52.0f + Variant;
            Company->OfficerProfileComponent->TacticalSkill = 50.0f + Variant;
            Company->OfficerProfileComponent->Initiative = 48.0f + Variant;
            Company->OfficerProfileComponent->StaffQuality = 54.0f + Variant;
            Company->OfficerProfileComponent->Aggression = 42.0f + Variant;
            Company->OfficerProfileComponent->Caution = 58.0f - Variant;
            Company->OfficerProfileComponent->Discipline = 60.0f + Variant;
            Company->OfficerProfileComponent->Composure = 56.0f + Variant;
            Company->OfficerProfileComponent->Experience = 50.0f + Variant;
        }
    }

    SpawnedUnitObjects.Add(Company);
    return Company;
}

ACavalryUnit* AStrategyOOBTestScenario::SpawnCavalry(
    const FName StableId,
    const FString& Name,
    const FVector& Location,
    AStrategyUnit* OrganicParent)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    const FVector SpawnLocation =
        UStrategyTerrainQueryLibrary::ProjectPointToTerrain(
            this,
            Location);

    ACavalryUnit* Cavalry = World->SpawnActor<ACavalryUnit>(
        ACavalryUnit::StaticClass(),
        SpawnLocation,
        FRotator::ZeroRotator);

    if (!Cavalry)
    {
        return nullptr;
    }

    Cavalry->StableUnitId = StableId;
    Cavalry->DisplayName = FText::FromString(Name);
    Cavalry->InitialStrength = 80;
    Cavalry->CurrentStrength = 80;
    Cavalry->Side = EStrategySide::Denmark;
    Cavalry->bPlayerControllable = true;
    Cavalry->RefreshDebugLabel();

    if (Cavalry->CommandComponent)
    {
        Cavalry->CommandComponent->SetOrganicParent(OrganicParent);
    }

    SpawnedUnitObjects.Add(Cavalry);
    return Cavalry;
}

AStrategyArtilleryBatteryUnit* AStrategyOOBTestScenario::SpawnArtilleryBattery(
    const FName StableId,
    const FString& Name,
    const FVector& Location,
    AStrategyUnit* OrganicParent)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    const FVector SpawnLocation =
        UStrategyTerrainQueryLibrary::ProjectPointToTerrain(
            this,
            Location);

    AStrategyArtilleryBatteryUnit* Battery =
        World->SpawnActor<AStrategyArtilleryBatteryUnit>(
            AStrategyArtilleryBatteryUnit::StaticClass(),
            SpawnLocation,
            FRotator(0.0f, 0.0f, 0.0f));

    if (!Battery)
    {
        return nullptr;
    }

    Battery->StableUnitId = StableId;
    Battery->DisplayName = FText::FromString(Name);
    Battery->Side = EStrategySide::Denmark;
    Battery->bPlayerControllable = true;

    Battery->GunCount = 6;
    Battery->CrewStrength = 72;
    Battery->DriverStrength = 18;
    Battery->HorseStrength = 48;
    Battery->HorsesRequiredForFullMobility = 36;
    Battery->DriversRequiredForFullMobility = 12;
    Battery->InitialStrength = 90;
    Battery->CurrentStrength = 90;
    Battery->Experience = 45.0f;

    if (Battery->DeploymentComponent)
    {
        Battery->DeploymentComponent->MobilityState =
            EStrategyArtilleryMobilityState::Deployed;
    }

    if (Battery->ArtilleryFireMissionComponent)
    {
        Battery->ArtilleryFireMissionComponent->SetHoldFire(false);
        Battery->ArtilleryFireMissionComponent->SetAutoTargetEnabled(false);
        Battery->ArtilleryFireMissionComponent->SetMissionLimits(4, 90.0f);
        Battery->ArtilleryFireMissionComponent->SetConserveAmmunition(
            true,
            0.20f);
    }

    if (Battery->ArtilleryAmmunitionComponent)
    {
        Battery->ArtilleryAmmunitionComponent->RoundShotRounds = 15;
        Battery->ArtilleryAmmunitionComponent->ShellRounds = 15;
        Battery->ArtilleryAmmunitionComponent->ShrapnelRounds = 10;
        Battery->ArtilleryAmmunitionComponent->CanisterRounds = 10;
    }

    if (Battery->SupplyComponent)
    {
        Battery->SupplyComponent->RequestResupply();
    }

    if (Battery->CommandComponent)
    {
        Battery->CommandComponent->SetOrganicParent(OrganicParent);
    }

    Battery->RefreshDebugLabel();
    SpawnedUnitObjects.Add(Battery);
    return Battery;
}

AStrategySupplyWagonUnit* AStrategyOOBTestScenario::SpawnSupplyWagon(
    const FName StableId,
    const FString& Name,
    const FVector& Location,
    AStrategyUnit* OrganicParent)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    const FVector SpawnLocation =
        UStrategyTerrainQueryLibrary::ProjectPointToTerrain(
            this,
            Location);

    AStrategySupplyWagonUnit* Wagon =
        World->SpawnActor<AStrategySupplyWagonUnit>(
            AStrategySupplyWagonUnit::StaticClass(),
            SpawnLocation,
            FRotator::ZeroRotator);

    if (!Wagon)
    {
        return nullptr;
    }

    Wagon->StableUnitId = StableId;
    Wagon->DisplayName = FText::FromString(Name);
    Wagon->Side = EStrategySide::Denmark;
    Wagon->bPlayerControllable = true;
    Wagon->DriverStrength = 4;
    Wagon->HorseStrength = 12;
    Wagon->DriversRequiredForFullMobility = 2;
    Wagon->HorsesRequiredForFullMobility = 8;
    Wagon->WagonCondition = 100.0f;
    Wagon->InitialStrength = 4;
    Wagon->CurrentStrength = 4;

    if (Wagon->CargoComponent)
    {
        Wagon->CargoComponent->SmallArmsRounds = 8000;
        Wagon->CargoComponent->ArtilleryRounds = 420;
        Wagon->CargoComponent->ArtilleryAmmunitionFamilyTag =
            TEXT("FIELD_ARTILLERY_GENERIC");
    }

    if (Wagon->SupplyComponent)
    {
        Wagon->SupplyComponent->bActsAsSupplySource = true;
        Wagon->SupplyComponent->ResupplyRadiusCm = 3000.0f;
        Wagon->SupplyComponent->TransferRoundsPerSecond = 90.0f;
    }

    if (Wagon->CommandComponent)
    {
        Wagon->CommandComponent->SetOrganicParent(OrganicParent);
    }

    Wagon->RefreshDebugLabel();
    SpawnedUnitObjects.Add(Wagon);
    return Wagon;
}

AStrategyTerrainFeature* AStrategyOOBTestScenario::SpawnTerrainFeature(
    EStrategyTerrainFeatureType FeatureType,
    const FVector& Location,
    const FVector2D& RadiusCm,
    float PeakHeightCm,
    float YawDegrees)
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    AStrategyTerrainFeature* Feature =
        World->SpawnActor<AStrategyTerrainFeature>(
            AStrategyTerrainFeature::StaticClass(),
            Location,
            FRotator(0.0f, YawDegrees, 0.0f));

    if (!Feature)
    {
        return nullptr;
    }

    Feature->FeatureType = FeatureType;
    Feature->RadiusXcm = FMath::Max(100.0f, RadiusCm.X);
    Feature->RadiusYcm = FMath::Max(100.0f, RadiusCm.Y);
    Feature->PeakHeightCm = PeakHeightCm;
    Feature->bAffectsGameplay = true;

    SpawnedTerrainFeatures.Add(Feature);
    return Feature;
}

TArray<AStrategyUnit*> AStrategyOOBTestScenario::GetSpawnedUnits() const
{
    TArray<AStrategyUnit*> Result;
    Result.Reserve(SpawnedUnitObjects.Num());

    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (IsValid(Unit))
        {
            Result.Add(Unit);
        }
    }

    return Result;
}


void AStrategyOOBTestScenario::ResetScenario()
{
    BuildTestOOB();
}

bool AStrategyOOBTestScenario::ValidateStableIdsAndHierarchy(
    TArray<FString>& OutErrors) const
{
    OutErrors.Reset();

    TSet<FName> SeenIds;

    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (!IsValid(Unit))
        {
            OutErrors.Add(TEXT("Spawned unit reference is invalid."));
            continue;
        }

        if (Unit->StableUnitId.IsNone())
        {
            OutErrors.Add(
                FString::Printf(
                    TEXT("Unit '%s' has no StableUnitId."),
                    *Unit->DisplayName.ToString()));
        }
        else if (SeenIds.Contains(Unit->StableUnitId))
        {
            OutErrors.Add(
                FString::Printf(
                    TEXT("Duplicate StableUnitId: %s"),
                    *Unit->StableUnitId.ToString()));
        }
        else
        {
            SeenIds.Add(Unit->StableUnitId);
        }

        if (!Unit->CommandComponent)
        {
            continue;
        }

        AStrategyUnit* CurrentParent =
            Unit->CommandComponent->CurrentCommandParent;

        if (IsValid(CurrentParent) &&
            (!CurrentParent->CommandComponent ||
             !CurrentParent->CommandComponent->CurrentSubordinates.Contains(Unit)))
        {
            OutErrors.Add(
                FString::Printf(
                    TEXT("%s current parent does not contain child backlink."),
                    *Unit->StableUnitId.ToString()));
        }

        AStrategyUnit* OrganicParent =
            Unit->CommandComponent->OrganicParent;

        if (IsValid(OrganicParent) &&
            (!OrganicParent->CommandComponent ||
             !OrganicParent->CommandComponent->OrganicSubordinates.Contains(Unit)))
        {
            OutErrors.Add(
                FString::Printf(
                    TEXT("%s organic parent does not contain child backlink."),
                    *Unit->StableUnitId.ToString()));
        }

        TSet<const AStrategyUnit*> ParentChain;
        const AStrategyUnit* Cursor = Unit;

        while (IsValid(Cursor) && Cursor->CommandComponent)
        {
            Cursor = Cursor->CommandComponent->CurrentCommandParent;
            if (!IsValid(Cursor))
            {
                break;
            }

            if (ParentChain.Contains(Cursor))
            {
                OutErrors.Add(
                    FString::Printf(
                        TEXT("Command cycle detected from %s."),
                        *Unit->StableUnitId.ToString()));
                break;
            }

            ParentChain.Add(Cursor);
        }
    }

    return OutErrors.Num() == 0;
}


bool AStrategyOOBTestScenario::RunRegressionChecklist(
    TArray<FString>& OutFailures) const
{
    OutFailures.Reset();

    TArray<FString> HierarchyErrors;
    if (!ValidateStableIdsAndHierarchy(HierarchyErrors))
    {
        OutFailures.Append(HierarchyErrors);
    }

    const int32 ExpectedCount =
        13 +
        (bSpawnCavalryQA ? 2 : 0) +
        (bSpawnEnemyQAUnits ? 2 : 0) +
        (bSpawnArtilleryQA ? 1 : 0) +
        (bSpawnSupplyWagonQA ? 1 : 0);

    int32 ValidCount = 0;
    int32 EnemySelectableCount = 0;
    int32 DanishCavalryCount = 0;
    int32 DanishArtilleryCount = 0;
    int32 DanishSupplyWagonCount = 0;
    bool bFoundDragoon = false;

    for (AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (!IsValid(Unit))
        {
            continue;
        }

        ++ValidCount;

        if (Unit->Side == EStrategySide::Prussia &&
            Unit->bPlayerControllable)
        {
            ++EnemySelectableCount;
        }

        if (Unit->Side == EStrategySide::Denmark &&
            Unit->Echelon == EStrategyEchelon::Company &&
            Unit->FormationComponent &&
            !Unit->FormationComponent->IsFullCompanyFrontageWithinBaseline(190))
        {
            OutFailures.Add(
                FString::Printf(
                    TEXT("%s full-company frontage is outside the 48m QA baseline."),
                    *Unit->StableUnitId.ToString()));
        }

        if (!Unit->MissionAnchorComponent ||
            !Unit->OfficerProfileComponent ||
            !Unit->CommandDelayComponent ||
            !Unit->ConditionComponent ||
            !Unit->ContactComponent ||
            !Unit->ReconComponent ||
            !Unit->AutonomousBattleAIComponent ||
            !Unit->RoutRecoveryComponent ||
            !Unit->FireDisciplineComponent ||
            !Unit->StanceComponent ||
            !Unit->DirectionalCoverComponent ||
            !Unit->FieldworksComponent ||
            !Unit->SkirmisherComponent ||
            !Unit->SupplyComponent ||
            !Unit->DoctrineComponent ||
            !Unit->AutonomyComponent ||
            !Unit->AIDifficultyComponent ||
            !Unit->AITelemetryComponent ||
            !Unit->MissionConstraintsComponent)
        {
            OutFailures.Add(
                FString::Printf(
                    TEXT("%s is missing one or more gameplay-core components."),
                    *Unit->StableUnitId.ToString()));
        }

        if (Unit->CommandDelayComponent &&
            Unit->CommandDelayComponent->CalculateDelayFromCurrentParent() < 0.0f)
        {
            OutFailures.Add(
                FString::Printf(
                    TEXT("%s produced a negative command delay."),
                    *Unit->StableUnitId.ToString()));
        }

        if (Unit->CombatComponent &&
            Unit->CombatComponent->AmmunitionRounds < 0)
        {
            OutFailures.Add(
                FString::Printf(
                    TEXT("%s has invalid negative ammunition."),
                    *Unit->StableUnitId.ToString()));
        }

        if (Unit->Side == EStrategySide::Denmark &&
            Unit->Echelon == EStrategyEchelon::Artillery)
        {
            ++DanishArtilleryCount;

            const AStrategyArtilleryBatteryUnit* Battery =
                Cast<AStrategyArtilleryBatteryUnit>(Unit);

            if (!Battery ||
                !Battery->DeploymentComponent ||
                !Battery->ArtilleryAmmunitionComponent ||
                !Battery->ArtilleryFireMissionComponent ||
                !Battery->ArtilleryDamageComponent ||
                !Battery->ArtilleryCaptureComponent ||
                !Battery->ArtilleryTraverseComponent ||
                !Battery->ArtilleryRepairComponent)
            {
                OutFailures.Add(
                    TEXT("Artillery QA battery is missing one or more artillery-core components."));
            }
            else
            {
                if (Battery->GunCount <= 0 ||
                    Battery->CrewStrength <= 0 ||
                    Battery->HorseStrength <= 0 ||
                    Battery->GetOperationalGunCount() <= 0)
                {
                    OutFailures.Add(
                        TEXT("Artillery QA battery has invalid gun/crew/horse operational state."));
                }

                if (Battery->ArtilleryAmmunitionComponent->GetTotalRounds() <= 0)
                {
                    OutFailures.Add(TEXT("Artillery QA battery has no ammunition."));
                }

                if (!Battery->DeploymentComponent->IsDeployed())
                {
                    OutFailures.Add(TEXT("Artillery QA battery did not start deployed."));
                }
            }
        }

        if (Unit->Side == EStrategySide::Denmark &&
            Unit->Echelon == EStrategyEchelon::Supply)
        {
            ++DanishSupplyWagonCount;

            const AStrategySupplyWagonUnit* Wagon =
                Cast<AStrategySupplyWagonUnit>(Unit);

            if (!Wagon ||
                !Wagon->CargoComponent ||
                !Wagon->SupplyCaptureComponent ||
                !Wagon->SupplyComponent)
            {
                OutFailures.Add(
                    TEXT("Supply wagon QA entity is missing logistics-core components."));
            }
            else
            {
                if (!Wagon->SupplyComponent->bActsAsSupplySource ||
                    Wagon->CargoComponent->SmallArmsRounds <= 0 ||
                    Wagon->CargoComponent->ArtilleryRounds <= 0)
                {
                    OutFailures.Add(
                        TEXT("Supply wagon QA cargo/source state is invalid."));
                }

                if (!Wagon->CanMoveSupplyWagon())
                {
                    OutFailures.Add(
                        TEXT("Supply wagon QA mobility state is invalid."));
                }

                if (Wagon->CargoComponent->ArtilleryAmmunitionFamilyTag !=
                    FName(TEXT("FIELD_ARTILLERY_GENERIC")))
                {
                    OutFailures.Add(
                        TEXT("Supply wagon QA artillery compatibility tag is invalid."));
                }
            }
        }

        if (Unit->Side == EStrategySide::Denmark &&
            Unit->Echelon == EStrategyEchelon::Cavalry)
        {
            ++DanishCavalryCount;

            const ACavalryUnit* Cavalry = Cast<ACavalryUnit>(Unit);

            if (Cavalry && !Cavalry->ScreenAIComponent)
            {
                OutFailures.Add(
                    FString::Printf(
                        TEXT("%s cavalry screen AI component is missing."),
                        *Unit->StableUnitId.ToString()));
            }

            if (Cavalry &&
                Cavalry->DragoonComponent &&
                Cavalry->DragoonComponent->Role == EStrategyCavalryRole::Dragoon)
            {
                bFoundDragoon = true;
            }
        }
    }

    if (ValidCount != ExpectedCount)
    {
        OutFailures.Add(
            FString::Printf(
                TEXT("Expected %d strategy entities, found %d."),
                ExpectedCount,
                ValidCount));
    }

    if (EnemySelectableCount > 0)
    {
        OutFailures.Add(
            FString::Printf(
                TEXT("%d Prussian QA units are incorrectly player-controllable."),
                EnemySelectableCount));
    }

    if (bSpawnCavalryQA && DanishCavalryCount != 2)
    {
        OutFailures.Add(
            FString::Printf(
                TEXT("Expected 2 Danish cavalry units, found %d."),
                DanishCavalryCount));
    }

    if (bSpawnCavalryQA && !bFoundDragoon)
    {
        OutFailures.Add(TEXT("Dragoon QA unit was not configured."));
    }

    if (bSpawnArtilleryQA && DanishArtilleryCount != 1)
    {
        OutFailures.Add(
            FString::Printf(
                TEXT("Expected 1 Danish artillery battery, found %d."),
                DanishArtilleryCount));
    }

    if (bSpawnSupplyWagonQA && DanishSupplyWagonCount != 1)
    {
        OutFailures.Add(
            FString::Printf(
                TEXT("Expected 1 Danish supply wagon, found %d."),
                DanishSupplyWagonCount));
    }

    if (bSpawnRiverQA && !IsValid(SpawnedRiverBarrier))
    {
        OutFailures.Add(TEXT("River QA barrier was not spawned."));
    }

    if (bSpawnObstacleQA && !IsValid(SpawnedNavigationObstacle))
    {
        OutFailures.Add(TEXT("Navigation obstacle QA actor was not spawned."));
    }

    bool bFoundConfiguredSupplySource = false;

    for (const AStrategyUnit* Unit : SpawnedUnitObjects)
    {
        if (!IsValid(Unit) ||
            !Unit->SupplyComponent ||
            !Unit->SupplyComponent->bActsAsSupplySource)
        {
            continue;
        }

        if (const AStrategySupplyWagonUnit* Wagon =
            Cast<AStrategySupplyWagonUnit>(Unit))
        {
            bFoundConfiguredSupplySource =
                Wagon->CargoComponent &&
                Wagon->CargoComponent->GetTotalAmmunitionRounds() > 0;
        }
        else
        {
            bFoundConfiguredSupplySource =
                Unit->SupplyComponent->StoredAmmunitionRounds > 0;
        }

        if (bFoundConfiguredSupplySource)
        {
            break;
        }
    }

    if (!bFoundConfiguredSupplySource)
    {
        OutFailures.Add(TEXT("No configured tactical ammunition supply source was found."));
    }

    return OutFailures.Num() == 0;
}
