using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServer.Launcher
{
    /// <summary>
    /// 게임 서버 런처 전용 설정
    /// </summary>
    public class LauncherConfig
    {
        private const string DefaultConfigPath = "launcher-config.json";

        /// <summary>
        /// 서버 설정 파일 경로
        /// </summary>
        [JsonPropertyName("serverConfigPath")]
        public string ServerConfigPath { get; set; } = "server-config.json";

        /// <summary>
        /// GameCommon 설정 로드 여부
        /// </summary>
        [JsonPropertyName("loadGameCommonConfig")]
        public bool LoadGameCommonConfig { get; set; } = false;

        /// <summary>
        /// GameCommon config 디렉토리 경로
        /// </summary>
        [JsonPropertyName("gameCommonConfigPath")]
        public string GameCommonConfigPath { get; set; } = "config";

        /// <summary>
        /// 서버 시작 후 대기 여부 (false면 즉시 반환)
        /// </summary>
        [JsonPropertyName("waitForExit")]
        public bool WaitForExit { get; set; } = true;

        /// <summary>
        /// 자동 재시작 여부
        /// </summary>
        [JsonPropertyName("autoRestart")]
        public bool AutoRestart { get; set; } = false;

        /// <summary>
        /// 재시작 대기 시간 (초)
        /// </summary>
        [JsonPropertyName("restartDelaySeconds")]
        public int RestartDelaySeconds { get; set; } = 5;

        /// <summary>
        /// 로그 레벨
        /// </summary>
        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// 콘솔 컬러 출력 여부
        /// </summary>
        [JsonPropertyName("enableColorOutput")]
        public bool EnableColorOutput { get; set; } = true;

        /// <summary>
        /// 상태 모니터링 간격 (초)
        /// </summary>
        [JsonPropertyName("statusMonitoringIntervalSeconds")]
        public int StatusMonitoringIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 런처 버전
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 설정 파일 로드
        /// </summary>
        public static LauncherConfig Load(string? configPath = null)
        {
            configPath ??= DefaultConfigPath;

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[CONFIG] Launcher config not found at {configPath}, creating default...");
                var defaultConfig = new LauncherConfig();
                defaultConfig.Save(configPath);
                return defaultConfig;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    WriteIndented = true
                };

                var config = JsonSerializer.Deserialize<LauncherConfig>(jsonContent, options);
                if (config == null)
                {
                    Console.WriteLine($"[WARNING] Failed to deserialize config, using defaults");
                    return new LauncherConfig();
                }

                Console.WriteLine($"[CONFIG] Launcher config loaded from {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to load launcher config: {ex.Message}");
                Console.ResetColor();
                return new LauncherConfig();
            }
        }

        /// <summary>
        /// 설정 파일 저장
        /// </summary>
        public void Save(string? configPath = null)
        {
            configPath ??= DefaultConfigPath;

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, jsonContent);
                Console.WriteLine($"[CONFIG] Launcher config saved to {configPath}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to save launcher config: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
