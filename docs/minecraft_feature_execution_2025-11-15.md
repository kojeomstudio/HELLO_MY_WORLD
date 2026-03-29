# Minecraft Feature Execution Plan (2025-11-15)

This log captures the client/server Minecraft functionality that was queued for this session and the order in which it was delivered so later contributors can continue the sequence without rediscovering context.

| Order | Feature | Server Responsibilities | Client / Tooling Responsibilities | Status | Follow-ups |
|-------|---------|-------------------------|-----------------------------------|--------|------------|
| 1 | Cave Aquifer Channels | `WorldManager.AddCaveAquiferChannels` blends hydrology + flow accumulation into horizontal aquifer tunnels, lays clay floors, and writes the results back into the chunk surface cache before chunk payloads are built. | `MapGeneratorLib.AddCaveAquiferChannels` mirrors the helper so Unity previews render the same channels, lighting cuts, and clay seams. | ✅ Done | Surface toggle in `WorldSeedConfig` if more tuning is required. |
| 2 | River Gradient Smoothing | A new pass after delta fan carving relaxes noisy riverbanks, fills shallow braid bars with sand, and keeps banks below `GlobalWaterLevel`. | MapGeneratorLib receives the same pass (`ApplyRiverGradientSmoothing`) so editor captures match the dedicated server output. | ✅ Done | Add HUD metrics when we expose river debugging overlays. |
| 3 | Lake Water Table Equalizer | Lakes sample rim hydrology and adjust the stored water level, re-carving basins and banks before overflow channels run so proto payloads stay deterministic. | MapGeneratorLib mirrors the equalizer so art captures and server chunks agree on waterlines, terraces, and seep depth. | ✅ Done | Surface hydrology averages in the debug HUD (future task). |
| 4 | Enhanced Proto Registry Coverage | `ProtocolRegistry` now registers the chunk/action/entity/time/weather/payload DTOs we consume, `ProtoDiagnostics` only tracks this curated descriptor list, and `SharedProtocol` links the generated `Common.cs` DTOs. | Unity tooling already enforces `ProtoFingerprint`; the richer diagnostics surface missing registrations before the editor or client connects. | ✅ Done | Consider auto-generating the registry from `.proto` ahead of the next protocol refresh. |

## Notes
- The hydrology work (aquifers → river smoothing → lake equalizer) must be rolled out in this order so MapGeneratorLib always mirrors `WorldManager` before Unity artists publish previews. Breaking the sequence will desync the client/server terrain, so future changes should extend this log.
- The protobuf validation step happens inside `ChunkPayloadBuilder` and `ServerConfig` now that the registry + diagnostics are in sync. Always regenerate `Assets/Generated/Protobuf` (including `Common.cs`) before building either project.
