# Session 195: 아키텍처 및 코드 흐름 분석 - 블록 ID 프로토콜 정규화

## 작성 일자
2026-03-19

## 목적
`minetest_project`의 `NameIdMapping` 아이디어를 참조해, 서버 내부 블록 ID 저장 체계는 유지하면서 네트워크 경계에서 프로토콜 ID를 정규화하는 계층을 도입한다.

---

## 1. minetest 참조 근거

### 1.1 양방향 매핑 핵심 (`nameidmapping.h`)
- `id -> name`
- `name -> id`
- 런타임 질의/직렬화 지원

### 1.2 현재 프로젝트에의 적용 해석
- minetest처럼 "내부 표현"과 "외부 전송 표현"을 분리한다.
- 서버 월드 저장/생성 로직은 기존 enum 기반 내부 표현 유지.
- 네트워크 송수신 지점에서만 프로토콜 ID로 변환한다.

---

## 2. 변경 전 문제 흐름

1. 클라이언트 요청 `WorldBlockChangeRequest.BlockType`를 서버 enum ID로 직접 캐스팅.
2. 월드 동기화 브로드캐스트도 입력 ID를 그대로 재전송.
3. 청크 전송 시 `ChunkData.ToBytes()` 원본(서버 내부 ID)을 그대로 프로토콜 페이로드로 사용.

결과: 서버 내부 ID와 공유 ID가 불일치할 때 클라이언트 해석 오류 가능.

---

## 3. 변경 후 코드 흐름

## 3.1 요청 경로 (`WorldBlockHandler`)
1. `BlockTypeProtocolMapper.TryProtocolToServer(...)`로 입력 ID를 서버 내부 타입으로 해석.
2. 서버 월드 업데이트는 내부 enum으로 수행.
3. 브로드캐스트는 `BlockTypeProtocolMapper.ToProtocol(...)` 결과(정규화 ID) 사용.

## 3.2 동기화 큐 경로 (`WorldSynchronizationManager`)
1. 큐에 쌓는 `WorldBlockChangeRequest`의 `BlockType`을 정규화 ID로 고정.
2. 즉시 월드 반영은 해석된 서버 내부 enum으로 수행.

## 3.3 청크 전송 경로 (`MinecraftChunkHandler`)
1. `ChunkData.ToBytes()`로 얻은 서버 내부 블록 배열 유지.
2. Enhanced payload 생성 시 `ConvertChunkBlockDataToProtocol(...)` 적용.
3. Legacy 응답의 `CompressedBlockData`는 기존 값을 유지해 저장/기존 흐름 호환성을 보존.

---

## 4. 구현 파일

- `GameServer/Models/BlockTypeProtocolMapper.cs`
- `GameServer/Handlers/WorldBlockHandler.cs`
- `GameServer/World/WorldSynchronizationManager.cs`
- `GameServer/Handlers/MinecraftChunkHandler.cs`

---

## 5. 정합성/호환성 판단

- 장점
  - 네트워크 상 블록 ID 해석 일관성 개선
  - 서버 저장 포맷 즉시 변경 없이 전송 계층만 개선
  - minetest식 매핑 계층(내부/외부 분리)을 부분 반영

- 제한
  - 서버 내부 enum 자체 통합(완전 단일화)은 후속 단계 필요
  - Legacy 클라이언트의 전체 전환 정책은 별도 결정 필요

---

## 6. 검증 결과

- `dotnet build SharedProtocol/SharedProtocol.csproj` 성공
- `dotnet build GameServer/GameServer.csproj` 성공 (기존 경고 다수, 신규 오류 없음)
