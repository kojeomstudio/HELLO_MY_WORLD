# 2026-01-20 Session Plan – Terrain, Protocol, Map Control

## Context (recent commits)
- 3616c383 feat(worldgen): add pressure balance and proto handler guard
- 04991893 feat: Session 07 - Comprehensive Implementation & Verification
- a1bfcf35 feat(worldgen): add erosion risk masks and proto diagnostics
- 64530434 feat: Comprehensive system review and data-driven approach validation (2026-01-19)

## Completed (carry-over)
- Cataloged client/server core-content-utility split for this track
- Added hydrology pressure balance pass for caves/rivers/lakes (server + MapGeneratorLib)
- Enabled proto handler binding validation at server bootstrap
- Synced world/map-control configs between server and Unity StreamingAssets

## To Do (today)
- Protocol: verify generated Google.Protobuf DTO usage, clean `using` drift, and confirm handler registration coverage.
- Terrain: improve caves/rivers/lakes (connectivity, bank erosion, lake shore sealing, pressure balance) and reapply to map-control paths.
- World map control: harden profile hash checks, align server/client loaders, and keep profiles in JSON for data-driven tuning.
- Data & config: ensure new knobs land in JSON (server/client/world-map) and stay data-driven.
- Documentation: update docs/README plus feature categorization file.
- Validation: compile server + SharedProtocol, spot-check proto packet creation, and prep for push.

## Feature categorization (core/content/utility, client/server)
### Client
- Core: chunk streaming & mesh rebuilds respecting map-control profile; network bootstrap/keepalive; block place/break & HUD.
- Content: biome-tinted terrain with rivers/lakes/caves; shoreline/wetland/aquifer visuals; ambient FX hooks.
- Utility: JSON config loading (StreamingAssets); map-control previews & debug overlays; protobuf desync/error reporting.
### Server
- Core: world map control generation/cache + profile export; hydrology/flow cache for caves/rivers/lakes; session lifecycle/auth/keepalive.
- Content: biome/loot/structure tables (JSON); riparian-safe cave/river/lake generation; weather scheduler; data-driven blocks/ores.
- Utility: JSON config management with reload hooks; monitoring/logging/admin commands; protobuf DTO registration/validation; tuning knobs via JSON.

## Execution sequence
1) Analyze proto usage & using statements, fix namespace/library drift.  
2) Improve terrain synthesis (cave connectivity, river bank erosion/flow alignment, lake rim sealing) and ensure hydrology pressure balance.  
3) Align world map control architecture (server/client profile hash checks, JSON parity) and expose data knobs.  
4) Update docs (README + docs/) and feature categorization file; keep configs in JSON.  
5) Build/test, then stage, commit, and push.

## Success criteria
- Generated protobuf packets referenced correctly; handler registrations validated.
- Caves/rivers/lakes show smoother seams, safer banks, and stable hydrology across chunks.
- World-map profile hash stable and consistent across server/client using JSON configs.
- Docs updated in markdown under docs/, configs remain JSON/data-driven.
- Builds pass; all changes committed and pushed.

## Context (Recent Commits)
- 04991893 feat: Session 07 - Comprehensive Implementation & Verification
- a1bfcf35 feat(worldgen): add erosion risk masks and proto diagnostics
- 64530434 feat: Comprehensive system review and data-driven approach validation (2026-01-19)
- 7bb5794f feat: Terrain seam smoothing & riparian cave guard (2026-01-19)

## Completed (Previous Sessions)
- [x] Cataloged client/server core-content-util split for this session
- [x] Added hydrology pressure-balancing pass for caves/rivers/lakes (server + MapGeneratorLib)
- [x] Enabled proto handler binding validation at server bootstrap
- [x] Synced world/map-control configs (server + Unity StreamingAssets)

## To Do (Today - 2026-01-20)

### Phase 1: Analysis & Planning
- [ ] Analyze current protobuf protocol implementation and identify issues
- [ ] Review terrain generation algorithms (caves, rivers, lakes)
- [ ] Examine world map control architecture
- [ ] Verify all using statements and references
- [ ] Create comprehensive feature categorization document

### Phase 2: Terrain Generation Improvements
- [ ] Implement improved cave generation algorithms with stability and connectivity
- [ ] Enhance river generation with flow direction and bank erosion
- [ ] Improve lake generation with depth variation and shoreline blending
- [ ] Add hydrology pressure-balancing system
- [ ] Implement erosion risk masks
- [ ] Add riparian cave guard system

### Phase 3: World Map Control Architecture
- [ ] Enhance server-side world map control
- [ ] Improve client-side world map control
- [ ] Implement profile hash validation
- [ ] Add hydrology/erosion overlay systems
- [ ] Improve chunk synchronization
- [ ] Add world border enforcement

### Phase 4: Protobuf Protocol Validation
- [ ] Review all protobuf message definitions
- [ ] Validate message handler registration
- [ ] Ensure proper serialization/deserialization
- [ ] Fix mixed protobuf-net and Google.Protobuf usage
- [ ] Implement handler binding validation
- [ ] Add proto fingerprint tracking

### Phase 5: Configuration & Data-Driven Approach
- [ ] Update server configuration JSON files
- [ ] Update client configuration JSON files
- [ ] Create world generation configuration
- [ ] Implement data-driven block system
- [ ] Create data-driven item system
- [ ] Add biome configuration data
- [ ] Implement ore distribution data

### Phase 6: Compilation & Testing
- [ ] Run server compilation tests
- [ ] Run client compilation tests
- [ ] Test protobuf packet handling
- [ ] Verify all using statements
- [ ] Run integration tests

### Phase 7: Documentation Updates
- [ ] Update README.md
- [ ] Create terrain generation documentation
- [ ] Create world map control documentation
- [ ] Create protobuf protocol documentation
- [ ] Update configuration documentation
- [ ] Create implementation summary

### Phase 8: Git Operations
- [ ] Stage all modified files
- [ ] Create local commit with detailed message
- [ ] Push changes to origin branch

## Feature Categorization (Core, Content, Utility)

### Client Features

#### Core
- Chunk streaming and mesh rebuilds gated by world-map control profile
- World map control preload with hydrology/erosion overlays
- Network bootstrap, reconnect, and keepalive handling
- Block placement/break + inventory HUD

#### Content
- Biome-tinted terrain, rivers, lakes, and caves
- Shoreline, wetland, and aquifer visualization
- Structure/loot preview hooks
- Ambient audio/FX triggers

#### Utility
- Debug overlays for chunk bounds and masks
- JSON config loading from StreamingAssets
- Protobuf desync/error reporting
- Localization and analytics stubs

### Server Features

#### Core
- World map control generation/caching and profile export
- Improved hydrology/flow cache feeding caves, rivers, lakes
- Session lifecycle/auth/keepalive handlers
- Chunk save/load pipeline with profile hash

#### Content
- Biome/loot/structure tables (JSON-driven)
- Cave/river/lake generation with riparian sealing
- Weather scheduler and progression events
- Data-driven block/ore distribution

#### Utility
- Config management via JSON with reload hooks
- Monitoring/logging and admin commands
- Protobuf DTO registration/validation
- Data-driven tuning (drop rates, mob stats, XP curves)

## Implementation Sequence

### 1. Map Control Parity
- Refresh world-map control profile
- Ensure client/server load identical hydrology + erosion knobs from JSON
- Validate profile hash consistency

### 2. Terrain Synthesis
- Strengthen cave/river/lake generation
- Implement hydrology-driven inlet/outlet balance
- Add erosion-aware sealing across server and MapGeneratorLib

### 3. Protocol Integrity
- Validate protobuf registry usage in handlers
- Regenerate DTOs if drift is detected
- Keep using statements aligned to real types

### 4. Observability and Config
- Extend JSON configs for new hydrology/erosion knobs
- Expose debug overlays for profile mismatches
- Add monitoring and logging

## Priority Order

1. **High Priority (P1)**
   - Fix protobuf protocol inconsistencies
   - Improve terrain generation algorithms
   - Enhance world map control architecture
   - Validate all using statements

2. **Medium Priority (P2)**
   - Complete configuration management
   - Implement data-driven systems
   - Add monitoring and debugging tools

3. **Low Priority (P3)**
   - Performance optimization
   - Advanced features
   - UI enhancements

## Success Criteria

- All protobuf messages properly handled and validated
- Terrain generation produces stable, connected caves and realistic rivers/lakes
- World map control synchronized between server and client
- All configuration files in JSON format
- All game data driven by JSON files
- Compilation successful with no errors
- All using statements reference existing files/classes
- Documentation updated and comprehensive
- Changes committed and pushed to origin

## Risk Mitigation

- **Protobuf Issues**: Maintain backward compatibility during fixes
- **Terrain Generation**: Test extensively before merging
- **Configuration Changes**: Version all config files
- **Breaking Changes**: Document clearly and provide migration path

## Notes

- This work builds on Session 07 comprehensive implementation
- Focus on hydrology and erosion improvements
- Maintain data-driven architecture principles
- Ensure all changes are properly documented
- Test thoroughly before committing
