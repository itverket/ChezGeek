@ECHO OFF

NET SESSION >nul 2>&1

IF (%ERRORLEVEL%) NEQ (0) GOTO :admin

PUSHD %~dp0

IF NOT EXIST ..\ChezGeek.ClusterNode.exe GOTO :output

@powershell -executionpolicy remotesigned -File init.ps1

PUSHD ..

ChezGeek.ClusterNode.exe

POPD

@powershell -executionpolicy remotesigned -File cleanup.ps1

POPD

GOTO :end

:output

ECHO Please run this script from the build output directory
ECHO.

GOTO :end

:admin

ECHO Please run as administrator
ECHO.

:end
PAUSE