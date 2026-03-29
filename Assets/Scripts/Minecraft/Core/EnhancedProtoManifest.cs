using SharedProtocol.EnhancedMinecraft;

namespace EnhancedMinecraftProtocol.Manifest
{
    /// <summary>
    /// Shared fingerprint + validation helpers that ensure server and Unity
    /// are compiled against the same EnhancedMinecraft protobuf descriptor.
    /// </summary>
    public static class EnhancedProtoManifest
    {
        /// <summary>
        /// SHA-256 fingerprint of the generated EnhancedMinecraft descriptor.
        /// Update this value whenever proto assets are regenerated.
        /// </summary>
        public const string DescriptorFingerprint = ProtoFingerprint.DescriptorFingerprint;

        public static string ComputeFingerprint() => ProtoFingerprint.ComputeFingerprint();

        public static void AssertFingerprint() => ProtoFingerprint.AssertDescriptorFingerprint();
    }
}
