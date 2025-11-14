using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

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
    }
}
