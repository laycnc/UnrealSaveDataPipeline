// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using EpicGames.UHT.Types;
using Microsoft.Extensions.Logging;

namespace SaveDataPipelineUbtPlugin
{
	internal class SaveDataPipelineSourceGenerator : SaveDataPipelineCodeGeneratorBase
	{
		public static string StructHashRead =
	"\tint32 Hash = 0;\r\n" +
	"\tif( InReadHash == nullptr )\r\n" +
	"\t{\r\n" +
	"\t\tMemoryReader << Hash;\r\n" +
	"\t}\r\n";

		public SaveDataPipelineSourceGenerator(IUhtExportFactory factory, UhtHeaderFile targetHeader, List<UhtStruct> targetStructs)
			: base(factory, targetHeader, targetStructs) 
		{
		}

		public void Generate()
		{
			using BorrowStringBuilder borrower = new(StringBuilderCache.Big);

			borrower.StringBuilder.Append(HeaderCopyright);
			borrower.StringBuilder.Append(IncludeMemoryWriterHeader);
			borrower.StringBuilder.Append(IncludeMemoryReaderHeader);

			List<UhtHeaderFile> DependHeaderLists = new List<UhtHeaderFile>();
			foreach (UhtStruct dependStruct in GetStructOfOtherHeaderFileOnDepends())
			{
				DependHeaderLists.Add(dependStruct.HeaderFile);
				foreach (UhtType propertyType in GetStructPropertyTypes(dependStruct))
				{
					DependHeaderLists.Add(dependStruct.HeaderFile);
				}
			}
			foreach(UhtHeaderFile headerFile in DependHeaderLists.Distinct())
			{
				if (headerFile != TargetHeader)
				{
					borrower.StringBuilder.Append($"#include \"{GetRelativeHeaderFilePath(headerFile)}\"\r\n");
				}
			}

			// インクルードのパス
			borrower.StringBuilder.Append($"#include \"{GetRelativeHeaderFilePath(TargetHeader)}\"\r\n");

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

			UhtStruct? BaseTypeResult = null;
			if (structObj.MetaData.ContainsKey("BaseType"))
			{
				string BaseTypeName = structObj.MetaData.GetValueOrDefault("BaseType");
				BaseTypeResult = Session.FindType(null, UhtFindOptions.SourceName | UhtFindOptions.ScriptStruct, $"F{BaseTypeName}") as UhtStruct;

				if (BaseTypeResult != null)
				{
					ExportConvert(builder, structObj, BaseTypeResult);
				}
				else
				{
					Session.Logger?.Log( LogLevel.Error , $"Not Found F{BaseTypeName}");
				}
			}

			builder.Append($"bool F{structObj.EngineName}::SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash)\r\n");
			builder.Append("{\r\n");
			builder.Append(StructHashRead);

			builder.Append($"\tif( Hash != F{structObj.EngineName}::GetSavePipelineHash() )\r\n");
			builder.Append("\t{\r\n");
			if(BaseTypeResult != null)
			{
				builder.Append($"\t\tF{BaseTypeResult.EngineName} OldVersion;\r\n");
				builder.Append($"\t\tif( OldVersion.SavePipelineRead( MemoryReader, &Hash ) )\r\n");
				builder.Append("\t\t{\r\n");
				builder.Append($"\t\t\treturn SavePipelineConvert(OldVersion);\r\n");
				builder.Append("\t\t}\r\n");
				builder.Append("\t\treturn false;\r\n");
			}
			else
			{
				builder.Append("\t\t// Read Save Data Version Not Found\r\n");
				builder.Append("\t\treturn false;\r\n");
			}
			builder.Append("\t}\r\n");

			foreach (UhtProperty Property in structObj.Children.OfType<UhtProperty>())
			{
				// 	MemoryReader << FileTypeTag;
				builder.Append($"\tMemoryReader << {Property.EngineName};\r\n");
			}
			builder.Append("\t// Success!\r\n");
			builder.Append("\treturn true;\r\n");
			builder.Append("}\r\n");
			builder.Append("\r\n");

			builder.Append($"bool F{structObj.EngineName}::SavePipelineWrite(FMemoryWriter& MemoryWriter)\r\n");
			builder.Append("{\r\n");
			builder.Append($"\tint32 Hash = F{structObj.EngineName}::GetSavePipelineHash();\r\n");
			builder.Append("\tMemoryWriter << Hash;\r\n");
			foreach (UhtProperty Property in structObj.Children.OfType<UhtProperty>())
			{
				// 	MemoryReader << FileTypeTag;
				builder.Append($"\tMemoryWriter << {Property.EngineName};\r\n");
			}
			builder.Append("\t// Success!\r\n");
			builder.Append("\treturn true;\r\n");
			builder.Append("}\r\n");
			builder.Append("\r\n");

		}

		public static void ExportConvert(StringBuilder builder, UhtStruct structObj, UhtStruct baseType)
		{
			builder.Append($"bool F{structObj.EngineName}::SavePipelineConvert(const F{baseType.EngineName}& InPrevData)\r\n");
			builder.Append("{\r\n");

			foreach (UhtProperty Property in structObj.Children.OfType<UhtProperty>())
			{
				UhtProperty? OldProperty = baseType.Children.Find(BaseChild => BaseChild.EngineName == Property.EngineName) as UhtProperty;

				if (OldProperty == null)
				{
					continue;
				}

				if( OldProperty is UhtEnumProperty oldEnumProperty && Property is UhtEnumProperty enumProperty1) 
				{
					if( oldEnumProperty.Enum == enumProperty1.Enum )
					{
						// 同じEnumなので代入でコピー出来る
						builder.Append($"\t{Property.EngineName} = InPrevData.{OldProperty.EngineName};\r\n");
					}
					else
					{
						builder.Append($"\tswitch(InPrevData.{OldProperty.EngineName})\r\n");
						builder.Append("\t{\r\n");

						string GetEnumLabel(UhtEnumValue enumValue, UhtEnumProperty enumProperty)
						{
							return enumValue.Name.Replace($"{enumProperty.Enum.EngineName}::", "");
						}

						foreach (UhtEnumValue enumValue in oldEnumProperty.Enum.EnumValues)
						{
							string EnumLabel = GetEnumLabel(enumValue, oldEnumProperty);

							bool IsExist(UhtEnumValue target)
							{
								return GetEnumLabel(target, enumProperty1) == EnumLabel;
							}

							builder.Append($"\t\tcase {enumValue.Name}:\r\n");
							if (enumProperty1.Enum.EnumValues.Exists(IsExist))
							{
								UhtEnumValue ResultValue = enumProperty1.Enum.EnumValues.Find(IsExist);
								builder.Append($"\t\t\t{Property.EngineName} = {ResultValue.Name};\r\n");
							}
							else
							{
								builder.Append($"\t\t\t// {enumValue.Name} Not Found\r\n");
							}
							builder.Append("\t\t\tbreak;\r\n");
						}
						builder.Append($"\t\tdefault:\r\n");
						builder.Append("\t\t\tbreak;\r\n");
						builder.Append("\t}\r\n\r\n");
						//
						//oldEnumProperty.Enum.Children
					}
					continue;
				}

				builder.Append($"\t{Property.EngineName} = InPrevData.{OldProperty.EngineName};\r\n");
			}

			builder.Append("\t// Success!\r\n");
			builder.Append("\treturn true;\r\n");
			builder.Append("}\r\n");
			builder.Append("\r\n");
		}
	}
}
