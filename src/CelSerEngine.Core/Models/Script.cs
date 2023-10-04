namespace CelSerEngine.Core.Models;
public class Script : IScript
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Logic { get; set; }
    public int TargetProcessId { get; set; }
    public TargetProcess? TargetProcess { get; set; }

    public Script()
    {
        Name = "Custom Script";
        Logic = "";
    }
}
