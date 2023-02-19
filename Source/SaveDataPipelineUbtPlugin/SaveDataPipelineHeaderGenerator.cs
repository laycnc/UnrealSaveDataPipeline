// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicGames.UHT.Tables;
using EpicGames.Core;
using EpicGames.UHT.Utils;
using EpicGames.UHT.Types;
using System.IO;

namespace SaveDataPipelineUbtPlugin
{
	class SaveDataPipelineHeaderGenerator : SaveDataPipelineCodeGeneratorBase
	{

		public string FileId { get; }

		public SaveDataPipelineHeaderGenerator(IUhtExportFactory factory, UhtHeaderFile targetHeader, List<UhtStruct> targetStructs)
						: base(factory, targetHeader, targetStructs)
		{
			FileId =  GetFileId();
		}

		public void Generate()
		{
			using BorrowStringBuilder borrower = new(StringBuilderCache.Big);

			borrower.StringBuilder.Append(HeaderCopyright);
			borrower.StringBuilder.Append(SaveDataPipelineMacrosHeader);

			borrower.StringBuilder.Append("class FMemoryReader;\r\n");
			borrower.StringBuilder.Append("class FMemoryWriter;\r\n");

			foreach( UhtStruct dependStruct in GetStructOfOtherHeaderFileOnDepends() )
			{
				borrower.StringBuilder.Append($"struct F{dependStruct.EngineName};\r\n");
			}

			borrower.StringBuilder.Append("\r\n\r\n");

			ExportStruct(borrower.StringBuilder, TargetHeader);

			if( SaveExportedHeaders )
			{
				string fileName = Path.Combine(TargetHeader.Package.Module.OutputDirectory, $"{TargetHeader.FileNameWithoutExtension}.savepipeline.h");
				Factory.CommitOutput(fileName, borrower.StringBuilder);
			}
		}

		protected override void ExportStruct(StringBuilder builder, UhtStruct structObj)
		{
			int LineNumber = GetGeneratedBodyLineNumber(structObj);

			// Struct Header
			builder.Append("// ========================================\r\n");
			builder.Append($"// {structObj.EngineName} \r\n");
			builder.Append("\r\n\r\n");

			// SaveData Hash
			string SaveDataPipelineHash = $"{FileId}_{LineNumber}_SAVE_PIPELINE_HASH";
			builder.Append($"#define {SaveDataPipelineHash} \\\r\n");
			builder.Append("public: \\\r\n");
			builder.Append("static int32 GetSavePipelineHash()\\\r\n");
			builder.Append("{\\\r\n");
			builder.Append($"\tconstexpr int32 Hash = {GetStructNameHash(structObj).GetHashCode()};\\\r\n");
			builder.Append($"\treturn Hash;\\\r\n");
			builder.Append("}\\\r\n");
			builder.Append("\r\n\r\n");

			// SaveDataSerialize
			string SaveDataPipelineSerialize = $"{FileId}_{LineNumber}_SAVE_PIPELINE_SERIALIZE";
			builder.Append($"#define {SaveDataPipelineSerialize} \\\r\n");
			builder.Append("public: \\\r\n");
			builder.Append("bool SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash = nullptr);\\\r\n");
			builder.Append("bool SavePipelineWrite(FMemoryWriter& MemoryWriter);\r\n");
			builder.Append("\r\n\r\n");

			// SaveDataPipelineConvert
			string SaveDataPipelineConvert = $"{FileId}_{LineNumber}_SAVE_PIPELINE_CONVERT";
			builder.Append($"#define {SaveDataPipelineConvert}");
			if (structObj.MetaData.ContainsKey("BaseType"))
			{
				builder.Append(" \\\r\n"); // #define after
				string BaseType = structObj.MetaData.GetValueOrDefault("BaseType");
				builder.Append("public: \\\r\n");
				builder.Append($"bool SavePipelineConvert(const F{BaseType}& InPrevData);\r\n");
			}
			builder.Append("\r\n\r\n");

			// Generated Body Phase
			builder.Append($"#define {FileId}_{LineNumber}_GENERATED_SAVE_PIPELINE_BODY \\\r\n");
			builder.Append("PRAGMA_DISABLE_DEPRECATION_WARNINGS \\\r\n");
			builder.Append("public: \\\r\n");
			builder.Append($"\t{SaveDataPipelineHash} \\\r\n");
			builder.Append($"\t{SaveDataPipelineSerialize} \\\r\n");
			builder.Append($"\t{SaveDataPipelineConvert} \\\r\n");
			builder.Append("PRAGMA_DISABLE_DEPRECATION_WARNINGS \r\n");
			builder.Append("\r\n\r\n");
		}

		private string GetFileId()
		{
			string filePath = TargetHeader.FilePath;
			bool isRelative = !Path.IsPathRooted(filePath);
			if (!isRelative && Session.EngineDirectory != null)
			{
				string? directory = Path.GetDirectoryName(Session.EngineDirectory);
				if (!String.IsNullOrEmpty(directory))
				{
					filePath = Path.GetRelativePath(directory, filePath);
					isRelative = !Path.IsPathRooted(filePath);
				}
			}
			if (!isRelative && Session.ProjectDirectory != null)
			{
				string? directory = Path.GetDirectoryName(Session.ProjectDirectory);
				if (!String.IsNullOrEmpty(directory))
				{
					filePath = Path.GetRelativePath(directory, filePath);
					isRelative = !Path.IsPathRooted(filePath);
				}
			}
			filePath = filePath.Replace('\\', '/');
			if (isRelative)
			{
				while (filePath.StartsWith("../", StringComparison.Ordinal))
				{
					filePath = filePath[3..];
				}
			}

			filePath = filePath.Replace('/', '_');
			filePath = filePath.Replace('.', '_');

			return $"FID_{filePath}";
		}

		private static IoHash GetStructNameHash(UhtStruct structObj)
		{
			byte[] Data = Encoding.ASCII.GetBytes(structObj.EngineName);
			return IoHash.Compute(Data);
		}

		/// <summary>
		/// マクロの行数を取得する
		/// 
		/// todo:本当はUhtKeywordアトリビュートでキャッシュしたものを使う
		/// </summary>
		/// <param name="structObj"></param>
		/// <returns></returns>
		private int GetGeneratedBodyLineNumber(UhtStruct structObj)
		{
			string HeaderFile = TargetHeader.Data.Span.ToString();

			// 対象の構造体のインデックスを取得
			int TargetStructIndex = HeaderFile.IndexOf($"struct F{structObj.EngineName}\r\n");
			if (TargetStructIndex == -1)
			{
				// 失敗
				return 0;
			}
			// 構造体以降の文字列
			string StructAfter = HeaderFile.Substring(TargetStructIndex);
			// 構造体以降に見つかった最初のGENERATED_BODYのインデックス
			int GeneratedBodyIndex = StructAfter.IndexOf("GENERATED_SAVE_PIPELINE_BODY()");

			if(GeneratedBodyIndex == -1)
			{
				return 0;
			}

			// 最初から構造体のGENERATED_BODYのインデックス
			GeneratedBodyIndex += TargetStructIndex;

			// 改行の数を数えて行数を求める
			int GeneratedBodyLine = HeaderFile.Substring(0, GeneratedBodyIndex).Split("\r\n").Length;
			return GeneratedBodyLine;
		}

	}
}
