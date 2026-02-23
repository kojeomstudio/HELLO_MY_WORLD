namespace SharedProtocol.Common.Enums;

/// <summary>
/// Core protocol enumeration types
/// NOTE: MessageType enum has been consolidated into SharedProtocol/Messages.cs
/// to avoid duplication and ensure single source of truth.
/// </summary>
public static class CoreEnums
{
    
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
