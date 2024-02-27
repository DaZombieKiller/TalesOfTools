@echo off
cmake -G "Visual Studio 17 2022" -A x64 -B build_x64 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake"
pause