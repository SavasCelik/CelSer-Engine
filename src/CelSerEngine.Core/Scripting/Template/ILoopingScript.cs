namespace CelSerEngine.Core.Scripting.Template;

/// <summary>
/// This interface is used when writing scripts. Scripts must implement this interface in order to be a valid script.
/// </summary>
public interface ILoopingScript
{
    /// <summary>
    /// Will be called once when activated.
    /// </summary>
    /// <param name="memoryManager">for reading and writing to the memory</param>
    public void OnStart(MemoryManager memoryManager);

    /// <summary>
    /// Will be called in a loop.
    /// </summary>
    /// <param name="memoryManager">for reading and writing to the memory</param>
    public void OnLoop(MemoryManager memoryManager);

    /// <summary>
    /// Will be called once when deactivated.
    /// </summary>
    /// <param name="memoryManager">for reading and writing to the memory</param>
    public void OnStop(MemoryManager memoryManager);
}
