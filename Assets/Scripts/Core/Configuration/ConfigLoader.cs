using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace Core.Configuration
{
    /// <summary>
    /// Configuration loader that manages data-driven JSON configurations with hot-reloading support
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        private static ConfigLoader _instance;
        private static readonly Dictionary<string, object> _configs = new Dictionary<string, object>();
        private static readonly Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();
        
        public static ConfigLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ConfigLoader");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ConfigLoader>();
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            LoadAllConfigs();
        }
        
        /// <summary>
        /// Load all configuration files from StreamingAssets/config
        /// </summary>
        public void LoadAllConfigs()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config");
                
                if (!Directory.Exists(configPath))
                {
                    Debug.LogWarning($"Config directory not found: {configPath}");
                    return;
                }
                
                // Load world generation config
                LoadConfig<WorldGenerationConfig>("world_generation.json");
                
                // Load block config
                LoadConfig<BlockConfig>("blocks.json");
                
                // Load item config
                LoadConfig<ItemConfig>("items.json");
                
                // Load entity config
                LoadConfig<EntityConfig>("entities.json");
                
                // Load UI config
                LoadConfig<UIConfig>("ui.json");
                
                // Load audio config
                LoadConfig<AudioConfig>("audio.json");
                
                // Load server config
                LoadConfig<ServerConfig>("server.json");
                
                // Load client config
                LoadConfig<ClientConfig>("client.json");
                
                Debug.Log("All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load configurations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load a specific configuration file and set up file watcher for hot-reloading
        /// </summary>
        private void LoadConfig<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                
                if (!File.Exists(configPath))
                {
                    Debug.LogWarning($"Config file not found: {configPath}");
                    _configs[fileName] = new T();
                    return;
                }
                
                string json = File.ReadAllText(configPath);
                var config = JsonUtility.FromJson<T>(json);
                _configs[fileName] = config;
                
                // Set up file watcher for hot-reloading
                SetupFileWatcher(fileName, configPath);
                
                Debug.Log($"Loaded config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config {fileName}: {ex.Message}");
                _configs[fileName] = new T();
            }
        }
        
        /// <summary>
        /// Set up file system watcher for hot-reloading configuration files
        /// </summary>
        private void SetupFileWatcher(string fileName, string fullPath)
        {
            try
            {
                if (_watchers.ContainsKey(fileName))
                {
                    _watchers[fileName].Dispose();
                }
                
                var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;
                watcher.Changed += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.Created += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.EnableRaisingEvents = true;
                
                _watchers[fileName] = watcher;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set up file watcher for {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle configuration file changes with debouncing
        /// </summary>
        private async void OnConfigChanged(string fileName, string fullPath)
        {
            try
            {
                // Debounce rapid file changes
                await Task.Delay(100);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Config file disappeared during reload: {fullPath}");
                    return;
                }
                
                string json = File.ReadAllText(fullPath);
                var configType = _configs[fileName]?.GetType();
                
                if (configType != null)
                {
                    var config = JsonUtility.FromJson(json, configType);
                    _configs[fileName] = config;
                    
                    Debug.Log($"Hot-reloaded config: {fileName}");
                    
                    // Notify listeners of configuration change
                    OnConfigReloaded?.Invoke(fileName, config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to reload config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a loaded configuration by filename
        /// </summary>
        public T GetConfig<T>(string fileName) where T : class
        {
            if (_configs.TryGetValue(fileName, out var config) && config is T typedConfig)
            {
                return typedConfig;
            }
            
            Debug.LogWarning($"Config not found or wrong type: {fileName}");
            return default(T);
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        public void SaveConfig<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(configPath, json);
                
                _configs[fileName] = config;
                Debug.Log($"Saved config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate all loaded configurations
        /// </summary>
        public bool ValidateConfigs()
        {
            bool allValid = true;
            
            foreach (var kvp in _configs)
            {
                try
                {
                    var validationMethod = kvp.Value.GetType().GetMethod("Validate");
                    if (validationMethod != null)
                    {
                        var result = validationMethod.Invoke(kvp.Value, null);
                        if (result is bool isValid && !isValid)
                        {
                            Debug.LogError($"Configuration validation failed: {kvp.Key}");
                            allValid = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to validate config {kvp.Key}: {ex.Message}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Event fired when a configuration is hot-reloaded
        /// </summary>
        public event Action<string, object> OnConfigReloaded;
        
        private void OnDestroy()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
        
        private void OnApplicationQuit()
        {
            OnDestroy();
        }
    }
    
    /// <summary>
    /// Base class for all configuration types with validation support
    /// </summary>
    public abstract class ConfigBase
    {
        /// <summary>
        /// Validate configuration data
        /// </summary>
        public virtual bool Validate()
        {
            return true; // Base implementation returns true
        }
    }
    
    /// <summary>
    /// Placeholder configuration classes (to be implemented as needed)
    /// </summary>
    [Serializable]
    public class BlockConfig : ConfigBase { }
    
    [Serializable]
    public class ItemConfig : ConfigBase { }
    
    [Serializable]
    public class EntityConfig : ConfigBase { }
    
    [Serializable]
    public class UIConfig : ConfigBase { }
    
    [Serializable]
    public class AudioConfig : ConfigBase { }
    
    [Serializable]
    public class ServerConfig : ConfigBase { }
    
    [Serializable]
    public class ClientConfig : ConfigBase { }
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace Core.Configuration
{
    /// <summary>
    /// Configuration loader that manages data-driven JSON configurations with hot-reloading support
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        private static ConfigLoader _instance;
        private static readonly Dictionary<string, object> _configs = new Dictionary<string, object>();
        private static readonly Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();
        
        public static ConfigLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ConfigLoader");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ConfigLoader>();
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            LoadAllConfigs();
        }
        
        /// <summary>
        /// Load all configuration files from StreamingAssets/config
        /// </summary>
        public void LoadAllConfigs()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config");
                
                if (!Directory.Exists(configPath))
                {
                    Debug.LogWarning($"Config directory not found: {configPath}");
                    return;
                }
                
                // Load world generation config
                LoadConfig<WorldGenerationConfig>("world_generation.json");
                
                // Load block config
                LoadConfig<BlockConfig>("blocks.json");
                
                // Load item config
                LoadConfig<ItemConfig>("items.json");
                
                // Load entity config
                LoadConfig<EntityConfig>("entities.json");
                
                // Load UI config
                LoadConfig<UIConfig>("ui.json");
                
                // Load audio config
                LoadConfig<AudioConfig>("audio.json");
                
                // Load server config
                LoadConfig<ServerConfig>("server.json");
                
                // Load client config
                LoadConfig<ClientConfig>("client.json");
                
                Debug.Log("All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load configurations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load a specific configuration file and set up file watcher for hot-reloading
        /// </summary>
        private void LoadConfig<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                
                if (!File.Exists(configPath))
                {
                    Debug.LogWarning($"Config file not found: {configPath}");
                    _configs[fileName] = new T();
                    return;
                }
                
                string json = File.ReadAllText(configPath);
                var config = JsonUtility.FromJson<T>(json);
                _configs[fileName] = config;
                
                // Set up file watcher for hot-reloading
                SetupFileWatcher(fileName, configPath);
                
                Debug.Log($"Loaded config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config {fileName}: {ex.Message}");
                _configs[fileName] = new T();
            }
        }
        
        /// <summary>
        /// Set up file system watcher for hot-reloading configuration files
        /// </summary>
        private void SetupFileWatcher(string fileName, string fullPath)
        {
            try
            {
                if (_watchers.ContainsKey(fileName))
                {
                    _watchers[fileName].Dispose();
                }
                
                var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;
                watcher.Changed += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.Created += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.EnableRaisingEvents = true;
                
                _watchers[fileName] = watcher;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set up file watcher for {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle configuration file changes with debouncing
        /// </summary>
        private async void OnConfigChanged(string fileName, string fullPath)
        {
            try
            {
                // Debounce rapid file changes
                await Task.Delay(100);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Config file disappeared during reload: {fullPath}");
                    return;
                }
                
                string json = File.ReadAllText(fullPath);
                var configType = _configs[fileName]?.GetType();
                
                if (configType != null)
                {
                    var config = JsonUtility.FromJson(json, configType);
                    _configs[fileName] = config;
                    
                    Debug.Log($"Hot-reloaded config: {fileName}");
                    
                    // Notify listeners of configuration change
                    OnConfigReloaded?.Invoke(fileName, config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to reload config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a loaded configuration by filename
        /// </summary>
        public T GetConfig<T>(string fileName) where T : class
        {
            if (_configs.TryGetValue(fileName, out var config) && config is T typedConfig)
            {
                return typedConfig;
            }
            
            Debug.LogWarning($"Config not found or wrong type: {fileName}");
            return default(T);
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        public void SaveConfig<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(configPath, json);
                
                _configs[fileName] = config;
                Debug.Log($"Saved config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate all loaded configurations
        /// </summary>
        public bool ValidateConfigs()
        {
            bool allValid = true;
            
            foreach (var kvp in _configs)
            {
                try
                {
                    var validationMethod = kvp.Value.GetType().GetMethod("Validate");
                    if (validationMethod != null)
                    {
                        var result = validationMethod.Invoke(kvp.Value, null);
                        if (result is bool isValid && !isValid)
                        {
                            Debug.LogError($"Configuration validation failed: {kvp.Key}");
                            allValid = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to validate config {kvp.Key}: {ex.Message}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Event fired when a configuration is hot-reloaded
        /// </summary>
        public event Action<string, object> OnConfigReloaded;
        
        private void OnDestroy()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
        
        private void OnApplicationQuit()
        {
            OnDestroy();
        }
    }
    
    /// <summary>
    /// Base class for all configuration types with validation support
    /// </summary>
    public abstract class ConfigBase
    {
        /// <summary>
        /// Validate configuration data
        /// </summary>
        public virtual bool Validate()
        {
            return true; // Base implementation returns true
        }
    }
    
    /// <summary>
    /// Placeholder configuration classes (to be implemented as needed)
    /// </summary>
    [Serializable]
    public class BlockConfig : ConfigBase { }
    
    [Serializable]
    public class ItemConfig : ConfigBase { }
    
    [Serializable]
    public class EntityConfig : ConfigBase { }
    
    [Serializable]
    public class UIConfig : ConfigBase { }
    
    [Serializable]
    public class AudioConfig : ConfigBase { }
    
    [Serializable]
    public class ServerConfig : ConfigBase { }
    
    [Serializable]
    public class ClientConfig : ConfigBase { }
}
}
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            LoadAllConfigs();
        }
        
        /// <summary>
        /// Load all configuration files from StreamingAssets/config
        /// </summary>
        public void LoadAllConfigs()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config");
                
                if (!Directory.Exists(configPath))
                {
                    Debug.LogWarning($"Config directory not found: {configPath}");
                    return;
                }
                
                // Load world generation config
                LoadConfig<WorldGenerationConfig>("world_generation.json");
                
                // Load block config
                LoadConfig<BlockConfig>("blocks.json");
                
                // Load item config
                LoadConfig<ItemConfig>("items.json");
                
                // Load entity config
                LoadConfig<EntityConfig>("entities.json");
                
                // Load UI config
                LoadConfig<UIConfig>("ui.json");
                
                // Load audio config
                LoadConfig<AudioConfig>("audio.json");
                
                // Load server config
                LoadConfig<ServerConfig>("server.json");
                
                // Load client config
                LoadConfig<ClientConfig>("client.json");
                
                Debug.Log("All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load configurations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load a specific configuration file and set up file watcher for hot-reloading
        /// </summary>
        private void LoadConfig<T>(string fileName) where T : class, new()
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                
                if (!File.Exists(configPath))
                {
                    Debug.LogWarning($"Config file not found: {configPath}");
                    _configs[fileName] = new T();
                    return;
                }
                
                string json = File.ReadAllText(configPath);
                var config = JsonUtility.FromJson<T>(json);
                _configs[fileName] = config;
                
                // Set up file watcher for hot-reloading
                SetupFileWatcher(fileName, configPath);
                
                Debug.Log($"Loaded config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config {fileName}: {ex.Message}");
                _configs[fileName] = new T();
            }
        }
        
        /// <summary>
        /// Set up file system watcher for hot-reloading configuration files
        /// </summary>
        private void SetupFileWatcher(string fileName, string fullPath)
        {
            try
            {
                if (_watchers.ContainsKey(fileName))
                {
                    _watchers[fileName].Dispose();
                }
                
                var watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
                watcher.NotifyFilter = fileName;
                watcher.Changed += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.Created += (sender, e) => OnConfigChanged(fileName, fullPath);
                watcher.EnableRaisingEvents = true;
                
                _watchers[fileName] = watcher;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set up file watcher for {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle configuration file changes with debouncing
        /// </summary>
        private async void OnConfigChanged(string fileName, string fullPath)
        {
            try
            {
                // Debounce rapid file changes
                await Task.Delay(100);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Config file disappeared during reload: {fullPath}");
                    return;
                }
                
                string json = File.ReadAllText(fullPath);
                var configType = _configs[fileName]?.GetType();
                
                if (configType != null)
                {
                    var config = JsonUtility.FromJson(json, configType);
                    _configs[fileName] = config;
                    
                    Debug.Log($"Hot-reloaded config: {fileName}");
                    
                    // Notify listeners of configuration change
                    OnConfigReloaded?.Invoke(fileName, config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to reload config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a loaded configuration by filename
        /// </summary>
        public T GetConfig<T>(string fileName) where T : class
        {
            if (_configs.TryGetValue(fileName, out var config) && config is T typedConfig)
            {
                return typedConfig;
            }
            
            Debug.LogWarning($"Config not found or wrong type: {fileName}");
            return default(T);
        }
        
        /// <summary>
        /// Save a configuration to file
        /// </summary>
        public void SaveConfig<T>(string fileName, T config) where T : class
        {
            try
            {
                var configPath = Path.Combine(Application.streamingAssetsPath, "config", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(configPath, json);
                
                _configs[fileName] = config;
                Debug.Log($"Saved config: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save config {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validate all loaded configurations
        /// </summary>
        public bool ValidateConfigs()
        {
            bool allValid = true;
            
            foreach (var kvp in _configs)
            {
                try
                {
                    var validationMethod = kvp.Value.GetType().GetMethod("Validate");
                    if (validationMethod != null)
                    {
                        var result = validationMethod.Invoke(kvp.Value, null);
                        if (result is bool isValid && !isValid)
                        {
                            Debug.LogError($"Configuration validation failed: {kvp.Key}");
                            allValid = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to validate config {kvp.Key}: {ex.Message}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Event fired when a configuration is hot-reloaded
        /// </summary>
        public event Action<string, object> OnConfigReloaded;
        
        private void OnDestroy()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
        
        private void OnApplicationQuit()
        {
            OnDestroy();
        }
    }
    
    /// <summary>
    /// Base class for all configuration types with validation support
    /// </summary>
    public abstract class ConfigBase
    {
        /// <summary>
        /// Validate the configuration data
        /// </summary>
        public virtual bool Validate()
        {
            return true; // Base implementation returns true
        }
    }
}
