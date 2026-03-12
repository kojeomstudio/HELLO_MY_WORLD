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
            var manifest = TryLoadLegacyFeatureArray(json) ?? TryLoadCategorizedFeatureMap(json) ?? new FeatureManifest();

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

        private static FeatureManifest? TryLoadLegacyFeatureArray(string json)
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<FeatureManifest>(json, JsonOptions);
                if (manifest?.Features?.Count > 0 || !string.IsNullOrWhiteSpace(manifest?.Version))
                {
                    return manifest;
                }
            }
            catch
            {
                // Fallback handled by categorized parser.
            }

            return null;
        }

        private static FeatureManifest? TryLoadCategorizedFeatureMap(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (!TryGetPropertyIgnoreCase(root, "categories", out JsonElement categories) || categories.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var manifest = new FeatureManifest
                {
                    Version = ReadString(root, "version", ReadString(root, "session", string.Empty)),
                    GeneratedAtUtc = ReadString(root, "generated_at_utc", ReadString(root, "generatedAt", string.Empty)),
                    Features = new List<FeatureManifestEntry>()
                };

                int order = 1;
                foreach (JsonProperty categoryNode in categories.EnumerateObject())
                {
                    FeatureCategory category = ParseCategory(categoryNode.Name);
                    if (!TryGetPropertyIgnoreCase(categoryNode.Value, "features", out JsonElement featureArray) || featureArray.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement featureNode in featureArray.EnumerateArray())
                    {
                        string side = ReadString(featureNode, "side", "shared");
                        manifest.Features.Add(new FeatureManifestEntry
                        {
                            Id = ReadString(featureNode, "id", string.Empty),
                            Name = ReadString(featureNode, "name", string.Empty),
                            Category = category,
                            Layer = ParseLayer(side),
                            Side = side,
                            Description = ReadString(featureNode, "description", string.Empty),
                            Order = ReadInt(featureNode, "order", order),
                            Status = ReadString(featureNode, "status", "planned"),
                            Artifacts = ReadStringArray(featureNode, "artifacts"),
                            Dependencies = ReadStringArray(featureNode, "dependencies")
                        });

                        order++;
                    }
                }

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        private static FeatureCategory ParseCategory(string value)
        {
            if (string.Equals(value, "core", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureCategory.Core;
            }

            if (string.Equals(value, "content", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureCategory.Content;
            }

            if (string.Equals(value, "utility", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "util", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureCategory.Utility;
            }

            return FeatureCategory.Core;
        }

        private static FeatureLayer ParseLayer(string side)
        {
            if (string.IsNullOrWhiteSpace(side))
            {
                return FeatureLayer.Shared;
            }

            if (side.Contains("server+client", StringComparison.OrdinalIgnoreCase) ||
                side.Contains("client+server", StringComparison.OrdinalIgnoreCase) ||
                side.Contains("shared", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureLayer.Shared;
            }

            if (side.Contains("server", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureLayer.Server;
            }

            if (side.Contains("client", StringComparison.OrdinalIgnoreCase))
            {
                return FeatureLayer.Client;
            }

            return FeatureLayer.Shared;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string ReadString(JsonElement element, string propertyName, string fallback)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            {
                return fallback;
            }

            return value.GetString() ?? fallback;
        }

        private static int ReadInt(JsonElement element, string propertyName, int fallback)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out JsonElement value))
            {
                return fallback;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static string[] ReadStringArray(JsonElement element, string propertyName)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        list.Add(text);
                    }
                }
            }

            return list.ToArray();
        }
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
