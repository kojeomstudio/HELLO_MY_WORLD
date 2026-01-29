using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameCommon.World;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Data-driven manifest of Minecraft-like features split across core/content/utility and client/server layers.
    /// Serialized as JSON so both Unity and the .NET server can consume the same plan via GameCommon.dll.
    /// </summary>
    public sealed class FeatureManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("generated_at_utc")]
        public string GeneratedAtUtc { get; set; } = string.Empty;

        [JsonPropertyName("features")]
        public List<FeatureManifestEntry> Features { get; set; } = new();

        public IEnumerable<FeatureManifestEntry> GetByCategory(FeatureCategory category) =>
            Features.Where(feature => feature.Category == category);

        public IEnumerable<FeatureManifestEntry> GetByLayer(FeatureLayer layer) =>
            Features.Where(feature => feature.Layer == layer);

        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            foreach (var feature in Features)
            {
                if (string.IsNullOrWhiteSpace(feature.Id))
                {
                    issues.Add("Feature missing id.");
                }

                if (string.IsNullOrWhiteSpace(feature.Name))
                {
                    issues.Add($"Feature '{feature.Id}' missing name.");
                }
            }

            return issues;
        }

        public static FeatureManifest Load(string path)
        {
            var json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<FeatureManifest>(json, JsonOptions)
                           ?? new FeatureManifest();

            if (string.IsNullOrWhiteSpace(manifest.GeneratedAtUtc))
            {
                manifest.GeneratedAtUtc = DateTime.UtcNow.ToString("o");
            }

            return manifest;
        }

        public static FeatureManifest? TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return Load(path);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public sealed class FeatureManifestEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public FeatureCategory Category { get; set; }
            = FeatureCategory.Core;

        [JsonPropertyName("layer")]
        public FeatureLayer Layer { get; set; }
            = FeatureLayer.Shared;

        [JsonPropertyName("side")]
        public string Side { get; set; } = "shared"; // client | server | shared

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }
            = 0;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "planned";

        [JsonPropertyName("artifacts")]
        public string[] Artifacts { get; set; } = Array.Empty<string>();

        [JsonPropertyName("dependencies")]
        public string[] Dependencies { get; set; } = Array.Empty<string>();
    }
}
