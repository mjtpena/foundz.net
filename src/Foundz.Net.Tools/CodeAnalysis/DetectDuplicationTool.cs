using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for detecting duplicated code blocks.
/// </summary>
public class DetectDuplicationTool : ITool
{
    public string Name => "detect_duplication";
    public string Description => "Detect duplicated code blocks in the codebase with similarity analysis.";
    public ToolCategory Category => ToolCategory.CodeAnalysis;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "directoryPath": {
                    "type": "string",
                    "description": "Path to the directory to analyze"
                },
                "minLines": {
                    "type": "integer",
                    "description": "Minimum number of lines to consider as duplication",
                    "default": 6
                },
                "similarityThreshold": {
                    "type": "number",
                    "description": "Similarity threshold (0.0 to 1.0)",
                    "default": 0.8
                },
                "filePattern": {
                    "type": "string",
                    "description": "File pattern to match (e.g., '*.cs')",
                    "default": "*.cs"
                }
            },
            "required": ["directoryPath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("directoryPath") || args["directoryPath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var directoryPath = args["directoryPath"].ToString()!;
            var minLines = args.ContainsKey("minLines") ? Convert.ToInt32(args["minLines"]) : 6;
            var similarityThreshold = args.ContainsKey("similarityThreshold") ? Convert.ToDouble(args["similarityThreshold"]) : 0.8;
            var filePattern = args.ContainsKey("filePattern") ? args["filePattern"].ToString() : "*.cs";

            if (!Directory.Exists(directoryPath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {directoryPath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var files = Directory.GetFiles(directoryPath, filePattern!, SearchOption.AllDirectories);
            var duplications = new List<DuplicationInfo>();

            // Simple token-based duplication detection
            for (int i = 0; i < files.Length; i++)
            {
                var file1Lines = await System.IO.File.ReadAllLinesAsync(files[i], cancellationToken);
                
                for (int j = i + 1; j < files.Length; j++)
                {
                    var file2Lines = await System.IO.File.ReadAllLinesAsync(files[j], cancellationToken);
                    
                    var dupes = FindDuplicateBlocks(file1Lines, file2Lines, minLines, similarityThreshold);
                    
                    foreach (var dupe in dupes)
                    {
                        duplications.Add(new DuplicationInfo
                        {
                            File1 = files[i],
                            File2 = files[j],
                            StartLine1 = dupe.Start1,
                            StartLine2 = dupe.Start2,
                            LineCount = dupe.Length,
                            Similarity = dupe.Similarity
                        });
                    }
                }
            }

            var output = new StringBuilder();
            output.AppendLine($"Duplication Analysis for: {directoryPath}");
            output.AppendLine($"Files analyzed: {files.Length}");
            output.AppendLine($"Minimum lines: {minLines}");
            output.AppendLine($"Similarity threshold: {similarityThreshold:P0}");
            output.AppendLine();

            if (duplications.Any())
            {
                output.AppendLine($"Found {duplications.Count} duplications:");
                
                foreach (var dup in duplications.OrderByDescending(d => d.LineCount).Take(20))
                {
                    output.AppendLine();
                    output.AppendLine($"  {Path.GetFileName(dup.File1)}:{dup.StartLine1} - {Path.GetFileName(dup.File2)}:{dup.StartLine2}");
                    output.AppendLine($"  Lines: {dup.LineCount}, Similarity: {dup.Similarity:P0}");
                }

                if (duplications.Count > 20)
                {
                    output.AppendLine($"\n  ... and {duplications.Count - 20} more duplications");
                }
            }
            else
            {
                output.AppendLine("No duplications found.");
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["filesAnalyzed"] = files.Length,
                    ["duplicationsFound"] = duplications.Count
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private static List<(int Start1, int Start2, int Length, double Similarity)> FindDuplicateBlocks(
        string[] lines1, string[] lines2, int minLines, double threshold)
    {
        var duplicates = new List<(int, int, int, double)>();

        for (int i = 0; i <= lines1.Length - minLines; i++)
        {
            for (int j = 0; j <= lines2.Length - minLines; j++)
            {
                var length = 0;
                var matches = 0;

                while (i + length < lines1.Length && j + length < lines2.Length)
                {
                    var line1 = lines1[i + length].Trim();
                    var line2 = lines2[j + length].Trim();

                    if (string.IsNullOrWhiteSpace(line1) || string.IsNullOrWhiteSpace(line2))
                    {
                        length++;
                        continue;
                    }

                    var similarity = CalculateSimilarity(line1, line2);
                    if (similarity >= threshold)
                    {
                        matches++;
                        length++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (length >= minLines)
                {
                    var avgSimilarity = matches > 0 ? (double)matches / length : 0;
                    duplicates.Add((i + 1, j + 1, length, avgSimilarity));
                }
            }
        }

        return duplicates;
    }

    private static double CalculateSimilarity(string str1, string str2)
    {
        if (str1 == str2) return 1.0;
        
        var len1 = str1.Length;
        var len2 = str2.Length;
        var maxLen = Math.Max(len1, len2);
        
        if (maxLen == 0) return 1.0;
        
        var distance = LevenshteinDistance(str1, str2);
        return 1.0 - ((double)distance / maxLen);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var d = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;
        
        for (int j = 1; j <= s2.Length; j++)
        {
            for (int i = 1; i <= s1.Length; i++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        
        return d[s1.Length, s2.Length];
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "detect_duplication({\"directoryPath\": \"./src\"})",
            "detect_duplication({\"directoryPath\": \"./src\", \"minLines\": 10, \"similarityThreshold\": 0.9})"
        ];
    }

    private record DuplicationInfo
    {
        public required string File1 { get; init; }
        public required string File2 { get; init; }
        public required int StartLine1 { get; init; }
        public required int StartLine2 { get; init; }
        public required int LineCount { get; init; }
        public required double Similarity { get; init; }
    }
}
