using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// JSON-backed configuration loader with optional environment suffixes, hot-reload, and backups.
    /// Keeps server/client knobs data-driven for world generation, networking, and map control.
    /// </summary>
    public sealed class DataDrivenConfigManager : IDisposable
    {
        private readonly string _configDirectory;
        private readonly string _environment;
        private readonly Dictionary<string, object> _cache = new();
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        private readonly object _sync = new();

        private static readonly Dictionary<Type, string> DefaultNames = new()
        {
            [typeof(ServerConfiguration)] = "server",
            [typeof(WorldConfiguration)] = "world",
            [typeof(GameplayConfiguration)] = "gameplay",
            [typeof(NetworkConfiguration)] = "network",
            [typeof(PerformanceConfiguration)] = "performance",
            [typeof(SecurityConfiguration)] = "security",
            [typeof(DatabaseConfiguration)] = "database",
            [typeof(LoggingConfiguration)] = "logging",
            [typeof(TerrainGenerationSettings)] = "terrain",
            [typeof(CaveGenerationSettings)] = "caves",
            [typeof(RiverGenerationSettings)] = "rivers",
            [typeof(LakeGenerationSettings)] = "lakes",
            [typeof(WorldMapControlSettings)] = "world_map_control"
        };

        public DataDrivenConfigManager(string configDirectory = "config", string environment = "default")
        {
            _configDirectory = configDirectory;
            _environment = environment ?? string.Empty;
            Directory.CreateDirectory(_configDirectory);
        }

        public T GetConfiguration<T>(string? name = null) where T : class, new()
        {
            var cacheKey = BuildCacheKey<T>(name);
            lock (_sync)
            {
                if (_cache.TryGetValue(cacheKey, out var cached) && cached is T typed)
                {
                    return typed;
                }
            }

            var config = LoadConfiguration<T>(name);
            lock (_sync)
            {
                _cache[cacheKey] = config;
            }

            EnsureWatcher<T>(name);
            return config;
        }

        public async Task SaveConfigurationAsync<T>(T configuration, string? name = null) where T : class
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var path = BuildPath<T>(name);
            var cacheKey = BuildCacheKey<T>(name);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(configuration, jsonOptions);
            await File.WriteAllTextAsync(path, json);

            lock (_sync)
            {
                _cache[cacheKey] = configuration;
            }
        }

        public async Task<string> CreateBackupAsync(string? destinationDirectory = null)
        {
            var targetRoot = string.IsNullOrWhiteSpace(destinationDirectory)
                ? Path.Combine(_configDirectory, "backups")
                : destinationDirectory;

            Directory.CreateDirectory(targetRoot);
            var backupPath = Path.Combine(targetRoot, $"config-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(backupPath);

            foreach (var file in Directory.GetFiles(_configDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(backupPath, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, target, true));
            }

            return backupPath;
        }

        public bool ValidateConfigurations()
        {
            var isValid = true;
            var net = GetConfiguration<NetworkConfiguration>();
            if (net.ConnectionSettings.MaxPacketSize <= 0)
            {
                Console.WriteLine("[Config] Invalid MaxPacketSize; using default 262144 bytes.");
                net.ConnectionSettings.MaxPacketSize = 262_144;
                isValid = false;
            }

            if (net.ConnectionSettings.CompressionThreshold < 0)
            {
                Console.WriteLine("[Config] CompressionThreshold below zero; clamping to 0.");
                net.ConnectionSettings.CompressionThreshold = 0;
                isValid = false;
            }

            var world = GetConfiguration<WorldConfiguration>();
            if (world.RenderDistance < 1)
            {
                world.RenderDistance = 1;
                isValid = false;
            }

            if (world.SeaLevel < 0 || world.SeaLevel > 320)
            {
                world.SeaLevel = 62;
                isValid = false;
            }

            return isValid;
        }

        private T LoadConfiguration<T>(string? name) where T : class, new()
        {
            var path = BuildPath<T>(name);
            if (!File.Exists(path))
            {
                var defaults = new T();
                File.WriteAllText(path, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
                return defaults;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, options) ?? new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Failed to load '{path}': {ex.Message}");
                return new T();
            }
        }

        private void EnsureWatcher<T>(string? name) where T : class, new()
        {
            var path = BuildPath<T>(name);
            var fileName = Path.GetFileName(path);

            if (_watchers.ContainsKey(path))
            {
                return;
            }

            var watcher = new FileSystemWatcher(_configDirectory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Changed += (_, __) => Reload<T>(name);
            watcher.Created += (_, __) => Reload<T>(name);

            _watchers[path] = watcher;
        }

        private void Reload<T>(string? name) where T : class, new()
        {
            var cacheKey = BuildCacheKey<T>(name);
            var updated = LoadConfiguration<T>(name);
            lock (_sync)
            {
                _cache[cacheKey] = updated;
            }

            Console.WriteLine($"[Config] Reloaded {cacheKey} after file change.");
        }

        private string BuildPath<T>(string? name) where T : class
        {
            var fileName = BuildFileName<T>(name);
            return Path.Combine(_configDirectory, fileName);
        }

        private string BuildFileName<T>(string? name) where T : class
        {
            var baseName = !string.IsNullOrWhiteSpace(name)
                ? name
                : DefaultNames.TryGetValue(typeof(T), out var mapped)
                    ? mapped
                    : typeof(T).Name.ToLowerInvariant().Replace("configuration", string.Empty);

            var suffix = string.IsNullOrWhiteSpace(_environment) ? string.Empty : $".{_environment}";
            return $"{baseName}{suffix}.json";
        }

        private static string BuildCacheKey<T>(string? name)
        {
            return $"{typeof(T).FullName}:{name ?? string.Empty}";
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
            _cache.Clear();
        }
    }
}
