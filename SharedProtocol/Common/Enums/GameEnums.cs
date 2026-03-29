namespace SharedProtocol.Common.Enums;

/// <summary>
/// Gameplay enumeration types
/// </summary>
public static class GameEnums
{
    /// <summary>
    /// Game modes
    /// </summary>
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }
    
    /// <summary>
    /// Difficulty levels
    /// </summary>
    public enum Difficulty
    {
        Peaceful = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3
    }
    
    /// <summary>
    /// Entity types
    /// </summary>
    public enum EntityType
    {
        Unknown = 0,
        Player = 1,
        // Hostile mobs
        Zombie = 10,
        Skeleton = 11,
        Creeper = 12,
        Spider = 13,
        Enderman = 14,
        Witch = 15,
        Slime = 16,
        // Neutral/Passive mobs
        Pig = 20,
        Cow = 21,
        Sheep = 22,
        Chicken = 23,
        Horse = 24,
        Wolf = 25,
        Cat = 26,
        Villager = 27,
        // Other
        DroppedItem = 30,
        Arrow = 31,
        ExperienceOrb = 32,
        Boat = 33,
        Minecart = 34,
        Fireball = 35
    }
}
