#!/bin/bash

# =============================================================================
# Protobuf 자동 생성 스크립트
# Google.Protobuf만 사용하여 일관된 C# 코드 생성
# =============================================================================

set -e  # 오류 발생 시 즉시 종료

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 경로 설정
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PROTO_DIR="$PROJECT_ROOT/proto"
OUTPUT_DIR="$PROJECT_ROOT/Assets/Generated/Protobuf"

echo -e "${GREEN}=== Protobuf 생성 시작 ===${NC}"
echo "프로토 디렉토리: $PROTO_DIR"
echo "출력 디렉토리: $OUTPUT_DIR"

# protoc 확인
if ! command -v protoc &> /dev/null; then
    echo -e "${RED}오류: protoc가 설치되지 않았습니다.${NC}"
    echo "설치 방법:"
    echo "  - Ubuntu/Debian: sudo apt-get install protobuf-compiler"
    echo "  - macOS: brew install protobuf"
    echo "  - Windows: https://github.com/protocolbuffers/protobuf/releases"
    exit 1
fi

# protoc 버전 확인
PROTOC_VERSION=$(protoc --version | awk '{print $2}')
echo "protoc 버전: $PROTOC_VERSION"

# 출력 디렉토리 생성
mkdir -p "$OUTPUT_DIR"

# 기존 생성 파일 백업
if [ -d "$OUTPUT_DIR" ] && [ "$(ls -A $OUTPUT_DIR)" ]; then
    BACKUP_DIR="$OUTPUT_DIR.backup.$(date +%Y%m%d_%H%M%S)"
    echo -e "${YELLOW}기존 파일 백업: $BACKUP_DIR${NC}"
    cp -r "$OUTPUT_DIR" "$BACKUP_DIR"
fi

# 생성할 proto 파일 목록
PROTO_FILES=(
    "common.proto"
    "game_core.proto"
    "game_auth.proto"
    "game_chat.proto"
    "game_diag.proto"
    "game_move.proto"
    "game_world.proto"
    "enhanced_minecraft_game.proto"
)

# 각 proto 파일 생성
SUCCESS_COUNT=0
FAIL_COUNT=0

for proto_file in "${PROTO_FILES[@]}"; do
    proto_path="$PROTO_DIR/$proto_file"

    if [ ! -f "$proto_path" ]; then
        echo -e "${YELLOW}경고: $proto_file 파일을 찾을 수 없습니다. 건너뜁니다.${NC}"
        continue
    fi

    echo -e "${GREEN}생성 중: $proto_file${NC}"

    # protoc 실행
    if protoc \
        --proto_path="$PROTO_DIR" \
        --csharp_out="$OUTPUT_DIR" \
        --csharp_opt=file_extension=.cs \
        "$proto_path"; then
        echo -e "${GREEN}✓ $proto_file 생성 완료${NC}"
        ((SUCCESS_COUNT++))
    else
        echo -e "${RED}✗ $proto_file 생성 실패${NC}"
        ((FAIL_COUNT++))
    fi
done

echo ""
echo -e "${GREEN}=== 생성 결과 ===${NC}"
echo "성공: $SUCCESS_COUNT"
echo "실패: $FAIL_COUNT"

# 생성된 파일 목록
echo ""
echo -e "${GREEN}=== 생성된 파일 ===${NC}"
ls -lh "$OUTPUT_DIR"/*.cs 2>/dev/null || echo "생성된 파일이 없습니다."

# 생성된 파일 검증
echo ""
echo -e "${GREEN}=== 파일 검증 ===${NC}"

ERROR_COUNT=0
for cs_file in "$OUTPUT_DIR"/*.cs; do
    if [ -f "$cs_file" ]; then
        # 기본 검증: 파일이 비어있지 않은지
        if [ ! -s "$cs_file" ]; then
            echo -e "${RED}✗ $(basename "$cs_file"): 파일이 비어있습니다${NC}"
            ((ERROR_COUNT++))
        else
            # namespace 검증
            if ! grep -q "namespace" "$cs_file"; then
                echo -e "${YELLOW}! $(basename "$cs_file"): namespace가 없습니다${NC}"
            else
                echo -e "${GREEN}✓ $(basename "$cs_file")${NC}"
            fi
        fi
    fi
done

if [ $ERROR_COUNT -gt 0 ]; then
    echo -e "${RED}검증 실패: $ERROR_COUNT 개의 파일에 문제가 있습니다.${NC}"
    exit 1
fi

# Unity 프로젝트 메타 파일 생성 여부 확인
echo ""
echo -e "${GREEN}=== Unity 메타 파일 ===${NC}"
META_MISSING=0
for cs_file in "$OUTPUT_DIR"/*.cs; do
    meta_file="$cs_file.meta"
    if [ ! -f "$meta_file" ]; then
        echo -e "${YELLOW}! $(basename "$cs_file"): .meta 파일이 없습니다${NC}"
        echo "  Unity 에디터에서 자동 생성됩니다."
        ((META_MISSING++))
    fi
done

if [ $META_MISSING -eq 0 ]; then
    echo -e "${GREEN}모든 .meta 파일이 존재합니다.${NC}"
fi

# 요약
echo ""
echo -e "${GREEN}=== 완료 ===${NC}"
echo "총 ${SUCCESS_COUNT}개의 프로토콜 파일이 성공적으로 생성되었습니다."
echo "출력 디렉토리: $OUTPUT_DIR"
echo ""
echo "다음 단계:"
echo "  1. Unity 에디터를 열어 생성된 파일을 확인하세요."
echo "  2. GameServer 프로젝트를 빌드하여 컴파일 오류가 없는지 확인하세요."
echo "  3. 클라이언트와 서버 코드에서 새로운 네임스페이스를 사용하세요:"
echo "     - MinecraftGame.Common (공통 타입)"
echo "     - EnhancedMinecraftProtocol (게임 프로토콜)"
echo "     - Game.Core, Game.Move, Game.World 등 (기타 프로토콜)"

exit 0
