cmake_minimum_required(VERSION 3.19)

project(MySummerCarModLoader LANGUAGES CSharp)

include(CSharpUtilities)
include(cmake/NugetRestore.cmake)

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
add_dependencies(MSCPatcher NugetRestore_MSCPatcher)


