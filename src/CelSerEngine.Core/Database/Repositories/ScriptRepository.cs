using CelSerEngine.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CelSerEngine.Core.Database.Repositories;

/// <inheritdoc cref="IScriptRepository"/>
public class ScriptRepository : IScriptRepository
{
    private readonly CelSerEngineDbContext _celSerEngineDbContext;

    public ScriptRepository(CelSerEngineDbContext celSerEngineDbContext)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
    }

    /// <inheritdoc />
    public async Task<Script> GetScriptById(int id)
    {
        var dbScript = await _celSerEngineDbContext.Scripts
            .Where(x => x.Id == id)
            .AsNoTracking()
            .FirstAsync()
            .ConfigureAwait(false);

        return dbScript;
    }

    /// <inheritdoc />
    public async Task AddScriptAsync(Script script)
    {
        await _celSerEngineDbContext.Scripts.AddAsync(script).ConfigureAwait(false);
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateScriptAsync(Script script)
    {
        _celSerEngineDbContext.Entry(script).State = EntityState.Modified;
        await _celSerEngineDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteScriptByIdAsync(int id)
    {
        var dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == id).ConfigureAwait(false);
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
    public async Task<TargetProcess?> GetTargetProcessByNameAsync(string targetProcessName)
    {
        TargetProcess? targetProcess = await _celSerEngineDbContext.TargetProcesses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == targetProcessName)
            .ConfigureAwait(false);

        return targetProcess;
    }
}
