# Minecraft-Clone Game Design (2026-03-16)

## 1. Product Goal
- Build a server-authoritative multiplayer sandbox with Minecraft-style block editing, exploration, and progression.
- Keep world simulation deterministic between server and Unity client.
- Keep game content data-driven so content updates do not require code rebuilds.

## 2. Core Experience Pillars
- Exploration: biome-aware terrain, cave/river/lake topology, map visibility progression.
- Building: low-latency place/break loop with authoritative server validation.
- Survival/Progression: health/hunger, crafting, inventory, combat, respawn loop.
- Social Multiplayer: rooms, chat, shared world edits, synchronized time/weather.

## 3. Core Gameplay Loop
1. Login and join room/world.
2. Stream nearby chunks from server and local preview cache.
3. Gather resources and update inventory.
4. Craft better tools/items from recipe data.
5. Fight mobs, survive hazards, and progress character stats.
6. Expand builds and repeat with better mobility/combat options.

## 4. World/Networking Rules
- Server is always authoritative for player state, block edits, entity lifecycle, and combat outcomes.
- Client performs local interpolation and preview only.
- Protocol contracts must be validated on startup via `SharedProtocol.EnhancedMinecraft`.
- Optional packets stay optional until handler and protobuf binding are both validated.

## 5. Content Scope (MVP -> Next)
- MVP:
  - Block place/break, chunk streaming, inventory, recipes, health/hunger, respawn.
  - Basic hostile/passive NPCs and simple loot tables.
  - Time/weather sync, room-based multiplayer.
- Next:
  - Biome quest hooks, structure generation, advanced mob AI behavior packs.
  - Administrative tools for moderation, telemetry dashboards, and data hot-reload.

## 6. Data-Driven Requirement
- All runtime content data must be JSON (`items`, `recipes`, `monsters`, `npcs`, `character_stats`).
- Authoring templates are Markdown and converted through a C# tool (`Tools/GameDataTemplateExporter`).
- Configuration/runtime knobs remain in JSON under `config/`, `GameServer/config/`, `Assets/StreamingAssets/`.

## 7. Development Guardrails
- Use design documents in `design/` as source-of-truth before coding core/content features.
- Compile/test gates:
  - `dotnet build SharedProtocol/SharedProtocol.csproj`
  - `dotnet build GameServer/GameServer.csproj`
  - `dotnet run --project GameServer -- --selftest`
- Tooling projects must target .NET 8.0~9.0.

