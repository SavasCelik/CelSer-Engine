using CelSerEngine.Core.Database;
using CelSerEngine.Core.Database.Repositories;
using CelSerEngine.Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.Repositories;
public class ScriptRepositoryTests
{
    private readonly ScriptRepository _scriptRepository;

    public ScriptRepositoryTests()
    {
        var dbOptions = new DbContextOptionsBuilder<CelSerEngineDbContext>();
        dbOptions.UseInMemoryDatabase(databaseName: "InMemoryDb");
        var inMemoryDbContext = new CelSerEngineDbContext(dbOptions.Options);
        _scriptRepository = new ScriptRepository(inMemoryDbContext);

        // fill in memory db with fake data
        foreach (var script in GetFakeScripts())
        {
            inMemoryDbContext.Scripts.Add(script);
        }

        inMemoryDbContext.SaveChanges();
    }

    private static IEnumerable<Script> GetFakeScripts()
    {
        yield return new Script { Id = 1, Name = "Script1", Logic = "Logic1" };
        yield return new Script { Id = 2, Name = "Script2", Logic = "Logic2" };
        yield return new Script { Id = 3, Name = "Script3", Logic = "Logic3" };
        yield return new Script { Id = 4, Name = "Script4", Logic = "Logic4" };
        yield return new Script { Id = 5, Name = "Script5", Logic = "Logic5" };
        yield return new Script { Id = 6, Name = "Script6", Logic = "Logic6" };
        yield return new Script { Id = 7, Name = "Script7", Logic = "Logic7" };
        yield return new Script { Id = 8, Name = "Script8", Logic = "Logic8" };
        yield return new Script { Id = 9, Name = "Script9", Logic = "Logic9" };
        yield return new Script { Id = 10, Name = "Script10", Logic = "Logic10" };
    }

    [Fact]
    public async Task GetScriptById_ReturnsScript()
    {
        var dbScript = await _scriptRepository.GetScriptByIdAsync(1);

        Assert.NotNull(dbScript);
    }
}
