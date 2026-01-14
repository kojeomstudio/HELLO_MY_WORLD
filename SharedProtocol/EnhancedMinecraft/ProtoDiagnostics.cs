using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using SharedProtocol;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Generates a lightweight report describing how the generated EnhancedMinecraft protobuf
/// contracts are referenced at runtime. The report is used by the server (and optionally
/// tooling) to quickly spot stale regeneration runs or missing registry entries.
/// </summary>
public static class ProtoDiagnostics
{

    public sealed record ProtoReferenceReport(
        string FileName,
        string Package,
        string DescriptorFingerprint,
        string ComputedFingerprint,
        IReadOnlyList<string> DeclaredMessages,
        IReadOnlyList<(MinecraftMessageType MessageType, string PrototypeName)> RegisteredMessages,
        IReadOnlyList<MinecraftMessageType> MissingRegistrations,
        IReadOnlyList<MinecraftMessageType> UnregisteredMessageTypes,
        IReadOnlyList<MinecraftMessageType> OptionalUnregistered,
        IReadOnlyList<string> UnboundDescriptors,
        IReadOnlyList<string> OrphanedDescriptors);

    public static ProtoReferenceReport BuildReferenceReport()
    {
        var descriptor = EnhancedMinecraftGameReflection.Descriptor;
        var declaredMessages = descriptor.MessageTypes.Select(d => d.Name).ToArray();

        var registeredMessages = ProtocolRegistry.RegisteredMessageTypes
            .Select(mt =>
            {
                if (ProtocolRegistry.TryCreatePrototype(mt, out IMessage? prototype))
                {
                    return (MessageType: mt, PrototypeName: prototype.Descriptor?.Name ?? string.Empty);
                }

                return (MessageType: mt, PrototypeName: string.Empty);
            })
            .ToArray();

        var missing = registeredMessages
            .Where(entry => string.IsNullOrWhiteSpace(entry.PrototypeName))
            .Select(entry => entry.MessageType)
            .ToArray();

        var tracked = ProtocolRegistry.RegisteredDescriptors.ToArray();
        var orphaned = tracked
            .Select(binding => binding.DescriptorName)
            .Where(name => !declaredMessages.Contains(name, StringComparer.Ordinal))
            .ToArray();

        var unbound = declaredMessages
            .Where(name => tracked.All(binding => !string.Equals(binding.DescriptorName, name, StringComparison.Ordinal)))
            .ToArray();

        var allMessageTypes = Enum.GetValues(typeof(MinecraftMessageType)).Cast<MinecraftMessageType>();
        var optionalMessages = ProtocolValidator.GetOptionalMessages().ToArray();
        var unregistered = allMessageTypes
            .Where(type => !ProtocolRegistry.IsRegistered(type) && !ProtocolValidator.IsOptionalMessage(type))
            .ToArray();

        var optionalUnregistered = optionalMessages
            .Where(type => !ProtocolRegistry.IsRegistered(type))
            .ToArray();

        string baselineFingerprint = ProtoFingerprint.DescriptorFingerprint;
        string computedFingerprint = ProtoFingerprint.ComputeFingerprint();

        return new ProtoReferenceReport(
            descriptor.Name ?? string.Empty,
            descriptor.Package ?? string.Empty,
            baselineFingerprint,
            computedFingerprint,
            declaredMessages,
            registeredMessages,
            missing,
            unregistered,
            optionalUnregistered,
            unbound,
            orphaned);
    }

    public static void AssertRegistryClean()
    {
        var report = BuildReferenceReport();

        if (report.MissingRegistrations.Count > 0)
        {
            throw new InvalidOperationException(
                "[Proto] Missing EnhancedMinecraft registrations: " +
                string.Join(", ", report.MissingRegistrations));
        }

        if (report.UnregisteredMessageTypes.Count > 0)
        {
            throw new InvalidOperationException(
                "[Proto] Enum values missing ProtocolRegistry bindings: " +
                string.Join(", ", report.UnregisteredMessageTypes));
        }

        if (report.OptionalUnregistered.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Optional EnhancedMinecraft enums missing ProtocolRegistry bindings: " +
                              string.Join(", ", report.OptionalUnregistered));
        }

        if (report.UnboundDescriptors.Count > 0)
        {
            Console.WriteLine(
                "[Proto][WARN] Generated EnhancedMinecraft messages are not bound in ProtocolRegistry (this is expected for nested/helper contracts): " +
                string.Join(", ", report.UnboundDescriptors));
        }

        if (report.OrphanedDescriptors.Count > 0)
        {
            throw new InvalidOperationException(
                "[Proto] Generated EnhancedMinecraft messages are not registered: " +
                string.Join(", ", report.OrphanedDescriptors));
        }
    }

    public static void LogSummary()
    {
        var report = BuildReferenceReport();
        Console.WriteLine("[Proto] EnhancedMinecraft descriptor: " + report.FileName);
        Console.WriteLine("[Proto] Package: " + report.Package);
        Console.WriteLine("[Proto] Expected fingerprint: " + report.DescriptorFingerprint);
        Console.WriteLine("[Proto] Computed fingerprint: " + report.ComputedFingerprint);
        if (!report.DescriptorFingerprint.Equals(report.ComputedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[Proto][WARN] Descriptor fingerprint mismatch detected.");
        }
        Console.WriteLine("[Proto] Declared messages: " + string.Join(", ", report.DeclaredMessages));

        foreach (var (messageType, prototypeName) in report.RegisteredMessages)
        {
            var resolvedName = string.IsNullOrWhiteSpace(prototypeName) ? "<unresolved>" : prototypeName;
            Console.WriteLine($"[Proto] {messageType} -> {resolvedName}");
        }

        if (report.MissingRegistrations.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Missing EnhancedMinecraft registrations: " +
                              string.Join(", ", report.MissingRegistrations));
        }

        if (report.OrphanedDescriptors.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Generated messages not wired into the registry: " +
                              string.Join(", ", report.OrphanedDescriptors));
        }

        if (report.UnboundDescriptors.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Generated descriptors missing ProtocolRegistry bindings: " +
                              string.Join(", ", report.UnboundDescriptors));
        }

        if (report.UnregisteredMessageTypes.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Enum values missing ProtocolRegistry bindings: " +
                              string.Join(", ", report.UnregisteredMessageTypes));
        }

        if (report.OptionalUnregistered.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Optional EnhancedMinecraft enums missing ProtocolRegistry bindings: " +
                              string.Join(", ", report.OptionalUnregistered));
        }
    }

    public static void LogHandlerCoverage(MinecraftMessageDispatcher dispatcher)
    {
        if (dispatcher == null)
        {
            Console.WriteLine("[Proto][WARN] Minecraft dispatcher is null; skipping handler coverage report.");
            return;
        }

        var missing = dispatcher.GetUnboundProtocolMessages();
        if (missing.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Missing EnhancedMinecraft handlers: " + string.Join(", ", missing));
            return;
        }

        Console.WriteLine("[Proto] EnhancedMinecraft handlers cover all registered messages.");
    }

    public static void LogMissingBinding(MinecraftMessageType messageType)
    {
        Console.WriteLine($"[Proto][WARN] ProtocolRegistry is missing a binding for {messageType}. " +
                          "Regenerate EnhancedMinecraft DTOs or update ProtocolRegistry to wire the generated message.");
    }
}
