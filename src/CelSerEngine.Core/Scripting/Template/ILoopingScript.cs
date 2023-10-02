namespace CelSerEngine.Core.Scripting.Template;
public interface ILoopingScript
{
    public void OnStart(MemoryManager memoryManager);
    public void OnLoop(MemoryManager memoryManager);
    public void OnStop(MemoryManager memoryManager);
}
