namespace CelSerEngine.Core.Models;
public class Script : BaseScript
{
    public int TargetProcessId { get; set; }
    public TargetProcess? TargetProcess { get; set; }
}
