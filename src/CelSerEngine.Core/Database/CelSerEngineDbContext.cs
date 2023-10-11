using CelSerEngine.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CelSerEngine.Core.Database;

/// <summary>
/// Represents the database context for the CelSerEngine application.
/// This context provides an entry point to interact with the underlying database using Entity Framework Core.
/// </summary>
public class CelSerEngineDbContext : DbContext
{
    public DbSet<Script> Scripts { get; set; }
    public DbSet<TargetProcess> TargetProcesses { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CelSerEngineDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public CelSerEngineDbContext(DbContextOptions<CelSerEngineDbContext> options) : base(options)
    {
        // Creates the database if not exists
        Database.EnsureCreated();
    }
}
