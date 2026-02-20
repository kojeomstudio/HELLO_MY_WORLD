namespace SharedProtocol.Common.Enums;

/// <summary>
/// Core protocol enumeration types
/// </summary>
public static class CoreEnums
{
    /// <summary>
    /// Message types for client-server communication
    /// </summary>
    public enum MessageType
    {
        // Authentication
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,
        
        // Movement
        MoveRequest = 10,
        MoveResponse = 11,
        
        // World/Blocks
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,
        
        // Chat
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,
        
        // Server Status
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,
        
        // Player
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
        
        // Health
        HealthActionRequest = 80,
        HealthActionResponse = 81,
        HealthUpdate = 82,
        RespawnRequest = 83,
        RespawnResponse = 84,
        PlayerDeath = 85,
        PlayerRespawnBroadcast = 86,
        CombatEvent = 87,
        
        // Room/Lobby
        RoomListRequest = 90,
        RoomListResponse = 91,
        RoomEnterRequest = 92,
        RoomEnterResponse = 93,
        RoomLeaveRequest = 94,
        RoomLeaveResponse = 95,
        RoomQueueUpdate = 96,
        RoomPromotionNotice = 97,
        
        // AI System
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,
        
        // Combat System
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,
        
        // Commands
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122
    }
    
    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3,
        Team = 4,
        Announcement = 5,
        Death = 6,
        JoinLeave = 7,
        Achievement = 8,
        CommandResult = 9
    }
    
    /// <summary>
    /// Room visibility settings
    /// </summary>
    public enum RoomVisibility
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2
    }
    
    /// <summary>
    /// Room status
    /// </summary>
    public enum RoomStatus
    {
        Waiting = 0,
        InGame = 1,
        Completed = 2,
        Locked = 3
    }
    
    /// <summary>
    /// Room member roles
    /// </summary>
    public enum RoomRole
    {
        Player = 0,
        Host = 1,
        Moderator = 2,
        Spectator = 3,
        Queue = 4
    }
}
