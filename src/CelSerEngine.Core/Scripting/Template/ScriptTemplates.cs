namespace CelSerEngine.Core.Scripting.Template;
public static class ScriptTemplates
{
    public static string BasicTemplate = @"using System;
using CelSerEngine.Core.Scripting.Template;

public class LoopingScript : ILoopingScript {
	public void OnLoop() {
		ScriptLogger.Print(""Hello World"");
	}
	public void OnStart(){}
	public void OnStop(){}
}";
}
