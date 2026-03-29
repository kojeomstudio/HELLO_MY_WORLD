using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Centralized loader for server/client/world/network/gameplay JSON configs.
    /// </summary>
    public sealed class UnifiedConfigManager
    {
        private static readonly Lazy<UnifiedConfigManager> _instance = new(() => new UnifiedConfigManager());
        public static UnifiedConfigManager Instance => _instance.Value;

        public ServerConfig Server { get; private set; } = new();
        public ClientConfig Client { get; private set; } = new();
        public WorldConfig World { get; private set; } = new();
        public GameplayConfig Gameplay { get; private set; } = new();
        public NetworkConfig Network { get; private set; } = new();
        public Dictionary<string, object> Runtime { get; private set; } = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private UnifiedConfigManager()
        {
        }

        public void LoadAll(string rootPath = "config")
        {
            Server = Load(Path.Combine(rootPath, "server.json"), new ServerConfig());
            Client = Load(Path.Combine(rootPath, "client_config.json"), new ClientConfig());
            World = Load(Path.Combine(rootPath, "world.json"), new WorldConfig());
            Gameplay = Load(Path.Combine(rootPath, "gameplay.json"), new GameplayConfig());
            Network = Load(Path.Combine(rootPath, "network.json"), new NetworkConfig());
            Runtime = Load(Path.Combine(rootPath, "runtime.json"), new Dictionary<string, object>());
        }

        public void SaveAll(string rootPath = "config")
        {
            Save(Path.Combine(rootPath, "server.json"), Server);
            Save(Path.Combine(rootPath, "client_config.json"), Client);
            Save(Path.Combine(rootPath, "world.json"), World);
            Save(Path.Combine(rootPath, "gameplay.json"), Gameplay);
            Save(Path.Combine(rootPath, "network.json"), Network);
            Save(Path.Combine(rootPath, "runtime.json"), Runtime);
        }

        private T Load<T>(string path, T fallback)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
                else
                {
                    Console.WriteLine($"[Config] Missing '{path}', using defaults.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Config] Failed to load '{path}': {ex.Message}");
            }

            return fallback;
        }

        private void Save<T>(string path, T data)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Config] Failed to save '{path}': {ex.Message}");
            }
        }
    }
}
