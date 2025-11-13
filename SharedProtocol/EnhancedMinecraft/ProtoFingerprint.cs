using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EnhancedMinecraftProtocol;
using Google.Protobuf.Reflection;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Computes and validates the EnhancedMinecraft descriptor fingerprint so both the
/// Unity client and dedicated server can detect stale/generated protobuf assets.
/// </summary>
public static class ProtoFingerprint
{
    /// <summary>
    /// SHA-256 fingerprint of the generated EnhancedMinecraft descriptor. Update this
    /// constant whenever the proto assets are regenerated.
    /// </summary>
    public const string DescriptorFingerprint = "79842CF452BEF34C3C21DA9C822B874DD172E26A47C6CB26DEDBC6FE6372B8E7";

    public static void AssertDescriptorFingerprint()
    {
        string current = ComputeFingerprint();
        if (!current.Equals(DescriptorFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"EnhancedMinecraft descriptor fingerprint mismatch. Expected {DescriptorFingerprint} but computed {current}. " +
                "Run protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto and rebuild SharedProtocol.");
        }
    }

    public static string ComputeFingerprint()
    {
        FileDescriptor descriptor = EnhancedMinecraftGameReflection.Descriptor;
        var builder = new StringBuilder();
        builder.Append(descriptor.Package ?? string.Empty);

        foreach (var message in descriptor.MessageTypes.OrderBy(m => m.FullName))
        {
            builder.Append('|').Append(message.FullName);
            foreach (var field in message.Fields.InDeclarationOrder())
            {
                builder.Append('#')
                       .Append(field.FieldNumber)
                       .Append(':')
                       .Append(field.Name)
                       .Append(':')
                       .Append(field.FieldType);
            }
        }

        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
