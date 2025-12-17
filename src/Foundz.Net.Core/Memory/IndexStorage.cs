using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Memory;

/// <summary>
/// Manages persistent storage and in-memory caching of codebase indexes.
/// Implements LRU cache for efficient memory usage.
/// </summary>
public class IndexStorage
{
    private readonly ILogger<IndexStorage> _logger;
    private readonly string _storagePath;
    private readonly int _maxCacheSize;
    private readonly ConcurrentDictionary<string, CodebaseIndex> _cache;
    private readonly LinkedList<string> _lruList;
    private readonly object _lruLock = new();

    public IndexStorage(
        ILogger<IndexStorage> logger,
        string storagePath,
        int maxCacheSize = 10)
    {
        _logger = logger;
        _storagePath = storagePath;
        _maxCacheSize = maxCacheSize;
        _cache = new ConcurrentDictionary<string, CodebaseIndex>();
        _lruList = new LinkedList<string>();

        EnsureStorageDirectory();
    }

    /// <summary>
    /// Save index to persistent storage
    /// </summary>
    public async Task SaveAsync(
        CodebaseIndex index, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = GenerateFileName(index.RootPath);
            var filePath = Path.Combine(_storagePath, fileName);

            var json = JsonSerializer.Serialize(index, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            // Update cache
            UpdateCache(index.RootPath, index);

            _logger.LogInformation("Saved index to: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save index for: {RootPath}", index.RootPath);
            throw;
        }
    }

    /// <summary>
    /// Load index from storage or cache
    /// </summary>
    public async Task<CodebaseIndex?> LoadAsync(
        string rootPath, 
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue(rootPath, out var cachedIndex))
        {
            UpdateLRU(rootPath);
            _logger.LogDebug("Index loaded from cache: {Path}", rootPath);
            return cachedIndex;
        }

        // Load from disk
        try
        {
            var fileName = GenerateFileName(rootPath);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No saved index found for: {Path}", rootPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var index = JsonSerializer.Deserialize<CodebaseIndex>(json);

            if (index != null)
            {
                UpdateCache(rootPath, index);
                _logger.LogInformation("Index loaded from disk: {Path}", filePath);
            }

            return index;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load index for: {RootPath}", rootPath);
            return null;
        }
    }

    /// <summary>
    /// Check if an index needs updating (files changed since last index)
    /// </summary>
    public async Task<bool> NeedsUpdateAsync(
        string rootPath, 
        CancellationToken cancellationToken = default)
    {
        var index = await LoadAsync(rootPath, cancellationToken);
        
        if (index == null)
            return true;

        // Check if any files have been modified since indexing
        foreach (var file in index.Files)
        {
            if (!File.Exists(file.FilePath))
                return true; // File deleted

            var fileInfo = new FileInfo(file.FilePath);
            if (fileInfo.LastWriteTimeUtc > index.IndexedAt)
                return true; // File modified
        }

        // Check for new files
        var currentFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !IsIgnoredPath(f))
            .ToHashSet();

        var indexedFiles = index.Files.Select(f => f.FilePath).ToHashSet();

        return !currentFiles.SetEquals(indexedFiles);
    }

    /// <summary>
    /// Get list of files that changed since last index
    /// </summary>
    public async Task<List<string>> GetChangedFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default)
    {
        var index = await LoadAsync(rootPath, cancellationToken);
        var changedFiles = new List<string>();

        if (index == null)
            return changedFiles;

        // Check modified and deleted files
        foreach (var file in index.Files)
        {
            if (!File.Exists(file.FilePath))
            {
                changedFiles.Add(file.FilePath); // Deleted
            }
            else
            {
                var fileInfo = new FileInfo(file.FilePath);
                if (fileInfo.LastWriteTimeUtc > index.IndexedAt)
                {
                    changedFiles.Add(file.FilePath); // Modified
                }
            }
        }

        // Check for new files
        var currentFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !IsIgnoredPath(f))
            .ToList();

        var indexedPaths = index.Files.Select(f => f.FilePath).ToHashSet();
        changedFiles.AddRange(currentFiles.Where(f => !indexedPaths.Contains(f)));

        return changedFiles;
    }

    /// <summary>
    /// Delete stored index
    /// </summary>
    public async Task DeleteAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = GenerateFileName(rootPath);
            var filePath = Path.Combine(_storagePath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted index: {Path}", filePath);
            }

            // Remove from cache
            _cache.TryRemove(rootPath, out _);
            
            lock (_lruLock)
            {
                _lruList.Remove(rootPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete index for: {RootPath}", rootPath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// List all stored indexes
    /// </summary>
    public async Task<List<string>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = Directory.GetFiles(_storagePath, "index_*.json");
            var rootPaths = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var index = JsonSerializer.Deserialize<CodebaseIndex>(json);
                    if (index != null)
                    {
                        rootPaths.Add(index.RootPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read index file: {File}", file);
                }
            }

            return rootPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list indexes");
            return new List<string>();
        }
    }

    /// <summary>
    /// Clear all caches
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        
        lock (_lruLock)
        {
            _lruList.Clear();
        }

        _logger.LogInformation("Cache cleared");
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Count, int MaxSize, long MemoryBytes) GetCacheStats()
    {
        var memoryBytes = _cache.Values
            .Sum(index => EstimateSize(index));

        return (_cache.Count, _maxCacheSize, memoryBytes);
    }

    private void UpdateCache(string rootPath, CodebaseIndex index)
    {
        // Add/update in cache
        _cache[rootPath] = index;

        // Update LRU
        UpdateLRU(rootPath);

        // Evict if cache is full
        lock (_lruLock)
        {
            while (_lruList.Count > _maxCacheSize)
            {
                var leastRecent = _lruList.First!.Value;
                _lruList.RemoveFirst();
                _cache.TryRemove(leastRecent, out _);
                
                _logger.LogDebug("Evicted from cache: {Path}", leastRecent);
            }
        }
    }

    private void UpdateLRU(string rootPath)
    {
        lock (_lruLock)
        {
            _lruList.Remove(rootPath);
            _lruList.AddLast(rootPath);
        }
    }

    private string GenerateFileName(string rootPath)
    {
        // Create safe filename from root path
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rootPath)))
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "")
            .Substring(0, 16);

        return $"index_{hash}.json";
    }

    private void EnsureStorageDirectory()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory: {Path}", _storagePath);
        }
    }

    private bool IsIgnoredPath(string path)
    {
        var ignoredPatterns = new[] 
        { 
            "bin", "obj", "node_modules", ".git", ".vs", ".vscode",
            "packages", "dist", "build", "target"
        };

        return ignoredPatterns.Any(pattern => 
            path.Contains(Path.DirectorySeparatorChar + pattern + Path.DirectorySeparatorChar) ||
            path.Contains(Path.AltDirectorySeparatorChar + pattern + Path.AltDirectorySeparatorChar));
    }

    private long EstimateSize(CodebaseIndex index)
    {
        // Rough estimation of memory usage
        long size = 0;
        size += index.Files.Count * 1024; // Assume 1KB per file entry
        size += index.Files.Sum(f => f.Symbols.Count * 256); // Assume 256 bytes per symbol
        size += index.DependencyGraph.Count * 512; // Assume 512 bytes per graph entry
        return size;
    }
}
