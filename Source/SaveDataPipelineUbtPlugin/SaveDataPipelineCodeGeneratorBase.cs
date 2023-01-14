// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicGames.UHT.Types;

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

	}
}
