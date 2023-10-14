using CelSerEngine.Core.Database.Repositories;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;

/// <inheritdoc cref="IScriptService"/>
public class ScriptService : IScriptService
{
    private readonly IScriptRepository _scriptRepository;
    private readonly ScriptCompiler _scriptCompiler;
    private readonly IFileSystem _fileSystem;

    public ScriptService(IScriptRepository scriptRepository, ScriptCompiler scriptCompiler, IFileSystem fileSystem)
    {
        _scriptRepository = scriptRepository;
        _scriptCompiler = scriptCompiler;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task InsertScriptAsync(Script script, string targetProcessName)
    {
        TargetProcess? targetProcess = await _scriptRepository
            .GetTargetProcessByNameAsync(targetProcessName)
            .ConfigureAwait(false);

        if (targetProcess == null)
        {
            targetProcess = new TargetProcess
            {
                Name = targetProcessName
            };
            script.TargetProcess = targetProcess;
        }
        else
        {
            script.TargetProcessId = targetProcess.Id;
        }

        await _scriptRepository.AddScriptAsync(script).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Script> DuplicateScriptAsync(IScript script)
    {
        var dbScript = await _scriptRepository.GetScriptByIdAsync(script.Id);
        var duplicatedScript = new Script
        {
            Name = dbScript.Name,
            Logic = dbScript.Logic,
            TargetProcessId = dbScript.TargetProcessId
        };
        await _scriptRepository.AddScriptAsync(duplicatedScript).ConfigureAwait(false);

        return duplicatedScript;
    }

    /// <inheritdoc />
    public async Task UpdateScriptAsync(IScript script)
    {
        var dbScript = await _scriptRepository.GetScriptByIdAsync(script.Id).ConfigureAwait(false);
        dbScript.Logic = script.Logic;
        dbScript.Name = script.Name;
        await _scriptRepository.UpdateScriptAsync(dbScript).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteScriptAsync(IScript script)
    {
        await _scriptRepository.DeleteScriptByIdAsync(script.Id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IList<Script>> GetScriptsByTargetProcessNameAsync(string targetProcessName)
    {
        IList<Script> dbScripts = await _scriptRepository
            .GetScriptsByTargetProcessNameAsync(targetProcessName)
            .ConfigureAwait(false);

        return dbScripts;
    }

    /// <inheritdoc />
    public async Task<IScript> ImportScriptAsync(string pathToFile, string targetProcessName)
    {
        var scriptAsJson = await _fileSystem.File.ReadAllTextAsync(pathToFile).ConfigureAwait(false);
        var importedScript = JsonSerializer.Deserialize<Script>(scriptAsJson)!;
        importedScript.Id = 0;
        await InsertScriptAsync(importedScript, targetProcessName).ConfigureAwait(false);

        return importedScript;
    }

    /// <inheritdoc />
    public async Task ExportScriptAsync(IScript script, string exportPath)
    {
        var scriptAsJson = JsonSerializer.Serialize(script);
        await _fileSystem.File.WriteAllTextAsync(exportPath, scriptAsJson).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ScriptValidationException">Thrown if the script validation fails.</exception>
    public ILoopingScript ValidateScript(IScript script)
    {
        return _scriptCompiler.CompileScript(script);
    }

    /// <inheritdoc />
    /// /// <exception cref="ScriptValidationException">Thrown if the script validation fails.</exception>
    public void RunScript(ObservableScript script, MemoryManager memoryManager)
    {
        if (script.ScriptState == ScriptState.NotValidated || script.LoopingScript == null)
        {
            script.LoopingScript = ValidateScript(script);
            script.ScriptState = ScriptState.Validated;
        }

        if (script.ScriptState is ScriptState.Validated or ScriptState.Stopped)
            StartScript(script, memoryManager);

        if (script.ScriptState == ScriptState.Started)
            script.LoopingScript.OnLoop(memoryManager);
    }

    /// <inheritdoc />
    public void StopScript(ObservableScript script, MemoryManager memoryManager)
    {
        script.LoopingScript!.OnStop(memoryManager);
        script.ScriptState = ScriptState.Stopped;
    }

    /// <summary>
    /// Starts the execution of a specified observable script using a given memory manager.
    /// The <paramref name="memoryManager"/> is used for <see cref="ILoopingScript"/>'s methods
    /// </summary>
    /// <param name="script">The observable script to be stopped.</param>
    /// <param name="memoryManager">The memory manager used at execution.</param>
    private static void StartScript(ObservableScript script, MemoryManager memoryManager)
    {
        script.LoopingScript!.OnStart(memoryManager);
        script.ScriptState = ScriptState.Started;
    }
}
