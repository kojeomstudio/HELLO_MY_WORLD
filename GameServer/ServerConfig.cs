using System.Text.Json;

namespace GameServerApp;

/// <summary>
/// Comprehensive server configuration for the Minecraft-style client-server architecture.
/// Replaces P2P networking with centralized server authority.
/// </summary>
public class ServerConfig
{
    public NetworkSettings Network { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public WorldSettings World { get; set; } = new();
    public GameplaySettings Gameplay { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();

    public static ServerConfig LoadFromFile(string configPath = "server-config.json")
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load server config: {ex.Message}");
        }

        var defaultConfig = new ServerConfig();
        defaultConfig.SaveToFile(configPath);
        return defaultConfig;
    }

    public void SaveToFile(string configPath = "server-config.json")
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, json);
            Console.WriteLine($"Server configuration saved to {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save server config: {ex.Message}");
        }
    }
}

public class NetworkSettings
{
    public int Port { get; set; } = 9000;
    public string BindAddress { get; set; } = "0.0.0.0";
    public int MaxConnections { get; set; } = 100;
    public int ConnectionTimeoutMinutes { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public bool EnableEncryption { get; set; } = false;
}

public class DatabaseSettings
{
    public string DatabaseFile { get; set; } = "minecraft_game.db";
    public bool EnableWALMode { get; set; } = true;
    public int ConnectionPoolSize { get; set; } = 10;
    public bool AutoBackup { get; set; } = true;
    public int BackupIntervalHours { get; set; } = 24;
}

public class WorldSettings
{
    public string DefaultWorldName { get; set; } = "default";
    public long WorldSeed { get; set; } = 12345;
    public int ChunkLoadRadius { get; set; } = 8;
    public int ChunkUnloadTimeoutMinutes { get; set; } = 30;
    public bool EnableTerrainGeneration { get; set; } = true;
    public bool EnableOreGeneration { get; set; } = true;
    public bool EnableVegetationGeneration { get; set; } = true;
    public int MaxWorldHeight { get; set; } = 256;
    public int MinWorldHeight { get; set; } = -64;
}

public class GameplaySettings
{
    public int MaxPlayersPerWorld { get; set; } = 20;
    public bool EnablePvP { get; set; } = true;
    public bool EnableFlying { get; set; } = true;
    public double MovementValidationTolerance { get; set; } = 10.0;
    public int MaxBlockInteractionDistance { get; set; } = 5;
    public bool EnableInventorySystem { get; set; } = true;
    public int MaxInventorySlots { get; set; } = 36;
    public bool EnableChatSystem { get; set; } = true;
}

public class SecuritySettings
{
    public bool RequireAuthentication { get; set; } = true;
    public int MinPasswordLength { get; set; } = 6;
    public int SessionTimeoutHours { get; set; } = 24;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxMessagesPerSecond { get; set; } = 10;
    public bool EnableAntiCheat { get; set; } = true;
}

public class PerformanceSettings
{
    public int MaintenanceIntervalMinutes { get; set; } = 5;
    public int ChunkSaveIntervalMinutes { get; set; } = 10;
    public int PlayerStateSaveIntervalMinutes { get; set; } = 2;
    public bool EnableGarbageCollection { get; set; } = true;
    public int MaxConcurrentChunkGenerations { get; set; } = 4;
    public bool EnableMetrics { get; set; } = true;
}

/// <summary>
/// Enhanced server launcher with comprehensive configuration support.
/// Completes the transition from P2P to centralized client-server architecture.
/// </summary>
public static class ServerLauncher
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Minecraft-Style Game Server ===");
        Console.WriteLine("Client-Server Architecture");
        Console.WriteLine("Protobuf Network Protocol");
        Console.WriteLine("====================================");

        try
        {
            var config = ServerConfig.LoadFromFile();
            var server = new GameServer(config.Network.Port, config.Database.DatabaseFile);

            Console.WriteLine($"Server Configuration:");
            Console.WriteLine($"  Network Port: {config.Network.Port}");
            Console.WriteLine($"  Max Connections: {config.Network.MaxConnections}");
            Console.WriteLine($"  Database: {config.Database.DatabaseFile}");
            Console.WriteLine($"  World Seed: {config.World.WorldSeed}");
            Console.WriteLine($"  Max Players: {config.Gameplay.MaxPlayersPerWorld}");
            Console.WriteLine($"  Authentication: {(config.Security.RequireAuthentication ? "Enabled" : "Disabled")}");
            Console.WriteLine();

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Shutting down server gracefully...");
                server.Stop();
                e.Cancel = true;
            };

            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start server: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }

        return 0;
    }

    public static void DisplayServerInfo()
    {
        Console.WriteLine("=== Server Architecture Information ===");
        Console.WriteLine("• Complete P2P to Client-Server conversion");
        Console.WriteLine("• Centralized world state management");
        Console.WriteLine("• Server-authoritative block changes");
        Console.WriteLine("• Enhanced player session management");
        Console.WriteLine("• Chunk-based world loading/unloading");
        Console.WriteLine("• Protobuf-based network communication");
        Console.WriteLine("• SQLite database with proper schema");
        Console.WriteLine("• Secure authentication with hashed passwords");
        Console.WriteLine("• Real-time player synchronization");
        Console.WriteLine("• Minecraft-style gameplay mechanics");
        Console.WriteLine("======================================");
    }
}