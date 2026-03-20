@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"

set "UNITY_EXE="
set "MODE=all"
set "LOG_FILE="

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--unity" (
    set "UNITY_EXE=%~2"
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--mode" (
    set "MODE=%~2"
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--log" (
    set "LOG_FILE=%~2"
    shift
    shift
    goto parse_args
)
echo Unknown argument: %~1
goto usage

:args_done
if "%UNITY_EXE%"=="" (
    if not "%UNITY_EXE_PATH%"=="" set "UNITY_EXE=%UNITY_EXE_PATH%"
)
if "%UNITY_EXE%"=="" (
    if not "%UNITY_EXE_ENV%"=="" set "UNITY_EXE=%UNITY_EXE_ENV%"
)
if "%UNITY_EXE%"=="" (
    if not "%UNITY_PATH%"=="" set "UNITY_EXE=%UNITY_PATH%"
)

if "%UNITY_EXE%"=="" goto usage
if not exist "%UNITY_EXE%" (
    echo Unity executable not found: %UNITY_EXE%
    exit /b 1
)

set "EXECUTE_METHOD=HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileAndTests"
if /I "%MODE%"=="compile" set "EXECUTE_METHOD=HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileOnly"
if /I "%MODE%"=="edit" set "EXECUTE_METHOD=HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunEditModeTests"
if /I "%MODE%"=="play" set "EXECUTE_METHOD=HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunPlayModeTests"

if /I not "%MODE%"=="all" if /I not "%MODE%"=="compile" if /I not "%MODE%"=="edit" if /I not "%MODE%"=="play" (
    echo Invalid mode: %MODE%
    goto usage
)

set "REPORT_DIR=%PROJECT_ROOT%\reports\unity-tests"
if not exist "%REPORT_DIR%" mkdir "%REPORT_DIR%"

if "%LOG_FILE%"=="" (
    set "LOG_FILE=%REPORT_DIR%\unity-commandlet.log"
)

echo Project root  : %PROJECT_ROOT%
echo Unity exe     : %UNITY_EXE%
echo Mode          : %MODE%
echo ExecuteMethod : %EXECUTE_METHOD%
echo Log file      : %LOG_FILE%

"%UNITY_EXE%" ^
  -batchmode ^
  -nographics ^
  -quit ^
  -projectPath "%PROJECT_ROOT%" ^
  -executeMethod %EXECUTE_METHOD% ^
  -logFile "%LOG_FILE%"

set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" (
    echo Unity compile/test failed with exit code %EXIT_CODE%.
    exit /b %EXIT_CODE%
)

echo Unity compile/test succeeded.
exit /b 0

:usage
echo Usage:
echo   scripts\unity_compile_test.bat --unity "C:\Path\To\Unity.exe" [--mode all^|compile^|edit^|play] [--log "path\to\unity.log"]
echo.
echo Alternative environment variables:
echo   UNITY_EXE_PATH or UNITY_EXE_ENV or UNITY_PATH
exit /b 1
