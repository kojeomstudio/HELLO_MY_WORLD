@echo off
REM =============================================================================
REM Protobuf 자동 생성 스크립트 (Windows)
REM Google.Protobuf만 사용하여 일관된 C# 코드 생성
REM =============================================================================

setlocal enabledelayedexpansion

REM 경로 설정
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set PROTO_DIR=%PROJECT_ROOT%\proto
set OUTPUT_DIR=%PROJECT_ROOT%\Assets\Generated\Protobuf

echo === Protobuf 생성 시작 ===
echo 프로토 디렉토리: %PROTO_DIR%
echo 출력 디렉토리: %OUTPUT_DIR%

REM protoc 확인
where protoc >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo 오류: protoc가 설치되지 않았습니다.
    echo 다운로드: https://github.com/protocolbuffers/protobuf/releases
    exit /b 1
)

REM protoc 버전 확인
for /f "tokens=2" %%i in ('protoc --version') do set PROTOC_VERSION=%%i
echo protoc 버전: %PROTOC_VERSION%

REM 출력 디렉토리 생성
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM 기존 파일 백업
if exist "%OUTPUT_DIR%\*.cs" (
    set TIMESTAMP=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
    set TIMESTAMP=!TIMESTAMP: =0!
    set BACKUP_DIR=%OUTPUT_DIR%.backup.!TIMESTAMP!
    echo 기존 파일 백업: !BACKUP_DIR!
    xcopy /E /I /Y "%OUTPUT_DIR%" "!BACKUP_DIR!" >nul
)

REM 생성할 proto 파일 목록
set PROTO_FILES=common.proto game_core.proto game_auth.proto game_chat.proto game_diag.proto game_move.proto game_world.proto enhanced_minecraft_game.proto

set SUCCESS_COUNT=0
set FAIL_COUNT=0

for %%f in (%PROTO_FILES%) do (
    set PROTO_PATH=%PROTO_DIR%\%%f

    if not exist "!PROTO_PATH!" (
        echo 경고: %%f 파일을 찾을 수 없습니다. 건너뜁니다.
    ) else (
        echo 생성 중: %%f

        protoc --proto_path="%PROTO_DIR%" --csharp_out="%OUTPUT_DIR%" --csharp_opt=file_extension=.cs "!PROTO_PATH!"

        if !ERRORLEVEL! equ 0 (
            echo ✓ %%f 생성 완료
            set /a SUCCESS_COUNT+=1
        ) else (
            echo ✗ %%f 생성 실패
            set /a FAIL_COUNT+=1
        )
    )
)

echo.
echo === 생성 결과 ===
echo 성공: %SUCCESS_COUNT%
echo 실패: %FAIL_COUNT%

echo.
echo === 생성된 파일 ===
dir /B "%OUTPUT_DIR%\*.cs" 2>nul

echo.
echo === 완료 ===
echo 총 %SUCCESS_COUNT%개의 프로토콜 파일이 성공적으로 생성되었습니다.
echo 출력 디렉토리: %OUTPUT_DIR%
echo.
echo 다음 단계:
echo   1. Unity 에디터를 열어 생성된 파일을 확인하세요.
echo   2. GameServer 프로젝트를 빌드하여 컴파일 오류가 없는지 확인하세요.
echo   3. 클라이언트와 서버 코드에서 새로운 네임스페이스를 사용하세요:
echo      - MinecraftGame.Common (공통 타입)
echo      - EnhancedMinecraftProtocol (게임 프로토콜)
echo      - Game.Core, Game.Move, Game.World 등 (기타 프로토콜)

endlocal
exit /b 0
