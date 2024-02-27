cmake_minimum_required(VERSION 3.20)
project(TO13NameDumper)

# Find Microsoft Detours
find_path(DETOURS_INCLUDE_DIRS detours.h PATH_SUFFIXES detours REQUIRED)
find_library(DETOURS_LIBRARIES detours REQUIRED)

# Find source files
file(GLOB_RECURSE SOURCES CONFIGURE_DEPENDS "source/*.cpp")
file(GLOB_RECURSE INCLUDES CONFIGURE_DEPENDS "include/*.h")

# Initialize target
set(TARGET_NAME "winmm")
add_library(${TARGET_NAME} SHARED ${SOURCES} ${INCLUDES})
target_include_directories(${TARGET_NAME} PRIVATE include ${DETOURS_INCLUDE_DIRS})
target_link_libraries(${TARGET_NAME} PRIVATE ${DETOURS_LIBRARIES})
set_property(TARGET ${TARGET_NAME} PROPERTY
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")