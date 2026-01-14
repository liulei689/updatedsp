@echo off
setlocal

cd /d "%~dp0"
echo [1/2] Restoring NuGet packages...

where msbuild >nul 2>nul
if %errorlevel%==0 (
  msbuild "UpdateDSP.sln" /m /t:Restore
  if errorlevel 1 goto :fail
  echo [2/2] Build solution (Debug)...
  msbuild "UpdateDSP.sln" /m /t:Build /p:Configuration=Debug
  if errorlevel 1 goto :fail
  echo Done.
  exit /b 0
)

where dotnet >nul 2>nul
if %errorlevel%==0 (
  dotnet restore "UpdateDSP.sln"
  if errorlevel 1 goto :fail
  echo [2/2] Build solution (Debug)...
  dotnet build "UpdateDSP.sln" -c Debug
  if errorlevel 1 goto :fail
  echo Done.
  exit /b 0
)

echo ERROR: msbuild and dotnet were not found.
echo Please open "Developer Command Prompt for VS" and run this script again.
exit /b 1

:fail
echo.
echo Build/Restore failed. You can also restore packages from Visual Studio.
exit /b 1
