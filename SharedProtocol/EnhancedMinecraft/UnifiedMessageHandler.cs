using System;
using System.IO;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using ProtoBuf;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Unified message handler that can process both Google.Protobuf and protobuf-net messages.
    /// Provides consistent error handling and serialization helpers.
    /// </summary>
    public abstract class UnifiedMessageHandler<TMessage> where TMessage : class
    {
        protected readonly MinecraftMessageType MessageType;

        protected UnifiedMessageHandler(MinecraftMessageType messageType)
        {
            MessageType = messageType;
            ProtocolStandardization.ValidateDependencies();
            ProtocolValidator.ValidateMessageContract<TMessage>(messageType);
        }

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

        protected async Task SendResponseAsync<TResponse>(Session session, TResponse response)
            where TResponse : class
        {
            try
            {
                byte[] payload;
                if (response is IMessage protobufMessage)
                {
                    payload = ProtocolStandardization.SerializeMessage(protobufMessage);
                }
                else
                {
                    using var stream = new MemoryStream();
                    Serializer.Serialize(stream, response);
                    payload = stream.ToArray();
                }

                await session.SendAsync((int)MessageType, payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending response for {MessageType}: {ex.Message}");
            }
        }

        protected virtual TMessage? DeserializeMessage(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(data);
                return Serializer.Deserialize<TMessage>(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize message {MessageType}: {ex.Message}");
                return null;
            }
        }

        protected abstract Task ProcessMessageAsync(Session session, TMessage message);

        protected virtual async Task HandleErrorAsync(Session session, Exception exception)
        {
            Console.WriteLine($"[{MessageType}] Error: {ProtocolStandardization.GetProtocolError(exception, "message processing")}");
            if (session != null)
            {
                await SendErrorResponseAsync(session, exception.Message);
            }
        }

        protected virtual Task SendErrorResponseAsync(Session session, string errorMessage)
        {
            Console.WriteLine($"[{MessageType}] Sending error response: {errorMessage}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Specialized handler for EnhancedMinecraft Google.Protobuf messages.
    /// </summary>
    public abstract class EnhancedMinecraftHandler<TMessage> : UnifiedMessageHandler<TMessage>
        where TMessage : class, IMessage<TMessage>, new()
    {
        protected EnhancedMinecraftHandler(MinecraftMessageType messageType) : base(messageType)
        {
        }

        protected override TMessage? DeserializeMessage(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

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

        protected virtual IMessage? CreateErrorResponse(string errorMessage)
        {
            return null;
        }
    }

    /// <summary>
    /// Specialized handler for legacy protobuf-net messages.
    /// </summary>
    public abstract class LegacyMessageHandler<TMessage> : UnifiedMessageHandler<TMessage>
        where TMessage : class
    {
        protected LegacyMessageHandler(MinecraftMessageType messageType) : base(messageType)
        {
        }

        protected override TMessage? DeserializeMessage(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(data);
                return Serializer.Deserialize<TMessage>(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize legacy message {MessageType}: {ex.Message}");
                return null;
            }
        }
    }
}
