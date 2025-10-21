using GameServerApp.Database;
using GameServerApp.Models;
using SharedProtocol;

namespace GameServerApp.Systems;

/// <summary>
/// ?Œë ˆ?´ì–´ ì²´ë ¥ê³??ˆê¸° ?œìŠ¤??
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

        // ì²´ë ¥ ?¬ìƒ ?€?´ë¨¸ (3ì´ˆë§ˆ??
        _healthRegenTimer = new Timer(ProcessHealthRegeneration, null, 
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));

        // ?ˆê¸° ê°ì†Œ ?€?´ë¨¸ (18ì´ˆë§ˆ??
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

        // ???Œë ˆ?´ì–´ ?ì„±
        var newHealthData = new PlayerHealthData(userName);
        _playerHealthCache[userName] = newHealthData;
        await SavePlayerHealthToDatabase(newHealthData);
        
        return newHealthData;
    }

    public async Task<bool> DamagePlayerAsync(string userName, float damage, DamageType damageType = DamageType.Generic)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health <= 0)
            return false; // ?´ë? ì£½ì? ?íƒœ

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
            return false; // ?´ë? ìµœë? ì²´ë ¥

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
                if (healthData.Hunger >= 18) // ?ˆê¸°ê°€ ì¶©ë¶„???Œë§Œ ?¬ìƒ
                {
                    await HealPlayerAsync(healthData.UserName, 1.0f, HealType.NaturalRegen);
                    await ConsumeHungerAsync(healthData.UserName, 1); // ì²´ë ¥ ?¬ìƒ???ˆê¸° ?Œëª¨
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
                // ?¬í™”?„ê? ?ˆìœ¼ë©?ë¨¼ì? ?¬í™”???Œëª¨
                if (healthData.Saturation > 0)
                {
                    healthData.Saturation = Math.Max(0, healthData.Saturation - 1);
                }
                else
                {
                    // ?¬í™”?„ê? ?†ìœ¼ë©??ˆê¸° ê°ì†Œ
                    await ConsumeHungerAsync(healthData.UserName, 1);
                    
                    // ?ˆê¸°ê°€ 0?´ë©´ ì²´ë ¥ ê°ì†Œ (ê¸°ì•„)
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

        // ë§ˆì?ë§??¼í•´ ??5ì´??´í›„ë¶€???¬ìƒ ?œì‘
        return (DateTime.UtcNow - healthData.LastDamageTime).TotalSeconds >= 5;
    }

    private bool ShouldProcessHunger(PlayerHealthData healthData)
    {
        // ì£½ì? ?Œë ˆ?´ì–´???ˆê¸° ì²˜ë¦¬ ?ˆí•¨
        if (healthData.Health <= 0)
            return false;

        // ë§ˆì?ë§??ˆê¸° ?…ë°?´íŠ¸ë¡œë???18ì´??´ìƒ ê²½ê³¼
        return (DateTime.UtcNow - healthData.LastHungerUpdate).TotalSeconds >= 18;
    }

    private async Task HandlePlayerDeath(string userName, DamageType damageType)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        healthData.DeathCount++;
        healthData.LastDeathTime = DateTime.UtcNow;
        healthData.LastDeathCause = damageType;

        // ë¦¬ìŠ¤???„ì¹˜ ?¤ì • (?˜ì¤‘??ì¹¨ë?/?¤í°?¬ì¸???œìŠ¤?œìœ¼ë¡??•ì¥ ê°€??
        healthData.RespawnPosition = new SharedProtocol.Vector3(0, 64, 0); // ê¸°ë³¸ ?¤í° ?„ì¹˜

        await SavePlayerHealthToDatabase(healthData);
        
        // ì£½ìŒ ë©”ì‹œì§€ ë¸Œë¡œ?œìº?¤íŠ¸
        await BroadcastPlayerDeath(userName, damageType);

        Console.WriteLine($"Player {userName} died from {damageType}. Death count: {healthData.DeathCount}");
    }

    private async Task<bool> RespawnPlayerAsync(string userName)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health > 0)
            return false; // ?´ë? ?´ì•„?ˆìŒ

        // ì²´ë ¥ê³??ˆê¸° ë³µêµ¬
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
        var deathText = GenerateDeathMessage(userName, damageType);
        var broadcast = new PlayerDeathMessage
        {
            PlayerName = userName,
            DeathMessage = deathText,
            DamageType = (int)damageType,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var recipients = _sessions.GetSessionsSnapshot()
            .Where(session => !string.IsNullOrWhiteSpace(session.UserName))
            .ToList();

        if (recipients.Count == 0)
        {
            Console.WriteLine($"Broadcasting player death skipped (no active sessions): {deathText}");
            return;
        }

        var sendTasks = new List<Task>(recipients.Count);
        foreach (var session in recipients)
        {
            sendTasks.Add(session.SendAsync(MessageType.PlayerDeath, broadcast));
        }

        await Task.WhenAll(sendTasks);

        Console.WriteLine($"Broadcasting player death: {deathText}; notified {recipients.Count} session(s).");
    }

    private string GenerateDeathMessage(string userName, DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Fall => $"{userName}??ê°€) ?¨ì–´?¸ì„œ ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Drowning => $"{userName}??ê°€) ?µì‚¬?ˆìŠµ?ˆë‹¤.",
            DamageType.Fire => $"{userName}??ê°€) ë¶ˆì— ?€ ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Lava => $"{userName}??ê°€) ?©ì•”??ë¹ ì ¸ ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Starvation => $"{userName}??ê°€) êµ¶ì–´ ì£½ì—ˆ?µë‹ˆ??",
            DamageType.PvP => $"{userName}??ê°€) ?¤ë¥¸ ?Œë ˆ?´ì–´?ê²Œ ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Monster => $"{userName}??ê°€) ëª¬ìŠ¤?°ì—ê²?ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Explosion => $"{userName}??ê°€) ??°œë¡?ì£½ì—ˆ?µë‹ˆ??",
            DamageType.Void => $"{userName}??ê°€) ê³µí—ˆë¡?ì¶”ë½?ˆìŠµ?ˆë‹¤.",
            _ => $"{userName}??ê°€) ì£½ì—ˆ?µë‹ˆ??"
        };
    }

    private async Task<PlayerHealthData?> LoadPlayerHealthFromDatabase(string userName)
    {
        // TODO: ?¤ì œ ?°ì´?°ë² ?´ìŠ¤?ì„œ ë¡œë“œ
        await Task.Delay(10);
        return null; // ???Œë ˆ?´ì–´
    }

    private async Task SavePlayerHealthToDatabase(PlayerHealthData healthData)
    {
        // TODO: ?¤ì œ ?°ì´?°ë² ?´ìŠ¤???€??
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
/// ?Œë ˆ?´ì–´ ì²´ë ¥ ?°ì´??
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
/// ?°ë?ì§€ ?€??
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
/// ì¹˜ìœ  ?€??
/// </summary>
public enum HealType
{
    Generic = 0,
    NaturalRegen = 1,
    Food = 2,
    Potion = 3,
    Magic = 4
}


