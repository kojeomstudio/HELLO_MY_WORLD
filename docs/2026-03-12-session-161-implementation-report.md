# Session 161 Implementation Report (2026-03-12)

## Summary
세션 161에서는 서버/클라이언트 월드 생성 파이프라인에 **Aquifer Conduit Exchange Bridge**를 추가하고, 월드맵 큐 제어 공통 정책에 **aquifer-conduit queue scale(v84)**를 도입해 동굴/강/호수 연동 안정성과 맵 스트리밍 안정성을 함께 개선했습니다.

## Core / Content / Utility 분류 및 순차 구현
순차 구현 기준은 `config/minecraft_feature_client_server_core_content_util_2026-03-12-session-161.json`(및 미러 파일)로 갱신했습니다.

1. Core
- Shared DLL 기준 상향 (`HydrologySignature=v83`, `MapControlProfileVersion=87`)
- WorldMap queue 공통 정책 확장 (`WorldMapQueuePolicy.ComputeAquiferConduitExchangeQueueScale`)

2. Content
- 서버 지형 생성: `ImprovedTerrainCoordinator`에 `ApplyAquiferConduitExchangeBridge` 추가
- 클라이언트 지형 생성: `WorldMapController.EnhancedTerrainGenerator`에 동일 브리지 로직 추가
- cave/river/lake coupling 후반부에 신규 브리지 단계 연결

3. Utility
- queue policy JSON v40 상향 및 서버/클라 파라미터 동기화
- 더미 클라이언트/프로토 프로브 최소 profile version 가드 87로 상향
- world/profile/config 미러 파일 동기화

## Key Code Changes
- `GameCommon/World/WorldMapQueuePolicy.cs`
  - v84 `ComputeAquiferConduitExchangeQueueScale` 추가
- `GameServer/World/WorldMapController.cs`
  - adaptive queue scale 결합식에 aquifer-conduit scale 반영
- `GameServer/World/WorldMapControlManager.cs`
  - 동적 queue slack/limit/near-keep 계산식에 aquifer-conduit scale 반영
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
  - `ApplyAquiferConduitExchangeBridge` 추가 및 파이프라인 연결
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - queue scale wrapper + 결합식 반영
  - `ApplyAquiferConduitExchangeBridge` 추가 및 파이프라인 연결
- `GameCommon/World/SharedFeatureCatalog.cs`
  - signature/profile baseline: `v83` / `87`

## Config / Data-driven Updates (JSON)
- world config baseline 상향
  - `config/world.json`
  - `GameServer/config/world.json`
  - `Assets/StreamingAssets/world-config.json`
- map-control queue policy v40 상향
  - `config/world_map_control_queue_policy.json`
  - `GameServer/config/world_map_control_queue_policy.json`
  - `Assets/StreamingAssets/world_map_control_queue_policy.json`
- map-control profile 재생성 및 미러
  - `GameServer/config/world_map_control_profile.json`
  - `config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`
- feature catalog(session 161)
  - `config/minecraft_feature_client_server_core_content_util_2026-03-12-session-161.json`
  - `GameServer/config/minecraft_feature_client_server_core_content_util_2026-03-12-session-161.json`
  - `Assets/StreamingAssets/minecraft_feature_client_server_core_content_util_2026-03-12-session-161.json`

## Protobuf / Packet Review
- 공통 fingerprint 검증은 성공했습니다.
- required packet round-trip은 통과했습니다.
- optional packet(`MultiBlockChange`, `InventoryUpdate`, `ItemUse` 등)는 현재 레지스트리 미등록 상태이며 기존 정책대로 warning으로 유지됩니다.

## Validation Commands
- `dotnet build SharedProtocol/SharedProtocol.csproj` ?
- `dotnet build GameServer/GameServer.csproj` ?
- `dotnet build Tools/DummyMinecraftClient/DummyMinecraftClient.csproj` ?
- `dotnet run --project GameServer -- --generate-map-profile` ?
- `dotnet run --project GameServer -- --proto-probe` ? (optional packet warning only)
- `dotnet run --project Tools/DummyMinecraftClient/DummyMinecraftClient.csproj -- --config config/dummy_minecraft_client.json --required-only --no-print-bindings` ?
- `dotnet test GameServer/GameServer.csproj --no-build` ?

## Notes
- protobuf optional 패킷 warning을 제거하려면 `.proto` 및 `ProtocolRegistry` 바인딩을 함께 갱신해야 합니다.
- 이번 세션은 required 경로 안정성 유지와 map-control/worldgen 연동 강화에 초점을 맞췄습니다.

