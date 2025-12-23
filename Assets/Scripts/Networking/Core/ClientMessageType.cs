using System;

namespace Networking.Core
{
    /// <summary>
    /// Client-side mirror of server MessageType enum for framing.
    /// Keep values in sync with SharedProtocol.MessageType.
    /// </summary>
    public enum ClientMessageType
    {
        // Auth
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,

        // Movement
        MoveRequest = 10,
        MoveResponse = 11,

        // World / Blocks
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,

        // Chat
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,

        // Diagnostics
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,

        // Player info
        PlayerInfoUpdate = 50,

        // Inventory
        InventoryRequest = 60,
        InventoryResponse = 61,
        InventoryUpdateBroadcast = 62,

        // Crafting
        CraftingRequest = 70,
        CraftingResponse = 71,
        RecipeListRequest = 72,
        RecipeListResponse = 73,

        // Health / Respawn
        HealthActionRequest = 80,
        HealthActionResponse = 81,
        HealthUpdate = 82,
        RespawnRequest = 83,
        RespawnResponse = 84,
        PlayerDeath = 85,
        PlayerRespawnBroadcast = 86,
        CombatEvent = 87,

        // Rooms / Queues
        RoomListRequest = 90,
        RoomListResponse = 91,
        RoomEnterRequest = 92,
        RoomEnterResponse = 93,
        RoomLeaveRequest = 94,
        RoomLeaveResponse = 95,
        RoomQueueUpdate = 96,
        RoomPromotionNotice = 97,

        // AI (server-authoritative)
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,

        // Combat
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,

        // Commands
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122
    }
}
