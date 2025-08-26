namespace SharedProtocol;

public interface IMessageHandler
{
    MessageType Type { get; }
    Task HandleAsync(Session session, object message);
}

public abstract class MessageHandler<T> : IMessageHandler
{
    public MessageType Type { get; }
    protected MessageHandler(MessageType type) => Type = type;
    public Task HandleAsync(Session session, object message) => HandleAsync(session, (T)message);
    protected abstract Task HandleAsync(Session session, T message);
}

public class MessageDispatcher
{
    private readonly Dictionary<MessageType, IMessageHandler> _handlers = new();

    public void Register(IMessageHandler handler) => _handlers[handler.Type] = handler;

    public Task DispatchAsync(Session session, MessageType type, object message)
    {
        if (_handlers.TryGetValue(type, out var handler))
            return handler.HandleAsync(session, message);
        return Task.CompletedTask;
    }
}
