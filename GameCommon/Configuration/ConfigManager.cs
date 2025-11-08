using System;
using System.IO;
using System.Text.Json;

namespace GameCommon.Configuration
{
    /// <summary>
    /// 통합 설정 관리자
    /// 모든 JSON 설정 파일을 로드하고 관리
    /// </summary>
    public class ConfigManager
    {
        private static ConfigManager? _instance;
        private static readonly object _lock = new object();

        public WorldConfig World { get; private set; } = new();
        public GameplayConfig Gameplay { get; private set; } = new();
        public ServerConfig Server { get; private set; } = new();

        private string _configDirectory = "config";
        private bool _initialized = false;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ConfigManager() { }

        /// <summary>
        /// 모든 설정 파일 로드
        /// </summary>
        public void LoadAll(string configDirectory = "config")
        {
            _configDirectory = configDirectory;

            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
                Console.WriteLine($"Created configuration directory: {_configDirectory}");
                GenerateDefaultConfigs();
            }

            try
            {
                LoadWorldConfig();
                LoadGameplayConfig();
                LoadServerConfig();

                _initialized = true;
                Console.WriteLine("All configuration files loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 월드 설정 로드
        /// </summary>
        private void LoadWorldConfig()
        {
            string path = Path.Combine(_configDirectory, "world.json");
            World = LoadConfig<WorldConfig>(path, new WorldConfig());
        }

        /// <summary>
        /// 게임플레이 설정 로드
        /// </summary>
        private void LoadGameplayConfig()
        {
            string path = Path.Combine(_configDirectory, "gameplay.json");
            Gameplay = LoadConfig<GameplayConfig>(path, new GameplayConfig());
        }

        /// <summary>
        /// 서버 설정 로드
        /// </summary>
        private void LoadServerConfig()
        {
            string path = Path.Combine(_configDirectory, "server.json");
            Server = LoadConfig<ServerConfig>(path, new ServerConfig());
        }

        /// <summary>
        /// JSON 설정 파일 로드 (제네릭)
        /// </summary>
        private T LoadConfig<T>(string path, T defaultValue) where T : new()
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Configuration file not found: {path}, using defaults");
                SaveConfig(path, defaultValue);
                return defaultValue;
            }

            try
            {
                string jsonContent = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(jsonContent, JsonOptions);

                if (config == null)
                {
                    Console.WriteLine($"Failed to deserialize {path}, using defaults");
                    return defaultValue;
                }

                Console.WriteLine($"Loaded configuration from {path}");
                return config;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing {path}: {ex.Message}, using defaults");
                return defaultValue;
            }
        }

        /// <summary>
        /// 설정을 JSON 파일로 저장
        /// </summary>
        private void SaveConfig<T>(string path, T config)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(path, jsonContent);
                Console.WriteLine($"Saved default configuration to {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save configuration to {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// 기본 설정 파일 생성
        /// </summary>
        private void GenerateDefaultConfigs()
        {
            SaveConfig(Path.Combine(_configDirectory, "world.json"), new WorldConfig());
            SaveConfig(Path.Combine(_configDirectory, "gameplay.json"), new GameplayConfig());
            SaveConfig(Path.Combine(_configDirectory, "server.json"), new ServerConfig());
            Console.WriteLine("Generated default configuration files");
        }

        /// <summary>
        /// 설정 파일 재로드
        /// </summary>
        public void Reload()
        {
            Console.WriteLine("Reloading configuration...");
            LoadAll(_configDirectory);
        }

        /// <summary>
        /// 현재 설정을 파일로 저장
        /// </summary>
        public void SaveAll()
        {
            SaveConfig(Path.Combine(_configDirectory, "world.json"), World);
            SaveConfig(Path.Combine(_configDirectory, "gameplay.json"), Gameplay);
            SaveConfig(Path.Combine(_configDirectory, "server.json"), Server);
            Console.WriteLine("All configurations saved");
        }

        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public bool IsInitialized => _initialized;
    }
}
