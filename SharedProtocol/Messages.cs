using ProtoBuf;

namespace SharedProtocol;

/// <summary>
/// 메시지 타입 정의 - 클라이언트와 서버 간 통신에 사용
/// </summary>
public enum MessageType
{
    // 인증 관련
    LoginRequest = 1,
    LoginResponse = 2,
    LogoutRequest = 3,
    LogoutResponse = 4,
    
    // 이동 관련
    MoveRequest = 10,
    MoveResponse = 11,
    
    // 월드/블록 관련
    WorldBlockChangeRequest = 20,
    WorldBlockChangeResponse = 21,
    WorldBlockChangeBroadcast = 22,
    
    // 채팅 관련
    ChatRequest = 30,
    ChatResponse = 31,
    ChatMessage = 32,
    
    // 서버 상태/진단
    PingRequest = 40,
    PingResponse = 41,
    ServerStatusRequest = 42,
    ServerStatusResponse = 43,
    
    // 플레이어 정보 업데이트
    PlayerInfoUpdate = 50,
    
    // 인벤토리 관련
    InventoryRequest = 60,
    InventoryResponse = 61,
    InventoryUpdateBroadcast = 62,
    
    // 제작 관련
    CraftingRequest = 70,
    CraftingResponse = 71,
    RecipeListRequest = 72,
    RecipeListResponse = 73,
    
    // 체력 및 허기 관련
    HealthActionRequest = 80,
    HealthActionResponse = 81,
    HealthUpdate = 82,
    RespawnRequest = 83,
    RespawnResponse = 84,
    PlayerDeath = 85,
    PlayerRespawnBroadcast = 86,
}

// 기본 데이터 구조
[ProtoContract]
public class Vector3
{
    [ProtoMember(1)] public float X { get; set; }
    [ProtoMember(2)] public float Y { get; set; }
    [ProtoMember(3)] public float Z { get; set; }
    
    public Vector3() { }
    public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
}

[ProtoContract]
public class Vector3Int
{
    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public int Y { get; set; }
    [ProtoMember(3)] public int Z { get; set; }
    
    public Vector3Int() { }
    public Vector3Int(int x, int y, int z) { X = x; Y = y; Z = z; }
}

[ProtoContract]
public class InventoryItem
{
    [ProtoMember(1)] public int ItemId { get; set; }
    [ProtoMember(2)] public string ItemName { get; set; } = string.Empty;
    [ProtoMember(3)] public int Quantity { get; set; }
}

[ProtoContract]
public class PlayerInfo
{
    [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
    [ProtoMember(2)] public string Username { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3? Position { get; set; }
    [ProtoMember(4)] public int Level { get; set; }
    [ProtoMember(5)] public int Health { get; set; }
    [ProtoMember(6)] public int MaxHealth { get; set; }
    [ProtoMember(7)] public List<InventoryItem> Inventory { get; set; } = new();
}

// 인증 관련 메시지
[ProtoContract]
public class LoginRequest
{
    [ProtoMember(1)] public string Username { get; set; } = string.Empty;
    [ProtoMember(2)] public string Password { get; set; } = string.Empty;
    [ProtoMember(3)] public string ClientVersion { get; set; } = string.Empty;
}

[ProtoContract]
public class LoginResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public string SessionToken { get; set; } = string.Empty;
    [ProtoMember(4)] public PlayerInfo? PlayerInfo { get; set; }
}

[ProtoContract]
public class LogoutRequest
{
    [ProtoMember(1)] public string SessionToken { get; set; } = string.Empty;
}

[ProtoContract]
public class LogoutResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}

// 이동 관련 메시지
[ProtoContract]
public class MoveRequest
{
    [ProtoMember(1)] public Vector3? TargetPosition { get; set; }
    [ProtoMember(2)] public float MovementSpeed { get; set; }
}

[ProtoContract]
public class MoveResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public Vector3? NewPosition { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}

// 월드/블록 관련 메시지
[ProtoContract]
public class WorldBlockChangeRequest
{
    [ProtoMember(1)] public string AreaId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SubworldId { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3Int? BlockPosition { get; set; }
    [ProtoMember(4)] public int BlockType { get; set; }
    [ProtoMember(5)] public int ChunkType { get; set; }
}

[ProtoContract]
public class WorldBlockChangeResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public long Timestamp { get; set; }
}

[ProtoContract]
public class WorldBlockChangeBroadcast
{
    [ProtoMember(1)] public string AreaId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SubworldId { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3Int? BlockPosition { get; set; }
    [ProtoMember(4)] public int BlockType { get; set; }
    [ProtoMember(5)] public int ChunkType { get; set; }
    [ProtoMember(6)] public string PlayerId { get; set; } = string.Empty;
    [ProtoMember(7)] public long Timestamp { get; set; }
}

// 채팅 관련 메시지
public enum ChatType
{
    Global = 0,
    Local = 1,
    Whisper = 2,
    System = 3
}

[ProtoContract]
public class ChatRequest
{
    [ProtoMember(1)] public string Message { get; set; } = string.Empty;
    [ProtoMember(2)] public int Type { get; set; } // ChatType을 int로 저장
    [ProtoMember(3)] public string TargetPlayer { get; set; } = string.Empty;
}

[ProtoContract]
public class ChatResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string ErrorMessage { get; set; } = string.Empty;
}

[ProtoContract]
public class ChatMessage
{
    [ProtoMember(1)] public string SenderId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SenderName { get; set; } = string.Empty;
    [ProtoMember(3)] public string Message { get; set; } = string.Empty;
    [ProtoMember(4)] public int Type { get; set; } // ChatType을 int로 저장
    [ProtoMember(5)] public long Timestamp { get; set; }
}

// 서버 상태 및 진단 메시지
[ProtoContract]
public class PingRequest
{
    [ProtoMember(1)] public long ClientTimestamp { get; set; }
}

[ProtoContract]
public class PingResponse
{
    [ProtoMember(1)] public long ClientTimestamp { get; set; }
    [ProtoMember(2)] public long ServerTimestamp { get; set; }
}

[ProtoContract]
public class ServerStatusRequest
{
    [ProtoMember(1)] public string SessionToken { get; set; } = string.Empty;
}

[ProtoContract]
public class ServerStatusResponse
{
    [ProtoMember(1)] public int OnlinePlayers { get; set; }
    [ProtoMember(2)] public string ServerVersion { get; set; } = string.Empty;
    [ProtoMember(3)] public long ServerUptime { get; set; }
}

[ProtoContract]
public class PlayerInfoUpdate
{
    [ProtoMember(1)] public PlayerInfo? PlayerInfo { get; set; }
    [ProtoMember(2)] public long Timestamp { get; set; }
}

// 인벤토리 관련 메시지
[ProtoContract]
public class InventorySlotData
{
    [ProtoMember(1)] public int SlotIndex { get; set; }
    [ProtoMember(2)] public string ItemId { get; set; } = string.Empty;
    [ProtoMember(3)] public int Amount { get; set; }
    [ProtoMember(4)] public string ItemData { get; set; } = string.Empty;
}

[ProtoContract]
public class InventoryRequest
{
    [ProtoMember(1)] public int Action { get; set; } // 0=Move, 1=Swap, 2=Split, 3=Drop
    [ProtoMember(2)] public int SourceSlot { get; set; }
    [ProtoMember(3)] public int TargetSlot { get; set; }
    [ProtoMember(4)] public int Amount { get; set; }
    [ProtoMember(5)] public string ItemId { get; set; } = string.Empty;
}

[ProtoContract]
public class InventoryResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public List<InventorySlotData> UpdatedSlots { get; set; } = new();
    [ProtoMember(4)] public long Timestamp { get; set; }
}

[ProtoContract]
public class InventoryUpdateBroadcast
{
    [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
    [ProtoMember(2)] public List<InventorySlotData> UpdatedSlots { get; set; } = new();
    [ProtoMember(3)] public long Timestamp { get; set; }
}

// 제작 관련 메시지
[ProtoContract]
public class CraftingIngredientData
{
    [ProtoMember(1)] public string ItemId { get; set; } = string.Empty;
    [ProtoMember(2)] public int Amount { get; set; }
}

[ProtoContract]
public class CraftingResultData
{
    [ProtoMember(1)] public string ItemId { get; set; } = string.Empty;
    [ProtoMember(2)] public int Amount { get; set; }
}

[ProtoContract]
public class RecipeData
{
    [ProtoMember(1)] public string RecipeId { get; set; } = string.Empty;
    [ProtoMember(2)] public string Name { get; set; } = string.Empty;
    [ProtoMember(3)] public List<CraftingIngredientData> Ingredients { get; set; } = new();
    [ProtoMember(4)] public List<CraftingResultData> Results { get; set; } = new();
    [ProtoMember(5)] public int CraftingType { get; set; } // 0=Hand, 1=Workbench, 2=Furnace
    [ProtoMember(6)] public int CraftingTime { get; set; }
}

[ProtoContract]
public class CraftingRequest
{
    [ProtoMember(1)] public string RecipeId { get; set; } = string.Empty;
    [ProtoMember(2)] public int CraftingAmount { get; set; } = 1;
    [ProtoMember(3)] public int CraftingType { get; set; }
}

[ProtoContract]
public class CraftedItemData
{
    [ProtoMember(1)] public string ItemId { get; set; } = string.Empty;
    [ProtoMember(2)] public int Amount { get; set; }
    [ProtoMember(3)] public int SlotIndex { get; set; }
}

[ProtoContract]
public class CraftingResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public string RecipeId { get; set; } = string.Empty;
    [ProtoMember(4)] public List<CraftedItemData> CraftedItems { get; set; } = new();
    [ProtoMember(5)] public string UpdatedInventory { get; set; } = string.Empty;
    [ProtoMember(6)] public long Timestamp { get; set; }
}

[ProtoContract]
public class RecipeListRequest
{
    [ProtoMember(1)] public int CraftingType { get; set; } = -1; // -1 = 모든 타입
}

[ProtoContract]
public class RecipeListResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public List<RecipeData> Recipes { get; set; } = new();
    [ProtoMember(3)] public long Timestamp { get; set; }
}

// 체력 및 허기 관련 메시지
[ProtoContract]
public class HealthActionRequest
{
    [ProtoMember(1)] public int ActionType { get; set; } // 0=Damage, 1=Heal, 2=Feed, 3=ConsumeHunger
    [ProtoMember(2)] public float Amount { get; set; }
    [ProtoMember(3)] public int DamageType { get; set; }
    [ProtoMember(4)] public int HealType { get; set; }
    [ProtoMember(5)] public float Saturation { get; set; }
}

[ProtoContract]
public class HealthActionResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public long Timestamp { get; set; }
}

[ProtoContract]
public class HealthUpdateMessage
{
    [ProtoMember(1)] public float Health { get; set; }
    [ProtoMember(2)] public float MaxHealth { get; set; }
    [ProtoMember(3)] public int Hunger { get; set; }
    [ProtoMember(4)] public int MaxHunger { get; set; }
    [ProtoMember(5)] public float Saturation { get; set; }
    [ProtoMember(6)] public long Timestamp { get; set; }
}

[ProtoContract]
public class RespawnRequest
{
    [ProtoMember(1)] public bool Force { get; set; } = false;
}

[ProtoContract]
public class RespawnResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3? RespawnPosition { get; set; }
    [ProtoMember(4)] public float Health { get; set; }
    [ProtoMember(5)] public float MaxHealth { get; set; }
    [ProtoMember(6)] public int Hunger { get; set; }
    [ProtoMember(7)] public int MaxHunger { get; set; }
    [ProtoMember(8)] public long Timestamp { get; set; }
}

[ProtoContract]
public class PlayerDeathMessage
{
    [ProtoMember(1)] public string PlayerName { get; set; } = string.Empty;
    [ProtoMember(2)] public string DeathMessage { get; set; } = string.Empty;
    [ProtoMember(3)] public int DamageType { get; set; }
    [ProtoMember(4)] public long Timestamp { get; set; }
}

[ProtoContract]
public class PlayerRespawnBroadcast
{
    [ProtoMember(1)] public string PlayerName { get; set; } = string.Empty;
    [ProtoMember(2)] public Vector3? RespawnPosition { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}
