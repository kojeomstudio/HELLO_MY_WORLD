namespace SharedProtocol.Common.Enums;

/// <summary>
/// Combat-related enumeration types
/// </summary>
public static class CombatEnums
{
    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction
    {
        // Block actions
        StartDestroyBlock = 0,
        AbortDestroyBlock = 1,
        FinishDestroyBlock = 2,
        PlaceBlock = 3,
        RightClickBlock = 4,
        
        // Item actions
        UseItem = 10,
        DropItem = 11,
        DropItemStack = 12,
        EatFood = 13,
        DrinkPotion = 14,
        
        // Combat actions
        AttackEntity = 20,
        ShootBow = 21,
        BlockWithShield = 22,
        
        // Movement
        Interact = 30,
        SneakStart = 31,
        SneakStop = 32,
        SprintStart = 33,
        SprintStop = 34,
        Jump = 35
    }
    
    /// <summary>
    /// Damage types
    /// </summary>
    public enum DamageType
    {
        Generic = 0,
        EntityAttack = 1,
        Projectile = 2,
        Fall = 3,
        Fire = 4,
        FireTick = 5,
        Lava = 6,
        Drowning = 7,
        Suffocation = 8,
        Explosion = 9,
        Void = 10,
        Poison = 11,
        Magic = 12,
        Wither = 13,
        Anvil = 14,
        Cactus = 15,
        Lightning = 16,
        Starvation = 17
    }
}

/// <summary>
/// Combat-related enumeration types
/// </summary>
public static class CombatEnums
{
    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction
    {
        // Block actions
        StartDestroyBlock = 0,
        AbortDestroyBlock = 1,
        FinishDestroyBlock = 2,
        PlaceBlock = 3,
        RightClickBlock = 4,
        
        // Item actions
        UseItem = 10,
        DropItem = 11,
        DropItemStack = 12,
        EatFood = 13,
        DrinkPotion = 14,
        
        // Combat actions
        AttackEntity = 20,
        ShootBow = 21,
        BlockWithShield = 22,
        
        // Movement
        Interact = 30,
        SneakStart = 31,
        SneakStop = 32,
        SprintStart = 33,
        SprintStop = 34,
        Jump = 35
    }
    
    /// <summary>
    /// Damage types
    /// </summary>
    public enum DamageType
    {
        Generic = 0,
        EntityAttack = 1,
        Projectile = 2,
        Fall = 3,
        Fire = 4,
        FireTick = 5,
        Lava = 6,
        Drowning = 7,
        Suffocation = 8,
        Explosion = 9,
        Void = 10,
        Poison = 11,
        Magic = 12,
        Wither = 13,
        Anvil = 14,
        Cactus = 15,
        Lightning = 16,
        Starvation = 17
    }
}

