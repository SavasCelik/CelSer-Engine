namespace CelSerEngine.Core.Scripting.Template;
public static class ScriptTemplates
{
    public const string BasicTemplate = $@"using System;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Core.Scripting;

public class LoopingScript : {nameof(ILoopingScript)}
{{
	public void {nameof(ILoopingScript.OnLoop)}({nameof(MemoryManager)} memoryManager)
    {{
		ScriptLogger.Print(""Hello World"");
	}}
	public void {nameof(ILoopingScript.OnStart)}({nameof(MemoryManager)} memoryManager){{}}
	public void {nameof(ILoopingScript.OnStop)}({nameof(MemoryManager)} memoryManager){{}}
}}";
}
