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

/// <summary>
/// 메시지 디스패처 - 수신된 메시지를 적절한 핸들러로 라우팅합니다.
/// </summary>
public class MessageDispatcher
{
    private readonly Dictionary<MessageType, IMessageHandler> _handlers = new();

    /// <summary>
    /// 등록된 핸들러 수
    /// </summary>
    public int HandlerCount => _handlers.Count;

    /// <summary>
    /// 메시지 핸들러를 등록합니다.
    /// </summary>
    public void Register(IMessageHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        
        _handlers[handler.Type] = handler;
        Console.WriteLine($"Registered handler for {handler.Type}: {handler.GetType().Name}");
    }

    /// <summary>
    /// 메시지를 적절한 핸들러로 디스패치합니다.
    /// </summary>
    public async Task DispatchAsync(Session session, MessageType type, object message)
    {
        try
        {
            if (_handlers.TryGetValue(type, out var handler))
            {
                await handler.HandleAsync(session, message);
            }
            else
            {
                Console.WriteLine($"No handler registered for message type: {type}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dispatching message {type}: {ex.Message}");
            // 예외를 재발생시키지 않음 - 서버가 중단되지 않도록
        }
    }

    /// <summary>
    /// 등록된 모든 메시지 타입을 반환합니다.
    /// </summary>
    public IReadOnlyCollection<MessageType> RegisteredMessageTypes => _handlers.Keys.ToList();
}
