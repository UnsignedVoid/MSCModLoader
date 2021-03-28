include(cmake/NAudioFlac.cmake)

add_library(MSCLoader SHARED)

target_sources(MSCLoader
	PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/MP3_Streaming/MP3Stream.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/MP3_Streaming/ReadFullyStream.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/Playlists.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Commands/CommandVersion.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Commands/CommandLogAll.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Commands/ManifestCommand.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ConsoleCommand.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ConsoleController.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ConsoleUIResizer.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ConsoleView.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/FsmHook.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Keybind.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/KeyBinding.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/LoadAssets.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Mod.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModAudio.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModAudioStream.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModConsole.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModFixedUpdate.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModLoader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModOnGUI.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModSettings.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModsManifest.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModUI.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModUIDrag.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/ModUpdate.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/MSCLoader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/MSCUnloader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/NAudio.Vorbis/VorbisWaveReader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/OBJLoader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/PlayMakerExtensions.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Properties/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/AudioFileReader.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/AudioLibrary/Manager.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/SaveLoad.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/Settings.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/MSCLoader/SettingsView.cs
)

add_dependencies(MSCLoader NAudioFlac)


set_property(TARGET MSCLoader PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v3.5")
set_property(TARGET MSCLoader PROPERTY WIN32_EXECUTABLE TRUE)

set_property(TARGET MSCLoader
    PROPERTY VS_DOTNET_REFERENCES
	"System"
	"System.Data"
	"System.Core"
)

file(TO_NATIVE_PATH "${MSC_GAME_PATH}/mysummercar_Data/Managed" MSC_GAME_ASSEMBLIES_PATH)
file(TO_NATIVE_PATH "${CMAKE_SOURCE_DIR}/MSCLoader/packages" MSCLoader_PACKAGES_PATH)

file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/Assembly-CSharp.dll" Assembly_CSharp_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/Assembly-CSharp-firstpass.dll" Assembly_CSharp_firstpass_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/Assembly-UnityScript.dll" Assembly_UnityScript_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/Assembly-UnityScript-firstpass.dll" Assembly_UnityScript_firstpass_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/PlayMaker.dll" Assembly_PlayMaker_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/UnityEngine.dll" Assembly_UnityEngine_PATH )
file(TO_NATIVE_PATH "${MSC_GAME_ASSEMBLIES_PATH}/UnityEngine.UI.dll" Assembly_UnityEngine_UI_PATH )
file(TO_NATIVE_PATH "${MSCLoader_PACKAGES_PATH}/Lib.Harmony.1.2.0.1/lib/net35/0Harmony.dll" 0Harmony_PATH)
file(TO_NATIVE_PATH	"${MSCLoader_PACKAGES_PATH}/Ionic.Zip.1.9.1.8/lib/Ionic.Zip.dll" Ionic_Zip_PATH)
file(TO_NATIVE_PATH	"${MSCLoader_PACKAGES_PATH}/NAudio.1.10.0/lib/net35/NAudio.dll" NAudio_PATH)
file(TO_NATIVE_PATH	"${MSCLoader_PACKAGES_PATH}/Newtonsoft.Json.13.0.1/lib/net35/Newtonsoft.Json.dll" Newtonsoft_Json_PATH)
file(TO_NATIVE_PATH	"${MSCLoader_PACKAGES_PATH}/NVorbis.0.8.6/lib/net35/NVorbis.dll" NVorbis_PATH)
file(TO_NATIVE_PATH	"${NAudioFlac_BYPRODUCTS}" NAudio_Flac_PATH)

set_target_properties(MSCLoader
	PROPERTIES
        VS_DOTNET_REFERENCE_Assembly_CSharp ${Assembly_CSharp_PATH}
        VS_DOTNET_REFERENCE_Assembly_CSharp_firstpass ${Assembly_CSharp_firstpass_PATH}
        VS_DOTNET_REFERENCE_Assembly_UnityScript ${Assembly_UnityScript_PATH}
        VS_DOTNET_REFERENCE_Assembly_UnityScript_firstpass ${Assembly_UnityScript_firstpass_PATH}
        VS_DOTNET_REFERENCE_Assembly_PlayMaker ${Assembly_PlayMaker_PATH}
        VS_DOTNET_REFERENCE_Assembly_UnityEngine ${Assembly_UnityEngine_PATH}
        VS_DOTNET_REFERENCE_Assembly_UnityEngine_UI ${Assembly_UnityEngine_UI_PATH}
        VS_DOTNET_REFERENCE_0Harmony ${0Harmony_PATH}
        VS_DOTNET_REFERENCE_Ionic_Zip ${Ionic_Zip_PATH}
        VS_DOTNET_REFERENCE_NAudio ${NAudio_PATH}
        VS_DOTNET_REFERENCE_Newtonsoft_Json ${Newtonsoft_Json_PATH}
        VS_DOTNET_REFERENCE_NVorbis ${NVorbis_PATH}
        VS_DOTNET_REFERENCE_NAudio_Flac ${NAudio_Flac_PATH}
)
