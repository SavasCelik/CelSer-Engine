namespace CelSerEngine.Core.Scripting.Template;
public static class ScriptTemplates
{
    public const string BasicTemplate = $@"using System;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Core.Scripting;

public class LoopingScript : {nameof(ILoopingScript)} 
{{
    /// <summary>
    /// Will be called once during when starting.
    /// </summary>
    /// <param name=""memoryManager"">for reading and writing to the memory</param>
	public void {nameof(ILoopingScript.OnStart)}({nameof(MemoryManager)} memoryManager)
    {{
        // Add your setup code here.
    }}

    /// <summary>
    /// Will be called in a loop.
    /// </summary>
    /// <param name=""memoryManager"">for reading and writing to the memory</param>
	public void {nameof(ILoopingScript.OnLoop)}({nameof(MemoryManager)} memoryManager)
    {{
        // Add your main logic here.
		ScriptLogger.Print(""Hello World"");
	}}

    /// <summary>
    /// Will be called once when deactivated.
    /// </summary>
    /// <param name=""memoryManager"">for reading and writing to the memory</param>
	public void {nameof(ILoopingScript.OnStop)}({nameof(MemoryManager)} memoryManager)
    {{
        // Add your cleaning up here.
    }}
}}";
}
