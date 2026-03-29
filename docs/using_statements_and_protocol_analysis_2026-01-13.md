# Using Statements and Protocol Analysis - 2026-01-13

## Executive Summary

This document analyzes the current state of using statements and protobuf protocol usage across the server and client codebases, identifying issues and providing recommendations for fixes.

## Key Findings

### 1. Namespace Inconsistencies

#### Issue: `GameProtocol` Namespace Confusion
**Problem:** The term `GameProtocol` is used inconsistently:
- In [`Assets/Scripts/Networking/Protocol/GameProtocol.cs`](Assets/Scripts/Networking/Protocol/GameProtocol.cs), it's a **namespace** containing AI-related classes
- In [`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:5), it's referenced as a namespace with `using GameProtocol;`
- In [`Assets/Scripts/Networking/NetworkManager.cs`](Assets/Scripts/Networking/NetworkManager.cs:5), it's referenced as a namespace with `using GameProtocol;`
- However, in [`GameServer/GameServer.cs`](GameServer/GameServer.cs:11), it's also referenced with `using GameProtocol;`

**Impact:** This creates confusion about which `GameProtocol` is being referenced and can lead to compilation errors.

#### Issue: `GameCommon` Namespace References
**Problem:** Files reference `using GameCommon;` but the actual namespaces are:
- `GameCommon.DataDriven`
- `GameCommon.Configuration`
- `GameCommon.Blocks`
- `GameCommon.World` (in docs only)

**Affected Files:**
- [`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7) uses `using GameCommon;`

**Impact:** The `GameCommon` namespace doesn't exist as a top-level namespace, causing compilation errors.

### 2. Vector3 Type Confusion

**Problem:** Multiple `Vector3` types exist across the codebase:

| Location | Type | Namespace | Usage |
|----------|-------|-----------|-------|
| [`Assets/Scripts/Networking/Protocol/GameProtocol.cs`](Assets/Scripts/Networking/Protocol/GameProtocol.cs:22) | `GameProtocol.Vector3` | `GameProtocol` | Protocol messages |
| [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:428) | `GameServerApp.Vector3` | `GameServerApp` (implicit) | Server internal |
| [`SharedProtocol/EnhancedMinecraft/`](SharedProtocol/EnhancedMinecraft/) | `SharedProtocol.Vector3` | `SharedProtocol` | Shared protocol |
| [`Assets/Generated/Protobuf/Common.cs`](Assets/Generated/Protobuf/Common.cs) | `MinecraftGame.Common.Vector3` | `MinecraftGame.Common` | Protobuf generated |

**Aliasing Used:**
- [`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:6-7):
  ```csharp
  using ProtoVector3 = GameProtocol.Vector3;
  using ServerVector3 = GameServerApp.Vector3;
  ```
- [`GameServer/Systems/CommandSystem.cs`](GameServer/Systems/CommandSystem.cs:6-7):
  ```csharp
  using ServerVector3 = GameServerApp.Vector3;
  using ProtoVector3 = SharedProtocol.Vector3;
  ```
- [`GameServer/Handlers/PlayerAttackHandler.cs`](GameServer/Handlers/PlayerAttackHandler.cs:5):
  ```csharp
  using ProtoVector3 = SharedProtocol.Vector3;
  ```

**Impact:** Multiple Vector3 types require careful aliasing and can lead to type confusion.

### 3. Protobuf Protocol Inconsistencies

#### Multiple Protocol Namespaces

| Namespace | Source | Status |
|-----------|--------|--------|
| `MinecraftGame.Common` | [`proto/common.proto`](proto/common.proto) | Generated |
| `Game.Auth` | [`proto/game_auth.proto`](proto/game_auth.proto) | Generated |
| `Game.Chat` | [`proto/game_chat.proto`](proto/game_chat.proto) | Generated |
| `Game.Diag` | [`proto/game_diag.proto`](proto/game_diag.proto) | Generated |
| `Game.Core` | [`proto/game_core.proto`](proto/game_core.proto) | Generated |
| `Game.Move` | [`proto/game_move.proto`](proto/game_move.proto) | Generated |
| `Game.World` | [`proto/game_world.proto`](proto/game_world.proto) | Generated |
| `EnhancedMinecraftProtocol` | [`proto/enhanced_minecraft_game.proto`](proto/enhanced_minecraft_game.proto) | Generated |

#### Mixed Serialization Libraries

**Protobuf-net (ProtoBuf):**
- Used in [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:7) with `using ProtoBuf;`
- Used for legacy protocol serialization

**Google.Protobuf:**
- Used in [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:8) with `using Google.Protobuf;`
- Used for enhanced protocol serialization
- Used in [`Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`](Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:5)

**Impact:** Dual serialization approach creates complexity and potential for bugs.

### 4. Conditional Compilation Issues

**Problem:** Code uses conditional compilation to support multiple protocols:

```csharp
#if HMW_PROTO
using Game.Move;
#endif
```

**Affected Files:**
- [`Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`](Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:10-12)

**Impact:** Creates maintenance burden and untested code paths.

### 5. Missing or Incorrect Using Statements

#### Files with Issues:

1. **[`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7)**
   - Issue: `using GameCommon;` - namespace doesn't exist
   - Fix: Should use specific namespaces like `GameCommon.DataDriven` or `GameCommon.Configuration`

2. **[`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:5)**
   - Issue: `using GameProtocol;` - ambiguous reference
   - Fix: Should reference the specific namespace or class

3. **[`Assets/Scripts/Networking/NetworkManager.cs`](Assets/Scripts/Networking/NetworkManager.cs:5)**
   - Issue: `using GameProtocol;` - ambiguous reference
   - Fix: Should reference the specific namespace

## Recommendations

### High Priority Fixes

1. **Fix `GameCommon` namespace references:**
   - Replace `using GameCommon;` with specific namespaces
   - Create a root `GameCommon` namespace if needed for convenience

2. **Resolve `GameProtocol` namespace ambiguity:**
   - Decide whether to keep `GameProtocol` as a namespace or move classes to a different namespace
   - Update all references consistently

3. **Standardize Vector3 usage:**
   - Choose one Vector3 type for each context (protocol, server internal, client)
   - Use consistent aliasing across all files
   - Consider creating extension methods for conversion between types

4. **Fix LoginHandler using statement:**
   - Remove or correct `using GameCommon;` in [`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7)

### Medium Priority Improvements

1. **Consolidate protobuf usage:**
   - Standardize on `EnhancedMinecraftProtocol` for all new features
   - Phase out legacy Game.* protocols over time
   - Remove conditional compilation directives

2. **Standardize serialization:**
   - Choose one serialization library (recommend Google.Protobuf)
   - Remove protobuf-net dependencies where possible
   - Document the migration path

3. **Improve namespace organization:**
   - Create clear namespace hierarchy
   - Document namespace usage conventions
   - Add namespace documentation to README

### Low Priority Enhancements

1. **Add namespace validation:**
   - Create a tool to validate all using statements
   - Add to CI/CD pipeline

2. **Create type conversion utilities:**
   - Add helper methods for converting between Vector3 types
   - Centralize type conversion logic

## Implementation Plan

### Phase 1: Critical Fixes
1. Fix `GameCommon` namespace references
2. Resolve `GameProtocol` namespace ambiguity
3. Fix LoginHandler using statement
4. Verify compilation

### Phase 2: Standardization
1. Standardize Vector3 usage with consistent aliasing
2. Document Vector3 conversion patterns
3. Update all files to use consistent patterns

### Phase 3: Protocol Consolidation
1. Audit protobuf protocol usage
2. Plan migration to `EnhancedMinecraftProtocol`
3. Remove conditional compilation
4. Standardize on Google.Protobuf

### Phase 4: Documentation
1. Update README with namespace conventions
2. Create namespace usage guide
3. Add examples of proper using statements

## Success Criteria

1. All using statements resolve correctly
2. No namespace conflicts or ambiguities
3. Consistent Vector3 usage across codebase
4. Clear protobuf protocol usage patterns
5. Compilation succeeds for all projects
6. Documentation updated

## References

- [`protobuf_protocol_validation_analysis.md`](protobuf_protocol_validation_analysis.md)
- [`minecraft_features_categorized_comprehensive.json`](minecraft_features_categorized_comprehensive.json)
- [`AGENTS.md`](AGENTS.md) - Repository Guidelines
- Proto files in [`proto/`](proto/)
- Generated protobuf in [`Assets/Generated/Protobuf/`](Assets/Generated/Protobuf/)
- Shared protocol in [`SharedProtocol/`](SharedProtocol/)

## Executive Summary

This document analyzes the current state of using statements and protobuf protocol usage across the server and client codebases, identifying issues and providing recommendations for fixes.

## Key Findings

### 1. Namespace Inconsistencies

#### Issue: `GameProtocol` Namespace Confusion
**Problem:** The term `GameProtocol` is used inconsistently:
- In [`Assets/Scripts/Networking/Protocol/GameProtocol.cs`](Assets/Scripts/Networking/Protocol/GameProtocol.cs), it's a **namespace** containing AI-related classes
- In [`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:5), it's referenced as a namespace with `using GameProtocol;`
- In [`Assets/Scripts/Networking/NetworkManager.cs`](Assets/Scripts/Networking/NetworkManager.cs:5), it's referenced as a namespace with `using GameProtocol;`
- However, in [`GameServer/GameServer.cs`](GameServer/GameServer.cs:11), it's also referenced with `using GameProtocol;`

**Impact:** This creates confusion about which `GameProtocol` is being referenced and can lead to compilation errors.

#### Issue: `GameCommon` Namespace References
**Problem:** Files reference `using GameCommon;` but the actual namespaces are:
- `GameCommon.DataDriven`
- `GameCommon.Configuration`
- `GameCommon.Blocks`
- `GameCommon.World` (in docs only)

**Affected Files:**
- [`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7) uses `using GameCommon;`

**Impact:** The `GameCommon` namespace doesn't exist as a top-level namespace, causing compilation errors.

### 2. Vector3 Type Confusion

**Problem:** Multiple `Vector3` types exist across the codebase:

| Location | Type | Namespace | Usage |
|----------|-------|-----------|-------|
| [`Assets/Scripts/Networking/Protocol/GameProtocol.cs`](Assets/Scripts/Networking/Protocol/GameProtocol.cs:22) | `GameProtocol.Vector3` | `GameProtocol` | Protocol messages |
| [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:428) | `GameServerApp.Vector3` | `GameServerApp` (implicit) | Server internal |
| [`SharedProtocol/EnhancedMinecraft/`](SharedProtocol/EnhancedMinecraft/) | `SharedProtocol.Vector3` | `SharedProtocol` | Shared protocol |
| [`Assets/Generated/Protobuf/Common.cs`](Assets/Generated/Protobuf/Common.cs) | `MinecraftGame.Common.Vector3` | `MinecraftGame.Common` | Protobuf generated |

**Aliasing Used:**
- [`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:6-7):
  ```csharp
  using ProtoVector3 = GameProtocol.Vector3;
  using ServerVector3 = GameServerApp.Vector3;
  ```
- [`GameServer/Systems/CommandSystem.cs`](GameServer/Systems/CommandSystem.cs:6-7):
  ```csharp
  using ServerVector3 = GameServerApp.Vector3;
  using ProtoVector3 = SharedProtocol.Vector3;
  ```
- [`GameServer/Handlers/PlayerAttackHandler.cs`](GameServer/Handlers/PlayerAttackHandler.cs:5):
  ```csharp
  using ProtoVector3 = SharedProtocol.Vector3;
  ```

**Impact:** Multiple Vector3 types require careful aliasing and can lead to type confusion.

### 3. Protobuf Protocol Inconsistencies

#### Multiple Protocol Namespaces

| Namespace | Source | Status |
|-----------|--------|--------|
| `MinecraftGame.Common` | [`proto/common.proto`](proto/common.proto) | Generated |
| `Game.Auth` | [`proto/game_auth.proto`](proto/game_auth.proto) | Generated |
| `Game.Chat` | [`proto/game_chat.proto`](proto/game_chat.proto) | Generated |
| `Game.Diag` | [`proto/game_diag.proto`](proto/game_diag.proto) | Generated |
| `Game.Core` | [`proto/game_core.proto`](proto/game_core.proto) | Generated |
| `Game.Move` | [`proto/game_move.proto`](proto/game_move.proto) | Generated |
| `Game.World` | [`proto/game_world.proto`](proto/game_world.proto) | Generated |
| `EnhancedMinecraftProtocol` | [`proto/enhanced_minecraft_game.proto`](proto/enhanced_minecraft_game.proto) | Generated |

#### Mixed Serialization Libraries

**Protobuf-net (ProtoBuf):**
- Used in [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:7) with `using ProtoBuf;`
- Used for legacy protocol serialization

**Google.Protobuf:**
- Used in [`GameServer/SessionManager.cs`](GameServer/SessionManager.cs:8) with `using Google.Protobuf;`
- Used for enhanced protocol serialization
- Used in [`Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`](Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:5)

**Impact:** Dual serialization approach creates complexity and potential for bugs.

### 4. Conditional Compilation Issues

**Problem:** Code uses conditional compilation to support multiple protocols:

```csharp
#if HMW_PROTO
using Game.Move;
#endif
```

**Affected Files:**
- [`Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`](Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:10-12)

**Impact:** Creates maintenance burden and untested code paths.

### 5. Missing or Incorrect Using Statements

#### Files with Issues:

1. **[`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7)**
   - Issue: `using GameCommon;` - namespace doesn't exist
   - Fix: Should use specific namespaces like `GameCommon.DataDriven` or `GameCommon.Configuration`

2. **[`GameServer/AI/ServerAIManager.cs`](GameServer/AI/ServerAIManager.cs:5)**
   - Issue: `using GameProtocol;` - ambiguous reference
   - Fix: Should reference the specific namespace or class

3. **[`Assets/Scripts/Networking/NetworkManager.cs`](Assets/Scripts/Networking/NetworkManager.cs:5)**
   - Issue: `using GameProtocol;` - ambiguous reference
   - Fix: Should reference the specific namespace

## Recommendations

### High Priority Fixes

1. **Fix `GameCommon` namespace references:**
   - Replace `using GameCommon;` with specific namespaces
   - Create a root `GameCommon` namespace if needed for convenience

2. **Resolve `GameProtocol` namespace ambiguity:**
   - Decide whether to keep `GameProtocol` as a namespace or move classes to a different namespace
   - Update all references consistently

3. **Standardize Vector3 usage:**
   - Choose one Vector3 type for each context (protocol, server internal, client)
   - Use consistent aliasing across all files
   - Consider creating extension methods for conversion between types

4. **Fix LoginHandler using statement:**
   - Remove or correct `using GameCommon;` in [`GameServer/Handlers/LoginHandler.cs`](GameServer/Handlers/LoginHandler.cs:7)

### Medium Priority Improvements

1. **Consolidate protobuf usage:**
   - Standardize on `EnhancedMinecraftProtocol` for all new features
   - Phase out legacy Game.* protocols over time
   - Remove conditional compilation directives

2. **Standardize serialization:**
   - Choose one serialization library (recommend Google.Protobuf)
   - Remove protobuf-net dependencies where possible
   - Document the migration path

3. **Improve namespace organization:**
   - Create clear namespace hierarchy
   - Document namespace usage conventions
   - Add namespace documentation to README

### Low Priority Enhancements

1. **Add namespace validation:**
   - Create a tool to validate all using statements
   - Add to CI/CD pipeline

2. **Create type conversion utilities:**
   - Add helper methods for converting between Vector3 types
   - Centralize type conversion logic

## Implementation Plan

### Phase 1: Critical Fixes
1. Fix `GameCommon` namespace references
2. Resolve `GameProtocol` namespace ambiguity
3. Fix LoginHandler using statement
4. Verify compilation

### Phase 2: Standardization
1. Standardize Vector3 usage with consistent aliasing
2. Document Vector3 conversion patterns
3. Update all files to use consistent patterns

### Phase 3: Protocol Consolidation
1. Audit protobuf protocol usage
2. Plan migration to `EnhancedMinecraftProtocol`
3. Remove conditional compilation
4. Standardize on Google.Protobuf

### Phase 4: Documentation
1. Update README with namespace conventions
2. Create namespace usage guide
3. Add examples of proper using statements

## Success Criteria

1. All using statements resolve correctly
2. No namespace conflicts or ambiguities
3. Consistent Vector3 usage across codebase
4. Clear protobuf protocol usage patterns
5. Compilation succeeds for all projects
6. Documentation updated

## References

- [`protobuf_protocol_validation_analysis.md`](protobuf_protocol_validation_analysis.md)
- [`minecraft_features_categorized_comprehensive.json`](minecraft_features_categorized_comprehensive.json)
- [`AGENTS.md`](AGENTS.md) - Repository Guidelines
- Proto files in [`proto/`](proto/)
- Generated protobuf in [`Assets/Generated/Protobuf/`](Assets/Generated/Protobuf/)
- Shared protocol in [`SharedProtocol/`](SharedProtocol/)

