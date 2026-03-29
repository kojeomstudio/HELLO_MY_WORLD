namespace SharedProtocol.Common.Interfaces;

/// <summary>
/// Interface for shared protocol functionality
/// </summary>
public interface ISharedProtocol
{
    /// <summary>
    /// Gets the protocol version
    /// </summary>
    string ProtocolVersion { get; }
    
    /// <summary>
    /// Gets the protocol name
    /// </summary>
    string ProtocolName { get; }
    
    /// <summary>
    /// Validates the protocol implementation
    /// </summary>
    bool Validate();
}
