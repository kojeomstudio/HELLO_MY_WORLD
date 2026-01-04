using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;
using Proto = EnhancedMinecraftProtocol;

namespace SharedProtocol.EnhancedMinecraft
{
    /// <summary>
    /// Shared helpers that standardize Google.Protobuf usage and registry validation
    /// across the dedicated server and Unity client.
    /// </summary>
    public static class ProtocolStandardization
    {
        private static readonly MinecraftMessageType[] RequiredMessages =
        {
            MinecraftMessageType.PlayerStateUpdate,
            MinecraftMessageType.PlayerActionRequest,
            MinecraftMessageType.PlayerActionResponse,
            MinecraftMessageType.ChunkDataRequest,
            MinecraftMessageType.ChunkDataResponse,
            MinecraftMessageType.ChunkUnloadNotification,
            MinecraftMessageType.ChunkUnloadAcknowledge,
            MinecraftMessageType.BlockChangeNotification,
            MinecraftMessageType.EntitySpawn,
            MinecraftMessageType.EntityDespawn,
            MinecraftMessageType.TimeUpdate,
            MinecraftMessageType.WeatherChange,
            MinecraftMessageType.SoundEffect,
            MinecraftMessageType.ParticleEffect
        };

        /// <summary>
        /// Ensures generated protobuf DTOs, registry bindings, and handler coverage stay aligned.
        /// Throws if any required binding is missing or mismatched.
        /// </summary>
        public static void ValidateProtocolImplementation()
        {
            var issues = new List<string>();

            try
            {
                ProtocolRegistry.ValidateBindings();
            }
            catch (Exception ex)
            {
                issues.Add($"Registry validation failed: {ex.Message}");
            }

            try
            {
                ProtocolValidator.ValidateEnhancedContracts();
            }
            catch (Exception ex)
            {
                issues.Add($"Enhanced contract validation failed: {ex.Message}");
            }

            try
            {
                ValidateParsers();
            }
            catch (Exception ex)
            {
                issues.Add($"Parser validation failed: {ex.Message}");
            }

            try
            {
                ValidateDescriptorCoverage(issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Descriptor coverage check failed: {ex.Message}");
            }

            try
            {
                ValidateDescriptorFileName(issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Descriptor filename validation failed: {ex.Message}");
            }

            try
            {
                ValidateDescriptorOrigin(issues);
            }
            catch (Exception ex)
            {
                issues.Add($"Descriptor origin validation failed: {ex.Message}");
            }

            try
            {
                var runtimeFingerprint = ProtoFingerprint.ComputeFingerprint();
                if (!string.Equals(ProtoFingerprint.DescriptorFingerprint, runtimeFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"Descriptor fingerprint drift detected: manifest={ProtoFingerprint.DescriptorFingerprint} runtime={runtimeFingerprint}. Regenerate EnhancedMinecraft protobuf DTOs or update references.");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Descriptor fingerprint check failed: {ex.Message}");
            }

            foreach (var messageType in RequiredMessages)
            {
                if (!ProtocolRegistry.IsRegistered(messageType))
                {
                    issues.Add($"Required message type {messageType} is not registered in ProtocolRegistry.");
                }
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException("Protocol implementation validation failed:\n" + string.Join("\n", issues));
            }
        }

        private static void ValidateParsers()
        {
            foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
            {
                if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                {
                    throw new InvalidOperationException($"Failed to create prototype for '{messageType}'. Ensure generated classes are referenced by ProtocolRegistry.");
                }

                if (prototype.Descriptor?.Parser == null)
                {
                    throw new InvalidOperationException($"Missing Google.Protobuf parser for '{messageType}' ({prototype.Descriptor?.Name}). Ensure using directives reference the generated DTOs and regenerate protobuf assets if needed.");
                }
            }
        }

        private static void ValidateDescriptorCoverage(ICollection<string> issues)
        {
            var missingDescriptors = ProtocolRegistry.RegisteredMessageTypes
                .Where(type => !ProtocolRegistry.RegisteredDescriptors.Any(binding => binding.MessageType == type))
                .ToArray();

            if (missingDescriptors.Length > 0)
            {
                issues.Add($"Descriptor bindings missing for: {string.Join(", ", missingDescriptors)}. Regenerate EnhancedMinecraft protobuf DTOs or update ProtocolRegistry to keep parsers and descriptors aligned.");
            }
        }

        private static void ValidateDescriptorFileName(ICollection<string> issues)
        {
            string? descriptorName = EnhancedMinecraftGameReflection.Descriptor?.Name;
            const string expectedName = "enhanced_minecraft_game.proto";

            if (string.IsNullOrWhiteSpace(descriptorName))
            {
                issues.Add("EnhancedMinecraft descriptor name is missing; regenerate proto assets.");
                return;
            }

            if (!string.Equals(descriptorName, expectedName, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"EnhancedMinecraft descriptor expected '{expectedName}' but loaded '{descriptorName}'. Ensure using directives point at the generated EnhancedMinecraftGame descriptors and discard stale .proto outputs.");
            }
        }

        private static void ValidateDescriptorOrigin(ICollection<string> issues)
        {
            var expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly.GetName().Name;

            foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
            {
                if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
                {
                    issues.Add($"Failed to create prototype for '{messageType}' during origin validation.");
                    continue;
                }

                var descriptor = prototype.Descriptor;
                if (descriptor == null)
                {
                    issues.Add($"Descriptor missing for '{messageType}' during origin validation.");
                    continue;
                }

                var clrType = descriptor.ClrType;
                var clrAssembly = clrType?.Assembly?.GetName().Name;
                if (!string.IsNullOrWhiteSpace(clrAssembly) && !string.Equals(clrAssembly, expectedAssembly, StringComparison.Ordinal))
                {
                    issues.Add($"Message {messageType} resolved from assembly '{clrAssembly}' instead of '{expectedAssembly}'. Check using directives and regenerate EnhancedMinecraftProtocol DTOs.");
                }

                var namespaceCandidate = descriptor.File?.Package ?? clrType?.Namespace ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(namespaceCandidate) && !namespaceCandidate.StartsWith(nameof(EnhancedMinecraftProtocol), StringComparison.Ordinal))
                {
                    issues.Add($"Message {messageType} uses namespace '{namespaceCandidate}', expected prefix '{nameof(EnhancedMinecraftProtocol)}'. Ensure handlers reference the generated EnhancedMinecraftProtocol classes.");
                }
            }
        }

        public static byte[] SerializeMessage(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.ToByteArray();
        }

        public static T DeserializeMessage<T>(byte[] data) where T : class, IMessage<T>, new()
        {
            if (data == null || data.Length == 0)
            {
                return new T();
            }

            var message = new T();
            message.MergeFrom(data);
            return message;
        }

        public static MessageParser<T> GetParser<T>() where T : class, IMessage<T>, new()
        {
            var staticParser = typeof(T).GetProperty("Parser", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageParser<T>;
            if (staticParser != null)
            {
                return staticParser;
            }

            var descriptorParser = new T().Descriptor?.Parser as MessageParser<T>;
            if (descriptorParser != null)
            {
                return descriptorParser;
            }

            return new MessageParser<T>(() => new T());
        }

        public static bool ValidateMessage(IMessage message)
        {
            if (message == null)
            {
                return false;
            }

            try
            {
                var descriptor = message.Descriptor;
                if (descriptor == null)
                {
                    return false;
                }

                var serialized = message.ToByteArray();
                var parsed = descriptor.Parser.ParseFrom(serialized);
                return parsed != null;
            }
            catch
            {
                return false;
            }
        }

        public static MinecraftMessageType? GetMessageType(IMessage message)
        {
            if (message == null)
            {
                return null;
            }

            var descriptorName = message.Descriptor?.Name;
            return descriptorName switch
            {
                nameof(Proto.PlayerInfo) => MinecraftMessageType.PlayerStateUpdate,
                nameof(Proto.PlayerActionRequest) => MinecraftMessageType.PlayerActionRequest,
                nameof(Proto.PlayerActionResponse) => MinecraftMessageType.PlayerActionResponse,
                nameof(Proto.ChunkLoadRequest) => MinecraftMessageType.ChunkDataRequest,
                nameof(Proto.ChunkLoadResponse) => MinecraftMessageType.ChunkDataResponse,
                nameof(Proto.ChunkUnloadNotification) => MinecraftMessageType.ChunkUnloadNotification,
                nameof(Proto.ChunkUnloadAck) => MinecraftMessageType.ChunkUnloadAcknowledge,
                nameof(Proto.BlockChangeBroadcast) => MinecraftMessageType.BlockChangeNotification,
                nameof(Proto.EntitySpawnBroadcast) => MinecraftMessageType.EntitySpawn,
                nameof(Proto.EntityDespawnBroadcast) => MinecraftMessageType.EntityDespawn,
                nameof(Proto.TimeUpdateBroadcast) => MinecraftMessageType.TimeUpdate,
                nameof(Proto.WeatherUpdateBroadcast) => MinecraftMessageType.WeatherChange,
                nameof(Proto.SoundEffect) => MinecraftMessageType.SoundEffect,
                nameof(Proto.ParticleEffect) => MinecraftMessageType.ParticleEffect,
                _ => null
            };
        }

        public static IMessage? CreateMessage(MinecraftMessageType messageType)
        {
            return ProtocolRegistry.TryCreatePrototype(messageType, out var prototype)
                ? prototype
                : null;
        }

        public static string GetProtocolError(Exception ex, string operation)
        {
            return $"Protocol error during {operation}: {ex.Message}";
        }

        /// <summary>
        /// Quick smoke test to ensure Google.Protobuf dependencies are available.
        /// </summary>
        public static void ValidateDependencies()
        {
            try
            {
                var testMessage = new Proto.PlayerInfo();
                var serialized = testMessage.ToByteArray();
                var parser = typeof(Proto.PlayerInfo).GetProperty("Parser", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageParser<Proto.PlayerInfo>;
                if (parser == null)
                {
                    throw new InvalidOperationException("Generated EnhancedMinecraft parser is missing for PlayerInfo.");
                }

                var parsed = parser.ParseFrom(serialized);

                if (parsed == null)
                {
                    throw new InvalidOperationException("Google.Protobuf basic functionality test failed.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Protocol dependency validation failed: {ex.Message}", ex);
            }
        }
    }
}
