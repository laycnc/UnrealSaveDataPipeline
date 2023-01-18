This "README.md" was created by DeepL translation

# This Plugin is under implementation.


# Overview

This Plugin performs code generation for UnrealEngine save data.  
UHT (UnrealHeaderTool) is used for code generation.  

In the save mechanism using USaveGame, serialization is performed in a reflection-based manner.  
The use of reflection results in performance degradation.  
Additional information such as meta information is also saved, which increases the file size.  
By generating the code in advance, the above disadvantages are eliminated.  

# For generated code

|Description|OutputConditions|OutputFunctions|
|:-|:-|:-|
|structure read function|Always|void SavePipelineRead(FMemoryReader& MemoryReader);|
|Structure write function|Always|void SavePipelineWrite(FMemoryWriter& MemoryWriter);|
|Structure conversion function between old version and new version|Structure conversion function between old version and new version|BaseType must be put in the meta-information of USTRUCT.|void SavePipelineConvert(const FSaveDataInfoV2& InPrevData);|

## Input Example

> Sample.cpp
```cpp
// Older version of the structure
USTRUCT(meta = (SaveDataPipeline))
struct FSaveDataSampleStructOldVersion
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()

public:
	UPROPERTY(EditAnywhere)
	int32 DeadCount;

	UPROPERTY(EditAnywhere)
	FString HogeHoge;
}

// new version structure
USTRUCT(meta = (SaveDataPipeline, BaseType = "SaveDataSampleStructOldVersion"))
struct FSaveDataSampleStruct
{
	GENERATED_BODY()

    GENERATED_SAVE_PIPELINE_BODY()

public:
	UPROPERTY(EditAnywhere)
	int32 DeadCount;

	UPROPERTY(EditAnywhere)
	FString HogeHoge;
}
```

## Output code.

`XXX.savepipeline.h` works in the same way as the UE standard GENERATED_BODY macro.  
By writing the dedicated macro `GENERATED_SAVE_PIPELINE_BODY`, the generated code functions can be included in the members.

> Sample.savepipeline.h

```cpp
#define FID_XXXXXX_Source_Example_Public_Sample_h__25_SAVE_PIPELINE_HASH \
public: \
static int32 GetSavePipelineHash()\
{\
	constexpr int32 Hash = -323057014;\
	return Hash;\
}\

#define FID_XXXXXX_Source_Example_Public_Sample_h_25_SAVE_PIPELINE_SERIALIZE \
public: \
void SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash = nullptr);\
void SavePipelineWrite(FMemoryWriter& MemoryWriter);

#define FID_XXXXXX_Source_Example_Public_Sample_h__25_SAVE_PIPELINE_CONVERT \
public: \
void SavePipelineConvert(const FSaveDataSampleStructOldVersion& InPrevData);

#define FID_XXXXXX_Source_Example_Public_Sample_h__25_GENERATED_SAVE_PIPELINE_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_XXXXXX_Source_Example_Public_Sample_h__25_SAVE_PIPELINE_HASH \
	FID_XXXXXX_Source_Example_Public_Sample_h__25_SAVE_PIPELINE_SERIALIZE \
	FID_XXXXXX_Source_Example_Public_Sample_h__25_SAVE_PIPELINE_CONVERT \
PRAGMA_DISABLE_DEPRECATION_WARNINGS 
```

The output cpp code is serialized and converted by reflection.  

> * Currently, only assignment is supported for conversion, but eventually enumeration conversion will be supported.

> Sample.savepipeline.gen.cpp
```cpp

// Function to convert structures between old and new versions.
void FSaveDataSampleStruct::SavePipelineConvert(const FSaveDataSampleStructOldVersion& InPrevData)
{
	switch(InPrevData.Flag)
	{
		case ESaveDataFlag::HogeHoge:
			Flag = ESaveDataFlagOldVersion::HogeHoge;
			break;
		case ESaveDataFlag::Foo:
			Flag = ESaveDataFlagOldVersion::Foo;
			break;
		case ESaveDataFlag::Piyo:
			Flag = ESaveDataFlagOldVersion::Piyo;
			break;
		default:
			break;
	}

	Count = InPrevData.Count;
	HogeHoge = InPrevData.HogeHoge;
	Time = InPrevData.Time;
}

// memory loading process
void FSaveDataSampleStruct::SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash)
{
	int32 Hash = 0;
	if( InReadHash == nullptr )
	{
		MemoryReader << Hash;
	}
	if( Hash != FSaveDataSampleStruct::GetSavePipelineHash() )
	{
		FSaveDataSampleStructOldVersion OldVersion;
		OldVersion.SavePipelineRead( MemoryReader, &Hash );
		SavePipelineConvert(OldVersion);
		return;
	}
	MemoryReader << Flag;
	MemoryReader << Count;
	MemoryReader << HogeHoge;
	MemoryReader << Time;
}

// Memory write process
void FSaveDataSampleStruct::SavePipelineWrite(FMemoryWriter& MemoryWriter)
{
	int32 Hash = FSaveDataSampleStruct::GetSavePipelineHash();
	MemoryWriter << Hash;
	MemoryWriter << Flag;
	MemoryWriter << Count;
	MemoryWriter << HogeHoge;
	MemoryWriter << Time;
}

```

# Save process

> Sample load process

```cpp

bool LoadExample( const FSaveDataSampleStruct& OutSaveData, const FString& SlotName, const int32 UserIndex )
{
	TArray<uint8> SaveDataBinary;
	if( UGameplayStatics::LoadDataFromSlot(SaveDataBinary, SlotName, UserIndex) )
	{
		FSaveDataSampleStruct SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		SaveData.SavePipelineRead(MemoryReader);
		OutSaveData = SaveData;
		return true;
	}
}

```

> Sample save process

```cpp


bool SaveExample( const FSaveDataSampleStruct& SaveData, const FString& SlotName, const int32 UserIndex )
{
	TArray<uint8> SaveDataBinary;
	FMemoryWriter MemoryWriter(SaveDataBinary, true);
	SaveData.SavePipelineWrite( MemoryWriter );
	if( UGameplayStatics::SaveDataToSlot(SaveDataBinary, SlotName, UserIndex) )
	{
		return true;
	}
}

```