set(pdb2mdb_PATH ${CMAKE_SOURCE_DIR}/MSCLoader/packages/Mono.Unofficial.pdb2mdb.4.2.3.4/tools/pdb2mdb.exe)
set(MSCLoader_MDB_DEBUGINFO_PATH_ ${CMAKE_BINARY_DIR}/MSCLoader/$<CONFIG>/MSCLoader.dll.mdb)
set(MSCLoader_MDB_DEBUGINFO_PATH $<$<CONFIG:Debug>:${MSCLoader_MDB_DEBUGINFO_PATH_}>)
add_custom_target(PDB2MDB_MSCLoader ALL
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/MSCLoader/$<CONFIG>
		BYPRODUCTS MSCLoader_MDB_DEBUGINFO_PATH
		COMMAND $<$<CONFIG:Debug>:${pdb2mdb_PATH}> MSCLoader.dll
)