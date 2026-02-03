# 2026-02-03 Core/Content/Util Feature Matrix (Session 40)

Date: 2026-02-03  
Hydrology Signature: `2026-02-03-hydrology-riverlake-v12` (server/client parity)  
Profile Version: 14  
Source Plans: `plans/2026-02-03-session-40-plan.md`, git commit `5b8089ca`

## Core (Systems & Protocol)
- Worldgen hydrology pipeline (caves/rivers/lakes) with seam-stable masks, flow memory, erosion-aware damping.
- World map control sync: profile hash + hydrology signature validation (`WorldMapControlProfile`, `WorldMapSignature`).
- Networking/protobuf: registry validation + dummy protocol probe (round-trip, optional packet audit).
- Shared DLL surface: `GameCommon.dll` (feature catalog/enums/contracts) consumed by server & Unity.
- Data-driven configs: `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`.

## Content (Gameplay Surfaces)
- Terrain water features: river curvature/edge feathering, lake outflow sealing, riparian buffers.
- Cave systems: moisture-safe ceilings, support pillars, riparian plugs, aquifer suppression aligned to hydrology.
- Biome overlays & preview: hydrology/flow masks mirrored on client for map preview.
- Resource placement hooks: erosion-risk aware anchors for ores/structures (uses shared hydrology mask).

## Utilities (Tooling & Ops)
- Protocol diagnostics: `DummyProtocolClient` matrix (required + optional packets, fingerprint & hydrology signature logging).
- Map-control telemetry: erosion/hydrology/flow dumps for parity checks (server & Unity hooks).
- Config validation: JSON schema-style guards for world/proto configs; profile regeneration when version/signature drifts.
- Build/test: `dotnet build SharedProtocol`, `dotnet build GameServer`, proto round-trip probe.

## Sequenced Work (session 40)
1. Refresh feature catalog JSON + shared DLL descriptors (core/content/util).
2. Apply hydrology continuity + river/lake/cave refinements (server & client map-control paths).
3. Update world map control profile (v14) + hydrology signature; regenerate hashes.
4. Extend dummy protocol client coverage and reports; verify registry.
5. Update docs/README with runbooks and config references; commit/push after builds.
