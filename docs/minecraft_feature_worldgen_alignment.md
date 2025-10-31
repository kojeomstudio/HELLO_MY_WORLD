# Minecraft World-Generation & Protocol Alignment Checklist (2025-10-30)

This session delivers the outstanding Minecraft-style features that affect both the Unity client and the .NET server. The table lists the required capabilities, their client/server responsibilities, and the execution order we are following.

| Step | Feature | Server Responsibilities | Client Responsibilities | Status |
|------|---------|-------------------------|-------------------------|--------|
| 1 | Enhanced Cave Network | Generate cross-chunk worm & noise caves, persist lava/water pockets, expose metadata | Rebuild chunk meshes with cavern hollows, stream lighting hints | In progress |
| 2 | River Continuity & Banks | Carve meandering rivers, smooth gradients across chunk seams, emit sandbank layers | Render water surfaces, blend shoreline materials, play ambient FX | In progress |
| 3 | Inland Lake Formation | Detect inland basins, flood-fill water to sampled level, decorate banks | Flatten vertex normals, spawn water/shore FX, prepare lily-pad props | In progress |
| 4 | Chunk Payload Validation | Serialize chunk payloads through `EnhancedMinecraftProtocol` descriptors, ensure parity with legacy payloads | Accept enhanced payloads (future), log validation warnings | In progress |

## Sequential Execution Plan
1. ✅ Capture baseline behaviour (chunk residency metrics, current cave/river/lake output) for regression comparison.
2. 🔄 Apply cave/river/lake algorithm refinements inside `WorldManager` and `MapGeneratorLib`, focusing on cross-chunk continuity and shoreline smoothing.
3. 🔄 Link the generated `EnhancedMinecraftProtocol` contracts into `SharedProtocol`, build chunk payload validators, and run against the refined generator.
4. ☐ Update Unity client notes and tooling once validation passes so engine consumers can migrate to the shared contracts.

## Notes
- World-generation tuning constants live under `GameServer/World/WorldManager.cs`; Unity simulators in `MapGeneratorLib` mirror the same offsets so offline tooling stays authoritative.
- Chunk payload validation never alters the on-wire format for this session—it ensures parity so that the client migration can proceed as a follow-up.
