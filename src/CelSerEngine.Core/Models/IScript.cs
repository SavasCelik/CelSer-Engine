namespace CelSerEngine.Core.Models;

/// <summary>
/// Provides a blueprint for classes that represent scripts. It contains properties related to the identity, naming, and logic of the script.
/// </summary>
public interface IScript
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Logic { get; set; }
}