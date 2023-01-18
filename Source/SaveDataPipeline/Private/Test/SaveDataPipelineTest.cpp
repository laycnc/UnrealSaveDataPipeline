// Copyright Epic Games, Inc. All Rights Reserved.

#include "Misc/AutomationTest.h"
#include "Kismet/GameplayStatics.h"
#include "Serialization/MemoryWriter.h"
#include "Serialization/MemoryReader.h"
#include "Test/SaveDataPipelineAutomationTest.h"
#include "Test/SaveDataPipelineAutomationTest2.h"

ESaveDataPipelineAutomationTestFlag1 OldConvertNew(
    ESaveDataPipelineAutomationTestFlag2 NewVersion)
{
	switch ( NewVersion )
	{
		case ESaveDataPipelineAutomationTestFlag2::HogeHoge:
			return ESaveDataPipelineAutomationTestFlag1::HogeHoge;
		case ESaveDataPipelineAutomationTestFlag2::Foo:
			return ESaveDataPipelineAutomationTestFlag1::Foo;
		case ESaveDataPipelineAutomationTestFlag2::Piyo:
			return ESaveDataPipelineAutomationTestFlag1::Piyo;
			case ESaveDataPipelineAutomationTestFlag2::Pool:
			break;
		default:
			break;
	}
	return ESaveDataPipelineAutomationTestFlag1::HogeHoge;
}

FSaveDataPipelineAutomationTestVersionLatest GetSaveData()
{
	FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
	SaveVersion1.Flag     = ESaveDataPipelineAutomationTestFlag2::HogeHoge;
	SaveVersion1.Count    = 12345;
	SaveVersion1.HogeHoge = TEXT("テストアプリケーション");
	SaveVersion1.Time     = 3.1415f;

	return SaveVersion1;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSaveDataPipelineTest,
                                 "SaveDataPipeline.Simple Load",
                                 EAutomationTestFlags::EditorContext |
                                     EAutomationTestFlags::EngineFilter)

bool FSaveDataPipelineTest::RunTest(const FString& Parameters)
{
	TArray<uint8> SaveDataBinary;

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = GetSaveData();
		FMemoryWriter MemoryWriter(SaveDataBinary, true);
		SaveVersion1.SavePipelineWrite(MemoryWriter);
	}

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		SaveVersion1.SavePipelineRead(MemoryReader);
		FSaveDataPipelineAutomationTestVersionLatest ComparisonValue = GetSaveData();

		TestTrue(TEXT("SavePipelineRead"),
		         SaveVersion1.Flag == ComparisonValue.Flag &&
		             SaveVersion1.Count == ComparisonValue.Count &&
		             SaveVersion1.HogeHoge == ComparisonValue.HogeHoge &&
		             SaveVersion1.Time == ComparisonValue.Time);
	}

	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSaveDataPipelineConvertLoadTest,
                                 "SaveDataPipeline.Convert Load",
                                 EAutomationTestFlags::EditorContext |
                                     EAutomationTestFlags::EngineFilter)

bool FSaveDataPipelineConvertLoadTest::RunTest(const FString& Parameters)
{
	TArray<uint8> SaveDataBinary;

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = GetSaveData();
		FSaveDataPipelineAutomationTestOldVersion1   OldVersion;
		OldVersion.Flag     = OldConvertNew(SaveVersion1.Flag);
		OldVersion.Count = SaveVersion1.Count;
		OldVersion.HogeHoge = SaveVersion1.HogeHoge;
		OldVersion.Time     = SaveVersion1.Time;

		FMemoryWriter MemoryWriter(SaveDataBinary, true);
		SaveVersion1.SavePipelineWrite(MemoryWriter);
	}

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		SaveVersion1.SavePipelineRead(MemoryReader);
		FSaveDataPipelineAutomationTestVersionLatest ComparisonValue = GetSaveData();

		TestTrue(TEXT("SavePipelineRead"),
		         SaveVersion1.Flag == ComparisonValue.Flag &&
		             SaveVersion1.Count == ComparisonValue.Count &&
		             SaveVersion1.HogeHoge == ComparisonValue.HogeHoge &&
		             SaveVersion1.Time == ComparisonValue.Time);
	}

	return true;
}