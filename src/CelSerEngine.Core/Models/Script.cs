namespace CelSerEngine.Core.Models;
public class Script : BaseScript
{
    public int TargetProcessId { get; set; }
    public required TargetProcess TargetProcess { get; set; }
}
