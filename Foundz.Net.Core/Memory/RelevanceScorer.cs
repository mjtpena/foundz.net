using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Memory;

/// <summary>
/// Scores files for relevance to a given query using multiple factors.
/// Implements TF-IDF, recency, dependency proximity, and user interaction tracking.
/// </summary>
public class RelevanceScorer
{
    private readonly ILogger<RelevanceScorer> _logger;
    private readonly Dictionary<string, int> _userInteractionCounts;
    private readonly Dictionary<string, DateTime> _lastAccessTimes;

    public RelevanceScorer(ILogger<RelevanceScorer> logger)
    {
        _logger = logger;
        _userInteractionCounts = new Dictionary<string, int>();
        _lastAccessTimes = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Score files by relevance to a query
    /// </summary>
    public List<ScoredFile> ScoreFiles(
        string query,
        CodebaseIndex index,
        int topN = 10)
    {
        _logger.LogDebug("Scoring {Count} files for query: {Query}", index.Files.Count, query);

        var scores = new List<ScoredFile>();
        var queryTerms = ExtractTerms(query);

        foreach (var file in index.Files)
        {
            var score = CalculateRelevanceScore(file, queryTerms, index);
            
            scores.Add(new ScoredFile
            {
                File = file,
                Score = score,
                Factors = new RelevanceFactors
                {
                    KeywordScore = CalculateKeywordScore(file, queryTerms),
                    RecencyScore = CalculateRecencyScore(file),
                    DependencyScore = CalculateDependencyScore(file, index),
                    ImportanceScore = GetImportanceScore(file, index),
                    InteractionScore = GetInteractionScore(file.FilePath)
                }
            });
        }

        var topFiles = scores
            .OrderByDescending(s => s.Score)
            .Take(topN)
            .ToList();

        _logger.LogDebug("Top {Count} files selected with scores: {Scores}",
            topFiles.Count,
            string.Join(", ", topFiles.Select(f => $"{Path.GetFileName(f.File.FileName)}:{f.Score:F2}")));

        return topFiles;
    }

    /// <summary>
    /// Find files semantically related to a given file
    /// </summary>
    public List<ScoredFile> FindRelatedFiles(
        string filePath,
        CodebaseIndex index,
        int topN = 5)
    {
        var targetFile = index.Files.FirstOrDefault(f => f.FilePath == filePath);
        if (targetFile == null)
        {
            _logger.LogWarning("File not found in index: {Path}", filePath);
            return new List<ScoredFile>();
        }

        var scores = new List<ScoredFile>();

        foreach (var file in index.Files.Where(f => f.FilePath != filePath))
        {
            var score = CalculateSimilarityScore(targetFile, file, index);
            
            scores.Add(new ScoredFile
            {
                File = file,
                Score = score,
                Factors = new RelevanceFactors
                {
                    DependencyScore = CalculateDependencyScore(file, index, targetFile),
                    ImportanceScore = GetImportanceScore(file, index)
                }
            });
        }

        return scores
            .OrderByDescending(s => s.Score)
            .Take(topN)
            .ToList();
    }

    /// <summary>
    /// Track user interaction with a file (increases relevance for future queries)
    /// </summary>
    public void TrackInteraction(string filePath)
    {
        _userInteractionCounts[filePath] = 
            _userInteractionCounts.GetValueOrDefault(filePath, 0) + 1;
        _lastAccessTimes[filePath] = DateTime.UtcNow;

        _logger.LogTrace("Tracked interaction with: {Path} (count: {Count})",
            filePath, _userInteractionCounts[filePath]);
    }

    /// <summary>
    /// Get files recently accessed by user
    /// </summary>
    public List<string> GetRecentlyAccessedFiles(int count = 10)
    {
        return _lastAccessTimes
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Clear interaction history
    /// </summary>
    public void ClearHistory()
    {
        _userInteractionCounts.Clear();
        _lastAccessTimes.Clear();
        _logger.LogInformation("Cleared interaction history");
    }

    private double CalculateRelevanceScore(
        FileIndex file,
        List<string> queryTerms,
        CodebaseIndex index)
    {
        var keywordScore = CalculateKeywordScore(file, queryTerms) * 3.0;
        var recencyScore = CalculateRecencyScore(file) * 1.0;
        var dependencyScore = CalculateDependencyScore(file, index) * 1.5;
        var importanceScore = GetImportanceScore(file, index) * 0.5;
        var interactionScore = GetInteractionScore(file.FilePath) * 2.0;

        var totalScore = keywordScore + recencyScore + dependencyScore + 
                        importanceScore + interactionScore;

        return totalScore;
    }

    private double CalculateKeywordScore(FileIndex file, List<string> queryTerms)
    {
        if (queryTerms.Count == 0)
            return 0.0;

        var score = 0.0;
        var fileText = $"{file.FileName} {file.Documentation} " +
                      string.Join(" ", file.Symbols.Select(s => s.Name));

        foreach (var term in queryTerms)
        {
            // Exact match in filename (highest weight)
            if (file.FileName.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10.0;

            // Match in symbol names (high weight)
            var symbolMatches = file.Symbols.Count(s => 
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            score += symbolMatches * 5.0;

            // Match in documentation (medium weight)
            if (file.Documentation?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                score += 2.0;

            // TF-IDF style scoring for full text
            var termFrequency = Regex.Matches(
                fileText, 
                $@"\b{Regex.Escape(term)}\b", 
                RegexOptions.IgnoreCase).Count;
            
            score += Math.Log(1 + termFrequency);
        }

        return score;
    }

    private double CalculateRecencyScore(FileIndex file)
    {
        var daysSinceModified = (DateTime.UtcNow - file.LastModified).TotalDays;
        
        // Files modified within last 7 days get full score
        if (daysSinceModified <= 7)
            return 10.0;
        
        // Linear decay over 30 days
        if (daysSinceModified <= 30)
            return 10.0 * (1.0 - (daysSinceModified - 7.0) / 23.0);
        
        // Very old files get minimal score
        return 1.0;
    }

    private double CalculateDependencyScore(
        FileIndex file, 
        CodebaseIndex index,
        FileIndex? relativeToFile = null)
    {
        var score = 0.0;

        if (index.DependencyGraph.TryGetValue(file.FilePath, out var dependencies))
        {
            // Score based on number of dependencies (well-connected files)
            score += Math.Min(dependencies.Count, 10) * 0.5;

            // If scoring relative to specific file, check if there's a connection
            if (relativeToFile != null)
            {
                if (dependencies.Contains(relativeToFile.FilePath))
                    score += 20.0; // Direct dependency

                // Check for shared dependencies
                if (index.DependencyGraph.TryGetValue(relativeToFile.FilePath, out var relDeps))
                {
                    var sharedDeps = dependencies.Intersect(relDeps).Count();
                    score += sharedDeps * 2.0;
                }
            }
        }

        // Score based on how many files depend on this file (importance)
        var dependentCount = index.DependencyGraph.Values
            .Count(deps => deps.Contains(file.FilePath));
        score += Math.Min(dependentCount, 10) * 1.0;

        return score;
    }

    private double GetImportanceScore(FileIndex file, CodebaseIndex index)
    {
        if (index.ImportanceScores.TryGetValue(file.FilePath, out var score))
        {
            return score / 10.0; // Normalize to 0-10 range
        }
        return 0.0;
    }

    private double GetInteractionScore(string filePath)
    {
        var interactionCount = _userInteractionCounts.GetValueOrDefault(filePath, 0);
        
        // Logarithmic scaling for interaction count
        var countScore = Math.Log(1 + interactionCount) * 2.0;

        // Bonus for recent access
        var recencyScore = 0.0;
        if (_lastAccessTimes.TryGetValue(filePath, out var lastAccess))
        {
            var hoursSinceAccess = (DateTime.UtcNow - lastAccess).TotalHours;
            if (hoursSinceAccess <= 1)
                recencyScore = 5.0;
            else if (hoursSinceAccess <= 24)
                recencyScore = 3.0;
            else if (hoursSinceAccess <= 168) // 1 week
                recencyScore = 1.0;
        }

        return countScore + recencyScore;
    }

    private double CalculateSimilarityScore(
        FileIndex file1,
        FileIndex file2,
        CodebaseIndex index)
    {
        var score = 0.0;

        // Same language
        if (file1.Language == file2.Language)
            score += 5.0;

        // Same directory or nearby
        var dir1 = Path.GetDirectoryName(file1.FilePath);
        var dir2 = Path.GetDirectoryName(file2.FilePath);
        if (dir1 == dir2)
            score += 10.0;
        else if (dir1?.StartsWith(dir2 ?? "") == true || dir2?.StartsWith(dir1 ?? "") == true)
            score += 5.0;

        // Shared dependencies
        var sharedDeps = file1.Dependencies.Intersect(file2.Dependencies).Count();
        score += sharedDeps * 3.0;

        // Similar symbol names (basic heuristic)
        var symbol1Names = file1.Symbols.Select(s => s.Name).ToHashSet();
        var symbol2Names = file2.Symbols.Select(s => s.Name).ToHashSet();
        var sharedSymbols = symbol1Names.Intersect(symbol2Names).Count();
        score += sharedSymbols * 2.0;

        // Similar file size
        var sizeRatio = Math.Min(file1.SizeBytes, file2.SizeBytes) / 
                       (double)Math.Max(file1.SizeBytes, file2.SizeBytes);
        score += sizeRatio * 2.0;

        return score;
    }

    private List<string> ExtractTerms(string query)
    {
        // Split query into terms, remove common words
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
            "have", "has", "had", "do", "does", "did", "will", "would", "should",
            "could", "may", "might", "can", "what", "where", "when", "why", "how"
        };

        var terms = Regex.Split(query, @"\W+")
            .Where(term => term.Length > 2 && !stopWords.Contains(term))
            .Select(term => term.ToLower())
            .Distinct()
            .ToList();

        return terms;
    }
}

/// <summary>
/// A file with its relevance score
/// </summary>
public class ScoredFile
{
    public FileIndex File { get; set; } = null!;
    public double Score { get; set; }
    public RelevanceFactors Factors { get; set; } = new();
}

/// <summary>
/// Individual scoring factors for transparency
/// </summary>
public class RelevanceFactors
{
    public double KeywordScore { get; set; }
    public double RecencyScore { get; set; }
    public double DependencyScore { get; set; }
    public double ImportanceScore { get; set; }
    public double InteractionScore { get; set; }

    public override string ToString()
    {
        return $"Keyword:{KeywordScore:F1}, Recency:{RecencyScore:F1}, " +
               $"Dependency:{DependencyScore:F1}, Importance:{ImportanceScore:F1}, " +
               $"Interaction:{InteractionScore:F1}";
    }
}
