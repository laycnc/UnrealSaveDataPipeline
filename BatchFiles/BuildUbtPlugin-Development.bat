chcp 65001
echo Build Start

set ProjectFile=%~dp0../Source/SaveDataPipelineUbtPlugin/SaveDataPipelineUbtPlugin.ubtplugin.csproj
echo if not exist %ProjectFile% goto NoProjectFile

echo Building UnrealBuildTool with dotnet...
dotnet build %ProjectFile% -c Development -v quiet

:NoProjectFile
