// Copyright Epic Games, Inc. All Rights Reserved.

using EpicGames.Core;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using EpicGames.UHT.Types;
using EpicGames.UHT.Parsers;
using EpicGames.UHT.Tokenizer;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace SaveDataPipelineUbtPlugin
{
	[UnrealHeaderTool]
	class SaveDataPipelineGenerator
	{
		public readonly IUhtExportFactory Factory;
		public UhtSession Session => Factory.Session;

		/// <summary>
		/// Target Struct
		/// </summary>
		private List<UhtStruct> TargetStructs { get; }
		private List<UhtHeaderFile> TargetHeaders { get; }

		//[UhtKeyword(Extends = UhtTableNames.NativeInterface)]
		//[UhtKeyword(Extends = UhtTableNames.Struct, Keyword = "GENERATED_SAVE_PIPELINE_BODY")]
		//private static UhtParseResult ClassKeyword(UhtParsingScope topScope, UhtParsingScope actionScope, ref UhtToken token)
		//{
		//
		//	// todo
		//	// ここのtokenをtopScopeに保持させる必要があるがサンプルコードが無いので一旦放置
		//
		//	return UhtParseResult.Handled;
		//}

		[UhtExporter(Name = "SaveDataPipeline", Description = "Generic Script Plugin Generator", Options = UhtExporterOptions.Default, ModuleName = "SaveDataPipeline", CppFilters = new string[] { "*.savepipeline.cpp" }, HeaderFilters = new string[] { "*.savepipeline.h" })]
		private static void ScriptGeneratorExporter(IUhtExportFactory Factory)
		{

			// Make sure this plugin should be run
			if (!Factory.Session.IsPluginEnabled("SaveDataPipeline", false))
			{
				return;
			}

			// Based on the WITH_LUA setting, run the proper exporter.
			if (Factory.PluginModule != null)
			{
				new SaveDataPipelineGenerator(Factory).Generate();
			}
		}


		public SaveDataPipelineGenerator(IUhtExportFactory factory)
		{
			Factory = factory;
			TargetStructs = new List<UhtStruct>();
			TargetHeaders = new List<UhtHeaderFile>();
		}

		/// <summary>
		/// Export all the classes in all the packages
		/// </summary>
		public void Generate()
		{
			// Init Package
			foreach (UhtPackage package in Session.Packages)
			{
				// game runtime only
				if (package.Module.ModuleType != UHTModuleType.GameRuntime)
				{
					continue;
				}

				InitPackageInfo(package, package);
			}

			List<Task?> tasks = new();

			foreach(UhtHeaderFile target_header in TargetHeaders) 
			{
				tasks.Add(Factory.CreateTask((IUhtExportFactory factory) =>
				{
					new SaveDataPipelineHeaderGenerator(factory, target_header, TargetStructs).Generate();
				}));

				tasks.Add(Factory.CreateTask((IUhtExportFactory factory) =>
				{
					new SaveDataPipelineSourceGenerator(factory, target_header, TargetStructs).Generate();
				}));
			}

			// Wait for all the classes to export
			Task[]? waitTasks = tasks.Where(x => x != null).Cast<Task>().ToArray();
			if (waitTasks.Length > 0)
			{
				Task.WaitAll(waitTasks);
			}
		}

		private void InitPackageInfo(UhtPackage package, UhtType type)
		{
			if (type is UhtStruct structObj)
			{
				if (structObj.MetaData.ContainsKey("SaveDataPipeline"))
				{
					TargetStructs.Add(structObj);
					if (!TargetHeaders.Contains(structObj.HeaderFile))
					{
						TargetHeaders.Add(structObj.HeaderFile);
					}
				}
			}
			foreach (UhtType child in type.Children)
			{
				InitPackageInfo(package, child);
			}
		}

		/// <summary>
		/// Export the given class
		/// </summary>
		/// <param name="Factory">Factory associated with the export</param>
		/// <param name="classObj">Class to export</param>
		private void ExportClass(UHTManifest.Module moduleObj, UhtStruct structObj)
		{
			using BorrowStringBuilder borrower = new(StringBuilderCache.Big);
			ExportClassString(borrower.StringBuilder, structObj);
			string fileName = Path.Combine(moduleObj.OutputDirectory, $"{structObj.EngineName}.script.h");
			Factory.CommitOutput(fileName, borrower.StringBuilder);
		}

		private static void ExportClassString(StringBuilder builder, UhtStruct structObj)
		{
			builder.Append("#pragma once\r\n\r\n");

			builder.Append($"void Build( const F{structObj.EngineName}& Data )\r\n");
			builder.Append("{\r\n");

			foreach(UhtType type in structObj.Children )
			{
				if (type is UhtProperty property)
				{
					//property.AppendText(builder, UhtPropertyTextType.SparseShort);
					//builder.Append($"\t//{property.CppTypeText} {property.EngineName}\r\n");
				}
			}

			builder.Append("};\r\n");
		}

	}

}