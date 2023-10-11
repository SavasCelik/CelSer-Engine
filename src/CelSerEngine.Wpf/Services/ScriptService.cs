using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;

/// <inheritdoc cref="IScriptService"/>
public class ScriptService : IScriptService
{
    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    private readonly ScriptCompiler _scriptCompiler;

    public ScriptService(CelSerEngineDbContext celSerEngineDbContext, ScriptCompiler scriptCompiler)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
        _scriptCompiler = scriptCompiler;
    }

    /// <inheritdoc />
    public async Task InsertScriptAsync(Script script, string targetProcessName)
    {
        TargetProcess? targetProcess = await _celSerEngineDbContext
            .TargetProcesses
            .SingleOrDefaultAsync(x => x.Name == targetProcessName)
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

        await _celSerEngineDbContext.Scripts.AddAsync(script).ConfigureAwait(false);
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateScriptAsync(IScript script)
    {
        var dbScript = await _celSerEngineDbContext.Scripts.Where(x => x.Id == script.Id).FirstAsync().ConfigureAwait(false);
        dbScript.Logic = script.Logic;
        dbScript.Name = script.Name;
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteScriptAsync(IScript script)
    {
        var dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == script.Id).ConfigureAwait(false);
        _celSerEngineDbContext.Scripts.Remove(dbScript);
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IList<Script>> GetScriptsByTargetProcessNameAsync(string targetProcessName)
    {
        IList<Script> dbScripts = await _celSerEngineDbContext.Scripts
            .AsNoTracking()
            .Include(x => x.TargetProcess)
            .Where(x => x.TargetProcess != null && x.TargetProcess.Name == targetProcessName)
            .ToListAsync()
            .ConfigureAwait(false);

        return dbScripts;
    }

    /// <inheritdoc />
    public async Task<IScript> ImportScriptAsync(string pathToFile, string targetProcessName)
    {
        var scriptAsJson = await File.ReadAllTextAsync(pathToFile).ConfigureAwait(false);
        var importedScript = JsonSerializer.Deserialize<Script>(scriptAsJson)!;
        importedScript.Id = 0;
        await InsertScriptAsync(importedScript, targetProcessName).ConfigureAwait(false);

        return importedScript;
    }

    /// <inheritdoc />
    public async Task ExportScriptAsync(IScript script, string exportPath)
    {
        var scriptAsJson = JsonSerializer.Serialize(script);
        await File.WriteAllTextAsync(exportPath, scriptAsJson).ConfigureAwait(false);
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
