using System.Text.Json;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Regex DatasetHeadingRegex =
        new(@"^##\s+dataset\s*:\s*(?<name>.+?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static int Main(string[] args)
    {
        if (!TryParseArgs(args, out var inputPath, out var outputDirectory))
        {
            PrintUsage();
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input template not found: {inputPath}");
            return 1;
        }

        string markdown = File.ReadAllText(inputPath);
        IReadOnlyList<DatasetBlock> datasets;
        try
        {
            datasets = ParseDatasets(markdown);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse template: {ex.Message}");
            return 1;
        }

        if (datasets.Count == 0)
        {
            Console.Error.WriteLine("No datasets found. Add headings in the format '## dataset: <name>' and JSON code blocks.");
            return 1;
        }

        Directory.CreateDirectory(outputDirectory);

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataset in datasets)
        {
            if (!seenNames.Add(dataset.Name))
            {
                Console.Error.WriteLine($"Duplicate dataset name detected: {dataset.Name}");
                return 1;
            }

            if (!TryNormalizeJson(dataset.JsonPayload, out string normalizedJson, out string error))
            {
                Console.Error.WriteLine($"Invalid JSON in dataset '{dataset.Name}': {error}");
                return 1;
            }

            string safeName = SanitizeFileName(dataset.Name);
            string outputPath = Path.Combine(outputDirectory, $"{safeName}.json");
            File.WriteAllText(outputPath, normalizedJson + Environment.NewLine);
            Console.WriteLine($"Wrote {outputPath}");
        }

        Console.WriteLine($"Export complete. Dataset count: {datasets.Count}");
        return 0;
    }

    private static bool TryParseArgs(string[] args, out string inputPath, out string outputDirectory)
    {
        inputPath = string.Empty;
        outputDirectory = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];
            if (string.Equals(current, "--input", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                inputPath = args[++i];
                continue;
            }

            if (string.Equals(current, "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                outputDirectory = args[++i];
                continue;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return false;
        }

        inputPath = Path.GetFullPath(inputPath);
        outputDirectory = Path.GetFullPath(outputDirectory);
        return true;
    }

    private static IReadOnlyList<DatasetBlock> ParseDatasets(string markdown)
    {
        var results = new List<DatasetBlock>();
        string[] lines = markdown.Replace("\r\n", "\n").Split('\n');

        string? pendingDatasetName = null;
        bool inJsonFence = false;
        var jsonLines = new List<string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();

            if (!inJsonFence)
            {
                Match match = DatasetHeadingRegex.Match(line.Trim());
                if (match.Success)
                {
                    pendingDatasetName = match.Groups["name"].Value.Trim();
                    continue;
                }

                if (pendingDatasetName != null && line.TrimStart().StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    inJsonFence = true;
                    jsonLines.Clear();
                    continue;
                }

                continue;
            }

            if (line.Trim().Equals("```", StringComparison.Ordinal))
            {
                if (pendingDatasetName == null)
                {
                    throw new InvalidOperationException("JSON code block found without a preceding dataset heading.");
                }

                string payload = string.Join(Environment.NewLine, jsonLines).Trim();
                if (string.IsNullOrWhiteSpace(payload))
                {
                    throw new InvalidOperationException($"Dataset '{pendingDatasetName}' has an empty JSON block.");
                }

                results.Add(new DatasetBlock(pendingDatasetName, payload));
                pendingDatasetName = null;
                inJsonFence = false;
                jsonLines.Clear();
                continue;
            }

            jsonLines.Add(rawLine);
        }

        if (inJsonFence)
        {
            throw new InvalidOperationException("Unterminated JSON code fence detected.");
        }

        return results;
    }

    private static bool TryNormalizeJson(string json, out string normalizedJson, out string error)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            normalizedJson = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            normalizedJson = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static string SanitizeFileName(string value)
    {
        string sanitized = value.Trim();
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '_');
        }

        sanitized = sanitized.Replace(' ', '_');
        return string.IsNullOrWhiteSpace(sanitized) ? "dataset" : sanitized;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project Tools/GameDataTemplateExporter/GameDataTemplateExporter.csproj -- --input <template.md> --output <directory>");
    }

    private sealed record DatasetBlock(string Name, string JsonPayload);
}

