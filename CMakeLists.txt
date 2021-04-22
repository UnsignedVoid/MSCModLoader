cmake_minimum_required(VERSION 3.19)

project(MySummerCarModLoader LANGUAGES CSharp)

add_compile_definitions($<$<CONFIG:Debug>:Debug> $<$<CONFIG:Debug>:DevMode>)

include(CSharpUtilities)
include(cmake/NugetRestore.cmake)
include(cmake/pdb2mdb.cmake)

option(MSC_GAME_PATH "Path to Installed My Summer Car" OFF)

if(NOT MSC_GAME_PATH)
	message(FATAL_ERROR "MSC_GAME_PATH Must be set")
else()
	message("MSC_GAME_PATH is ${MSC_GAME_PATH}")
endif()

set(CMAKE_CSharp_FLAGS "/langversion:latest /platform:anycpu")


add_subdirectory(MSCLoader)
add_subdirectory(MSCPatcher)

add_dependencies(MSCLoader NugetRestore_MSCLoader)
add_dependencies(PDB2MDB_MSCLoader MSCLoader)
add_dependencies(MSCPatcher NugetRestore_MSCPatcher)


install(
	TARGETS 
		MSCLoader MSCPatcher MSCLoader
	DESTINATION
		${CMAKE_INSTALL_PREFIX}
)