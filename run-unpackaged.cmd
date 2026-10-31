@echo off
rem ---------------------------------------------------------------------------------------------
rem  EDAM Studio - unpackaged development launch
rem
rem  Runs the unpackaged build straight from its output folder, keeping application data inside the
rem  repository instead of the user profile. That matters on a machine that confines a process so
rem  tightly that it cannot write its own %LOCALAPPDATA%: the process can still write here, so the
rem  application gets a data root it can actually use.
rem
rem  Build the unpackaged flavour first (packaged is the supported deployment):
rem
rem    MSBuild.exe src\Edam.Studio\Edam.Studio\Edam.Studio.csproj /t:Build /p:Configuration=Debug ^
rem      /p:Platform=x64 /p:WindowsPackageType=None /p:PublishAppxPackage=false ^
rem      /p:WindowsAppSDKSelfContained=true
rem ---------------------------------------------------------------------------------------------

setlocal
set "ROOT=%~dp0"
set "EDAM_STUDIO_DATA_ROOT=%ROOT%.local-data"
set "APP=%ROOT%src\Edam.Studio\Edam.Studio\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Edam.Studio.exe"

if not exist "%APP%" (
   echo.
   echo   The unpackaged build was not found:
   echo   %APP%
   echo.
   echo   Build it first - see the command in this file's header.
   echo.
   pause
   exit /b 1
)

echo   data root : %EDAM_STUDIO_DATA_ROOT%
echo   starting  : %APP%
start "" "%APP%"
endlocal
