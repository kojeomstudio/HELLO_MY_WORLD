# Session 185 Architecture and Code Flow (2026-03-18)

## 1. 목적
`work/work.md` 지시에 맞춰 현재 서버/클라이언트 구조와 코드 흐름을 재정리하고, 반드시 참조 대상으로 지정된 `minetest_project` 구조를 기준선으로 매핑한다.

## 2. 현재 프로젝트 핵심 구조

### 2.1 Server Runtime (`GameServer/`)
- 진입점: `Program.cs`
  - proto contract/fingerprint 검증
  - world-map queue/profile parity 검사
  - game-data dataset 필수 항목 검사
  - `--server`, `--selftest`, `--proto-probe` 실행 경로 분기
- 세션/접속: `SessionManager.cs`, `GameServer.cs`
- 핸들러: `Handlers/*.cs` (로그인, 이동, 채팅, 월드블록, 청크, 인벤토리, 액션 등)
- 월드: `World/` + `World/Generation/`
  - 지형/하천/동굴 생성 파이프라인
  - 월드맵 제어 프로파일 적용

### 2.2 Shared Protocol (`SharedProtocol/`)
- `EnhancedMinecraft/ProtocolRegistry.cs`: enum -> generated protobuf type 바인딩
- `EnhancedMinecraft/ProtocolValidator.cs`: descriptor/field/binding 정합성 검증
- `MinecraftMessageDispatcher.cs`: 핸들러 등록 및 런타임 디스패치

### 2.3 Unity Client (`Assets/`)
- 월드/청크 표시: `Assets/MyAssets/Scripts/GameWorld/`
- 네트워크 흐름: `Assets/MyAssets/Scripts/Network/`
- protobuf DTO: `Assets/Generated/Protobuf/`
- 스트리밍 구성: `Assets/StreamingAssets/world-map-control.json`

### 2.4 데이터 드리븐 파이프라인
- 템플릿 원본: `design/templates/game-data-template.md`
- 추출 도구(.NET 8): `Tools/GameDataTemplateExporter`
- 런타임 JSON: `config/game-data/*.json`

## 3. 실행 코드 흐름 (요약)

### 3.1 시작 검증 흐름
`Program.Main`
-> `ProtoRuntime.EnsureInitialized`
-> `ProtocolValidator.ValidateEnhancedContracts`
-> Queue/Profile parity check
-> GameData dataset check
-> 서버 실행 또는 selftest 실행

### 3.2 콘텐츠 데이터 흐름
`design/templates/game-data-template.md`
-> `Tools/GameDataTemplateExporter`
-> `config/game-data/*.json`
-> 서버 시작 시 필수 데이터셋 검증

### 3.3 selftest 흐름
`dotnet run --project GameServer -- --selftest`
-> 서버 시작
-> 테스트 클라이언트 로그인/이동/채팅/핑/블록변경 시퀀스
-> proto probe 리포트/참조 리포트 갱신
-> 정상 종료 (exit code 0)

## 4. Minetest 참조 매핑

| Minetest reference | 의미 | 본 프로젝트 대응 |
|---|---|---|
| `minetest_project/src/main.cpp` | 엔진 시작/옵션/서버 실행 분기 | `GameServer/Program.cs` |
| `minetest_project/src/server.cpp` | 서버 스텝 루프/접속 처리/월드 업데이트 | `GameServer/GameServer.cs`, `SessionManager.cs` |
| `minetest_project/src/client/client.cpp` | 클라이언트 상태/패킷 처리 흐름 | `Assets/MyAssets/Scripts/Network/*`, `GameWorld/*` |
| `minetest_project/src/network/networkprotocol.h` | 네트워크 메시지/프로토콜 명세 | `SharedProtocol/*`, `proto/*.proto` |
| `minetest_project/src/emerge.cpp` | 맵 생성 스레드/큐 관리 | `GameServer/World/Generation/*`, `WorldMapControl*` |
| `minetest_project/doc/world_format.md` | 월드 저장/맵 블록 포맷 개념 | `GameServer`의 월드/DB 및 청크 직렬화 설계 참고 기준 |
| `minetest_project/doc/protocol.txt` | 초기 핸드셰이크/프로토콜 흐름 | 본 프로젝트 protobuf 기반 접속 시퀀스 설계 참고 기준 |

## 5. 현재 상태와 리스크
- 장점:
  - 서버 권위형 구조, selftest 자동 검증, 데이터 드리븐 파이프라인이 유지되고 있음
- 확인된 갭:
  - protobuf 바인딩 커버리지 `14/54`
  - optional packet 바인딩/핸들러 일부 미등록
  - selftest에서 `Unexpected response type` 로그가 비치명으로 잔존

## 6. 결론
현재 구조는 minetest의 핵심 축(서버 권위, 월드 생성 파이프라인, 명시적 네트워크 프로토콜)을 참조한 형태로 정착되어 있다. 다음 단계는 optional protobuf 패킷의 바인딩/핸들러 확장과 selftest 응답 타입 정합성 개선이다.
