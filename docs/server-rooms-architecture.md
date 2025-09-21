# Server Rooms & Lobby Architecture

The dedicated GameServer organises players into rooms so gameplay broadcasts (chat, block updates, etc.) stay scoped and easy to reason about. The 2024 terrain update adds a lobby aware queueing system, structured metadata, and richer protocol messages so both Unity and .NET components share the same mental model.

## Core Components

| Component | Responsibility |
|-----------|----------------|
| `RoomManager` | Owns the registry of `GameRoom` instances, tracks which room each player belongs to, and provides helpers to broadcast or auto-assign rooms. Exposes `TryAssignPlayerToRoom`, `AutoAssign`, `RemovePlayer`, and aggregate lobby summaries. |
| `GameRoom` | Holds immutable identifiers (`RoomId`, `WorldId`, `LobbyId`) plus mutable state such as visibility, game mode, current status, member list, and queue. It also manages ownership/roles, password validation, and promotion from the waiting queue. |
| `RoomMember` | Captures per-player metadata inside a room (role, ready flag, queued position, join timestamp). |

## Lifecycle Overview

1. **Bootstrap** – the server creates a default lobby (`RoomManager.DefaultLobbyId`, world 1) on startup.
2. **Login** – `LoginHandler` validates the session, assigns the user to the lobby (`TryAssignPlayerToRoom` with queue disabled), and updates the player’s world metadata maintained by `SessionManager`.
3. **Join Request** – `RoomEnterHandler` accepts a `RoomEnterRequest` which can target a specific room or request auto-assignment within a lobby. It forwards the request to `RoomManager.TryAssignPlayerToRoom` with a `RoomJoinOptions` payload describing password, preferred role, spectator mode, and whether the caller is willing to wait in the queue.
4. **Queue Handling** – if a room is full and queueing is permitted, the player is stored in the room’s wait list and receives a `RoomEnterResponse` with `IsQueued=true` plus their current queue position. Whenever seats free up `RoomManager` promotes the next player, sends them a `RoomPromotionNotice`, and broadcasts an updated queue snapshot.
5. **Leave & Cleanup** – `RoomLeaveHandler` removes the player from their room using `RoomManager.RemovePlayer`. Active members free seats, potentially promoting a queued player, after which the leaving player is redirected back to the lobby.

## Queue & Promotion Messages

Two new push messages keep clients synchronised:

- `RoomQueueUpdateMessage` (`MessageType.RoomQueueUpdate`) lists the current waiting queue for a room. It is broadcast to active members and anyone still waiting so UI can surface accurate positions and estimates.
- `RoomPromotionMessage` (`MessageType.RoomPromotionNotice`) targets the promoted player when their queue entry converts into an active seat. It includes the updated `RoomInfo` snapshot so the client can refresh status immediately.

Unity’s `MinecraftGameClient` listens for both messages and surfaces lightweight events (`RoomQueueUpdated`, `RoomPromotionReceived`) so UI layers can present queue status without re-requesting the whole room list.

## Room Metadata

`RoomInfo` now carries richer metadata that mirrors the server’s `GameRoom` state:

- `LobbyId` – logical grouping (e.g. “lobby”, “pve”, “creative”).
- `Owner` & `Visibility` – allow distinguishing public/friends-only/private spaces.
- `GameMode` – arbitrary tag for the gameplay variant hosted by the room.
- `QueueCount` & `SpectatorCount` – snapshot of non-playing participants.
- `Tags` – a small key/value map for future extensibility (e.g. region, difficulty).
- `Status` – coarse lifecycle (waiting, in game, completed, locked).

Room member snapshots now include a parallel `MemberInfos` collection. Each entry exposes the member’s role (`Player`, `Host`, `Spectator`, etc.), join timestamp, queue position, and ready flag. This is used by both the lobby browser and the queue update/broadcast pipeline.

## Handler Integration Highlights

- **Chat & Block Handlers** continue to broadcast only to active members. Queue-only members do not receive room chat or block updates until they are promoted.
- **RoomEnterHandler** builds a tailored `RoomEnterResponse` indicating whether the caller is in the queue or already active. Successful active joins trigger a room-wide system message.
- **RoomLeaveHandler** reports whether a new player was promoted (`PromotedFromQueue`) and whether the caller has been returned to the lobby (`ReturnedToLobby`).

## Extensibility Points

- Additional lobby definitions can be created at runtime by calling `CreateRoom` with a new `LobbyId` and `RoomVisibility`.
- Custom matchmaking can be layered on top of `AutoAssign` by pre-filtering the candidate rooms and passing bespoke `RoomJoinOptions` (e.g. enforcing spectator joins).
- `RoomInfo.Tags` is intentionally unopinionated – features such as “region” or “difficulty” filters can simply add tagging logic in a contained fashion without changing the network schema again.
- `RoomStatus` was introduced to make transitions (waiting → in-game → completed) explicit. Future gameplay systems can toggle this flag to track progression or lock the queue once a match starts.

## Related Protocol Changes

The networking guide (`docs/networking-protocol.md`) now documents the updated protobuf contracts. Whenever room-related features change, update both the shared DTO definitions (`SharedProtocol/Messages.cs`) and this document so the Unity client and external tools stay aligned.
