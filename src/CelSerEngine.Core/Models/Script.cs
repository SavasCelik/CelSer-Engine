namespace CelSerEngine.Core.Models;

/// <summary>
/// This class implements the IScript interface, representing a script with properties related to its identity, naming, logic, and associated target process.
/// </summary>
public class Script : IScript
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Logic { get; set; }
    public int TargetProcessId { get; set; }
    public TargetProcess? TargetProcess { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Script"/> class with default values.
    /// </summary>
    public Script()
    {
        Name = "Custom Script";
        Logic = "";
    }
}
