add_executable(MSCPatcher WIN32)

target_sources(MSCPatcher
	PRIVATE
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Log.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/MainData.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/MD5FileHashes.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Patcher.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Program.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/DebugStuff.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/AssemblyInfo.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.Designer.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.resx
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.Designer.cs 
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Resources.Designer.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Resources.resx
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Settings.Designer.cs
		${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Settings.settings
)

csharp_set_designer_cs_properties(
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Program.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.resx
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.Designer.cs 
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Resources.Designer.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Resources.resx
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Settings.Designer.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Properties/Settings.settings
)


set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.Designer.cs PROPERTIES
  VS_CSHARP_DependentUpon ${CMAKE_CURRENT_SOURCE_DIR}/MSCPatcher/Form1.cs)



set_property(TARGET MSCPatcher PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v3.5")
set_property(TARGET MSCPatcher PROPERTY WIN32_EXECUTABLE TRUE)


set_property(TARGET MSCPatcher
    PROPERTY VS_DOTNET_REFERENCES
	"System.Windows.Forms"
	"System"
	"System.Core"
	"System.Xml.Linq"
	"System.Data.DataSetExtensions"
	"System.Data"
	"System.Deployment"
	"System.Drawing"
	"System.Xml"
)

file(TO_NATIVE_PATH "${MSC_GAME_PATH}/mysummercar_Data/Managed" MSC_GAME_ASSEMBLIES_PATH)
file(TO_NATIVE_PATH "${CMAKE_SOURCE_DIR}/MSCPatcher/packages" MSCPatcher_PACKAGES_PATH)
file(TO_NATIVE_PATH	"${MSCPatcher_PACKAGES_PATH}/Ionic.Zip.1.9.1.8/lib/Ionic.Zip.dll" Ionic_Zip_PATH)
file(TO_NATIVE_PATH "${CMAKE_SOURCE_DIR}/MSCPatcher/packages/Mono.Cecil.0.10.3/lib/net35/Mono.Cecil" MSCPatcher_PACKAGES_MONO_CECIL_BASE)

set_target_properties(MSCPatcher
	PROPERTIES
        VS_DOTNET_REFERENCE_Ionic_Zip ${Ionic_Zip_PATH}
				VS_DOTNET_REFERENCE_Mono_Cecil ${MSCPatcher_PACKAGES_MONO_CECIL_BASE}.dll
				VS_DOTNET_REFERENCE_Mono_Cecil_Mdb ${MSCPatcher_PACKAGES_MONO_CECIL_BASE}.Mdb.dll
				VS_DOTNET_REFERENCE_Mono_Cecil_Pdb ${MSCPatcher_PACKAGES_MONO_CECIL_BASE}.Pdb.dll
				VS_DOTNET_REFERENCE_Mono_Cecil_Rocks ${MSCPatcher_PACKAGES_MONO_CECIL_BASE}.Rocks.dll
)
