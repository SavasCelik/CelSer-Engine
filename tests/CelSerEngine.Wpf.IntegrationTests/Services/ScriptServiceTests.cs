using CelSerEngine.Core.Database;
using CelSerEngine.Core.Database.Repositories;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Services;
using Microsoft.EntityFrameworkCore;
using System.IO.Abstractions;
using System.Text.Json;
using Xunit;

namespace CelSerEngine.Wpf.IntegrationTests.Services;

public class ScriptServiceTests : IDisposable
{
    private readonly IScriptRepository _scriptRepository;
    private readonly IScriptService _scriptService;
    private readonly IFileSystem _fileSystem; // Using real file system
    private List<string> _createdFiles; // To track created files and cleanup later
    private readonly string _scriptTempPath;

    public ScriptServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<CelSerEngineDbContext>();
        dbOptions.UseInMemoryDatabase(databaseName: $"InMemoryDb{Guid.NewGuid()}");
        var inMemoryDbContext = new CelSerEngineDbContext(dbOptions.Options);
        _scriptRepository = new ScriptRepository(inMemoryDbContext);
        var scriptCompiler = new ScriptCompiler();
        _fileSystem = new FileSystem(); // Using real file system
        _scriptTempPath = Path.GetTempPath() + "CelSerEngine";

        if (!_fileSystem.Directory.Exists(_scriptTempPath))
        {
            _fileSystem.Directory.CreateDirectory(_scriptTempPath);
        }

        _scriptService = new ScriptService(_scriptRepository, scriptCompiler, _fileSystem);
        _createdFiles = new List<string>();
    }

    [Fact]
    public async Task InsertScriptAsync_WhenTargetProcessDoesNotExist_ShouldInsertNewTargetProcess()
    {
        // Arrange
        var script = new Script();
        const string targetProcessName = "TestProcessForInsert";

        // Act
        await _scriptService.InsertScriptAsync(script, targetProcessName);

        // Assert
        var insertedScript = await _scriptRepository.GetScriptByIdAsync(script.Id);
        Assert.NotNull(insertedScript);
    }

    [Theory]
    [InlineData("NewScriptName")]
    [InlineData("")]
    public async Task UpdateScriptAsync_ChangeName_ShouldUpdateScriptName(string newName)
    {
        // Arrange
        var originalScript = new Script { Name = "ChangeName", Logic = "Logic" };
        await _scriptRepository.AddScriptAsync(originalScript);
        originalScript.Name = newName;

        // Act
        await _scriptService.UpdateScriptAsync(originalScript);

        // Assert
        var updatedScript = await _scriptRepository.GetScriptByIdAsync(originalScript.Id);
        Assert.Equal(newName, updatedScript.Name);
    }

    [Theory]
    [InlineData(ScriptTemplates.BasicTemplate)] // valid
    [InlineData("string myString : ????")] // invalid
    public async Task UpdateScriptAsync_ValidOrInvalidLogic_ShouldUpdateScriptLogic(string newLogic)
    {
        // Arrange
        var originalScript = new Script { Name = "ChangeLogic", Logic = "OldLogic" };
        await _scriptRepository.AddScriptAsync(originalScript);
        originalScript.Logic = newLogic;

        // Act
        await _scriptService.UpdateScriptAsync(originalScript);

        // Assert
        var updatedScript = await _scriptRepository.GetScriptByIdAsync(originalScript.Id);
        Assert.Equal(newLogic, updatedScript.Logic);
    }

    [Fact]
    public async Task DuplicateScriptAsync_ShouldDuplicateScript()
    {
        // Arrange
        var originalScript = new Script { Name = "Original", Logic = "OriginalLogic" };
        await _scriptRepository.AddScriptAsync(originalScript);

        // Act
        var duplicatedScript = await _scriptService.DuplicateScriptAsync(originalScript);

        // Assert
        var insertedScript = await _scriptRepository.GetScriptByIdAsync(duplicatedScript.Id);
        Assert.NotNull(insertedScript);
        Assert.NotNull(duplicatedScript);
        Assert.NotEqual(originalScript.Id, duplicatedScript.Id);
        Assert.Equal(originalScript.Name, duplicatedScript.Name);
    }

    [Fact]
    public async Task DeleteScriptAsync_ShouldRemoveScript()
    {
        // Arrange
        var script = new Script { Name = "ToDelete", Logic = "TestLogic" };
        await _scriptRepository.AddScriptAsync(script);

        // Act
        await _scriptService.DeleteScriptAsync(script);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _scriptRepository.GetScriptByIdAsync(script.Id));
    }

    [Fact]
    public async Task GetScriptsByTargetProcessNameAsync_ShouldReturnScripts()
    {
        // Arrange
        const string targetProcessName = "TargetProcessForGetScriptsByTargetProcessName";
        var script1 = new Script { Name = "Script1", Logic = "Logic1", TargetProcess = new TargetProcess { Name = targetProcessName } };
        var script2 = new Script { Name = "Script2", Logic = "Logic2", TargetProcess = new TargetProcess { Name = targetProcessName } };
        await _scriptRepository.AddScriptAsync(script1);
        await _scriptRepository.AddScriptAsync(script2);

        // Act
        var scripts = await _scriptService.GetScriptsByTargetProcessNameAsync(targetProcessName);

        // Assert
        Assert.Equal(2, scripts.Count);
        Assert.Contains(scripts, s => s.Name == "Script1");
        Assert.Contains(scripts, s => s.Name == "Script2");
    }

    [Fact]
    public void ValidateScript_WithValidLogic_ShouldCompileScript()
    {
        // Arrange
        var script = new Script { Name = "ValidScript", Logic = ScriptTemplates.BasicTemplate };

        // Act
        var result = _scriptService.ValidateScript(script);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ValidateScript_WithInvalidLogic_Throws()
    {
        // Arrange
        var script = new Script { Name = "invalidScript", Logic = "InvalidLogic" };

        // Act & Assert
        Assert.Throws<ScriptValidationException>(() => _scriptService.ValidateScript(script));
    }

    [Fact]
    public async Task ImportScriptAsync_ValidJson_ShouldImportScriptFromFile()
    {
        // Arrange
        var script = new Script { Name = "Import", Logic = "TestLogic" };
        var scriptJson = JsonSerializer.Serialize(script);
        var fileTempPath = Path.Combine(_scriptTempPath, Guid.NewGuid().ToString() + ".json");
        await _fileSystem.File.WriteAllTextAsync(fileTempPath, scriptJson);
        _createdFiles.Add(fileTempPath); // Track the file for cleanup

        // Act
        var importedScript = await _scriptService.ImportScriptAsync(fileTempPath, "TestProcessForImportScript");

        // Assert
        var insertedScript = await _scriptRepository.GetScriptByIdAsync(importedScript.Id);
        Assert.NotNull(insertedScript);
        Assert.Equal(script.Name, importedScript.Name);
        Assert.Equal(script.Logic, importedScript.Logic);
        Assert.NotEqual(0, importedScript.Id);
    }

    [Fact]
    public async Task ImportScriptAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var script = new Script { Name = "Import", Logic = "TestLogic" };
        var scriptInvalidJson = script.ToString();
        var fileTempPath = Path.Combine(_scriptTempPath, Guid.NewGuid().ToString() + ".json");
        await _fileSystem.File.WriteAllTextAsync(fileTempPath, scriptInvalidJson);
        _createdFiles.Add(fileTempPath); // Track the file for cleanup

        // Act && Assert
        await Assert.ThrowsAsync<JsonException>(() => _scriptService.ImportScriptAsync(fileTempPath, "TestProcessForImportScript"));
    }

    [Fact]
    public async Task ExportScriptAsync_ShouldExportScriptToFile()
    {
        // Arrange
        var sampleScript = new Script { Name = "Export", Logic = "TestLogic" };
        var exportPath = Path.Combine(_scriptTempPath, Guid.NewGuid().ToString() + ".json");

        // Act
        await _scriptService.ExportScriptAsync(sampleScript, exportPath);

        // Assert
        var exportedContent = await _fileSystem.File.ReadAllTextAsync(exportPath);
        Assert.NotNull(exportedContent);
        var exportedScript = JsonSerializer.Deserialize<Script>(exportedContent);
        Assert.NotNull(exportedScript);
        Assert.Equal(sampleScript.Name, exportedScript.Name);
        Assert.Equal(sampleScript.Logic, exportedScript.Logic);

        _createdFiles.Add(exportPath); // Track the file for cleanup
    }

    public void Dispose()
    {
        // Cleanup resources, delete created files.
        foreach (var filePath in _createdFiles)
        {
            if (_fileSystem.File.Exists(filePath))
            {
                _fileSystem.File.Delete(filePath);
            }
        }
    }
}
