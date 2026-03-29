## Minecraft feature rollout (core/content/util) — 2026-01-07
- Order of work: Core authority & protocol → Content (terrain/gameplay) → Utility/tooling. Keep knobs JSON-first across `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.
- Worldgen tuning is mirrored between server (`GameServer/World/WorldManager.cs`) and Unity preview (`MapGeneratorLib/.../WorldGenAlgorithms.cs` + `WorldAreaManager`) so chunk seams, rivers, lakes, and caves stay coherent.

### Core (authority, world map control, protocol)
| Step | Server focus | Client focus | Data/Proto | Status |
| --- | --- | --- | --- | --- |
| 1 | Hydrology gradients blend slope + flow with clamp to stabilise chunk seams (`BuildHydrologyGradient` in `WorldManager`) | Unity worldgen uses the same slope/clamp mix (`WorldGenAlgorithms` via `WorldAreaManager`) | `HydrologyGradientSlopeWeight`, `HydrologyGradientClamp` in `config/world.json` & Unity config | ✅ implemented |
| 2 | Enforce EnhancedMinecraft protobuf registry/descriptors before chunk streaming (`ProtoRuntime.EnsureInitialized`, `ProtocolValidator.ValidateEnhancedContracts`) | Unity bootstrap/bridge uses the same registry/descriptor guard | Generated DTOs under `SharedProtocol` + `Assets/Generated/Protobuf` | ✅ in place (rerun `protoc` + `dotnet build SharedProtocol/GameServer`) |
| 3 | Room/session + chunk routing stability and unload acks | World load/preview orchestration per area | `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `RoomEnter/Leave/List` | ⬜ follow-up: verify edge cases and metrics |

### Content (terrain, entities, gameplay)
| Step | Server focus | Client focus | Data/Proto | Status |
| --- | --- | --- | --- | --- |
| 1 | Rivers/lakes/caves read slope-aware gradients to reduce tearing and overshoot | Unity preview mirrors slope-aware gradients for terrain carve | Worldgen JSON knobs; hydrology caches | ✅ implemented |
| 2 | Lake placement heatmap vs river proximity + erosion bias | Preview heatmap/overlay parity | `LakeInflowBlendWeight`, `LakeRiverProximitySuppression` JSON | ⬜ tune after playtest |
| 3 | Biome/weather/sky + entity routing | Visual/weather FX + entity prediction | `WorldInfo`, `WeatherUpdate`, `EntitySpawn/Update/Despawn` | ⬜ validate current builds |

### Utility (data, tooling, ops)
| Step | Server focus | Client focus | Data/Proto | Status |
| --- | --- | --- | --- | --- |
| 1 | Config parity + hydration cache diagnostics | Unity uses same config surface; warns on drift | JSON configs, `WorldConfigFile`, telemetry hooks | ✅ slope/clamp knobs added |
| 2 | Protobuf toolchain + fingerprints (`ProtoFingerprint.AssertDescriptorFingerprint`) | Unity decode path reuses registry | `proto/*.proto`, generated C# outputs | ✅ guard present |
| 3 | Telemetry/ops for chunk residency + hydrology health | Overlay/stats panel for designers | `ServerStatusRequest/Response`, recordings | ⬜ add alerts & dashboards |
