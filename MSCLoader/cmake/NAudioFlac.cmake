include(ExternalProject)

file(TO_NATIVE_PATH ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac/trunk/bin/$<CONFIG>/NAudio.Flac.dll CURRNET_NAUDIO_PATH)
file(TO_NATIVE_PATH ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac/trunk/bin/NAudio.Flac.dll INSTALL_NAUDIO_PATH)

ExternalProject_Add(NAudioFlac
	PREFIX NAudioFlac
	URL https://storage.googleapis.com/google-code-archive-source/v2/code.google.com/naudio-flac/source-archive.zip
	DOWNLOAD_DIR ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac
	SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac
	BUILD_COMMAND echo msbuild ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac/trunk/NAudio.Flac.csproj -p:Configuration=$<CONFIG>
	INSTALL_COMMAND copy ${CURRNET_NAUDIO_PATH} ${INSTALL_NAUDIO_PATH}
	BUILD_IN_SOURCE True
	CONFIGURE_COMMAND ""
	BUILD_BYPRODUCTS ${CMAKE_CURRENT_SOURCE_DIR}/NAudioFlac/trunk/bin/NAudio.Flac.dll
)

ExternalProject_Get_Property(NAudioFlac BUILD_BYPRODUCTS)
set(NAudioFlac_BYPRODUCTS ${BUILD_BYPRODUCTS})