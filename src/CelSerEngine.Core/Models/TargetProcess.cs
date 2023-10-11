namespace CelSerEngine.Core.Models;

/// <summary>
/// This class represents a process.
/// It's used to provide context or an association for scripts to interact with or target a specific process.
/// </summary>
public class TargetProcess
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
