using System;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Ensures the EnhancedMinecraft protobuf contracts are validated exactly once per process.
/// Both the dedicated server and Unity tooling call into this helper so stale generated
/// code is caught before any packets are exchanged.
/// </summary>
public static class ProtoRuntime
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            ProtocolValidator.ValidateEnhancedContracts();
            ProtocolRegistry.ValidateBindings();
            ProtocolRegistry.ValidateTypeConsistency();
            ProtocolStandardization.ValidateProtocolImplementation();
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtoDiagnostics.LogSummary();
            _initialized = true;
        }
    }
}
