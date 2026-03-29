# Session 183 Architecture and Code Flow (2026-03-18)

## 1. System Overview
This session revalidated the current server/client architecture and data-driven content pipeline without changing protocol contracts.

## 2. Core Components

### 2.1 Server Runtime (`GameServer/`)
- `Program.cs`: startup, dispatcher registration, selftest entry
- `Handlers/`: gameplay and packet handlers
- `SessionManager.cs`: player session lifecycle/state
- `World/` + `World/Generation/`: terrain, chunk generation, hydrology and cave systems

### 2.2 Protocol Layer (`SharedProtocol/`)
- `MinecraftMessageDispatcher.cs`: packet dispatch and handler invocation
- `ProtocolRegistry.cs`: protocol enum/message binding table
- `Session.cs`: low-level session read/write envelope handling

### 2.3 Client/Unity Assets (`Assets/`)
- `MyAssets/Scripts/GameWorld/`: chunk/world presentation
- `MyAssets/Scripts/Network/`: client message flow
- `Generated/Protobuf/`: generated DTOs for available protobuf messages

### 2.4 Data-Driven Assets and Tooling
- Template source: `design/templates/game-data-template.md`
- Export tool: `Tools/GameDataTemplateExporter` (`.NET 8`)
- Runtime datasets: `config/game-data/*.json`
- Runtime-generated parity/probe artifacts:
  - `config/world_map_control_profile.json`
  - `GameServer/config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`
  - `reports/proto_probe_report.json`

## 3. Operational Flow

### 3.1 Data Authoring/Export
`design/templates/game-data-template.md`
-> `Tools/GameDataTemplateExporter`
-> `config/game-data/*.json`
-> server startup validation (`required=5`)

### 3.2 Startup Validation Path
`Program.Main`
-> protocol descriptor fingerprint check
-> queue/profile parity checks
-> game-data contract validation
-> handler coverage/probe report generation

### 3.3 Runtime Selftest Path
`dotnet run --project GameServer -- --selftest`
-> boots server and test client
-> exercises login/move/chat/ping/block-change flow
-> writes proto probe report and reference report
-> exits cleanly (exit code 0)

## 4. Current Validation Snapshot
- Proto binding coverage: `14/54`
- Optional handler coverage: `7/10`
- World map queue policy parity: `version=44`
- World map control profile parity: `version=94`
- Game-data required datasets: `5/5` validated

## 5. Observed Risks / Follow-Ups
- Selftest still logs `Unexpected response type` in several test-client steps.
- Optional EnhancedMinecraft bindings remain partially unregistered (`MultiBlockChange`, `ItemPickup`, `EntityInteract`, etc.).
- Mirror directories for `game-data` (`GameServer/config/game-data`, `Assets/StreamingAssets/game-data`) are still absent and logged as warnings.
