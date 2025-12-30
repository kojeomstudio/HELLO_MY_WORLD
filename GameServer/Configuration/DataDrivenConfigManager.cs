using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Comprehensive data-driven configuration manager that handles all game settings
    /// with JSON file management, validation, hot-reloading, and environment-specific configurations.
    /// </summary>
    public class DataDrivenConfigManager
    {
        private readonly Dictionary<Type, object> _configurations;
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers;
        private readonly Dictionary<string, DateTime> _lastModified;
        private readonly string _configDirectory;
        private readonly string _environment;
        
        public DataDrivenConfigManager(string configDirectory = "configs", string environment = "production")
        {
            _configDirectory = configDirectory;
            _environment = environment;
            _configurations = new Dictionary<Type, object>();
            _fileWatchers = new Dictionary<string, FileSystemWatcher>();
            _lastModified = new Dictionary<string, DateTime>();
            
            // Ensure config directory exists
            Directory.CreateDirectory(_configDirectory);
            
            // Load all configurations
            LoadAllConfigurations();
            
            // Setup file watchers for hot-reloading
            SetupFileWatchers();
        }
        
        /// <summary>
        /// Get configuration of type T
        /// </summary>
        public T GetConfiguration<T>(string configName = null) where T : class, new()
        {
            var type = typeof(T);
            
            if (_configurations.TryGetValue(type, out var config))
            {
                return config as T;
            }
            
            // Try to load from file if not in memory
            var loadedConfig = LoadConfiguration<T>(configName);
            if (loadedConfig != null)
            {
                _configurations[type] = loadedConfig;
                return loadedConfig;
            }
            
            // Return default instance if loading fails
            return new T();
        }
        
        /// <summary>
        /// Save configuration of type T
        /// </summary>
        public async Task SaveConfigurationAsync<T>(T configuration, string configName = null) where T : class
        {
            var type = typeof(T);
            var fileName = GetConfigFileName<T>(configName);
            var filePath = Path.Combine(_configDirectory, fileName);
            
            try
            {
                // Update in-memory configuration
                _configurations[type] = configuration;
                
                // Serialize to JSON with pretty formatting
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(configuration, jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                
                // Update last modified time
                _lastModified[fileName] = DateTime.UtcNow;
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration {fileName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load all configuration files from directory
        /// </summary>
        private void LoadAllConfigurations()
        {
            var configFiles = Directory.GetFiles(_configDirectory, "*.json");
            
            foreach (var filePath in configFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                _lastModified[fileName] = File.GetLastWriteTimeUtc(filePath);
            }
            
            // Load known configuration types
            LoadConfiguration<ServerConfiguration>("server");
            LoadConfiguration<WorldConfiguration>("world");
            LoadConfiguration<GameplayConfiguration>("gameplay");
            LoadConfiguration<NetworkConfiguration>("network");
            LoadConfiguration<PerformanceConfiguration>("performance");
            LoadConfiguration<SecurityConfiguration>("security");
            LoadConfiguration<DatabaseConfiguration>("database");
            LoadConfiguration<LoggingConfiguration>("logging");
            LoadConfiguration<TerrainGenerationSettings>("terrain");
            LoadConfiguration<CaveGenerationSettings>("caves");
            LoadConfiguration<RiverGenerationSettings>("rivers");
            LoadConfiguration<LakeGenerationSettings>("lakes");
            LoadConfiguration<WorldMapControlSettings>("worldmap");
        }
        
        /// <summary>
        /// Load configuration of type T from file
        /// </summary>
        private T LoadConfiguration<T>(string configName = null) where T : class, new()
        {
            var fileName = GetConfigFileName<T>(configName);
            var filePath = Path.Combine(_configDirectory, fileName);
            
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var configuration = JsonSerializer.Deserialize<T>(json, options);
                    
                    if (configuration != null)
                    {
                        ValidateConfiguration(configuration);
                        return configuration;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration {fileName}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Get configuration file name for type T
        /// </summary>
        private string GetConfigFileName<T>(string configName) where T : class
        {
            if (!string.IsNullOrEmpty(configName))
            {
                configName = typeof(T).Name.ToLowerInvariant().Replace("configuration", "");
            }
            
            return $"{configName}-{_environment}.json";
        }
        
        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void ValidateConfiguration<T>(T configuration) where T : class
        {
            var validationMethods = typeof(T).GetMethods()
                .Where(m => m.Name.StartsWith("Validate") && m.ReturnType == typeof(bool));
            
            foreach (var method in validationMethods)
            {
                try
                {
                    var result = (bool)method.Invoke(configuration, null);
                    if (!result)
                    {
                        Console.WriteLine($"Configuration validation failed: {method.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Configuration validation error in {method.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Setup file watchers for hot-reloading
        /// </summary>
        private void SetupFileWatchers()
        {
            foreach (var kvp in _lastModified)
            {
                var fileName = kvp.Key;
                var filePath = Path.Combine(_configDirectory, fileName + ".json");
                
                try
                {
                    var watcher = new FileSystemWatcher(_configDirectory, fileName + ".json")
                    {
                        NotifyFilter = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };
                    
                    watcher.Changed += (sender, e) => OnConfigurationFileChanged(fileName);
                    watcher.Created += (sender, e) => OnConfigurationFileChanged(fileName);
                    
                    _fileWatchers[fileName] = watcher;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting up file watcher for {fileName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Handle configuration file change
        /// </summary>
        private void OnConfigurationFileChanged(string fileName)
        {
            var filePath = Path.Combine(_configDirectory, fileName + ".json");
            
            // Debounce rapid file changes
            Task.Delay(100).ContinueWith(async _ =>
            {
                try
                {
                    var currentModified = File.GetLastWriteTimeUtc(filePath);
                    if (currentModified > _lastModified[fileName])
                    {
                        Console.WriteLine($"Configuration file {fileName} changed, reloading...");
                        
                        // Reload configuration based on file name
                        switch (fileName)
                        {
                            case "server":
                                LoadConfiguration<ServerConfiguration>("server");
                                break;
                            case "world":
                                LoadConfiguration<WorldConfiguration>("world");
                                break;
                            case "gameplay":
                                LoadConfiguration<GameplayConfiguration>("gameplay");
                                break;
                            case "network":
                                LoadConfiguration<NetworkConfiguration>("network");
                                break;
                            case "performance":
                                LoadConfiguration<PerformanceConfiguration>("performance");
                                break;
                            case "security":
                                LoadConfiguration<SecurityConfiguration>("security");
                                break;
                            case "database":
                                LoadConfiguration<DatabaseConfiguration>("database");
                                break;
                            case "logging":
                                LoadConfiguration<LoggingConfiguration>("logging");
                                break;
                            case "terrain":
                                LoadConfiguration<TerrainGenerationSettings>("terrain");
                                break;
                            case "caves":
                                LoadConfiguration<CaveGenerationSettings>("caves");
                                break;
                            case "rivers":
                                LoadConfiguration<RiverGenerationSettings>("rivers");
                                break;
                            case "lakes":
                                LoadConfiguration<LakeGenerationSettings>("lakes");
                                break;
                            case "worldmap":
                                LoadConfiguration<WorldMapControlSettings>("worldmap");
                                break;
                        }
                        
                        _lastModified[fileName] = currentModified;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reloading configuration {fileName}: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Get environment-specific configuration value
        /// </summary>
        public T GetEnvironmentValue<T>(string key, T defaultValue = default(T))
        {
            var envKey = $"HELLO_MY_WORLD_{key.ToUpperInvariant()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            
            if (!string.IsNullOrEmpty(envValue))
            {
                try
                {
                    return (T)Convert.ChangeType(envValue, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Create default configuration files
        /// </summary>
        public async Task CreateDefaultConfigurationsAsync()
        {
            await SaveConfigurationAsync(new ServerConfiguration(), "server");
            await SaveConfigurationAsync(new WorldConfiguration(), "world");
            await SaveConfigurationAsync(new GameplayConfiguration(), "gameplay");
            await SaveConfigurationAsync(new NetworkConfiguration(), "network");
            await SaveConfigurationAsync(new PerformanceConfiguration(), "performance");
            await SaveConfigurationAsync(new SecurityConfiguration(), "security");
            await SaveConfigurationAsync(new DatabaseConfiguration(), "database");
            await SaveConfigurationAsync(new LoggingConfiguration(), "logging");
            await SaveConfigurationAsync(new TerrainGenerationSettings(), "terrain");
            await SaveConfigurationAsync(new CaveGenerationSettings(), "caves");
            await SaveConfigurationAsync(new RiverGenerationSettings(), "rivers");
            await SaveConfigurationAsync(new LakeGenerationSettings(), "lakes");
            await SaveConfigurationAsync(new WorldMapControlSettings(), "worldmap");
            
            Console.WriteLine("Default configuration files created successfully.");
        }
        
        /// <summary>
        /// Backup all configuration files
        /// </summary>
        public async Task<bool> BackupConfigurationsAsync(string backupDirectory)
        {
            try
            {
                Directory.CreateDirectory(backupDirectory);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"config-backup-{timestamp}");
                
                Directory.CreateDirectory(backupPath);
                
                foreach (var file in Directory.GetFiles(_configDirectory, "*.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var destination = Path.Combine(backupPath, fileName);
                    await Task.Run(() => File.Copy(file, destination, true));
                }
                
                Console.WriteLine($"Configuration backup created at: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating configuration backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restore configuration from backup
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    Console.WriteLine($"Backup directory does not exist: {backupPath}");
                    return false;
                }
                
                foreach (var file in Directory.GetFiles(backupPath, "*.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var destination = Path.Combine(_configDirectory, fileName);
                    await Task.Run(() => File.Copy(file, destination, true));
                }
                
                Console.WriteLine($"Configuration restored from backup: {backupPath}");
                
                // Reload all configurations after restore
                LoadAllConfigurations();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring configuration from backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Dispose file watchers
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _fileWatchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            
            _fileWatchers.Clear();
            _configurations.Clear();
            _lastModified.Clear();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Comprehensive data-driven configuration manager that handles all game settings
    /// with JSON file management, validation, hot-reloading, and environment-specific configurations.
    /// </summary>
    public class DataDrivenConfigManager
    {
        private readonly Dictionary<Type, object> _configurations;
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers;
        private readonly Dictionary<string, DateTime> _lastModified;
        private readonly string _configDirectory;
        private readonly string _environment;
        
        public DataDrivenConfigManager(string configDirectory = "configs", string environment = "production")
        {
            _configDirectory = configDirectory;
            _environment = environment;
            _configurations = new Dictionary<Type, object>();
            _fileWatchers = new Dictionary<string, FileSystemWatcher>();
            _lastModified = new Dictionary<string, DateTime>();
            
            // Ensure config directory exists
            Directory.CreateDirectory(_configDirectory);
            
            // Load all configurations
            LoadAllConfigurations();
            
            // Setup file watchers for hot-reloading
            SetupFileWatchers();
        }
        
        /// <summary>
        /// Get configuration of type T
        /// </summary>
        public T GetConfiguration<T>(string configName = null) where T : class, new()
        {
            var type = typeof(T);
            
            if (_configurations.TryGetValue(type, out var config))
            {
                return config as T;
            }
            
            // Try to load from file if not in memory
            var loadedConfig = LoadConfiguration<T>(configName);
            if (loadedConfig != null)
            {
                _configurations[type] = loadedConfig;
                return loadedConfig;
            }
            
            // Return default instance if loading fails
            return new T();
        }
        
        /// <summary>
        /// Save configuration of type T
        /// </summary>
        public async Task SaveConfigurationAsync<T>(T configuration, string configName = null) where T : class
        {
            var type = typeof(T);
            var fileName = GetConfigFileName<T>(configName);
            var filePath = Path.Combine(_configDirectory, fileName);
            
            try
            {
                // Update in-memory configuration
                _configurations[type] = configuration;
                
                // Serialize to JSON with pretty formatting
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(configuration, jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                
                // Update last modified time
                _lastModified[fileName] = DateTime.UtcNow;
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration {fileName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load all configuration files from directory
        /// </summary>
        private void LoadAllConfigurations()
        {
            var configFiles = Directory.GetFiles(_configDirectory, "*.json");
            
            foreach (var filePath in configFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                _lastModified[fileName] = File.GetLastWriteTimeUtc(filePath);
            }
            
            // Load known configuration types
            LoadConfiguration<ServerConfiguration>("server");
            LoadConfiguration<WorldConfiguration>("world");
            LoadConfiguration<GameplayConfiguration>("gameplay");
            LoadConfiguration<NetworkConfiguration>("network");
            LoadConfiguration<PerformanceConfiguration>("performance");
            LoadConfiguration<SecurityConfiguration>("security");
            LoadConfiguration<DatabaseConfiguration>("database");
            LoadConfiguration<LoggingConfiguration>("logging");
            LoadConfiguration<TerrainGenerationSettings>("terrain");
            LoadConfiguration<CaveGenerationSettings>("caves");
            LoadConfiguration<RiverGenerationSettings>("rivers");
            LoadConfiguration<LakeGenerationSettings>("lakes");
            LoadConfiguration<WorldMapControlSettings>("worldmap");
        }
        
        /// <summary>
        /// Load configuration of type T from file
        /// </summary>
        private T LoadConfiguration<T>(string configName = null) where T : class, new()
        {
            var fileName = GetConfigFileName<T>(configName);
            var filePath = Path.Combine(_configDirectory, fileName);
            
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var configuration = JsonSerializer.Deserialize<T>(json, options);
                    
                    if (configuration != null)
                    {
                        ValidateConfiguration(configuration);
                        return configuration;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration {fileName}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Get configuration file name for type T
        /// </summary>
        private string GetConfigFileName<T>(string configName) where T : class
        {
            if (!string.IsNullOrEmpty(configName))
            {
                configName = typeof(T).Name.ToLowerInvariant().Replace("configuration", "");
            }
            
            return $"{configName}-{_environment}.json";
        }
        
        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void ValidateConfiguration<T>(T configuration) where T : class
        {
            var validationMethods = typeof(T).GetMethods()
                .Where(m => m.Name.StartsWith("Validate") && m.ReturnType == typeof(bool));
            
            foreach (var method in validationMethods)
            {
                try
                {
                    var result = (bool)method.Invoke(configuration, null);
                    if (!result)
                    {
                        Console.WriteLine($"Configuration validation failed: {method.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Configuration validation error in {method.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Setup file watchers for hot-reloading
        /// </summary>
        private void SetupFileWatchers()
        {
            foreach (var kvp in _lastModified)
            {
                var fileName = kvp.Key;
                var filePath = Path.Combine(_configDirectory, fileName + ".json");
                
                try
                {
                    var watcher = new FileSystemWatcher(_configDirectory, fileName + ".json")
                    {
                        NotifyFilter = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };
                    
                    watcher.Changed += (sender, e) => OnConfigurationFileChanged(fileName);
                    watcher.Created += (sender, e) => OnConfigurationFileChanged(fileName);
                    
                    _fileWatchers[fileName] = watcher;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting up file watcher for {fileName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Handle configuration file change
        /// </summary>
        private void OnConfigurationFileChanged(string fileName)
        {
            var filePath = Path.Combine(_configDirectory, fileName + ".json");
            
            // Debounce rapid file changes
            Task.Delay(100).ContinueWith(async _ =>
            {
                try
                {
                    var currentModified = File.GetLastWriteTimeUtc(filePath);
                    if (currentModified > _lastModified[fileName])
                    {
                        Console.WriteLine($"Configuration file {fileName} changed, reloading...");
                        
                        // Reload configuration based on file name
                        switch (fileName)
                        {
                            case "server":
                                LoadConfiguration<ServerConfiguration>("server");
                                break;
                            case "world":
                                LoadConfiguration<WorldConfiguration>("world");
                                break;
                            case "gameplay":
                                LoadConfiguration<GameplayConfiguration>("gameplay");
                                break;
                            case "network":
                                LoadConfiguration<NetworkConfiguration>("network");
                                break;
                            case "performance":
                                LoadConfiguration<PerformanceConfiguration>("performance");
                                break;
                            case "security":
                                LoadConfiguration<SecurityConfiguration>("security");
                                break;
                            case "database":
                                LoadConfiguration<DatabaseConfiguration>("database");
                                break;
                            case "logging":
                                LoadConfiguration<LoggingConfiguration>("logging");
                                break;
                            case "terrain":
                                LoadConfiguration<TerrainGenerationSettings>("terrain");
                                break;
                            case "caves":
                                LoadConfiguration<CaveGenerationSettings>("caves");
                                break;
                            case "rivers":
                                LoadConfiguration<RiverGenerationSettings>("rivers");
                                break;
                            case "lakes":
                                LoadConfiguration<LakeGenerationSettings>("lakes");
                                break;
                            case "worldmap":
                                LoadConfiguration<WorldMapControlSettings>("worldmap");
                                break;
                        }
                        
                        _lastModified[fileName] = currentModified;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reloading configuration {fileName}: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Get environment-specific configuration value
        /// </summary>
        public T GetEnvironmentValue<T>(string key, T defaultValue = default(T))
        {
            var envKey = $"HELLO_MY_WORLD_{key.ToUpperInvariant()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            
            if (!string.IsNullOrEmpty(envValue))
            {
                try
                {
                    return (T)Convert.ChangeType(envValue, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Create default configuration files
        /// </summary>
        public async Task CreateDefaultConfigurationsAsync()
        {
            await SaveConfigurationAsync(new ServerConfiguration(), "server");
            await SaveConfigurationAsync(new WorldConfiguration(), "world");
            await SaveConfigurationAsync(new GameplayConfiguration(), "gameplay");
            await SaveConfigurationAsync(new NetworkConfiguration(), "network");
            await SaveConfigurationAsync(new PerformanceConfiguration(), "performance");
            await SaveConfigurationAsync(new SecurityConfiguration(), "security");
            await SaveConfigurationAsync(new DatabaseConfiguration(), "database");
            await SaveConfigurationAsync(new LoggingConfiguration(), "logging");
            await SaveConfigurationAsync(new TerrainGenerationSettings(), "terrain");
            await SaveConfigurationAsync(new CaveGenerationSettings(), "caves");
            await SaveConfigurationAsync(new RiverGenerationSettings(), "rivers");
            await SaveConfigurationAsync(new LakeGenerationSettings(), "lakes");
            await SaveConfigurationAsync(new WorldMapControlSettings(), "worldmap");
            
            Console.WriteLine("Default configuration files created successfully.");
        }
        
        /// <summary>
        /// Backup all configuration files
        /// </summary>
        public async Task<bool> BackupConfigurationsAsync(string backupDirectory)
        {
            try
            {
                Directory.CreateDirectory(backupDirectory);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"config-backup-{timestamp}");
                
                Directory.CreateDirectory(backupPath);
                
                foreach (var file in Directory.GetFiles(_configDirectory, "*.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var destination = Path.Combine(backupPath, fileName);
                    await Task.Run(() => File.Copy(file, destination, true));
                }
                
                Console.WriteLine($"Configuration backup created at: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating configuration backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restore configuration from backup
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    Console.WriteLine($"Backup directory does not exist: {backupPath}");
                    return false;
                }
                
                foreach (var file in Directory.GetFiles(backupPath, "*.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var destination = Path.Combine(_configDirectory, fileName);
                    await Task.Run(() => File.Copy(file, destination, true));
                }
                
                Console.WriteLine($"Configuration restored from backup: {backupPath}");
                
                // Reload all configurations after restore
                LoadAllConfigurations();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring configuration from backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Dispose file watchers
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _fileWatchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            
            _fileWatchers.Clear();
            _configurations.Clear();
            _lastModified.Clear();
        }
    }
}
}
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    LogFileName = "server-{Date}.log",
                    MaxLogFileSize = 10485760,
                    MaxLogFiles = 10,
                    EnableJsonLogging = false,
                    EnableStructuredLogging = false,
                    Loggers = new Dictionary<string, string>
                    {
                        ["GameServer"] = "info",
                        ["Network"] = "debug",
                        ["World"] = "info",
                        ["Database"] = "warn",
                        ["Security"] = "info"
                    }
                };
                
                SaveConfiguration(LoggingConfigFile, config);
            }
        }
        
        /// <summary>
        /// Load all configurations
        /// </summary>
        private void LoadAllConfigurations()
        {
            LoadConfiguration<ServerConfiguration>(ServerConfigFile);
            LoadConfiguration<WorldConfiguration>(WorldConfigFile);
            LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
            LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
            LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
            LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
            LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
            LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
        }
        
        /// <summary>
        /// Load a specific configuration file
        /// </summary>
        private void LoadConfiguration<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    
                    var config = JsonSerializer.Deserialize<T>(json, options);
                    if (config != null)
                    {
                        lock (_configLock)
                        {
                            _configurations[fileName] = config;
                        }
                    }
                }
                else
                {
                    // Create default configuration if file doesn't exist
                    var defaultConfig = new T();
                    SaveConfiguration(fileName, defaultConfig);
                    lock (_configLock)
                    {
                        _configurations[fileName] = defaultConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration {fileName}: {ex.Message}");
                // Load default configuration as fallback
                var defaultConfig = new T();
                lock (_configLock)
                {
                    _configurations[fileName] = defaultConfig;
                }
            }
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        private void SaveConfiguration<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Setup file watchers for hot-reloading
        /// </summary>
        private void SetupFileWatchers()
        {
            var configFiles = new[]
            {
                ServerConfigFile, WorldConfigFile, GameplayConfigFile,
                NetworkConfigFile, PerformanceConfigFile, SecurityConfigFile,
                DatabaseConfigFile, LoggingConfigFile
            };
            
            foreach (var configFile in configFiles)
            {
                var watcher = new FileSystemWatcher(_configDirectory, configFile);
                watcher.Changed += (sender, e) => OnConfigurationChanged(configFile);
                watcher.Created += (sender, e) => OnConfigurationChanged(configFile);
                watcher.EnableRaisingEvents = true;
                
                lock (_configLock)
                {
                    _watchers[configFile] = watcher;
                }
            }
        }
        
        /// <summary>
        /// Handle configuration file changes
        /// </summary>
        private void OnConfigurationChanged(string fileName)
        {
            try
            {
                // Debounce rapid file changes
                System.Threading.Thread.Sleep(100);
                
                switch (fileName)
                {
                    case ServerConfigFile:
                        LoadConfiguration<ServerConfiguration>(ServerConfigFile);
                        break;
                    case WorldConfigFile:
                        LoadConfiguration<WorldConfiguration>(WorldConfigFile);
                        break;
                    case GameplayConfigFile:
                        LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
                        break;
                    case NetworkConfigFile:
                        LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
                        break;
                    case PerformanceConfigFile:
                        LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
                        break;
                    case SecurityConfigFile:
                        LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
                        break;
                    case DatabaseConfigFile:
                        LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
                        break;
                    case LoggingConfigFile:
                        LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
                        break;
                }
                
                Console.WriteLine($"Configuration {fileName} reloaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get configuration by type
        /// </summary>
        public T? GetConfiguration<T>() where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                lock (_configLock)
                {
                    return _configurations.TryGetValue(fileName, out var config) ? config as T : null;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update configuration
        /// </summary>
        public void UpdateConfiguration<T>(T config) where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                SaveConfiguration(fileName, config);
                lock (_configLock)
                {
                    _configurations[fileName] = config;
                }
            }
        }
        
        /// <summary>
        /// Validate all configurations
        /// </summary>
        public bool ValidateConfigurations()
        {
            var isValid = true;
            
            var serverConfig = GetConfiguration<ServerConfiguration>();
            if (serverConfig != null)
            {
                if (serverConfig.MaxPlayers <= 0 || serverConfig.MaxPlayers > 1000)
                {
                    Console.WriteLine("Invalid MaxPlayers in server configuration");
                    isValid = false;
                }
                
                if (serverConfig.Port <= 0 || serverConfig.Port > 65535)
                {
                    Console.WriteLine("Invalid Port in server configuration");
                    isValid = false;
                }
            }
            
            var worldConfig = GetConfiguration<WorldConfiguration>();
            if (worldConfig != null)
            {
                if (worldConfig.Environment.SeaLevel < 0 || worldConfig.Environment.SeaLevel > 255)
                {
                    Console.WriteLine("Invalid SeaLevel in world configuration");
                    isValid = false;
                }
                
                if (worldConfig.Environment.MaxBuildHeight < 0 || worldConfig.Environment.MaxBuildHeight > 1024)
                {
                    Console.WriteLine("Invalid MaxBuildHeight in world configuration");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Comprehensive data-driven configuration manager that handles all game settings
    /// through JSON files with validation, hot-reloading, and environment-specific configurations.
    /// </summary>
    public class DataDrivenConfigManager
    {
        private readonly Dictionary<string, object> _configurations;
        private readonly Dictionary<string, FileSystemWatcher> _watchers;
        private readonly object _configLock = new object();
        private readonly string _configDirectory;
        
        // Configuration file names
        private const string ServerConfigFile = "server-config.json";
        private const string WorldConfigFile = "world-config.json";
        private const string GameplayConfigFile = "gameplay-config.json";
        private const string NetworkConfigFile = "network-config.json";
        private const string PerformanceConfigFile = "performance-config.json";
        private const string SecurityConfigFile = "security-config.json";
        private const string DatabaseConfigFile = "database-config.json";
        private const string LoggingConfigFile = "logging-config.json";
        
        public DataDrivenConfigManager(string configDirectory = "config")
        {
            _configDirectory = configDirectory;
            _configurations = new Dictionary<string, object>();
            _watchers = new Dictionary<string, FileSystemWatcher>();
            
            InitializeConfigDirectory();
            LoadAllConfigurations();
            SetupFileWatchers();
        }
        
        /// <summary>
        /// Initialize configuration directory
        /// </summary>
        private void InitializeConfigDirectory()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
            
            // Create default configuration files if they don't exist
            CreateDefaultConfigurations();
        }
        
        /// <summary>
        /// Create default configuration files
        /// </summary>
        private void CreateDefaultConfigurations()
        {
            CreateDefaultServerConfig();
            CreateDefaultWorldConfig();
            CreateDefaultGameplayConfig();
            CreateDefaultNetworkConfig();
            CreateDefaultPerformanceConfig();
            CreateDefaultSecurityConfig();
            CreateDefaultDatabaseConfig();
            CreateDefaultLoggingConfig();
        }
        
        /// <summary>
        /// Create default server configuration
        /// </summary>
        private void CreateDefaultServerConfig()
        {
            var configPath = Path.Combine(_configDirectory, ServerConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new ServerConfiguration
                {
                    ServerName = "HELLO_MY_WORLD Server",
                    ServerVersion = "1.0.0",
                    MaxPlayers = 100,
                    Port = 8080,
                    BindAddress = "0.0.0.0",
                    EnableWhitelist = false,
                    EnablePvP = true,
                    EnableNether = false,
                    EnableEnd = false,
                    Motd = "Welcome to HELLO_MY_WORLD!",
                    ViewDistance = 10,
                    Difficulty = "normal",
                    GameMode = "survival",
                    EnableCommandBlocks = false,
                    AllowFlight = false,
                    SpawnProtection = true,
                    SpawnRadius = 16,
                    KeepSpawnLoaded = true,
                    EnableRcon = false,
                    RconPort = 25575,
                    RconPassword = ""
                };
                
                SaveConfiguration(ServerConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default world configuration
        /// </summary>
        private void CreateDefaultWorldConfig()
        {
            var configPath = Path.Combine(_configDirectory, WorldConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new WorldConfiguration
                {
                    WorldName = "world",
                    WorldType = "default",
                    Seed = null,
                    GenerateStructures = true,
                    AllowCheats = false,
                    Hardcore = false,
                    WorldBorder = new WorldBorderConfig
                    {
                        Enabled = false,
                        CenterX = 0,
                        CenterZ = 0,
                        Size = 60000000,
                        DamageBuffer = 5.0,
                        WarningTime = 15,
                        WarningDistance = 5
                    },
                    WorldMapControl = new WorldMapControlConfig
                    {
                        ProfileName = "default",
                        TerrainScale = 1.0,
                        TerrainHeightMultiplier = 1.0,
                        TerrainRoughness = 0.5,
                        CaveEnabled = true,
                        CaveDensity = 0.5,
                        RiverEnabled = true,
                        RiverDensity = 0.3,
                        LakeEnabled = true,
                        LakeDensity = 0.2,
                        BiomeTemperatureScale = 0.002,
                        BiomeMoistureScale = 0.003,
                        VegetationDensity = 0.5,
                        TreeDensity = 0.1,
                        GrassDensity = 0.3
                    },
                    Environment = new EnvironmentConfig
                    {
                        DayDuration = 12000,
                        NightDuration = 12000,
                        WeatherCycle = true,
                        ThunderCycle = true,
                        SeaLevel = 64,
                        MaxBuildHeight = 256,
                        MinBuildHeight = -64
                    }
                };
                
                SaveConfiguration(WorldConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default gameplay configuration
        /// </summary>
        private void CreateDefaultGameplayConfig()
        {
            var configPath = Path.Combine(_configDirectory, GameplayConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new GameplayConfiguration
                {
                    PlayerSettings = new PlayerSettingsConfig
                    {
                        MaxHealth = 20,
                        MaxHunger = 20,
                        MaxExperience = 2147483647,
                        RespawnCooldown = 5,
                        KeepInventoryOnDeath = false,
                        KeepExperienceOnDeath = false,
                        EnableSpectatorMode = true,
                        EnableFlying = false,
                        EnableCreativeMode = true
                    },
                    MobSettings = new MobSettingsConfig
                    {
                        EnableMobs = true,
                        EnableHostileMobs = true,
                        EnablePassiveMobs = true,
                        EnableNeutralMobs = true,
                        MobSpawningRate = 1.0,
                        MaxMobsPerChunk = 70,
                        MaxHostileMobsPerChunk = 40,
                        DespawnDistance = 128,
                        PersistentMobs = false
                    },
                    ItemSettings = new ItemSettingsConfig
                    {
                        EnableItemDrops = true,
                        EnableItemDespawning = true,
                        ItemDespawnTime = 6000,
                        MaxItemsPerChunk = 200,
                        EnableEnchanting = true,
                        EnableBrewing = true,
                        EnableAnvil = true,
                        EnableEnchantingTable = true,
                        MaxEnchantmentLevel = 30
                    },
                    BlockSettings = new BlockSettingsConfig
                    {
                        EnableBlockBreaking = true,
                        EnableBlockPlacing = true,
                        EnableRedstone = true,
                        EnablePistons = true,
                        EnableHoppers = true,
                        MaxBlockUpdateDistance = 64,
                        EnableTileEntities = true,
                        EnableCommandBlocks = false
                    },
                    EconomySettings = new EconomySettingsConfig
                    {
                        EnableEconomy = false,
                        StartingBalance = 0,
                        CurrencySymbol = "$",
                        EnablePlayerShops = false,
                        EnableAdminShops = false,
                        TaxRate = 0.0m,
                        EnableBanking = false
                    }
                };
                
                SaveConfiguration(GameplayConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default network configuration
        /// </summary>
        private void CreateDefaultNetworkConfig()
        {
            var configPath = Path.Combine(_configDirectory, NetworkConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new NetworkConfiguration
                {
                    ConnectionSettings = new ConnectionSettingsConfig
                    {
                        MaxConnections = 1000,
                        ConnectionTimeout = 30000,
                        KeepAliveInterval = 15000,
                        MaxPacketSize = 2097152,
                        EnableCompression = true,
                        CompressionThreshold = 256,
                        EnableEncryption = true,
                        ProtocolVersion = 757
                    },
                    BandwidthSettings = new BandwidthSettingsConfig
                    {
                        MaxUploadBandwidth = 1048576,
                        MaxDownloadBandwidth = 1048576,
                        EnableThrottling = false,
                        ThrottleThreshold = 10485760,
                        EnableQoS = false
                    },
                    SecuritySettings = new NetworkSecurityConfig
                    {
                        EnableDDoSProtection = true,
                        MaxConnectionsPerIP = 5,
                        ConnectionRateLimit = 10,
                        EnableIPWhitelist = false,
                        EnableIPBlacklist = false,
                        WhitelistIPs = new List<string>(),
                        BlacklistIPs = new List<string>(),
                        EnableProxyDetection = true
                    }
                };
                
                SaveConfiguration(NetworkConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default performance configuration
        /// </summary>
        private void CreateDefaultPerformanceConfig()
        {
            var configPath = Path.Combine(_configDirectory, PerformanceConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new PerformanceConfiguration
                {
                    ChunkSettings = new ChunkPerformanceConfig
                    {
                        MaxLoadedChunks = 10000,
                        ChunkGenerationThreads = 4,
                        ChunkSaveInterval = 600,
                        EnableChunkCompression = true,
                        EnableChunkCaching = true,
                        MaxCachedChunks = 1000,
                        ChunkUnloadDistance = 192,
                        EnableAsyncChunkLoading = true
                    },
                    EntitySettings = new EntityPerformanceConfig
                    {
                        MaxLoadedEntities = 10000,
                        EntityUpdateDistance = 128,
                        EnableEntityCulling = true,
                        EnableLazyEntityLoading = true,
                        MaxEntityUpdatesPerTick = 100,
                        EnableAsyncEntityProcessing = true
                    },
                    MemorySettings = new MemoryPerformanceConfig
                    {
                        MaxMemoryUsage = 4096,
                        EnableMemoryMonitoring = true,
                        GarbageCollectionInterval = 60,
                        EnableMemoryPooling = true,
                        MaxPooledObjects = 10000,
                        MemoryWarningThreshold = 0.8
                    },
                    ThreadSettings = new ThreadPerformanceConfig
                    {
                        WorkerThreads = Environment.ProcessorCount,
                        IoThreads = 4,
                        EnableThreadPool = true,
                        MaxThreadPoolSize = 100,
                        EnableWorkStealing = true,
                        ThreadPriority = "normal"
                    }
                };
                
                SaveConfiguration(PerformanceConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default security configuration
        /// </summary>
        private void CreateDefaultSecurityConfig()
        {
            var configPath = Path.Combine(_configDirectory, SecurityConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new SecurityConfiguration
                {
                    AuthenticationSettings = new AuthenticationConfig
                    {
                        EnableAuthentication = true,
                        RequirePassword = false,
                        MinPasswordLength = 8,
                        EnableTwoFactor = false,
                        SessionTimeout = 3600,
                        MaxLoginAttempts = 5,
                        LockoutDuration = 300,
                        EnableBruteForceProtection = true
                    },
                    PermissionSettings = new PermissionConfig
                    {
                        EnablePermissions = true,
                        DefaultPermissionLevel = "player",
                        PermissionLevels = new List<string>
                        {
                            "player", "moderator", "admin", "owner"
                        },
                        EnableInheritance = true,
                        EnableWildcardPermissions = true
                    },
                    ValidationSettings = new ValidationConfig
                    {
                        EnableInputValidation = true,
                        EnableCommandValidation = true,
                        EnableChatFilter = false,
                        MaxChatLength = 256,
                        MaxCommandLength = 256,
                        BlockedWords = new List<string>(),
                        EnableProfanityFilter = false
                    }
                };
                
                SaveConfiguration(SecurityConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default database configuration
        /// </summary>
        private void CreateDefaultDatabaseConfig()
        {
            var configPath = Path.Combine(_configDirectory, DatabaseConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new DatabaseConfiguration
                {
                    Type = "sqlite",
                    ConnectionString = "Data Source=world.db",
                    EnableConnectionPooling = true,
                    MaxPoolSize = 100,
                    MinPoolSize = 5,
                    ConnectionTimeout = 30,
                    CommandTimeout = 30,
                    EnableMigrations = true,
                    BackupInterval = 3600,
                    BackupRetentionDays = 7,
                    EnableCompression = false,
                    EnableEncryption = false
                };
                
                SaveConfiguration(DatabaseConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default logging configuration
        /// </summary>
        private void CreateDefaultLoggingConfig()
        {
            var configPath = Path.Combine(_configDirectory, LoggingConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new LoggingConfiguration
                {
                    LogLevel = "info",
                    EnableConsoleLogging = true,
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    LogFileName = "server-{Date}.log",
                    MaxLogFileSize = 10485760,
                    MaxLogFiles = 10,
                    EnableJsonLogging = false,
                    EnableStructuredLogging = false,
                    Loggers = new Dictionary<string, string>
                    {
                        ["GameServer"] = "info",
                        ["Network"] = "debug",
                        ["World"] = "info",
                        ["Database"] = "warn",
                        ["Security"] = "info"
                    }
                };
                
                SaveConfiguration(LoggingConfigFile, config);
            }
        }
        
        /// <summary>
        /// Load all configurations
        /// </summary>
        private void LoadAllConfigurations()
        {
            LoadConfiguration<ServerConfiguration>(ServerConfigFile);
            LoadConfiguration<WorldConfiguration>(WorldConfigFile);
            LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
            LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
            LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
            LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
            LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
            LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
        }
        
        /// <summary>
        /// Load a specific configuration file
        /// </summary>
        private void LoadConfiguration<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    
                    var config = JsonSerializer.Deserialize<T>(json, options);
                    if (config != null)
                    {
                        lock (_configLock)
                        {
                            _configurations[fileName] = config;
                        }
                    }
                }
                else
                {
                    // Create default configuration if file doesn't exist
                    var defaultConfig = new T();
                    SaveConfiguration(fileName, defaultConfig);
                    lock (_configLock)
                    {
                        _configurations[fileName] = defaultConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration {fileName}: {ex.Message}");
                // Load default configuration as fallback
                var defaultConfig = new T();
                lock (_configLock)
                {
                    _configurations[fileName] = defaultConfig;
                }
            }
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        private void SaveConfiguration<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Setup file watchers for hot-reloading
        /// </summary>
        private void SetupFileWatchers()
        {
            var configFiles = new[]
            {
                ServerConfigFile, WorldConfigFile, GameplayConfigFile,
                NetworkConfigFile, PerformanceConfigFile, SecurityConfigFile,
                DatabaseConfigFile, LoggingConfigFile
            };
            
            foreach (var configFile in configFiles)
            {
                var watcher = new FileSystemWatcher(_configDirectory, configFile);
                watcher.Changed += (sender, e) => OnConfigurationChanged(configFile);
                watcher.Created += (sender, e) => OnConfigurationChanged(configFile);
                watcher.EnableRaisingEvents = true;
                
                lock (_configLock)
                {
                    _watchers[configFile] = watcher;
                }
            }
        }
        
        /// <summary>
        /// Handle configuration file changes
        /// </summary>
        private void OnConfigurationChanged(string fileName)
        {
            try
            {
                // Debounce rapid file changes
                System.Threading.Thread.Sleep(100);
                
                switch (fileName)
                {
                    case ServerConfigFile:
                        LoadConfiguration<ServerConfiguration>(ServerConfigFile);
                        break;
                    case WorldConfigFile:
                        LoadConfiguration<WorldConfiguration>(WorldConfigFile);
                        break;
                    case GameplayConfigFile:
                        LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
                        break;
                    case NetworkConfigFile:
                        LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
                        break;
                    case PerformanceConfigFile:
                        LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
                        break;
                    case SecurityConfigFile:
                        LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
                        break;
                    case DatabaseConfigFile:
                        LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
                        break;
                    case LoggingConfigFile:
                        LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
                        break;
                }
                
                Console.WriteLine($"Configuration {fileName} reloaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get configuration by type
        /// </summary>
        public T? GetConfiguration<T>() where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                lock (_configLock)
                {
                    return _configurations.TryGetValue(fileName, out var config) ? config as T : null;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update configuration
        /// </summary>
        public void UpdateConfiguration<T>(T config) where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                SaveConfiguration(fileName, config);
                lock (_configLock)
                {
                    _configurations[fileName] = config;
                }
            }
        }
        
        /// <summary>
        /// Validate all configurations
        /// </summary>
        public bool ValidateConfigurations()
        {
            var isValid = true;
            
            var serverConfig = GetConfiguration<ServerConfiguration>();
            if (serverConfig != null)
            {
                if (serverConfig.MaxPlayers <= 0 || serverConfig.MaxPlayers > 1000)
                {
                    Console.WriteLine("Invalid MaxPlayers in server configuration");
                    isValid = false;
                }
                
                if (serverConfig.Port <= 0 || serverConfig.Port > 65535)
                {
                    Console.WriteLine("Invalid Port in server configuration");
                    isValid = false;
                }
            }
            
            var worldConfig = GetConfiguration<WorldConfiguration>();
            if (worldConfig != null)
            {
                if (worldConfig.Environment.SeaLevel < 0 || worldConfig.Environment.SeaLevel > 255)
                {
                    Console.WriteLine("Invalid SeaLevel in world configuration");
                    isValid = false;
                }
                
                if (worldConfig.Environment.MaxBuildHeight < 0 || worldConfig.Environment.MaxBuildHeight > 1024)
                {
                    Console.WriteLine("Invalid MaxBuildHeight in world configuration");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
    }
}
}
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Comprehensive data-driven configuration manager that handles all game settings
    /// through JSON files with validation, hot-reloading, and environment-specific configurations.
    /// </summary>
    public class DataDrivenConfigManager
    {
        private readonly Dictionary<string, object> _configurations;
        private readonly Dictionary<string, FileSystemWatcher> _watchers;
        private readonly object _configLock = new object();
        private readonly string _configDirectory;
        
        // Configuration file names
        private const string ServerConfigFile = "server-config.json";
        private const string WorldConfigFile = "world-config.json";
        private const string GameplayConfigFile = "gameplay-config.json";
        private const string NetworkConfigFile = "network-config.json";
        private const string PerformanceConfigFile = "performance-config.json";
        private const string SecurityConfigFile = "security-config.json";
        private const string DatabaseConfigFile = "database-config.json";
        private const string LoggingConfigFile = "logging-config.json";
        
        public DataDrivenConfigManager(string configDirectory = "config")
        {
            _configDirectory = configDirectory;
            _configurations = new Dictionary<string, object>();
            _watchers = new Dictionary<string, FileSystemWatcher>();
            
            InitializeConfigDirectory();
            LoadAllConfigurations();
            SetupFileWatchers();
        }
        
        /// <summary>
        /// Initialize configuration directory
        /// </summary>
        private void InitializeConfigDirectory()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
            
            // Create default configuration files if they don't exist
            CreateDefaultConfigurations();
        }
        
        /// <summary>
        /// Create default configuration files
        /// </summary>
        private void CreateDefaultConfigurations()
        {
            CreateDefaultServerConfig();
            CreateDefaultWorldConfig();
            CreateDefaultGameplayConfig();
            CreateDefaultNetworkConfig();
            CreateDefaultPerformanceConfig();
            CreateDefaultSecurityConfig();
            CreateDefaultDatabaseConfig();
            CreateDefaultLoggingConfig();
        }
        
        /// <summary>
        /// Create default server configuration
        /// </summary>
        private void CreateDefaultServerConfig()
        {
            var configPath = Path.Combine(_configDirectory, ServerConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new ServerConfiguration
                {
                    ServerName = "HELLO_MY_WORLD Server",
                    ServerVersion = "1.0.0",
                    MaxPlayers = 100,
                    Port = 8080,
                    BindAddress = "0.0.0.0",
                    EnableWhitelist = false,
                    EnablePvP = true,
                    EnableNether = false,
                    EnableEnd = false,
                    Motd = "Welcome to HELLO_MY_WORLD!",
                    ViewDistance = 10,
                    Difficulty = "normal",
                    GameMode = "survival",
                    EnableCommandBlocks = false,
                    AllowFlight = false,
                    SpawnProtection = true,
                    SpawnRadius = 16,
                    KeepSpawnLoaded = true,
                    EnableRcon = false,
                    RconPort = 25575,
                    RconPassword = ""
                };
                
                SaveConfiguration(ServerConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default world configuration
        /// </summary>
        private void CreateDefaultWorldConfig()
        {
            var configPath = Path.Combine(_configDirectory, WorldConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new WorldConfiguration
                {
                    WorldName = "world",
                    WorldType = "default",
                    Seed = null,
                    GenerateStructures = true,
                    AllowCheats = false,
                    Hardcore = false,
                    WorldBorder = new WorldBorderConfig
                    {
                        Enabled = false,
                        CenterX = 0,
                        CenterZ = 0,
                        Size = 60000000,
                        DamageBuffer = 5.0,
                        WarningTime = 15,
                        WarningDistance = 5
                    },
                    WorldMapControl = new WorldMapControlConfig
                    {
                        ProfileName = "default",
                        TerrainScale = 1.0,
                        TerrainHeightMultiplier = 1.0,
                        TerrainRoughness = 0.5,
                        CaveEnabled = true,
                        CaveDensity = 0.5,
                        RiverEnabled = true,
                        RiverDensity = 0.3,
                        LakeEnabled = true,
                        LakeDensity = 0.2,
                        BiomeTemperatureScale = 0.002,
                        BiomeMoistureScale = 0.003,
                        VegetationDensity = 0.5,
                        TreeDensity = 0.1,
                        GrassDensity = 0.3
                    },
                    Environment = new EnvironmentConfig
                    {
                        DayDuration = 12000,
                        NightDuration = 12000,
                        WeatherCycle = true,
                        ThunderCycle = true,
                        SeaLevel = 64,
                        MaxBuildHeight = 256,
                        MinBuildHeight = -64
                    }
                };
                
                SaveConfiguration(WorldConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default gameplay configuration
        /// </summary>
        private void CreateDefaultGameplayConfig()
        {
            var configPath = Path.Combine(_configDirectory, GameplayConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new GameplayConfiguration
                {
                    PlayerSettings = new PlayerSettingsConfig
                    {
                        MaxHealth = 20,
                        MaxHunger = 20,
                        MaxExperience = 2147483647,
                        RespawnCooldown = 5,
                        KeepInventoryOnDeath = false,
                        KeepExperienceOnDeath = false,
                        EnableSpectatorMode = true,
                        EnableFlying = false,
                        EnableCreativeMode = true
                    },
                    MobSettings = new MobSettingsConfig
                    {
                        EnableMobs = true,
                        EnableHostileMobs = true,
                        EnablePassiveMobs = true,
                        EnableNeutralMobs = true,
                        MobSpawningRate = 1.0,
                        MaxMobsPerChunk = 70,
                        MaxHostileMobsPerChunk = 40,
                        DespawnDistance = 128,
                        PersistentMobs = false
                    },
                    ItemSettings = new ItemSettingsConfig
                    {
                        EnableItemDrops = true,
                        EnableItemDespawning = true,
                        ItemDespawnTime = 6000,
                        MaxItemsPerChunk = 200,
                        EnableEnchanting = true,
                        EnableBrewing = true,
                        EnableAnvil = true,
                        EnableEnchantingTable = true,
                        MaxEnchantmentLevel = 30
                    },
                    BlockSettings = new BlockSettingsConfig
                    {
                        EnableBlockBreaking = true,
                        EnableBlockPlacing = true,
                        EnableRedstone = true,
                        EnablePistons = true,
                        EnableHoppers = true,
                        MaxBlockUpdateDistance = 64,
                        EnableTileEntities = true,
                        EnableCommandBlocks = false
                    },
                    EconomySettings = new EconomySettingsConfig
                    {
                        EnableEconomy = false,
                        StartingBalance = 0,
                        CurrencySymbol = "$",
                        EnablePlayerShops = false,
                        EnableAdminShops = false,
                        TaxRate = 0.0,
                        EnableBanking = false
                    }
                };
                
                SaveConfiguration(GameplayConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default network configuration
        /// </summary>
        private void CreateDefaultNetworkConfig()
        {
            var configPath = Path.Combine(_configDirectory, NetworkConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new NetworkConfiguration
                {
                    ConnectionSettings = new ConnectionSettingsConfig
                    {
                        MaxConnections = 1000,
                        ConnectionTimeout = 30000,
                        KeepAliveInterval = 15000,
                        MaxPacketSize = 2097152,
                        EnableCompression = true,
                        CompressionThreshold = 256,
                        EnableEncryption = true,
                        ProtocolVersion = 757
                    },
                    BandwidthSettings = new BandwidthSettingsConfig
                    {
                        MaxUploadBandwidth = 1048576,
                        MaxDownloadBandwidth = 1048576,
                        EnableThrottling = false,
                        ThrottleThreshold = 10485760,
                        EnableQoS = false
                    },
                    SecuritySettings = new NetworkSecurityConfig
                    {
                        EnableDDoSProtection = true,
                        MaxConnectionsPerIP = 5,
                        ConnectionRateLimit = 10,
                        EnableIPWhitelist = false,
                        EnableIPBlacklist = false,
                        WhitelistIPs = new List<string>(),
                        BlacklistIPs = new List<string>(),
                        EnableProxyDetection = true
                    }
                };
                
                SaveConfiguration(NetworkConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default performance configuration
        /// </summary>
        private void CreateDefaultPerformanceConfig()
        {
            var configPath = Path.Combine(_configDirectory, PerformanceConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new PerformanceConfiguration
                {
                    ChunkSettings = new ChunkPerformanceConfig
                    {
                        MaxLoadedChunks = 10000,
                        ChunkGenerationThreads = 4,
                        ChunkSaveInterval = 600,
                        EnableChunkCompression = true,
                        EnableChunkCaching = true,
                        MaxCachedChunks = 1000,
                        ChunkUnloadDistance = 192,
                        EnableAsyncChunkLoading = true
                    },
                    EntitySettings = new EntityPerformanceConfig
                    {
                        MaxLoadedEntities = 10000,
                        EntityUpdateDistance = 128,
                        EnableEntityCulling = true,
                        EnableLazyEntityLoading = true,
                        MaxEntityUpdatesPerTick = 100,
                        EnableAsyncEntityProcessing = true
                    },
                    MemorySettings = new MemoryPerformanceConfig
                    {
                        MaxMemoryUsage = 4096,
                        EnableMemoryMonitoring = true,
                        GarbageCollectionInterval = 60,
                        EnableMemoryPooling = true,
                        MaxPooledObjects = 10000,
                        MemoryWarningThreshold = 0.8
                    },
                    ThreadSettings = new ThreadPerformanceConfig
                    {
                        WorkerThreads = Environment.ProcessorCount,
                        IoThreads = 4,
                        EnableThreadPool = true,
                        MaxThreadPoolSize = 100,
                        EnableWorkStealing = true,
                        ThreadPriority = "normal"
                    }
                };
                
                SaveConfiguration(PerformanceConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default security configuration
        /// </summary>
        private void CreateDefaultSecurityConfig()
        {
            var configPath = Path.Combine(_configDirectory, SecurityConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new SecurityConfiguration
                {
                    AuthenticationSettings = new AuthenticationConfig
                    {
                        EnableAuthentication = true,
                        RequirePassword = false,
                        MinPasswordLength = 8,
                        EnableTwoFactor = false,
                        SessionTimeout = 3600,
                        MaxLoginAttempts = 5,
                        LockoutDuration = 300,
                        EnableBruteForceProtection = true
                    },
                    PermissionSettings = new PermissionConfig
                    {
                        EnablePermissions = true,
                        DefaultPermissionLevel = "player",
                        PermissionLevels = new List<string>
                        {
                            "player", "moderator", "admin", "owner"
                        },
                        EnableInheritance = true,
                        EnableWildcardPermissions = true
                    },
                    ValidationSettings = new ValidationConfig
                    {
                        EnableInputValidation = true,
                        EnableCommandValidation = true,
                        EnableChatFilter = false,
                        MaxChatLength = 256,
                        MaxCommandLength = 256,
                        BlockedWords = new List<string>(),
                        EnableProfanityFilter = false
                    }
                };
                
                SaveConfiguration(SecurityConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default database configuration
        /// </summary>
        private void CreateDefaultDatabaseConfig()
        {
            var configPath = Path.Combine(_configDirectory, DatabaseConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new DatabaseConfiguration
                {
                    Type = "sqlite",
                    ConnectionString = "Data Source=world.db",
                    EnableConnectionPooling = true,
                    MaxPoolSize = 100,
                    MinPoolSize = 5,
                    ConnectionTimeout = 30,
                    CommandTimeout = 30,
                    EnableMigrations = true,
                    BackupInterval = 3600,
                    BackupRetentionDays = 7,
                    EnableCompression = false,
                    EnableEncryption = false
                };
                
                SaveConfiguration(DatabaseConfigFile, config);
            }
        }
        
        /// <summary>
        /// Create default logging configuration
        /// </summary>
        private void CreateDefaultLoggingConfig()
        {
            var configPath = Path.Combine(_configDirectory, LoggingConfigFile);
            if (!File.Exists(configPath))
            {
                var config = new LoggingConfiguration
                {
                    LogLevel = "info",
                    EnableConsoleLogging = true,
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    LogFileName = "server-{Date}.log",
                    MaxLogFileSize = 10485760,
                    MaxLogFiles = 10,
                    EnableJsonLogging = false,
                    EnableStructuredLogging = false,
                    Loggers = new Dictionary<string, string>
                    {
                        ["GameServer"] = "info",
                        ["Network"] = "debug",
                        ["World"] = "info",
                        ["Database"] = "warn",
                        ["Security"] = "info"
                    }
                };
                
                SaveConfiguration(LoggingConfigFile, config);
            }
        }
        
        /// <summary>
        /// Load all configurations
        /// </summary>
        private void LoadAllConfigurations()
        {
            LoadConfiguration<ServerConfiguration>(ServerConfigFile);
            LoadConfiguration<WorldConfiguration>(WorldConfigFile);
            LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
            LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
            LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
            LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
            LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
            LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
        }
        
        /// <summary>
        /// Load a specific configuration file
        /// </summary>
        private void LoadConfiguration<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    
                    var config = JsonSerializer.Deserialize<T>(json, options);
                    if (config != null)
                    {
                        lock (_configLock)
                        {
                            _configurations[fileName] = config;
                        }
                    }
                }
                else
                {
                    // Create default configuration if file doesn't exist
                    var defaultConfig = new T();
                    SaveConfiguration(fileName, defaultConfig);
                    lock (_configLock)
                    {
                        _configurations[fileName] = defaultConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration {fileName}: {ex.Message}");
                // Load default configuration as fallback
                var defaultConfig = new T();
                lock (_configLock)
                {
                    _configurations[fileName] = defaultConfig;
                }
            }
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        private void SaveConfiguration<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(_configDirectory, fileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Setup file watchers for hot-reloading
        /// </summary>
        private void SetupFileWatchers()
        {
            var configFiles = new[]
            {
                ServerConfigFile, WorldConfigFile, GameplayConfigFile,
                NetworkConfigFile, PerformanceConfigFile, SecurityConfigFile,
                DatabaseConfigFile, LoggingConfigFile
            };
            
            foreach (var configFile in configFiles)
            {
                var watcher = new FileSystemWatcher(_configDirectory, configFile);
                watcher.Changed += (sender, e) => OnConfigurationChanged(configFile);
                watcher.Created += (sender, e) => OnConfigurationChanged(configFile);
                watcher.EnableRaisingEvents = true;
                
                lock (_configLock)
                {
                    _watchers[configFile] = watcher;
                }
            }
        }
        
        /// <summary>
        /// Handle configuration file changes
        /// </summary>
        private void OnConfigurationChanged(string fileName)
        {
            try
            {
                // Debounce rapid file changes
                System.Threading.Thread.Sleep(100);
                
                switch (fileName)
                {
                    case ServerConfigFile:
                        LoadConfiguration<ServerConfiguration>(ServerConfigFile);
                        break;
                    case WorldConfigFile:
                        LoadConfiguration<WorldConfiguration>(WorldConfigFile);
                        break;
                    case GameplayConfigFile:
                        LoadConfiguration<GameplayConfiguration>(GameplayConfigFile);
                        break;
                    case NetworkConfigFile:
                        LoadConfiguration<NetworkConfiguration>(NetworkConfigFile);
                        break;
                    case PerformanceConfigFile:
                        LoadConfiguration<PerformanceConfiguration>(PerformanceConfigFile);
                        break;
                    case SecurityConfigFile:
                        LoadConfiguration<SecurityConfiguration>(SecurityConfigFile);
                        break;
                    case DatabaseConfigFile:
                        LoadConfiguration<DatabaseConfiguration>(DatabaseConfigFile);
                        break;
                    case LoggingConfigFile:
                        LoadConfiguration<LoggingConfiguration>(LoggingConfigFile);
                        break;
                }
                
                Console.WriteLine($"Configuration {fileName} reloaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading configuration {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get configuration by type
        /// </summary>
        public T? GetConfiguration<T>() where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                lock (_configLock)
                {
                    return _configurations.TryGetValue(fileName, out var config) ? config as T : null;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update configuration
        /// </summary>
        public void UpdateConfiguration<T>(T config) where T : class
        {
            var configFiles = new Dictionary<Type, string>
            {
                [typeof(ServerConfiguration)] = ServerConfigFile,
                [typeof(WorldConfiguration)] = WorldConfigFile,
                [typeof(GameplayConfiguration)] = GameplayConfigFile,
                [typeof(NetworkConfiguration)] = NetworkConfigFile,
                [typeof(PerformanceConfiguration)] = PerformanceConfigFile,
                [typeof(SecurityConfiguration)] = SecurityConfigFile,
                [typeof(DatabaseConfiguration)] = DatabaseConfigFile,
                [typeof(LoggingConfiguration)] = LoggingConfigFile
            };
            
            if (configFiles.TryGetValue(typeof(T), out var fileName))
            {
                SaveConfiguration(fileName, config);
                lock (_configLock)
                {
                    _configurations[fileName] = config;
                }
            }
        }
        
        /// <summary>
        /// Validate all configurations
        /// </summary>
        public bool ValidateConfigurations()
        {
            var isValid = true;
            
            var serverConfig = GetConfiguration<ServerConfiguration>();
            if (serverConfig != null)
            {
                if (serverConfig.MaxPlayers <= 0 || serverConfig.MaxPlayers > 1000)
                {
                    Console.WriteLine("Invalid MaxPlayers in server configuration");
                    isValid = false;
                }
                
                if (serverConfig.Port <= 0 || serverConfig.Port > 65535)
                {
                    Console.WriteLine("Invalid Port in server configuration");
                    isValid = false;
                }
            }
            
            var worldConfig = GetConfiguration<WorldConfiguration>();
            if (worldConfig != null)
            {
                if (worldConfig.Environment.SeaLevel < 0 || worldConfig.Environment.SeaLevel > 255)
                {
                    Console.WriteLine("Invalid SeaLevel in world configuration");
                    isValid = false;
                }
                
                if (worldConfig.Environment.MaxBuildHeight < 0 || worldConfig.Environment.MaxBuildHeight > 1024)
                {
                    Console.WriteLine("Invalid MaxBuildHeight in world configuration");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
    }
}
