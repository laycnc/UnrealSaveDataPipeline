// Copyright Epic Games, Inc. All Rights Reserved.

#include "Misc/AutomationTest.h"
#include "Kismet/GameplayStatics.h"
#include "Serialization/MemoryWriter.h"
#include "Serialization/MemoryReader.h"
#include "Test/SaveDataPipelineAutomationTest.h"
#include "Test/SaveDataPipelineAutomationTest2.h"

namespace
{
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

	bool IsEqualTest(const FSaveDataPipelineAutomationTestVersionLatest& Lhs,
	                 const FSaveDataPipelineAutomationTestVersionLatest& Rhs)
	{
		const UStruct* Struct =
		    FSaveDataPipelineAutomationTestVersionLatest::StaticStruct();
		for ( const FProperty* Property : TFieldRange<FProperty>(Struct) )
		{
			const uint8* LhsPtr = Property->ContainerPtrToValuePtr<uint8>(&Lhs);
			const uint8* RhsPtr = Property->ContainerPtrToValuePtr<uint8>(&Rhs);
			if ( !Property->Identical(LhsPtr, RhsPtr) )
			{
				return false;
			}
		}
		return true;
	}

	void CopyProperty(UStruct*    DestStruct,
	                  UStruct*    SrcStruct,
	                  void*       Dest,
	                  const void* Src)
	{
		for ( const FProperty* DestProperty : TFieldRange<FProperty>(DestStruct) )
		{
			const FProperty* SrcProperty =
			    FindFProperty<FProperty>(SrcStruct, DestProperty->GetFName());
			if ( SrcProperty == nullptr )
			{
				continue;
			}

			if ( SrcProperty->GetClass() != DestProperty->GetClass() )
			{
				continue;
			}
			const uint8* SrcPtr  = SrcProperty->ContainerPtrToValuePtr<uint8>(Src);
			uint8*       DestPtr = DestProperty->ContainerPtrToValuePtr<uint8>(Dest);
			DestProperty->CopyCompleteValue(DestPtr, SrcPtr);
		}
	}

	template<class LhsT, class RhsT>
	void CopyProperty(LhsT& Dest, const RhsT& Src)
	{
		CopyProperty(LhsT::StaticStruct(), RhsT::StaticStruct(), &Lhs, &Rhs);
	}

} // namespace

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
		const bool    Result = SaveVersion1.SavePipelineWrite(MemoryWriter);
		TestTrue(TEXT("SavePipelineWrite"), Result);
	}

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		const bool    Result = SaveVersion1.SavePipelineRead(MemoryReader);
		TestTrue(TEXT("SavePipelineRead"), Result);

		FSaveDataPipelineAutomationTestVersionLatest ComparisonValue = GetSaveData();

		TestTrue(TEXT("SavePipelineRead ComparisonCheck"),
		         IsEqualTest(SaveVersion1, ComparisonValue));
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
		FSaveDataPipelineAutomationTestOldVersion1   OldVersion   = {};
		OldVersion.Flag     = OldConvertNew(SaveVersion1.Flag);
		OldVersion.Count    = SaveVersion1.Count;
		OldVersion.HogeHoge = SaveVersion1.HogeHoge;
		OldVersion.Time     = SaveVersion1.Time;

		FMemoryWriter MemoryWriter(SaveDataBinary, true);
		const bool    Result = SaveVersion1.SavePipelineWrite(MemoryWriter);
		TestTrue(TEXT("SavePipelineWrite"), Result);
	}

	{
		FSaveDataPipelineAutomationTestVersionLatest SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		const bool    Result = SaveVersion1.SavePipelineRead(MemoryReader);
		TestTrue(TEXT("SavePipelineRead"), Result);

		FSaveDataPipelineAutomationTestVersionLatest ComparisonValue = GetSaveData();

		TestTrue(TEXT("SavePipelineRead ComparisonCheck"),
		         SaveVersion1.Flag == ComparisonValue.Flag &&
		             SaveVersion1.Count == ComparisonValue.Count &&
		             SaveVersion1.HogeHoge == ComparisonValue.HogeHoge &&
		             SaveVersion1.Time == ComparisonValue.Time);
	}

	return true;
}