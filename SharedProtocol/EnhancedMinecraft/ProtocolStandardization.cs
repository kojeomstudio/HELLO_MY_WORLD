using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Standardizes protobuf protocol usage across client and server
    /// Ensures consistent use of Google.Protobuf instead of protobuf-net
    /// </summary>
    public static class ProtocolStandardization
    {
        /// <summary>
        /// Validates that all message types are properly registered and accessible
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();
            
            // Check protocol registry bindings
            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Protocol registry validation failed: {ex.Message}");
            }
            
            // Check that all required message types have handlers
            var requiredMessages = new[]
            {
                MinecraftMessageType.PlayerStateUpdate,
                MinecraftMessageType.PlayerActionRequest,
                MinecraftMessageType.PlayerActionResponse,
                MinecraftMessageType.ChunkDataRequest,
                MinecraftMessageType.ChunkDataResponse,
                MinecraftMessageType.BlockChangeNotification,
                MinecraftMessageType.EntitySpawn,
                MinecraftMessageType.EntityDespawn,
                MinecraftMessageType.EntityUpdate,
                MinecraftMessageType.TimeUpdate,
                MinecraftMessageType.WeatherChange,
                MinecraftMessageType.SoundEffect,
                MinecraftMessageType.ParticleEffect
            };
            
            foreach (var messageType in requiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry");
                }
            }
            
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Protocol implementation validation failed:\n" + 
                    string.Join("\n", issues));
            }
        }
        
        /// <summary>
        /// Creates a standardized message serializer for Google.Protobuf messages
        /// </summary>
        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return message.ToByteArray();
        }
        
        /// <summary>
        /// Creates a standardized message deserializer for Google.Protobuf messages
        /// </summary>
        public static T DeserializeMessage<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return new T();
                
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        
        /// <summary>
        /// Gets the message parser for a specific message type
        /// </summary>
        public static MessageParser<T> GetParser<T>() where T : IMessage, new()
        {
            return new T().Descriptor?.Parser ?? throw new InvalidOperationException($"No parser found for {typeof(T).Name}");
        }
        
        /// <summary>
        /// Validates that a message conforms to the expected protocol structure
        /// </summary>
        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
                return false;
                
            try
            {
                // Check if message has a valid descriptor
                var descriptor = message.Descriptor;
                if (descriptor == null)
                    return false;
                
                // Try to serialize and deserialize to verify integrity
                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the message type ID for a Google.Protobuf message
        /// </summary>
        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
                return null;
                
            var descriptorName = message.Descriptor?.Name;
            if (string.IsNullOrEmpty(descriptorName))
                return null;
                
            // Map descriptor names to message types
            return descriptorName switch
            {
                "PlayerInfo" => MinecraftMessageType.PlayerStateUpdate,
                "PlayerActionRequest" => MinecraftMessageType.PlayerActionRequest,
                "PlayerActionResponse" => MinecraftMessageType.PlayerActionResponse,
                "ChunkLoadRequest" => MinecraftMessageType.ChunkDataRequest,
                "ChunkLoadResponse" => MinecraftMessageType.ChunkDataResponse,
                "BlockChangeBroadcast" => MinecraftMessageType.BlockChangeNotification,
                "EntitySpawnBroadcast" => MinecraftMessageType.EntitySpawn,
                "EntityDespawnBroadcast" => MinecraftMessageType.EntityDespawn,
                "EntityStateUpdate" => MinecraftMessageType.EntityUpdate,
                "TimeUpdateBroadcast" => MinecraftMessageType.TimeUpdate,
                "WeatherUpdateBroadcast" => MinecraftMessageType.WeatherChange,
                "SoundEffect" => MinecraftMessageType.SoundEffect,
                "ParticleEffect" => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }
        
        /// <summary>
        /// Creates a message instance from message type ID
        /// </summary>
        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                return null;
                
            return prototype;
        }
        
        /// <summary>
        /// Standardizes error handling for protocol operations
        /// </summary>
        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }
        
        /// <summary>
        /// Ensures all required protobuf dependencies are available
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                // Check Google.Protobuf availability
                var testMessage = new EnhancedMinecraftProtocol.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parsed = EnhancedMinecraftProtocol.PlayerInfo.Parser.ParseFrom(serialized);
                
                if (parsed == null)
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Standardizes protobuf protocol usage across client and server
    /// Ensures consistent use of Google.Protobuf instead of protobuf-net
    /// </summary>
    public static class ProtocolStandardization
    {
        /// <summary>
        /// Validates that all message types are properly registered and accessible
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();
            
            // Check protocol registry bindings
            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Protocol registry validation failed: {ex.Message}");
            }
            
            // Check that all required message types have handlers
            var requiredMessages = new[]
            {
                MinecraftMessageType.PlayerStateUpdate,
                MinecraftMessageType.PlayerActionRequest,
                MinecraftMessageType.PlayerActionResponse,
                MinecraftMessageType.ChunkDataRequest,
                MinecraftMessageType.ChunkDataResponse,
                MinecraftMessageType.BlockChangeNotification,
                MinecraftMessageType.EntitySpawn,
                MinecraftMessageType.EntityDespawn,
                MinecraftMessageType.EntityUpdate,
                MinecraftMessageType.TimeUpdate,
                MinecraftMessageType.WeatherChange,
                MinecraftMessageType.SoundEffect,
                MinecraftMessageType.ParticleEffect
            };
            
            foreach (var messageType in requiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry");
                }
            }
            
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Protocol implementation validation failed:\n" + 
                    string.Join("\n", issues));
            }
        }
        
        /// <summary>
        /// Creates a standardized message serializer for Google.Protobuf messages
        /// </summary>
        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return message.ToByteArray();
        }
        
        /// <summary>
        /// Creates a standardized message deserializer for Google.Protobuf messages
        /// </summary>
        public static T DeserializeMessage<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return new T();
                
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        
        /// <summary>
        /// Gets the message parser for a specific message type
        /// </summary>
        public static MessageParser<T> GetParser<T>() where T : IMessage, new()
        {
            return new T().Descriptor?.Parser ?? throw new InvalidOperationException($"No parser found for {typeof(T).Name}");
        }
        
        /// <summary>
        /// Validates that a message conforms to the expected protocol structure
        /// </summary>
        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
                return false;
                
            try
            {
                // Check if message has a valid descriptor
                var descriptor = message.Descriptor;
                if (descriptor == null)
                    return false;
                
                // Try to serialize and deserialize to verify integrity
                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the message type ID for a Google.Protobuf message
        /// </summary>
        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
                return null;
                
            var descriptorName = message.Descriptor?.Name;
            if (string.IsNullOrEmpty(descriptorName))
                return null;
                
            // Map descriptor names to message types
            return descriptorName switch
            {
                "PlayerInfo" => MinecraftMessageType.PlayerStateUpdate,
                "PlayerActionRequest" => MinecraftMessageType.PlayerActionRequest,
                "PlayerActionResponse" => MinecraftMessageType.PlayerActionResponse,
                "ChunkLoadRequest" => MinecraftMessageType.ChunkDataRequest,
                "ChunkLoadResponse" => MinecraftMessageType.ChunkDataResponse,
                "BlockChangeBroadcast" => MinecraftMessageType.BlockChangeNotification,
                "EntitySpawnBroadcast" => MinecraftMessageType.EntitySpawn,
                "EntityDespawnBroadcast" => MinecraftMessageType.EntityDespawn,
                "EntityStateUpdate" => MinecraftMessageType.EntityUpdate,
                "TimeUpdateBroadcast" => MinecraftMessageType.TimeUpdate,
                "WeatherUpdateBroadcast" => MinecraftMessageType.WeatherChange,
                "SoundEffect" => MinecraftMessageType.SoundEffect,
                "ParticleEffect" => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }
        
        /// <summary>
        /// Creates a message instance from message type ID
        /// </summary>
        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                return null;
                
            return prototype;
        }
        
        /// <summary>
        /// Standardizes error handling for protocol operations
        /// </summary>
        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }
        
        /// <summary>
        /// Ensures all required protobuf dependencies are available
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                // Check Google.Protobuf availability
                var testMessage = new EnhancedMinecraftProtocol.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parsed = EnhancedMinecraftProtocol.PlayerInfo.Parser.ParseFrom(serialized);
                
                if (parsed == null)
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}
}
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Standardizes protobuf protocol usage across client and server
    /// Ensures consistent use of Google.Protobuf instead of protobuf-net
    /// </summary>
    public static class ProtocolStandardization
    {
        /// <summary>
        /// Validates that all message types are properly registered and accessible
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();
            
            // Check protocol registry bindings
            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Protocol registry validation failed: {ex.Message}");
            }
            
            // Check that all required message types have handlers
            var requiredMessages = new[]
            {
                MinecraftMessageType.PlayerStateUpdate,
                MinecraftMessageType.PlayerActionRequest,
                MinecraftMessageType.PlayerActionResponse,
                MinecraftMessageType.ChunkDataRequest,
                MinecraftMessageType.ChunkDataResponse,
                MinecraftMessageType.BlockChangeNotification,
                MinecraftMessageType.EntitySpawn,
                MinecraftMessageType.EntityDespawn,
                MinecraftMessageType.EntityUpdate,
                MinecraftMessageType.TimeUpdate,
                MinecraftMessageType.WeatherChange,
                MinecraftMessageType.SoundEffect,
                MinecraftMessageType.ParticleEffect
            };
            
            foreach (var messageType in requiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry");
                }
            }
            
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Protocol implementation validation failed:\n" + 
                    string.Join("\n", issues));
            }
        }
        
        /// <summary>
        /// Creates a standardized message serializer for Google.Protobuf messages
        /// </summary>
        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return message.ToByteArray();
        }
        
        /// <summary>
        /// Creates a standardized message deserializer for Google.Protobuf messages
        /// </summary>
        public static T DeserializeMessage<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return new T();
                
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        
        /// <summary>
        /// Gets the message parser for a specific message type
        /// </summary>
        public static MessageParser<T> GetParser<T>() where T : IMessage, new()
        {
            return new T().Descriptor?.Parser ?? throw new InvalidOperationException($"No parser found for {typeof(T).Name}");
        }
        
        /// <summary>
        /// Validates that a message conforms to the expected protocol structure
        /// </summary>
        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
                return false;
                
            try
            {
                // Check if message has a valid descriptor
                var descriptor = message.Descriptor;
                if (descriptor == null)
                    return false;
                
                // Try to serialize and deserialize to verify integrity
                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the message type ID for a Google.Protobuf message
        /// </summary>
        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
                return null;
                
            var descriptorName = message.Descriptor?.Name;
            if (string.IsNullOrEmpty(descriptorName))
                return null;
                
            // Map descriptor names to message types
            return descriptorName switch
            {
                "PlayerInfo" => MinecraftMessageType.PlayerStateUpdate,
                "PlayerActionRequest" => MinecraftMessageType.PlayerActionRequest,
                "PlayerActionResponse" => MinecraftMessageType.PlayerActionResponse,
                "ChunkLoadRequest" => MinecraftMessageType.ChunkDataRequest,
                "ChunkLoadResponse" => MinecraftMessageType.ChunkDataResponse,
                "BlockChangeBroadcast" => MinecraftMessageType.BlockChangeNotification,
                "EntitySpawnBroadcast" => MinecraftMessageType.EntitySpawn,
                "EntityDespawnBroadcast" => MinecraftMessageType.EntityDespawn,
                "EntityStateUpdate" => MinecraftMessageType.EntityUpdate,
                "TimeUpdateBroadcast" => MinecraftMessageType.TimeUpdate,
                "WeatherUpdateBroadcast" => MinecraftMessageType.WeatherChange,
                "SoundEffect" => MinecraftMessageType.SoundEffect,
                "ParticleEffect" => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }
        
        /// <summary>
        /// Creates a message instance from message type ID
        /// </summary>
        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                return null;
                
            return prototype;
        }
        
        /// <summary>
        /// Standardizes error handling for protocol operations
        /// </summary>
        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }
        
        /// <summary>
        /// Ensures all required protobuf dependencies are available
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                // Check Google.Protobuf availability
                var testMessage = new EnhancedMinecraftProtocol.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parsed = EnhancedMinecraftProtocol.PlayerInfo.Parser.ParseFrom(serialized);
                
                if (parsed == null)
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed");
            }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}
}
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Standardizes protobuf protocol usage across client and server
    /// Ensures consistent use of Google.Protobuf instead of protobuf-net
    /// </summary>
    public static class ProtocolStandardization
    {
        /// <summary>
        /// Validates that all message types are properly registered and accessible
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();
            
            // Check protocol registry bindings
            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Protocol registry validation failed: {ex.Message}");
            }
            
            // Check that all required message types have handlers
            var requiredMessages = new[]
            {
                MinecraftMessageType.PlayerStateUpdate,
                MinecraftMessageType.PlayerActionRequest,
                MinecraftMessageType.PlayerActionResponse,
                MinecraftMessageType.ChunkDataRequest,
                MinecraftMessageType.ChunkDataResponse,
                MinecraftMessageType.BlockChangeNotification,
                MinecraftMessageType.EntitySpawn,
                MinecraftMessageType.EntityDespawn,
                MinecraftMessageType.EntityUpdate,
                MinecraftMessageType.TimeUpdate,
                MinecraftMessageType.WeatherChange,
                MinecraftMessageType.SoundEffect,
                MinecraftMessageType.ParticleEffect
            };
            
            foreach (var messageType in requiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry");
                }
            }
            
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Protocol implementation validation failed:\n" + 
                    string.Join("\n", issues));
            }
        }
        
        /// <summary>
        /// Creates a standardized message serializer for Google.Protobuf messages
        /// </summary>
        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return message.ToByteArray();
        }
        
        /// <summary>
        /// Creates a standardized message deserializer for Google.Protobuf messages
        /// </summary>
        public static T DeserializeMessage<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return new T();
                
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        
        /// <summary>
        /// Gets the message parser for a specific message type
        /// </summary>
        public static MessageParser<T> GetParser<T>() where T : IMessage, new()
        {
            return new T().Descriptor?.Parser ?? throw new InvalidOperationException($"No parser found for {typeof(T).Name}");
        }
        
        /// <summary>
        /// Validates that a message conforms to the expected protocol structure
        /// </summary>
        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
                return false;
                
            try
            {
                // Check if message has a valid descriptor
                var descriptor = message.Descriptor;
                if (descriptor == null)
                    return false;
                
                // Try to serialize and deserialize to verify integrity
                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the message type ID for a Google.Protobuf message
        /// </summary>
        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
                return null;
                
            var descriptorName = message.Descriptor?.Name;
            if (string.IsNullOrEmpty(descriptorName))
                return null;
                
            // Map descriptor names to message types
            return descriptorName switch
            {
                "PlayerInfo" => MinecraftMessageType.PlayerStateUpdate,
                "PlayerActionRequest" => MinecraftMessageType.PlayerActionRequest,
                "PlayerActionResponse" => MinecraftMessageType.PlayerActionResponse,
                "ChunkLoadRequest" => MinecraftMessageType.ChunkDataRequest,
                "ChunkLoadResponse" => MinecraftMessageType.ChunkDataResponse,
                "BlockChangeBroadcast" => MinecraftMessageType.BlockChangeNotification,
                "EntitySpawnBroadcast" => MinecraftMessageType.EntitySpawn,
                "EntityDespawnBroadcast" => MinecraftMessageType.EntityDespawn,
                "EntityStateUpdate" => MinecraftMessageType.EntityUpdate,
                "TimeUpdateBroadcast" => MinecraftMessageType.TimeUpdate,
                "WeatherUpdateBroadcast" => MinecraftMessageType.WeatherChange,
                "SoundEffect" => MinecraftMessageType.SoundEffect,
                "ParticleEffect" => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }
        
        /// <summary>
        /// Creates a message instance from message type ID
        /// </summary>
        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                return null;
                
            return prototype;
        }
        
        /// <summary>
        /// Standardizes error handling for protocol operations
        /// </summary>
        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }
        
        /// <summary>
        /// Ensures all required protobuf dependencies are available
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                // Check Google.Protobuf availability
                var testMessage = new EnhancedMinecraftProtocol.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parsed = EnhancedMinecraftProtocol.PlayerInfo.Parser.ParseFrom(serialized);
                
                if (parsed == null)
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Standardizes protobuf protocol usage across client and server
    /// Ensures consistent use of Google.Protobuf instead of protobuf-net
    /// </summary>
    public static class ProtocolStandardization
    {
        /// <summary>
        /// Validates that all message types are properly registered and accessible
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();
            
            // Check protocol registry bindings
            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Protocol registry validation failed: {ex.Message}");
            }
            
            // Check that all required message types have handlers
            var requiredMessages = new[]
            {
                MinecraftMessageType.PlayerStateUpdate,
                MinecraftMessageType.PlayerActionRequest,
                MinecraftMessageType.PlayerActionResponse,
                MinecraftMessageType.ChunkDataRequest,
                MinecraftMessageType.ChunkDataResponse,
                MinecraftMessageType.BlockChangeNotification,
                MinecraftMessageType.EntitySpawn,
                MinecraftMessageType.EntityDespawn,
                MinecraftMessageType.EntityUpdate,
                MinecraftMessageType.TimeUpdate,
                MinecraftMessageType.WeatherChange,
                MinecraftMessageType.SoundEffect,
                MinecraftMessageType.ParticleEffect
            };
            
            foreach (var messageType in requiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry");
                }
            }
            
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Protocol implementation validation failed:\n" + 
                    string.Join("\n", issues));
            }
        }
        
        /// <summary>
        /// Creates a standardized message serializer for Google.Protobuf messages
        /// </summary>
        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
                
            return message.ToByteArray();
        }
        
        /// <summary>
        /// Creates a standardized message deserializer for Google.Protobuf messages
        /// </summary>
        public static T DeserializeMessage<T>(byte[] data) where T : IMessage, new()
        {
            if (data == null || data.Length == 0)
                return new T();
                
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        
        /// <summary>
        /// Gets the message parser for a specific message type
        /// </summary>
        public static MessageParser<T> GetParser<T>() where T : IMessage, new()
        {
            return new T().Descriptor?.Parser ?? throw new InvalidOperationException($"No parser found for {typeof(T).Name}");
        }
        
        /// <summary>
        /// Validates that a message conforms to the expected protocol structure
        /// </summary>
        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
                return false;
                
            try
            {
                // Check if message has a valid descriptor
                var descriptor = message.Descriptor;
                if (descriptor == null)
                    return false;
                
                // Try to serialize and deserialize to verify integrity
                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the message type ID for a Google.Protobuf message
        /// </summary>
        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
                return null;
                
            var descriptorName = message.Descriptor?.Name;
            if (string.IsNullOrEmpty(descriptorName))
                return null;
                
            // Map descriptor names to message types
            return descriptorName switch
            {
                "PlayerInfo" => MinecraftMessageType.PlayerStateUpdate,
                "PlayerActionRequest" => MinecraftMessageType.PlayerActionRequest,
                "PlayerActionResponse" => MinecraftMessageType.PlayerActionResponse,
                "ChunkLoadRequest" => MinecraftMessageType.ChunkDataRequest,
                "ChunkLoadResponse" => MinecraftMessageType.ChunkDataResponse,
                "BlockChangeBroadcast" => MinecraftMessageType.BlockChangeNotification,
                "EntitySpawnBroadcast" => MinecraftMessageType.EntitySpawn,
                "EntityDespawnBroadcast" => MinecraftMessageType.EntityDespawn,
                "EntityStateUpdate" => MinecraftMessageType.EntityUpdate,
                "TimeUpdateBroadcast" => MinecraftMessageType.TimeUpdate,
                "WeatherUpdateBroadcast" => MinecraftMessageType.WeatherChange,
                "SoundEffect" => MinecraftMessageType.SoundEffect,
                "ParticleEffect" => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }
        
        /// <summary>
        /// Creates a message instance from message type ID
        /// </summary>
        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                return null;
                
            return prototype;
        }
        
        /// <summary>
        /// Standardizes error handling for protocol operations
        /// </summary>
        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }
        
        /// <summary>
        /// Ensures all required protobuf dependencies are available
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                // Check Google.Protobuf availability
                var testMessage = new EnhancedMinecraftProtocol.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parsed = EnhancedMinecraftProtocol.PlayerInfo.Parser.ParseFrom(serialized);
                
                if (parsed == null)
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed");
            }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}
}

