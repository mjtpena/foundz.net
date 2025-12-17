using Foundz.Net.Tools.File;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class FileToolsTests : IDisposable
{
    private readonly string _testDirectory;

    public FileToolsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "foundz_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); } catch { }
        }
    }

    #region ReadFileTool Tests
    
    [Fact]
    public async Task ReadFileTool_ShouldReadFileContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World\nLine 2\nLine 3");
        var tool = new ReadFileTool();
        var args = new Dictionary<string, object> { ["path"] = testFile };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Hello World");
        result.Output.Should().Contain("Line 2");
    }

    [Fact]
    public async Task ReadFileTool_ShouldReadLineRange()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "lines.txt");
        await File.WriteAllTextAsync(testFile, "Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        var tool = new ReadFileTool();
        var args = new Dictionary<string, object> 
        { 
            ["path"] = testFile,
            ["startLine"] = 2,
            ["endLine"] = 4
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Line 2");
        result.Output.Should().Contain("Line 3");
        result.Output.Should().Contain("Line 4");
        result.Output.Should().NotContain("Line 1");
        result.Output.Should().NotContain("Line 5");
    }

    [Fact]
    public async Task ReadFileTool_ShouldFailForNonExistentFile()
    {
        // Arrange
        var tool = new ReadFileTool();
        var args = new Dictionary<string, object> { ["path"] = "nonexistent.txt" };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ReadFileTool_ShouldValidateArgs()
    {
        // Arrange
        var tool = new ReadFileTool();

        // Act & Assert
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["path"] = "test.txt" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    #endregion

    #region WriteFileTool Tests

    [Fact]
    public async Task WriteFileTool_ShouldCreateFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "write_test.txt");
        var tool = new WriteFileTool();
        var args = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = "Test content"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(testFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Test content");
    }

    [Fact]
    public async Task WriteFileTool_ShouldOverwriteExistingFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "overwrite.txt");
        await File.WriteAllTextAsync(testFile, "Original");
        var tool = new WriteFileTool();
        var args = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = "New content"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("New content");
        result.Output.Should().Contain("Backup created");
    }

    [Fact]
    public async Task WriteFileTool_ShouldCreateDirectory()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "subdir", "test.txt");
        var tool = new WriteFileTool();
        var args = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = "Content"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(testFile).Should().BeTrue();
    }

    [Fact]
    public async Task WriteFileTool_ShouldValidateArgs()
    {
        // Arrange
        var tool = new WriteFileTool();

        // Act & Assert
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["path"] = "test.txt",
            ["content"] = "test"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    #endregion

    #region DeleteFileTool Tests

    [Fact]
    public async Task DeleteFileTool_ShouldDeleteFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "delete.txt");
        await File.WriteAllTextAsync(testFile, "Delete me");
        var tool = new DeleteFileTool();
        var args = new Dictionary<string, object> { ["path"] = testFile };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(testFile).Should().BeFalse();
        result.Output.Should().Contain("Deleted file");
    }

    [Fact]
    public async Task DeleteFileTool_ShouldDeleteEmptyDirectory()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "emptydir");
        Directory.CreateDirectory(testDir);
        var tool = new DeleteFileTool();
        var args = new Dictionary<string, object> { ["path"] = testDir };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(testDir).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileTool_ShouldDeleteRecursively()
    {
        // Arrange
        var testDir = Path.Combine(_testDirectory, "recursivedir");
        Directory.CreateDirectory(testDir);
        await File.WriteAllTextAsync(Path.Combine(testDir, "file.txt"), "content");
        var tool = new DeleteFileTool();
        var args = new Dictionary<string, object> 
        { 
            ["path"] = testDir,
            ["recursive"] = true
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(testDir).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileTool_ShouldFailForNonExistent()
    {
        // Arrange
        var tool = new DeleteFileTool();
        var args = new Dictionary<string, object> { ["path"] = "nonexistent.txt" };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region CopyFileTool Tests

    [Fact]
    public async Task CopyFileTool_ShouldCopyFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "source.txt");
        var destFile = Path.Combine(_testDirectory, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "Copy me");
        var tool = new CopyFileTool();
        var args = new Dictionary<string, object>
        {
            ["sourcePath"] = sourceFile,
            ["destinationPath"] = destFile
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(destFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(destFile);
        content.Should().Be("Copy me");
    }

    #endregion

    #region RenameFileTool Tests

    [Fact]
    public async Task RenameFileTool_ShouldRenameFile()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old.txt");
        var newPath = Path.Combine(_testDirectory, "new.txt");
        await File.WriteAllTextAsync(oldPath, "Rename me");
        var tool = new RenameFileTool();
        var args = new Dictionary<string, object>
        {
            ["oldPath"] = oldPath,
            ["newPath"] = newPath
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(newPath).Should().BeTrue();
        File.Exists(oldPath).Should().BeFalse();
    }

    #endregion

    #region ListDirectoryTool Tests

    [Fact]
    public async Task ListDirectoryTool_ShouldListFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file1.txt"), "test");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file2.txt"), "test");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "subdir"));
        var tool = new ListDirectoryTool();
        var args = new Dictionary<string, object> { ["path"] = _testDirectory };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("file1.txt");
        result.Output.Should().Contain("file2.txt");
        result.Output.Should().Contain("subdir");
    }

    #endregion

    #region CreateDirectoryTool Tests

    [Fact]
    public async Task CreateDirectoryTool_ShouldCreateDirectory()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "newdir");
        var tool = new CreateDirectoryTool();
        var args = new Dictionary<string, object> { ["path"] = newDir };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(newDir).Should().BeTrue();
    }

    #endregion

    #region GetFileInfoTool Tests

    [Fact]
    public async Task GetFileInfoTool_ShouldReturnInfo()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "info.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var tool = new GetFileInfoTool();
        var args = new Dictionary<string, object> { ["path"] = testFile };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("info.txt");
        result.Output.Should().Contain("Size");
    }

    #endregion

    #region EditFileTool Tests

    [Fact]
    public async Task EditFileTool_ShouldEditFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "edit.txt");
        await File.WriteAllTextAsync(testFile, "Line 1\nLine 2\nLine 3");
        var tool = new EditFileTool();
        var args = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["oldText"] = "Line 2",
            ["newText"] = "Modified Line 2"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("Modified Line 2");
    }

    #endregion

    #region SearchFilesTool Tests

    [Fact]
    public async Task SearchFilesTool_ShouldFindFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test1.txt"), "test");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test2.cs"), "test");
        var tool = new SearchFilesTool();
        var args = new Dictionary<string, object> 
        { 
            ["directory"] = _testDirectory,
            ["pattern"] = "*.txt"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("test1.txt");
    }

    #endregion
}
