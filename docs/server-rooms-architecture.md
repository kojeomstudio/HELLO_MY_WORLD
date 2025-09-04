# Server Rooms Architecture

This document outlines the room-based architecture introduced for the standalone GameServer.

## Goals
- Separate server core (sessions, dispatch, DB, world) from gameplay orchestration (rooms).
- Scope broadcasts (chat, block changes) to players in the same room.
- Keep world/chunk logic independent; rooms map to worlds but are not required to be 1:1.

## Components
- `RoomManager`: central registry. Creates/removes rooms, tracks player membership, and provides room-scoped broadcast helpers.
- `GameRoom`: lightweight container with `RoomId`, `WorldId`, and a member list.

## Lifecycle
- On server start: `RoomManager` creates a default room `lobby` bound to world id 1.
- On login success: the `LoginHandler` assigns the player to `lobby` via `RoomManager.AssignPlayerToRoom`.
- On client disconnect: `GameServer` removes the player from the room.

## Handler Integration
- `ChatHandler`: Global chat now broadcasts only to the sender’s room.
- `WorldBlockHandler`: Block change broadcasts are scoped to the sender’s room.

## Extensibility
- Add a join-room message to allow players to switch rooms.
- Multiple rooms can target the same `WorldId` to isolate player groups.
- Hooks can be added for per-room game rules and content modules.

