# Protobuf Regeneration Skill

당신은 HELLO_MY_WORLD 마인크래프트 모작 프로젝트의 프로토버퍼 스키마를 재생성하는 전문가입니다.

## 작업 단계

1. **프로토 파일 검증**
   - `/home/user/HELLO_MY_WORLD/proto/` 디렉토리의 모든 .proto 파일 확인
   - 문법 오류 및 호환성 검사

2. **프로토버퍼 컴파일**
   - 다음 명령어로 C# 코드 생성:
   ```bash
   cd /home/user/HELLO_MY_WORLD
   protoc -I proto --csharp_out=Assets/Generated/Protobuf \
     proto/game_auth.proto \
     proto/game_core.proto \
     proto/game_move.proto \
     proto/game_chat.proto \
     proto/game_world.proto \
     proto/game_diag.proto \
     proto/enhanced_minecraft_game.proto
   ```

3. **생성된 파일 검증**
   - `Assets/Generated/Protobuf/` 디렉토리 확인
   - 생성된 C# 파일 목록:
     - EnhancedMinecraftGame.cs
     - GameAuth.cs
     - GameChat.cs
     - GameCore.cs
     - GameDiag.cs
     - GameMove.cs
     - GameWorld.cs

4. **SharedProtocol 프로젝트 재빌드**
   - 생성된 프로토버퍼 파일이 SharedProtocol.csproj에 링크되어 있는지 확인
   - `dotnet build /home/user/HELLO_MY_WORLD/SharedProtocol/SharedProtocol.csproj`

5. **GameServer 프로젝트 재빌드**
   - `dotnet build /home/user/HELLO_MY_WORLD/GameServer/GameServer.csproj`

## 주의사항

- 프로토버퍼 파일 수정 시 반드시 버전 호환성 유지
- 기존 메시지 타입의 필드 번호 변경 금지
- 새로운 필드는 항상 끝에 추가
- deprecated 필드는 주석으로 표시하되 삭제하지 않음

## 트러블슈팅

- **protoc 명령어를 찾을 수 없는 경우**: Protocol Buffers 컴파일러 설치 필요
- **C# 코드 생성 실패**: proto 파일의 문법 오류 확인
- **빌드 실패**: 생성된 C# 파일의 네임스페이스 확인
