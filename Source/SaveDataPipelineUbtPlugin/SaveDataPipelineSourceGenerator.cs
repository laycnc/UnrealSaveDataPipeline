// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using EpicGames.UHT.Types;
using System.IO;
using Microsoft.Extensions.Logging;

namespace SaveDataPipelineUbtPlugin
{
	internal class SaveDataPipelineSourceGenerator : SaveDataPipelineCodeGeneratorBase
	{
		public readonly IUhtExportFactory Factory;
		public UhtSession Session => Factory.Session;
		public UhtHeaderFile TargetHeader { get; set; }

		public SaveDataPipelineSourceGenerator(IUhtExportFactory factory, UhtHeaderFile targetHeader)
		{
			Factory = factory;
			TargetHeader = targetHeader;
		}

		public void Generate()
		{
			using BorrowStringBuilder borrower = new(StringBuilderCache.Big);

			borrower.StringBuilder.Append(HeaderCopyright);
			borrower.StringBuilder.Append(IncludeMemoryWriterHeader);
			borrower.StringBuilder.Append(IncludeMemoryReaderHeader);

			// インクルードのパス
			string IncludePath = Path.GetRelativePath(TargetHeader.Package.Module.IncludeBase, TargetHeader.FilePath).Replace('\\', '/');
			borrower.StringBuilder.Append($"#include \"{IncludePath}\"\r\n");

			borrower.StringBuilder.Append("\r\n\r\n");

			ExportStruct(borrower.StringBuilder, TargetHeader);

			string fileName = Path.Combine(TargetHeader.Package.Module.OutputDirectory, $"{TargetHeader.FileNameWithoutExtension}.savepipeline.gen.cpp");

			Factory.CommitOutput(fileName, borrower.StringBuilder);
		}

		protected override void ExportStruct(StringBuilder builder, UhtStruct structObj)
		{

			// Struct Header
			builder.Append("// ========================================\r\n");
			builder.Append($"// F{structObj.EngineName} \r\n");
			builder.Append("\r\n\r\n");

			if (structObj.MetaData.ContainsKey("BaseType"))
			{
				string BaseTypeName = structObj.MetaData.GetValueOrDefault("BaseType");
				UhtStruct? BaseTypeResult = Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.ScriptStruct, $"F{BaseTypeName}") as UhtStruct;

				if (BaseTypeResult != null)
				{
					ExportConvert(builder, structObj, BaseTypeResult);
				}
				else
				{
					Session.Logger?.Log( LogLevel.Error , $"Not Found F{BaseTypeName}");
				}
			}

			builder.Append($"void F{structObj.EngineName}::SavePipelineRead(FMemoryReader& MemoryReader)\r\n");
			builder.Append("{\r\n");
			foreach (UhtType Child in structObj.Children)
			{
				if (Child is UhtProperty Property)
				{
					// 	MemoryReader << FileTypeTag;
					builder.Append($"\tMemoryReader << {Property.EngineName};\r\n");
				}
			}
			builder.Append("}\r\n");
			builder.Append("\r\n");

			builder.Append($"void F{structObj.EngineName}::SavePipelineWrite(FMemoryWriter& MemoryWriter)\r\n");
			builder.Append("{\r\n");
			foreach (UhtType Child in structObj.Children)
			{
				if (Child is UhtProperty Property)
				{
					// 	MemoryReader << FileTypeTag;
					builder.Append($"\tMemoryWriter << {Property.EngineName};\r\n");
				}
			}
			builder.Append("}\r\n");
			builder.Append("\r\n");

		}

		public static void ExportConvert(StringBuilder builder, UhtStruct structObj, UhtStruct baseType)
		{
			builder.Append($"void F{structObj.EngineName}::SavePipelineConvert(const F{baseType.EngineName}& InPrevData)\r\n");
			builder.Append("{\r\n");

			foreach (UhtProperty Property in structObj.Children.Cast<UhtProperty>())
			{
				UhtProperty? Result = baseType.Children.Find(BaseChild => BaseChild.EngineName == Property.EngineName) as UhtProperty;

				if (Result != null)
				{
					builder.Append($"\t{Property.EngineName} = InPrevData.{Result.EngineName};\r\n");
				}
			}

			builder.Append("}\r\n");
			builder.Append("\r\n");
		}
	}
}
