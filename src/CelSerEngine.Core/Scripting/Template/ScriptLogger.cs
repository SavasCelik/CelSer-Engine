using System.Diagnostics;

namespace CelSerEngine.Core.Scripting.Template;
public class ScriptLogger
{
    public static void Print(string message)
    {
        Debug.WriteLine(message);
    }
}
