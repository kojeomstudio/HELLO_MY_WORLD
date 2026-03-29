# Core/Content/Utility Feature Inventory (2025-12-28)

- Source JSON: `minecraft_feature_core_content_util.json`; config mirror: `config/minecraft_feature_core_content_util.json`.
- Phases: Critical (`core_001`-`core_005`), Essential (`content_001`/`content_002`/`content_003`/`content_006`), Advanced (`content_004`/`content_005`), Polish (`util_001`-`util_005`).
- Data model: IDs and components stay data-driven; keep updates in JSON so Unity StreamingAssets and the server share the same view.
- Priority focus for this iteration: hydrology edge damping for caves/rivers/lakes and protocol registry validation to keep client/server parity.

## Core
- `core_001` World Generation: terrain + hydrology + cave/river/lake passes (`WorldManager`, `MapGeneratorLib`).
- `core_002` Networking & Protocol: Google.Protobuf contracts + handler registry checks (`ProtocolStandardization.ValidateProtocolImplementation`).
- `core_003` Player Systems: movement/auth/sync; health & hunger next.
- `core_004` Block System: placement/change handlers; future state system and block entities.
- `core_005` Chunk Management: load/unload/render; multithreaded generation backlog.
- `core_006` Configuration System: JSON-driven world/server/client settings.

## Content
- `content_001` Items & Equipment; `content_002` Crafting; `content_003` Mobs & Entities.
- `content_004` Structures & Buildings (villages/dungeons/strongholds).
- `content_005` World Features (biomes, weather, dimensions).
- `content_006` Ores & Resources with JSON vein configs.

## Utilities
- `util_001` UI/UX (inventory, map, settings); `util_002` Server Management (permissions, backups).
- `util_003` Development Tools (debugging, editors); `util_004` Data Management (JSON configs, SQLite, backups).
- `util_005` Performance & Optimization (chunk loading, multithreading, memory/bandwidth).
