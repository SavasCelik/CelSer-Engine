using CelSerEngine.Core.Database.Repositories;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Services;
using Moq;
using System.IO.Abstractions;
using System.Text.Json;
using Xunit;

namespace CelSerEngine.Wpf.UnitTests.Services;

public class ScriptServiceTests
{
    private readonly Mock<IScriptRepository> _mockScriptRepository;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ScriptCompiler> _mockScriptCompiler;
    private readonly ScriptService _scriptService;
    private readonly MemoryManager _stubMemoryManager;

    public ScriptServiceTests()
    {
        _mockScriptRepository = new Mock<IScriptRepository>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockScriptCompiler = new Mock<ScriptCompiler>();
        _scriptService = new ScriptService(_mockScriptRepository.Object, _mockScriptCompiler.Object, _mockFileSystem.Object);
        var stubINativeApi = new Mock<INativeApi>();
        _stubMemoryManager = new MemoryManager(IntPtr.Zero, stubINativeApi.Object);
    }

    [Fact]
    public async Task InsertScriptAsync_WithNewTargetProcess_SetsScriptTargetProcess()
    {
        // Arrange
        var script = new Script();
        const string targetProcessName = "TestTargetProcess";
        _mockScriptRepository.Setup(r => r.GetTargetProcessByNameAsync(targetProcessName))
            .ReturnsAsync(default(TargetProcess));

        // Act
        await _scriptService.InsertScriptAsync(script, targetProcessName);

        // Assert
        Assert.Equal(targetProcessName, script.TargetProcess?.Name);
    }

    [Theory]
    [InlineData(ScriptTemplates.BasicTemplate)] // valid
    [InlineData("string myString : ????")] // invalid
    public async Task UpdateScriptAsync_ValidOrInvalidLogic_UpdatesDbScript(string newLogic)
    {
        // Arrange
        var script = new Script { Id = 1, Name = "NewName", Logic = newLogic };
        var dbScript = new Script { Id = 1, Name = "OldName", Logic = "int myInteger = 42;" };
        _mockScriptRepository.Setup(r => r.GetScriptByIdAsync(script.Id))
            .ReturnsAsync(dbScript);

        // Act
        await _scriptService.UpdateScriptAsync(script);

        // Assert
        Assert.Equal(newLogic, dbScript.Logic);
        _mockScriptRepository.Verify(x => x.UpdateScriptAsync(It.IsAny<Script>()), Times.Once);
    }

    [Fact]
    public async Task UpdateScriptAsync_ChangedName_UpdatesDbScript()
    {
        // Arrange
        const string newName = "NewName";
        var script = new Script { Id = 1, Name = newName, Logic = "int myInteger = 42;" };
        var dbScript = new Script { Id = 1, Name = "OldName", Logic = "int myInteger = 42;" };
        _mockScriptRepository.Setup(r => r.GetScriptByIdAsync(script.Id))
            .ReturnsAsync(dbScript);

        // Act
        await _scriptService.UpdateScriptAsync(script);

        // Assert
        Assert.Equal(newName, dbScript.Name);
        _mockScriptRepository.Verify(x => x.UpdateScriptAsync(It.IsAny<Script>()), Times.Once);
    }

    [Fact]
    public async Task DuplicateScriptAsync_ReturnsDuplicatedScript()
    {
        // Arrange
        var expectedId = 1111;
        var dbScript = new Script { Id = 1, Name = "ScriptName", Logic = "int myInteger = 42;" };
        _mockScriptRepository.Setup(r => r.GetScriptByIdAsync(dbScript.Id))
            .ReturnsAsync(dbScript);
        _mockScriptRepository.Setup(r => r.AddScriptAsync(It.IsAny<Script>()))
            .Callback<Script>(x => x.Id = 1111);

        // Act
        var duplicatedScript = await _scriptService.DuplicateScriptAsync(dbScript);

        // Assert
        Assert.Equal(expectedId, duplicatedScript.Id);
        Assert.Equal(duplicatedScript.Name, dbScript.Name);
        Assert.Equal(duplicatedScript.Logic, dbScript.Logic);
    }

    [Fact]
    public async Task GetScriptsByTargetProcessNameAsync_ReturnsScripts()
    {
        // Arrange
        var expectedScripts = new List<Script>
        {
            new() { Id = 1, Name = "Script1" },
            new() { Id = 2, Name = "Script2" }
        };
        _mockScriptRepository.Setup(r => r.GetScriptsByTargetProcessNameAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedScripts);

        // Act
        var result = await _scriptService.GetScriptsByTargetProcessNameAsync("TestTargetProcess");

        // Assert
        Assert.Equal(expectedScripts, result);
    }

    [Fact]
    public async Task DeleteScriptAsync_DeletesScriptById()
    {
        // Arrange
        var script = new Script { Id = 1, Name = "Script1" };
        bool deleteCalled = false;
        _mockScriptRepository.Setup(r => r.DeleteScriptByIdAsync(script.Id))
            .Callback(() => deleteCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await _scriptService.DeleteScriptAsync(script);

        // Assert
        Assert.True(deleteCalled);
        _mockScriptRepository.Verify(x => x.DeleteScriptByIdAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void RunScript_NotValidated_CompilesAndStartsScript()
    {
        // Arrange
        var mockLoopingScript = new Mock<ILoopingScript>();
        var observableScript = GetTestObservableScript();
        observableScript.State = ScriptState.NotValidated;
        _mockScriptCompiler.Setup(c => c.CompileScript(observableScript)).Returns(mockLoopingScript.Object);

        // Act
        _scriptService.RunScript(observableScript, _stubMemoryManager);

        // Assert
        Assert.Equal(ScriptState.Started, observableScript.State);
        _mockScriptCompiler.Verify(x => x.CompileScript(observableScript), Times.Once);
        mockLoopingScript.Verify(x => x.OnStart(_stubMemoryManager), Times.Once);
    }

    [Fact]
    public void RunScript_Validated_StartsScript()
    {
        // Arrange
        var mockLoopingScript = new Mock<ILoopingScript>();
        var observableScript = GetTestObservableScript();
        observableScript.LoopingScript = mockLoopingScript.Object;
        observableScript.State = ScriptState.Validated;
        _mockScriptCompiler.Setup(c => c.CompileScript(observableScript)).Returns(mockLoopingScript.Object);

        // Act
        _scriptService.RunScript(observableScript, _stubMemoryManager);

        // Assert
        Assert.Equal(ScriptState.Started, observableScript.State);
        _mockScriptCompiler.Verify(x => x.CompileScript(observableScript), Times.Never);
        mockLoopingScript.Verify(x => x.OnStart(_stubMemoryManager), Times.Once);
    }

    [Fact]
    public void RunScript_Stopped_StartsScript()
    {
        // Arrange
        var mockLoopingScript = new Mock<ILoopingScript>();
        var observableScript = GetTestObservableScript();
        observableScript.LoopingScript = mockLoopingScript.Object;
        observableScript.State = ScriptState.Stopped;
        _mockScriptCompiler.Setup(c => c.CompileScript(observableScript)).Returns(mockLoopingScript.Object);

        // Act
        _scriptService.RunScript(observableScript, _stubMemoryManager);

        // Assert
        Assert.Equal(ScriptState.Started, observableScript.State);
        _mockScriptCompiler.Verify(x => x.CompileScript(observableScript), Times.Never);
        mockLoopingScript.Verify(x => x.OnStart(_stubMemoryManager), Times.Once);
    }

    [Fact]
    public void RunScript_Started_OnlyCallsOnLoop()
    {
        // Arrange
        var mockLoopingScript = new Mock<ILoopingScript>();
        var observableScript = GetTestObservableScript();
        observableScript.LoopingScript = mockLoopingScript.Object;
        observableScript.State = ScriptState.Started;
        _mockScriptCompiler.Setup(c => c.CompileScript(observableScript)).Returns(mockLoopingScript.Object);

        // Act
        _scriptService.RunScript(observableScript, _stubMemoryManager);

        // Assert
        Assert.Equal(ScriptState.Started, observableScript.State);
        _mockScriptCompiler.Verify(x => x.CompileScript(observableScript), Times.Never);
        mockLoopingScript.Verify(x => x.OnStart(_stubMemoryManager), Times.Never);
        mockLoopingScript.Verify(x => x.OnLoop(_stubMemoryManager), Times.Once);
    }

    [Theory]
    [InlineData(ScriptState.NotValidated)]
    [InlineData(ScriptState.Validated)]
    [InlineData(ScriptState.Started)]
    [InlineData(ScriptState.Stopped)]
    public void StopScript_AnyState_StopsLoopingScript(ScriptState scriptState)
    {
        // Arrange
        var mockLoopingScript = new Mock<ILoopingScript>();
        var observableScript = GetTestObservableScript();
        observableScript.LoopingScript = mockLoopingScript.Object;
        observableScript.State = scriptState;

        // Act
        _scriptService.StopScript(observableScript, _stubMemoryManager);

        // Assert
        mockLoopingScript.Verify(s => s.OnStop(_stubMemoryManager), Times.Once());
        Assert.Equal(ScriptState.Stopped, observableScript.State);
    }

    [Fact]
    public void ValidateScript_CallsCompiler()
    {
        // Arrange
        var script = new Script();
        var mockLoopingScript = new Mock<ILoopingScript>();
        _mockScriptCompiler.Setup(c => c.CompileScript(script)).Returns(mockLoopingScript.Object);

        // Act
        var loopingScript = _scriptService.ValidateScript(script);

        // Assert
        Assert.Equal(mockLoopingScript.Object, loopingScript);
        _mockScriptCompiler.Verify(c => c.CompileScript(script), Times.Once());
    }

    [Fact]
    public async Task ImportScriptAsync_ValidJsonFormat_ImportsFromFilePath()
    {
        // Arrange
        const string filePath = "D:/path/to/script.json";
        const string scriptContent = @"{ ""Id"": 2, ""Name"": ""Test"", ""Logic"": ""Logic"" }";
        _mockFileSystem.Setup(fs => fs.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptContent);

        // Act
        var importedScript = await _scriptService.ImportScriptAsync(filePath, "targetProcessName");

        // Assert
        Assert.Equal("Test", importedScript.Name);
        Assert.Equal("Logic", importedScript.Logic);
        Assert.Equal(0, importedScript.Id);
        _mockScriptRepository.Verify(x => x.AddScriptAsync(It.IsAny<Script>()), Times.Once);
        _mockFileSystem.Verify(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportScriptAsync_InvalidJsonFormat_ThrowsJsonException()
    {
        // Arrange
        const string filePath = "D:/path/to/script.json";
        const string scriptContent = @"{ <Id>2</id>, ""Name"": ""Test"", ""Logic"": ""Logic"" }";
        _mockFileSystem.Setup(fs => fs.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptContent);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _scriptService.ImportScriptAsync(filePath, "targetProcessName"));
        _mockScriptRepository.Verify(x => x.AddScriptAsync(It.IsAny<Script>()), Times.Never);
        _mockFileSystem.Verify(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportScriptAsync_WritesToFileSystem()
    {
        // Arrange
        const string exportPath = "D:/path/to/output.json";
        var scriptToExport = new Script { Id = 1, Name = "TestScript" };
        _mockFileSystem.Setup(fs => fs.File.WriteAllTextAsync(exportPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _scriptService.ExportScriptAsync(scriptToExport, exportPath);

        // Assert
        _mockFileSystem.Verify(x => x.File.WriteAllTextAsync(exportPath, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ObservableScript GetTestObservableScript() => new(1, "TestScript", "TestLogic");
}
