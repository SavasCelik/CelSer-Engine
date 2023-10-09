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
public class ScriptService : IScriptService
{
    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    private readonly ScriptCompiler _scriptCompiler;

    public ScriptService(CelSerEngineDbContext celSerEngineDbContext, ScriptCompiler scriptCompiler)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
        _scriptCompiler = scriptCompiler;
    }

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

    public async Task UpdateScriptAsync(IScript script)
    {
        var dbScript = await _celSerEngineDbContext.Scripts.Where(x => x.Id == script.Id).FirstAsync().ConfigureAwait(false);
        dbScript.Logic = script.Logic;
        dbScript.Name = script.Name;
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteScriptAsync(IScript script)
    {
        var dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == script.Id).ConfigureAwait(false);
        _celSerEngineDbContext.Scripts.Remove(dbScript);
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

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

    public async Task<IScript> ImportScriptAsync(string pathToFile, string targetProcessName)
    {
        var scriptAsJson = await File.ReadAllTextAsync(pathToFile).ConfigureAwait(false);
        var importedScript = JsonSerializer.Deserialize<Script>(scriptAsJson)!;
        importedScript.Id = 0;
        await InsertScriptAsync(importedScript, targetProcessName).ConfigureAwait(false);

        return importedScript;
    }

    public async Task ExportScriptAsync(IScript script, string exportPath)
    {
        var scriptAsJson = JsonSerializer.Serialize(script);
        await File.WriteAllTextAsync(exportPath, scriptAsJson).ConfigureAwait(false);
    }

    //throws exception
    public ILoopingScript ValidateScript(IScript script)
    {
        return _scriptCompiler.CompileScript(script);
    }

    //throws exception
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

    public void StopScript(ObservableScript script, MemoryManager memoryManager)
    {
        script.LoopingScript!.OnStop(memoryManager);
        script.ScriptState = ScriptState.Stopped;
    }

    private static void StartScript(ObservableScript script, MemoryManager memoryManager)
    {
        script.LoopingScript!.OnStart(memoryManager);
        script.ScriptState = ScriptState.Started;
    }
}
