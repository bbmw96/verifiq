@echo off
REM VERIFIQ Build Script
REM Builds the full solution in Release x64 configuration.
REM Usage: build.bat

echo ============================================
echo  VERIFIQ: IFC Compliance Checker
echo  BBMW0 Technologies -- Build Script
echo ============================================
echo.

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found.
    echo Install from: https://dotnet.microsoft.com/download/dotnet/8
    pause
    exit /b 1
)

echo Restoring NuGet packages...
dotnet restore VERIFIQ.sln
if %errorlevel% neq 0 ( echo RESTORE FAILED & pause & exit /b 1 )

echo.
echo Building solution (Release x64)...
dotnet build VERIFIQ.sln -c Release -r win-x64 --no-restore
if %errorlevel% neq 0 ( echo BUILD FAILED & pause & exit /b 1 )

echo.
echo Publishing Desktop project...
dotnet publish src\VERIFIQ.Desktop\VERIFIQ.Desktop.csproj ^
    -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -o publish\VERIFIQ_v1.0
if %errorlevel% neq 0 ( echo PUBLISH FAILED & pause & exit /b 1 )

echo.
echo ============================================
echo  Build complete.
echo  Output: publish\VERIFIQ_v1.0\
echo  Run:    publish\VERIFIQ_v1.0\VERIFIQ.Desktop.exe
echo ============================================
pause
