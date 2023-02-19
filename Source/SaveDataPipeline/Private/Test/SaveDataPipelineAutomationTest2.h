// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "SaveDataPipelineAutomationTest2.savepipeline.h"
#include "SaveDataPipelineAutomationTest2.generated.h"


USTRUCT()
struct FPiyoPiyo2
{
	GENERATED_BODY()
public:
	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Poool;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Piyo;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Count;
};

USTRUCT(meta = (SaveDataPipeline))
struct FHogeHogeHoge
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()
public:
	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Poool;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	FPiyoPiyo2 Piyo;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Count;
};

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
	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	ESaveDataPipelineAutomationTestFlag2 Flag;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	int32 Count;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test", meta = ( SaveDataPipelineFixedSize = 32 ))
	FString HogeHoge;
	
	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	FString HogeFoo;
	
	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	float Time;

	UPROPERTY(EditAnywhere, Category = "SaveDataPipeline.Test")
	FHogeHogeHoge StructValue;
};
