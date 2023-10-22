using CelSerEngine.Core.Database;
using CelSerEngine.Core.Database.Repositories;
using CelSerEngine.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Repositories;
public class ScriptRepositoryTests
{
    private readonly ScriptRepository _scriptRepository;

    public ScriptRepositoryTests()
    {
        var dbOptions = new DbContextOptionsBuilder<CelSerEngineDbContext>();
        dbOptions.UseInMemoryDatabase(databaseName: $"InMemoryDb{Guid.NewGuid()}");
        var inMemoryDbContext = new CelSerEngineDbContext(dbOptions.Options);
        _scriptRepository = new ScriptRepository(inMemoryDbContext);

        // fill in memory db with fake data
        foreach (var script in GetFakeScripts())
        {
            inMemoryDbContext.Scripts.Add(script);
        }

        inMemoryDbContext.SaveChanges();
        inMemoryDbContext.ChangeTracker.Clear();
    }

    private static IEnumerable<Script> GetFakeScripts()
    {
        var targetProcess = GetTestTargetProcess();
        yield return new Script { Id = 1, Name = "Script1", Logic = "Logic1", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 2, Name = "Script2", Logic = "Logic2", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 3, Name = "Script3", Logic = "Logic3", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 4, Name = "Script4", Logic = "Logic4", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 5, Name = "Script5", Logic = "Logic5", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 6, Name = "Script6", Logic = "Logic6", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 7, Name = "Script7", Logic = "Logic7", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 8, Name = "Script8", Logic = "Logic8", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 9, Name = "Script9", Logic = "Logic9", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
        yield return new Script { Id = 10, Name = "Script10", Logic = "Logic10", TargetProcessId = targetProcess.Id, TargetProcess = targetProcess };
    }

    private static TargetProcess GetTestTargetProcess() => new() { Id = 1, Name = "TargetProcess.exe" };

    [Fact]
    public async Task GetScriptByIdAsync_ReturnsScript()
    {
        var dbScript = await _scriptRepository.GetScriptByIdAsync(1);

        Assert.NotNull(dbScript);
    }

    [Fact]
    public async Task AddScriptAsync_WithExistingTargetProcess_AddsScript()
    {
        // Arrange
        var targetProcess = GetTestTargetProcess();
        var script = new Script
        {
            Name = "AddScript",
            Logic = "WithExistingTargetProcess",
            TargetProcessId = targetProcess.Id
        };

        // Act
        await _scriptRepository.AddScriptAsync(script);

        // Assert
        Assert.NotEqual(0, script.Id);
    }

    [Fact]
    public async Task AddScriptAsync_WithNewTargetProcess_AddsScriptAndTargetProcess()
    {
        // Arrange
        var targetProcess = new TargetProcess { Name = "NewTargetProcess.exe" };
        var script = new Script
        {
            Name = "AddScript",
            Logic = "WithExistingTargetProcess",
            TargetProcess = targetProcess
        };

        // Act
        await _scriptRepository.AddScriptAsync(script);

        // Assert
        Assert.NotEqual(0, script.Id);
        Assert.NotEqual(0, targetProcess.Id);
    }

    [Fact]
    public async Task UpdateScriptAsync_UpdatesScript()
    {
        // Arrange
        var script = GetFakeScripts().First();
        script.Name = "ChangedNameToNewName";
        script.Logic = "ChangedLogicToNewLogic";

        // Act
        await _scriptRepository.UpdateScriptAsync(script);
        var dbScript = await _scriptRepository.GetScriptByIdAsync(script.Id);

        // Assert
        Assert.Equal(script.Name, dbScript.Name);
        Assert.Equal(script.Logic, dbScript.Logic);
    }

    [Fact]
    public async Task DeleteScriptByIdAsync_DeletesScript()
    {
        // Arrange
        var dbScript = await _scriptRepository.GetScriptByIdAsync(9);

        // Act
        await _scriptRepository.DeleteScriptByIdAsync(dbScript.Id);

        // Assert
        Assert.NotNull(dbScript);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _scriptRepository.GetScriptByIdAsync(dbScript.Id));
    }

    [Fact]
    public async Task GetScriptsByTargetProcessNameAsync_ExistingTargetProcess_ReturnsListOfScripts()
    {
        // Arrange
        var targetProcess = GetTestTargetProcess();

        // Act
        var scripts = await _scriptRepository.GetScriptsByTargetProcessNameAsync(targetProcess.Name);

        // Assert
        Assert.NotEmpty(scripts);
    }

    [Fact]
    public async Task GetScriptsByTargetProcessNameAsync_NotExistingTargetProcess_ReturnsEmptyScriptList()
    {
        // Arrange
        var targetProcessName = "NewTargetProcess'sName.exe";

        // Act
        var scripts = await _scriptRepository.GetScriptsByTargetProcessNameAsync(targetProcessName);

        // Assert
        Assert.Empty(scripts);
    }

    [Fact]
    public async Task GetTargetProcessByNameAsync_ExistingTargetProcess_ReturnsTargetProcess()
    {
        // Arrange
        var targetProcess = GetTestTargetProcess();

        // Act
        var dbTargetProcess = await _scriptRepository.GetTargetProcessByNameAsync(targetProcess.Name);

        // Assert
        Assert.NotNull(dbTargetProcess);
    }

    [Fact]
    public async Task GetTargetProcessByNameAsync_NotExistingTargetProcess_ReturnsNull()
    {
        // Arrange
        var targetProcessName = "NewTargetProcess'sName.exe";

        // Act
        var dbTargetProcess = await _scriptRepository.GetTargetProcessByNameAsync(targetProcessName);

        // Assert
        Assert.Null(dbTargetProcess);
    }
}
