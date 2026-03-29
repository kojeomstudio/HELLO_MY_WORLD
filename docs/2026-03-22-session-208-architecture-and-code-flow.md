# Session 208 Architecture and Code Flow

## Session Date
- 2026-03-22 (KST)

## Overview
This session focused on cleaning up outdated files and unused code based on minetest reference analysis. Removed 60 files including session config files, deprecated P2P networking code, and experimental scripts.

## Architecture Cleanup

### Removed Components

#### 1. P2P Networking (Deprecated)
The following files were removed as P2P networking was replaced by client-server architecture:
```
KojeomNetWorkSpace/KojeomNet/FrameWork/Soruces/
├── P2PCharacterSynchronizer.cs   (removed)
├── P2PLevelSynchronizer.cs       (removed)
├── P2PMessage.cs                 (removed)
└── PeerToPeerNetwork.cs          (removed)
```

**Remaining networking code:**
```
KojeomNetWorkSpace/KojeomNet/FrameWork/Soruces/
├── BufferManager.cs
├── CPacket.cs
├── Connector.cs
├── DoubleBufferingQueue.cs
├── HeartbeatSender.cs
├── ILogicQueue.cs
├── IMessageDispatcher.cs
├── IPeer.cs
├── ListenManager.cs
├── Logger.cs
├── LogicMessageEntry.cs
├── MessageResolver.cs
├── NetworkServiceManager.cs
├── ServerUserManager.cs
├── SocketAsyncEventArgsPool.cs
├── UserToken.cs
└── Utils.cs
```

#### 2. Session Config Files (55 files)
All `minecraft_feature_*-session-*.json` files removed from:
- `GameServer/config/`
- `Assets/StreamingAssets/`

These were historical session snapshots no longer needed.

#### 3. Experimental Code
```
Assets/MyAssets/Scripts/Experiments/
└── TEST_Experiment.cs  (removed)
```

## Current Project Structure

### .NET Projects
```
C:\Workspace\HelloMyWorld_repo\
├── SharedProtocol/          # Protocol messages (net6.0)
├── GameServer/              # Game server (net6.0)
├── GameCommon/              # Shared game logic (netstandard2.1)
├── GameServer.Launcher/     # Server launcher
├── KojeomNetWorkSpace/      # Networking library
├── MapGeneratorLib/         # Map generation
└── Tools/
    ├── GameDataTemplateExporter/  # MD → JSON converter (net8.0)
    └── DummyMinecraftClient/      # Test client (net8.0)
```

### Unity Client
```
Assets/
├── MyAssets/Scripts/
│   ├── GameWorld/       # World management
│   ├── Network/         # Client networking
│   ├── UI/              # User interface
│   └── Editor/Automation/
│       └── UnityCiCommandlet.cs  # CI compile/test
├── StreamingAssets/
│   ├── game-data/       # JSON game data
│   ├── world-config.json
│   └── config_parity_manifest.json
└── Generated/Protobuf/  # Protocol buffers
```

### Configuration Files (After Cleanup)
```
config/
├── biomes.json
├── blocks.json
├── client_config.json
├── config_parity_manifest.json
├── dummy_minecraft_client.json
├── enhanced-terrain-config.json
├── enhanced_terrain_generation.json
├── enhanced_world_map_control_client.json
├── enhanced_world_map_control_server.json
├── gameplay.json
├── game-data/
│   ├── items.json
│   ├── recipes.json
│   ├── monsters.json
│   ├── npcs.json
│   └── character_stats.json
├── hunger_config.json
├── item_categories.json
├── items.json
├── items_config.json
├── network.default.json
├── proto_reference_report.json
├── protocol_dummy_client.json
├── recipes.json
├── server.json
└── server_config.json
```

## Code Flow Summary

### Server Startup
```
Program.cs → LoadConfig() → InitializeWorld() → StartTcpServer()
    → SessionManager.RegisterHandlers()
    → GameDataCatalog.LoadRecipes()
```

### Client Game Data Loading
```
UnityClient → GameDataManager.LoadFromStreamingAssets()
    → Parse items.json, recipes.json
    → Build ItemRegistry, RecipeRegistry
```

### Crafting Flow (from minetest patterns)
```
PlayerActionHandler.OnCraftRequest()
    → RecipeManager.FindMatchingRecipe(input)
    → ValidateIngredients() → ConsumeIngredients()
    → ApplyReplacements() → GiveResults()
```

## Minetest Reference Patterns Applied

### Key Adaptations
1. **Item Groups**: `groups: ["tool", "pickaxe", "wood"]` → crafting ingredient matching
2. **Recipe Methods**: NORMAL (shaped/shapeless), COOKING, FUEL
3. **Replacements**: `{ "from": "milk_bucket", "to": "bucket" }` for container return

### Recommended Future Enhancements
1. Add `tool_capabilities` for mining speed calculation
2. Add `drop` tables with rarity for node drops
3. Add `node` type items with `tiles`, `drawtype` properties
