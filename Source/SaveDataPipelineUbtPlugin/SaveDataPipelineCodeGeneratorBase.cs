// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using Microsoft.Extensions.Logging;

namespace SaveDataPipelineUbtPlugin
{
	internal abstract class SaveDataPipelineCodeGeneratorBase
	{
		public static string HeaderCopyright =
	"// Copyright Epic Games, Inc. All Rights Reserved.\r\n" +
	"/*===========================================================================\r\n" +
	"\tGenerated code exported from UnrealHeaderTool.\r\n" +
	"\tDO NOT modify this manually! Edit the corresponding .h files instead!\r\n" +
	"===========================================================================*/\r\n" +
	"\r\n";

		public static string SaveDataPipelineMacrosHeader = "#include \"SaveDataPipeline/SaveDataPipelineMacros.h\"\r\n";

		public static string IncludeMemoryWriterHeader = "#include \"Serialization/MemoryWriter.h\"\r\n";
		public static string IncludeMemoryReaderHeader = "#include \"Serialization/MemoryReader.h\"\r\n";

		public static string EnableDeprecationWarnings = "PRAGMA_ENABLE_DEPRECATION_WARNINGS";
		public static string DisableDeprecationWarnings = "PRAGMA_DISABLE_DEPRECATION_WARNINGS";

		public static string BeginEditorOnlyGuard = "#if WITH_EDITOR\r\n";
		public static string EndEditorOnlyGuard = "#endif //WITH_EDITOR\r\n";


		public readonly IUhtExportFactory Factory;
		public UhtSession Session => Factory.Session;
		public UhtHeaderFile TargetHeader { get; }
		public List<UhtStruct> TargetStructs { get; }

		public SaveDataPipelineCodeGeneratorBase(IUhtExportFactory factory, UhtHeaderFile targetHeader, List<UhtStruct> targetStructs)
		{
			Factory = factory;
			TargetHeader = targetHeader;
			TargetStructs = targetStructs;
		}

		protected void ExportStruct(StringBuilder builder, UhtType type)
		{
			if (type is UhtStruct structObj)
			{
				if (structObj.MetaData.ContainsKey("SaveDataPipeline"))
				{
					ExportStruct(builder, structObj);
				}
			}
			foreach (UhtType child in type.Children)
			{
				ExportStruct(builder, child);
			}
		}

		protected abstract void ExportStruct(StringBuilder builder, UhtStruct structObj);

		protected IEnumerable<UhtStruct> GetStructOfOtherHeaderFileOnDepends()
		{
			foreach (UhtStruct structObj in TargetHeader.Children.OfType<UhtStruct>())
			{
				if (!structObj.MetaData.ContainsKey("BaseType"))
				{
					continue;
				}
				string BaseTypeName = structObj.MetaData.GetValueOrDefault("BaseType");
				UhtStruct? baseType = TargetStructs.Find(target => target.EngineName == BaseTypeName);
				if (baseType == null)
				{
					Session.Logger?.Log(LogLevel.Error, $"Not Found Type F{BaseTypeName}");
					continue;
				}
				if (baseType.HeaderFile != TargetHeader)
				{
					yield return baseType;
				}
			}
		}
		protected static IEnumerable<UhtType> GetStructPropertyTypes(UhtStruct TargetStruct)
		{
			foreach (UhtProperty Property in TargetStruct.Children.OfType<UhtProperty>())
			{
				if (Property is UhtEnumProperty enumProperty)
				{
					yield return enumProperty.Enum;
				}
				if (Property is UhtStructProperty structProperty)
				{
					yield return structProperty.ScriptStruct;
				}
			}
		}

		protected static string GetRelativeHeaderFilePath( UhtHeaderFile InHaderFile )
		{
			string IncludePath = Path.GetRelativePath(InHaderFile.Package.Module.IncludeBase, InHaderFile.FilePath);
			IncludePath = IncludePath.Replace('\\', '/');
			return IncludePath;
		}

	}
}
