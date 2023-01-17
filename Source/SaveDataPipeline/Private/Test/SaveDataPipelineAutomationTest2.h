// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "SaveDataPipelineAutomationTest2.savepipeline.h"
#include "SaveDataPipelineAutomationTest2.generated.h"


UENUM()
enum struct ESaveDataPipelineAutomationTestFlag2 : uint8
{
    HogeHoge,
	Foo,
	Piyo,
	Pool,
};

USTRUCT(meta = (SaveDataPipeline, BaseType = "SaveDataPipelineAutomationTestOldVersion2"))
struct FSaveDataPipelineAutomationTestVersionLatest
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()

public:
	UPROPERTY(EditAnywhere)
	ESaveDataPipelineAutomationTestFlag2 Flag;

	UPROPERTY(EditAnywhere)
	int32 Count;

	UPROPERTY(EditAnywhere)
	FString HogeHoge;
	
	UPROPERTY(EditAnywhere)
	float Time;
};
