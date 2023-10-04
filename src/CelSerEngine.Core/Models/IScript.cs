namespace CelSerEngine.Core.Models;
public interface IScript
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Logic { get; set; }
}