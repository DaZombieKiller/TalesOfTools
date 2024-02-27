@echo off
cmake -G "Visual Studio 17 2022" -A Win32 -B build_x86 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake"
pause