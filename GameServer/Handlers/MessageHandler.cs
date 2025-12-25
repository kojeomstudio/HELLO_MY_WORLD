using System.Threading.Tasks;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// Base class for all message handlers in the GameServer
    /// Provides common functionality for handling protobuf messages
    /// </summary>
    public abstract class MessageHandler<T> : IMessageHandler where T : class
    {
        public MessageType Type { get; }
        
        protected MessageHandler(MessageType type)
        {
            Type = type;
        }
        
        public Task HandleAsync(Session session, object message)
        {
            return HandleAsync(session, (T)message);
        }
        
        protected abstract Task HandleAsync(Session session, T message);
    }
