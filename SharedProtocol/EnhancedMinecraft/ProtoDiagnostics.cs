using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    private const int LogPreviewLimit = 12;

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

    private static void EnsureFingerprint(ProtoReferenceReport report)
    {
        if (!report.DescriptorFingerprint.Equals(report.ComputedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "[Proto] Descriptor fingerprint mismatch detected. Regenerate EnhancedMinecraft protobuf outputs (proto/*.proto -> SharedProtocol/EnhancedMinecraft + Assets/Generated/Protobuf).");
        }
    }

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

    public static void AssertFingerprint()
    {
        var report = BuildReferenceReport();
        EnsureFingerprint(report);
    }

    public static void AssertRegistryClean()
    {
        var report = BuildReferenceReport();
        EnsureFingerprint(report);

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
                              FormatListForLog(report.OptionalUnregistered.Select(item => item.ToString())));
        }

        if (report.UnboundDescriptors.Count > 0)
        {
            Console.WriteLine(
                "[Proto][WARN] Generated EnhancedMinecraft messages are not bound in ProtocolRegistry (this is expected for nested/helper contracts): " +
                FormatListForLog(report.UnboundDescriptors));
        }

        if (report.OrphanedDescriptors.Count > 0)
        {
            throw new InvalidOperationException(
                "[Proto] Generated EnhancedMinecraft messages are not registered: " +
                string.Join(", ", report.OrphanedDescriptors));
        }
    }

    /// <summary>
    /// Validates that generated protobuf C# files are newer than source .proto files and
    /// that required generated files are present.
    /// </summary>
    public static void AssertGeneratedSourceFreshness(
        string protoDirectory,
        string generatedDirectory,
        IEnumerable<string>? requiredGeneratedFiles = null)
    {
        if (string.IsNullOrWhiteSpace(protoDirectory) || !Directory.Exists(protoDirectory))
        {
            throw new InvalidOperationException($"Proto source directory was not found: '{protoDirectory}'.");
        }

        if (string.IsNullOrWhiteSpace(generatedDirectory) || !Directory.Exists(generatedDirectory))
        {
            throw new InvalidOperationException($"Generated protobuf directory was not found: '{generatedDirectory}'.");
        }

        var protoFiles = Directory.GetFiles(protoDirectory, "*.proto", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .ToArray();
        var generatedFiles = Directory.GetFiles(generatedDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .ToArray();

        if (protoFiles.Length == 0)
        {
            throw new InvalidOperationException($"No .proto files were found under '{protoDirectory}'.");
        }

        if (generatedFiles.Length == 0)
        {
            throw new InvalidOperationException($"No generated protobuf C# files were found under '{generatedDirectory}'.");
        }

        DateTime newestProto = protoFiles.Max(file => file.LastWriteTimeUtc);
        DateTime newestGenerated = generatedFiles.Max(file => file.LastWriteTimeUtc);
        if (newestProto > newestGenerated)
        {
            throw new InvalidOperationException(
                $"Generated protobuf DTOs are stale (newest proto: {newestProto:o}, newest generated C#: {newestGenerated:o}).");
        }

        string[] required = (requiredGeneratedFiles ?? new[] { "Common.cs", "EnhancedMinecraftGame.cs", "GameAuth.cs" })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var generatedFileNames = generatedFiles
            .Select(file => file.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] missing = required
            .Where(file => !generatedFileNames.Contains(file))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "Generated protobuf DTOs are missing required files: " + string.Join(", ", missing));
        }
    }

    /// <summary>
    /// Writes the current registry/fingerprint snapshot to disk so CI and manual audits can diff protobuf usage.
    /// </summary>
    public static void WriteReportToFile(string path)
    {
        var report = BuildReferenceReport();
        var coverage = ProtocolRegistry.GetBindingCoverage();
        var missingGeneratedRequiredDescriptors = ProtocolRegistry.GetGeneratedRequiredDescriptorsWithoutBindings()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var payload = new
        {
            report.FileName,
            report.Package,
            report.DescriptorFingerprint,
            report.ComputedFingerprint,
            BoundDescriptorCount = coverage.BoundDescriptors,
            GeneratedDescriptorCount = coverage.GeneratedDescriptors,
            DescriptorCoverageRatio = coverage.GeneratedDescriptors > 0
                ? coverage.BoundDescriptors / (double)coverage.GeneratedDescriptors
                : 1.0,
            DeclaredMessages = report.DeclaredMessages,
            Registered = report.RegisteredMessages
                .Select(entry => new { MessageType = entry.MessageType.ToString(), entry.PrototypeName })
                .ToArray(),
            MissingRegistrations = report.MissingRegistrations.Select(m => m.ToString()).ToArray(),
            UnregisteredMessageTypes = report.UnregisteredMessageTypes.Select(m => m.ToString()).ToArray(),
            OptionalUnregistered = report.OptionalUnregistered.Select(m => m.ToString()).ToArray(),
            MissingGeneratedRequiredDescriptors = missingGeneratedRequiredDescriptors,
            UnboundDescriptors = report.UnboundDescriptors,
            OrphanedDescriptors = report.OrphanedDescriptors
        };

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    public static void LogSummary()
    {
        var report = BuildReferenceReport();
        var coverage = ProtocolRegistry.GetBindingCoverage();
        Console.WriteLine("[Proto] EnhancedMinecraft descriptor: " + report.FileName);
        Console.WriteLine("[Proto] Package: " + report.Package);
        Console.WriteLine("[Proto] Expected fingerprint: " + report.DescriptorFingerprint);
        Console.WriteLine("[Proto] Computed fingerprint: " + report.ComputedFingerprint);
        Console.WriteLine("[Proto] Binding coverage: " + coverage.BoundDescriptors + "/" + coverage.GeneratedDescriptors);
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
                              FormatListForLog(report.OrphanedDescriptors));
        }

        if (report.UnboundDescriptors.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Generated descriptors missing ProtocolRegistry bindings: " +
                              FormatListForLog(report.UnboundDescriptors));
        }

        if (report.UnregisteredMessageTypes.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Enum values missing ProtocolRegistry bindings: " +
                              string.Join(", ", report.UnregisteredMessageTypes));
        }

        if (report.OptionalUnregistered.Count > 0)
        {
            Console.WriteLine("[Proto][WARN] Optional EnhancedMinecraft enums missing ProtocolRegistry bindings: " +
                              FormatListForLog(report.OptionalUnregistered.Select(item => item.ToString())));
        }

        var missingGeneratedRequiredDescriptors = ProtocolRegistry.GetGeneratedRequiredDescriptorsWithoutBindings()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (missingGeneratedRequiredDescriptors.Length > 0)
        {
            Console.WriteLine("[Proto][WARN] Required generated descriptors missing ProtocolRegistry bindings: " +
                              string.Join(", ", missingGeneratedRequiredDescriptors));
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

    private static string FormatListForLog(IEnumerable<string> values)
    {
        string[] filtered = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (filtered.Length <= LogPreviewLimit)
        {
            return string.Join(", ", filtered);
        }

        string preview = string.Join(", ", filtered.Take(LogPreviewLimit));
        return $"{preview}, ... (total={filtered.Length})";
    }
}
