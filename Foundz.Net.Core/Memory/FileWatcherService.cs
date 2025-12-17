using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Memory;

/// <summary>
/// Watches file system for changes and triggers incremental reindexing.
/// Provides real-time index updates as files change.
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly CodebaseIndexer _indexer;
    private readonly IndexStorage _storage;
    private FileSystemWatcher? _watcher;
    private readonly HashSet<string> _pendingChanges;
    private readonly object _changeLock = new();
    private Timer? _debounceTimer;
    private const int DebounceMilliseconds = 2000; // Wait 2 seconds after last change

    public event EventHandler<FileChangedEventArgs>? FileChanged;
    public event EventHandler<IndexUpdatedEventArgs>? IndexUpdated;

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        CodebaseIndexer indexer,
        IndexStorage storage)
    {
        _logger = logger;
        _indexer = indexer;
        _storage = storage;
        _pendingChanges = new HashSet<string>();
    }

    /// <summary>
    /// Start watching a directory for changes
    /// </summary>
    public void StartWatching(string rootPath)
    {
        if (_watcher != null)
        {
            StopWatching();
        }

        try
        {
            _watcher = new FileSystemWatcher(rootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | 
                              NotifyFilters.LastWrite | 
                              NotifyFilters.CreationTime,
                Filter = "*.*"
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true;

            _logger.LogInformation("Started watching directory: {Path}", rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start watching directory: {Path}", rootPath);
            throw;
        }
    }

    /// <summary>
    /// Stop watching for changes
    /// </summary>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Deleted -= OnFileChanged;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Dispose();
            _watcher = null;

            _logger.LogInformation("Stopped watching directory");
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    /// <summary>
    /// Force process pending changes immediately
    /// </summary>
    public async Task ProcessPendingChangesAsync(CancellationToken cancellationToken = default)
    {
        List<string> changesToProcess;
        
        lock (_changeLock)
        {
            if (_pendingChanges.Count == 0)
                return;

            changesToProcess = new List<string>(_pendingChanges);
            _pendingChanges.Clear();
        }

        await ProcessChangesAsync(changesToProcess, cancellationToken);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        _logger.LogDebug("File {ChangeType}: {Path}", e.ChangeType, e.FullPath);

        lock (_changeLock)
        {
            _pendingChanges.Add(e.FullPath);
        }

        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = e.ChangeType.ToString()
        });

        // Reset debounce timer
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            async _ => await ProcessPendingChangesAsync(),
            null,
            DebounceMilliseconds,
            Timeout.Infinite);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        _logger.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);

        lock (_changeLock)
        {
            _pendingChanges.Add(e.OldFullPath); // Mark old path for removal
            _pendingChanges.Add(e.FullPath);   // Mark new path for indexing
        }

        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = "Renamed",
            OldFilePath = e.OldFullPath
        });

        // Reset debounce timer
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            async _ => await ProcessPendingChangesAsync(),
            null,
            DebounceMilliseconds,
            Timeout.Infinite);
    }

    private async Task ProcessChangesAsync(
        List<string> changedFiles, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_watcher?.Path == null)
                return;

            _logger.LogInformation("Processing {Count} file changes", changedFiles.Count);

            // Load existing index
            var existingIndex = await _storage.LoadAsync(_watcher.Path, cancellationToken);
            
            if (existingIndex == null)
            {
                // No existing index, do full reindex
                _logger.LogWarning("No existing index found, triggering full reindex");
                existingIndex = await _indexer.IndexDirectoryAsync(_watcher.Path, cancellationToken);
                await _storage.SaveAsync(existingIndex, cancellationToken);
            }
            else
            {
                // Incremental update
                var updatedIndex = await _indexer.IncrementalUpdateAsync(
                    _watcher.Path,
                    changedFiles,
                    existingIndex,
                    cancellationToken);

                await _storage.SaveAsync(updatedIndex, cancellationToken);

                IndexUpdated?.Invoke(this, new IndexUpdatedEventArgs
                {
                    RootPath = _watcher.Path,
                    UpdatedFileCount = changedFiles.Count,
                    TotalFileCount = updatedIndex.FileCount,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation(
                    "Index updated. {UpdatedCount} files processed, {TotalCount} total files",
                    changedFiles.Count,
                    updatedIndex.FileCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file changes");
        }
    }

    private bool ShouldIgnoreFile(string filePath)
    {
        // Ignore files in specific directories
        var ignoredPatterns = new[]
        {
            Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + ".vs" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + "node_modules" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + "packages" + Path.DirectorySeparatorChar
        };

        if (ignoredPatterns.Any(pattern => filePath.Contains(pattern)))
            return true;

        // Ignore specific file extensions
        var extension = Path.GetExtension(filePath).ToLower();
        var ignoredExtensions = new[] 
        { 
            ".dll", ".exe", ".pdb", ".cache", ".tmp", ".log",
            ".jpg", ".png", ".gif", ".ico", ".pdf", ".zip"
        };

        return ignoredExtensions.Contains(extension);
    }

    public void Dispose()
    {
        StopWatching();
        _debounceTimer?.Dispose();
    }
}

public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? OldFilePath { get; set; }
}

public class IndexUpdatedEventArgs : EventArgs
{
    public string RootPath { get; set; } = string.Empty;
    public int UpdatedFileCount { get; set; }
    public int TotalFileCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
