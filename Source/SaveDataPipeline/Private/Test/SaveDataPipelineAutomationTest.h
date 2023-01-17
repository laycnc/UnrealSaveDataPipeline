// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "SaveDataPipelineAutomationTest.savepipeline.h"
#include "SaveDataPipelineAutomationTest.generated.h"


UENUM()
enum struct ESaveDataPipelineAutomationTestFlag1 : uint8
{
    HogeHoge,
	Foo,
	Piyo,
};

USTRUCT(meta = (SaveDataPipeline))
struct FSaveDataPipelineAutomationTestOldVersion1
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()

public:
	UPROPERTY(EditAnywhere)
	ESaveDataPipelineAutomationTestFlag1 Flag;

	UPROPERTY(EditAnywhere)
	int32 Count;

	UPROPERTY(EditAnywhere)
	FString HogeHoge;
	
	UPROPERTY(EditAnywhere)
	float Time;
};


USTRUCT(meta = (SaveDataPipeline, BaseType = "SaveDataPipelineAutomationTestOldVersion1"))
struct FSaveDataPipelineAutomationTestOldVersion2
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()

public:
	UPROPERTY(EditAnywhere)
	ESaveDataPipelineAutomationTestFlag1 Flag;

	UPROPERTY(EditAnywhere)
	int32 Count;

	UPROPERTY(EditAnywhere)
	FString HogeHoge;
	
	UPROPERTY(EditAnywhere)
	float Time;
};
