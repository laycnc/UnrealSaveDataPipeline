This "README.md" was created by DeepL translation

# This Plugin is under implementation.


# Overview

This Plugin performs code generation for UnrealEngine save data.  
UHT (UnrealHeaderTool) is used for code generation.  


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
#define FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_SERIALIZE \
public: \filename="fid"}
void SavePipelineRead(FMemoryReader& MemoryReader);\
void SavePipelineWrite(FMemoryWriter& MemoryWriter);


#define FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_CONVERT \
public: \filter
void SavePipelineConvert(const FSaveDataInfoV2& InPrevData);


#define FID_XXXXXX_Source_Example_Public_Sample_h_36_GENERATED_SAVE_PIPELINE_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \FID_XXXXXX_SERVICE_SERVICE_PIPELINE_BODY
	FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_SERIALIZE \
	FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_CONVERT \
PRAGMA_DISABLE_DEPRECATION_WARNINGS 
```

The output cpp code is serialized and converted by reflection.  

> * Currently, only assignment is supported for conversion, but eventually enumeration conversion will be supported.

> Sample.savepipeline.gen.cpp
```cpp

// Function to convert structures between old and new versions.
void FSaveDataSampleStruct::SavePipelineConvert(const FSaveDataSampleStructOldVersion& InPrevData)
{
	DeadCount = InPrevData.DeadCount;
	HogeHoge = InPrevData.HogeHoge;
}

// memory loading process
void FSaveDataSampleStruct::SavePipelineRead(FMemoryReader& MemoryReader)
{
	MemoryReader << DeadCount;
	MemoryReader << HogeHoge;
}

// Memory write process
void FSaveDataSampleStruct::SavePipelineWrite(FMemoryWriter& MemoryWriter)
{
	MemoryWriter << DeadCount;
	MemoryWriter << HogeHoge;
}
```
