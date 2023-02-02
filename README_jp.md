# このPluginは実装中です。


# 概要

このPluginはUnrealEngineのセーブデータに対するコード生成を行います。  
コード生成には、UHT(UnrealHeaderTool)を使用します。  

USaveGameを使ったセーブ機構では、リフレクションを使った方法でシリアライズが行われます。  
リフレクションを使う関係上パフォーマンスが低下する。  
メタ情報などの追加情報も保存されてしまうので、ファイルサイズが増えてしまう。  
事前にコード生成を行う事で、上記のデメリットを解消をするのが狙いです。  

# 生成コードに関して

|説明|出力条件|出力関数|
|:-|:-|:-|
|構造体の読み込み関数|常時|void SavePipelineRead(FMemoryReader& MemoryReader);|
|構造体の書き込み関数|常時|void SavePipelineWrite(FMemoryWriter& MemoryWriter);|
|旧バージョンと新バージョンの構造体変換関数|USTRUCTのメタ情報にBaseTypeを入れる必要があります。|void SavePipelineConvert(const FSaveDataInfoV2& InPrevData);|


## 入力例

> Sample.cpp
```cpp

// 旧バージョンの構造体
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
};


// 新バージョンの構造体
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
};
```

## 出力されたコード

`XXX.savepipeline.h`はUE標準のGENERATED_BODYマクロと同じ仕組みで動いています。  
専用のマクロ`GENERATED_SAVE_PIPELINE_BODY`を記述する事で生成コードの関数をメンバに組み込む事が出来ます。

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

出力されたcppコードに関しては、リフレクションによってシリアライズやコンバート処理が行われます。  

> ※ 現在では、コンバート処理は代入のみですが、最終的には、列挙型のコンバートなどをサポート予定

> Sample.savepipeline.gen.cpp
```cpp

// 旧バージョンと新バージョンの構造体を変換する関数です。
bool FSaveDataSampleStruct::SavePipelineConvert(const FSaveDataSampleStructOldVersion& InPrevData)
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
	// Success!
	return true;
}

// メモリの読み込み処理
bool FSaveDataSampleStruct::SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash)
{
	int32 Hash = 0;
	if( InReadHash == nullptr )
	{
		MemoryReader << Hash;
	}
	if( Hash != FSaveDataSampleStruct::GetSavePipelineHash() )
	{
		FSaveDataSampleStructOldVersion OldVersion;
		if( OldVersion.SavePipelineRead( MemoryReader, &Hash ) )
		{
			return SavePipelineConvert(OldVersion);
		}
		return false;
	}
	MemoryReader << Flag;
	MemoryReader << Count;
	MemoryReader << HogeHoge;
	MemoryReader << Time;
	// Success!
	return true;
}

// メモリの書き込み処理
bool FSaveDataSampleStruct::SavePipelineWrite(FMemoryWriter& MemoryWriter)
{
	int32 Hash = FSaveDataSampleStruct::GetSavePipelineHash();
	MemoryWriter << Hash;
	MemoryWriter << Flag;
	MemoryWriter << Count;
	MemoryWriter << HogeHoge;
	MemoryWriter << Time;
	// Success!
	return true;
}

```

# セーブ処理


> ロード処理のサンプル

```cpp

bool LoadExample( const FSaveDataSampleStruct& OutSaveData, const FString& SlotName, const int32 UserIndex )
{
	TArray<uint8> SaveDataBinary;
	if( UGameplayStatics::LoadDataFromSlot(SaveDataBinary, SlotName, UserIndex) )
	{
		FSaveDataSampleStruct SaveVersion1 = {};
		FMemoryReader MemoryReader(SaveDataBinary, true);
		if( SaveData.SavePipelineRead(MemoryReader) )
		{
			OutSaveData = SaveData;
			return true;
		}
	}
	return false;
}

```

> セーブ処理のサンプル

```cpp


bool SaveExample( const FSaveDataSampleStruct& SaveData, const FString& SlotName, const int32 UserIndex )
{
	TArray<uint8> SaveDataBinary;
	FMemoryWriter MemoryWriter(SaveDataBinary, true);
	if( SaveData.SavePipelineWrite( MemoryWriter ) )
	{
		if( UGameplayStatics::SaveDataToSlot(SaveDataBinary, SlotName, UserIndex) )
		{
			return true;
		}
	}
	return false;
}

```