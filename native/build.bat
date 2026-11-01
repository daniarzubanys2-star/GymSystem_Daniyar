@echo off
rem Windows, 64-bit. Run from "x64 Native Tools Command Prompt for VS" (MSVC):
cd /d "%~dp0"
if not exist out mkdir out
cl /nologo /LD /O2 gym_native.c /Fe:out\gymnative.dll
rem Alternative with MinGW-w64:  gcc -shared -O2 -o out\gymnative.dll gym_native.c
