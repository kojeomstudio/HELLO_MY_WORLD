# Minecraft Feature Map (2026-01-15 Session 02)

Source JSON: `config/minecraft_feature_client_server_core_content_util_2026-01-15-session-02.json`

## Core
- **Server** — `world-map-control-parity` (done, order 1): `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapControlProfile.cs`, `config/world_map_control_profile.json`. Map-control generation signatures now include a pipeline version stamp so Unity reloads when hydrology/cave algorithms change.
- **Server** — `hydrology-envelope` (done, order 2): `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`. Rivers/lakes receive seam-aware envelope smoothing and continuity boosts before carving.
- **Server** — `protocol-registry-validation` (in-progress, order 3): `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Network/EnhancedProtocolHandler.cs`. Descriptor fingerprint checks remain; next step is expanded handler coverage.
- **Client** — `world-map-preview-parity` (done, order 1): `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`. Unity previews mirror hydrology envelope logic and pipeline signature.
- **Client** — `protocol-client-bindings` (in-progress, order 3): `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`, `Assets/Generated/Protobuf`. Startup runs ProtoRuntime + ProtoDiagnostics alongside registry validation to guard stale DTO references.

## Content
- **Server** — `cave-river-lake-coherence` (done, order 2): `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`. Continuity-aware depths, edge cave plugs, and smoother outflow channels reduce chunk seams for caves/rivers/lakes.
- **Client** — `map-visualization` (in-progress, order 4): `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`. Preview masks include continuity boosts; UI wiring for wetlands/coasts still pending.

## Utility
- **Server** — `data-driven-config-refresh` (in-progress, order 0): `config/world.json`, `config/world_map_control_profile.json`. JSON stays the single source; pipeline signature forces cache invalidation when configs drift.
- **Client** — `streaming-profile-hotload` (in-progress, order 0): `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/StreamingAssets/world-config.json`. StreamingAssets reload reacts to signature/version changes to avoid stale preview chunks.

## Sequencing Notes
- Execute core parity and hydrology envelope first to unblock content coherence, then tighten protocol validation. Utility hotload/config refresh remains continuous and should run after each regeneration of `world_map_control_profile.json`.
