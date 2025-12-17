using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Memory;

/// <summary>
/// Indexes codebase files for semantic search and context retrieval.
/// Extracts symbols, dependencies, and metadata for intelligent code understanding.
/// </summary>
public class CodebaseIndexer
{
    private readonly ILogger<CodebaseIndexer> _logger;
    private readonly ConcurrentDictionary<string, FileIndex> _indexCache;
    private readonly HashSet<string> _ignorePatterns;
    private DateTime _lastIndexTime;

    public CodebaseIndexer(ILogger<CodebaseIndexer> logger)
    {
        _logger = logger;
        _indexCache = new ConcurrentDictionary<string, FileIndex>();
        _ignorePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "node_modules", ".git", ".vs", ".vscode", 
            "packages", "dist", "build", "target", ".idea"
        };
        _lastIndexTime = DateTime.MinValue;
    }

    /// <summary>
    /// Index an entire directory recursively
    /// </summary>
    public async Task<CodebaseIndex> IndexDirectoryAsync(
        string rootPath, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting codebase indexing for: {Path}", rootPath);
        var startTime = DateTime.UtcNow;

        var gitignorePatterns = await LoadGitignorePatternsAsync(rootPath, cancellationToken);
        var aiaignorePatterns = await LoadAiaignorePatternsAsync(rootPath, cancellationToken);
        var allIgnorePatterns = _ignorePatterns
            .Union(gitignorePatterns)
            .Union(aiaignorePatterns)
            .ToHashSet();

        var files = await DiscoverFilesAsync(rootPath, allIgnorePatterns, cancellationToken);
        
        _logger.LogInformation("Discovered {Count} files to index", files.Count);

        var indexTasks = files.Select(file => 
            IndexFileAsync(file, cancellationToken)).ToList();
        
        var fileIndexes = await Task.WhenAll(indexTasks);
        
        foreach (var fileIndex in fileIndexes.Where(fi => fi != null))
        {
            _indexCache[fileIndex!.FilePath] = fileIndex;
        }

        var dependencyGraph = BuildDependencyGraph(fileIndexes!);
        var importanceScores = CalculateImportanceScores(fileIndexes!, dependencyGraph);

        var index = new CodebaseIndex
        {
            RootPath = rootPath,
            FileCount = fileIndexes.Length,
            TotalSizeBytes = fileIndexes.Sum(f => f?.SizeBytes ?? 0),
            Files = fileIndexes.Where(f => f != null).ToList()!,
            DependencyGraph = dependencyGraph,
            ImportanceScores = importanceScores,
            IndexedAt = DateTime.UtcNow,
            Duration = DateTime.UtcNow - startTime
        };

        _lastIndexTime = DateTime.UtcNow;
        
        _logger.LogInformation(
            "Indexing complete. {FileCount} files, {SymbolCount} symbols in {Duration}ms",
            index.FileCount,
            fileIndexes.Sum(f => f?.Symbols.Count ?? 0),
            index.Duration.TotalMilliseconds);

        return index;
    }

    /// <summary>
    /// Index a single file
    /// </summary>
    public async Task<FileIndex?> IndexFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {Path}", filePath);
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            var language = DetectLanguage(filePath);
            
            // Skip binary files and very large files (>10MB)
            if (IsBinaryFile(filePath) || fileInfo.Length > 10 * 1024 * 1024)
            {
                _logger.LogDebug("Skipping file: {Path} (binary or too large)", filePath);
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var lines = content.Split('\n').Length;

            var symbols = language == "csharp" 
                ? await ExtractCSharpSymbolsAsync(content, filePath, cancellationToken)
                : ExtractGenericSymbols(content, language);

            var dependencies = ExtractDependencies(content, language);
            var documentation = ExtractDocumentation(content, language);

            return new FileIndex
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Language = language,
                SizeBytes = fileInfo.Length,
                Lines = lines,
                LastModified = fileInfo.LastWriteTimeUtc,
                Symbols = symbols,
                Dependencies = dependencies,
                Documentation = documentation,
                ContentHash = ComputeHash(content)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing file: {Path}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Update index for changed files only (incremental)
    /// </summary>
    public async Task<CodebaseIndex> IncrementalUpdateAsync(
        string rootPath,
        IEnumerable<string> changedFiles,
        CodebaseIndex existingIndex,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing incremental index update for {Count} files", 
            changedFiles.Count());

        var updateTasks = changedFiles.Select(file => 
            IndexFileAsync(file, cancellationToken)).ToList();
        
        var updatedIndexes = await Task.WhenAll(updateTasks);

        // Update cache and existing index
        foreach (var fileIndex in updatedIndexes.Where(fi => fi != null))
        {
            _indexCache[fileIndex!.FilePath] = fileIndex;
            
            var existingFile = existingIndex.Files
                .FirstOrDefault(f => f.FilePath == fileIndex.FilePath);
            
            if (existingFile != null)
            {
                existingIndex.Files.Remove(existingFile);
            }
            existingIndex.Files.Add(fileIndex);
        }

        // Rebuild dependency graph
        existingIndex.DependencyGraph = BuildDependencyGraph(
            existingIndex.Files.ToArray());
        existingIndex.ImportanceScores = CalculateImportanceScores(
            existingIndex.Files.ToArray(), existingIndex.DependencyGraph);

        return existingIndex;
    }

    /// <summary>
    /// Get cached index for a file
    /// </summary>
    public FileIndex? GetCachedFileIndex(string filePath)
    {
        return _indexCache.TryGetValue(filePath, out var index) ? index : null;
    }

    private async Task<List<string>> DiscoverFilesAsync(
        string rootPath, 
        HashSet<string> ignorePatterns,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();
        var queue = new Queue<string>();
        queue.Enqueue(rootPath);

        while (queue.Count > 0)
        {
            var currentDir = queue.Dequeue();
            
            try
            {
                foreach (var dir in Directory.GetDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(dir);
                    if (!ignorePatterns.Contains(dirName))
                    {
                        queue.Enqueue(dir);
                    }
                }

                var extensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".go", 
                    ".rs", ".cpp", ".c", ".h", ".md", ".txt", ".json", ".xml", ".yaml" };
                
                files.AddRange(
                    Directory.GetFiles(currentDir)
                        .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied to directory: {Dir}", currentDir);
            }

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        return files;
    }

    private async Task<HashSet<string>> LoadGitignorePatternsAsync(
        string rootPath, 
        CancellationToken cancellationToken)
    {
        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gitignorePath = Path.Combine(rootPath, ".gitignore");

        if (File.Exists(gitignorePath))
        {
            var lines = await File.ReadAllLinesAsync(gitignorePath, cancellationToken);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                {
                    patterns.Add(trimmed.TrimStart('/').TrimEnd('/'));
                }
            }
        }

        return patterns;
    }

    private async Task<HashSet<string>> LoadAiaignorePatternsAsync(
        string rootPath, 
        CancellationToken cancellationToken)
    {
        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var aiaignorePath = Path.Combine(rootPath, ".aiaignore");

        if (File.Exists(aiaignorePath))
        {
            var lines = await File.ReadAllLinesAsync(aiaignorePath, cancellationToken);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                {
                    patterns.Add(trimmed);
                }
            }
        }

        return patterns;
    }

    private async Task<List<SymbolInfo>> ExtractCSharpSymbolsAsync(
        string content, 
        string filePath, 
        CancellationToken cancellationToken)
    {
        var symbols = new List<SymbolInfo>();

        try
        {
            var tree = CSharpSyntaxTree.ParseText(content, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            // Extract classes
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                symbols.Add(new SymbolInfo
                {
                    Name = classDecl.Identifier.Text,
                    Kind = "class",
                    LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Documentation = ExtractXmlDoc(classDecl)
                });
            }

            // Extract interfaces
            foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                symbols.Add(new SymbolInfo
                {
                    Name = interfaceDecl.Identifier.Text,
                    Kind = "interface",
                    LineNumber = interfaceDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Documentation = ExtractXmlDoc(interfaceDecl)
                });
            }

            // Extract methods
            foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                symbols.Add(new SymbolInfo
                {
                    Name = methodDecl.Identifier.Text,
                    Kind = "method",
                    LineNumber = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Signature = methodDecl.ToString().Split('\n').FirstOrDefault()?.Trim(),
                    Documentation = ExtractXmlDoc(methodDecl)
                });
            }

            // Extract properties
            foreach (var propDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                symbols.Add(new SymbolInfo
                {
                    Name = propDecl.Identifier.Text,
                    Kind = "property",
                    LineNumber = propDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting C# symbols from: {Path}", filePath);
        }

        return symbols;
    }

    private List<SymbolInfo> ExtractGenericSymbols(string content, string language)
    {
        var symbols = new List<SymbolInfo>();
        
        // Basic regex patterns for common languages
        var patterns = language switch
        {
            "javascript" or "typescript" => new[]
            {
                @"(?:export\s+)?(?:async\s+)?function\s+(\w+)",
                @"(?:export\s+)?class\s+(\w+)",
                @"(?:export\s+)?const\s+(\w+)\s*="
            },
            "python" => new[]
            {
                @"def\s+(\w+)",
                @"class\s+(\w+)"
            },
            _ => Array.Empty<string>()
        };

        int lineNumber = 1;
        foreach (var line in content.Split('\n'))
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    symbols.Add(new SymbolInfo
                    {
                        Name = match.Groups[1].Value,
                        Kind = "symbol",
                        LineNumber = lineNumber
                    });
                }
            }
            lineNumber++;
        }

        return symbols;
    }

    private string? ExtractXmlDoc(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                       t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .FirstOrDefault();

        if (trivia == default)
            return null;

        var text = trivia.ToString();
        // Extract summary from XML doc
        var summaryMatch = Regex.Match(text, @"<summary>(.*?)</summary>", 
            RegexOptions.Singleline);
        
        return summaryMatch.Success 
            ? summaryMatch.Groups[1].Value.Trim() 
            : text;
    }

    private List<string> ExtractDependencies(string content, string language)
    {
        var dependencies = new List<string>();

        var patterns = language switch
        {
            "csharp" => new[] { @"using\s+([\w\.]+);" },
            "javascript" or "typescript" => new[] 
            { 
                @"import\s+.*\s+from\s+['""](.*?)['""]",
                @"require\s*\(\s*['""](.*?)['""]\s*\)"
            },
            "python" => new[] 
            { 
                @"import\s+([\w\.]+)",
                @"from\s+([\w\.]+)\s+import"
            },
            _ => Array.Empty<string>()
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(content, pattern);
            dependencies.AddRange(matches.Select(m => m.Groups[1].Value));
        }

        return dependencies.Distinct().ToList();
    }

    private string? ExtractDocumentation(string content, string language)
    {
        // Extract file-level documentation
        var lines = content.Split('\n').Take(20); // First 20 lines
        var docLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("///") || trimmed.StartsWith("//") || 
                trimmed.StartsWith("#") || trimmed.StartsWith("/*"))
            {
                docLines.Add(trimmed);
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                break; // Stop at first non-comment line
            }
        }

        return docLines.Any() ? string.Join("\n", docLines) : null;
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(FileIndex[] files)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var file in files)
        {
            if (file == null) continue;

            graph[file.FilePath] = new List<string>();

            foreach (var dep in file.Dependencies)
            {
                // Find files that match this dependency
                var dependentFiles = files.Where(f => 
                    f != null && 
                    (f.FileName.Contains(dep, StringComparison.OrdinalIgnoreCase) ||
                     f.Symbols.Any(s => s.Name.Contains(dep, StringComparison.OrdinalIgnoreCase))))
                    .Select(f => f!.FilePath);

                graph[file.FilePath].AddRange(dependentFiles);
            }
        }

        return graph;
    }

    private Dictionary<string, double> CalculateImportanceScores(
        FileIndex[] files, 
        Dictionary<string, List<string>> dependencyGraph)
    {
        var scores = new Dictionary<string, double>();

        foreach (var file in files)
        {
            if (file == null) continue;

            double score = 0.0;

            // Factor 1: Number of dependents (PageRank-like)
            var dependentCount = dependencyGraph.Values
                .Count(deps => deps.Contains(file.FilePath));
            score += dependentCount * 2.0;

            // Factor 2: Number of symbols (complexity)
            score += file.Symbols.Count * 0.5;

            // Factor 3: Recency (files modified recently are more important)
            var daysSinceModified = (DateTime.UtcNow - file.LastModified).TotalDays;
            score += Math.Max(0, 30 - daysSinceModified) * 0.1;

            // Factor 4: File type (source files more important than tests/docs)
            if (file.FilePath.Contains("test", StringComparison.OrdinalIgnoreCase))
                score *= 0.5;
            else if (file.Language == "csharp" || file.Language == "typescript")
                score *= 1.2;

            // Factor 5: Documentation presence
            if (!string.IsNullOrEmpty(file.Documentation))
                score += 1.0;

            scores[file.FilePath] = score;
        }

        // Normalize scores to 0-100 range
        if (scores.Any())
        {
            var maxScore = scores.Values.Max();
            if (maxScore > 0)
            {
                foreach (var key in scores.Keys.ToList())
                {
                    scores[key] = (scores[key] / maxScore) * 100.0;
                }
            }
        }

        return scores;
    }

    private string DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".go" => "go",
            ".rs" => "rust",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".c" => "c",
            ".h" or ".hpp" => "header",
            ".md" => "markdown",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            _ => "unknown"
        };
    }

    private bool IsBinaryFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var binaryExtensions = new[] 
        { 
            ".dll", ".exe", ".so", ".dylib", ".bin", ".dat", 
            ".jpg", ".png", ".gif", ".ico", ".pdf", ".zip", 
            ".tar", ".gz", ".7z", ".rar", ".pdb"
        };
        
        return binaryExtensions.Contains(extension);
    }

    private string ComputeHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Represents an indexed file with symbols and metadata
/// </summary>
public class FileIndex
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int Lines { get; set; }
    public DateTime LastModified { get; set; }
    public List<SymbolInfo> Symbols { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string? Documentation { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code symbol (class, method, function, etc.)
/// </summary>
public class SymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string? Signature { get; set; }
    public string? Documentation { get; set; }
}

/// <summary>
/// Complete codebase index
/// </summary>
public class CodebaseIndex
{
    public string RootPath { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public List<FileIndex> Files { get; set; } = new();
    public Dictionary<string, List<string>> DependencyGraph { get; set; } = new();
    public Dictionary<string, double> ImportanceScores { get; set; } = new();
    public DateTime IndexedAt { get; set; }
    public TimeSpan Duration { get; set; }
}
