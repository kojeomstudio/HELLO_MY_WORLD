# Session 187 Architecture and Code Flow (2026-03-19)

## 1. Scope
이번 세션은 `work/work.md` 지시에 따라 optional packet 3종(`MultiBlockChange`, `ItemPickup`, `EntityInteract`)의 서버 처리 경로를 보강하고, 해당 흐름을 문서화한다.

## 2. Changed Architecture Nodes

### 2.1 Shared Protocol
- 신규 파일: `SharedProtocol/MinecraftOptionalMessages.cs`
- 역할:
  - `MultiBlockChangeRequest/ResponseMessage`
  - `ItemPickupRequest/ResponseMessage`
  - `EntityInteractRequest/ResponseMessage`
  - `EntityInteractionType`
- 의미:
  - Enhanced descriptor에 아직 없는 optional 패킷을 legacy protobuf-net 계약으로 수용

### 2.2 GameServer Optional Handlers
- 변경 파일: `GameServer/Handlers/MinecraftOptionalHandlers.cs`
- 추가 핸들러:
  - `MinecraftMultiBlockChangeHandler`
  - `MinecraftItemPickupHandler`
  - `MinecraftEntityInteractHandler`
- 동작:
  - 역직렬화 성공 시 acknowledged 응답 전송
  - 서버 권위 정책에 맞게 "허용/무시" 경로를 표준화

### 2.3 Dispatcher Registration
- 변경 파일: `GameServer/GameServer.cs`
- `_minecraftDispatcher.RegisterHandler(...)`에 3종 메시지 타입 등록 추가

### 2.4 Probe Fallback Coverage
- 변경 파일: `GameServer/Testing/DummyProtocolClient.cs`
- `TryCreateLegacyOptionalPayload`에 3종 payload 생성 경로 추가
- 결과:
  - optional 패킷의 프로토타입 미생성 구간을 legacy fallback으로 검증 가능

## 3. Runtime Code Flow (Optional Packet)
1. `Session.ReceiveAsync`가 미정의 `MessageType`을 raw payload(`byte[]`)로 전달
2. `GameServer.HandleClientAsync`가 `MinecraftMessageType` enum 매핑 후 `_minecraftDispatcher.DispatchMinecraftMessageAsync` 호출
3. `MinecraftMessageHandlerBase<T>`가 protobuf-net 역직렬화 수행
4. 신규 optional handler가 메시지 처리 후 same raw type으로 응답 송신
5. `DummyProtocolClient`는 fallback payload로 round-trip/네트워크 probe 검증

## 4. Minetest Mapping (Required Reference)
- `minetest_project/src/network/serverpackethandler.cpp`
  - 상호작용(`handleCommand_Interact`)에서 권한/거리/생존 상태를 서버에서 검증
  - 상호작용 결과를 서버 기준으로 `SendInventory`, `SetBlockNotSent`, `ResendBlockIfOnWire`로 보정
- `minetest_project/src/server.cpp`
  - active object add/remove 및 message routing 루프 유지
- `minetest_project/src/serverenvironment.cpp`
  - active object step, inventory/mapblock 변경 반영을 서버 틱 루프에서 일괄 관리

## 5. Post-Change Status
- Optional handler coverage: `10/10`
- Missing optional prototypes in probe: `0`
- Optional message registration 자체(Enhanced descriptor 바인딩)는 여전히 미완이며 INFO/WARN 로그로 추적됨

## 6. Next Step
- optional 3종을 Enhanced descriptor(required 후보)로 승격하려면 `proto/enhanced_minecraft_game.proto` 메시지 정의 추가 + generated DTO/`ProtocolRegistry` 동기화가 필요하다.
