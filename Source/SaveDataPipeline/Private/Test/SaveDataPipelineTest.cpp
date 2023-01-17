// Copyright Epic Games, Inc. All Rights Reserved.

#include "Misc/AutomationTest.h"
#include "Kismet/GameplayStatics.h"
#include "Serialization/MemoryWriter.h"
#include "Serialization/MemoryReader.h"
#include "Test/SaveDataPipelineAutomationTest.h"
#include "Test/SaveDataPipelineAutomationTest2.h"

/**
* This Login and Logout functional test is intended to test that a user can
* login and logout from the service.
* Use -OSSIDTESTUSER='TestUser' and -OSSIDTESTPSSWD='YouPassword'
*/
IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSaveDataPipelineTest,
                                  "SaveDataPipeline",
                                 EAutomationTestFlags::EditorContext |
                                     EAutomationTestFlags::EngineFilter)


FSaveDataPipelineAutomationTestVersionLatest GetSaveData()
{
	FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
	SaveVersion1.Flag     = ESaveDataPipelineAutomationTestFlag2::HogeHoge;
	SaveVersion1.Count    = 12345;
	SaveVersion1.HogeHoge = TEXT("テストアプリケーション");
	SaveVersion1.Time     = 3.1415f;

	return SaveVersion1;
}

bool FSaveDataPipelineTest::RunTest( const FString& Parameters )
{
	TArray<uint8> SaveDataBinary;

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = GetSaveData();
		FMemoryWriter                           MemoryWriter(SaveDataBinary, true);
		SaveVersion1.SavePipelineWrite( MemoryWriter );
	}

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
		FMemoryReader                           MemoryReader(SaveDataBinary, true);
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