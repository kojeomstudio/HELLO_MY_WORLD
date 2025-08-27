using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 채팅 메시지를 처리하는 핸들러
/// 글로벌, 로컬, 귓속말 채팅을 지원합니다.
/// </summary>
public class ChatHandler : MessageHandler<ChatRequest>
{
    private readonly SessionManager _sessions;
    private readonly HashSet<string> _bannedWords = new() { "badword1", "badword2" }; // 금지어 목록

    public ChatHandler(SessionManager sessions) : base(MessageType.ChatRequest)
    {
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, ChatRequest message)
    {
        try
        {
            // 세션 인증 확인
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendChatFailure(session, "인증되지 않은 세션입니다.");
                return;
            }

            // 메시지 검증
            if (string.IsNullOrWhiteSpace(message.Message))
            {
                await SendChatFailure(session, "빈 메시지는 전송할 수 없습니다.");
                return;
            }

            if (message.Message.Length > 500) // 최대 500자 제한
            {
                await SendChatFailure(session, "메시지가 너무 깁니다. (최대 500자)");
                return;
            }

            // 금지어 필터링
            if (ContainsBannedWords(message.Message))
            {
                await SendChatFailure(session, "부적절한 언어가 포함되어 있습니다.");
                return;
            }

            // 채팅 타입에 따라 처리
            var chatType = (ChatType)message.Type;
            switch (chatType)
            {
                case ChatType.Global:
                    await HandleGlobalChat(session, message);
                    break;
                case ChatType.Local:
                    await HandleLocalChat(session, message);
                    break;
                case ChatType.Whisper:
                    await HandleWhisperChat(session, message);
                    break;
                case ChatType.System:
                    await SendChatFailure(session, "시스템 메시지는 서버에서만 전송할 수 있습니다.");
                    return;
                default:
                    await SendChatFailure(session, "알 수 없는 채팅 타입입니다.");
                    return;
            }

            // 성공 응답
            var response = new ChatResponse { Success = true };
            await session.SendAsync(MessageType.ChatResponse, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chat error for user '{session.UserName}': {ex.Message}");
            await SendChatFailure(session, "채팅 처리 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// 글로벌 채팅을 처리합니다.
    /// </summary>
    private async Task HandleGlobalChat(Session senderSession, ChatRequest message)
    {
        var chatMessage = new ChatMessage
        {
            SenderId = senderSession.PlayerInfo?.PlayerId ?? senderSession.UserName ?? "Unknown",
            SenderName = senderSession.UserName ?? "Unknown",
            Message = message.Message,
            Type = (int)ChatType.Global,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 모든 연결된 플레이어에게 브로드캐스트
        var tasks = new List<Task>();
        foreach (var playerName in _sessions.ConnectedUsers)
        {
            var playerSession = _sessions.GetSession(playerName);
            if (playerSession != null)
            {
                tasks.Add(playerSession.SendAsync(MessageType.ChatMessage, chatMessage));
            }
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"[GLOBAL] {senderSession.UserName}: {message.Message}");
    }

    /// <summary>
    /// 로컬 채팅을 처리합니다. (근처 플레이어만)
    /// </summary>
    private async Task HandleLocalChat(Session senderSession, ChatRequest message)
    {
        var chatMessage = new ChatMessage
        {
            SenderId = senderSession.PlayerInfo?.PlayerId ?? senderSession.UserName ?? "Unknown",
            SenderName = senderSession.UserName ?? "Unknown",
            Message = message.Message,
            Type = (int)ChatType.Local,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // TODO: 실제로는 위치 기반으로 근처 플레이어만 필터링해야 함
        // 현재는 모든 플레이어에게 전송 (데모용)
        var tasks = new List<Task>();
        foreach (var playerName in _sessions.ConnectedUsers)
        {
            var playerSession = _sessions.GetSession(playerName);
            if (playerSession != null)
            {
                tasks.Add(playerSession.SendAsync(MessageType.ChatMessage, chatMessage));
            }
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"[LOCAL] {senderSession.UserName}: {message.Message}");
    }

    /// <summary>
    /// 귓속말 채팅을 처리합니다.
    /// </summary>
    private async Task HandleWhisperChat(Session senderSession, ChatRequest message)
    {
        if (string.IsNullOrWhiteSpace(message.TargetPlayer))
        {
            await SendChatFailure(senderSession, "귓속말 대상을 지정해주세요.");
            return;
        }

        var targetSession = _sessions.GetSession(message.TargetPlayer);
        if (targetSession == null)
        {
            await SendChatFailure(senderSession, "대상 플레이어를 찾을 수 없습니다.");
            return;
        }

        var chatMessage = new ChatMessage
        {
            SenderId = senderSession.PlayerInfo?.PlayerId ?? senderSession.UserName ?? "Unknown",
            SenderName = senderSession.UserName ?? "Unknown",
            Message = message.Message,
            Type = (int)ChatType.Whisper,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 대상에게만 전송
        await targetSession.SendAsync(MessageType.ChatMessage, chatMessage);
        
        // 발신자에게도 확인 메시지 전송 (선택사항)
        var confirmMessage = new ChatMessage
        {
            SenderId = "System",
            SenderName = "System",
            Message = $"[귓속말] {message.TargetPlayer}에게: {message.Message}",
            Type = (int)ChatType.System,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await senderSession.SendAsync(MessageType.ChatMessage, confirmMessage);

        Console.WriteLine($"[WHISPER] {senderSession.UserName} -> {message.TargetPlayer}: {message.Message}");
    }

    /// <summary>
    /// 채팅 실패 응답을 보냅니다.
    /// </summary>
    private async Task SendChatFailure(Session session, string errorMessage)
    {
        var response = new ChatResponse { Success = false, ErrorMessage = errorMessage };
        await session.SendAsync(MessageType.ChatResponse, response);
    }

    /// <summary>
    /// 금지어가 포함되어 있는지 확인합니다.
    /// </summary>
    private bool ContainsBannedWords(string message)
    {
        var lowerMessage = message.ToLower();
        return _bannedWords.Any(word => lowerMessage.Contains(word));
    }

    /// <summary>
    /// 시스템 메시지를 모든 플레이어에게 브로드캐스트합니다.
    /// </summary>
    public async Task BroadcastSystemMessage(string message)
    {
        var chatMessage = new ChatMessage
        {
            SenderId = "System",
            SenderName = "System",
            Message = message,
            Type = (int)ChatType.System,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var tasks = new List<Task>();
        foreach (var playerName in _sessions.ConnectedUsers)
        {
            var playerSession = _sessions.GetSession(playerName);
            if (playerSession != null)
            {
                tasks.Add(playerSession.SendAsync(MessageType.ChatMessage, chatMessage));
            }
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"[SYSTEM] {message}");
    }
}