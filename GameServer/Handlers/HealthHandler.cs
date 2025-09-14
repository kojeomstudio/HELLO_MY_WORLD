using GameServerApp.Database;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 체력 관련 요청을 처리하는 핸들러
/// </summary>
public class HealthHandler : MessageHandler<HealthActionRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly HealthAndHungerSystem _healthSystem;

    public HealthHandler(DatabaseHelper database, SessionManager sessions, HealthAndHungerSystem healthSystem)
        : base(MessageType.HealthActionRequest)
    {
        _database = database;
        _sessions = sessions;
        _healthSystem = healthSystem;
    }

    protected override async Task HandleAsync(Session session, HealthActionRequest message)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            bool success = false;
            string resultMessage = "";

            switch (message.ActionType)
            {
                case 0: // Damage
                    success = await _healthSystem.DamagePlayerAsync(session.UserName, message.Amount, (DamageType)message.DamageType);
                    resultMessage = success ? "데미지 적용 완료" : "데미지 적용 실패";
                    break;

                case 1: // Heal
                    success = await _healthSystem.HealPlayerAsync(session.UserName, message.Amount, (HealType)message.HealType);
                    resultMessage = success ? "치유 완료" : "치유 실패";
                    break;

                case 2: // Feed
                    success = await _healthSystem.FeedPlayerAsync(session.UserName, (int)message.Amount, message.Saturation);
                    resultMessage = success ? "식사 완료" : "식사 실패";
                    break;

                case 3: // Consume Hunger
                    success = await _healthSystem.ConsumeHungerAsync(session.UserName, (int)message.Amount);
                    resultMessage = success ? "허기 소모 완료" : "허기 소모 실패";
                    break;

                default:
                    await SendFailureResponse(session, "알 수 없는 액션입니다.");
                    return;
            }

            var response = new HealthActionResponse
            {
                Success = success,
                Message = resultMessage,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.HealthActionResponse, response);

            Console.WriteLine($"Health action {message.ActionType} processed for {session.UserName}: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health action error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "체력 처리 중 오류가 발생했습니다.");
        }
    }

    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new HealthActionResponse
        {
            Success = false,
            Message = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.HealthActionResponse, response);
    }
}

/// <summary>
/// 플레이어 리스폰 요청을 처리하는 핸들러
/// </summary>
public class RespawnHandler : MessageHandler<RespawnRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly HealthAndHungerSystem _healthSystem;

    public RespawnHandler(DatabaseHelper database, SessionManager sessions, HealthAndHungerSystem healthSystem)
        : base(MessageType.RespawnRequest)
    {
        _database = database;
        _sessions = sessions;
        _healthSystem = healthSystem;
    }

    protected override async Task HandleAsync(Session session, RespawnRequest message)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            var healthData = await _healthSystem.GetPlayerHealthAsync(session.UserName);
            
            if (healthData.IsAlive())
            {
                await SendFailureResponse(session, "이미 살아있는 플레이어입니다.");
                return;
            }

            // 리스폰 처리
            healthData.Health = healthData.MaxHealth;
            healthData.Hunger = healthData.MaxHunger;
            healthData.Saturation = 5.0f;
            healthData.LastDamageTime = DateTime.MinValue;

            var respawnPosition = healthData.RespawnPosition ?? new SharedProtocol.Vector3(0, 64, 0);

            var response = new RespawnResponse
            {
                Success = true,
                Message = "리스폰 완료",
                RespawnPosition = respawnPosition,
                Health = healthData.Health,
                MaxHealth = healthData.MaxHealth,
                Hunger = healthData.Hunger,
                MaxHunger = healthData.MaxHunger,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.RespawnResponse, response);

            // 다른 플레이어들에게 리스폰 알림
            await BroadcastPlayerRespawn(session.UserName, respawnPosition);

            Console.WriteLine($"Player {session.UserName} respawned at ({respawnPosition.X}, {respawnPosition.Y}, {respawnPosition.Z})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Respawn error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "리스폰 처리 중 오류가 발생했습니다.");
        }
    }

    private async Task BroadcastPlayerRespawn(string userName, SharedProtocol.Vector3 respawnPosition)
    {
        var broadcast = new PlayerRespawnBroadcast
        {
            PlayerName = userName,
            RespawnPosition = respawnPosition,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // TODO: 모든 세션에 브로드캐스트 (SessionManager 개선 필요)
        Console.WriteLine($"Player {userName} respawned at ({respawnPosition.X}, {respawnPosition.Y}, {respawnPosition.Z})");
    }

    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new RespawnResponse
        {
            Success = false,
            Message = errorMessage,
            RespawnPosition = null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.RespawnResponse, response);
    }
}