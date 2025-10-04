# Minecraft Feature Backlog

This backlog captures the feature sets required to deliver core Minecraft-style gameplay. Items marked DONE have working implementations in the current branch; TODO items remain outstanding.

## Client Feature Checklist
| Status | Feature | Notes |
| --- | --- | --- |
| DONE | Network transport and protobuf serialization | `Assets/Scripts/Networking/Core` handles framed transport and DTO serialization. |
| DONE | Chunk streaming and mesh rebuild | `ChunkManager` and `ChunkRenderer` keep meshes synced with server snapshots. |
| DONE | Block interaction flow | `MinecraftGameClient` routes place and break actions through the server authority. |
| DONE | UI for login, chat, and inventory | Core UI available; further polish tracked separately. |
| DONE | Server status overlay | Unity HUD auto-refreshes `ServerStatusResponse` every 15s with manual refresh support in `MinecraftGameManager`. |
| TODO | Entity rendering refresh | Add interpolation and animation states for spawned mobs. |

## Server Feature Checklist
| Status | Feature | Notes |
| --- | --- | --- |
| DONE | Authentication and session tracking | `LoginHandler` and `SessionManager` persist state to SQLite. |
| DONE | Movement and position validation | `MovementHandler` clamps velocity and updates authoritative state. |
| DONE | Block mutation pipeline | `WorldBlockHandler` plus chunk cache enforce authoritative block updates. |
| DONE | Inventory and crafting subsystems | `InventoryHandler` and `CraftingHandler` coordinate recipes and persistence. |
| DONE | Room and lobby management | `RoomManager` keeps lobby and room membership in sync. |
| DONE | Server status endpoint | `ServerStatusHandler` replies with online players, version, and uptime metrics. |
| TODO | Entity simulation loop | AI tick and server-side pathing tracking outstanding. |
| IN PROGRESS | Weather broadcast scheduling | Server now emits time/weather snapshots; client particles & skybox still pending. |

## Near-Term Work Queue
1. [Done] Task-09A — Persist inventory snapshots to SQLite and surface JSON for reconnect diffs (server).
2. [Done] Task-09B — Unity client inventory snapshot/diff consumer (hotbar wiring + event feed).
3. [Todo] Bind day/night lighting & weather FX in Unity to the new broadcasts.
4. [Todo] Extend the pause menu with live server metrics (players/uptime) sourced from the HUD overlay pipeline.
5. [Todo] Capture chunk residency metrics for future capacity planning.
6. [Todo] Automate protocol regression tests around inventory and chunk mutations.

All future tasks should stay within the 200-line function guideline and update associated docs when wire contracts evolve.

