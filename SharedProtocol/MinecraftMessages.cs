using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SharedProtocol
{
    // =============================================================================
    // 마인크래프트 전용 메시지 타입 확장
    // =============================================================================
    
    public enum MinecraftMessageType
    {
        // 플레이어 상태 및 액션
        PlayerStateUpdate = 100,
        PlayerActionRequest = 101,
        PlayerActionResponse = 102,
        
        // 블록 및 월드 관리 (마인크래프트 전용)
        ChunkDataRequest = 110,
        ChunkDataResponse = 111,
        BlockChangeNotification = 112,
        MultiBlockChange = 113,
        
        // 인벤토리 및 아이템
        InventoryUpdate = 120,
        ItemUse = 121,
        ItemDrop = 122,
        ItemPickup = 123,
        
        // 엔티티 관리
        EntitySpawn = 130,
        EntityDespawn = 131,
        EntityUpdate = 132,
        EntityInteract = 133,
        
        // 게임 메커니즘
        TimeUpdate = 140,
        WeatherChange = 141,
        SoundEffect = 142,
        ParticleEffect = 143,
        
        // 컨테이너 (상자, 화로 등)
        ContainerOpen = 150,
        ContainerClose = 151,
        ContainerUpdate = 152
    }
    
    // =============================================================================
    // 기본 데이터 구조체
    // =============================================================================
    
    [ProtoContract]
    public class Vector3D
    {
        [ProtoMember(1)] public double X { get; set; }
        [ProtoMember(2)] public double Y { get; set; }
        [ProtoMember(3)] public double Z { get; set; }
        
        public Vector3D() { }
        public Vector3D(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }
    }
    
    [ProtoContract]
    public class Vector3I
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Y { get; set; }
        [ProtoMember(3)] public int Z { get; set; }
        
        public Vector3I() { }
        public Vector3I(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
    }
    
    // =============================================================================
    // 플레이어 관련 메시지
    // =============================================================================
    
    /// <summary>
    /// 플레이어 상태 정보 - 마인크래프트 게임 모드, 건강, 허기 등 포함
    /// </summary>
    [ProtoContract]
    public class PlayerStateInfo
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Username { get; set; } = string.Empty;
        [ProtoMember(3)] public Vector3D Position { get; set; } = new();
        [ProtoMember(4)] public Vector3D Rotation { get; set; } = new();
        [ProtoMember(5)] public int Level { get; set; }
        [ProtoMember(6)] public int Experience { get; set; }
        [ProtoMember(7)] public float Health { get; set; } = 20.0f;
        [ProtoMember(8)] public float MaxHealth { get; set; } = 20.0f;
        [ProtoMember(9)] public float Hunger { get; set; } = 20.0f;
        [ProtoMember(10)] public float MaxHunger { get; set; } = 20.0f;
        [ProtoMember(11)] public GameMode GameMode { get; set; } = GameMode.Survival;
        [ProtoMember(12)] public List<InventoryItemInfo> Inventory { get; set; } = new();
        [ProtoMember(13)] public InventoryItemInfo HeldItem { get; set; } = new();
        [ProtoMember(14)] public int SelectedSlot { get; set; }
        [ProtoMember(15)] public bool IsOnGround { get; set; }
        [ProtoMember(16)] public bool IsSneaking { get; set; }
        [ProtoMember(17)] public bool IsSprinting { get; set; }
        [ProtoMember(18)] public bool IsFlying { get; set; }
    }
    
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }
    
    /// <summary>
    /// 플레이어 액션 요청 - 블록 부수기, 아이템 사용, 블록 설치 등
    /// </summary>
    [ProtoContract]
    public class PlayerActionRequestMessage
    {
        [ProtoMember(1)] public PlayerActionType Action { get; set; }
        [ProtoMember(2)] public Vector3I TargetPosition { get; set; } = new();
        [ProtoMember(3)] public int Face { get; set; } // 블록 면 (0-5)
        [ProtoMember(4)] public Vector3D CursorPosition { get; set; } = new();
        [ProtoMember(5)] public InventoryItemInfo UsedItem { get; set; } = new();
        [ProtoMember(6)] public int Sequence { get; set; }
        [ProtoMember(7)] public long Timestamp { get; set; }
    }
    
    public enum PlayerActionType
    {
        StartDestroyBlock = 0,
        AbortDestroyBlock = 1,
        StopDestroyBlock = 2,
        PlaceBlock = 3,
        UseItem = 4,
        DropItem = 5,
        RightClickBlock = 6,
        RightClickAir = 7,
        SwapHands = 8
    }
    
    [ProtoContract]
    public class PlayerActionResponseMessage
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public float BreakProgress { get; set; }
        [ProtoMember(4)] public int Sequence { get; set; }
        [ProtoMember(5)] public List<InventoryItemInfo> UpdatedItems { get; set; } = new();
        [ProtoMember(6)] public List<ItemDropInfo> DroppedItems { get; set; } = new();
    }
    
    // =============================================================================
    // 아이템 및 인벤토리 시스템
    // =============================================================================
    
    /// <summary>
    /// 인벤토리 아이템 정보 - 내구도, 인챈트 등 지원
    /// </summary>
    [ProtoContract]
    public class InventoryItemInfo
    {
        [ProtoMember(1)] public int ItemId { get; set; }
        [ProtoMember(2)] public string ItemName { get; set; } = string.Empty;
        [ProtoMember(3)] public int Quantity { get; set; }
        [ProtoMember(4)] public int Durability { get; set; }
        [ProtoMember(5)] public int MaxDurability { get; set; }
        [ProtoMember(6)] public List<EnchantmentInfo> Enchantments { get; set; } = new();
        [ProtoMember(7)] public string CustomData { get; set; } = string.Empty; // NBT 데이터
        [ProtoMember(8)] public ItemType ItemType { get; set; }
    }
    
    public enum ItemType
    {
        Block = 0,
        Tool = 1,
        Weapon = 2,
        Armor = 3,
        Food = 4,
        Material = 5,
        Misc = 6
    }
    
    [ProtoContract]
    public class EnchantmentInfo
    {
        [ProtoMember(1)] public int EnchantId { get; set; }
        [ProtoMember(2)] public int Level { get; set; }
    }
    
    [ProtoContract]
    public class ItemDropInfo
    {
        [ProtoMember(1)] public InventoryItemInfo Item { get; set; } = new();
        [ProtoMember(2)] public Vector3D DropPosition { get; set; } = new();
        [ProtoMember(3)] public Vector3D Velocity { get; set; } = new();
        [ProtoMember(4)] public string EntityId { get; set; } = string.Empty;
    }
    
    // =============================================================================
    // 블록 및 청크 시스템
    // =============================================================================
    
    /// <summary>
    /// 블록 정보 - 위치, 타입, 메타데이터, 조명 등 포함
    /// </summary>
    [ProtoContract]
    public class BlockInfo
    {
        [ProtoMember(1)] public int BlockId { get; set; }
        [ProtoMember(2)] public Vector3I Position { get; set; } = new();
        [ProtoMember(3)] public int Metadata { get; set; }
        [ProtoMember(4)] public string BlockEntityData { get; set; } = string.Empty;
        [ProtoMember(5)] public long LastUpdate { get; set; }
        [ProtoMember(6)] public LightLevelInfo LightLevel { get; set; } = new();
    }
    
    [ProtoContract]
    public class LightLevelInfo
    {
        [ProtoMember(1)] public int BlockLight { get; set; } // 0-15
        [ProtoMember(2)] public int SkyLight { get; set; } // 0-15
    }
    
    /// <summary>
    /// 청크 데이터 요청
    /// </summary>
    [ProtoContract]
    public class ChunkDataRequestMessage
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ViewDistance { get; set; }
    }
    
    /// <summary>
    /// 청크 데이터 응답 - 압축된 블록 데이터와 엔티티 정보 포함
    /// </summary>
    [ProtoContract]
    public class ChunkDataResponseMessage
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public bool Success { get; set; }
        [ProtoMember(4)] public byte[] CompressedBlockData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public List<EntityInfo> Entities { get; set; } = new();
        [ProtoMember(6)] public BiomeInfo BiomeData { get; set; } = new();
        [ProtoMember(7)] public bool IsFromCache { get; set; }
    }
    
    [ProtoContract]
    public class BiomeInfo
    {
        [ProtoMember(1)] public List<int> BiomeIds { get; set; } = new(); // 16x16 바이옴 배열
        [ProtoMember(2)] public float Temperature { get; set; }
        [ProtoMember(3)] public float Humidity { get; set; }
    }
    
    /// <summary>
    /// 블록 변경 알림 - 다른 플레이어들에게 브로드캐스트
    /// </summary>
    [ProtoContract]
    public class BlockChangeNotificationMessage
    {
        [ProtoMember(1)] public Vector3I Position { get; set; } = new();
        [ProtoMember(2)] public int OldBlockId { get; set; }
        [ProtoMember(3)] public int NewBlockId { get; set; }
        [ProtoMember(4)] public int Metadata { get; set; }
        [ProtoMember(5)] public string PlayerName { get; set; } = string.Empty;
        [ProtoMember(6)] public long Timestamp { get; set; }
        [ProtoMember(7)] public List<ItemDropInfo> Drops { get; set; } = new();
    }
    
    // =============================================================================
    // 엔티티 시스템
    // =============================================================================
    
    /// <summary>
    /// 엔티티 정보 - 몹, 드롭 아이템, 투사체 등
    /// </summary>
    [ProtoContract]
    public class EntityInfo
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public EntityType EntityType { get; set; }
        [ProtoMember(3)] public Vector3D Position { get; set; } = new();
        [ProtoMember(4)] public Vector3D Rotation { get; set; } = new();
        [ProtoMember(5)] public Vector3D Velocity { get; set; } = new();
        [ProtoMember(6)] public float Health { get; set; }
        [ProtoMember(7)] public float MaxHealth { get; set; }
        [ProtoMember(8)] public string CustomData { get; set; } = string.Empty;
    }
    
    public enum EntityType
    {
        Unknown = 0,
        Player = 1,
        // 적대적 몹
        Zombie = 10,
        Skeleton = 11,
        Creeper = 12,
        Spider = 13,
        Enderman = 14,
        // 중립/평화 몹
        Pig = 20,
        Cow = 21,
        Sheep = 22,
        Chicken = 23,
        Horse = 24,
        Wolf = 25,
        // 아이템 엔티티
        DroppedItem = 30,
        ExperienceOrb = 31,
        Arrow = 32
    }
    
    [ProtoContract]
    public class EntitySpawnMessage
    {
        [ProtoMember(1)] public EntityInfo Entity { get; set; } = new();
        [ProtoMember(2)] public SpawnReason SpawnReason { get; set; }
    }
    
    public enum SpawnReason
    {
        Natural = 0,
        Spawner = 1,
        Breeding = 2,
        Command = 3,
        ItemDrop = 4
    }
    
    [ProtoContract]
    public class EntityUpdateMessage
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public Vector3D Position { get; set; } = new();
        [ProtoMember(3)] public Vector3D Rotation { get; set; } = new();
        [ProtoMember(4)] public Vector3D Velocity { get; set; } = new();
        [ProtoMember(5)] public float Health { get; set; }
        [ProtoMember(6)] public EntityUpdateFlags UpdateFlags { get; set; } = new();
    }
    
    [ProtoContract]
    public class EntityUpdateFlags
    {
        [ProtoMember(1)] public bool PositionUpdated { get; set; }
        [ProtoMember(2)] public bool RotationUpdated { get; set; }
        [ProtoMember(3)] public bool VelocityUpdated { get; set; }
        [ProtoMember(4)] public bool HealthUpdated { get; set; }
    }
    
    // =============================================================================
    // 게임 환경 및 효과
    // =============================================================================
    
    /// <summary>
    /// 게임 시간 업데이트
    /// </summary>
    [ProtoContract]
    public class TimeUpdateMessage
    {
        [ProtoMember(1)] public long WorldTime { get; set; }
        [ProtoMember(2)] public long DayTime { get; set; } // 0-24000
    }
    
    /// <summary>
    /// 날씨 변경
    /// </summary>
    [ProtoContract]
    public class WeatherChangeMessage
    {
        [ProtoMember(1)] public WeatherType WeatherType { get; set; }
        [ProtoMember(2)] public int Duration { get; set; } // 틱 단위
        [ProtoMember(3)] public float Intensity { get; set; } // 0.0 - 1.0
    }
    
    public enum WeatherType
    {
        Clear = 0,
        Rain = 1,
        Thunderstorm = 2,
        Snow = 3
    }
    
    /// <summary>
    /// 사운드 효과 브로드캐스트
    /// </summary>
    [ProtoContract]
    public class SoundEffectMessage
    {
        [ProtoMember(1)] public SoundType SoundType { get; set; }
        [ProtoMember(2)] public Vector3D Position { get; set; } = new();
        [ProtoMember(3)] public float Volume { get; set; } = 1.0f;
        [ProtoMember(4)] public float Pitch { get; set; } = 1.0f;
        [ProtoMember(5)] public int ViewDistance { get; set; } = 64;
    }
    
    public enum SoundType
    {
        BlockBreakStone = 0,
        BlockBreakWood = 1,
        BlockPlaceStone = 2,
        FootstepStone = 10,
        FootstepWood = 11,
        ItemPickup = 20,
        LevelUp = 21
    }
    
    /// <summary>
    /// 파티클 효과 브로드캐스트
    /// </summary>
    [ProtoContract]
    public class ParticleEffectMessage
    {
        [ProtoMember(1)] public ParticleType ParticleType { get; set; }
        [ProtoMember(2)] public Vector3D Position { get; set; } = new();
        [ProtoMember(3)] public Vector3D Velocity { get; set; } = new();
        [ProtoMember(4)] public int Count { get; set; } = 1;
        [ProtoMember(5)] public float Spread { get; set; } = 0.0f;
        [ProtoMember(6)] public string ExtraData { get; set; } = string.Empty;
    }
    
    public enum ParticleType
    {
        BlockBreak = 0,
        BlockDust = 1,
        WaterSplash = 2,
        Smoke = 3,
        Flame = 4,
        CriticalHit = 5
    }
}