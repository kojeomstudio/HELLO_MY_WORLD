using GameServerApp.Database;
using GameServerApp.Models;
using SharedProtocol;

namespace GameServerApp.Systems;

/// <summary>
/// 플레이어 체력과 허기 시스템
/// </summary>
public class HealthAndHungerSystem
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly Dictionary<string, PlayerHealthData> _playerHealthCache;
    private readonly Timer _healthRegenTimer;
    private readonly Timer _hungerDecayTimer;

    public HealthAndHungerSystem(DatabaseHelper database, SessionManager sessions)
    {
        _database = database;
        _sessions = sessions;
        _playerHealthCache = new Dictionary<string, PlayerHealthData>();

        // 체력 재생 타이머 (3초마다)
        _healthRegenTimer = new Timer(ProcessHealthRegeneration, null, 
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));

        // 허기 감소 타이머 (18초마다)
        _hungerDecayTimer = new Timer(ProcessHungerDecay, null, 
            TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(18));
    }

    public async Task<PlayerHealthData> GetPlayerHealthAsync(string userName)
    {
        if (_playerHealthCache.TryGetValue(userName, out var cachedData))
        {
            return cachedData;
        }

        var healthData = await LoadPlayerHealthFromDatabase(userName);
        if (healthData != null)
        {
            _playerHealthCache[userName] = healthData;
            return healthData;
        }

        // 새 플레이어 생성
        var newHealthData = new PlayerHealthData(userName);
        _playerHealthCache[userName] = newHealthData;
        await SavePlayerHealthToDatabase(newHealthData);
        
        return newHealthData;
    }

    public async Task<bool> DamagePlayerAsync(string userName, float damage, DamageType damageType = DamageType.Generic)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health <= 0)
            return false; // 이미 죽은 상태

        healthData.Health = Math.Max(0, healthData.Health - damage);
        healthData.LastDamageTime = DateTime.UtcNow;
        healthData.LastDamageType = damageType;

        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);

        if (healthData.Health <= 0)
        {
            await HandlePlayerDeath(userName, damageType);
        }

        Console.WriteLine($"Player {userName} took {damage} damage ({damageType}). Health: {healthData.Health:F1}/{healthData.MaxHealth}");
        
        return true;
    }

    public async Task<bool> HealPlayerAsync(string userName, float healAmount, HealType healType = HealType.Generic)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health >= healthData.MaxHealth)
            return false; // 이미 최대 체력

        float oldHealth = healthData.Health;
        healthData.Health = Math.Min(healthData.MaxHealth, healthData.Health + healAmount);
        
        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);

        Console.WriteLine($"Player {userName} healed {healthData.Health - oldHealth:F1} health ({healType}). Health: {healthData.Health:F1}/{healthData.MaxHealth}");
        
        return true;
    }

    public async Task<bool> ConsumeHungerAsync(string userName, int hungerPoints)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        healthData.Hunger = Math.Max(0, healthData.Hunger - hungerPoints);
        healthData.LastHungerUpdate = DateTime.UtcNow;

        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);

        Console.WriteLine($"Player {userName} consumed {hungerPoints} hunger. Hunger: {healthData.Hunger}/{healthData.MaxHunger}");
        
        return true;
    }

    public async Task<bool> FeedPlayerAsync(string userName, int foodPoints, float saturation = 0)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        int oldHunger = healthData.Hunger;
        healthData.Hunger = Math.Min(healthData.MaxHunger, healthData.Hunger + foodPoints);
        healthData.Saturation = Math.Min(healthData.Hunger, healthData.Saturation + saturation);
        healthData.LastHungerUpdate = DateTime.UtcNow;

        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);

        Console.WriteLine($"Player {userName} ate food (+{healthData.Hunger - oldHunger} hunger, +{saturation:F1} saturation). Hunger: {healthData.Hunger}/{healthData.MaxHunger}");
        
        return true;
    }

    private async void ProcessHealthRegeneration(object? state)
    {
        try
        {
            var playersToRegen = _playerHealthCache.Values
                .Where(data => CanRegenerateHealth(data))
                .ToList();

            foreach (var healthData in playersToRegen)
            {
                if (healthData.Hunger >= 18) // 허기가 충분할 때만 재생
                {
                    await HealPlayerAsync(healthData.UserName, 1.0f, HealType.NaturalRegen);
                    await ConsumeHungerAsync(healthData.UserName, 1); // 체력 재생시 허기 소모
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health regeneration error: {ex.Message}");
        }
    }

    private async void ProcessHungerDecay(object? state)
    {
        try
        {
            var playersToProcess = _playerHealthCache.Values
                .Where(data => ShouldProcessHunger(data))
                .ToList();

            foreach (var healthData in playersToProcess)
            {
                // 포화도가 있으면 먼저 포화도 소모
                if (healthData.Saturation > 0)
                {
                    healthData.Saturation = Math.Max(0, healthData.Saturation - 1);
                }
                else
                {
                    // 포화도가 없으면 허기 감소
                    await ConsumeHungerAsync(healthData.UserName, 1);
                    
                    // 허기가 0이면 체력 감소 (기아)
                    if (healthData.Hunger <= 0 && healthData.Health > 1)
                    {
                        await DamagePlayerAsync(healthData.UserName, 1.0f, DamageType.Starvation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hunger decay error: {ex.Message}");
        }
    }

    private bool CanRegenerateHealth(PlayerHealthData healthData)
    {
        if (healthData.Health >= healthData.MaxHealth)
            return false;

        if (healthData.Health <= 0)
            return false;

        // 마지막 피해 후 5초 이후부터 재생 시작
        return (DateTime.UtcNow - healthData.LastDamageTime).TotalSeconds >= 5;
    }

    private bool ShouldProcessHunger(PlayerHealthData healthData)
    {
        // 죽은 플레이어는 허기 처리 안함
        if (healthData.Health <= 0)
            return false;

        // 마지막 허기 업데이트로부터 18초 이상 경과
        return (DateTime.UtcNow - healthData.LastHungerUpdate).TotalSeconds >= 18;
    }

    private async Task HandlePlayerDeath(string userName, DamageType damageType)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        healthData.DeathCount++;
        healthData.LastDeathTime = DateTime.UtcNow;
        healthData.LastDeathCause = damageType;

        // 리스폰 위치 설정 (나중에 침대/스폰포인트 시스템으로 확장 가능)
        healthData.RespawnPosition = new SharedProtocol.Vector3(0, 64, 0); // 기본 스폰 위치

        await SavePlayerHealthToDatabase(healthData);
        
        // 죽음 메시지 브로드캐스트
        await BroadcastPlayerDeath(userName, damageType);

        Console.WriteLine($"Player {userName} died from {damageType}. Death count: {healthData.DeathCount}");
    }

    private async Task<bool> RespawnPlayerAsync(string userName)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health > 0)
            return false; // 이미 살아있음

        // 체력과 허기 복구
        healthData.Health = healthData.MaxHealth;
        healthData.Hunger = healthData.MaxHunger;
        healthData.Saturation = 5.0f;
        healthData.LastDamageTime = DateTime.MinValue;

        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);

        Console.WriteLine($"Player {userName} respawned with full health and hunger");
        
        return true;
    }

    private async Task BroadcastHealthUpdate(string userName, PlayerHealthData healthData)
    {
        var session = _sessions.GetSession(userName);
        if (session == null) return;

        var update = new HealthUpdateMessage
        {
            Health = healthData.Health,
            MaxHealth = healthData.MaxHealth,
            Hunger = healthData.Hunger,
            MaxHunger = healthData.MaxHunger,
            Saturation = healthData.Saturation,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        await session.SendAsync(MessageType.HealthUpdate, update);
    }

    private async Task BroadcastPlayerDeath(string userName, DamageType damageType)
    {
        var deathMessage = GenerateDeathMessage(userName, damageType);
        
        // TODO: 모든 플레이어에게 죽음 메시지 전송 (SessionManager 개선 필요)
        Console.WriteLine($"Broadcasting player death: {deathMessage}");
    }

    private string GenerateDeathMessage(string userName, DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Fall => $"{userName}이(가) 떨어져서 죽었습니다.",
            DamageType.Drowning => $"{userName}이(가) 익사했습니다.",
            DamageType.Fire => $"{userName}이(가) 불에 타 죽었습니다.",
            DamageType.Lava => $"{userName}이(가) 용암에 빠져 죽었습니다.",
            DamageType.Starvation => $"{userName}이(가) 굶어 죽었습니다.",
            DamageType.PvP => $"{userName}이(가) 다른 플레이어에게 죽었습니다.",
            DamageType.Monster => $"{userName}이(가) 몬스터에게 죽었습니다.",
            DamageType.Explosion => $"{userName}이(가) 폭발로 죽었습니다.",
            DamageType.Void => $"{userName}이(가) 공허로 추락했습니다.",
            _ => $"{userName}이(가) 죽었습니다."
        };
    }

    private async Task<PlayerHealthData?> LoadPlayerHealthFromDatabase(string userName)
    {
        // TODO: 실제 데이터베이스에서 로드
        await Task.Delay(10);
        return null; // 새 플레이어
    }

    private async Task SavePlayerHealthToDatabase(PlayerHealthData healthData)
    {
        // TODO: 실제 데이터베이스에 저장
        await Task.Delay(10);
        Console.WriteLine($"Health data saved for {healthData.UserName}");
    }

    public void Dispose()
    {
        _healthRegenTimer?.Dispose();
        _hungerDecayTimer?.Dispose();
    }
}

/// <summary>
/// 플레이어 체력 데이터
/// </summary>
public class PlayerHealthData
{
    public string UserName { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public int Hunger { get; set; }
    public int MaxHunger { get; set; }
    public float Saturation { get; set; }
    public int DeathCount { get; set; }
    public DateTime LastDamageTime { get; set; }
    public DateTime LastHungerUpdate { get; set; }
    public DateTime LastDeathTime { get; set; }
    public DamageType LastDamageType { get; set; }
    public DamageType LastDeathCause { get; set; }
    public SharedProtocol.Vector3? RespawnPosition { get; set; }

    public PlayerHealthData(string userName)
    {
        UserName = userName;
        Health = 20.0f;
        MaxHealth = 20.0f;
        Hunger = 20;
        MaxHunger = 20;
        Saturation = 5.0f;
        DeathCount = 0;
        LastDamageTime = DateTime.MinValue;
        LastHungerUpdate = DateTime.UtcNow;
        LastDeathTime = DateTime.MinValue;
        LastDamageType = DamageType.Generic;
        LastDeathCause = DamageType.Generic;
        RespawnPosition = null;
    }

    public bool IsAlive() => Health > 0;
    public bool IsFullHealth() => Health >= MaxHealth;
    public bool IsFullHunger() => Hunger >= MaxHunger;
    public bool IsStarving() => Hunger <= 0;
    public float HealthPercentage() => Health / MaxHealth;
    public float HungerPercentage() => (float)Hunger / MaxHunger;
}

/// <summary>
/// 데미지 타입
/// </summary>
public enum DamageType
{
    Generic = 0,
    Fall = 1,
    Drowning = 2,
    Fire = 3,
    Lava = 4,
    Starvation = 5,
    PvP = 6,
    Monster = 7,
    Explosion = 8,
    Void = 9,
    Poison = 10,
    Magic = 11
}

/// <summary>
/// 치유 타입
/// </summary>
public enum HealType
{
    Generic = 0,
    NaturalRegen = 1,
    Food = 2,
    Potion = 3,
    Magic = 4
}