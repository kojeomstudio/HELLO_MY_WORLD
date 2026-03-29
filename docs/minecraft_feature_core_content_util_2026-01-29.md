# Minecraft Feature Map (Core/Content/Utility) — 2026-01-29

Data source: `config/minecraft_feature_core_content_util_2026-01-29.json` (loaded via `GameCommon.DataDriven.FeatureManifest`).

## Core (server + client shared)
- (1) CORE-001 — World map control + hydrology signature v6 (profile/hash parity, riparian stability).
- (2) CORE-002 — Shared enums + proto DLL distribution (GameCommon + SharedProtocol).
- (3) CORE-003 — Worldgen riparian stabilization (caves/rivers/lakes seam control).

## Content
- (1) CONTENT-001 — Hydrology-aware cave/river/lake masks for chunk streaming (server + Unity previews).
- (2) CONTENT-002 — JSON-driven worldgen knobs (hydrology/cave/lake configs).
- (3) CONTENT-003 — Feature manifest surfaced to Unity tooling for gating and UI hints.

## Utility
- (1) UTIL-001 — Protocol registry + fingerprint JSON report for CI/regeneration audits.
- (2) UTIL-002 — Dummy protocol client for packet encode/decode probes (headless TCP optional).
- (3) UTIL-003 — Feature manifest loader in GameCommon.dll for shared consumption.

## Notes
- Keep `FeatureCategory` / `FeatureLayer` enums in `GameCommon` as the shared contract for both Unity and server.
- Update `config/proto_reference_report.json` and `config/world_map_control_profile.json` whenever proto/terrain knobs change.
- Sequence: finish CORE items before CONTENT; run UTIL-001 before UTIL-002 network probes to ensure registry health.
