add_custom_target(NugetRestore_MSCPatcher
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/MSCPatcher/MSCPatcher
    COMMAND nuget restore -PackagesDirectory ${CMAKE_SOURCE_DIR}/MSCPatcher/packages
)

add_custom_target(NugetRestore_MSCLoader
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/MSCLoader/MSCLoader
    COMMAND nuget restore -PackagesDirectory ${CMAKE_SOURCE_DIR}/MSCLoader/packages
)