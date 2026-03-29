using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.Utils;
using GameServerApp;

namespace GameServer
{
    /// <summary>
    /// 서버 설정 검증기
    /// </summary>
    public static class ConfigValidator
    {
        private static readonly Logger _logger = Logger.Instance;

        public static bool Validate(ServerConfig config, out List<string> errors)
        {
            errors = new List<string>();

            // Network 검증
            ValidateNetwork(config.Network, errors);

            // Database 검증
            ValidateDatabase(config.Database, errors);

            // World 검증
            ValidateWorld(config.World, errors);

            // Gameplay 검증
            ValidateGameplay(config.Gameplay, errors);

            // Security 검증
            ValidateSecurity(config.Security, errors);

            // Performance 검증
            ValidatePerformance(config.Performance, errors);

            if (errors.Any())
            {
                _logger.Error("ConfigValidator", $"Configuration validation failed with {errors.Count} error(s)");
                foreach (var error in errors)
                {
                    _logger.Error("ConfigValidator", $"  - {error}");
                }
                return false;
            }

            _logger.Info("ConfigValidator", "Configuration validation successful");
            return true;
        }

        private static void ValidateNetwork(NetworkSettings network, List<string> errors)
        {
            if (network.Port < 1024 || network.Port > 65535)
            {
                errors.Add($"Network.Port must be between 1024 and 65535 (current: {network.Port})");
            }

            if (network.MaxConnections < 1 || network.MaxConnections > 10000)
            {
                errors.Add($"Network.MaxConnections must be between 1 and 10000 (current: {network.MaxConnections})");
            }

            if (network.HeartbeatIntervalSeconds < 5 || network.HeartbeatIntervalSeconds > 300)
            {
                errors.Add($"Network.HeartbeatIntervalSeconds must be between 5 and 300 (current: {network.HeartbeatIntervalSeconds})");
            }
        }

        private static void ValidateDatabase(DatabaseSettings database, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(database.DatabaseFile))
            {
                errors.Add("Database.DatabaseFile cannot be empty");
            }

            if (database.ConnectionPoolSize < 1 || database.ConnectionPoolSize > 100)
            {
                errors.Add($"Database.ConnectionPoolSize must be between 1 and 100 (current: {database.ConnectionPoolSize})");
            }

            if (database.AutoBackup && database.BackupIntervalHours < 1)
            {
                errors.Add($"Database.BackupIntervalHours must be at least 1 when AutoBackup is enabled (current: {database.BackupIntervalHours})");
            }
        }

        private static void ValidateWorld(WorldSettings world, List<string> errors)
        {
            if (world.ChunkLoadRadius < 1 || world.ChunkLoadRadius > 32)
            {
                errors.Add($"World.ChunkLoadRadius must be between 1 and 32 (current: {world.ChunkLoadRadius})");
            }

            if (world.InitialWorldTime < 0 || world.InitialWorldTime > 24000)
            {
                errors.Add($"World.InitialWorldTime must be between 0 and 24000 (current: {world.InitialWorldTime})");
            }

            if (world.WeatherTickIntervalSeconds < 1 || world.WeatherTickIntervalSeconds > 300)
            {
                errors.Add($"World.WeatherTickIntervalSeconds must be between 1 and 300 (current: {world.WeatherTickIntervalSeconds})");
            }

            if (world.ClearWeatherDurationSeconds < 60)
            {
                errors.Add($"World.ClearWeatherDurationSeconds must be at least 60 (current: {world.ClearWeatherDurationSeconds})");
            }

            if (world.RainWeatherDurationSeconds < 30)
            {
                errors.Add($"World.RainWeatherDurationSeconds must be at least 30 (current: {world.RainWeatherDurationSeconds})");
            }

            if (world.StormWeatherDurationSeconds < 30)
            {
                errors.Add($"World.StormWeatherDurationSeconds must be at least 30 (current: {world.StormWeatherDurationSeconds})");
            }

            if (world.SnowWeatherDurationSeconds < 30)
            {
                errors.Add($"World.SnowWeatherDurationSeconds must be at least 30 (current: {world.SnowWeatherDurationSeconds})");
            }
        }

        private static void ValidateGameplay(GameplaySettings gameplay, List<string> errors)
        {
            if (gameplay.MaxPlayersPerWorld < 1 || gameplay.MaxPlayersPerWorld > 1000)
            {
                errors.Add($"Gameplay.MaxPlayersPerWorld must be between 1 and 1000 (current: {gameplay.MaxPlayersPerWorld})");
            }

            if (gameplay.MaxBlockInteractionDistance < 1 || gameplay.MaxBlockInteractionDistance > 20)
            {
                errors.Add($"Gameplay.MaxBlockInteractionDistance must be between 1 and 20 (current: {gameplay.MaxBlockInteractionDistance})");
            }
        }

        private static void ValidateSecurity(SecuritySettings security, List<string> errors)
        {
            if (security.RequireAuthentication && security.MinPasswordLength < 4)
            {
                errors.Add($"Security.MinPasswordLength must be at least 4 when RequireAuthentication is enabled (current: {security.MinPasswordLength})");
            }

            if (security.EnableRateLimiting && security.MaxMessagesPerSecond < 1)
            {
                errors.Add($"Security.MaxMessagesPerSecond must be at least 1 when EnableRateLimiting is enabled (current: {security.MaxMessagesPerSecond})");
            }

            if (security.MinPasswordLength > 128)
            {
                errors.Add($"Security.MinPasswordLength cannot exceed 128 (current: {security.MinPasswordLength})");
            }
        }

        private static void ValidatePerformance(PerformanceSettings performance, List<string> errors)
        {
            if (performance.MaintenanceIntervalMinutes < 1 || performance.MaintenanceIntervalMinutes > 1440)
            {
                errors.Add($"Performance.MaintenanceIntervalMinutes must be between 1 and 1440 (current: {performance.MaintenanceIntervalMinutes})");
            }

            if (performance.ChunkSaveIntervalMinutes < 1 || performance.ChunkSaveIntervalMinutes > 60)
            {
                errors.Add($"Performance.ChunkSaveIntervalMinutes must be between 1 and 60 (current: {performance.ChunkSaveIntervalMinutes})");
            }

            if (performance.MaxConcurrentChunkGenerations < 1 || performance.MaxConcurrentChunkGenerations > 64)
            {
                errors.Add($"Performance.MaxConcurrentChunkGenerations must be between 1 and 64 (current: {performance.MaxConcurrentChunkGenerations})");
            }
        }

        public static void LogConfiguration(ServerConfig config)
        {
            _logger.Info("ConfigValidator", "=== Server Configuration ===");
            _logger.Info("ConfigValidator", $"Network:");
            _logger.Info("ConfigValidator", $"  Port: {config.Network.Port}");
            _logger.Info("ConfigValidator", $"  MaxConnections: {config.Network.MaxConnections}");
            _logger.Info("ConfigValidator", $"  HeartbeatInterval: {config.Network.HeartbeatIntervalSeconds}s");

            _logger.Info("ConfigValidator", $"Database:");
            _logger.Info("ConfigValidator", $"  File: {config.Database.DatabaseFile}");
            _logger.Info("ConfigValidator", $"  WAL Mode: {config.Database.EnableWALMode}");
            _logger.Info("ConfigValidator", $"  Connection Pool: {config.Database.ConnectionPoolSize}");
            _logger.Info("ConfigValidator", $"  Auto Backup: {config.Database.AutoBackup}");

            _logger.Info("ConfigValidator", $"World:");
            _logger.Info("ConfigValidator", $"  Seed: {config.World.WorldSeed}");
            _logger.Info("ConfigValidator", $"  Chunk Load Radius: {config.World.ChunkLoadRadius}");
            _logger.Info("ConfigValidator", $"  Terrain Generation: {config.World.EnableTerrainGeneration}");
            _logger.Info("ConfigValidator", $"  Day/Night Cycle: {config.World.EnableDayNightCycle}");

            _logger.Info("ConfigValidator", $"Gameplay:");
            _logger.Info("ConfigValidator", $"  Max Players: {config.Gameplay.MaxPlayersPerWorld}");
            _logger.Info("ConfigValidator", $"  PvP: {config.Gameplay.EnablePvP}");
            _logger.Info("ConfigValidator", $"  Flying: {config.Gameplay.EnableFlying}");

            _logger.Info("ConfigValidator", $"Security:");
            _logger.Info("ConfigValidator", $"  Authentication Required: {config.Security.RequireAuthentication}");
            _logger.Info("ConfigValidator", $"  Rate Limiting: {config.Security.EnableRateLimiting}");
            _logger.Info("ConfigValidator", $"  Max Messages/sec: {config.Security.MaxMessagesPerSecond}");

            _logger.Info("ConfigValidator", $"Performance:");
            _logger.Info("ConfigValidator", $"  Maintenance Interval: {config.Performance.MaintenanceIntervalMinutes}min");
            _logger.Info("ConfigValidator", $"  Chunk Save Interval: {config.Performance.ChunkSaveIntervalMinutes}min");
            _logger.Info("ConfigValidator", $"  Max Concurrent Chunk Generations: {config.Performance.MaxConcurrentChunkGenerations}");
            _logger.Info("ConfigValidator", "============================");
        }
    }
}
