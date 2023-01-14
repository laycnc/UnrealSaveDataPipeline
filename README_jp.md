# このPluginは実装中です。


# 概要

このPluginはUnrealEngineのセーブデータに対するコード生成を行います。  
コード生成には、UHT(UnrealHeaderTool)を使用します。  


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

#define FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_SERIALIZE \
public: \
void SavePipelineRead(FMemoryReader& MemoryReader);\
void SavePipelineWrite(FMemoryWriter& MemoryWriter);


#define FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_CONVERT \
public: \
void SavePipelineConvert(const FSaveDataInfoV2& InPrevData);


#define FID_XXXXXX_Source_Example_Public_Sample_h_36_GENERATED_SAVE_PIPELINE_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_SERIALIZE \
	FID_XXXXXX_Source_Example_Public_Sample_h_36_SAVE_PIPELINE_CONVERT \
PRAGMA_DISABLE_DEPRECATION_WARNINGS 

```

出力されたcppコードに関しては、リフレクションによってシリアライズやコンバート処理が行われます。  

> ※ 現在では、コンバート処理は代入のみですが、最終的には、列挙型のコンバートなどをサポート予定

> Sample.savepipeline.gen.cpp
```cpp

// 旧バージョンと新バージョンの構造体を変換する関数です。
void FSaveDataSampleStruct::SavePipelineConvert(const FSaveDataSampleStructOldVersion& InPrevData)
{
	DeadCount = InPrevData.DeadCount;
	HogeHoge = InPrevData.HogeHoge;
}

// メモリの読み込み処理
void FSaveDataSampleStruct::SavePipelineRead(FMemoryReader& MemoryReader)
{
	MemoryReader << DeadCount;
	MemoryReader << HogeHoge;
}

// メモリの書き込み処理
void FSaveDataSampleStruct::SavePipelineWrite(FMemoryWriter& MemoryWriter)
{
	MemoryWriter << DeadCount;
	MemoryWriter << HogeHoge;
}
```