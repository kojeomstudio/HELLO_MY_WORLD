using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;
using System.Security.Cryptography;
using System.Text;

namespace GameServerApp.Handlers;

/// <summary>
/// 로그인 요청을 처리하는 핸들러
/// 사용자 인증, 세션 생성, 플레이어 데이터 로드를 담당합니다.
/// </summary>
public class LoginHandler : MessageHandler<LoginRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    
    // 지원되는 클라이언트 버전 목록
    private readonly HashSet<string> _supportedVersions = new() { "1.0.0", "1.0.1" };

    public LoginHandler(DatabaseHelper database, SessionManager sessions) : base(MessageType.LoginRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, LoginRequest message)
    {
        try
        {
            // 입력 검증
            if (string.IsNullOrWhiteSpace(message.Username) || string.IsNullOrWhiteSpace(message.Password))
            {
                await SendLoginFailure(session, "사용자명과 비밀번호를 입력해주세요.");
                return;
            }

            // 클라이언트 버전 확인
            if (!string.IsNullOrEmpty(message.ClientVersion) && !_supportedVersions.Contains(message.ClientVersion))
            {
                await SendLoginFailure(session, $"지원하지 않는 클라이언트 버전입니다: {message.ClientVersion}");
                return;
            }

            // 이미 로그인된 사용자인지 확인
            if (_sessions.GetSession(message.Username) != null)
            {
                await SendLoginFailure(session, "이미 로그인된 사용자입니다.");
                return;
            }

            // 사용자 인증 (실제 환경에서는 해시된 비밀번호와 비교해야 함)
            if (!await AuthenticateUser(message.Username, message.Password))
            {
                await SendLoginFailure(session, "잘못된 사용자명 또는 비밀번호입니다.");
                return;
            }

            // 세션 토큰 생성
            var sessionToken = GenerateSessionToken();
            session.SessionToken = sessionToken;
            session.UserName = message.Username;
            
            // 플레이어 데이터 로드 또는 생성
            var character = await GetOrCreateCharacter(message.Username);
            
            // 플레이어 정보 생성
            var playerInfo = new PlayerInfo
            {
                PlayerId = character.Name, // 실제로는 GUID 등을 사용
                Username = character.Name,
                Position = new Vector3((float)character.X, (float)character.Y, 0),
                Level = 1,
                Health = 100,
                MaxHealth = 100,
                Inventory = new List<InventoryItem>()
            };
            
            session.PlayerInfo = playerInfo;
            
            // 세션 등록
            _sessions.Add(session);
            
            // 로그인 성공 응답
            var response = new LoginResponse 
            { 
                Success = true, 
                Message = $"환영합니다, {message.Username}님!",
                SessionToken = sessionToken,
                PlayerInfo = playerInfo
            };
            
            await session.SendAsync(MessageType.LoginResponse, response);
            Console.WriteLine($"User '{message.Username}' logged in successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error for user '{message.Username}': {ex.Message}");
            await SendLoginFailure(session, "로그인 처리 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// 로그인 실패 응답을 보냅니다.
    /// </summary>
    private async Task SendLoginFailure(Session session, string errorMessage)
    {
        var response = new LoginResponse { Success = false, Message = errorMessage };
        await session.SendAsync(MessageType.LoginResponse, response);
        Console.WriteLine($"Login failed: {errorMessage}");
    }

    /// <summary>
    /// 사용자를 인증합니다. (현재는 간단한 구현)
    /// 실제 환경에서는 해시된 비밀번호와 솔트를 사용해야 합니다.
    /// </summary>
    private async Task<bool> AuthenticateUser(string username, string password)
    {
        // TODO: 실제 데이터베이스에서 사용자 인증 정보 확인
        // 현재는 테스트용으로 간단한 검증만 수행
        await Task.Delay(50); // 네트워크 지연 시뮬레이션
        
        // 기본 테스트 사용자
        if (username == "test" && password == "password")
            return true;
            
        // 실제로는 데이터베이스에서 해시된 비밀번호와 비교
        return password.Length >= 6; // 최소 6자리 비밀번호
    }

    /// <summary>
    /// 캐릭터 정보를 가져오거나 새로 생성합니다.
    /// </summary>
    private async Task<Character> GetOrCreateCharacter(string username)
    {
        await Task.Delay(10); // DB 접근 지연 시뮬레이션
        
        var existingPlayers = _database.GetPlayers().ToList();
        var existingCharacter = existingPlayers.FirstOrDefault(p => p.Name == username);
        
        if (existingCharacter != null)
        {
            return existingCharacter;
        }
        
        // 새 캐릭터 생성
        var newCharacter = new Character(username, 0, 0); // 기본 스폰 위치
        _database.SavePlayer(newCharacter);
        return newCharacter;
    }

    /// <summary>
    /// 보안 세션 토큰을 생성합니다.
    /// </summary>
    private string GenerateSessionToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
