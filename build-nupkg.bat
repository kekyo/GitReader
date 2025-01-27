@echo off

rem GitReader
rem Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
rem
rem Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo.
echo "==========================================================="
echo "Build GitReader"
echo.

dotnet build -p:Configuration=Release -p:Platform="Any CPU" -p:RestoreNoCache=True GitReader.sln
dotnet pack -p:Configuration=Release -p:Platform="Any CPU" -o artifacts GitReader.sln
