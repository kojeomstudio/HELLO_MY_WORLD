using System;
using System.IO;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Unified message handler that can process both Google.Protobuf and protobuf-net messages
/// Provides consistent interface for all protocol messages
/// </summary>
public abstract class UnifiedMessageHandler<TMessage> where TMessage : class
{
    protected readonly MinecraftMessageType MessageType;

    protected UnifiedMessageHandler(MinecraftMessageType messageType)
    {
        MessageType = messageType;
        ProtocolStandardization.ValidateDependencies();
    }

    /// <summary>
    /// Handles incoming message data, automatically detecting and deserializing
    /// using the appropriate protobuf implementation
    /// </summary>
    public async Task HandleAsync(Session session, byte[] messageData)
    {
        try
        {
            var message = DeserializeMessage(messageData);
            if (message != null)
            {
                await ProcessMessageAsync(session, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message {MessageType}: {ex.Message}");
            await HandleErrorAsync(session, ex);
        }
    }

    /// <summary>
    /// Serializes a response message using the appropriate protobuf implementation
    /// </summary>
    protected async Task SendResponseAsync<TResponse>(Session session, TResponse response) 
        where TResponse : class
    {
        try
        {
            var serialized = ProtocolStandardization.SerializeMessage(response);
            await session.SendAsync((int)MessageType, serialized);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response for {MessageType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes incoming message data using the appropriate protobuf implementation
    /// </summary>
    private TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            // Try Google.Protobuf first (EnhancedMinecraftProtocol)
            if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)))
            {
                return ProtocolStandardization.DeserializeMessage<TMessage>(data);
            }

            // Fall back to protobuf-net for legacy messages
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize message {MessageType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes the deserialized message - must be implemented by concrete handlers
    /// </summary>
    protected abstract Task ProcessMessageAsync(Session session, TMessage message);

    /// <summary>
    /// Handles errors during message processing
    /// </summary>
    protected virtual async Task HandleErrorAsync(Session session, Exception exception)
    {
        // Default error handling - can be overridden by concrete handlers
        Console.WriteLine($"[{MessageType}] Error: {ProtocolStandardization.GetProtocolError(exception, "message processing")}");
        
        // Send error response if applicable
        if (session != null)
        {
            await SendErrorResponseAsync(session, exception.Message);
        }
    }

    /// <summary>
    /// Sends an error response to the client
    /// </summary>
    protected virtual async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Default implementation - can be overridden for specific error response types
        Console.WriteLine($"[{MessageType}] Sending error response: {errorMessage}");
    }
}

/// <summary>
/// Specialized handler for EnhancedMinecraft Google.Protobuf messages
/// </summary>
public abstract class EnhancedMinecraftHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class, IMessage, new()
{
    protected EnhancedMinecraftHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            var parser = ProtocolStandardization.GetParser<TMessage>();
            return parser.ParseFrom(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize EnhancedMinecraft message {MessageType}: {ex.Message}");
            return null;
        }
    }

    protected override async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Try to create appropriate error response for EnhancedMinecraft messages
        try
        {
            var errorResponse = CreateErrorResponse(errorMessage);
            if (errorResponse != null)
            {
                await SendResponseAsync(session, errorResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create error response: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an error response appropriate for the message type
    /// Can be overridden by concrete handlers to provide specific error responses
    /// </summary>
    protected virtual IMessage? CreateErrorResponse(string errorMessage)
    {
        // Default implementation - no specific error response
        return null;
    }
}

/// <summary>
/// Specialized handler for legacy protobuf-net messages
/// </summary>
public abstract class LegacyMessageHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class
{
    protected LegacyMessageHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize legacy message {MessageType}: {ex.Message}");
            return null;
        }
    }
}
using System.IO;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Unified message handler that can process both Google.Protobuf and protobuf-net messages
/// Provides consistent interface for all protocol messages
/// </summary>
public abstract class UnifiedMessageHandler<TMessage> where TMessage : class
{
    protected readonly MinecraftMessageType MessageType;

    protected UnifiedMessageHandler(MinecraftMessageType messageType)
    {
        MessageType = messageType;
        ProtocolStandardization.ValidateDependencies();
    }

    /// <summary>
    /// Handles incoming message data, automatically detecting and deserializing
    /// using the appropriate protobuf implementation
    /// </summary>
    public async Task HandleAsync(Session session, byte[] messageData)
    {
        try
        {
            var message = DeserializeMessage(messageData);
            if (message != null)
            {
                await ProcessMessageAsync(session, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message {MessageType}: {ex.Message}");
            await HandleErrorAsync(session, ex);
        }
    }

    /// <summary>
    /// Serializes a response message using the appropriate protobuf implementation
    /// </summary>
    protected async Task SendResponseAsync<TResponse>(Session session, TResponse response) 
        where TResponse : class
    {
        try
        {
            var serialized = ProtocolStandardization.SerializeMessage(response);
            await session.SendAsync((int)MessageType, serialized);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response for {MessageType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes incoming message data using the appropriate protobuf implementation
    /// </summary>
    private TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            // Try Google.Protobuf first (EnhancedMinecraftProtocol)
            if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)))
            {
                return ProtocolStandardization.DeserializeMessage<TMessage>(data);
            }

            // Fall back to protobuf-net for legacy messages
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize message {MessageType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes the deserialized message - must be implemented by concrete handlers
    /// </summary>
    protected abstract Task ProcessMessageAsync(Session session, TMessage message);

    /// <summary>
    /// Handles errors during message processing
    /// </summary>
    protected virtual async Task HandleErrorAsync(Session session, Exception exception)
    {
        // Default error handling - can be overridden by concrete handlers
        Console.WriteLine($"[{MessageType}] Error: {ProtocolStandardization.GetProtocolError(exception, "message processing")}");
        
        // Send error response if applicable
        if (session != null)
        {
            await SendErrorResponseAsync(session, exception.Message);
        }
    }

    /// <summary>
    /// Sends an error response to the client
    /// </summary>
    protected virtual async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Default implementation - can be overridden for specific error response types
        Console.WriteLine($"[{MessageType}] Sending error response: {errorMessage}");
    }
}

/// <summary>
/// Specialized handler for EnhancedMinecraft Google.Protobuf messages
/// </summary>
public abstract class EnhancedMinecraftHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class, IMessage, new()
{
    protected EnhancedMinecraftHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            var parser = ProtocolStandardization.GetParser<TMessage>();
            return parser.ParseFrom(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize EnhancedMinecraft message {MessageType}: {ex.Message}");
            return null;
        }
    }

    protected override async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Try to create appropriate error response for EnhancedMinecraft messages
        try
        {
            var errorResponse = CreateErrorResponse(errorMessage);
            if (errorResponse != null)
            {
                await SendResponseAsync(session, errorResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create error response: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an error response appropriate for the message type
    /// Can be overridden by concrete handlers to provide specific error responses
    /// </summary>
    protected virtual IMessage? CreateErrorResponse(string errorMessage)
    {
        // Default implementation - no specific error response
        return null;
    }
}

/// <summary>
/// Specialized handler for legacy protobuf-net messages
/// </summary>
public abstract class LegacyMessageHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class
{
    protected LegacyMessageHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize legacy message {MessageType}: {ex.Message}");
            return null;
        }
    }
}
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Unified message handler that can process both Google.Protobuf and protobuf-net messages
/// Provides consistent interface for all protocol messages
/// </summary>
public abstract class UnifiedMessageHandler<TMessage> where TMessage : class
{
    protected readonly MinecraftMessageType MessageType;

    protected UnifiedMessageHandler(MinecraftMessageType messageType)
    {
        MessageType = messageType;
        ProtocolStandardization.ValidateDependencies();
    }

    /// <summary>
    /// Handles incoming message data, automatically detecting and deserializing
    /// using the appropriate protobuf implementation
    /// </summary>
    public async Task HandleAsync(Session session, byte[] messageData)
    {
        try
        {
            var message = DeserializeMessage(messageData);
            if (message != null)
            {
                await ProcessMessageAsync(session, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message {MessageType}: {ex.Message}");
            await HandleErrorAsync(session, ex);
        }
    }

    /// <summary>
    /// Serializes a response message using the appropriate protobuf implementation
    /// </summary>
    protected async Task SendResponseAsync<TResponse>(Session session, TResponse response) 
        where TResponse : class
    {
        try
        {
            var serialized = ProtocolStandardization.SerializeMessage(response);
            await session.SendAsync((int)MessageType, serialized);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response for {MessageType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes incoming message data using the appropriate protobuf implementation
    /// </summary>
    private TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            // Try Google.Protobuf first (EnhancedMinecraftProtocol)
            if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)))
            {
                return ProtocolStandardization.DeserializeMessage<TMessage>(data);
            }

            // Fall back to protobuf-net for legacy messages
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize message {MessageType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes the deserialized message - must be implemented by concrete handlers
    /// </summary>
    protected abstract Task ProcessMessageAsync(Session session, TMessage message);

    /// <summary>
    /// Handles errors during message processing
    /// </summary>
    protected virtual async Task HandleErrorAsync(Session session, Exception exception)
    {
        // Default error handling - can be overridden by concrete handlers
        Console.WriteLine($"[{MessageType}] Error: {ProtocolStandardization.GetProtocolError(exception, "message processing")}");
        
        // Send error response if applicable
        if (session != null)
        {
            await SendErrorResponseAsync(session, exception.Message);
        }
    }

    /// <summary>
    /// Sends an error response to the client
    /// </summary>
    protected virtual async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Default implementation - can be overridden for specific error response types
        Console.WriteLine($"[{MessageType}] Sending error response: {errorMessage}");
    }
}

/// <summary>
/// Specialized handler for EnhancedMinecraft Google.Protobuf messages
/// </summary>
public abstract class EnhancedMinecraftHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class, IMessage, new()
{
    protected EnhancedMinecraftHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            var parser = ProtocolStandardization.GetParser<TMessage>();
            return parser.ParseFrom(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize EnhancedMinecraft message {MessageType}: {ex.Message}");
            return null;
        }
    }

    protected override async Task SendErrorResponseAsync(Session session, string errorMessage)
    {
        // Try to create appropriate error response for EnhancedMinecraft messages
        try
        {
            var errorResponse = CreateErrorResponse(errorMessage);
            if (errorResponse != null)
            {
                await SendResponseAsync(session, errorResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create error response: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an error response appropriate for the message type
    /// Can be overridden by concrete handlers to provide specific error responses
    /// </summary>
    protected virtual IMessage? CreateErrorResponse(string errorMessage)
    {
        // Default implementation - no specific error response
        return null;
    }
}

/// <summary>
/// Specialized handler for legacy protobuf-net messages
/// </summary>
public abstract class LegacyMessageHandler<TMessage> : UnifiedMessageHandler<TMessage> 
    where TMessage : class
{
    protected LegacyMessageHandler(MinecraftMessageType messageType) : base(messageType)
    {
    }

    protected override TMessage? DeserializeMessage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            using var stream = new MemoryStream(data);
            return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize legacy message {MessageType}: {ex.Message}");
            return null;
        }
    }
}
