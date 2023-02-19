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

			if( SaveExportedHeaders )
			{
				string fileName = Path.Combine(TargetHeader.Package.Module.OutputDirectory, $"{TargetHeader.FileNameWithoutExtension}.savepipeline.gen.cpp");
				Factory.CommitOutput(fileName, borrower.StringBuilder);
			}
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

			ExportSavePipelineRead( builder, structObj, BaseTypeResult );

			ExportSavePipelineWrite( builder, structObj );
		}


		private static void ExportSavePipelineRead(StringBuilder builder, UhtStruct structObj, UhtStruct? baseType)
		{
			builder.Append($"bool F{structObj.EngineName}::SavePipelineRead(FMemoryReader& MemoryReader, int32* InReadHash)\r\n");
			builder.Append("{\r\n");
			builder.Append(StructHashRead);

			builder.Append($"\tif( Hash != F{structObj.EngineName}::GetSavePipelineHash() )\r\n");
			builder.Append("\t{\r\n");
			if(baseType != null)
			{
				builder.Append($"\t\tF{baseType.EngineName} OldVersion;\r\n");
				builder.Append("\t\tif( OldVersion.SavePipelineRead( MemoryReader, &Hash ) )\r\n");
				builder.Append("\t\t{\r\n");
				builder.Append("\t\t\treturn SavePipelineConvert(OldVersion);\r\n");
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
				//builder.Append($"\tMemoryReader << {Property.EngineName};\r\n");
				ExportProperty( builder, Property, "MemoryReader", Property.EngineName, false );
			}
			builder.Append("\t// Success!\r\n");
			builder.Append("\treturn true;\r\n");
			builder.Append("}\r\n");
			builder.Append("\r\n");
		}

		private static void ExportSavePipelineWrite(StringBuilder builder, UhtStruct structObj)
		{
			builder.Append($"bool F{structObj.EngineName}::SavePipelineWrite(FMemoryWriter& MemoryWriter)\r\n");
			builder.Append("{\r\n");
			builder.Append($"\tint32 Hash = F{structObj.EngineName}::GetSavePipelineHash();\r\n");
			builder.Append("\tMemoryWriter << Hash;\r\n");
			foreach (UhtProperty Property in structObj.Children.OfType<UhtProperty>())
			{
				// 	MemoryReader << FileTypeTag;
				//builder.Append($"\tMemoryWriter << {Property.EngineName};\r\n");
				ExportProperty( builder, Property, "MemoryWriter", Property.EngineName, true );
			}
			builder.Append("\t// Success!\r\n");
			builder.Append("\treturn true;\r\n");
			builder.Append("}\r\n");
			builder.Append("\r\n");
		}

		private static void ExportProperty( StringBuilder builder, UhtProperty InProperty, string MemortyName, string PropetyPath, bool IsWrite )
		{
			if( InProperty is UhtStructProperty structProperty )
			{
				if( structProperty.ScriptStruct.MetaData.ContainsKey( "SaveDataPipeline" ) )
				{
					if( IsWrite )
					{
						builder.Append($"\tif(!{PropetyPath}.SavePipelineWrite({MemortyName}))\r\n");
						builder.Append("\t{\r\n");
						builder.Append("\t\treturn false;\r\n");
						builder.Append("\t}\r\n");
						return;
					}
					builder.Append($"\tif(!{PropetyPath}.SavePipelineRead({MemortyName}))\r\n");
					builder.Append("\t{\r\n");
					builder.Append("\t\treturn false;\r\n");
					builder.Append("\t}\r\n");
					return;
				}
				foreach( UhtProperty childProperty in structProperty.ScriptStruct.Children.OfType<UhtProperty>() )
				{
					ExportProperty( builder, childProperty, MemortyName, $"{PropetyPath}.{childProperty.EngineName}", IsWrite );
				}
				return;
			}

			if( InProperty is UhtStrProperty strProperty )
			{
				if( strProperty.MetaData.ContainsKey("SaveDataPipelineFixedSize") )
				{
					if( strProperty.MetaData.TryGetValue( "SaveDataPipelineFixedSize", out var SaveDataPipelineFixedSize ) )
					{
						if( SaveDataPipelineFixedSize != null )
						{
							if( IsWrite )
							{
								string BufferName = PropetyPath.Replace('.', '_');
								builder.Append("\t{\r\n");
								builder.Append("\t\tTArray<TCHAR> __SDP_Buffer;\r\n");
								builder.Append($"\t\t__SDP_Buffer.SetNumZeroed( {SaveDataPipelineFixedSize} );\r\n");
								builder.Append($"\t\tFMemory::Memcpy(__SDP_Buffer.GetData(), *{PropetyPath}, FMath::Min({SaveDataPipelineFixedSize}, {PropetyPath}.Len()) * sizeof(TCHAR));\r\n");
								builder.Append("\t\t__SDP_Buffer[__SDP_Buffer.Num() - 1] = TEXT('\\0');\r\n");
								builder.Append($"\t\t{MemortyName} << __SDP_Buffer;\r\n");
								builder.Append("\t}\r\n");
							}
							else
							{
								string BufferName = PropetyPath.Replace('.', '_');
								builder.Append("\t{\r\n");
								builder.Append("\t\tTArray<TCHAR> __SDP_Buffer;\r\n");
								builder.Append($"\t\t__SDP_Buffer.Reserve( {SaveDataPipelineFixedSize} );\r\n");
								builder.Append($"\t\t{MemortyName} << __SDP_Buffer;\r\n");
								builder.Append($"\t\t{PropetyPath} = TStringView<TCHAR>( __SDP_Buffer.GetData() );\r\n");
								builder.Append("\t}\r\n");
							}
							return;
						}
					}
				}
			}

			builder.Append($"\t{MemortyName} << {PropetyPath};\r\n");
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
