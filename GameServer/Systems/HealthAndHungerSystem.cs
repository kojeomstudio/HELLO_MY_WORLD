using GameServerApp.Database;
using GameServerApp.Models;
using SharedProtocol;

namespace GameServerApp.Systems;

/// <summary>
/// ?åÎ†à?¥Ïñ¥ Ï≤¥Î†•Í≥??àÍ∏∞ ?úÏä§??
/// </summary>
public class HealthAndHungerSystem
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly ServerMetricsService _metrics;
    private readonly Dictionary<string, PlayerHealthData> _playerHealthCache;
    private readonly Timer _healthRegenTimer;
    private readonly Timer _hungerDecayTimer;

    public HealthAndHungerSystem(DatabaseHelper database, SessionManager sessions, ServerMetricsService metrics)
    {
        _database = database;
        _sessions = sessions;
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _playerHealthCache = new Dictionary<string, PlayerHealthData>();

        // Ï≤¥Î†• ?¨ÏÉù ?Ä?¥Î®∏ (3Ï¥àÎßà??
        _healthRegenTimer = new Timer(ProcessHealthRegeneration, null, 
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));

        // ?àÍ∏∞ Í∞êÏÜå ?Ä?¥Î®∏ (18Ï¥àÎßà??
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

        // ???åÎ†à?¥Ïñ¥ ?ùÏÑ±
        var newHealthData = new PlayerHealthData(userName);
        _playerHealthCache[userName] = newHealthData;
        await SavePlayerHealthToDatabase(newHealthData);
        
        return newHealthData;
    }

    public async Task<bool> DamagePlayerAsync(
        string userName,
        float damage,
        DamageType damageType = DamageType.Generic,
        CombatEventContext? combatContext = null)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health <= 0)
            return false; // Already dead

        var appliedDamage = Math.Max(0f, damage);
        if (appliedDamage <= 0f)
        {
            return false;
        }

        healthData.Health = Math.Max(0, healthData.Health - appliedDamage);
        healthData.LastDamageTime = DateTime.UtcNow;
        healthData.LastDamageType = damageType;

        await SavePlayerHealthToDatabase(healthData);
        await BroadcastHealthUpdate(userName, healthData);
        await BroadcastCombatEvent(userName, damageType, healthData, combatContext, appliedDamage);

        if (healthData.Health <= 0)
        {
            await HandlePlayerDeath(userName, damageType);
        }

        var attackerLabel = combatContext?.AttackerDisplayName ?? combatContext?.AttackerUserName ?? "environment";
        Console.WriteLine($"Player {userName} took {appliedDamage} damage ({damageType}) from {attackerLabel}. Health: {healthData.Health:F1}/{healthData.MaxHealth}");
        
        return true;
    }

    public async Task<bool> HealPlayerAsync(string userName, float healAmount, HealType healType = HealType.Generic)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health >= healthData.MaxHealth)
            return false; // ?¥Î? ÏµúÎ? Ï≤¥Î†•

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
                if (healthData.Hunger >= 18) // ?àÍ∏∞Í∞Ä Ï∂©Î∂Ñ???åÎßå ?¨ÏÉù
                {
                    await HealPlayerAsync(healthData.UserName, 1.0f, HealType.NaturalRegen);
                    await ConsumeHungerAsync(healthData.UserName, 1); // Ï≤¥Î†• ?¨ÏÉù???àÍ∏∞ ?åÎ™®
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
                // ?¨Ìôî?ÑÍ? ?àÏúºÎ©?Î®ºÏ? ?¨Ìôî???åÎ™®
                if (healthData.Saturation > 0)
                {
                    healthData.Saturation = Math.Max(0, healthData.Saturation - 1);
                }
                else
                {
                    // ?¨Ìôî?ÑÍ? ?ÜÏúºÎ©??àÍ∏∞ Í∞êÏÜå
                    await ConsumeHungerAsync(healthData.UserName, 1);
                    
                    // ?àÍ∏∞Í∞Ä 0?¥Î©¥ Ï≤¥Î†• Í∞êÏÜå (Í∏∞ÏïÑ)
                    if (healthData.Hunger <= 0 && healthData.Health > 1)
                    {
                        await DamagePlayerAsync(healthData.UserName, 1.0f, DamageType.Starvation, CombatEventContext.CreateEnvironmental("Starvation", 1.0f));
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

        // ÎßàÏ?Îß??ºÌï¥ ??5Ï¥??¥ÌõÑÎ∂Ä???¨ÏÉù ?úÏûë
        return (DateTime.UtcNow - healthData.LastDamageTime).TotalSeconds >= 5;
    }

    private bool ShouldProcessHunger(PlayerHealthData healthData)
    {
        // Ï£ΩÏ? ?åÎ†à?¥Ïñ¥???àÍ∏∞ Ï≤òÎ¶¨ ?àÌï®
        if (healthData.Health <= 0)
            return false;

        // ÎßàÏ?Îß??àÍ∏∞ ?ÖÎç∞?¥Ìä∏Î°úÎ???18Ï¥??¥ÏÉÅ Í≤ΩÍ≥º
        return (DateTime.UtcNow - healthData.LastHungerUpdate).TotalSeconds >= 18;
    }

    private async Task HandlePlayerDeath(string userName, DamageType damageType)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        healthData.DeathCount++;
        healthData.LastDeathTime = DateTime.UtcNow;
        healthData.LastDeathCause = damageType;

        // Î¶¨Ïä§???ÑÏπò ?§Ï†ï (?òÏ§ë??Ïπ®Î?/?§Ìè∞?¨Ïù∏???úÏä§?úÏúºÎ°??ïÏû• Í∞Ä??
        healthData.RespawnPosition = new SharedProtocol.Vector3(0, 64, 0); // Í∏∞Î≥∏ ?§Ìè∞ ?ÑÏπò

        await SavePlayerHealthToDatabase(healthData);
        
        // Ï£ΩÏùå Î©îÏãúÏßÄ Î∏åÎ°ú?úÏ∫ê?§Ìä∏
        await BroadcastPlayerDeath(userName, damageType);

        Console.WriteLine($"Player {userName} died from {damageType}. Death count: {healthData.DeathCount}");
        _metrics.RecordPlayerDeath(userName, damageType);
    }

    private async Task<bool> RespawnPlayerAsync(string userName)
    {
        var healthData = await GetPlayerHealthAsync(userName);
        
        if (healthData.Health > 0)
            return false; // ?¥Î? ?¥ÏïÑ?àÏùå

        // Ï≤¥Î†•Í≥??àÍ∏∞ Î≥µÍµ¨
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


    private async Task BroadcastCombatEvent(string targetUserName, DamageType damageType, PlayerHealthData targetHealth, CombatEventContext? context, float appliedDamage)
    {
        if (appliedDamage <= 0f)
        {
            return;
        }

        var message = new CombatEventMessage
        {
            AttackerName = context?.AttackerDisplayName ?? context?.AttackerUserName ?? string.Empty,
            TargetName = targetUserName,
            DamageType = (int)damageType,
            RawDamage = context?.RawDamage ?? appliedDamage,
            FinalDamage = appliedDamage,
            TargetRemainingHealth = Math.Max(0f, targetHealth.Health),
            IsCritical = context?.IsCritical ?? false,
            IsBlocked = context?.IsBlocked ?? false,
            WeaponName = context?.WeaponName ?? string.Empty,
            WeaponItemId = context?.WeaponItemId ?? 0,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var recipients = new List<Session>();
        var targetSession = _sessions.GetSession(targetUserName);
        if (targetSession != null)
        {
            recipients.Add(targetSession);
        }

        var attackerUserName = context?.AttackerUserName;
        if (!string.IsNullOrWhiteSpace(attackerUserName) &&
            !string.Equals(attackerUserName, targetUserName, StringComparison.OrdinalIgnoreCase))
        {
            var attackerSession = _sessions.GetSession(attackerUserName);
            if (attackerSession != null)
            {
                recipients.Add(attackerSession);
            }
        }

        if (recipients.Count == 0)
        {
            return;
        }

        var sendTasks = new List<Task>(recipients.Count);
        foreach (var session in recipients)
        {
            sendTasks.Add(session.SendAsync(MessageType.CombatEvent, message));
        }

        await Task.WhenAll(sendTasks);
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
            DamageType.Fall => $"{userName}??Í∞Ä) ?®Ïñ¥?∏ÏÑú Ï£ΩÏóà?µÎãà??",
            DamageType.Drowning => $"{userName}??Í∞Ä) ?µÏÇ¨?àÏäµ?àÎã§.",
            DamageType.Fire => $"{userName}??Í∞Ä) Î∂àÏóê ?Ä Ï£ΩÏóà?µÎãà??",
            DamageType.Lava => $"{userName}??Í∞Ä) ?©Ïïî??Îπ†Ï†∏ Ï£ΩÏóà?µÎãà??",
            DamageType.Starvation => $"{userName}??Í∞Ä) Íµ∂Ïñ¥ Ï£ΩÏóà?µÎãà??",
            DamageType.PvP => $"{userName}??Í∞Ä) ?§Î•∏ ?åÎ†à?¥Ïñ¥?êÍ≤å Ï£ΩÏóà?µÎãà??",
            DamageType.Monster => $"{userName}??Í∞Ä) Î™¨Ïä§?∞ÏóêÍ≤?Ï£ΩÏóà?µÎãà??",
            DamageType.Explosion => $"{userName}??Í∞Ä) ??∞úÎ°?Ï£ΩÏóà?µÎãà??",
            DamageType.Void => $"{userName}??Í∞Ä) Í≥µÌóàÎ°?Ï∂îÎùΩ?àÏäµ?àÎã§.",
            _ => $"{userName}??Í∞Ä) Ï£ΩÏóà?µÎãà??"
        };
    }

    private async Task<PlayerHealthData?> LoadPlayerHealthFromDatabase(string userName)
    {
        // TODO: ?§Ï†ú ?∞Ïù¥?∞Î≤†?¥Ïä§?êÏÑú Î°úÎìú
        await Task.Delay(10);
        return null; // ???åÎ†à?¥Ïñ¥
    }

    private async Task SavePlayerHealthToDatabase(PlayerHealthData healthData)
    {
        // TODO: ?§Ï†ú ?∞Ïù¥?∞Î≤†?¥Ïä§???Ä??
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
/// ?åÎ†à?¥Ïñ¥ Ï≤¥Î†• ?∞Ïù¥??
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
/// ?∞Î?ÏßÄ ?Ä??
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
/// ÏπòÏú† ?Ä??
/// </summary>
public enum HealType
{
    Generic = 0,
    NaturalRegen = 1,
    Food = 2,
    Potion = 3,
    Magic = 4
}

/// <summary>
/// Lightweight context describing where combat damage originated.
/// </summary>
public sealed class CombatEventContext
{
    public string? AttackerUserName { get; init; }
    public string? AttackerDisplayName { get; init; }
    public string? WeaponName { get; init; }
    public int WeaponItemId { get; init; }
    public bool IsCritical { get; init; }
    public bool IsBlocked { get; init; }
    public float RawDamage { get; init; }

    public static CombatEventContext CreateEnvironmental(string displayName, float rawDamage)
    {
        return new CombatEventContext
        {
            AttackerDisplayName = displayName,
            RawDamage = rawDamage
        };
    }
}
