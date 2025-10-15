# Minecraft Container Feature Plan

This plan tracks the client and server work required to finish shared container interactions (chests, furnaces, crafting tables). Update this file as milestones shift so the next session can continue without rediscovery.

## Feature Matrix
| ID | Layer | Description | Dependencies | Owner Notes | Status |
|----|-------|-------------|--------------|-------------|--------|
| C1 | Server | Add container tables + CRUD helpers in DatabaseHelper | SQLite schema updates | Required before persistence works | Done |
| C2 | Server | Register ContainerSystem and container handlers with dispatchers | C1 | Enables server to receive requests | Done |
| C3 | Server | Persist slot mutations & broadcast diffs via ContainerSystem | C1, C2 | Snapshot hashes + diff validation handshake in place | Done |
| C4 | Client | Add container request/response pipeline inside MinecraftGameClient | C1, C2 | Exposes events for ContainerManager | Done |
| C5 | Client | Bind ContainerManager events to UI prefabs (chest panel MVP) | C4 | Pending UI wiring | Backlog |
| C6 | Shared | Expand regression tests for container protobuf round-trip | After C1-C4 | Cover open/update/close flows | Backlog |

## Active Work Queue (Oct Sprint)
- [x] Capture container hash mismatch metrics and surface via diagnostics endpoint (supports C6 prep, landed 2025-10-15).
- [ ] Prototype chest UI binding for C5 once event data is stable.
- [ ] Draft regression test cases for C6 (ser/deser + diff replay).

## Parking Lot / Follow-ups
- Author lightweight Unity chest UI using existing UI framework once data events fire.
- Extend self-test client to exercise container lifecycle (open/update/close).
- Document synchronization flow in docs/networking-protocol.md after pipeline stabilises.



