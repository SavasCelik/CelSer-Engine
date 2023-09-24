using CelSerEngine.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CelSerEngine.Core.Database;

public class CelSerEngineDbContext : DbContext
{
    public DbSet<Script> Scripts { get; set; }

    public CelSerEngineDbContext(DbContextOptions<CelSerEngineDbContext> options) : base(options)
    {
        // Creates the database if not exists
        Database.EnsureCreated();
    }
}
